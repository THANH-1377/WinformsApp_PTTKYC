using Microsoft.EntityFrameworkCore;
using PhanTichThietKeYeuCau;
using PhanTichThietKeYeuCau.Models;
using System.ComponentModel;
using System.Drawing.Printing;
using System.Text;

namespace PhanTichThietKeYeuCau;

public class DongCapPhat
{
    public string MaVatTu { get; set; } = "";
    public string TenVatTu { get; set; } = "";
    public string DonViTinh { get; set; } = "";
    public int SoLuongCapPhat { get; set; }
    public int SoLuongThucNhan { get; set; }
    public string GhiChu { get; set; } = "";
}

public partial class frmCapPhat : Form
{
    // ----- Tab 1: lập phiếu -----
    private readonly TabControl tabs = new();
    private readonly TextBox txtMaPhieuCP = new(), txtMucDich = new(), txtGhiChuCT = new();
    private readonly DateTimePicker dtpNgayCapPhat = new(), dtpTuNgay = new(), dtpDenNgay = new();
    private readonly ComboBox cboLenhSanXuat = UI.Combo(), cboKho = UI.Combo(), cboBoPhan = UI.Combo(),
                              cboNhanVienXK = UI.Combo(), cboVatTu = UI.Combo(),
                              cboLocLSX = UI.Combo(), cboLocBoPhan = UI.Combo();
    private readonly Label lblTenSanPham = new() { AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) },
                           lblSoLuongSX = new() { AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) },
                           lblTonKho = new() { AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) },
                           lblDinhMuc = new() { AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) },
                           lblTongVuot = new() { AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) },
                           lblTongChenhLech = new() { AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) },
                           lblTyLeTB = new() { AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
    private readonly NumericUpDown numSoLuongCapPhat = UI.Num(), numSoLuongThucNhan = UI.Num();
    private DataGridView dgvChiTietCapPhat = null!, dgvPhieuCapPhat = null!, dgvTieuHao = null!;

    private readonly BindingList<DongCapPhat> _dong = new();
    private List<LENH_SAN_XUAT> _lsx = new();
    private List<VAT_TU> _vatTu = new();
    private string? _maPhieuDangXem;   // null = đang lập phiếu mới; có giá trị = đang xem phiếu đã lưu
    private int _tonHienTai;

    public frmCapPhat()
    {
        InitializeComponent();
        BuildUi();
        Load += (_, _) =>
        {
            try
            {
                NapDanhMuc();
                GanSuKien();
                OnLsxChanged();
                LapPhieuMoi();
                LoadPhieu();
                XemTieuHao();
            }
            catch (Exception ex) { UI.Error("Lỗi khởi tạo: " + (ex.InnerException?.Message ?? ex.Message)); }
        };
    }

    // =================== DỰNG GIAO DIỆN ===================
    private void BuildUi()
    {
        Text = "CẤP PHÁT VÀ THEO DÕI TIÊU HAO VẬT TƯ";
        ClientSize = new Size(1090, 740);
        StartPosition = FormStartPosition.CenterParent;

        tabs.At(this, 5, 5, 1080, 730);
        var tpLap = new TabPage("Lập phiếu cấp phát");
        var tpTheoDoi = new TabPage("Theo dõi tiêu hao");
        tabs.TabPages.Add(tpLap);
        tabs.TabPages.Add(tpTheoDoi);
        BuildTabLap(tpLap);
        BuildTabTheoDoi(tpTheoDoi);
    }

    private void BuildTabLap(TabPage p)
    {
        var gb1 = new GroupBox { Text = "THÔNG TIN PHIẾU" }.At(p, 10, 8, 1050, 160);
        UI.Lbl(gb1, "Mã phiếu:", 15, 22); txtMaPhieuCP.At(gb1, 100, 22, 140); txtMaPhieuCP.ReadOnly = true;
        UI.Lbl(gb1, "Ngày cấp phát:", 270, 22); dtpNgayCapPhat.At(gb1, 375, 22, 140);
        dtpNgayCapPhat.Format = DateTimePickerFormat.Short;
        UI.Lbl(gb1, "Lệnh sản xuất:", 540, 22); cboLenhSanXuat.At(gb1, 640, 22, 380);
        UI.Lbl(gb1, "Sản phẩm:", 15, 57); lblTenSanPham.At(gb1, 100, 61);
        UI.Lbl(gb1, "SL sản xuất:", 540, 57); lblSoLuongSX.At(gb1, 640, 61);
        UI.Lbl(gb1, "Kho xuất:", 15, 92); cboKho.At(gb1, 100, 92, 220);
        UI.Lbl(gb1, "Bộ phận nhận:", 340, 92); cboBoPhan.At(gb1, 440, 92, 190);
        UI.Lbl(gb1, "Thủ kho xuất:", 650, 92); cboNhanVienXK.At(gb1, 745, 92, 275);
        UI.Lbl(gb1, "Mục đích:", 15, 125); txtMucDich.At(gb1, 100, 125, 600); txtMucDich.MaxLength = 200;

        var gb2 = new GroupBox { Text = "CHI TIẾT CẤP PHÁT" }.At(p, 10, 175, 1050, 265);
        UI.Lbl(gb2, "Vật tư:", 15, 22); cboVatTu.At(gb2, 75, 22, 330);
        UI.Lbl(gb2, "Tồn kho:", 425, 22); lblTonKho.At(gb2, 490, 26);
        UI.Lbl(gb2, "Định mức LSX:", 600, 22); lblDinhMuc.At(gb2, 700, 26);
        UI.Lbl(gb2, "SL cấp phát:", 15, 57); numSoLuongCapPhat.At(gb2, 105, 57, 110);
        UI.Lbl(gb2, "SL thực nhận:", 240, 57); numSoLuongThucNhan.At(gb2, 335, 57, 110);
        UI.Lbl(gb2, "Ghi chú:", 470, 57); txtGhiChuCT.At(gb2, 530, 57, 250); txtGhiChuCT.MaxLength = 200;
        UI.Btn(gb2, "Thêm vật tư", 800, 53, 110, ThemVatTu);
        UI.Btn(gb2, "Xóa dòng", 920, 53, 110, XoaDong);
        dgvChiTietCapPhat = UI.Grid(gb2, 10, 95, 1030, 160);
        dgvChiTietCapPhat.DataSource = _dong;
        UI.Headers(dgvChiTietCapPhat, ("MaVatTu", "Mã vật tư"), ("TenVatTu", "Tên vật tư"), ("DonViTinh", "ĐVT"),
            ("SoLuongCapPhat", "SL cấp phát"), ("SoLuongThucNhan", "SL thực nhận"), ("GhiChu", "Ghi chú"));

        UI.Btn(p, "Lập phiếu mới", 10, 450, 120, () => { LapPhieuMoi(); CapNhatTonVaDinhMuc(); });
        UI.Btn(p, "Lưu phiếu", 140, 450, 110, LuuPhieu);
        UI.Btn(p, "Xác nhận thực nhận", 260, 450, 160, XacNhanThucNhan);
        UI.Btn(p, "In phiếu", 430, 450, 100, InPhieu);
        UI.Btn(p, "Đóng", 540, 450, 90, Close);

        new Label
        {
            Text = "DANH SÁCH PHIẾU ĐÃ LẬP (nhấp vào một dòng để xem chi tiết / xác nhận thực nhận)",
            AutoSize = true,
            Font = new Font("Segoe UI", 9, FontStyle.Bold)
        }.At(p, 10, 495);
        dgvPhieuCapPhat = UI.Grid(p, 10, 520, 1050, 170);
    }

    private void BuildTabTheoDoi(TabPage p)
    {
        UI.Lbl(p, "Lệnh SX:", 10, 15); cboLocLSX.At(p, 75, 15, 230);
        UI.Lbl(p, "Bộ phận:", 320, 15); cboLocBoPhan.At(p, 385, 15, 180);
        UI.Lbl(p, "Từ ngày:", 580, 15); dtpTuNgay.At(p, 645, 15, 130);
        UI.Lbl(p, "Đến ngày:", 790, 15); dtpDenNgay.At(p, 860, 15, 130);
        foreach (var d in new[] { dtpTuNgay, dtpDenNgay })
        { d.Format = DateTimePickerFormat.Short; d.ShowCheckBox = true; d.Checked = false; }

        UI.Btn(p, "Xem tiêu hao", 10, 50, 120, XemTieuHao);
        UI.Btn(p, "Xuất CSV", 140, 50, 100, () => UI.ExportCsv(dgvTieuHao, "TieuHaoVatTu.csv"));
        UI.Btn(p, "Làm mới", 250, 50, 100, () =>
        {
            cboLocLSX.SelectedIndex = 0; cboLocBoPhan.SelectedIndex = 0;
            dtpTuNgay.Checked = false; dtpDenNgay.Checked = false;
            XemTieuHao();
        });

        dgvTieuHao = UI.Grid(p, 10, 95, 1050, 520);
        lblTongVuot.At(p, 10, 630);
        lblTongChenhLech.At(p, 330, 630);
        lblTyLeTB.At(p, 720, 630);
    }

    // =================== NẠP DỮ LIỆU & SỰ KIỆN ===================
    private void NapDanhMuc()
    {
        using var db = new QuanLyTieuHaoVatTuContext();
        _lsx = db.LENH_SAN_XUAT.AsNoTracking().OrderBy(l => l.MaLSX).ToList();
        _vatTu = db.VAT_TU.AsNoTracking().OrderBy(v => v.MaVatTu).ToList();
        var khos = db.KHO.AsNoTracking().OrderBy(k => k.MaKho).ToList();
        var bps = db.BO_PHAN.AsNoTracking().OrderBy(b => b.MaBoPhan).ToList();

        var nvs = db.NHAN_VIEN.AsNoTracking().Where(n => n.ChucVu == "Thủ kho").OrderBy(n => n.MaNhanVien).ToList();
        if (nvs.Count == 0) nvs = db.NHAN_VIEN.AsNoTracking().OrderBy(n => n.MaNhanVien).ToList();

        // Mỗi ComboBox một List riêng để không dùng chung vị trí chọn
        UI.Bind(cboLenhSanXuat, _lsx.Where(l => l.TrangThai == "Đang SX")
            .Select(l => new ComboItem(UI.T(l.MaLSX), $"{UI.T(l.MaLSX)} - {l.TenSanPham}")).ToList());
        UI.Bind(cboKho, khos.Select(k => new ComboItem(UI.T(k.MaKho), k.TenKho)).ToList());
        UI.Bind(cboBoPhan, bps.Select(b => new ComboItem(UI.T(b.MaBoPhan), b.TenBoPhan)).ToList());
        UI.Bind(cboNhanVienXK, nvs.Select(n => new ComboItem(UI.T(n.MaNhanVien), n.HoTen)).ToList());
        UI.Bind(cboVatTu, _vatTu.Select(v => new ComboItem(UI.T(v.MaVatTu), $"{UI.T(v.MaVatTu)} - {v.TenVatTu}")).ToList());

        var locLsx = new List<ComboItem> { new("", "Tất cả") };
        locLsx.AddRange(_lsx.Select(l => new ComboItem(UI.T(l.MaLSX), $"{UI.T(l.MaLSX)} - {l.TenSanPham}")));
        UI.Bind(cboLocLSX, locLsx);

        var locBp = new List<ComboItem> { new("", "Tất cả") };
        locBp.AddRange(bps.Select(b => new ComboItem(UI.T(b.MaBoPhan), b.TenBoPhan)));
        UI.Bind(cboLocBoPhan, locBp);
    }

    private void GanSuKien()
    {
        cboLenhSanXuat.SelectedIndexChanged += (_, _) => OnLsxChanged();
        cboKho.SelectedIndexChanged += (_, _) => CapNhatTonVaDinhMuc();
        cboVatTu.SelectedIndexChanged += (_, _) => CapNhatTonVaDinhMuc();
        numSoLuongCapPhat.ValueChanged += (_, _) =>
        {
            if (_maPhieuDangXem == null) numSoLuongThucNhan.Value = numSoLuongCapPhat.Value;
        };
        dgvPhieuCapPhat.CellClick += dgvPhieuCapPhat_CellClick;
        dgvChiTietCapPhat.CellClick += dgvChiTiet_CellClick;
        dgvTieuHao.DataBindingComplete += dgvTieuHao_DataBindingComplete;
        tabs.SelectedIndexChanged += (_, _) => { if (tabs.SelectedIndex == 1) XemTieuHao(); };
    }

    private static void Chon(ComboBox c, string value)
    {
        if (c.DataSource != null && value != "") c.SelectedValue = value;
    }

    private void OnLsxChanged()
    {
        if (cboLenhSanXuat.SelectedItem is not ComboItem it) { lblTenSanPham.Text = lblSoLuongSX.Text = ""; return; }
        var ls = _lsx.First(l => UI.T(l.MaLSX) == it.Value);
        lblTenSanPham.Text = ls.TenSanPham;
        lblSoLuongSX.Text = ls.SoLuongSX.ToString("N0");
        if (_maPhieuDangXem == null) Chon(cboBoPhan, UI.T(ls.MaBoPhan));   // mặc định bộ phận thực hiện LSX
        CapNhatTonVaDinhMuc();
    }

    private void CapNhatTonVaDinhMuc()
    {
        _tonHienTai = 0;
        lblTonKho.Text = "0"; lblDinhMuc.Text = "0";
        if (cboKho.SelectedItem is not ComboItem k || cboVatTu.SelectedItem is not ComboItem v) return;

        using var db = new QuanLyTieuHaoVatTuContext();
        var tk = db.TON_KHO.AsNoTracking().FirstOrDefault(t => t.MaKho == k.Value && t.MaVatTu == v.Value);
        _tonHienTai = tk?.SoLuongTon ?? 0;
        lblTonKho.Text = _tonHienTai.ToString("N0");

        if (cboLenhSanXuat.SelectedItem is ComboItem l)
        {
            var ls = _lsx.First(x => UI.T(x.MaLSX) == l.Value);
            var vt = _vatTu.First(x => UI.T(x.MaVatTu) == v.Value);
            lblDinhMuc.Text = ((vt.MucDinhMuc ?? 0m) * ls.SoLuongSX).ToString("N2");
        }
    }

    // =================== LẬP PHIẾU ===================
    private static string NextMaPhieu(QuanLyTieuHaoVatTuContext db)
    {
        var max = db.PHIEU_CAP_PHAT.Select(p => p.MaPhieuCP).ToList()
            .Select(c => int.TryParse(UI.T(c).Replace("CP", ""), out var n) ? n : 0)
            .DefaultIfEmpty(0).Max();
        return "CP" + (max + 1).ToString("000");
    }

    private void LapPhieuMoi()
    {
        _maPhieuDangXem = null;
        _dong.Clear();
        using var db = new QuanLyTieuHaoVatTuContext();
        txtMaPhieuCP.Text = NextMaPhieu(db);
        dtpNgayCapPhat.Value = DateTime.Today;
        txtMucDich.Clear(); txtGhiChuCT.Clear();
        numSoLuongCapPhat.Value = 0; numSoLuongThucNhan.Value = 0;
    }

    private int DaCapTruocDo(string maLsx, string maVatTu)
    {
        using var db = new QuanLyTieuHaoVatTuContext();
        return (from ct in db.CHI_TIET_CAP_PHAT
                join cp in db.PHIEU_CAP_PHAT on ct.MaPhieuCP equals cp.MaPhieuCP
                where cp.MaLSX == maLsx && ct.MaVatTu == maVatTu
                select ct.SoLuongThucNhan ?? 0).Sum();
    }

    private void ThemVatTu()
    {
        if (_maPhieuDangXem != null)
        { UI.Error("Đang xem phiếu đã lưu. Bấm 'Lập phiếu mới' để tạo phiếu khác."); return; }
        if (cboLenhSanXuat.SelectedItem is not ComboItem l) { UI.Error("Hãy chọn lệnh sản xuất."); return; }
        if (cboKho.SelectedItem is not ComboItem) { UI.Error("Hãy chọn kho xuất."); return; }
        if (cboVatTu.SelectedItem is not ComboItem v) { UI.Error("Hãy chọn vật tư."); return; }

        var sl = (int)numSoLuongCapPhat.Value;
        var tn = (int)numSoLuongThucNhan.Value;
        if (sl <= 0) { UI.Error("Số lượng cấp phát phải lớn hơn 0."); return; }
        if (tn > sl) { UI.Error("Số lượng thực nhận không được lớn hơn số lượng cấp phát."); return; }
        if (sl > _tonHienTai)
        { UI.Error($"Số lượng cấp phát ({sl:N0}) vượt tồn kho hiện tại ({_tonHienTai:N0})."); return; }
        if (_dong.Any(d => d.MaVatTu == v.Value)) { UI.Error("Vật tư này đã có trong phiếu."); return; }

        // Cảnh báo nếu tổng thực nhận vượt định mức lý thuyết của LSX
        var ls = _lsx.First(x => UI.T(x.MaLSX) == l.Value);
        var vt = _vatTu.First(x => UI.T(x.MaVatTu) == v.Value);
        var dinhMuc = (vt.MucDinhMuc ?? 0m) * ls.SoLuongSX;
        var tong = DaCapTruocDo(l.Value, v.Value) + tn;
        if (dinhMuc > 0 && tong > dinhMuc &&
            !UI.Confirm($"Tổng thực nhận của {vt.TenVatTu} cho {l.Value} sẽ là {tong:N0}, vượt định mức lý thuyết {dinhMuc:N0}.\nVẫn thêm vào phiếu?"))
            return;

        _dong.Add(new DongCapPhat
        {
            MaVatTu = v.Value,
            TenVatTu = vt.TenVatTu,
            DonViTinh = vt.DonViTinh,
            SoLuongCapPhat = sl,
            SoLuongThucNhan = tn,
            GhiChu = txtGhiChuCT.Text.Trim()
        });
        txtGhiChuCT.Clear();
    }

    private void XoaDong()
    {
        if (_maPhieuDangXem != null) { UI.Error("Không xóa dòng của phiếu đã lưu."); return; }
        if (dgvChiTietCapPhat.CurrentRow?.DataBoundItem is DongCapPhat d) _dong.Remove(d);
    }

    private void LuuPhieu()
    {
        if (_maPhieuDangXem != null) { UI.Error("Phiếu này đã được lưu. Bấm 'Lập phiếu mới' để tạo phiếu khác."); return; }
        if (_dong.Count == 0) { UI.Error("Phiếu chưa có vật tư nào."); return; }
        if (cboLenhSanXuat.SelectedItem is not ComboItem ls ||
            cboKho.SelectedItem is not ComboItem kho ||
            cboBoPhan.SelectedItem is not ComboItem bp)
        { UI.Error("Hãy chọn đủ lệnh sản xuất, kho xuất và bộ phận nhận."); return; }
        var nv = (cboNhanVienXK.SelectedItem as ComboItem)?.Value;

        using var db = new QuanLyTieuHaoVatTuContext();
        using var tran = db.Database.BeginTransaction();
        try
        {
            var maPhieu = NextMaPhieu(db);
            db.PHIEU_CAP_PHAT.Add(new PHIEU_CAP_PHAT
            {
                MaPhieuCP = maPhieu,
                MaLSX = ls.Value,
                MaKho = kho.Value,
                MaBoPhan = bp.Value,
                NgayCapPhat = DateOnly.FromDateTime(dtpNgayCapPhat.Value),
                MaNhanVienXK = nv,
                MucDich = string.IsNullOrWhiteSpace(txtMucDich.Text) ? null : txtMucDich.Text.Trim()
            });

            foreach (var d in _dong)
            {
                db.CHI_TIET_CAP_PHAT.Add(new CHI_TIET_CAP_PHAT
                {
                    MaPhieuCP = maPhieu,
                    MaVatTu = d.MaVatTu,
                    SoLuongCapPhat = d.SoLuongCapPhat,
                    SoLuongThucNhan = d.SoLuongThucNhan,
                    GhiChu = string.IsNullOrWhiteSpace(d.GhiChu) ? null : d.GhiChu
                });
            }

            db.SaveChanges();      // trigger trg_ChiTietCapPhat_CapNhatTon tự trừ TON_KHO
            tran.Commit();
            UI.Info($"Đã lưu phiếu {maPhieu}. Tồn kho đã được cập nhật tự động.");
        }
        catch (Exception ex)
        {
            // Không cần gọi Rollback: thoát khỏi 'using var tran' mà chưa Commit thì EF tự hoàn tác
            UI.Error("Lưu phiếu thất bại, dữ liệu đã được hoàn tác:\n" + (ex.InnerException?.Message ?? ex.Message));
            return;
        }

        LoadPhieu();
        LapPhieuMoi();
        CapNhatTonVaDinhMuc();
    }

    // =================== XEM PHIẾU & XÁC NHẬN THỰC NHẬN ===================
    private void LoadPhieu()
    {
        using var db = new QuanLyTieuHaoVatTuContext();
        var raw = (from p in db.PHIEU_CAP_PHAT
                   join ls in db.LENH_SAN_XUAT on p.MaLSX equals ls.MaLSX
                   join k in db.KHO on p.MaKho equals k.MaKho
                   join bp in db.BO_PHAN on p.MaBoPhan equals bp.MaBoPhan
                   join nv0 in db.NHAN_VIEN on p.MaNhanVienXK equals nv0.MaNhanVien into g
                   from nv in g.DefaultIfEmpty()
                   orderby p.MaPhieuCP descending
                   select new
                   {
                       p.MaPhieuCP,
                       p.MaLSX,
                       ls.TenSanPham,
                       k.TenKho,
                       bp.TenBoPhan,
                       p.NgayCapPhat,
                       NguoiXuat = nv.HoTen,
                       p.MucDich
                   }).ToList();

        dgvPhieuCapPhat.DataSource = raw.Select(x => new
        {
            MaPhieuCP = UI.T(x.MaPhieuCP),
            MaLSX = UI.T(x.MaLSX),
            x.TenSanPham,
            x.TenKho,
            x.TenBoPhan,
            NgayCapPhat = x.NgayCapPhat.ToString("dd/MM/yyyy"),
            NguoiXuat = x.NguoiXuat ?? "",
            MucDich = x.MucDich ?? ""
        }).ToList();

        UI.Headers(dgvPhieuCapPhat, ("MaPhieuCP", "Mã phiếu"), ("MaLSX", "Mã LSX"), ("TenSanPham", "Sản phẩm"),
            ("TenKho", "Kho xuất"), ("TenBoPhan", "Bộ phận nhận"), ("NgayCapPhat", "Ngày cấp"),
            ("NguoiXuat", "Người xuất"), ("MucDich", "Mục đích"));
    }

    private void dgvPhieuCapPhat_CellClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0) return;
        var ma = UI.T(Convert.ToString(dgvPhieuCapPhat.Rows[e.RowIndex].Cells["MaPhieuCP"].Value));

        using var db = new QuanLyTieuHaoVatTuContext();
        var p = db.PHIEU_CAP_PHAT.AsNoTracking().FirstOrDefault(x => x.MaPhieuCP == ma);
        if (p == null) return;

        var ct = (from c in db.CHI_TIET_CAP_PHAT
                  join v in db.VAT_TU on c.MaVatTu equals v.MaVatTu
                  where c.MaPhieuCP == ma
                  select new { c.MaVatTu, v.TenVatTu, v.DonViTinh, c.SoLuongCapPhat, c.SoLuongThucNhan, c.GhiChu }).ToList();

        _maPhieuDangXem = ma;
        _dong.Clear();
        foreach (var r in ct)
            _dong.Add(new DongCapPhat
            {
                MaVatTu = UI.T(r.MaVatTu),
                TenVatTu = r.TenVatTu,
                DonViTinh = r.DonViTinh,
                SoLuongCapPhat = r.SoLuongCapPhat,
                SoLuongThucNhan = r.SoLuongThucNhan ?? 0,
                GhiChu = r.GhiChu ?? ""
            });

        txtMaPhieuCP.Text = ma;
        dtpNgayCapPhat.Value = p.NgayCapPhat.ToDateTime(TimeOnly.MinValue);
        txtMucDich.Text = p.MucDich ?? "";
        Chon(cboLenhSanXuat, UI.T(p.MaLSX));
        Chon(cboKho, UI.T(p.MaKho));
        Chon(cboBoPhan, UI.T(p.MaBoPhan));
        Chon(cboNhanVienXK, UI.T(p.MaNhanVienXK));
    }

    private void dgvChiTiet_CellClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || dgvChiTietCapPhat.Rows[e.RowIndex].DataBoundItem is not DongCapPhat d) return;
        Chon(cboVatTu, d.MaVatTu);
        numSoLuongCapPhat.Value = d.SoLuongCapPhat;
        numSoLuongThucNhan.Value = d.SoLuongThucNhan;
        txtGhiChuCT.Text = d.GhiChu;
    }

    private void XacNhanThucNhan()
    {
        if (_maPhieuDangXem == null)
        { UI.Error("Chọn một phiếu trong danh sách bên dưới để xác nhận thực nhận."); return; }
        if (dgvChiTietCapPhat.CurrentRow?.DataBoundItem is not DongCapPhat d)
        { UI.Error("Chọn dòng vật tư cần xác nhận."); return; }

        var tn = (int)numSoLuongThucNhan.Value;
        if (tn > d.SoLuongCapPhat)
        { UI.Error("Số lượng thực nhận không được lớn hơn số lượng cấp phát."); return; }

        try
        {
            using var db = new QuanLyTieuHaoVatTuContext();
            var ct = db.CHI_TIET_CAP_PHAT.FirstOrDefault(c => c.MaPhieuCP == _maPhieuDangXem && c.MaVatTu == d.MaVatTu);
            if (ct == null) { UI.Error("Không tìm thấy dòng chi tiết."); return; }

            ct.SoLuongThucNhan = tn;
            ct.GhiChu = string.IsNullOrWhiteSpace(txtGhiChuCT.Text) ? null : txtGhiChuCT.Text.Trim();
            db.SaveChanges();

            d.SoLuongThucNhan = tn;
            d.GhiChu = txtGhiChuCT.Text.Trim();
            _dong.ResetBindings();
            UI.Info("Đã xác nhận số lượng thực nhận.");
        }
        catch (Exception ex) { UI.Error("Không lưu được: " + (ex.InnerException?.Message ?? ex.Message)); }
    }

    // =================== IN PHIẾU ===================
    private void InPhieu()
    {
        if (_dong.Count == 0) { UI.Error("Phiếu chưa có vật tư để in."); return; }

        var sb = new StringBuilder();
        sb.AppendLine("PHIẾU CẤP PHÁT VẬT TƯ");
        sb.AppendLine($"Mã phiếu : {txtMaPhieuCP.Text}      Ngày: {dtpNgayCapPhat.Value:dd/MM/yyyy}");
        sb.AppendLine($"Lệnh SX  : {cboLenhSanXuat.Text}");
        sb.AppendLine($"Kho xuất : {cboKho.Text}");
        sb.AppendLine($"Bộ phận nhận: {cboBoPhan.Text}");
        sb.AppendLine($"Mục đích : {txtMucDich.Text}");
        sb.AppendLine(new string('-', 78));
        sb.AppendLine($"{"Mã",-8}{"Tên vật tư",-34}{"ĐVT",-8}{"Cấp phát",10}{"Thực nhận",12}");
        foreach (var d in _dong)
            sb.AppendLine($"{d.MaVatTu,-8}{(d.TenVatTu.Length > 32 ? d.TenVatTu[..32] : d.TenVatTu),-34}{d.DonViTinh,-8}{d.SoLuongCapPhat,10:N0}{d.SoLuongThucNhan,12:N0}");
        sb.AppendLine(new string('-', 78));
        sb.AppendLine();
        sb.AppendLine("Thủ kho (ký tên)                          Bộ phận nhận (ký tên)");

        var lines = sb.ToString().Split(Environment.NewLine);
        using var pd = new PrintDocument();
        pd.PrintPage += (_, e) =>
        {
            float y = 50;
            using var f = new Font("Consolas", 10);
            foreach (var line in lines) { e.Graphics!.DrawString(line, f, Brushes.Black, 50, y); y += 20; }
        };
        using var dlg = new PrintPreviewDialog { Document = pd, Width = 900, Height = 700 };
        dlg.ShowDialog(this);
    }

    // =================== TAB THEO DÕI TIÊU HAO ===================
    private void XemTieuHao()
    {
        try
        {
            var maLsx = (cboLocLSX.SelectedItem as ComboItem)?.Value;
            var maBp = (cboLocBoPhan.SelectedItem as ComboItem)?.Value;
            if (string.IsNullOrEmpty(maLsx)) maLsx = null;
            if (string.IsNullOrEmpty(maBp)) maBp = null;
            DateOnly? tu = dtpTuNgay.Checked ? DateOnly.FromDateTime(dtpTuNgay.Value) : null;
            DateOnly? den = dtpDenNgay.Checked ? DateOnly.FromDateTime(dtpDenNgay.Value) : null;

            using var db = new QuanLyTieuHaoVatTuContext();
            var ds = TieuHaoService.Tinh(db, maLsx, maBp, tu, den);
            dgvTieuHao.DataSource = ds;

            UI.Headers(dgvTieuHao, ("MaLSX", "Mã LSX"), ("TenSanPham", "Sản phẩm"), ("TenVatTu", "Vật tư"),
                ("DinhMucLyThuyet", "Định mức lý thuyết"), ("TongCapPhat", "Tổng cấp phát"),
                ("TongThucNhan", "Tổng thực nhận"), ("ChenhLechCapNhan", "Chênh lệch cấp phát – thực nhận"),
                ("ChenhLechDinhMuc", "Chênh lệch so với định mức"), ("TyLeVuot", "Tỷ lệ vượt (%)"),
                ("DanhGia", "Đánh giá"));

            lblTongVuot.Text = "Số dòng vượt định mức: " + ds.Count(x => x.DanhGia == "Vượt định mức");
            lblTongChenhLech.Text = "Tổng chênh lệch cấp phát – thực nhận: " + ds.Sum(x => x.ChenhLechCapNhan).ToString("N0");
            lblTyLeTB.Text = "Tỷ lệ vượt định mức trung bình: " +
                             (ds.Count == 0 ? 0m : ds.Average(x => x.TyLeVuot)).ToString("N2") + "%";
        }
        catch (Exception ex) { UI.Error("Không tính được tiêu hao: " + (ex.InnerException?.Message ?? ex.Message)); }
    }

    private void dgvTieuHao_DataBindingComplete(object? sender, DataGridViewBindingCompleteEventArgs e)
    {
        foreach (var c in new[] { "DinhMucLyThuyet", "ChenhLechDinhMuc", "TyLeVuot" })
            if (dgvTieuHao.Columns.Contains(c)) dgvTieuHao.Columns[c]!.DefaultCellStyle.Format = "N2";

        foreach (DataGridViewRow r in dgvTieuHao.Rows)
            r.DefaultCellStyle.ForeColor =
                Convert.ToString(r.Cells["DanhGia"].Value) == "Vượt định mức" ? Color.Red : Color.DarkGreen;
    }
}