using Microsoft.EntityFrameworkCore;
using PhanTichThietKeYeuCau;
using PhanTichThietKeYeuCau.Models;
using System.Data;

namespace PhanTichThietKeYeuCau;

public class frmBaoCao : Form
{
    private record BaoCaoInfo(string Ten, string ApDung);

    private static readonly BaoCaoInfo[] DS =
    {
        new("Danh sách vật tư theo nhóm", "Tham số: Nhóm vật tư"),
        new("Tổng giá trị đơn đặt hàng theo nhà cung cấp", "Không có tham số"),
        new("Tổng nhập kho theo kho và vật tư", "Tham số: Từ ngày – Đến ngày (ngày nhập)"),
        new("Xếp hạng vật tư theo giá trị nhập kho", "Tham số: Từ ngày – Đến ngày (ngày nhập)"),
        new("Tồn kho hiện tại kèm cảnh báo", "Không có tham số"),
        new("Vật tư sắp hết (tồn dưới 20% mức tối đa)", "Không có tham số"),
        new("Lịch sử cấp phát theo lệnh sản xuất", "Tham số: Lệnh sản xuất; Từ ngày – Đến ngày (ngày cấp)"),
        new("Tiêu hao thực tế so với định mức", "Tham số: Lệnh sản xuất; Từ ngày – Đến ngày (ngày cấp)"),
        new("Quy trình: kế hoạch → đặt hàng → kiểm tra → nhập kho", "Không có tham số"),
    };

