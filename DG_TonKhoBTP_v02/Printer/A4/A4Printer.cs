using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.Drawing.Text;
using System.IO;
using static DG_TonKhoBTP_v02.Printer.A4.PrinterModel;

namespace DG_TonKhoBTP_v02.Printer.A4
{
    // ═══════════════════════════════════════════════════════════════════════════
    // ColumnDef — khai báo một cột trong bảng
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Khai báo một cột bảng.
    /// <para><b>Width</b> = số nguyên dương → chiều rộng cố định (đơn vị: 1/100 inch).</para>
    /// <para><b>Width</b> = 0 → cột "fill": lấy toàn bộ phần còn lại sau khi trừ các cột cố định.</para>
    /// </summary>
    public class ColumnDef<TItem>
    {
        public string Header { get; set; }
        public int Width { get; set; }   // 0 = fill
        public StringAlignment HAlign { get; set; } = StringAlignment.Center;
        public StringAlignment VAlign { get; set; } = StringAlignment.Center;
        public StringAlignment DataHAlign { get; set; } = StringAlignment.Near;
        public StringAlignment DataVAlign { get; set; } = StringAlignment.Near;
        public Func<TItem, string> GetValue { get; set; }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // BaseRenderer<TPrintData, TItem>
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Renderer dùng chung: header công ty, tiêu đề, bảng đa cột, chữ ký.
    /// Lớp con chỉ cần override <see cref="GetCompanyInfo"/>, <see cref="GetDocumentInfo"/>,
    /// <see cref="GetItems"/>, <see cref="GetSignature"/> và <see cref="BuildColumns"/>.
    /// </summary>
    public abstract class BaseRenderer<TPrintData, TItem> : IPrintRenderer<TPrintData>
    {
        // ── Fonts (dùng chung) ──────────────────────────────────────────────
        protected readonly Font FontNormal = new Font("Times New Roman", 10f, FontStyle.Regular);
        protected readonly Font FontBold = new Font("Times New Roman", 10f, FontStyle.Bold);
        protected readonly Font FontTitle = new Font("Times New Roman", 16f, FontStyle.Bold);
        protected readonly Font FontSmall = new Font("Times New Roman", 9f, FontStyle.Regular);
        protected readonly Font FontSmallBold = new Font("Times New Roman", 9f, FontStyle.Bold);

        private int _currentItemIndex;
        private IReadOnlyList<ColumnDef<TItem>> _columns;

        // ── Abstract helpers ────────────────────────────────────────────────
        protected abstract CompanyInfo GetCompanyInfo(TPrintData data);
        protected abstract DocumentInfo GetDocumentInfo(TPrintData data);
        protected abstract IReadOnlyList<TItem> GetItems(TPrintData data);
        protected abstract SignatureInfo GetSignature(TPrintData data);

        /// <summary>
        /// Khai báo danh sách cột. Đúng một cột có Width = 0 (fill).
        /// </summary>
        protected abstract IReadOnlyList<ColumnDef<TItem>> BuildColumns(int totalWidth);

        // ── IPrintRenderer ──────────────────────────────────────────────────
        public void Reset()
        {
            _currentItemIndex = 0;
            _columns = null;
        }

        public void Render(Graphics g, Rectangle marginBounds, PrintPageEventArgs e, TPrintData data)
        {
            g.TextRenderingHint = TextRenderingHint.AntiAlias;

            int x = marginBounds.Left;
            int y = marginBounds.Top;
            int contentWidth = marginBounds.Width;
            int bottomLimit = marginBounds.Bottom;

            // Lazy-build columns once per print job
            _columns ??= ResolveColumnWidths(BuildColumns(contentWidth), contentWidth);

            DrawHeader(g, ref y, x, contentWidth, GetCompanyInfo(data));
            y += 8;
            DrawDocumentTitle(g, ref y, x, contentWidth, GetDocumentInfo(data));
            y += 10;

            y = DrawTable(g, y, x, contentWidth, bottomLimit, GetItems(data), out bool hasMore);

            if (!hasMore)
            {
                y += 25;
                DrawSignature(g, ref y, x, contentWidth, GetSignature(data));
            }

            e.HasMorePages = hasMore;
        }

