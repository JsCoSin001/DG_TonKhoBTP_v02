using System;
using System.Data.SQLite;
using System.Globalization;

namespace DG_TonKhoBTP_v02.Database.KeToan.VatTuKhac
{
    internal static class ConfigApp_DB
    {
        private const string TEN_NGAY_KHOA_VAT_TU = "NgayKhoa_VatTu";
        private const string DATE_FORMAT = "yyyyMMdd";

        public static DateTime? GetNgayKhoaVatTu()
        {
            const string sql = @"
            SELECT GiaTri
            FROM ConfigApp
            WHERE Ten = @ten
            LIMIT 1;";

            using (var conn = new SQLiteConnection(DatabaseHelper.GetStringConnector))
            using (var cmd = new SQLiteCommand(sql, conn))
            {
                conn.Open();
                cmd.Parameters.AddWithValue("@ten", TEN_NGAY_KHOA_VAT_TU);

                using (SQLiteDataReader reader = cmd.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        throw new InvalidOperationException(
                            $"Không tìm thấy cấu hình '{TEN_NGAY_KHOA_VAT_TU}' trong bảng ConfigApp.");
                    }

                    if (reader.IsDBNull(0))
                        return null;

                    string rawValue = Convert.ToString(reader.GetValue(0));
                    if (string.IsNullOrWhiteSpace(rawValue))
                    {
                        throw new FormatException(
                            $"Giá trị cấu hình '{TEN_NGAY_KHOA_VAT_TU}' không hợp lệ. " +
                            $"Giá trị phải là NULL hoặc ngày theo định dạng {DATE_FORMAT}.");
                    }

                    if (!DateTime.TryParseExact(
                            rawValue.Trim(),
                            DATE_FORMAT,
                            CultureInfo.InvariantCulture,
                            DateTimeStyles.None,
                            out DateTime ngayKhoa))
                    {
                        throw new FormatException(
                            $"Giá trị cấu hình '{TEN_NGAY_KHOA_VAT_TU}' không hợp lệ: '{rawValue}'. " +
                            $"Định dạng yêu cầu: {DATE_FORMAT}.");
                    }

                    return ngayKhoa.Date;
                }
            }
        }

        public static void SetNgayKhoaVatTu(DateTime? ngayKhoa)
        {
            const string sql = @"
                UPDATE ConfigApp
                SET GiaTri = @giaTri
                WHERE Ten = @ten;";

            using (var conn = new SQLiteConnection(DatabaseHelper.GetStringConnector))
            using (var cmd = new SQLiteCommand(sql, conn))
            {
                conn.Open();
                cmd.Parameters.AddWithValue("@ten", TEN_NGAY_KHOA_VAT_TU);
                cmd.Parameters.AddWithValue(
                    "@giaTri",
                    ngayKhoa.HasValue
                        ? (object)ngayKhoa.Value.Date.ToString(DATE_FORMAT, CultureInfo.InvariantCulture)
                        : DBNull.Value);

                int affected = cmd.ExecuteNonQuery();
                if (affected != 1)
                {
                    throw new InvalidOperationException(
                        $"Không thể cập nhật cấu hình '{TEN_NGAY_KHOA_VAT_TU}'. " +
                        "Vui lòng kiểm tra dữ liệu ConfigApp.");
                }
            }
        }
    }
}
