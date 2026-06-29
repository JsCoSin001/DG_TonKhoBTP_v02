using DG_TonKhoBTP_v02.Models.KeToan;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Threading.Tasks;

namespace DG_TonKhoBTP_v02.Database.KeToan
{
    public static class KiemKe_DB
    {
        public static DataTable TimKiemSanPhamKiemKe(string keyword)
        {
            const string sql = @"
                    SELECT id, Ten, Ma
                    FROM DanhSachMaSP
                    WHERE Ten LIKE @kw
                      AND (
                            Ma LIKE 'NVL.%' COLLATE NOCASE
                             OR Ma LIKE 'TP.%'  COLLATE NOCASE
                             OR Ma LIKE 'BTP.%'  COLLATE NOCASE
                      )
                    ORDER BY Ten
                    LIMIT 30";

            return DB_Base.GetData(sql, $"%{keyword}%", "kw");
        }

        public static async Task<Dictionary<string, decimal>> LayDanhSachBin_KhoiLuong()
        {
            var result = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

            using (var conn = await DB_Base.OpenConnectionAsync())
            {
                const string sql = @"SELECT TenBin, KhoiLuongBin FROM DanhSachBin";

                using (var cmd = new SQLiteCommand(sql, conn))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        string tenBin = reader["TenBin"]?.ToString()?.Trim();

                        decimal khoiLuong = reader["KhoiLuongBin"] != DBNull.Value
                            ? Convert.ToDecimal(reader["KhoiLuongBin"])
                            : 0m;

                        if (!string.IsNullOrWhiteSpace(tenBin))
                        {
                            result[tenBin] = khoiLuong;
                        }
                    }
                }
            }

