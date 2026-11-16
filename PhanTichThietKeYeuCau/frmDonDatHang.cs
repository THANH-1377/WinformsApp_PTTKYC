using Microsoft.EntityFrameworkCore;
using PhanTichThietKeYeuCau;
using PhanTichThietKeYeuCau.Models;
using System.ComponentModel;
using System.Text;

namespace PhanTichThietKeYeuCau;

public class DongDDH
{
    public string MaVatTu { get; set; } = "";
    public string TenVatTu { get; set; } = "";
    public string DonViTinh { get; set; } = "";
    public int SoLuongDat { get; set; }
    public decimal DonGiaDat { get; set; }
    public decimal ThanhTien => SoLuongDat * DonGiaDat;
}

public class frmDonDatHang : Form
{
    private static Font Bold => new("Segoe UI", 9, FontStyle.Bold);

    private readonly TextBox txtMaDDH = new(), txtGhiChu = new();
    private readonly DateTimePicker dtpNgayDat = new(), dtpNgayGiao = new();
    private readonly ComboBox cboKHM = UI.Combo(), cboNCC = UI.Combo(), cboNhanVienLap = UI.Combo(), cboVatTu = UI.Combo();
    private readonly NumericUpDown numSoLuong = UI.Num(), numDonGia = UI.Num(0, 100_000_000_000m);
    private readonly Label lblTrangThai = new() { AutoSize = true }, lblDVT = new() { AutoSize = true },
                           lblTong = new() { AutoSize = true };
    private DataGridView dgvChiTiet = null!, dgvLichSu = null!;
    private Button btnLuu = null!, btnDuyet = null!, btnHuy = null!;
    private readonly BindingList<DongDDH> _dong = new();
    private Dictionary<string, VAT_TU> _vt = new();
    private string? _ma;
    private bool _choSua = true;     // chỉ sửa được khi mới lập hoặc "Chờ duyệt"

    public frmDonDatHang()
    {
        BuildUi();
        Load += (_, _) =>
        {
            try { FormatGrid(); NapDanhMuc(); LoadLichSu(); LapMoi(); }
            catch (Exception ex) { UI.Error("Lỗi khởi tạo: " + (ex.InnerException?.Message ?? ex.Message)); }
        };
    }

