using DG_TonKhoBTP_v02.Database;
using DG_TonKhoBTP_v02.Dictionary;
using DG_TonKhoBTP_v02.Models;
using DG_TonKhoBTP_v02.UI;
using DG_TonKhoBTP_v02.UI.Helper;
using DocumentFormat.OpenXml.Wordprocessing;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Drawing.Printing;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using WinCheckBox = System.Windows.Forms.CheckBox;
using WinControl = System.Windows.Forms.Control;

namespace DG_TonKhoBTP_v02.Helper
{
    public static class Helper
    {
        public static string _connStr;

        public static bool KiemTraEmpty(string values)
        {
           if (string.IsNullOrWhiteSpace(values))
            {
                FrmWaiting.ShowGifAlert("KHÔNG TÌM THẤY ĐƯỜNG DẪN DATABASE.");
                return true;
            }
            return false;
        }

        // có phải tên máy thuộc danh sách máy đặc biệt hay không: true là thuộc
        

        // kiểm tra khối lượng đầu vào có lớn hơn khối lượng cuối không: true là khối lượng đầu vào lớn hơn
        public static bool KLDauVaoLonHonKLCuoi(decimal klDau, decimal klCuoi)
        {
            return klDau > klCuoi;
        }

        public static List<CongDoan> GetDanhSachCongDoan()
        {
            return ThongTinChungCongDoan.TatCaCongDoan
                .Select(cd =>
                {
                    cd.TenCongDoan = cd.TenCongDoan.ToUpper();
                    return cd;
                })
                .OrderBy(cd => cd.Id)
                .ToList();
        }

        public static double GetKhoiLuongDongThua()
        {
            using var f = new GetUserInputValue_Simple();
            f.StartPosition = FormStartPosition.CenterScreen;
            var result = f.ShowDialog();

            if (result == DialogResult.OK)
            {
                return (double)f.TongDongThuaValue;
            }else
                return 0;
        }

        public static string TaoThongBao(Label lb = null)
        {
            ConfigDB configDB = DatabaseHelper.GetConfig();

            // Nếu Active == true ⇒ chỉ ẩn label và thoát
            if (configDB  == null || configDB.Active)
            {
                if (lb != null) lb.Visible = false;
                return "";
            }

            // Đến đây nghĩa là Active == false
            string tb = $"{configDB.Author}: {configDB.Message} ".ToUpper();

            FrmWaiting.ShowGifAlert(tb, "THÔNG BÁO");

            if (lb != null)
            {
                lb.Text = tb.Replace("\n", " ");
                lb.Visible = true;
            }

            return tb;
        }

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
                t.CongDoan      AS CongDoan,
                t.KhoiLuongSau  AS KlBatDau,
                t.ChieuDaiSau   AS CDBatDau,
                t.id            AS id,
                d.Ma           AS MaNVL,
                d.DonVi         AS DonVi,
                d.id            as DanhSachMaSP_ID,
                t.Qc            as Qc,
                t.MaBin         AS BinNVL,
                v.Ngay          AS Ngay,
                v.Ca            AS Ca,
                v.NguoiLam      AS NguoiLam,
                t.GhiChu        as GhiChu
            FROM TTThanhPham AS t
            JOIN DanhSachMaSP AS d
                ON d.id = t.DanhSachSP_ID
            JOIN ThongTinCaLamViec AS v
                ON t.id = v.TTThanhPham_id
            WHERE
                (
                    (d.DonVi = 'KG' AND t.KhoiLuongSau <> 0)
                    OR
                    (d.DonVi = 'M' AND t.ChieuDaiSau  <> 0)
                )
                AND 
                (
                    @ten IS NULL OR TRIM(@ten) = ''
                    OR t.MaBin LIKE '%' || @ten || '%' COLLATE NOCASE
                )
                AND
                (
                    t.Active = 1    
                )
            ";
        }

