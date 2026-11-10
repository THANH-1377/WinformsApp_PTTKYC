using PhanTichThietKeYeuCau;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using PhanTichThietKeYeuCau.Models;

namespace PhanTichThietKeYeuCau;

public partial class frmMain : Form
{
    private readonly Label lblVatTuThieu = new(), lblDDHChoDuyet = new(),
                           lblLSXDangSX = new(), lblTongGiaTriTon = new();
    private DataGridView dgvCanhBaoTon = null!, dgvVuotDinhMuc = null!;

    public frmMain()
    {
        InitializeComponent();
        BuildUi();
        Load += (_, _) => LoadDashboard();
    }

    private void BuildUi()
    {
        Text = "HỆ THỐNG QUẢN LÝ TIÊU HAO VẬT TƯ SẢN XUẤT";
        ClientSize = new Size(1100, 620);
        StartPosition = FormStartPosition.CenterScreen;

        // ----- MenuStrip -----
        var menu = new MenuStrip();
        var mHeThong = new ToolStripMenuItem("Hệ thống");
        mHeThong.DropDownItems.Add("Thoát", null, (_, _) => Close());

        var mDanhMuc = new ToolStripMenuItem("Danh mục");
        mDanhMuc.DropDownItems.Add("Vật tư", null, (_, _) => Mo(new frmVatTu()));
        mDanhMuc.DropDownItems.Add("Nhà cung cấp", null, (_, _) => ChuaCo());
        mDanhMuc.DropDownItems.Add("Bộ phận / Nhân viên / Kho", null, (_, _) => ChuaCo());

        var mMua = new ToolStripMenuItem("Mua hàng");
        mMua.DropDownItems.Add("Kế hoạch mua", null, (_, _) => ChuaCo());
        mMua.DropDownItems.Add("Đơn đặt hàng", null, (_, _) => ChuaCo());
        mMua.DropDownItems.Add("Kiểm tra chất lượng", null, (_, _) => ChuaCo());

        var mKho = new ToolStripMenuItem("Kho");
        mKho.DropDownItems.Add("Nhập kho", null, (_, _) => Mo(new frmNhapKho()));
        mKho.DropDownItems.Add("Tồn kho", null, (_, _) => ChuaCo());

        var mCap = new ToolStripMenuItem("Cấp phát & Tiêu hao");
        mCap.DropDownItems.Add("Cấp phát và theo dõi tiêu hao", null, (_, _) => Mo(new frmCapPhat()));

        var mBC = new ToolStripMenuItem("Báo cáo");
        mBC.DropDownItems.Add("Thống kê – báo cáo", null, (_, _) => ChuaCo());

        menu.Items.AddRange(new ToolStripItem[] { mHeThong, mDanhMuc, mMua, mKho, mCap, mBC });
        MainMenuStrip = menu;
        Controls.Add(menu);

        // ----- Banner -----
        new Label
        {
            Text = "HỆ THỐNG QUẢN LÝ TIÊU HAO VẬT TƯ SẢN XUẤT",
            BackColor = Color.CornflowerBlue,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 16, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter,
            AutoSize = false,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        }.At(this, 0, 28, ClientSize.Width, 50);

        // ----- 4 ô thống kê -----
        Card("VẬT TƯ DƯỚI MỨC TỐI THIỂU", lblVatTuThieu, 20, Color.MistyRose);
        Card("ĐĐH CHỜ DUYỆT", lblDDHChoDuyet, 290, Color.LightYellow);
        Card("LỆNH SX ĐANG SẢN XUẤT", lblLSXDangSX, 560, Color.Honeydew);
        Card("TỔNG GIÁ TRỊ TỒN KHO", lblTongGiaTriTon, 830, Color.AliceBlue);

        // ----- 2 DataGridView -----
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

        // ----- Truy cập nhanh -----
        new Label { Text = "TRUY CẬP NGHIỆP VỤ NHANH", Font = new Font("Segoe UI", 10, FontStyle.Bold), AutoSize = true }
            .At(this, 20, 505);
        UI.Btn(this, "QUẢN LÝ VẬT TƯ", 20, 535, 200, () => Mo(new frmVatTu()), 45);
        UI.Btn(this, "NHẬP KHO", 240, 535, 150, () => Mo(new frmNhapKho()), 45);
        UI.Btn(this, "CẤP PHÁT & THEO DÕI TIÊU HAO", 410, 535, 280, () => Mo(new frmCapPhat()), 45);
        UI.Btn(this, "LÀM MỚI SỐ LIỆU", 710, 535, 150, LoadDashboard, 45);
        UI.Btn(this, "THOÁT CHƯƠNG TRÌNH", 880, 535, 190, () =>
        {
            if (UI.Confirm("Bạn có chắc muốn thoát chương trình?")) Close();
        }, 45);

        var status = new StatusStrip();
        status.Items.Add("Ngày: " + DateTime.Now.ToString("dd/MM/yyyy"));
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
        LoadDashboard();   // quay về thì cập nhật lại số liệu
    }

    private void ChuaCo() =>
        UI.Info("Chức năng này nằm ngoài 3 giao diện được thiết kế trong báo cáo.");

    private void LoadDashboard()
    {
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

            dgvVuotDinhMuc.DataSource = TieuHaoService.Tinh(db)
                .Where(r => r.DanhGia == "Vượt định mức").ToList();
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