    private void BuildUi()
    {
        Text = "ĐƠN ĐẶT HÀNG";
        ClientSize = new Size(1090, 780);
        StartPosition = FormStartPosition.CenterParent;

        var gb1 = new GroupBox { Text = "THÔNG TIN ĐƠN ĐẶT HÀNG" }.At(this, 10, 8, 1070, 135);
        UI.Lbl(gb1, "Mã đơn:", 15, 22); txtMaDDH.At(gb1, 85, 22, 110); txtMaDDH.ReadOnly = true;
        UI.Lbl(gb1, "Kế hoạch mua:", 225, 22); cboKHM.At(gb1, 325, 22, 300);
        UI.Lbl(gb1, "Nhà cung cấp:", 650, 22); cboNCC.At(gb1, 750, 22, 300);
        UI.Lbl(gb1, "Người lập:", 15, 57); cboNhanVienLap.At(gb1, 85, 57, 200);
        UI.Lbl(gb1, "Ngày đặt:", 310, 57); dtpNgayDat.At(gb1, 380, 57, 120); dtpNgayDat.Format = DateTimePickerFormat.Short;
        UI.Lbl(gb1, "Giao dự kiến:", 525, 57); dtpNgayGiao.At(gb1, 620, 57, 130);
        dtpNgayGiao.Format = DateTimePickerFormat.Short; dtpNgayGiao.ShowCheckBox = true;
        UI.Lbl(gb1, "Trạng thái:", 780, 57); lblTrangThai.Font = Bold; lblTrangThai.At(gb1, 860, 61);
        UI.Lbl(gb1, "Ghi chú:", 15, 92); txtGhiChu.At(gb1, 85, 92, 600); txtGhiChu.MaxLength = 300;
        lblTong.Font = new Font("Segoe UI", 10, FontStyle.Bold); lblTong.At(gb1, 720, 96);

        var gb2 = new GroupBox { Text = "CHI TIẾT ĐƠN (sửa số lượng, đơn giá ngay trên lưới)" }.At(this, 10, 150, 1070, 285);
        UI.Lbl(gb2, "Vật tư:", 15, 22); cboVatTu.At(gb2, 75, 22, 330);
        UI.Lbl(gb2, "ĐVT:", 420, 22); lblDVT.Font = Bold; lblDVT.At(gb2, 460, 26);
        UI.Lbl(gb2, "SL đặt:", 540, 22); numSoLuong.At(gb2, 600, 22, 100);
        UI.Lbl(gb2, "Đơn giá:", 720, 22); numDonGia.At(gb2, 785, 22, 130);
        UI.Btn(gb2, "Thêm vật tư", 15, 55, 110, ThemVatTu);
        UI.Btn(gb2, "Xóa dòng", 135, 55, 100, XoaDong);
        UI.Btn(gb2, "Nạp vật tư từ kế hoạch mua", 245, 55, 230, NapTuKeHoach);
        dgvChiTiet = UI.Grid(gb2, 10, 95, 1050, 180);
        dgvChiTiet.DataSource = _dong;
        dgvChiTiet.ReadOnly = false;
        dgvChiTiet.CellEndEdit += (_, _) => { dgvChiTiet.Refresh(); CapNhatTong(); };
        dgvChiTiet.DataError += (_, e) => { UI.Error("Giá trị không hợp lệ."); e.ThrowException = false; };
        cboVatTu.SelectedIndexChanged += (_, _) =>
        {
            if (cboVatTu.SelectedItem is ComboItem it && _vt.TryGetValue(it.Value, out var v))
            { lblDVT.Text = v.DonViTinh; numDonGia.Value = v.DonGiaMua ?? 0m; }
        };

        UI.Btn(this, "Lập đơn mới", 10, 445, 120, LapMoi);
        btnLuu = UI.Btn(this, "Lưu đơn", 140, 445, 110, Luu);
        btnDuyet = UI.Btn(this, "Duyệt đơn", 260, 445, 110, Duyet);
        btnHuy = UI.Btn(this, "Hủy đơn", 380, 445, 100, HuyDon);
        UI.Btn(this, "In đơn", 490, 445, 90, InDon);
        UI.Btn(this, "Đóng", 590, 445, 80, Close);

        new Label { Text = "DANH SÁCH ĐƠN ĐẶT HÀNG (nhấp vào một dòng để xem / duyệt)", AutoSize = true, Font = Bold }
            .At(this, 10, 490);
        dgvLichSu = UI.Grid(this, 10, 515, 1070, 255);
        dgvLichSu.CellClick += (_, e) =>
        {
            if (e.RowIndex >= 0) Xem(UI.T(Convert.ToString(dgvLichSu.Rows[e.RowIndex].Cells["MaDDH"].Value)));
        };
    }

    private void FormatGrid()
    {
        UI.Headers(dgvChiTiet, ("MaVatTu", "Mã vật tư"), ("TenVatTu", "Tên vật tư"), ("DonViTinh", "ĐVT"),
            ("SoLuongDat", "SL đặt"), ("DonGiaDat", "Đơn giá"), ("ThanhTien", "Thành tiền"));
        foreach (DataGridViewColumn c in dgvChiTiet.Columns)
            c.ReadOnly = c.Name is not ("SoLuongDat" or "DonGiaDat");
        foreach (var n in new[] { "DonGiaDat", "ThanhTien" })
            if (dgvChiTiet.Columns.Contains(n)) dgvChiTiet.Columns[n]!.DefaultCellStyle.Format = "N0";
    }

