using DG_TonKhoBTP_v02.Database.ChatLuong;
using DG_TonKhoBTP_v02.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml.Linq;



namespace DG_TonKhoBTP_v02.UI.NghiepVuKhac.ChatLuong
{
    public partial class UC_NhapKhoTP
    {
        private const int ImportExcelColumnCount = 17;

        private static readonly string[] ImportFallbackHeaders =
        {
            "Ngày nhập", "Số biên bản", "Tên sản phẩm", "Mã bin", "Số mét",
            "Số cuộn", "Tổng chiều dài", "Số đầu", "Số cuối", "Loại đơn",
            "Khách hàng", "Ghi chú", "Loại hàng", "Chiều cao lô",
            "Người làm", "Tên dự án", "Kiểu"
        };

        private void InitImportExcelButton()
        {
            if (btnImportExcel == null)
                btnImportExcel = FindControlRecursive(this, "btnImportExcel") as Button;

            if (btnImportExcel == null)
            {
                btnImportExcel = new Button
                {
                    Name = "btnImportExcel",
                    Text = "Import Excel",
                    Size = new System.Drawing.Size(117, 50),
                    UseVisualStyleBackColor = true
                };

                if (flowLayoutPanel2 != null)
                    flowLayoutPanel2.Controls.Add(btnImportExcel);
            }

            btnImportExcel.Click -= btnImportExcel_Click;
            btnImportExcel.Click += btnImportExcel_Click;
        }

        private void SetImportExcelButtonEnabled(bool enabled)
        {
            if (btnImportExcel != null)
                btnImportExcel.Enabled = enabled;
        }

        private static Control FindControlRecursive(Control parent, string name)
        {
            if (parent == null) return null;

            foreach (Control child in parent.Controls)
            {
                if (string.Equals(child.Name, name, StringComparison.OrdinalIgnoreCase))
                    return child;

                Control found = FindControlRecursive(child, name);
                if (found != null)
                    return found;
            }

            return null;
        }

        private void btnImportExcel_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Title = "Chọn file Excel nhập kho";
                ofd.Filter = "Excel Workbook (*.xlsx;*.xlsm)|*.xlsx;*.xlsm";
                ofd.Multiselect = false;

                if (ofd.ShowDialog(this.FindForm()) != DialogResult.OK)
                    return;

