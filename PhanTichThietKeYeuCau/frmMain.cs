using PhanTichThietKeYeuCau;
using PhanTichThietKeYeuCau.Models;

namespace PhanTichThietKeYeuCau;

public partial class frmMain : Form
{
    private record MucMenu(string Text, string Quyen, Func<Form> Tao);

    private readonly Label lblVatTuThieu = new(), lblDDHChoDuyet = new(),
                           lblLSXDangSX = new(), lblTongGiaTriTon = new();
    private DataGridView? dgvCanhBaoTon, dgvVuotDinhMuc;
    private readonly bool _xemSoLieu = Session.Co(Chuc.TonKho) || Session.Co(Chuc.BaoCao);

    public bool DangXuat { get; private set; }

    public frmMain()
    {
        InitializeComponent();
        BuildUi();
        Load += (_, _) => LoadDashboard();
    }

    // Tạo một nhóm menu, chỉ thêm các mục mà người dùng có quyền
    private ToolStripMenuItem Nhom(string ten, params MucMenu[] muc)
    {
        var m = new ToolStripMenuItem(ten);
        foreach (var x in muc)
            if (Session.Co(x.Quyen))
            {
                var tao = x.Tao;
                m.DropDownItems.Add(x.Text, null, (_, _) => Mo(tao()));
            }
        m.Visible = m.DropDownItems.Count > 0;
        return m;
    }