    private void NapDanhMuc()
    {
        using var db = new QuanLyTieuHaoVatTuContext();
        _vt = db.VAT_TU.AsNoTracking().ToList().ToDictionary(v => UI.T(v.MaVatTu), v => v);
        UI.Bind(cboKHM, Nguon.KeHoachMua("Đã duyệt", "Đang thực hiện"));
        UI.Bind(cboNCC, Nguon.NhaCungCap());
        UI.Bind(cboNhanVienLap, Nguon.NhanVien("Nhân viên mua hàng"));
        UI.Bind(cboVatTu, Nguon.VatTu());
    }

    private void CapNhatTong() =>
        lblTong.Text = "Tổng giá trị: " + _dong.Sum(d => d.ThanhTien).ToString("N0") + " đ";

    private void SetEditable()
    {
        dgvChiTiet.ReadOnly = !_choSua;
        cboKHM.Enabled = cboNCC.Enabled = cboNhanVienLap.Enabled = dtpNgayDat.Enabled =
            dtpNgayGiao.Enabled = txtGhiChu.Enabled = _choSua;
        btnLuu.Enabled = _choSua;
        btnDuyet.Enabled = _ma != null && lblTrangThai.Text == "Chờ duyệt" && Session.Co(Chuc.DuyetChungTu);
        btnHuy.Enabled = _ma != null && lblTrangThai.Text is "Chờ duyệt" or "Đã duyệt" && Session.Co(Chuc.DuyetChungTu);
    }

    private static string NextMa(QuanLyTieuHaoVatTuContext db)
    {
        var max = db.DON_DAT_HANG.Select(d => d.MaDDH).ToList()
            .Select(c => int.TryParse(UI.T(c).Replace("DDH", ""), out var n) ? n : 0).DefaultIfEmpty(0).Max();
        return "DDH" + (max + 1).ToString("000");
    }

    private void LapMoi()
    {
        _ma = null; _choSua = true;
        _dong.Clear();
        using var db = new QuanLyTieuHaoVatTuContext();
        txtMaDDH.Text = NextMa(db);
        txtGhiChu.Clear();
        dtpNgayDat.Value = DateTime.Today; dtpNgayGiao.Value = DateTime.Today.AddDays(7); dtpNgayGiao.Checked = true;
        cboKHM.SelectedIndex = -1; cboNCC.SelectedIndex = -1;
        lblTrangThai.Text = "Chờ duyệt (mới)";
        if (Session.MaNhanVien != null) cboNhanVienLap.SelectedValue = Session.MaNhanVien;   // frmKeHoachMua, frmDonDatHang
        SetEditable(); CapNhatTong();
    }

    private void LoadLichSu()
    {
        using var db = new QuanLyTieuHaoVatTuContext();
        var ncc = db.NHA_CUNG_CAP.AsNoTracking().ToList().ToDictionary(n => UI.T(n.MaNCC), n => n.TenNCC);
        dgvLichSu.DataSource = db.DON_DAT_HANG.AsNoTracking().OrderByDescending(d => d.MaDDH).ToList().Select(d => new
        {
            MaDDH = UI.T(d.MaDDH),
            KeHoach = UI.T(d.MaKHM),
            NhaCungCap = ncc.GetValueOrDefault(UI.T(d.MaNCC), ""),
            NgayDat = d.NgayDat.ToString("dd/MM/yyyy"),
            NgayGiao = d.NgayGiaoDuKien?.ToString("dd/MM/yyyy") ?? "",
            TrangThai = d.TrangThai ?? "",
            TongGiaTri = d.TongGiaTri ?? 0m
        }).ToList();
        UI.Headers(dgvLichSu, ("MaDDH", "Mã đơn"), ("KeHoach", "Kế hoạch mua"), ("NhaCungCap", "Nhà cung cấp"),
            ("NgayDat", "Ngày đặt"), ("NgayGiao", "Giao dự kiến"), ("TrangThai", "Trạng thái"), ("TongGiaTri", "Tổng giá trị"));
        if (dgvLichSu.Columns.Contains("TongGiaTri")) dgvLichSu.Columns["TongGiaTri"]!.DefaultCellStyle.Format = "N0";
    }

