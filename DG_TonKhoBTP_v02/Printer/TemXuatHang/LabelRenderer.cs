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
        private float _dpiX;
        private float _dpiY;

        private Image _logoSmall;   // logo GOLDCUP nhỏ (header trái + watermark)
        private Image _certLogo;    // ảnh chứng nhận ISO/QUACERT (header phải)
        private Image _kcsLogo;     // badge KCS PASSED (vùng sản phẩm phải)

        private readonly LabelPrintConfig _config;

        // ─── Layout heights (mm) — dùng nội bộ, nhất quán với nhau ───────────
        // Dòng BH code + ban hành
        private const float BhRowMm = 5f;
        // Đường kẻ đầu header
        private const float Sep1Mm = 0.5f;
        // Logo + tên công ty + ĐT/FAX
        private const float HeaderBodyMm = 18f;
        // Đường kẻ cuối header (굵)
        private const float Sep2Mm = 0.8f;
        // Vùng sản phẩm (Sản phẩm: + tên + Dự án:)
        private const float ProductMm = 30f;
        // Đường kẻ sau product
        private const float Sep3Mm = 0.5f;
        // Footer
        private const float FooterMm = 8.5f;

        // Tổng Y đầu data rows (tính từ inner.Y)
        private float DataStartOffsetMm =>
            BhRowMm + Sep1Mm + HeaderBodyMm + Sep2Mm + ProductMm + Sep3Mm;

        public LabelRenderer(LabelPrintConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            LoadImages();
        }

        // ═════════════════════════════════════════════════════════════════════
        // PUBLIC
        // ═════════════════════════════════════════════════════════════════════

        public void DrawLabel(Graphics g, LabelData data, RectangleF bounds)
        {
            _dpiX = g.DpiX;
            _dpiY = g.DpiY;

            g.SmoothingMode = SmoothingMode.HighQuality;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;

            g.SetClip(bounds);

            float pad = Mm2Px(LabelConstants.PaddingMm);
            RectangleF inner = new RectangleF(
                bounds.X + pad, bounds.Y + pad,
                bounds.Width - 2 * pad,
                bounds.Height - 2 * pad);

            DrawWatermark(g, bounds);
            DrawHeader(g, inner);
            DrawProductSection(g, inner, data);
            DrawDataRows(g, inner, data);
            DrawFooter(g, bounds);
            DrawBorder(g, bounds);

            g.ResetClip();
        }

        // ═════════════════════════════════════════════════════════════════════
        // 0. Viền ngoài
        // ═════════════════════════════════════════════════════════════════════

        private void DrawBorder(Graphics g, RectangleF b)
        {
            using (Pen pen = new Pen(Color.Black, 28f))
                g.DrawRectangle(pen, b.X, b.Y, b.Width - 1f, b.Height - 1f);
        }

        // ═════════════════════════════════════════════════════════════════════
        // 1. Watermark — logo GOLDCUP mờ chính giữa tem
        // ═════════════════════════════════════════════════════════════════════

        private void DrawWatermark(Graphics g, RectangleF bounds)
        {
            if (_logoSmall == null) return;

            float size = Mm2Px(LabelConstants.WatermarkSizeMm);
            float cx = bounds.X + (bounds.Width - size) / 2f - Mm2Px(12f); // dịch phải 5mm so với bản v8 (còn lệch trái 12mm)
            float cy = bounds.Y + (bounds.Height - size) / 2f + Mm2Px(20f); // lệch xuống 2cm

            using (ImageAttributes ia = new ImageAttributes())
            {
                ia.SetColorMatrix(BuildColorMatrix(_config.WatermarkOpacity, grayscale: false));
                DrawImageKeepAspect(g, _logoSmall,
                    new RectangleF(cx, cy, size, size),
                    StringAlignment.Center, StringAlignment.Center, ia);
            }
        }

        // ═════════════════════════════════════════════════════════════════════
        // 2. Header
        //    ┌─────────────────────────────────────────────────────────────┐
        //    │ BH-QC-13-58-02                        Ban hành: 13/12/2025 │
        //    ├─────────────────────────────────────────────────────────────┤
        //    │ [LOGO] │  CÔNG TY CP ĐÔNG GIANG  │ [CERT1][CERT2]         │
        //    │        │  Nhà máy: ...            │                        │
        //    │        │  ĐT: ...                 │                        │
        //    ├═════════════════════════════════════════════════════════════╡  (굵)
        // ═════════════════════════════════════════════════════════════════════

        private void DrawHeader(Graphics g, RectangleF inner)
        {
            float x = inner.X;
            float y = inner.Y;
            float w = inner.Width;

            // ── Dòng BH code / Ban hành ──────────────────────────────────────
            float bhH = Mm2Px(BhRowMm);
            using (GdiFont f = MakeFont(LabelConstants.FontSizeBhCode, bold: false))
            {
                DrawText(g, LabelConstants.BhCode, f, Brushes.Black,
                    new RectangleF(x, y, w * 0.5f, bhH), StringAlignment.Near, StringAlignment.Center);
                DrawText(g, _config.PublishedDate, f, Brushes.Black,
                    new RectangleF(x + w * 0.5f, y, w * 0.5f, bhH), StringAlignment.Far, StringAlignment.Center);
            }
            y += bhH;

            // Không vẽ gạch dài dưới mã BH theo mẫu in mới.
            y += Mm2Px(Sep1Mm);

            // ── Logo | Tên công ty | Cert ─────────────────────────────────────
            float hH = Mm2Px(HeaderBodyMm);
            float logoW = Mm2Px(9.45f);  // tăng 105% so với 9mm hiện tại
            float logoH = Mm2Px(9.45f);
            float certW = Mm2Px(16.5f);  // tăng 150% so với 11mm hiện tại
            float certH = Mm2Px(13.5f);
            float nameX = x + logoW + Mm2Px(2f);
            float nameW = w - logoW - certW - Mm2Px(3f);

            // Logo GOLDCUP nhỏ — đặt ngay dưới dòng BH-QC-13-58-02, giữ nguyên tỷ lệ ảnh
            RectangleF logoRect = new RectangleF(x, y, logoW, logoH);
            if (_logoSmall != null)
                DrawImageKeepAspect(g, _logoSmall, logoRect,
                    StringAlignment.Near, StringAlignment.Near);
            else
            {
                // Fallback: text "GOLDCUP"
                using (GdiFont f = MakeFont(7f, bold: true))
                    DrawText(g, "GOLDCUP", f, Brushes.Black, logoRect,
                        StringAlignment.Center, StringAlignment.Center);
            }

            // Cert logo — đặt ngay dưới dòng Ban hành, giữ nguyên tỷ lệ ảnh
            if (_certLogo != null)
            {
                RectangleF certRect = new RectangleF(x + w - certW, y, certW, certH);
                DrawImageKeepAspect(g, _certLogo, certRect,
                    StringAlignment.Far, StringAlignment.Near);
            }

            // Tên công ty + địa chỉ + ĐT/FAX/Website.
            // CompanyName dịch xuống 3mm so với bản v5; các dòng còn lại giữ đủ khoảng trống
            // để không đè lên đường kẻ đã được đưa lên 5mm.
            float headerTextY = y - Mm2Px(5f);
            float companyY = headerTextY + Mm2Px(3f);
            float companyH = hH * 0.32f;
            float addressY = companyY + companyH;
            float addressH = hH * 0.23f;
            float phoneY = addressY + addressH;
            float phoneH = hH * 0.23f;

            using (GdiFont f = MakeFont(LabelConstants.FontSizeCompanyName, bold: true))
                DrawText(g, LabelConstants.CompanyName, f, Brushes.Black,
                    new RectangleF(nameX, companyY, nameW, companyH),
                    StringAlignment.Center, StringAlignment.Center);

            // Địa chỉ — tăng 1 size
            using (GdiFont f = MakeFont(LabelConstants.FontSizeSubTitle + 1f, bold: false))
                DrawText(g, LabelConstants.FactoryAddress, f, Brushes.Black,
                    new RectangleF(nameX, addressY, nameW, addressH),
                    StringAlignment.Center, StringAlignment.Center);

            // ĐT / FAX / Website — tăng 1 size, không xuống dòng và không cắt bằng dấu ...
            using (GdiFont f = MakeFont(LabelConstants.FontSizeSubTitle + 1f, bold: false))
                DrawTextNoWrap(g, LabelConstants.PhoneFax, f, Brushes.Black,
                    new RectangleF(nameX, phoneY, nameW, phoneH),
                    StringAlignment.Center, StringAlignment.Center);

            y += hH;

            // ── Đường kẻ đậm cuối header — đặt lên trên 5mm so với bản v5 ─────
            DrawHLine(g, x, y - Mm2Px(5f), w, 4.5f);
        }

        // ═════════════════════════════════════════════════════════════════════
        // 3. Vùng sản phẩm
        //    "Sản phẩm:" căn giữa, đậm nghiêng
        //    Tên sản phẩm căn giữa, đậm
        //    KCS badge góc phải (tròn)
        //    "Dự án:" căn giữa, đậm nghiêng
        // ═════════════════════════════════════════════════════════════════════

        private void DrawProductSection(Graphics g, RectangleF inner, LabelData data)
        {
            float x = inner.X;
            float w = inner.Width;
            float y = inner.Y + Mm2Px(BhRowMm + Sep1Mm + HeaderBodyMm + Sep2Mm);
            float pH = Mm2Px(ProductMm);

            // KCS badge — góc phải, vuông tròn hoặc dùng ảnh
            float kcsSize = Mm2Px(18.15f);  // tăng 125% so với 14.52mm hiện tại
            float kcsX = x + w - kcsSize - Mm2Px(0.5f);
            float kcsY = y + Mm2Px(1f);

            if (_kcsLogo != null)
                DrawImageKeepAspect(g, _kcsLogo, new RectangleF(kcsX, kcsY, kcsSize, kcsSize),
                    StringAlignment.Center, StringAlignment.Center);
            else
                DrawFallbackKcsBadge(g, kcsX, kcsY, kcsSize);

            // "Sản phẩm:" / ProductType / "Dự án:" / Project
            // Dịch riêng cụm nội dung này lên trên 6mm.
            // Trong GDI+, trục Y tăng xuống dưới, nên muốn lên trên thì phải TRỪ 6mm khỏi tọa độ Y.
            float productTextShiftUpMm = 6f;
            float textBaseY = y - Mm2Px(productTextShiftUpMm);

            float textW = w - kcsSize - Mm2Px(2f);  // bỏ vùng KCS
            float labelH = Mm2Px(5.5f);

            // "Sản phẩm:"
            using (GdiFont f = MakeFont(LabelConstants.FontSizeLabel + 0.5f, bold: true, italic: true))
                DrawText(g, LabelConstants.LblProduct, f, Brushes.Black,
                    new RectangleF(x, textBaseY + Mm2Px(1f), textW, labelH),
                    StringAlignment.Center, StringAlignment.Center);

            // ProductType — căn giữa, đậm, tự động xuống dòng khi quá dài.
            // productTypeMaxH là ceiling để DrawString biết chỗ cắt/ellipsis,
            // nhưng projY sẽ dùng chiều cao thực đo được, không cứng theo ceiling này.
            float productTypeY = textBaseY + labelH + Mm2Px(1.5f);
            float productTypeMaxH = Mm2Px(13f);
            float measuredProductTypeH;

            using (GdiFont f = MakeFont(LabelConstants.FontSizeProductName + 0.5f, bold: true))
            using (StringFormat sf = new StringFormat())
            {
                sf.Alignment = StringAlignment.Center;
                sf.LineAlignment = StringAlignment.Near;
                sf.Trimming = StringTrimming.EllipsisWord;
                sf.FormatFlags = StringFormatFlags.LineLimit;

                string productType = data.ProductType ?? string.Empty;

                // Đo chiều cao thực tế GDI+ sẽ render (với đúng font + wrap width).
                // Clamp về ceiling để không vượt quá vùng tối đa cho phép.
                SizeF measured = g.MeasureString(productType, f, (int)textW, sf);
                measuredProductTypeH = Math.Min(measured.Height, productTypeMaxH);

                // Vẫn truyền productTypeMaxH vào DrawString để LineLimit + EllipsisWord hoạt động đúng.
                g.DrawString(productType, f, Brushes.Black,
                    new RectangleF(x, productTypeY, textW, productTypeMaxH), sf);
            }

            // "Dự án:" + giá trị — đặt ngay bên dưới phần ProductType thực tế,
            // không còn dùng khoảng cứng 13mm nữa.
            float productProjectGapMm = 1.5f;
            float projY = productTypeY + measuredProductTypeH + Mm2Px(productProjectGapMm);

            using (GdiFont fLabel = MakeFont(LabelConstants.FontSizeLabel + 0.5f, bold: true, italic: true))
                DrawText(g, LabelConstants.LblProject, fLabel, Brushes.Black,
                    new RectangleF(x, projY, textW, labelH),
                    StringAlignment.Center, StringAlignment.Center);

            if (!string.IsNullOrWhiteSpace(data.Project))
            {
                using (GdiFont fVal = MakeFont(LabelConstants.FontSizeValue, bold: true))
                    DrawText(g, data.Project, fVal, Brushes.Black,
                        new RectangleF(x, projY + labelH, textW, labelH),
                        StringAlignment.Center, StringAlignment.Center);
            }

            // Không vẽ gạch dưới nội dung dự án theo mẫu in mới.
        }

        // ── Fallback KCS badge khi không có ảnh ──────────────────────────────
        private void DrawFallbackKcsBadge(Graphics g, float x, float y, float size)
        {
            // Vòng tròn nền cam nhạt
            using (Brush bg = new SolidBrush(Color.FromArgb(255, 240, 200)))
                g.FillEllipse(bg, x, y, size, size);
            using (Pen pen = new Pen(Color.FromArgb(200, 120, 0), 1.5f))
                g.DrawEllipse(pen, x, y, size, size);

            float cx = x + size / 2f;
            using (GdiFont f0 = MakeFont(5f, bold: true))
                DrawText(g, "GOLDCUP", f0, Brushes.DarkOrange,
                    new RectangleF(x, y + size * 0.08f, size, size * 0.2f),
                    StringAlignment.Center, StringAlignment.Center);

            using (GdiFont f1 = MakeFont(11f, bold: true))
                DrawText(g, "1", f1, Brushes.Black,
                    new RectangleF(x, y + size * 0.18f, size * 0.35f, size * 0.45f),
                    StringAlignment.Center, StringAlignment.Center);

            using (GdiFont f2 = MakeFont(10f, bold: true))
                DrawText(g, "KCS", f2, Brushes.DarkBlue,
                    new RectangleF(x + size * 0.3f, y + size * 0.22f, size * 0.65f, size * 0.35f),
                    StringAlignment.Center, StringAlignment.Center);

            using (GdiFont f3 = MakeFont(4.5f, bold: false))
                DrawText(g, "✓ PASSED", f3, Brushes.DarkBlue,
                    new RectangleF(x, y + size * 0.60f, size, size * 0.2f),
                    StringAlignment.Center, StringAlignment.Center);
        }

        // ═════════════════════════════════════════════════════════════════════
        // 4. Data rows + QR Code
        //
        //   Mã sản phẩm:  [ E10-261743/7-01 ]          ┌──────────┐
        //   Chiều dài:              180 m               │          │
        //              ( 0000 ~ 0180 )                  │  QR Code │
        //   Khối lượng cáp:         730 Kg              │          │
        //   Khối lượng tổng:        945 Kg              └──────────┘
        //   ─────────────────────────────────────────────────────
        //   Ngày kiểm tra:                         23/04/2026
        //   Người kiểm tra:           Nguyễn Huy Toàn-DG112
        //   Đánh giá chất lượng:                        Đạt
        //   Tiêu chuẩn sản xuất:                  IEC 60502-1
        //                   Sản xuất tại Việt Nam
        // ═════════════════════════════════════════════════════════════════════

        private void DrawDataRows(Graphics g, RectangleF inner, LabelData data)
        {
            float x = inner.X;
            float w = inner.Width;
            float rowH = Mm2Px(LabelConstants.RowHeightMm);

            // QR — bên phải, bắt đầu từ dòng Chiều dài
            float qrSize = Mm2Px(LabelConstants.QrSizeMm);
            float qrX = x + w - qrSize - Mm2Px(LabelConstants.QrRightGapMm);
            float dataW = w - qrSize - Mm2Px(LabelConstants.QrRightGapMm + 1f);

            float y = inner.Y + Mm2Px(DataStartOffsetMm - 11f);
            float rowStep = rowH + Mm2Px(1.5f); // tăng giãn cách các dòng dữ liệu thêm 1mm so với bản v8

            // ── Dòng 1: Mã sản phẩm ──────────────────────────────────────────
            float lblCodeW = Mm2Px(30f);
            using (GdiFont fLabel = MakeFont(LabelConstants.FontSizeLabel + 3f, bold: true, italic: true))
            using (GdiFont fCode = MakeFont(LabelConstants.FontSizeProductCode + 3.5f, bold: true))
            {
                DrawText(g, LabelConstants.LblProductCode, fLabel, Brushes.Black,
                    new RectangleF(x, y, lblCodeW, rowH),
                    StringAlignment.Near, StringAlignment.Center);

                // Box quanh mã sản phẩm
                SizeF sz = g.MeasureString(data.ProductCode, fCode);
                float bPad = Mm2Px(2.5f);
                float bW = sz.Width + bPad * 2f;
                float bH = rowH * 0.88f;
                // Giá trị ProductCode giảm thêm 1 size và dịch trái 5mm; không để đè lên nhãn.
                float codeLeft = x + lblCodeW + Mm2Px(1f) - Mm2Px(5f);
                float minCodeLeft = x + g.MeasureString(LabelConstants.LblProductCode, fLabel).Width + Mm2Px(1f);
                float bX = Math.Max(codeLeft, minCodeLeft);
                float bY = y + (rowH - bH) / 2f;

                using (Pen pen = new Pen(Color.Black, 1.2f))
                    g.DrawRectangle(pen, bX, bY, bW, bH);

                DrawText(g, data.ProductCode, fCode, Brushes.Black,
                    new RectangleF(bX, bY, bW, bH),
                    StringAlignment.Center, StringAlignment.Center);
            }
            y += rowStep;

            // QR code — bắt đầu từ đây (dòng Chiều dài)
            float qrStartY = y;
            using (Bitmap qr = QrCodeService.GenerateQrBitmap(data.ProductCode, (int)qrSize))
            {
                if (qr != null)
                    g.DrawImage(qr, qrX, qrStartY, qrSize, qrSize);
            }

            // ── Dòng 2: Chiều dài ─────────────────────────────────────────────
            DrawDataRow(g, x, y, dataW, rowH,
                LabelConstants.LblLength, data.Length,
                labelFontSize: LabelConstants.FontSizeLabel + 3f,
                valueFontSize: LabelConstants.FontSizeValue + 7f,
                valueBold: true);
            y += rowStep;

            // ── Dòng 3: Dải chiều dài ─────────────────────────────────────────
            using (GdiFont fRange = MakeFont(LabelConstants.FontSizeLabel + 2f, bold: false))
                DrawText(g, data.LengthRange, fRange, Brushes.Black,
                    new RectangleF(x + Mm2Px(21f), y, dataW - Mm2Px(21f), rowH * 0.85f),
                    StringAlignment.Near, StringAlignment.Center);
            y += rowH * 0.85f + Mm2Px(2f);

            // ── Dòng 4: Khối lượng cáp ────────────────────────────────────────
            DrawDataRow(g, x, y, dataW, rowH,
                LabelConstants.LblCableWeight, data.CableWeight,
                labelFontSize: LabelConstants.FontSizeLabel + 3f,
                valueFontSize: LabelConstants.FontSizeValue + 4f,
                valueBold: true,
                valueLeftExpandMm: 5f);
            y += rowStep;

            // ── Dòng 5: Khối lượng tổng ───────────────────────────────────────
            DrawDataRow(g, x, y, dataW, rowH,
                LabelConstants.LblTotalWeight, data.TotalWeight,
                labelFontSize: LabelConstants.FontSizeLabel + 3f,
                valueFontSize: LabelConstants.FontSizeValue + 4f,
                valueBold: true,
                valueLeftExpandMm: 5f);
            y += rowStep;

            // Không vẽ gạch dưới nội dung khối lượng tổng theo mẫu in mới.
            y += Mm2Px(1f);

            // ── Dòng 6-9 dùng full width (QR đã kết thúc phía trên) ──────────

            // Dòng 6: Ngày kiểm tra
            DrawDataRow(g, x, y, w, rowH,
                LabelConstants.LblInspectionDate, data.InspectionDate,
                labelFontSize: LabelConstants.FontSizeLabel + 3f,
                valueFontSize: LabelConstants.FontSizeValue + 3f,
                valueBold: true,
                valueLeftExpandMm: 5f);
            y += rowStep;

            // Dòng 7: Người kiểm tra
            DrawDataRow(g, x, y, w, rowH,
                LabelConstants.LblInspector, data.Inspector,
                labelFontSize: LabelConstants.FontSizeLabel + 3f,
                valueFontSize: LabelConstants.FontSizeValue + 2f,
                valueBold: true,
                valueLeftExpandMm: 5f);
            y += rowStep;

            // Dòng 8: Đánh giá chất lượng
            DrawDataRow(g, x, y, w, rowH,
                LabelConstants.LblQuality, data.QualityResult,
                labelFontSize: LabelConstants.FontSizeLabel + 3f,
                valueFontSize: LabelConstants.FontSizeValue + 2f,
                valueBold: true,
                valueLeftExpandMm: 5f);
            y += rowStep;

            // Dòng 9: Tiêu chuẩn sản xuất
            DrawDataRow(g, x, y, w, rowH,
                LabelConstants.LblStandard, data.Standard,
                labelFontSize: LabelConstants.FontSizeLabel + 3f,
                valueFontSize: LabelConstants.FontSizeValue + 2f,
                valueBold: true,
                valueLeftExpandMm: 5f);
            y += rowStep;

            // "Sản xuất tại Việt Nam" — căn giữa, nghiêng
            // "Sản xuất tại Việt Nam" — nằm trên footer, cách footer 1mm
            float footerTopY =
                inner.Y
                + inner.Height
                + Mm2Px(LabelConstants.PaddingMm)
                - Mm2Px(FooterMm);

            float madeInGapToFooter = Mm2Px(1f);
            float madeInY = footerTopY - madeInGapToFooter - rowH;

            using (GdiFont f = MakeFont(LabelConstants.FontSizeSubTitle, italic: true))
            {
                DrawText(g, LabelConstants.MadeIn, f, Brushes.Black,
                    new RectangleF(x, madeInY, w, rowH),
                    StringAlignment.Center, StringAlignment.Center);
            }
        }

        // ═════════════════════════════════════════════════════════════════════
        // 5. Footer — nền đen full width
        //    ┌──────────┬────────────────────────────────────────┐
        //    │ GOLDCUP  │  WIRE AND CABLE  -  ISO 9001 : 2015   │
        //    └──────────┴────────────────────────────────────────┘
        // ═════════════════════════════════════════════════════════════════════

        private void DrawFooter(Graphics g, RectangleF bounds)
        {
            float fH = Mm2Px(FooterMm);
            float fY = bounds.Y + bounds.Height - fH;
            float fX = bounds.X;
            float fW = bounds.Width;

            // Nền footer giảm độ đậm so với nền đen cũ
            using (Brush footerBg = new SolidBrush(Color.FromArgb(165, 165, 165)))
                g.FillRectangle(footerBg, fX, fY, fW, fH);

            // Bỏ đường/ký tự phân cách giữa FooterLeft và FooterRight
            float divX = fX + Mm2Px(28f);

            // "GOLDCUP" — to, đậm, trắng
            using (GdiFont f = MakeFont(LabelConstants.FontSizeFooter, bold: true))
            {
                DrawTextExtraBold(g, LabelConstants.FooterLeft, f, Brushes.White,
                    new RectangleF(fX, fY, divX - fX, fH),
                    StringAlignment.Center, StringAlignment.Center,
                    strokePx: Math.Max(1f, Mm2Px(0.12f)));
            }

            // "WIRE AND CABLE — ISO 9001 : 2015" — trắng
            using (GdiFont f = MakeFont(LabelConstants.FontSizeFooter - 2f, bold: true))
                DrawText(g, LabelConstants.FooterRight, f, Brushes.White,
                    new RectangleF(divX + Mm2Px(2f), fY, fW - (divX - fX) - Mm2Px(2f), fH),
                    StringAlignment.Near, StringAlignment.Center);
        }

        // ═════════════════════════════════════════════════════════════════════
        // HELPERS — vẽ 1 dòng label + value
        // ═════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Vẽ 1 dòng: label căn trái (đậm nghiêng) + value căn phải.
        /// </summary>
        private void DrawDataRow(
            Graphics g,
            float x, float y, float width, float rowH,
            string label, string value,
            float labelFontSize = 0f,
            float valueFontSize = 0f,
            bool valueBold = true,
            float valueLeftExpandMm = 0f)
        {
            float lfs = labelFontSize > 0 ? labelFontSize : LabelConstants.FontSizeLabel;
            float vfs = valueFontSize > 0 ? valueFontSize : LabelConstants.FontSizeValue;

            // Label chiếm ~48% width, value phần còn lại căn phải.
            // valueLeftExpandMm mở rộng vùng giá trị về bên trái để tăng bề rộng hiển thị LabelData.
            float labelW = width * 0.48f;
            float valueExpand = Mm2Px(valueLeftExpandMm);
            float valueX = x + labelW - valueExpand;
            float valueW = width - labelW + valueExpand;

            using (GdiFont fLabel = MakeFont(lfs, bold: true, italic: true))
            using (GdiFont fValue = MakeFont(vfs, bold: valueBold))
            {
                DrawText(g, label, fLabel, Brushes.Black,
                    new RectangleF(x, y, labelW, rowH),
                    StringAlignment.Near, StringAlignment.Center);

                DrawText(g, value, fValue, Brushes.Black,
                    new RectangleF(valueX, y, valueW, rowH),
                    StringAlignment.Far, StringAlignment.Center);
            }
        }

        // ═════════════════════════════════════════════════════════════════════
        // HELPERS — vẽ text / line
        // ═════════════════════════════════════════════════════════════════════

        private void DrawImageKeepAspect(
            Graphics g,
            Image image,
            RectangleF target,
            StringAlignment hAlign = StringAlignment.Center,
            StringAlignment vAlign = StringAlignment.Center,
            ImageAttributes attributes = null)
        {
            if (image == null || image.Width <= 0 || image.Height <= 0) return;

            float scale = Math.Min(target.Width / image.Width, target.Height / image.Height);
            float drawW = image.Width * scale;
            float drawH = image.Height * scale;

            float drawX = target.X;
            if (hAlign == StringAlignment.Center)
                drawX += (target.Width - drawW) / 2f;
            else if (hAlign == StringAlignment.Far)
                drawX += target.Width - drawW;

            float drawY = target.Y;
            if (vAlign == StringAlignment.Center)
                drawY += (target.Height - drawH) / 2f;
            else if (vAlign == StringAlignment.Far)
                drawY += target.Height - drawH;

            RectangleF dest = new RectangleF(drawX, drawY, drawW, drawH);
            if (attributes == null)
            {
                g.DrawImage(image, dest);
            }
            else
            {
                Rectangle destInt = Rectangle.Round(dest);
                g.DrawImage(image, destInt,
                    0, 0, image.Width, image.Height,
                    GraphicsUnit.Pixel, attributes);
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

        private void DrawTextNoWrap(
            Graphics g, string text, GdiFont font, Brush brush,
            RectangleF rect,
            StringAlignment hAlign = StringAlignment.Near,
            StringAlignment vAlign = StringAlignment.Center)
        {
            if (string.IsNullOrEmpty(text)) return;

            // Không dùng EllipsisCharacter vì PhoneFax cần hiển thị đủ nội dung.
            // Nếu vùng vẽ không đủ rộng, tự giảm font theo cùng font family/style hiện tại.
            using (StringFormat sf = new StringFormat(StringFormat.GenericTypographic))
            {
                sf.Alignment = hAlign;
                sf.LineAlignment = vAlign;
                sf.Trimming = StringTrimming.None;
                sf.FormatFlags = StringFormatFlags.NoWrap;

                const float minFontSize = 4.0f;
                GdiFont fittedFont = null;

                for (float size = font.Size; size >= minFontSize; size -= 0.25f)
                {
                    GdiFont testFont = new GdiFont(font.FontFamily, size, font.Style, GraphicsUnit.Point);
                    SizeF measured = g.MeasureString(text, testFont, int.MaxValue, sf);

                    if (measured.Width <= rect.Width && measured.Height <= rect.Height * 1.25f)
                    {
                        fittedFont = testFont;
                        break;
                    }

                    testFont.Dispose();
                }

                if (fittedFont == null)
                    fittedFont = new GdiFont(font.FontFamily, minFontSize, font.Style, GraphicsUnit.Point);

                try
                {
                    g.DrawString(text, fittedFont, brush, rect, sf);
                }
                finally
                {
                    fittedFont.Dispose();
                }
            }
        }

        private void DrawHLine(Graphics g, float x, float y, float width, float thickness)
        {
            using (Pen pen = new Pen(Color.Black, thickness))
                g.DrawLine(pen, x, y, x + width, y);
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
        /// Tạo ColorMatrix tuỳ chọn: grayscale hoặc màu gốc, với opacity tuỳ chỉnh.
        /// </summary>
        private static ColorMatrix BuildColorMatrix(float opacity, bool grayscale = true)
        {
            if (grayscale)
            {
                float r = 0.299f, gr = 0.587f, b = 0.114f;
                return new ColorMatrix(new float[][]
                {
                    new float[] { r,  r,  r,  0,       0 },
                    new float[] { gr, gr, gr, 0,       0 },
                    new float[] { b,  b,  b,  0,       0 },
                    new float[] { 0,  0,  0,  opacity, 0 },
                    new float[] { 0,  0,  0,  0,       1 }
                });
            }
            else
            {
                // Giữ màu gốc, chỉ điều chỉnh opacity
                return new ColorMatrix(new float[][]
                {
                    new float[] { 1, 0, 0, 0,       0 },
                    new float[] { 0, 1, 0, 0,       0 },
                    new float[] { 0, 0, 1, 0,       0 },
                    new float[] { 0, 0, 0, opacity, 0 },
                    new float[] { 0, 0, 0, 0,       1 }
                });
            }
        }

        // ═════════════════════════════════════════════════════════════════════
        // HELPERS — convert & image loading
        // ═════════════════════════════════════════════════════════════════════

        /// <summary>Convert mm sang pixel theo DPI trục X.</summary>
        private float Mm2Px(float mm) => mm * _dpiX / 25.4f;

        private void LoadImages()
        {
            _logoSmall = LoadImage(_config.LogoSmallPath);
            _certLogo = LoadImage(_config.CertLogoPath);
            _kcsLogo = LoadImage(_config.KcsLogoPath);
        }

        /// <summary>
        /// Load ảnh từ đường dẫn. Tự resolve path tương đối từ thư mục exe.
        /// </summary>
        private static Image LoadImage(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return null;

            // Resolve relative path từ thư mục chứa exe
            if (!Path.IsPathRooted(path))
                path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);

            if (!File.Exists(path)) return null;
            try { return Image.FromFile(path); }
            catch { return null; }
        }

        private void DrawTextExtraBold(
            Graphics g, string text, GdiFont font, Brush brush,
            RectangleF rect,
            StringAlignment hAlign = StringAlignment.Near,
            StringAlignment vAlign = StringAlignment.Center,
            float strokePx = 1f)
        {
            if (string.IsNullOrEmpty(text)) return;

            // Vẽ chồng theo trục X để tăng độ dày chữ, không làm tăng chiều cao chữ.
            DrawText(g, text, font, brush,
                new RectangleF(rect.X - strokePx * 0.5f, rect.Y, rect.Width, rect.Height),
                hAlign, vAlign);

            DrawText(g, text, font, brush,
                rect,
                hAlign, vAlign);

            DrawText(g, text, font, brush,
                new RectangleF(rect.X + strokePx * 0.5f, rect.Y, rect.Width, rect.Height),
                hAlign, vAlign);
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