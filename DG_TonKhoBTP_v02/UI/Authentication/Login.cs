using DG_TonKhoBTP_v02.Database;
using DG_TonKhoBTP_v02.Models;
using DocumentFormat.OpenXml.Office.Word;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DG_TonKhoBTP_v02.UI.Authentication
{
    public partial class Login : Form
    {
        public Login()
        {
            InitializeComponent();
        }

        private async void btnLogin_Click(object sender, EventArgs e)
        {
            string username = (txtUser.Text ?? "").Trim();
            string passwordInput = (txtPassword.Text ?? "").Trim();

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(passwordInput))
            {
                FrmWaiting.ShowGifAlert("Tài khoản hoặc mật khẩu đang bỏ trống");
                return ;
            }

            var login = await WaitingHelper.RunWithWaiting(() =>
                Task.Run(() => DatabaseHelper.Login(username, txtPassword.Text)),
                "ĐANG ĐĂNG NHẬP, VUI LÒNG ĐỢI..."
            );

            if (login == null || !login.Success)
            {
                FrmWaiting.ShowGifAlert(login?.Message ?? "Đăng nhập thất bại.");
                return; // KHÔNG Close để user có thể nhập lại
            }

            // ✅ Login thành công
            UserContext.Set(login);

            this.DialogResult = DialogResult.OK; // 👈 báo thành công
            this.Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            txtPassword.Text = "";
            txtUser.Text = "";
        }
    }
}
