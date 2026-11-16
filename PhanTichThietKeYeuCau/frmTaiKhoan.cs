using Microsoft.EntityFrameworkCore;
using PhanTichThietKeYeuCau;
using PhanTichThietKeYeuCau.Models;
using System.Text.RegularExpressions;

namespace PhanTichThietKeYeuCau;

public class frmTaiKhoan : Form
{
    private readonly TextBox txtUser = new(), txtHoTen = new() { MaxLength = 100 },
                             txtMatKhau = new() { UseSystemPasswordChar = true };
    private readonly ComboBox cboVaiTro = UI.Combo(), cboNhanVien = UI.Combo();
    private readonly CheckBox chkHoatDong = new() { Text = "Đang hoạt động", Checked = true, AutoSize = true };
    private GroupBox gb = null!;
    private DataGridView dgv = null!;
    private Button btnThem = null!, btnLuu = null!, btnXoa = null!, btnMoKhoa = null!, btnHuy = null!;
    private bool _them;

    public frmTaiKhoan()
    {
        Text = "QUẢN LÝ TÀI KHOẢN";
        ClientSize = new Size(1000, 640);
        StartPosition = FormStartPosition.CenterParent;
        BuildUi();
        Load += (_, _) =>
        {
            try { NapDanhMuc(); LoadData(); CheDo(false, false, true); }
            catch (Exception ex) { UI.Error("Lỗi khởi tạo: " + (ex.InnerException?.Message ?? ex.Message)); }
        };
    }

    private void BuildUi()
    {
        gb = new GroupBox { Text = "THÔNG TIN TÀI KHOẢN" }.At(this, 15, 10, 970, 165);
        UI.Lbl(gb, "Tên đăng nhập *:", 15, 25); txtUser.At(gb, 135, 25, 200); txtUser.MaxLength = 50;
        UI.Lbl(gb, "Họ tên *:", 360, 25); txtHoTen.At(gb, 430, 25, 280);
        UI.Lbl(gb, "Vai trò *:", 15, 62); cboVaiTro.At(gb, 135, 62, 200);
        UI.Lbl(gb, "Nhân viên liên kết:", 360, 62); cboNhanVien.At(gb, 490, 62, 220);
        UI.Lbl(gb, "Mật khẩu:", 15, 99); txtMatKhau.At(gb, 135, 99, 200);
        UI.Lbl(gb, "(nhập khi tạo mới hoặc khi muốn đặt lại; để trống nếu giữ nguyên)", 345, 99);
        chkHoatDong.At(gb, 135, 132);

        btnThem = UI.Btn(this, "Thêm", 15, 185, 90, () => CheDo(true, true, true));
        btnLuu = UI.Btn(this, "Lưu", 115, 185, 90, Luu);
        btnXoa = UI.Btn(this, "Xóa", 215, 185, 90, Xoa);
        btnMoKhoa = UI.Btn(this, "Mở khóa đăng nhập", 315, 185, 160, MoKhoa);
        btnHuy = UI.Btn(this, "Hủy", 485, 185, 90, () => CheDo(false, false, true));
        UI.Btn(this, "Đóng", 585, 185, 90, Close);

        dgv = UI.Grid(this, 15, 230, 970, 395);
        dgv.CellClick += (_, e) =>
        {
            if (e.RowIndex >= 0) Nap(Convert.ToString(dgv.Rows[e.RowIndex].Cells["TenDangNhap"].Value) ?? "");
        };
        cboNhanVien.SelectedIndexChanged += (_, _) =>
        {
            if (_them && txtHoTen.Text == "" && cboNhanVien.SelectedItem is ComboItem it && it.Value != "")
                txtHoTen.Text = it.Text;
        };
    }

    private void NapDanhMuc()
    {
        UI.Bind(cboVaiTro, PhanQuyen.TenVaiTro.Select(kv => new ComboItem(kv.Key, kv.Value)).ToList());
        using var db = new QuanLyTieuHaoVatTuContext();
        var ds = new List<ComboItem> { new("", "(Không liên kết)") };
        ds.AddRange(db.NHAN_VIEN.AsNoTracking().OrderBy(n => n.MaNhanVien).ToList()
            .Select(n => new ComboItem(UI.T(n.MaNhanVien), n.HoTen)));
        UI.Bind(cboNhanVien, ds);
    }

