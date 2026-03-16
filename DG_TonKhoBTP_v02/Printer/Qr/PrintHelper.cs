using DG_TonKhoBTP_v02.Models;
using QRCoder;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Printing;
using System.Management;
using System.Printing;
using System.Text;
using System.Windows;
using System.Windows.Forms;
using FontStyle = System.Drawing.FontStyle;
using Size = System.Drawing.Size;


namespace DG_TonKhoBTP_v02.Printer
{
    public class PrintHelper
    {
        // ============================================================
        //  Hằng số layout (mm)
        // ============================================================
        private const double MARGIN_MM = 1.0;
        private const double QR_SIZE_MM = 35.0;
        private const double LINE_HEIGHT_MM = 6.5;  // khoảng cách dòng vùng nội dung phải QR
        private const double ROW_SINGLE_MM = 5.5;  // chiều cao 1 dòng đơn (ROW2 + dòng GhiChu khi rỗng)
        private const double BOTTOM_PAD_MM = 2.0;
        private const double NOTE_PADDING_MM = 2.0;  // padding trái/phải/trên/dưới vùng GhiChu
        private const double LABEL_WIDTH_MM = 100.0;
        private const double ROW2_PAD_MM = 1.0;  // padding trên/dưới dòng Ngày + Người làm

        // Font size (Point)
        private const float FONT_TITLE_PT = 12f;
        private const float FONT_NORMAL_PT = 10f;
        private const float FONT_BOLD_PT = 11f;
        private const float FONT_NOTE_PT = 10f;
        private const string FONT_NAME = "Times New Roman";

        // ============================================================
        //  Public API
        // ============================================================
        public static void PrintLabel(PrinterModel printer)
        {
            string qrLabel = $"{printer.MaBin}";

            using (Bitmap image = ConvertToImage(printer, qrLabel))
            {
                try
                {
                    PrintImage(image);
                }
                catch (Exception ex)
                {
                    throw new Exception($"CHƯA IN ĐƯỢC TEM: {printer.MaBin}. {ex.Message}", ex);
                }
            }
        }