    private void BuildUi()
    {
        Text = "QUẢN LÝ TIÊU HAO VẬT TƯ SẢN XUẤT";
        ClientSize = new Size(1100, 660);
        StartPosition = FormStartPosition.CenterScreen;

        // ----- Menu -----
        var menu = new MenuStrip();
        var mHeThong = new ToolStripMenuItem("Hệ thống");
        mHeThong.DropDownItems.Add("Đổi mật khẩu", null, (_, _) => { using var f = new frmDoiMatKhau(); f.ShowDialog(this); });
        if (Session.Co(Chuc.TaiKhoan))
            mHeThong.DropDownItems.Add("Quản lý tài khoản", null, (_, _) => Mo(new frmTaiKhoan()));
        mHeThong.DropDownItems.Add(new ToolStripSeparator());
        mHeThong.DropDownItems.Add("Đăng xuất", null, (_, _) => DangXuatNgay());
        mHeThong.DropDownItems.Add("Thoát", null, (_, _) => Close());

        menu.Items.Add(mHeThong);
        menu.Items.Add(Nhom("Danh mục",
            new MucMenu("Vật tư", Chuc.VatTu, () => new frmVatTu()),
            new MucMenu("Nhà cung cấp", Chuc.NhaCungCap, DanhMucForms.NhaCungCap),
            new MucMenu("Bộ phận / Nhân viên / Kho", Chuc.DanhMucKhac, () => new frmDanhMuc())));
        menu.Items.Add(Nhom("Mua hàng",
            new MucMenu("Kế hoạch mua", Chuc.KeHoachMua, () => new frmKeHoachMua()),
            new MucMenu("Đơn đặt hàng", Chuc.DonDatHang, () => new frmDonDatHang()),
            new MucMenu("Kiểm tra chất lượng", Chuc.KiemTraCL, DanhMucForms.KiemTraCL)));
        menu.Items.Add(Nhom("Kho",
            new MucMenu("Nhập kho", Chuc.NhapKho, () => new frmNhapKho()),
            new MucMenu("Tồn kho", Chuc.TonKho, () => new frmTonKho())));
        menu.Items.Add(Nhom("Cấp phát & Tiêu hao",
            new MucMenu("Lệnh sản xuất", Chuc.LenhSanXuat, DanhMucForms.LenhSanXuat),
            new MucMenu("Cấp phát và theo dõi tiêu hao", Chuc.CapPhat, () => new frmCapPhat())));
        menu.Items.Add(Nhom("Báo cáo",
            new MucMenu("Thống kê – báo cáo", Chuc.BaoCao, () => new frmBaoCao())));
        MainMenuStrip = menu;
        Controls.Add(menu);

        // ----- Banner -----
        new Label
        {
            Text = "QUẢN LÝ TIÊU HAO VẬT TƯ SẢN XUẤT",
            BackColor = Color.CornflowerBlue,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 16, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter,
            AutoSize = false,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        }.At(this, 0, 28, ClientSize.Width, 50);

        if (_xemSoLieu)
        {
            Card("VẬT TƯ DƯỚI MỨC TỐI THIỂU", lblVatTuThieu, 20, Color.MistyRose);
            Card("ĐĐH CHỜ DUYỆT", lblDDHChoDuyet, 290, Color.LightYellow);
            Card("LỆNH SX ĐANG SẢN XUẤT", lblLSXDangSX, 560, Color.Honeydew);
            Card("TỔNG GIÁ TRỊ TỒN KHO", lblTongGiaTriTon, 830, Color.AliceBlue);

            new Label { Text = "CẢNH BÁO TỒN KHO", Font = new Font("Segoe UI", 10, FontStyle.Bold), AutoSize = true }
                .At(this, 20, 200);
            dgvCanhBaoTon = UI.Grid(this, 20, 225, 520, 270);
            dgvCanhBaoTon.DataBindingComplete += (_, _) =>
            {
                foreach (DataGridViewRow r in dgvCanhBaoTon.Rows)
                    r.DefaultCellStyle.BackColor =
                        Convert.ToString(r.Cells["TinhTrangTon"].Value) == "Dưới mức tối thiểu"
                            ? Color.MistyRose : Color.LightYellow;
            };

            new Label { Text = "VẬT TƯ VƯỢT ĐỊNH MỨC", Font = new Font("Segoe UI", 10, FontStyle.Bold), AutoSize = true }
                .At(this, 560, 200);
            dgvVuotDinhMuc = UI.Grid(this, 560, 225, 520, 270);
        }
        else
        {
            new Label
            {
                Text = $"Xin chào, {Session.HoTen}!\nVai trò: {Session.TenVaiTro}\n\n" +
                       "Hãy chọn chức năng ở menu phía trên hoặc ở các nút bên dưới.",
                Font = new Font("Segoe UI", 14),
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter
            }.At(this, 20, 110, 1060, 330);
        }

        
        // ----- Nút truy cập nhanh (chỉ hiện nút được phép) -----
        /*new Label { Text = "TRUY CẬP NGHIỆP VỤ NHANH", Font = new Font("Segoe UI", 10, FontStyle.Bold), AutoSize = true }
            .At(this, 20, 505);
        var nhanh = new[]
        {
            new MucMenu("VẬT TƯ", Chuc.VatTu, () => new frmVatTu()),
            new MucMenu("ĐƠN ĐẶT HÀNG", Chuc.DonDatHang, () => new frmDonDatHang()),
            new MucMenu("KIỂM TRA CL", Chuc.KiemTraCL, DanhMucForms.KiemTraCL),
            new MucMenu("NHẬP KHO", Chuc.NhapKho, () => new frmNhapKho()),
            new MucMenu("CẤP PHÁT", Chuc.CapPhat, () => new frmCapPhat()),
            new MucMenu("LỆNH SX", Chuc.LenhSanXuat, DanhMucForms.LenhSanXuat),
            new MucMenu("TỒN KHO", Chuc.TonKho, () => new frmTonKho()),
            new MucMenu("BÁO CÁO", Chuc.BaoCao, () => new frmBaoCao()),
        };
        int x = 20;
        foreach (var m in nhanh.Where(m => Session.Co(m.Quyen)))
        {
            var tao = m.Tao;
            UI.Btn(this, m.Text, x, 535, 125, () => Mo(tao()), 45);
            x += 133;
        }*/

        /*int x2 = 20;
        if (_xemSoLieu) { UI.Btn(this, "LÀM MỚI SỐ LIỆU", x2, 590, 170, LoadDashboard, 40); x2 += 180; }
        UI.Btn(this, "ĐĂNG XUẤT", x2, 590, 150, DangXuatNgay, 40); x2 += 160;
        UI.Btn(this, "THOÁT CHƯƠNG TRÌNH", x2, 590, 200, () =>
        {
            if (UI.Confirm("Bạn có chắc muốn thoát chương trình?")) Close();
        }, 40);*/

        var status = new StatusStrip();
        status.Items.Add($"Người dùng: {Session.HoTen} ({Session.TenVaiTro})   |   Ngày: {DateTime.Now:dd/MM/yyyy}");
        Controls.Add(status);
    }

