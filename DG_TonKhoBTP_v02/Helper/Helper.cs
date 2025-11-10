using DG_TonKhoBTP_v02.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DG_TonKhoBTP_v02.Helper
{
    public static class Helper
    {
        public static string _connStr;

        public static string GetShiftValue()
        {
            int hour = DateTime.Now.Hour;

            if (hour >= 6 && hour < 14)
                return "1";

            if (hour >= 14 && hour < 22)
                return "2";

            return "3";

        }

        public static string TaoSQL_LayDLTTThanhPham()
        {
            // KHÔNG đặt dấu ; ở cuối vì còn nối UNION ALL ở hàm sau
            return @"
            SELECT
                t.KhoiLuongSau AS KlBatDau,
                t.ChieuDaiSau  AS CDBatDau,
                t.id           AS id,
                d.id            as DanhSachMaSP_ID,
                t.MaBin        AS BinNVL
            FROM TTThanhPham AS t
            JOIN DanhSachMaSP AS d
                ON d.id = t.DanhSachSP_ID
            WHERE
                (
                    (d.DonVi = 'KG' AND t.KhoiLuongSau <> 0)
                    OR
                    (d.DonVi = 'M' AND t.ChieuDaiSau  <> 0)
                )
                AND (
                    @ten IS NULL OR TRIM(@ten) = ''
                    OR t.MaBin LIKE '%' || @ten || '%' COLLATE NOCASE
                )
            ";
        }

        public static string TaoSQL_LayDLNVL_TTThanhPham()
        {
            string baseQuery = TaoSQL_LayDLTTThanhPham(); // KHÔNG có ; ở cuối

            return baseQuery + @"
            UNION ALL

            SELECT
                -1      AS KlBatDau,
                -1      AS CDBatDau,
                d.id    AS id,
                d.id            as DanhSachMaSP_ID,
                d.ten   AS BinNVL
            FROM DanhSachMaSP AS d
            WHERE
                d.Ma LIKE 'NVL.%' COLLATE NOCASE
                AND (
                    @ten IS NULL OR TRIM(@ten) = ''
                    OR d.Ten LIKE '%' || @ten || '%' COLLATE NOCASE
                    OR d.Ma  LIKE '%' || @ten || '%' COLLATE NOCASE
                )
            ";
        }

        public static string TaoKhoangTrong(int tongKhoangTrong, string noiDung)
        {
            return new string(' ', tongKhoangTrong - noiDung.Length);

        }

        public static string GetNgayHienTai()
        {
            DateTime now = DateTime.Now;
            DateTime ngayHienTai = (now.TimeOfDay < new TimeSpan(6, 0, 0))
                ? DateTime.Today.AddDays(-1)
                : DateTime.Today;

            return ngayHienTai.ToString("yyyy-MM-dd");
        }

        public static string TaoSqL_LayThongTinBaoCaoChung()
        {
            return @"
            SELECT
              ttp.id AS STT,
              tclv.Ngay, tclv.Ca, tclv.May,
              ttp.MaBin as MaBin, ds.Ten AS Ten, ds.Ma AS Ma, ds.id AS id,
              tclv.NguoiLam, tclv.ToTruong, tclv.QuanDoc,
              ttp.KhoiLuongTruoc AS KhoiLuongTruoc, ttp.KhoiLuongSau as KhoiLuongSau,
              ttp.ChieuDaiTruoc as ChieuDaiTruoc, ttp.ChieuDaiSau as ChieuDaiSau,
              ttp.Phe as Phe, ttp.GhiChu as GhiChu ";
        }

        public static string TaoSQL_DSMaBin(string tenHienThi)
        {
            return @" SELECT MaBin as" +tenHienThi + "FROM TTThanhPham WHERE MaBin LIKE '%' || @ma || '%'; ";
        }

        public static string TaoSQL_LayDLTruyVet(bool col, string key, out string selectedCol)
        {
            // Cột hiển thị trong ComboBox (phải đúng tên alias trong DataTable)
            selectedCol = (col) ? "Ten" : "MaBin";

            // Cột dùng cho điều kiện WHERE (có thể giữ prefix bảng)
            string whereCol = (col) ? "SP.Ten" : "TT.MaBin";

            string sql = $@"
                SELECT
                    TT.id AS STT,
                    CLV.Ngay AS Ngay,
                    CLV.Ca AS Ca,
                    TT.MaBin AS MaBin,
                    SP.Ten AS Ten,
                    SP.Ma AS Ma,
                    CLV.May AS May,
                    CLV.NguoiLam AS NguoiLam,
                    TT.CongDoan AS CongDoan,
                    TT.KhoiLuongTruoc,
                    TT.KhoiLuongSau,
                    TT.ChieuDaiTruoc,
                    TT.ChieuDaiSau,
                    TT.Phe,
                    TT.GhiChu,
                    TT.CongDoan,
                    CLV.ToTruong AS ToTruong,
                    CLV.QuanDoc AS QuanDoc
                FROM TTThanhPham AS TT
                INNER JOIN DanhSachMaSP AS SP
                    ON TT.DanhSachSP_ID = SP.id
                INNER JOIN ThongTinCaLamViec AS CLV
                    ON TT.ThongTinCaLamViec_ID = CLV.id
                WHERE {whereCol} LIKE '%' || @{key} || '%';
            ";
            return sql;
        }


        public static string TaoSQL_TaoKetNoiCacBang()
        {
            return @"
                FROM TTThanhPham ttp
                JOIN ThongTinCaLamViec tclv ON tclv.id = ttp.ThongTinCaLamViec_ID
                JOIN DanhSachMaSP ds        ON ds.id   = ttp.DanhSachSP_ID
                LEFT JOIN CaiDatCDBoc  cdb  ON cdb.TTThanhPham_ID   = ttp.id
                LEFT JOIN CD_BocVo     cbv  ON cbv.CaiDatCDBoc_ID   = cdb.id
                LEFT JOIN CD_BocLot    cbl  ON cbl.CaiDatCDBoc_ID   = cdb.id
                LEFT JOIN CD_BocMach   cbm  ON cbm.CaiDatCDBoc_ID   = cdb.id
                LEFT JOIN CD_KeoRut    ckr  ON ckr.TTThanhPham_ID   = ttp.id
                LEFT JOIN CD_BenRuot   cbr  ON cbr.TTThanhPham_ID   = ttp.id
                LEFT JOIN CD_GhepLoiQB cgl  ON cgl.TTThanhPham_ID   = ttp.id
                LEFT JOIN TTNVL        nvl  ON nvl.TTThanhPham_ID   = ttp.id
                LEFT JOIN DanhSachMaSP ds_nvl ON ds_nvl.id          = nvl.DanhSachMaSP_ID
            ";
        }


        public static string TaoSQL_LayChiTiet_1CD(int id)
        {
            string[] dsCotCongDoan = ChiTietCongDoanBoc.DSTenCotRieng;

            string sqlChung = "";

            if (2 < id && id < 6) sqlChung = ChiTietCongDoanBoc.DSTenCotChung + ", ";

            if (id > 5) id = 2;

            return sqlChung + dsCotCongDoan[id];
        }


        public static (string Columns, string DieuKien) TaoSQL_LayChiTiet_NhieuCD(List<CongDoan> selectedCongDoans)
        {
            if (selectedCongDoans == null || selectedCongDoans.Count == 0)
                return (string.Empty, string.Empty);

            // Lấy danh sách cột
            var allCols = selectedCongDoans
                .Select(cd => TaoSQL_LayChiTiet_1CD(cd.Id))
                .SelectMany(s => s.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct(StringComparer.OrdinalIgnoreCase);

            string columnsSql = string.Join(", ", allCols);

            // Tạo điều kiện WHERE: (ttp.CongDoan = 1 OR ttp.CongDoan = 2 ...)
            string dieuKienSql = " AND (" + string.Join(" OR ", selectedCongDoans.Select(cd => $"ttp.CongDoan = {cd.Id}")) + ")";

            return (columnsSql, dieuKienSql);
        }


        public static string TaoSQL_LayDuLieuNVL(params IEnumerable<ColumnDefinition>[] groups)
        {
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (groups != null)
            {
                foreach (var group in groups)
                {
                    if (group == null) continue;
                    foreach (var col in group)
                    {
                        var name = col?.Name?.Trim();
                        if (string.IsNullOrWhiteSpace(name)) continue;
                        if (string.Equals(name, "id", StringComparison.OrdinalIgnoreCase)) continue; // bỏ nvl.id
                        names.Add(name);
                    }
                }
            }

            var cols = new List<string>();
            // luôn thêm tên NVL (alias từ bảng DanhSachMaSP dành cho NVL)
            cols.Add("ds_nvl.Ten AS TenNVL");

            // các cột NVL khác (nếu có)
            cols.AddRange(names.Select(n => $"nvl.{n} AS {n}"));

            return string.Join(", ", cols); // KHÔNG có dấu phẩy đầu/cuối
        }



        public static void SetIfPresent(DataRow row, string col, Action<object> setter)
        {
            if (row.Table.Columns.Contains(col))
            {
                var val = row[col];
                if (val != DBNull.Value) setter(val);
            }
        }

        public static string LOTGenerated(ComboBox may, NumericUpDown maHT, ComboBox sttCongDoan, NumericUpDown sttBin, NumericUpDown soBin)
        {
            string lot = "";

            // Kiểm tra maHT có đủ 6 chữ số
            int maHTValue = (int)maHT.Value;
            if (maHTValue < 100000 || maHTValue > 999999)
                return lot;

            // Kiểm tra ComboBox 'may'
            if (may.Text == null ||
                string.IsNullOrWhiteSpace(may.Text) ||
                may.Text == "0")
                return lot;

            // Kiểm tra sttCongDoan
            if (sttCongDoan.SelectedItem == null ||
                string.IsNullOrWhiteSpace(sttCongDoan.Text) ||
                sttCongDoan.Text == "0")
                return lot;

            // Kiểm tra sttBin và soBin
            if (sttBin.Value == 0)
                return lot;

            string sttBinT = sttBin.Value < 10 ? "0" + sttBin.Text : sttBin.Text;
            string soBinT = soBin.Value < 10 ? "0" + soBin.Text : soBin.Text;

            // Tạo mã LOT: may-maHT-sttCongDoan-sttBin-soBin
            lot = $"{may.Text}-{maHTValue}/{sttCongDoan.Text}-{sttBinT}-{soBinT}";

            return lot;
        }

        public static T FindControlRecursive<T>(Control root) where T : Control
        {
            foreach (Control c in root.Controls)
            {
                if (c is T t)
                    return t;

                if (c.HasChildren)
                {
                    var child = FindControlRecursive<T>(c);
                    if (child != null)
                        return child;
                }
            }
            return null;
        }

        public static void MapRowToObject<T>(DataGridViewRow row, T target)
        {
            var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var p in props)
            {
                if (!row.DataGridView.Columns.Contains(p.Name))
                    continue;

                var raw = row.Cells[p.Name]?.Value;

                try
                {
                    object value = null;

                    if (p.PropertyType == typeof(string))
                    {
                        value = raw?.ToString() ?? string.Empty;
                    }
                    else if (IsNumeric(p.PropertyType))
                    {
                        // Ô trống => 0
                        var s = raw?.ToString();
                        if (string.IsNullOrWhiteSpace(s))
                        {
                            value = ConvertToNumericDefaultZero(p.PropertyType);
                        }
                        else
                        {
                            value = Convert.ChangeType(s, Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType);
                        }
                    }
                    else
                    {
                        // Kiểu khác (int?, double?, …)
                        var underlying = Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType;
                        if (raw == null || string.IsNullOrWhiteSpace(raw.ToString()))
                        {
                            value = underlying.IsValueType ? Activator.CreateInstance(underlying) : null;
                        }
                        else
                        {
                            value = Convert.ChangeType(raw, underlying);
                        }
                    }

                    p.SetValue(target, value);
                }
                catch
                {
                    // Nếu chuyển kiểu lỗi -> gán 0 cho numeric, "" cho string
                    if (p.PropertyType == typeof(string))
                        p.SetValue(target, string.Empty);
                    else if (IsNumeric(p.PropertyType))
                        p.SetValue(target, ConvertToNumericDefaultZero(p.PropertyType));
                }
            }
        }

        private static bool IsNumeric(Type t)
        {
            t = Nullable.GetUnderlyingType(t) ?? t;
            return t == typeof(int) || t == typeof(long) || t == typeof(short) ||
                   t == typeof(double) || t == typeof(float) || t == typeof(decimal);
        }

        private static object ConvertToNumericDefaultZero(Type t)
        {
            t = Nullable.GetUnderlyingType(t) ?? t;
            if (t == typeof(int)) return 0;
            if (t == typeof(long)) return 0L;
            if (t == typeof(short)) return (short)0;
            if (t == typeof(double)) return 0.0d;
            if (t == typeof(float)) return 0.0f;
            if (t == typeof(decimal)) return 0.0m;
            return 0;
        }

        public static string[] CatMaBin(string input)
        {
            // Tách chuỗi bằng cả '-' và '/'
            char[] separators = { '-', '/' };
            return input.Split(separators, StringSplitOptions.RemoveEmptyEntries);
        }

        public static string ShowErrorDatabase(Exception ex, string? ten = null)
        {
            // Bóc lớp Aggregate/TargetInvocation/InnerException thường gặp
            while (ex.InnerException != null &&
                   (ex is AggregateException || ex is TargetInvocationException))
            {
                ex = ex.InnerException!;
            }

            // 1) SQLiteException: chi tiết hoá theo loại ràng buộc/lỗi
            if (ex is SQLiteException sqliteEx)
            {
                // Một số hệ lib có ResultCode/ExtendedResultCode; nếu không có, dùng ErrorCode + Message
                // Map nhanh theo mã lỗi trước
                switch (sqliteEx.ErrorCode)
                {
                    // 19
                    case (int)SQLiteErrorCode.Constraint:
                        {
                            var msg = sqliteEx.Message ?? string.Empty;

                            // Nhận diện từng loại constraint theo message phổ biến của SQLite
                            if (msg.IndexOf("NOT NULL constraint failed", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                var col = ExtractTail(msg); // lấy "Bảng.Cột" nếu có
                                return $"Thiếu dữ liệu bắt buộc{FormatField(col)}. Vui lòng kiểm tra và nhập đầy đủ.";
                            }
                            if (msg.IndexOf("UNIQUE constraint failed", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                var col = ExtractTail(msg);
                                return $"{ten ?? "DỮ LIỆU"} đã tồn tại{FormatField(col)}. Vui lòng kiểm tra trùng lặp.";
                            }
                            if (msg.IndexOf("FOREIGN KEY constraint failed", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                msg.IndexOf("foreign key", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                return $"Dữ liệu tham chiếu không hợp lệ. Vui lòng kiểm tra các khoá ngoại (bản ghi cha/bảng liên quan).";
                            }
                            if (msg.IndexOf("CHECK constraint failed", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                var rule = ExtractTail(msg);
                                return $"Dữ liệu vi phạm ràng buộc kiểm tra{FormatField(rule)}. Vui lòng xem lại điều kiện hợp lệ.";
                            }
                            if (msg.IndexOf("PRIMARY KEY", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                var col = ExtractTail(msg);
                                return $"{ten ?? "DỮ LIỆU"} đã tồn tại (trùng khoá chính){FormatField(col)}.";
                            }

                            // Fallback chung cho Constraint
                            return $"{ten ?? "DỮ LIỆU"} không thoả ràng buộc. {Normalize(sqliteEx.Message)}";
                        }

                    // 5
                    case (int)SQLiteErrorCode.Busy:
                        return "Cơ sở dữ liệu đang bận, vui lòng thử lại sau giây lát.";
                    // 6
                    case (int)SQLiteErrorCode.Locked:
                        return "Cơ sở dữ liệu đang bị khoá bởi tiến trình khác. Hãy đóng kết nối/ứng dụng đang sử dụng rồi thử lại.";
                    // 10
                    //case (int)SQLiteErrorCode.IOErr:
                    //    return "Lỗi I/O khi truy cập tệp cơ sở dữ liệu. Vui lòng kiểm tra quyền truy cập và dung lượng đĩa.";
                    // 13
                    case (int)SQLiteErrorCode.Perm:
                        return "Không đủ quyền để thao tác với cơ sở dữ liệu. Vui lòng kiểm tra quyền truy cập tệp.";
                    // 14
                    case (int)SQLiteErrorCode.Abort:
                        return "Thao tác với cơ sở dữ liệu đã bị huỷ.";
                    // 8
                    case (int)SQLiteErrorCode.ReadOnly:
                        return "Cơ sở dữ liệu ở chế độ chỉ đọc. Vui lòng bật quyền ghi hoặc chọn tệp khác.";
                    // 11
                    case (int)SQLiteErrorCode.Corrupt:
                        return "Tệp cơ sở dữ liệu có dấu hiệu hỏng. Hãy khôi phục từ bản sao lưu.";
                    // 13 (FULL trong 1 số binding khác; nếu không có, bỏ qua)
                    case (int)SQLiteErrorCode.Full:
                        return "Đĩa đầy. Không thể ghi dữ liệu mới.";
                    // 23
                    case (int)SQLiteErrorCode.Auth:
                        return "Xác thực cơ sở dữ liệu thất bại.";
                    // 25
                    case (int)SQLiteErrorCode.Range:
                        return "Tham số câu lệnh SQL không hợp lệ hoặc nằm ngoài phạm vi.";
                    // 1
                    case (int)SQLiteErrorCode.Error:
                    default:
                        // Rơi về thông điệp gốc nhưng đã “mềm hoá”
                        return $"Lỗi cơ sở dữ liệu: {Normalize(sqliteEx.Message)}";
                }
            }

            // 2) Một số lỗi .NET thường gặp khi thao tác dữ liệu
            if (ex is TimeoutException)
                return "Quá thời gian chờ khi thao tác với cơ sở dữ liệu. Vui lòng thử lại.";
            if (ex is InvalidCastException)
                return "Kiểu dữ liệu không khớp, vui lòng kiểm tra lại định dạng nhập.";
            if (ex is FormatException)
                return "Giá trị nhập sai định dạng. Vui lòng kiểm tra số/ngày giờ/ký tự.";
            if (ex is ArgumentNullException ane)
                return $"Thiếu tham số bắt buộc: {ane.ParamName ?? "không rõ"}.";
            if (ex is ArgumentOutOfRangeException aore)
                return $"Giá trị vượt phạm vi hợp lệ: {aore.ParamName ?? "không rõ"}.";
            if (ex is ArgumentException)
                return Normalize(ex.Message);
            if (ex is InvalidOperationException)
                return "Thao tác hiện không hợp lệ trong trạng thái hiện tại. Vui lòng kiểm tra luồng xử lý.";
            if (ex is NullReferenceException)
                return "Một dữ liệu cần thiết chưa được khởi tạo. Vui lòng kiểm tra biểu mẫu nhập.";
            if (ex is UnauthorizedAccessException)
                return "Không đủ quyền truy cập tài nguyên cơ sở dữ liệu.";
            if (ex is System.IO.IOException)
                return "Lỗi đọc/ghi tệp cơ sở dữ liệu. Vui lòng kiểm tra đường dẫn và quyền truy cập.";

            // 3) Fallback cuối
            return $"Đã xảy ra lỗi: {Normalize(ex.Message)}";
        }

        // Chuẩn hoá thông điệp: cắt xuống 1 dòng, bỏ ký tự xuống dòng thừa
        private static string Normalize(string? message)
        {
            if (string.IsNullOrWhiteSpace(message)) return "Không có thêm thông tin.";
            var oneLine = message.Replace("\r", " ").Replace("\n", " ").Trim();
            // Có thể cắt ngắn nếu quá dài
            return oneLine.Length > 350 ? oneLine.Substring(0, 350) + "..." : oneLine;
        }

        // Trích phần đuôi "Bảng.Cột" hoặc tên ràng buộc từ thông điệp SQLite
        // Ví dụ: "NOT NULL constraint failed: CaiDatCDBoc.TTNhua" -> "CaiDatCDBoc.TTNhua"
        private static string? ExtractTail(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return null;

            // Thử bắt cụm sau dấu ":" (thường là Table.Column hoặc tên constraint)
            var colonIdx = message.LastIndexOf(':');
            if (colonIdx >= 0 && colonIdx + 1 < message.Length)
            {
                var tail = message.Substring(colonIdx + 1).Trim();
                if (!string.IsNullOrEmpty(tail)) return tail;
            }

            // Thử Regex cho "failed: <something>"
            var m = Regex.Match(message, @"failed:\s*(.+)$", RegexOptions.IgnoreCase);
            if (m.Success) return m.Groups[1].Value.Trim();

            return null;
        }

        private static string FormatField(string? fieldOrRule)
            => string.IsNullOrWhiteSpace(fieldOrRule) ? "" : $" ({fieldOrRule})";

        public static string ConvertTiengVietKhongDau(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            // Chuẩn hóa chuỗi về dạng FormD (mỗi ký tự có dấu được tách ra thành ký tự + dấu)
            string normalized = input.Normalize(NormalizationForm.FormD);

            // Loại bỏ các ký tự dấu (NonSpacingMark)
            StringBuilder sb = new StringBuilder();
            foreach (char c in normalized)
            {
                UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(c);
                if (category != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }

            // Chuẩn hóa lại về FormC (chuẩn bình thường)
            string result = sb.ToString().Normalize(NormalizationForm.FormC);

            // Xử lý đặc biệt cho "đ" và "Đ"
            result = result.Replace('đ', 'd').Replace('Đ', 'D');

            // Loại bỏ khoảng trắng thừa (tùy chọn)
            result = Regex.Replace(result, @"\s+", " ").Trim();

            return result;
        }

        public static List<CongDoan> GetCheckedCongDoans(TableLayoutPanel tbCheckBox)
        {
            var result = new List<CongDoan>();

            foreach (Control control in tbCheckBox.Controls)
            {
                if (control is CheckBox cb && cb.Checked)
                {
                    if (cb.Tag is CongDoan congDoan)
                    {
                        result.Add(congDoan);
                    }
                }
            }

            return result;
        }


    }


}