        // ============================================================
        //  ConvertToImage
        //
        //  Layout:
        //  ┌─────────────────────────────────────────┐
        //  │  [QR 35mm]  │  TenSP                    │  ← PHẦN A
        //  │             │  LOT:    MaBin             │
        //  │             │  K.Lượng:  ...             │
        //  │             │  C.Dài:    ...             │
        //  │             │  Ca:       ...             │
        //  ├─────────────────────────────────────────┤
        //  │  Ngày: ...     │  Người làm: ...        │  ← ROW2 (X thẳng hàng với QR)
        //  ├─────────────────────────────────────────┤
        //  │  Ghi chú: (nếu có — chiều cao động)     │  ← PHẦN B
        //  └─────────────────────────────────────────┘
        // ============================================================
        public static Bitmap ConvertToImage(PrinterModel data, string qrContent)
        {
            int dpi = LabelConfig.Dpi;
            int labelWidthPx = Mm2Px(LABEL_WIDTH_MM, dpi);
            int marginPx = Mm2Px(MARGIN_MM, dpi);
            int qrSizePx = Mm2Px(QR_SIZE_MM, dpi);
            int lineHeightPx = Mm2Px(LINE_HEIGHT_MM, dpi);
            int rowSinglePx = Mm2Px(ROW_SINGLE_MM, dpi);
            int bottomPadPx = Mm2Px(BOTTOM_PAD_MM, dpi);
            int notePadPx = Mm2Px(NOTE_PADDING_MM, dpi);
            int row2PadPx = Mm2Px(ROW2_PAD_MM, dpi);

            // ── PHASE 1A: chiều cao PHẦN A ──────────────────────────
            int heightA1 = marginPx + qrSizePx;

            int rowCount = 5;
            int extraTitle = Mm2Px(2.0, dpi);
            int heightA2 = marginPx + extraTitle + (rowCount * lineHeightPx);

            int heightA = Math.Max(heightA1, heightA2);

            // ── PHASE 1B: chiều cao ROW2 (Ngày + Người làm) ─────────
            // dùng rowSinglePx thay lineHeightPx để dòng này nhỏ gọn hơn
            int row2Height = row2PadPx + rowSinglePx + row2PadPx;

            // ── PHASE 1C: chiều cao PHẦN B (Ghi chú) ────────────────
            int noteWidth = labelWidthPx - 2 * notePadPx;
            int heightNoteSection = 0;
            bool hasGhiChu = !string.IsNullOrWhiteSpace(data.GhiChu);

            if (hasGhiChu)
            {
                using var measureBmp = new Bitmap(1, 1);
                measureBmp.SetResolution(dpi, dpi);
                using var gMeasure = Graphics.FromImage(measureBmp);
                gMeasure.PageUnit = GraphicsUnit.Pixel;

                using var noteFont = new Font(FONT_NAME, FONT_NOTE_PT,
                                             FontStyle.Regular, GraphicsUnit.Point);

                string fullNote = $"Ghi chú:\r\n{data.GhiChu}";

                var sf = new StringFormat(StringFormatFlags.NoClip)
                {
                    Trimming = StringTrimming.Word
                };

                SizeF measured = gMeasure.MeasureString(
                    fullNote, noteFont,
                    new SizeF(noteWidth, float.MaxValue), sf);

                heightNoteSection = notePadPx + (int)Math.Ceiling(measured.Height) + notePadPx;
            }
            else
            {
                // Không có ghi chú — chừa đúng 1 dòng nhỏ để in chữ "Ghi chú:"
                heightNoteSection = notePadPx + rowSinglePx + notePadPx;
            }

            // ── Tổng chiều cao ───────────────────────────────────────
            int heightAboveNote = heightA + row2Height;
            int totalHeight = heightAboveNote + heightNoteSection + bottomPadPx;

            // ── PHASE 2: Tạo Bitmap ──────────────────────────────────
            var bmp = new Bitmap(labelWidthPx, totalHeight, PixelFormat.Format24bppRgb);
            bmp.SetResolution(dpi, dpi);

            using (var g = Graphics.FromImage(bmp))
            {
                g.PageUnit = GraphicsUnit.Pixel;
                g.Clear(Color.White);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;

                using var titleFont = new Font(FONT_NAME, FONT_TITLE_PT, FontStyle.Bold, GraphicsUnit.Point);
                using var normalFont = new Font(FONT_NAME, FONT_NORMAL_PT, FontStyle.Regular, GraphicsUnit.Point);
                using var boldFont = new Font(FONT_NAME, FONT_BOLD_PT, FontStyle.Bold, GraphicsUnit.Point);
                using var noteFont = new Font(FONT_NAME, FONT_NOTE_PT, FontStyle.Regular, GraphicsUnit.Point);
                Brush brush = Brushes.Black;

                // ── PHẦN A1: QR code ─────────────────────────────────
                if (!string.IsNullOrEmpty(qrContent))
                {
                    using var gen = new QRCodeGenerator();
                    using var qrData = gen.CreateQrCode(qrContent, QRCodeGenerator.ECCLevel.Q);
                    using var qr = new QRCode(qrData);
                    using var qrBmp = qr.GetGraphic(6);

                    g.DrawImage(qrBmp, marginPx, marginPx, qrSizePx, qrSizePx);
                }

                // ── PHẦN A2: Nội dung bên phải QR ───────────────────
                float xText = marginPx + qrSizePx;
                float y = marginPx;

                // Dòng 1: Tên sản phẩm
                g.DrawString(data.TenSP ?? "", titleFont, brush, xText, y + 20);
                y += lineHeightPx + extraTitle;

                // Dòng 2: LOT
                g.DrawString("LOT:", normalFont, brush, xText, y);
                g.DrawString(data.MaBin ?? "", boldFont, brush, xText + Mm2Px(14, dpi), y);
                y += lineHeightPx;

                // Dòng 3: Khối lượng
                g.DrawString("K.Lượng:", normalFont, brush, xText, y);
                g.DrawString($"{data.KhoiLuong ?? ""} Kg", boldFont, brush, xText + Mm2Px(40, dpi), y);
                y += lineHeightPx;

                // Dòng 4: Chiều dài
                g.DrawString("C.Dài:", normalFont, brush, xText, y);
                g.DrawString($"{data.ChieuDai ?? ""} M", boldFont, brush, xText + Mm2Px(40, dpi), y);
                y += lineHeightPx;

                // Dòng 5: Ca sản xuất
                g.DrawString("Ca:", normalFont, brush, xText, y);
                g.DrawString(data.CaSX ?? "", boldFont, brush, xText + Mm2Px(40, dpi), y);

                // ── ROW2: Ngày (trái) | Người làm (phải) ─────────────
                // X của "Ngày" = marginPx → thẳng hàng với cạnh trái QR
                float yRow2 = heightA + row2PadPx;

                g.DrawString("Ngày:", normalFont, brush, (float)marginPx, yRow2);
                g.DrawString(data.NgaySX ?? "", boldFont, brush, marginPx + Mm2Px(13, dpi), yRow2);

                float xNguoiLam = labelWidthPx / 2f - 30;
                g.DrawString("Người làm:", normalFont, brush, xNguoiLam, yRow2);
                g.DrawString(data.TenCN ?? "", boldFont, brush, xNguoiLam + Mm2Px(22, dpi), yRow2);

                // ── PHẦN B: Ghi chú ───────────────────────────────────
                if (hasGhiChu)
                {
                    string fullNote = $"Ghi chú:{data.Mau}\r\n{data.GhiChu}";

                    var sf = new StringFormat(StringFormatFlags.NoClip)
                    {
                        Trimming = StringTrimming.Word
                    };

                    var noteRect = new RectangleF(
                        notePadPx,
                        heightAboveNote + notePadPx,
                        noteWidth,
                        heightNoteSection - notePadPx * 2
                    );

                    g.DrawString(fullNote, noteFont, brush, noteRect, sf);
                }
                else
                {
                    g.DrawString($"Ghi chú: {data.Mau}", normalFont, brush,
                                 (float)notePadPx, heightAboveNote + notePadPx);
                }
            }

            // SET LẠI DPI sau khi vẽ — tránh GDI+ reset về screen DPI
            bmp.SetResolution(dpi, dpi);

            return bmp;
        }

