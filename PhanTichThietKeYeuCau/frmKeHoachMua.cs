using Microsoft.EntityFrameworkCore;
using PhanTichThietKeYeuCau;
using PhanTichThietKeYeuCau.Models;
using System.ComponentModel;
using System.Globalization;

namespace PhanTichThietKeYeuCau;

public class DongKHM
{
    public string MaVatTu { get; set; } = "";
    public string TenVatTu { get; set; } = "";
    public string DonViTinh { get; set; } = "";
    public int SoLuongCan { get; set; }
    public string GhiChu { get; set; } = "";
}

public class frmKeHoachMua : Form
{
    private static Font Bold => new("Segoe UI", 9, FontStyle.Bold);

    private readonly TextBox txtMaKHM = new(), txtTen = new(), txtGhiChu = new(), txtGhiChuCT = new();
    private readonly DateTimePicker dtpThangNam = new(), dtpNgayLap = new();
    private readonly ComboBox cboNhanVienLap = UI.Combo(), cboVatTu = UI.Combo();
    private readonly NumericUpDown numSoLuongCan = UI.Num();
    private readonly Label lblTrangThai = new() { AutoSize = true }, lblNgayDuyet = new() { AutoSize = true },
                           lblDVT = new() { AutoSize = true };
    private DataGridView dgvChiTiet = null!, dgvLichSu = null!;
    private Button btnLuu = null!, btnDuyet = null!, btnXoa = null!;
    private readonly BindingList<DongKHM> _dong = new();
    private Dictionary<string, VAT_TU> _vt = new();
    private string? _ma;           // null = đang lập mới
    private bool _choSua = true;   // chỉ sửa được khi là bản mới hoặc "Dự thảo"

    public frmKeHoachMua()
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
        Text = "KẾ HOẠCH MUA VẬT TƯ";
        ClientSize = new Size(1090, 760);
        StartPosition = FormStartPosition.CenterParent;

        var gb1 = new GroupBox { Text = "THÔNG TIN KẾ HOẠCH MUA" }.At(this, 10, 8, 1070, 130);
        UI.Lbl(gb1, "Mã KH:", 15, 22); txtMaKHM.At(gb1, 85, 22, 110); txtMaKHM.ReadOnly = true;
        UI.Lbl(gb1, "Tên kế hoạch:", 225, 22); txtTen.At(gb1, 325, 22, 400); txtTen.MaxLength = 200;
        UI.Lbl(gb1, "Kỳ (MM/yyyy):", 750, 22); dtpThangNam.At(gb1, 850, 22, 100);
        dtpThangNam.Format = DateTimePickerFormat.Custom; dtpThangNam.CustomFormat = "MM/yyyy"; dtpThangNam.ShowUpDown = true;
        UI.Lbl(gb1, "Người lập:", 15, 57); cboNhanVienLap.At(gb1, 85, 57, 240);
        UI.Lbl(gb1, "Ngày lập:", 350, 57); dtpNgayLap.At(gb1, 420, 57, 120); dtpNgayLap.Format = DateTimePickerFormat.Short;
        UI.Lbl(gb1, "Trạng thái:", 570, 57); lblTrangThai.Font = Bold; lblTrangThai.At(gb1, 650, 61);
        UI.Lbl(gb1, "Ngày duyệt:", 800, 57); lblNgayDuyet.Font = Bold; lblNgayDuyet.At(gb1, 890, 61);
        UI.Lbl(gb1, "Ghi chú:", 15, 92); txtGhiChu.At(gb1, 85, 92, 640); txtGhiChu.MaxLength = 300;

