using Microsoft.EntityFrameworkCore;
using PhanTichThietKeYeuCau;
using PhanTichThietKeYeuCau.Models;

namespace PhanTichThietKeYeuCau;

public class frmTonKho : Form
{
    private readonly ComboBox cboKho = UI.Combo(), cboNhom = UI.Combo(), cboTinhTrang = UI.Combo();
    private readonly TextBox txtTim = new();
    private readonly Label lblThongKe = new() { AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
    private DataGridView dgv = null!;

    public frmTonKho()
    {
        Text = "TỒN KHO";
        ClientSize = new Size(1180, 680);
        StartPosition = FormStartPosition.CenterParent;

        UI.Lbl(this, "Kho:", 15, 12); cboKho.At(this, 55, 12, 190);
        UI.Lbl(this, "Nhóm:", 260, 12); cboNhom.At(this, 310, 12, 150);
        UI.Lbl(this, "Tình trạng:", 475, 12); cboTinhTrang.At(this, 555, 12, 170);
        UI.Lbl(this, "Tìm:", 740, 12); txtTim.At(this, 775, 12, 150);
        UI.Btn(this, "Xem", 935, 10, 70, LoadData);
        UI.Btn(this, "Xuất CSV", 1010, 10, 85, () => UI.ExportCsv(dgv, "TonKho.csv"));
        UI.Btn(this, "Đóng", 1100, 10, 70, Close);

        dgv = UI.Grid(this, 15, 55, 1150, 570);
        dgv.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        dgv.DataBindingComplete += (_, _) =>
        {
            foreach (DataGridViewRow r in dgv.Rows)
                r.DefaultCellStyle.BackColor = Convert.ToString(r.Cells["TinhTrang"].Value) switch
                {
                    "Dưới mức tối thiểu" => Color.MistyRose,
                    "Vượt mức tối đa" => Color.LightYellow,
                    _ => Color.White
                };
        };
        lblThongKe.At(this, 15, 640);
        Load += (_, _) =>
        {
            try { NapLoc(); LoadData(); }
            catch (Exception ex) { UI.Error("Lỗi khởi tạo: " + (ex.InnerException?.Message ?? ex.Message)); }
        };
    }

    private void NapLoc()
    {
        using var db = new QuanLyTieuHaoVatTuContext();
        var kho = new List<ComboItem> { new("", "Tất cả") };
        kho.AddRange(db.KHO.AsNoTracking().OrderBy(k => k.MaKho).ToList().Select(k => new ComboItem(UI.T(k.MaKho), k.TenKho)));
        UI.Bind(cboKho, kho);

        var nhom = new List<ComboItem> { new("", "Tất cả") };
        nhom.AddRange(db.VAT_TU.AsNoTracking().Select(v => v.NhomVatTu).Distinct().ToList()
            .Where(n => !string.IsNullOrEmpty(n)).OrderBy(n => n).Select(n => new ComboItem(n!, n!)));
        UI.Bind(cboNhom, nhom);

        UI.Bind(cboTinhTrang, new List<ComboItem>
        {
            new("", "Tất cả"), new("Dưới mức tối thiểu", "Dưới mức tối thiểu"),
            new("Bình thường", "Bình thường"), new("Vượt mức tối đa", "Vượt mức tối đa")
        });
    }

    private void LoadData()
    {
        try
        {
            var maKho = (cboKho.SelectedItem as ComboItem)?.Value ?? "";
            var nhom = (cboNhom.SelectedItem as ComboItem)?.Value ?? "";
            var tt = (cboTinhTrang.SelectedItem as ComboItem)?.Value ?? "";
            var kw = txtTim.Text.Trim();

            using var db = new QuanLyTieuHaoVatTuContext();
            var all = (from t in db.TON_KHO.AsNoTracking()
                       join k in db.KHO on t.MaKho equals k.MaKho
                       join v in db.VAT_TU.AsNoTracking() on t.MaVatTu equals v.MaVatTu
                       select new
                       {
                           k.MaKho,
                           k.TenKho,
                           v.MaVatTu,
                           v.TenVatTu,
                           v.DonViTinh,
                           v.NhomVatTu,
                           t.SoLuongTon,
                           v.MucTonMin,
                           v.MucTonMax,
                           v.DonGiaMua,
                           t.NgayCapNhat
                       })
                      .ToList()
                      .OrderBy(x => x.TenKho).ThenBy(x => x.MaVatTu);

            var rows = all.Select(x =>
            {
                var min = x.MucTonMin ?? 0;
                var tinhTrang = x.SoLuongTon < min ? "Dưới mức tối thiểu"
                              : (x.MucTonMax.HasValue && x.SoLuongTon > x.MucTonMax.Value) ? "Vượt mức tối đa" : "Bình thường";
                return new
                {
                    MaKho = UI.T(x.MaKho),
                    Kho = x.TenKho,
                    MaVatTu = UI.T(x.MaVatTu),
                    TenVatTu = x.TenVatTu,
                    DonViTinh = x.DonViTinh,
                    Nhom = x.NhomVatTu ?? "",
                    SoLuongTon = x.SoLuongTon,
                    TonToiThieu = min,
                    TonToiDa = x.MucTonMax ?? 0,
                    PhanTram = x.MucTonMax is > 0 ? Math.Round(x.SoLuongTon * 100m / x.MucTonMax.Value, 1) : 0m,
                    TinhTrang = tinhTrang,
                    CanBoSung = tinhTrang == "Dưới mức tối thiểu" ? Math.Max((x.MucTonMax ?? min * 2) - x.SoLuongTon, 0) : 0,
                    GiaTriTon = x.SoLuongTon * (x.DonGiaMua ?? 0m),
                    NgayCapNhat = x.NgayCapNhat?.ToString("dd/MM/yyyy HH:mm") ?? ""
                };
            })
            .Where(r => (maKho == "" || r.MaKho == maKho) && (nhom == "" || r.Nhom == nhom) && (tt == "" || r.TinhTrang == tt)
                     && (kw == "" || r.TenVatTu.Contains(kw, StringComparison.CurrentCultureIgnoreCase)
                                  || r.MaVatTu.Contains(kw, StringComparison.CurrentCultureIgnoreCase)))
            .ToList();

            dgv.DataSource = rows;
            UI.Headers(dgv, ("Kho", "Kho"), ("MaVatTu", "Mã VT"), ("TenVatTu", "Tên vật tư"), ("DonViTinh", "ĐVT"),
                ("Nhom", "Nhóm"), ("SoLuongTon", "Tồn"), ("TonToiThieu", "Tối thiểu"), ("TonToiDa", "Tối đa"),
                ("PhanTram", "% so với tối đa"), ("TinhTrang", "Tình trạng"), ("CanBoSung", "Cần bổ sung"),
                ("GiaTriTon", "Giá trị tồn"), ("NgayCapNhat", "Cập nhật lần cuối"));
            if (dgv.Columns.Contains("MaKho")) dgv.Columns["MaKho"]!.Visible = false;
            foreach (var c in new[] { "SoLuongTon", "TonToiThieu", "TonToiDa", "CanBoSung", "GiaTriTon" })
                if (dgv.Columns.Contains(c)) dgv.Columns[c]!.DefaultCellStyle.Format = "N0";

            lblThongKe.Text = $"Số dòng: {rows.Count}   |   Dưới mức tối thiểu: {rows.Count(r => r.TinhTrang == "Dưới mức tối thiểu")}   |   " +
                              $"Vượt mức tối đa: {rows.Count(r => r.TinhTrang == "Vượt mức tối đa")}   |   " +
                              $"Tổng giá trị tồn: {rows.Sum(r => r.GiaTriTon):N0} đ";
        }
        catch (Exception ex) { UI.Error("Không đọc được tồn kho: " + (ex.InnerException?.Message ?? ex.Message)); }
    }
}