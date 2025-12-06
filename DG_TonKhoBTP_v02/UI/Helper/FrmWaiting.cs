using DocumentFormat.OpenXml.Drawing.Charts;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DG_TonKhoBTP_v02.UI
{
    public partial class FrmWaiting : Form
    {
        public FrmWaiting()
            : this("Đang xử lý, vui lòng đợi...", null, "Đang xử lý")
        {
        }

        public static FrmWaiting CreateGifAlert(
            string message,
            string gifPath,
            string title = "THÔNG BÁO",
            Icon icon = null)
        {
            var form = new FrmWaiting(message, icon, title, gifPath);

            form.ControlBox = true;     // Bật nút X
            form.MinimizeBox = false;
            form.MaximizeBox = false;
            form.TopMost = true;

            return form;
        }

        public FrmWaiting(string message, Icon icon = null, string title = "Thông báo", string gifPath = null)
        {
            InitializeComponent();

            // Cấu hình label
            lblMessage.AutoSize = true;
            //lblMessage.MaximumSize = new System.Drawing.Size(288, 0);
            lblMessage.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            lblMessage.Text = message;

            this.Text = title.ToUpper();

            if (icon != null)
            {
                this.Icon = icon;
                this.ShowIcon = true;
            }

            if (!string.IsNullOrEmpty(gifPath) && File.Exists(gifPath))
            {
                picLoading.Image = Image.FromFile(gifPath.ToLower());
                picLoading.Visible = true;
            }

            // Tự động điều chỉnh form
            this.AutoSize = true;
            this.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            this.MinimumSize = new System.Drawing.Size(330, 150);
        }

        public static void ShowGifAlert( string message, string title = "THÔNG BÁO", string myIcon = "warning")
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string iconPath = Path.Combine(baseDir, "Assets", "megaphone.ico");
            string gifPath = Path.Combine(baseDir, "Assets", myIcon + ".gif");

            Icon alertIcon = File.Exists(iconPath) ? new Icon(iconPath) : null;

            using (var alert = new FrmWaiting(message, alertIcon, title, gifPath))
            {
                alert.StartPosition = FormStartPosition.CenterScreen;
                alert.ControlBox = true;
                alert.MinimizeBox = false;
                alert.MaximizeBox = false;
                alert.TopMost = true;

                alert.ShowIcon = alertIcon != null;

                alert.ShowDialog();  // ⭐ THIS IS THE IMPORTANT PART ⭐
            }
        }


        // Cho phép đổi nội dung khi đang chạy (tuỳ chọn)
        public string MessageText
        {
            get => lblMessage.Text;
            set => lblMessage.Text = value;
        }

        // Cho phép đổi title khi đang chạy
        public string TitleText
        {
            get => this.Text;
            set => this.Text = value;
        }

        // Cho phép đổi icon khi đang chạy
        public Icon TitleIcon
        {
            get => this.Icon;
            set => this.Icon = value;
        }

        // Đóng form an toàn từ luồng nền
        public void SafeClose()
        {
            if (IsDisposed) return;
            if (InvokeRequired) BeginInvoke(new Action(Close));
            else Close();
        }
    }



    public static class WaitingHelper
    {
        /// <summary>
        /// Chạy hàm async với form chờ.
        /// </summary>
        public static async Task RunWithWaiting(Func<Task> action, string message = "ĐANG THỰC HIỆN YÊU CẦU...")
        {
            using (var waiting = new FrmWaiting(message))
            {
                waiting.TopMost = true;
                waiting.StartPosition = FormStartPosition.CenterScreen;
                waiting.Show();
                waiting.Refresh();

                try
                {
                    await action();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi: " + ex.Message, "Lỗi",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    waiting.Close();
                }
            }
        }

        /// <summary>
        /// Chạy hàm đồng bộ (Action) với form chờ.
        /// </summary>
        public static async Task RunWithWaiting(Action action, string message = "ĐANG THỰC HIỆN YÊU CẦU...")
        {
            await RunWithWaiting(() => Task.Run(action), message);
        }

        /// <summary>
        /// Phiên bản generic: chạy hàm async trả về kết quả T trong khi hiển thị form chờ.
        /// </summary>
        public static async Task<T> RunWithWaiting<T>(Func<Task<T>> func, string message = "ĐANG THỰC HIỆN YÊU CẦU...")
        {
            using (var waiting = new FrmWaiting(message))
            {
                waiting.TopMost = true;
                waiting.StartPosition = FormStartPosition.CenterScreen;
                waiting.Show();
                waiting.Refresh();

                try
                {
                    return await func();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi: " + ex.Message, "Lỗi",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return default;
                }
                finally
                {
                    waiting.Close();
                }
            }
        }
    }

}