    private void CheDo(bool nhap, bool them, bool xoaTrang = false)
    {
        _them = them;
        gb.Enabled = nhap;
        txtUser.ReadOnly = !them;
        btnLuu.Enabled = btnHuy.Enabled = nhap;
        btnThem.Enabled = btnXoa.Enabled = btnMoKhoa.Enabled = !nhap;
        if (xoaTrang)
        {
            txtUser.Clear(); txtHoTen.Clear(); txtMatKhau.Clear();
            cboVaiTro.SelectedIndex = -1; cboNhanVien.SelectedIndex = 0; chkHoatDong.Checked = true;
        }
        if (them) txtUser.Focus();
    }

    private void LoadData()
    {
        using var db = new QuanLyTieuHaoVatTuContext();
        dgv.DataSource = db.TAI_KHOAN.AsNoTracking().OrderBy(t => t.TenDangNhap).ToList().Select(t => new
        {
            TenDangNhap = t.TenDangNhap,
            HoTen = t.HoTen,
            VaiTro = PhanQuyen.Ten(t.VaiTro),
            NhanVien = UI.T(t.MaNhanVien),
            TrangThai = t.HoatDong == true ? "Hoạt động" : "Vô hiệu hóa",
            KhoaDen = t.KhoaDen is DateTime k && k > DateTime.Now ? k.ToString("HH:mm dd/MM") : "",
            DangNhapCuoi = t.LanDangNhapCuoi?.ToString("dd/MM/yyyy HH:mm") ?? ""
        }).ToList();
        UI.Headers(dgv, ("TenDangNhap", "Tên đăng nhập"), ("HoTen", "Họ tên"), ("VaiTro", "Vai trò"),
            ("NhanVien", "Mã NV"), ("TrangThai", "Trạng thái"), ("KhoaDen", "Khóa đến"), ("DangNhapCuoi", "Đăng nhập gần nhất"));
    }

    private void Nap(string user)
    {
        if (user == "") return;
        using var db = new QuanLyTieuHaoVatTuContext();
        var t = db.TAI_KHOAN.AsNoTracking().FirstOrDefault(x => x.TenDangNhap == user);
        if (t == null) return;
        CheDo(false, false);
        txtUser.Text = t.TenDangNhap;
        txtHoTen.Text = t.HoTen;
        cboVaiTro.SelectedValue = t.VaiTro.Trim();
        cboNhanVien.SelectedValue = UI.T(t.MaNhanVien);
        chkHoatDong.Checked = t.HoatDong == true;
        txtMatKhau.Clear();
        // chọn dòng để sửa: cho phép sửa thông tin (khóa ô tên đăng nhập)
        gb.Enabled = true; btnLuu.Enabled = btnHuy.Enabled = true;
        btnThem.Enabled = true; btnXoa.Enabled = true; btnMoKhoa.Enabled = true;
    }

    private static int SoAdminConLai(QuanLyTieuHaoVatTuContext db, string tru) =>
        db.TAI_KHOAN.Count(t => t.VaiTro == "ADMIN" && t.HoatDong == true && t.TenDangNhap != tru);

