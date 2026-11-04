// Dung alias de phan biet cac model trung ten tai Models va Models.KeToan.
using KiemKeModel = DG_TonKhoBTP_v02.Models.KeToan.KiemKe;
using DanhSachBinModel = DG_TonKhoBTP_v02.Models.KeToan.DanhSachBin;
using UserContext = DG_TonKhoBTP_v02.Models.UserContext;
using System;
using System.Collections.Generic;
using System.Linq;
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

        public static List<string> LoadActiveUsernames()
        {
            var result = new List<string>();

            const string sql = @"
            SELECT username
            FROM users
            WHERE is_active = 1
            ORDER BY username COLLATE NOCASE ASC;";

            using (var conn = DB_Base.OpenConnection())
            using (var cmd = new SQLiteCommand(sql, conn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    string username = reader["username"]?.ToString()?.Trim();

                    if (!string.IsNullOrWhiteSpace(username))
                        result.Add(username);
                }
            }

            return result;
        }

        public static DataTable Load_TTKiemKeThang(string namThang = null, string nguoiKK = null)
        {
            if (string.IsNullOrWhiteSpace(namThang))
            {
                namThang = DateTime.Now.ToString("yyyy-MM");
            }

            const string sql = @"
            SELECT
                kk.DanhSachSP_ID                              AS DanhSachSP_ID,
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
            LEFT JOIN TTThanhPham tp ON tp.id = kk.TTThanhPham_ID AND tp.Temp = 0
            LEFT JOIN DanhSachMaSP sp ON sp.id = COALESCE(kk.DanhSachSP_ID, tp.DanhSachSP_ID)
            WHERE kk.ThoiGianKiemKe = @namThang
              AND (
                    @nguoiKK IS NULL
                    OR TRIM(@nguoiKK) = ''
                    OR kk.NguoiKK COLLATE NOCASE = @nguoiKK COLLATE NOCASE
                  )
            ORDER BY kk.DateInsert DESC, kk.id DESC;";

            using (var conn = DB_Base.OpenConnection())
            using (var cmd = new SQLiteCommand(sql, conn))
            using (var da = new SQLiteDataAdapter(cmd))
            {
                cmd.Parameters.AddWithValue("@namThang", namThang.Trim());
                cmd.Parameters.AddWithValue("@nguoiKK",
                    string.IsNullOrWhiteSpace(nguoiKK)
                        ? (object)DBNull.Value
                        : nguoiKK.Trim());

                var dt = new DataTable();
                da.Fill(dt);

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
                ca.NgayKetThuc            AS NgaySX,
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
            WHERE ttp.Temp = 0
              AND ttp.MaBin = @maBin
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

        public static long InsertTTThanhPham_FromKiemKe(KiemKeModel model)
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
                CongDoan,
                GhiChu,
                DateInsert,
                Temp
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
                @GhiChu,
                @DateInsert,
                0
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

        public static long Insert_TTKiemKeThang(KiemKeModel model)
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
                            TTThanhPham_ID, DanhSachSP_ID, MaBin, ChieuDai, KhoiLuong,
                            GhiChu, ThoiGianKiemKe, ApprovedDate, DateInsert, NguoiKK
                        )
                        VALUES
                        (
                            @TTThanhPham_ID, @DanhSachSP_ID, @MaBin, @ChieuDai, @KhoiLuong,
                            @GhiChu, @ThoiGianKiemKe, @ApprovedDate, @DateInsert, @NguoiKK
                        );", conn, tx))
                        {
                            cmdInsert.Parameters.AddWithValue("@TTThanhPham_ID", (object?)model.TTThanhPham_ID ?? DBNull.Value);
                            cmdInsert.Parameters.AddWithValue("@DanhSachSP_ID", (object?)model.DanhSachSP_ID ?? DBNull.Value);
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

        public static void UpsertDanhSachBin(DanhSachBinModel bin)
        {
            string tenBin = bin.TenBin;
            decimal khoiLuong = bin.KhoiLuongBin;
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

        // Các ID được chọn trên bảng kiểm kê; không tin dữ liệu đã hiển thị trên lưới.
        private sealed class SelectedKiemKeRow
        {
            public long Id { get; set; }
            public long TTThanhPhamId { get; set; }
            public string MaBin { get; set; }
            public decimal KhoiLuong { get; set; }
            public decimal ChieuDai { get; set; }
            public long? DanhSachSpId { get; set; }
        }

        public static int ValidateCapNhatTTThanhPham(List<long> selectedIds)
        {
            using (var conn = DB_Base.OpenConnection())
                return ReadAndValidateSelected(conn, null, selectedIds).Count;
        }

        // Reset + cập nhật + ApprovedDate luôn chung một giao dịch SQLite.
        public static int CapNhatTTThanhPhamTheoKiemKe(List<long> selectedIds, bool resetAll)
        {
            // Kiểm tra ở tầng thực thi, không chỉ dựa vào trạng thái Enabled của nút.
            if (!UserContext.IsAuthenticated || !UserContext.HasRole(RoleNames.Admin))
                throw new UnauthorizedAccessException("Chỉ tài khoản có quyền Admin mới được cập nhật dữ liệu kiểm kê.");

            using (var conn = DB_Base.OpenConnection())
            using (var tx = conn.BeginTransaction())
            {
                try
                {
                    // Kiểm tra LẠI trong giao dịch: dữ liệu có thể thay đổi khi hộp thoại reset đang mở.
                    List<SelectedKiemKeRow> rows = ReadAndValidateSelected(conn, tx, selectedIds);
                    string now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                    if (resetAll)
                    {
                        using (var cmdReset = new SQLiteCommand(
                            @"UPDATE TTThanhPham
                              SET KhoiLuongSau = 0, ChieuDaiSau = 0
                              WHERE CongDoan IS NULL OR CAST(CongDoan AS INTEGER) <> 5;", conn, tx))
                            cmdReset.ExecuteNonQuery();
                    }

                    const string updateSp = @"
                        UPDATE TTThanhPham
                        SET KhoiLuongSau = @KhoiLuong,
                            ChieuDaiSau = @ChieuDai,
                            DanhSachSP_ID = COALESCE(@DanhSachSP_ID, DanhSachSP_ID)
                        WHERE id = @TTThanhPham_ID AND MaBin = @MaBin AND Temp = 0;";
                    const string approve = @"
                        UPDATE TTKiemKeThang SET ApprovedDate = @ApprovedDate
                        WHERE id = @id AND (ApprovedDate IS NULL OR TRIM(ApprovedDate) = '');";

                    using (var cmdUpdate = new SQLiteCommand(updateSp, conn, tx))
                    using (var cmdApprove = new SQLiteCommand(approve, conn, tx))
                    {
                        cmdUpdate.Parameters.Add("@KhoiLuong", DbType.Decimal);
                        cmdUpdate.Parameters.Add("@ChieuDai", DbType.Decimal);
                        cmdUpdate.Parameters.Add("@DanhSachSP_ID", DbType.Int64);
                        cmdUpdate.Parameters.Add("@TTThanhPham_ID", DbType.Int64);
                        cmdUpdate.Parameters.Add("@MaBin", DbType.String);
                        cmdApprove.Parameters.Add("@ApprovedDate", DbType.String).Value = now;
                        cmdApprove.Parameters.Add("@id", DbType.Int64);

                        foreach (SelectedKiemKeRow item in rows)
                        {
                            cmdUpdate.Parameters["@KhoiLuong"].Value = item.KhoiLuong;
                            cmdUpdate.Parameters["@ChieuDai"].Value = item.ChieuDai;
                            cmdUpdate.Parameters["@DanhSachSP_ID"].Value = item.DanhSachSpId.HasValue
                                ? (object)item.DanhSachSpId.Value : DBNull.Value;
                            cmdUpdate.Parameters["@TTThanhPham_ID"].Value = item.TTThanhPhamId;
                            cmdUpdate.Parameters["@MaBin"].Value = item.MaBin;
                            if (cmdUpdate.ExecuteNonQuery() != 1)
                                throw new InvalidOperationException("Không thể cập nhật Bin: " + item.MaBin);

                            cmdApprove.Parameters["@id"].Value = item.Id;
                            if (cmdApprove.ExecuteNonQuery() != 1)
                                throw new InvalidOperationException("Phiếu kiểm kê đã được xử lý: " + item.Id);
                        }
                    }

                    tx.Commit();
                    return rows.Count;
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }
            }
        }

        private static List<SelectedKiemKeRow> ReadAndValidateSelected(
            SQLiteConnection conn, SQLiteTransaction tx, List<long> selectedIds)
        {
            if (selectedIds == null || selectedIds.Count == 0)
                throw new InvalidOperationException("Vui lòng chọn ít nhất một phiếu kiểm kê.");

            var ids = new HashSet<long>(selectedIds);
            if (ids.Count != selectedIds.Count || ids.Contains(0) || ids.Any(id => id < 0))
                throw new InvalidOperationException("Danh sách ID phiếu kiểm kê được chọn không hợp lệ.");

            bool hasSanPhamId = false;
            using (var schema = new SQLiteCommand("PRAGMA table_info(TTKiemKeThang);", conn, tx))
            using (var reader = schema.ExecuteReader())
            {
                while (reader.Read())
                {
                    if (string.Equals(reader["name"].ToString(), "DanhSachSP_ID", StringComparison.OrdinalIgnoreCase))
                    {
                        hasSanPhamId = true;
                        break;
                    }
                }
            }
            if (!hasSanPhamId)
                throw new InvalidOperationException(
                    "TTKiemKeThang chưa có cột DanhSachSP_ID. Hãy cập nhật cấu trúc database trước khi sử dụng.");

            var rows = new List<SelectedKiemKeRow>();
            var idsList = new List<long>(ids);
            const int batchSize = 400; // Tránh vượt giới hạn biến SQL của SQLite.
            for (int offset = 0; offset < idsList.Count; offset += batchSize)
            {
                int count = Math.Min(batchSize, idsList.Count - offset);
                var placeholders = new List<string>();
                using (var cmd = new SQLiteCommand(string.Empty, conn, tx))
                {
                    cmd.Transaction = tx;
                    for (int j = 0; j < count; j++)
                    {
                        string param = "@p" + j;
                        placeholders.Add(param);
                        cmd.Parameters.AddWithValue(param, idsList[offset + j]);
                    }
                    cmd.CommandText = @"
                        SELECT kk.id, kk.TTThanhPham_ID, kk.MaBin,
                               kk.DanhSachSP_ID, kk.KhoiLuong, kk.ChieuDai,
                               kk.ApprovedDate, tp.id AS ActualId,
                               tp.MaBin AS ActualMaBin, tp.Temp AS ActualTemp,
                               sp.id AS FoundSpId
                        FROM TTKiemKeThang kk
                        LEFT JOIN TTThanhPham tp ON tp.id = kk.TTThanhPham_ID
                        LEFT JOIN DanhSachMaSP sp ON sp.id = kk.DanhSachSP_ID
                        WHERE kk.id IN (" + string.Join(",", placeholders) + ");";

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            long id = Convert.ToInt64(reader["id"]);
                            string bin = reader["MaBin"] == DBNull.Value ? "" : reader["MaBin"].ToString().Trim();
                            if (string.IsNullOrWhiteSpace(bin))
                                throw new InvalidOperationException("Phiếu " + id + " không có mã Bin.");
                            if (reader["ApprovedDate"] != DBNull.Value &&
                                !string.IsNullOrWhiteSpace(reader["ApprovedDate"].ToString()))
                                throw new InvalidOperationException("Phiếu " + id + " đã có ApprovedDate, không được cập nhật lại.");
                            if (reader["TTThanhPham_ID"] == DBNull.Value || reader["ActualId"] == DBNull.Value)
                                throw new InvalidOperationException("Phiếu " + id + " tham chiếu Bin không tồn tại.");
                            if (reader["ActualTemp"] == DBNull.Value || Convert.ToInt32(reader["ActualTemp"]) != 0)
                                throw new InvalidOperationException("Bin " + bin + " không phải bản ghi sản xuất chính thức.");
                            if (!string.Equals(bin, reader["ActualMaBin"]?.ToString()?.Trim(), StringComparison.Ordinal))
                                throw new InvalidOperationException("Phiếu " + id + " có MaBin không khớp với TTThanhPham_ID.");

                            long? productId = reader["DanhSachSP_ID"] == DBNull.Value
                                ? (long?)null : Convert.ToInt64(reader["DanhSachSP_ID"]);
                            if (productId.HasValue && (productId.Value <= 0 || reader["FoundSpId"] == DBNull.Value))
                                throw new InvalidOperationException("Phiếu " + id + " có ID sản phẩm không tồn tại.");

                            decimal kl = reader["KhoiLuong"] == DBNull.Value ? 0m : Convert.ToDecimal(reader["KhoiLuong"]);
                            decimal cd = reader["ChieuDai"] == DBNull.Value ? 0m : Convert.ToDecimal(reader["ChieuDai"]);
                            if (kl < 0 || cd < 0)
                                throw new InvalidOperationException("Phiếu " + id + " có số lượng âm.");

                            rows.Add(new SelectedKiemKeRow
                            {
                                Id = id,
                                TTThanhPhamId = Convert.ToInt64(reader["TTThanhPham_ID"]),
                                MaBin = bin,
                                KhoiLuong = kl,
                                ChieuDai = cd,
                                DanhSachSpId = productId
                            });
                        }
                    }
                }
            }

            if (rows.Count != ids.Count)
                throw new InvalidOperationException("Có phiếu kiểm kê đã bị xóa hoặc không còn tồn tại.");

            var repeated = rows.GroupBy(r => r.MaBin, StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Count() >= 2)
                .Select(group => group.Key + " (phiếu: " + string.Join(", ", group.Select(r => r.Id)) + ")")
                .ToList();
            if (repeated.Count > 0)
                throw new InvalidOperationException(
                    "Phát hiện MaBin trùng trong các phiếu đã chọn. Dừng toàn bộ cập nhật:\n" +
                    string.Join("\n", repeated));

            return rows;
        }

    }
}
