using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DG_TonKhoBTP_v02.Models.Kho
{
    public static class LotCodeDocxWriter
    {
        /// <summary>
        /// Tạo file .docx chứa chuỗi LotCode với đầy đủ sub/superscript.
        /// </summary>
        /// <param name="model">Dữ liệu LotCode đã điền sẵn</param>
        /// <param name="filePath">Đường dẫn file đầu ra (vd: "C:\output\lot.docx")</param>
        public static void Write(LotCode model, string filePath)
        {
            using var doc = WordprocessingDocument.Create(
                filePath, WordprocessingDocumentType.Document);

            // ── Main document part ──────────────────────────────────────
            var mainPart = doc.AddMainDocumentPart();
            mainPart.Document = new Document(new Body());

            SetPageMargins(mainPart);

            var body = mainPart.Document.Body!;
            body.AppendChild(BuildLotCodeParagraph(model));

            // Đảm bảo body kết thúc đúng chuẩn OOXML
            body.AppendChild(new SectionProperties());

            mainPart.Document.Save();
        }

        // ── Page margin ─────────────────────────────────────────────────
        private static void SetPageMargins(MainDocumentPart mainPart)
        {
            var sectPr = new SectionProperties(
                new PageMargin
                {
                    Top = (int)LotCodeDocxConfig.MarginTopTwip,
                    Bottom = (int)LotCodeDocxConfig.MarginBotTwip,
                    Left = LotCodeDocxConfig.MarginLeftTwip,
                    Right = LotCodeDocxConfig.MarginRightTwip
                });

            // Đặt vào cuối body (sẽ bị AppendChild sau nên chỉ dùng như cấu hình mặc định)
            // Thực tế SectionProperties cuối body = page settings
        }

        // ── Build paragraph ─────────────────────────────────────────────
        /// <summary>
        /// Xây dựng 1 Paragraph chứa toàn bộ chuỗi với định dạng:
        ///   plain("(")  sub(B)  plain(A)  sup("D/E(F)")  sub(C)  plain(")")  sup(H)  sub(G)
        /// </summary>
        private static Paragraph BuildLotCodeParagraph(LotCode m)
        {
            var para = new Paragraph();
            var pPr = new ParagraphProperties(
                new SpacingBetweenLines { Line = "360", LineRule = LineSpacingRuleValues.Auto });
            para.AppendChild(pPr);

            // ( — plain
            para.AppendChild(PlainRun("("));

            // ChieuCao — subscript bên trái của SoM
            para.AppendChild(SubRun(m.ChieuCao));

            // SoM — ký tự chính
            para.AppendChild(PlainRun(m.SoM));

            // SoThuTu/TenKhach(ChiSoNgoai) — superscript của SoM
            string supText = $"{m.SoThuTu}/{m.TenKhach}({m.SoCuoi})";
            para.AppendChild(SupRun(supText));

            // ChiSoDau — subscript bên phải của SoM
            para.AppendChild(SubRun(m.SoDau));

            // ) — plain
            para.AppendChild(PlainRun(")"));

            // MauSac — superscript của )
            para.AppendChild(SupRun(m.MauSac));

            // KeHoachSanXuat — subscript của )
            para.AppendChild(SubRun(m.KHSX));

            return para;
        }

        // ── Run factories ───────────────────────────────────────────────

        private static Run PlainRun(string text)
        {
            var rPr = new RunProperties();
            rPr.AppendChild(new RunFonts { Ascii = LotCodeDocxConfig.FontName, HighAnsi = LotCodeDocxConfig.FontName });
            rPr.AppendChild(new FontSize { Val = LotCodeDocxConfig.BaseFontSizeHp.ToString() });

            var run = new Run();
            run.AppendChild(rPr);
            run.AppendChild(new Text(text) { Space = SpaceProcessingModeValues.Preserve });
            return run;
        }

        private static Run SubRun(string text)
        {
            var rPr = new RunProperties();
            rPr.AppendChild(new RunFonts { Ascii = LotCodeDocxConfig.FontName, HighAnsi = LotCodeDocxConfig.FontName });
            rPr.AppendChild(new FontSize { Val = LotCodeDocxConfig.SubSuperSizeHp.ToString() });
            rPr.AppendChild(new VerticalTextAlignment
            { Val = VerticalPositionValues.Subscript });

            var run = new Run();
            run.AppendChild(rPr);
            run.AppendChild(new Text(text) { Space = SpaceProcessingModeValues.Preserve });
            return run;
        }

        private static Run SupRun(string text)
        {
            var rPr = new RunProperties();
            rPr.AppendChild(new RunFonts { Ascii = LotCodeDocxConfig.FontName, HighAnsi = LotCodeDocxConfig.FontName });
            rPr.AppendChild(new FontSize { Val = LotCodeDocxConfig.SubSuperSizeHp.ToString() });
            rPr.AppendChild(new VerticalTextAlignment
            { Val = VerticalPositionValues.Superscript });

            var run = new Run();
            run.AppendChild(rPr);
            run.AppendChild(new Text(text) { Space = SpaceProcessingModeValues.Preserve });
            return run;
        }
    }

}
