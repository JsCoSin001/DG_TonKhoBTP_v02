using DG_TonKhoBTP_v02.Models;
using QRCoder;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Printing;
using System.Text;
using System.Windows;
using FontStyle = System.Drawing.FontStyle;
using System.Management;


using System.Printing;


namespace DG_TonKhoBTP_v02.Printer
{
    public class PrintHelper
    {

        public static void PrintLabel(PrinterModel printer)
        {
            //string qrLabel = $"{printer.MaBin}_{printer.KhoiLuong}kg_{printer.ChieuDai}m";
            string qrLabel = $"{printer.MaBin}";

            using (Bitmap image = ConvertToImage(printer, qrLabel))
            {
                try
                {
                    PrintImage(image);
                }
                catch (Exception ex)
                {
                    // Không MessageBox ở đây, chỉ ném lỗi ra ngoài
                    throw new Exception($"CHƯA IN ĐƯỢC TEM: {printer.MaBin}. {ex.Message}", ex);
                }
            }
        }


        public static bool IsPrinterReady(string printerName)
        {
            try
            {
                // LocalPrintServer = máy tính hiện tại
                using (var server = new LocalPrintServer())
                {
                    // Tên printerName phải đúng với tên trong "Devices and Printers"
                    // ví dụ: "SATO WS4(4inch) 412TT"
                    PrintQueue queue = server.GetPrintQueue(printerName);

                    // Cập nhật trạng thái mới nhất từ spooler
                    queue.Refresh();

                    PrintQueueStatus status = queue.QueueStatus;

                    // Các trạng thái coi là LỖI / KHÔNG SẴN SÀNG
                    bool hasFatalStatus =
                        status.HasFlag(PrintQueueStatus.Paused) || // đang bị pause
                        status.HasFlag(PrintQueueStatus.Error) || // lỗi chung
                        status.HasFlag(PrintQueueStatus.PendingDeletion) ||
                        status.HasFlag(PrintQueueStatus.PaperJam) ||
                        status.HasFlag(PrintQueueStatus.PaperOut) || // hết giấy / nhãn
                        status.HasFlag(PrintQueueStatus.ManualFeed) ||
                        status.HasFlag(PrintQueueStatus.PaperProblem) || // kẹt giấy, nắp mở,...
                        status.HasFlag(PrintQueueStatus.Offline) || // Use printer offline, mất kết nối
                        status.HasFlag(PrintQueueStatus.NoToner) ||
                        status.HasFlag(PrintQueueStatus.NotAvailable) ||
                        status.HasFlag(PrintQueueStatus.OutputBinFull) ||
                        status.HasFlag(PrintQueueStatus.UserIntervention) || // cần thao tác người dùng
                        status.HasFlag(PrintQueueStatus.OutOfMemory);

                    // CÁI NÀO COI LÀ OK?
                    // - None, Printing, Busy, Waiting, Processing, WarmingUp, PowerSave,...
                    // vẫn cho in bình thường nên return true nếu không có "fatal" ở trên.
                    return !hasFatalStatus;
                }
            }
            catch
            {
                // Không tìm thấy printer hoặc lỗi khác → coi như không sẵn sàng
                return false;
            }
        }


        public static bool PrintImage(Bitmap image)
        {
            string PRINTER_NAME = Properties.Settings.Default.PrinterName;

            try
            {
                using (PrintDocument pd = new PrintDocument())
                {
                    pd.PrinterSettings.PrinterName = PRINTER_NAME;

                    if (!pd.PrinterSettings.IsValid)
                        throw new Exception($"Máy in '{PRINTER_NAME}' không tồn tại");

                    if (!IsPrinterReady(PRINTER_NAME))
                        throw new Exception($"máy in '{PRINTER_NAME}' không sẵn sàng.");

                    double dpiX = image.HorizontalResolution;
                    double dpiY = image.VerticalResolution;

                    double labelWidthMm = image.Width / dpiX * 25.4;
                    double labelHeightMm = image.Height / dpiY * 25.4;

                    int paperWidthHi = LabelConfig.MmToHi(labelWidthMm);
                    int paperHeightHi = LabelConfig.MmToHi(labelHeightMm);

                    pd.DefaultPageSettings.PaperSize = new PaperSize("Label", paperWidthHi, paperHeightHi);
                    pd.DefaultPageSettings.Margins = new Margins(0, 0, 0, 0);

                    pd.PrintPage += (sender, e) =>
                    {
                        Rectangle dest = new Rectangle(
                            0,
                            0,
                            e.PageBounds.Width,
                            e.PageBounds.Height
                        );

                        e.Graphics.DrawImage(image, dest);
                    };

                    pd.Print();
                }

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"\n{ex.Message}", ex);
            }
        }

