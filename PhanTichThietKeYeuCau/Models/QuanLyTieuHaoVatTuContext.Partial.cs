using Microsoft.EntityFrameworkCore;
using PhanTichThietKeYeuCau.Models;

namespace PhanTichThietKeYeuCau.Models;

public partial class QuanLyTieuHaoVatTuContext
{
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        // Báo cho EF biết các bảng có trigger (EF Core 7+ bắt buộc)
        modelBuilder.Entity<PHIEU_NHAP_KHO>().ToTable(tb => tb.HasTrigger("trg_PhieuNhapKho_KiemTraDieuKien"));
        modelBuilder.Entity<CHI_TIET_NHAP_KHO>().ToTable(tb => tb.HasTrigger("trg_ChiTietNhapKho_CapNhatTon"));
        modelBuilder.Entity<PHIEU_CAP_PHAT>().ToTable(tb => tb.HasTrigger("trg_PhieuCapPhat_KiemTraLSX"));
        modelBuilder.Entity<CHI_TIET_CAP_PHAT>().ToTable(tb => tb.HasTrigger("trg_ChiTietCapPhat_CapNhatTon"));
        modelBuilder.Entity<DON_DAT_HANG>().ToTable(tb => tb.HasTrigger("trg_DonDatHang_KiemTraKHM"));
        modelBuilder.Entity<CHI_TIET_DDH>().ToTable(tb => tb.HasTrigger("trg_ChiTietDDH_CapNhatTongGiaTri"));
        modelBuilder.Entity<PHIEU_KIEM_TRA_CL>().ToTable(tb => tb.HasTrigger("trg_PhieuKiemTra_CapNhatDDH"));
    }
}