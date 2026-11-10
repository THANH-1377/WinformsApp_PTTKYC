using Microsoft.EntityFrameworkCore;
using PhanTichThietKeYeuCau;
using PhanTichThietKeYeuCau.Models;

namespace PhanTichThietKeYeuCau;

public partial class frmVatTu : Form
{
    private readonly TextBox txtMaVatTu = new(), txtTenVatTu = new(), txtGhiChu = new(), txtTimKiem = new();
    private readonly ComboBox cboDonViTinh = UI.Combo(), cboNhomVatTu = UI.Combo(), cboLocNhom = UI.Combo();
    private readonly NumericUpDown numDonGiaMua = UI.Num(2), numMucDinhMuc = UI.Num(4, 100000),
                                   numMucTonMin = UI.Num(), numMucTonMax = UI.Num();
    private GroupBox gbThongTin = null!;
    private DataGridView dgvVatTu = null!;
    private Button btnThem = null!, btnLuu = null!, btnSua = null!, btnXoa = null!, btnHuy = null!;
    private bool _dangThem;

    private static readonly string[] DonVi = { "Cái", "Kg", "Cây", "Lít", "Bình", "Đôi", "m", "Bộ", "Cuộn" };
    private static readonly string[] Nhom =
        { "Nguyên liệu", "Vật tư hàn", "Vật tư cắt", "Hoàn thiện", "BHLĐ", "Bảo trì", "Vệ sinh" };

    public frmVatTu()
    {
        InitializeComponent();
        BuildUi();
        Load += (_, _) =>
        {
            cboDonViTinh.Items.AddRange(DonVi);
            cboNhomVatTu.Items.AddRange(Nhom);
            cboLocNhom.Items.Add("Tất cả");
            cboLocNhom.Items.AddRange(Nhom);
            cboLocNhom.SelectedIndex = 0;
            LoadData();
            CheDo(nhap: false, them: false);
        };
    }

    private void BuildUi()
    {
        Text = "QUẢN LÝ VẬT TƯ";
        ClientSize = new Size(985, 660);
        StartPosition = FormStartPosition.CenterParent;

        // Thanh tìm kiếm
        UI.Lbl(this, "Tìm kiếm:", 15, 12); txtTimKiem.At(this, 85, 12, 220);
        UI.Lbl(this, "Nhóm:", 320, 12); cboLocNhom.At(this, 375, 12, 160);
        UI.Btn(this, "Tìm kiếm", 550, 10, 90, LoadData);
        UI.Btn(this, "Làm mới", 650, 10, 90, () =>
        {
            txtTimKiem.Clear(); cboLocNhom.SelectedIndex = 0; LoadData(); CheDo(false, false); XoaTrang();
        });

        // Thông tin vật tư
        gbThongTin = new GroupBox { Text = "THÔNG TIN VẬT TƯ" }.At(this, 15, 50, 955, 205);
        UI.Lbl(gbThongTin, "Mã vật tư:", 15, 25); txtMaVatTu.At(gbThongTin, 110, 25, 180); txtMaVatTu.MaxLength = 10;
        UI.Lbl(gbThongTin, "Nhóm vật tư:", 330, 25); cboNhomVatTu.At(gbThongTin, 430, 25, 200);
        UI.Lbl(gbThongTin, "Tên vật tư:", 15, 60); txtTenVatTu.At(gbThongTin, 110, 60, 520); txtTenVatTu.MaxLength = 150;
        UI.Lbl(gbThongTin, "Đơn vị tính:", 15, 95); cboDonViTinh.At(gbThongTin, 110, 95, 180);
        UI.Lbl(gbThongTin, "Đơn giá mua:", 330, 95); numDonGiaMua.At(gbThongTin, 430, 95, 200);
        UI.Lbl(gbThongTin, "Định mức/SP:", 15, 130); numMucDinhMuc.At(gbThongTin, 110, 130, 180);
        UI.Lbl(gbThongTin, "Tồn tối thiểu:", 330, 130); numMucTonMin.At(gbThongTin, 430, 130, 90);
        UI.Lbl(gbThongTin, "Tồn tối đa:", 535, 130); numMucTonMax.At(gbThongTin, 620, 130, 100);
        UI.Lbl(gbThongTin, "Ghi chú:", 15, 165); txtGhiChu.At(gbThongTin, 110, 165, 520); txtGhiChu.MaxLength = 300;

        // Nút chức năng
        btnThem = UI.Btn(this, "Thêm", 15, 265, 90, () => CheDo(true, true, xoaTrang: true));
        btnLuu = UI.Btn(this, "Lưu", 115, 265, 90, Luu);
        btnSua = UI.Btn(this, "Sửa", 215, 265, 90, () =>
        {
            if (UI.T(txtMaVatTu.Text) == "") { UI.Error("Chọn một vật tư trong danh sách để sửa."); return; }
            CheDo(true, false);
        });
        btnXoa = UI.Btn(this, "Xóa", 315, 265, 90, Xoa);
        btnHuy = UI.Btn(this, "Hủy", 415, 265, 90, () => { CheDo(false, false); XoaTrang(); });
        UI.Btn(this, "Xuất CSV", 515, 265, 100, () => UI.ExportCsv(dgvVatTu, "DanhSachVatTu.csv"));
        UI.Btn(this, "Đóng", 625, 265, 90, Close);

        // Lưới
        dgvVatTu = UI.Grid(this, 15, 310, 955, 335);
        dgvVatTu.CellClick += dgvVatTu_CellClick;
    }

