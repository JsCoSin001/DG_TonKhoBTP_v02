// File: WarehouseIssuesRenderer.cs (TẠO MỚI)
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
        protected override CompanyInfo GetCompanyInfo(WarehouseIssuesPrintData d) => d.Company;
        protected override IReadOnlyList<WarehouseIssuesItem> GetItems(WarehouseIssuesPrintData d) => d.Items;
        protected override SignatureInfo GetSignature(WarehouseIssuesPrintData d) => d.Signature;
        protected override DocumentInfo GetDocumentInfo(WarehouseIssuesPrintData d) => new DocumentInfo();
        protected override IReadOnlyList<ColumnDef<WarehouseIssuesItem>> BuildColumns(int totalWidth)
            => Array.Empty<ColumnDef<WarehouseIssuesItem>>();

        public override void Render(Graphics g, Rectangle marginBounds, PrintPageEventArgs e,
            WarehouseIssuesPrintData data)
        {
            g.TextRenderingHint = TextRenderingHint.AntiAlias;

            int x = marginBounds.Left;
            int y = marginBounds.Top;
            int width = marginBounds.Width;

            DrawHeaderBlock(g, ref y, x, width, data);
            DrawTitleBlock(g, ref y, x, width, data);
            DrawInfoBlock(g, ref y, x, width, data);
            y += 8;
            DrawIssuesTable(g, ref y, x, width, data);
            y += 6;
            DrawTotalLine(g, ref y, x, width);
            y += 10;
            DrawBottomBlock(g, ref y, x, width, data);

            e.HasMorePages = false;
        }

        // ------------------------------------------------------------------ //
        private void DrawHeaderBlock(Graphics g, ref int y, int x, int width,
            WarehouseIssuesPrintData data)
        {
            var co = data.Company ?? new CompanyInfo();

            int logoW = 95, rightW = 215, gap = 10;
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
                    Brushes.Black, new RectangleF(rightRect.Left, rightRect.Top, rightRect.Width, 16), sf);

                g.DrawString("( Ban hành theo Thông tư số 200/2014/TT-BTC", FontSmall,
                    Brushes.Black, new RectangleF(rightRect.Left, rightRect.Top + 16, rightRect.Width, 14), sf);

                g.DrawString("ngày 22/12/2014 của Bộ Tài chính )", FontSmall,
                    Brushes.Black, new RectangleF(rightRect.Left, rightRect.Top + 30, rightRect.Width, 14), sf);
            }

            y += 86;
        }

        // ------------------------------------------------------------------ //
        // File: WarehouseIssuesRenderer.cs
        // Class: WarehouseIssuesRenderer
        // Sửa hàm DrawTitleBlock — thay toàn bộ nội dung hàm

        private void DrawTitleBlock(Graphics g, ref int y, int x, int width,
            WarehouseIssuesPrintData data)
        {
            using (var sf = new StringFormat { Alignment = StringAlignment.Center })
            {
                g.DrawString("PHIẾU XUẤT KHO",
                    new Font("Times New Roman", 18f, FontStyle.Bold),
                    Brushes.Black, new RectangleF(x, y, width, 28), sf);
            }
            y += 30;

            using (var sf = new StringFormat { Alignment = StringAlignment.Center })
            {
                g.DrawString(data.NgayIn ?? "",
                    new Font("Times New Roman", 12f, FontStyle.Regular),
                    Brushes.Black, new RectangleF(x, y, width, 20), sf);
            }

            int rightX = x + (int)(width * 0.72);
            int valueOffset = 30;

            g.DrawString("Nợ:", FontNormal, Brushes.Black, rightX, y + 4);
            g.DrawString(data.So ?? "", FontNormal, Brushes.Black, rightX + valueOffset, y + 4);

            y += 24;

            // "Số:" căn giữa toàn bộ width
            using (var sf = new StringFormat { Alignment = StringAlignment.Center })
            {
                string soText = $"Số :  {data.SoPhieu ?? ""}";
                g.DrawString(soText,
                    new Font("Times New Roman", 12f, FontStyle.Regular),
                    Brushes.Black, new RectangleF(x, y, width, 20), sf);
            }

            g.DrawString("Có:", FontNormal, Brushes.Black, rightX, y);
            g.DrawString(data.Co ?? "", FontNormal, Brushes.Black, rightX + valueOffset, y);

            y += 30;
        }

        // ------------------------------------------------------------------ //
        private void DrawInfoBlock(Graphics g, ref int y, int x, int width,
            WarehouseIssuesPrintData data)
        {
            int labelW = 200, lineH = 22;

            DrawInfoLine(g, x, y, labelW, "Họ và tên người nhận hàng:", data.NguoiNhan);
            y += lineH;

            DrawInfoLine(g, x, y, labelW, "Lý do xuất kho :", data.LyDoXuat);
            y += lineH;

            DrawInfoLine(g, x, y, labelW, "Xuất tại kho:", data.XuatTaiKho);
            y += lineH;
        }

        private void DrawInfoLine(Graphics g, int x, int y, int labelW, string label, string value)
        {
            g.DrawString(label, FontNormal, Brushes.Black, x, y);
            g.DrawString(value ?? "", new Font("Times New Roman", 12f, FontStyle.Italic),
                Brushes.Black, x + labelW, y);
        }

        // ------------------------------------------------------------------ //
        // File: WarehouseIssuesRenderer.cs
        // Class: WarehouseIssuesRenderer
        // Sửa hàm DrawIssuesTable — thay toàn bộ nội dung hàm

        private void DrawIssuesTable(Graphics g, ref int y, int x, int width,
            WarehouseIssuesPrintData data)
        {
            var items = data.Items ?? new List<WarehouseIssuesItem>();

            // STT | Tên hàng | Mã số | ĐVT | Số lượng | Đơn giá | Thành tiền
            int[] colW = new[]
            {
        34,   // STT
        270,  // Tên hàng
        100,  // Mã số
        50,   // ĐVT
        90,   // Số lượng
        width - (34 + 270 + 100 + 50 + 90 + 90), // Đơn giá (fill)
        90    // Thành tiền
    };

            int h = 56, rowMinH = 28, totalH = 28;

            int c0 = x;
            int c1 = c0 + colW[0];
            int c2 = c1 + colW[1];
            int c3 = c2 + colW[2];
            int c4 = c3 + colW[3];
            int c5 = c4 + colW[4];
            int c6 = c5 + colW[5];

            // Header
            DrawCell(g, "Stt", FontBold, c0, y, colW[0], h, StringAlignment.Center, StringAlignment.Center);
            DrawCell(g, "Tên, nhãn hiệu, quy cách,\nphẩm chất vật tư (sản\nphẩm hàng hóa)",
                FontBold, c1, y, colW[1], h, StringAlignment.Center, StringAlignment.Center);
            DrawCell(g, "Mã số", FontBold, c2, y, colW[2], h, StringAlignment.Center, StringAlignment.Center);
            DrawCell(g, "Đơn vị\ntính", FontBold, c3, y, colW[3], h, StringAlignment.Center, StringAlignment.Center);
            DrawCell(g, "Số lượng", FontBold, c4, y, colW[4], h, StringAlignment.Center, StringAlignment.Center);
            DrawCell(g, "Đơn giá", FontBold, c5, y, colW[5], h, StringAlignment.Center, StringAlignment.Center);
            DrawCell(g, "Thành tiền", FontBold, c6, y, colW[6], h, StringAlignment.Center, StringAlignment.Center);

            y += h;

            foreach (var item in items)
            {
                int rowH = Math.Max(rowMinH,
                    MeasureCellTextHeight(g, item.Ten, FontNormal, colW[1] - 8) + 8);

                DrawCell(g, item.No.ToString(), FontNormal, c0, y, colW[0], rowH, StringAlignment.Center, StringAlignment.Center);
                DrawCell(g, item.Ten ?? "", FontNormal, c1, y, colW[1], rowH, StringAlignment.Near, StringAlignment.Center);
                DrawCell(g, item.Ma ?? "", FontNormal, c2, y, colW[2], rowH, StringAlignment.Near, StringAlignment.Center);
                DrawCell(g, item.DonVi ?? "", FontNormal, c3, y, colW[3], rowH, StringAlignment.Center, StringAlignment.Center);
                DrawCell(g, item.SoLuong ?? "", FontNormal, c4, y, colW[4], rowH, StringAlignment.Far, StringAlignment.Center);
                DrawCell(g, item.DonGia ?? "", FontNormal, c5, y, colW[5], rowH, StringAlignment.Far, StringAlignment.Center);
                DrawCell(g, item.ThanhTien ?? "", FontNormal, c6, y, colW[6], rowH, StringAlignment.Far, StringAlignment.Center);

                y += rowH;
            }

            // Dòng Cộng
            DrawCell(g, "Cộng:", FontBold, c0, y, colW[0] + colW[1], totalH, StringAlignment.Center, StringAlignment.Center);
            DrawCell(g, "", FontNormal, c2, y, colW[2], totalH, StringAlignment.Center, StringAlignment.Center);
            DrawCell(g, "", FontNormal, c3, y, colW[3], totalH, StringAlignment.Center, StringAlignment.Center);
            DrawCell(g, FormatTotal(items.Sum(i => ParseDecimal(i.SoLuong))),
                FontBold, c4, y, colW[4], totalH, StringAlignment.Far, StringAlignment.Center);
            DrawCell(g, "", FontNormal, c5, y, colW[5], totalH, StringAlignment.Center, StringAlignment.Center);
            DrawCell(g, "", FontNormal, c6, y, colW[6], totalH, StringAlignment.Center, StringAlignment.Center);

            y += totalH;
        }

        // ------------------------------------------------------------------ //
        private void DrawTotalLine(Graphics g, ref int y, int x, int width)
        {
            g.DrawString("Tổng số tiền ( viết bằng chữ ) :",
                FontNormal, Brushes.Black, x, y);

            // đường kẻ chấm dài đến hết dòng
            using (var pen = new Pen(Color.Black, 0.5f) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dot })
                g.DrawLine(pen, x + 220, y + 14, x + width, y + 14);

            y += 24;
        }

        // ------------------------------------------------------------------ //
        private void DrawBottomBlock(Graphics g, ref int y, int x, int width,
            WarehouseIssuesPrintData data)
        {
            g.DrawString("Số chứng từ gốc kèm theo :",
                FontNormal, Brushes.Black, x + 6, y);
            y += 34;

            // 4 cột ký tên: Kế toán | Người nhận hàng | Thủ kho | Giám đốc
            int colW = width / 4;

            using (var sfC = new StringFormat { Alignment = StringAlignment.Center })
            {
                string ngayKy = data.NgayIn ?? "";

                // Dòng ngày ký — lệch phải căn trên cột Giám đốc
                g.DrawString(ngayKy, new Font("Times New Roman", 12f, FontStyle.Italic),
                    Brushes.Black,
                    new RectangleF(x + colW * 3, y - 4, colW, 18), sfC);

                string[] titles = new[]
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

        // ------------------------------------------------------------------ //
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