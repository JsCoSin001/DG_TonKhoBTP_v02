using DG_TonKhoBTP_v02.Models.SanXuat;
using System;
using System.Collections.Generic;
using System.Data.SQLite;

namespace DG_TonKhoBTP_v02.Database.SanXuat
{
    internal static class LoiDungMay_DB
    {
        /// <summary>
        /// Lấy các công đoạn có ít nhất một máy.
        /// Thứ tự hiển thị bám theo id đã lưu trong database.
        /// </summary>
        public static List<DanhSachCongDoan_Model> GetDanhSachCongDoanCoMay()
        {
            const string sql = @"
                SELECT
                    cd.id,
                    cd.MaCongDoan,
                    cd.TenCongDoan
                FROM DanhSachCongDoan cd
                WHERE EXISTS
                (
                    SELECT 1
                    FROM DanhSachMay m
                    WHERE m.DanhSachCongDoan_MaCongDoan = cd.MaCongDoan
                )
                ORDER BY cd.id ASC;";

            var result = new List<DanhSachCongDoan_Model>();

            using (SQLiteConnection conn = DB_Base.OpenConnection())
            using (SQLiteCommand cmd = new SQLiteCommand(sql, conn))
            using (SQLiteDataReader reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    result.Add(new DanhSachCongDoan_Model
                    {
                        Id = Convert.ToInt32(reader["id"]),
                        MaCongDoan = Convert.ToInt32(reader["MaCongDoan"]),
                        TenCongDoan = Convert.ToString(reader["TenCongDoan"]) ?? string.Empty
                    });
                }
            }

            return result;
        }

        /// <summary>
        /// Lấy danh sách máy theo MaCongDoan, sắp xếp theo id đã lưu trong database.
        /// </summary>
        public static List<DanhSachMay_Model> GetDanhSachMayTheoMaCongDoan(int maCongDoan)
        {
            const string sql = @"
                SELECT
                    id,
                    DanhSachCongDoan_MaCongDoan,
                    TenMay
                FROM DanhSachMay
                WHERE DanhSachCongDoan_MaCongDoan = @MaCongDoan
                ORDER BY id ASC;";

            var result = new List<DanhSachMay_Model>();

            using (SQLiteConnection conn = DB_Base.OpenConnection())
            using (SQLiteCommand cmd = new SQLiteCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@MaCongDoan", maCongDoan);

                using (SQLiteDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new DanhSachMay_Model
                        {
                            Id = Convert.ToInt32(reader["id"]),
                            MaCongDoan = Convert.ToInt32(reader["DanhSachCongDoan_MaCongDoan"]),
                            TenMay = Convert.ToString(reader["TenMay"]) ?? string.Empty
                        });
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Lấy danh sách lỗi dừng máy theo MaCongDoan,
        /// sắp xếp theo id đã lưu trong database.
        /// </summary>
        public static List<TenLoiDungMay_Model> GetDanhSachTenLoiTheoMaCongDoan(int maCongDoan)
        {
            const string sql = @"
                SELECT
                    id,
                    TenLoi,
                    MoTaLoi,
                    DanhSachCongDoan_MaCongDoan
                FROM TenLoiDungMay
                WHERE DanhSachCongDoan_MaCongDoan = @MaCongDoan
                ORDER BY id ASC;";

            var result = new List<TenLoiDungMay_Model>();

            using (SQLiteConnection conn = DB_Base.OpenConnection())
            using (SQLiteCommand cmd = new SQLiteCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@MaCongDoan", maCongDoan);

                using (SQLiteDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new TenLoiDungMay_Model
                        {
                            Id = Convert.ToInt32(reader["id"]),
                            TenLoi = Convert.ToString(reader["TenLoi"]) ?? string.Empty,
                            MoTaLoi = Convert.ToString(reader["MoTaLoi"]) ?? string.Empty,
                            MaCongDoan = Convert.ToInt32(reader["DanhSachCongDoan_MaCongDoan"])
                        });
                    }
                }
            }

            return result;
        }
    }
}
