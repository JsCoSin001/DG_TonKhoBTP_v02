// File: WarehouseIssuesRenderer.cs
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
    public class WarehouseIssuesRenderer
        : BaseRenderer<WarehouseIssuesPrintData, WarehouseIssuesItem>
    {
        // ── Trạng thái phân trang ────────────────────────────────────────────
        private int _startItemIndex = 0;
        private bool _headerDrawn = false;

        // Chiều cao dự trữ cuối trang: dòng Cộng + Tổng tiền + chữ ký
        private const int BottomReserve = 200;

        // ── Abstract overrides (bắt buộc bởi BaseRenderer) ──────────────────
        protected override CompanyInfo GetCompanyInfo(WarehouseIssuesPrintData d) => d.Company;
        protected override IReadOnlyList<WarehouseIssuesItem> GetItems(WarehouseIssuesPrintData d) => d.Items;
        protected override SignatureInfo GetSignature(WarehouseIssuesPrintData d) => d.Signature;
        protected override DocumentInfo GetDocumentInfo(WarehouseIssuesPrintData d) => new DocumentInfo();
        protected override IReadOnlyList<ColumnDef<WarehouseIssuesItem>> BuildColumns(int totalWidth)
            => Array.Empty<ColumnDef<WarehouseIssuesItem>>();

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
            WarehouseIssuesPrintData data)
        {
            g.TextRenderingHint = TextRenderingHint.AntiAlias;

            int x = marginBounds.Left;
            int y = marginBounds.Top;
            int width = marginBounds.Width;
            int bottom = marginBounds.Bottom;

            // ── Trang đầu: vẽ header công ty + tiêu đề + thông tin phiếu ──
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
                // Trang tiếp: lề nhỏ trên cùng
                y += 10;
            }

            // ── Vẽ header bảng ───────────────────────────────────────────────
            int[] colW = BuildColumnWidths(width);
            int[] colX = BuildColumnX(x, colW);
            DrawTableHeader(g, ref y, colW, colX);

            // ── Vẽ từng dòng dữ liệu ────────────────────────────────────────
            var items = data.Items ?? new List<WarehouseIssuesItem>();
            bool hasMore = false;

            for (int i = _startItemIndex; i < items.Count; i++)
            {
                var item = items[i];
                int rowH = Math.Max(28,
                    MeasureCellTextHeight(g, item.Ten, FontNormal, colW[1] - 8) + 8);

                // Kiểm tra còn đủ chỗ không (dành chỗ cho Cộng + Tổng + chữ ký)
                if (y + rowH + BottomReserve > bottom)
                {
                    _startItemIndex = i;
                    hasMore = true;
                    break;
                }

                DrawIssuesRow(g, item, colW, colX, ref y, rowH);
            }

            // ── Trang cuối: vẽ dòng Cộng + Tổng tiền + chữ ký ──────────────
            if (!hasMore)
            {
                DrawTotalRow(g, ref y, colW, colX, items);
                y += 6;
                DrawTotalLine(g, ref y, x, width);
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
            // STT | Tên hàng | Mã số | ĐVT | Số lượng | Đơn giá (fill) | Thành tiền
            return new[]
            {
                34,
                270,
                100,
                50,
                90,
                width - (34 + 270 + 100 + 50 + 90 + 90),
                90
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
        // Header bảng (vẽ lại ở mỗi trang)
        // ════════════════════════════════════════════════════════════════════
        private void DrawTableHeader(Graphics g, ref int y, int[] colW, int[] colX)
        {
            const int h = 56;

            DrawCell(g, "Stt",
                FontBold, colX[0], y, colW[0], h, StringAlignment.Center, StringAlignment.Center);
            DrawCell(g, "Tên, nhãn hiệu, quy cách,\nphẩm chất vật tư (sản\nphẩm hàng hóa)",
                FontBold, colX[1], y, colW[1], h, StringAlignment.Center, StringAlignment.Center);
            DrawCell(g, "Mã số",
                FontBold, colX[2], y, colW[2], h, StringAlignment.Center, StringAlignment.Center);
            DrawCell(g, "Đơn vị\ntính",
                FontBold, colX[3], y, colW[3], h, StringAlignment.Center, StringAlignment.Center);
            DrawCell(g, "Số lượng",
                FontBold, colX[4], y, colW[4], h, StringAlignment.Center, StringAlignment.Center);
            DrawCell(g, "Đơn giá",
                FontBold, colX[5], y, colW[5], h, StringAlignment.Center, StringAlignment.Center);
            DrawCell(g, "Thành tiền",
                FontBold, colX[6], y, colW[6], h, StringAlignment.Center, StringAlignment.Center);

            y += h;
        }

        // ════════════════════════════════════════════════════════════════════
        // Một dòng dữ liệu
        // ════════════════════════════════════════════════════════════════════
        private void DrawIssuesRow(Graphics g, WarehouseIssuesItem item,
            int[] colW, int[] colX, ref int y, int rowH)
        {
            DrawCell(g, item.No.ToString(), FontNormal, colX[0], y, colW[0], rowH, StringAlignment.Center, StringAlignment.Center);
            DrawCell(g, item.Ten ?? "", FontNormal, colX[1], y, colW[1], rowH, StringAlignment.Near, StringAlignment.Center);
            DrawCell(g, item.Ma ?? "", FontNormal, colX[2], y, colW[2], rowH, StringAlignment.Near, StringAlignment.Center);
            DrawCell(g, item.DonVi ?? "", FontNormal, colX[3], y, colW[3], rowH, StringAlignment.Center, StringAlignment.Center);
            DrawCell(g, item.SoLuong ?? "", FontNormal, colX[4], y, colW[4], rowH, StringAlignment.Far, StringAlignment.Center);
            DrawCell(g, item.DonGia ?? "", FontNormal, colX[5], y, colW[5], rowH, StringAlignment.Far, StringAlignment.Center);
            DrawCell(g, item.ThanhTien ?? "", FontNormal, colX[6], y, colW[6], rowH, StringAlignment.Far, StringAlignment.Center);
            y += rowH;
        }

        // ════════════════════════════════════════════════════════════════════
        // Dòng Cộng (chỉ vẽ ở trang cuối)
        // ════════════════════════════════════════════════════════════════════
        private void DrawTotalRow(Graphics g, ref int y, int[] colW, int[] colX,
            IList<WarehouseIssuesItem> items)
        {
            const int totalH = 28;
            string totalQty = FormatTotal(items.Sum(i => ParseDecimal(i.SoLuong)));

            DrawCell(g, "Cộng:", FontBold, colX[0], y, colW[0] + colW[1], totalH, StringAlignment.Center, StringAlignment.Center);
            DrawCell(g, "", FontNormal, colX[2], y, colW[2], totalH, StringAlignment.Center, StringAlignment.Center);
            DrawCell(g, "", FontNormal, colX[3], y, colW[3], totalH, StringAlignment.Center, StringAlignment.Center);
            DrawCell(g, totalQty, FontBold, colX[4], y, colW[4], totalH, StringAlignment.Far, StringAlignment.Center);
            DrawCell(g, "", FontNormal, colX[5], y, colW[5], totalH, StringAlignment.Center, StringAlignment.Center);
            DrawCell(g, "", FontNormal, colX[6], y, colW[6], totalH, StringAlignment.Center, StringAlignment.Center);

            y += totalH;
        }

        // ════════════════════════════════════════════════════════════════════
        // Header công ty
        // ════════════════════════════════════════════════════════════════════
        private void DrawHeaderBlock(Graphics g, ref int y, int x, int width,
            WarehouseIssuesPrintData data)
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
                g.DrawString("Mẫu số 02 - VT",
                    new Font("Times New Roman", 10.5f, FontStyle.Bold),
                    Brushes.Black,
                    new RectangleF(rightRect.Left, rightRect.Top, rightRect.Width, 16), sf);

                g.DrawString("( Ban hành theo Thông tư số 200/2014/TT-BTC", FontSmall,
                    Brushes.Black,
                    new RectangleF(rightRect.Left, rightRect.Top + 16, rightRect.Width, 14), sf);

                g.DrawString("ngày 22/12/2014 của Bộ Tài chính )", FontSmall,
                    Brushes.Black,
                    new RectangleF(rightRect.Left, rightRect.Top + 30, rightRect.Width, 14), sf);
            }

            y += 86;
        }

        // ════════════════════════════════════════════════════════════════════
        // Tiêu đề phiếu
        // ════════════════════════════════════════════════════════════════════
        private void DrawTitleBlock(Graphics g, ref int y, int x, int width,
            WarehouseIssuesPrintData data)
        {
            using (var sf = new StringFormat { Alignment = StringAlignment.Center })
                g.DrawString("PHIẾU XUẤT KHO",
                    new Font("Times New Roman", 18f, FontStyle.Bold),
                    Brushes.Black, new RectangleF(x, y, width, 28), sf);
            y += 30;

            using (var sf = new StringFormat { Alignment = StringAlignment.Center })
                g.DrawString(data.NgayIn ?? "",
                    new Font("Times New Roman", 12f, FontStyle.Regular),
                    Brushes.Black, new RectangleF(x, y, width, 20), sf);

            int rightX = x + (int)(width * 0.72);
            int valueOffset = 30;

            g.DrawString("Nợ:", FontNormal, Brushes.Black, rightX, y + 4);
            g.DrawString(data.So ?? "", FontNormal, Brushes.Black, rightX + valueOffset, y + 4);

            y += 24;

            using (var sf = new StringFormat { Alignment = StringAlignment.Center })
                g.DrawString($"Số :  {data.SoPhieu ?? ""}",
                    new Font("Times New Roman", 12f, FontStyle.Regular),
                    Brushes.Black, new RectangleF(x, y, width, 20), sf);

            g.DrawString("Có:", FontNormal, Brushes.Black, rightX, y);
            g.DrawString(data.Co ?? "", FontNormal, Brushes.Black, rightX + valueOffset, y);

            y += 30;
        }

        // ════════════════════════════════════════════════════════════════════
        // Thông tin phiếu
        // ════════════════════════════════════════════════════════════════════
        private void DrawInfoBlock(Graphics g, ref int y, int x, int width,
            WarehouseIssuesPrintData data)
        {
            int labelW = 200;
            int lineH = 22;

            DrawInfoLine(g, x, y, labelW, "Họ và tên người nhận hàng:", data.NguoiNhan); y += lineH;
            DrawInfoLine(g, x, y, labelW, "Lý do xuất kho :", data.LyDoXuat); y += lineH;
            DrawInfoLine(g, x, y, labelW, "Xuất tại kho:", data.XuatTaiKho); y += lineH;
        }

        private void DrawInfoLine(Graphics g, int x, int y, int labelW, string label, string value)
        {
            g.DrawString(label, FontNormal, Brushes.Black, x, y);
            g.DrawString(value ?? "",
                new Font("Times New Roman", 12f, FontStyle.Italic),
                Brushes.Black, x + labelW, y);
        }

        // ════════════════════════════════════════════════════════════════════
        // Dòng Tổng số tiền bằng chữ
        // ════════════════════════════════════════════════════════════════════
        private void DrawTotalLine(Graphics g, ref int y, int x, int width)
        {
            g.DrawString("Tổng số tiền ( viết bằng chữ ) :",
                FontNormal, Brushes.Black, x, y);

            using (var pen = new Pen(Color.Black, 0.5f)
            { DashStyle = System.Drawing.Drawing2D.DashStyle.Dot })
                g.DrawLine(pen, x + 220, y + 14, x + width, y + 14);

            y += 24;
        }

        // ════════════════════════════════════════════════════════════════════
        // Khối chữ ký
        // ════════════════════════════════════════════════════════════════════
        private void DrawBottomBlock(Graphics g, ref int y, int x, int width,
            WarehouseIssuesPrintData data)
        {
            g.DrawString("Số chứng từ gốc kèm theo :",
                FontNormal, Brushes.Black, x + 6, y);
            y += 34;

            int colW = width / 4;

            using (var sfC = new StringFormat { Alignment = StringAlignment.Center })
            {
                string ngayKy = data.NgayIn ?? "";

                g.DrawString(ngayKy,
                    new Font("Times New Roman", 12f, FontStyle.Italic),
                    Brushes.Black,
                    new RectangleF(x + colW * 2 + 20, y - 4, colW * 2 - 20, 18), sfC);

                string[] titles =
                {
                    data.Signature?.CheckerTitle         ?? "Kế toán",
                    data.Signature?.RequesterTitle       ?? "Người nhận hàng",
                    data.Signature?.FactoryDirectorTitle ?? "Thủ kho",
                    data.Signature?.DirectorTitle        ?? "Giám đốc"
                };

                for (int i = 0; i < 4; i++)
                {
                    int cx = x + colW * i;
                    g.DrawString(titles[i],
                        new Font("Times New Roman", 12f, FontStyle.Bold),
                        Brushes.Black, new RectangleF(cx, y + 18, colW, 20), sfC);
                    g.DrawString("(Ký, họ tên)",
                        new Font("Times New Roman", 12f, FontStyle.Italic),
                        Brushes.Black, new RectangleF(cx, y + 40, colW, 18), sfC);
                }
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