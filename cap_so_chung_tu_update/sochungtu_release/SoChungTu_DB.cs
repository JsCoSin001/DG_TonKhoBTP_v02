using System;
using System.Data.SQLite;

namespace DG_TonKhoBTP_v02.Database
{
    internal static class SoChungTu_DB
    {
        private const int BusyTimeoutMilliseconds = 10000;

        public static void ConfigureBusyTimeout(SQLiteConnection connection)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            using (var cmd = new SQLiteCommand($"PRAGMA busy_timeout = {BusyTimeoutMilliseconds};", connection))
                cmd.ExecuteNonQuery();
        }

        public static int GetSoDuKien(string tienTo, DateTime ngay)
        {
            string normalizedPrefix = NormalizePrefix(tienTo);

            const string sql = @"
                SELECT IFNULL(SoCuoi, 0) + 1
                FROM SoChungTu
                WHERE TienTo = @tienTo
                  AND Nam = @nam
                  AND Thang = @thang;
            ";

            using (var conn = DB_Base.OpenConnection())
            {
                ConfigureBusyTimeout(conn);

                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@tienTo", normalizedPrefix);
                    cmd.Parameters.AddWithValue("@nam", ngay.Year);
                    cmd.Parameters.AddWithValue("@thang", ngay.Month);

                    object result = cmd.ExecuteScalar();
                    return result == null || result == DBNull.Value
                        ? 1
                        : Convert.ToInt32(result);
                }
            }
        }

        public static int GetSoCuoi(string tienTo, DateTime ngay)
        {
            return Math.Max(0, GetSoDuKien(tienTo, ngay) - 1);
        }

        public static string GetMaDuKien(string tienTo, DateTime ngay)
        {
            return FormatMaChungTu(tienTo, ngay, GetSoDuKien(tienTo, ngay));
        }

        public static int CapSoTiepTheo(
            SQLiteConnection connection,
            SQLiteTransaction transaction,
            string tienTo,
            DateTime ngay)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));
            if (transaction == null)
                throw new ArgumentNullException(nameof(transaction));

            string normalizedPrefix = NormalizePrefix(tienTo);

            const string sqlInsert = @"
                INSERT OR IGNORE INTO SoChungTu
                (
                    TienTo,
                    Nam,
                    Thang,
                    SoCuoi,
                    NgayCapNhat
                )
                VALUES
                (
                    @tienTo,
                    @nam,
                    @thang,
                    0,
                    strftime('%Y-%m-%d %H:%M:%S', 'now', 'localtime')
                );
            ";

            const string sqlUpdate = @"
                UPDATE SoChungTu
                SET SoCuoi = SoCuoi + 1,
                    NgayCapNhat = strftime('%Y-%m-%d %H:%M:%S', 'now', 'localtime')
                WHERE TienTo = @tienTo
                  AND Nam = @nam
                  AND Thang = @thang;
            ";

            const string sqlSelect = @"
                SELECT SoCuoi
                FROM SoChungTu
                WHERE TienTo = @tienTo
                  AND Nam = @nam
                  AND Thang = @thang;
            ";

            using (var cmdInsert = new SQLiteCommand(sqlInsert, connection, transaction))
            {
                AddKeyParameters(cmdInsert, normalizedPrefix, ngay);
                cmdInsert.ExecuteNonQuery();
            }

            using (var cmdUpdate = new SQLiteCommand(sqlUpdate, connection, transaction))
            {
                AddKeyParameters(cmdUpdate, normalizedPrefix, ngay);
                if (cmdUpdate.ExecuteNonQuery() != 1)
                    throw new InvalidOperationException("Không thể cập nhật bộ đếm chứng từ.");
            }

            using (var cmdSelect = new SQLiteCommand(sqlSelect, connection, transaction))
            {
                AddKeyParameters(cmdSelect, normalizedPrefix, ngay);
                object result = cmdSelect.ExecuteScalar();
                if (result == null || result == DBNull.Value)
                    throw new InvalidOperationException("Không đọc được số chứng từ vừa cấp.");

                return Convert.ToInt32(result);
            }
        }

        public static string CapMaChungTu(
            SQLiteConnection connection,
            SQLiteTransaction transaction,
            string tienTo,
            DateTime ngay)
        {
            int soThuTu = CapSoTiepTheo(connection, transaction, tienTo, ngay);
            return FormatMaChungTu(tienTo, ngay, soThuTu);
        }

        public static string FormatMaChungTu(string tienTo, DateTime ngay, int soThuTu)
        {
            if (soThuTu <= 0)
                throw new ArgumentOutOfRangeException(nameof(soThuTu), "Số thứ tự phải lớn hơn 0.");

            string normalizedPrefix = NormalizePrefix(tienTo);
            return $"{normalizedPrefix}{ngay:yy/MM}-{soThuTu:D4}";
        }

        private static void AddKeyParameters(SQLiteCommand cmd, string tienTo, DateTime ngay)
        {
            cmd.Parameters.AddWithValue("@tienTo", tienTo);
            cmd.Parameters.AddWithValue("@nam", ngay.Year);
            cmd.Parameters.AddWithValue("@thang", ngay.Month);
        }

        private static string NormalizePrefix(string tienTo)
        {
            string result = tienTo?.Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(result))
                throw new ArgumentException("Tiền tố chứng từ không được để trống.", nameof(tienTo));

            return result;
        }
    }
}
