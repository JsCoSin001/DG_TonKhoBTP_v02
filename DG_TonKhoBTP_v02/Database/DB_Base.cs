using System.Data;
using System.Data.SQLite;

namespace DG_TonKhoBTP_v02.Database
{
    /// <summary>
    /// File này chứa cấu hình kết nối SQLite dùng chung cho toàn bộ project.
    /// Mọi file Database khác đều dùng OpenConnection() từ file này.
    /// </summary>
    public static class DB_Base
    {
        public static string _connStr;

        /// <summary>
        /// Chuyển đường dẫn file sang dạng phù hợp với parser connection string của System.Data.SQLite.
        /// Với UNC path (\\server\share\file.db), cặp backslash đầu phải được escape thành bốn backslash
        /// khi đặt trong chuỗi Data Source. Đường dẫn ổ đĩa cục bộ/mapped drive được giữ nguyên.
        /// </summary>
        internal static string ToSQLiteDataSource(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return path;

            return path.StartsWith(@"\\", System.StringComparison.Ordinal)
                ? @"\\" + path
                : path;
        }

        /// <summary>Thiết lập đường dẫn đến file SQLite.</summary>
        public static void SetDatabasePath(string path)
        {
            string dataSource = ToSQLiteDataSource(path);
            _connStr = $"Data Source={dataSource};Version=3;";
        }

        public static string GetStringConnector => _connStr;

        /// <summary>
        /// Mở connection và bật PRAGMA foreign_keys = ON.
        /// Tất cả các hàm trong project phải dùng method này thay vì
        /// new SQLiteConnection(...) + conn.Open() thủ công.
        /// </summary>
        public static SQLiteConnection OpenConnection()
        {
            var conn = new SQLiteConnection(_connStr);
            conn.Open();
            using (var cmd = new SQLiteCommand("PRAGMA foreign_keys = ON;", conn))
                cmd.ExecuteNonQuery();
            return conn;
        }

        /// <summary>Async version của OpenConnection().</summary>
        public static async System.Threading.Tasks.Task<SQLiteConnection> OpenConnectionAsync()
        {
            var conn = new SQLiteConnection(_connStr);
            await conn.OpenAsync();
            using (var cmd = new SQLiteCommand("PRAGMA foreign_keys = ON;", conn))
                await cmd.ExecuteNonQueryAsync();
            return conn;
        }

        // ── Helper nội bộ dùng trong nhiều file ─────────────────────────────

        /// <summary>
        /// Thực thi SQL trả về DataTable.
        /// Dùng cho các query không có tham số hoặc chỉ có 1 tham số đơn giản.
        /// </summary>
        public static System.Data.DataTable GetData(string sql, string paramValue = null, string paramName = null)
        {
            var dt = new System.Data.DataTable();

            using var conn = OpenConnection();
            using var cmd = new SQLiteCommand(sql, conn);

            if (paramValue != null && paramName != null)
                cmd.Parameters.AddWithValue("@" + paramName, paramValue);

            using var adapter = new SQLiteDataAdapter(cmd);
            adapter.Fill(dt);

            return dt;
        }

        public static int GetSoLuongXuatNhapThangHienTai(bool isNhap)
        {
            var now = System.DateTime.Now;
            string start = new System.DateTime(now.Year, now.Month, 1).ToString("yyyy-MM-dd");
            string end = new System.DateTime(now.Year, now.Month, 1).AddMonths(1).ToString("yyyy-MM-dd");
            string prefix = isNhap ? "KNK" : "KXK";

            const string sql = @"
                SELECT COUNT(*)
                FROM LichSuXuatNhap
                WHERE Ngay >= @start AND Ngay < @end
                  AND TenPhieu LIKE @prefix";

            using var conn = OpenConnection();
            using var cmd = new SQLiteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@start", start);
            cmd.Parameters.AddWithValue("@end", end);
            cmd.Parameters.AddWithValue("@prefix", prefix + "%");

            var result = cmd.ExecuteScalar();
            return result == null ? 0 : System.Convert.ToInt32(result);
        }


        

    }
}