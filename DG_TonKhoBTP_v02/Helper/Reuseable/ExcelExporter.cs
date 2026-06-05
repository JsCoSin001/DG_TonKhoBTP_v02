using ClosedXML.Excel;
using DG_TonKhoBTP_v02.UI;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace DG_TonKhoBTP_v02.Helper
{
    public enum ExcelExportTextFormat
    {
        Unicode = 0,
        TCVN = 1,
        TCVN3 = TCVN
    }

    public static class ExcelExporter
    {
        private const string DefaultTcvnFontName = ".VnTime";

        private static readonly Dictionary<char, string> UnicodeToTcvn3Map = new Dictionary<char, string>
        {
            // A/a
            ['À'] = "Aµ", ['Á'] = "A¸", ['Ả'] = "A¶", ['Ã'] = "A·", ['Ạ'] = "A¹",
            ['Ă'] = "¡", ['Ắ'] = "¡¾", ['Ằ'] = "¡»", ['Ẳ'] = "¡¼", ['Ẵ'] = "¡½", ['Ặ'] = "¡Æ",
            ['Â'] = "¢", ['Ấ'] = "¢Ê", ['Ầ'] = "¢Ç", ['Ẩ'] = "¢È", ['Ẫ'] = "¢É", ['Ậ'] = "¢Ë",
            ['à'] = "µ", ['á'] = "¸", ['ả'] = "¶", ['ã'] = "·", ['ạ'] = "¹",
            ['ă'] = "¨", ['ắ'] = "¾", ['ằ'] = "»", ['ẳ'] = "¼", ['ẵ'] = "½", ['ặ'] = "Æ",
            ['â'] = "©", ['ấ'] = "Ê", ['ầ'] = "Ç", ['ẩ'] = "È", ['ẫ'] = "É", ['ậ'] = "Ë",

            // D/d
            ['Đ'] = "§", ['đ'] = "®",

            // E/e
            ['È'] = "EÌ", ['É'] = "EÐ", ['Ẻ'] = "EÎ", ['Ẽ'] = "EÏ", ['Ẹ'] = "EÑ",
            ['Ê'] = "£", ['Ế'] = "£Õ", ['Ề'] = "£Ò", ['Ể'] = "£Ó", ['Ễ'] = "£Ô", ['Ệ'] = "£Ö",
            ['è'] = "Ì", ['é'] = "Ð", ['ẻ'] = "Î", ['ẽ'] = "Ï", ['ẹ'] = "Ñ",
            ['ê'] = "ª", ['ế'] = "Õ", ['ề'] = "Ò", ['ể'] = "Ó", ['ễ'] = "Ô", ['ệ'] = "Ö",

            // I/i
            ['Ì'] = "I×", ['Í'] = "IÝ", ['Ỉ'] = "IØ", ['Ĩ'] = "IÜ", ['Ị'] = "IÞ",
            ['ì'] = "×", ['í'] = "Ý", ['ỉ'] = "Ø", ['ĩ'] = "Ü", ['ị'] = "Þ",

            // O/o
            ['Ò'] = "Oß", ['Ó'] = "Oã", ['Ỏ'] = "Oá", ['Õ'] = "Oâ", ['Ọ'] = "Oä",
            ['Ô'] = "¤", ['Ố'] = "¤è", ['Ồ'] = "¤å", ['Ổ'] = "¤æ", ['Ỗ'] = "¤ç", ['Ộ'] = "¤é",
            ['Ơ'] = "¥", ['Ớ'] = "¥í", ['Ờ'] = "¥ê", ['Ở'] = "¥ë", ['Ỡ'] = "¥ì", ['Ợ'] = "¥î",
            ['ò'] = "ß", ['ó'] = "ã", ['ỏ'] = "á", ['õ'] = "â", ['ọ'] = "ä",
            ['ô'] = "«", ['ố'] = "è", ['ồ'] = "å", ['ổ'] = "æ", ['ỗ'] = "ç", ['ộ'] = "é",
            ['ơ'] = "¬", ['ớ'] = "í", ['ờ'] = "ê", ['ở'] = "ë", ['ỡ'] = "ì", ['ợ'] = "î",

            // U/u
            ['Ù'] = "Uï", ['Ú'] = "Uó", ['Ủ'] = "Uñ", ['Ũ'] = "Uò", ['Ụ'] = "Uô",
            ['Ư'] = "¦", ['Ứ'] = "¦ø", ['Ừ'] = "¦õ", ['Ử'] = "¦ö", ['Ữ'] = "¦÷", ['Ự'] = "¦ù",
            ['ù'] = "ï", ['ú'] = "ó", ['ủ'] = "ñ", ['ũ'] = "ò", ['ụ'] = "ô",
            ['ư'] = "\u00AD", ['ứ'] = "ø", ['ừ'] = "õ", ['ử'] = "ö", ['ữ'] = "÷", ['ự'] = "ù",

            // Y/y
            ['Ỳ'] = "Yú", ['Ý'] = "Yý", ['Ỷ'] = "Yû", ['Ỹ'] = "Yü", ['Ỵ'] = "Yþ",
            ['ỳ'] = "ú", ['ý'] = "ý", ['ỷ'] = "û", ['ỹ'] = "ü", ['ỵ'] = "þ"
        };

        public static void Export(
            DataTable table,
            string defaultFileName = "Report",
            ExcelExportTextFormat textFormat = ExcelExportTextFormat.Unicode)
        {
            if (table == null || table.Rows.Count == 0)
            {
                FrmWaiting.ShowGifAlert("Không có dữ liệu để xuất.", "Export", EnumStore.Icon.Warning);
                return;
            }

            using var sfd = new SaveFileDialog
            {
                Title = "Xuất báo cáo Excel",
                Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                FileName = $"{defaultFileName}_{DateTime.Now:yyyyMMdd_HHmm}"
            };

            if (sfd.ShowDialog() != DialogResult.OK)
            {
                FrmWaiting.ShowGifAlert("Huỷ quá trình xuất Excel", "Export", EnumStore.Icon.Warning);
                return;
            }
            try
            {
                // CHẠY TRÊN UI THREAD — OK
                ExportToPath(table, sfd.FileName, textFormat);
                FrmWaiting.ShowGifAlert("Đã xuất Excel thành công!", "Export", EnumStore.Icon.Success);
            }
            catch (Exception ex)
            {
                FrmWaiting.ShowGifAlert($"Lỗi khi xuất Excel: {ex.Message}", "Export Error", EnumStore.Icon.Warning);
            }
        }

        // 🟢 Public để có thể gọi từ thread nền, KHÔNG hiện dialog/MessageBox
        // Mặc định xuất Unicode. Truyền ExcelExportTextFormat.TCVN hoặc TCVN3 để xuất chuỗi mã TCVN3/ABC.
        public static void ExportToPath(
            DataTable table,
            string path,
            ExcelExportTextFormat textFormat = ExcelExportTextFormat.Unicode,
            string tcvnFontName = DefaultTcvnFontName)
        {
            if (table == null)
                throw new ArgumentNullException(nameof(table));

            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Đường dẫn xuất Excel không hợp lệ.", nameof(path));

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Report");

            ws.Cell(1, 1).InsertTable(table, "Data", true);
            ws.Cell(1, table.Columns.Count + 2).Value = "Ngày xuất:";
            ws.Cell(1, table.Columns.Count + 3).Value = DateTime.Now.ToString("dd/MM/yyyy HH:mm");

            ApplyTextFormat(ws, textFormat, tcvnFontName);

            ws.Columns().AdjustToContents();
            wb.SaveAs(path);
        }

        private static void ApplyTextFormat(IXLWorksheet worksheet, ExcelExportTextFormat textFormat, string tcvnFontName)
        {
            if (textFormat == ExcelExportTextFormat.Unicode)
                return;

            if (textFormat != ExcelExportTextFormat.TCVN)
                throw new ArgumentOutOfRangeException(nameof(textFormat), textFormat, "Định dạng xuất Excel không được hỗ trợ.");

            var usedRange = worksheet.RangeUsed();
            if (usedRange == null)
                return;

            foreach (var cell in usedRange.CellsUsed())
            {
                if (cell.HasFormula || cell.DataType != XLDataType.Text)
                    continue;

                cell.SetValue(ConvertUnicodeToTcvn3(cell.GetValue<string>()));
            }

            usedRange.Style.Font.FontName = string.IsNullOrWhiteSpace(tcvnFontName)
                ? DefaultTcvnFontName
                : tcvnFontName;
        }

        private static string ConvertUnicodeToTcvn3(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            var builder = new StringBuilder(input.Length * 2);

            foreach (char character in input)
            {
                if (UnicodeToTcvn3Map.TryGetValue(character, out string convertedCharacter))
                {
                    builder.Append(convertedCharacter);
                }
                else
                {
                    builder.Append(character);
                }
            }

            return builder.ToString();
        }
    }
}