    private void Xem(string ma)
    {
        if (ma == "") return;
        try
        {
            using var db = new QuanLyTieuHaoVatTuContext();
            var h = db.DON_DAT_HANG.AsNoTracking().FirstOrDefault(d => d.MaDDH == ma);
            if (h == null) return;
            var ct = (from c in db.CHI_TIET_DDH
                      join v in db.VAT_TU on c.MaVatTu equals v.MaVatTu
                      where c.MaDDH == ma
                      orderby c.MaVatTu
                      select new { c.MaVatTu, v.TenVatTu, v.DonViTinh, c.SoLuongDat, c.DonGiaDat }).ToList();

            _ma = ma; _choSua = h.TrangThai == "Chờ duyệt";
            _dong.Clear();
            foreach (var r in ct)
                _dong.Add(new DongDDH
                {
                    MaVatTu = UI.T(r.MaVatTu),
                    TenVatTu = r.TenVatTu,
                    DonViTinh = r.DonViTinh,
                    SoLuongDat = r.SoLuongDat,
                    DonGiaDat = r.DonGiaDat ?? 0m
                });

            txtMaDDH.Text = ma; txtGhiChu.Text = h.GhiChu ?? "";
            dtpNgayDat.Value = h.NgayDat.ToDateTime(TimeOnly.MinValue);
            if (h.NgayGiaoDuKien is DateOnly g) { dtpNgayGiao.Value = g.ToDateTime(TimeOnly.MinValue); dtpNgayGiao.Checked = true; }
            else dtpNgayGiao.Checked = false;
            cboKHM.SelectedValue = UI.T(h.MaKHM);
            cboNCC.SelectedValue = UI.T(h.MaNCC);
            cboNhanVienLap.SelectedValue = UI.T(h.MaNhanVienLap);
            lblTrangThai.Text = h.TrangThai ?? "";
            SetEditable(); CapNhatTong();
        }
        catch (Exception ex) { UI.Error("Không đọc được đơn: " + (ex.InnerException?.Message ?? ex.Message)); }
    }

    private void ThemVatTu()
    {
        if (!_choSua) { UI.Error("Đơn không còn ở trạng thái Chờ duyệt nên không được sửa."); return; }
        if (cboVatTu.SelectedItem is not ComboItem v) { UI.Error("Hãy chọn vật tư."); return; }
        var sl = (int)numSoLuong.Value;
        if (sl <= 0) { UI.Error("Số lượng đặt phải lớn hơn 0."); return; }
        var co = _dong.FirstOrDefault(d => d.MaVatTu == v.Value);
        if (co != null) { co.SoLuongDat += sl; co.DonGiaDat = numDonGia.Value; _dong.ResetBindings(); }
        else
        {
            var vt = _vt[v.Value];
            _dong.Add(new DongDDH
            {
                MaVatTu = v.Value,
                TenVatTu = vt.TenVatTu,
                DonViTinh = vt.DonViTinh,
                SoLuongDat = sl,
                DonGiaDat = numDonGia.Value
            });
        }
        CapNhatTong();
    }

    private void XoaDong()
    {
        if (!_choSua) { UI.Error("Đơn không còn ở trạng thái Chờ duyệt nên không được sửa."); return; }
        if (dgvChiTiet.CurrentRow?.DataBoundItem is DongDDH d) { _dong.Remove(d); CapNhatTong(); }
    }