        public static string TaoSQL_LayDLNVL_TTThanhPham()
        {
            string baseQuery = TaoSQL_LayDLTTThanhPham(); // KHÔNG có ; ở cuối

            return baseQuery + @"
            UNION ALL

            SELECT
                -1          AS CongDoan,
                -1          AS KlBatDau,
                -1          AS CDBatDau,
                d.id        AS id,
                d.ma        AS MaNVL,
                d.DonVi     AS DonVi,
                d.id        AS DanhSachMaSP_ID,
                'NA'         As Qc,
                d.Ten       AS BinNVL,
                NULL        AS Ngay,
                ''          AS Ca,
                ''          AS NguoiLam,
                ''          as GhiChu
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
              tclv.Ngay, tclv.Ca, tclv.May,nvl.QC,
              ttp.MaBin as MaBin, ds.Ten AS Ten, ds.Ma AS Ma,ds.DonVi, ds.id AS id,
              tclv.NguoiLam, tclv.ToTruong, tclv.QuanDoc,
              ttp.KhoiLuongTruoc AS KhoiLuongTruoc, ttp.KhoiLuongSau as KhoiLuongSau,
              ttp.ChieuDaiTruoc as ChieuDaiTruoc, ttp.ChieuDaiSau as ChieuDaiSau,
              ttp.Phe as Phe, ttp.HanNoi as HanNoi, ttp.GhiChu as GhiChu ";
        }

        public static string TaoSqL_LayThongTinBaoCaoChung_Edit()
        {
            return @"
            SELECT
              ttp.id AS STT,
              ttp_bin.id AS id, 
              tclv.Ngay, tclv.Ca, tclv.May,nvl.QC,
              ttp.MaBin as MaBin, ds.Ten AS Ten, ds.Ma AS Ma,ds.DonVi, ds.id AS DanhSachMaSP_ID,
              tclv.NguoiLam, tclv.ToTruong, tclv.QuanDoc,
              ttp.KhoiLuongTruoc AS KhoiLuongTruoc, ttp.KhoiLuongSau as KhoiLuongSau,
              ttp.ChieuDaiTruoc as ChieuDaiTruoc, ttp.ChieuDaiSau as ChieuDaiSau,
              ttp.Phe as Phe, ttp.HanNoi as HanNoi, ttp.GhiChu as GhiChu ";
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
                    CLV.ToTruong AS ToTruong,
                    CLV.QuanDoc AS QuanDoc
                FROM TTThanhPham AS TT
                INNER JOIN DanhSachMaSP AS SP
                    ON TT.DanhSachSP_ID = SP.id
                INNER JOIN ThongTinCaLamViec AS CLV
                    ON CLV.TTThanhPham_id = TT.id
                WHERE {whereCol} LIKE '%' || @{key} || '%';
            ";
            return sql;
        }

