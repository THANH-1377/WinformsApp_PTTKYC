using Microsoft.EntityFrameworkCore;
using PhanTichThietKeYeuCau;
using PhanTichThietKeYeuCau.Models;
using System.ComponentModel;
using System.Globalization;
using System.Text;

namespace PhanTichThietKeYeuCau;

public class DongNhap
{
    public string MaVatTu { get; set; } = "";
    public string TenVatTu { get; set; } = "";
    public string DonViTinh { get; set; } = "";
    public int SoLuongDat { get; set; }
    public int SoLuongNhap { get; set; }
    public decimal DonGiaNhap { get; set; }
    public string SoLoSX { get; set; } = "";
    public string HanSuDung { get; set; } = "";     // dd/MM/yyyy
}

public partial class frmNhapKho : Form
{
    private readonly TextBox txtMaPhieuNK = new(), txtGhiChu = new();
    private readonly DateTimePicker dtpNgayNhap = new();
    private readonly ComboBox cboKho = UI.Combo(), cboNhanVienNK = UI.Combo();
    private readonly Label lblDon = new() { AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) },
                           lblKetQuaKT = new() { AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) },
                           lblTong = new() { AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
    private DataGridView dgvDonCho = null!, dgvChiTietNhap = null!, dgvPhieuNhap = null!;
    private Button btnLuu = null!, btnXoa = null!;

    private readonly BindingList<DongNhap> _dong = new();
    private string? _maDdh;             // đơn đang chọn để lập phiếu
    private string? _maPhieuDangXem;    // phiếu đã lưu đang xem (null = đang lập mới)
    private bool _duDieuKien;           // kết quả kiểm tra cho phép nhập kho?

    public frmNhapKho()
    {
        InitializeComponent();
        BuildUi();
        Load += (_, _) =>
        {
            try
            {
                FormatGridChiTiet();
                NapDanhMuc();
                LoadDonCho();
                LoadPhieu();
                LapPhieuMoi();
            }
            catch (Exception ex) { UI.Error("Lỗi khởi tạo: " + (ex.InnerException?.Message ?? ex.Message)); }
        };
    }

    // =================== DỰNG GIAO DIỆN ===================
    private void BuildUi()
    {
        Text = "NHẬP KHO";
        ClientSize = new Size(1090, 770);
        StartPosition = FormStartPosition.CenterParent;

        // --- Danh sách đơn chưa nhập kho ---
        var gbDon = new GroupBox { Text = "ĐƠN ĐẶT HÀNG CHƯA NHẬP KHO (nhấp vào một dòng để lập phiếu nhập)" }
            .At(this, 10, 8, 1070, 170);
        dgvDonCho = UI.Grid(gbDon, 10, 22, 1050, 138);
        dgvDonCho.CellClick += (_, e) =>
        {
            if (e.RowIndex < 0) return;
            var r = dgvDonCho.Rows[e.RowIndex];
            ChonDon(Convert.ToString(r.Cells["MaDDH"].Value) ?? "",
                    Convert.ToString(r.Cells["NhaCungCap"].Value) ?? "");
        };
        dgvDonCho.DataBindingComplete += (_, _) =>
        {
            foreach (DataGridViewRow r in dgvDonCho.Rows)
            {
                var kq = Convert.ToString(r.Cells["KetQuaKiemTra"].Value);
                r.DefaultCellStyle.ForeColor = kq == "Đạt" ? Color.DarkGreen
                                             : kq == "Đạt có điều kiện" ? Color.DarkOrange
                                             : Color.Firebrick;
            }
        };

        // --- Thông tin phiếu ---
        var gbPhieu = new GroupBox { Text = "THÔNG TIN PHIẾU NHẬP" }.At(this, 10, 184, 1070, 125);
        UI.Lbl(gbPhieu, "Mã phiếu:", 15, 22); txtMaPhieuNK.At(gbPhieu, 95, 22, 120); txtMaPhieuNK.ReadOnly = true;
        UI.Lbl(gbPhieu, "Ngày nhập:", 240, 22); dtpNgayNhap.At(gbPhieu, 320, 22, 130);
        dtpNgayNhap.Format = DateTimePickerFormat.Short;
        UI.Lbl(gbPhieu, "Kho nhập:", 470, 22); cboKho.At(gbPhieu, 545, 22, 230);
        UI.Lbl(gbPhieu, "Thủ kho nhập:", 795, 22); cboNhanVienNK.At(gbPhieu, 890, 22, 165);
        UI.Lbl(gbPhieu, "Đơn đặt hàng:", 15, 57); lblDon.At(gbPhieu, 110, 61);
        UI.Lbl(gbPhieu, "Kết quả kiểm tra:", 560, 57); lblKetQuaKT.At(gbPhieu, 680, 61);
        UI.Lbl(gbPhieu, "Ghi chú:", 15, 92); txtGhiChu.At(gbPhieu, 95, 92, 680); txtGhiChu.MaxLength = 200;

        // --- Chi tiết nhập kho (lưới cho sửa trực tiếp) ---
        var gbCT = new GroupBox { Text = "CHI TIẾT NHẬP KHO (sửa số lượng nhập, số lô, hạn sử dụng ngay trên lưới)" }
            .At(this, 10, 315, 1070, 215);
        dgvChiTietNhap = UI.Grid(gbCT, 10, 22, 1050, 183);
        dgvChiTietNhap.DataSource = _dong;
        dgvChiTietNhap.ReadOnly = false;
        dgvChiTietNhap.CellEndEdit += (_, _) => CapNhatTong();
        dgvChiTietNhap.DataError += (_, e) =>
        {
            UI.Error("Giá trị nhập không hợp lệ (số lượng phải là số nguyên không âm).");
            e.ThrowException = false;
        };

        // --- Nút chức năng ---
        UI.Btn(this, "Lập phiếu mới", 10, 535, 120, LapPhieuMoi);
        btnLuu = UI.Btn(this, "Lưu phiếu nhập", 140, 535, 130, LuuPhieu);
        btnXoa = UI.Btn(this, "Xóa phiếu đang xem", 280, 535, 160, XoaPhieu);
        UI.Btn(this, "In phiếu", 450, 535, 90, InPhieu);
        UI.Btn(this, "Đóng", 550, 535, 90, Close);
        lblTong.At(this, 670, 541);

        // --- Lịch sử phiếu nhập ---
        new Label
        {
            Text = "LỊCH SỬ PHIẾU NHẬP (nhấp vào một dòng để xem chi tiết)",
            AutoSize = true,
            Font = new Font("Segoe UI", 9, FontStyle.Bold)
        }.At(this, 10, 580);
        dgvPhieuNhap = UI.Grid(this, 10, 605, 1070, 155);
        dgvPhieuNhap.CellClick += (_, e) =>
        {
            if (e.RowIndex < 0) return;
            XemPhieu(UI.T(Convert.ToString(dgvPhieuNhap.Rows[e.RowIndex].Cells["MaPhieuNK"].Value)));
        };
    }

    private void FormatGridChiTiet()
    {
        UI.Headers(dgvChiTietNhap, ("MaVatTu", "Mã vật tư"), ("TenVatTu", "Tên vật tư"), ("DonViTinh", "ĐVT"),
            ("SoLuongDat", "SL đặt"), ("SoLuongNhap", "SL nhập"), ("DonGiaNhap", "Đơn giá nhập"),
            ("SoLoSX", "Số lô SX"), ("HanSuDung", "Hạn dùng (dd/MM/yyyy)"));
        foreach (DataGridViewColumn c in dgvChiTietNhap.Columns)
            c.ReadOnly = c.Name is not ("SoLuongNhap" or "SoLoSX" or "HanSuDung");
        if (dgvChiTietNhap.Columns.Contains("DonGiaNhap"))
            dgvChiTietNhap.Columns["DonGiaNhap"]!.DefaultCellStyle.Format = "N0";
    }

    private static void Chon(ComboBox c, string value)
    {
        if (c.DataSource != null && value != "") c.SelectedValue = value;
    }

    // e = true: đang lập phiếu mới (cho sửa); e = false: đang xem phiếu đã lưu
    private void SetEditable(bool e)
    {
        dgvChiTietNhap.ReadOnly = !e;
        dtpNgayNhap.Enabled = cboKho.Enabled = cboNhanVienNK.Enabled = txtGhiChu.Enabled = e;
        btnLuu.Enabled = e;
        btnXoa.Enabled = !e;
    }

    private void CapNhatTong() =>
        lblTong.Text = "Tổng giá trị nhập: " + _dong.Sum(d => d.SoLuongNhap * d.DonGiaNhap).ToString("N0") + " đ";

    // =================== NẠP DỮ LIỆU ===================
    private void NapDanhMuc()
    {
        using var db = new QuanLyTieuHaoVatTuContext();
        UI.Bind(cboKho, db.KHO.AsNoTracking().OrderBy(k => k.MaKho).ToList()
            .Select(k => new ComboItem(UI.T(k.MaKho), k.TenKho)).ToList());

        var nvs = db.NHAN_VIEN.AsNoTracking().Where(n => n.ChucVu == "Thủ kho").OrderBy(n => n.MaNhanVien).ToList();
        if (nvs.Count == 0) nvs = db.NHAN_VIEN.AsNoTracking().OrderBy(n => n.MaNhanVien).ToList();
        UI.Bind(cboNhanVienNK, nvs.Select(n => new ComboItem(UI.T(n.MaNhanVien), n.HoTen)).ToList());
    }

    // Đơn chưa có phiếu nhập và chưa hủy
    private void LoadDonCho()
    {
        using var db = new QuanLyTieuHaoVatTuContext();
        var daNhap = db.PHIEU_NHAP_KHO.Select(p => p.MaDDH).ToList().Select(UI.T).ToHashSet();
        var kt = db.PHIEU_KIEM_TRA_CL.AsNoTracking().ToList().ToDictionary(k => UI.T(k.MaDDH), k => k);
        var ncc = db.NHA_CUNG_CAP.AsNoTracking().ToList().ToDictionary(n => UI.T(n.MaNCC), n => n.TenNCC);

        var dons = db.DON_DAT_HANG.AsNoTracking().Where(d => d.TrangThai != "Hủy")
            .OrderBy(d => d.MaDDH).ToList()
            .Where(d => !daNhap.Contains(UI.T(d.MaDDH)))
            .Select(d =>
            {
                kt.TryGetValue(UI.T(d.MaDDH), out var k);
                return new
                {
                    MaDDH = UI.T(d.MaDDH),
                    NhaCungCap = ncc.GetValueOrDefault(UI.T(d.MaNCC), ""),
                    NgayDat = d.NgayDat.ToString("dd/MM/yyyy"),
                    NgayGiaoDuKien = d.NgayGiaoDuKien?.ToString("dd/MM/yyyy") ?? "",
                    TongGiaTri = d.TongGiaTri ?? 0m,
                    TrangThai = d.TrangThai ?? "",
                    KetQuaKiemTra = k?.KetQua ?? "Chưa kiểm tra"
                };
            }).ToList();

        dgvDonCho.DataSource = dons;
        UI.Headers(dgvDonCho, ("MaDDH", "Mã đơn"), ("NhaCungCap", "Nhà cung cấp"), ("NgayDat", "Ngày đặt"),
            ("NgayGiaoDuKien", "Ngày giao dự kiến"), ("TongGiaTri", "Tổng giá trị"),
            ("TrangThai", "Trạng thái đơn"), ("KetQuaKiemTra", "Kết quả kiểm tra"));
        if (dgvDonCho.Columns.Contains("TongGiaTri"))
            dgvDonCho.Columns["TongGiaTri"]!.DefaultCellStyle.Format = "N0";
    }

    private void LoadPhieu()
    {
        using var db = new QuanLyTieuHaoVatTuContext();
        var ps = db.PHIEU_NHAP_KHO.AsNoTracking().OrderByDescending(p => p.MaPhieuNK).ToList();
        var khos = db.KHO.AsNoTracking().ToList().ToDictionary(k => UI.T(k.MaKho), k => k.TenKho);
        var nvs = db.NHAN_VIEN.AsNoTracking().ToList().ToDictionary(n => UI.T(n.MaNhanVien), n => n.HoTen);
        var tenNcc = (from d in db.DON_DAT_HANG
                      join n in db.NHA_CUNG_CAP on d.MaNCC equals n.MaNCC
                      select new { d.MaDDH, n.TenNCC }).ToList()
                     .ToDictionary(x => UI.T(x.MaDDH), x => x.TenNCC);
        var gt = db.CHI_TIET_NHAP_KHO.AsNoTracking().ToList()
                   .GroupBy(c => UI.T(c.MaPhieuNK))
                   .ToDictionary(g => g.Key, g => g.Sum(c => c.SoLuongNhap * (c.DonGiaNhap ?? 0m)));

        dgvPhieuNhap.DataSource = ps.Select(p => new
        {
            MaPhieuNK = UI.T(p.MaPhieuNK),
            MaDDH = UI.T(p.MaDDH),
            NhaCungCap = tenNcc.GetValueOrDefault(UI.T(p.MaDDH), ""),
            Kho = khos.GetValueOrDefault(UI.T(p.MaKho), ""),
            NgayNhap = p.NgayNhap.ToString("dd/MM/yyyy"),
            NguoiNhap = nvs.GetValueOrDefault(UI.T(p.MaNhanVienNK), ""),
            GiaTri = gt.GetValueOrDefault(UI.T(p.MaPhieuNK), 0m)
        }).ToList();

        UI.Headers(dgvPhieuNhap, ("MaPhieuNK", "Mã phiếu"), ("MaDDH", "Mã đơn"), ("NhaCungCap", "Nhà cung cấp"),
            ("Kho", "Kho nhập"), ("NgayNhap", "Ngày nhập"), ("NguoiNhap", "Người nhập"), ("GiaTri", "Giá trị nhập"));
        if (dgvPhieuNhap.Columns.Contains("GiaTri"))
            dgvPhieuNhap.Columns["GiaTri"]!.DefaultCellStyle.Format = "N0";
    }

    private static string NextMaPhieu(QuanLyTieuHaoVatTuContext db)
    {
        var max = db.PHIEU_NHAP_KHO.Select(p => p.MaPhieuNK).ToList()
            .Select(c => int.TryParse(UI.T(c).Replace("NK", ""), out var n) ? n : 0)
            .DefaultIfEmpty(0).Max();
        return "NK" + (max + 1).ToString("000");
    }

    private static Dictionary<string, int> TonCuaKho(string maKho, IEnumerable<string> maVatTu)
    {
        var ds = maVatTu.ToList();
        using var db = new QuanLyTieuHaoVatTuContext();
        return db.TON_KHO.AsNoTracking().Where(t => t.MaKho == maKho && ds.Contains(t.MaVatTu))
            .ToList().ToDictionary(t => UI.T(t.MaVatTu), t => t.SoLuongTon);
    }

    // =================== LẬP PHIẾU ===================
    private void LapPhieuMoi()
    {
        _maPhieuDangXem = null;
        _maDdh = null;
        _duDieuKien = false;
        _dong.Clear();
        SetEditable(true);
        using var db = new QuanLyTieuHaoVatTuContext();
        txtMaPhieuNK.Text = NextMaPhieu(db);
        dtpNgayNhap.Value = DateTime.Today;
        txtGhiChu.Clear();
        lblDon.Text = "(chưa chọn đơn)";
        lblKetQuaKT.Text = "";
        CapNhatTong();
    }

    private void ChonDon(string ma, string tenNcc)
    {
        if (ma == "") return;
        try
        {
            using var db = new QuanLyTieuHaoVatTuContext();
            var kt = db.PHIEU_KIEM_TRA_CL.AsNoTracking().FirstOrDefault(k => k.MaDDH == ma);
            var ct = (from c in db.CHI_TIET_DDH
                      join v in db.VAT_TU on c.MaVatTu equals v.MaVatTu
                      where c.MaDDH == ma
                      orderby c.MaVatTu
                      select new { c.MaVatTu, v.TenVatTu, v.DonViTinh, c.SoLuongDat, c.DonGiaDat }).ToList();

            _maPhieuDangXem = null;
            _maDdh = ma;
            SetEditable(true);
            txtMaPhieuNK.Text = NextMaPhieu(db);
            _dong.Clear();
            foreach (var r in ct)
                _dong.Add(new DongNhap
                {
                    MaVatTu = UI.T(r.MaVatTu),
                    TenVatTu = r.TenVatTu,
                    DonViTinh = r.DonViTinh,
                    SoLuongDat = r.SoLuongDat,
                    SoLuongNhap = r.SoLuongDat,   // mặc định nhập đủ số đặt
                    DonGiaNhap = r.DonGiaDat ?? 0m
                });

            lblDon.Text = $"{ma} - {tenNcc}";
            var kq = kt?.KetQua;
            _duDieuKien = kq is "Đạt" or "Đạt có điều kiện";
            var loi = kt?.TiLeLoiPhatHien is decimal l ? $" (lỗi {l:N2}%)" : "";
            lblKetQuaKT.Text = (kq ?? "Chưa kiểm tra") + loi + (_duDieuKien ? "" : "  →  KHÔNG ĐƯỢC NHẬP KHO");
            lblKetQuaKT.ForeColor = kq == "Đạt" ? Color.DarkGreen
                                  : kq == "Đạt có điều kiện" ? Color.DarkOrange : Color.Firebrick;
            CapNhatTong();
        }
        catch (Exception ex) { UI.Error("Không đọc được chi tiết đơn: " + (ex.InnerException?.Message ?? ex.Message)); }
    }

    private void LuuPhieu()
    {
        if (_maPhieuDangXem != null)
        { UI.Error("Đây là phiếu đã lưu. Bấm 'Lập phiếu mới' rồi chọn đơn để nhập kho."); return; }
        if (_maDdh == null) { UI.Error("Hãy chọn một đơn đặt hàng ở danh sách phía trên."); return; }
        if (!_duDieuKien)
        { UI.Error("Đơn này chưa có kết quả kiểm tra \"Đạt\" hoặc \"Đạt có điều kiện\" nên chưa được nhập kho."); return; }
        if (cboKho.SelectedItem is not ComboItem kho) { UI.Error("Hãy chọn kho nhập."); return; }
        var nv = (cboNhanVienNK.SelectedItem as ComboItem)?.Value;

        dgvChiTietNhap.EndEdit();
        var ngayNhap = DateOnly.FromDateTime(dtpNgayNhap.Value);

        foreach (var d in _dong)
            if (d.SoLuongNhap < 0 || d.SoLuongNhap > d.SoLuongDat)
            { UI.Error($"Vật tư {d.MaVatTu}: số lượng nhập phải từ 0 đến số lượng đặt ({d.SoLuongDat:N0})."); return; }

        var rows = _dong.Where(d => d.SoLuongNhap > 0).ToList();   // dòng nhập 0 thì bỏ qua
        if (rows.Count == 0) { UI.Error("Chưa có vật tư nào có số lượng nhập lớn hơn 0."); return; }

        var hsdMap = new Dictionary<string, DateOnly?>();
        foreach (var d in rows)
        {
            DateOnly? hsd = null;
            if (!string.IsNullOrWhiteSpace(d.HanSuDung))
            {
                if (!DateOnly.TryParseExact(d.HanSuDung.Trim(), new[] { "dd/MM/yyyy", "d/M/yyyy" },
                        CultureInfo.InvariantCulture, DateTimeStyles.None, out var x))
                { UI.Error($"Vật tư {d.MaVatTu}: hạn sử dụng phải có dạng dd/MM/yyyy."); return; }
                if (x < ngayNhap)
                { UI.Error($"Vật tư {d.MaVatTu}: hạn sử dụng đã hết trước ngày nhập."); return; }
                hsd = x;
            }
            hsdMap[d.MaVatTu] = hsd;
        }

        try
        {
            var truoc = TonCuaKho(kho.Value, rows.Select(r => r.MaVatTu));
            string maPhieu;
            using (var db = new QuanLyTieuHaoVatTuContext())
            {
                maPhieu = NextMaPhieu(db);
                db.PHIEU_NHAP_KHO.Add(new PHIEU_NHAP_KHO
                {
                    MaPhieuNK = maPhieu,
                    MaDDH = _maDdh,
                    MaKho = kho.Value,
                    NgayNhap = ngayNhap,
                    MaNhanVienNK = nv,
                    GhiChu = string.IsNullOrWhiteSpace(txtGhiChu.Text) ? null : txtGhiChu.Text.Trim()
                });
                foreach (var d in rows)
                    db.CHI_TIET_NHAP_KHO.Add(new CHI_TIET_NHAP_KHO
                    {
                        MaPhieuNK = maPhieu,
                        MaVatTu = d.MaVatTu,
                        SoLuongNhap = d.SoLuongNhap,
                        DonGiaNhap = d.DonGiaNhap,
                        SoLoSX = string.IsNullOrWhiteSpace(d.SoLoSX) ? null : d.SoLoSX.Trim(),
                        HanSuDung = hsdMap[d.MaVatTu]
                    });
                db.SaveChanges();       // trigger tự cộng TON_KHO; EF tự bọc trong một transaction
            }

            var sau = TonCuaKho(kho.Value, rows.Select(r => r.MaVatTu));
            var sb = new StringBuilder($"Đã lưu phiếu {maPhieu}. Tồn kho được cập nhật tự động:\n");
            foreach (var r in rows)
                sb.AppendLine($"   {r.MaVatTu}: {truoc.GetValueOrDefault(r.MaVatTu):N0} → {sau.GetValueOrDefault(r.MaVatTu):N0}");
            UI.Info(sb.ToString());
        }
        catch (Exception ex)
        {
            UI.Error("Không lưu được phiếu nhập:\n" + (ex.InnerException?.Message ?? ex.Message));
            return;
        }

        LoadDonCho();
        LoadPhieu();
        LapPhieuMoi();
    }

    // =================== XEM / XÓA PHIẾU ĐÃ LƯU ===================
    private void XemPhieu(string ma)
    {
        if (ma == "") return;
        try
        {
            using var db = new QuanLyTieuHaoVatTuContext();
            var p = db.PHIEU_NHAP_KHO.AsNoTracking().FirstOrDefault(x => x.MaPhieuNK == ma);
            if (p == null) return;

            var ct = (from c in db.CHI_TIET_NHAP_KHO
                      join v in db.VAT_TU on c.MaVatTu equals v.MaVatTu
                      where c.MaPhieuNK == ma
                      orderby c.MaVatTu
                      select new { c.MaVatTu, v.TenVatTu, v.DonViTinh, c.SoLuongNhap, c.DonGiaNhap, c.SoLoSX, c.HanSuDung })
                     .ToList();
            var dat = db.CHI_TIET_DDH.AsNoTracking().Where(c => c.MaDDH == p.MaDDH).ToList()
                        .ToDictionary(c => UI.T(c.MaVatTu), c => c.SoLuongDat);

            _maPhieuDangXem = ma;
            _maDdh = UI.T(p.MaDDH) == "" ? null : UI.T(p.MaDDH);
            SetEditable(false);
            _dong.Clear();
            foreach (var r in ct)
                _dong.Add(new DongNhap
                {
                    MaVatTu = UI.T(r.MaVatTu),
                    TenVatTu = r.TenVatTu,
                    DonViTinh = r.DonViTinh,
                    SoLuongDat = dat.GetValueOrDefault(UI.T(r.MaVatTu), r.SoLuongNhap),
                    SoLuongNhap = r.SoLuongNhap,
                    DonGiaNhap = r.DonGiaNhap ?? 0m,
                    SoLoSX = r.SoLoSX ?? "",
                    HanSuDung = r.HanSuDung?.ToString("dd/MM/yyyy") ?? ""
                });

            txtMaPhieuNK.Text = ma;
            dtpNgayNhap.Value = p.NgayNhap.ToDateTime(TimeOnly.MinValue);
            Chon(cboKho, UI.T(p.MaKho));
            Chon(cboNhanVienNK, UI.T(p.MaNhanVienNK));
            txtGhiChu.Text = p.GhiChu ?? "";
            lblDon.Text = _maDdh ?? "(không theo đơn)";
            lblKetQuaKT.Text = "(phiếu đã lưu – chỉ xem)";
            lblKetQuaKT.ForeColor = Color.DimGray;
            CapNhatTong();
        }
        catch (Exception ex) { UI.Error("Không đọc được phiếu: " + (ex.InnerException?.Message ?? ex.Message)); }
    }

    private void XoaPhieu()
    {
        var ma = _maPhieuDangXem;
        if (ma == null) { UI.Error("Hãy chọn một phiếu trong danh sách lịch sử để xóa."); return; }
        if (!UI.Confirm($"Xóa phiếu {ma}? Tồn kho sẽ được hoàn tác tự động.")) return;

        try
        {
            using var db = new QuanLyTieuHaoVatTuContext();
            db.CHI_TIET_NHAP_KHO.RemoveRange(db.CHI_TIET_NHAP_KHO.Where(c => c.MaPhieuNK == ma).ToList());
            var p = db.PHIEU_NHAP_KHO.FirstOrDefault(x => x.MaPhieuNK == ma);
            if (p != null) db.PHIEU_NHAP_KHO.Remove(p);
            db.SaveChanges();           // trigger trừ lại tồn; không đủ tồn thì báo lỗi
        }
        catch (Exception ex)
        {
            UI.Error("Không xóa được phiếu:\n" + (ex.InnerException?.Message ?? ex.Message));
            return;
        }

        LoadDonCho();
        LoadPhieu();
        LapPhieuMoi();
    }

    // =================== IN PHIẾU ===================
    private void InPhieu()
    {
        if (_dong.Count == 0) { UI.Error("Phiếu chưa có vật tư để in."); return; }
        var sb = new StringBuilder();
        sb.AppendLine("PHIẾU NHẬP KHO");
        sb.AppendLine($"Mã phiếu : {txtMaPhieuNK.Text}      Ngày nhập: {dtpNgayNhap.Value:dd/MM/yyyy}");
        sb.AppendLine($"Đơn đặt hàng: {lblDon.Text}");
        sb.AppendLine($"Kho nhập : {cboKho.Text}");
        sb.AppendLine($"Thủ kho  : {cboNhanVienNK.Text}");
        sb.AppendLine(new string('-', 92));
        sb.AppendLine($"{"Mã",-8}{"Tên vật tư",-32}{"ĐVT",-7}{"SL nhập",9}{"Đơn giá",12}{"Số lô",-13}{"Hạn dùng",11}");
        foreach (var d in _dong.Where(x => x.SoLuongNhap > 0))
            sb.AppendLine($"{d.MaVatTu,-8}{(d.TenVatTu.Length > 30 ? d.TenVatTu[..30] : d.TenVatTu),-32}{d.DonViTinh,-7}" +
                          $"{d.SoLuongNhap,9:N0}{d.DonGiaNhap,12:N0}{d.SoLoSX,-13}{d.HanSuDung,11}");
        sb.AppendLine(new string('-', 92));
        sb.AppendLine(lblTong.Text);
        sb.AppendLine();
        sb.AppendLine("Thủ kho (ký tên)                          Người giao hàng (ký tên)");
        UI.PrintPreview(this, sb.ToString());
    }
}