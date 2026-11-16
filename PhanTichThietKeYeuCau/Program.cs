using PhanTichThietKeYeuCau;
using System.Globalization;

namespace PhanTichThietKeYeuCau;

static class Program
{
    [STAThread]
    static void Main()
    {
        var vi = new CultureInfo("vi-VN");
        CultureInfo.DefaultThreadCurrentCulture = vi;
        CultureInfo.DefaultThreadCurrentUICulture = vi;
        CultureInfo.CurrentCulture = vi;

        ApplicationConfiguration.Initialize();

        try { TaiKhoanService.KhoiTao(); }
        catch (Exception ex)
        {
            MessageBox.Show("Không kết nối được cơ sở dữ liệu hoặc chưa có bảng TAI_KHOAN:\n" +
                            (ex.InnerException?.Message ?? ex.Message),
                            "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        while (true)
        {
            using (var login = new frmLogin())
                if (login.ShowDialog() != DialogResult.OK) break;      // Thoát ở màn hình đăng nhập

            var main = new frmMain();
            Application.Run(main);
            Session.Xoa();
            if (!main.DangXuat) break;                                 // đóng cửa sổ chính = thoát hẳn
        }
    }
}