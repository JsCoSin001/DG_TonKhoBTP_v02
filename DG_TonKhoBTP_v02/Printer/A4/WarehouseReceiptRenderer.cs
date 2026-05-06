// File: WarehouseReceiptRenderer.cs
// Thư mục: Printer/A4/

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using static DG_TonKhoBTP_v02.Printer.A4.PrinterModel;

namespace DG_TonKhoBTP_v02.Printer.A4
{
    public class WarehouseReceiptRenderer
        : BaseRenderer<WarehouseReceiptPrintData, WarehouseReceiptItem>
    {
        // ── Trạng thái phân trang ────────────────────────────────────────────
        private int _startItemIndex = 0;
        private bool _headerDrawn = false;

        // Chiều cao dự trữ cuối trang: dòng Cộng + chữ ký
        private const int BottomReserve = 180;

        // ── Abstract overrides ───────────────────────────────────────────────
        protected override CompanyInfo GetCompanyInfo(WarehouseReceiptPrintData d) => d.Company;
        protected override IReadOnlyList<WarehouseReceiptItem> GetItems(WarehouseReceiptPrintData d) => d.Items;
        protected override SignatureInfo GetSignature(WarehouseReceiptPrintData d) => d.Signature;
        protected override DocumentInfo GetDocumentInfo(WarehouseReceiptPrintData d) => new DocumentInfo();
        protected override IReadOnlyList<ColumnDef<WarehouseReceiptItem>> BuildColumns(int totalWidth)
            => Array.Empty<ColumnDef<WarehouseReceiptItem>>();

        // ── OnReset: được gọi tự động bởi BaseRenderer.Reset() ─────────────
        protected override void OnReset()
        {
            _startItemIndex = 0;
            _headerDrawn = false;
        }

        // ════════════════════════════════════════════════════════════════════
        // Render — vẽ một trang, đặt e.HasMorePages nếu còn dữ liệu
        // ════════════════════════════════════════════════════════════════════
        public override void Render(Graphics g, Rectangle marginBounds, PrintPageEventArgs e,
            WarehouseReceiptPrintData data)
        {
            g.TextRenderingHint = TextRenderingHint.AntiAlias;

            int x = marginBounds.Left;
            int y = marginBounds.Top;
            int width = marginBounds.Width;
            int bottom = marginBounds.Bottom;

            // ── Trang đầu: header công ty + tiêu đề + thông tin phiếu ───────
            if (!_headerDrawn)
            {
                DrawHeaderBlock(g, ref y, x, width, data);
                DrawTitleBlock(g, ref y, x, width, data);
                DrawInfoBlock(g, ref y, x, width, data);
                y += 8;
                _headerDrawn = true;
            }
            else
            {
                y += 10;
            }

            // ── Vẽ header bảng ───────────────────────────────────────────────
            int[] colW = BuildColumnWidths(width);
            int[] colX = BuildColumnX(x, colW);
            DrawTableHeader(g, ref y, colW, colX);

            // ── Vẽ từng dòng dữ liệu ────────────────────────────────────────
            var items = data.Items ?? new List<WarehouseReceiptItem>();
            bool hasMore = false;

            for (int i = _startItemIndex; i < items.Count; i++)
            {
                var item = items[i];
                int rowH = 28;
                rowH = Math.Max(rowH, MeasureCellTextHeight(g, item.Ten, FontNormal, colW[1] - 8) + 8);
                rowH = Math.Max(rowH, MeasureCellTextHeight(g, item.GhiChu, FontNormal, colW[6] - 8) + 8);

                // Kiểm tra còn đủ chỗ không
                if (y + rowH + BottomReserve > bottom)
                {
                    _startItemIndex = i;
                    hasMore = true;
                    break;
                }

                DrawReceiptRow(g, item, colW, colX, ref y, rowH);
            }

            // ── Trang cuối: dòng Cộng + chữ ký ─────────────────────────────
            if (!hasMore)
            {
                DrawTotalRow(g, ref y, colW, colX, items);
                y += 10;
                DrawBottomBlock(g, ref y, x, width, data);
                e.HasMorePages = false;
            }
            else
            {
                e.HasMorePages = true;
            }
        }

        // ════════════════════════════════════════════════════════════════════
        // Cột bảng
        // ════════════════════════════════════════════════════════════════════
        private int[] BuildColumnWidths(int width)
        {
            // STT | Tên hàng | Mã số | ĐVT | Yêu cầu | Thực nhận | Ghi chú (fill)
            return new[]
            {
                34,
                250,
                100,
                46,
                70,
                90,
                width - (34 + 250 + 100 + 46 + 70 + 90)
            };
        }

        private int[] BuildColumnX(int startX, int[] colW)
        {
            var cx = new int[colW.Length];
            cx[0] = startX;
            for (int i = 1; i < colW.Length; i++)
                cx[i] = cx[i - 1] + colW[i - 1];
            return cx;
        }

        // ════════════════════════════════════════════════════════════════════
        // Header bảng (2 tầng — vẽ lại ở mỗi trang)
        // ════════════════════════════════════════════════════════════════════
        private void DrawTableHeader(Graphics g, ref int y, int[] colW, int[] colX)
        {
            const int h1 = 34;
            const int h2 = 28;

            // Tầng 1
            DrawCell(g, "Stt",
                FontBold, colX[0], y, colW[0], h1 + h2, StringAlignment.Center, StringAlignment.Center);
            DrawCell(g, "Tên, nhãn hiệu, quy cách,\nphẩm chất vật tư (sản\nphẩm hàng hóa)",
                FontBold, colX[1], y, colW[1], h1 + h2, StringAlignment.Center, StringAlignment.Center);
            DrawCell(g, "Mã số",
                FontBold, colX[2], y, colW[2], h1 + h2, StringAlignment.Center, StringAlignment.Center);
            DrawCell(g, "Đơn vị\ntính",
                FontBold, colX[3], y, colW[3], h1 + h2, StringAlignment.Center, StringAlignment.Center);
            DrawCell(g, "Số lượng",
                FontBold, colX[4], y, colW[4] + colW[5], h1, StringAlignment.Center, StringAlignment.Center);
            DrawCell(g, "Ghi chú",
                FontBold, colX[6], y, colW[6], h1 + h2, StringAlignment.Center, StringAlignment.Center);

            // Tầng 2
            DrawCell(g, "Yêu cầu",
                FontNormal, colX[4], y + h1, colW[4], h2, StringAlignment.Center, StringAlignment.Center);
            DrawCell(g, "Thực nhận",
                FontNormal, colX[5], y + h1, colW[5], h2, StringAlignment.Center, StringAlignment.Center);

            y += h1 + h2;
        }

        // ════════════════════════════════════════════════════════════════════
        // Một dòng dữ liệu
        // ════════════════════════════════════════════════════════════════════
        private void DrawReceiptRow(Graphics g, WarehouseReceiptItem item,
            int[] colW, int[] colX, ref int y, int rowH)
        {
            DrawCell(g, item.No.ToString(), FontNormal, colX[0], y, colW[0], rowH, StringAlignment.Center, StringAlignment.Center);
            DrawCell(g, item.Ten ?? "", FontNormal, colX[1], y, colW[1], rowH, StringAlignment.Near, StringAlignment.Center);
            DrawCell(g, item.Ma ?? "", FontNormal, colX[2], y, colW[2], rowH, StringAlignment.Near, StringAlignment.Center);
            DrawCell(g, item.DonVi ?? "", FontNormal, colX[3], y, colW[3], rowH, StringAlignment.Center, StringAlignment.Center);
            DrawCell(g, item.YeuCau ?? "", FontNormal, colX[4], y, colW[4], rowH, StringAlignment.Far, StringAlignment.Center);
            DrawCell(g, item.ThucNhan ?? "", FontNormal, colX[5], y, colW[5], rowH, StringAlignment.Far, StringAlignment.Center);
            DrawCell(g, item.GhiChu ?? "", FontNormal, colX[6], y, colW[6], rowH, StringAlignment.Near, StringAlignment.Center);
            y += rowH;
        }

        // ════════════════════════════════════════════════════════════════════
        // Dòng Cộng (chỉ vẽ ở trang cuối)
        // ════════════════════════════════════════════════════════════════════
        private void DrawTotalRow(Graphics g, ref int y, int[] colW, int[] colX,
            IList<WarehouseReceiptItem> items)
        {
            const int totalH = 28;
            string totalYeuCau = FormatTotal(items.Sum(i => ParseDecimal(i.YeuCau)));
            string totalThucNhan = FormatTotal(items.Sum(i => ParseDecimal(i.ThucNhan)));

            DrawCell(g, "Cộng:", FontBold, colX[0], y, colW[0] + colW[1], totalH, StringAlignment.Center, StringAlignment.Center);
            DrawCell(g, "", FontNormal, colX[2], y, colW[2], totalH, StringAlignment.Center, StringAlignment.Center);
            DrawCell(g, "", FontNormal, colX[3], y, colW[3], totalH, StringAlignment.Center, StringAlignment.Center);
            DrawCell(g, totalYeuCau, FontBold, colX[4], y, colW[4], totalH, StringAlignment.Far, StringAlignment.Center);
            DrawCell(g, totalThucNhan, FontBold, colX[5], y, colW[5], totalH, StringAlignment.Far, StringAlignment.Center);
            DrawCell(g, "", FontNormal, colX[6], y, colW[6], totalH, StringAlignment.Center, StringAlignment.Center);

            y += totalH;
        }

        // ════════════════════════════════════════════════════════════════════
        // Header công ty
        // ════════════════════════════════════════════════════════════════════
        private void DrawHeaderBlock(Graphics g, ref int y, int x, int width,
            WarehouseReceiptPrintData data)
        {
            var co = data.Company ?? new CompanyInfo();

            int logoW = 95;
            int rightW = 215;
            int gap = 10;
            int centerW = width - logoW - rightW - gap * 2;

            var logoRect = new Rectangle(x, y, logoW, 78);
            var centerRect = new Rectangle(x + logoW + gap, y, centerW, 78);
            var rightRect = new Rectangle(centerRect.Right + gap, y, rightW, 78);

            if (!string.IsNullOrWhiteSpace(co.LogoPath) && File.Exists(co.LogoPath))
                using (var img = Image.FromFile(co.LogoPath))
                    g.DrawImage(img, logoRect);

            g.DrawString(co.CompanyName ?? "",
                new Font("Times New Roman", 12f, FontStyle.Bold),
                Brushes.Black, centerRect.Left, centerRect.Top + 2);

            DrawWrappedText(g, co.Address ?? "", FontNormal, Brushes.Black,
                new RectangleF(centerRect.Left, centerRect.Top + 24, centerRect.Width, 45),
                StringAlignment.Near, StringAlignment.Near);

            using (var sf = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Near
            })
            {
                g.DrawString("Mẫu số 20 - HD",
                    new Font("Times New Roman", 10.5f, FontStyle.Bold),
                    Brushes.Black,
                    new RectangleF(rightRect.Left, rightRect.Top, rightRect.Width, 16), sf);

                g.DrawString("( Theo QĐ số: 19/2006/QĐ-BTC", FontSmall,
                    Brushes.Black,
                    new RectangleF(rightRect.Left, rightRect.Top + 16, rightRect.Width, 14), sf);

                g.DrawString("ngày 30 tháng 3 năm 2006", FontSmall,
                    Brushes.Black,
                    new RectangleF(rightRect.Left, rightRect.Top + 30, rightRect.Width, 14), sf);

                g.DrawString("của Bộ trưởng Bộ Tài chính )", FontSmall,
                    Brushes.Black,
                    new RectangleF(rightRect.Left, rightRect.Top + 44, rightRect.Width, 14), sf);
            }

            y += 86;
        }

