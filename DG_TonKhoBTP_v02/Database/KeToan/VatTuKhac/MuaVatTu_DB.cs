using DG_TonKhoBTP_v02.Models.KeToan.VatTuKhac;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;

namespace DG_TonKhoBTP_v02.Database.KeToan.VatTuKhac
{
    internal static class MuaVatTu_DB
    {
        private const string DateFormat = "yyyy-MM-dd";
        private const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss";

        public static int CountDanhSachDatHangInMonth(DateTime now)
        {
            DateTime start = new DateTime(now.Year, now.Month, 1);
            DateTime end = start.AddMonths(1);

            const string sql = @"
                SELECT COUNT(*)
                FROM DanhSachDatHang
                WHERE NgayThem >= @start
                  AND NgayThem < @end";

            using var conn = DB_Base.OpenConnection();
            using var cmd = new SQLiteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@start", start.ToString(DateFormat));
            cmd.Parameters.AddWithValue("@end", end.ToString(DateFormat));

            object result = cmd.ExecuteScalar();
            return result == null || result == DBNull.Value ? 0 : Convert.ToInt32(result);
        }

        public static bool MaDonExists(string maDon)
        {
            const string sql = @"
                SELECT COUNT(1)
                FROM DanhSachDatHang
                WHERE MaDon = @maDon";

            using var conn = DB_Base.OpenConnection();
            using var cmd = new SQLiteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@maDon", maDon);
            return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        }

        public static DataTable SearchDanhSachMaSP(string keyword, string keywordKhongDau)
        {
            const string sql = @"
                SELECT id,
                       Ten AS ten,
                       Ten_KhongDau AS ten_khongdau,
                       Ma AS ma,
                       DonVi AS donvi
                FROM DanhSachMaSP
                WHERE Active = 1
                  AND KieuSP NOT IN ('NVL', 'TP', 'BTP')
                  AND (
                        LOWER(COALESCE(Ten_KhongDau, '')) LIKE @keywordKhongDau
                        OR LOWER(COALESCE(Ma, '')) LIKE @keyword
                      )
                ORDER BY Ten
                LIMIT 50";

            using var conn = DB_Base.OpenConnection();
            using var cmd = new SQLiteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@keyword", Like(keyword));
            cmd.Parameters.AddWithValue("@keywordKhongDau", Like(keywordKhongDau));

            var dt = new DataTable();
            using var adapter = new SQLiteDataAdapter(cmd);
            adapter.Fill(dt);
            return dt;
        }

        public static DataTable SearchDonDatHang(string keyword, string keywordKhongDau, int loaiDon, string nguoiDat)
        {
            const string sql = @"
                SELECT DISTINCT
                       'MADON' AS search_type,
                       ddh.id AS danh_sach_dat_hang_id,
                       ddh.MaDon AS ma_don,
                       ddh.MaDon AS display_text
                FROM DanhSachDatHang ddh
                WHERE ddh.LoaiDon = @loaiDon
                  AND COALESCE(ddh.NguoiDat, '') = COALESCE(@nguoiDat, '')
                  AND LOWER(COALESCE(ddh.MaDon, '')) LIKE @keyword

                UNION

                SELECT DISTINCT
                       'TEN_VAT_TU' AS search_type,
                       ddh.id AS danh_sach_dat_hang_id,
                       ddh.MaDon AS ma_don,
                       ddh.MaDon || ' - ' || COALESCE(dsm.Ten, ttdh.TenVatTu, '') AS display_text
                FROM DanhSachDatHang ddh
                JOIN ThongTinDatHang ttdh ON ttdh.DanhSachDatHang_ID = ddh.id
                LEFT JOIN DanhSachMaSP dsm ON dsm.id = ttdh.DanhSachMaSP_ID
                WHERE ddh.LoaiDon = @loaiDon
                  AND COALESCE(ddh.NguoiDat, '') = COALESCE(@nguoiDat, '')
                  AND (
                        (@loaiDon = 1 AND LOWER(COALESCE(dsm.Ten_KhongDau, '')) LIKE @keywordKhongDau)
                        OR
                        (@loaiDon = 2 AND LOWER(COALESCE(ttdh.TenVatTu_KhongDau, '')) LIKE @keywordKhongDau)
                      )
                ORDER BY display_text
                LIMIT 80";

            using var conn = DB_Base.OpenConnection();
            using var cmd = new SQLiteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@keyword", Like(keyword));
            cmd.Parameters.AddWithValue("@keywordKhongDau", Like(keywordKhongDau));
            cmd.Parameters.AddWithValue("@loaiDon", loaiDon);
            cmd.Parameters.AddWithValue("@nguoiDat", (object)nguoiDat ?? DBNull.Value);

            var dt = new DataTable();
            using var adapter = new SQLiteDataAdapter(cmd);
            adapter.Fill(dt);
            return dt;
        }

