using System;
using System.Data;
using System.IO;
using System.Windows.Forms;
using ClosedXML.Excel;

namespace DG_TonKhoBTP_v02.Helper
{
    public static class ExcelExporter
    {
        /// <summary>
        /// Xuất DataTable ra file Excel (.xlsx).
        /// </summary>
        /// <param name="table">Nguồn dữ liệu DataTable</param>
        /// <param name="defaultFileName">Tên gợi ý ban đầu cho file</param>
        public static void Export(DataTable table, string defaultFileName = "Report")
        {
            if (table == null || table.Rows.Count == 0)
            {
                MessageBox.Show("Không có dữ liệu để xuất.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using var sfd = new SaveFileDialog
            {
                Title = "Xuất báo cáo Excel",
                Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                FileName = $"{defaultFileName}_{DateTime.Now:yyyyMMdd_HHmm}"
            };

            if (sfd.ShowDialog() != DialogResult.OK) return;

            try
            {
                ExportToExcel(table, sfd.FileName);
                MessageBox.Show("Đã xuất Excel thành công!", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi xuất Excel: {ex.Message}", "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Ghi DataTable ra file Excel (.xlsx) bằng ClosedXML.
        /// </summary>
        private static void ExportToExcel(DataTable table, string path)
        {
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Report");

            // Ghi bảng dữ liệu vào Excel
            ws.Cell(1, 1).InsertTable(table, "Data", true);

            // Tự động căn chỉnh độ rộng cột
            ws.Columns().AdjustToContents();

            // Ghi ngày xuất và người tạo ở góc trên (tùy chọn)
            ws.Cell(1, table.Columns.Count + 2).Value = "Ngày xuất:";
            ws.Cell(1, table.Columns.Count + 3).Value = DateTime.Now.ToString("dd/MM/yyyy HH:mm");

            // Lưu file
            wb.SaveAs(path);
        }
    }
}
