using QRCoder;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace DG_TonKhoBTP_v02.Printer.TemXuatHang
{
    public static class QrCodeService
    {
        /// <summary>
        /// Sinh QR Code từ chuỗi nội dung, trả về Bitmap kích thước chỉ định.
        /// </summary>
        /// <param name="content">Nội dung cần encode. Ví dụ: "E10-261743/7-01"</param>
        /// <param name="pixelSize">
        /// Kích thước ảnh QR đầu ra: pixelSize x pixelSize.
        /// </param>
        /// <param name="pixelsPerModule">
        /// Số pixel mỗi module QR.
        /// Mặc định 10 — đủ nét để in.
        /// </param>
        /// <param name="quietZoneCropModules">
        /// Số module trắng bị crop mỗi cạnh.
        /// QRCoder mặc định tạo quiet zone khoảng 4 module.
        /// Giá trị 2 nghĩa là giảm padding trắng còn khoảng 1/2.
        /// </param>
        /// <returns>Bitmap QR Code, hoặc null nếu sinh thất bại.</returns>
        public static Bitmap GenerateQrBitmap(
            string content,
            int pixelSize = 280,
            int pixelsPerModule = 10,
            int quietZoneCropModules = 2)
        {
            if (string.IsNullOrWhiteSpace(content))
                return null;

            try
            {
                using (var generator = new QRCodeGenerator())
                using (QRCodeData qrData = generator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q))
                using (var qrCode = new QRCode(qrData))
                {
                    // Sinh QR gốc, vẫn có quiet zone trắng bao quanh
                    using (Bitmap rawBitmap = qrCode.GetGraphic(
                        pixelsPerModule: pixelsPerModule,
                        darkColor: Color.Black,
                        lightColor: Color.White,
                        drawQuietZones: true))
                    {
                        Bitmap croppedBitmap = null;

                        try
                        {
                            // Crop bớt quiet zone để padding trắng nhỏ hơn
                            int cropInset = Math.Max(0, quietZoneCropModules * pixelsPerModule);

                            Bitmap sourceBitmap;

                            if (cropInset > 0 &&
                                rawBitmap.Width > cropInset * 2 &&
                                rawBitmap.Height > cropInset * 2)
                            {
                                Rectangle cropRect = new Rectangle(
                                    cropInset,
                                    cropInset,
                                    rawBitmap.Width - cropInset * 2,
                                    rawBitmap.Height - cropInset * 2);

                                croppedBitmap = rawBitmap.Clone(cropRect, rawBitmap.PixelFormat);
                                sourceBitmap = croppedBitmap;
                            }
                            else
                            {
                                sourceBitmap = rawBitmap;
                            }

                            // Resize về đúng kích thước cần in
                            Bitmap resized = new Bitmap(pixelSize, pixelSize, PixelFormat.Format32bppArgb);

                            using (Graphics g = Graphics.FromImage(resized))
                            {
                                // QR code nên dùng NearestNeighbor để cạnh vuông rõ nét,
                                // tránh HighQualityBicubic vì có thể làm QR bị nhòe khi in.
                                g.InterpolationMode = InterpolationMode.NearestNeighbor;
                                g.SmoothingMode = SmoothingMode.None;
                                g.PixelOffsetMode = PixelOffsetMode.Half;
                                g.CompositingQuality = CompositingQuality.HighSpeed;

                                g.Clear(Color.White);

                                g.DrawImage(
                                    sourceBitmap,
                                    new Rectangle(0, 0, pixelSize, pixelSize),
                                    new Rectangle(0, 0, sourceBitmap.Width, sourceBitmap.Height),
                                    GraphicsUnit.Pixel);
                            }

                            return resized;
                        }
                        finally
                        {
                            if (croppedBitmap != null)
                                croppedBitmap.Dispose();
                        }
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
        /// Sinh QR Code và trả về dạng Image, tiện dùng với PictureBox.
        /// </summary>
        public static Image GenerateQrImage(string content, int pixelSize = 280)
        {
            return GenerateQrBitmap(content, pixelSize);
        }
    }
}