    private void NapTuKeHoach()
    {
        if (!_choSua) { UI.Error("Đơn không còn ở trạng thái Chờ duyệt nên không được sửa."); return; }
        if (cboKHM.SelectedItem is not ComboItem k) { UI.Error("Hãy chọn kế hoạch mua trước."); return; }
        using var db = new QuanLyTieuHaoVatTuContext();
        var ct = db.CHI_TIET_KHM.AsNoTracking().Where(c => c.MaKHM == k.Value).OrderBy(c => c.MaVatTu).ToList();
        var them = 0;
        foreach (var c in ct)
        {
            var ma = UI.T(c.MaVatTu);
            if (_dong.Any(d => d.MaVatTu == ma) || !_vt.TryGetValue(ma, out var v)) continue;
            _dong.Add(new DongDDH
            {
                MaVatTu = ma,
                TenVatTu = v.TenVatTu,
                DonViTinh = v.DonViTinh,
                SoLuongDat = c.SoLuongCan,
                DonGiaDat = v.DonGiaMua ?? 0m
            });
            them++;
        }
        CapNhatTong();
        UI.Info($"Đã nạp {them} vật tư từ kế hoạch {k.Value}. Hãy kiểm tra lại số lượng và đơn giá.");
    }

    private void Luu()
    {
        if (!_choSua) { UI.Error("Đơn không còn ở trạng thái Chờ duyệt nên không được sửa."); return; }
        dgvChiTiet.EndEdit();
        if (cboKHM.SelectedItem is not ComboItem khm) { UI.Error("Hãy chọn kế hoạch mua (đã duyệt)."); return; }
        if (cboNCC.SelectedItem is not ComboItem ncc) { UI.Error("Hãy chọn nhà cung cấp."); return; }
        if (cboNhanVienLap.SelectedItem is not ComboItem nv) { UI.Error("Hãy chọn người lập."); return; }
        if (_dong.Count == 0) { UI.Error("Đơn chưa có vật tư nào."); return; }
        if (_dong.Any(d => d.SoLuongDat <= 0 || d.DonGiaDat < 0))
        { UI.Error("Số lượng đặt phải lớn hơn 0 và đơn giá không âm."); return; }

        var ngayDat = DateOnly.FromDateTime(dtpNgayDat.Value);
        DateOnly? ngayGiao = dtpNgayGiao.Checked ? DateOnly.FromDateTime(dtpNgayGiao.Value) : null;
        if (ngayGiao < ngayDat) { UI.Error("Ngày giao dự kiến phải sau hoặc bằng ngày đặt."); return; }
        var ghiChu = string.IsNullOrWhiteSpace(txtGhiChu.Text) ? null : txtGhiChu.Text.Trim();

        try
        {
            using var db = new QuanLyTieuHaoVatTuContext();
            using var tran = db.Database.BeginTransaction();
            string ma;
            if (_ma == null)
            {
                ma = NextMa(db);
                db.DON_DAT_HANG.Add(new DON_DAT_HANG
                {
                    MaDDH = ma,
                    MaKHM = khm.Value,
                    MaNCC = ncc.Value,
                    MaNhanVienLap = nv.Value,
                    NgayDat = ngayDat,
                    NgayGiaoDuKien = ngayGiao,
                    TrangThai = "Chờ duyệt",
                    GhiChu = ghiChu
                });
                // lập đơn từ kế hoạch đã duyệt thì kế hoạch chuyển sang "Đang thực hiện"
                var k = db.KE_HOACH_MUA.FirstOrDefault(x => x.MaKHM == khm.Value && x.TrangThai == "Đã duyệt");
                if (k != null) k.TrangThai = "Đang thực hiện";
            }
            else
            {
                ma = _ma;
                var h = db.DON_DAT_HANG.First(d => d.MaDDH == ma);
                h.MaKHM = khm.Value; h.MaNCC = ncc.Value; h.MaNhanVienLap = nv.Value;
                h.NgayDat = ngayDat; h.NgayGiaoDuKien = ngayGiao; h.GhiChu = ghiChu;
                db.CHI_TIET_DDH.RemoveRange(db.CHI_TIET_DDH.Where(c => c.MaDDH == ma).ToList());
                db.SaveChanges();
            }
            foreach (var d in _dong)
                db.CHI_TIET_DDH.Add(new CHI_TIET_DDH { MaDDH = ma, MaVatTu = d.MaVatTu, SoLuongDat = d.SoLuongDat, DonGiaDat = d.DonGiaDat });
            db.SaveChanges();           // trigger tự tính TongGiaTri
            tran.Commit();
            UI.Info($"Đã lưu đơn {ma} (trạng thái Chờ duyệt). Tổng giá trị: {_dong.Sum(d => d.ThanhTien):N0} đ.");
        }
        catch (Exception ex) { UI.Error("Không lưu được:\n" + (ex.InnerException?.Message ?? ex.Message)); return; }

        LoadLichSu(); LapMoi();
    }