        public static int InsertThongTinDatHang(DanhSachDatHangModel header, ThongTinDatHangModel detail)
        {
            using var conn = DB_Base.OpenConnection();
            using var tran = conn.BeginTransaction();
            try
            {
                int headerId = EnsureDanhSachDatHang(conn, tran, header);
                detail.DanhSachDatHang_ID = headerId;

                const string sqlInsertDetail = @"
                    INSERT INTO ThongTinDatHang
                    (
                        DanhSachMaSP_ID,
                        DanhSachDatHang_ID,
                        TenVatTu,
                        TenVatTu_KhongDau,
                        SoLuongMua,
                        MucDichMua,
                        NgayGiao,
                        Date_Insert,
                        DonGia
                    )
                    VALUES
                    (
                        @danhSachMaSPId,
                        @danhSachDatHangId,
                        @tenVatTu,
                        @tenVatTuKhongDau,
                        @soLuongMua,
                        @mucDichMua,
                        @ngayGiao,
                        @dateInsert,
                        @donGia
                    );
                    SELECT last_insert_rowid();";

                using var cmd = new SQLiteCommand(sqlInsertDetail, conn, tran);
                AddDetailParameters(cmd, detail, includeId: false);

                int newDetailId = Convert.ToInt32(cmd.ExecuteScalar());
                tran.Commit();
                return newDetailId;
            }
            catch
            {
                tran.Rollback();
                throw;
            }
        }

        public static void UpdateDanhSachDatHangNgayThem(string maDon, DateTime ngayThem)
        {
            const string sql = @"
                UPDATE DanhSachDatHang
                SET NgayThem = @ngayThem
                WHERE MaDon = @maDon";

            using var conn = DB_Base.OpenConnection();
            using var cmd = new SQLiteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@ngayThem", ngayThem.ToString(DateFormat));
            cmd.Parameters.AddWithValue("@maDon", maDon);
            cmd.ExecuteNonQuery();
        }

        public static void UpdateThongTinDatHang(ThongTinDatHangModel detail)
        {
            if (HasLichSuXuatNhap(detail.Id))
                throw new InvalidOperationException("Dòng này đã có dữ liệu trong Lịch sử xuất nhập nên không được sửa.");

            const string sql = @"
                UPDATE ThongTinDatHang
                SET DanhSachMaSP_ID = @danhSachMaSPId,
                    TenVatTu = @tenVatTu,
                    TenVatTu_KhongDau = @tenVatTuKhongDau,
                    SoLuongMua = @soLuongMua,
                    MucDichMua = @mucDichMua,
                    NgayGiao = @ngayGiao,
                    DonGia = @donGia
                WHERE id = @id";

            using var conn = DB_Base.OpenConnection();
            using var cmd = new SQLiteCommand(sql, conn);
            AddDetailParameters(cmd, detail, includeId: true);
            cmd.ExecuteNonQuery();
        }