                try
                {
                    ImportNhapKhoResult result = ImportNhapKhoFromExcel(ofd.FileName);

                    if (result.InsertedIds.Count > 0)
                    {
                        DataTable dt = NhapKho_DB.LayNhapKhoTheoIds(result.InsertedIds);
                        LoadNhapKhoVaoGrid(dt);
                    }

                    string errorFile = string.Empty;
                    if (result.Errors.Count > 0)
                    {
                        errorFile = SaveImportErrorWorkbook(ofd.FileName, result.Headers, result.Errors);
                    }

                    string message = $"Import hoàn tất.\n" +
                                     $"- Phiếu nhập đã lưu: {result.InsertedIds.Count}\n" +
                                     $"- Dòng lỗi: {result.Errors.Count}";

                    if (!string.IsNullOrWhiteSpace(errorFile))
                        message += $"\n\nFile lỗi đã lưu tại:\n{errorFile}";

                    MessageBox.Show(message, "Import Excel", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi khi import Excel:\n{ex.Message}",
                        "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private ImportNhapKhoResult ImportNhapKhoFromExcel(string excelPath)
        {
            List<List<string>> rows = SimpleXlsx.ReadFirstWorksheet(excelPath);
            if (rows == null || rows.Count == 0)
                throw new InvalidOperationException("File Excel không có dữ liệu.");

            ImportNhapKhoResult result = new ImportNhapKhoResult
            {
                Headers = GetImportHeaders(rows[0])
            };

            Dictionary<string, DataTable> maBinCache = new Dictionary<string, DataTable>(StringComparer.OrdinalIgnoreCase);
            List<ImportExcelRow> validRows = new List<ImportExcelRow>();

            for (int rowIndex = 1; rowIndex < rows.Count; rowIndex++)
            {
                string[] raw = NormalizeExcelRow(rows[rowIndex]);

                if (raw.All(string.IsNullOrWhiteSpace))
                    continue;

                string error;
                ImportExcelRow parsedRow;
                if (!TryParseImportRow(rowIndex + 1, raw, maBinCache, out parsedRow, out error))
                {
                    result.Errors.Add(new ImportErrorRow
                    {
                        Error = $"Dòng {rowIndex + 1}: {error}",
                        Values = raw
                    });
                    continue;
                }

                validRows.Add(parsedRow);
            }

            if (validRows.Count == 0)
                return result;

            var groups = validRows
                .GroupBy(x => x.GroupKey)
                .ToList();

            foreach (var group in groups)
            {
                List<ImportExcelRow> groupRows = group.ToList();
                ImportExcelRow first = groupRows[0];

                try
                {
                    NhapKho_Model model = CloneImportModel(first.Model);
                    List<ThongTinCuonDay> dsCuon = groupRows.Select(x => x.CuonDay).ToList();

                    if (dsCuon.Count == 0)
                        throw new InvalidOperationException("Không có dòng chi tiết TTCuonDay hợp lệ.");

                    long newId = NhapKho_DB.NhapKho(model, dsCuon);
                    result.InsertedIds.Add(newId);
                }
                catch (Exception ex)
                {
                    foreach (ImportExcelRow r in groupRows)
                    {
                        result.Errors.Add(new ImportErrorRow
                        {
                            Error = $"Dòng {r.ExcelRowNumber}: {ex.Message}",
                            Values = r.RawValues
                        });
                    }
                }
            }

            return result;
        }

        private static NhapKho_Model CloneImportModel(NhapKho_Model source)
        {
            return new NhapKho_Model
            {
                Ngay = source.Ngay,
                SoBB = source.SoBB,
                TTThanhPham_ID = source.TTThanhPham_ID,
                TenSP = source.TenSP,
                SoMet = source.SoMet,
                LoaiDon = source.LoaiDon,
                KhachHang = source.KhachHang,
                GhiChu = source.GhiChu,
                Loai = source.Loai,
                ChieuCaoLo = source.ChieuCaoLo,
                NguoiLam = source.NguoiLam,
                TenDuAn = source.TenDuAn,
                Kieu = source.Kieu
            };
        }

        private bool TryParseImportRow(
            int excelRowNumber,
            string[] raw,
            Dictionary<string, DataTable> maBinCache,
            out ImportExcelRow parsedRow,
            out string error)
        {
            parsedRow = null;
            error = string.Empty;
            List<string> errors = new List<string>();

            DateTime ngay;
            if (!TryParseExcelDate(raw[0], out ngay))
                errors.Add("Ngày nhập không hợp lệ");

            int soBB;
            if (!TryParseRoundedInt(raw[1], out soBB) || soBB <= 0)
                errors.Add("Số biên bản không hợp lệ");

            string tenSP = Clean(raw[2]);
            if (string.IsNullOrWhiteSpace(tenSP))
                errors.Add("Tên sản phẩm không được trống");

            string maBin = Clean(raw[3]);
            long ttThanhPhamId = 0;
            if (string.IsNullOrWhiteSpace(maBin))
            {
                errors.Add("Mã bin không được trống");
            }
            else
            {
                DataTable dtMaBin;
                if (!maBinCache.TryGetValue(maBin, out dtMaBin))
                {
                    dtMaBin = NhapKho_DB.LayTTThanhPhamTheoMaBin(maBin);
                    maBinCache[maBin] = dtMaBin;
                }

                if (dtMaBin.Rows.Count == 0)
                {
                    errors.Add($"Không tìm thấy mã bin '{maBin}' trong TTThanhPham");
                }
                else if (dtMaBin.Rows.Count > 1)
                {
                    errors.Add($"Mã bin '{maBin}' có nhiều hơn 1 dòng trong TTThanhPham");
                }
                else
                {
                    object idValue = dtMaBin.Rows[0]["TTThanhPham_ID"];
                    if (idValue == DBNull.Value || !long.TryParse(idValue.ToString(), out ttThanhPhamId) || ttThanhPhamId <= 0)
                        errors.Add($"TTThanhPham_ID của mã bin '{maBin}' không hợp lệ");
                }
            }

            double soMet;
            if (!TryParseDouble(raw[4], out soMet) || soMet <= 0)
                errors.Add("Số mét không hợp lệ");

            int soCuon;
            if (!TryParseRoundedInt(raw[5], out soCuon) || soCuon <= 0)
                errors.Add("Số cuộn không hợp lệ");

            int tongChieuDai;
            if (!TryParseRoundedInt(raw[6], out tongChieuDai) || tongChieuDai <= 0)
                errors.Add("Tổng chiều dài không hợp lệ");

            int soDau;
            if (!TryParseRoundedInt(raw[7], out soDau))
                errors.Add("Số đầu không hợp lệ");

            int soCuoi;
            if (!TryParseRoundedInt(raw[8], out soCuoi))
                errors.Add("Số cuối không hợp lệ");

            string loaiDon = Clean(raw[9]);
            if (string.IsNullOrWhiteSpace(loaiDon))
                errors.Add("Loại đơn không được trống");

            string khachHang = Clean(raw[10]);
            if (IsVietnameseToken(loaiDon, "Hang dat") && string.IsNullOrWhiteSpace(khachHang))
                errors.Add("Khách hàng không được trống khi Loại đơn là Hàng đặt");

            string ghiChu = Clean(raw[11]);

            string loai;
            if (IsVietnameseToken(raw[12], "Lo"))
                loai = "Lô";
            else if (IsVietnameseToken(raw[12], "Cuon"))
                loai = "Cuộn";
            else
            {
                loai = string.Empty;
                errors.Add("Loại hàng chỉ được là Lô hoặc Cuộn");
            }

            double chieuCaoLo = 0;
            if (loai == "Lô")
            {
                if (!TryParseDouble(raw[13], out chieuCaoLo) || chieuCaoLo <= 0)
                    errors.Add("Chiều cao lô không hợp lệ khi Loại hàng là Lô");
            }

            string nguoiLam = Clean(raw[14]);
            if (string.IsNullOrWhiteSpace(nguoiLam))
                errors.Add("Người làm không được trống");

            string tenDuAn = Clean(raw[15]);

            int kieu;
            if (IsVietnameseToken(raw[16], "Lo"))
                kieu = 1;
            else if (IsVietnameseToken(raw[16], "Le"))
                kieu = 0;
            else
            {
                kieu = 1;
                errors.Add("Kiểu chỉ được là Lô hoặc Lẻ");
            }

            if (errors.Count > 0)
            {
                error = string.Join("; ", errors);
                return false;
            }

            NhapKho_Model model = new NhapKho_Model
            {
                Ngay = ngay.ToString("yyyy-MM-dd"),
                SoBB = soBB,
                TTThanhPham_ID = ttThanhPhamId,
                TenSP = tenSP,
                SoMet = soMet,
                LoaiDon = loaiDon,
                KhachHang = khachHang,
                GhiChu = ghiChu,
                Loai = loai,
                ChieuCaoLo = loai == "Lô" ? chieuCaoLo : 0,
                NguoiLam = nguoiLam,
                TenDuAn = tenDuAn,
                Kieu = kieu
            };

            ThongTinCuonDay cuonDay = new ThongTinCuonDay
            {
                SoCuon = soCuon,
                TongChieuDai = tongChieuDai,
                SoDau = soDau,
                soCuoi = soCuoi,
                Ghichu = null
            };

            parsedRow = new ImportExcelRow
            {
                ExcelRowNumber = excelRowNumber,
                RawValues = raw,
                Model = model,
                CuonDay = cuonDay,
                GroupKey = BuildImportGroupKey(model)
            };

            return true;
        }

        private static string BuildImportGroupKey(NhapKho_Model model)
        {
            return string.Join("\u001F", new[]
            {
                model.Ngay ?? string.Empty,
                model.SoBB.ToString(CultureInfo.InvariantCulture),
                model.TTThanhPham_ID.ToString(CultureInfo.InvariantCulture),
                model.TenSP ?? string.Empty,
                model.SoMet.ToString("R", CultureInfo.InvariantCulture),
                model.LoaiDon ?? string.Empty,
                model.KhachHang ?? string.Empty,
                model.GhiChu ?? string.Empty,
                model.Loai ?? string.Empty,
                model.Loai == "Lô" ? model.ChieuCaoLo.ToString("R", CultureInfo.InvariantCulture) : string.Empty,
                model.NguoiLam ?? string.Empty,
                model.TenDuAn ?? string.Empty,
                model.Kieu.ToString(CultureInfo.InvariantCulture)
            });
        }

        private static string SaveImportErrorWorkbook(string sourceExcelPath, string[] headers, List<ImportErrorRow> errors)
        {
            if (errors == null || errors.Count == 0)
                return string.Empty;

            string folder = Path.GetDirectoryName(sourceExcelPath);
            if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
                folder = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

            string fileName = $"LoiImportNhapKho_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            string outputPath = Path.Combine(folder, fileName);

            List<string[]> rows = new List<string[]>();
            string[] headerRow = new string[ImportExcelColumnCount + 1];
            headerRow[0] = "Lỗi";
            for (int i = 0; i < ImportExcelColumnCount; i++)
                headerRow[i + 1] = headers != null && i < headers.Length && !string.IsNullOrWhiteSpace(headers[i])
                    ? headers[i]
                    : ImportFallbackHeaders[i];
            rows.Add(headerRow);

            foreach (ImportErrorRow error in errors)
            {
                string[] row = new string[ImportExcelColumnCount + 1];
                row[0] = error.Error ?? string.Empty;

                for (int i = 0; i < ImportExcelColumnCount; i++)
                    row[i + 1] = error.Values != null && i < error.Values.Length
                        ? error.Values[i] ?? string.Empty
                        : string.Empty;

                rows.Add(row);
            }

            SimpleXlsx.WriteWorksheet(outputPath, "LoiImport", rows);
            return outputPath;
        }

        private static string[] GetImportHeaders(List<string> headerRow)
        {
            string[] headers = new string[ImportExcelColumnCount];
            for (int i = 0; i < ImportExcelColumnCount; i++)
            {
                string value = headerRow != null && i < headerRow.Count ? headerRow[i] : string.Empty;
                headers[i] = string.IsNullOrWhiteSpace(value) ? ImportFallbackHeaders[i] : value.Trim();
            }
            return headers;
        }

        private static string[] NormalizeExcelRow(List<string> row)
        {
            string[] values = new string[ImportExcelColumnCount];
            for (int i = 0; i < ImportExcelColumnCount; i++)
                values[i] = row != null && i < row.Count ? Clean(row[i]) : string.Empty;
            return values;
        }

        private static string Clean(string value)
        {
            return value == null ? string.Empty : value.Trim();
        }

        private static bool TryParseExcelDate(string text, out DateTime value)
        {
            text = Clean(text);

            string[] formats =
            {
                "yyyy-MM-dd", "dd/MM/yyyy", "d/M/yyyy", "dd-MM-yyyy", "d-M-yyyy",
                "M/d/yyyy", "MM/dd/yyyy", "yyyy/MM/dd"
            };

            if (DateTime.TryParseExact(text, formats, CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out value))
                return true;

            if (DateTime.TryParse(text, CultureInfo.CurrentCulture, DateTimeStyles.None, out value))
                return true;

            if (TryParseDouble(text, out double serial))
            {
                try
                {
                    value = DateTime.FromOADate(serial);
                    return true;
                }
                catch
                {
                    // ignored
                }
            }

            return false;
        }

        private static bool TryParseRoundedInt(string text, out int value)
        {
            value = 0;
            if (!TryParseDouble(text, out double d))
                return false;

            try
            {
                value = Convert.ToInt32(Math.Round(d, MidpointRounding.AwayFromZero));
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool TryParseDouble(string text, out double value)
        {
            text = Clean(text);

            if (text.Contains(",") && !text.Contains("."))
            {
                string decimalCommaText = text.Replace(',', '.');
                if (double.TryParse(decimalCommaText, NumberStyles.Any, CultureInfo.InvariantCulture, out value))
                    return true;
            }

            return double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out value)
                   || double.TryParse(text, NumberStyles.Any, CultureInfo.CurrentCulture, out value);
        }

        private static bool IsVietnameseToken(string input, string expectedNoAccent)
        {
            string left = RemoveVietnameseSigns(input).Replace(" ", string.Empty).ToLowerInvariant();
            string right = RemoveVietnameseSigns(expectedNoAccent).Replace(" ", string.Empty).ToLowerInvariant();
            return left == right;
        }

        private static string RemoveVietnameseSigns(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;

            string normalized = text.Trim().Normalize(NormalizationForm.FormD);
            StringBuilder sb = new StringBuilder();

            foreach (char c in normalized)
            {
                UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(c);
                if (category != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }

            return sb.ToString().Normalize(NormalizationForm.FormC)
                .Replace('đ', 'd')
                .Replace('Đ', 'D');
        }

        private class ImportNhapKhoResult
        {
            public string[] Headers { get; set; }
            public List<long> InsertedIds { get; } = new List<long>();
            public List<ImportErrorRow> Errors { get; } = new List<ImportErrorRow>();
        }

        private class ImportExcelRow
        {
            public int ExcelRowNumber { get; set; }
            public string[] RawValues { get; set; }
            public NhapKho_Model Model { get; set; }
            public ThongTinCuonDay CuonDay { get; set; }
            public string GroupKey { get; set; }
        }

        private class ImportErrorRow
        {
            public string Error { get; set; }
            public string[] Values { get; set; }
        }

        private static class SimpleXlsx
        {
            private static readonly XNamespace SpreadsheetNs = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
            private static readonly XNamespace RelationshipNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
            private static readonly XNamespace PackageRelationshipNs = "http://schemas.openxmlformats.org/package/2006/relationships";

            public static List<List<string>> ReadFirstWorksheet(string path)
            {
                using (ZipArchive archive = ZipFile.OpenRead(path))
                {
                    List<string> sharedStrings = ReadSharedStrings(archive);
                    HashSet<int> dateStyleIndexes = ReadDateStyleIndexes(archive);
                    string worksheetPath = GetFirstWorksheetPath(archive);

                    ZipArchiveEntry sheetEntry = archive.GetEntry(worksheetPath);
                    if (sheetEntry == null)
                        throw new InvalidOperationException("Không đọc được sheet đầu tiên trong file Excel.");

                    using (Stream stream = sheetEntry.Open())
                    {
                        XDocument sheetDoc = XDocument.Load(stream);
                        List<List<string>> rows = new List<List<string>>();

                        foreach (XElement rowElement in sheetDoc.Descendants(SpreadsheetNs + "row"))
                        {
                            List<string> rowValues = new List<string>();

                            foreach (XElement cell in rowElement.Elements(SpreadsheetNs + "c"))
                            {
                                string cellReference = (string)cell.Attribute("r");
                                int columnIndex = GetColumnIndexFromCellReference(cellReference);
                                while (rowValues.Count <= columnIndex)
                                    rowValues.Add(string.Empty);

                                rowValues[columnIndex] = ReadCellValue(cell, sharedStrings, dateStyleIndexes);
                            }

                            rows.Add(rowValues);
                        }

                        return rows;
                    }
                }
            }

            public static void WriteWorksheet(string path, string sheetName, List<string[]> rows)
            {
                if (File.Exists(path))
                    File.Delete(path);

                using (ZipArchive archive = ZipFile.Open(path, ZipArchiveMode.Create))
                {
                    AddTextEntry(archive, "[Content_Types].xml", @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<Types xmlns=""http://schemas.openxmlformats.org/package/2006/content-types""><Default Extension=""rels"" ContentType=""application/vnd.openxmlformats-package.relationships+xml""/><Default Extension=""xml"" ContentType=""application/xml""/><Override PartName=""/xl/workbook.xml"" ContentType=""application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml""/><Override PartName=""/xl/worksheets/sheet1.xml"" ContentType=""application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml""/></Types>");

                    AddTextEntry(archive, "_rels/.rels", @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<Relationships xmlns=""http://schemas.openxmlformats.org/package/2006/relationships""><Relationship Id=""rId1"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument"" Target=""xl/workbook.xml""/></Relationships>");

                    XDocument workbookDoc = new XDocument(
                        new XDeclaration("1.0", "UTF-8", "yes"),
                        new XElement(SpreadsheetNs + "workbook",
                            new XAttribute(XNamespace.Xmlns + "r", RelationshipNs.NamespaceName),
                            new XElement(SpreadsheetNs + "sheets",
                                new XElement(SpreadsheetNs + "sheet",
                                    new XAttribute("name", string.IsNullOrWhiteSpace(sheetName) ? "Sheet1" : sheetName),
                                    new XAttribute("sheetId", "1"),
                                    new XAttribute(RelationshipNs + "id", "rId1")))));
                    AddXmlEntry(archive, "xl/workbook.xml", workbookDoc);

                    AddTextEntry(archive, "xl/_rels/workbook.xml.rels", @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<Relationships xmlns=""http://schemas.openxmlformats.org/package/2006/relationships""><Relationship Id=""rId1"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet"" Target=""worksheets/sheet1.xml""/></Relationships>");

                    XDocument worksheetDoc = BuildWorksheetDocument(rows);
                    AddXmlEntry(archive, "xl/worksheets/sheet1.xml", worksheetDoc);
                }
            }

            private static List<string> ReadSharedStrings(ZipArchive archive)
            {
                List<string> result = new List<string>();
                ZipArchiveEntry entry = archive.GetEntry("xl/sharedStrings.xml");
                if (entry == null)
                    return result;

                using (Stream stream = entry.Open())
                {
                    XDocument doc = XDocument.Load(stream);
                    foreach (XElement si in doc.Descendants(SpreadsheetNs + "si"))
                    {
                        string text = string.Concat(si.Descendants(SpreadsheetNs + "t").Select(x => x.Value));
                        result.Add(text);
                    }
                }

                return result;
            }

            private static HashSet<int> ReadDateStyleIndexes(ZipArchive archive)
            {
                HashSet<int> result = new HashSet<int>();
                ZipArchiveEntry entry = archive.GetEntry("xl/styles.xml");
                if (entry == null)
                    return result;

                XDocument doc;
                using (Stream stream = entry.Open())
                    doc = XDocument.Load(stream);

                HashSet<int> dateNumFmtIds = new HashSet<int>
                {
                    14, 15, 16, 17, 18, 19, 20, 21, 22,
                    27, 28, 29, 30, 31, 32, 33, 34, 35, 36,
                    45, 46, 47, 50, 51, 52, 53, 54, 55, 56, 57, 58
                };

                foreach (XElement numFmt in doc.Descendants(SpreadsheetNs + "numFmt"))
                {
                    int id;
                    string formatCode = ((string)numFmt.Attribute("formatCode") ?? string.Empty).ToLowerInvariant();
                    if (int.TryParse((string)numFmt.Attribute("numFmtId"), out id) && LooksLikeDateFormat(formatCode))
                        dateNumFmtIds.Add(id);
                }

                XElement cellXfs = doc.Descendants(SpreadsheetNs + "cellXfs").FirstOrDefault();
                if (cellXfs == null)
                    return result;

                int styleIndex = 0;
                foreach (XElement xf in cellXfs.Elements(SpreadsheetNs + "xf"))
                {
                    int numFmtId;
                    if (int.TryParse((string)xf.Attribute("numFmtId"), out numFmtId) && dateNumFmtIds.Contains(numFmtId))
                        result.Add(styleIndex);

                    styleIndex++;
                }

                return result;
            }

            private static bool LooksLikeDateFormat(string formatCode)
            {
                if (string.IsNullOrWhiteSpace(formatCode)) return false;

                string cleaned = Regex.Replace(formatCode, "\\\\.|\"[^\"]*\"|\\[[^\\]]*\\]", string.Empty);
                return cleaned.IndexOf('y') >= 0 || cleaned.IndexOf('d') >= 0;
            }

            private static string GetFirstWorksheetPath(ZipArchive archive)
            {
                ZipArchiveEntry workbookEntry = archive.GetEntry("xl/workbook.xml");
                ZipArchiveEntry relsEntry = archive.GetEntry("xl/_rels/workbook.xml.rels");

                if (workbookEntry == null || relsEntry == null)
                    throw new InvalidOperationException("File Excel không đúng định dạng .xlsx/.xlsm.");

                string relationId;
                using (Stream stream = workbookEntry.Open())
                {
                    XDocument workbookDoc = XDocument.Load(stream);
                    XElement firstSheet = workbookDoc.Descendants(SpreadsheetNs + "sheet").FirstOrDefault();
                    if (firstSheet == null)
                        throw new InvalidOperationException("Workbook không có sheet.");

                    relationId = (string)firstSheet.Attribute(RelationshipNs + "id");
                }

                using (Stream stream = relsEntry.Open())
                {
                    XDocument relDoc = XDocument.Load(stream);
                    XElement rel = relDoc.Descendants(PackageRelationshipNs + "Relationship")
                        .FirstOrDefault(x => (string)x.Attribute("Id") == relationId);

                    if (rel == null)
                        throw new InvalidOperationException("Không tìm thấy relationship của sheet đầu tiên.");

                    string target = ((string)rel.Attribute("Target") ?? string.Empty).Replace('\\', '/');
                    if (target.StartsWith("/"))
                        return target.TrimStart('/');

                    if (target.StartsWith("xl/", StringComparison.OrdinalIgnoreCase))
                        return target;

                    return "xl/" + target;
                }
            }

            private static string ReadCellValue(XElement cell, List<string> sharedStrings, HashSet<int> dateStyleIndexes)
            {
                string type = (string)cell.Attribute("t");

                if (type == "inlineStr")
                    return string.Concat(cell.Descendants(SpreadsheetNs + "t").Select(x => x.Value));

                string raw = cell.Element(SpreadsheetNs + "v")?.Value ?? string.Empty;

                if (type == "s")
                {
                    int sharedIndex;
                    if (int.TryParse(raw, out sharedIndex) && sharedIndex >= 0 && sharedIndex < sharedStrings.Count)
                        return sharedStrings[sharedIndex];

                    return string.Empty;
                }

                if (type == "b")
                    return raw == "1" ? "TRUE" : "FALSE";

                int styleIndex;
                if (int.TryParse((string)cell.Attribute("s"), out styleIndex) && dateStyleIndexes.Contains(styleIndex))
                {
                    double serial;
                    if (double.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out serial))
                    {
                        try
                        {
                            return DateTime.FromOADate(serial).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                        }
                        catch
                        {
                            return raw;
                        }
                    }
                }

                return raw;
            }

            private static int GetColumnIndexFromCellReference(string cellReference)
            {
                if (string.IsNullOrWhiteSpace(cellReference)) return 0;

                int index = 0;
                foreach (char c in cellReference)
                {
                    if (!char.IsLetter(c)) break;
                    index = index * 26 + (char.ToUpperInvariant(c) - 'A' + 1);
                }

                return Math.Max(0, index - 1);
            }

            private static XDocument BuildWorksheetDocument(List<string[]> rows)
            {
                XElement sheetData = new XElement(SpreadsheetNs + "sheetData");

                for (int r = 0; r < rows.Count; r++)
                {
                    XElement rowElement = new XElement(SpreadsheetNs + "row",
                        new XAttribute("r", r + 1));

                    string[] values = rows[r] ?? new string[0];
                    for (int c = 0; c < values.Length; c++)
                    {
                        XElement cell = new XElement(SpreadsheetNs + "c",
                            new XAttribute("r", GetCellReference(r + 1, c + 1)),
                            new XAttribute("t", "inlineStr"),
                            new XElement(SpreadsheetNs + "is",
                                new XElement(SpreadsheetNs + "t", values[c] ?? string.Empty)));

                        rowElement.Add(cell);
                    }

                    sheetData.Add(rowElement);
                }

                return new XDocument(
                    new XDeclaration("1.0", "UTF-8", "yes"),
                    new XElement(SpreadsheetNs + "worksheet",
                        new XAttribute(XNamespace.Xmlns + "r", RelationshipNs.NamespaceName),
                        new XElement(SpreadsheetNs + "dimension",
                            new XAttribute("ref", $"A1:{GetCellReference(Math.Max(1, rows.Count), ImportExcelColumnCount + 1)}")),
                        new XElement(SpreadsheetNs + "sheetViews",
                            new XElement(SpreadsheetNs + "sheetView", new XAttribute("workbookViewId", "0"))),
                        new XElement(SpreadsheetNs + "sheetFormatPr", new XAttribute("defaultRowHeight", "15")),
                        sheetData));
            }

            private static string GetCellReference(int rowNumber, int columnNumber)
            {
                int dividend = columnNumber;
                string columnName = string.Empty;

                while (dividend > 0)
                {
                    int modulo = (dividend - 1) % 26;
                    columnName = Convert.ToChar('A' + modulo) + columnName;
                    dividend = (dividend - modulo) / 26;
                }

                return columnName + rowNumber.ToString(CultureInfo.InvariantCulture);
            }

            private static void AddXmlEntry(ZipArchive archive, string path, XDocument doc)
            {
                ZipArchiveEntry entry = archive.CreateEntry(path);
                using (Stream stream = entry.Open())
                    doc.Save(stream);
            }

            private static void AddTextEntry(ZipArchive archive, string path, string content)
            {
                ZipArchiveEntry entry = archive.CreateEntry(path);
                using (StreamWriter writer = new StreamWriter(entry.Open(), new UTF8Encoding(false)))
                    writer.Write(content);
            }
        }
    }
}
