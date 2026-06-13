using DG_TonKhoBTP_v02.Models.KeToan;
using System;
using System.Collections.Generic;
using System.Data.SQLite;

namespace DG_TonKhoBTP_v02.Database.KeToan
{
    public static class KiemTraDuLieuSXNhap_DB
    {
        public static List<KiemTraDuLieuSXNhap_Model> LayDanhSachKhacBietBomChuaXacNhan()
        {
            var result = new List<KiemTraDuLieuSXNhap_Model>();

            const string sql = @"
            SELECT
                kb.id                         AS id_KhacBietBom,
                kb.TTThanhpham_ID             AS ttthanhpham_id,
                tp.CongDoan                   AS CongDoanTP_ID,
                tp.MaBin                      AS lotTP,
                dspTP.Ma                      AS maTP,
                dspTP.Ten                     AS tenTP,
                kb.CongDoanThucTe             AS CongDoanTTe_ID,
                kb.TenBinNVL                  AS lotNVL,
                dspNVL.Ma                     AS maNVL,
                dspNVL.Ten                    AS tenNVL
            FROM KhacBietBOM kb
            LEFT JOIN TTThanhPham tp
                   ON kb.TTThanhpham_ID = tp.id
            LEFT JOIN DanhSachMaSP dspTP
                   ON tp.DanhSachSP_ID = dspTP.id
            LEFT JOIN DanhSachMaSP dspNVL
                   ON kb.DanhSachMaSP_ID = dspNVL.id
            WHERE IFNULL(kb.Confirmed, 0) = 0
            ORDER BY kb.id;";

            using (var conn = DB_Base.OpenConnection())
            using (var cmd = new SQLiteCommand(sql, conn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    int? congDoanTTeId = GetNullableInt(reader, "CongDoanTTe_ID");

                    result.Add(new KiemTraDuLieuSXNhap_Model
                    {
                        id_KhacBietBom = GetInt(reader, "id_KhacBietBom"),
                        ttthanhpham_id = GetInt(reader, "ttthanhpham_id"),

                        CongDoanTP_ID = GetNullableInt(reader, "CongDoanTP_ID"),
                        lotTP = GetString(reader, "lotTP"),
                        maTP = GetString(reader, "maTP"),
                        tenTP = GetString(reader, "tenTP"),

                        CongDoanTTe_ID = congDoanTTeId,
                        IsCongDoanTTe_NullFromDB = !congDoanTTeId.HasValue,

                        lotNVL = GetString(reader, "lotNVL"),
                        maNVL = GetString(reader, "maNVL"),
                        tenNVL = GetString(reader, "tenNVL"),
                        confirm = "Xác nhận"
                    });
                }
            }

            return result;
        }

        public static bool CapNhatConfirmedKhacBietBom(int idKhacBietBom, bool confirmed)
        {
            const string sql = @"
                UPDATE KhacBietBOM
                SET Confirmed = @Confirmed
                WHERE id = @id;";

            using (var conn = DB_Base.OpenConnection())
            using (var cmd = new SQLiteCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@Confirmed", confirmed ? 1 : 0);
                cmd.Parameters.AddWithValue("@id", idKhacBietBom);

                return cmd.ExecuteNonQuery() > 0;
            }
        }

        private static string GetString(SQLiteDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? string.Empty : Convert.ToString(reader.GetValue(ordinal));
        }

        private static int GetInt(SQLiteDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? 0 : Convert.ToInt32(reader.GetValue(ordinal));
        }

        private static int? GetNullableInt(SQLiteDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? (int?)null : Convert.ToInt32(reader.GetValue(ordinal));
        }
    }
}
