using DG_TonKhoBTP_v02.Models.KeToan;
using System;
using System.Data;
using System.Data.SQLite;

namespace DG_TonKhoBTP_v02.Database.KeToan
{
    public static class TTLo_DB
    {
        public static DataTable GetAll()
        {
            const string sql = @"
                SELECT 
                    id, 
                    KichThuoc, 
                    KhoiLuong, 
                    KhoiLuongCaNanPhu
                FROM TTLo
                ORDER BY id DESC";

            var dt = new DataTable();

            using (var conn = DB_Base.OpenConnection())
            using (var cmd = new SQLiteCommand(sql, conn))
            using (var adapter = new SQLiteDataAdapter(cmd))
            {
                adapter.Fill(dt);
            }

            return dt;
        }

        public static DataTable SearchByKichThuoc(string keyword)
        {
            const string sql = @"
                SELECT 
                    id, 
                    KichThuoc, 
                    KhoiLuong, 
                    KhoiLuongCaNanPhu
                FROM TTLo
                WHERE KichThuoc LIKE '%' || @keyword || '%' COLLATE NOCASE
                ORDER BY id DESC";

            var dt = new DataTable();

            using (var conn = DB_Base.OpenConnection())
            using (var cmd = new SQLiteCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@keyword", keyword ?? string.Empty);

                using (var adapter = new SQLiteDataAdapter(cmd))
                {
                    adapter.Fill(dt);
                }
            }

            return dt;
        }

        public static TTLo_Model GetById(int id)
        {
            const string sql = @"
                SELECT 
                    id, 
                    KichThuoc, 
                    KhoiLuong, 
                    KhoiLuongCaNanPhu
                FROM TTLo
                WHERE id = @id
                LIMIT 1";

            using (var conn = DB_Base.OpenConnection())
            using (var cmd = new SQLiteCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@id", id);

                using (var reader = cmd.ExecuteReader())
                {
                    if (!reader.Read())
                        return null;

                    return ReadModel(reader);
                }
            }
        }

        public static TTLo_Model GetByKichThuoc(string kichThuoc)
        {
            const string sql = @"
                SELECT 
                    id, 
                    KichThuoc, 
                    KhoiLuong, 
                    KhoiLuongCaNanPhu
                FROM TTLo
                WHERE KichThuoc = @kichThuoc
                LIMIT 1";

            using (var conn = DB_Base.OpenConnection())
            using (var cmd = new SQLiteCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@kichThuoc", kichThuoc ?? string.Empty);

                using (var reader = cmd.ExecuteReader())
                {
                    if (!reader.Read())
                        return null;

                    return ReadModel(reader);
                }
            }
        }

        public static int Insert(TTLo_Model model)
        {
            ValidateModel(model);

            const string insertSql = @"
                INSERT INTO TTLo 
                (
                    KichThuoc, 
                    KhoiLuong, 
                    KhoiLuongCaNanPhu
                )
                VALUES 
                (
                    @kichThuoc, 
                    @khoiLuong, 
                    @khoiLuongCaNanPhu
                )";

            using (var conn = DB_Base.OpenConnection())
            using (var transaction = conn.BeginTransaction())
            {
                using (var cmd = new SQLiteCommand(insertSql, conn, transaction))
                {
                    AddParameters(cmd, model, includeId: false);
                    cmd.ExecuteNonQuery();
                }

                using (var cmd = new SQLiteCommand("SELECT last_insert_rowid();", conn, transaction))
                {
                    object result = cmd.ExecuteScalar();
                    transaction.Commit();

                    return Convert.ToInt32(result);
                }
            }
        }

        public static int Update(TTLo_Model model)
        {
            ValidateModel(model);

            if (model.Id <= 0)
                throw new ArgumentException("Thiếu id ru lô cần cập nhật.");

            const string sql = @"
                UPDATE TTLo
                SET 
                    KichThuoc = @kichThuoc,
                    KhoiLuong = @khoiLuong,
                    KhoiLuongCaNanPhu = @khoiLuongCaNanPhu
                WHERE id = @id";

            using (var conn = DB_Base.OpenConnection())
            using (var cmd = new SQLiteCommand(sql, conn))
            {
                AddParameters(cmd, model, includeId: true);
                return cmd.ExecuteNonQuery();
            }
        }

        private static void ValidateModel(TTLo_Model model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            if (string.IsNullOrWhiteSpace(model.KichThuoc))
                throw new ArgumentException("Kích thước ru lô không được để trống.");
        }

        private static void AddParameters(SQLiteCommand cmd, TTLo_Model model, bool includeId)
        {
            if (includeId)
                cmd.Parameters.AddWithValue("@id", model.Id);

            cmd.Parameters.AddWithValue("@kichThuoc", model.KichThuoc.Trim());
            cmd.Parameters.AddWithValue("@khoiLuong", model.KhoiLuong);
            cmd.Parameters.AddWithValue("@khoiLuongCaNanPhu", model.KhoiLuongCaNanPhu);
        }

        private static TTLo_Model ReadModel(SQLiteDataReader reader)
        {
            return new TTLo_Model
            {
                Id = Convert.ToInt32(reader["id"]),
                KichThuoc = reader["KichThuoc"].ToString(),
                KhoiLuong = reader["KhoiLuong"] == DBNull.Value ? 0 : Convert.ToDouble(reader["KhoiLuong"]),
                KhoiLuongCaNanPhu = reader["KhoiLuongCaNanPhu"] == DBNull.Value ? 0 : Convert.ToDouble(reader["KhoiLuongCaNanPhu"])
            };
        }
    }
}