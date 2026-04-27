using DG_TonKhoBTP_v02.Database;
using DG_TonKhoBTP_v02.Models;
using DG_TonKhoBTP_v02.UI.Helper;
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
    public partial class FrmChangePassword : Form
    {
        public FrmChangePassword()
        {
            InitializeComponent();
        }

        private async void btnDoiMatKhau_Click(object sender, EventArgs e)
        {
            int userId = UserContext.UserId;
            string newPassword = tbMatKhauMoi.Text.Trim();

            if (!UserContext.IsAuthenticated || userId <= 0)
            {
                MessageBox.Show("Phiên đăng nhập không hợp lệ.");
                return;
            }

            if (string.IsNullOrWhiteSpace(newPassword))
            {
                MessageBox.Show("Vui lòng nhập mật khẩu mới.");
                tbMatKhauMoi.Focus();
                return;
            }

            bool result = false;

            await WaitingHelper.RunWithWaiting(() =>
            {
                result = User_DatabaseHelper.UpdatePassword(userId, newPassword);
            }, "ĐANG ĐỔI MẬT KHẨU...");

            if (result)
            {
                FrmWaiting.ShowGifAlert("Đổi mật khẩu thành công.", "THÔNG BÁO", EnumStore.Icon.Success);
                this.Close();
            }
            else
            {
                FrmWaiting.ShowGifAlert("Đổi mật khẩu thất bại.");
            }
        }

        private void FrmChangePassword_Load(object sender, EventArgs e)
        {
            lblUserName.Text = UserContext.Name;
        }
    }
}
