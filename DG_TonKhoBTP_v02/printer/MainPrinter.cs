using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DG_TonKhoBTP_v02.Printer
{
    public static class MainPrinter
    {
        // 👉 Đặt đúng tên máy in trong Windows
        private const string PrinterName = "SATO WS408";

        // 👉 In chữ + QR cho tem 100x100mm (203 dpi)
        //public static void PrintUpperTextAndQr(string upperText,
        //                                       int textX = 120, int textY = 820,
        //                                       string fontCmd = "XS",
        //                                       string enlarge = "0304",
        //                                       int pitch = 2)
        //{
        public static void PrintUpperTextAndQr(string upperText)
        {

            const char ESC = (char)0x1B;

            // ----- Khối text -----
            string textBlock =
                $"{ESC}{(upperText ?? string.Empty)}";

            // ----- Cấu hình tem -----
            // WS408: 203 dpi ≈ 8 dots/mm → 100 mm = 800 dots
            //int labelHeight = 1200; // 100 mm * 12
            //int labelWidth = 1200; // 100 mm * 12

            var job = new StringBuilder();
            job.Append(ESC).Append('A');                           // Bắt đầu
            job.Append($"{ESC}%0");                                // SBPL emulation
            //job.Append($"{ESC}A1{labelHeight:0000}{labelWidth:0000}"); // 👈 Đặt kích thước tem
            job.Append($"{ESC}AR");                                // Vùng in chuẩn
            //job.Append($"{ESC}D{labelHeight:0000}");                // Chiều cao vùng in (tùy chọn)
            job.Append(textBlock);                                 // Text
            //job.Append(qrSbpl);                                    // QR code
            job.Append($"{ESC}Q1");                                // In 1 tem
            job.Append(ESC).Append('Z');                           // Kết thúc và in
            job.Append("\r\n");

            RawPrint(PrinterName, job.ToString());
        }

        #region RawPrinterHelper (Winspool)
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal class DOCINFO
        {
            [MarshalAs(UnmanagedType.LPWStr)] public string pDocName;
            [MarshalAs(UnmanagedType.LPWStr)] public string pOutputFile;
            [MarshalAs(UnmanagedType.LPWStr)] public string pDataType;
        }

        [DllImport("winspool.Drv", EntryPoint = "OpenPrinterW", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true)]
        static extern bool OpenPrinter(string src, out IntPtr hPrinter, IntPtr pd);

        [DllImport("winspool.Drv", SetLastError = true, ExactSpelling = true)]
        static extern bool ClosePrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", EntryPoint = "StartDocPrinterW", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true)]
        static extern bool StartDocPrinter(IntPtr hPrinter, int level, [In] DOCINFO di);

        [DllImport("winspool.Drv", SetLastError = true, ExactSpelling = true)]
        static extern bool EndDocPrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", SetLastError = true, ExactSpelling = true)]
        static extern bool StartPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", SetLastError = true, ExactSpelling = true)]
        static extern bool EndPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", SetLastError = true, ExactSpelling = true)]
        static extern bool WritePrinter(IntPtr hPrinter, IntPtr pBytes, int dwCount, out int dwWritten);

        private static void RawPrint(string printerName, string content)
        {
            if (!OpenPrinter(printerName, out IntPtr hPrinter, IntPtr.Zero))
                throw new InvalidOperationException($"Không mở được máy in '{printerName}'.");

            try
            {
                var di = new DOCINFO { pDocName = "WS408 SBPL Job", pDataType = "RAW" };
                if (!StartDocPrinter(hPrinter, 1, di)) throw new InvalidOperationException("StartDocPrinter thất bại.");
                if (!StartPagePrinter(hPrinter)) throw new InvalidOperationException("StartPagePrinter thất bại.");

                byte[] bytes = Encoding.ASCII.GetBytes(content);
                IntPtr ptr = Marshal.AllocHGlobal(bytes.Length);
                try
                {
                    Marshal.Copy(bytes, 0, ptr, bytes.Length);
                    if (!WritePrinter(hPrinter, ptr, bytes.Length, out _))
                        throw new InvalidOperationException("WritePrinter thất bại.");
                }
                finally { Marshal.FreeHGlobal(ptr); }

                EndPagePrinter(hPrinter);
                EndDocPrinter(hPrinter);
            }
            finally { ClosePrinter(hPrinter); }
        }
        #endregion
    }
}