    private readonly ComboBox cboBaoCao = UI.Combo(), cboNhom = UI.Combo(), cboLsx = UI.Combo();
    private readonly DateTimePicker dtpTu = new(), dtpDen = new();
    private readonly Label lblApDung = new() { AutoSize = true, ForeColor = Color.DimGray },
                           lblSo = new() { AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
    private DataGridView dgv = null!;

    public frmBaoCao()
    {
        Text = "THỐNG KÊ – BÁO CÁO";
        ClientSize = new Size(1180, 700);
        StartPosition = FormStartPosition.CenterParent;

        UI.Lbl(this, "Báo cáo:", 15, 12); cboBaoCao.At(this, 80, 12, 430);
        cboBaoCao.Items.AddRange(DS.Select(d => d.Ten).ToArray());
        cboBaoCao.SelectedIndexChanged += (_, _) => lblApDung.Text = cboBaoCao.SelectedIndex >= 0 ? DS[cboBaoCao.SelectedIndex].ApDung : "";
        lblApDung.At(this, 525, 17);

        UI.Lbl(this, "Nhóm VT:", 15, 50); cboNhom.At(this, 85, 50, 170);
        UI.Lbl(this, "Lệnh SX:", 275, 50); cboLsx.At(this, 340, 50, 250);
        UI.Lbl(this, "Từ ngày:", 610, 50); dtpTu.At(this, 675, 50, 120);
        UI.Lbl(this, "Đến ngày:", 810, 50); dtpDen.At(this, 885, 50, 120);
        foreach (var d in new[] { dtpTu, dtpDen }) { d.Format = DateTimePickerFormat.Short; d.ShowCheckBox = true; d.Checked = false; }

        UI.Btn(this, "Xem báo cáo", 15, 88, 130, Xem);
        UI.Btn(this, "Xuất CSV", 155, 88, 100, () =>
            UI.ExportCsv(dgv, (cboBaoCao.Text == "" ? "BaoCao" : cboBaoCao.Text.Replace(":", "")) + ".csv"));
        UI.Btn(this, "Đóng", 265, 88, 80, Close);

        dgv = UI.Grid(this, 15, 130, 1150, 520);
        dgv.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        dgv.DataBindingComplete += (_, _) => TaMau();
        lblSo.At(this, 15, 662);

        Load += (_, _) =>
        {
            try
            {
                var nhom = new List<ComboItem> { new("", "Tất cả") };
                using (var db = new QuanLyTieuHaoVatTuContext())
                    nhom.AddRange(db.VAT_TU.AsNoTracking().Select(v => v.NhomVatTu).Distinct().ToList()
                        .Where(n => !string.IsNullOrEmpty(n)).OrderBy(n => n).Select(n => new ComboItem(n!, n!)));
                UI.Bind(cboNhom, nhom);

                var lsx = new List<ComboItem> { new("", "Tất cả") };
                lsx.AddRange(Nguon.LenhSanXuat());
                UI.Bind(cboLsx, lsx);
                cboBaoCao.SelectedIndex = 0;
            }
            catch (Exception ex) { UI.Error("Lỗi khởi tạo: " + (ex.InnerException?.Message ?? ex.Message)); }
        };
    }

    private DateOnly? Tu() => dtpTu.Checked ? DateOnly.FromDateTime(dtpTu.Value) : null;
    private DateOnly? Den() => dtpDen.Checked ? DateOnly.FromDateTime(dtpDen.Value) : null;

    private static DataTable Bang(params (string Ten, Type Kieu)[] cot)
    {
        var dt = new DataTable();
        foreach (var (ten, kieu) in cot) dt.Columns.Add(ten, kieu);
        return dt;
    }
    private static readonly Type S = typeof(string), I = typeof(int), D = typeof(decimal);

    private void Xem()
    {
        if (cboBaoCao.SelectedIndex < 0) { UI.Error("Hãy chọn một báo cáo."); return; }
        try
        {
            using var db = new QuanLyTieuHaoVatTuContext();
            DataTable dt = cboBaoCao.SelectedIndex switch
            {
                0 => R0(db),
                1 => R1(db),
                2 => R2(db),
                3 => R3(db),
                4 => R4(db, false),
                5 => R4(db, true),
                6 => R6(db),
                7 => R7(db),
                _ => R8(db)
            };
            dgv.DataSource = dt;
            foreach (DataGridViewColumn c in dgv.Columns)
            {
                if (c.ValueType == typeof(int)) c.DefaultCellStyle.Format = "N0";
                else if (c.ValueType == typeof(decimal))
                    c.DefaultCellStyle.Format = c.HeaderText.Contains("giá", StringComparison.OrdinalIgnoreCase)
                                             || c.HeaderText.Contains("tiền", StringComparison.OrdinalIgnoreCase) ? "N0" : "N2";
            }
            lblSo.Text = $"Số dòng: {dt.Rows.Count}";
        }
        catch (Exception ex) { UI.Error("Không lập được báo cáo: " + (ex.InnerException?.Message ?? ex.Message)); }
    }

    private void TaMau()
    {
        if (dgv.Columns.Contains("Đánh giá"))
            foreach (DataGridViewRow r in dgv.Rows)
                r.DefaultCellStyle.ForeColor = Convert.ToString(r.Cells["Đánh giá"].Value) == "Vượt định mức" ? Color.Red : Color.DarkGreen;
        if (dgv.Columns.Contains("Tình trạng"))
            foreach (DataGridViewRow r in dgv.Rows)
                r.DefaultCellStyle.BackColor = Convert.ToString(r.Cells["Tình trạng"].Value) switch
                {
                    "Dưới mức tối thiểu" => Color.MistyRose,
                    "Vượt mức tối đa" => Color.LightYellow,
                    _ => Color.White
                };
    }

    // 0. Vật tư theo nhóm
    private DataTable R0(QuanLyTieuHaoVatTuContext db)
    {
        var nhom = (cboNhom.SelectedItem as ComboItem)?.Value ?? "";
        var dt = Bang(("Mã vật tư", S), ("Tên vật tư", S), ("ĐVT", S), ("Đơn giá mua", D), ("Định mức/SP", D), ("Nhóm", S));
        foreach (var v in db.VAT_TU.AsNoTracking().ToList().OrderBy(v => v.NhomVatTu).ThenBy(v => v.MaVatTu))
            if (nhom == "" || v.NhomVatTu == nhom)
                dt.Rows.Add(UI.T(v.MaVatTu), v.TenVatTu, v.DonViTinh, v.DonGiaMua ?? 0m, v.MucDinhMuc ?? 0m, v.NhomVatTu ?? "");
        return dt;
    }

    // 1. Tổng giá trị ĐĐH theo NCC (không tính đơn hủy)
    private DataTable R1(QuanLyTieuHaoVatTuContext db)
    {
        var don = db.DON_DAT_HANG.AsNoTracking().Where(d => d.TrangThai != "Hủy").ToList()
                    .GroupBy(d => UI.T(d.MaNCC)).ToDictionary(g => g.Key, g => g.ToList());
        var dt = Bang(("Mã NCC", S), ("Tên NCC", S), ("Đánh giá", S), ("Số đơn", I), ("Tổng giá trị", D), ("Giá trị trung bình", D));
        foreach (var n in db.NHA_CUNG_CAP.AsNoTracking().ToList()
                     .OrderByDescending(n => don.GetValueOrDefault(UI.T(n.MaNCC))?.Sum(d => d.TongGiaTri ?? 0m) ?? 0m))
        {
            var l = don.GetValueOrDefault(UI.T(n.MaNCC)) ?? new List<DON_DAT_HANG>();
            var tong = l.Sum(d => d.TongGiaTri ?? 0m);
            dt.Rows.Add(UI.T(n.MaNCC), n.TenNCC, n.DanhGia ?? "", l.Count, tong, l.Count == 0 ? 0m : tong / l.Count);
        }
        return dt;
    }

    private IEnumerable<(string TenKho, string MaVT, string TenVT, int SL, decimal GiaTri)> NhapKhoRows(QuanLyTieuHaoVatTuContext db)
    {
        var tu = Tu(); var den = Den();
        var q = (from ct in db.CHI_TIET_NHAP_KHO
                 join pn in db.PHIEU_NHAP_KHO on ct.MaPhieuNK equals pn.MaPhieuNK
                 join k in db.KHO on pn.MaKho equals k.MaKho
                 join v in db.VAT_TU on ct.MaVatTu equals v.MaVatTu
                 select new { k.TenKho, v.MaVatTu, v.TenVatTu, pn.NgayNhap, ct.SoLuongNhap, ct.DonGiaNhap }).ToList();
        return q.Where(x => (tu == null || x.NgayNhap >= tu) && (den == null || x.NgayNhap <= den))
                .Select(x => (x.TenKho, UI.T(x.MaVatTu), x.TenVatTu, x.SoLuongNhap, x.SoLuongNhap * (x.DonGiaNhap ?? 0m)));
    }

    // 2. Tổng nhập kho theo kho và vật tư
    private DataTable R2(QuanLyTieuHaoVatTuContext db)
    {
        var dt = Bang(("Kho", S), ("Tên vật tư", S), ("Tổng nhập", I), ("Giá trị nhập", D));
        foreach (var g in NhapKhoRows(db).GroupBy(x => (x.TenKho, x.TenVT)).OrderBy(g => g.Key.TenKho).ThenByDescending(g => g.Sum(x => x.SL)))
            dt.Rows.Add(g.Key.TenKho, g.Key.TenVT, g.Sum(x => x.SL), g.Sum(x => x.GiaTri));
        return dt;
    }

    // 3. Xếp hạng vật tư theo giá trị nhập
    private DataTable R3(QuanLyTieuHaoVatTuContext db)
    {
        var dt = Bang(("Xếp hạng", I), ("Mã vật tư", S), ("Tên vật tư", S), ("Tổng nhập", I), ("Giá trị nhập", D));
        var rank = 0;
        foreach (var g in NhapKhoRows(db).GroupBy(x => (x.MaVT, x.TenVT)).OrderByDescending(g => g.Sum(x => x.GiaTri)))
            dt.Rows.Add(++rank, g.Key.MaVT, g.Key.TenVT, g.Sum(x => x.SL), g.Sum(x => x.GiaTri));
        return dt;
    }

    // 4. Tồn kho kèm cảnh báo / 5. Vật tư sắp hết
    private DataTable R4(QuanLyTieuHaoVatTuContext db, bool sapHet)
    {
        var dt = Bang(("Kho", S), ("Mã VT", S), ("Tên vật tư", S), ("Nhóm", S), ("Tồn", I), ("Tối thiểu", I), ("Tối đa", I),
                      ("% so với tối đa", D), ("Tình trạng", S));
        var q = (from t in db.TON_KHO.AsNoTracking()
                 join k in db.KHO on t.MaKho equals k.MaKho
                 join v in db.VAT_TU on t.MaVatTu equals v.MaVatTu
                 select new { k.TenKho, v.MaVatTu, v.TenVatTu, v.NhomVatTu, t.SoLuongTon, v.MucTonMin, v.MucTonMax }).ToList();
        foreach (var x in q.OrderBy(x => x.TenKho).ThenBy(x => x.MaVatTu))
        {
            var min = x.MucTonMin ?? 0;
            var pct = x.MucTonMax is > 0 ? Math.Round(x.SoLuongTon * 100m / x.MucTonMax.Value, 1) : 0m;
            if (sapHet && !(x.MucTonMax is > 0 && pct < 20)) continue;
            var tt = x.SoLuongTon < min ? "Dưới mức tối thiểu"
                   : (x.MucTonMax.HasValue && x.SoLuongTon > x.MucTonMax.Value) ? "Vượt mức tối đa" : "Bình thường";
            dt.Rows.Add(x.TenKho, UI.T(x.MaVatTu), x.TenVatTu, x.NhomVatTu ?? "", x.SoLuongTon, min, x.MucTonMax ?? 0, pct, tt);
        }
        return dt;
    }

    // 6. Lịch sử cấp phát theo lệnh sản xuất
    private DataTable R6(QuanLyTieuHaoVatTuContext db)
    {
        var lsx = (cboLsx.SelectedItem as ComboItem)?.Value ?? "";
        var tu = Tu(); var den = Den();
        var dt = Bang(("Mã LSX", S), ("Sản phẩm", S), ("Bộ phận nhận", S), ("Vật tư", S),
                      ("Tổng cấp phát", I), ("Tổng thực nhận", I), ("Chênh lệch", I));
        var q = (from ct in db.CHI_TIET_CAP_PHAT
                 join cp in db.PHIEU_CAP_PHAT on ct.MaPhieuCP equals cp.MaPhieuCP
                 join ls in db.LENH_SAN_XUAT on cp.MaLSX equals ls.MaLSX
                 join bp in db.BO_PHAN on cp.MaBoPhan equals bp.MaBoPhan
                 join v in db.VAT_TU on ct.MaVatTu equals v.MaVatTu
                 select new { ls.MaLSX, ls.TenSanPham, bp.TenBoPhan, v.TenVatTu, cp.NgayCapPhat, ct.SoLuongCapPhat, ct.SoLuongThucNhan }).ToList();
        foreach (var g in q.Where(x => (lsx == "" || UI.T(x.MaLSX) == lsx) && (tu == null || x.NgayCapPhat >= tu) && (den == null || x.NgayCapPhat <= den))
                           .GroupBy(x => (Ma: UI.T(x.MaLSX), x.TenSanPham, x.TenBoPhan, x.TenVatTu))
                           .OrderBy(g => g.Key.Ma).ThenBy(g => g.Key.TenVatTu))
        {
            var cap = g.Sum(x => x.SoLuongCapPhat);
            var nhan = g.Sum(x => x.SoLuongThucNhan ?? 0);
            dt.Rows.Add(g.Key.Ma, g.Key.TenSanPham, g.Key.TenBoPhan, g.Key.TenVatTu, cap, nhan, cap - nhan);
        }
        return dt;
    }

    // 7. Tiêu hao thực tế so với định mức (dùng chung TieuHaoService với frmCapPhat)
    private DataTable R7(QuanLyTieuHaoVatTuContext db)
    {
        var lsx = (cboLsx.SelectedItem as ComboItem)?.Value ?? "";
        var dt = Bang(("Mã LSX", S), ("Sản phẩm", S), ("Vật tư", S), ("Định mức lý thuyết", D), ("Tổng cấp phát", I),
                      ("Tổng thực nhận", I), ("Chênh lệch so với định mức", D), ("Tỷ lệ vượt (%)", D), ("Đánh giá", S));
        foreach (var r in TieuHaoService.Tinh(db, lsx == "" ? null : lsx, null, Tu(), Den()))
            dt.Rows.Add(r.MaLSX, r.TenSanPham, r.TenVatTu, r.DinhMucLyThuyet, r.TongCapPhat, r.TongThucNhan,
                        r.ChenhLechDinhMuc, r.TyLeVuot, r.DanhGia);
        return dt;
    }

    // 8. Toàn bộ quy trình kế hoạch -> đặt hàng -> kiểm tra -> nhập kho
    private DataTable R8(QuanLyTieuHaoVatTuContext db)
    {
        var dt = Bang(("Mã KHM", S), ("Tên kế hoạch", S), ("TT kế hoạch", S), ("Mã ĐĐH", S), ("Nhà cung cấp", S), ("TT đơn", S),
                      ("Kết quả kiểm tra", S), ("Tỷ lệ lỗi (%)", S), ("Mã phiếu nhập", S), ("Ngày nhập", S), ("Kho nhập", S));
        var ddh = db.DON_DAT_HANG.AsNoTracking().ToList().ToLookup(d => UI.T(d.MaKHM));
        var ncc = db.NHA_CUNG_CAP.AsNoTracking().ToList().ToDictionary(n => UI.T(n.MaNCC), n => n.TenNCC);
        var kt = db.PHIEU_KIEM_TRA_CL.AsNoTracking().ToList().ToDictionary(k => UI.T(k.MaDDH), k => k);
        var nk = db.PHIEU_NHAP_KHO.AsNoTracking().ToList().Where(p => p.MaDDH != null).ToDictionary(p => UI.T(p.MaDDH), p => p);
        var kho = db.KHO.AsNoTracking().ToList().ToDictionary(k => UI.T(k.MaKho), k => k.TenKho);

        foreach (var k in db.KE_HOACH_MUA.AsNoTracking().OrderBy(x => x.MaKHM).ToList())
        {
            var ds = ddh[UI.T(k.MaKHM)].OrderBy(d => d.MaDDH).ToList();
            if (ds.Count == 0) { dt.Rows.Add(UI.T(k.MaKHM), k.TenKeHoach, k.TrangThai ?? "", "", "", "", "", "", "", "", ""); continue; }
            foreach (var d in ds)
            {
                var ma = UI.T(d.MaDDH);
                kt.TryGetValue(ma, out var t);
                nk.TryGetValue(ma, out var p);
                dt.Rows.Add(UI.T(k.MaKHM), k.TenKeHoach, k.TrangThai ?? "", ma, ncc.GetValueOrDefault(UI.T(d.MaNCC), ""),
                    d.TrangThai ?? "", t?.KetQua ?? "", t?.TiLeLoiPhatHien?.ToString("N2") ?? "",
                    p == null ? "" : UI.T(p.MaPhieuNK), p?.NgayNhap.ToString("dd/MM/yyyy") ?? "",
                    p == null ? "" : kho.GetValueOrDefault(UI.T(p.MaKho), ""));
            }
        }
        return dt;
    }
}