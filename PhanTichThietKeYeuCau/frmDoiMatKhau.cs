using PhanTichThietKeYeuCau;

namespace PhanTichThietKeYeuCau;

public class frmDoiMatKhau : Form
{
    private readonly TextBox txtCu = new() { UseSystemPasswordChar = true },
                             txtMoi = new() { UseSystemPasswordChar = true },
                             txtNhapLai = new() { UseSystemPasswordChar = true };

    public frmDoiMatKhau()
    {
        Text = "Đổi mật khẩu";
        ClientSize = new Size(420, 235);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;

        UI.Lbl(this, "Tài khoản: " + Session.TenDangNhap, 20, 15);
        UI.Lbl(this, "Mật khẩu hiện tại:", 20, 55); txtCu.At(this, 180, 55, 210);
        UI.Lbl(this, "Mật khẩu mới:", 20, 95); txtMoi.At(this, 180, 95, 210);
        UI.Lbl(this, "Nhập lại mật khẩu mới:", 20, 135); txtNhapLai.At(this, 180, 135, 210);
        var ok = UI.Btn(this, "Đổi mật khẩu", 180, 182, 120, Doi, 34);
        var huy = UI.Btn(this, "Hủy", 310, 182, 80, () => DialogResult = DialogResult.Cancel, 34);
        AcceptButton = ok; CancelButton = huy;
    }

    private void Doi()
    {
        if (txtMoi.Text.Length < 6) { UI.Error("Mật khẩu mới phải có ít nhất 6 ký tự."); return; }
        if (txtMoi.Text != txtNhapLai.Text) { UI.Error("Hai lần nhập mật khẩu mới không khớp."); return; }
        if (txtMoi.Text == txtCu.Text) { UI.Error("Mật khẩu mới phải khác mật khẩu hiện tại."); return; }

        try
        {
            if (!TaiKhoanService.DoiMatKhau(Session.TenDangNhap, txtCu.Text, txtMoi.Text, out var loi))
            { UI.Error(loi); return; }
            UI.Info("Đổi mật khẩu thành công.");
            DialogResult = DialogResult.OK;
        }
        catch (Exception ex) { UI.Error("Không đổi được mật khẩu: " + (ex.InnerException?.Message ?? ex.Message)); }
    }
}