using Microsoft.EntityFrameworkCore;
using PhanTichThietKeYeuCau;
using PhanTichThietKeYeuCau.Models;
using System.Security.Cryptography;

namespace PhanTichThietKeYeuCau;

// ---------- Băm mật khẩu (PBKDF2-SHA256, salt ngẫu nhiên cho từng tài khoản) ----------
public static class MatKhau
{
    private const int SoLanLap = 100_000;

    public static string Hash(string matKhau)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var h = Rfc2898DeriveBytes.Pbkdf2(matKhau, salt, SoLanLap, HashAlgorithmName.SHA256, 32);
        return $"PBKDF2${SoLanLap}${Convert.ToBase64String(salt)}${Convert.ToBase64String(h)}";
    }

    public static bool Verify(string matKhau, string daLuu)
    {
        var p = daLuu.Split('$');
        if (p.Length != 4 || p[0] != "PBKDF2" || !int.TryParse(p[1], out var lap)) return false;
        var salt = Convert.FromBase64String(p[2]);
        var mong = Convert.FromBase64String(p[3]);
        var h = Rfc2898DeriveBytes.Pbkdf2(matKhau, salt, lap, HashAlgorithmName.SHA256, mong.Length);
        return CryptographicOperations.FixedTimeEquals(h, mong);
    }
}

// ---------- Danh sách chức năng được phân quyền ----------
public static class Chuc
{
    public const string TaiKhoan = "TaiKhoan";
    public const string VatTu = "VatTu";
    public const string NhaCungCap = "NhaCungCap";
    public const string DanhMucKhac = "DanhMucKhac";       // bộ phận, nhân viên, kho
    public const string KeHoachMua = "KeHoachMua";
    public const string DonDatHang = "DonDatHang";
    public const string KiemTraCL = "KiemTraCL";
    public const string NhapKho = "NhapKho";
    public const string TonKho = "TonKho";
    public const string LenhSanXuat = "LenhSanXuat";
    public const string CapPhat = "CapPhat";               // mở form cấp phát
    public const string BaoCao = "BaoCao";
    public const string DuyetChungTu = "DuyetChungTu";     // duyệt KHM, duyệt / hủy ĐĐH
    public const string LapPhieuCapPhat = "LapPhieuCapPhat";
    public const string XacNhanThucNhan = "XacNhanThucNhan";
}

public static class PhanQuyen
{
    public static readonly Dictionary<string, string> TenVaiTro = new()
    {
        ["ADMIN"] = "Quản trị phần mềm",
        ["QLVT"] = "Quản lý vật tư",
        ["NVMH"] = "Nhân viên mua hàng",
        ["KCS"] = "Nhân viên KCS",
        ["THUKHO"] = "Thủ kho",
        ["BPSX"] = "Bộ phận sản xuất",
        ["BGD"] = "Ban giám đốc",
    };

    private static readonly string[] TatCa =
    {
        Chuc.TaiKhoan, Chuc.VatTu, Chuc.NhaCungCap, Chuc.DanhMucKhac, Chuc.KeHoachMua, Chuc.DonDatHang,
        Chuc.KiemTraCL, Chuc.NhapKho, Chuc.TonKho, Chuc.LenhSanXuat, Chuc.CapPhat, Chuc.BaoCao,
        Chuc.DuyetChungTu, Chuc.LapPhieuCapPhat, Chuc.XacNhanThucNhan
    };

    private static readonly Dictionary<string, HashSet<string>> Ma = new()
    {
        ["ADMIN"] = TatCa.ToHashSet(),
        ["QLVT"] = TatCa.Where(c => c != Chuc.TaiKhoan).ToHashSet(),
        ["NVMH"] = new() { Chuc.NhaCungCap, Chuc.KeHoachMua, Chuc.DonDatHang, Chuc.TonKho },
        ["KCS"] = new() { Chuc.KiemTraCL },
        ["THUKHO"] = new() { Chuc.NhapKho, Chuc.TonKho, Chuc.CapPhat, Chuc.LapPhieuCapPhat, Chuc.XacNhanThucNhan },
        ["BPSX"] = new() { Chuc.LenhSanXuat, Chuc.CapPhat, Chuc.XacNhanThucNhan },
        ["BGD"] = new() { Chuc.TonKho, Chuc.BaoCao },
    };

    public static bool Co(string vaiTro, string chucNang) =>
        Ma.TryGetValue(vaiTro, out var s) && s.Contains(chucNang);

    public static string Ten(string vaiTro) => TenVaiTro.GetValueOrDefault(vaiTro, vaiTro);
}

// ---------- Người dùng đang đăng nhập ----------
public static class Session
{
    public static string TenDangNhap { get; private set; } = "";
    public static string HoTen { get; private set; } = "";
    public static string VaiTro { get; private set; } = "";
    public static string? MaNhanVien { get; private set; }

