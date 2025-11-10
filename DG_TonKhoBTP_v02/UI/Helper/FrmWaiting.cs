using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DG_TonKhoBTP_v02.UI
{
    public partial class FrmWaiting : Form
    {
        public FrmWaiting() : this("Đang xử lý, vui lòng đợi...")
        {
        }
        public FrmWaiting(string message)
        {
            InitializeComponent();
            lblMessage.Text = message;
        }

        // Cho phép đổi nội dung khi đang chạy (tuỳ chọn)
        public string MessageText
        {
            get => lblMessage.Text;
            set => lblMessage.Text = value;
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
