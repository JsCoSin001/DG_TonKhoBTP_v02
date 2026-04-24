using System;
using System.IO;
using System.Linq;
using System.Reflection;
using ClosedXML.Excel;

namespace DG_TonKhoBTP_v02.Helper.Reuseable
{
    public static class ExcelHelper
    {
        // Thư mục chứa file Excel trên server
        private static readonly string FolderPath = @"\\192.168.4.10\DungChungDG\DanhSach_SP_NCC_Kho";

        // Đường dẫn đầy đủ tới file Excel
        private static readonly string FilePath = Path.Combine(FolderPath, "data.xlsx");

        /// <summary>
        /// Hàm chính:
        /// - Kiểm tra input
        /// - Tạo thư mục nếu chưa có
        /// - Tạo file Excel nếu chưa có
        /// - Đảm bảo có đủ 3 sheet: DsSanPham, DsNcc, DsKho
        /// - Nếu sheet đã có ID của model thì update dòng cũ
        /// - Nếu chưa có ID thì thêm dòng mới
        /// </summary>
        public static void InsertModelToSheet<T>(T model, string sheetName)
        {
            ValidateInput(model, sheetName);

            EnsureFolderExists();
            EnsureExcelFileExists();

            using (var workbook = OpenWorkbook())
            {
                EnsureRequiredSheetsExist(workbook);

                IXLWorksheet worksheet = GetWorksheetByName(workbook, sheetName);

                UpsertModelToWorksheet(worksheet, model);

                workbook.Save();
            }
        }

        /// <summary>
        /// Kiểm tra dữ liệu đầu vào
        /// </summary>
        private static void ValidateInput<T>(T model, string sheetName)
        {
            if (model == null)
                throw new ArgumentNullException("model");

            if (string.IsNullOrWhiteSpace(sheetName))
                throw new ArgumentNullException("sheetName");

            string[] validSheets = new string[] { "DsSanPham", "DsNcc", "DsKho" };

            if (!validSheets.Contains(sheetName))
                throw new ArgumentException("sheetName không hợp lệ. Chỉ chấp nhận: DsSanPham, DsNcc, DsKho");
        }

        /// <summary>
        /// Đảm bảo thư mục tồn tại
        /// </summary>
        private static void EnsureFolderExists()
        {
            if (!Directory.Exists(FolderPath))
            {
                Directory.CreateDirectory(FolderPath);
            }
        }

        /// <summary>
        /// Nếu file Excel chưa có thì tạo mới, đồng thời tạo sẵn 3 sheet mặc định
        /// </summary>
        private static void EnsureExcelFileExists()
        {
            if (File.Exists(FilePath))
                return;

            using (var workbook = new XLWorkbook())
            {
                workbook.Worksheets.Add("DsSanPham");
                workbook.Worksheets.Add("DsNcc");
                workbook.Worksheets.Add("DsKho");

                workbook.SaveAs(FilePath);
            }
        }

        /// <summary>
        /// Mở file Excel
        /// </summary>
        private static XLWorkbook OpenWorkbook()
        {
            return new XLWorkbook(FilePath);
        }

        /// <summary>
        /// Đảm bảo workbook có đủ 3 sheet yêu cầu
        /// </summary>
        private static void EnsureRequiredSheetsExist(XLWorkbook workbook)
        {
            EnsureSheetExists(workbook, "DsSanPham");
            EnsureSheetExists(workbook, "DsNcc");
            EnsureSheetExists(workbook, "DsKho");
        }

        /// <summary>
        /// Kiểm tra sheet đã tồn tại hay chưa, nếu chưa có thì tạo mới
        /// </summary>
        private static void EnsureSheetExists(XLWorkbook workbook, string sheetName)
        {
            bool exists = workbook.Worksheets.Any(ws => ws.Name == sheetName);

            if (!exists)
            {
                workbook.Worksheets.Add(sheetName);
            }
        }

