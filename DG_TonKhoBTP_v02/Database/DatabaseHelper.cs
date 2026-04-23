
using DG_TonKhoBTP_v02.Core;
using DG_TonKhoBTP_v02.Dictionary;
using DG_TonKhoBTP_v02.DL_Ben;
using DG_TonKhoBTP_v02.Helper;
using DG_TonKhoBTP_v02.Models;
using DG_TonKhoBTP_v02.UI;
using DG_TonKhoBTP_v02.UI.Helper.AutoSearchWithCombobox;
using DocumentFormat.OpenXml.Math;
using DocumentFormat.OpenXml.Spreadsheet;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Data.SQLite;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;
using static DG_TonKhoBTP_v02.Models.KeHoach;
using CoreHelper = DG_TonKhoBTP_v02.Helper.Helper;
using UserInfo = DG_TonKhoBTP_v02.Models.UserInfo;

namespace DG_TonKhoBTP_v02.Database
{
    public static class DatabaseHelper
    {
        public static string _connStr;

        // Thiết lập đường dẫn đến cơ sở dữ liệu SQLite
        public static void SetDatabasePath(string path)
        {
            _connStr = $"Data Source={path};Version=3;";
        }

        public static string GetStringConnector
        {
            get { return _connStr; }
        }

        #region Kiểm kê


        /// <summary>
        /// Kiểm kê thực tế kho hiện tại.
        /// Tồn thực tế = SUM(LichSuXuatNhap.SoLuong)
        /// Chỉ lấy các vật tư còn tồn > 0
        /// </summary>
        public static async Task<DataTable> LayBaoCaoKiemKeThucTeKho(string tuKhoa = "")
        {
            const string sql = @"
            SELECT
                MIN(t.id)                            AS id,
                t.TenVatTu                           AS TenVatTu,
                IFNULL(sp.Ma, '')                    AS MaVatTu,
                IFNULL(sp.Ten, '')                   AS TenDanhMuc,
                IFNULL(sp.DonVi, '')                 AS DonVi,
                SUM(IFNULL(t.SoLuongMua, 0))         AS TongSoLuongMua,
                SUM(IFNULL(ls.TonKho, 0))            AS TonThucTe
            FROM ThongTinDatHang t
            INNER JOIN DanhSachDatHang d
                ON d.id = t.DanhSachDatHang_ID
            LEFT JOIN DanhSachMaSP sp
                ON sp.id = t.DanhSachMaSP_ID
            LEFT JOIN
            (
                SELECT
                    ThongTinDatHang_ID,
                    SUM(SoLuong) AS TonKho
                FROM LichSuXuatNhap
                GROUP BY ThongTinDatHang_ID
            ) ls
                ON ls.ThongTinDatHang_ID = t.id
            WHERE
                t.CanEdit = 1
                AND
                (
                    @tuKhoa IS NULL OR TRIM(@tuKhoa) = ''
                    OR t.TenVatTu LIKE '%' || @tuKhoa || '%'
                    OR sp.Ma LIKE '%' || @tuKhoa || '%'
                    OR sp.Ten LIKE '%' || @tuKhoa || '%'
                )
            GROUP BY
                t.TenVatTu,
                sp.Ma,
                sp.Ten,
                sp.DonVi
            HAVING SUM(IFNULL(ls.TonKho, 0)) > 0
            ORDER BY t.TenVatTu, sp.Ma;";

            var dt = new DataTable();

            await Task.Run(() =>
            {
                using var conn = new SQLiteConnection(_connStr);
                conn.Open();

                using var cmd = new SQLiteCommand(sql, conn);
                cmd.Parameters.AddWithValue("@tuKhoa", tuKhoa ?? "");

                using var adapter = new SQLiteDataAdapter(cmd);
                adapter.Fill(dt);
            });

            return dt;
        }

