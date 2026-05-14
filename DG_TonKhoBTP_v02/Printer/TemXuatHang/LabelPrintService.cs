using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;

namespace DG_TonKhoBTP_v02.Printer.TemXuatHang
{
    public class LabelPrintService : IDisposable
    {
        // ── State dùng trong PrintPage callback ──────────────────────────────
        private List<LabelData> _labels;
        private LabelPrintConfig _config;
        private LabelRenderer _renderer;
        private int _currentPageIndex;   // tờ hiện tại (0-based)

        // ── Kích thước tem (mm) → dùng tính layout ───────────────────────────
        private const float LabelWMm = LabelConstants.LabelWidthMm;
        private const float LabelHMm = LabelConstants.LabelHeightMm;

        // ═════════════════════════════════════════════════════════════════════
        // PUBLIC API
        // ═════════════════════════════════════════════════════════════════════

        /// <summary>
        /// In danh sách tem. Mỗi phần tử = 1 tem. 4 tem/tờ A4.
        /// Nếu LabelPrintConfig.ShowPreview = true → mở PrintPreviewDialog.
        /// Ngược lại → in thẳng ra máy in.
        /// </summary>
        public void Print(
            List<LabelData> labels,
            LabelPrintConfig config,
            Form ownerForm = null)
        {
            if (labels == null || labels.Count == 0)
                throw new ArgumentException("Danh sách tem không được rỗng.", nameof(labels));

            if (config == null)
                throw new ArgumentNullException(nameof(config));

            _labels = labels;
            _config = config;
            _currentPageIndex = 0;
            _renderer = new LabelRenderer(config);

            using (PrintDocument doc = BuildPrintDocument(config))
            {
                if (config.ShowPreview)
                    ShowPreview(doc, ownerForm);
                else
                    doc.Print();
            }
        }

        // ═════════════════════════════════════════════════════════════════════
        // PRIVATE — cấu hình PrintDocument
        // ═════════════════════════════════════════════════════════════════════

        private PrintDocument BuildPrintDocument(LabelPrintConfig config)
        {
            var doc = new PrintDocument();

            // Máy in
            if (!string.IsNullOrWhiteSpace(config.PrinterName))
                doc.PrinterSettings.PrinterName = config.PrinterName;

            // Tờ A4, portrait
            doc.DefaultPageSettings.PaperSize = new PaperSize("A4", 827, 1169); // 1/100 inch
            doc.DefaultPageSettings.Landscape = false;

            // Margin = 0.
            // Lưu ý: máy in thật vẫn có vùng không in được ở mép giấy.
            doc.DefaultPageSettings.Margins = new Margins(0, 0, 0, 0);

            doc.DocumentName = "In tem nhãn cáp - Goldcup";
            doc.PrintPage += OnPrintPage;

            return doc;
        }

        // ═════════════════════════════════════════════════════════════════════
        // PRIVATE — sự kiện PrintPage
        // ═════════════════════════════════════════════════════════════════════

        private void OnPrintPage(object sender, PrintPageEventArgs e)
        {
            Graphics g = e.Graphics;
            g.PageUnit = GraphicsUnit.Pixel;
            g.PageScale = 1f;

            float dpiX = g.DpiX;
            float dpiY = g.DpiY;

            // Convert mm → pixel
            float labelWPx = MmToPx(LabelWMm, dpiX);
            float labelHPx = MmToPx(LabelHMm, dpiY);

            float cellWPx = MmToPx(LabelConstants.A4WidthMm / LabelConstants.LabelsPerRow, dpiX);
            float cellHPx = MmToPx(LabelConstants.A4HeightMm / LabelConstants.LabelsPerCol, dpiY);

            float horizontalInsetPx = Math.Max(0, (cellWPx - labelWPx) / 2f);
            float verticalInsetPx = Math.Max(0, (cellHPx - labelHPx) / 2f);

            // Giảm khoảng cách ngang giữa 2 tem:
            // cột trái dịch phải 1.5mm, cột phải dịch trái 1.5mm.
            float horizontalGapAdjustPx = MmToPx(1.5f, dpiX);

            // Dịch toàn bộ nội dung in sang trái để item 2 và item 4
            // không bị rơi vào vùng không in được ở mép phải máy in.
            // Do mép trái thực tế còn khoảng 11mm, dịch 5mm là mức an toàn.
            float wholePageLeftShiftPx = MmToPx(5f, dpiX);

            RectangleF[] slots = new RectangleF[]
            {
                // Item 1 — trên trái
                new RectangleF(
                    horizontalInsetPx + horizontalGapAdjustPx - wholePageLeftShiftPx,
                    verticalInsetPx,
                    labelWPx,
                    labelHPx
                ),

                // Item 2 — trên phải
                new RectangleF(
                    cellWPx + horizontalInsetPx - horizontalGapAdjustPx - wholePageLeftShiftPx,
                    verticalInsetPx,
                    labelWPx,
                    labelHPx
                ),

                // Item 3 — dưới trái
                new RectangleF(
                    horizontalInsetPx + horizontalGapAdjustPx - wholePageLeftShiftPx,
                    cellHPx + verticalInsetPx,
                    labelWPx,
                    labelHPx
                ),

                // Item 4 — dưới phải
                new RectangleF(
                    cellWPx + horizontalInsetPx - horizontalGapAdjustPx - wholePageLeftShiftPx,
                    cellHPx + verticalInsetPx,
                    labelWPx,
                    labelHPx
                ),
            };

            int startIndex = _currentPageIndex * LabelConstants.LabelsPerPage;

            for (int i = 0; i < LabelConstants.LabelsPerPage; i++)
            {
                int dataIndex = startIndex + i;

                if (dataIndex >= _labels.Count)
                    break;

                _renderer.DrawLabel(g, _labels[dataIndex], slots[i]);
            }

            _currentPageIndex++;

            e.HasMorePages = (_currentPageIndex * LabelConstants.LabelsPerPage) < _labels.Count;

            if (!e.HasMorePages)
                _currentPageIndex = 0;
        }

        // ═════════════════════════════════════════════════════════════════════
        // PRIVATE — PrintPreview
        // ═════════════════════════════════════════════════════════════════════

        private void ShowPreview(PrintDocument doc, Form owner)
        {
            using (var preview = new PrintPreviewDialog())
            {
                preview.Document = doc;
                preview.Text = "Xem trước tem nhãn — Goldcup";
                preview.WindowState = FormWindowState.Maximized;
                preview.ShowDialog(owner);
            }
        }

        // ═════════════════════════════════════════════════════════════════════
        // HELPER
        // ═════════════════════════════════════════════════════════════════════

        private static float MmToPx(float mm, float dpi)
        {
            return mm * dpi / 25.4f;
        }

        // ═════════════════════════════════════════════════════════════════════
        // DISPOSE
        // ═════════════════════════════════════════════════════════════════════

        public void Dispose()
        {
            _renderer?.Dispose();
        }
    }
}