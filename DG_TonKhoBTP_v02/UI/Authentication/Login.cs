using DG_TonKhoBTP_v02.Database;
using DG_TonKhoBTP_v02.Models;
using DG_TonKhoBTP_v02.Properties;
using DocumentFormat.OpenXml.Office.Word;
using System;
using CoreHelper = DG_TonKhoBTP_v02.Helper.Helper;
using System.Threading.Tasks;
using System.Windows.Forms;
using DG_TonKhoBTP_v02.UI.Helper;

namespace DG_TonKhoBTP_v02.UI.Authentication
{
    public partial class Login : Form
    {
        bool flg = false;
        public Login(bool flg = false)
        {
            InitializeComponent();
            this.flg = flg;
        }

        private async void btnLogin_Click(object sender, EventArgs e)
        {
            string username = (txtUser.Text ?? "").Trim();
            string passwordInput = (txtPassword.Text ?? "").Trim();

            if (flg)
            {
                string privateDBPath = Settings.Default.PassApp;
                if (passwordInput == privateDBPath)
                {
                    using (var dlg = new OpenFileDialog())
                    {
                        dlg.Title = "Chọn file database";
                        dlg.Multiselect = false;
                        dlg.CheckFileExists = true;
                        dlg.Filter = "Database files (*.db)|*.db|All files (*.*)|*.*";
                        if (dlg.ShowDialog(this) == DialogResult.OK)
                        {
                            Properties.Settings.Default.URL = dlg.FileName;
                            Properties.Settings.Default.Save();
                        }
                    }
                    this.Close();
                }
                return;
            }

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(passwordInput))
            {
                FrmWaiting.ShowGifAlert("Tài khoản hoặc mật khẩu đang bỏ trống");
                return;
            }

            try
            {
                var login = await WaitingHelper.RunWithWaiting(
                    () => Task.Run(() => DatabaseHelper.Login(username, txtPassword.Text)),
                    "ĐANG ĐĂNG NHẬP, VUI LÒNG ĐỢI..."
                );

                if (login == null || !login.Success)
                {
                    FrmWaiting.ShowGifAlert(login?.Message ?? "Đăng nhập thất bại.");
                    return;
                }

                UserContext.Set(login);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                FrmWaiting.ShowGifAlert($"Có lỗi xảy ra: {ex.Message}", "LỖI", EnumStore.Icon.Warning);
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            txtPassword.Text = "";
            txtUser.Text = "";
        }
    }
}