            return result;
        }

        public static DataTable Load_TTKiemKeThang(string namThang = null, string nguoiKK = "Người 1")
        {
            if (string.IsNullOrWhiteSpace(namThang))
            {
                namThang = DateTime.Now.ToString("yyyy-MM");
            }

            const string sql = @"
            SELECT 
                tp.DanhSachSP_ID                              AS DanhSachSP_ID,
                kk.id                                         AS id,
                kk.MaBin                                      AS MaBin,
                sp.Ten                                        AS Ten,
                kk.ChieuDai                                   AS ChieuDai,
                kk.KhoiLuong                                  AS KhoiLuong,
                kk.NguoiKK                                    AS NguoiKK,
                kk.GhiChu                                     AS GhiChu,
                CASE COALESCE(kk.Confirmed, 0)
                    WHEN 1 THEN 'Đã lưu'
                    ELSE 'Chưa lưu'
                END                                           AS TrangThaiLuu
            FROM TTKiemKeThang kk
            LEFT JOIN TTThanhPham tp ON tp.id = kk.TTThanhPham_ID
            LEFT JOIN DanhSachMaSP sp ON sp.id = tp.DanhSachSP_ID
            WHERE kk.ThoiGianKiemKe = @namThang
              AND kk.NguoiKK COLLATE NOCASE = @nguoiKK COLLATE NOCASE
            ORDER BY kk.DateInsert DESC, kk.id DESC;";

            using (var conn = DB_Base.OpenConnection())
            using (var cmd = new SQLiteCommand(sql, conn))
            using (var da = new SQLiteDataAdapter(cmd))
            {
                cmd.Parameters.AddWithValue("@namThang", namThang.Trim());
                cmd.Parameters.AddWithValue("@nguoiKK", (nguoiKK ?? string.Empty).Trim());

                var dt = new DataTable();
                da.Fill(dt);

                if (!dt.Columns.Contains("Confirmed"))
                    dt.Columns.Add("Confirmed", typeof(int));

                if (!dt.Columns.Contains("TrangThaiLuu"))
                    dt.Columns.Add("TrangThaiLuu", typeof(string));

                foreach (DataRow row in dt.Rows)
                {
                    int confirmed = 0;
                    if (row["Confirmed"] != DBNull.Value)
                        int.TryParse(row["Confirmed"].ToString(), out confirmed);

                    row["Confirmed"] = confirmed;
                    row["TrangThaiLuu"] = confirmed == 1 ? "Đã lưu" : "Chưa lưu";
                }

                return dt;
            }
        }

        public static async Task<DataRow> LayThongTinTheoMaBin(string maBin)
        {
            const string sql = @"
            SELECT
                ttp.DanhSachSP_ID       AS DanhSachSP_ID,
                ttp.id                  AS TTThanhPham_ID,
                sp.Ten                  AS Ten,
                sp.Ma                   AS Ma,
                ttp.ChieuDaiSau         AS ChieuDaiSau,
                ttp.KhoiLuongSau        AS KhoiLuongSau,
                ttp.GhiChu              AS GhiChu,
                ca.Ngay                 AS NgaySX,
                ca.Ca                   AS CaSX,
                ca.NguoiLam             AS TenCN,
                bm.Mau                  AS Mau,
                nvl.QC                  AS QC
            FROM TTThanhPham ttp
            JOIN DanhSachMaSP sp            ON sp.id = ttp.DanhSachSP_ID
            LEFT JOIN ThongTinCaLamViec ca  ON ca.TTThanhPham_id = ttp.id
            LEFT JOIN CaiDatCDBoc cdb       ON cdb.TTThanhPham_ID = ttp.id
            LEFT JOIN CD_BocMach bm         ON bm.CaiDatCDBoc_ID = cdb.id
            LEFT JOIN TTNVL nvl             ON nvl.TTThanhPham_ID = ttp.id
            WHERE ttp.MaBin = @maBin
            ORDER BY ttp.id DESC
            LIMIT 1;";

            DataTable dt = new DataTable();

            using (var conn = await DB_Base.OpenConnectionAsync())
            using (var cmd = new SQLiteCommand(sql, conn))
            using (var da = new SQLiteDataAdapter(cmd))
            {
                cmd.Parameters.AddWithValue("@maBin", maBin?.Trim());

                await Task.Run(() => da.Fill(dt));
            }

            return dt.Rows.Count > 0 ? dt.Rows[0] : null;
        }

        public static long InsertTTThanhPham_FromKiemKe(KiemKe model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            if (string.IsNullOrWhiteSpace(model.MaBin))
                throw new ArgumentException("MaBin không được để trống.", nameof(model.MaBin));

            const string sql = @"
            INSERT INTO TTThanhPham
            (
                DanhSachSP_ID,
                MaBin,
                KhoiLuongTruoc,
                KhoiLuongSau,
                ChieuDaiTruoc,
                ChieuDaiSau,
                Phe,
                CongDoan,
                GhiChu,
                DateInsert
            )
            VALUES
            (
                @DanhSachSP_ID,
                @MaBin,
                @KhoiLuongTruoc,
                @KhoiLuongSau,
                @ChieuDaiTruoc,
                @ChieuDaiSau,
                0,
                0,
                @GhiChu,
                @DateInsert
            );
            SELECT last_insert_rowid();";

            using (var conn = DB_Base.OpenConnection())
            {
                using (var tx = conn.BeginTransaction())
                {
                    try
                    {
                        using (var cmd = new SQLiteCommand(sql, conn, tx))
                        {
                            cmd.Parameters.AddWithValue("@DanhSachSP_ID",
                                (object?)model.DanhSachSP_ID ?? DBNull.Value);

                            cmd.Parameters.AddWithValue("@MaBin",
                                model.MaBin.Trim());

                            cmd.Parameters.AddWithValue("@KhoiLuongTruoc",
                                (object?)model.KhoiLuong ?? DBNull.Value);

                            cmd.Parameters.AddWithValue("@KhoiLuongSau",
                                (object?)model.KhoiLuong ?? DBNull.Value);

                            cmd.Parameters.AddWithValue("@ChieuDaiTruoc",
                                (object?)model.ChieuDai ?? DBNull.Value);

                            cmd.Parameters.AddWithValue("@ChieuDaiSau",
                                (object?)model.ChieuDai ?? DBNull.Value);

                            cmd.Parameters.AddWithValue("@GhiChu",
                                string.IsNullOrWhiteSpace(model.GhiChu)
                                    ? (object)DBNull.Value
                                    : model.GhiChu.Trim());

                            cmd.Parameters.AddWithValue("@DateInsert",
                                string.IsNullOrWhiteSpace(model.DateInsert)
                                    ? DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                                    : model.DateInsert.Trim());

                            long newId = Convert.ToInt64(cmd.ExecuteScalar());

                            tx.Commit();
                            return newId;
                        }
                    }
                    catch
                    {
                        tx.Rollback();
                        throw;
                    }
                }
            }
        }

        public static long Insert_TTKiemKeThang(KiemKe model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            if (string.IsNullOrWhiteSpace(model.MaBin))
                throw new ArgumentException("MaBin không được để trống.", nameof(model.MaBin));

            using (var conn = DB_Base.OpenConnection())
            {
                using (var tx = conn.BeginTransaction())
                {
                    try
                    {
                        var now = DateTime.Now;

                        string thoiGianKiemKe = string.IsNullOrWhiteSpace(model.ThoiGianKiemKe)
                            ? now.ToString("yyyy-MM")
                            : model.ThoiGianKiemKe.Trim();

                        string dateInsert = string.IsNullOrWhiteSpace(model.DateInsert)
                            ? now.ToString("yyyy-MM-dd HH:mm:ss")
                            : model.DateInsert.Trim();

                        using (var cmdInsert = new SQLiteCommand(@"
                        INSERT INTO TTKiemKeThang
                        (
                            TTThanhPham_ID, MaBin, ChieuDai, KhoiLuong,
                            GhiChu, ThoiGianKiemKe, ApprovedDate, DateInsert, NguoiKK
                        )
                        VALUES
                        (
                            @TTThanhPham_ID, @MaBin, @ChieuDai, @KhoiLuong,
                            @GhiChu, @ThoiGianKiemKe, @ApprovedDate, @DateInsert, @NguoiKK
                        );", conn, tx))
                        {
                            cmdInsert.Parameters.AddWithValue("@TTThanhPham_ID", (object?)model.TTThanhPham_ID ?? DBNull.Value);
                            cmdInsert.Parameters.AddWithValue("@MaBin", model.MaBin.Trim());
                            cmdInsert.Parameters.AddWithValue("@ChieuDai", (object?)model.ChieuDai ?? DBNull.Value);
                            cmdInsert.Parameters.AddWithValue("@KhoiLuong", (object?)model.KhoiLuong ?? DBNull.Value);
                            cmdInsert.Parameters.AddWithValue("@GhiChu", string.IsNullOrWhiteSpace(model.GhiChu) ? (object)DBNull.Value : model.GhiChu.Trim());
                            cmdInsert.Parameters.AddWithValue("@ThoiGianKiemKe", thoiGianKiemKe);
                            cmdInsert.Parameters.AddWithValue("@ApprovedDate", string.IsNullOrWhiteSpace(model.ApprovedDate) ? (object)DBNull.Value : model.ApprovedDate.Trim());
                            cmdInsert.Parameters.AddWithValue("@DateInsert", dateInsert);
                            cmdInsert.Parameters.AddWithValue("@NguoiKK", model.NguoiKK);

                            cmdInsert.ExecuteNonQuery();
                        }

                        long newId = conn.LastInsertRowId;
                        tx.Commit();
                        return newId;
                    }
                    catch
                    {
                        tx.Rollback();
                        throw;
                    }
                }
            }
        }

        public static void UpsertDanhSachBin(DanhSachBin bin)
        {
            string tenBin = bin.TenBin;
            decimal khoiLuong = (decimal)bin.KhoiLuongBin;
            if (string.IsNullOrWhiteSpace(tenBin)) return;

            const string sql = @"
            INSERT OR REPLACE INTO DanhSachBin (TenBin, KhoiLuongBin)
            VALUES (@TenBin, @KhoiLuong);";

            using (var conn = DB_Base.OpenConnection())
            using (var cmd = new SQLiteCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@TenBin", tenBin.Trim());
                cmd.Parameters.AddWithValue("@KhoiLuong", khoiLuong);
                cmd.ExecuteNonQuery();
            }
        }

        public static bool Update_TTKiemKeThang(
            int id,
            decimal? chieuDai,
            decimal? khoiLuong,
            string ghiChu)
        {
            const string sql = @"
        UPDATE TTKiemKeThang
        SET 
            ChieuDai  = @ChieuDai,
            KhoiLuong = @KhoiLuong,
            GhiChu    = @GhiChu
        WHERE id = @id;";

            using (var conn = DB_Base.OpenConnection())
            using (var cmd = new SQLiteCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@id", id);
                cmd.Parameters.AddWithValue("@ChieuDai", (object?)chieuDai ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@KhoiLuong", (object?)khoiLuong ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@GhiChu", string.IsNullOrWhiteSpace(ghiChu) ? (object)DBNull.Value : ghiChu.Trim());

                return cmd.ExecuteNonQuery() > 0;
            }
        }

        public static bool Delete_TTKiemKeThang(int id)
        {
            const string sql = @"DELETE FROM TTKiemKeThang WHERE id = @id;";

            using (var conn = DB_Base.OpenConnection())
            using (var cmd = new SQLiteCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@id", id);

                return cmd.ExecuteNonQuery() > 0;
            }
        }

        public static int UpsertTTThanhPhamByMaBin(List<KiemKe> items)
        {
            if (items == null || items.Count == 0)
                return 0;

            int affectedTotal = 0;
            string now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            const string sqlUpdate = @"
            UPDATE TTThanhPham
            SET DanhSachSP_ID = @DanhSachSP_ID,
                KhoiLuongSau  = @KhoiLuongSau,
                ChieuDaiSau   = @ChieuDaiSau,
                GhiChu        = @GhiChu,
                DateInsert    = @DateInsert
            WHERE MaBin = @MaBin;";

            const string sqlInsert = @"
            INSERT INTO TTThanhPham
            (
                DanhSachSP_ID, MaBin, KhoiLuongTruoc,
                KhoiLuongSau, ChieuDaiTruoc, ChieuDaiSau, Phe,
                CongDoan, GhiChu, HanNoi, DateInsert
            )
            VALUES
            (
                @DanhSachSP_ID, @MaBin, @KhoiLuongTruoc,
                @KhoiLuongSau, @ChieuDaiTruoc, @ChieuDaiSau,
                0, 0, @GhiChu, 0, @DateInsert
            );
            SELECT last_insert_rowid();";

            const string sqlApprove = @"
            UPDATE TTKiemKeThang
            SET ApprovedDate = @ApprovedDate
            WHERE id = @id;";

            using (var conn = DB_Base.OpenConnection())
            using (var tx = conn.BeginTransaction())
            {
                try
                {
                    using (var cmdUpdate = new SQLiteCommand(sqlUpdate, conn, tx))
                    using (var cmdInsert = new SQLiteCommand(sqlInsert, conn, tx))
                    using (var cmdApprove = new SQLiteCommand(sqlApprove, conn, tx))
                    {
                        // Khai báo parameters 1 lần, tái sử dụng
                        cmdUpdate.Parameters.Add("@DanhSachSP_ID", DbType.Int64);
                        cmdUpdate.Parameters.Add("@KhoiLuongSau", DbType.Decimal);
                        cmdUpdate.Parameters.Add("@ChieuDaiSau", DbType.Decimal);
                        cmdUpdate.Parameters.Add("@GhiChu", DbType.String);
                        cmdUpdate.Parameters.Add("@DateInsert", DbType.String);
                        cmdUpdate.Parameters.Add("@MaBin", DbType.String);

                        cmdInsert.Parameters.Add("@DanhSachSP_ID", DbType.Int64);
                        cmdInsert.Parameters.Add("@MaBin", DbType.String);
                        cmdInsert.Parameters.Add("@KhoiLuongTruoc", DbType.Decimal);
                        cmdInsert.Parameters.Add("@KhoiLuongSau", DbType.Decimal);
                        cmdInsert.Parameters.Add("@ChieuDaiTruoc", DbType.Decimal);
                        cmdInsert.Parameters.Add("@ChieuDaiSau", DbType.Decimal);
                        cmdInsert.Parameters.Add("@GhiChu", DbType.String);
                        cmdInsert.Parameters.Add("@DateInsert", DbType.String);

                        cmdApprove.Parameters.Add("@ApprovedDate", DbType.String);
                        cmdApprove.Parameters.Add("@id", DbType.Int64);

                        foreach (var item in items)
                        {
                            if (item == null) continue;
                            if (!item.DanhSachSP_ID.HasValue || item.DanhSachSP_ID.Value <= 0) continue;

                            string maBin = (item.MaBin ?? "").Trim();
                            if (string.IsNullOrWhiteSpace(maBin)) continue;

                            decimal khoiLuong = item.KhoiLuong ?? 0m;
                            decimal chieuDai = item.ChieuDai ?? 0m;
                            object ghiChu = string.IsNullOrWhiteSpace(item.GhiChu)
                                                    ? DBNull.Value
                                                    : (object)item.GhiChu.Trim();
                            string dateInsert = string.IsNullOrWhiteSpace(item.DateInsert)
                                                    ? now
                                                    : item.DateInsert.Trim();

                            // ── UPDATE ──────────────────────────────────────────────
                            cmdUpdate.Parameters["@DanhSachSP_ID"].Value = item.DanhSachSP_ID.Value;
                            cmdUpdate.Parameters["@KhoiLuongSau"].Value = khoiLuong;
                            cmdUpdate.Parameters["@ChieuDaiSau"].Value = chieuDai;
                            cmdUpdate.Parameters["@GhiChu"].Value = ghiChu;
                            cmdUpdate.Parameters["@DateInsert"].Value = dateInsert;
                            cmdUpdate.Parameters["@MaBin"].Value = maBin;

                            int rows = cmdUpdate.ExecuteNonQuery();

                            // ── INSERT nếu chưa tồn tại ─────────────────────────────
                            if (rows == 0)
                            {
                                cmdInsert.Parameters["@DanhSachSP_ID"].Value = item.DanhSachSP_ID.Value;
                                cmdInsert.Parameters["@MaBin"].Value = maBin;
                                cmdInsert.Parameters["@KhoiLuongTruoc"].Value = khoiLuong;
                                cmdInsert.Parameters["@KhoiLuongSau"].Value = khoiLuong;
                                cmdInsert.Parameters["@ChieuDaiTruoc"].Value = chieuDai;
                                cmdInsert.Parameters["@ChieuDaiSau"].Value = chieuDai;
                                cmdInsert.Parameters["@GhiChu"].Value = ghiChu;
                                cmdInsert.Parameters["@DateInsert"].Value = dateInsert;

                                rows = cmdInsert.ExecuteNonQuery();
                            }

                            affectedTotal += rows;

                            // ── ApprovedDate trong TTKiemKeThang ────────────────────
                            if (item.id.HasValue)
                            {
                                cmdApprove.Parameters["@ApprovedDate"].Value = now;
                                cmdApprove.Parameters["@id"].Value = item.id.Value;

                                Console.WriteLine("@sql: " + sqlApprove + "\n");
                                Console.WriteLine("@id: " + item.id.Value);

                                cmdApprove.ExecuteNonQuery();
                            }
                        }
                    }

                    tx.Commit();
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }
            }

            return affectedTotal;
        }
    }
}
