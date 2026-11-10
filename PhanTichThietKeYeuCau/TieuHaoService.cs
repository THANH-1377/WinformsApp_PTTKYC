using PhanTichThietKeYeuCau.Models;

namespace PhanTichThietKeYeuCau;

public record TieuHaoRow(
    string MaLSX, string TenSanPham, string TenVatTu,
    decimal DinhMucLyThuyet, int TongCapPhat, int TongThucNhan,
    int ChenhLechCapNhan, decimal ChenhLechDinhMuc, decimal TyLeVuot, string DanhGia);

public static class TieuHaoService
{
    public static List<TieuHaoRow> Tinh(QuanLyTieuHaoVatTuContext db,
        string? maLsx = null, string? maBoPhan = null, DateOnly? tu = null, DateOnly? den = null)
    {
        var q = from ct in db.CHI_TIET_CAP_PHAT
                join cp in db.PHIEU_CAP_PHAT on ct.MaPhieuCP equals cp.MaPhieuCP
                join ls in db.LENH_SAN_XUAT on cp.MaLSX equals ls.MaLSX
                join v in db.VAT_TU on ct.MaVatTu equals v.MaVatTu
                select new { ct, cp, ls, v };

        if (maLsx != null) q = q.Where(x => x.ls.MaLSX == maLsx);
        if (maBoPhan != null) q = q.Where(x => x.ls.MaBoPhan == maBoPhan);  
        if (tu != null) { var t = tu.Value; q = q.Where(x => x.cp.NgayCapPhat >= t); }
        if (den != null) { var d = den.Value; q = q.Where(x => x.cp.NgayCapPhat <= d); }

        var data = q.GroupBy(x => new
        {
            x.ls.MaLSX,
            x.ls.TenSanPham,
            x.ls.SoLuongSX,
            x.v.MaVatTu,
            x.v.TenVatTu,
            x.v.MucDinhMuc
        })
                    .Select(g => new
                    {
                        g.Key.MaLSX,
                        g.Key.TenSanPham,
                        g.Key.SoLuongSX,
                        g.Key.TenVatTu,
                        g.Key.MucDinhMuc,
                        CapPhat = g.Sum(x => x.ct.SoLuongCapPhat),
                        ThucNhan = g.Sum(x => x.ct.SoLuongThucNhan ?? 0)
                    })
                    .ToList();

        return data.Select(x =>
        {
            var dinhMuc = (x.MucDinhMuc ?? 0m) * x.SoLuongSX;     // định mức lý thuyết
            var chenh = x.ThucNhan - dinhMuc;                      // thực tế - lý thuyết
            var ty = dinhMuc > 0 ? Math.Round(chenh / dinhMuc * 100, 2) : 0m;
            return new TieuHaoRow(UI.T(x.MaLSX), x.TenSanPham, x.TenVatTu, dinhMuc,
                x.CapPhat, x.ThucNhan, x.CapPhat - x.ThucNhan, chenh, ty,
                chenh > 0 ? "Vượt định mức" : "Trong định mức");
        })
        .OrderBy(r => r.MaLSX).ThenBy(r => r.TenVatTu).ToList();
    }
}