        var gb2 = new GroupBox { Text = "CHI TIẾT KẾ HOẠCH (sửa số lượng, ghi chú ngay trên lưới)" }.At(this, 10, 145, 1070, 290);
        UI.Lbl(gb2, "Vật tư:", 15, 22); cboVatTu.At(gb2, 75, 22, 350);
        UI.Lbl(gb2, "ĐVT:", 440, 22); lblDVT.Font = Bold; lblDVT.At(gb2, 480, 26);
        UI.Lbl(gb2, "SL cần:", 570, 22); numSoLuongCan.At(gb2, 630, 22, 100);
        UI.Lbl(gb2, "Ghi chú:", 745, 22); txtGhiChuCT.At(gb2, 805, 22, 245); txtGhiChuCT.MaxLength = 200;
        UI.Btn(gb2, "Thêm vật tư", 15, 55, 110, ThemVatTu);
        UI.Btn(gb2, "Xóa dòng", 135, 55, 100, XoaDong);
        UI.Btn(gb2, "Gợi ý từ tồn kho", 245, 55, 150, GoiY);
        dgvChiTiet = UI.Grid(gb2, 10, 95, 1050, 185);
        dgvChiTiet.DataSource = _dong;
        dgvChiTiet.ReadOnly = false;
        dgvChiTiet.DataError += (_, e) => { UI.Error("Giá trị không hợp lệ."); e.ThrowException = false; };
        cboVatTu.SelectedIndexChanged += (_, _) =>
            lblDVT.Text = cboVatTu.SelectedItem is ComboItem it && _vt.TryGetValue(it.Value, out var v) ? v.DonViTinh : "";

        UI.Btn(this, "Lập kế hoạch mới", 10, 445, 150, LapMoi);
        btnLuu = UI.Btn(this, "Lưu kế hoạch", 170, 445, 130, Luu);
        btnDuyet = UI.Btn(this, "Duyệt kế hoạch", 310, 445, 140, Duyet);
        btnXoa = UI.Btn(this, "Xóa kế hoạch", 460, 445, 130, Xoa);
        UI.Btn(this, "Đóng", 600, 445, 90, Close);