    // nhap = cho phép nhập liệu; them = đang ở chế độ thêm mới
    private void CheDo(bool nhap, bool them, bool xoaTrang = false)
    {
        _dangThem = them;
        gbThongTin.Enabled = nhap;
        txtMaVatTu.ReadOnly = !them;
        btnLuu.Enabled = btnHuy.Enabled = nhap;
        btnThem.Enabled = btnSua.Enabled = btnXoa.Enabled = !nhap;
        if (xoaTrang) { XoaTrang(); txtMaVatTu.Focus(); }
    }

    private void XoaTrang()
    {
        txtMaVatTu.Clear(); txtTenVatTu.Clear(); txtGhiChu.Clear();
        cboDonViTinh.SelectedIndex = -1; cboNhomVatTu.SelectedIndex = -1;
        numDonGiaMua.Value = numMucDinhMuc.Value = numMucTonMin.Value = numMucTonMax.Value = 0;
    }

    private void LoadData()
    {
        try
        {
            using var db = new QuanLyTieuHaoVatTuContext();
            var kw = txtTimKiem.Text.Trim();
            var nhom = cboLocNhom.Text;

            var q = db.VAT_TU.AsNoTracking().AsQueryable();
            if (kw != "") q = q.Where(v => v.MaVatTu.Contains(kw) || v.TenVatTu.Contains(kw));
            if (nhom != "" && nhom != "Tất cả") q = q.Where(v => v.NhomVatTu == nhom);

            dgvVatTu.DataSource = q.OrderBy(v => v.MaVatTu).ToList()
                .Select(v => new
                {
                    MaVatTu = UI.T(v.MaVatTu),
                    v.TenVatTu,
                    v.DonViTinh,
                    v.DonGiaMua,
                    v.MucDinhMuc,
                    v.MucTonMin,
                    v.MucTonMax,
                    v.NhomVatTu,
                    v.GhiChu
                }).ToList();

            UI.Headers(dgvVatTu, ("MaVatTu", "Mã vật tư"), ("TenVatTu", "Tên vật tư"), ("DonViTinh", "ĐVT"),
                ("DonGiaMua", "Đơn giá mua"), ("MucDinhMuc", "Định mức/SP"), ("MucTonMin", "Tồn tối thiểu"),
                ("MucTonMax", "Tồn tối đa"), ("NhomVatTu", "Nhóm"), ("GhiChu", "Ghi chú"));
            dgvVatTu.Columns["DonGiaMua"]!.DefaultCellStyle.Format = "N0";
            dgvVatTu.Columns["MucDinhMuc"]!.DefaultCellStyle.Format = "N4";
        }
        catch (Exception ex) { UI.Error("Không đọc được dữ liệu: " + (ex.InnerException?.Message ?? ex.Message)); }
    }