        // ============================================================
        //  PrintImage
        // ============================================================
        public static bool PrintImage(Bitmap image)
        {
            string PRINTER_NAME = Properties.Settings.Default.PrinterName;

            try
            {
                using (var pd = new PrintDocument())
                {
                    pd.PrinterSettings.PrinterName = PRINTER_NAME;

                    if (!pd.PrinterSettings.IsValid)
                        throw new Exception($"Máy in '{PRINTER_NAME}' không tồn tại");

                    if (!IsPrinterReady(PRINTER_NAME))
                        throw new Exception($"Máy in '{PRINTER_NAME}' không sẵn sàng.");

                    double trueDpi = LabelConfig.Dpi;

                    double labelWidthMm = image.Width / trueDpi * 25.4;
                    double labelHeightMm = image.Height / trueDpi * 25.4;

                    System.Diagnostics.Debug.WriteLine(
                        $"[PrintHelper] Bitmap: {image.Width}x{image.Height}px | " +
                        $"DPI bmp: {image.HorizontalResolution}x{image.VerticalResolution} | " +
                        $"Label: {labelWidthMm:F1}x{labelHeightMm:F1}mm");

                    int paperWidthHi = LabelConfig.MmToHi(labelWidthMm);
                    int paperHeightHi = LabelConfig.MmToHi(labelHeightMm);

                    pd.DefaultPageSettings.PaperSize = new PaperSize("Label", paperWidthHi, paperHeightHi);
                    pd.DefaultPageSettings.Margins = new Margins(0, 0, 0, 0);

                    pd.PrintPage += (sender, e) =>
                    {
                        e.Graphics.DrawImage(
                            image,
                            new Rectangle(0, 0, e.PageBounds.Width, e.PageBounds.Height));
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

        // ============================================================
        //  IsPrinterReady
        // ============================================================
        public static bool IsPrinterReady(string printerName)
        {
            try
            {
                using var server = new LocalPrintServer();
                PrintQueue queue = server.GetPrintQueue(printerName);
                queue.Refresh();

                PrintQueueStatus status = queue.QueueStatus;

                bool hasFatalStatus =
                    status.HasFlag(PrintQueueStatus.Paused) ||
                    status.HasFlag(PrintQueueStatus.Error) ||
                    status.HasFlag(PrintQueueStatus.PendingDeletion) ||
                    status.HasFlag(PrintQueueStatus.PaperJam) ||
                    status.HasFlag(PrintQueueStatus.PaperOut) ||
                    status.HasFlag(PrintQueueStatus.ManualFeed) ||
                    status.HasFlag(PrintQueueStatus.PaperProblem) ||
                    status.HasFlag(PrintQueueStatus.Offline) ||
                    status.HasFlag(PrintQueueStatus.NoToner) ||
                    status.HasFlag(PrintQueueStatus.NotAvailable) ||
                    status.HasFlag(PrintQueueStatus.OutputBinFull) ||
                    status.HasFlag(PrintQueueStatus.UserIntervention) ||
                    status.HasFlag(PrintQueueStatus.OutOfMemory);

                return !hasFatalStatus;
            }
            catch
            {
                return false;
            }
        }

        // ============================================================
        //  Helper: mm → pixel
        // ============================================================
        private static int Mm2Px(double mm, int dpi) =>
            (int)Math.Round(mm / 25.4 * dpi);
    }
}