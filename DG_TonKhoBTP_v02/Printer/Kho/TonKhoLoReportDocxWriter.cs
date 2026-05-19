using DG_TonKhoBTP_v02.Models.Kho;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DG_TonKhoBTP_v02.Printer.Kho
{
    /// <summary>
    /// Writer mới cho báo cáo tồn kho loại Lô.
    ///
    /// Khác với LotCodeDocxWriter cũ:
    /// - Writer cũ render từng LotCode độc lập: (lot)MauSac/KHSX.
    /// - Writer mới render theo group KHSX + MauSac: (lot1 + lot2 + lot3)MauSac/KHSX.
    /// - MauSac và KHSX chỉ xuất hiện một lần ở cuối group.
    /// </summary>
    public static class TonKhoLoReportDocxWriter
    {
        private const string FontName = "Times New Roman";

        private const string ColorHeaderFill = "3366FF";
        private const string ColorHeaderText = "FFFFFF";
        private const string ColorLabelRed = "FF0000";
        private const string ColorLotBlue = "0000FF";
        private const string ColorProductGreen = "004000";
        private const string ColorBlack = "000000";

        private const string FontSizeTitle = "32";       // 16pt
        private const string FontSizeHeader = "26";      // 13pt
        private const string FontSizeNormal = "24";      // 12pt
        private const string FontSizeLotBase = "26";     // 13pt
        private const string FontSizeLotSubSup = "18";   // 9pt
        private const string FontSizeGroupName = "28";   // 14pt

        public static void Write(TonKhoLoReport report, string filePath)
        {
            if (report == null)
                throw new ArgumentNullException(nameof(report));

            using var doc = WordprocessingDocument.Create(filePath, WordprocessingDocumentType.Document);

            var mainPart = doc.AddMainDocumentPart();
            mainPart.Document = new Document(new Body());

            Body body = mainPart.Document.Body!;

            body.AppendChild(BuildTitleParagraph(report.CreatedAt));

            foreach (TonKhoLoReportGroup group in report.Groups)
            {
                body.AppendChild(BuildGroupNameParagraph(group.TenNhom));
                body.AppendChild(BuildGroupTable(group));
                body.AppendChild(EmptyParagraph("120"));
            }

            body.AppendChild(BuildLandscapeSectionProperties());
            mainPart.Document.Save();
        }

        private static Paragraph BuildTitleParagraph(DateTime createdAt)
        {
            var para = new Paragraph();
            para.AppendChild(new ParagraphProperties(
                new Justification { Val = JustificationValues.Center },
                new SpacingBetweenLines { After = "240" }));

            para.AppendChild(TextRun(
                $"BÁO CÁO TỒN KHO ĐG - {createdAt:dd/MM/yyyy HH:mm}",
                ColorBlack,
                FontSizeTitle,
                bold: true));

            return para;
        }

        private static Paragraph BuildGroupNameParagraph(string tenNhom)
        {
            var para = new Paragraph();
            para.AppendChild(new ParagraphProperties(
                new SpacingBetweenLines { Before = "160", After = "80" }));

            para.AppendChild(TextRun(
                string.IsNullOrWhiteSpace(tenNhom) ? "Khác" : tenNhom,
                ColorBlack,
                FontSizeGroupName,
                bold: true));

            return para;
        }

        private static Table BuildGroupTable(TonKhoLoReportGroup group)
        {
            var table = new Table();

            table.AppendChild(new TableProperties(
                new TableWidth { Width = "5000", Type = TableWidthUnitValues.Pct },
                BuildTableBorders()));

            table.AppendChild(new TableGrid(
                new GridColumn { Width = "3000" },
                new GridColumn { Width = "6200" },
                new GridColumn { Width = "6200" }));

            table.AppendChild(BuildHeaderRow());

            foreach (TonKhoLoProductRow row in group.Rows)
            {
                table.AppendChild(BuildDataRow(row));
            }

            return table;
        }

        private static TableRow BuildHeaderRow()
        {
            var row = new TableRow();

            row.AppendChild(BuildHeaderCell("Tên sản phẩm"));
            row.AppendChild(BuildHeaderCell("Hàng bán"));
            row.AppendChild(BuildHeaderCell("Hàng đặt + Hàng gửi"));

            return row;
        }

        private static TableRow BuildDataRow(TonKhoLoProductRow model)
        {
            var row = new TableRow();

            row.AppendChild(BuildProductNameCell(model.TenSanPham));
            row.AppendChild(BuildLotContentCell(model.HangBan));
            row.AppendChild(BuildLotContentCell(model.HangDat));

            return row;
        }

        private static TableCell BuildHeaderCell(string text)
        {
            var cell = new TableCell();
            cell.AppendChild(new TableCellProperties(
                new TableCellWidth { Width = "0", Type = TableWidthUnitValues.Auto },
                new Shading { Val = ShadingPatternValues.Clear, Color = "auto", Fill = ColorHeaderFill },
                new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center }));

            var para = new Paragraph();
            para.AppendChild(new ParagraphProperties(new Justification { Val = JustificationValues.Center }));
            para.AppendChild(TextRun(text, ColorHeaderText, FontSizeHeader, bold: true));
            cell.AppendChild(para);

            return cell;
        }

        private static TableCell BuildProductNameCell(string text)
        {
            var cell = new TableCell();
            cell.AppendChild(new TableCellProperties(
                new TableCellWidth { Width = "3000", Type = TableWidthUnitValues.Dxa },
                new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center }));

            var para = new Paragraph();
            para.AppendChild(new ParagraphProperties(new Justification { Val = JustificationValues.Center }));
            para.AppendChild(TextRun(text, ColorProductGreen, FontSizeNormal, bold: true));
            cell.AppendChild(para);

            return cell;
        }

        private static TableCell BuildLotContentCell(TonKhoLoCell content)
        {
            var cell = new TableCell();
            cell.AppendChild(new TableCellProperties(
                new TableCellWidth { Width = "6200", Type = TableWidthUnitValues.Dxa },
                new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Top }));

            // Theo yêu cầu: sau nhãn Lô: xuống dòng.
            cell.AppendChild(BuildLabelParagraph("Lô:"));
            cell.AppendChild(BuildGroupsParagraph(content.LoGroups));

            // Tạo khoảng cách nhẹ giữa Lô và Lẻ.
            cell.AppendChild(EmptyParagraph("40"));

            // Theo yêu cầu: luôn hiển thị nhãn Lẻ:, dù không có dữ liệu bên dưới.
            cell.AppendChild(BuildLabelParagraph("Lẻ:"));
            cell.AppendChild(BuildGroupsParagraph(content.LeGroups));

            return cell;
        }

        private static Paragraph BuildLabelParagraph(string label)
        {
            var para = new Paragraph();
            para.AppendChild(new ParagraphProperties(
                new SpacingBetweenLines { Before = "0", After = "0" }));

            para.AppendChild(TextRun(label, ColorLabelRed, FontSizeNormal, bold: true));
            return para;
        }

        private static Paragraph BuildGroupsParagraph(List<TonKhoLotCodeGroup> groups)
        {
            var para = new Paragraph();
            para.AppendChild(new ParagraphProperties(
                new SpacingBetweenLines { Before = "0", After = "0", Line = "300", LineRule = LineSpacingRuleValues.Auto }));

            if (groups == null || groups.Count == 0)
                return para;

            bool firstGroup = true;
            foreach (TonKhoLotCodeGroup group in groups)
            {
                if (!firstGroup)
                    para.AppendChild(LotPlainRun(" + "));

                AppendLotGroup(para, group);
                firstGroup = false;
            }

            return para;
        }

        /// <summary>
        /// Render một group chung KHSX + MauSac:
        /// (lot1 + lot2 + lot3) MauSac_sup KHSX_sub
        /// </summary>
        private static void AppendLotGroup(Paragraph para, TonKhoLotCodeGroup group)
        {
            para.AppendChild(LotPlainRun("("));

            bool firstLot = true;
            foreach (LotCode lot in group.Lots)
            {
                if (!firstLot)
                    para.AppendChild(LotPlainRun(" + "));

                AppendLotAtom(para, lot);
                firstLot = false;
            }

            para.AppendChild(LotPlainRun(")"));

            // Giữ đúng tinh thần writer cũ: MauSac là superscript, KHSX là subscript của dấu đóng ngoặc.
            // Không in lại MauSac/KHSX sau từng lot; chỉ in một lần ở cuối group.
            para.AppendChild(LotSupRun(group.MauSac));
            para.AppendChild(LotSubRun(group.KHSX));
        }

        /// <summary>
        /// Render phần atom của một lô, tức phần nằm trong ngoặc.
        /// Tách từ writer cũ: sub(ChieuCaoLo) + SoM + sup(SoThuTuBin/TenKhach(SoCuoi)) + sub(SoDau).
        /// </summary>
        private static void AppendLotAtom(Paragraph para, LotCode lot)
        {
            para.AppendChild(LotSubRun(lot.ChieuCaoLo));
            para.AppendChild(LotPlainRun(lot.SoM));

            string supText = $"{lot.SoThuTuBin}/{lot.TenKhach}({lot.SoCuoi})";
            para.AppendChild(LotSupRun(supText));

            para.AppendChild(LotSubRun(lot.SoDau));
        }

        private static TableBorders BuildTableBorders()
        {
            return new TableBorders(
                new TopBorder { Val = BorderValues.Single, Size = 6, Color = ColorBlack },
                new BottomBorder { Val = BorderValues.Single, Size = 6, Color = ColorBlack },
                new LeftBorder { Val = BorderValues.Single, Size = 6, Color = ColorBlack },
                new RightBorder { Val = BorderValues.Single, Size = 6, Color = ColorBlack },
                new InsideHorizontalBorder { Val = BorderValues.Single, Size = 6, Color = ColorBlack },
                new InsideVerticalBorder { Val = BorderValues.Single, Size = 6, Color = ColorBlack });
        }

        private static Paragraph EmptyParagraph(string after = "0")
        {
            var para = new Paragraph();
            para.AppendChild(new ParagraphProperties(new SpacingBetweenLines { After = after }));
            para.AppendChild(TextRun(string.Empty, ColorBlack, FontSizeNormal));
            return para;
        }

        private static SectionProperties BuildLandscapeSectionProperties()
        {
            // A4 ngang: 297mm x 210mm ≈ 16838 x 11906 twips.
            return new SectionProperties(
                new PageSize
                {
                    Width = 16838U,
                    Height = 11906U,
                    Orient = PageOrientationValues.Landscape
                },
                new PageMargin
                {
                    Top = 720,
                    Bottom = 720,
                    Left = 720,
                    Right = 720,
                    Header = 360,
                    Footer = 360,
                    Gutter = 0
                });
        }

        private static Run TextRun(string text, string color, string fontSize, bool bold = false)
        {
            var rPr = BuildRunProperties(color, fontSize, bold, vertical: null);
            var run = new Run();
            run.AppendChild(rPr);
            run.AppendChild(new Text(text ?? string.Empty) { Space = SpaceProcessingModeValues.Preserve });
            return run;
        }

        private static Run LotPlainRun(string text)
        {
            var rPr = BuildRunProperties(ColorLotBlue, FontSizeLotBase, bold: true, vertical: null);
            var run = new Run();
            run.AppendChild(rPr);
            run.AppendChild(new Text(text ?? string.Empty) { Space = SpaceProcessingModeValues.Preserve });
            return run;
        }

        private static Run LotSubRun(string text)
        {
            var rPr = BuildRunProperties(ColorLotBlue, FontSizeLotSubSup, bold: true, vertical: VerticalPositionValues.Subscript);
            var run = new Run();
            run.AppendChild(rPr);
            run.AppendChild(new Text(text ?? string.Empty) { Space = SpaceProcessingModeValues.Preserve });
            return run;
        }

        private static Run LotSupRun(string text)
        {
            var rPr = BuildRunProperties(ColorLotBlue, FontSizeLotSubSup, bold: true, vertical: VerticalPositionValues.Superscript);
            var run = new Run();
            run.AppendChild(rPr);
            run.AppendChild(new Text(text ?? string.Empty) { Space = SpaceProcessingModeValues.Preserve });
            return run;
        }

        private static RunProperties BuildRunProperties(
            string color,
            string fontSize,
            bool bold,
            VerticalPositionValues? vertical)
        {
            var rPr = new RunProperties();
            rPr.AppendChild(new RunFonts { Ascii = FontName, HighAnsi = FontName, EastAsia = FontName });
            rPr.AppendChild(new FontSize { Val = fontSize });
            rPr.AppendChild(new Color { Val = color });

            if (bold)
                rPr.AppendChild(new Bold());

            if (vertical.HasValue)
                rPr.AppendChild(new VerticalTextAlignment { Val = vertical.Value });

            return rPr;
        }
    }
}
