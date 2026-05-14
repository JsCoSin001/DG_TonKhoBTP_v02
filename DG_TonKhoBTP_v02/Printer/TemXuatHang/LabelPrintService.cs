using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        /// Nếu <see cref="LabelPrintConfig.ShowPreview"/> = true → mở PrintPreviewDialog.
        /// Ngược lại → in thẳng ra máy in.
        /// </summary>
        /// <param name="labels">Danh sách dữ liệu tem cần in. Không được null/rỗng.</param>
        /// <param name="config">Cấu hình in (đường dẫn ảnh, ngày ban hành...).</param>
        /// <param name="ownerForm">
        ///     Form cha cho PrintPreviewDialog (có thể null, dialog vẫn hiện).
        /// </param>
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

            // Tờ A4, landscape = false (dọc)
            doc.DefaultPageSettings.PaperSize = new PaperSize("A4", 827, 1169); // 1/100 inch
            doc.DefaultPageSettings.Landscape = false;

            // Margin = 0 để tem có thể chiếm toàn bộ tờ
            doc.DefaultPageSettings.Margins = new Margins(0, 0, 0, 0);

            doc.DocumentName = "In tem nhãn cáp - Goldcup";
            doc.PrintPage += OnPrintPage;

            return doc;
        }

        // ═════════════════════════════════════════════════════════════════════
        // PRIVATE — sự kiện PrintPage (gọi mỗi tờ)
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

            // Vị trí 4 ô tem trên tờ A4 (2 cột × 2 hàng)
            // ┌──────┬──────┐
            // │  [0] │  [1] │
            // ├──────┼──────┤
            // │  [2] │  [3] │
            // └──────┴──────┘
            RectangleF[] slots = new RectangleF[]
            {
                new RectangleF(0,          0,          labelWPx, labelHPx),  // top-left
                new RectangleF(labelWPx,   0,          labelWPx, labelHPx),  // top-right
                new RectangleF(0,          labelHPx,   labelWPx, labelHPx),  // bottom-left
                new RectangleF(labelWPx,   labelHPx,   labelWPx, labelHPx),  // bottom-right
            };

            int startIndex = _currentPageIndex * LabelConstants.LabelsPerPage;

            for (int i = 0; i < LabelConstants.LabelsPerPage; i++)
            {
                int dataIndex = startIndex + i;
                if (dataIndex >= _labels.Count) break;   // ô trống — bỏ qua

                _renderer.DrawLabel(g, _labels[dataIndex], slots[i]);
            }

            _currentPageIndex++;

            // Còn tem chưa in → báo còn trang tiếp
            e.HasMorePages = (_currentPageIndex * LabelConstants.LabelsPerPage) < _labels.Count;

            // Reset về tờ đầu nếu in xong (chuẩn bị cho lần Print() tiếp theo)
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
            => mm * dpi / 25.4f;

        // ═════════════════════════════════════════════════════════════════════
        // DISPOSE
        // ═════════════════════════════════════════════════════════════════════

        public void Dispose()
        {
            _renderer?.Dispose();
        }
    }
}
