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

            form.ControlBox = true;
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

        /// <summary>
        /// Hiển thị form waiting với cấu hình mặc định (TopMost, CenterScreen, Refresh)
        /// </summary>
        public void ShowAndRefresh()
        {
            this.TopMost = true;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Show();
            this.Refresh();
            Application.DoEvents();
        }

        public static void ShowGifAlert(string message, string title = "THÔNG BÁO", string myIcon = "warning")
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

                alert.ShowDialog();
            }
        }

        // Cho phép đổi nội dung khi đang chạy
        public string MessageText
        {
            get => lblMessage.Text;
            set
            {
                lblMessage.Text = value;
                this.Refresh();
                Application.DoEvents();
            }
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

        // Đóng và dispose an toàn
        public void CloseAndDispose()
        {
            try
            {
                if (!IsDisposed)
                {
                    this.Close();
                    this.Dispose();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi đóng FrmWaiting: {ex.Message}");
            }
        }
    }

    
}