        // ═══════════════════════════════════════════════════════════════════
        // HEADER
        // ═══════════════════════════════════════════════════════════════════
        private void DrawHeader(Graphics g, ref int y, int x, int width, CompanyInfo co)
        {
            int logoW = 70;
            int rightW = 185;
            int centerW = width - logoW - rightW - 20;

            var logoRect = new Rectangle(x, y, logoW, 70);
            var centerRect = new Rectangle(x + logoW + 10, y, centerW, 70);
            var rightRect = new Rectangle(x + logoW + 10 + centerW + 10, y, rightW, 70);

            if (!string.IsNullOrWhiteSpace(co.LogoPath) && File.Exists(co.LogoPath))
                using (var img = Image.FromFile(co.LogoPath))
                    g.DrawImage(img, logoRect);

            g.DrawString(co.CompanyName ?? "", FontBold, Brushes.Black, centerRect.Left, centerRect.Top + 4);
            g.DrawString(co.Address ?? "", FontNormal, Brushes.Black, centerRect.Left, centerRect.Top + 26);

            DrawLabelValue(g, "Mã số:", co.FormCode ?? "", rightRect.Left, rightRect.Top + 2, rightW);
            DrawLabelValue(g, "Ngày ban hành:", co.IssueDate ?? "", rightRect.Left, rightRect.Top + 20, rightW);
            DrawLabelValue(g, "Lần sửa đổi:", co.Revision ?? "", rightRect.Left, rightRect.Top + 38, rightW);

            y += 80;
        }

        private void DrawLabelValue(Graphics g, string label, string value, int x, int y, int width)
        {
            g.DrawString(label, FontSmall, Brushes.Black, x, y);
            float labelW = g.MeasureString(label, FontSmall).Width;
            g.DrawString(value, FontSmall, Brushes.Black, x + labelW + 2, y);
        }

        // ═══════════════════════════════════════════════════════════════════
        // DOCUMENT TITLE
        // ═══════════════════════════════════════════════════════════════════
        private void DrawDocumentTitle(Graphics g, ref int y, int x, int width, DocumentInfo doc)
        {
            using (var center = new StringFormat { Alignment = StringAlignment.Center })
                g.DrawString(doc.Title ?? "", FontTitle, Brushes.Black,
                             new RectangleF(x, y, width, 30), center);

            y += 40;

            int blockW = 320;
            int startX = x + width - blockW;
            int labelW = 130;

            g.DrawString("Ngày đặt hàng:", FontBold, Brushes.Black, startX, y);
            g.DrawString(doc.OrderDate ?? "", FontBold, Brushes.Black, startX + labelW, y);
            y += 22;
            g.DrawString("Mã đơn hàng đặt:", FontBold, Brushes.Black, startX, y);
            g.DrawString(doc.OrderCode ?? "", FontBold, Brushes.Black, startX + labelW, y);
            y += 14;
        }

        // ═══════════════════════════════════════════════════════════════════
        // TABLE  (column-driven)
        // ═══════════════════════════════════════════════════════════════════
        private int DrawTable(
            Graphics g, int y, int x, int width, int bottomLimit,
            IReadOnlyList<TItem> items, out bool hasMore)
        {
            hasMore = false;

            const int headerH = 36;
            const int rowPad = 5;
            const int signatureReserve = 140;

            // ── Header row ──
            int cx = x;
            foreach (var col in _columns)
            {
                DrawCell(g, col.Header, FontBold, cx, y, col.Width, headerH,
                         col.HAlign, col.VAlign);
                cx += col.Width;
            }
            y += headerH;

            // ── Data rows ──
            while (_currentItemIndex < items.Count)
            {
                var item = items[_currentItemIndex];

                // Measure max row height across all columns
                int rowH = 26;
                foreach (var col in _columns)
                {
                    int h = MeasureCellTextHeight(g, col.GetValue(item), FontNormal, col.Width - 8);
                    rowH = Math.Max(rowH, h + rowPad * 2);
                }

                bool isLastItem = (_currentItemIndex == items.Count - 1);
                int neededBelow = isLastItem ? signatureReserve : 0;

                if (y + rowH + neededBelow > bottomLimit)
                {
                    hasMore = true;
                    return y;
                }

                cx = x;
                foreach (var col in _columns)
                {
                    DrawCell(g, col.GetValue(item), FontNormal,
                             cx, y, col.Width, rowH,
                             col.DataHAlign, col.DataVAlign);
                    cx += col.Width;
                }

                y += rowH;
                _currentItemIndex++;
            }

            return y;
        }

