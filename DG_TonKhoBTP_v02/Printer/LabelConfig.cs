using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DG_TonKhoBTP_v02.Printer
{
    public static class LabelConfig
    {
        // Độ phân giải máy in WS412TT (300 dpi = 12 dots/mm)
        public const int Dpi = 300;

        // ===============================
        //   CHỈ CẦN SỬA Ở 2 DÒNG NÀY
        // ===============================
        public const double LabelWidthMm = 100.0; // Rộng giấy 10 cm
        public const double MarginMm = 4.0;      // Lề 4 mm quanh ảnh
                                                 // ===============================

        // Rộng vùng nội dung
        public static double ContentWidthMm => LabelWidthMm - 2 * MarginMm;

        // Chuyển đổi mm → pixel
        public static int MmToPx(double mm) =>
            (int)Math.Round(mm / 25.4 * Dpi);

        // mm → đơn vị 1/100 inch (PaperSize)
        public static int MmToHi(double mm) =>
            (int)Math.Round(mm / 25.4 * 100);

        public const double TopMarginMm = 8.0;

        public static int TopMarginPx => MmToPx(TopMarginMm);
    }
}