        // ════════════════════════════════════════════════════════════════════
        // Tiêu đề phiếu
        // ════════════════════════════════════════════════════════════════════
        private void DrawTitleBlock(Graphics g, ref int y, int x, int width,
            WarehouseReceiptPrintData data)
        {
            using (var sf = new StringFormat { Alignment = StringAlignment.Center })
                g.DrawString("PHIẾU NHẬP KHO",
                    new Font("Times New Roman", 18f, FontStyle.Bold),
                    Brushes.Black, new RectangleF(x, y, width, 28), sf);
            y += 30;

            using (var sf = new StringFormat { Alignment = StringAlignment.Center })
                g.DrawString(data.NgayIn ?? "",
                    new Font("Times New Roman", 12f, FontStyle.Regular),
                    Brushes.Black, new RectangleF(x, y, width, 20), sf);

            int rightX = x + (int)(width * 0.72);
            int valueOffset = 76;

            g.DrawString("Số PO:", FontNormal, Brushes.Black, rightX, y + 4);
            g.DrawString(data.SoPO ?? "", FontNormal, Brushes.Black, rightX + valueOffset, y + 4);

            y += 24;

            g.DrawString("Số phiếu:", FontNormal, Brushes.Black, rightX, y);
            g.DrawString(data.SoPhieu ?? "", FontNormal, Brushes.Black, rightX + valueOffset, y);

            y += 26;
        }