        public static string TaoSQL_TaoKetNoiCacBang()
        {
            return @"
                FROM TTThanhPham ttp
                JOIN ThongTinCaLamViec tclv ON ttp.id = tclv.TTThanhPham_ID
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

        public static (string query, string loaiCD) TaoSQL_BCSX( List<CongDoan> selectedCongDoans)
        {
            // Tạo phần SELECT chung
            string sqlSelect = TaoSqL_LayThongTinBaoCaoChung();

            // Lấy chi tiết công đoạn
            var (sqlLayChiTietCD, loaiCD) = TaoSQL_LayChiTiet_NhieuCD(selectedCongDoans);


            // Lấy dữ liệu NVL theo danh sách công đoạn
            string sqlTenNVL = TaoSQL_LayDuLieuNVL(selectedCongDoans.Select(cd => cd.Columns).ToArray());


            // Câu nối các bảng
            string sqlJoin = TaoSQL_TaoKetNoiCacBang();

            // Format ngày sang dạng SQLite hiểu được
            //string ngayBD = ngayBatDau.Date.AddHours(5).AddMinutes(59).ToString("yyyy-MM-dd HH:mm:ss");

            //string ngayKT = ngayKetThuc.Date.AddDays(1).AddHours(6).ToString("yyyy-MM-dd HH:mm:ss");

            // Điều kiện WHERE – chèn trực tiếp giá trị ngày
            //string sqlDkNgay = $" WHERE date(tclv.Ngay) >= date('{ngayBD}') AND date(tclv.Ngay) <= date('{ngayKT}')";


            // Sắp xếp
            //string sqlOrder = " ORDER BY tclv.Ngay DESC, ttp.id DESC;";

            // Ghép chuỗi hoàn chỉnh
            //string query = sqlSelect + " ," + sqlLayChiTietCD + " ," + sqlTenNVL + sqlJoin + sqlDkNgay + loaiCD + sqlOrder;
            string query = sqlSelect + " ," + sqlLayChiTietCD + " ," + sqlTenNVL + sqlJoin + loaiCD ;

            return (query, loaiCD);
        }

        public static (string Columns, string DieuKien)   TaoSQL_LayChiTiet_NhieuCD(List<CongDoan> selectedCongDoans)
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
                        if (string.Equals(name, "DanhSachMaSP_ID", StringComparison.OrdinalIgnoreCase)) continue; // bỏ nvl.id
                        if (string.Equals(name, "CongDoan", StringComparison.OrdinalIgnoreCase)) continue; // bỏ nvl.id
                        if (string.Equals(name, "DonVi", StringComparison.OrdinalIgnoreCase)) continue; // bỏ nvl.id
                        if (string.Equals(name, "TenNVL", StringComparison.OrdinalIgnoreCase)) continue; // bỏ nvl.id
                        if (string.Equals(name, "MaNVL", StringComparison.OrdinalIgnoreCase)) continue; // bỏ nvl.id
                        if (string.Equals(name, "Ngay", StringComparison.OrdinalIgnoreCase)) continue; // bỏ nvl.id
                        if (string.Equals(name, "Ca", StringComparison.OrdinalIgnoreCase)) continue; // bỏ nvl.id
                        if (string.Equals(name, "NguoiLam", StringComparison.OrdinalIgnoreCase)) continue; // bỏ nvl.id
                        if (string.Equals(name, "GhiChu", StringComparison.OrdinalIgnoreCase)) continue; // bỏ nvl.id
                        if (string.Equals(name, "QC", StringComparison.OrdinalIgnoreCase)) continue; // bỏ nvl.id
                        names.Add(name);
                    }
                }
            }

            var cols = new List<string>();
            // luôn thêm tên NVL (alias từ bảng DanhSachMaSP dành cho NVL)
            cols.Add("ds_nvl.Ten AS TenNVL");
            //cols.Add("ds_nvl.DonVi AS DonVi");
            //cols.Add("tclv.Ngay AS Ngay");
            //cols.Add("tclv.Ca AS Ca");
            //cols.Add("tclv.NguoiLam AS CaNguoiLam");
            //cols.Add("ttp.GhiChu AS GhiChu");
            
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

        public static T FindControlRecursive<T>(WinControl root) where T : WinControl
        {
            foreach (WinControl c in root.Controls)
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
            // Lấy tất cả property public instance của kiểu T
            var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var p in props)
            {
                // Nếu DataGridView không có cột trùng tên property thì bỏ qua
                if (!row.DataGridView.Columns.Contains(p.Name))
                    continue;

                // Lấy giá trị ô tương ứng với property p
                var raw = row.Cells[p.Name]?.Value;

                try
                {
                    object value = null;

                    // Nếu property là string
                    if (p.PropertyType == typeof(string))
                    {
                        // Nếu null thì cho chuỗi rỗng
                        value = raw?.ToString() ?? string.Empty;
                    }
                    // Nếu là kiểu numeric (int, double, decimal, ...) hoặc nullable của chúng (int?, double?, ...)
                    else if (IsNumeric(p.PropertyType))
                    {
                        var s = raw?.ToString();

                        // Ô trống hoặc whitespace
                        if (string.IsNullOrWhiteSpace(s))
                        {
                            // Nếu property là kiểu nullable (int?, double?, ...)
                            if (Nullable.GetUnderlyingType(p.PropertyType) != null)
                            {
                                // => gán null theo yêu cầu của bạn
                                value = null;
                            }
                            else
                            {
                                // Nếu là kiểu numeric không nullable (int, double, ...) thì gán 0
                                value = ConvertToNumericDefaultZero(p.PropertyType);
                            }
                        }
                        else
                        {
                            // Có giá trị -> convert sang kiểu underlying (nếu là nullable thì lấy kiểu gốc bên trong)
                            var targetType = Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType;
                            value = Convert.ChangeType(s, targetType);
                        }
                    }
                    else
                    {
                        // Các kiểu khác (ví dụ: DateTime?, bool?, ...)
                        var underlying = Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType;

                        if (raw == null || string.IsNullOrWhiteSpace(raw.ToString()))
                        {
                            // Nếu là nullable type -> cho null
                            if (Nullable.GetUnderlyingType(p.PropertyType) != null)
                            {
                                value = null;
                            }
                            else
                            {
                                // Nếu không phải nullable -> tạo instance mặc định (ví dụ: 0 cho int, 01/01/0001 cho DateTime, ...)
                                value = Activator.CreateInstance(underlying);
                            }
                        }
                        else
                        {
                            value = Convert.ChangeType(raw, underlying);
                        }
                    }

                    // Gán giá trị đã convert cho property
                    p.SetValue(target, value);
                }
                catch
                {
                    // Nếu lỗi convert:
                    if (p.PropertyType == typeof(string))
                    {
                        p.SetValue(target, string.Empty);
                    }
                    else if (IsNumeric(p.PropertyType))
                    {
                        // Nếu là nullable numeric -> cho null khi lỗi
                        if (Nullable.GetUnderlyingType(p.PropertyType) != null)
                        {
                            p.SetValue(target, null);
                        }
                        else
                        {
                            // Numeric không nullable -> cho 0
                            p.SetValue(target, ConvertToNumericDefaultZero(p.PropertyType));
                        }
                    }
                    else
                    {
                        // Các kiểu khác: nếu nullable -> null, nếu không nullable -> default của kiểu
                        if (Nullable.GetUnderlyingType(p.PropertyType) != null)
                        {
                            p.SetValue(target, null);
                        }
                        else
                        {
                            var underlying = Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType;
                            p.SetValue(target, Activator.CreateInstance(underlying));
                        }
                    }
                }
            }
        }

        private static bool IsNumeric(Type t)
        {
            // Nếu là nullable thì lấy kiểu gốc bên trong (ví dụ int? -> int)
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

        public static bool SplitByLastDash(string input, out string left, out string right)
        {
            left = "";
            right = "";

            if (string.IsNullOrWhiteSpace(input)) return false;

            int lastDash = input.LastIndexOf('-');
            if (lastDash <= 0 || lastDash >= input.Length - 1) return false;

            left = input.Substring(0, lastDash);
            right = input.Substring(lastDash + 1);
            return true;
        }


        public static string? TrimToNull(string? s)
        {
            s = (s ?? "").Trim();
            return string.IsNullOrWhiteSpace(s) ? null : s;
        }
        public static string? GetDateOrNull(DateTimePicker dtp)
        {
            const string DbDateFormat = "dd/MM/yyyy";
            // Quy ước: nếu < 02/01/2020 thì coi như không lọc
            return dtp.Value.Date < new DateTime(2000, 1, 2)
                ? null
                : dtp.Value.ToString(DbDateFormat);
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
                            if (ex is ArgumentException || ex is InvalidOperationException)
                                return ex.Message;

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

        private static string Normalize(string? message)
        {
            if (string.IsNullOrWhiteSpace(message)) return "Không có thêm thông tin.";
            var oneLine = message.Replace("\r", " ").Replace("\n", " ").Trim();
            // Có thể cắt ngắn nếu quá dài
            return oneLine.Length > 350 ? oneLine.Substring(0, 350) + "..." : oneLine;
        }

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

            foreach (WinControl control in tbCheckBox.Controls)
            {
                if (control is WinCheckBox cb && cb.Checked)
                {
                    if (cb.Tag is CongDoan congDoan)
                        result.Add(congDoan);
                }
            }

            return result;
        }

        public static string SetURLDatabase()
        {
            string result = "";

            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Title = "Chọn file database (.db)";
                dialog.Filter = "SQLite Database (*.db)|*.db|Tất cả các file (*.*)|*.*";
                dialog.Multiselect = false;

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    result = dialog.FileName;
                }
                else
                {
                    result = "";
                }
                return result;
            }
        }

        public static void UpdatePassApp(string tb)
        {
            string[] parts = tb.Split('|');

            if (parts.Count() == 2 && parts[0] == "Change")
                Properties.Settings.Default.PassApp = parts[1].Trim();
            else
                Properties.Settings.Default.UserPass = tb;

            Properties.Settings.Default.Save();
        }

        public static void LoadPrinters(ComboBox comboBox)
        {
            if (comboBox == null) return;

            comboBox.Items.Clear();

            // Lấy danh sách máy in đã cài
            foreach (string printerName in PrinterSettings.InstalledPrinters)
            {
                comboBox.Items.Add(printerName);
            }

            string savedPrinter = Properties.Settings.Default.PrinterName;
            string defaultPrinter = new PrinterSettings().PrinterName;

            if (!string.IsNullOrEmpty(savedPrinter) && comboBox.Items.Contains(savedPrinter))
            {
                comboBox.SelectedItem = savedPrinter;
                return;
            }

            if (!string.IsNullOrEmpty(defaultPrinter) && comboBox.Items.Contains(defaultPrinter))
            {
                // 2. Nếu không có, chọn máy in mặc định của Windows
                comboBox.SelectedItem = defaultPrinter;
                return;
            }

            if (comboBox.Items.Count > 0)
            {
                // 3. Cuối cùng chọn phần tử đầu tiên
                comboBox.SelectedIndex = 0;
                return;
            }

            FrmWaiting.ShowGifAlert("KHÔNG TÌM THẤY MÁY IN");
        }

        public static bool CheckLoginAndPermission(string k, string requiredPermission = "CAN_WRITE")
        {
            if (!UserContext.IsAuthenticated)
            {
                FrmWaiting.ShowGifAlert(EnumStore.ThongBao.YeuCauDangNhap);
                return false;
            }

            if (!UserContext.PermissionsDict.TryGetValue(k, out var perms)
                || perms == null
                || !perms.Contains(requiredPermission))
            {
                FrmWaiting.ShowGifAlert(EnumStore.ThongBao.YeuCauCapQuyen);
                return false;
            }

            return true;
        }


        public static void LoadUsersWithSameRoles(TreeView treeView)
        {
            if (!UserContext.IsAuthenticated)
            {
                MessageBox.Show("Người dùng chưa đăng nhập!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            treeView.Nodes.Clear();
            treeView.BeginUpdate();

            try
            {
                // Lấy danh sách users có chung role
                var users = DatabaseHelper.GetUsersWithSameRoles(UserContext.UserId);

                foreach (var user in users)
                {
                    // Tạo node cho user
                    var userNode = new TreeNode($"{user.Name} ({user.Username})")
                    {
                        Tag = user,
                        ImageIndex = 0, // Icon user
                        SelectedImageIndex = 0
                    };

                    // Thêm các role của user
                    foreach (var role in user.Roles)
                    {
                        var roleNode = new TreeNode(role.RoleName)
                        {
                            Tag = role,
                            ImageIndex = 1, // Icon role
                            SelectedImageIndex = 1,
                            ForeColor = System.Drawing.Color.Blue
                        };
                        userNode.Nodes.Add(roleNode);
                    }

                    treeView.Nodes.Add(userNode);
                }

                // Expand tất cả nodes
                treeView.ExpandAll();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi load dữ liệu: {ex.Message}", "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                treeView.EndUpdate();
            }
        }



    }


}