        // ═══════════════════════════════════════════════════════════════════
        // SIGNATURE
        // ═══════════════════════════════════════════════════════════════════
        private void DrawSignature(Graphics g, ref int y, int x, int width, SignatureInfo sign)
        {
            const int boxH = 120;
            int colW = width / 4;
            int lastW = width - colW * 3;

            DrawSignatureCell(g, sign.DirectorTitle, sign.DirectorName, x, y, colW, boxH);
            DrawSignatureCell(g, sign.FactoryDirectorTitle, sign.FactoryDirectorName, x + colW, y, colW, boxH);
            DrawSignatureCell(g, sign.CheckerTitle, sign.CheckerName, x + colW * 2, y, colW, boxH);
            DrawSignatureCell(g, sign.RequesterTitle, sign.RequesterName, x + colW * 3, y, lastW, boxH);

            y += boxH;
        }

        private void DrawSignatureCell(Graphics g, string title, string name, int x, int y, int w, int h)
        {
            g.DrawRectangle(Pens.Black, x, y, w, h);

            const int titleH = 32;
            var titleRect = new Rectangle(x, y, w, titleH);
            g.DrawLine(Pens.Black, x, y + titleH, x + w, y + titleH);

            using (var sf = new StringFormat
            { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                g.DrawString(title ?? "", FontSmallBold, Brushes.Black, (RectangleF)titleRect, sf);

            if (!string.IsNullOrWhiteSpace(name))
            {
                var nameRect = new RectangleF(x + 4, y + titleH + 2, w - 8, h - titleH - 6);
                using (var sf = new StringFormat
                { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Far })
                    g.DrawString(name, FontNormal, Brushes.Black, nameRect, sf);
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        // HELPERS
        // ═══════════════════════════════════════════════════════════════════
        protected void DrawCell(
            Graphics g, string text, Font font,
            int x, int y, int w, int h,
            StringAlignment horizontal, StringAlignment vertical)
        {
            g.DrawRectangle(Pens.Black, x, y, w, h);
            var rect = new RectangleF(x + 4, y + 4, w - 8, h - 8);
            using (var sf = new StringFormat())
            {
                sf.Alignment = horizontal;
                sf.LineAlignment = vertical;
                sf.Trimming = StringTrimming.Word;
                sf.FormatFlags = StringFormatFlags.LineLimit;
                g.DrawString(text ?? "", font, Brushes.Black, rect, sf);
            }
        }

        protected int MeasureCellTextHeight(Graphics g, string text, Font font, int availableWidth)
            => (int)Math.Ceiling(g.MeasureString(text ?? "", font, availableWidth).Height);

        /// <summary>
        /// Tính Width thực cho cột fill (Width == 0).
        /// </summary>
        private static IReadOnlyList<ColumnDef<TItem>> ResolveColumnWidths(
            IReadOnlyList<ColumnDef<TItem>> cols, int totalWidth)
        {
            int fixed_ = 0;
            foreach (var c in cols) fixed_ += c.Width;
            int fill = totalWidth - fixed_;

            var result = new List<ColumnDef<TItem>>(cols.Count);
            foreach (var c in cols)
            {
                if (c.Width == 0)
                {
                    // clone with resolved width
                    result.Add(new ColumnDef<TItem>
                    {
                        Header = c.Header,
                        Width = fill,
                        HAlign = c.HAlign,
                        VAlign = c.VAlign,
                        DataHAlign = c.DataHAlign,
                        DataVAlign = c.DataVAlign,
                        GetValue = c.GetValue
                    });
                }
                else
                {
                    result.Add(c);
                }
            }
            return result;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // A4Printer — chứa 2 renderer cụ thể
    // ═══════════════════════════════════════════════════════════════════════════

    public static class A4Printer
    {
        // ── Mẫu B: Giấy đề nghị MUA DỊCH VỤ ──────────────────────────────

        public class PurchaseRequestRenderer
            : BaseRenderer<PurchaseRequestPrintData, ServiceItem>
        {
            protected override CompanyInfo GetCompanyInfo(PurchaseRequestPrintData d) => d.Company;
            protected override DocumentInfo GetDocumentInfo(PurchaseRequestPrintData d) => d.Document;
            protected override IReadOnlyList<ServiceItem> GetItems(PurchaseRequestPrintData d) => d.Items;
            protected override SignatureInfo GetSignature(PurchaseRequestPrintData d) => d.Signature;

            protected override IReadOnlyList<ColumnDef<ServiceItem>> BuildColumns(int totalWidth)
                => new[]
                {
                    new ColumnDef<ServiceItem>
                    {
                        Header     = "Stt",
                        Width      = 38,
                        DataHAlign = StringAlignment.Center,
                        DataVAlign = StringAlignment.Center,
                        GetValue   = i => i.No.ToString()
                    },
                    new ColumnDef<ServiceItem>          // fill
                    {
                        Header     = "Dịch vụ",
                        Width      = 0,
                        DataHAlign = StringAlignment.Center,
                        DataVAlign = StringAlignment.Near,
                        GetValue   = i => i.ServiceName
                    },
                    new ColumnDef<ServiceItem>
                    {
                        Header     = "Mục đích",
                        Width      = 220,
                        DataHAlign = StringAlignment.Near,
                        DataVAlign = StringAlignment.Near,
                        GetValue   = i => i.Purpose
                    },
                    new ColumnDef<ServiceItem>
                    {
                        Header     = "Ngày cần",
                        Width      = 72,
                        DataHAlign = StringAlignment.Center,
                        DataVAlign = StringAlignment.Center,
                        GetValue   = i => i.RequiredDate
                    },
                };
        }

        // ── Mẫu A: Giấy đề nghị MUA VẬT TƯ ──────────────────────────────

        public class MaterialRequestRenderer
            : BaseRenderer<MaterialRequestPrintData, MaterialItem>
        {
            protected override CompanyInfo GetCompanyInfo(MaterialRequestPrintData d) => d.Company;
            protected override DocumentInfo GetDocumentInfo(MaterialRequestPrintData d) => d.Document;
            protected override IReadOnlyList<MaterialItem> GetItems(MaterialRequestPrintData d) => d.Items;
            protected override SignatureInfo GetSignature(MaterialRequestPrintData d) => d.Signature;

            protected override IReadOnlyList<ColumnDef<MaterialItem>> BuildColumns(int totalWidth)
                => new[]
                {
                    new ColumnDef<MaterialItem>
                    {
                        Header = "Stt",          Width = 30,
                        DataHAlign = StringAlignment.Center, DataVAlign = StringAlignment.Center,
                        GetValue = i => i.No.ToString()
                    },
                    new ColumnDef<MaterialItem>
                    {
                        Header = "Mã vật tư",    Width = 75,
                        DataHAlign = StringAlignment.Center, DataVAlign = StringAlignment.Center,
                        GetValue = i => i.MaterialCode
                    },
                    new ColumnDef<MaterialItem>  // fill
                    {
                        Header = "Tên vật tư",   Width = 0,
                        DataHAlign = StringAlignment.Near, DataVAlign = StringAlignment.Near,
                        GetValue = i => i.MaterialName
                    },
                    new ColumnDef<MaterialItem>
                    {
                        Header = "Đơn\nvị",      Width = 40,
                        DataHAlign = StringAlignment.Center, DataVAlign = StringAlignment.Center,
                        GetValue = i => i.Unit
                    },
                    new ColumnDef<MaterialItem>
                    {
                        Header = "Số lượng",     Width = 55,
                        DataHAlign = StringAlignment.Center, DataVAlign = StringAlignment.Center,
                        GetValue = i => i.Quantity
                    },
                    new ColumnDef<MaterialItem>
                    {
                        Header = "Đơn giá",      Width = 60,
                        DataHAlign = StringAlignment.Center, DataVAlign = StringAlignment.Center,
                        GetValue = i => i.UnitPrice
                    },
                    new ColumnDef<MaterialItem>
                    {
                        Header = "Mục đích",     Width = 130,
                        DataHAlign = StringAlignment.Near, DataVAlign = StringAlignment.Near,
                        GetValue = i => i.Purpose
                    },
                    new ColumnDef<MaterialItem>
                    {
                        Header = "Ngày YC\ngiao hàng", Width = 52,
                        DataHAlign = StringAlignment.Center, DataVAlign = StringAlignment.Center,
                        GetValue = i => i.RequiredDate
                    },
                    new ColumnDef<MaterialItem>
                    {
                        Header = "SL tồn\nhiện tại",   Width = 52,
                        DataHAlign = StringAlignment.Center, DataVAlign = StringAlignment.Center,
                        GetValue = i => i.CurrentStock
                    },
                };
        }
    }
}