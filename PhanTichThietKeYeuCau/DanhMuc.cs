using Microsoft.EntityFrameworkCore;
using PhanTichThietKeYeuCau;
using PhanTichThietKeYeuCau.Models;

namespace PhanTichThietKeYeuCau;

public static class Nguon
{
    public static List<ComboItem> BoPhan()
    {
        using var db = new QuanLyTieuHaoVatTuContext();
        return db.BO_PHAN.AsNoTracking().OrderBy(x => x.MaBoPhan).ToList()
            .Select(x => new ComboItem(UI.T(x.MaBoPhan), x.TenBoPhan)).ToList();
    }

    public static List<ComboItem> NhanVien(string? chucVu = null)
    {
        using var db = new QuanLyTieuHaoVatTuContext();
        var ds = db.NHAN_VIEN.AsNoTracking().OrderBy(x => x.MaNhanVien).ToList();
        if (chucVu != null && ds.Any(x => x.ChucVu == chucVu)) ds = ds.Where(x => x.ChucVu == chucVu).ToList();
        return ds.Select(x => new ComboItem(UI.T(x.MaNhanVien), x.HoTen)).ToList();
    }

    public static List<ComboItem> VatTu()
    {
        using var db = new QuanLyTieuHaoVatTuContext();
        return db.VAT_TU.AsNoTracking().OrderBy(x => x.MaVatTu).ToList()
            .Select(x => new ComboItem(UI.T(x.MaVatTu), $"{UI.T(x.MaVatTu)} - {x.TenVatTu}")).ToList();
    }

    public static List<ComboItem> NhaCungCap()
    {
        using var db = new QuanLyTieuHaoVatTuContext();
        return db.NHA_CUNG_CAP.AsNoTracking().OrderBy(x => x.MaNCC).ToList()
            .Select(x => new ComboItem(UI.T(x.MaNCC), x.TenNCC)).ToList();
    }

    public static List<ComboItem> KeHoachMua(params string[] trangThai)
    {
        using var db = new QuanLyTieuHaoVatTuContext();
        return db.KE_HOACH_MUA.AsNoTracking().Where(k => trangThai.Contains(k.TrangThai!))
            .OrderBy(k => k.MaKHM).ToList()
            .Select(k => new ComboItem(UI.T(k.MaKHM), $"{UI.T(k.MaKHM)} - {k.TenKeHoach}")).ToList();
    }

    public static List<ComboItem> DonDatHang()
    {
        using var db = new QuanLyTieuHaoVatTuContext();
        var ncc = db.NHA_CUNG_CAP.AsNoTracking().ToList().ToDictionary(n => UI.T(n.MaNCC), n => n.TenNCC);
        return db.DON_DAT_HANG.AsNoTracking().Where(d => d.TrangThai != "Hủy").OrderBy(d => d.MaDDH).ToList()
            .Select(d => new ComboItem(UI.T(d.MaDDH), $"{UI.T(d.MaDDH)} - {ncc.GetValueOrDefault(UI.T(d.MaNCC), "")}")).ToList();
    }

    public static List<ComboItem> LenhSanXuat()
    {
        using var db = new QuanLyTieuHaoVatTuContext();
        return db.LENH_SAN_XUAT.AsNoTracking().OrderBy(l => l.MaLSX).ToList()
            .Select(l => new ComboItem(UI.T(l.MaLSX), $"{UI.T(l.MaLSX)} - {l.TenSanPham}")).ToList();
    }
}

public static class DanhMucForms
{
    public static Form NhaCungCap() => new frmCrud<NHA_CUNG_CAP>("Nhà cung cấp", new[]
    {
        new Field("MaNCC", "Mã NCC") { IsKey = true, Required = true, MaxLen = 10 },
        new Field("TenNCC", "Tên NCC") { Required = true, MaxLen = 150 },
        new Field("DiaChi", "Địa chỉ") { MaxLen = 250 },
        new Field("SoDienThoai", "Số điện thoại") { MaxLen = 15 },
        new Field("Email", "Email") { MaxLen = 100 },
        new Field("MaSoThue", "Mã số thuế") { MaxLen = 15 },
        new Field("DanhGia", "Đánh giá", Kind.Combo) { Items = new[] { "Tốt", "Trung bình", "Kém" } },
    });

    public static Form BoPhan() => new frmCrud<BO_PHAN>("Bộ phận", new[]
    {
        new Field("MaBoPhan", "Mã bộ phận") { IsKey = true, Required = true, MaxLen = 10 },
        new Field("TenBoPhan", "Tên bộ phận") { Required = true, MaxLen = 100 },
        new Field("LoaiBoPhan", "Loại bộ phận", Kind.Combo)
            { Items = new[] { "Sản xuất", "Kho", "QC", "Hành chính" }, Editable = true },
        new Field("NguoiPhuTrach", "Người phụ trách") { MaxLen = 100 },
        new Field("SoDienThoai", "Số điện thoại") { MaxLen = 15 },
    });