        /// <summary>
        /// Lấy sheet theo tên
        /// </summary>
        private static IXLWorksheet GetWorksheetByName(XLWorkbook workbook, string sheetName)
        {
            IXLWorksheet worksheet = workbook.Worksheets.FirstOrDefault(ws => ws.Name == sheetName);

            if (worksheet == null)
                throw new Exception("Không tìm thấy sheet: " + sheetName);

            return worksheet;
        }

        /// <summary>
        /// Upsert model vào sheet:
        /// - Nếu sheet rỗng: tạo header + thêm dữ liệu
        /// - Nếu có sẵn ID: update dòng cũ
        /// - Nếu chưa có ID: thêm dòng mới ở cuối
        /// </summary>
        private static void UpsertModelToWorksheet<T>(IXLWorksheet worksheet, T model)
        {
            Type modelType = typeof(T);
            PropertyInfo[] properties = GetModelProperties(modelType);

            // Nếu sheet chưa có dữ liệu thì tạo header
            if (IsWorksheetEmpty(worksheet))
            {
                WriteHeader(worksheet, modelType);
                WriteModelToRow(worksheet, model, 2);
                return;
            }

            // Tìm property ID trong model
            PropertyInfo idProperty = properties.FirstOrDefault(p => p.Name.Equals("ID", StringComparison.OrdinalIgnoreCase));

            // Nếu model không có ID thì append cuối
            if (idProperty == null)
            {
                int nextRowNoId = GetNextRowNumber(worksheet);
                WriteModelToRow(worksheet, model, nextRowNoId);
                return;
            }

            object modelIdValue = idProperty.GetValue(model, null);

            // Nếu ID null/rỗng thì append cuối
            if (modelIdValue == null || string.IsNullOrWhiteSpace(modelIdValue.ToString()))
            {
                int nextRowNoValue = GetNextRowNumber(worksheet);
                WriteModelToRow(worksheet, model, nextRowNoValue);
                return;
            }

            // Tìm vị trí cột ID dựa vào header dòng 1
            int idColumnIndex = FindColumnIndexByHeaderName(worksheet, "ID");

            // Nếu file Excel chưa có cột ID thì fallback theo thứ tự property
            if (idColumnIndex == -1)
            {
                idColumnIndex = FindPropertyIndex(properties, "ID") + 1;
            }

            // Nếu vẫn không tìm thấy cột ID thì append cuối
            if (idColumnIndex <= 0)
            {
                int nextRowNoColumn = GetNextRowNumber(worksheet);
                WriteModelToRow(worksheet, model, nextRowNoColumn);
                return;
            }

            // Tìm dòng có ID trùng
            int existingRow = FindRowById(worksheet, idColumnIndex, modelIdValue.ToString());

            if (existingRow > 0)
            {
                // Có ID rồi -> update dòng cũ
                WriteModelToRow(worksheet, model, existingRow);
            }
            else
            {
                // Chưa có -> thêm cuối
                int nextRow = GetNextRowNumber(worksheet);
                WriteModelToRow(worksheet, model, nextRow);
            }
        }

        /// <summary>
        /// Kiểm tra sheet có rỗng không
        /// </summary>
        private static bool IsWorksheetEmpty(IXLWorksheet worksheet)
        {
            return worksheet.LastRowUsed() == null;
        }

        /// <summary>
        /// Lấy số dòng tiếp theo để ghi dữ liệu
        /// </summary>
        private static int GetNextRowNumber(IXLWorksheet worksheet)
        {
            IXLRow lastRow = worksheet.LastRowUsed();

            if (lastRow == null)
                return 1;

            return lastRow.RowNumber() + 1;
        }

        /// <summary>
        /// Ghi header vào dòng 1 theo tên property của model
        /// </summary>
        private static void WriteHeader(IXLWorksheet worksheet, Type modelType)
        {
            PropertyInfo[] properties = GetModelProperties(modelType);

            for (int i = 0; i < properties.Length; i++)
            {
                worksheet.Cell(1, i + 1).SetValue(properties[i].Name);
            }
        }

