using System;
using System.Data;
using System.Data.SQLite;
using System.Threading;
using System.Threading.Tasks;

namespace DG_TonKhoBTP_v02.Database.Kho
{
    public static class XuatKho_DB
    {
        // ════════════════════════════════════════════════════════════════════════
        // TÌM KIẾM LOT / BIN ĐÃ NHẬP KHO
        // ════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Tìm các bin khớp với keyword.
        /// Trả về: MaBin, Ten, TTThanhPham_ID, DanhSachMaSP_ID, Loai (của TTNhapKho).
        /// Chỉ lấy những bin đã nhập kho: TTThanhPham.NhapKho = 1.
        /// </summary>
        public static Task<DataTable> TimKiemLotAsync(string keyword, CancellationToken ct)
        {
            keyword = keyword?.Trim() ?? string.Empty;

            return Task.Run(() =>
            {
                ct.ThrowIfCancellationRequested();

                const string sql = @"
                SELECT DISTINCT
                        nk.TTThanhPham_ID AS TTThanhPham_ID,
                        tp.MaBin,
                        sp.Ten,
                        sp.id             AS DanhSachMaSP_ID,
                        nk.Loai
                FROM    TTNhapKho      nk
                JOIN    TTThanhPham    tp ON tp.id = nk.TTThanhPham_ID
                JOIN    DanhSachMaSP   sp ON sp.id = tp.DanhSachSP_ID
                WHERE   nk.Kieu = 1
                  AND  (tp.MaBin          LIKE @kw
                        OR sp.Ten_KhongDau LIKE @kw
                        OR sp.Ten          LIKE @kw)
                  AND EXISTS (
                        SELECT 1
                        FROM   TTCuonDay cd
                        WHERE  cd.ThongTinNhapKho_ID = nk.id
                          AND (
                                (nk.Loai = 'Lô'    AND cd.SoCuoi > cd.SoDau)
                             OR (nk.Loai = 'Cuộn'  AND cd.SoCuon <> 0)
                          )
                  )
                ORDER BY tp.MaBin
                LIMIT 50";

                DataTable dt = new DataTable();

                using var conn = DB_Base.OpenConnection();
                using var cmd = new SQLiteCommand(sql, conn);

                cmd.Parameters.AddWithValue("@kw", "%" + keyword + "%");

                ct.ThrowIfCancellationRequested();

                using var adapter = new SQLiteDataAdapter(cmd);
                adapter.Fill(dt);

                return dt;

            }, ct);
        }

        // ════════════════════════════════════════════════════════════════════════
        // LẤY DỮ LIỆU CUỘN / DÂY THEO THÀNH PHẨM
        // ════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Query JOIN TTNhapKho ↔ TTCuonDay theo TTThanhPham_ID.
        /// Trả về thêm TTCuonDay.id để dùng làm khoá khi lưu TTXuatKho.
        /// </summary>
        public static DataTable LayDuLieuCuonDay(long ttThanhPhamId)
        {
            DataTable dt = new DataTable();

            if (ttThanhPhamId <= 0)
                return dt;

            const string sql = @"
                SELECT  nk.id           AS TTNhapKho_ID,
                        cd.id           AS TTCuonDay_ID,
                        cd.TongChieuDai,
                        cd.SoCuon,
                        cd.SoDau,
                        cd.SoCuoi,
                        cd.GhiChu,
                        nk.Loai
                FROM    TTNhapKho   nk
                JOIN    TTCuonDay   cd ON cd.ThongTinNhapKho_ID = nk.id
                WHERE   nk.TTThanhPham_ID = @id
                ORDER BY nk.id, cd.id";

            using var conn = DB_Base.OpenConnection();
            using var cmd = new SQLiteCommand(sql, conn);

            cmd.Parameters.AddWithValue("@id", ttThanhPhamId);

            using var adapter = new SQLiteDataAdapter(cmd);
            adapter.Fill(dt);

            return dt;
        }

