
using DG_TonKhoBTP_v02.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace DG_TonKhoBTP_v02.Printer
{
    public class PrintHelper
    {
        private static List<string> WrapText(string text, int maxCharsPerLine = 42)
        {
            var result = new List<string>();
            if (string.IsNullOrWhiteSpace(text)) return result;

            var words = text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var line = new StringBuilder();

            foreach (var w in words)
            {
                if (line.Length == 0) line.Append(w);
                else if (line.Length + 1 + w.Length <= maxCharsPerLine) line.Append(' ').Append(w);
                else { result.Add(line.ToString()); line.Clear(); line.Append(w); }
            }
            if (line.Length > 0) result.Add(line.ToString());

            return result;
        }

        public static void PrintLabel(PrinterModel printer)
        {
            string qrLabel= $"{printer.MaBin}_{printer.KhoiLuong}kg_{printer.ChieuDai}m";

            string qr = Qr.GeneratedQr(qrLabel);

            string kl_cdBin = printer.KhoiLuong + "(kg)" + (printer.ChieuDai != "" ? "_" + printer.ChieuDai + "(m)" : "");


            var lines = new List<TextLineSpec>
            {
                new TextLineSpec { Content = "PHIEU QUAN LY SAN PHAM", X = 180 },
                new TextLineSpec { Prefix = new string(' ',27) +"BQ-ISO-09-08"},
                new TextLineSpec { Prefix = "Ngay SX: " + Helper.Helper.TaoKhoangTrong(25,printer.NgaySX) + " Ca SX:", Content = printer.NgaySX + new string(' ',12)  + printer.CaSX, X = 170 },
                new TextLineSpec { Prefix = "Thong tin Bin: ", Content = kl_cdBin, X = 300 },
                new TextLineSpec { Prefix = "San Pham: ",    Content = printer.TenSP, X = 300 },
                new TextLineSpec { Prefix = "Ma san pham: ", Content = printer.MaBin, X = 300 },
                new TextLineSpec { Prefix = "Quy cach: ",    Content = printer.TenSP, X = 300 },
                new TextLineSpec { Prefix = "Danh Gia: ",    Content = printer.DanhGia, X = 270 },
                new TextLineSpec { Prefix = "CN van hanh: " + Helper.Helper.TaoKhoangTrong(15,printer.TenCN) + " T.truong:", Content = printer.TenCN, X = 250 },
            }; 


            // --- Ghi chu: tự động xuống dòng ---
            var ghiChuLines = WrapText(printer.GhiChu ?? string.Empty, maxCharsPerLine: 35);
            if (ghiChuLines.Count == 0)
            {
                lines.Add(new TextLineSpec { Prefix = "Ghi chu: ", Content = string.Empty, X = 175 });
            }
            else
            {
                for (int i = 0; i < ghiChuLines.Count; i++)
                {
                    lines.Add(new TextLineSpec
                    {
                        Prefix = i == 0 ? "Ghi chu: " : new string(' ', 10), // dòng sau thụt nhẹ
                        Content = ghiChuLines[i],
                        X = 175
                    });
                }
            }

            // các dòng còn lại giữ nguyên
            lines.Add(new TextLineSpec { Prefix = new string(' ', 5) + "KCS ", Content = qr, X = 400 });
            lines.Add(new TextLineSpec { Content = new string(' ', 1), X = 0 });
            lines.Add(new TextLineSpec { Content = new string(' ', 5), X = 0 });
            lines.Add(new TextLineSpec { Content = new string(' ', 5), X = 0 });
            lines.Add(new TextLineSpec { Content = new string(' ', 5), X = 0 });
            lines.Add(new TextLineSpec { Content = new string(' ', 5), X = 0 });
            lines.Add(new TextLineSpec { Content = new string('.', 1), X = 0});

            string textBlock = SbplTextBuilder.BuildMultiLineTextBlock(lines, startX: 30, startY: 10, lineSpacing: 50);

            MainPrinter.PrintUpperTextAndQr(textBlock);
        }
    }
}
