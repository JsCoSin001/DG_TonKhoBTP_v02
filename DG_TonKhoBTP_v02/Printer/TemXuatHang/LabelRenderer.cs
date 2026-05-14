using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using GdiFont = System.Drawing.Font;
using GdiFontStyle = System.Drawing.FontStyle;

namespace DG_TonKhoBTP_v02.Printer.TemXuatHang
{
    public class LabelRenderer
    {
        // ── Hệ số convert mm → pixel ─────────────────────────────────────────
        // PrintDocument trả về Graphics với DPI = 100 (screen) hoặc 600 (printer).
        // Ta tính lại từ Graphics.DpiX để luôn chính xác.
        private float _dpiX;
        private float _dpiY;

        // ── Cache ảnh (load 1 lần, dùng nhiều tem) ───────────────────────────
        private Image _logoSmall;
        private Image _certLogo;
        private Image _kcsLogo;

        private readonly LabelPrintConfig _config;

        public LabelRenderer(LabelPrintConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            LoadImages();
        }

        // ═════════════════════════════════════════════════════════════════════
        // PUBLIC ENTRY POINT
        // ═════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Vẽ 1 tem hoàn chỉnh vào vùng <paramref name="bounds"/> trên <paramref name="g"/>.
        /// </summary>
        /// <param name="g">Graphics của PrintDocument (hoặc Preview).</param>
        /// <param name="data">Dữ liệu động của tem này.</param>
        /// <param name="bounds">Vùng hình chữ nhật dành cho tem (pixel).</param>
        public void DrawLabel(Graphics g, LabelData data, RectangleF bounds)
        {
            // Lưu DPI để convert mm→px chính xác
            _dpiX = g.DpiX;
            _dpiY = g.DpiY;

            // Clip để không vẽ tràn sang tem bên cạnh
            g.SetClip(bounds);

            float pad = Mm2Px(LabelConstants.PaddingMm);
            RectangleF inner = new RectangleF(
                bounds.X + pad, bounds.Y + pad,
                bounds.Width - 2 * pad, bounds.Height - 2 * pad);

            DrawBorder(g, bounds);
            DrawWatermark(g, bounds);
            DrawHeader(g, inner);
            DrawProductSection(g, inner, data);
            DrawDataRows(g, inner, data);
            DrawFooter(g, bounds, inner);

            g.ResetClip();
        }

        // ═════════════════════════════════════════════════════════════════════
        // PRIVATE — từng vùng vẽ
        // ═════════════════════════════════════════════════════════════════════

        // ── 0. Viền ngoài tem ────────────────────────────────────────────────
        private void DrawBorder(Graphics g, RectangleF bounds)
        {
            using (Pen pen = new Pen(LabelConstants.ColorBorder, 1f))
                g.DrawRectangle(pen, bounds.X, bounds.Y, bounds.Width - 1, bounds.Height - 1);
        }