    public static bool DaDangNhap => TenDangNhap != "";
    public static string TenVaiTro => PhanQuyen.Ten(VaiTro);

    public static void Bat(TAI_KHOAN t)
    {
        TenDangNhap = t.TenDangNhap;
        HoTen = t.HoTen;
        VaiTro = t.VaiTro;
        MaNhanVien = string.IsNullOrWhiteSpace(t.MaNhanVien) ? null : t.MaNhanVien.Trim();
    }

    public static void Xoa() { TenDangNhap = HoTen = VaiTro = ""; MaNhanVien = null; }

    public static bool Co(string chucNang) => DaDangNhap && PhanQuyen.Co(VaiTro, chucNang);
}

// ---------- Xử lý tài khoản ----------
public static class TaiKhoanService
{
    public const string MatKhauMacDinh = "123456";   // chỉ dùng cho tài khoản mẫu, hãy đổi khi demo thật
    private const int MaxSai = 5;
    private const int KhoaPhut = 5;

    // Lần chạy đầu: nếu chưa có tài khoản nào thì tạo bộ tài khoản mẫu
    public static void KhoiTao()
    {
        using var db = new QuanLyTieuHaoVatTuContext();
        if (db.TAI_KHOAN.Any()) return;

        var nv = db.NHAN_VIEN.AsNoTracking().ToList().ToDictionary(n => UI.T(n.MaNhanVien), n => n.HoTen);

        void Them(string user, string vaiTro, string hoTen, string? maNv) =>
            db.TAI_KHOAN.Add(new TAI_KHOAN
            {
                TenDangNhap = user,
                MatKhauHash = MatKhau.Hash(MatKhauMacDinh),
                VaiTro = vaiTro,
                HoTen = hoTen,
                MaNhanVien = maNv,
                HoatDong = true,
                SoLanSai = 0,
                NgayTao = DateTime.Now
            });
        void ThemNv(string user, string vaiTro, string maNv)
        {
            if (nv.TryGetValue(maNv, out var ten)) Them(user, vaiTro, ten, maNv);
        }

        Them("admin", "ADMIN", "Quản trị phần mềm", null);
        Them("bgd", "BGD", "Ban giám đốc", null);
        ThemNv("qlvt", "QLVT", "NV001");
        ThemNv("thukho", "THUKHO", "NV002");
        ThemNv("kcs", "KCS", "NV003");
        ThemNv("nvmh", "NVMH", "NV004");
        ThemNv("bpsx", "BPSX", "NV005");
        db.SaveChanges();
    }

    public static TAI_KHOAN? DangNhap(string user, string matKhau, out string loi)
    {
        loi = "";
        user = user.Trim().ToLowerInvariant();
        using var db = new QuanLyTieuHaoVatTuContext();
        var tk = db.TAI_KHOAN.FirstOrDefault(t => t.TenDangNhap == user);

        if (tk == null) { loi = "Sai tên đăng nhập hoặc mật khẩu."; return null; }
        if (tk.HoatDong != true) { loi = "Tài khoản đã bị vô hiệu hóa. Hãy liên hệ quản trị viên."; return null; }
        if (tk.KhoaDen is DateTime kd && kd > DateTime.Now)
        { loi = $"Tài khoản đang tạm khóa do nhập sai nhiều lần. Thử lại sau {kd:HH:mm:ss}."; return null; }

        if (!MatKhau.Verify(matKhau, tk.MatKhauHash))
        {
            tk.SoLanSai++;
            if (tk.SoLanSai >= MaxSai)
            {
                tk.KhoaDen = DateTime.Now.AddMinutes(KhoaPhut);
                tk.SoLanSai = 0;
                loi = $"Nhập sai {MaxSai} lần. Tài khoản bị khóa {KhoaPhut} phút.";
            }
            else loi = $"Sai tên đăng nhập hoặc mật khẩu. Còn {MaxSai - tk.SoLanSai} lần thử.";
            db.SaveChanges();
            return null;
        }

        tk.SoLanSai = 0;
        tk.KhoaDen = null;
        tk.LanDangNhapCuoi = DateTime.Now;
        db.SaveChanges();
        return tk;
    }

    public static bool DoiMatKhau(string user, string cu, string moi, out string loi)
    {
        loi = "";
        using var db = new QuanLyTieuHaoVatTuContext();
        var tk = db.TAI_KHOAN.FirstOrDefault(t => t.TenDangNhap == user);
        if (tk == null || !MatKhau.Verify(cu, tk.MatKhauHash)) { loi = "Mật khẩu hiện tại không đúng."; return false; }
        tk.MatKhauHash = MatKhau.Hash(moi);
        db.SaveChanges();
        return true;
    }
}