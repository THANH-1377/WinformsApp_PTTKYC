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
        Application.Run(new frmMain());
    }
}