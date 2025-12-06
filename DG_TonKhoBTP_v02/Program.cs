using DG_TonKhoBTP_v02.UI;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DG_TonKhoBTP_v02
{
    internal static class Program
    {
        private static Mutex _mutex;
        private static EventWaitHandle _startedEvent;



        [STAThread]
        static void Main()
        {

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string _iconPath = Path.Combine(baseDir, "Assets", "megaphone.ico");
            string _gifPath = Path.Combine(baseDir, "Assets", "alert.gif");

            // Nhận biết có phải đang chạy do Application.Restart hay không
            var args = Environment.GetCommandLineArgs();
            bool isRestart = args.Any(a =>
                string.Equals(a, "/restart", StringComparison.OrdinalIgnoreCase));

            bool isNewInstance;

            // Mutex ngăn ứng dụng chạy nhiều instance
            _mutex = new Mutex(true, "DG_TonKhoBTP_v02_Mutex", out isNewInstance);

            // Nếu KHÔNG phải instance mới, nhưng đây là tiến trình restart
            // → chờ instance cũ thoát (nhả mutex) rồi coi mình là instance chính.
            if (!isNewInstance && isRestart)
            {
                try
                {
                    // Chờ tối đa 10 giây để instance cũ thoát
                    if (_mutex.WaitOne(TimeSpan.FromSeconds(10)))
                    {
                        isNewInstance = true;
                    }
                    else
                    {
                        FrmWaiting.ShowGifAlert("Không thể khởi động lại ứng dụng. Instance cũ không phản hồi.\nVui lòng đóng ứng dụng cũ và thử lại.",
                            "LỖI KHỞI ĐỘNG LẠI");
                        return;
                    }
                }
                catch (AbandonedMutexException)
                {
                    // Instance cũ chết đột ngột, nhưng mình đã giành được mutex
                    isNewInstance = true;
                }
            }

            // Event báo hiệu ứng dụng đã khởi tạo xong
            _startedEvent = new EventWaitHandle(
                false,
                EventResetMode.ManualReset,
                "DG_TonKhoBTP_v02_StartedEvent"
            );

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // ================================
            // INSTANCE THỨ 2 (không phải restart)
            // ================================
            if (!isNewInstance)
            {
                // Kiểm tra xem instance đầu tiên đã khởi tạo xong chưa
                bool isAppFullyStarted = _startedEvent.WaitOne(0);

                // CASE 2: Ứng dụng đang khởi tạo
                if (!isAppFullyStarted)
                {
                    using (var waiting = new FrmWaiting(
                        "Đang khởi động, Xin chờ...",
                        null,
                        "Thông báo",
                        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "alert.gif")
                    ))
                    {
                        waiting.StartPosition = FormStartPosition.CenterScreen;

                        // Luồng nền đợi app chính khởi tạo xong rồi tự đóng form
                        Task.Run(() =>
                        {
                            _startedEvent.WaitOne();
                            waiting.SafeClose();
                        });

                        Application.Run(waiting);
                    }

                    return;
                }
                // CASE 3: Ứng dụng đã chạy
                else
                {

                    Icon waitingIcon = null;
                    if (File.Exists(_iconPath))
                        waitingIcon = new Icon(_iconPath);

                    using (var waiting = new FrmWaiting(
                        "ỨNG DỤNG ĐANG CHẠY.",
                        waitingIcon,
                        "Thông báo ứng dụng",
                        _gifPath
                    ))
                    {
                        waiting.StartPosition = FormStartPosition.CenterScreen;

                        // Chỉ bật nút X
                        waiting.ControlBox = true;
                        waiting.MinimizeBox = false;
                        waiting.MaximizeBox = false;

                        Application.Run(waiting);
                    }

                    return;
                }
            }

            // ================================
            // INSTANCE ĐẦU TIÊN (hoặc instance RESTART)
            // ================================
            var mainForm = new MainForm();

//#if DEBUG
//            // GIẢ LẬP KHỞI ĐỘNG CHẬM 15 GIÂY ĐỂ TEST CASE 2
//            Thread.Sleep(8000);
//#endif


            // Khi form chính load xong → báo hiệu ứng dụng đã sẵn sàng
            mainForm.Shown += (s, e) =>
            {
                _startedEvent.Set();
            };

            // Khi form đóng → dọn dẹp tài nguyên
            mainForm.FormClosed += (s, e) =>
            {
                try
                {
                    _startedEvent?.Reset();
                    _mutex?.ReleaseMutex();
                    _mutex?.Dispose();
                }
                catch { }
            };

            Application.Run(mainForm);
        }

        /// <summary>
        /// Method để restart ứng dụng an toàn
        /// </summary>
        public static void RestartApplication()
        {
            try
            {
                // Lấy đường dẫn exe hiện tại
                string exePath = Application.ExecutablePath;

                // Khởi động process mới với argument /restart
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = exePath,
                    Arguments = "/restart",
                    UseShellExecute = true
                };

                Process.Start(startInfo);

                // Đóng ứng dụng hiện tại (sẽ giải phóng mutex)
                Application.Exit();
            }
            catch (Exception ex)
            {
                FrmWaiting.ShowGifAlert($"Lỗi khi khởi động lại ứng dụng: {ex.Message}", "LỖI");
            }
        }
    }
}