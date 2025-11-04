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


        public static string TaoSql_LayThongTinBaoCaoToanBo()
        {
            return @"
                    SELECT
                    ttp.id                          AS ttp_id,
                    ttp.DanhSachSP_ID               AS ttp_DanhSachSP_ID,
                    ttp.ThongTinCaLamViec_ID        AS ttp_ThongTinCaLamViec_ID,
                    ttp.MaBin                       AS ttp_MaBin,
                    ttp.KhoiLuongTruoc              AS ttp_KhoiLuongTruoc,
                    ttp.KhoiLuongSau                AS ttp_KhoiLuongSau,
                    ttp.ChieuDaiTruoc               AS ttp_ChieuDaiTruoc,
                    ttp.ChieuDaiSau                 AS ttp_ChieuDaiSau,
                    ttp.Phe                         AS ttp_Phe,
                    ttp.CongDoan                    AS ttp_CongDoan,
                    ttp.GhiChu                      AS ttp_GhiChu,
                    ttp.LastEdit_ID                 AS ttp_LastEdit_ID,
                    ttp.DateInsert                  AS ttp_DateInsert,

                    tclv.id                         AS tclv_id,
                    tclv.Ngay                       AS tclv_Ngay,
                    tclv.May                        AS tclv_May,
                    tclv.Ca                         AS tclv_Ca,
                    tclv.NguoiLam                   AS tclv_NguoiLam,
                    tclv.ToTruong                   AS tclv_ToTruong,
                    tclv.QuanDoc                    AS tclv_QuanDoc,

                    ds.id                           AS ds_id,
                    ds.Ten                          AS ds_Ten,
                    ds.Ma                           AS ds_Ma,
                    ds.DonVi                        AS ds_DonVi,
                    ds.KieuSP                       AS ds_KieuSP,
                    ds.DateInsert                   AS ds_DateInsert,

                    cbv.id                          AS cbv_id,
                    cbv.TTThanhPham_ID              AS cbv_TTThanhPham_ID,
                    cbv.DayVoTB                     AS cbv_DayVoTB,
                    cbv.InAn                        AS cbv_InAn,

                    cbl.id                          AS cbl_id,
                    cbl.TTThanhPham_ID              AS cbl_TTThanhPham_ID,
                    cbl.DoDayTBLot                  AS cbl_DoDayTBLot,

                    cbm.id                          AS cbm_id,
                    cbm.TTThanhPham_ID              AS cbm_TTThanhPham_ID,
                    cbm.NgoaiQuan                   AS cbm_NgoaiQuan,
                    cbm.LanDanhThung               AS cbm_LanDanhThung,
                    cbm.SoMet                       AS cbm_SoMet,

                    ckr.id                          AS ckr_id,
                    ckr.TTThanhPham_ID              AS ckr_TTThanhPham_ID,
                    ckr.DKTrucX                     AS ckr_DKTrucX,
                    ckr.DKTrucY                     AS ckr_DKTrucY,
                    ckr.NgoaiQuan                   AS ckr_NgoaiQuan,
                    ckr.TocDo                       AS ckr_TocDo,
                    ckr.DienApU                     AS ckr_DienApU,
                    ckr.DongDienU                   AS ckr_DongDienU,

                    cbr.id                          AS cbr_id,
                    cbr.TTThanhPham_ID              AS cbr_TTThanhPham_ID,
                    cbr.DKSoi                       AS cbr_DKSoi,
                    cbr.SoSoi                       AS cbr_SoSoi,
                    cbr.ChieuXoan                   AS cbr_ChieuXoan,
                    cbr.BuocBen                     AS cbr_BuocBen,

                    cgl.id                          AS cgl_id,
                    cgl.TTThanhPham_ID              AS cgl_TTThanhPham_ID,
                    cgl.BuocXoan                    AS cgl_BuocXoan,
                    cgl.ChieuXoan                   AS cgl_ChieuXoan,
                    cgl.GoiCachMep                  AS cgl_GoiCachMep,
                    cgl.DKBTP                       AS cgl_DKBTP,

                    cdb.id                          AS cdb_id,
                    cdb.TTThanhPham_ID              AS cdb_TTThanhPham_ID,
                    cdb.MangNuoc                    AS cdb_MangNuoc,
                    cdb.PuliDanDay                  AS cdb_PuliDanDay,
                    cdb.BoDemMet                    AS cdb_BoDemMet,
                    cdb.MayIn                       AS cdb_MayIn,
                    cdb.v1                          AS cdb_v1,
                    cdb.v2                          AS cdb_v2,
                    cdb.v3                          AS cdb_v3,
                    cdb.v4                          AS cdb_v4,
                    cdb.v5                          AS cdb_v5,
                    cdb.v6                          AS cdb_v6,
                    cdb.Co                          AS cdb_Co,
                    cdb.Dau1                        AS cdb_Dau1,
                    cdb.Dau2                        AS cdb_Dau2,
                    cdb.Khuon                       AS cdb_Khuon,
                    cdb.BinhSay                     AS cdb_BinhSay,
                    cdb.DKKhuon1                    AS cdb_DKKhuon1,
                    cdb.DKKhuon2                    AS cdb_DKKhuon2,
                    cdb.TTNhua                      AS cdb_TTNhua,
                    cdb.NhuaPhe                     AS cdb_NhuaPhe,
                    cdb.GhiChuNhuaPhe               AS cdb_GhiChuNhuaPhe,
                    cdb.DayPhe                      AS cdb_DayPhe,
                    cdb.GhiChuDayPhe                AS cdb_GhiChuDayPhe,
                    cdb.KTDKLan1                    AS cdb_KTDKLan1,
                    cdb.KTDKLan2                    AS cdb_KTDKLan2,
                    cdb.KTDKLan3                    AS cdb_KTDKLan3,
                    cdb.DiemMongLan1                AS cdb_DiemMongLan1,
                    cdb.DiemMongLan2                AS cdb_DiemMongLan2,

                    nvl.id                          AS id,
                    nvl.TTThanhPham_ID              AS nvl_TTThanhPham_ID,
                    nvl.BinNVL                      AS BinNVL,
                    nvl.KlBatDau                    AS KlBatDau,
                    nvl.CdBatDau                    AS CdBatDau,
                    nvl.KlConLai                    AS KlConLai,
                    nvl.CdConLai                    AS CdConLai,
                    nvl.DuongKinhSoiDong            AS DuongKinhSoiDong,
                    nvl.SoSoi                       AS SoSoi,
                    nvl.KetCauLoi                   AS KetCauLoi,
                    nvl.DuongKinhSoiMach            AS DuongKinhSoiMach,
                    nvl.BanRongBang                 AS BanRongBang,
                    nvl.DoDayBang                   AS DoDayBang,
                ";
        }

        public static string TaoSQL_TaoKetNoiCacBang()
        {
            return @"
                FROM TTThanhPham ttp
                JOIN ThongTinCaLamViec tclv ON tclv.id = ttp.ThongTinCaLamViec_ID
                JOIN DanhSachMaSP ds        ON ds.id   = ttp.DanhSachSP_ID
                LEFT JOIN CD_BocVo     cbv  ON cbv.TTThanhPham_ID   = ttp.id
                LEFT JOIN CD_BocLot    cbl  ON cbl.TTThanhPham_ID   = ttp.id
                LEFT JOIN CD_BocMach   cbm  ON cbm.TTThanhPham_ID   = ttp.id
                LEFT JOIN CD_KeoRut    ckr  ON ckr.TTThanhPham_ID   = ttp.id
                LEFT JOIN CD_BenRuot   cbr  ON cbr.TTThanhPham_ID   = ttp.id
                LEFT JOIN CD_GhepLoiQB cgl  ON cgl.TTThanhPham_ID   = ttp.id
                LEFT JOIN CaiDatCDBoc  cdb  ON cdb.TTThanhPham_ID   = ttp.id
                LEFT JOIN TTNVL        nvl  ON nvl.TTThanhPham_ID   = ttp.id
                LEFT JOIN DanhSachMaSP ds_nvl ON ds_nvl.id          = nvl.DanhSachMaSP_ID
            ";
        }


        public static string TaoSQL_LayChiTiet_1CD(int id)
        {
            string[] dsCotCongDoan = ChiTietCongDoan.DSTenCot;

            id = id < 6 ? id : 2;
            return dsCotCongDoan[id];
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
            if (ex is SQLiteException sqliteEx)
            {
                switch (sqliteEx.ErrorCode)
                {
                    case (int)SQLiteErrorCode.Constraint:
                        return $"{ten ?? "DỮ LIỆU"} ĐÃ TỒN TẠI.";
                    case (int)SQLiteErrorCode.Busy:
                        return "CƠ SỞ DỮ LIỆU ĐANG BẬN, HÃY THỬ LẠI LẦN NỮA.";
                    default:
                        return $"Lỗi cơ sở dữ liệu: {sqliteEx.Message}";
                }
            }
            else if (ex is InvalidCastException)
            {
                return "Kiểu dữ liệu không khớp, vui lòng kiểm tra thông tin công đoạn.";
            }
            else if (ex is ArgumentException)
            {
                return ex.Message; // ví dụ: "Chi tiết công đoạn không hợp lệ."
            }
            else if (ex is NullReferenceException)
            {
                return "Một dữ liệu cần thiết chưa được khởi tạo. Vui lòng kiểm tra lại biểu mẫu nhập.";
            }
            else
            {
                return $"Lỗi không xác định: {ex.Message}";
            }
        }

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
