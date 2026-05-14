using QRCoder;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DG_TonKhoBTP_v02.Printer.TemXuatHang
{
    public static class QrCodeService
    {
        /// <summary>
        /// Sinh QR Code từ chuỗi nội dung, trả về <see cref="Bitmap"/> kích thước chỉ định.
        /// </summary>
        /// <param name="content">Nội dung cần encode. Ví dụ: "E10-261743/7-01"</param>
        /// <param name="pixelSize">
        ///     Kích thước ảnh QR đầu ra (pixel x pixel).
        ///     QRCoder sinh mỗi "module" = <paramref name="pixelsPerModule"/> px,
        ///     ta resize về <paramref name="pixelSize"/> để fit vùng in.
        /// </param>
        /// <param name="pixelsPerModule">
        ///     Số pixel mỗi module QR (ảnh hưởng độ nét trước khi resize).
        ///     Mặc định 10 — đủ nét để in.
        /// </param>
        /// <returns>Bitmap QR Code, hoặc null nếu sinh thất bại.</returns>
        public static Bitmap GenerateQrBitmap(
            string content,
            int pixelSize = 280,
            int pixelsPerModule = 10)
        {
            if (string.IsNullOrWhiteSpace(content))
                return null;

            try
            {
                using (var generator = new QRCodeGenerator())
                {
                    // ECCLevel.Q — mức sửa lỗi 25%, cân bằng giữa kích thước và khả năng đọc
                    QRCodeData qrData = generator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);

                    using (var qrCode = new QRCode(qrData))
                    {
                        // Sinh bitmap thô từ QRCoder
                        Bitmap rawBitmap = qrCode.GetGraphic(
                            pixelsPerModule: pixelsPerModule,
                            darkColor: Color.Black,
                            lightColor: Color.White,
                            drawQuietZones: true);   // vùng trắng bao quanh QR

                        // Resize về kích thước chỉ định để fit đúng vùng in
                        if (rawBitmap.Width == pixelSize && rawBitmap.Height == pixelSize)
                            return rawBitmap;

                        Bitmap resized = new Bitmap(pixelSize, pixelSize, PixelFormat.Format32bppArgb);
                        using (Graphics g = Graphics.FromImage(resized))
                        {
                            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                            g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                            g.DrawImage(rawBitmap, 0, 0, pixelSize, pixelSize);
                        }

                        rawBitmap.Dispose();
                        return resized;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[QrCodeService] Lỗi sinh QR: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Sinh QR Code và trả về dạng <see cref="Image"/> (tiện dùng với PictureBox).
        /// </summary>
        public static Image GenerateQrImage(string content, int pixelSize = 280)
            => GenerateQrBitmap(content, pixelSize);
    }
}