    private static decimal D(object? o) => o == null ? 0m : Convert.ToDecimal(o);

    private void dgvVatTu_CellClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0) return;
        var r = dgvVatTu.Rows[e.RowIndex];
        txtMaVatTu.Text = Convert.ToString(r.Cells["MaVatTu"].Value);
        txtTenVatTu.Text = Convert.ToString(r.Cells["TenVatTu"].Value);
        cboDonViTinh.Text = Convert.ToString(r.Cells["DonViTinh"].Value);
        cboNhomVatTu.Text = Convert.ToString(r.Cells["NhomVatTu"].Value);
        numDonGiaMua.Value = D(r.Cells["DonGiaMua"].Value);
        numMucDinhMuc.Value = D(r.Cells["MucDinhMuc"].Value);
        numMucTonMin.Value = D(r.Cells["MucTonMin"].Value);
        numMucTonMax.Value = D(r.Cells["MucTonMax"].Value);
        txtGhiChu.Text = Convert.ToString(r.Cells["GhiChu"].Value);
    }

    private void GanDuLieu(VAT_TU vt, string ten)
    {
        vt.TenVatTu = ten;
        vt.DonViTinh = cboDonViTinh.Text;
        vt.DonGiaMua = numDonGiaMua.Value;
        vt.MucDinhMuc = numMucDinhMuc.Value;
        vt.MucTonMin = (int)numMucTonMin.Value;
        vt.MucTonMax = (int)numMucTonMax.Value;
        vt.NhomVatTu = cboNhomVatTu.Text == "" ? null : cboNhomVatTu.Text;
        vt.GhiChu = string.IsNullOrWhiteSpace(txtGhiChu.Text) ? null : txtGhiChu.Text.Trim();
    }

    private void Luu()
    {
        var ma = txtMaVatTu.Text.Trim();
        var ten = txtTenVatTu.Text.Trim();
        if (ma == "" || ten == "") { UI.Error("Mã vật tư và tên vật tư không được để trống."); return; }
        if (cboDonViTinh.Text == "") { UI.Error("Hãy chọn đơn vị tính."); return; }
        if (numMucTonMax.Value < numMucTonMin.Value)
        { UI.Error("Tồn tối đa phải lớn hơn hoặc bằng tồn tối thiểu."); return; }

        try
        {
            using var db = new QuanLyTieuHaoVatTuContext();
            if (_dangThem)
            {
                if (db.VAT_TU.Any(v => v.MaVatTu == ma)) { UI.Error("Mã vật tư đã tồn tại."); return; }
                var vt = new VAT_TU { MaVatTu = ma };
                GanDuLieu(vt, ten);
                db.VAT_TU.Add(vt);
            }
            else
            {
                var vt = db.VAT_TU.Find(ma);
                if (vt == null) { UI.Error("Không tìm thấy vật tư cần sửa."); return; }
                GanDuLieu(vt, ten);
            }
            db.SaveChanges();
            UI.Info("Lưu thành công.");
            CheDo(false, false);
            LoadData();
        }
        catch (Exception ex) { UI.Error("Không lưu được: " + (ex.InnerException?.Message ?? ex.Message)); }
    }

    private void Xoa()
    {
        var ma = txtMaVatTu.Text.Trim();
        if (ma == "") { UI.Error("Chọn một vật tư trong danh sách để xóa."); return; }
        if (!UI.Confirm($"Bạn có chắc muốn xóa vật tư {ma}?")) return;

        try
        {
            using var db = new QuanLyTieuHaoVatTuContext();
            var vt = db.VAT_TU.Find(ma);
            if (vt != null) { db.VAT_TU.Remove(vt); db.SaveChanges(); }
            XoaTrang();
            LoadData();
        }
        catch (DbUpdateException)
        {
            UI.Error("Không thể xóa vật tư đã phát sinh giao dịch (kế hoạch, đơn hàng, nhập kho, tồn kho, cấp phát).");
        }
    }
}