        new Label { Text = "DANH SÁCH KẾ HOẠCH MUA (nhấp vào một dòng để xem / duyệt)", AutoSize = true, Font = Bold }
            .At(this, 10, 490);
        dgvLichSu = UI.Grid(this, 10, 515, 1070, 235);
        dgvLichSu.CellClick += (_, e) =>
        {
            if (e.RowIndex >= 0) Xem(UI.T(Convert.ToString(dgvLichSu.Rows[e.RowIndex].Cells["MaKHM"].Value)));
        };
    }

    private void FormatGrid()
    {
        UI.Headers(dgvChiTiet, ("MaVatTu", "Mã vật tư"), ("TenVatTu", "Tên vật tư"), ("DonViTinh", "ĐVT"),
            ("SoLuongCan", "SL cần"), ("GhiChu", "Ghi chú"));
        foreach (DataGridViewColumn c in dgvChiTiet.Columns)
            c.ReadOnly = c.Name is not ("SoLuongCan" or "GhiChu");
    }

    private void NapDanhMuc()
    {
        using var db = new QuanLyTieuHaoVatTuContext();
        _vt = db.VAT_TU.AsNoTracking().ToList().ToDictionary(v => UI.T(v.MaVatTu), v => v);
        UI.Bind(cboNhanVienLap, Nguon.NhanVien("Nhân viên mua hàng"));
        UI.Bind(cboVatTu, Nguon.VatTu());
    }

    private void SetEditable()
    {
        dgvChiTiet.ReadOnly = !_choSua;
        txtTen.Enabled = txtGhiChu.Enabled = dtpThangNam.Enabled = dtpNgayLap.Enabled = cboNhanVienLap.Enabled = _choSua;
        btnLuu.Enabled = _choSua;
        btnDuyet.Enabled = btnXoa.Enabled = _ma != null && _choSua;
    }

    private static string NextMa(QuanLyTieuHaoVatTuContext db)
    {
        var max = db.KE_HOACH_MUA.Select(k => k.MaKHM).ToList()
            .Select(c => int.TryParse(UI.T(c).Replace("KHM", ""), out var n) ? n : 0).DefaultIfEmpty(0).Max();
        return "KHM" + (max + 1).ToString("000");
    }

    private void LapMoi()
    {
        _ma = null; _choSua = true;
        _dong.Clear();
        using var db = new QuanLyTieuHaoVatTuContext();
        txtMaKHM.Text = NextMa(db);
        txtTen.Clear(); txtGhiChu.Clear(); txtGhiChuCT.Clear();
        dtpThangNam.Value = DateTime.Today; dtpNgayLap.Value = DateTime.Today;
        lblTrangThai.Text = "Dự thảo (mới)"; lblNgayDuyet.Text = "";
        numSoLuongCan.Value = 0;
        SetEditable();
    }

    private void LoadLichSu()
    {
        using var db = new QuanLyTieuHaoVatTuContext();
        var nv = db.NHAN_VIEN.AsNoTracking().ToList().ToDictionary(n => UI.T(n.MaNhanVien), n => n.HoTen);
        var soDong = db.CHI_TIET_KHM.AsNoTracking().ToList().GroupBy(c => UI.T(c.MaKHM)).ToDictionary(g => g.Key, g => g.Count());
        dgvLichSu.DataSource = db.KE_HOACH_MUA.AsNoTracking().OrderByDescending(k => k.MaKHM).ToList().Select(k => new
        {
            MaKHM = UI.T(k.MaKHM),
            k.TenKeHoach,
            KyKeHoach = UI.T(k.ThangNam),
            TrangThai = k.TrangThai ?? "",
            NguoiLap = nv.GetValueOrDefault(UI.T(k.MaNhanVienLap), ""),
            NgayLap = k.NgayLap.ToString("dd/MM/yyyy"),
            NgayDuyet = k.NgayDuyet?.ToString("dd/MM/yyyy") ?? "",
            SoVatTu = soDong.GetValueOrDefault(UI.T(k.MaKHM), 0)
        }).ToList();
        UI.Headers(dgvLichSu, ("MaKHM", "Mã KH"), ("TenKeHoach", "Tên kế hoạch"), ("KyKeHoach", "Kỳ"),
            ("TrangThai", "Trạng thái"), ("NguoiLap", "Người lập"), ("NgayLap", "Ngày lập"),
            ("NgayDuyet", "Ngày duyệt"), ("SoVatTu", "Số vật tư"));
    }

    private void Xem(string ma)
    {
        if (ma == "") return;
        try
        {
            using var db = new QuanLyTieuHaoVatTuContext();
            var h = db.KE_HOACH_MUA.AsNoTracking().FirstOrDefault(k => k.MaKHM == ma);
            if (h == null) return;
            var ct = (from c in db.CHI_TIET_KHM
                      join v in db.VAT_TU on c.MaVatTu equals v.MaVatTu
                      where c.MaKHM == ma
                      orderby c.MaVatTu
                      select new { c.MaVatTu, v.TenVatTu, v.DonViTinh, c.SoLuongCan, c.GhiChu }).ToList();

            _ma = ma; _choSua = h.TrangThai == "Dự thảo";
            _dong.Clear();
            foreach (var r in ct)
                _dong.Add(new DongKHM
                {
                    MaVatTu = UI.T(r.MaVatTu),
                    TenVatTu = r.TenVatTu,
                    DonViTinh = r.DonViTinh,
                    SoLuongCan = r.SoLuongCan,
                    GhiChu = r.GhiChu ?? ""
                });

            txtMaKHM.Text = ma; txtTen.Text = h.TenKeHoach; txtGhiChu.Text = h.GhiChu ?? "";
            if (DateTime.TryParseExact(UI.T(h.ThangNam), "MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var kt))
                dtpThangNam.Value = kt;
            dtpNgayLap.Value = h.NgayLap.ToDateTime(TimeOnly.MinValue);
            cboNhanVienLap.SelectedValue = UI.T(h.MaNhanVienLap);
            lblTrangThai.Text = h.TrangThai ?? "";
            lblNgayDuyet.Text = h.NgayDuyet?.ToString("dd/MM/yyyy") ?? "";
            SetEditable();
        }
        catch (Exception ex) { UI.Error("Không đọc được kế hoạch: " + (ex.InnerException?.Message ?? ex.Message)); }
    }

    private void ThemVatTu()
    {
        if (!_choSua) { UI.Error("Kế hoạch đã duyệt, không được sửa."); return; }
        if (cboVatTu.SelectedItem is not ComboItem v) { UI.Error("Hãy chọn vật tư."); return; }
        var sl = (int)numSoLuongCan.Value;
        if (sl <= 0) { UI.Error("Số lượng cần phải lớn hơn 0."); return; }

        var co = _dong.FirstOrDefault(d => d.MaVatTu == v.Value);
        if (co != null) { co.SoLuongCan += sl; _dong.ResetBindings(); }
        else
        {
            var vt = _vt[v.Value];
            _dong.Add(new DongKHM
            {
                MaVatTu = v.Value,
                TenVatTu = vt.TenVatTu,
                DonViTinh = vt.DonViTinh,
                SoLuongCan = sl,
                GhiChu = txtGhiChuCT.Text.Trim()
            });
        }
        txtGhiChuCT.Clear();
    }

    private void XoaDong()
    {
        if (!_choSua) { UI.Error("Kế hoạch đã duyệt, không được sửa."); return; }
        if (dgvChiTiet.CurrentRow?.DataBoundItem is DongKHM d) _dong.Remove(d);
    }

    // Gợi ý: vật tư có tổng tồn < mức tối thiểu => cần mua (mức tối đa - tồn hiện tại)
    private void GoiY()
    {
        if (!_choSua) { UI.Error("Kế hoạch đã duyệt, không được sửa."); return; }
        using var db = new QuanLyTieuHaoVatTuContext();
        var ton = db.TON_KHO.AsNoTracking().ToList().GroupBy(t => UI.T(t.MaVatTu))
                    .ToDictionary(g => g.Key, g => g.Sum(t => t.SoLuongTon));
        var them = 0;
        foreach (var v in _vt.Values)
        {
            var ma = UI.T(v.MaVatTu);
            var min = v.MucTonMin ?? 0;
            var t = ton.GetValueOrDefault(ma, 0);
            if (t >= min || _dong.Any(d => d.MaVatTu == ma)) continue;
            var can = (v.MucTonMax ?? min * 2) - t;
            if (can <= 0) continue;
            _dong.Add(new DongKHM
            {
                MaVatTu = ma,
                TenVatTu = v.TenVatTu,
                DonViTinh = v.DonViTinh,
                SoLuongCan = can,
                GhiChu = "Bổ sung: tồn dưới mức tối thiểu"
            });
            them++;
        }
        UI.Info(them == 0 ? "Không có vật tư nào dưới mức tối thiểu hoặc đều đã có trong kế hoạch."
                          : $"Đã thêm {them} vật tư (SL cần = mức tối đa − tồn hiện tại).");
    }

    private void Luu()
    {
        if (!_choSua) { UI.Error("Kế hoạch đã duyệt, không được sửa."); return; }
        dgvChiTiet.EndEdit();
        if (txtTen.Text.Trim() == "") { UI.Error("Hãy nhập tên kế hoạch."); return; }
        if (cboNhanVienLap.SelectedItem is not ComboItem nv) { UI.Error("Hãy chọn người lập."); return; }
        if (_dong.Count == 0) { UI.Error("Kế hoạch chưa có vật tư nào."); return; }
        if (_dong.Any(d => d.SoLuongCan <= 0)) { UI.Error("Số lượng cần của mọi dòng phải lớn hơn 0."); return; }

        var thangNam = dtpThangNam.Value.ToString("MM/yyyy", CultureInfo.InvariantCulture);
        var ngayLap = DateOnly.FromDateTime(dtpNgayLap.Value);
        var ghiChu = string.IsNullOrWhiteSpace(txtGhiChu.Text) ? null : txtGhiChu.Text.Trim();

        try
        {
            using var db = new QuanLyTieuHaoVatTuContext();
            using var tran = db.Database.BeginTransaction();
            string ma;
            if (_ma == null)
            {
                ma = NextMa(db);
                db.KE_HOACH_MUA.Add(new KE_HOACH_MUA
                {
                    MaKHM = ma,
                    TenKeHoach = txtTen.Text.Trim(),
                    ThangNam = thangNam,
                    TrangThai = "Dự thảo",
                    MaNhanVienLap = nv.Value,
                    NgayLap = ngayLap,
                    GhiChu = ghiChu
                });
            }
            else
            {
                ma = _ma;
                var h = db.KE_HOACH_MUA.First(k => k.MaKHM == ma);
                h.TenKeHoach = txtTen.Text.Trim(); h.ThangNam = thangNam;
                h.MaNhanVienLap = nv.Value; h.NgayLap = ngayLap; h.GhiChu = ghiChu;
                db.CHI_TIET_KHM.RemoveRange(db.CHI_TIET_KHM.Where(c => c.MaKHM == ma).ToList());
                db.SaveChanges();        // xóa chi tiết cũ trước để thêm lại không trùng khóa
            }
            foreach (var d in _dong)
                db.CHI_TIET_KHM.Add(new CHI_TIET_KHM
                {
                    MaKHM = ma,
                    MaVatTu = d.MaVatTu,
                    SoLuongCan = d.SoLuongCan,
                    DonViTinh = d.DonViTinh,
                    GhiChu = string.IsNullOrWhiteSpace(d.GhiChu) ? null : d.GhiChu
                });
            db.SaveChanges();
            tran.Commit();
            UI.Info($"Đã lưu kế hoạch {ma} (trạng thái Dự thảo).");
        }
        catch (Exception ex) { UI.Error("Không lưu được:\n" + (ex.InnerException?.Message ?? ex.Message)); return; }

        LoadLichSu(); LapMoi();
    }

    private void Duyet()
    {
        if (_ma == null || !_choSua) { UI.Error("Hãy chọn một kế hoạch đang ở trạng thái Dự thảo để duyệt."); return; }
        if (!UI.Confirm($"Duyệt kế hoạch {_ma}? Sau khi duyệt sẽ không sửa được nữa.")) return;
        var ma = _ma;
        try
        {
            using var db = new QuanLyTieuHaoVatTuContext();
            db.Database.ExecuteSqlInterpolated($"EXEC dbo.sp_DuyetKeHoachMua {ma}");
        }
        catch (Exception ex) { UI.Error("Không duyệt được:\n" + (ex.InnerException?.Message ?? ex.Message)); return; }
        LoadLichSu(); Xem(ma);
    }

    private void Xoa()
    {
        if (_ma == null || !_choSua) { UI.Error("Chỉ xóa được kế hoạch ở trạng thái Dự thảo."); return; }
        if (!UI.Confirm($"Xóa kế hoạch {_ma}?")) return;
        var ma = _ma;
        try
        {
            using var db = new QuanLyTieuHaoVatTuContext();
            db.CHI_TIET_KHM.RemoveRange(db.CHI_TIET_KHM.Where(c => c.MaKHM == ma).ToList());
            var h = db.KE_HOACH_MUA.FirstOrDefault(k => k.MaKHM == ma);
            if (h != null) db.KE_HOACH_MUA.Remove(h);
            db.SaveChanges();
        }
        catch (Exception ex) { UI.Error("Không xóa được:\n" + (ex.InnerException?.Message ?? ex.Message)); return; }
        LoadLichSu(); LapMoi();
    }
}