        public static bool ExistsDuplicateDetail(int danhSachDatHangId, int currentThongTinDatHangId, int loaiDon, int? danhSachMaSPId, string tenVatTuKhongDau)
        {
            const string sql = @"
                SELECT COUNT(1)
                FROM ThongTinDatHang
                WHERE DanhSachDatHang_ID = @danhSachDatHangId
                  AND id <> @currentThongTinDatHangId
                  AND (
                        (@loaiDon = 1 AND DanhSachMaSP_ID = @danhSachMaSPId)
                        OR
                        (@loaiDon = 2 AND LOWER(COALESCE(TenVatTu_KhongDau, '')) = LOWER(COALESCE(@tenVatTuKhongDau, '')))
                      )";

            using var conn = DB_Base.OpenConnection();
            using var cmd = new SQLiteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@danhSachDatHangId", danhSachDatHangId);
            cmd.Parameters.AddWithValue("@currentThongTinDatHangId", currentThongTinDatHangId);
            cmd.Parameters.AddWithValue("@loaiDon", loaiDon);
            cmd.Parameters.AddWithValue("@danhSachMaSPId", (object)danhSachMaSPId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@tenVatTuKhongDau", (object)tenVatTuKhongDau ?? DBNull.Value);

            return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        }

        public static bool HasLichSuXuatNhap(int thongTinDatHangId)
        {
            const string sql = @"
                SELECT COUNT(1)
                FROM LichSuXuatNhap
                WHERE ThongTinDatHang_ID = @thongTinDatHangId";

            using var conn = DB_Base.OpenConnection();
            using var cmd = new SQLiteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@thongTinDatHangId", thongTinDatHangId);
            return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        }

        public static DeleteDatHangResult DeleteThongTinDatHangAndHeaderIfEmpty(int thongTinDatHangId)
        {
            using var conn = DB_Base.OpenConnection();
            using var tran = conn.BeginTransaction();
            try
            {
                if (HasLichSuXuatNhap(conn, tran, thongTinDatHangId))
                    throw new InvalidOperationException("Dòng này đã có dữ liệu trong Lịch sử xuất nhập nên không được xoá.");

                const string sqlHeader = @"
                    SELECT ddh.id, ddh.MaDon
                    FROM ThongTinDatHang ttdh
                    JOIN DanhSachDatHang ddh ON ddh.id = ttdh.DanhSachDatHang_ID
                    WHERE ttdh.id = @id";

                int headerId;
                string maDon;
                using (var cmdHeader = new SQLiteCommand(sqlHeader, conn, tran))
                {
                    cmdHeader.Parameters.AddWithValue("@id", thongTinDatHangId);
                    using var reader = cmdHeader.ExecuteReader();
                    if (!reader.Read())
                        throw new InvalidOperationException("Không tìm thấy dòng cần xoá.");

                    headerId = Convert.ToInt32(reader["id"]);
                    maDon = Convert.ToString(reader["MaDon"]);
                }

                using (var cmdDeleteDetail = new SQLiteCommand("DELETE FROM ThongTinDatHang WHERE id = @id", conn, tran))
                {
                    cmdDeleteDetail.Parameters.AddWithValue("@id", thongTinDatHangId);
                    cmdDeleteDetail.ExecuteNonQuery();
                }

                int remaining;
                using (var cmdCount = new SQLiteCommand("SELECT COUNT(1) FROM ThongTinDatHang WHERE DanhSachDatHang_ID = @headerId", conn, tran))
                {
                    cmdCount.Parameters.AddWithValue("@headerId", headerId);
                    remaining = Convert.ToInt32(cmdCount.ExecuteScalar());
                }

                bool deletedHeader = false;
                if (remaining == 0)
                {
                    using var cmdDeleteHeader = new SQLiteCommand("DELETE FROM DanhSachDatHang WHERE id = @headerId", conn, tran);
                    cmdDeleteHeader.Parameters.AddWithValue("@headerId", headerId);
                    cmdDeleteHeader.ExecuteNonQuery();
                    deletedHeader = true;
                }

                tran.Commit();
                return new DeleteDatHangResult
                {
                    DeletedDetail = true,
                    DeletedHeader = deletedHeader,
                    DanhSachDatHangId = headerId,
                    MaDon = maDon
                };
            }
            catch
            {
                tran.Rollback();
                throw;
            }
        }

        public static MuaVatTuGridRowModel GetGridRowByThongTinDatHangId(int thongTinDatHangId)
        {
            const string sql = @"
                SELECT ttdh.id AS ThongTinDatHangId,
                       ddh.id AS DanhSachDatHangId,
                       ddh.MaDon,
                       ddh.NgayThem,
                       ttdh.DanhSachMaSP_ID,
                       dsm.Ma AS MaVatTu,
                       COALESCE(dsm.Ten, ttdh.TenVatTu) AS TenVatTu,
                       dsm.DonVi,
                       ttdh.SoLuongMua,
                       ttdh.MucDichMua,
                       ttdh.NgayGiao,
                       COALESCE(dsm.Ten_KhongDau, ttdh.TenVatTu_KhongDau) AS TenVatTuKhongDau,
                       ttdh.DonGia,
                       NULL AS SLTon
                FROM ThongTinDatHang ttdh
                JOIN DanhSachDatHang ddh ON ddh.id = ttdh.DanhSachDatHang_ID
                LEFT JOIN DanhSachMaSP dsm ON dsm.id = ttdh.DanhSachMaSP_ID
                WHERE ttdh.id = @id";

            using var conn = DB_Base.OpenConnection();
            using var cmd = new SQLiteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", thongTinDatHangId);

            using var reader = cmd.ExecuteReader();
            return reader.Read() ? ReadGridRow(reader) : null;
        }

        public static List<MuaVatTuGridRowModel> GetGridRowsByMaDon(string maDon, int loaiDon, string nguoiDat)
        {
            const string sql = @"
                SELECT ttdh.id AS ThongTinDatHangId,
                       ddh.id AS DanhSachDatHangId,
                       ddh.MaDon,
                       ddh.NgayThem,
                       ttdh.DanhSachMaSP_ID,
                       dsm.Ma AS MaVatTu,
                       COALESCE(dsm.Ten, ttdh.TenVatTu) AS TenVatTu,
                       dsm.DonVi,
                       ttdh.SoLuongMua,
                       ttdh.MucDichMua,
                       ttdh.NgayGiao,
                       COALESCE(dsm.Ten_KhongDau, ttdh.TenVatTu_KhongDau) AS TenVatTuKhongDau,
                       ttdh.DonGia,
                       NULL AS SLTon
                FROM DanhSachDatHang ddh
                JOIN ThongTinDatHang ttdh ON ttdh.DanhSachDatHang_ID = ddh.id
                LEFT JOIN DanhSachMaSP dsm ON dsm.id = ttdh.DanhSachMaSP_ID
                WHERE ddh.MaDon = @maDon
                  AND ddh.LoaiDon = @loaiDon
                  AND COALESCE(ddh.NguoiDat, '') = COALESCE(@nguoiDat, '')
                ORDER BY ttdh.id";

            var result = new List<MuaVatTuGridRowModel>();
            using var conn = DB_Base.OpenConnection();
            using var cmd = new SQLiteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@maDon", maDon);
            cmd.Parameters.AddWithValue("@loaiDon", loaiDon);
            cmd.Parameters.AddWithValue("@nguoiDat", (object)nguoiDat ?? DBNull.Value);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                result.Add(ReadGridRow(reader));

            return result;
        }

        private static int EnsureDanhSachDatHang(SQLiteConnection conn, SQLiteTransaction tran, DanhSachDatHangModel header)
        {
            const string sqlFind = "SELECT id FROM DanhSachDatHang WHERE MaDon = @maDon";
            using (var cmdFind = new SQLiteCommand(sqlFind, conn, tran))
            {
                cmdFind.Parameters.AddWithValue("@maDon", header.MaDon);
                object found = cmdFind.ExecuteScalar();
                if (found != null && found != DBNull.Value)
                    return Convert.ToInt32(found);
            }

            const string sqlInsert = @"
                INSERT INTO DanhSachDatHang
                (
                    MaDon,
                    LoaiDon,
                    DateInsert,
                    NguoiDat,
                    NgayThem
                )
                VALUES
                (
                    @maDon,
                    @loaiDon,
                    @dateInsert,
                    @nguoiDat,
                    @ngayThem
                );
                SELECT last_insert_rowid();";

            using var cmdInsert = new SQLiteCommand(sqlInsert, conn, tran);
            cmdInsert.Parameters.AddWithValue("@maDon", header.MaDon);
            cmdInsert.Parameters.AddWithValue("@loaiDon", header.LoaiDon);
            cmdInsert.Parameters.AddWithValue("@dateInsert", header.DateInsert.ToString(DateTimeFormat));
            cmdInsert.Parameters.AddWithValue("@nguoiDat", (object)header.NguoiDat ?? DBNull.Value);
            cmdInsert.Parameters.AddWithValue("@ngayThem", header.NgayThem.ToString(DateFormat));

            return Convert.ToInt32(cmdInsert.ExecuteScalar());
        }

        private static void AddDetailParameters(SQLiteCommand cmd, ThongTinDatHangModel detail, bool includeId)
        {
            if (includeId)
                cmd.Parameters.AddWithValue("@id", detail.Id);

            cmd.Parameters.AddWithValue("@danhSachMaSPId", (object)detail.DanhSachMaSP_ID ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@danhSachDatHangId", detail.DanhSachDatHang_ID);
            cmd.Parameters.AddWithValue("@tenVatTu", (object)detail.TenVatTu ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@tenVatTuKhongDau", (object)detail.TenVatTu_KhongDau ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@soLuongMua", detail.SoLuongMua);
            cmd.Parameters.AddWithValue("@mucDichMua", (object)detail.MucDichMua ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ngayGiao", detail.NgayGiao.HasValue ? (object)detail.NgayGiao.Value.ToString(DateFormat) : DBNull.Value);
            cmd.Parameters.AddWithValue("@dateInsert", detail.Date_Insert.ToString(DateTimeFormat));
            cmd.Parameters.AddWithValue("@donGia", detail.DonGia.HasValue ? (object)detail.DonGia.Value : DBNull.Value);
        }

        private static bool HasLichSuXuatNhap(SQLiteConnection conn, SQLiteTransaction tran, int thongTinDatHangId)
        {
            const string sql = @"
                SELECT COUNT(1)
                FROM LichSuXuatNhap
                WHERE ThongTinDatHang_ID = @thongTinDatHangId";

            using var cmd = new SQLiteCommand(sql, conn, tran);
            cmd.Parameters.AddWithValue("@thongTinDatHangId", thongTinDatHangId);
            return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        }

        private static MuaVatTuGridRowModel ReadGridRow(SQLiteDataReader reader)
        {
            return new MuaVatTuGridRowModel
            {
                ThongTinDatHangId = ReadInt(reader, "ThongTinDatHangId"),
                DanhSachDatHangId = ReadInt(reader, "DanhSachDatHangId"),
                MaDon = ReadString(reader, "MaDon"),
                NgayThem = ReadNullableDate(reader, "NgayThem"),
                DanhSachMaSPId = ReadNullableInt(reader, "DanhSachMaSP_ID"),
                MaVatTu = ReadString(reader, "MaVatTu"),
                TenVatTu = ReadString(reader, "TenVatTu"),
                DonVi = ReadString(reader, "DonVi"),
                SoLuongMua = ReadDecimal(reader, "SoLuongMua"),
                MucDichMua = ReadString(reader, "MucDichMua"),
                NgayGiao = ReadNullableDate(reader, "NgayGiao"),
                TenVatTuKhongDau = ReadString(reader, "TenVatTuKhongDau"),
                DonGia = ReadNullableDecimal(reader, "DonGia"),
                SLTon = ReadNullableDecimal(reader, "SLTon")
            };
        }

        private static string Like(string value)
        {
            return "%" + (value ?? string.Empty).Trim().ToLowerInvariant() + "%";
        }

        private static int ReadInt(SQLiteDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? 0 : Convert.ToInt32(reader.GetValue(ordinal));
        }

        private static int? ReadNullableInt(SQLiteDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? (int?)null : Convert.ToInt32(reader.GetValue(ordinal));
        }

        private static decimal ReadDecimal(SQLiteDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? 0m : Convert.ToDecimal(reader.GetValue(ordinal));
        }

        private static decimal? ReadNullableDecimal(SQLiteDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? (decimal?)null : Convert.ToDecimal(reader.GetValue(ordinal));
        }

        private static string ReadString(SQLiteDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? null : Convert.ToString(reader.GetValue(ordinal));
        }

        private static DateTime? ReadNullableDate(SQLiteDataReader reader, string columnName)
        {
            string value = ReadString(reader, columnName);
            if (string.IsNullOrWhiteSpace(value))
                return null;

            return DateTime.TryParse(value, out DateTime date) ? date : (DateTime?)null;
        }
    }
}