        public static async Task<List<NhaCungCapItem>> TimKiemNhaCungCap(string keyword)
        {
            const string sql = @"
            SELECT TenNCC, id
            FROM DanhSachNCC
            WHERE TenNCC_KhongDau LIKE @kw COLLATE NOCASE
                OR TenNCC LIKE @kw COLLATE NOCASE
                OR Ma LIKE @kw COLLATE NOCASE
            ORDER BY TenNCC
            LIMIT 30";

            var result = new List<NhaCungCapItem>();

            await Task.Run(() =>
            {
                using var conn = new SQLiteConnection(_connStr);
                conn.Open();
                using var cmd = new SQLiteCommand(sql, conn);
                cmd.Parameters.AddWithValue("@kw", $"%{keyword}%");

                using var rd = cmd.ExecuteReader();
                while (rd.Read())
                {
                    result.Add(new NhaCungCapItem
                    {
                        Id = Convert.ToInt32(rd["id"]),
                        Ten = rd["TenNCC"].ToString()
                    });
                }
            });

            return result;
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

            using (var conn = new SQLiteConnection(_connStr))
            {
                conn.Open();

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

        public static async Task<Dictionary<string, decimal>> LayDanhSachBin_KhoiLuong()
        {
            var result = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

            using (var conn = new SQLiteConnection(GetStringConnector))
            {
                await conn.OpenAsync();

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
                    tp.DanhSachSP_ID,
                    kk.id,
                    kk.MaBin,
                    sp.Ten,
                    kk.ChieuDai,
                    kk.KhoiLuong,
                    kk.NguoiKK,
                    kk.GhiChu
                FROM TTKiemKeThang kk
                LEFT JOIN TTThanhPham tp ON tp.id = kk.TTThanhPham_ID
                LEFT JOIN DanhSachMaSP sp ON sp.id = tp.DanhSachSP_ID
                WHERE kk.ThoiGianKiemKe = @namThang  AND kk.NguoiKK = @nguoiKK COLLATE NOCASE
                ORDER BY kk.DateInsert DESC, kk.id DESC;";

            using (var conn = new SQLiteConnection(GetStringConnector))
            using (var cmd = new SQLiteCommand(sql, conn))
            using (var da = new SQLiteDataAdapter(cmd))
            {
                cmd.Parameters.AddWithValue("@namThang", namThang);
                cmd.Parameters.AddWithValue("@nguoiKK", nguoiKK);

                var dt = new DataTable();
                conn.Open();
                da.Fill(dt);
                return dt;
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

            using (var conn = new SQLiteConnection(GetStringConnector))
            using (var cmd = new SQLiteCommand(sql, conn))
            {
                conn.Open();

                cmd.Parameters.AddWithValue("@id", id);
                cmd.Parameters.AddWithValue("@ChieuDai", (object)chieuDai ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@KhoiLuong", (object)khoiLuong ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@GhiChu", string.IsNullOrWhiteSpace(ghiChu) ? (object)DBNull.Value : ghiChu.Trim());

                return cmd.ExecuteNonQuery() > 0;
            }
        }

        public static bool Delete_TTKiemKeThang(int id)
        {
            const string sql = @"DELETE FROM TTKiemKeThang WHERE id = @id;";

            using (var conn = new SQLiteConnection(GetStringConnector))
            using (var cmd = new SQLiteCommand(sql, conn))
            {
                conn.Open();
                cmd.Parameters.AddWithValue("@id", id);

                return cmd.ExecuteNonQuery() > 0;
            }
        }


        public static bool Delete_ByID(string table ,int id)
        {
            string sql = $@"DELETE FROM {table} WHERE id = @id;";

            using (var conn = new SQLiteConnection(GetStringConnector))
            using (var cmd = new SQLiteCommand(sql, conn))
            {
                conn.Open();
                cmd.Parameters.AddWithValue("@id", id);

                return cmd.ExecuteNonQuery() > 0;
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

            using (var conn = new SQLiteConnection(GetStringConnector))
            using (var cmd = new SQLiteCommand(sql, conn))
            using (var da = new SQLiteDataAdapter(cmd))
            {
                await conn.OpenAsync();
                cmd.Parameters.AddWithValue("@maBin", maBin?.Trim());

                await Task.Run(() => da.Fill(dt));
            }

            return dt.Rows.Count > 0 ? dt.Rows[0] : null;
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

            using var conn = new SQLiteConnection(_connStr);
            conn.Open();

            using var tx = conn.BeginTransaction();
            try
            {
                using var cmdUpdate = new SQLiteCommand(sqlUpdate, conn, tx);
                using var cmdInsert = new SQLiteCommand(sqlInsert, conn, tx);
                using var cmdApprove = new SQLiteCommand(sqlApprove, conn, tx);

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

                        Console.WriteLine("@sql: " + sqlApprove +"\n");
                        Console.WriteLine("@id: " + item.id.Value);



                        cmdApprove.ExecuteNonQuery();
                    }
                }

                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }

            return affectedTotal;
        }

        public static void UpsertDanhSachBin(DanhSachBin bin)
        {
            string tenBin = bin.TenBin;
            decimal khoiLuong = (decimal)bin.KhoiLuongBin;
            if (string.IsNullOrWhiteSpace(tenBin)) return;

            const string sql = @"
            INSERT OR REPLACE INTO DanhSachBin (TenBin, KhoiLuongBin)
            VALUES (@TenBin, @KhoiLuong);";

            using var conn = new SQLiteConnection(GetStringConnector);
            conn.Open();
            using var cmd = new SQLiteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@TenBin", tenBin.Trim());
            cmd.Parameters.AddWithValue("@KhoiLuong", khoiLuong);
            cmd.ExecuteNonQuery();
        }

        public static long Insert_TTKiemKeThang(KiemKe model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            if (string.IsNullOrWhiteSpace(model.MaBin))
                throw new ArgumentException("MaBin không được để trống.", nameof(model.MaBin));

            using (var conn = new SQLiteConnection(GetStringConnector))
            {
                conn.Open();

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
                            cmdInsert.Parameters.AddWithValue("@TTThanhPham_ID", (object)model.TTThanhPham_ID ?? DBNull.Value);
                            cmdInsert.Parameters.AddWithValue("@MaBin", model.MaBin.Trim());
                            cmdInsert.Parameters.AddWithValue("@ChieuDai", (object)model.ChieuDai ?? DBNull.Value);
                            cmdInsert.Parameters.AddWithValue("@KhoiLuong", (object)model.KhoiLuong ?? DBNull.Value);
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

        #endregion


        #region Truy xuất dữ liệu
        public static Task<DataTable> SearchLotSXAsync(string keyword, CancellationToken ct)
        {
            return Task.Run(() =>
            {
                ct.ThrowIfCancellationRequested();

                string sql = @"
                SELECT DISTINCT
                       BinNVL
                FROM TTNVL
                WHERE IFNULL(BinNVL, '') <> ''
                  AND BinNVL LIKE '%' || @key || '%' COLLATE NOCASE
                ORDER BY BinNVL
                LIMIT 50;";

                return DatabaseHelper.GetData(sql, keyword, "key");
            }, ct);
        }


        public static async Task LoadLichSuSXAsync(string binNVL, DataGridView dtView)
        {
            if (string.IsNullOrWhiteSpace(binNVL)) return;

            FrmWaiting waiting = null;
            DataTable dt = null;

            try
            {
                waiting = new FrmWaiting("Đang tải lịch sử sản xuất...");
                waiting.TopMost = true;
                waiting.StartPosition = FormStartPosition.CenterScreen;
                waiting.Show();
                waiting.Refresh();

                dt = await Task.Run(() =>
                {
                    string sql = @"
                SELECT
                    nvl.TTThanhPham_ID           AS ID,
                    nvl.BinNVL                   AS lotNVL,
                    CAST(nvl.KlBatDau AS REAL)   AS klBanDau,
                    CAST(nvl.KlConLai AS REAL)   AS klSau,
                    CAST(nvl.CdBatDau AS REAL)   AS cdBanDau,
                    CAST(nvl.CdConLai AS REAL)   AS cdSau,
                    tp.MaBin                     AS lotTP,
                    sp.Ten                       AS tenTP,
                    tp.DateInsert             AS ngay
                FROM TTNVL nvl
                INNER JOIN TTThanhPham tp
                    ON tp.id = nvl.TTThanhPham_ID
                LEFT JOIN DanhSachMaSP sp
                    ON sp.id = tp.DanhSachSP_ID
                WHERE nvl.BinNVL = @key
                ORDER BY nvl.TTThanhPham_ID DESC;";

                    return DatabaseHelper.GetData(sql, binNVL, "key");
                });

                BindGridLichSuSX(dt, dtView);
            }

            catch (Exception ex)
            {
                MessageBox.Show(
                    "Không tải được lịch sử sản xuất.\n" + ex.Message,
                    "Thông báo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
            finally
            {
                waiting?.SafeClose();
            }
        }


        private static void BindGridLichSuSX(DataTable dt, DataGridView grv)
        {
            grv.AutoGenerateColumns = false;
            grv.DataSource = dt;

            string[] cols = { "klBanDau", "klSau", "cdBanDau", "cdSau" };

            foreach (string col in cols)
            {
                if (grv.Columns.Contains(col))
                {
                    grv.Columns[col].DefaultCellStyle.Format = "N2";
                    grv.Columns[col].DefaultCellStyle.FormatProvider = new CultureInfo("en-US");
                    grv.Columns[col].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                }
            }

        }


        public static Task<DataTable> SearchLichSuSuaDoiTheoLotAsync(string keyword, CancellationToken ct)
        {
            return Task.Run(() =>
            {
                ct.ThrowIfCancellationRequested();

                const string sql = @"
                    SELECT DISTINCT
                           CASE
                               WHEN IFNULL(LOT_Moi, '') <> '' THEN LOT_Moi
                               ELSE LOT_Cu
                           END AS SoLot,
                           CASE
                               WHEN IFNULL(LOT_Moi, '') <> '' AND IFNULL(LOT_Cu, '') <> '' AND LOT_Moi <> LOT_Cu
                                    THEN LOT_Cu || ' -> ' || LOT_Moi
                               WHEN IFNULL(LOT_Moi, '') <> ''
                                    THEN LOT_Moi
                               ELSE LOT_Cu
                           END AS SoLotHienThi
                    FROM LichSuSuaDoiThongTin
                    WHERE
                        (
                            IFNULL(LOT_Cu, '') LIKE '%' || @key || '%' COLLATE NOCASE
                            OR IFNULL(LOT_Moi, '') LIKE '%' || @key || '%' COLLATE NOCASE
                        )
                    ORDER BY SoLot DESC
                    LIMIT 50;";

                return GetData(sql, keyword?.Trim(), "key");
            }, ct);
        }

        public static async Task LoadLichSuSuaDoiTheoLotAsync(string lot, DataGridView dtView)
        {
            if (string.IsNullOrWhiteSpace(lot)) return;

            FrmWaiting waiting = null;
            DataTable dt = null;

            try
            {
                waiting = new FrmWaiting("Đang tải lịch sử sửa đổi...");
                waiting.TopMost = true;
                waiting.StartPosition = FormStartPosition.CenterScreen;
                waiting.Show();
                waiting.Refresh();

                dt = await Task.Run(() =>
                {
                    const string sql = @"
                    SELECT
                        TTThanhPham_ID AS ID_lichSuSuDoi,
                        Ten_Cu         AS TenCu,
                        Ten_Moi        AS TenMoi,
                        LOT_Cu         AS LotCu,
                        LOT_Moi        AS LotMoi,
                        KL_Cu          AS KlCu,
                        KL_Moi         AS KlMoi,
                        CD_Cu          AS cdCu,
                        CD_Moi         AS cdMoi,
                        DateInsert     AS TGsua,
                        TenMay         AS tenMay,
                        GhiChu_Cu      AS ghiChu_Cu,
                        GhiChu_Moi     AS ghiChu_Moi
                    FROM LichSuSuaDoiThongTin
                    WHERE IFNULL(LOT_Cu, '') = @key
                       OR IFNULL(LOT_Moi, '') = @key
                    ORDER BY datetime(DateInsert) DESC, id DESC;";

                    return GetData(sql, lot.Trim(), "key");
                });

                BindGridLichSuSuaDoi(dt, dtView);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Không tải được lịch sử sửa đổi.\n" + ex.Message,
                    "Thông báo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
            finally
            {
                waiting?.SafeClose();
            }
        }

        public static async Task LoadLichSuSuaDoiTheoSoNgayAsync(int soNgay, DataGridView dtView)
        {
            if (soNgay <= 0) return;

            FrmWaiting waiting = null;
            DataTable dt = null;

            try
            {
                waiting = new FrmWaiting("Đang tải lịch sử sửa đổi...");
                waiting.TopMost = true;
                waiting.StartPosition = FormStartPosition.CenterScreen;
                waiting.Show();
                waiting.Refresh();

                dt = await Task.Run(() =>
                {
                    const string sql = @"
                    SELECT
                        TTThanhPham_ID AS ID_lichSuSuDoi,
                        Ten_Cu         AS TenCu,
                        Ten_Moi        AS TenMoi,
                        LOT_Cu         AS LotCu,
                        LOT_Moi        AS LotMoi,
                        KL_Cu          AS KlCu,
                        KL_Moi         AS KlMoi,
                        CD_Cu          AS cdCu,
                        CD_Moi         AS cdMoi,
                        DateInsert     AS TGsua,
                        TenMay         AS tenMay,
                        GhiChu_Cu      AS ghiChu_Cu,
                        GhiChu_Moi     AS ghiChu_Moi
                    FROM LichSuSuaDoiThongTin
                    WHERE date(DateInsert) >= date('now', '-' || (@soNgay - 1) || ' day')
                    ORDER BY datetime(DateInsert) DESC, id DESC;";

                    using (var conn = new SQLiteConnection(GetStringConnector))
                    using (var cmd = new SQLiteCommand(sql, conn))
                    using (var da = new SQLiteDataAdapter(cmd))
                    {
                        cmd.Parameters.AddWithValue("@soNgay", soNgay);

                        var result = new DataTable();
                        conn.Open();
                        da.Fill(result);
                        return result;
                    }
                });


                Console.WriteLine(dt);

                BindGridLichSuSuaDoi(dt, dtView);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Không tải được lịch sử sửa đổi.\n" + ex.Message,
                    "Thông báo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
            finally
            {
                waiting?.SafeClose();
            }
        }

        private static void BindGridLichSuSuaDoi(DataTable dt, DataGridView grv)
        {
            grv.AutoGenerateColumns = false;
            grv.DataSource = dt;

            string[] colsSo = { "KlCu", "KlMoi", "cdCu", "cdMoi" };

            foreach (string col in colsSo)
            {
                if (grv.Columns.Contains(col))
                {
                    grv.Columns[col].DefaultCellStyle.Format = "N2";
                    grv.Columns[col].DefaultCellStyle.FormatProvider = new CultureInfo("en-US");
                    grv.Columns[col].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                }
            }
        }

        #endregion


        #region Vật tư phụ - Tìm kiếm đơn & load chi tiết

        public static DataTable TinhTonKho(DateTime? ngayBatDau, DateTime? ngayKetThuc, int? idKho = null)
        {
            bool coLocNgay = ngayBatDau.HasValue && ngayKetThuc.HasValue;

            string khoFilter = (idKho.HasValue && idKho.Value > 0)
                ? "AND lx.DanhSachKho_ID = @IdKho" : "";

            // Khi không lọc ngày: TonDauKy = 0, toàn bộ phát sinh nằm trong PhatSinhKy
            string tonDauKyNgayFilter = coLocNgay ? "WHERE lx.Ngay < @NgayBatDau" : "WHERE 1=0";
            string phatSinhNgayFilter = coLocNgay ? "AND lx.Ngay >= @NgayBatDau AND lx.Ngay <= @NgayKetThuc" : "";

            string sql = $@"
                WITH
                TonDauKy AS (
                    SELECT
                        lx.DanhSachKho_ID,
                        ttdh.DanhSachMaSP_ID,
                        SUM(lx.SoLuong) AS TonDau
                    FROM LichSuXuatNhap lx
                    INNER JOIN ThongTinDatHang ttdh ON ttdh.id = lx.ThongTinDatHang_ID
                    {tonDauKyNgayFilter}
                      AND lx.DanhSachKho_ID IS NOT NULL
                      AND lx.ThongTinDatHang_ID IS NOT NULL
                      {khoFilter}
                    GROUP BY lx.DanhSachKho_ID, ttdh.DanhSachMaSP_ID
                ),
                PhatSinhKy AS (
                    SELECT
                        lx.DanhSachKho_ID,
                        ttdh.DanhSachMaSP_ID,
                        SUM(CASE WHEN lx.SoLuong > 0 THEN  lx.SoLuong ELSE 0 END) AS TongNhap,
                        SUM(CASE WHEN lx.SoLuong < 0 THEN -lx.SoLuong ELSE 0 END) AS TongXuat
                    FROM LichSuXuatNhap lx
                    INNER JOIN ThongTinDatHang ttdh ON ttdh.id = lx.ThongTinDatHang_ID
                    WHERE lx.DanhSachKho_ID IS NOT NULL
                      AND lx.ThongTinDatHang_ID IS NOT NULL
                      {phatSinhNgayFilter}
                      {khoFilter}
                    GROUP BY lx.DanhSachKho_ID, ttdh.DanhSachMaSP_ID
                ),
                AllKeys AS (
                    SELECT DanhSachKho_ID, DanhSachMaSP_ID FROM TonDauKy
                    UNION
                    SELECT DanhSachKho_ID, DanhSachMaSP_ID FROM PhatSinhKy
                )
                SELECT
                    sp.Ten                                                AS TenVatTu,
                    sp.Ma                                                AS MaVatTu,
                    kho.TenKho                                            AS TenKho,
                    COALESCE(tdk.TonDau,   0)                             AS TonDauKy,
                    COALESCE(psk.TongNhap, 0)                             AS TongNhap,
                    COALESCE(psk.TongXuat, 0)                             AS TongXuat,
                    COALESCE(tdk.TonDau, 0) + COALESCE(psk.TongNhap, 0)
                                            - COALESCE(psk.TongXuat, 0)   AS TonCuoiKy
                FROM AllKeys ak
                INNER JOIN DanhSachMaSP sp  ON sp.id  = ak.DanhSachMaSP_ID
                INNER JOIN DanhSachKho  kho ON kho.id = ak.DanhSachKho_ID
                LEFT  JOIN TonDauKy    tdk  ON tdk.DanhSachMaSP_ID = ak.DanhSachMaSP_ID
                                           AND tdk.DanhSachKho_ID  = ak.DanhSachKho_ID
                LEFT  JOIN PhatSinhKy  psk  ON psk.DanhSachMaSP_ID = ak.DanhSachMaSP_ID
                                           AND psk.DanhSachKho_ID  = ak.DanhSachKho_ID
                ORDER BY kho.TenKho, sp.Ten;
            ";

            var dt = new DataTable();
            dt.Columns.Add("Tên Vật Tư", typeof(string));
            dt.Columns.Add("Mã Vật Tư", typeof(string));
            dt.Columns.Add("Tên Kho", typeof(string));
            dt.Columns.Add("Tồn Đầu Kỳ", typeof(decimal));
            dt.Columns.Add("Tổng Nhập", typeof(decimal));
            dt.Columns.Add("Tổng Xuất", typeof(decimal));
            dt.Columns.Add("Tồn Cuối Kỳ", typeof(decimal));

            using var connection = new SQLiteConnection(_connStr);
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = sql;

            if (coLocNgay)
            {
                command.Parameters.AddWithValue("@NgayBatDau", ngayBatDau!.Value.ToString("yyyy-MM-dd"));
                command.Parameters.AddWithValue("@NgayKetThuc", ngayKetThuc!.Value.ToString("yyyy-MM-dd 23:59:59"));
            }
            if (idKho.HasValue && idKho.Value > 0)
                command.Parameters.AddWithValue("@IdKho", idKho.Value);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                dt.Rows.Add(
                    reader.IsDBNull(0) ? string.Empty : reader.GetString(0),
                    reader.IsDBNull(0) ? string.Empty : reader.GetString(1),
                    reader.IsDBNull(1) ? string.Empty : reader.GetString(2),
                    reader.IsDBNull(2) ? 0m : reader.GetDecimal(3),
                    reader.IsDBNull(3) ? 0m : reader.GetDecimal(4),
                    reader.IsDBNull(4) ? 0m : reader.GetDecimal(5),
                    reader.IsDBNull(5) ? 0m : reader.GetDecimal(6)
                );
            }

            return dt;
        }



        public static DataTable LoadLichSuXuatNhap_LoaiDon1(bool i,string maDon = null,long? khoId = null)
        {
            using var conn = new SQLiteConnection(_connStr);
            conn.Open();

            string sql = @"
                SELECT
                    d.id AS DanhSachDatHang_ID,
                    d.LoaiDon AS LoaiDon,

                    k.TenKho AS TenKho,

                    sp.Ma AS MaHang,
                    t.TenVatTu AS TenVatTu,
                    sp.DonVi AS DonVi,

                    COALESCE(SUM(CASE WHEN l.SoLuong > 0 THEN l.SoLuong ELSE 0 END), 0) AS Nhap,
                    COALESCE(SUM(CASE WHEN l.SoLuong < 0 THEN ABS(l.SoLuong) ELSE 0 END), 0) AS Xuat,
                    COALESCE(SUM(l.SoLuong), 0) AS Ton

                FROM DanhSachDatHang d
                INNER JOIN ThongTinDatHang t
                    ON t.DanhSachDatHang_ID = d.id
                LEFT JOIN DanhSachMaSP sp
                    ON sp.id = t.DanhSachMaSP_ID
                LEFT JOIN LichSuXuatNhap l
                    ON l.ThongTinDatHang_ID = t.id
                LEFT JOIN DanhSachKho k
                    ON k.id = l.DanhSachKho_ID

                WHERE d.LoaiDon = 1
            ";

            if (!string.IsNullOrWhiteSpace(maDon))
                sql += " AND d.MaDon = @MaDon ";

            if (khoId.HasValue)
                sql += " AND k.id = @KhoId ";

            sql += @"
                GROUP BY
                    d.id,
                    d.MaDon,
                    d.LoaiDon,
                    t.id,
                    sp.Ma,
                    t.TenVatTu,
                    sp.DonVi,
                    k.id,
                    k.KiHieu,
                    k.TenKho,
                    t.SoLuongMua
                HAVING COALESCE(SUM(l.SoLuong), 0) != 0
                ORDER BY d.DateInsert DESC, d.MaDon, t.id;
            ";

            using var cmd = new SQLiteCommand(sql, conn);

            if (!string.IsNullOrWhiteSpace(maDon))
                cmd.Parameters.AddWithValue("@MaDon", maDon.Trim());

            if (khoId.HasValue)
                cmd.Parameters.AddWithValue("@KhoId", khoId.Value);

            using var da = new SQLiteDataAdapter(cmd);
            var dt = new DataTable();
            da.Fill(dt);

            return dt;
        }

        public static void DeleteDanhSachDatHang(int id)
        {
            const string sqlPragma = "PRAGMA foreign_keys = ON;";
            const string sqlDelete = "DELETE FROM DanhSachDatHang WHERE id = @Id;";

            using var conn = new SQLiteConnection(_connStr);
            conn.Open();

            using (var pragmaCmd = new SQLiteCommand(sqlPragma, conn))
                pragmaCmd.ExecuteNonQuery();

            using var tx = conn.BeginTransaction();
            try
            {
                using var cmd = new SQLiteCommand(sqlDelete, conn, tx);
                cmd.Parameters.AddWithValue("@Id", id);

                int affected = cmd.ExecuteNonQuery();
                if (affected == 0)
                    throw new Exception($"Không tìm thấy DanhSachDatHang ID = {id} để xóa.");

                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        public static void DeleteLichSuXuatNhap(int id)
        {
            const string sqlDelete = "DELETE FROM LichSuXuatNhap WHERE id = @Id;";

            using var conn = new SQLiteConnection(_connStr);
            conn.Open();

            using var tx = conn.BeginTransaction();
            try
            {
                using var cmd = new SQLiteCommand(sqlDelete, conn, tx);
                cmd.Parameters.AddWithValue("@Id", id);

                int affected = cmd.ExecuteNonQuery();
                if (affected == 0)
                    throw new Exception($"Không tìm thấy LichSuXuatNhap ID = {id} để xóa.");

                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        public static void UpdateLichSuXuatNhap(LichSuXuatNhapUpdateModel model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            const string sql = @"
                UPDATE LichSuXuatNhap
                SET
                    SoLuong        = @SoLuong,
                    NguoiGiao_Nhan = @NguoiGiaoNhan,
                    Kho            = @Kho,
                    LyDo           = @LyDo,
                    Ngay           = @Ngay,
                    TenPhieu       = @TenPhieu,
                    GhiChu         = @GhiChu
                WHERE id = @Id;
            ";

            using var conn = new SQLiteConnection(_connStr);
            conn.Open();
            using var tx = conn.BeginTransaction();

            try
            {
                using var cmd = new SQLiteCommand(sql, conn, tx);
                cmd.Parameters.AddWithValue("@Id", model.Id);
                cmd.Parameters.AddWithValue("@SoLuong", model.SoLuong);
                cmd.Parameters.AddWithValue("@NguoiGiaoNhan", string.IsNullOrWhiteSpace(model.NguoiGiaoNhan) ? (object)DBNull.Value : model.NguoiGiaoNhan.Trim());
                cmd.Parameters.AddWithValue("@Kho", string.IsNullOrWhiteSpace(model.Kho) ? (object)DBNull.Value : model.Kho.Trim());
                cmd.Parameters.AddWithValue("@LyDo", string.IsNullOrWhiteSpace(model.LyDo) ? (object)DBNull.Value : model.LyDo.Trim());
                cmd.Parameters.AddWithValue("@Ngay", string.IsNullOrWhiteSpace(model.Ngay) ? (object)DBNull.Value : model.Ngay.Trim());
                cmd.Parameters.AddWithValue("@TenPhieu", string.IsNullOrWhiteSpace(model.TenPhieu) ? (object)DBNull.Value : model.TenPhieu.Trim());
                cmd.Parameters.AddWithValue("@GhiChu", string.IsNullOrWhiteSpace(model.GhiChu) ? (object)DBNull.Value : model.GhiChu.Trim());

                int affected = cmd.ExecuteNonQuery();
                if (affected == 0)
                    throw new Exception($"Không tìm thấy LichSuXuatNhap ID = {model.Id}.");

                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        public static void UpdateThongTinDatHang(ThongTinDatHangUpdateModel model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            const string sql = @"
                UPDATE ThongTinDatHang
                SET
                    TenVatTu   = @TenVatTu,
                    SoLuongMua = @SoLuongMua,
                    DonGia     = @DonGia,
                    MucDichMua = @MucDichMua,
                    NgayGiao   = @NgayGiao
                WHERE id = @Id;
            ";

            using var conn = new SQLiteConnection(_connStr);
            conn.Open();
            using var tx = conn.BeginTransaction();

            try
            {
                using var cmd = new SQLiteCommand(sql, conn, tx);
                cmd.Parameters.AddWithValue("@Id", model.Id);
                cmd.Parameters.AddWithValue("@TenVatTu", model.TenVatTu ?? "");
                cmd.Parameters.AddWithValue("@SoLuongMua", model.SoLuongMua);
                cmd.Parameters.AddWithValue("@DonGia", model.DonGia);
                cmd.Parameters.AddWithValue("@MucDichMua", string.IsNullOrWhiteSpace(model.MucDichMua) ? (object)DBNull.Value : model.MucDichMua.Trim());
                cmd.Parameters.AddWithValue("@NgayGiao", string.IsNullOrWhiteSpace(model.NgayGiao) ? (object)DBNull.Value : model.NgayGiao.Trim());
                cmd.Parameters.AddWithValue("@Id", model.Id);

                int affected = cmd.ExecuteNonQuery();
                if (affected == 0)
                    throw new Exception($"Không tìm thấy ThongTinDatHang ID = {model.Id}.");

                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        public static void UpdateCanEdit(DataGridView dgr, string clCheck)
        {
            using var conn = new SQLiteConnection(_connStr);
            conn.Open();
            using var transaction = conn.BeginTransaction();
            try
            {
                const string sql = @"
                    UPDATE LichSuXuatNhap 
                    SET CanEdit = @CanEdit 
                    WHERE id = @Id
                ";

                using var cmd = new SQLiteCommand(sql, conn, transaction);
                cmd.Parameters.Add("@CanEdit", DbType.Int32);
                cmd.Parameters.Add("@Id", DbType.Int32);

                foreach (DataGridViewRow row in dgr.Rows)
                {
                    if (!dgr.Columns.Contains("colChon")) break;

                    var idVal = row.Cells["lsxn_id"].Value;
                    if (idVal == null || idVal == DBNull.Value) continue;  

                    bool isChecked = row.Cells[clCheck].Value != null
                        && Convert.ToBoolean(row.Cells[clCheck].Value);

                    cmd.Parameters["@CanEdit"].Value = isChecked ? 0 : 1;
                    cmd.Parameters["@Id"].Value = Convert.ToInt32(idVal);
                    cmd.ExecuteNonQuery();
                }

                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                throw new Exception($"UpdateCanEdit lỗi: {ex.Message}", ex);
            }
        }

        public static DataTable GetBaoCao(string ngayBatDau, string ngayKetThuc, int kho, int tinhTrang)
        {
            DataTable dt = new DataTable();

            try
            {
                StringBuilder sql = new StringBuilder();

                sql.AppendLine(@"
                    SELECT 
                        dsdh.id             AS DonHang_ID,
                        dsdh.MaDon,
                        dsdh.LoaiDon,
                        dsdh.DateInsert     AS NgayTaoDon,
                        dsdh.NguoiDat,

                        ttdh.id             AS ThongTinDatHang_ID,
                        ttdh.TenVatTu,
                        ttdh.SoLuongMua AS SL_YeuCau,
                        ttdh.DonGia,
                        ttdh.MucDichMua,
                        ttdh.NgayGiao,
                        ttdh.Date_Insert    AS NgayNhapChiTiet,

                        lsxn.id             AS LichSu_ID,
                        lsxn.SoLuong,
                        lsxn.NguoiGiao_Nhan,
                        kho.TenKho,
                        lsxn.LyDo,
                        lsxn.Ngay           AS NgayXuatNhap,
                        lsxn.TenPhieu,
                        lsxn.GhiChu,
                        lsxn.CanEdit        AS Edit
                        
                    FROM DanhSachDatHang dsdh
                    INNER JOIN ThongTinDatHang ttdh 
                        ON ttdh.DanhSachDatHang_ID = dsdh.id

                    LEFT JOIN LichSuXuatNhap lsxn 
                        ON lsxn.ThongTinDatHang_ID = ttdh.id

                    INNER JOIN DanhSachKho Kho 
                        ON Kho.id = lsxn.DanhSachKho_ID

                    WHERE 1=1
                ");

                List<SQLiteParameter> parameters = new List<SQLiteParameter>();

                // --- Lọc theo ngày (trên bảng DanhSachDatHang.DateInsert) ---
                if (!string.IsNullOrEmpty(ngayBatDau) && !string.IsNullOrEmpty(ngayKetThuc))
                {
                    sql.AppendLine("AND dsdh.DateInsert >= @NgayBatDau");
                    sql.AppendLine("AND dsdh.DateInsert <= @NgayKetThuc");
                    parameters.Add(new SQLiteParameter("@NgayBatDau", ngayBatDau));
                    parameters.Add(new SQLiteParameter("@NgayKetThuc", ngayKetThuc));
                }

                // --- Lọc theo Kho (nằm trong LichSuXuatNhap) ---
                if (kho != 0)
                {
                    sql.AppendLine("AND lsxn.Kho = @Kho");
                    parameters.Add(new SQLiteParameter("@Kho", kho));
                }

                // --- Lọc theo tình trạng ---
                switch (tinhTrang)
                {
                    case 0:
                        // Không gán điều kiện, lấy tất cả
                        break;

                    case 1:
                        // Có trong DanhSachDatHang nhưng KHÔNG có trong LichSuXuatNhap
                        sql.AppendLine(@"
                    AND ttdh.id NOT IN (
                        SELECT DISTINCT ThongTinDatHang_ID 
                        FROM LichSuXuatNhap 
                        WHERE ThongTinDatHang_ID IS NOT NULL
                    )
                ");
                        break;

                    case 2:
                        // SoLuongMua khác TongSoLuong (lớn hơn hoặc nhỏ hơn)
                        sql.AppendLine(@"
                    AND ttdh.SoLuongMua != COALESCE((
                        SELECT SUM(sl.SoLuong)
                        FROM LichSuXuatNhap sl
                        WHERE sl.ThongTinDatHang_ID = ttdh.id
                    ), 0)
                ");
                        break;

                    case 3:
                        // SoLuongMua = TongSoLuong
                        sql.AppendLine(@"
                    AND ttdh.SoLuongMua = COALESCE((
                        SELECT SUM(sl.SoLuong)
                        FROM LichSuXuatNhap sl
                        WHERE sl.ThongTinDatHang_ID = ttdh.id
                    ), 0)
                ");
                        break;
                }

                sql.AppendLine("ORDER BY dsdh.DateInsert DESC, dsdh.id DESC");

                using (SQLiteConnection conn = new SQLiteConnection(_connStr))
                {
                    conn.Open();
                    using (SQLiteCommand cmd = new SQLiteCommand(sql.ToString(), conn))
                    {
                        cmd.Parameters.AddRange(parameters.ToArray());
                        using (SQLiteDataAdapter adapter = new SQLiteDataAdapter(cmd))
                        {
                            adapter.Fill(dt);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"GetBaoCao lỗi: {ex.Message}", ex);
            }

            return dt;
        }


        /// <summary>
        /// cbxKieu = 1 — Lấy dữ liệu từ DanhSachDatHang + ThongTinDatHang theo bộ lọc.
        /// Bỏ qua điều kiện nếu tham số rỗng; bỏ qua tinhTrang nếu = 0.
        /// </summary>
        public static DataTable GetBaoCaoDatHang(string ngayBatDau, string ngayKetThuc, string nguoiThucHien)
        {
            var dt = new DataTable();
            try
            {
                var sql = new StringBuilder(@"
                    SELECT
                        dsdh.id             AS DanhSachDatHang_ID,
                        dsdh.nguoiDat,
                        dsdh.MaDon,
                        dsdh.LoaiDon,
                        dsdh.DateInsert     AS NgayTaoDon,
                        dsdh.NguoiDat,
                        ttdh.id             AS ThongTinDatHang_ID,
                        ttdh.TenVatTu,
                        ttdh.SoLuongMua,
                        ttdh.DonGia,
                        ttdh.MucDichMua,
                        ttdh.NgayGiao,
                        ttdh.Date_Insert    AS NgayNhapChiTiet,
                        CASE
                            WHEN EXISTS (
                                SELECT 1
                                FROM LichSuXuatNhap lsxn
                                INNER JOIN ThongTinDatHang t
                                    ON t.id = lsxn.ThongTinDatHang_ID
                                WHERE t.DanhSachDatHang_ID = dsdh.id
                            ) THEN 0
                            ELSE 1
                        END                 AS Edit
                    FROM DanhSachDatHang dsdh
                    INNER JOIN ThongTinDatHang ttdh
                        ON ttdh.DanhSachDatHang_ID = dsdh.id
                    WHERE 1=1
                ");

                var parameters = new List<SQLiteParameter>();

                // Lọc theo ngày tạo đơn
                if (!string.IsNullOrWhiteSpace(ngayBatDau) && !string.IsNullOrWhiteSpace(ngayKetThuc))
                {
                    sql.AppendLine("AND dsdh.DateInsert >= @NgayBatDau");
                    sql.AppendLine("AND dsdh.DateInsert <= @NgayKetThuc");
                    parameters.Add(new SQLiteParameter("@NgayBatDau", ngayBatDau));
                    parameters.Add(new SQLiteParameter("@NgayKetThuc", ngayKetThuc));
                }

                sql.AppendLine("ORDER BY dsdh.DateInsert DESC, dsdh.id DESC, ttdh.id;");

                using (var conn = new SQLiteConnection(_connStr))
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand(sql.ToString(), conn))
                    {
                        cmd.Parameters.AddRange(parameters.ToArray());
                        using (var adapter = new SQLiteDataAdapter(cmd))
                            adapter.Fill(dt);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"GetBaoCaoDatHang lỗi: {ex.Message}", ex);
            }
            return dt;
        }

        public static DataTable GetBaoCaoDatHang_v2(string nguoiThucHien, DateTime? ngayBatDau = null, DateTime? ngayKetThuc = null)
        {
            var dt = new DataTable();
            try
            {
                var sql = new StringBuilder(@"
                    SELECT 
                        dh.id AS dh_id,
                        dh.MaDon AS dh_MaDon,
                        dh.NguoiDat AS dh_NguoiDat,
                        tt.TenVatTu AS tt_TenVatTu,
                        tt.SoLuongMua AS tt_SoLuongMua,
                        tt.MucDichMua AS tt_MucDichMua,
                        strftime('%d/%m/%Y', tt.NgayGiao) AS NgayGiao,
                        strftime('%d/%m/%Y', tt.Date_Insert) AS Date_Insert,
                        tt.DonGia AS tt_DonGia
                    FROM ThongTinDatHang tt
                    INNER JOIN DanhSachDatHang dh 
                        ON tt.DanhSachDatHang_ID = dh.id
                    LEFT JOIN DanhSachMaSP sp 
                        ON tt.DanhSachMaSP_ID = sp.id
                    WHERE 1 = 1
                ");
                var parameters = new List<SQLiteParameter>();

                if (!string.IsNullOrWhiteSpace(nguoiThucHien))
                {
                    sql.AppendLine(" AND dh.NguoiDat = @NguoiDat ");
                    parameters.Add(new SQLiteParameter("@NguoiDat", nguoiThucHien.Trim()));
                }
                if (ngayBatDau.HasValue)
                {
                    sql.AppendLine(" AND DATE(dh.DateInsert) >= DATE(@NgayBatDau) ");
                    parameters.Add(new SQLiteParameter("@NgayBatDau", ngayBatDau.Value.ToString("yyyy-MM-dd")));
                }
                if (ngayKetThuc.HasValue)
                {
                    sql.AppendLine(" AND DATE(dh.DateInsert) <= DATE(@NgayKetThuc) ");
                    parameters.Add(new SQLiteParameter("@NgayKetThuc", ngayKetThuc.Value.ToString("yyyy-MM-dd")));
                }

                sql.AppendLine(" ORDER BY dh.DateInsert DESC, dh.id DESC, tt.id DESC ");
                using (var conn = new SQLiteConnection(_connStr))
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand(sql.ToString(), conn))
                    {
                        cmd.Parameters.AddRange(parameters.ToArray());
                        using (var adapter = new SQLiteDataAdapter(cmd))
                        {
                            adapter.Fill(dt);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"GetBaoCaoDatHang lỗi: {ex.Message}", ex);
            }
            return dt;
        }




        /// <summary>
        /// cbxKieu = 2  →  soLuongDuong = true  (SoLuong > 0, tức là Nhập kho)
        /// cbxKieu = 3  →  soLuongDuong = false (SoLuong &lt; 0, tức là Xuất kho)
        /// Bỏ qua điều kiện nếu tham số rỗng; bỏ qua tinhTrang nếu = 0.
        /// </summary>
        public static DataTable GetBaoCaoLichSuXuatNhap(
            string ngayBatDau, string ngayKetThuc, int kho, int tinhTrang,string nguoiThucHien,
            bool soLuongDuong)
        {
            var dt = new DataTable();
            try
            {
                var sql = new StringBuilder(@"
                    SELECT
                        lsxn.id             AS LichSu_ID,
                        dsdh.MaDon,
                        dsdh.DateInsert     AS NgayTaoDon,

                        ttdh.id             AS ThongTinDatHang_ID,
                        ttdh.TenVatTu,
                        ttdh.SoLuongMua,
                        ttdh.DonGia,
                        ttdh.MucDichMua,

                        lsxn.NguoiLam,
                        lsxn.SoLuong,
                        lsxn.NguoiGiao_Nhan,
                        kho.TenKho,
                        lsxn.LyDo,
                        lsxn.Ngay           AS NgayXuatNhap,
                        lsxn.TenPhieu,
                        lsxn.GhiChu,
                        lsxn.CanEdit        AS Edit,
                        lsxn.DonGia         AS DonGiaPhieu
                    FROM LichSuXuatNhap lsxn
                    INNER JOIN ThongTinDatHang ttdh
                        ON ttdh.id = lsxn.ThongTinDatHang_ID
                    INNER JOIN DanhSachDatHang dsdh
                        ON dsdh.id = ttdh.DanhSachDatHang_ID
                    INNER JOIN DanhSachKho kho
                        ON kho.id = lsxn.DanhSachKho_ID
                    WHERE
                ");

                // Điều kiện cốt lõi: hướng xuất / nhập
                sql.AppendLine(soLuongDuong ? "lsxn.SoLuong > 0" : "lsxn.SoLuong < 0");

                var parameters = new List<SQLiteParameter>();

                // Lọc theo ngày giao dịch (Ngay của LichSuXuatNhap)
                if (!string.IsNullOrWhiteSpace(ngayBatDau) && !string.IsNullOrWhiteSpace(ngayKetThuc))
                {
                    sql.AppendLine("AND lsxn.Ngay >= @NgayBatDau");
                    sql.AppendLine("AND lsxn.Ngay <= @NgayKetThuc");
                    parameters.Add(new SQLiteParameter("@NgayBatDau", ngayBatDau));
                    parameters.Add(new SQLiteParameter("@NgayKetThuc", ngayKetThuc));
                }


                // Lọc theo ngày giao dịch (Ngay của LichSuXuatNhap)
                if (!string.IsNullOrWhiteSpace(nguoiThucHien))
                {
                    sql.AppendLine("AND lsxn.NguoiLam = @NguoiLam");
                    parameters.Add(new SQLiteParameter("@NguoiLam", nguoiThucHien));
                }

                // Lọc theo kho
                if (kho != 0)
                {
                    sql.AppendLine("AND kho.id = @Kho");
                    parameters.Add(new SQLiteParameter("@Kho", kho));
                }

                // Lọc theo tình trạng (bỏ qua nếu = 0)
                if (tinhTrang != 0)
                {
                    switch (tinhTrang)
                    {
                        case 1:
                            sql.AppendLine("AND lsxn.CanEdit = 1");
                            break;
                        case 2:
                            sql.AppendLine("AND lsxn.CanEdit = 0");
                            break;
                        case 3:
                            // Mở rộng thêm case khác nếu cần
                            break;
                    }
                }

                sql.AppendLine("ORDER BY lsxn.Ngay DESC, lsxn.id DESC;");

                using (var conn = new SQLiteConnection(_connStr))
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand(sql.ToString(), conn))
                    {
                        cmd.Parameters.AddRange(parameters.ToArray());
                        using (var adapter = new SQLiteDataAdapter(cmd))
                            adapter.Fill(dt);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"GetBaoCaoLichSuXuatNhap lỗi: {ex.Message}", ex);
            }
            return dt;
        }

        public static DataTable GetBaoCaoLichSuXuatNhap_v2(int kho, string nguoiThucHien, bool isNhap, DateTime? ngayBatDau = null, DateTime? ngayKetThuc = null)
        {
            var dt = new DataTable();
            try
            {
                var sql = new StringBuilder(@"
                    SELECT
                        lsxn.id AS lsxn_id,
                        strftime('%d/%m/%Y', lsxn.Ngay) AS NgayXuatNhap,
                        lsxn.TenPhieu AS TenPhieu,
                        lsxn.LyDo AS LyDo,
                        dssp.Ma AS Ma,
                        CASE 
                            WHEN ttdh.DanhSachMaSP_ID IS NULL 
                                THEN ttdh.TenVatTu
                            ELSE dssp.Ten
                        END AS TenVatTu,
                        lsxn.SoLuong AS SoLuong,
                        lsxn.DonGia AS DonGia,
                        dsk.TenKho AS TenKho,
                        ncc.TenNcc AS NCC,
                        lsxn.GhiChu AS GhiChu,
                        dsdh.MaDon AS MaDon,
                        dsdh.NguoiDat AS NguoiDat,
                        lsxn.NguoiGiao_Nhan AS NguoiGiaoNhan,
                        strftime('%d/%m/%Y', dsdh.DateInsert) AS NgayDatPR,
                        lsxn.NguoiLam AS NguoiLam,
                        ttdh.SoLuongMua AS SoLuongMua,
                        ttdh.MucDichMua AS MucDichMua,
                        strftime('%d/%m/%Y', ttdh.NgayGiao) AS NgayGiao,
                        strftime('%d/%m/%Y', ttdh.Date_Insert) AS Date_Insert,
                        lsxn.canEdit AS canEdit,
                        ttdh.DonGia AS DonGia
                    FROM LichSuXuatNhap lsxn
                    INNER JOIN ThongTinDatHang ttdh
                        ON lsxn.ThongTinDatHang_ID = ttdh.id
                    INNER JOIN DanhSachDatHang dsdh
                        ON ttdh.DanhSachDatHang_ID = dsdh.id
                    LEFT JOIN DanhSachMaSP dssp
                        ON ttdh.DanhSachMaSP_ID = dssp.id
                    LEFT JOIN DanhSachKho dsk
                        ON lsxn.DanhSachKho_ID = dsk.id
                    LEFT JOIN DanhSachNcc ncc
                        ON lsxn.DanhSachNcc_ID = ncc.id
                    WHERE 1 = 1
                ");
                var parameters = new List<SQLiteParameter>();

                if (kho > 0)
                {
                    sql.AppendLine(" AND lsxn.DanhSachKho_ID = @DanhSachKho_ID ");
                    parameters.Add(new SQLiteParameter("@DanhSachKho_ID", kho));
                }
                if (!string.IsNullOrWhiteSpace(nguoiThucHien))
                {
                    sql.AppendLine(" AND lsxn.NguoiLam = @NguoiLam ");
                    parameters.Add(new SQLiteParameter("@NguoiLam", nguoiThucHien.Trim()));
                }
                if (ngayBatDau.HasValue)
                {
                    sql.AppendLine(" AND DATE(lsxn.Ngay) >= DATE(@NgayBatDau) ");
                    parameters.Add(new SQLiteParameter("@NgayBatDau", ngayBatDau.Value.ToString("yyyy-MM-dd")));
                }
                if (ngayKetThuc.HasValue)
                {
                    sql.AppendLine(" AND DATE(lsxn.Ngay) <= DATE(@NgayKetThuc) ");
                    parameters.Add(new SQLiteParameter("@NgayKetThuc", ngayKetThuc.Value.ToString("yyyy-MM-dd")));
                }
                if (isNhap)
                    sql.AppendLine(" AND lsxn.SoLuong > 0 ");
                else
                    sql.AppendLine(" AND lsxn.SoLuong < 0 ");

                sql.AppendLine(" ORDER BY lsxn.Ngay DESC, lsxn.id DESC; ");
                using (var conn = new SQLiteConnection(_connStr))
                {
                    Console.WriteLine( sql.ToString() );
                    conn.Open();
                    using (var cmd = new SQLiteCommand(sql.ToString(), conn))
                    {
                        cmd.Parameters.AddRange(parameters.ToArray());
                        using (var adapter = new SQLiteDataAdapter(cmd))
                        {
                            adapter.Fill(dt);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"GetBaoCaoLichSuXuatNhap lỗi: {ex.Message}", ex);
            }
            return dt;
        }

        public static async Task<List<string>> TimKiemMaDon(string keyword, bool isEdit)
        {
            string sql = isEdit
            ? @"
            SELECT DISTINCT l.TenPhieu as MaDon
            FROM LichSuXuatNhap as l                    
            WHERE (
                l.canedit = 1 
                and l.TenPhieu LIKE @kw   COLLATE NOCASE 
            )
            LIMIT 30"
                : 
            @"
            SELECT MaDon
            FROM DanhSachDatHang
            WHERE MaDon LIKE @kw
            ORDER BY DateInsert DESC
            LIMIT 30";

            var result = new List<string>();

            using var conn = new SQLiteConnection(_connStr);
            await conn.OpenAsync();

            using var cmd = new SQLiteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@kw", $"%{keyword}%");

            using var rd = await cmd.ExecuteReaderAsync();
            while (await rd.ReadAsync())
                result.Add(rd["MaDon"]?.ToString());

            return result;
        }


        public static async Task<DataTable> LayChiTietDonDatHang(string maDon, bool isEdit, bool isNhap = true)
        {
            string sql = @"
            SELECT
                t.id          AS id,
                d.MaDon        AS MaDon,
                t.TenVatTu        AS ten,
                sp.Ma         AS ma,
                sp.DonVi      AS donVi,
                (t.SoLuongMua - IFNULL(ls.TongSoLuong, 0)) AS yeuCau
            FROM ThongTinDatHang t
            INNER JOIN DanhSachDatHang d ON d.id = t.DanhSachDatHang_ID
            LEFT JOIN DanhSachMaSP sp ON sp.id = t.DanhSachMaSP_ID
            LEFT JOIN (
                SELECT 
                    ThongTinDatHang_ID,
                    SUM(SoLuong) AS TongSoLuong
                FROM LichSuXuatNhap
                WHERE SoLuong > 0
                GROUP BY ThongTinDatHang_ID
            ) ls ON ls.ThongTinDatHang_ID = t.id
            WHERE d.MaDon = @maDon
              AND t.SoLuongMua > IFNULL(ls.TongSoLuong, 0)
            ORDER BY t.id";



            if (isEdit)
            {
                sql = @"
                    SELECT
                        l.id           AS id,
                        l.TenPhieu     AS MaDon,
                        t.TenVatTu     AS ten,
                        sp.Ma          AS ma,
                        sp.DonVi       AS donVi, 
                        ABS(l.soluong) as thucNhan,
                        l.donGia       as dongia,
                        l.SoLuong * l.donGia AS thanhTien,
                        l.ngay         as ngay,
                        l.Ghichu       as GhiChu,

                        ncc.id         AS idNcc,
                        ncc.TenNCC     AS NhaCungCap

                    FROM LichSuXuatNhap l

                    LEFT JOIN ThongTinDatHang t 
                        ON t.id = l.ThongTinDatHang_ID

                    INNER JOIN DanhSachDatHang d 
                        ON d.id = t.DanhSachDatHang_ID

                    LEFT JOIN DanhSachMaSP sp 
                        ON sp.id = t.DanhSachMaSP_ID

                    LEFT JOIN DanhSachNCC ncc
                        ON ncc.id = l.DanhSachNCC_ID
            
                    WHERE l.TenPhieu = @maDon 
                        AND l.CanEdit = 1 
                        AND (
                            (l.SoLuong > 0 AND @isNhap = 1) 
                            OR 
                            (l.SoLuong < 0 AND @isNhap = 0)
                        )
                ";
            }


            Console.WriteLine("LayChiTietDonDatHang");
            Console.WriteLine(maDon);
            Console.WriteLine(isNhap);
            Console.WriteLine(sql);
            Console.WriteLine("========================");

            var dt = new DataTable();

            await Task.Run(() =>
            {
                using var conn = new SQLiteConnection(_connStr);
                conn.Open();
                using var cmd = new SQLiteCommand(sql, conn);
                cmd.Parameters.AddWithValue("@maDon", maDon);
                cmd.Parameters.AddWithValue("@isNhap", isNhap);

                using var adapter = new SQLiteDataAdapter(cmd);
                adapter.Fill(dt);
            });

            return dt;
        }

        public static async Task<List<string>> TimKiemTheoTenVatTu(string keyword, int kieu, bool isEdit, bool isKhac)
        {
            string keyWord_KhongDau = CoreHelper.BoDauTiengViet(keyword.Trim());

            string sql = $@"
                SELECT 
                    CASE 
                        WHEN t.DanhSachMaSP_ID IS NULL THEN t.TenVatTu
                        ELSE sp.Ten
                    END AS TenVatTu
                FROM ThongTinDatHang t
                JOIN DanhSachDatHang d ON t.DanhSachDatHang_ID = d.id
                LEFT JOIN DanhSachMaSP sp ON sp.id = t.DanhSachMaSP_ID
                LEFT JOIN (
                    SELECT ThongTinDatHang_ID, SUM(SoLuong) AS TongNhap
                    FROM LichSuXuatNhap
                    WHERE SoLuong > 0
                    GROUP BY ThongTinDatHang_ID
                ) l ON t.id = l.ThongTinDatHang_ID
                WHERE 
                    (
                        (t.DanhSachMaSP_ID IS NULL AND t.TenVatTu_KhongDau LIKE @kw COLLATE NOCASE)
                        OR
                        (t.DanhSachMaSP_ID IS NOT NULL AND sp.Ten_KhongDau LIKE @kw COLLATE NOCASE)
                    )
                    AND d.LoaiDon = @kieu
                GROUP BY 
                    CASE 
                        WHEN t.DanhSachMaSP_ID IS NULL THEN t.TenVatTu
                        ELSE sp.Ten
                    END
                HAVING SUM(IFNULL(l.TongNhap, 0)) < SUM(t.SoLuongMua)
                LIMIT 50;";

            if (isKhac)
            {
                sql = $@"
                SELECT t.Ten as TenVatTu
                FROM DanhSachMaSP t                
                WHERE t.Ten_KhongDau LIKE '%' || @kw || '%' COLLATE NOCASE
                ";
            }

            if (isEdit)
            {
                sql = $@"
                SELECT DISTINCT
                    CASE
                        WHEN ttdh.DanhSachMaSP_ID IS NULL THEN ttdh.TenVatTu
                        ELSE sp.Ten
                    END AS TenVatTu
                FROM ThongTinDatHang ttdh
                JOIN LichSuXuatNhap lsxn 
                    ON lsxn.ThongTinDatHang_ID = ttdh.id
                    AND lsxn.CanEdit = 1
                LEFT JOIN DanhSachMaSP sp 
                    ON sp.id = ttdh.DanhSachMaSP_ID
                WHERE
                    (
                        (ttdh.DanhSachMaSP_ID IS NULL AND ttdh.TenVatTu_KhongDau LIKE '%' || @kw || '%' COLLATE NOCASE)
                        OR
                        (ttdh.DanhSachMaSP_ID IS NOT NULL AND sp.Ten_KhongDau LIKE '%' || @kw || '%' COLLATE NOCASE)
                    );";
            }

            var result = new List<string>();

            using var conn = new SQLiteConnection(_connStr);
            await conn.OpenAsync();

            using var cmd = new SQLiteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@kw", $"%{keyWord_KhongDau}%");
            cmd.Parameters.AddWithValue("@kieu", kieu);

            using var rd = await cmd.ExecuteReaderAsync();
            while (await rd.ReadAsync())
            {
                result.Add(rd["TenVatTu"]?.ToString() ?? string.Empty);
            }

            return result;
        }

        public static async Task<DataTable> LayChiTietDonTheoTenVatTu(string tenVatTu, int kieu, bool isKhac)
        {
            string keyWord_KhongDau = CoreHelper.BoDauTiengViet(tenVatTu.Trim());

            string sql = @"
            SELECT
                t.id          AS id,
                t.TenVatTu    AS ten,
                d.MaDon       AS maDon,
                sp.Ma         AS ma,
                sp.DonVi      AS donVi,
                (t.SoLuongMua - IFNULL(ls.TongSoLuong, 0)) AS yeuCau,
                CASE 
                    WHEN @kieu = 1 THEN 0
                    WHEN @kieu = 2 THEN (t.SoLuongMua - IFNULL(ls.TongSoLuong, 0))
                END AS thucNhan
            FROM ThongTinDatHang t
            INNER JOIN DanhSachDatHang d ON d.id = t.DanhSachDatHang_ID
            LEFT JOIN DanhSachMaSP sp ON sp.id = t.DanhSachMaSP_ID
            LEFT JOIN (
                SELECT 
                    ThongTinDatHang_ID,
                    SUM(SoLuong) AS TongSoLuong
                FROM LichSuXuatNhap
                WHERE SoLuong > 0
                GROUP BY ThongTinDatHang_ID
            ) ls ON ls.ThongTinDatHang_ID = t.id
            WHERE
                (
                    (t.DanhSachMaSP_ID IS NULL AND t.TenVatTu_KhongDau = @tenVatTu COLLATE NOCASE)
                    OR
                    (t.DanhSachMaSP_ID IS NOT NULL AND sp.Ten_KhongDau = @tenVatTu COLLATE NOCASE)
                )
                AND t.SoLuongMua > IFNULL(ls.TongSoLuong, 0)
            ORDER BY t.id;";

            if (isKhac)
            {
                sql = @"
                SELECT
                    id      AS id,
                    Ten     AS ten,
                    ''      AS maDon,
                    Ma      AS ma,
                    DonVi   AS donVi,
                    ''      AS yeuCau,
                    ''      AS thucNhan
                FROM DanhSachMaSP
                WHERE Ten_KhongDau LIKE @tenVatTu COLLATE NOCASE
                ORDER BY Ten";
            }


            Console.WriteLine(sql);


            var dt = new DataTable();

            await Task.Run(() =>
            {
                using var conn = new SQLiteConnection(_connStr);
                conn.Open();
                using var cmd = new SQLiteCommand(sql, conn);
                cmd.Parameters.AddWithValue("@tenVatTu", keyWord_KhongDau);
                cmd.Parameters.AddWithValue("@kieu", kieu); 

                using var adapter = new SQLiteDataAdapter(cmd);
                adapter.Fill(dt);
            });

            return dt;
        }


        public static async Task<DataTable> GetDataTuTenVatTuXuatNhap_Edit( string tenVatTu, string nguoiLam, bool isNhapKho = true)
        {
            string keyWord_KhongDau = CoreHelper.BoDauTiengViet(tenVatTu.Trim());

            string sql = @"
                SELECT
                    ls.id           AS id,
                    t.TenVatTu      AS ten,
                    t.TenVatTu_KhongDau,
                    d.MaDon         AS maDon,
                    sp.Ma           AS ma,
                    sp.DonVi        AS donVi,
                    t.SoLuongMua    AS yeuCau,

                    CASE
                        WHEN @isNhapKho = 1 THEN ls.SoLuong
                        ELSE ABS(ls.SoLuong)
                    END AS thucNhan,

                    ls.DonGia       AS donGia,

                    ls.DonGia *
                    CASE
                        WHEN @isNhapKho = 1 THEN ls.SoLuong
                        ELSE ABS(ls.SoLuong)
                    END AS thanhTien,

                    ls.ngay         AS ngay,

                    ncc.id          AS nhaCungCapId,
                    ncc.tenNCC      AS NhaCungCap,

                    ls.GhiChu       AS GhiChu

                FROM ThongTinDatHang t

                INNER JOIN DanhSachDatHang d
                    ON d.id = t.DanhSachDatHang_ID

                LEFT JOIN DanhSachMaSP sp
                    ON sp.id = t.DanhSachMaSP_ID

                INNER JOIN LichSuXuatNhap ls
                    ON ls.ThongTinDatHang_ID = t.id

                LEFT JOIN DanhSachNCC ncc
                    ON ncc.id = ls.DanhSachNCC_ID 

                WHERE t.TenVatTu_KhongDau = @tenVatTu COLLATE NOCASE
                  AND ls.NguoiLam = @nguoiLam
                  AND ls.CanEdit = 1
                  AND (
                        (@isNhapKho = 1 AND ls.SoLuong > 0)
                        OR
                        (@isNhapKho = 0 AND ls.SoLuong < 0)
                      )

                ORDER BY t.id, ls.id;";

            DataTable dt = new DataTable();

            await Task.Run(() =>
            {
                using var conn = new SQLiteConnection(_connStr);
                conn.Open();

                using var cmd = new SQLiteCommand(sql, conn);
                cmd.Parameters.AddWithValue("@tenVatTu", keyWord_KhongDau);
                cmd.Parameters.AddWithValue("@nguoiLam", nguoiLam?.Trim() ?? "");
                cmd.Parameters.AddWithValue("@isNhapKho", isNhapKho ? 1 : 0);

                using var adapter = new SQLiteDataAdapter(cmd);
                adapter.Fill(dt);
            });

            return dt;
        }


        public static int GetSoLuongXuatNhapThangHienTai(bool isNhapKho = false)
        {
            DateTime now = DateTime.Now;

            string start = new DateTime(now.Year, now.Month, 1)
                .ToString("yyyy-MM-dd");

            string end = new DateTime(now.Year, now.Month, 1)
                .AddMonths(1)
                .ToString("yyyy-MM-dd");

            string sql = @"
                SELECT 
                    IFNULL(MAX(CAST(SUBSTR(TenPhieu, INSTR(TenPhieu, '-') + 1) AS INTEGER)), 0)
                FROM LichSuXuatNhap
                WHERE Ngay >= @start 
                AND Ngay < @end
            ";


            sql += isNhapKho ? " AND TenPhieu LIKE 'KNK%'" : " AND TenPhieu  LIKE 'KXK%'";

            using var conn = new SQLiteConnection(DatabaseHelper.GetStringConnector);
            conn.Open();

            using var cmd = new SQLiteCommand(sql, conn);

            cmd.Parameters.AddWithValue("@start", start);
            cmd.Parameters.AddWithValue("@end", end);

            object result = cmd.ExecuteScalar();

            return result == null ? 0 : Convert.ToInt32(result);
        }
        public static async Task<string> CapNhatLichSuXuatNhap( DataGridView dgv, string nguoiGiaoNhan, string lyDoChung,  string nguoiLam, bool isNhapKho)
        {
            if (dgv == null || dgv.Rows.Count == 0)
                return null;

            const string sqlUpdate = @"
                UPDATE LichSuXuatNhap
                SET
                    Ngay = @Ngay,
                    NguoiGiao_Nhan = @NguoiGiao_Nhan,
                    LyDo = @LyDo,
                    SoLuong = @SoLuong,
                    nguoiLam = @nguoiLam,
                    nhaCC = @nhaCC,
                    GhiChu = @GhiChu,
                    DonGia = @DonGia
                WHERE id = @id ;";

            bool hasUpdate = false;

            await Task.Run(() =>
            {
                using var conn = new SQLiteConnection(_connStr);
                conn.Open();

                using var tx = conn.BeginTransaction();
                using var cmd = new SQLiteCommand(sqlUpdate, conn, tx);

                cmd.Parameters.Add("@Ngay", DbType.String);
                cmd.Parameters.Add("@NguoiGiao_Nhan", DbType.String);
                cmd.Parameters.Add("@LyDo", DbType.String);
                cmd.Parameters.Add("@SoLuong", DbType.Decimal);
                cmd.Parameters.Add("@nguoiLam", DbType.String);
                cmd.Parameters.Add("@nhaCC", DbType.String);
                cmd.Parameters.Add("@GhiChu", DbType.String);
                cmd.Parameters.Add("@DonGia", DbType.String);
                cmd.Parameters.Add("@id", DbType.Int64);

                try
                {
                    foreach (DataGridViewRow row in dgv.Rows)
                    {
                        if (row.IsNewRow) continue;

                        object idLichSuObj = row.Cells["id"]?.Value;

                        if (idLichSuObj == null || idLichSuObj == DBNull.Value) continue;

                        if (!long.TryParse(idLichSuObj.ToString(), out long idLichSuXuatNhap))
                            continue;

                        object slObj = row.Cells["thucNhan"]?.Value;
                        if (slObj == null || slObj == DBNull.Value) continue;

                        if (!decimal.TryParse(slObj.ToString(), out decimal soLuong))
                            continue;                        

                        string ghiChuRieng = dgv.Columns.Contains("ghiChu")
                            ? row.Cells["ghiChu"]?.Value?.ToString()?.Trim()
                            : null;

                        string donGia = dgv.Columns.Contains("DonGia")
                            ? row.Cells["DonGia"]?.Value?.ToString()?.Trim()
                            : null;

                        string ghiChu = !string.IsNullOrWhiteSpace(ghiChuRieng)
                            ? ghiChuRieng
                            : ghiChuRieng?.Trim();

                        cmd.Parameters["@Ngay"].Value = dgv.Columns.Contains("ngay")
                            ? row.Cells["ngay"]?.Value?.ToString()?.Trim()
                            : null;

                        cmd.Parameters["@nhaCC"].Value = dgv.Columns.Contains("NhaCungCap")
                            ? row.Cells["NhaCungCap"]?.Value?.ToString()?.Trim()
                            : null;

                        cmd.Parameters["@NguoiGiao_Nhan"].Value = nguoiGiaoNhan?.Trim() ?? "";
                        cmd.Parameters["@LyDo"].Value = lyDoChung;
                        cmd.Parameters["@GhiChu"].Value = string.IsNullOrWhiteSpace(ghiChu)
                            ? (object)DBNull.Value
                            : ghiChu;
                        cmd.Parameters["@SoLuong"].Value = isNhapKho ? soLuong : -Math.Abs(soLuong);
                        cmd.Parameters["@DonGia"].Value = string.IsNullOrWhiteSpace(donGia)
                            ? (object)DBNull.Value
                            : donGia;
                        cmd.Parameters["@nguoiLam"].Value = nguoiLam;
                        cmd.Parameters["@id"].Value = idLichSuXuatNhap;

                        cmd.ExecuteNonQuery();
                        hasUpdate = true;
                    }

                    tx.Commit();
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }
            });

            return hasUpdate ? "Update thành công" : null;
        }

        public static async Task<string> LuuLichSuXuatNhap( DataGridView dgv,  string nguoiGiaoNhan, string lyDoChung, string ngay, decimal nhacc, int kho,  string nguoiLam, bool isNhapKho = true)
        {
            if (dgv == null || dgv.Rows.Count == 0)
                return null;

            DateTime parsedNgay = DateTime.TryParse(ngay, out DateTime tempNgay)
                ? tempNgay
                : DateTime.Now;

            const string sqlInsert = @"
                INSERT INTO LichSuXuatNhap
                (
                    ThongTinDatHang_ID,
                    Ngay,
                    DanhSachNCC_ID,
                    NguoiGiao_Nhan,
                    LyDo,
                    SoLuong,
                    DanhSachKho_ID,
                    nguoiLam,
                    GhiChu,
                    DonGia,
                    TenPhieu
                )
                VALUES
                (
                    @ThongTinDatHang_ID,
                    @Ngay,
                    @nhacc,
                    @NguoiGiao_Nhan,
                    @LyDo,
                    @SoLuong,
                    @Kho,
                    @nguoiLam,
                    @GhiChu,
                    @DonGia,
                    @TenPhieu
                );";

            string tenPhieu = null;
            bool hasInsert = false;

            await Task.Run(() =>
            {
                using var conn = new SQLiteConnection(_connStr);
                conn.Open();

                using var tx = conn.BeginTransaction();
                using var cmd = new SQLiteCommand(sqlInsert, conn, tx);

                cmd.Parameters.Add("@ThongTinDatHang_ID", DbType.Int64);
                cmd.Parameters.Add("@Ngay", DbType.String);
                cmd.Parameters.Add("@nhacc", DbType.Decimal);
                cmd.Parameters.Add("@NguoiGiao_Nhan", DbType.String);
                cmd.Parameters.Add("@LyDo", DbType.String);
                cmd.Parameters.Add("@SoLuong", DbType.Decimal);
                cmd.Parameters.Add("@Kho", DbType.Int64);
                cmd.Parameters.Add("@nguoiLam", DbType.String);
                cmd.Parameters.Add("@GhiChu", DbType.String);
                cmd.Parameters.Add("@DonGia", DbType.String);
                cmd.Parameters.Add("@TenPhieu", DbType.String);

                try
                {
                    int soThuTu = GetSoLuongXuatNhapThangHienTai(isNhapKho) + 1;
                    string prefix = isNhapKho ? "KNK" : "KXK";
                    tenPhieu = $"{prefix}{parsedNgay:yy/MM}-{soThuTu:D4}";

                    foreach (DataGridViewRow row in dgv.Rows)
                    {
                        if (row.IsNewRow) continue;

                        object idObj = row.Cells["id"]?.Value;
                        if (idObj == null || idObj == DBNull.Value) continue;

                        if (!long.TryParse(idObj.ToString(), out long thongTinDatHangId))
                            continue;

                        var raw = row.Cells["thucNhan"]?.Value?.ToString()?.Trim()?.Replace(',', '.');

                        if (!decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal soLuong))
                            continue;

                        if (soLuong == 0) continue;

                        string ghiChuRieng = dgv.Columns.Contains("ghiChu")
                            ? row.Cells["ghiChu"]?.Value?.ToString()?.Trim()
                            : null;

                        string donGia = dgv.Columns.Contains("DonGia")
                           ? row.Cells["DonGia"]?.Value?.ToString()?.Trim()
                           : null;

                        string ghiChu = !string.IsNullOrWhiteSpace(ghiChuRieng)
                            ? ghiChuRieng
                            : ghiChuRieng?.Trim();

                        cmd.Parameters["@ThongTinDatHang_ID"].Value = thongTinDatHangId;
                        cmd.Parameters["@Ngay"].Value = parsedNgay.ToString("yyyy-MM-dd");
                        cmd.Parameters["@nhacc"].Value = nhacc == 0 ? (object)DBNull.Value : nhacc;
                        cmd.Parameters["@NguoiGiao_Nhan"].Value = nguoiGiaoNhan?.Trim() ?? "";
                        cmd.Parameters["@LyDo"].Value = lyDoChung;
                        cmd.Parameters["@GhiChu"].Value = string.IsNullOrWhiteSpace(ghiChu)
                            ? (object)DBNull.Value
                            : ghiChu;

                        cmd.Parameters["@SoLuong"].Value = isNhapKho ? soLuong : -Math.Abs(soLuong);
                        cmd.Parameters["@DonGia"].Value = donGia;

                        cmd.Parameters["@TenPhieu"].Value = tenPhieu;
                        cmd.Parameters["@Kho"].Value = kho;
                        cmd.Parameters["@nguoiLam"].Value = nguoiLam;

                        cmd.ExecuteNonQuery();
                        hasInsert = true;
                    }

                    tx.Commit();
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }
            });

            return hasInsert ? tenPhieu : null;
        }

        public static async Task<string> LuuDonKhacAsync( DonKhacInfo info, List<DonKhacItem> items)
        {
            if (info == null || items == null || items.Count == 0)
                return null;

            // Chỉ lấy các item hợp lệ
            var validItems = items
                .Where(x => x != null
                            && !string.IsNullOrWhiteSpace(x.TenVatTu)
                            && x.SoLuong != 0)
                .ToList();

            if (validItems.Count == 0)
                return null;

            DateTime ngay = info.Ngay == default ? DateTime.Now : info.Ngay;

            int soThuTu = GetSoLuongXuatNhapThangHienTai(info.IsNhapKho) + 1;
            string prefix = info.IsNhapKho ? "KNK" : "KXK";
            string tenPhieu = $"{prefix}{ngay:yy/MM}-{soThuTu:D4}";
            string maDon = tenPhieu;

            const string sqlInsertDanhSachDatHang = @"
                INSERT INTO DanhSachDatHang
                (
                    MaDon,
                    LoaiDon,
                    DateInsert,
                    NguoiDat
                )
                VALUES
                (
                    @MaDon,
                    @LoaiDon,
                    @DateInsert,
                    @NguoiDat
                );
                SELECT last_insert_rowid();";

            const string sqlInsertThongTinDatHang = @"
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
                    @DanhSachMaSP_ID,
                    @DanhSachDatHang_ID,
                    @TenVatTu,
                    @TenVatTu_KhongDau,
                    @SoLuongMua,
                    @MucDichMua,
                    @NgayGiao,
                    @Date_Insert,
                    @DonGia
                );
                SELECT last_insert_rowid();";

            const string sqlInsertLichSuXuatNhap = @"
                INSERT INTO LichSuXuatNhap
                (
                    ThongTinDatHang_ID,
                    Ngay,
                    NguoiGiao_Nhan,
                    LyDo,
                    SoLuong,
                    DanhSachKho_ID,
                    NguoiLam,
                    DanhSachNCC_ID,
                    GhiChu,
                    DonGia,
                    TenPhieu
                )
                VALUES
                (
                    @ThongTinDatHang_ID,
                    @Ngay,
                    @NguoiGiao_Nhan,
                    @LyDo,
                    @SoLuong,
                    @DanhSachKho_ID,
                    @NguoiLam,
                    @NhaCC,
                    @GhiChu,
                    @DonGia,
                    @TenPhieu
                );";

            return await Task.Run(() =>
            {
                using var conn = new SQLiteConnection(_connStr);
                conn.Open();

                using var tx = conn.BeginTransaction();

                try
                {
                    long danhSachDatHangId;


                    if (!info.IsNhapKho)
                    {
                        foreach (var item in validItems)
                        {
                            long thongTinDatHangId = item.DanhSachMaSpId ?? 0;

                            InsertLichSuXuatNhap(
                                conn,
                                tx,
                                sqlInsertLichSuXuatNhap,
                                info,
                                item,
                                thongTinDatHangId,
                                ngay,
                                tenPhieu
                            );
                        }

                    }
                    else
                    {
                        // 1) Insert DanhSachDatHang
                        using (var cmdDon = new SQLiteCommand(sqlInsertDanhSachDatHang, conn, tx))
                        {
                            cmdDon.Parameters.AddWithValue("@MaDon", maDon);
                            cmdDon.Parameters.AddWithValue("@LoaiDon", 1); // 1 = vật tư
                            cmdDon.Parameters.AddWithValue("@DateInsert", ngay.ToString("yyyy-MM-dd"));
                            cmdDon.Parameters.AddWithValue("@NguoiDat", info.NguoiDat?.Trim() ?? "");

                            danhSachDatHangId = Convert.ToInt64(cmdDon.ExecuteScalar());
                        }


                        // 2) Insert từng item vào ThongTinDatHang + LichSuXuatNhap
                        foreach (var item in validItems)
                        {
                            long thongTinDatHangId;

                            // Lưu vào thông tin đặt hàng
                            using (var cmdTTDH = new SQLiteCommand(sqlInsertThongTinDatHang, conn, tx))
                            {
                                cmdTTDH.Parameters.AddWithValue(
                                    "@DanhSachMaSP_ID",
                                    item.DanhSachMaSpId.HasValue
                                        ? (object)item.DanhSachMaSpId.Value
                                        : DBNull.Value);

                                cmdTTDH.Parameters.AddWithValue("@DanhSachDatHang_ID", danhSachDatHangId);
                                cmdTTDH.Parameters.AddWithValue("@TenVatTu", item.TenVatTu.Trim());
                                cmdTTDH.Parameters.AddWithValue(
                                    "@TenVatTu_KhongDau", item.TenVatTuKhongDau.Trim());

                                cmdTTDH.Parameters.AddWithValue("@SoLuongMua", item.SoLuong);
                                cmdTTDH.Parameters.AddWithValue(
                                    "@MucDichMua",
                                    string.IsNullOrWhiteSpace(item.MucDichMua)
                                        ? DBNull.Value
                                        : (object)item.MucDichMua.Trim());

                                cmdTTDH.Parameters.AddWithValue("@NgayGiao", ngay.ToString("yyyy-MM-dd"));
                                cmdTTDH.Parameters.AddWithValue("@Date_Insert", ngay.ToString("yyyy-MM-dd"));
                                cmdTTDH.Parameters.AddWithValue(
                                    "@DonGia",
                                    item.DonGia.HasValue ? (object)item.DonGia.Value : DBNull.Value);

                                thongTinDatHangId = Convert.ToInt64(cmdTTDH.ExecuteScalar());
                            }

                            // Lưu vào lịch sử xuất nhập
                            InsertLichSuXuatNhap(
                                conn,
                                tx,
                                sqlInsertLichSuXuatNhap,
                                info,
                                item,
                                thongTinDatHangId,
                                ngay,
                                tenPhieu
                            );
                        }
                    }
                    tx.Commit();
                    return maDon;
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }
            });
        }

        private static void InsertLichSuXuatNhap( SQLiteConnection conn,  SQLiteTransaction tx,  string sql, DonKhacInfo info,  DonKhacItem item,  long thongTinDatHangId, DateTime ngay, string tenPhieu)
        {
            using var cmd = new SQLiteCommand(sql, conn, tx);

            cmd.Parameters.AddWithValue("@ThongTinDatHang_ID", thongTinDatHangId);
            cmd.Parameters.AddWithValue("@Ngay", ngay.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("@NguoiGiao_Nhan", info.NguoiGiaoNhan?.Trim() ?? "");

            cmd.Parameters.AddWithValue(
                "@LyDo",
                string.IsNullOrWhiteSpace(info.LyDoChung)
                    ? DBNull.Value
                    : (object)info.LyDoChung.Trim());

            cmd.Parameters.AddWithValue(
                "@SoLuong",
                info.IsNhapKho ? item.SoLuong : -Math.Abs(item.SoLuong));

            cmd.Parameters.AddWithValue("@DanhSachKho_ID", info.KhoId < 1 ? (object)DBNull.Value : info.KhoId);
            cmd.Parameters.AddWithValue("@NguoiLam", info.NguoiLam?.Trim() ?? "");
            cmd.Parameters.AddWithValue("@NhaCC", info.Nhacc == 0 ? (object)DBNull.Value : info.Nhacc);

            cmd.Parameters.AddWithValue(
                "@GhiChu",
                string.IsNullOrWhiteSpace(item.GhiChu)
                    ? DBNull.Value
                    : (object)item.GhiChu.Trim());

            cmd.Parameters.AddWithValue(
                "@DonGia",
                item.DonGia.HasValue ? (object)item.DonGia.Value : DBNull.Value);

            cmd.Parameters.AddWithValue("@TenPhieu", tenPhieu);

            cmd.ExecuteNonQuery();
        }

        public static async Task<List<string>> TimKiemMaDonConHang(string keyword, bool isEdit)
        {
            string sql = @"
            SELECT DISTINCT d.MaDon
            FROM DanhSachDatHang d
            INNER JOIN ThongTinDatHang t ON t.DanhSachDatHang_ID = d.id
            INNER JOIN LichSuXuatNhap l ON l.ThongTinDatHang_ID = t.id
            WHERE d.MaDon LIKE @kw
            GROUP BY t.id
            HAVING SUM(l.SoLuong) > 0
            ORDER BY d.DateInsert DESC
            LIMIT 30";

            if (isEdit)
            {
                sql = @"
                    SELECT DISTINCT l.TenPhieu as MaDon
                    FROM LichSuXuatNhap as l                    
                    WHERE (
                        l.canedit = 1 
                        and l.TenPhieu LIKE @kw   COLLATE NOCASE 
                    )
                    LIMIT 30";
            }

            var result = new List<string>();

            await Task.Run(() =>
            {
                using var conn = new SQLiteConnection(_connStr);
                conn.Open();
                using var cmd = new SQLiteCommand(sql, conn);
                cmd.Parameters.AddWithValue("@kw", $"%{keyword}%");

                using var rd = cmd.ExecuteReader();
                while (rd.Read())
                    result.Add(rd["MaDon"].ToString());
            });

            return result;
        }

        public static async Task<List<string>> TimKiemTheoTenVatTuConHang(string keyword)
        {

            string keyWord_KhongDau = CoreHelper.BoDauTiengViet(keyword.Trim());

            const string sql = @"
            SELECT 
                CASE 
                    WHEN t.DanhSachMaSP_ID IS NULL THEN t.TenVatTu
                    ELSE d.Ten
                END AS TenVatTu
            FROM ThongTinDatHang t
            LEFT JOIN DanhSachMaSP d 
                ON d.id = t.DanhSachMaSP_ID
            INNER JOIN LichSuXuatNhap l 
                ON l.ThongTinDatHang_ID = t.id
            WHERE
                (
                    t.DanhSachMaSP_ID IS NULL
                    AND t.TenVatTu_KhongDau LIKE @kw COLLATE NOCASE
                    AND t.TenVatTu IS NOT NULL
                )
                OR
                (
                    t.DanhSachMaSP_ID IS NOT NULL
                    AND d.Ten_KhongDau LIKE @kw COLLATE NOCASE
                    AND d.Ten IS NOT NULL
                )
            GROUP BY 
                CASE 
                    WHEN t.DanhSachMaSP_ID IS NULL THEN t.TenVatTu
                    ELSE d.Ten
                END
            HAVING SUM(l.SoLuong) > 0
            ORDER BY TenVatTu
            LIMIT 50;";

            var result = new List<string>();

            await Task.Run(() =>
            {
                using var conn = new SQLiteConnection(_connStr);
                conn.Open();
                using var cmd = new SQLiteCommand(sql, conn);
                cmd.Parameters.AddWithValue("@kw", $"%{keyWord_KhongDau}%");

                using var rd = cmd.ExecuteReader();
                while (rd.Read())
                    result.Add(rd["TenVatTu"].ToString());
            });

            return result;
        }

        #endregion

        public static int GetSoLuongDonThangHienTai()
        {
            DateTime now = DateTime.Now;

            string start = new DateTime(now.Year, now.Month, 1)
                .ToString("yyyy-MM-dd");

            string end = new DateTime(now.Year, now.Month, 1)
                .AddMonths(1)
                .ToString("yyyy-MM-dd");

            string sql = @"
                SELECT COUNT(*)
                FROM DanhSachDatHang
                WHERE NgayThem >= @start 
                AND NgayThem < @end
            ";

            using var conn = new SQLiteConnection(DatabaseHelper.GetStringConnector);
            conn.Open();

            using var cmd = new SQLiteCommand(sql, conn);

            cmd.Parameters.AddWithValue("@start", start);
            cmd.Parameters.AddWithValue("@end", end);

            object result = cmd.ExecuteScalar();

            return result == null ? 0 : Convert.ToInt32(result);
        }

        public static bool TryPing(string dbPath, int timeoutSeconds = 3)
        {

            if (string.IsNullOrWhiteSpace(dbPath))
            {
                return false;
            }

            dbPath = System.IO.Path.GetFullPath(dbPath);

            if (!File.Exists(dbPath))
            {
                return false;
            }

            try
            {
                // Read Only để chỉ kiểm tra (không ghi/không tạo mới)
                // FailIfMissing để báo lỗi rõ nếu file thiếu
                // Default Timeout áp dụng cho các thao tác lock/chờ
                string connStr =
                    $"Data Source={dbPath};Version=3;Read Only=True;FailIfMissing=True;Default Timeout={timeoutSeconds};";

                using (var conn = new SQLiteConnection(connStr))
                {
                    conn.Open();

                    using (var cmd = new SQLiteCommand("SELECT 1;", conn))
                    {
                        cmd.CommandTimeout = timeoutSeconds;
                        cmd.ExecuteScalar();
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
                
        public static string GenerateLotCode()
        {
            // Lấy 2 số cuối của năm hiện tại
            string yearSuffix = DateTime.Now.ToString("yy");

            // Lấy năm hiện tại để filter
            int currentYear = DateTime.Now.Year;

            using (var connection = new SQLiteConnection(_connStr))
            {
                connection.Open();

                // Đếm số lượng lot đã được tạo trong năm hiện tại
                string query = @"
                    SELECT COUNT(*) 
                    FROM KeHoachSX 
                    WHERE strftime('%Y', InsertedAt) = @currentYear";

                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@currentYear", currentYear.ToString());

                    long count = (long)command.ExecuteScalar();

                    // Số thứ tự mới = count + 1
                    int sequenceNumber = (int)count + 1;

                    // Format thành chuỗi 4 chữ số (padding với số 0)
                    string sequenceSuffix = sequenceNumber.ToString("D4");

                    // Kết hợp thành mã Lot
                    string lotCode = yearSuffix + sequenceSuffix;

                    return lotCode;
                }
            }
        }

        /// <summary>
        /// Load chi tiết đơn theo MaDon cho xuất kho
        /// Cột yeuCau = tồn kho hiện tại (SUM toàn bộ LichSuXuatNhap)
        /// Chỉ lấy vật tư còn tồn kho > 0
        /// </summary>
        public static async Task<DataTable> LayChiTietDonDatHangXuatKho(string maDon, bool isEdit)
        {
            string sql = @"
            SELECT
                t.id AS id,
                CASE
                    WHEN t.DanhSachMaSP_ID IS NULL THEN t.TenVatTu
                    ELSE sp.Ten
                END AS ten,
                CASE
                    WHEN t.DanhSachMaSP_ID IS NULL THEN NULL
                    ELSE sp.Ma
                END AS ma,
                d.MaDon AS maDon,
                CASE
                    WHEN t.DanhSachMaSP_ID IS NULL THEN NULL
                    ELSE sp.DonVi
                END AS donVi,
                IFNULL(ls.TonKho, 0) AS yeuCau
            FROM ThongTinDatHang t
            INNER JOIN DanhSachDatHang d 
                ON d.id = t.DanhSachDatHang_ID
            LEFT JOIN DanhSachMaSP sp 
                ON sp.id = t.DanhSachMaSP_ID
            LEFT JOIN (
                SELECT
                    ThongTinDatHang_ID,
                    SUM(SoLuong) AS TonKho
                FROM LichSuXuatNhap
                GROUP BY ThongTinDatHang_ID
            ) ls 
                ON ls.ThongTinDatHang_ID = t.id
            WHERE
                (
                    (t.DanhSachMaSP_ID IS NULL AND t.TenVatTu_KhongDau = @tenVatTu COLLATE NOCASE)
                    OR
                    (t.DanhSachMaSP_ID IS NOT NULL AND sp.Ten_KhongDau = @tenVatTu COLLATE NOCASE)
                )
                AND IFNULL(ls.TonKho, 0) > 0
            ORDER BY t.id;";


            if (isEdit)
            {
                sql = @"
                    SELECT
                        l.id           AS id,
                        l.TenPhieu     AS MaDon,
                        t.TenVatTu     AS ten,
                        sp.Ma          AS ma,
                        sp.DonVi       AS donVi, 
	                    ABS(l.soluong) as thucNhan,
                        l.donGia as dongia  ,
                        l.SoLuong * l.donGia AS thanhTien,
                        l.ngay as ngay,
                        l.GhiChu as GhiChu,

                        ncc.id         AS idNcc,
                        ncc.TenNCC     AS NhaCungCap
                
                    FROM LichSuXuatNhap l

                    LEFT JOIN ThongTinDatHang t 
                        ON t.id = l.ThongTinDatHang_ID

                    INNER JOIN DanhSachDatHang d 
                        ON d.id = t.DanhSachDatHang_ID

                    LEFT JOIN DanhSachMaSP sp 
                        ON sp.id = t.DanhSachMaSP_ID

                    LEFT JOIN DanhSachNCC ncc
                        ON ncc.id = l.DanhSachNCC_ID
            
                    WHERE l.TenPhieu = @maDon AND l.CanEdit = 1 AND ((l.SoLuong > 0 AND @isNhap = 1) OR (l.SoLuong < 0 AND @isNhap = 0))
                ";
            }

            var dt = new DataTable();

            await Task.Run(() =>
            {
                using var conn = new SQLiteConnection(_connStr);
                conn.Open();
                using var cmd = new SQLiteCommand(sql, conn);
                cmd.Parameters.AddWithValue("@maDon", maDon);
                cmd.Parameters.AddWithValue("@isNhap", 0);

                using var adapter = new SQLiteDataAdapter(cmd);
                adapter.Fill(dt);
            });

            return dt;
        }

        /// <summary>
        /// Load chi tiết theo tên vật tư cho xuất kho
        /// Cột yeuCau = tồn kho hiện tại (SUM toàn bộ LichSuXuatNhap)
        /// Chỉ lấy vật tư còn tồn kho > 0
        /// </summary>
        public static async Task<DataTable> LayChiTietDonTheoTenVatTuXuatKho(string tenVatTu)
        {
            const string sql = @"
            SELECT
                t.id                    AS id,
                sp.Ten                  AS ten,
                sp.Ma                   AS ma,
                d.MaDon                 AS maDon,
                sp.DonVi                AS donVi,
                IFNULL(ls.TonKho, 0)   AS yeuCau
            FROM ThongTinDatHang t
            INNER JOIN DanhSachDatHang d ON d.id = t.DanhSachDatHang_ID
            LEFT JOIN DanhSachMaSP sp ON sp.id = t.DanhSachMaSP_ID
            LEFT JOIN (
                SELECT
                    ThongTinDatHang_ID,
                    SUM(SoLuong) AS TonKho
                FROM LichSuXuatNhap
                GROUP BY ThongTinDatHang_ID
            ) ls ON ls.ThongTinDatHang_ID = t.id
            WHERE sp.Ten_KhongDau = @tenVatTu  COLLATE NOCASE
              AND IFNULL(ls.TonKho, 0) > 0
            ORDER BY t.id";


            var dt = new DataTable();

            await Task.Run(() =>
            {
                using var conn = new SQLiteConnection(_connStr);
                conn.Open();
                tenVatTu = CoreHelper.BoDauTiengViet(tenVatTu.Trim());


                Console.WriteLine(sql);
                Console.WriteLine(tenVatTu);

                using var cmd = new SQLiteCommand(sql, conn);
                cmd.Parameters.AddWithValue("@tenVatTu", tenVatTu);

                using var adapter = new SQLiteDataAdapter(cmd);
                adapter.Fill(dt);
            });

            return dt;
        }

        #region Kế hoạch sản xuất

        public static int TaoKeHoachMoi(DataGridView dgv)
        {
            if (dgv == null || dgv.Rows.Count == 0)
            {
                FrmWaiting.ShowGifAlert("KHÔNG CÓ DỮ LIỆU ĐỂ THÊM");
                return 0;
            }

            var errors = new List<string>();
            int successCount = 0;

            try
            {
                // BƯỚC 1: Map DataGridView → List<KeHoachSX>
                var keHoachList = MapDataGridViewToKeHoachList(dgv);

                if (keHoachList.Count == 0)
                {
                    FrmWaiting.ShowGifAlert("KHÔNG CÓ DỮ LIỆU ĐỂ THÊM");
                    return 0;
                }

                // BƯỚC 2: Batch lookup Ma → ID
                var lookupErrors = AssignDanhSachMaSP_IDs(keHoachList);
                errors.AddRange(lookupErrors);

                // BƯỚC 3: Insert - Để database validate
                var insertErrors = BulkInsertKeHoachSX(keHoachList, out successCount);
                errors.AddRange(insertErrors);

                // Hiển thị kết quả
                ShowInsertResult(successCount, errors);

                return successCount;
            }
            catch (Exception ex)
            {
                FrmWaiting.ShowGifAlert(CoreHelper.ShowErrorDatabase(ex, "KẾ HOẠCH"), "LỖI HỆ THỐNG");
                return 0;
            }
        }

        // ============================================================
        // BƯỚC 1: MAP DATAGRIDVIEW → LIST<KEHOACHSX>
        // ============================================================

        /// <summary>
        /// Map DataGridView → List<KeHoachSX>
        /// ✅ KHÔNG VALIDATE - Để database làm việc đó
        /// ✅ CHỈ CHUẨN HÓA DỮ LIỆU (uppercase, trim, format date)
        /// </summary>
        private static List<KeHoachSX> MapDataGridViewToKeHoachList(DataGridView dgv)
        {
            var keHoachList = new List<KeHoachSX>();

            foreach (DataGridViewRow row in dgv.Rows)
            {
                if (row.IsNewRow) continue;

                try
                {
                    // ✅ SỬ DỤNG MapRowToObject có sẵn
                    var keHoach = new KeHoachSX();
                    CoreHelper.MapRowToObject(row, keHoach);

                    // Chuẩn hóa dữ liệu
                    NormalizeKeHoachSX(keHoach);

                    keHoachList.Add(keHoach);
                }
                catch (Exception ex)
                {
                    // Chỉ log lỗi mapping, không dừng
                    System.Diagnostics.Debug.WriteLine($"Lỗi map dòng {row.Index + 1}: {ex.Message}");
                }
            }

            return keHoachList;
        }

        /// <summary>
        /// Chuẩn hóa dữ liệu KeHoachSX (không validate)
        /// </summary>
        private static void NormalizeKeHoachSX(KeHoachSX keHoach)
        {
            // Chuẩn hóa Ma, Lot sang chữ hoa
            if (!string.IsNullOrWhiteSpace(keHoach.Ma))
                keHoach.Ma = keHoach.Ma.ToUpper().Trim();

            if (!string.IsNullOrWhiteSpace(keHoach.Lot))
                keHoach.Lot = keHoach.Lot.ToUpper().Trim();

            // Chuẩn hóa ngày về format yyyy-MM-dd
            keHoach.NgayNhan = NormalizeDate(keHoach.NgayNhan);
            keHoach.NgayGiao = NormalizeDate(keHoach.NgayGiao);

            // Trim các string khác
            if (!string.IsNullOrWhiteSpace(keHoach.Ten))
                keHoach.Ten = keHoach.Ten.Trim();

            if (!string.IsNullOrWhiteSpace(keHoach.Mau))
                keHoach.Mau = keHoach.Mau.Trim();

            if (!string.IsNullOrWhiteSpace(keHoach.GhiChu))
                keHoach.GhiChu = keHoach.GhiChu.Trim();
        }

        /// <summary>
        /// Chuẩn hóa ngày về format yyyy-MM-dd (giống GetNgayHienTai)
        /// </summary>
        private static string NormalizeDate(string dateStr)
        {
            if (string.IsNullOrWhiteSpace(dateStr))
                return null;

            if (DateTime.TryParse(dateStr, out DateTime date))
                return date.ToString("yyyy-MM-dd");

            return dateStr;
        }

        // ============================================================
        // BƯỚC 2: BATCH LOOKUP Ma → ID
        // ============================================================

        /// <summary>
        /// Batch lookup Ma → DanhSachMaSP_ID
        /// </summary>
        private static List<string> AssignDanhSachMaSP_IDs(List<KeHoachSX> keHoachList)
        {
            var errors = new List<string>();

            try
            {
                // Thu thập unique Ma
                var uniqueMaList = keHoachList
                    .Select(k => k.Ma)
                    .Where(ma => !string.IsNullOrWhiteSpace(ma))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                if (uniqueMaList.Count == 0)
                    return errors;

                // Batch lookup trong 1 query
                Dictionary<string, int> maToIdMap;
                using (var conn = new SQLiteConnection(_connStr))
                {
                    conn.Open();
                    maToIdMap = BatchLookupMaToId(uniqueMaList, conn);
                }

                // Gán ID vào objects
                foreach (var keHoach in keHoachList)
                {
                    if (maToIdMap.TryGetValue(keHoach.Ma, out int id))
                    {
                        keHoach.DanhSachMaSP_ID = id;
                    }
                    else
                    {
                        // Không tìm thấy Ma → đánh dấu không hợp lệ
                        keHoach.DanhSachMaSP_ID = 0;
                        errors.Add($"Mã '{keHoach.Ma}' ({keHoach.Ten ?? ""}): Không tìm thấy trong danh sách sản phẩm");
                    }
                }
            }
            catch (Exception ex)
            {
                errors.Add($"Lỗi lookup mã SP: {CoreHelper.ShowErrorDatabase(ex)}");
            }

            return errors;
        }

        /// <summary>
        /// Batch lookup: Query tất cả Ma trong 1 query
        /// </summary>
        private static Dictionary<string, int> BatchLookupMaToId(
            HashSet<string> maList,
            SQLiteConnection conn)
        {
            var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            if (maList == null || maList.Count == 0)
                return result;

            // SQLite giới hạn 999 parameters → chia batch
            const int maxParams = 900;
            var maArray = maList.ToArray();

            for (int i = 0; i < maArray.Length; i += maxParams)
            {
                var batch = maArray.Skip(i).Take(maxParams).ToList();

                // Tạo SQL: WHERE Ma IN (@p0, @p1, ...)
                var paramNames = Enumerable.Range(0, batch.Count)
                    .Select(idx => $"@p{idx}")
                    .ToList();

                string sql = $@"
                    SELECT Ma, id 
                    FROM DanhSachMaSP 
                    WHERE Ma IN ({string.Join(", ", paramNames)});
                ";

                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    for (int j = 0; j < batch.Count; j++)
                        cmd.Parameters.AddWithValue($"@p{j}", batch[j]);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string ma = reader.GetString(0);
                            int id = reader.GetInt32(1);
                            result[ma] = id;
                        }
                    }
                }
            }

            return result;
        }

        // ============================================================
        // BƯỚC 3: BULK INSERT - ĐỂ DATABASE VALIDATE
        // ============================================================

        /// <summary>
        /// Bulk insert với transaction
        /// ✅ ĐỂ DATABASE VALIDATE: NOT NULL, UNIQUE, FOREIGN KEY...
        /// ✅ ShowErrorDatabase sẽ format lỗi thân thiện
        /// </summary>
        private static List<string> BulkInsertKeHoachSX(
            List<KeHoachSX> keHoachList,
            out int successCount)
        {
            var errors = new List<string>();
            successCount = 0;

            // Chỉ insert các record có DanhSachMaSP_ID hợp lệ
            var validKeHoachList = keHoachList
                .Where(k => k.DanhSachMaSP_ID > 0)
                .ToList();

            if (validKeHoachList.Count == 0)
                return errors;

            using (var conn = new SQLiteConnection(_connStr))
            {
                conn.Open();

                // ✅ TRANSACTION - Quan trọng nhất cho hiệu suất
                using (var tran = conn.BeginTransaction())
                {
                    try
                    {
                        // ✅ TÁI SỬ DỤNG COMMAND
                        using (var cmd = CreateInsertKeHoachSXCommand(conn, tran))
                        {
                            foreach (var keHoach in validKeHoachList)
                            {
                                try
                                {
                                    BindInsertKeHoachSXParameters(cmd, keHoach);
                                    cmd.ExecuteNonQuery();
                                    successCount++;
                                }
                                catch (Exception ex)
                                {
                                    // ✅ DATABASE TRẢ VỀ LỖI → ShowErrorDatabase xử lý
                                    // - NOT NULL constraint → "Thiếu dữ liệu bắt buộc (Lot)"
                                    // - UNIQUE constraint → "DỮ LIỆU đã tồn tại (Lot)"
                                    // - CHECK constraint → "Dữ liệu vi phạm ràng buộc"
                                    string errorMsg = CoreHelper.ShowErrorDatabase(ex, keHoach.Lot);
                                    errors.Add($"Lot '{keHoach.Lot}': {errorMsg}");
                                }
                            }
                        }

                        tran.Commit();
                    }
                    catch (Exception ex)
                    {
                        tran.Rollback();
                        errors.Add($"Lỗi hệ thống: {CoreHelper.ShowErrorDatabase(ex)}");
                    }
                }
            }

            return errors;
        }

        /// <summary>
        /// Tạo prepared statement cho INSERT
        /// </summary>
        private static SQLiteCommand CreateInsertKeHoachSXCommand(
            SQLiteConnection conn,
            SQLiteTransaction tran)
        {
            var cmd = new SQLiteCommand(conn);
            cmd.Transaction = tran;
            cmd.CommandText = @"
                INSERT INTO KeHoachSX (
                    DanhSachMaSP_ID, NgayNhan, Lot, SLHangDat, SLHangBan,
                    Mau, NgayGiao, GhiChu, TinhTrangKH, TinhTrangSX
                ) VALUES (
                    @DanhSachMaSP_ID, @NgayNhan, @Lot, @SLHangDat, @SLHangBan,
                    @Mau, @NgayGiao, @GhiChu, @TinhTrangKH, @TinhTrangSX
                );
            ";

            cmd.Parameters.Add("@DanhSachMaSP_ID", DbType.Int32);
            cmd.Parameters.Add("@NgayNhan", DbType.String);
            cmd.Parameters.Add("@Lot", DbType.String);
            cmd.Parameters.Add("@SLHangDat", DbType.Double);
            cmd.Parameters.Add("@SLHangBan", DbType.Double);
            cmd.Parameters.Add("@Mau", DbType.String);
            cmd.Parameters.Add("@NgayGiao", DbType.String);
            cmd.Parameters.Add("@GhiChu", DbType.String);
            cmd.Parameters.Add("@TinhTrangKH", DbType.Int32);
            cmd.Parameters.Add("@TinhTrangSX", DbType.Int32);

            return cmd;
        }

        /// <summary>
        /// Bind parameters từ object
        /// </summary>
        private static void BindInsertKeHoachSXParameters(SQLiteCommand cmd, KeHoachSX keHoach)
        {
            cmd.Parameters["@DanhSachMaSP_ID"].Value = keHoach.DanhSachMaSP_ID;
            cmd.Parameters["@NgayNhan"].Value = keHoach.NgayNhan ?? (object)DBNull.Value;
            cmd.Parameters["@Lot"].Value = keHoach.Lot ?? (object)DBNull.Value;
            cmd.Parameters["@SLHangDat"].Value = keHoach.SLHangDat ?? (object)DBNull.Value;
            cmd.Parameters["@SLHangBan"].Value = keHoach.SLHangBan ?? (object)DBNull.Value;
            cmd.Parameters["@Mau"].Value = keHoach.Mau ?? (object)DBNull.Value;
            cmd.Parameters["@NgayGiao"].Value = keHoach.NgayGiao ?? (object)DBNull.Value;
            cmd.Parameters["@GhiChu"].Value = keHoach.GhiChu ?? (object)DBNull.Value;
            cmd.Parameters["@TinhTrangKH"].Value = keHoach.TinhTrang;
            cmd.Parameters["@MucDoUuTienKH"].Value = keHoach.MucDoUuTienKH;
            cmd.Parameters["@TrangThaiSX"].Value = keHoach.TrangThaiSX;
        }

        /// <summary>
        /// Hiển thị kết quả
        /// </summary>
        private static void ShowInsertResult(int successCount, List<string> errors)
        {
            if (errors.Count > 0)
            {
                string errorMsg = $"{successCount} kế hoạch được thêm\n\n";
                errorMsg += $"Có ({errors.Count}) lỗi\n";
                errorMsg += string.Join("\n", errors.Take(10));

                if (errors.Count > 10)
                    errorMsg += $"\n... và {errors.Count - 10} lỗi khác";

                FrmWaiting.ShowGifAlert(errorMsg, "KẾT QUẢ");
            }
            else if (successCount > 0)
            {
                FrmWaiting.ShowGifAlert(
                    $"{successCount} kế hoạch được thêm",
                    "THÀNH CÔNG",EnumStore.Icon.Success
                );
            }
            else
            {
                FrmWaiting.ShowGifAlert("KHÔNG CÓ DỮ LIỆU NÀO ĐƯỢC THÊM", "THÔNG BÁO");
            }
        }

        public static DbResult InsertKeHoachSX(KeHoachSX dto)
        {
            try
            {
                using var conn = new SQLiteConnection(_connStr);
                conn.Open();

                using var tx = conn.BeginTransaction();

                using var cmd = conn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = @"
                    INSERT INTO KeHoachSX
                    (
                        DanhSachMaSP_ID, NgayNhan, Lot, SLHangDat, SLHangBan, Mau,
                        NgayGiao, GhiChu, TenKhachHang, TinhTrangKH, TrangThaiSX, MucDoUuTienKH
                    )
                    VALUES
                    (
                        @DanhSachMaSP_ID, @NgayNhan, @Lot, @SLHangDat, @SLHangBan, @Mau,
                        @NgayGiao, @GhiChu, @TenKhachHang, @TinhTrangKH, @TrangThaiSX, @MucDoUuTienKH
                    );
                    SELECT last_insert_rowid();
                    ";

                BindParams(cmd, dto, isUpdate: false);

                var newIdObj = cmd.ExecuteScalar();
                var newId = Convert.ToInt64(newIdObj);

                tx.Commit();

                return new DbResult
                {
                    Ok = true,
                    Id = newId,
                    Message = "thành công."
                };
            }
            catch (Exception ex)
            {
                return new DbResult
                {
                    Ok = false,
                    Id = null,
                    Message = CoreHelper.ShowErrorDatabase(ex)
                };
            }
        }

        public static DbResult UpdateKeHoachSX( KeHoachSX dto)
        {
            try
            {
                using var conn = new SQLiteConnection(_connStr);
                conn.Open();

                using var tx = conn.BeginTransaction();

                using var cmd = conn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = @"
                UPDATE KeHoachSX
                SET
                    DanhSachMaSP_ID = @DanhSachMaSP_ID,
                    NgayNhan        = @NgayNhan,
                    SLHangDat       = @SLHangDat,
                    SLHangBan       = @SLHangBan,
                    Mau             = @Mau,
                    NgayGiao        = @NgayGiao,
                    GhiChu          = @GhiChu,
                    TenKhachHang    = @TenKhachHang,
                    TinhTrangKH     = @TinhTrangKH,
                    TrangThaiSX     = @TrangThaiSX,
                    MucDoUuTienKH    = @MucDoUuTienKH
                WHERE Lot = @Lot;
                ";

                BindParams(cmd, dto, isUpdate: true);

                var affected = cmd.ExecuteNonQuery();
                if (affected == 0)
                {
                    tx.Rollback();
                    return new DbResult
                    {
                        Ok = false,
                        Id = dto.Id,
                        Message = "Không tìm thấy bản ghi để cập nhật (id không tồn tại)."
                    };
                }

                tx.Commit();

                return new DbResult
                {
                    Ok = true,
                    Id = dto.Id,
                    Message = "Thành công."
                };
            }
            catch (Exception ex)
            {
                return new DbResult
                {
                    Ok = false,
                    Id = dto.Id,
                   Message = CoreHelper.ShowErrorDatabase(ex)
                };
            }
        }

        public static void UpsertDanhSachNCC(string id, string ma, string tenNcc, string diaChi = null)
        {
            bool isInsert = string.IsNullOrWhiteSpace(id);

            using var conn = new SQLiteConnection(_connStr);
            conn.Open();

            using var tx = conn.BeginTransaction();
            try
            {
                string sql = isInsert
                    ? @"
                        INSERT INTO DanhSachNCC
                        (Ma, TenNCC, TenNCC_KhongDau, DiaChi)
                        VALUES
                        (@Ma, @TenNCC, @TenNCC_KhongDau, @DiaChi);"
                                : @"
                        UPDATE DanhSachNCC
                        SET
                            Ma = @Ma,
                            TenNCC = @TenNCC,
                            TenNCC_KhongDau = @TenNCC_KhongDau,
                            DiaChi = @DiaChi
                        WHERE id = @id;";

                using var cmd = new SQLiteCommand(sql, conn, tx);

                cmd.Parameters.AddWithValue("@Ma", ma?.Trim());
                cmd.Parameters.AddWithValue("@TenNCC", tenNcc?.Trim());

                // giả sử bạn có hàm bỏ dấu
                cmd.Parameters.AddWithValue("@TenNCC_KhongDau",
                    string.IsNullOrWhiteSpace(tenNcc)
                        ? (object)DBNull.Value
                        : CoreHelper.BoDauTiengViet(tenNcc));

                cmd.Parameters.AddWithValue("@DiaChi",
                    string.IsNullOrWhiteSpace(diaChi) ? (object)DBNull.Value : diaChi.Trim());

                if (!isInsert)
                    cmd.Parameters.AddWithValue("@id", Convert.ToInt32(id));

                cmd.ExecuteNonQuery();

                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        public static void UpsertDanhSachKho(string id, string kiHieu, string tenKho, string ghiChu = null)
        {
            bool isInsert = string.IsNullOrWhiteSpace(id);

            using var conn = new SQLiteConnection(_connStr);
            conn.Open();

            using var tx = conn.BeginTransaction();
            try
            {
                string sql = isInsert
                    ? @"
                    INSERT INTO DanhSachKho
                    (KiHieu, TenKho, GhiChu)
                    VALUES
                    (@KiHieu, @TenKho,@TenKho_KhongDau, @GhiChu);"
                            : @"
                    UPDATE DanhSachKho
                    SET
                        KiHieu = @KiHieu,
                        TenKho = @TenKho,
                        TenKho_KhongDau = @TenKho_KhongDau,
                        GhiChu = @GhiChu
                    WHERE id = @id;";

                using var cmd = new SQLiteCommand(sql, conn, tx);

                cmd.Parameters.AddWithValue("@KiHieu", kiHieu?.Trim());
                cmd.Parameters.AddWithValue("@TenKho", tenKho?.Trim());
                cmd.Parameters.AddWithValue("@TenKho_KhongDau", CoreHelper.BoDauTiengViet(tenKho?.Trim()));
                cmd.Parameters.AddWithValue("@GhiChu",
                    string.IsNullOrWhiteSpace(ghiChu) ? (object)DBNull.Value : ghiChu.Trim());

                if (!isInsert)
                    cmd.Parameters.AddWithValue("@id", Convert.ToInt32(id));

                cmd.ExecuteNonQuery();

                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        //public static List<ResultFindKeHoachSX> SearchKeHoachSX(TimKiemKeHoachSX f)
        //{
        //    var result = new List<ResultFindKeHoachSX>();
        //    var (sql, pars) = BuildSqlSearchKeHoachSX(f);

        //    using (var conn = new SQLiteConnection(_connStr))
        //    {
        //        conn.Open();
        //        using (var cmd = new SQLiteCommand(sql, conn))
        //        {
        //            // add parameters
        //            foreach (var kv in pars)
        //                cmd.Parameters.AddWithValue(kv.Key, kv.Value ?? DBNull.Value);

        //            using (var rd = cmd.ExecuteReader())
        //            {
        //                while (rd.Read())
        //                {
        //                    ResultFindKeHoachSX item = new ResultFindKeHoachSX();

        //                    int TinhTrangCuaKH = rd["TinhTrangKH"] != DBNull.Value ? Convert.ToInt32(rd["TinhTrangKH"]) : 0;
        //                    int MucDoUuTienKH = rd["MucDoUuTienKH"] != DBNull.Value ? Convert.ToInt32(rd["MucDoUuTienKH"]) : 0;
        //                    int TrangThaiThucHienKH = rd["TrangThaiSX"] != DBNull.Value ? Convert.ToInt32(rd["TrangThaiSX"]) : 0;

        //                    item.Ten = rd["Ten"] != DBNull.Value ? rd["Ten"].ToString() : "";

        //                    item.NgayNhan = rd["NgayNhan"] != DBNull.Value ? rd["NgayNhan"].ToString() : "";
        //                    item.Lot = rd["Lot"] != DBNull.Value ? rd["Lot"].ToString() : "";

        //                    item.SLHangDat = rd["SLHangDat"] != DBNull.Value ? (double?)Convert.ToDouble(rd["SLHangDat"]) : null;
        //                    item.SLHangBan = rd["SLHangBan"] != DBNull.Value ? (double?)Convert.ToDouble(rd["SLHangBan"]) : null;
        //                    item.SLTong = item.SLHangDat + item.SLHangBan;

        //                    item.Mau = rd["Mau"] != DBNull.Value ? rd["Mau"].ToString() : "";
        //                    item.NgayGiao = rd["NgayGiao"] != DBNull.Value ? rd["NgayGiao"].ToString() : "";

        //                    item.GhiChu = rd["GhiChu"] != DBNull.Value ? rd["GhiChu"].ToString() : "";
        //                    item.TenKhachHang = rd["TenKhachHang"] != DBNull.Value ? rd["TenKhachHang"].ToString() : null;

        //                    item.KieuKH = EnumStore.TrangThaiBanHanhKH[TinhTrangCuaKH]; 

        //                    item.DoUuTien = EnumStore.MucDoUuTien[MucDoUuTienKH];

        //                    item.TrangThaiDon = EnumStore.TrangThaiThucHienTheoKH[TrangThaiThucHienKH];

        //                    result.Add(item);
        //                }
        //            }
        //        }
        //    }

        //    return result;
        //}

        public static DataTable SearchKeHoachSX_DataTable(TimKiemKeHoachSX f)
        {
            var (sql, pars) = BuildSqlSearchKeHoachSX(f);

            var dt = new DataTable();
            dt.Columns.Add("DoUuTien", typeof(string));
            dt.Columns.Add("TrangThaiDon", typeof(string));
            dt.Columns.Add("Ten", typeof(string));
            dt.Columns.Add("NgayNhan", typeof(string));
            dt.Columns.Add("Lot", typeof(string));
            dt.Columns.Add("SLHangDat", typeof(double));
            dt.Columns.Add("SLHangBan", typeof(double));
            dt.Columns.Add("SLTong", typeof(double));
            dt.Columns.Add("Mau", typeof(string));
            dt.Columns.Add("NgayGiao", typeof(string));
            dt.Columns.Add("GhiChu", typeof(string));
            dt.Columns.Add("TenKhachHang", typeof(string));
            dt.Columns.Add("KieuKH", typeof(string));

            using (var conn = new SQLiteConnection(_connStr))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    foreach (var kv in pars)
                        cmd.Parameters.AddWithValue(kv.Key, kv.Value ?? DBNull.Value);

                    using (var rd = cmd.ExecuteReader())
                    {
                        while (rd.Read())
                        {
                            int tinhTrangKH = rd["TinhTrangKH"] != DBNull.Value ? Convert.ToInt32(rd["TinhTrangKH"]) : 0;
                            int mucDoUuTienKH = rd["MucDoUuTienKH"] != DBNull.Value ? Convert.ToInt32(rd["MucDoUuTienKH"]) : 0;
                            int trangThaiSX = rd["TrangThaiSX"] != DBNull.Value ? Convert.ToInt32(rd["TrangThaiSX"]) : 0;

                            string ten = rd["Ten"] != DBNull.Value ? rd["Ten"].ToString() : "";
                            string ngayNhan = rd["NgayNhan"] != DBNull.Value ? rd["NgayNhan"].ToString() : "";
                            string lot = rd["Lot"] != DBNull.Value ? rd["Lot"].ToString() : "";

                            double? slDat = rd["SLHangDat"] != DBNull.Value ? (double?)Convert.ToDouble(rd["SLHangDat"]) : null;
                            double? slBan = rd["SLHangBan"] != DBNull.Value ? (double?)Convert.ToDouble(rd["SLHangBan"]) : null;
                            double? slTong = (slDat ?? 0) + (slBan ?? 0);

                            string mau = rd["Mau"] != DBNull.Value ? rd["Mau"].ToString() : "";
                            string ngayGiao = rd["NgayGiao"] != DBNull.Value ? rd["NgayGiao"].ToString() : "";

                            string ghiChu = rd["GhiChu"] != DBNull.Value ? rd["GhiChu"].ToString() : "";
                            string tenKH = rd["TenKhachHang"] != DBNull.Value ? rd["TenKhachHang"].ToString() : null;

                            string kieuKH = EnumStore.TrangThaiBanHanhKH.TryGetValue(tinhTrangKH, out var v1) ? v1 : "";

                            string doUuTien = EnumStore.MucDoUuTien.TryGetValue(mucDoUuTienKH, out var v2) ? v2 : "";

                            string trangThaiDon = EnumStore.TrangThaiThucHienTheoKH.TryGetValue(trangThaiSX, out var v3) ? v3 : "";


                            dt.Rows.Add(doUuTien, trangThaiDon, ten, ngayNhan, lot,
                                        slDat.HasValue ? (object)slDat.Value : DBNull.Value,
                                        slBan.HasValue ? (object)slBan.Value : DBNull.Value,
                                        (object)slTong,
                                        mau, ngayGiao, ghiChu, (object)tenKH ?? DBNull.Value,
                                        kieuKH);
                        }
                    }
                }
            }

            return dt;
        }

        private static (string sql, Dictionary<string, object> pars) BuildSqlSearchKeHoachSX(TimKiemKeHoachSX f)
        {
            var pars = new Dictionary<string, object>();
            var where = new List<string>();

            var sb = new StringBuilder();
            sb.Append(@"
                SELECT
                    k.id,
                    k.DanhSachMaSP_ID,
                    ds.Ma,
                    ds.Ten,
                    k.NgayNhan,
                    k.Lot,
                    k.SLHangDat,
                    k.SLHangBan,
                    k.Mau,
                    k.NgayGiao,
                    k.GhiChu,
                    k.TenKhachHang,
                    k.TinhTrangKH,
                    k.MucDoUuTienKH,
                    k.TrangThaiSX
                FROM KeHoachSX k
                LEFT JOIN DanhSachMaSP ds ON ds.id = k.DanhSachMaSP_ID
            ");

            // ===== int filters =====
            if (f.TrangThaiThucHienKH.HasValue)
            {
                where.Add("k.TrangThaiSX = @TrangThaiSX");
                pars["@TrangThaiSX"] = f.TrangThaiThucHienKH.Value;
            }

            if (f.TinhTrangCuaKH.HasValue)
            {
                where.Add("k.TinhTrangKH = @TinhTrangKH");
                pars["@TinhTrangKH"] = f.TinhTrangCuaKH.Value;
            }

            if (f.MucDoUuTienKH.HasValue)
            {
                where.Add("k.MucDoUuTienKH = @MucDoUuTienKH");
                pars["@MucDoUuTienKH"] = f.MucDoUuTienKH.Value;
            }

            // ===== text filters =====
            if (!string.IsNullOrWhiteSpace(f.Lot))
            {
                where.Add("k.Lot = @Lot");
                pars["@Lot"] = f.Lot.Trim();
            }

            if (!string.IsNullOrWhiteSpace(f.TenKhachHang))
            {
                where.Add("k.TenKhachHang LIKE @TenKhachHang");
                pars["@TenKhachHang"] = "%" + f.TenKhachHang.Trim() + "%";
            }

            if (!string.IsNullOrWhiteSpace(f.Mau))
            {
                where.Add("k.Mau LIKE @Mau");
                pars["@Mau"] = "%" + f.Mau.Trim() + "%";
            }

            if (!string.IsNullOrWhiteSpace(f.GhiChu))
            {
                where.Add("k.GhiChu LIKE @GhiChu");
                pars["@GhiChu"] = "%" + f.GhiChu.Trim() + "%";
            }

            // ===== date filters =====
            // DB lưu TEXT dạng "dd/MM/yyyy" => so sánh chuỗi đã format
            if (!string.IsNullOrWhiteSpace(f.NgayNhan))
            {
                where.Add("k.NgayNhan = @NgayNhan");
                pars["@NgayNhan"] = f.NgayNhan.Trim();
            }

            if (!string.IsNullOrWhiteSpace(f.NgayGiao))
            {
                where.Add("k.NgayGiao = @NgayGiao");
                pars["@NgayGiao"] = f.NgayGiao.Trim();
            }

            // ===== numeric filters =====
            if (f.SLTong.HasValue)
            {
                where.Add("(IFNULL(k.SLHangDat,0) + IFNULL(k.SLHangBan,0)) = @TongMin");
                pars["@TongMin"] = f.SLTong.Value;
            }

            if (f.SLHangBan.HasValue)
            {
                where.Add("IFNULL(k.SLHangBan,0) = @HangBanMin");
                pars["@HangBanMin"] = f.SLHangBan.Value;
            }

            if (f.SLHangDat.HasValue )
            {
                where.Add("IFNULL(k.SLHangDat,0) == @HangDatMin");
                pars["@HangDatMin"] = f.SLHangDat.Value;
            }

            if (!string.IsNullOrWhiteSpace(f.Ten))
            {
                where.Add("ds.Ten LIKE @Ten");
                pars["@Ten"] = "%" + f.Ten.Trim() + "%";
            }

            if (where.Count > 0)
            {
                sb.Append("WHERE ");
                sb.Append(string.Join(" AND ", where));
                sb.AppendLine();
            }

            sb.AppendLine("ORDER BY k.MucDoUuTienKH ASC;");
            return (sb.ToString(), pars);
        }

        // ========= PRIVATE HELPERS =========

        private static void BindParams(SQLiteCommand cmd, KeHoachSX dto, bool isUpdate)
        {
            cmd.Parameters.Clear();

            
                

            AddParam(cmd, "@DanhSachMaSP_ID", DbType.Int64, dto.DanhSachMaSP_ID);
            AddParam(cmd, "@NgayNhan", DbType.String, dto.NgayNhan);
            AddParam(cmd, "@Lot", DbType.Int64, dto.Lot);
            // Lot & NgayGiao: nếu null/"" => DB sẽ báo NOT NULL constraint failed (đúng ý bạn)
            AddParam(cmd, "@SLHangDat", DbType.Double, dto.SLHangDat);
            AddParam(cmd, "@SLHangBan", DbType.Double, dto.SLHangBan);
            AddParam(cmd, "@Mau", DbType.String, dto.Mau);

            AddParam(cmd, "@NgayGiao", DbType.String, dto.NgayGiao);

            AddParam(cmd, "@GhiChu", DbType.String, dto.GhiChu);
            AddParam(cmd, "@TenKhachHang", DbType.String, dto.TenKhachHang);

            AddParam(cmd, "@TinhTrangKH", DbType.Int32, dto.TinhTrang);
            AddParam(cmd, "@TrangThaiSX", DbType.Int32, dto.TrangThaiSX);
            AddParam(cmd, "@MucDoUuTienKH", DbType.Int32, dto.MucDoUuTienKH);
        }

        private static void AddParam(SQLiteCommand cmd, string name, DbType type, object? value)
        {
            var p = cmd.CreateParameter();
            p.ParameterName = name;
            p.DbType = type;
            p.Value = value ?? DBNull.Value;
            cmd.Parameters.Add(p);
        }


        #endregion

        #region Lấy dữ liệu
        // Lấy dữ liệu theo 1 điều kiện
        public static DataTable GetData(string query , string key = null,  string para = null)
        {
            using (SQLiteConnection conn = new SQLiteConnection(_connStr))
            {
                conn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(query, conn))
                {
                    if (!string.IsNullOrWhiteSpace(para) && key != null)
                    {
                        cmd.Parameters.AddWithValue("@" + para, key);
                    }

                    using (SQLiteDataAdapter adapter = new SQLiteDataAdapter(cmd))
                    {
                        DataTable resultTable = new DataTable();
                        adapter.Fill(resultTable);
                        return resultTable;
                    }
                }
            }
        }

        public static DataTable GetDataByCongDoan(DateTime selectedDate, CongDoan cd,int ca, string nguoiKiemTra)
        {
            string key = selectedDate.ToString("yyyy-MM-dd");

            string sqlSelect = CoreHelper.TaoSqL_LayThongTinBaoCaoChung();

            string sqlLayChiTietCD = CoreHelper.TaoSQL_LayChiTiet_1CD(cd.Id);

            string sqlTenNVL = CoreHelper.TaoSQL_LayDuLieuNVL(cd.Columns);

            string sqlJoin = CoreHelper.TaoSQL_TaoKetNoiCacBang();

            string sqlDk1 = " WHERE date(tclv.Ngay) = date(@para) ";

            string sqlDk2 = " AND ttp.CongDoan = " + cd.Id;

            string sqlDk3 = " AND tclv.Ca = " + ca;

            if (!string.IsNullOrWhiteSpace(nguoiKiemTra))
            {
                sqlDk3 += " AND tclv.NguoiLam = '" + nguoiKiemTra + "' ";
            }

            // 6) ORDER BY
            string sqlOrder = " ORDER BY tclv.Ngay DESC, ttp.id DESC;";

            // 7) Kết hợp hoàn chỉnh
            string query = sqlSelect + " ," + sqlLayChiTietCD + " ," + sqlTenNVL + sqlJoin + sqlDk1 + sqlDk2 + sqlDk3 + sqlOrder;

            return GetData(query, key, "para");
        }

        // Lấy dữ liệu theo ngày tháng
        public static DataTable GetDataByMonth(DateTime selectedDate, CongDoan cd)
        {
            string key = selectedDate.ToString("yyyy-MM-dd");
            
            string sqlSelect = CoreHelper.TaoSqL_LayThongTinBaoCaoChung();

            string sqlLayChiTietCD = CoreHelper.TaoSQL_LayChiTiet_1CD(cd.Id);
            
            string sqlTenNVL = CoreHelper.TaoSQL_LayDuLieuNVL(cd.Columns);

            string sqlJoin = CoreHelper.TaoSQL_TaoKetNoiCacBang();

            string sqlDk1 = " WHERE strftime('%Y-%m', tclv.Ngay) = strftime('%Y-%m', @para) ";

            string sqlDk2 = " AND ttp.CongDoan = " + cd.Id;

            // 6) ORDER BY
            string sqlOrder = " ORDER BY tclv.Ngay DESC, ttp.id DESC;";

            // 7) Kết hợp hoàn chỉnh
            string query = sqlSelect + " ," + sqlLayChiTietCD + " ," + sqlTenNVL + sqlJoin + sqlDk1 + sqlDk2 + sqlOrder;

            return GetData(query, key, "para");
        }
        // Lấy dữ liệu theo ID
        public static DataTable GetDataByID(string key, CongDoan cd, int kieuDL)
        {
            // Tạo select
            string sqlSelect = CoreHelper.TaoSqL_LayThongTinBaoCaoChung_Edit();

            // Lấy thông tin chi tiết công đoạn
            string sqlLayChiTietCD = CoreHelper.TaoSQL_LayChiTiet_1CD(cd.Id);

            // Lấy dữ liệu nvl theo công đoạn
            string sqlTenNVL = CoreHelper.TaoSQL_LayDuLieuNVL(cd.Columns);

            // Tạo câu nối các bảng
            string sqlJoin = CoreHelper.TaoSQL_TaoKetNoiCacBang();

            sqlJoin += "LEFT JOIN TTThanhPham ttp_bin ON ttp_bin.MaBin = nvl.BinNVL ";

            // Tạo điều kiện lọc theo ID
            string sqlDk1 = " WHERE ttp.id = @id";

            string sqlDk2 = " AND ttp.CongDoan = " + cd.Id;

            string sqlDk3 = kieuDL == 0 ? " AND ( (ds.DonVi = 'M'  AND IFNULL(ttp.ChieuDaiSau, 0) <> 0)  OR (ds.DonVi = 'KG' AND IFNULL(ttp.KhoiLuongSau, 0) <> 0)) " : "";

            // Kết hợp câu truy vấn
            string query = sqlSelect + " ,"+ sqlLayChiTietCD + " ," + sqlTenNVL + sqlJoin + sqlDk1 + sqlDk2 + sqlDk3;

            return GetData(query, key, "id");
        }
        // Lấy báo cáo sản xuất
        public static DataTable GetDataBaoCaoSX(DateTime ngayBatDau, DateTime ngayKetThuc, List<CongDoan> selectedCongDoans)
        {
            // Tạo phần SELECT chung
            string sqlSelect = CoreHelper.TaoSqL_LayThongTinBaoCaoChung();

            // Lấy dữ liệu NVL theo danh sách công đoạn
            string sqlTenNVL = CoreHelper.TaoSQL_LayDuLieuNVL(selectedCongDoans.Select(cd => cd.Columns).ToArray());

            // Lấy chi tiết công đoạn
            var(sqlLayChiTietCD, loaiCD) = CoreHelper.TaoSQL_LayChiTiet_NhieuCD(selectedCongDoans) ;

            // Câu nối các bảng
            string sqlJoin = CoreHelper.TaoSQL_TaoKetNoiCacBang();

            // Format ngày sang dạng SQLite hiểu được
            string ngayBD = ngayBatDau.Date.AddHours(5).AddMinutes(59).ToString("yyyy-MM-dd HH:mm:ss");

            string ngayKT = ngayKetThuc.Date.AddDays(1).AddHours(6).ToString("yyyy-MM-dd HH:mm:ss");

            // Điều kiện WHERE – chèn trực tiếp giá trị ngày
            string sqlDkNgay = $" WHERE date(tclv.Ngay) >= date('{ngayBD}') AND date(tclv.Ngay) <= date('{ngayKT}')";

            // Sắp xếp
            string sqlOrder = " ORDER BY tclv.Ngay DESC, ttp.id DESC;";

            // Ghép chuỗi hoàn chỉnh
            string query = sqlSelect + " ," + sqlLayChiTietCD + " ," + sqlTenNVL + sqlJoin + sqlDkNgay + loaiCD + sqlOrder;

            return GetData(query);
        }

        public static DataTable GetTonKhoCD( List<CongDoan> selectedCongDoans)
        {
            // Tạo phần SELECT chung
            string sqlSelect = CoreHelper.TaoSqL_LayThongTinBaoCaoChung();

            // Lấy dữ liệu NVL theo danh sách công đoạn
            string sqlTenNVL = CoreHelper.TaoSQL_LayDuLieuNVL(selectedCongDoans.Select(cd => cd.Columns).ToArray());

            // Lấy chi tiết công đoạn
            var (sqlLayChiTietCD, loaiCD) = CoreHelper.TaoSQL_LayChiTiet_NhieuCD(selectedCongDoans);

            // Câu nối các bảng
            string sqlJoin = CoreHelper.TaoSQL_TaoKetNoiCacBang();

            loaiCD = loaiCD.Replace("AND", "WHERE")
                    + @"
                        AND (
                            (ds.DonVi = 'KG' AND ttp.KhoiLuongSau <> 0)
                            OR
                            (ds.DonVi = 'M' AND ttp.ChieuDaiSau <> 0)
                        )
                    ";


            // Sắp xếp
            string sqlOrder = " ORDER BY tclv.Ngay DESC, ttp.id DESC;";

            // Ghép chuỗi hoàn chỉnh
            string query = sqlSelect + " ," + sqlLayChiTietCD + " ," + sqlTenNVL + sqlJoin +  loaiCD + sqlOrder;

            return GetData(query);
        }

        public static DataTable GetThongTinNVLTheoMaBin(string mabin)
        {
            string query = @"
                SELECT                     
                    TP.id AS STT,
                    SP_NVL.Ten AS TenNVL,
                    NVL.BinNVL as MaBin,
                    SP_NVL.Ma AS MaNVL,
                    NVL.KlBatDau,
                    NVL.CdBatDau,
                    NVL.KlConLai,
                    NVL.CdConLai,
                    NVL.DuongKinhSoiDong,
                    NVL.SoSoi,
                    NVL.KetCauLoi,
                    NVL.DuongKinhSoiMach,
                    NVL.BanRongBang,
                    NVL.DoDayBang
                FROM TTThanhPham AS TP
                LEFT JOIN TTNVL AS NVL ON TP.id = NVL.TTThanhPham_ID
                LEFT JOIN DanhSachMaSP AS SP_NVL ON NVL.DanhSachMaSP_ID = SP_NVL.id
                WHERE TP.MaBin = @mabin;
            ";

            return GetData(query, mabin, "mabin");
        }

        public static List<PrinterModel> GetPrinterDataByListBin(List<string> listBin)
        {
            List<PrinterModel> result = new List<PrinterModel>();


            // 1. Tạo danh sách parameter @bin0, @bin1,...
            var paramNames = listBin.Select((bin, index) => "@bin" + index).ToList();
            string inClause = string.Join(",", paramNames);

            // 2. SQL truy vấn
            string query = $@"
                SELECT  
                    t.Ngay AS NgaySX,
                    t.Ca AS CaSX,
                    tp.QC AS QC,
                    tp.KhoiLuongSau AS KhoiLuong,
                    tp.ChieuDaiSau AS ChieuDai,
                    d.ten AS TenSP,
                    tp.MaBin AS MaBin,
                    d.ma AS MaSP,
                    t.NguoiLam AS TenCN,
                    tp.GhiChu AS GhiChu
                FROM TTThanhPham tp
                LEFT JOIN ThongTinCaLamViec t ON t.TTThanhPham_id = tp.id
                JOIN DanhSachMaSP d ON tp.DanhSachSP_ID = d.id
                WHERE tp.MaBin IN ({inClause});
                ";

            using (SQLiteConnection conn = new SQLiteConnection(_connStr))
            {
                conn.Open();

                using (SQLiteCommand cmd = new SQLiteCommand(query, conn))
                {
                    // 3. Gán giá trị cho từng parameter
                    for (int i = 0; i < listBin.Count; i++)
                    {
                        cmd.Parameters.AddWithValue("@bin" + i, listBin[i]);
                    }

                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string qc = CoreHelper.GetString(reader, "QC").Trim();
                            string ghiChu = CoreHelper.GetString(reader, "GhiChu");

                            var model = new PrinterModel
                            {
                                NgaySX = DateTime.TryParse(CoreHelper.GetString(reader, "NgaySX"), out DateTime d) ? d.ToString("dd/MM/yyyy") : "",
                                CaSX = CoreHelper.GetString(reader, "CaSX"),
                                KhoiLuong = CoreHelper.GetString(reader, "KhoiLuong"),
                                ChieuDai = CoreHelper.GetString(reader, "ChieuDai"),
                                TenSP = CoreHelper.GetString(reader, "TenSP"),
                                MaBin = CoreHelper.GetString(reader, "MaBin"),
                                MaSP = CoreHelper.GetString(reader, "MaSP"),
                                DanhGia = "",
                                QC = qc,
                                TenCN = CoreHelper.GetString(reader, "TenCN"),
                                GhiChu = ghiChu
                            };

                            result.Add(model);
                        }
                    }
                }
            }

            return result;
        }

        public static ConfigDB GetConfig()
        {

            using (var conn = new SQLiteConnection(_connStr))
            {
                conn.Open();

                string sql = "SELECT Active,Author, Message FROM ConfigDB ORDER BY ID DESC  LIMIT 1";

                using (var cmd = new SQLiteCommand(sql, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new ConfigDB
                        {
                            Active = reader.IsDBNull(0) ? false : reader.GetBoolean(0),
                            Author = reader.IsDBNull(1) ? null : reader.GetString(1),
                            Message = reader.IsDBNull(2) ? null : reader.GetString(2)
                        };
                    }
                }
            }

            return null; // Trường hợp bảng rỗng
        }
        #endregion

        #region Update dữ liệu

        public static int UpdateTrangThaiSX(HashSet<(string Lot, int TrangThai, string Ten)> items,
            string userUpdate)
        {
            int result = 0;
            using var conn = new SQLiteConnection(_connStr);
            SQLiteTransaction tx = null;

            try
            {
                conn.Open();                 
                tx = conn.BeginTransaction(); 
                result = UpdateTrangThaiSX_ByLots(conn,tx ,items, userUpdate);
                tx.Commit();
            }
            catch { }
            return result;
        }

        public static int UpdateTrangThaiSX_ByLots( SQLiteConnection conn, SQLiteTransaction tx, HashSet<(string Lot, int TrangThai, string Ten)> items, string userUpdate)
        {
            var list = items
                .Where(x => !string.IsNullOrWhiteSpace(x.Lot))
                .Select(x => (Lot: x.Lot.Trim(), TrangThai: x.TrangThai, Ten: (x.Ten ?? "").Trim()))
                .Where(x => x.Ten.Length > 0)
                .ToList();

            if (list.Count == 0) return 0;

            const string sql = @"
            UPDATE KeHoachSX
            SET UpdateTrangThaiSX = @userUpdate,
                TrangThaiSX = @tt
            WHERE Lot = @lot
              AND EXISTS (
                  SELECT 1
                  FROM DanhSachMaSP sp
                  WHERE sp.id = KeHoachSX.DanhSachMaSP_ID
                    AND sp.Ten = @ten
              );";

            using var cmd = new SQLiteCommand(sql, conn, tx);

            var pUser = cmd.CreateParameter();
            pUser.ParameterName = "@userUpdate";
            pUser.DbType = DbType.String;
            cmd.Parameters.Add(pUser);

            var pTt = cmd.CreateParameter();
            pTt.ParameterName = "@tt";
            pTt.DbType = DbType.Int32;
            cmd.Parameters.Add(pTt);

            var pLot = cmd.CreateParameter();
            pLot.ParameterName = "@lot";
            pLot.DbType = DbType.String;
            cmd.Parameters.Add(pLot);

            var pTen = cmd.CreateParameter();
            pTen.ParameterName = "@ten";
            pTen.DbType = DbType.String;
            cmd.Parameters.Add(pTen);

            int affectedTotal = 0;

            foreach (var (lot, tt, ten) in list)
            {
                pUser.Value = userUpdate ?? string.Empty;
                pLot.Value = lot;
                pTt.Value = tt;
                pTen.Value = ten;

                affectedTotal += cmd.ExecuteNonQuery();
            }

            return affectedTotal;
        }

        public static bool UpdateNguoiKiemTra(List<int> listStt, string nguoiKT)
        {
            if (listStt == null || listStt.Count == 0)
                return false;

            int rows = 0;

            try
            {
                using (var con = new SQLiteConnection(_connStr))
                {
                    con.Open();

                    using (var cmd = con.CreateCommand())
                    {
                        // tạo param hàng loạt @p0, @p1, ...
                        List<string> paramNames = new List<string>();
                        int i = 0;

                        foreach (int stt in listStt)
                        {
                            string p = "@p" + i;
                            paramNames.Add(p);
                            cmd.Parameters.AddWithValue(p, stt);
                            i++;
                        }

                        // param tên tổ trưởng
                        cmd.Parameters.AddWithValue("@nguoiKT", nguoiKT);

                        cmd.CommandText = $@"
                            UPDATE ThongTinCaLamViec
                            SET ToTruong = @nguoiKT
                            WHERE TTThanhPham_id IN ({string.Join(",", paramNames)});
                        ";

                        rows = cmd.ExecuteNonQuery();
                    }
                }

                return rows > 0;
            }
            catch (Exception ex)
            {
               return false;
            }
        }

        private static void UpdateKL_CD_TTThanhPham(SQLiteConnection conn, SQLiteTransaction tx, List<TTNVL> nvlList, long thongTinSpId)
        {
            const string sql = @"
                UPDATE TTThanhPham
                SET KhoiLuongSau = @KhoiLuongSau,
                    ChieuDaiSau = @ChieuDaiSau,
                    QC = @QC,
                    LastEdit_ID = @LastEdit_ID
                WHERE MaBin = @MaBin;";

            using var cmd = new SQLiteCommand(sql, conn, tx);
            cmd.Parameters.Add("@KhoiLuongSau", System.Data.DbType.Double);
            cmd.Parameters.Add("@ChieuDaiSau", System.Data.DbType.Double);
            cmd.Parameters.Add("@LastEdit_ID", System.Data.DbType.Int64);
            cmd.Parameters.Add("@MaBin", System.Data.DbType.String);
            cmd.Parameters.Add("@QC", System.Data.DbType.String);

            foreach (var nvl in nvlList)
            {
                cmd.Parameters["@KhoiLuongSau"].Value = nvl.KlConLai;
                cmd.Parameters["@ChieuDaiSau"].Value = nvl.CdConLai;
                cmd.Parameters["@LastEdit_ID"].Value = thongTinSpId;
                cmd.Parameters["@QC"].Value = nvl.QC;
                cmd.Parameters["@MaBin"].Value = nvl.BinNVL;

                cmd.ExecuteNonQuery(); 
            }
        }

        // Main update function
        public static bool UpdateDataSanPham(int tpId, ThongTinCaLamViec caLam, TTThanhPham tp, List<TTNVL> nvl, List<object> chiTietCD, out string errorMsg)
        {
            using var conn = new SQLiteConnection(_connStr);
            conn.Open();
            using var tx = conn.BeginTransaction();

            CaiDatCDBoc caiDat = (CaiDatCDBoc)chiTietCD[1];

            errorMsg = string.Empty;

            try
            {
                // 0) Lưu lịch sử thay đổi KL & CD vào bảng TTThanhPham để phục vụ mục đích truy xuất sau này (nếu có)
                BackupThongTinTruocKhiSua(conn, tx, tpId, tp, caLam.NguoiLam);

                // 1) ThongTinCaLamViec
                UpdateThongTinCaLamViec(conn, tx, caLam, tpId);

                // 2) TTThanhPham
                UpdateTTThanhPham(conn, tx, tp, tpId,nvl);

                // restore lại
                RestoreFromNVL(conn, tx,tpId);

                // 3.1) Update Khối lượng sau, Chiều dài sau và thêm ID được update ở TTThanhPham 
                UpdateKhoiLuongConLai_TTThanhPham(conn, tx, nvl, tpId);

                // 3.2) TTNVL -> Xoá nvl cũ và Tạo mới
                Del_InsertTTNVL(conn, tx, tpId, nvl);


                // 4) CaiDatCDBoc - Nếu có
                if (caiDat != null) UpdateCaiDatCDBoc(conn, tx, tpId, caiDat);

                // 5) Thêm chi tiết các công đoạn
                switch (chiTietCD[0])
                {
                    case CD_BenRuot ben:
                        UpdateCDBenRuot(conn, tx, tpId, ben);
                        break;

                    case CD_KeoRut keo:
                        UpdateCDKeoRut(conn, tx, tpId, keo);
                        break;

                    case CD_GhepLoiQB qb:
                        UpdateCDGhepLoiQB(conn, tx, tpId, qb);
                        break;

                    case CD_BocMach mach:
                        UpdateCDBocMach(conn, tx, tpId, mach);
                        break;

                    case CD_BocLot lotBL:
                        UpdateCDBocLot(conn, tx, tpId, lotBL);
                        break;

                    case CD_BocVo vo:
                        UpdateCDBocVo(conn, tx, tpId, vo);

                        // Cập nhật trạng thái của kế hoạch sx
                        //var parts = CoreHelper.CatMaBin(tp.MaBin);
                        //string lot = parts.Length == 5 ? parts[1] : parts[0];
                        //string ttUpdate = $"{caLam.NguoiLam}_Ca {caLam.Ca}";

                        //int trangThai = 2;  
                        //var items = new HashSet<(string Lot, int TrangThai, string Ten)>{
                        //    (Lot: lot, TrangThai: trangThai, Ten: tp.TenTP)
                        //};

                        //UpdateTrangThaiSX_ByLots(conn, tx, items, ttUpdate);


                        break;

                    default:
                        throw new ArgumentException("Lỗi bất thường.");
                }

                tx.Commit();
                return true;
            }
            catch (Exception ex)
            {
                tx.Rollback();

                errorMsg = CoreHelper.ShowErrorDatabase(ex, tp.MaBin);

                return false;
            }

        }

        private static void BackupThongTinTruocKhiSua( SQLiteConnection conn,  SQLiteTransaction tx,  long tpId, TTThanhPham tp, string nguoiSua)
        {
            // 1) Lấy dữ liệu cũ từ TTThanhPham + JOIN DanhSachMaSP để lấy Ten
            const string sqlGetCu = @"
            SELECT 
                ttp.MaBin, 
                ttp.KhoiLuongSau, 
                ttp.ChieuDaiSau,
                ttp.GhiChu,
                ds.Ten
            FROM TTThanhPham ttp
            LEFT JOIN DanhSachMaSP ds ON ds.id = ttp.DanhSachSP_ID
            WHERE ttp.id = @tpId;";

            string lotCu = null;
            decimal klCu = 0;
            decimal cdCu = 0;
            string tenCu = null;
            string ghiChuCu = "";
            

            using (var cmd = new SQLiteCommand(sqlGetCu, conn, tx))
            {
                cmd.Parameters.AddWithValue("@tpId", tpId);
                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    lotCu = reader["MaBin"]?.ToString();
                    klCu = reader["KhoiLuongSau"] != DBNull.Value ? Convert.ToDecimal(reader["KhoiLuongSau"]) : 0;
                    cdCu = reader["ChieuDaiSau"] != DBNull.Value ? Convert.ToDecimal(reader["ChieuDaiSau"]) : 0;
                    tenCu = reader["Ten"] != DBNull.Value ? reader["Ten"].ToString() : null;
                    ghiChuCu = reader["GhiChu"] != DBNull.Value ? reader["GhiChu"].ToString() : "";
                }
            }

            // 2) Insert vào LichSuSuaDoiThongTin
            const string sqlInsertLichSu = @"
            INSERT INTO LichSuSuaDoiThongTin
                (TTThanhPham_ID, NguoiSua, Ten_Cu, Ten_Moi, LOT_Cu, LOT_Moi, KL_Cu, KL_Moi, CD_Cu, CD_Moi, DateInsert, TenMay, GhiChu_Cu, GhiChu_Moi)
            VALUES
                (@TTThanhPham_ID, @NguoiSua, @Ten_Cu,@Ten_Moi, @LOT_Cu, @LOT_Moi, @KL_Cu, @KL_Moi, @CD_Cu, @CD_Moi, @DateInsert, @TenMay, @GhiChu_Cu, @GhiChu_Moi);
            SELECT last_insert_rowid();";

            long lichSuId;
            using (var cmd = new SQLiteCommand(sqlInsertLichSu, conn, tx))
            {
                cmd.Parameters.AddWithValue("@TTThanhPham_ID", tpId);
                cmd.Parameters.AddWithValue("@NguoiSua", string.IsNullOrWhiteSpace(nguoiSua) ? "Unknown" : nguoiSua.Trim());
                cmd.Parameters.AddWithValue("@Ten_Cu", tenCu ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Ten_Moi", tp.TenTP ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@LOT_Cu", lotCu ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@LOT_Moi", tp.MaBin ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@KL_Cu", klCu);
                cmd.Parameters.AddWithValue("@KL_Moi", tp.KhoiLuongSau);
                cmd.Parameters.AddWithValue("@CD_Cu", cdCu);
                cmd.Parameters.AddWithValue("@CD_Moi", tp.ChieuDaiSau);
                cmd.Parameters.AddWithValue("@DateInsert", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                cmd.Parameters.AddWithValue("@TenMay", Properties.Settings.Default.TenMay);
                cmd.Parameters.AddWithValue("@GhiChu_Cu", ghiChuCu);
                cmd.Parameters.AddWithValue("@GhiChu_Moi", tp.GhiChu);

                lichSuId = Convert.ToInt64(cmd.ExecuteScalar());
            }

            // 3) Lấy danh sách NVL cũ từ TTNVL theo tpId
            const string sqlGetNVL = @"
            SELECT BinNVL
            FROM TTNVL
            WHERE TTThanhPham_ID = @tpId;";

            var binNVLList = new List<string>();
            using (var cmd = new SQLiteCommand(sqlGetNVL, conn, tx))
            {
                cmd.Parameters.AddWithValue("@tpId", tpId);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var bin = reader["BinNVL"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(bin))
                        binNVLList.Add(bin);
                }
            }

            // 4) Insert từng BinNVL vào ListNVLThayDoi
            const string sqlInsertNVL = @"
                INSERT INTO ListNVLThayDoi (LichSuSuaDoiThongTin_ID, LOT)
                VALUES (@LichSuSuaDoiThongTin_ID, @LOT);";

            using (var cmd = new SQLiteCommand(sqlInsertNVL, conn, tx))
            {
                var pLichSuId = cmd.Parameters.Add("@LichSuSuaDoiThongTin_ID", DbType.Int64);
                var pLot = cmd.Parameters.Add("@LOT", DbType.String);

                pLichSuId.Value = lichSuId;

                foreach (var bin in binNVLList)
                {
                    pLot.Value = bin;
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private static void UpdateThongTinCaLamViec(SQLiteConnection conn, SQLiteTransaction tx, ThongTinCaLamViec m, int id)
        {
            string sqlUpdate = @"UPDATE ThongTinCaLamViec 
                        SET Ngay = @Ngay,
                            May = @May,
                            Ca = @Ca,
                            NguoiLam = @NguoiLam,
                            ToTruong = @ToTruong,
                            QuanDoc = @QuanDoc
                        WHERE TTThanhPham_id = @id";

            using (var cmd = new SQLiteCommand(sqlUpdate, conn, tx))
            {
                cmd.Parameters.AddWithValue("@Ngay", m.Ngay);
                cmd.Parameters.AddWithValue("@May", m.May);
                cmd.Parameters.AddWithValue("@Ca", m.Ca);
                cmd.Parameters.AddWithValue("@NguoiLam", m.NguoiLam);
                cmd.Parameters.AddWithValue("@ToTruong", m.ToTruong ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@QuanDoc", m.QuanDoc ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@id", id);

                int rowsAffected = cmd.ExecuteNonQuery();

                if (rowsAffected == 0)
                {
                    throw new Exception($"Không tìm thấy hoặc không thể update ThongTinCaLamViec cho TTThanhPham id = {id}");
                }
            }
        }
                
        private static void UpdateTTThanhPham(SQLiteConnection conn, SQLiteTransaction tx, TTThanhPham m, int thongTinCaLamViecId, List<TTNVL> nvl)
        {
            string sqlUpdate = @"UPDATE TTThanhPham 
                                SET DanhSachSP_ID = @DanhSachSP_ID,
                                    MaBin = @MaBin,
                                    KhoiLuongTruoc = @KhoiLuongTruoc,
                                    KhoiLuongSau = @KhoiLuongSau,
                                    ChieuDaiTruoc = @ChieuDaiTruoc,
                                    ChieuDaiSau = @ChieuDaiSau,
                                    HanNoi = @HanNoi,
                                    Phe = @Phe,
                                    GhiChu = @GhiChu
                                WHERE id = @id";
            m.GhiChu = m.GhiChu + "- Đã sửa";


            using (var cmd = new SQLiteCommand(sqlUpdate, conn, tx))
            {
                cmd.Parameters.AddWithValue("@DanhSachSP_ID", m.DanhSachSP_ID);
                cmd.Parameters.AddWithValue("@MaBin", m.MaBin);
                cmd.Parameters.AddWithValue("@KhoiLuongTruoc", m.KhoiLuongTruoc);
                cmd.Parameters.AddWithValue("@KhoiLuongSau", m.KhoiLuongSau);
                cmd.Parameters.AddWithValue("@ChieuDaiTruoc", m.ChieuDaiTruoc);
                cmd.Parameters.AddWithValue("@ChieuDaiSau", m.ChieuDaiSau);
                cmd.Parameters.AddWithValue("@Phe", m.Phe);
                cmd.Parameters.AddWithValue("@HanNoi", m.HanNoi);
                cmd.Parameters.AddWithValue("@GhiChu", m.GhiChu ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@id", thongTinCaLamViecId);

                int rowsAffected = cmd.ExecuteNonQuery();

                if (rowsAffected == 0)
                {
                    throw new Exception($"Không tìm thấy hoặc không thể update TTThanhPham với id = {m.Id}");
                }
            }
        }

        private static void Del_InsertTTNVL(SQLiteConnection conn, SQLiteTransaction tx, long thongTinSpId, List<TTNVL> items)
        {
            // Restore cho dữ liệu cũ 


            // Xoá dữ liệu cũ
            using (var cmd = new SQLiteCommand(conn))
            {
                cmd.Transaction = tx;
                cmd.CommandText = @"DELETE FROM TTNVL WHERE TTThanhPham_ID = @TTThanhPham_ID";
                cmd.Parameters.AddWithValue("@TTThanhPham_ID", thongTinSpId);
                cmd.ExecuteNonQuery();
            }
            // Thêm dữ liệu mới
            InsertTTNVL(conn, tx, thongTinSpId, items);
        }

        private static void UpdateKhoiLuongConLai_TTThanhPham( SQLiteConnection conn, SQLiteTransaction tx, List<TTNVL> nvlList,long thongTinSpId)
        {
            if (conn == null) throw new ArgumentNullException(nameof(conn));
            if (tx == null) throw new ArgumentNullException(nameof(tx));
            if (nvlList == null || nvlList.Count == 0) return;

            using var cmd = new SQLiteCommand(@"
            UPDATE TTThanhPham
               SET  KhoiLuongSau = @kl,
                    QC = @QC,
                    ChieuDaiSau  = @cd,
                    LastEdit_id = @lastEditId
             WHERE MaBin       = @mabin ;", conn, tx);

            var pKL = cmd.Parameters.Add("@kl", DbType.Double);
            var QC = cmd.Parameters.Add("@QC", DbType.String);
            var pCD = cmd.Parameters.Add("@cd", DbType.Double);
            var pBin = cmd.Parameters.Add("@mabin", DbType.String);
            var pLE = cmd.Parameters.Add("@lastEditId", DbType.Int64);

            pLE.Value = thongTinSpId;

            foreach (var nvl in nvlList)
            {
                if (nvl == null || string.IsNullOrWhiteSpace(nvl.BinNVL))
                    continue;

                pKL.Value = nvl.KlConLai;
                QC.Value = nvl.QC;
                pCD.Value = nvl.CdConLai;
                pBin.Value = nvl.BinNVL.Trim();

                cmd.ExecuteNonQuery(); 
            }
        }

        private static void UpdateCaiDatCDBoc(SQLiteConnection conn, SQLiteTransaction tx, long id, CaiDatCDBoc m)
        {
            string query = @"
                UPDATE CaiDatCDBoc
                SET 
                    MangNuoc = @MangNuoc,
                    PuliDanDay = @PuliDanDay,
                    BoDemMet = @BoDemMet,
                    MayIn = @MayIn,
                    v1 = @v1,
                    v2 = @v2,
                    v3 = @v3,
                    v4 = @v4,
                    v5 = @v5,
                    v6 = @v6,
                    Co = @Co,
                    Dau1 = @Dau1,
                    Dau2 = @Dau2,
                    Khuon = @Khuon,
                    BinhSay = @BinhSay,
                    DKKhuon1 = @DKKhuon1,
                    DKKhuon2 = @DKKhuon2,
                    TTNhua = @TTNhua,
                    NhuaPhe = @NhuaPhe,
                    GhiChuNhuaPhe = @GhiChuNhuaPhe,
                    DayPhe = @DayPhe,
                    GhiChuDayPhe = @GhiChuDayPhe,
                    KTDKLan1 = @KTDKLan1,
                    KTDKLan2 = @KTDKLan2,
                    KTDKLan3 = @KTDKLan3,
                    DiemMongLan1 = @DiemMongLan1,
                    DiemMongLan2 = @DiemMongLan2
                WHERE TTThanhPham_ID = @TTThanhPham_ID;
            ";

            using (var cmd = new SQLiteCommand(query, conn, tx))
            {
                cmd.Parameters.AddWithValue("@TTThanhPham_ID", id);


                cmd.Parameters.AddWithValue("@MangNuoc", (object?)m.MangNuoc ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@PuliDanDay", (object?)m.PuliDanDay ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@BoDemMet", (object?)m.BoDemMet ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@MayIn", (object?)m.MayIn ?? DBNull.Value);

                cmd.Parameters.AddWithValue("@v1", (object?)m.v1 ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@v2", (object?)m.v2 ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@v3", (object?)m.v3 ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@v4", (object?)m.v4 ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@v5", (object?)m.v5 ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@v6", (object?)m.v6 ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Co", (object?)m.Co ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Dau1", (object?)m.Dau1 ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Dau2", (object?)m.Dau2 ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Khuon", (object?)m.Khuon ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@BinhSay", (object?)m.BinhSay ?? DBNull.Value);

                cmd.Parameters.AddWithValue("@DKKhuon1", m.DKKhuon1);
                cmd.Parameters.AddWithValue("@DKKhuon2", m.DKKhuon2);
                cmd.Parameters.AddWithValue("@TTNhua", m.TTNhua ?? string.Empty);
                cmd.Parameters.AddWithValue("@NhuaPhe", m.NhuaPhe);
                cmd.Parameters.AddWithValue("@GhiChuNhuaPhe", (object?)m.GhiChuNhuaPhe ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@DayPhe", m.DayPhe);
                cmd.Parameters.AddWithValue("@GhiChuDayPhe", (object?)m.GhiChuDayPhe ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@KTDKLan1", m.KTDKLan1);
                cmd.Parameters.AddWithValue("@KTDKLan2", m.KTDKLan2);
                cmd.Parameters.AddWithValue("@KTDKLan3", m.KTDKLan3);
                cmd.Parameters.AddWithValue("@DiemMongLan1", m.DiemMongLan1);
                cmd.Parameters.AddWithValue("@DiemMongLan2", m.DiemMongLan2);

                cmd.ExecuteNonQuery();
            }
        }

        private static void UpdateCDBocLot(SQLiteConnection conn, SQLiteTransaction tx, long id, CD_BocLot m)
        {
            const string sql = @"
                UPDATE CD_BocLot
                SET DoDayTBLot = @DoDayTBLot
                WHERE CaiDatCDBoc_ID IN (
                    SELECT id FROM CaiDatCDBoc WHERE TTThanhPham_ID = @TTThanhPham_ID
                );";

            using var cmd = new SQLiteCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@DoDayTBLot", m.DoDayTBLot);
            cmd.Parameters.AddWithValue("@TTThanhPham_ID", id);
            cmd.ExecuteNonQuery();
        }
        
        private static void UpdateCDBocVo(SQLiteConnection conn, SQLiteTransaction tx, long id, CD_BocVo m)
        {
            const string sql = @"
                UPDATE CD_BocVo
                SET DayVoTB = @DayVoTB,
                    InAn = @InAn
                WHERE CaiDatCDBoc_ID IN (
                    SELECT id FROM CaiDatCDBoc WHERE TTThanhPham_ID = @TTThanhPham_ID
                );";

            using var cmd = new SQLiteCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@DayVoTB", m.DayVoTB);
            cmd.Parameters.AddWithValue("@InAn", m.InAn ?? string.Empty);
            cmd.Parameters.AddWithValue("@TTThanhPham_ID", id);
            cmd.ExecuteNonQuery();
        }

        private static void UpdateCDBocMach(SQLiteConnection conn, SQLiteTransaction tx, long id, CD_BocMach m)
        {
            const string sql = @"
            UPDATE CD_BocMach
            SET 
                NgoaiQuan = @NgoaiQuan,
                LanDanhThung = @LanDanhThung,
                SoMet = @SoMet
            WHERE CaiDatCDBoc_ID IN (
                SELECT id FROM CaiDatCDBoc WHERE TTThanhPham_ID = @TTThanhPham_ID
            );";

            using var cmd = new SQLiteCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@NgoaiQuan", m.NgoaiQuan ?? "1");
            cmd.Parameters.AddWithValue("@LanDanhThung", m.LanDanhThung);
            cmd.Parameters.AddWithValue("@SoMet", m.SoMet);
            cmd.Parameters.AddWithValue("@TTThanhPham_ID", id);
            cmd.ExecuteNonQuery();
        }

        private static void UpdateCDKeoRut(SQLiteConnection conn, SQLiteTransaction tx, long thongTinSpId, CD_KeoRut m)
        {
            const string sql = @"
            UPDATE CD_KeoRut
            SET DKTrucX = @DKTrucX,
                DKTrucY = @DKTrucY,
                NgoaiQuan = @NgoaiQuan,
                TocDo = @TocDo,
                DienApU = @DienApU,
                DongDienU = @DongDienU
            WHERE TTThanhPham_ID = @TTThanhPham_ID;";

            using var cmd = new SQLiteCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@DKTrucX", m.DKTrucX);
            cmd.Parameters.AddWithValue("@DKTrucY", m.DKTrucY);
            cmd.Parameters.AddWithValue("@NgoaiQuan", m.NgoaiQuan ?? string.Empty);
            cmd.Parameters.AddWithValue("@TocDo", m.TocDo);
            cmd.Parameters.AddWithValue("@DienApU", m.DienApU);
            cmd.Parameters.AddWithValue("@DongDienU", m.DongDienU);
            cmd.Parameters.AddWithValue("@TTThanhPham_ID", thongTinSpId);
            cmd.ExecuteNonQuery();
        }

        private static void UpdateCDBenRuot(SQLiteConnection conn, SQLiteTransaction tx, long thongTinSpId, CD_BenRuot m)
        {
            const string sql = @"
            UPDATE CD_BenRuot
            SET DKSoi = @DKSoi,
                SoSoi = @SoSoi,
                ChieuXoan = @ChieuXoan,
                BuocBen = @BuocBen
            WHERE TTThanhPham_ID = @TTThanhPham_ID;";

            using var cmd = new SQLiteCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@DKSoi", m.DKSoi);
            cmd.Parameters.AddWithValue("@SoSoi", (object?)m.SoSoi ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ChieuXoan", m.ChieuXoan ?? "Z");
            cmd.Parameters.AddWithValue("@BuocBen", m.BuocBen);
            cmd.Parameters.AddWithValue("@TTThanhPham_ID", thongTinSpId);
            cmd.ExecuteNonQuery();
        }

        private static void UpdateCDGhepLoiQB(SQLiteConnection conn, SQLiteTransaction tx, long thongTinSpId, CD_GhepLoiQB m)
        {
            //const string sql = @"
            //UPDATE CD_GhepLoiQB
            //SET BuocXoan = @BuocXoan,
            //    ChieuXoan = @ChieuXoan,
            //    GoiCachMep = @GoiCachMep,
            //    DKBTP = @DKBTP
            //WHERE TTThanhPham_ID = @TTThanhPham_ID;";

            const string sql = @"
            UPDATE CD_GhepLoiQB
            SET ChieuXoan = @ChieuXoan,
                GoiCachMep = @GoiCachMep,
                DKBTP = @DKBTP
            WHERE TTThanhPham_ID = @TTThanhPham_ID;";

            using var cmd = new SQLiteCommand(sql, conn, tx);
            //cmd.Parameters.AddWithValue("@BuocXoan", m.BuocXoan);
            cmd.Parameters.AddWithValue("@ChieuXoan", m.ChieuXoan ?? "Z");
            cmd.Parameters.AddWithValue("@GoiCachMep", m.GoiCachMep);
            cmd.Parameters.AddWithValue("@DKBTP", m.DKBTP);
            cmd.Parameters.AddWithValue("@TTThanhPham_ID", thongTinSpId);
            cmd.ExecuteNonQuery();
        }

        public static string UpdateDanhSachMaSP(DanhSachMaSP sp, int key)
        {
            string query = @"
                UPDATE DanhSachMaSP
                SET 
                    Ten = @Ten,
                    Ten_KhongDau = @Ten_KhongDau,
                    Ma = @Ma,
                    DonVi = @DonVi,
                    KieuSP = @KieuSP,
                    ChuyenDoi = @ChuyenDoi,
                    DateInsert = @DateInsert
                WHERE id = @Id;
            ";

            try
            {
                using (var conn = new SQLiteConnection(_connStr))
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Ten", sp.Ten);
                        cmd.Parameters.AddWithValue("@Ten_KhongDau", sp.Ten_KhongDau);
                        cmd.Parameters.AddWithValue("@Ma", sp.Ma);
                        cmd.Parameters.AddWithValue("@DonVi", sp.DonVi);
                        cmd.Parameters.AddWithValue("@KieuSP", sp.KieuSP);
                        cmd.Parameters.AddWithValue("@ChuyenDoi", sp.ChuyenDoi);
                        cmd.Parameters.AddWithValue("@DateInsert", sp.DateInsert);
                        cmd.Parameters.AddWithValue("@Id", key);

                        int rowsAffected = cmd.ExecuteNonQuery();

                        // Nếu cập nhật thành công → trả về chuỗi rỗng
                        if (rowsAffected > 0)
                            return "";
                        else
                            return $"Không tìm thấy bản ghi với Tên = {sp.Ten}";
                    }
                }
            }
            catch (Exception ex)
            {
                // Trả về lỗi từ helper
                return CoreHelper.ShowErrorDatabase(ex, sp.Ten);
            }
        }

        public static string UpdateKLConLai_BanTran(BanTran bt)
        {
            string query = @"
                UPDATE TTThanhPham
                SET 
                    KhoiLuongSau = @KhoiLuongSau,
                    KLBanTran = COALESCE(KLBanTran, 0) + @KhoiLuongBanTran
                WHERE MaBin = @MaBin;
            ";

            try
            {
                using (var conn = new SQLiteConnection(_connStr))
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@KhoiLuongSau", bt.KhoiLuongSau);
                        cmd.Parameters.AddWithValue("@KhoiLuongBanTran", bt.KhoiLuongBanTran);
                        cmd.Parameters.AddWithValue("@MaBin", bt.MaBin);

                        int rowsAffected = cmd.ExecuteNonQuery();

                        if (rowsAffected > 0)
                            return ""; // cập nhật thành công → trả về chuỗi rỗng
                        else
                            return $"Không tìm thấy bản ghi với MaBin = {bt.MaBin}";
                    }
                }
            }
            catch (Exception ex)
            {
                // Gọi helper hiển thị lỗi giống phong cách bạn đang dùng
                return CoreHelper.ShowErrorDatabase(ex, bt.MaBin);
            }
        }
        #endregion

        #region Insert dữ liệu các công đoạn

        public static bool SaveTachBin(ThongTinCaLamViec caLam, List<TTThanhPham> list_tp, List<TTNVL> nvl, out string errorMsg)
        {
            errorMsg = string.Empty;
            using var conn = new SQLiteConnection(_connStr);
            SQLiteTransaction tx = null;

            try
            {
                conn.Open();                  
                tx = conn.BeginTransaction();

                foreach (TTThanhPham tp in list_tp)
                {                

                    // 1) Tạo mới thông tin thành phẩm
                    long tpId = InsertTTThanhPham(conn, tx, tp, nvl);

                    // 2) ThongTinCaLamViec
                    InsertThongTinCaLamViec(conn, tx, caLam, tpId);

                    // 3) TTNVL -> Tạo mới
                    InsertTTNVL(conn, tx, tpId, nvl);

                    // 3.1) Update Khối lượng sau, Chiều dài sau và thêm ID được update ở TTThanhPham 
                    List<TTNVL> t = new List<TTNVL>
                    {
                        new TTNVL{
                            BinNVL = nvl[0].BinNVL,
                            KlConLai = 0,
                            CdConLai = 0,
                            QC = tp.QC
                        }
                    };
                    
                    UpdateKL_CD_TTThanhPham(conn, tx, t, tpId);
                }
                tx.Commit();



                return true;
            }
            catch (Exception ex)
            {
                try { tx?.Rollback(); } catch { /* nuốt lỗi rollback để không che mất lỗi chính */ }

                errorMsg = CoreHelper.ShowErrorDatabase(ex);
                return false;
            }

        }

        public static bool SaveDataSanPham( ThongTinCaLamViec caLam, TTThanhPham tp, List<TTNVL> nvl, List<object> chiTietCD, out string errorMsg)
        {
            errorMsg = string.Empty;

            // Guard clauses
            if (tp == null)
            {
                errorMsg = "Thiếu thông tin thành phẩm.";
                return false;
            }
            if (chiTietCD == null || chiTietCD.Count == 0 || chiTietCD[0] == null)
            {
                errorMsg = "Thiếu chi tiết công đoạn.";
                return false;
            }

            long idCaiDatCDBoc = 0;
            using var conn = new SQLiteConnection(_connStr);
            SQLiteTransaction tx = null;

            try
            {
                conn.Open();                  // ✅ MỞ TRƯỚC
                tx = conn.BeginTransaction(); // ✅ RỒI MỚI BẮT ĐẦU TRANSACTION

                // 1) TTThanhPham -> Tạo mới thành phẩm
                long tpId = InsertTTThanhPham(conn, tx, tp, nvl);

                // 2) ThongTinCaLamViec -> tạo mới thông tin ca làm việc
                InsertThongTinCaLamViec(conn, tx, caLam, tpId);

                // 3) TTNVL -> Tạo mới nguyên vật liệu
                InsertTTNVL(conn, tx, tpId, nvl);

                // 3.1) Update Khối lượng sau, Chiều dài sau và thêm ID được update ở TTThanhPham 
                UpdateKL_CD_TTThanhPham(conn, tx, nvl, tpId);
                    

                // 4) CaiDatCDBoc (chỉ áp dụng cho nhóm bóc)
                var congDoan = chiTietCD[0];

                if (congDoan is CD_BocLot || congDoan is CD_BocVo || congDoan is CD_BocMach)
                {
                    // chỉ lấy caiDat nếu có phần tử thứ 2 và đúng kiểu
                    CaiDatCDBoc caiDat = (chiTietCD.Count > 1) ? chiTietCD[1] as CaiDatCDBoc : null;
                    if (caiDat != null)
                        idCaiDatCDBoc = InsertCaiDatCDBoc(conn, tx, tpId, caiDat);
                }

                // 5) Thêm chi tiết các công đoạn
                switch (congDoan)
                {
                    case CD_KeoRut keo:
                        InsertCDKeoRut(conn, tx, tpId, keo);

                        break;

                    case CD_BenRuot ben:
                        InsertCDBenRuot(conn, tx, tpId, ben);
                        break;

                    case CD_GhepLoiQB qb:
                       InsertCDGhepLoiQB(conn, tx, tpId, qb);
                        break;

                    case CD_BocLot bocLot:
                        InsertCDBocLot(conn, tx, idCaiDatCDBoc, bocLot);
                        break;

                    case CD_BocMach mach:
                        InsertCDBocMach(conn, tx, idCaiDatCDBoc, mach);
                        break;

                    case CD_BocVo vo:
                        InsertCDBocVo(conn, tx, idCaiDatCDBoc, vo);

                        // Cập nhật trạng thái của kế hoạch sx
                        //var parts = CoreHelper.CatMaBin(tp.MaBin);
                        //string lot = parts.Length == 5 ? parts[1] : parts[0];
                        //string ttUpdate = $"{caLam.NguoiLam}_Ca {caLam.Ca}";

                        //int trangThai = 2;  // EnumStore.TrangThaiThucHienTheoKH[2] = "Đã xong"
                        //var items = new HashSet<(string Lot, int TrangThai, string Ten)>{
                        //    (Lot: lot, TrangThai: trangThai, Ten: tp.TenTP)
                        //};

                        //UpdateTrangThaiSX_ByLots(conn, tx, items, ttUpdate);


                        break;

                    default:
                        throw new ArgumentException("Lỗi bất thường: Công đoạn không hợp lệ.");
                }

                tx.Commit(); // ✅ nhớ commit
                return true;
            }
            catch (Exception ex)
            {
                try { tx?.Rollback(); } catch { /* nuốt lỗi rollback để không che mất lỗi chính */ }

                errorMsg = CoreHelper.ShowErrorDatabase(ex, tp.MaBin);
                return false;
            }
        }

        private static void RestoreFromNVL(SQLiteConnection conn, SQLiteTransaction tx, long tpId)
        {
            if (conn == null) throw new ArgumentNullException(nameof(conn));
            if (tx == null) throw new ArgumentNullException(nameof(tx));

            // Update TTThanhPham theo đúng điều kiện:
            // tp.LastEdit_id = nvl.TTThanhPham_ID  và  tp.MaBin = nvl.BinNVL
            // đồng thời chỉ update cho đúng tpId (tpId = ttthanhpham_id của dòng TTNVL)
            const string sql = @"
            UPDATE TTThanhPham AS tp
            SET
              KhoiLuongSau = (
                SELECT nvl.KlBatDau
                FROM TTNVL AS nvl
                WHERE nvl.TTThanhPham_ID = tp.LastEdit_id
                  AND nvl.BinNVL = tp.MaBin
              ),
              ChieuDaiSau = (
                SELECT nvl.CdBatDau
                FROM TTNVL AS nvl
                WHERE nvl.TTThanhPham_ID = tp.LastEdit_id
                  AND nvl.BinNVL = tp.MaBin
              ),
              LastEdit_id = NULL 
            WHERE tp.LastEdit_id = @tpId
              AND EXISTS (
                SELECT 1
                FROM TTNVL AS nvl
                WHERE nvl.TTThanhPham_ID = tp.LastEdit_id
                  AND nvl.BinNVL = tp.MaBin
              );";

            using var cmd = new SQLiteCommand(sql, conn, tx);
            cmd.CommandType = CommandType.Text;
            cmd.Parameters.AddWithValue("@tpId", tpId);

            var rowsAffected = cmd.ExecuteNonQuery();
        }
        
        private static long InsertThongTinCaLamViec(SQLiteConnection conn, SQLiteTransaction tx, ThongTinCaLamViec m, long id)
        {
            const string sql = @"
            INSERT INTO ThongTinCaLamViec (Ngay,TTThanhPham_id, May, Ca, NguoiLam, ToTruong, QuanDoc)
            VALUES (@Ngay, @TTThanhPham_id, @May, @Ca, @NguoiLam, @ToTruong, @QuanDoc);
            SELECT last_insert_rowid();";
            using var cmd = new SQLiteCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@Ngay", m.Ngay);
            cmd.Parameters.AddWithValue("@TTThanhPham_id", id);
            cmd.Parameters.AddWithValue("@May", m.May);
            cmd.Parameters.AddWithValue("@Ca", m.Ca);
            cmd.Parameters.AddWithValue("@NguoiLam", m.NguoiLam);
            cmd.Parameters.AddWithValue("@ToTruong", (object?)m.ToTruong ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@QuanDoc", (object?)m.QuanDoc ?? DBNull.Value);
            return (long)(cmd.ExecuteScalar() ?? 0L);
        }

        private static long InsertTTThanhPham(SQLiteConnection conn, SQLiteTransaction tx, TTThanhPham m, List<TTNVL> nvl)
        {

            const string sql = @"
            INSERT INTO TTThanhPham
                (DanhSachSP_ID,QC ,  MaBin, KhoiLuongTruoc, KhoiLuongSau, ChieuDaiTruoc, ChieuDaiSau, Phe, CongDoan, GhiChu,HanNoi, DateInsert)
            VALUES
                (@DanhSachSP_ID,@QC,  @MaBin, @KhoiLuongTruoc, @KhoiLuongSau, @ChieuDaiTruoc, @ChieuDaiSau, @Phe, @CongDoan, @GhiChu, @HanNoi, @DateInsert);
            SELECT last_insert_rowid();";


            using var cmd = new SQLiteCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@DanhSachSP_ID", m.DanhSachSP_ID);
            cmd.Parameters.AddWithValue("@QC", m.QC);
            cmd.Parameters.AddWithValue("@MaBin", m.MaBin);
            cmd.Parameters.AddWithValue("@KhoiLuongTruoc", m.KhoiLuongTruoc);
            cmd.Parameters.AddWithValue("@KhoiLuongSau", m.KhoiLuongSau);
            cmd.Parameters.AddWithValue("@ChieuDaiTruoc", m.ChieuDaiTruoc);
            cmd.Parameters.AddWithValue("@ChieuDaiSau", m.ChieuDaiSau);
            cmd.Parameters.AddWithValue("@Phe", m.Phe);
            cmd.Parameters.AddWithValue("@CongDoan", m.CongDoan.Id);
            cmd.Parameters.AddWithValue("@GhiChu", (object?)m.GhiChu ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@HanNoi", m.HanNoi);
            cmd.Parameters.AddWithValue("@DateInsert", (object?)m.DateInsert ?? DBNull.Value);
            return (long)(cmd.ExecuteScalar() ?? 0L);
        }

        private static void InsertTTNVL(SQLiteConnection conn, SQLiteTransaction tx, long thongTinSpId, List<TTNVL> items)
        {
            if (items == null || items.Count == 0) return;

            const string sql = @"
            INSERT INTO TTNVL
                (TTThanhPham_ID, BinNVL,QC, DanhSachMaSP_ID, KlBatDau, CdBatDau, KlConLai, CdConLai, DuongKinhSoiDong, SoSoi, KetCauLoi, DuongKinhSoiMach, BanRongBang, DoDayBang)
            VALUES
                (@TTThanhPham_ID, @BinNVL,@QC,@DanhSachMaSP_ID, @KlBatDau, @CdBatDau, @KlConLai, @CdConLai, @DuongKinhSoiDong, @SoSoi, @KetCauLoi, @DuongKinhSoiMach, @BanRongBang, @DoDayBang);";

            using var cmd = new SQLiteCommand(sql, conn, tx);

            var pThongTinSP_ID = cmd.Parameters.Add("@TTThanhPham_ID", DbType.Int64);
            var pBinNVL = cmd.Parameters.Add("@BinNVL", DbType.String);
            var DanhSachMaSP_ID = cmd.Parameters.Add("@DanhSachMaSP_ID", DbType.Int64);
            var KlBatDau = cmd.Parameters.Add("@KlBatDau", DbType.Double);
            var QC = cmd.Parameters.Add("@QC", DbType.String);
            var CdBatDau = cmd.Parameters.Add("@CdBatDau", DbType.Double);
            var KlConLai = cmd.Parameters.Add("@KlConLai", DbType.Double);
            var CdConLai = cmd.Parameters.Add("@CdConLai", DbType.Double);
            var pDuongKinhSoiDong = cmd.Parameters.Add("@DuongKinhSoiDong", DbType.Double);
            var pSoSoi = cmd.Parameters.Add("@SoSoi", DbType.Int32);
            var pKetCauLoi = cmd.Parameters.Add("@KetCauLoi", DbType.Double);
            var pDuongKinhSoiMach = cmd.Parameters.Add("@DuongKinhSoiMach", DbType.Double);
            var pBanRongBang = cmd.Parameters.Add("@BanRongBang", DbType.Double);
            var pDoDayBang = cmd.Parameters.Add("@DoDayBang", DbType.Double);

            foreach (TTNVL m in items)
            {
                pThongTinSP_ID.Value = thongTinSpId;
                pBinNVL.Value = m.BinNVL ?? string.Empty;
                DanhSachMaSP_ID.Value = m.DanhSachMaSP_ID;
                KlBatDau.Value = m.KlBatDau;
                CdBatDau.Value = m.CdBatDau;
                KlConLai.Value = m.KlConLai;
                CdConLai.Value = m.CdConLai;
                QC.Value = m.QC;
                pDuongKinhSoiDong.Value = m.DuongKinhSoiDong;
                pSoSoi.Value = m.SoSoi;
                pKetCauLoi.Value = m.KetCauLoi;
                pDuongKinhSoiMach.Value = m.DuongKinhSoiMach;
                pBanRongBang.Value = m.BanRongBang;
                pDoDayBang.Value = m.DoDayBang;

                cmd.ExecuteNonQuery();
            }
        }

        private static void InsertCDBocLot(SQLiteConnection conn, SQLiteTransaction tx, long id, CD_BocLot m)
        {
            const string sql = @"
            INSERT INTO CD_BocLot (CaiDatCDBoc_ID, DoDayTBLot)
            VALUES (@CaiDatCDBoc_ID, @DoDayTBLot);";

            using var cmd = new SQLiteCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@CaiDatCDBoc_ID", id);
            cmd.Parameters.AddWithValue("@DoDayTBLot", m.DoDayTBLot);
            cmd.ExecuteNonQuery();
        }

        private static void InsertCDBocVo(SQLiteConnection conn, SQLiteTransaction tx, long id, CD_BocVo m)
        {
            const string sql = @"
            INSERT INTO CD_BocVo (CaiDatCDBoc_ID, DayVoTB, InAn)
            VALUES (@CaiDatCDBoc_ID, @DayVoTB, @InAn);";

            using var cmd = new SQLiteCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@CaiDatCDBoc_ID", id);
            cmd.Parameters.AddWithValue("@DayVoTB", m.DayVoTB);
            cmd.Parameters.AddWithValue("@InAn", m.InAn ?? string.Empty);
            cmd.ExecuteNonQuery();
        }

        private static long InsertCaiDatCDBoc(SQLiteConnection conn, SQLiteTransaction tx, long tpId, CaiDatCDBoc m)
        {
            const string sql = @"
            INSERT INTO CaiDatCDBoc
            (TTThanhPham_ID, MangNuoc, PuliDanDay, BoDemMet, MayIn,
             v1, v2, v3, v4, v5, v6, Co, Dau1, Dau2, Khuon, BinhSay,
             DKKhuon1, DKKhuon2, TTNhua, NhuaPhe, GhiChuNhuaPhe, DayPhe, GhiChuDayPhe,
             KTDKLan1, KTDKLan2, KTDKLan3, DiemMongLan1, DiemMongLan2)
            VALUES
            (@TTThanhPham_ID, @MangNuoc, @PuliDanDay, @BoDemMet, @MayIn,
             @v1, @v2, @v3, @v4, @v5, @v6, @Co, @Dau1, @Dau2, @Khuon, @BinhSay,
             @DKKhuon1, @DKKhuon2, @TTNhua, @NhuaPhe, @GhiChuNhuaPhe, @DayPhe, @GhiChuDayPhe,
             @KTDKLan1, @KTDKLan2, @KTDKLan3, @DiemMongLan1, @DiemMongLan2);";

            using var cmd = new SQLiteCommand(sql, conn, tx);

            cmd.Parameters.AddWithValue("@TTThanhPham_ID", tpId);
            cmd.Parameters.AddWithValue("@MangNuoc", (object?)m.MangNuoc ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@PuliDanDay", (object?)m.PuliDanDay ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@BoDemMet", (object?)m.BoDemMet ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@MayIn", (object?)m.MayIn ?? DBNull.Value);

            cmd.Parameters.AddWithValue("@v1", (object?)m.v1 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@v2", (object?)m.v2 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@v3", (object?)m.v3 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@v4", (object?)m.v4 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@v5", (object?)m.v5 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@v6", (object?)m.v6 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Co", (object?)m.Co ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Dau1", (object?)m.Dau1 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Dau2", (object?)m.Dau2 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Khuon", (object?)m.Khuon ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@BinhSay", (object?)m.BinhSay ?? DBNull.Value);

            cmd.Parameters.AddWithValue("@DKKhuon1", m.DKKhuon1);
            cmd.Parameters.AddWithValue("@DKKhuon2", m.DKKhuon2);
            cmd.Parameters.AddWithValue("@TTNhua", m.TTNhua ?? string.Empty);
            cmd.Parameters.AddWithValue("@NhuaPhe", m.NhuaPhe);
            cmd.Parameters.AddWithValue("@GhiChuNhuaPhe", (object?)m.GhiChuNhuaPhe ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@DayPhe", m.DayPhe);
            cmd.Parameters.AddWithValue("@GhiChuDayPhe", (object?)m.GhiChuDayPhe ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@KTDKLan1", m.KTDKLan1);
            cmd.Parameters.AddWithValue("@KTDKLan2", m.KTDKLan2);
            cmd.Parameters.AddWithValue("@KTDKLan3", m.KTDKLan3);
            cmd.Parameters.AddWithValue("@DiemMongLan1", m.DiemMongLan1);
            cmd.Parameters.AddWithValue("@DiemMongLan2", m.DiemMongLan2);

            try
            {
                cmd.ExecuteNonQuery();
                return conn.LastInsertRowId;
            }
            catch (Exception ex)
            {
                throw new Exception("Lỗi khi thêm dữ liệu vào bảng CaiDatCDBoc.", ex);
            }

        }

        private static void InsertCDBocMach(SQLiteConnection conn, SQLiteTransaction tx, long id, CD_BocMach m)
        {
            const string sql = @"
            INSERT INTO CD_BocMach (CaiDatCDBoc_ID, NgoaiQuan, LanDanhThung, SoMet, Mau)
            VALUES (@CaiDatCDBoc_ID, @NgoaiQuan, @LanDanhThung, @SoMet, @Mau);";

            using var cmd = new SQLiteCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@CaiDatCDBoc_ID", id);
            cmd.Parameters.AddWithValue("@NgoaiQuan", m.NgoaiQuan ?? "1"); // default theo schema
            cmd.Parameters.AddWithValue("@LanDanhThung", m.LanDanhThung);
            cmd.Parameters.AddWithValue("@SoMet", m.SoMet);
            cmd.Parameters.AddWithValue("@Mau", m.Mau);
            cmd.ExecuteNonQuery();
        }

        private static void InsertCDKeoRut(SQLiteConnection conn, SQLiteTransaction tx, long thongTinSpId, CD_KeoRut m)
        {
            const string sql = @"
            INSERT INTO CD_KeoRut
            (TTThanhPham_ID, DKTrucX, DKTrucY, NgoaiQuan, TocDo, DienApU, DongDienU)
            VALUES
            (@TTThanhPham_ID, @DKTrucX, @DKTrucY, @NgoaiQuan, @TocDo, @DienApU, @DongDienU);";

            using var cmd = new SQLiteCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@TTThanhPham_ID", thongTinSpId);
            cmd.Parameters.AddWithValue("@DKTrucX", m.DKTrucX);
            cmd.Parameters.AddWithValue("@DKTrucY", m.DKTrucY);
            cmd.Parameters.AddWithValue("@NgoaiQuan", m.NgoaiQuan ?? string.Empty);
            cmd.Parameters.AddWithValue("@TocDo", m.TocDo);
            cmd.Parameters.AddWithValue("@DienApU", m.DienApU);
            cmd.Parameters.AddWithValue("@DongDienU", m.DongDienU);
            cmd.ExecuteNonQuery();
        }

        private static void InsertCDBenRuot(SQLiteConnection conn, SQLiteTransaction tx, long thongTinSpId, CD_BenRuot m)
        {
            // Lưu ý cột "Chiều Xoắn" có dấu và khoảng trắng -> cần trích dẫn bằng dấu "
            const string sql = @"
            INSERT INTO CD_BenRuot
            (TTThanhPham_ID, DKSoi, SoSoi, ChieuXoan, BuocBen)
            VALUES
            (@TTThanhPham_ID, @DKSoi, @SoSoi, @ChieuXoan, @BuocBen);";

            using var cmd = new SQLiteCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@TTThanhPham_ID", thongTinSpId);
            cmd.Parameters.AddWithValue("@DKSoi", m.DKSoi);
            cmd.Parameters.AddWithValue("@SoSoi", (object?)m.SoSoi ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ChieuXoan", m.ChieuXoan ?? "Z");
            cmd.Parameters.AddWithValue("@BuocBen", m.BuocBen);
            cmd.ExecuteNonQuery();
        }

        private static void InsertCDGhepLoiQB(SQLiteConnection conn, SQLiteTransaction tx, long thongTinSpId, CD_GhepLoiQB m)
        {
            //const string sql = @"
            //INSERT INTO CD_GhepLoiQB
            //(TTThanhPham_ID, BuocXoan, ChieuXoan, GoiCachMep, DKBTP)
            //VALUES
            //(@TTThanhPham_ID, @BuocXoan, @ChieuXoan, @GoiCachMep, @DKBTP);";


            const string sql = @"
            INSERT INTO CD_GhepLoiQB
            (TTThanhPham_ID, ChieuXoan, GoiCachMep, DKBTP)
            VALUES
            (@TTThanhPham_ID,  @ChieuXoan, @GoiCachMep, @DKBTP);";


            using var cmd = new SQLiteCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@TTThanhPham_ID", thongTinSpId);
            //cmd.Parameters.AddWithValue("@BuocXoan", m.BuocXoan);
            cmd.Parameters.AddWithValue("@ChieuXoan", m.ChieuXoan);
            cmd.Parameters.AddWithValue("@GoiCachMep", m.GoiCachMep);
            cmd.Parameters.AddWithValue("@DKBTP", m.DKBTP);
            cmd.ExecuteNonQuery();
        }

        public static string InsertDSMaSP(DanhSachMaSP sp)
        {
            try
            {
                using var conn = new SQLiteConnection(_connStr);
                conn.Open();
                using var tx = conn.BeginTransaction();

                string sql = @"
                    INSERT INTO DanhSachMaSP (Ten, Ten_KhongDau, Ma, DonVi, KieuSP,ChuyenDoi,DateInsert)
                    VALUES (@Ten,@Ten_KhongDau, @Ma, @DonVi, @KieuSP, @ChuyenDoi, @DateInsert);
                ";

                using (var cmd = new SQLiteCommand(sql, conn, tx))
                {
                    cmd.Parameters.AddWithValue("@Ten", sp.Ten);
                    cmd.Parameters.AddWithValue("@Ten_KhongDau", sp.Ten_KhongDau);
                    cmd.Parameters.AddWithValue("@Ma", sp.Ma);
                    cmd.Parameters.AddWithValue("@DonVi", sp.DonVi);
                    cmd.Parameters.AddWithValue("@KieuSP", sp.KieuSP);
                    cmd.Parameters.AddWithValue("@ChuyenDoi", sp.ChuyenDoi);
                    cmd.Parameters.AddWithValue("@DateInsert", sp.DateInsert ?? DateTime.Now);

                    cmd.ExecuteNonQuery();
                }

                tx.Commit();
                return ""; 
            }
            catch (Exception ex)
            {
                return CoreHelper.ShowErrorDatabase(ex, sp.Ma);
            }
        }
        #endregion

        #region setup config
        public static bool InsertConfig(ConfigDB config)
        {
            string query = "INSERT INTO ConfigDB (Active, Author, Message, Ngay) VALUES (@active, @author, @message, @ngay)";
            bool flg = false;
            try
            {
                using (SQLiteConnection conn = new SQLiteConnection(_connStr))
                {
                    conn.Open();

                    using (SQLiteCommand cmd = new SQLiteCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@active", config.Active);
                        cmd.Parameters.AddWithValue("@author", config.Author);
                        cmd.Parameters.AddWithValue("@message", config.Message);
                        cmd.Parameters.AddWithValue("@ngay", config.Ngay);

                        cmd.ExecuteNonQuery();
                    }

                    conn.Close();
                    flg = true;
                }
            }
            catch (Exception) { }

            return flg;

        }
        #endregion

        #region User
       
        // 1) Tạo user mới + gán roles (chỉ INSERT)
        public static bool CreateUserWithRoles(string username, string passwordHash, string name, List<int> roleIds, bool is_active = true)
        {

            roleIds = NormalizeRoleIds(roleIds);

            using var conn = new SQLiteConnection(_connStr);
            conn.Open();
            using var tran = conn.BeginTransaction();

            try
            {                

                long userId;

                using (var ins = new SQLiteCommand(@"
                INSERT INTO users(username, password_hash, name, is_active)
                VALUES(@u, @ph, @n, @ia);", conn, tran))
                {
                    ins.Parameters.AddWithValue("@u", username);
                    ins.Parameters.AddWithValue("@ph", passwordHash);
                    ins.Parameters.AddWithValue("@n", name);
                    ins.Parameters.AddWithValue("@ia", is_active ? 1 : 0);
                    ins.ExecuteNonQuery();
                    userId = conn.LastInsertRowId;
                }

                SyncUserRoles(conn, tran, userId, roleIds);

                tran.Commit();
                return true;
            }
            catch
            {
                tran.Rollback();
                throw; 
            }
        }

        public static bool UpdateUserWithRoles(string username, string? passwordHash, string name, List<int> roleIds, bool is_active = true)
        {
            roleIds = NormalizeRoleIds(roleIds);

            using var conn = new SQLiteConnection(_connStr);
            conn.Open();
            using var tran = conn.BeginTransaction();

            try
            {
                long userId = GetUserIdByUsername(conn, tran, username);
                if (userId <= 0) throw new InvalidOperationException("Không tìm thấy user để cập nhật.");

                if (!string.IsNullOrWhiteSpace(passwordHash))
                {
                    using var upd = new SQLiteCommand(@"
                    UPDATE users
                    SET password_hash=@ph,
                        name=@n,
                        is_active=@ia
                    WHERE user_id=@id;", conn, tran);

                    upd.Parameters.AddWithValue("@ph", passwordHash);
                    upd.Parameters.AddWithValue("@n", name);
                    upd.Parameters.AddWithValue("@ia", is_active ? 1 : 0);
                    upd.Parameters.AddWithValue("@id", userId);
                    upd.ExecuteNonQuery();
                }
                else
                {
                    using var upd = new SQLiteCommand(@"
                    UPDATE users
                    SET name=@n,
                        is_active=@ia
                    WHERE user_id=@id;", conn, tran);

                    upd.Parameters.AddWithValue("@n", name);
                    upd.Parameters.AddWithValue("@ia", is_active ? 1 : 0);
                    upd.Parameters.AddWithValue("@id", userId);
                    upd.ExecuteNonQuery();
                }

                SyncUserRoles(conn, tran, userId, roleIds);

                tran.Commit();
                return true;
            }
            catch
            {
                tran.Rollback();
                throw;
            }
        }
        private static List<int> NormalizeRoleIds(List<int>? roleIds)
        {
            return (roleIds ?? new List<int>())
                .Where(r => r > 0)
                .Distinct()
                .ToList();
        }

        public static async Task<List<string>> QueryAsync(string typed, CancellationToken ct)
        {
            string Esc(string s) => s.Replace(@"\", @"\\").Replace("%", @"\%").Replace("_", @"\_");
            var list = new List<string>();
            var prefix = Esc(typed) + "%";

            using (var conn = new SQLiteConnection(_connStr)) // 👈 using (không có await)
            {
                await conn.OpenAsync(ct);
                using (var cmd = conn.CreateCommand()) // 👈 using (không có await)
                {
                    cmd.CommandText = @"
                SELECT username
                FROM users
                WHERE @t = '' OR username LIKE @p ESCAPE '\'
                ORDER BY username
                LIMIT 20;";
                    cmd.Parameters.AddWithValue("@t", typed);
                    cmd.Parameters.AddWithValue("@p", prefix);

                    using (var r = await cmd.ExecuteReaderAsync(ct)) // 👈 using (không có await)
                    {
                        while (await r.ReadAsync(ct))
                        {
                            list.Add(r.GetString(0));
                        }
                    }
                }
            }
            return list;
        }

        private static long GetUserIdByUsername(SQLiteConnection conn, SQLiteTransaction tran, string username)
        {
            using var cmd = new SQLiteCommand("SELECT user_id FROM users WHERE username=@u LIMIT 1;", conn, tran);
            cmd.Parameters.AddWithValue("@u", username);
            var obj = cmd.ExecuteScalar();
            if (obj == null || obj == DBNull.Value) return -1;
            return Convert.ToInt64(obj);
        }

        // Sync roles: xóa role không còn + thêm role mới (INSERT OR IGNORE)
        private static void SyncUserRoles(SQLiteConnection conn, SQLiteTransaction tran, long userId, List<int> roleIds)
        {
            // DELETE role không còn được chọn
            if (roleIds.Count == 0)
            {
                using var delAll = new SQLiteCommand("DELETE FROM user_roles WHERE user_id=@uid;", conn, tran);
                delAll.Parameters.AddWithValue("@uid", userId);
                delAll.ExecuteNonQuery();
            }
            else
            {
                var paramNames = roleIds.Select((_, i) => $"@r{i}").ToArray();
                var sqlDel = $"DELETE FROM user_roles WHERE user_id=@uid AND role_id NOT IN ({string.Join(",", paramNames)});";

                using var del = new SQLiteCommand(sqlDel, conn, tran);
                del.Parameters.AddWithValue("@uid", userId);
                for (int i = 0; i < roleIds.Count; i++)
                    del.Parameters.AddWithValue(paramNames[i], roleIds[i]);

                del.ExecuteNonQuery();
            }

            // INSERT role đã chọn
            using var insUR = new SQLiteCommand(
                "INSERT OR IGNORE INTO user_roles(user_id, role_id) VALUES(@uid, @rid);",
                conn, tran);

            var pUid = insUR.Parameters.Add("@uid", DbType.Int64);
            var pRid = insUR.Parameters.Add("@rid", DbType.Int32);
            pUid.Value = userId;

            insUR.Prepare();

            foreach (var rid in roleIds)
            {
                pRid.Value = rid;
                insUR.ExecuteNonQuery();
            }
        }

        public static async Task<UserInfo> GetUserWithRolesByUsernameAsync(string username, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(username)) return null;

            using (var conn = new SQLiteConnection(_connStr))
            {
                await conn.OpenAsync(ct);

                // 1) user
                UserInfo u = null;
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                    SELECT user_id, username, name, is_active, created_at
                    FROM users
                    WHERE username = @u
                    LIMIT 1;";
                    cmd.Parameters.AddWithValue("@u", username.Trim());

                    using (var r = await cmd.ExecuteReaderAsync(ct))
                    {
                        if (!await r.ReadAsync(ct)) return null;

                        u = new UserInfo
                        {
                            UserId = r.GetInt32(0),
                            Username = r.GetString(1),
                            Name = r.IsDBNull(2) ? "" : r.GetString(2),
                            IsActive = r.GetInt32(3) == 1,
                            CreatedAt = r.IsDBNull(4) ? "" : r.GetString(4)
                        };
                    }
                }

                // 2) roles
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT r.role_name
                        FROM roles r
                        JOIN user_roles ur ON ur.role_id = r.role_id
                        WHERE ur.user_id = @id
                        ORDER BY r.role_id;";
                    cmd.Parameters.AddWithValue("@id", u.UserId);

                    using (var r = await cmd.ExecuteReaderAsync(ct))
                        while (await r.ReadAsync(ct)) u.Roles.Add(r.GetString(0));
                }

                return u;
            }
        }



        // treeeeview
        public static List<UserWithRoles> GetUsersWithSameRoles(int currentUserId)
        {
            var users = new List<UserWithRoles>();

            using (var conn = new SQLiteConnection(_connStr))
            {
                conn.Open();

                string query = @"
                SELECT DISTINCT 
                    u.user_id,
                    u.username,
                    u.name,
                    u.is_active
                FROM users u
                INNER JOIN user_roles ur ON u.user_id = ur.user_id
                WHERE u.is_active = 1
                  AND ur.role_id IN (
                      SELECT role_id 
                      FROM user_roles 
                      WHERE user_id = @currentUserId
                  )
                ORDER BY u.name, u.username";

                using (var cmd = new SQLiteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@currentUserId", currentUserId);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var user = new UserWithRoles
                            {
                                UserId = reader.GetInt32(0),
                                Username = reader.GetString(1),
                                Name = reader.IsDBNull(2) ? reader.GetString(1) : reader.GetString(2),
                                IsActive = reader.GetInt32(3) == 1,
                                Roles = new List<RoleInfo>()
                            };

                            // Load roles cho user này
                            user.Roles = GetUserRoles(conn, user.UserId);

                            users.Add(user);
                        }
                    }
                }
            }

            return users;
        }

        /// <summary>
        /// Lấy danh sách roles của một user
        /// </summary>
        private static List<RoleInfo> GetUserRoles(SQLiteConnection conn, int userId)
        {
            var roles = new List<RoleInfo>();

            string query = @"
            SELECT 
                r.role_id,
                r.role_name,
                r.description
            FROM roles r
            INNER JOIN user_roles ur ON r.role_id = ur.role_id
            WHERE ur.user_id = @userId
            ORDER BY r.role_name";

            using (var cmd = new SQLiteCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@userId", userId);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        roles.Add(new RoleInfo
                        {
                            RoleId = reader.GetInt32(0),
                            RoleName = reader.GetString(1),
                            Description = reader.IsDBNull(2) ? null : reader.GetString(2)
                        });
                    }
                }
            }

            return roles;
        }

        /// <summary>
        /// Load với filter theo role cụ thể
        /// </summary>
        private static List<UserWithRoles> GetUsersByRole(string roleName)
        {
            var users = new List<UserWithRoles>();

            using (var conn = new SQLiteConnection(_connStr))
            {
                conn.Open();

                string query = @"
                SELECT DISTINCT 
                    u.user_id,
                    u.username,
                    u.name,
                    u.is_active
                FROM users u
                INNER JOIN user_roles ur ON u.user_id = ur.user_id
                INNER JOIN roles r ON ur.role_id = r.role_id
                WHERE u.is_active = 1
                  AND r.role_name = @roleName
                ORDER BY u.name, u.username";

                using (var cmd = new SQLiteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@roleName", roleName);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var user = new UserWithRoles
                            {
                                UserId = reader.GetInt32(0),
                                Username = reader.GetString(1),
                                Name = reader.IsDBNull(2) ? reader.GetString(1) : reader.GetString(2),
                                IsActive = reader.GetInt32(3) == 1,
                                Roles = GetUserRoles(conn, reader.GetInt32(0))
                            };

                            users.Add(user);
                        }
                    }
                }
            }

            return users;
        }


        //end tree

        public static void LoadQuyenTheoRole(int roleId, DataGridView grvQuyen)
        {
            // Lấy toàn bộ permission + đánh dấu những permission đã thuộc roleId
            const string sql = @"
                SELECT 
                    p.permission_id,
                    p.permission_code,
                    p.permission_name,
                    CASE WHEN EXISTS (
                        SELECT 1 
                        FROM role_permissions rp
                        WHERE rp.role_id = @roleId
                          AND rp.permission_id = p.permission_id
                    ) THEN 1 ELSE 0 END AS IsChecked
                FROM permissions p;
            ";

            // Nếu grid có row trống cuối cùng, tắt nó để không bị Add nhầm
            grvQuyen.AllowUserToAddRows = false;

            grvQuyen.AutoGenerateColumns = false;
            grvQuyen.Rows.Clear();

            using (var conn = new SQLiteConnection(_connStr))
            using (var cmd = new SQLiteCommand(sql, conn))
            {
                conn.Open();
                cmd.Parameters.AddWithValue("@roleId", roleId);

                using (var rd = cmd.ExecuteReader())
                {
                    while (rd.Read())
                    {
                        int rowIndex = grvQuyen.Rows.Add();

                        grvQuyen.Rows[rowIndex].Cells[0].Value = rd.GetInt32(rd.GetOrdinal("IsChecked")) == 1;
                        grvQuyen.Rows[rowIndex].Cells[1].Value = EnumStore.TransferPermissionName[rd["permission_code"]?.ToString()];
                        grvQuyen.Rows[rowIndex].Cells[2].Value = rd["permission_name"]?.ToString();
                        grvQuyen.Rows[rowIndex].Cells[3].Value = rd["permission_code"]?.ToString();

                        grvQuyen.Rows[rowIndex].Tag = rd.GetInt32(rd.GetOrdinal("permission_id"));

                    }
                }
            }
        }

        // Giữ nguyên hàm cũ để tương thích ngược
        public static void SaveRolePermissions_ByGrid(int roleId, DataGridView grvQuyen)
        {
            // Commit trạng thái checkbox vừa click
            grvQuyen.EndEdit();
            var selected = new HashSet<int>();
            // 1) Lấy permission_id được tick từ grid
            foreach (DataGridViewRow row in grvQuyen.Rows)
            {
                if (row.IsNewRow) continue;
                bool isChecked = row.Cells["cb"].Value != null &&
                                 row.Cells["cb"].Value != DBNull.Value &&
                                 Convert.ToBoolean(row.Cells["cb"].Value);
                if (!isChecked) continue;
                if (row.Tag == null) continue;
                selected.Add(Convert.ToInt32(row.Tag));
            }

            using (var conn = new SQLiteConnection(_connStr))
            {
                conn.Open();
                using (var tran = conn.BeginTransaction())
                {
                    // 2) Lấy permission_id hiện có của role trong DB
                    var existing = new HashSet<int>();
                    using (var cmd = new SQLiteCommand(
                        @"SELECT permission_id FROM role_permissions WHERE role_id = @roleId;", conn, tran))
                    {
                        cmd.Parameters.AddWithValue("@roleId", roleId);
                        using (var rd = cmd.ExecuteReader())
                        {
                            while (rd.Read())
                                existing.Add(Convert.ToInt32(rd["permission_id"]));
                        }
                    }

                    // 3) INSERT những cái mới tick
                    using (var cmdIns = new SQLiteCommand(
                        @"INSERT OR IGNORE INTO role_permissions(role_id, permission_id)
                    VALUES (@roleId, @pid);", conn, tran))
                    {
                        var pRole = cmdIns.Parameters.Add("@roleId", System.Data.DbType.Int32);
                        var pPid = cmdIns.Parameters.Add("@pid", System.Data.DbType.Int32);
                        foreach (var pid in selected)
                        {
                            if (existing.Contains(pid)) continue;
                            pRole.Value = roleId;
                            pPid.Value = pid;
                            cmdIns.ExecuteNonQuery();
                        }
                    }

                    // 4) DELETE những cái bị bỏ tick
                    using (var cmdDel = new SQLiteCommand(
                        @"DELETE FROM role_permissions 
                  WHERE role_id = @roleId AND permission_id = @pid;", conn, tran))
                    {
                        var pRole = cmdDel.Parameters.Add("@roleId", System.Data.DbType.Int32);
                        var pPid = cmdDel.Parameters.Add("@pid", System.Data.DbType.Int32);
                        foreach (var pid in existing)
                        {
                            if (selected.Contains(pid)) continue;
                            pRole.Value = roleId;
                            pPid.Value = pid;
                            cmdDel.ExecuteNonQuery();
                        }
                    }

                    tran.Commit();
                }
            }
        }

        //public static void SaveRolePermissions_ByGrid(int roleId, DataGridView grvQuyen)
        //{
        //    // Commit trạng thái checkbox vừa click
        //    grvQuyen.EndEdit();

        //    var selected = new HashSet<int>();

        //    // 1) Lấy permission_id được tick từ grid
        //    foreach (DataGridViewRow row in grvQuyen.Rows)
        //    {
        //        if (row.IsNewRow) continue;

        //        bool isChecked = row.Cells["cb"].Value != null &&
        //                         row.Cells["cb"].Value != DBNull.Value &&
        //                         Convert.ToBoolean(row.Cells["cb"].Value);

        //        if (!isChecked) continue;

        //        if (row.Tag == null) continue; // không có permission_id thì bỏ qua

        //        selected.Add(Convert.ToInt32(row.Tag));
        //    }

        //    using (var conn = new SQLiteConnection(_connStr))
        //    {
        //        conn.Open();
        //        using (var tran = conn.BeginTransaction())
        //        {
        //            // 2) Lấy permission_id hiện có của role trong DB
        //            var existing = new HashSet<int>();
        //            using (var cmd = new SQLiteCommand(
        //                @"SELECT permission_id FROM role_permissions WHERE role_id = @roleId;", conn, tran))
        //            {
        //                cmd.Parameters.AddWithValue("@roleId", roleId);
        //                using (var rd = cmd.ExecuteReader())
        //                {
        //                    while (rd.Read())
        //                        existing.Add(Convert.ToInt32(rd["permission_id"]));
        //                }
        //            }

        //            // 3) INSERT những cái mới tick
        //            using (var cmdIns = new SQLiteCommand(
        //                @"INSERT OR IGNORE INTO role_permissions(role_id, permission_id)
        //                    VALUES (@roleId, @pid);", conn, tran))
        //            {
        //                var pRole = cmdIns.Parameters.Add("@roleId", System.Data.DbType.Int32);
        //                var pPid = cmdIns.Parameters.Add("@pid", System.Data.DbType.Int32);

        //                foreach (var pid in selected)
        //                {
        //                    if (existing.Contains(pid)) continue;

        //                    pRole.Value = roleId;
        //                    pPid.Value = pid;
        //                    cmdIns.ExecuteNonQuery();
        //                }
        //            }

        //            // 4) DELETE những cái bị bỏ tick
        //            using (var cmdDel = new SQLiteCommand(
        //                @"DELETE FROM role_permissions 
        //                WHERE role_id = @roleId AND permission_id = @pid;", conn, tran))
        //            {
        //                var pRole = cmdDel.Parameters.Add("@roleId", System.Data.DbType.Int32);
        //                var pPid = cmdDel.Parameters.Add("@pid", System.Data.DbType.Int32);

        //                foreach (var pid in existing)
        //                {
        //                    if (selected.Contains(pid)) continue;

        //                    pRole.Value = roleId;
        //                    pPid.Value = pid;
        //                    cmdDel.ExecuteNonQuery();
        //                }
        //            }

        //            tran.Commit();
        //        }
        //    }



        //    MessageBox.Show("Đã lưu quyền cho role!", "Thông báo",
        //        MessageBoxButtons.OK, MessageBoxIcon.Information);
        //}


        // Đăng nhập
        public static LoginResult Login(string usernameInput, string passwordInput)
        {
            LoginResult result = new LoginResult();

            using (SQLiteConnection conn = new SQLiteConnection(_connStr))
            {
                conn.Open();

                int userId = 0;
                string storedHash = null;
                string name = null;

                // ===== QUERY 1: XÁC THỰC USER =====
                string sqlUser = @"
                    SELECT user_id, name, password_hash
                    FROM users
                    WHERE username = @username
                      AND is_active = 1
                    LIMIT 1;
                ";

                using (SQLiteCommand cmd = new SQLiteCommand(sqlUser, conn))
                {
                    cmd.Parameters.AddWithValue("@username", usernameInput);

                    using (SQLiteDataReader rd = cmd.ExecuteReader())
                    {
                        if (!rd.Read())
                        {
                            result.Message = "Sai tài khoản hoặc mật khẩu.";
                            return result;
                        }

                        userId = Convert.ToInt32(rd["user_id"]);
                        name = Convert.ToString(rd["name"]);
                        storedHash = rd["password_hash"].ToString();
                    }
                }

                // Verify BCrypt
                if (!BCrypt.Net.BCrypt.Verify(passwordInput, storedHash))
                {
                    result.Message = "Sai tài khoản hoặc mật khẩu.";
                    return result;
                }

                // ===== QUERY 2: LOAD ROLES + PERMISSIONS =====
                string sqlPerms = @"
                    SELECT
                        r.role_name,
                        r.description AS role_description,
                        p.permission_code
                    FROM user_roles ur
                    JOIN roles r ON r.role_id = ur.role_id
                    LEFT JOIN role_permissions rp ON rp.role_id = r.role_id
                    LEFT JOIN permissions p ON p.permission_id = rp.permission_id
                    WHERE ur.user_id = @user_id;
                ";

                var rolesDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                var permsDict = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

                using (SQLiteCommand cmd = new SQLiteCommand(sqlPerms, conn))
                {
                    cmd.Parameters.AddWithValue("@user_id", userId);

                    using (SQLiteDataReader rd = cmd.ExecuteReader())
                    {
                        while (rd.Read())
                        {
                            // role_name -> description
                            if (rd["role_name"] != DBNull.Value)
                            {
                                string roleName = rd["role_name"].ToString();
                                string roleDesc = rd["role_description"] == DBNull.Value ? null : rd["role_description"].ToString();

                                if (!rolesDict.ContainsKey(roleName))
                                    rolesDict.Add(roleName, roleDesc);

                                // role_name -> set(permission_code)
                                if (rd["permission_code"] != DBNull.Value)
                                {
                                    string permCode = rd["permission_code"].ToString();

                                    if (!permsDict.TryGetValue(roleName, out var set))
                                    {
                                        set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                                        permsDict[roleName] = set;
                                    }

                                    set.Add(permCode);
                                }
                                else
                                {
                                    // đảm bảo role vẫn có key dù chưa gán permission nào
                                    if (!permsDict.ContainsKey(roleName))
                                        permsDict[roleName] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                                }
                            }
                        }
                    }
                }

                // ===== HOÀN TẤT LOGIN =====
                result.Success = true;
                result.UserId = userId;
                result.Name = name;
                result.RolesDict = rolesDict;
                result.PermissionsDict = permsDict;
                result.Message = "OK";

                return result;
            }
        }

        #endregion


        #region KẾ hoạch ===================

        public static Dictionary<string, decimal> LayNVLCanTheoKeHoach(Dictionary<string, decimal> demandPlan, bool kTinhTon)
        {
            using (var connection = new SQLiteConnection(_connStr))
            {
                connection.Open();

                // 1. Lấy danh sách mã sản phẩm từ demandPlan
                var demandKeys = string.Join(",", demandPlan.Keys.Select(key => $"'{key}'"));

                // 2. SQL để lấy dữ liệu từ bảng DanhSachMaSP và BOMStructure
                string sql = $@"
                SELECT sp.Ma, sp.DonVi, sp.KieuSP, sp.Ten, sp.id, bom.ParentProduct, bom.Component, bom.TyLe, bom.TyLeHoanDoi
                FROM DanhSachMaSP sp
                INNER JOIN BOMStructure bom ON bom.Component = sp.id
                WHERE sp.Ten IN ({demandKeys}) AND sp.KieuSP = 'TP'";

                var command = new SQLiteCommand(sql, connection);
                var reader = command.ExecuteReader();

                // 3. Tạo dictionary chứa thông tin về vật liệu (donvi, tyle, v.v.)
                var materials = new Dictionary<string, (string DonVi, string KieuSP, decimal TyLe, decimal TyLeHoanDoi)>();

                while (reader.Read())
                {
                    string ma = reader["Ma"].ToString();
                    string donVi = reader["DonVi"].ToString();
                    string kieuSP = reader["KieuSP"].ToString();
                    decimal tyLe = Convert.ToDecimal(reader["TyLe"]);
                    decimal tyLeHoanDoi = Convert.ToDecimal(reader["TyLeHoanDoi"]);

                    materials[ma] = (donVi, kieuSP, tyLe, tyLeHoanDoi);
                }

                // 4. Tính toán nguyên vật liệu (NVL) cần thiết dựa trên demandPlan
                var result = new Dictionary<string, decimal>();

                foreach (var demand in demandPlan)
                {
                    string ten = demand.Key;
                    decimal quantity = demand.Value;

                    // Kiểm tra nếu vật liệu có trong dữ liệu
                    if (materials.ContainsKey(ten))
                    {
                        var material = materials[ten];

                        if (kTinhTon)
                        {
                            // Trường hợp 1: Tính toán nguyên vật liệu trực tiếp
                            // Tính toán theo TyLe và TyLeHoanDoi
                            decimal nvlRequired = quantity * material.TyLe;
                            decimal nvlConverted = nvlRequired * material.TyLeHoanDoi;
                            result[ten] = nvlConverted;
                        }
                        else
                        {
                            // Trường hợp 2: Tính toán với BTP đã sản xuất
                            // Trừ đi lượng BTP đã sản xuất (dựa vào KhoiLuongSau hoặc ChieuDaiSau)
                            decimal btpProduced = GetProducedBTP(ten, connection); // Truyền kết nối đã mở
                            decimal btpRequired = quantity - btpProduced;
                            decimal nvlRequired = btpRequired * material.TyLe;
                            decimal nvlConverted = nvlRequired * material.TyLeHoanDoi;
                            result[ten] = nvlConverted;
                        }
                    }
                }

                return result;
            }
        }




        private static decimal GetProducedBTP(string maSP, SQLiteConnection connection)
        {
            // Sử dụng kết nối đã mở thay vì mở kết nối mới
            string sql = $@"
            SELECT 
                CASE 
                    WHEN sp.DonVi = 'KG' THEN sp.KhoiLuongSau
                    WHEN sp.DonVi = 'M' THEN sp.ChieuDaiSau
                    ELSE 0 
                END AS ProducedQuantity
            FROM DanhSachMaSP sp
            WHERE sp.Ma = '{maSP}'";

            var command = new SQLiteCommand(sql, connection);
            var result = command.ExecuteScalar();

            // Nếu có giá trị trả về, chuyển sang kiểu decimal, nếu không trả về 0
            return result != DBNull.Value ? Convert.ToDecimal(result) : 0;
        }

        // Hàm hiển thị kết quả (có thể thay thế bằng logic giao diện người dùng của bạn)
        private static void DisplayResults(Dictionary<string, decimal> result)
        {
            foreach (var res in result)
            {
                Console.WriteLine($"Vật liệu: {res.Key}, Cần thiết: {res.Value} kg");
            }
        }

        #endregion


        #region Hạ Bin
        public static void Update_KhoiLuongSau_ChieuDaiSau(string maBin, decimal khoiLuongSau, decimal chieuDaiSau,string ghiChu)
        {
            using (var connection = new SQLiteConnection(_connStr))
            {
                connection.Open();
                string sql = @"
                UPDATE TTThanhPham
                SET KhoiLuongSau = @khoiLuongSau,
                    ChieuDaiSau  = @chieuDaiSau,
                    GhiChu  = @GhiChu
                WHERE MaBin = @maBin;";

                using var command = new SQLiteCommand(sql, connection);
                command.Parameters.AddWithValue("@khoiLuongSau", khoiLuongSau);
                command.Parameters.AddWithValue("@chieuDaiSau", chieuDaiSau);
                command.Parameters.AddWithValue("@GhiChu", ghiChu);
                command.Parameters.AddWithValue("@maBin", maBin);

                int affected = command.ExecuteNonQuery();
                if (affected == 0)
                    throw new Exception($"Không tìm thấy MaBin: {maBin}");
            }
        }


        #endregion

    }


}