        // ── 1. Watermark logo lớn mờ ở giữa ─────────────────────────────────
        private void DrawWatermark(Graphics g, RectangleF bounds)
        {
            if (_logoSmall == null) return;

            float size = Mm2Px(LabelConstants.WatermarkSizeMm);
            float cx = bounds.X + (bounds.Width - size) / 2f;
            float cy = bounds.Y + (bounds.Height - size) / 2f;

            ColorMatrix cm = new ColorMatrix { Matrix33 = _config.WatermarkOpacity };
            using (ImageAttributes ia = new ImageAttributes())
            {
                ia.SetColorMatrix(cm, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
                // Convert sang grayscale + opacity
                ia.SetColorMatrix(BuildGrayscaleMatrix(_config.WatermarkOpacity));

                g.DrawImage(_logoSmall,
                    new Rectangle((int)cx, (int)cy, (int)size, (int)size),
                    0, 0, _logoSmall.Width, _logoSmall.Height,
                    GraphicsUnit.Pixel, ia);
            }
        }

        // ── 2. Header ─────────────────────────────────────────────────────────
        private void DrawHeader(Graphics g, RectangleF inner)
        {
            float x = inner.X;
            float y = inner.Y;
            float w = inner.Width;

            // ─ Dòng 0: BH-QC-13-58-02 (trái) | Ban hành: ... (phải) ─────────
            float bhH = Mm2Px(4f);
            using (GdiFont f = MakeFont(LabelConstants.FontSizeFooter - 2f, false))
            {
                // BH code - trái
                DrawText(g, LabelConstants.BhCode, f, Brushes.Black,
                    new RectangleF(x, y, w * 0.5f, bhH), StringAlignment.Near);

                // Ban hành - phải
                DrawText(g, _config.PublishedDate, f, Brushes.Black,
                    new RectangleF(x + w * 0.5f, y, w * 0.5f, bhH), StringAlignment.Far);
            }
            y += bhH;

            // ─ Đường kẻ phân cách ────────────────────────────────────────────
            DrawHLine(g, inner.X, y, w, 0.5f);
            y += Mm2Px(0.5f);

            // ─ Dòng 1: logo nhỏ | tên công ty | cert logo ────────────────────
            float headerH = Mm2Px(9f);

            // Logo nhỏ (trái)
            float logoW = Mm2Px(12f);
            float logoH = Mm2Px(9f);
            if (_logoSmall != null)
                DrawImageGrayscale(g, _logoSmall,
                    new RectangleF(x, y, logoW, logoH));

            // Cert logo (phải)
            float certW = Mm2Px(16f);
            float certH = Mm2Px(9f);
            if (_certLogo != null)
                DrawImageGrayscale(g, _certLogo,
                    new RectangleF(x + w - certW, y, certW, certH));

            // Tên công ty (giữa)
            float nameX = x + logoW + Mm2Px(1f);
            float nameW = w - logoW - certW - Mm2Px(2f);
            using (GdiFont f = MakeFont(LabelConstants.FontSizeCompanyName, bold: true))
                DrawText(g, LabelConstants.CompanyName, f, Brushes.Black,
                    new RectangleF(nameX, y, nameW, headerH * 0.55f),
                    StringAlignment.Center, StringAlignment.Center);

            // Địa chỉ nhà máy
            using (GdiFont f = MakeFont(LabelConstants.FontSizeSubTitle - 0.5f, false))
                DrawText(g, LabelConstants.FactoryAddress, f, Brushes.Black,
                    new RectangleF(nameX, y + headerH * 0.52f, nameW, headerH * 0.28f),
                    StringAlignment.Center);

            // ĐT / FAX / Website
            using (GdiFont f = MakeFont(LabelConstants.FontSizeSubTitle - 0.5f, false))
                DrawText(g, LabelConstants.PhoneFax, f, Brushes.Black,
                    new RectangleF(nameX, y + headerH * 0.78f, nameW, headerH * 0.25f),
                    StringAlignment.Center);

            y += headerH;

            // ─ Đường kẻ phân cách cuối header ────────────────────────────────
            DrawHLine(g, inner.X, y, w, 1f);
        }

        // ── 3. Vùng sản phẩm + KCS badge ─────────────────────────────────────
        private void DrawProductSection(Graphics g, RectangleF inner, LabelData data)
        {
            float topOfSection = GetProductSectionY(inner);
            float x = inner.X;
            float w = inner.Width;
            float pH = Mm2Px(LabelConstants.ProductAreaHeightMm);

            // Nhãn "Sản phẩm:" in đậm trái
            float labelW = Mm2Px(20f);
            float lineH = Mm2Px(5f);
            using (GdiFont f = MakeFont(LabelConstants.FontSizeLabel, bold: true, italic: true))
                DrawText(g, LabelConstants.LblProduct, f, Brushes.Black,
                    new RectangleF(x, topOfSection, labelW, lineH),
                    StringAlignment.Near);

            // KCS badge góc phải (chiếm 18mm x 18mm)
            float kcsSize = Mm2Px(18f);
            float kcsX = x + w - kcsSize - Mm2Px(1f);
            float kcsY = topOfSection;
            if (_kcsLogo != null)
                DrawImageGrayscale(g, _kcsLogo,
                    new RectangleF(kcsX, kcsY, kcsSize, kcsSize));

            // Nội dung sản phẩm (giữa — tránh KCS)
            float prodW = w - kcsSize - Mm2Px(3f);
            using (GdiFont f = MakeFont(LabelConstants.FontSizeProductName, bold: true))
            {
                var sf = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Near,
                    Trimming = StringTrimming.None,
                    FormatFlags = StringFormatFlags.NoClip
                };
                g.DrawString(data.ProductType, f, Brushes.Black,
                    new RectangleF(x, topOfSection + lineH, prodW, Mm2Px(12f)), sf);
            }

            // Nhãn "Dự án:" + giá trị
            float projY = topOfSection + Mm2Px(14f);
            using (GdiFont fLabel = MakeFont(LabelConstants.FontSizeLabel, bold: true, italic: true))
            using (GdiFont fValue = MakeFont(LabelConstants.FontSizeLabel, bold: false))
            {
                DrawText(g, LabelConstants.LblProject, fLabel, Brushes.Black,
                    new RectangleF(x, projY, labelW, lineH), StringAlignment.Near);
                DrawText(g, data.Project, fValue, Brushes.Black,
                    new RectangleF(x + labelW, projY, prodW - labelW, lineH),
                    StringAlignment.Near);
            }

            // Đường kẻ phân cách sau vùng sản phẩm
            float sepY = topOfSection + pH;
            DrawHLine(g, x, sepY, w, 0.5f);
        }