        // ════════════════════════════════════════════════════════════════════════
        // THÊM MỚI BẢN GHI XUẤT KHO
        // ════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Chèn một bản ghi vào TTXuatKho.
        /// Trả về id mới được chèn (last insert rowid).
        /// TongChieuDai = SoCuon * (SoCuoi - SoDau) — tính ở tầng DB để nhất quán.
        /// </summary>
        public static long ThemXuatKho(
            long ttCuonDayId,
            int? soCuon,
            int? soDau,
            int? soCuoi,
            string ghiChu,
            string ngayXuat,
            string nguoiLam)
        {
            int tongChieuDai = 0;
            if (soCuon.HasValue && soDau.HasValue && soCuoi.HasValue)
                tongChieuDai = soCuon.Value * (soCuoi.Value - soDau.Value);

            const string sql = @"
                INSERT INTO TTXuatKho
                    (TTCuonDay_ID, SoCuon, TongChieuDai, SoDau, SoCuoi, GhiChu, NgayXuat, NguoiLam)
                VALUES
                    (@ttCuonDayId, @soCuon, @tongChieuDai, @soDau, @soCuoi, @ghiChu, @ngayXuat, @nguoiLam);
                SELECT last_insert_rowid();";

            using var conn = DB_Base.OpenConnection();
            using var cmd = new SQLiteCommand(sql, conn);

            cmd.Parameters.AddWithValue("@ttCuonDayId", ttCuonDayId);
            cmd.Parameters.AddWithValue("@soCuon", (object?)soCuon ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@tongChieuDai", tongChieuDai);
            cmd.Parameters.AddWithValue("@soDau", (object?)soDau ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@soCuoi", (object?)soCuoi ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ghiChu", (object?)ghiChu ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ngayXuat", ngayXuat);
            cmd.Parameters.AddWithValue("@nguoiLam", nguoiLam);

            return Convert.ToInt64(cmd.ExecuteScalar());
        }

        // ════════════════════════════════════════════════════════════════════════
        // CẬP NHẬT BẢN GHI XUẤT KHO
        // ════════════════════════════════════════════════════════════════════════

        public static void SuaXuatKho(
            long id,
            int? soCuon,
            int? soDau,
            int? soCuoi,
            string ghiChu,
            string ngayXuat,
            string nguoiLam)
        {
            int tongChieuDai = 0;
            if (soCuon.HasValue && soDau.HasValue && soCuoi.HasValue)
                tongChieuDai = soCuon.Value * (soCuoi.Value - soDau.Value);

            const string sql = @"
                UPDATE TTXuatKho
                SET    SoCuon       = @soCuon,
                       TongChieuDai = @tongChieuDai,
                       SoDau        = @soDau,
                       SoCuoi       = @soCuoi,
                       GhiChu       = @ghiChu,
                       NgayXuat     = @ngayXuat,
                       NguoiLam     = @nguoiLam
                WHERE  id = @id";

            using var conn = DB_Base.OpenConnection();
            using var cmd = new SQLiteCommand(sql, conn);

            cmd.Parameters.AddWithValue("@id", id);
            cmd.Parameters.AddWithValue("@soCuon", (object?)soCuon ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@tongChieuDai", tongChieuDai);
            cmd.Parameters.AddWithValue("@soDau", (object?)soDau ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@soCuoi", (object?)soCuoi ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ghiChu", (object?)ghiChu ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ngayXuat", ngayXuat);
            cmd.Parameters.AddWithValue("@nguoiLam", nguoiLam);

            cmd.ExecuteNonQuery();
        }

        // ════════════════════════════════════════════════════════════════════════
        // XOÁ BẢN GHI XUẤT KHO
        // ════════════════════════════════════════════════════════════════════════

        public static void XoaXuatKho(long id)
        {
            const string sql = "DELETE FROM TTXuatKho WHERE id = @id";

            using var conn = DB_Base.OpenConnection();
            using var cmd = new SQLiteCommand(sql, conn);

            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }

        public static DataTable LayChiTietXuatKho(long xuatKhoId)
        {
            const string sql = @"
            SELECT  cd.TongChieuDai  AS TongChieuDai_NK,
                    cd.SoCuon        AS SoCuon_CD,
                    cd.SoDau         AS SoDau_CD,
                    cd.SoCuoi        AS SoCuoi_CD,
                    cd.GhiChu        AS GhiChu_CD,
                    xk.SoCuon        AS SoCuon_XK,
                    xk.SoDau         AS SoDau_XK,
                    xk.SoCuoi        AS SoCuoi_XK,
                    xk.GhiChu        AS GhiChu_XK,
                    xk.NgayXuat,
                    xk.NguoiLam,
                    nk.Loai
            FROM    TTXuatKho    xk
            JOIN    TTCuonDay    cd ON cd.id  = xk.TTCuonDay_ID
            JOIN    TTNhapKho    nk ON nk.id  = cd.ThongTinNhapKho_ID
            WHERE   xk.id = @id
            LIMIT 1";

            DataTable dt = new DataTable();

            using var conn = DB_Base.OpenConnection();
            using var cmd = new SQLiteCommand(sql, conn);

            cmd.Parameters.AddWithValue("@id", xuatKhoId);

            using var adapter = new SQLiteDataAdapter(cmd);
            adapter.Fill(dt);

            return dt;
        }


        // ════════════════════════════════════════════════════════════════════════
        // TÌM KIẾM LỊCH SỬ XUẤT KHO (cho dataGridView1 / preview)
        // ════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Tìm kiếm theo: Tên sản phẩm, NgayXuat, MaBin.
        /// Trả về đủ thông tin để hiển thị lên dataGridView1.
        /// </summary>
        public static DataTable TimKiemXuatKho(string keyword)
        {
            keyword = keyword?.Trim() ?? string.Empty;

            const string sql = @"
                SELECT  xk.id               AS id_preview,
                        xk.TongChieuDai     AS tongCD_preview,
                        sp.Ten              AS ten_preview,
                        tp.MaBin            AS lot_preview,
                        cd.SoCuon           AS soCuon_preview,
                        cd.SoDau            AS soDau_preview,
                        cd.SoCuoi           AS soCuoi_preview,
                        cd.GhiChu           AS ghiChu_preview,
                        xk.SoCuon           AS SoCuon_user_preview,
                        xk.SoDau            AS soDau_user_preview,
                        xk.SoCuoi           AS soCuoi_user_preview,
                        xk.GhiChu           AS ghiChu_user_preview,
                        xk.NgayXuat,
                        xk.NguoiLam,
                        xk.TTCuonDay_ID
                FROM    TTXuatKho       xk
                JOIN    TTCuonDay       cd ON cd.id           = xk.TTCuonDay_ID
                JOIN    TTNhapKho       nk ON nk.id           = cd.ThongTinNhapKho_ID
                JOIN    TTThanhPham     tp ON tp.id           = nk.TTThanhPham_ID
                JOIN    DanhSachMaSP    sp ON sp.id           = tp.DanhSachSP_ID
                WHERE  (sp.Ten          LIKE @kw
                        OR sp.Ten_KhongDau LIKE @kw
                        OR xk.NgayXuat  LIKE @kw
                        OR tp.MaBin     LIKE @kw)
                ORDER BY xk.id DESC
                LIMIT 200";

            DataTable dt = new DataTable();

            using var conn = DB_Base.OpenConnection();
            using var cmd = new SQLiteCommand(sql, conn);

            cmd.Parameters.AddWithValue("@kw", "%" + keyword + "%");

            using var adapter = new SQLiteDataAdapter(cmd);
            adapter.Fill(dt);

            return dt;
        }
    }
}