        // ════════════════════════════════════════════════════════════════════
        // Thông tin phiếu
        // ════════════════════════════════════════════════════════════════════
        private void DrawInfoBlock(Graphics g, ref int y, int x, int width,
            WarehouseReceiptPrintData data)
        {
            int labelW = 165;
            int lineH = 22;
            int indentX = x + 60;

            DrawInfoLine(g, indentX, y, labelW, "Họ tên người giao hàng:", data.NguoiGiao); y += lineH;
            DrawInfoLine(g, indentX, y, labelW, "Nhà cung cấp:", data.NhaCungCap); y += lineH;
            DrawInfoLine(g, indentX, y, labelW, "Lý do nhập kho:", data.LyDoNhap); y += lineH;
            DrawInfoLine(g, indentX, y, labelW, "Kho hàng:", data.KhoHang); y += lineH;
        }

        private void DrawInfoLine(Graphics g, int x, int y, int labelW, string label, string value)
        {
            g.DrawString(label, FontNormal, Brushes.Black, x, y);
            g.DrawString(value ?? "", FontNormal, Brushes.Black, x + labelW, y);
        }

        // ════════════════════════════════════════════════════════════════════
        // Khối chữ ký
        // ════════════════════════════════════════════════════════════════════
        private void DrawBottomBlock(Graphics g, ref int y, int x, int width,
            WarehouseReceiptPrintData data)
        {
            g.DrawString("Số chứng từ gốc kèm theo :",
                FontNormal, Brushes.Black, x + 6, y);
            y += 34;

            int leftW = width / 3;
            int centerW = width / 3;
            int rightW = width - leftW - centerW;

            int leftX = x;
            int centerX = x + leftW;
            int rightX = centerX + centerW;

            using (var sfC = new StringFormat { Alignment = StringAlignment.Center })
            {
                g.DrawString(data.NgayIn ?? "",
                    new Font("Times New Roman", 12f, FontStyle.Italic),
                    Brushes.Black, new RectangleF(rightX, y - 4, rightW, 18), sfC);

                // Cột trái: Giám đốc nhà máy
                g.DrawString(data.Signature?.FactoryDirectorTitle ?? "Giám đốc nhà máy",
                    new Font("Times New Roman", 12f, FontStyle.Bold),
                    Brushes.Black, new RectangleF(leftX, y, leftW, 20), sfC);
                g.DrawString("(Ký, họ tên)",
                    new Font("Times New Roman", 12f, FontStyle.Italic),
                    Brushes.Black, new RectangleF(leftX, y + 22, leftW, 18), sfC);

                // Cột giữa: Kế toán
                g.DrawString(data.Signature?.CheckerTitle ?? "Kế toán",
                    new Font("Times New Roman", 12f, FontStyle.Bold),
                    Brushes.Black, new RectangleF(centerX, y, centerW, 20), sfC);
                g.DrawString("(Ký, họ tên)",
                    new Font("Times New Roman", 12f, FontStyle.Italic),
                    Brushes.Black, new RectangleF(centerX, y + 22, centerW, 18), sfC);

                // Cột phải: Thủ kho
                g.DrawString(data.Signature?.RequesterTitle ?? "Thủ kho",
                    new Font("Times New Roman", 12f, FontStyle.Bold),
                    Brushes.Black, new RectangleF(rightX, y + 18, rightW, 20), sfC);
                g.DrawString("(Ký, họ tên)",
                    new Font("Times New Roman", 12f, FontStyle.Italic),
                    Brushes.Black, new RectangleF(rightX, y + 40, rightW, 18), sfC);
            }

            y += 120;
        }

        // ════════════════════════════════════════════════════════════════════
        // Helpers
        // ════════════════════════════════════════════════════════════════════
        private void DrawWrappedText(Graphics g, string text, Font font, Brush brush,
            RectangleF rect, StringAlignment align, StringAlignment lineAlign)
        {
            using (var sf = new StringFormat())
            {
                sf.Alignment = align;
                sf.LineAlignment = lineAlign;
                sf.Trimming = StringTrimming.Word;
                g.DrawString(text ?? "", font, brush, rect, sf);
            }
        }

        private decimal ParseDecimal(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return 0m;
            value = value.Trim().Replace(",", "");
            return decimal.TryParse(value, out var d) ? d : 0m;
        }

        private string FormatTotal(decimal value) => value.ToString("0.0");
    }
}