        // ── 4. Các dòng dữ liệu + QR Code ────────────────────────────────────
        private void DrawDataRows(Graphics g, RectangleF inner, LabelData data)
        {
            float x = inner.X;
            float w = inner.Width;
            float rowH = Mm2Px(LabelConstants.RowHeightMm);
            float qrSize = Mm2Px(LabelConstants.QrSizeMm);
            float qrX = x + w - qrSize - Mm2Px(LabelConstants.QrRightGapMm);
            float dataW = w - qrSize - Mm2Px(LabelConstants.QrRightGapMm + 1f); // chiều rộng vùng text

            float y = GetDataRowsStartY(inner);

            // ── Dòng 1: Mã sản phẩm (KHÔNG có QR ở dòng này) ─────────────────
            float lblCodeW = Mm2Px(22f);
            using (GdiFont fLabel = MakeFont(LabelConstants.FontSizeLabel, bold: true, italic: true))
            using (GdiFont fValue = MakeFont(LabelConstants.FontSizeProductCode, bold: true))
            {
                // Nhãn "Mã sản phẩm:"
                DrawText(g, LabelConstants.LblProductCode, fLabel, Brushes.Black,
                    new RectangleF(x, y, lblCodeW, rowH), StringAlignment.Near);

                // Tính chiều rộng box theo nội dung
                SizeF codeSize = g.MeasureString(data.ProductCode, fValue);
                float boxPad = Mm2Px(2f);
                float boxW = codeSize.Width + boxPad * 2;
                float boxH = rowH * 0.85f;
                float boxX = x + lblCodeW + Mm2Px(1f);
                float boxY = y + (rowH - boxH) / 2f;

                // Vẽ viền box
                using (Pen pen = new Pen(Color.Black, 1f))
                    g.DrawRectangle(pen, boxX, boxY, boxW, boxH);

                // Vẽ text mã SP trong box
                DrawText(g, data.ProductCode, fValue, Brushes.Black,
                    new RectangleF(boxX, boxY, boxW, boxH),
                    StringAlignment.Center, StringAlignment.Center);
            }
            y += rowH;

            // ── Từ đây QR bắt đầu (cùng dòng với Chiều dài) ─────────────────
            float qrStartY = y;

            // Vẽ QR code
            using (Bitmap qrBmp = QrCodeService.GenerateQrBitmap(data.ProductCode, (int)qrSize))
            {
                if (qrBmp != null)
                    g.DrawImage(qrBmp, qrX, qrStartY, qrSize, qrSize);
            }

            // ── Dòng 2: Chiều dài ─────────────────────────────────────────────
            DrawDataRow(g, x, y, dataW, rowH,
                LabelConstants.LblLength, data.Length,
                valueFontSize: LabelConstants.FontSizeValue + 1f,
                valueBold: true);
            y += rowH;

            // ── Dòng 3: Dải chiều dài ( 0000 ~ 0180 ) ────────────────────────
            using (GdiFont fRange = MakeFont(LabelConstants.FontSizeLabel, false))
                DrawText(g, data.LengthRange, fRange, Brushes.Black,
                    new RectangleF(x + Mm2Px(5f), y, dataW - Mm2Px(5f), rowH * 0.8f),
                    StringAlignment.Near);
            y += rowH * 0.8f;

            // ── Dòng 4: Khối lượng cáp ────────────────────────────────────────
            DrawDataRow(g, x, y, dataW, rowH,
                LabelConstants.LblCableWeight, data.CableWeight);
            y += rowH;

            // ── Dòng 5: Khối lượng tổng ───────────────────────────────────────
            DrawDataRow(g, x, y, dataW, rowH,
                LabelConstants.LblTotalWeight, data.TotalWeight);
            y += rowH;

            // ── Đường kẻ phân cách ────────────────────────────────────────────
            DrawHLine(g, x, y, w, 0.3f);
            y += Mm2Px(0.8f);

            // ── Dòng 6: Ngày kiểm tra ─────────────────────────────────────────
            DrawDataRow(g, x, y, w, rowH,
                LabelConstants.LblInspectionDate, data.InspectionDate);
            y += rowH;

            // ── Dòng 7: Người kiểm tra ────────────────────────────────────────
            DrawDataRow(g, x, y, w, rowH,
                LabelConstants.LblInspector, data.Inspector);
            y += rowH;

            // ── Dòng 8: Đánh giá chất lượng ──────────────────────────────────
            DrawDataRow(g, x, y, w, rowH,
                LabelConstants.LblQuality, data.QualityResult,
                valueBold: true);
            y += rowH;

            // ── Dòng 9: Tiêu chuẩn sản xuất ──────────────────────────────────
            DrawDataRow(g, x, y, w, rowH,
                LabelConstants.LblStandard, data.Standard);
            y += rowH;

            // ── Sản xuất tại Việt Nam ─────────────────────────────────────────
            using (GdiFont f = MakeFont(LabelConstants.FontSizeSubTitle, italic: true))
                DrawText(g, LabelConstants.MadeIn, f, Brushes.Black,
                    new RectangleF(x, y, w, rowH * 0.8f), StringAlignment.Center);
        }