    private void Duyet()
    {
        if (!Session.Co(Chuc.DuyetChungTu)) { UI.Error("Chỉ Quản lý vật tư mới được duyệt đơn đặt hàng."); return; }
        if (_ma == null || lblTrangThai.Text != "Chờ duyệt") { UI.Error("Hãy chọn một đơn đang Chờ duyệt để duyệt."); return; }
        if (!UI.Confirm($"Duyệt đơn {_ma} và gửi cho nhà cung cấp?")) return;
        var ma = _ma;
        try
        {
            using var db = new QuanLyTieuHaoVatTuContext();
            db.Database.ExecuteSqlInterpolated($"EXEC dbo.sp_DuyetDonDatHang {ma}");
        }
        catch (Exception ex) { UI.Error("Không duyệt được:\n" + (ex.InnerException?.Message ?? ex.Message)); return; }
        LoadLichSu(); Xem(ma);
    }

    private void HuyDon()
    {
        if (!Session.Co(Chuc.DuyetChungTu)) { UI.Error("Chỉ Quản lý vật tư mới được hủy đơn đặt hàng."); return; }
        if (_ma == null) { UI.Error("Hãy chọn một đơn trong danh sách."); return; }
        var ma = _ma;
        try
        {
            using var db = new QuanLyTieuHaoVatTuContext();
            var d = db.DON_DAT_HANG.FirstOrDefault(x => x.MaDDH == ma);
            if (d == null) return;
            if (d.TrangThai is not ("Chờ duyệt" or "Đã duyệt")) { UI.Error("Chỉ hủy được đơn Chờ duyệt hoặc Đã duyệt."); return; }
            if (db.PHIEU_NHAP_KHO.Any(p => p.MaDDH == ma)) { UI.Error("Đơn đã nhập kho, không thể hủy."); return; }
            if (!UI.Confirm($"Hủy đơn {ma}?")) return;
            d.TrangThai = "Hủy";
            db.SaveChanges();
        }
        catch (Exception ex) { UI.Error("Không hủy được:\n" + (ex.InnerException?.Message ?? ex.Message)); return; }
        LoadLichSu(); Xem(ma);
    }

    private void InDon()
    {
        if (_dong.Count == 0) { UI.Error("Đơn chưa có vật tư để in."); return; }
        var sb = new StringBuilder();
        sb.AppendLine("ĐƠN ĐẶT HÀNG");
        sb.AppendLine($"Mã đơn: {txtMaDDH.Text}     Ngày đặt: {dtpNgayDat.Value:dd/MM/yyyy}");
        sb.AppendLine($"Nhà cung cấp: {cboNCC.Text}");
        sb.AppendLine($"Kế hoạch mua: {cboKHM.Text}");
        sb.AppendLine(new string('-', 86));
        sb.AppendLine($"{"Mã",-8}{"Tên vật tư",-32}{"ĐVT",-7}{"SL",8}{"Đơn giá",14}{"Thành tiền",17}");
        foreach (var d in _dong)
            sb.AppendLine($"{d.MaVatTu,-8}{(d.TenVatTu.Length > 30 ? d.TenVatTu[..30] : d.TenVatTu),-32}{d.DonViTinh,-7}" +
                          $"{d.SoLuongDat,8:N0}{d.DonGiaDat,14:N0}{d.ThanhTien,17:N0}");
        sb.AppendLine(new string('-', 86));
        sb.AppendLine(lblTong.Text);
        sb.AppendLine();
        sb.AppendLine("Người lập (ký tên)                        Quản lý vật tư (ký tên)");
        UI.PrintPreview(this, sb.ToString());
    }
}