        private static float CalculateNeededHeight(PrinterModel data, float maxWidth)
        {
            if (string.IsNullOrEmpty(data.GhiChu))
                return 0;

            using (Bitmap bmp = new Bitmap(1, 1))
            {
                // Đặt DPI thật
                bmp.SetResolution(LabelConfig.Dpi, LabelConfig.Dpi);

                using (Graphics g = Graphics.FromImage(bmp))
                using (Font font = new Font("Arial", 11, FontStyle.Bold))
                {
                    StringFormat format = new StringFormat
                    {
                        Trimming = StringTrimming.Word,
                        FormatFlags = StringFormatFlags.NoClip
                    };

                    SizeF size = g.MeasureString(
                        data.GhiChu,
                        font,
                        new SizeF(maxWidth, float.MaxValue),
                        format
                    );

                    return size.Height;
                }
            }
        }

        public static Bitmap ConvertToImage(PrinterModel data, string qrContent)
        {
            try
            {
                int widthPx = LabelConfig.MmToPx(LabelConfig.ContentWidthMm);
                float maxWidth = widthPx - 100;

                //==============================
                //  PHASE 1: TÍNH CHIỀU CAO THẬT
                //==============================
                float chieuCaoGhiChu = CalculateNeededHeight(data, maxWidth);

                // Chiều cao các phần cố định (tính theo thứ tự từ trên xuống)
                float lineHeight = 80;
                float baseHeight = LabelConfig.TopMarginPx;  // top margin
                baseHeight += lineHeight;  // Tiêu đề "PHIẾU QUẢN LÝ SẢN PHẨM"
                baseHeight += lineHeight;  // BC-ISO-09-08
                baseHeight += lineHeight;  // Ngày SX + Ca SX
                baseHeight += lineHeight;  // Khối lượng + Chiều dài
                baseHeight += lineHeight;  // Sản phẩm
                baseHeight += lineHeight;  // Mã SP
                baseHeight += lineHeight;  // Đánh Giá
                baseHeight += lineHeight;  // CN Vận hành
                baseHeight += 25;          // Spacing trước "Ghi chú"
                baseHeight += chieuCaoGhiChu;  // Chiều cao ghi chú (có thể = 0 nếu không có)
                baseHeight += 25;          // Spacing sau "Ghi chú"
                baseHeight += lineHeight;  // KCS + QR section
                baseHeight += LabelConfig.MmToPx(30);  // QR size (30mm)
                baseHeight += 50;          // Bottom padding

                int totalHeightPx = (int)Math.Ceiling(baseHeight);

                Bitmap bitmap = new Bitmap(widthPx, totalHeightPx, PixelFormat.Format24bppRgb);
                bitmap.SetResolution(LabelConfig.Dpi, LabelConfig.Dpi);

                using (Graphics graphics = Graphics.FromImage(bitmap))
                {
                    graphics.Clear(Color.White);
                    graphics.SmoothingMode = SmoothingMode.AntiAlias;

                    Font labelFont = new Font("Arial", 10);
                    Font labelFontISO = new Font("Arial", 9);
                    Font labelBoldFont = new Font("Arial", 11, FontStyle.Bold);
                    Font titleFont = new Font("Arial", 12, FontStyle.Bold);
                    Brush brush = Brushes.Black;

                    float y = LabelConfig.TopMarginPx;
                    float lineHeightVal = 80;

                    StringFormat centerFormat = new StringFormat() { Alignment = StringAlignment.Center };
                    graphics.DrawString("PHIẾU QUẢN LÝ SẢN PHẨM", titleFont, brush, widthPx / 2f, y, centerFormat);
                    y += lineHeightVal - 5;

                    StringFormat rightAlign = new StringFormat() { Alignment = StringAlignment.Far };
                    graphics.DrawString("BC-ISO-09-08", labelFontISO, brush, widthPx, y, rightAlign);
                    y += lineHeightVal;

                    graphics.DrawString("Ngày SX: ", labelFont, brush, 30, y);
                    graphics.DrawString(data.NgaySX, labelBoldFont, brush, 250, y);
                    graphics.DrawString("Ca SX: ", labelFont, brush, 650, y);
                    graphics.DrawString(data.CaSX, labelBoldFont, brush, 800, y);
                    y += lineHeightVal;

                    graphics.DrawString("Khối lượng: ", labelFont, brush, 30, y);
                    graphics.DrawString($"{data.KhoiLuong} Kg", labelBoldFont, brush, 300, y);
                    graphics.DrawString("Chiều dài: ", labelFont, brush, 580, y);
                    graphics.DrawString($"{data.ChieuDai} M", labelBoldFont, brush, 800, y);
                    y += lineHeightVal;

                    graphics.DrawString("Sản phẩm: ", labelFont, brush, 30, y);
                    graphics.DrawString(data.TenSP, labelBoldFont, brush, 280, y);
                    y += lineHeightVal;

                    graphics.DrawString("Mã SP: ", labelFont, brush, 30, y);
                    graphics.DrawString(data.MaBin, labelBoldFont, brush, 200, y);
                    y += lineHeightVal;

                    graphics.DrawString("Đánh Giá: ", labelFont, brush, 30, y);
                    graphics.DrawString("☐ OK    ☐ NG", labelFont, brush, 250, y);
                    y += lineHeightVal;

                    graphics.DrawString("CN Vận hành: ", labelFont, brush, 30, y);
                    graphics.DrawString(data.TenCN, labelBoldFont, brush, 350, y);
                    y += lineHeightVal;

                    graphics.DrawString("Ghi chú: ", labelFont, brush, 30, y);
                    y += 25;

                    if (!string.IsNullOrEmpty(data.GhiChu))
                    {
                        StringFormat wrap = new StringFormat(StringFormatFlags.NoClip)
                        {
                            Trimming = StringTrimming.Word
                        };

                        // đo lại cho chắc (hoàn toàn trùng phase 1)
                        SizeF textSize = graphics.MeasureString(
                            data.GhiChu,
                            labelBoldFont,
                            new SizeF(maxWidth, float.MaxValue),
                            wrap
                        );

                        RectangleF textBox = new RectangleF(50, y + 25, maxWidth, textSize.Height);
                        graphics.DrawString(data.GhiChu, labelBoldFont, brush, textBox, wrap);

                        y += textSize.Height + 25;
                    }

                    y += lineHeightVal + 50;

                    graphics.DrawString("KCS", labelFont, brush, 180, y);

                    if (data.QC != "")
                    {
                        graphics.DrawString("KCS: " + data.QC, labelBoldFont, brush, 100, y + 90);

                        graphics.DrawString("ĐÃ KIỂM TRA", labelFont, brush, 70, y + 160);
                    }

                    // QR
                    if (!string.IsNullOrEmpty(qrContent))
                    {
                        int qrSizePx = LabelConfig.MmToPx(30);
                        int qrX = 600;

                        Rectangle qrRect = new Rectangle(qrX, (int)y, qrSizePx, qrSizePx);

                        using (var qrGenerator = new QRCodeGenerator())
                        using (var qrData = qrGenerator.CreateQrCode(qrContent, QRCodeGenerator.ECCLevel.Q))
                        using (var qrCode = new QRCode(qrData))
                        using (Bitmap qrBmp = qrCode.GetGraphic(20))
                        {
                            graphics.DrawImage(qrBmp, qrRect);
                        }

                        y += qrSizePx;
                    }

                    labelFont.Dispose();
                    labelBoldFont.Dispose();
                    labelFontISO.Dispose();
                    titleFont.Dispose();
                }

                return bitmap;
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi tạo ảnh: {ex.Message}");
            }
        }
    }
}