        // ── 5. Footer GOLDCUP | WIRE AND CABLE ───────────────────────────────
        private void DrawFooter(Graphics g, RectangleF bounds, RectangleF inner)
        {
            float fH = Mm2Px(LabelConstants.FooterHeightMm);
            float fY = bounds.Y + bounds.Height - fH;
            float fX = bounds.X;
            float fW = bounds.Width;

            // Nền đen full width
            g.FillRectangle(Brushes.Black, fX, fY, fW, fH);

            // Đường kẻ trắng phân cách giữa "GOLDCUP" và "WIRE AND CABLE"
            float divX = fX + Mm2Px(22f);
            using (Pen pen = new Pen(Color.White, 1f))
                g.DrawLine(pen, divX, fY + Mm2Px(1f), divX, fY + fH - Mm2Px(1f));

            // "GOLDCUP" bên trái
            using (GdiFont f = MakeFont(LabelConstants.FontSizeFooter, bold: true))
                DrawText(g, LabelConstants.FooterLeft, f, Brushes.White,
                    new RectangleF(fX, fY, divX - fX, fH),
                    StringAlignment.Center, StringAlignment.Center);

            // "WIRE AND CABLE — ISO 9001 : 2015" bên phải
            using (GdiFont f = MakeFont(LabelConstants.FontSizeFooter - 1f, bold: true))
                DrawText(g, LabelConstants.FooterRight, f, Brushes.White,
                    new RectangleF(divX + Mm2Px(1f), fY, fW - divX + fX - Mm2Px(1f), fH),
                    StringAlignment.Near, StringAlignment.Center);
        }

        // ═════════════════════════════════════════════════════════════════════
        // HELPERS — vị trí Y các vùng
        // ═════════════════════════════════════════════════════════════════════

        private float GetProductSectionY(RectangleF inner)
        {
            // Sau: bhCode (4mm) + separator (0.5mm) + header (9mm) + sep line (1mm)
            return inner.Y
                + Mm2Px(4f + 0.5f + 9f + 0.5f);
        }

        private float GetDataRowsStartY(RectangleF inner)
        {
            return GetProductSectionY(inner)
                + Mm2Px(LabelConstants.ProductAreaHeightMm);
        }

        // ═════════════════════════════════════════════════════════════════════
        // HELPERS — vẽ
        // ═════════════════════════════════════════════════════════════════════