    private void Luu()
    {
        var user = txtUser.Text.Trim().ToLowerInvariant();
        var hoTen = txtHoTen.Text.Trim();
        if (!Regex.IsMatch(user, "^[a-z0-9._-]{3,50}$"))
        { UI.Error("Tên đăng nhập gồm 3–50 ký tự: chữ thường không dấu, số, dấu chấm, gạch dưới, gạch ngang."); return; }
        if (hoTen == "") { UI.Error("Họ tên không được để trống."); return; }
        if (cboVaiTro.SelectedItem is not ComboItem vt) { UI.Error("Hãy chọn vai trò."); return; }
        var maNv = (cboNhanVien.SelectedItem as ComboItem)?.Value;
        if (string.IsNullOrEmpty(maNv)) maNv = null;
        var mk = txtMatKhau.Text;
        if (_them && mk.Length < 6) { UI.Error("Mật khẩu phải có ít nhất 6 ký tự."); return; }
        if (!_them && mk != "" && mk.Length < 6) { UI.Error("Mật khẩu mới phải có ít nhất 6 ký tự."); return; }

        try
        {
            using var db = new QuanLyTieuHaoVatTuContext();
            if (_them)
            {
                if (db.TAI_KHOAN.Any(t => t.TenDangNhap == user)) { UI.Error("Tên đăng nhập đã tồn tại."); return; }
                db.TAI_KHOAN.Add(new TAI_KHOAN
                {
                    TenDangNhap = user,
                    MatKhauHash = MatKhau.Hash(mk),
                    VaiTro = vt.Value,
                    HoTen = hoTen,
                    MaNhanVien = maNv,
                    HoatDong = chkHoatDong.Checked,
                    SoLanSai = 0,
                    NgayTao = DateTime.Now
                });
            }
            else
            {
                var t = db.TAI_KHOAN.FirstOrDefault(x => x.TenDangNhap == user);
                if (t == null) { UI.Error("Không tìm thấy tài khoản cần sửa."); return; }

                var doiVaiTro = t.VaiTro.Trim() != vt.Value;
                var tat = !chkHoatDong.Checked;
                if (user == Session.TenDangNhap && (doiVaiTro || tat))
                { UI.Error("Không thể tự đổi vai trò hoặc vô hiệu hóa tài khoản đang đăng nhập."); return; }
                if (t.VaiTro.Trim() == "ADMIN" && (doiVaiTro || tat) && SoAdminConLai(db, user) == 0)
                { UI.Error("Phần mềm phải còn ít nhất một quản trị viên đang hoạt động."); return; }

                t.HoTen = hoTen; t.VaiTro = vt.Value; t.MaNhanVien = maNv; t.HoatDong = chkHoatDong.Checked;
                if (mk != "")
                {
                    t.MatKhauHash = MatKhau.Hash(mk);
                    t.SoLanSai = 0; t.KhoaDen = null;        // đặt lại mật khẩu thì mở khóa luôn
                }
            }
            db.SaveChanges();
            UI.Info("Lưu thành công.");
            CheDo(false, false, true);
            LoadData();
        }
        catch (DbUpdateException ex)
        {
            UI.Error("Không lưu được. Có thể nhân viên này đã có tài khoản khác.\n" + (ex.InnerException?.Message ?? ex.Message));
        }
        catch (Exception ex) { UI.Error("Không lưu được: " + (ex.InnerException?.Message ?? ex.Message)); }
    }

    private void Xoa()
    {
        var user = txtUser.Text.Trim().ToLowerInvariant();
        if (user == "") { UI.Error("Hãy chọn một tài khoản trong danh sách."); return; }
        if (user == Session.TenDangNhap) { UI.Error("Không thể xóa tài khoản đang đăng nhập."); return; }
        if (!UI.Confirm($"Xóa tài khoản {user}?")) return;
        try
        {
            using var db = new QuanLyTieuHaoVatTuContext();
            var t = db.TAI_KHOAN.FirstOrDefault(x => x.TenDangNhap == user);
            if (t == null) return;
            if (t.VaiTro.Trim() == "ADMIN" && SoAdminConLai(db, user) == 0)
            { UI.Error("Phần mềm phải còn ít nhất một quản trị viên đang hoạt động."); return; }
            db.TAI_KHOAN.Remove(t);
            db.SaveChanges();
            CheDo(false, false, true);
            LoadData();
        }
        catch (Exception ex) { UI.Error("Không xóa được: " + (ex.InnerException?.Message ?? ex.Message)); }
    }

    private void MoKhoa()
    {
        var user = txtUser.Text.Trim().ToLowerInvariant();
        if (user == "") { UI.Error("Hãy chọn một tài khoản trong danh sách."); return; }
        using var db = new QuanLyTieuHaoVatTuContext();
        var t = db.TAI_KHOAN.FirstOrDefault(x => x.TenDangNhap == user);
        if (t == null) return;
        t.SoLanSai = 0; t.KhoaDen = null;
        db.SaveChanges();
        UI.Info("Đã mở khóa đăng nhập cho tài khoản " + user + ".");
        LoadData();
    }
}