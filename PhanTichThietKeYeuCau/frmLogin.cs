using PhanTichThietKeYeuCau;

namespace PhanTichThietKeYeuCau;

public class frmLogin : Form
{
    private readonly TextBox txtUser = new(), txtPass = new() { UseSystemPasswordChar = true };
    private readonly CheckBox chkHien = new() { Text = "Hiện mật khẩu", AutoSize = true };
    private readonly Label lblLoi = new() { AutoSize = false, ForeColor = Color.Firebrick };
    private readonly Button btnDangNhap;

    public frmLogin()
    {
        Text = "Đăng nhập";
        ClientSize = new Size(420, 330);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;

        new Label
        {
            Text = "QUẢN LÝ\nTIÊU HAO VẬT TƯ SẢN XUẤT",
            BackColor = Color.CornflowerBlue,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 13, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter,
            AutoSize = false
        }.At(this, 0, 0, 420, 90);

        UI.Lbl(this, "Tên đăng nhập:", 30, 115); txtUser.At(this, 140, 115, 240);
        UI.Lbl(this, "Mật khẩu:", 30, 155); txtPass.At(this, 140, 155, 240);
        chkHien.At(this, 140, 190);
        chkHien.CheckedChanged += (_, _) => txtPass.UseSystemPasswordChar = !chkHien.Checked;
        lblLoi.At(this, 30, 222, 360, 40);

        btnDangNhap = UI.Btn(this, "Đăng nhập", 140, 275, 110, DangNhap, 36);
        var btnThoat = UI.Btn(this, "Thoát", 270, 275, 110, () => DialogResult = DialogResult.Cancel, 36);
        AcceptButton = btnDangNhap;      // Enter = đăng nhập
        CancelButton = btnThoat;         // Esc = thoát
        Shown += (_, _) => txtUser.Focus();
    }

    private void DangNhap()
    {
        lblLoi.Text = "";
        if (txtUser.Text.Trim() == "" || txtPass.Text == "")
        { lblLoi.Text = "Hãy nhập tên đăng nhập và mật khẩu."; return; }

        try
        {
            btnDangNhap.Enabled = false;
            Cursor = Cursors.WaitCursor;
            var tk = TaiKhoanService.DangNhap(txtUser.Text, txtPass.Text, out var loi);
            if (tk == null) { lblLoi.Text = loi; txtPass.Clear(); txtPass.Focus(); return; }
            Session.Bat(tk);
            DialogResult = DialogResult.OK;
        }
        catch (Exception ex) { lblLoi.Text = "Lỗi kết nối: " + (ex.InnerException?.Message ?? ex.Message); }
        finally { btnDangNhap.Enabled = true; Cursor = Cursors.Default; }
    }
}