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
        /// Trả về: MaBin, Ten, TTThanhPham_ID, DanhSachMaSP_ID.
        /// Chỉ lấy những bin đã nhập kho: TTThanhPham.NhapKho = 1.
        /// </summary>
        public static Task<DataTable> TimKiemLotAsync(string keyword, CancellationToken ct)
        {
            keyword = keyword?.Trim() ?? string.Empty;

            return Task.Run(() =>
            {
                ct.ThrowIfCancellationRequested();

                const string sql = @"
                    SELECT  tp.id          AS TTThanhPham_ID,
                            tp.MaBin,
                            sp.Ten,
                            sp.id          AS DanhSachMaSP_ID
                    FROM    TTThanhPham    tp
                    JOIN    DanhSachMaSP   sp ON sp.id = tp.DanhSachSP_ID
                    WHERE   tp.NhapKho = 1
                      AND  (tp.MaBin        LIKE @kw
                            OR sp.Ten_KhongDau LIKE @kw
                            OR sp.Ten          LIKE @kw)
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
        /// Dùng để nạp dữ liệu vào grid xuất kho.
        /// </summary>
        public static DataTable LayDuLieuCuonDay(long ttThanhPhamId)
        {
            DataTable dt = new DataTable();

            if (ttThanhPhamId <= 0)
                return dt;

            const string sql = @"
                SELECT  nk.id           AS TTNhapKho_ID,
                        cd.TongChieuDai,
                        cd.SoCuon,
                        cd.SoDau,
                        cd.SoCuoi,
                        cd.GhiChu
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
    }
}