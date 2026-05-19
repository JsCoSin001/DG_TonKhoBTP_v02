using ClosedXML.Excel;
using System;
using System.Data;
using System.Linq;

namespace DG_TonKhoBTP_v02.Printer.Kho
{
    /// <summary>
    /// Xuất báo cáo tồn kho ra Excel, gồm 2 sheet:
    /// - Lô
    /// - Cuộn
    ///
    /// Mỗi sheet được tạo thành Excel Table thật để người dùng lọc/sort trực tiếp trong Excel.
    /// </summary>
    public static class TonKhoExcelExporter
    {
        private const string HeaderBlue = "#2F65F8";
        private const string White = "#FFFFFF";

        public static void ExportToPath(DataTable tonKhoLo, DataTable tonKhoCuon, string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Đường dẫn file Excel không hợp lệ.", nameof(path));

            using var workbook = new XLWorkbook();

            AddInventorySheet(
                workbook,
                sheetName: "Lô",
                tableName: "tblTonKhoLo",
                sourceTable: tonKhoLo,
                reportTitle: "BÁO CÁO TỒN KHO ĐG - LÔ");

            AddInventorySheet(
                workbook,
                sheetName: "Cuộn",
                tableName: "tblTonKhoCuon",
                sourceTable: tonKhoCuon,
                reportTitle: "BÁO CÁO TỒN KHO ĐG - CUỘN");

            workbook.SaveAs(path);
        }

        private static void AddInventorySheet(
            XLWorkbook workbook,
            string sheetName,
            string tableName,
            DataTable sourceTable,
            string reportTitle)
        {
            DataTable table = PrepareTableForExport(sourceTable);
            var worksheet = workbook.Worksheets.Add(sheetName);

            int columnCount = Math.Max(table.Columns.Count, 1);
            const int titleRow = 1;
            const int dateRow = 2;
            const int tableStartRow = 4;
            const int tableStartColumn = 1;

            // Tiêu đề lớn.
            var titleRange = worksheet.Range(titleRow, 1, titleRow, columnCount);
            titleRange.Merge();
            worksheet.Cell(titleRow, 1).Value = reportTitle;
            titleRange.Style.Font.Bold = true;
            titleRange.Style.Font.FontSize = 16;
            titleRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            titleRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            // Ngày xuất.
            var dateRange = worksheet.Range(dateRow, 1, dateRow, columnCount);
            dateRange.Merge();
            worksheet.Cell(dateRow, 1).Value = "Ngày xuất: " + DateTime.Now.ToString("dd/MM/yyyy HH:mm");
            dateRange.Style.Font.Italic = true;
            dateRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            // InsertTable tạo Excel Table thật, có AutoFilter.
            var insertedTable = worksheet.Cell(tableStartRow, tableStartColumn)
                .InsertTable(table, tableName, true);

            insertedTable.Theme = XLTableTheme.TableStyleMedium2;
            insertedTable.ShowAutoFilter = true;

            //var tableRange = insertedTable.Range();
            var tableRange = insertedTable.AsRange();
            tableRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            tableRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            tableRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            var headerRow = insertedTable.HeadersRow();
            headerRow.Style.Fill.BackgroundColor = XLColor.FromHtml(HeaderBlue);
            headerRow.Style.Font.FontColor = XLColor.FromHtml(White);
            headerRow.Style.Font.Bold = true;
            headerRow.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            headerRow.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            // Tô border cho toàn bộ dữ liệu trong table.
            insertedTable.DataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            insertedTable.DataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            // Căn lề theo kiểu dữ liệu.
            ApplyColumnAlignment(insertedTable);

            // Freeze đến dòng header để khi scroll vẫn nhìn thấy tiêu đề + header table.
            worksheet.SheetView.FreezeRows(tableStartRow);

            worksheet.Columns().AdjustToContents();
            LimitColumnWidths(worksheet, maxWidth: 45);

            worksheet.Row(titleRow).Height = 24;
            worksheet.Row(tableStartRow).Height = 22;
        }

        private static DataTable PrepareTableForExport(DataTable sourceTable)
        {
            if (sourceTable == null)
                throw new ArgumentNullException(nameof(sourceTable));

            DataTable table = sourceTable.Copy();

            if (table.Rows.Count > 0)
                return table;

            // ClosedXML tạo Excel Table ổn định hơn khi có ít nhất một data row.
            // Nếu sheet không có dữ liệu, thêm một dòng thông báo để vẫn giữ cấu trúc table/filter.
            DataRow row = table.NewRow();
            if (table.Columns.Count > 0)
                row[0] = "Không có dữ liệu";
            table.Rows.Add(row);

            return table;
        }

        private static void ApplyColumnAlignment(IXLTable table)
        {
            //var tableRange = table.Range();
            var tableRange = table.AsRange();
            var headerRow = table.HeadersRow();
            int fieldCount = table.Fields.Count();

            for (int columnIndex = 1; columnIndex <= fieldCount; columnIndex++)
            {
                var column = tableRange.Column(columnIndex);
                string header = headerRow.Cell(columnIndex).GetString();

                if (IsNumericHeader(header))
                {
                    column.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                    column.Style.NumberFormat.Format = "#,##0";
                }
                else
                {
                    column.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                }

                column.Style.Alignment.WrapText = true;
            }
        }

        private static bool IsNumericHeader(string header)
        {
            string[] keywords =
            {
                "Số", "Chiều", "Tổng"
            };

            return keywords.Any(k => header.StartsWith(k, StringComparison.OrdinalIgnoreCase));
        }

        private static void LimitColumnWidths(IXLWorksheet worksheet, double maxWidth)
        {
            foreach (var column in worksheet.ColumnsUsed())
            {
                if (column.Width > maxWidth)
                    column.Width = maxWidth;
            }
        }
    }
}
