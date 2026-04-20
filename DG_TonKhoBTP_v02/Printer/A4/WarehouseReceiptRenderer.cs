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
        protected override CompanyInfo GetCompanyInfo(WarehouseReceiptPrintData d) => d.Company;
        protected override IReadOnlyList<WarehouseReceiptItem> GetItems(WarehouseReceiptPrintData d) => d.Items;
        protected override SignatureInfo GetSignature(WarehouseReceiptPrintData d) => d.Signature;

        protected override DocumentInfo GetDocumentInfo(WarehouseReceiptPrintData d)
            => new DocumentInfo();

        protected override IReadOnlyList<ColumnDef<WarehouseReceiptItem>> BuildColumns(int totalWidth)
            => Array.Empty<ColumnDef<WarehouseReceiptItem>>();

        public override void Render(Graphics g, Rectangle marginBounds, PrintPageEventArgs e, WarehouseReceiptPrintData data)
        {
            g.TextRenderingHint = TextRenderingHint.AntiAlias;

            int x = marginBounds.Left;
            int y = marginBounds.Top;
            int width = marginBounds.Width;

            DrawHeaderBlock(g, ref y, x, width, data);
            DrawTitleBlock(g, ref y, x, width, data);
            DrawInfoBlock(g, ref y, x, width, data);
            y += 8;
            DrawReceiptTable(g, ref y, x, width, data);
            y += 10;
            DrawBottomBlock(g, ref y, x, width, data);

            e.HasMorePages = false;
        }

        private void DrawHeaderBlock(Graphics g, ref int y, int x, int width, WarehouseReceiptPrintData data)
        {
            var co = data.Company ?? new CompanyInfo();

            int logoW = 95;
            int rightW = 215;
            int gap = 10;
            int centerW = width - logoW - rightW - gap * 2;

            Rectangle logoRect = new Rectangle(x, y, logoW, 78);
            Rectangle centerRect = new Rectangle(x + logoW + gap, y, centerW, 78);
            Rectangle rightRect = new Rectangle(centerRect.Right + gap, y, rightW, 78);

            if (!string.IsNullOrWhiteSpace(co.LogoPath) && File.Exists(co.LogoPath))
            {
                using (var img = Image.FromFile(co.LogoPath))
                {
                    g.DrawImage(img, logoRect);
                }
            }

            g.DrawString(co.CompanyName ?? "", new Font("Times New Roman", 12f, FontStyle.Bold),
                Brushes.Black, centerRect.Left, centerRect.Top + 2);

            DrawWrappedText(
                g,
                co.Address ?? "",
                FontNormal,
                Brushes.Black,
                new RectangleF(centerRect.Left, centerRect.Top + 24, centerRect.Width, 45),
                StringAlignment.Near,
                StringAlignment.Near);
            using (var sf = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Near
            })
            {
                g.DrawString("Mẫu số 20 - HD", new Font("Times New Roman", 10.5f, FontStyle.Bold),
                    Brushes.Black, new RectangleF(rightRect.Left, rightRect.Top, rightRect.Width, 16), sf);

                g.DrawString("( Theo QĐ số: 19/2006/QĐ-BTC", FontSmall,
                    Brushes.Black, new RectangleF(rightRect.Left, rightRect.Top + 16, rightRect.Width, 14), sf);

                g.DrawString("ngày 30 tháng 3 năm 2006", FontSmall,
                    Brushes.Black, new RectangleF(rightRect.Left, rightRect.Top + 30, rightRect.Width, 14), sf);

                g.DrawString("của Bộ trưởng Bộ Tài chính )", FontSmall,
                    Brushes.Black, new RectangleF(rightRect.Left, rightRect.Top + 44, rightRect.Width, 14), sf);
            }

            y += 86;
        }

        private void DrawTitleBlock(Graphics g, ref int y, int x, int width, WarehouseReceiptPrintData data)
        {
            using (var sf = new StringFormat { Alignment = StringAlignment.Center })
            {
                g.DrawString("PHIẾU NHẬP KHO",
                    new Font("Times New Roman", 18f, FontStyle.Bold),
                    Brushes.Black,
                    new RectangleF(x, y, width, 28),
                    sf);
            }
            y += 30;

            using (var sf = new StringFormat { Alignment = StringAlignment.Center })
            {
                g.DrawString(data.NgayIn ?? "",
                    new Font("Times New Roman", 12f, FontStyle.Regular),
                    Brushes.Black,
                    new RectangleF(x, y, width, 20),
                    sf);
            }

            int rightX = x + (int)(width * 0.72);
            int valueOffset = 76;

            g.DrawString("Số PO:", new Font("Times New Roman", 12f, FontStyle.Regular), Brushes.Black, rightX, y + 4);
            g.DrawString(data.SoPO ?? "", new Font("Times New Roman", 12f, FontStyle.Regular), Brushes.Black, rightX + valueOffset, y + 4);

            y += 24;

            g.DrawString("Số phiếu:", new Font("Times New Roman", 12f, FontStyle.Regular), Brushes.Black, rightX, y);
            g.DrawString(data.SoPhieu ?? "", new Font("Times New Roman", 12f, FontStyle.Regular), Brushes.Black, rightX + valueOffset, y);

            y += 26;
        }

        private void DrawInfoBlock(Graphics g, ref int y, int x, int width, WarehouseReceiptPrintData data)
        {
            int labelW = 165;
            int lineH = 22;

            DrawInfoLine(g, x + 60, y, labelW, lineH, "Họ tên người giao hàng:", data.NguoiGiao);
            y += lineH;

            DrawInfoLine(g, x + 60, y, labelW, lineH, "Nhà cung cấp:", data.NhaCungCap);
            y += lineH;
            DrawInfoLine(g, x + 60, y, labelW, lineH, "Lý do nhập kho:", data.LyDoNhap);
            y += lineH;

            DrawInfoLine(g, x + 60, y, labelW, lineH, "Kho hàng:", data.KhoHang);
            y += lineH;
        }

        private void DrawInfoLine(Graphics g, int x, int y, int labelW, int lineH, string label, string value)
        {
            g.DrawString(label, new Font("Times New Roman", 12f, FontStyle.Regular), Brushes.Black, x, y);
            g.DrawString(value ?? "", new Font("Times New Roman", 12f, FontStyle.Regular), Brushes.Black, x + labelW, y);
        }

        private void DrawReceiptTable(Graphics g, ref int y, int x, int width, WarehouseReceiptPrintData data)
        {
            var items = data.Items ?? new List<WarehouseReceiptItem>();

            int[] colW = new[]
            {
                34,  // STT
                250, // Tên hàng
                100, // Mã số
                46,  // ĐVT
                70,  // Yêu cầu
                90,  // Thực nhận
                width - (34 + 250 + 100 + 46 + 70 + 90 ) // Ghi chú
            };

            int h1 = 34;
            int h2 = 28;
            int rowMinH = 28;
            int totalH = 28;

            int c0 = x;
            int c1 = c0 + colW[0];
            int c2 = c1 + colW[1];
            int c3 = c2 + colW[2];
            int c4 = c3 + colW[3];
            int c5 = c4 + colW[4];
            int c6 = c5 + colW[5];
            int c7 = c6 + colW[6];

            // Header tầng 1
            DrawCell(g, "Stt", FontBold, c0, y, colW[0], h1 + h2, StringAlignment.Center, StringAlignment.Center);
            DrawCell(g, "Tên, nhãn hiệu, quy cách,\nphẩm chất vật tư (sản\nphẩm hàng hóa)",
                FontBold, c1, y, colW[1], h1 + h2, StringAlignment.Center, StringAlignment.Center);
            DrawCell(g, "Mã số", FontBold, c2, y, colW[2], h1 + h2, StringAlignment.Center, StringAlignment.Center);
            DrawCell(g, "Đơn vị\ntính", FontBold, c3, y, colW[3], h1 + h2, StringAlignment.Center, StringAlignment.Center);
            DrawCell(g, "Số lượng", FontBold, c4, y, colW[4] + colW[5], h1, StringAlignment.Center, StringAlignment.Center);
            DrawCell(g, "Ghi chú", FontBold, c6, y, colW[6], h1 + h2, StringAlignment.Center, StringAlignment.Center);

            // Header tầng 2
            DrawCell(g, "Yêu cầu", FontNormal, c4, y + h1, colW[4], h2, StringAlignment.Center, StringAlignment.Center);
            DrawCell(g, "Thực nhận", FontNormal, c5, y + h1, colW[5], h2, StringAlignment.Center, StringAlignment.Center);

            y += h1 + h2;

            foreach (var item in items)
            {
                int rowH = rowMinH;

                rowH = Math.Max(rowH, MeasureCellTextHeight(g, item.Ten, FontNormal, colW[1] - 8) + 8);
                rowH = Math.Max(rowH, MeasureCellTextHeight(g, item.GhiChu, FontNormal, colW[6] - 8) + 8);
                DrawCell(g, item.No.ToString(), FontNormal, c0, y, colW[0], rowH, StringAlignment.Center, StringAlignment.Center);
                DrawCell(g, item.Ten ?? "", FontNormal, c1, y, colW[1], rowH, StringAlignment.Near, StringAlignment.Center);
                DrawCell(g, item.Ma ?? "", FontNormal, c2, y, colW[2], rowH, StringAlignment.Near, StringAlignment.Center);
                DrawCell(g, item.DonVi ?? "", FontNormal, c3, y, colW[3], rowH, StringAlignment.Center, StringAlignment.Center);
                DrawCell(g, item.YeuCau ?? "", FontNormal, c4, y, colW[4], rowH, StringAlignment.Far, StringAlignment.Center);
                DrawCell(g, item.ThucNhan ?? "", FontNormal, c5, y, colW[5], rowH, StringAlignment.Far, StringAlignment.Center);
                DrawCell(g, item.GhiChu ?? "", FontNormal, c6, y, colW[6], rowH, StringAlignment.Near, StringAlignment.Center);

                y += rowH;
            }

            // Dòng cộng
            string totalYeuCau = FormatTotal(items.Sum(i => ParseDecimal(i.YeuCau)));
            string totalThucNhan = FormatTotal(items.Sum(i => ParseDecimal(i.ThucNhan)));

            DrawCell(g, "Cộng:", FontBold, c0, y, colW[0] + colW[1], totalH, StringAlignment.Center, StringAlignment.Center);
            DrawCell(g, "", FontNormal, c2, y, colW[2], totalH, StringAlignment.Center, StringAlignment.Center);
            DrawCell(g, "", FontNormal, c3, y, colW[3], totalH, StringAlignment.Center, StringAlignment.Center);
            DrawCell(g, totalYeuCau, FontBold, c4, y, colW[4], totalH, StringAlignment.Far, StringAlignment.Center);
            DrawCell(g, totalThucNhan, FontBold, c5, y, colW[5], totalH, StringAlignment.Far, StringAlignment.Center);
            DrawCell(g, "", FontNormal, c6, y, colW[6], totalH, StringAlignment.Center, StringAlignment.Center);

            y += totalH;
        }

        private void DrawBottomBlock(Graphics g, ref int y, int x, int width, WarehouseReceiptPrintData data)
        {
            g.DrawString("Số chứng từ gốc kèm theo :", new Font("Times New Roman", 12f, FontStyle.Regular),
                Brushes.Black, x + 6, y);

            y += 34;

            int leftW = width / 3;
            int centerW = width / 3;
            int rightW = width - leftW - centerW;

            int leftX = x;
            int centerX = x + leftW;
            int rightX = centerX + centerW;

            string ngayKy = data.NgayIn ?? "";

            using (var sfCenter = new StringFormat { Alignment = StringAlignment.Center })
            {
                g.DrawString(data.Signature?.FactoryDirectorTitle ?? "Giám đốc nhà máy",
                    new Font("Times New Roman", 12f, FontStyle.Bold),
                    Brushes.Black, new RectangleF(leftX, y, leftW, 20), sfCenter);

                g.DrawString("(Ký, họ tên)",
                    new Font("Times New Roman", 12f, FontStyle.Italic),
                    Brushes.Black, new RectangleF(leftX, y + 22, leftW, 18), sfCenter);

                g.DrawString(data.Signature?.CheckerTitle ?? "Kế toán",
                    new Font("Times New Roman", 12f, FontStyle.Bold),
                    Brushes.Black, new RectangleF(centerX, y, centerW, 20), sfCenter);

                g.DrawString("(Ký, họ tên)",
                    new Font("Times New Roman", 12f, FontStyle.Italic),
                    Brushes.Black, new RectangleF(centerX, y + 22, centerW, 18), sfCenter);

                g.DrawString(ngayKy,
                    new Font("Times New Roman", 12f, FontStyle.Italic),
                    Brushes.Black, new RectangleF(rightX, y - 4, rightW, 18), sfCenter);

                g.DrawString(data.Signature?.RequesterTitle ?? "Thủ kho",
                    new Font("Times New Roman", 12f, FontStyle.Bold),
                    Brushes.Black, new RectangleF(rightX, y + 18, rightW, 20), sfCenter);

                g.DrawString("(Ký, họ tên)",
                    new Font("Times New Roman", 12f, FontStyle.Italic),
                    Brushes.Black, new RectangleF(rightX, y + 40, rightW, 18), sfCenter);
            }

            y += 120;
        }

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
            if (decimal.TryParse(value, out var d))
                return d;

            return 0m;
        }

        private string FormatTotal(decimal value)
        {
            return value.ToString("0.0");
        }
    }
}