    private void Card(string title, Label value, int x, Color color)
    {
        var p = new Panel { BackColor = color, BorderStyle = BorderStyle.FixedSingle }.At(this, x, 95, 240, 90);
        new Label
        {
            Text = title,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleCenter
        }.At(p, 0, 5, 240, 25);
        value.Font = new Font("Segoe UI", 16, FontStyle.Bold);
        value.AutoSize = false; value.TextAlign = ContentAlignment.MiddleCenter; value.Text = "0";
        value.At(p, 0, 35, 240, 45);
    }

    private void Mo(Form f)
    {
        using (f) f.ShowDialog(this);
        LoadDashboard();
    }

    private void DangXuatNgay()
    {
        if (!UI.Confirm("Đăng xuất khỏi phần mềm?")) return;
        DangXuat = true;
        Close();
    }

    private void LoadDashboard()
    {
        if (!_xemSoLieu || dgvCanhBaoTon == null || dgvVuotDinhMuc == null) return;
        try
        {
            using var db = new QuanLyTieuHaoVatTuContext();

            lblDDHChoDuyet.Text = db.DON_DAT_HANG.Count(d => d.TrangThai == "Chờ duyệt").ToString();
            lblLSXDangSX.Text = db.LENH_SAN_XUAT.Count(l => l.TrangThai == "Đang SX").ToString();

            var ton = (from tk in db.TON_KHO
                       join k in db.KHO on tk.MaKho equals k.MaKho
                       join v in db.VAT_TU on tk.MaVatTu equals v.MaVatTu
                       select new { k.TenKho, v.TenVatTu, tk.SoLuongTon, v.MucTonMin, v.MucTonMax, v.DonGiaMua })
                      .ToList();

            var rows = ton.Select(x => new
            {
                x.TenKho,
                x.TenVatTu,
                x.SoLuongTon,
                x.MucTonMin,
                x.MucTonMax,
                TinhTrangTon = x.SoLuongTon < (x.MucTonMin ?? 0) ? "Dưới mức tối thiểu"
                             : (x.MucTonMax.HasValue && x.SoLuongTon > x.MucTonMax.Value) ? "Vượt mức tối đa"
                             : "Bình thường"
            }).ToList();

            lblVatTuThieu.Text = rows.Count(r => r.TinhTrangTon == "Dưới mức tối thiểu").ToString();
            lblTongGiaTriTon.Text = ton.Sum(x => x.SoLuongTon * (x.DonGiaMua ?? 0m)).ToString("N0") + " đ";

            dgvCanhBaoTon.DataSource = rows.Where(r => r.TinhTrangTon != "Bình thường").ToList();
            UI.Headers(dgvCanhBaoTon, ("TenKho", "Kho"), ("TenVatTu", "Vật tư"), ("SoLuongTon", "Tồn"),
                ("MucTonMin", "Tối thiểu"), ("MucTonMax", "Tối đa"), ("TinhTrangTon", "Tình trạng"));

            dgvVuotDinhMuc.DataSource = TieuHaoService.Tinh(db).Where(r => r.DanhGia == "Vượt định mức").ToList();
            UI.Headers(dgvVuotDinhMuc, ("MaLSX", "Mã LSX"), ("TenSanPham", "Sản phẩm"), ("TenVatTu", "Vật tư"),
                ("DinhMucLyThuyet", "Định mức"), ("TongThucNhan", "Thực nhận"), ("TyLeVuot", "Vượt (%)"));
            foreach (var c in new[] { "TongCapPhat", "ChenhLechCapNhan", "ChenhLechDinhMuc", "DanhGia" })
                if (dgvVuotDinhMuc.Columns.Contains(c)) dgvVuotDinhMuc.Columns[c]!.Visible = false;
        }
        catch (Exception ex)
        {
            UI.Error("Không đọc được dữ liệu: " + (ex.InnerException?.Message ?? ex.Message));
        }
    }
}