    public static Form NhanVien() => new frmCrud<NHAN_VIEN>("Nhân viên", new[]
    {
        new Field("MaNhanVien", "Mã nhân viên") { IsKey = true, Required = true, MaxLen = 10 },
        new Field("HoTen", "Họ tên") { Required = true, MaxLen = 100 },
        new Field("ChucVu", "Chức vụ", Kind.Combo)
            { Items = new[] { "Quản lý vật tư", "Thủ kho", "KCS", "Nhân viên mua hàng", "Quản đốc xưởng" }, Editable = true },
        new Field("MaBoPhan", "Bộ phận", Kind.Combo) { Source = Nguon.BoPhan },
        new Field("SoDienThoai", "Số điện thoại") { MaxLen = 15 },
        new Field("Email", "Email") { MaxLen = 100 },
    });

    public static Form Kho() => new frmCrud<KHO>("Kho", new[]
    {
        new Field("MaKho", "Mã kho") { IsKey = true, Required = true, MaxLen = 10 },
        new Field("TenKho", "Tên kho") { Required = true, MaxLen = 100 },
        new Field("ViTri", "Vị trí") { MaxLen = 200 },
        new Field("DienTich", "Diện tích (m²)", Kind.Dec) { Decimals = 2 },
        new Field("SucChua", "Sức chứa", Kind.Int),
        new Field("NguoiPhuTrach", "Người phụ trách") { MaxLen = 100 },
    });

    public static Form LenhSanXuat() => new frmCrud<LENH_SAN_XUAT>("Lệnh sản xuất", new[]
    {
        new Field("MaLSX", "Mã lệnh SX") { IsKey = true, Required = true, MaxLen = 10 },
        new Field("TenSanPham", "Tên sản phẩm") { Required = true, MaxLen = 150 },
        new Field("SoLuongSX", "Số lượng SX", Kind.Int) { Required = true, Min = 1 },
        new Field("MaBoPhan", "Bộ phận thực hiện", Kind.Combo) { Required = true, Source = Nguon.BoPhan },
        new Field("NgayBatDau", "Ngày bắt đầu", Kind.Date),
        new Field("NgayKetThuc", "Ngày kết thúc", Kind.Date),
        new Field("TrangThai", "Trạng thái", Kind.Combo)
            { Required = true, Items = new[] { "Đang SX", "Hoàn thành", "Dừng" } },
    });

    // Lưu phiếu kiểm tra xong thì tự cập nhật đánh giá nhà cung cấp (tiến trình 2.3)
    public static Form KiemTraCL() => new frmCrud<PHIEU_KIEM_TRA_CL>("Phiếu kiểm tra chất lượng", new[]
    {
        new Field("MaPhieuKT", "Mã phiếu KT") { IsKey = true, Required = true, MaxLen = 10 },
        new Field("MaDDH", "Đơn đặt hàng", Kind.Combo) { Required = true, Source = Nguon.DonDatHang },
        new Field("NgayKiemTra", "Ngày kiểm tra", Kind.Date) { Required = true },
        new Field("KetQua", "Kết quả", Kind.Combo)
            { Required = true, Items = new[] { "Đạt", "Đạt có điều kiện", "Không đạt" } },
        new Field("MaNhanVienKT", "Nhân viên KCS", Kind.Combo) { Source = () => Nguon.NhanVien("KCS") },
        new Field("TiLeLoiPhatHien", "Tỷ lệ lỗi (%)", Kind.Dec) { Decimals = 2, Max = 100 },
        new Field("GhiChu", "Ghi chú") { MaxLen = 300 },
    }, afterSave: e =>
    {
        using var db = new QuanLyTieuHaoVatTuContext();
        var ma = e.MaDDH;
        var ncc = db.DON_DAT_HANG.Where(d => d.MaDDH == ma).Select(d => d.MaNCC).FirstOrDefault();
        if (ncc != null) db.Database.ExecuteSqlInterpolated($"EXEC dbo.sp_CapNhatDanhGiaNCC {ncc}");
    });
}

// Form 3 tab: Bộ phận | Nhân viên | Kho
public class frmDanhMuc : Form
{
    public frmDanhMuc()
    {
        Text = "DANH MỤC BỘ PHẬN – NHÂN VIÊN – KHO";
        ClientSize = new Size(1060, 700);
        StartPosition = FormStartPosition.CenterParent;
        var tabs = new TabControl { Dock = DockStyle.Fill };
        Controls.Add(tabs);
        Them(tabs, "Bộ phận", DanhMucForms.BoPhan());
        Them(tabs, "Nhân viên", DanhMucForms.NhanVien());
        Them(tabs, "Kho", DanhMucForms.Kho());
    }

    private static void Them(TabControl t, string ten, Form f)
    {
        var p = new TabPage(ten);
        t.TabPages.Add(p);
        f.TopLevel = false;
        f.FormBorderStyle = FormBorderStyle.None;
        f.Dock = DockStyle.Fill;
        p.Controls.Add(f);
        f.Show();
    }
}