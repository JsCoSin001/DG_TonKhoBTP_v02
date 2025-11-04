using DG_TonKhoBTP_v02.Database;
using DG_TonKhoBTP_v02.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace DG_TonKhoBTP_v02.Helper
{
    public static class ReportHelper
    {
        // Các bảng core luôn join
        private static readonly HashSet<string> CoreTables = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "TTThanhPham", "DanhSachMaSP", "TTNVL" };

        // Build mặc định join qua TTThanhPham
        private static string BuildJoinClause(string tableName)
        {
            // ⛔️ Không join alias DSNVL ở đây (DSNVL là alias của DanhSachMaSP được join thủ công)
            if (tableName.Equals("DSNVL", StringComparison.OrdinalIgnoreCase))
                return string.Empty;

            if (tableName.Equals("DanhSachMaSP", StringComparison.OrdinalIgnoreCase))
                return "JOIN DanhSachMaSP ON TTThanhPham.DanhSachSP_ID = DanhSachMaSP.id";

            if (tableName.Equals("TTNVL", StringComparison.OrdinalIgnoreCase))
                return "LEFT JOIN TTNVL ON TTNVL.TTThanhPham_ID = TTThanhPham.id";

            return $"LEFT JOIN {tableName} ON {tableName}.TTThanhPham_ID = TTThanhPham.id";
        }

        // Lấy danh sách bảng từ ListData_Report (dạng Table.Column)
        private static HashSet<string> GetTablesFromList(List<string> listDataReport)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (listDataReport != null)
            {
                foreach (var item in listDataReport)
                {
                    var parts = item.Split(new[] { '.' }, 2);
                    if (parts.Length == 2) set.Add(parts[0].Trim());
                }
            }

            // Bảo đảm 3 bảng core luôn có
            set.Add("TTThanhPham");
            set.Add("DanhSachMaSP");
            set.Add("TTNVL");

            // ❗️Không để alias DSNVL rơi vào danh sách join tự động
            set.RemoveWhere(t => t.Equals("DSNVL", StringComparison.OrdinalIgnoreCase));

            return set;
        }

        // WHERE theo ListMa_Accept (DanhSachMaSP.Ma LIKE ...)
        private static string BuildAcceptWhere(List<string> accepts, List<SQLiteParameter> parameters)
        {
            if (accepts == null || accepts.Count == 0) return string.Empty;

            var likeClauses = new List<string>();
            for (int i = 0; i < accepts.Count; i++)
            {
                var p = $"@acc{i}";
                likeClauses.Add($"DanhSachMaSP.Ma LIKE {p}");
                parameters.Add(new SQLiteParameter(p, accepts[i]));
            }
            return $"WHERE ({string.Join(" OR ", likeClauses)})";
        }

        // Tạo SELECT cho 1 công đoạn
        private static string BuildSqlForCongDoan(CongDoan cd, List<SQLiteParameter> parametersOut)
        {
            var tables = GetTablesFromList(cd.ListData_Report);

            var selectItems = new List<string>
        {
            "TTThanhPham.id AS \"id\"",
            "DanhSachMaSP.Ma AS \"MaTP\"",
            "DanhSachMaSP.Ten AS \"TenTP\""
        };

            // Thêm các cột user yêu cầu (giữ nguyên cú pháp đầu vào)
            if (cd.ListData_Report != null)
            {
                foreach (var col in cd.ListData_Report.Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    // Tránh add trùng literal hoàn toàn
                    if (!selectItems.Any(s => s.Equals(col, StringComparison.OrdinalIgnoreCase)))
                        selectItems.Add(col);
                }
            }


            // gắn nhãn công đoạn để phân biệt khi gộp
            selectItems.Add($"'{cd.TenCongDoan}' AS \"TenCongDoan\"");

            var sb = new StringBuilder();
            sb.AppendLine("SELECT");
            sb.AppendLine("  " + string.Join(",\n  ", selectItems));
            sb.AppendLine("FROM TTThanhPham");
            sb.AppendLine(BuildJoinClause("DanhSachMaSP"));
            sb.AppendLine(BuildJoinClause("TTNVL"));

            // ✅ Join alias DSNVL (bảng mã NVL) đúng chuẩn – chỉ join 1 lần duy nhất
            sb.AppendLine("LEFT JOIN DanhSachMaSP AS DSNVL ON TTNVL.DanhSachMaSP_ID = DSNVL.id");

            // Join các bảng còn lại theo ListData_Report (ngoại trừ core và DSNVL)
            foreach (var t in tables.Where(t => !CoreTables.Contains(t)))
                sb.AppendLine(BuildJoinClause(t));

            var where = BuildAcceptWhere(cd.ListMa_Accept, parametersOut);
            if (!string.IsNullOrWhiteSpace(where)) sb.AppendLine(where);

            sb.AppendLine("ORDER BY TTThanhPham.id DESC");
            return sb.ToString();
        }

        // Gộp DataTable part vào master
        private static void AppendTable(DataTable master, DataTable part)
        {
            foreach (DataColumn c in part.Columns)
                if (!master.Columns.Contains(c.ColumnName))
                    master.Columns.Add(c.ColumnName, c.DataType);

            foreach (DataRow r in part.Rows)
            {
                var newRow = master.NewRow();
                foreach (DataColumn c in part.Columns)
                    newRow[c.ColumnName] = r[c.ColumnName];
                master.Rows.Add(newRow);
            }
        }

        // ✅ Overload nhận List<CongDoan>
        public static DataTable GetDataReport(List<CongDoan> listCongDoan)
            => GetDataReport(listCongDoan?.ToArray());

        // Hàm chính nhận mảng
        public static DataTable GetDataReport(CongDoan[] arrCongDoan)
        {
            DataTable master = new DataTable("Report");

            var sqliteConnectionString = DatabaseHelper.GetStringConnector;
            if (string.IsNullOrWhiteSpace(sqliteConnectionString))
                throw new InvalidOperationException("Connection string rỗng. Hãy gọi SetDatabasePath() trước.");            


            using (var conn = new SQLiteConnection(sqliteConnectionString))
            {
                conn.Open();

                foreach (var cd in arrCongDoan)
                {
                    var parameters = new List<SQLiteParameter>();
                    var sql = BuildSqlForCongDoan(cd, parameters);

                    using var cmd = new SQLiteCommand(sql, conn);
                    if (parameters.Count > 0) cmd.Parameters.AddRange(parameters.ToArray());

                    using var da = new SQLiteDataAdapter(cmd);
                    var temp = new DataTable();
                    da.Fill(temp);

                    if (master.Columns.Count == 0) master = temp;
                    else AppendTable(master, temp);
                }
            }

            return master;
        }
    }







}