        /// <summary>Vẽ 1 dòng: label (trái, italic bold) + value (phải/cuối).</summary>
        private void DrawDataRow(
            Graphics g,
            float x, float y, float width, float rowH,
            string label, string value,
            float labelFontSize = 0f,
            float valueFontSize = 0f,
            bool valueBold = false)
        {
            float lfs = labelFontSize > 0 ? labelFontSize : LabelConstants.FontSizeLabel;
            float vfs = valueFontSize > 0 ? valueFontSize : LabelConstants.FontSizeValue;

            float labelW = width * 0.55f;   // 55% cho nhãn

            using (GdiFont fLabel = MakeFont(lfs, bold: true, italic: true))
            using (GdiFont fValue = MakeFont(vfs, bold: valueBold))
            {
                DrawText(g, label, fLabel, Brushes.Black,
                    new RectangleF(x, y, labelW, rowH), StringAlignment.Near);

                DrawText(g, value, fValue, Brushes.Black,
                    new RectangleF(x + labelW, y, width - labelW, rowH),
                    StringAlignment.Far);
            }
        }

        private void DrawText(
            Graphics g, string text, GdiFont font, Brush brush,
            RectangleF rect,
            StringAlignment hAlign = StringAlignment.Near,
            StringAlignment vAlign = StringAlignment.Center)
        {
            if (string.IsNullOrEmpty(text)) return;
            using (StringFormat sf = new StringFormat())
            {
                sf.Alignment = hAlign;
                sf.LineAlignment = vAlign;
                sf.Trimming = StringTrimming.EllipsisCharacter;
                g.DrawString(text, font, brush, rect, sf);
            }
        }

        private void DrawHLine(Graphics g, float x, float y, float width, float thickness)
        {
            using (Pen pen = new Pen(Color.Black, thickness))
                g.DrawLine(pen, x, y, x + width, y);
        }

        private void DrawImageGrayscale(Graphics g, Image img, RectangleF dest)
        {
            using (ImageAttributes ia = new ImageAttributes())
            {
                ia.SetColorMatrix(BuildGrayscaleMatrix(1f));
                g.DrawImage(img,
                    new Rectangle((int)dest.X, (int)dest.Y, (int)dest.Width, (int)dest.Height),
                    0, 0, img.Width, img.Height,
                    GraphicsUnit.Pixel, ia);
            }
        }

        // ═════════════════════════════════════════════════════════════════════
        // HELPERS — font & color matrix
        // ═════════════════════════════════════════════════════════════════════

        private GdiFont MakeFont(float sizePoint, bool bold = false, bool italic = false)
        {
            GdiFontStyle style = GdiFontStyle.Regular;
            if (bold) style |= GdiFontStyle.Bold;
            if (italic) style |= GdiFontStyle.Italic;
            return new GdiFont(LabelConstants.FontDefault, sizePoint, style, GraphicsUnit.Point);
        }

        /// <summary>
        /// Tạo ColorMatrix chuyển ảnh về grayscale với opacity tuỳ chỉnh.
        /// </summary>
        private static ColorMatrix BuildGrayscaleMatrix(float opacity)
        {
            // Grayscale: R=G=B = 0.299R + 0.587G + 0.114B
            float r = 0.299f, gr = 0.587f, b = 0.114f;
            return new ColorMatrix(new float[][]
            {
                new float[] { r,  r,  r,  0,  0 },
                new float[] { gr, gr, gr, 0,  0 },
                new float[] { b,  b,  b,  0,  0 },
                new float[] { 0,  0,  0,  opacity, 0 },
                new float[] { 0,  0,  0,  0,  1 }
            });
        }

        // ═════════════════════════════════════════════════════════════════════
        // HELPERS — convert & image loading
        // ═════════════════════════════════════════════════════════════════════

        /// <summary>Convert mm sang pixel theo DPI của Graphics hiện tại (trục X).</summary>
        private float Mm2Px(float mm) => mm * _dpiX / 25.4f;

        private void LoadImages()
        {
            _logoSmall = LoadImage(_config.LogoSmallPath);
            _certLogo = LoadImage(_config.CertLogoPath);
            _kcsLogo = LoadImage(_config.KcsLogoPath);
        }

        private static Image LoadImage(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return null;
            try { return Image.FromFile(path); }
            catch { return null; }
        }

        // ═════════════════════════════════════════════════════════════════════
        // DISPOSE
        // ═════════════════════════════════════════════════════════════════════

        public void Dispose()
        {
            _logoSmall?.Dispose();
            _certLogo?.Dispose();
            _kcsLogo?.Dispose();
        }
    }
}