        /// <summary>
        /// Ghi dữ liệu model vào một dòng cụ thể
        /// </summary>
        private static void WriteModelToRow<T>(IXLWorksheet worksheet, T model, int rowNumber)
        {
            PropertyInfo[] properties = GetModelProperties(typeof(T));

            for (int i = 0; i < properties.Length; i++)
            {
                object value = properties[i].GetValue(model, null);
                SetCellValue(worksheet.Cell(rowNumber, i + 1), value);
            }
        }

        /// <summary>
        /// Lấy danh sách property public của model
        /// </summary>
        private static PropertyInfo[] GetModelProperties(Type modelType)
        {
            return modelType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        }

        /// <summary>
        /// Tìm chỉ số cột theo tên header ở dòng 1
        /// Trả về index bắt đầu từ 1. Không thấy thì trả về -1.
        /// </summary>
        private static int FindColumnIndexByHeaderName(IXLWorksheet worksheet, string headerName)
        {
            IXLRow headerRow = worksheet.Row(1);
            IXLCell lastCell = headerRow.LastCellUsed();

            if (lastCell == null)
                return -1;

            int lastColumn = lastCell.Address.ColumnNumber;

            for (int col = 1; col <= lastColumn; col++)
            {
                string cellText = worksheet.Cell(1, col).GetValue<string>();

                if (string.Equals(cellText, headerName, StringComparison.OrdinalIgnoreCase))
                    return col;
            }

            return -1;
        }

        /// <summary>
        /// Tìm index property theo tên, index bắt đầu từ 0
        /// Không thấy thì trả về -1
        /// </summary>
        private static int FindPropertyIndex(PropertyInfo[] properties, string propertyName)
        {
            for (int i = 0; i < properties.Length; i++)
            {
                if (properties[i].Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase))
                    return i;
            }

            return -1;
        }

        /// <summary>
        /// Tìm dòng có ID trùng trong sheet
        /// Bắt đầu tìm từ dòng 2 vì dòng 1 là header
        /// Không thấy thì trả về -1
        /// </summary>
        private static int FindRowById(IXLWorksheet worksheet, int idColumnIndex, string idValue)
        {
            IXLRow lastRow = worksheet.LastRowUsed();

            if (lastRow == null)
                return -1;

            int lastRowNumber = lastRow.RowNumber();

            for (int row = 2; row <= lastRowNumber; row++)
            {
                string cellValue = worksheet.Cell(row, idColumnIndex).GetValue<string>();

                if (string.Equals(cellValue, idValue, StringComparison.OrdinalIgnoreCase))
                    return row;
            }

            return -1;
        }

        /// <summary>
        /// Gán giá trị cho cell theo đúng kiểu dữ liệu
        /// Dùng SetValue để tránh lỗi object -> XLCellValue
        /// </summary>
        private static void SetCellValue(IXLCell cell, object value)
        {
            if (value == null)
            {
                cell.SetValue(string.Empty);
                return;
            }

            Type valueType = value.GetType();

            if (valueType == typeof(string))
            {
                cell.SetValue((string)value);
            }
            else if (valueType == typeof(int))
            {
                cell.SetValue((int)value);
            }
            else if (valueType == typeof(long))
            {
                cell.SetValue((long)value);
            }
            else if (valueType == typeof(short))
            {
                cell.SetValue((short)value);
            }
            else if (valueType == typeof(decimal))
            {
                cell.SetValue((decimal)value);
            }
            else if (valueType == typeof(double))
            {
                cell.SetValue((double)value);
            }
            else if (valueType == typeof(float))
            {
                cell.SetValue((float)value);
            }
            else if (valueType == typeof(bool))
            {
                cell.SetValue((bool)value);
            }
            else if (valueType == typeof(DateTime))
            {
                cell.SetValue((DateTime)value);
            }
            else
            {
                cell.SetValue(value.ToString());
            }
        }
    }
}