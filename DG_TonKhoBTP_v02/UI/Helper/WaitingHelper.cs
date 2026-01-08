using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DG_TonKhoBTP_v02.UI.Helper
{
    public static class WaitingHelper
    {
        /// <summary>
        /// Chạy hàm async với form chờ.
        /// </summary>
        public static async Task RunWithWaiting(Func<Task> action, string message = "ĐANG THỰC HIỆN YÊU CẦU...")
        {
            using (var waiting = new FrmWaiting(message))
            {
                waiting.ShowAndRefresh(); // ✅ Dùng method mới

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
                waiting.ShowAndRefresh(); // ✅ Dùng method mới

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
