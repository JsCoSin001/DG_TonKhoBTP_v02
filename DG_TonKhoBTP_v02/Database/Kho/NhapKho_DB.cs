using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DG_TonKhoBTP_v02.Database.ChatLuong
{
    public static class NhapKho_DB
    {
        public static Task<DataTable> TimKiemMaBinAsync(string keyword, CancellationToken ct)
        {
            return Task.Run(() =>
            {
                ct.ThrowIfCancellationRequested();

                const string sql = @"
                    SELECT  tp.id          AS TTThanhPham_ID,
                            tp.MaBin,
                            sp.Ten,
                            tp.ChieuDaiSau,
                            tp.GhiChu
                    FROM    TTThanhPham   tp
                    JOIN    DanhSachMaSP  sp ON sp.id = tp.DanhSachSP_ID
                    WHERE   tp.MaBin LIKE @keyword AND tp.CongDoan = 5 AND tp.ChieuDaiSau > 0 
                    ORDER BY tp.MaBin
                    LIMIT   50";

                var dt = new DataTable();

                using var conn = DB_Base.OpenConnection();
                using var cmd = new SQLiteCommand(sql, conn);
                cmd.Parameters.AddWithValue("@keyword", "%" + keyword + "%");

                ct.ThrowIfCancellationRequested();

                using var adapter = new SQLiteDataAdapter(cmd);
                adapter.Fill(dt);

                return dt;

            }, ct);
        }


        /// <summary>
        /// Tìm các TTCuonDay_CD còn lại theo Ngày/Ca để nhập kho hàng loạt.
        /// Chỉ trả MaBin có CongDoan=5, ChieuDaiSau>0 và tổng mét chi tiết còn lại
        /// khớp ChieuDaiSau. MaBin lệch dữ liệu được đưa vào MaBinBatThuong và không trả item.
        /// </summary>
        public static DG_TonKhoBTP_v02.Models.NhapKhoTheoNgaySearchResult TimKiemNhapKhoTheoNgay(
            DateTime ngayBatDau,
            DateTime ngayKetThuc,
            string ca)
        {
            if (ngayBatDau.Date > ngayKetThuc.Date)
                throw new ArgumentException("Ngày bắt đầu không được lớn hơn ngày kết thúc.");

            string caFilter = (ca ?? string.Empty).Trim();
            bool toanBoCa = string.IsNullOrWhiteSpace(caFilter)
                || string.Equals(caFilter, "Toàn bộ", StringComparison.OrdinalIgnoreCase);

            var result = new DG_TonKhoBTP_v02.Models.NhapKhoTheoNgaySearchResult();

            // Hỗ trợ Ngay dạng yyyy-MM-dd (chuẩn hiện tại) và dd/MM/yyyy.
            const string normalizedNgay = @"CASE
                WHEN instr(clv.Ngay, '/') > 0 AND length(clv.Ngay) >= 10
                    THEN substr(clv.Ngay, 7, 4) || '-' || substr(clv.Ngay, 4, 2) || '-' || substr(clv.Ngay, 1, 2)
                ELSE substr(clv.Ngay, 1, 10)
            END";

            string sourceCte = @"
                WITH source_remaining AS (
                    SELECT
                        tcd.id AS TTCuonDay_CD_ID,
                        cdb.TTThanhPham_ID,
                        tcd.TTLo_ID,
                        lo.KichThuoc AS KichThuocLo,
                        CASE
                            WHEN tcd.TTLo_ID IS NULL THEN 1
                            WHEN lo.id IS NOT NULL THEN 1
                            ELSE 0
                        END AS TTLoHopLe,
                        MAX(0, IFNULL(tcd.SoCuon,0) - COALESCE(SUM(IFNULL(td.SoCuon,0)),0)) AS SoLuongCon,
                        tcd.TongChieuDai AS ChieuDai1Cuon,
                        tcd.SoDau,
                        tcd.SoCuoi,
                        IFNULL(tcd.GhiChu,'') AS GhiChu
                    FROM TTCuonDay_CD tcd
                    INNER JOIN CD_BocVo cbv ON cbv.id=tcd.CongDoan_ID
                    INNER JOIN CaiDatCDBoc cdb ON cdb.id=cbv.CaiDatCDBoc_ID
                    LEFT JOIN TTLo lo ON lo.id=tcd.TTLo_ID
                    LEFT JOIN TTCuonDay td ON td.TTCuonDay_CD_ID=tcd.id
                    GROUP BY
                        tcd.id, cdb.TTThanhPham_ID, tcd.TTLo_ID, lo.KichThuoc, lo.id,
                        tcd.SoCuon, tcd.TongChieuDai, tcd.SoDau, tcd.SoCuoi, tcd.GhiChu
                )";

            string summarySql = sourceCte + @"
                SELECT
                    tp.id AS TTThanhPham_ID,
                    tp.MaBin,
                    sp.Ma AS MaSP,
                    sp.Ten AS TenSP,
                    tp.ChieuDaiSau,
                    clv.Ngay,
                    clv.Ca,
                    COALESCE(SUM(CASE WHEN sr.SoLuongCon>0 THEN sr.SoLuongCon * sr.ChieuDai1Cuon ELSE 0 END),0) AS TongMetChiTietCon,
                    COALESCE(SUM(CASE WHEN sr.SoLuongCon>0 THEN 1 ELSE 0 END),0) AS SoDongCon,
                    COALESCE(SUM(CASE WHEN sr.SoLuongCon>0 AND sr.TTLoHopLe=0 THEN 1 ELSE 0 END),0) AS SoDongLoKhongHopLe
                FROM TTThanhPham tp
                INNER JOIN DanhSachMaSP sp ON sp.id=tp.DanhSachSP_ID
                INNER JOIN ThongTinCaLamViec clv ON clv.TTThanhPham_id=tp.id
                LEFT JOIN source_remaining sr ON sr.TTThanhPham_ID=tp.id
                WHERE tp.CongDoan=5
                  AND tp.ChieuDaiSau>0
                  AND date(" + normalizedNgay + @") BETWEEN date(@NgayBD) AND date(@NgayKT)
                  AND (@ToanBoCa=1 OR clv.Ca=@Ca)
                GROUP BY tp.id,tp.MaBin,sp.Ma,sp.Ten,tp.ChieuDaiSau,clv.Ngay,clv.Ca
                ORDER BY clv.Ngay,tp.MaBin;";

            var validIds = new HashSet<long>();
            using (var conn = DB_Base.OpenConnection())
            using (var cmd = new SQLiteCommand(summarySql, conn))
            {
                cmd.Parameters.AddWithValue("@NgayBD", ngayBatDau.Date.ToString("yyyy-MM-dd"));
                cmd.Parameters.AddWithValue("@NgayKT", ngayKetThuc.Date.ToString("yyyy-MM-dd"));
                cmd.Parameters.AddWithValue("@ToanBoCa", toanBoCa ? 1 : 0);
                cmd.Parameters.AddWithValue("@Ca", caFilter);

                using SQLiteDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    long id = Convert.ToInt64(reader["TTThanhPham_ID"]);
                    string maBin = Convert.ToString(reader["MaBin"]) ?? string.Empty;
                    double chieuDaiSau = Convert.ToDouble(reader["ChieuDaiSau"]);
                    double tongChiTietCon = Convert.ToDouble(reader["TongMetChiTietCon"]);
                    int soDongCon = Convert.ToInt32(reader["SoDongCon"]);
                    int soDongLoKhongHopLe = Convert.ToInt32(reader["SoDongLoKhongHopLe"]);

                    bool hopLe = soDongCon > 0
                        && soDongLoKhongHopLe == 0
                        && Math.Abs(chieuDaiSau - tongChiTietCon) <= 0.000001d;

                    if (hopLe)
                        validIds.Add(id);
                    else if (!result.MaBinBatThuong.Contains(maBin))
                        result.MaBinBatThuong.Add(maBin);
                }
            }

            if (validIds.Count == 0)
                return result;

            string[] paramNames = validIds.Select((_, i) => "@id" + i).ToArray();
            string detailSql = sourceCte + @"
                SELECT
                    tp.id AS TTThanhPham_ID,
                    clv.Ngay,
                    clv.Ca,
                    tp.MaBin,
                    sp.Ma AS MaSP,
                    sp.Ten AS TenSP,
                    tp.ChieuDaiSau,
                    sr.TTCuonDay_CD_ID,
                    sr.TTLo_ID,
                    sr.KichThuocLo,
                    sr.TTLoHopLe,
                    sr.SoLuongCon,
                    sr.ChieuDai1Cuon,
                    sr.SoDau,
                    sr.SoCuoi,
                    sr.GhiChu
                FROM TTThanhPham tp
                INNER JOIN DanhSachMaSP sp ON sp.id=tp.DanhSachSP_ID
                INNER JOIN ThongTinCaLamViec clv ON clv.TTThanhPham_id=tp.id
                INNER JOIN source_remaining sr ON sr.TTThanhPham_ID=tp.id AND sr.SoLuongCon>0
                WHERE tp.id IN (" + string.Join(",", paramNames) + @")
                  AND date(" + normalizedNgay + @") BETWEEN date(@NgayBD) AND date(@NgayKT)
                  AND (@ToanBoCa=1 OR clv.Ca=@Ca)
                ORDER BY clv.Ngay,tp.MaBin,sr.TTCuonDay_CD_ID;";

            using (var conn = DB_Base.OpenConnection())
            using (var cmd = new SQLiteCommand(detailSql, conn))
            {
                cmd.Parameters.AddWithValue("@NgayBD", ngayBatDau.Date.ToString("yyyy-MM-dd"));
                cmd.Parameters.AddWithValue("@NgayKT", ngayKetThuc.Date.ToString("yyyy-MM-dd"));
                cmd.Parameters.AddWithValue("@ToanBoCa", toanBoCa ? 1 : 0);
                cmd.Parameters.AddWithValue("@Ca", caFilter);
                int i = 0;
                foreach (long id in validIds)
                    cmd.Parameters.AddWithValue("@id" + i++, id);

                using SQLiteDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    result.Items.Add(new DG_TonKhoBTP_v02.Models.NhapKhoTheoNgayDong
                    {
                        TTThanhPham_ID = Convert.ToInt64(reader["TTThanhPham_ID"]),
                        Ngay = Convert.ToString(reader["Ngay"]) ?? string.Empty,
                        Ca = Convert.ToString(reader["Ca"]) ?? string.Empty,
                        MaBin = Convert.ToString(reader["MaBin"]) ?? string.Empty,
                        MaSP = Convert.ToString(reader["MaSP"]) ?? string.Empty,
                        TenSP = Convert.ToString(reader["TenSP"]) ?? string.Empty,
                        ChieuDaiSauSnapshot = Convert.ToDouble(reader["ChieuDaiSau"]),
                        TTCuonDay_CD_ID = Convert.ToInt64(reader["TTCuonDay_CD_ID"]),
                        TTLo_ID = reader["TTLo_ID"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["TTLo_ID"]),
                        KichThuocLo = reader["KichThuocLo"] == DBNull.Value ? string.Empty : (Convert.ToString(reader["KichThuocLo"]) ?? string.Empty).Trim(),
                        TTLoHopLe = Convert.ToInt32(reader["TTLoHopLe"]) == 1,
                        SoLuongCon = Convert.ToInt32(reader["SoLuongCon"]),
                        ChieuDai1Cuon = Convert.ToInt32(reader["ChieuDai1Cuon"]),
                        SoDau = reader["SoDau"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["SoDau"]),
                        SoCuoi = reader["SoCuoi"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["SoCuoi"]),
                        GhiChu = Convert.ToString(reader["GhiChu"]) ?? string.Empty
                    });
                }
            }

            return result;
        }

        /// <summary>
        /// Lấy danh sách kích thước lô từ TTLo để đổ vào ComboBox nrChieuCaoLo.
        /// </summary>
        public static DataTable LayDanhSachKichThuocLo()
        {
            const string sql = @"
                SELECT KichThuoc
                FROM TTLo
                WHERE TRIM(IFNULL(KichThuoc, '')) <> ''
                ORDER BY CAST(KichThuoc AS REAL), KichThuoc;";

            DataTable dt = new DataTable();

            using var conn = DB_Base.OpenConnection();
            using var cmd = new SQLiteCommand(sql, conn);
            using var adapter = new SQLiteDataAdapter(cmd);
            adapter.Fill(dt);

            return dt;
        }

        /// <summary>
        /// Lấy phần TTCuonDay_CD còn chưa nhập kho của một TTThanhPham.
        /// Số cuộn trả về = SoCuon nguồn - SUM(SoCuon đã lưu trong TTCuonDay theo TTCuonDay_CD_ID).
        /// Chỉ trả các dòng còn SoCuon > 0.
        /// </summary>
        public static List<DG_TonKhoBTP_v02.Models.ThongTinCuonDay> LayTTCuonDayConLaiTheoTTThanhPhamId(long ttThanhPhamId)
        {
            var result = new List<DG_TonKhoBTP_v02.Models.ThongTinCuonDay>();
            if (ttThanhPhamId <= 0) return result;

            const string sql = @"
                SELECT
                    tcd.id AS TTCuonDay_CD_ID,
                    tcd.TTLo_ID,
                    lo.KichThuoc AS KichThuocLo,
                    CASE
                        WHEN tcd.TTLo_ID IS NULL THEN 1
                        WHEN lo.id IS NOT NULL THEN 1
                        ELSE 0
                    END AS TTLoHopLe,
                    IFNULL(tcd.SoCuon, 0) AS SoCuonNguon,
                    COALESCE(SUM(IFNULL(td.SoCuon, 0)), 0) AS SoCuonDaNhap,
                    tcd.TongChieuDai,
                    tcd.SoDau,
                    tcd.SoCuoi,
                    IFNULL(tcd.GhiChu, '') AS GhiChu
                FROM TTCuonDay_CD tcd
                INNER JOIN CD_BocVo cbv ON cbv.id = tcd.CongDoan_ID
                INNER JOIN CaiDatCDBoc cdb ON cdb.id = cbv.CaiDatCDBoc_ID
                LEFT JOIN TTLo lo ON lo.id = tcd.TTLo_ID
                LEFT JOIN TTCuonDay td ON td.TTCuonDay_CD_ID = tcd.id
                WHERE cdb.TTThanhPham_ID = @TTThanhPham_ID
                GROUP BY
                    tcd.id, tcd.TTLo_ID, lo.KichThuoc, lo.id,
                    tcd.SoCuon, tcd.TongChieuDai, tcd.SoDau, tcd.SoCuoi, tcd.GhiChu
                HAVING IFNULL(tcd.SoCuon, 0) - COALESCE(SUM(IFNULL(td.SoCuon, 0)), 0) > 0
                ORDER BY tcd.id;";

            using var conn = DB_Base.OpenConnection();
            using var cmd = new SQLiteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@TTThanhPham_ID", ttThanhPhamId);
            using SQLiteDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                int soCuonNguon = Convert.ToInt32(reader["SoCuonNguon"]);
                int soCuonDaNhap = Convert.ToInt32(reader["SoCuonDaNhap"]);
                int soCuonConLai = soCuonNguon - soCuonDaNhap;
                if (soCuonConLai <= 0) continue;

                result.Add(new DG_TonKhoBTP_v02.Models.ThongTinCuonDay
                {
                    TTCuonDay_CD_ID = Convert.ToInt64(reader["TTCuonDay_CD_ID"]),
                    TTLo_ID = reader["TTLo_ID"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["TTLo_ID"]),
                    KichThuocLo = reader["KichThuocLo"] == DBNull.Value
                        ? string.Empty
                        : (Convert.ToString(reader["KichThuocLo"]) ?? string.Empty).Trim(),
                    TTLoHopLe = Convert.ToInt32(reader["TTLoHopLe"]) == 1,
                    SoCuon = soCuonConLai,
                    // TongChieuDai của TTCuonDay_CD là chiều dài của 1 cuộn/lô.
                    TongChieuDai = Convert.ToInt32(reader["TongChieuDai"]),
                    SoDau = reader["SoDau"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["SoDau"]),
                    soCuoi = reader["SoCuoi"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["SoCuoi"]),
                    Ghichu = Convert.ToString(reader["GhiChu"]) ?? string.Empty
                });
            }

            return result;
        }

        /// <summary>
        /// Cập nhật nguồn TTCuonDay_CD từ Frm_DLCuon.
        /// Dòng đã từng nhập kho:
        /// - không được đổi TongChieuDai / TTLo_ID / SoDau / SoCuoi;
        /// - SoCuon mới không được nhỏ hơn tổng SoCuon đã nhập;
        /// - không được xoá.
        /// Dòng chưa từng nhập được phép sửa/xoá; dòng mới sẽ được thêm vào công đoạn bọc vỏ gần nhất.
        /// </summary>
        public static void CapNhatTTCuonDayCDTheoTTThanhPhamId(
            long ttThanhPhamId,
            List<DG_TonKhoBTP_v02.Models.ThongTinCuonDay> items)
        {
            if (ttThanhPhamId <= 0)
                throw new ArgumentException("TTThanhPham_ID không hợp lệ.", nameof(ttThanhPhamId));

            items = items ?? new List<DG_TonKhoBTP_v02.Models.ThongTinCuonDay>();

            const string sqlCurrent = @"
                SELECT
                    tcd.id,
                    tcd.CongDoan_ID,
                    IFNULL(tcd.SoCuon, 0) AS SoCuon,
                    tcd.TongChieuDai,
                    tcd.SoDau,
                    tcd.SoCuoi,
                    tcd.GhiChu,
                    tcd.TTLo_ID,
                    COALESCE((
                        SELECT SUM(IFNULL(td.SoCuon, 0))
                        FROM TTCuonDay td
                        WHERE td.TTCuonDay_CD_ID = tcd.id
                    ), 0) AS SoCuonDaNhap
                FROM TTCuonDay_CD tcd
                INNER JOIN CD_BocVo cbv ON cbv.id = tcd.CongDoan_ID
                INNER JOIN CaiDatCDBoc cdb ON cdb.id = cbv.CaiDatCDBoc_ID
                WHERE cdb.TTThanhPham_ID = @TTThanhPham_ID
                ORDER BY tcd.id;";

            const string sqlLatestCongDoan = @"
                SELECT cbv.id
                FROM CD_BocVo cbv
                INNER JOIN CaiDatCDBoc cdb ON cdb.id = cbv.CaiDatCDBoc_ID
                WHERE cdb.TTThanhPham_ID = @TTThanhPham_ID
                ORDER BY cbv.id DESC
                LIMIT 1;";

            const string sqlUpdateFull = @"
                UPDATE TTCuonDay_CD
                SET SoCuon = @SoCuon,
                    TongChieuDai = @TongChieuDai,
                    SoDau = @SoDau,
                    SoCuoi = @SoCuoi,
                    GhiChu = @GhiChu,
                    TTLo_ID = @TTLo_ID
                WHERE id = @id;";

            const string sqlUpdateAfterImported = @"
                UPDATE TTCuonDay_CD
                SET SoCuon = @SoCuon,
                    GhiChu = @GhiChu
                WHERE id = @id;";

            const string sqlInsert = @"
                INSERT INTO TTCuonDay_CD
                    (SoCuon, TongChieuDai, SoDau, SoCuoi, GhiChu, CongDoan_ID, TTLo_ID)
                VALUES
                    (@SoCuon, @TongChieuDai, @SoDau, @SoCuoi, @GhiChu, @CongDoan_ID, @TTLo_ID);";

            const string sqlDelete = @"DELETE FROM TTCuonDay_CD WHERE id = @id;";

            using var conn = DB_Base.OpenConnection();
            using var tran = conn.BeginTransaction();

            try
            {
                var current = new Dictionary<long, SourceEditState>();
                using (var cmd = new SQLiteCommand(sqlCurrent, conn, tran))
                {
                    cmd.Parameters.AddWithValue("@TTThanhPham_ID", ttThanhPhamId);
                    using SQLiteDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        long id = Convert.ToInt64(reader["id"]);
                        current[id] = new SourceEditState
                        {
                            Id = id,
                            CongDoanId = Convert.ToInt64(reader["CongDoan_ID"]),
                            SoCuon = Convert.ToInt32(reader["SoCuon"]),
                            TongChieuDai = Convert.ToInt32(reader["TongChieuDai"]),
                            SoDau = reader["SoDau"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["SoDau"]),
                            SoCuoi = reader["SoCuoi"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["SoCuoi"]),
                            TTLoId = reader["TTLo_ID"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["TTLo_ID"]),
                            SoCuonDaNhap = Convert.ToInt32(reader["SoCuonDaNhap"])
                        };
                    }
                }

                long latestCongDoanId = 0;
                using (var cmd = new SQLiteCommand(sqlLatestCongDoan, conn, tran))
                {
                    cmd.Parameters.AddWithValue("@TTThanhPham_ID", ttThanhPhamId);
                    object value = cmd.ExecuteScalar();
                    if (value != null && value != DBNull.Value)
                        latestCongDoanId = Convert.ToInt64(value);
                }

                if (latestCongDoanId <= 0 && items.Any(x => x != null && !x.TTCuonDay_CD_ID.HasValue))
                    throw new InvalidOperationException("Không tìm thấy CD_BocVo để thêm mới TTCuonDay_CD.");

                var submittedIds = new HashSet<long>();

                foreach (DG_TonKhoBTP_v02.Models.ThongTinCuonDay item in items)
                {
                    if (item == null) continue;
                    if (item.SoCuon < 0)
                        throw new InvalidOperationException("Số cuộn không được âm.");

                    if (item.TTCuonDay_CD_ID.HasValue && item.TTCuonDay_CD_ID.Value > 0)
                    {
                        long id = item.TTCuonDay_CD_ID.Value;
                        if (!current.TryGetValue(id, out SourceEditState old))
                            throw new InvalidOperationException($"TTCuonDay_CD id={id} không thuộc TTThanhPham hiện tại.");

                        submittedIds.Add(id);

                        if (item.SoCuon < old.SoCuonDaNhap)
                        {
                            throw new InvalidOperationException(
                                $"TTCuonDay_CD id={id}: đã nhập {old.SoCuonDaNhap} cuộn, " +
                                $"không thể giảm số cuộn nguồn xuống {item.SoCuon}.");
                        }

                        if (old.SoCuonDaNhap > 0)
                        {
                            bool technicalChanged =
                                item.TongChieuDai != old.TongChieuDai ||
                                item.SoDau != old.SoDau ||
                                item.soCuoi != old.SoCuoi ||
                                item.TTLo_ID != old.TTLoId;

                            if (technicalChanged)
                            {
                                throw new InvalidOperationException(
                                    $"TTCuonDay_CD id={id} đã có lịch sử nhập kho. " +
                                    "Không được đổi chiều dài, loại lô, số đầu hoặc số cuối.");
                            }

                            using var cmd = new SQLiteCommand(sqlUpdateAfterImported, conn, tran);
                            cmd.Parameters.AddWithValue("@id", id);
                            cmd.Parameters.AddWithValue("@SoCuon", item.SoCuon);
                            cmd.Parameters.AddWithValue("@GhiChu", string.IsNullOrWhiteSpace(item.Ghichu)
                                ? (object)DBNull.Value
                                : item.Ghichu.Trim());
                            cmd.ExecuteNonQuery();
                        }
                        else
                        {
                            using var cmd = new SQLiteCommand(sqlUpdateFull, conn, tran);
                            BindSourceEditParameters(cmd, item);
                            cmd.Parameters.AddWithValue("@id", id);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        using var cmd = new SQLiteCommand(sqlInsert, conn, tran);
                        BindSourceEditParameters(cmd, item);
                        cmd.Parameters.AddWithValue("@CongDoan_ID", latestCongDoanId);
                        cmd.ExecuteNonQuery();
                    }
                }

                foreach (SourceEditState old in current.Values)
                {
                    if (submittedIds.Contains(old.Id)) continue;

                    if (old.SoCuonDaNhap > 0)
                    {
                        throw new InvalidOperationException(
                            $"TTCuonDay_CD id={old.Id} đã có {old.SoCuonDaNhap} cuộn được nhập kho, không thể xoá dòng nguồn.");
                    }

                    using var delete = new SQLiteCommand(sqlDelete, conn, tran);
                    delete.Parameters.AddWithValue("@id", old.Id);
                    delete.ExecuteNonQuery();
                }

                tran.Commit();
            }
            catch
            {
                tran.Rollback();
                throw;
            }
        }

        private sealed class SourceEditState
        {
            public long Id { get; set; }
            public long CongDoanId { get; set; }
            public int SoCuon { get; set; }
            public int TongChieuDai { get; set; }
            public int? SoDau { get; set; }
            public int? SoCuoi { get; set; }
            public int? TTLoId { get; set; }
            public int SoCuonDaNhap { get; set; }
        }

        private static void BindSourceEditParameters(
            SQLiteCommand cmd,
            DG_TonKhoBTP_v02.Models.ThongTinCuonDay item)
        {
            cmd.Parameters.AddWithValue("@SoCuon", item.SoCuon);
            cmd.Parameters.AddWithValue("@TongChieuDai", item.TongChieuDai);
            cmd.Parameters.AddWithValue("@SoDau", item.SoDau.HasValue ? (object)item.SoDau.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@SoCuoi", item.soCuoi.HasValue ? (object)item.soCuoi.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@GhiChu", string.IsNullOrWhiteSpace(item.Ghichu)
                ? (object)DBNull.Value
                : item.Ghichu.Trim());
            cmd.Parameters.AddWithValue("@TTLo_ID", item.TTLo_ID.HasValue
                ? (object)item.TTLo_ID.Value
                : DBNull.Value);
        }


        private sealed class TTCuonDayEditState
        {
            public long Id { get; set; }
            public long? SourceId { get; set; }
            public int SoCuon { get; set; }
            public int ChieuDai1Cuon { get; set; }
            public int? SoDau { get; set; }
            public int? SoCuoi { get; set; }
            public int? TTLoId { get; set; }
            public string GhiChu { get; set; } = string.Empty;
            public bool CoLichSuCatDay { get; set; }
            public bool CoTTXuatKho { get; set; }
            public bool CoLichSuDownstream => CoLichSuCatDay || CoTTXuatKho;
        }

        private static bool TableExists(SQLiteConnection conn, SQLiteTransaction tran, string tableName)
        {
            using var cmd = new SQLiteCommand(@"
                SELECT 1
                FROM sqlite_master
                WHERE type='table' AND name=@name
                LIMIT 1;", conn, tran);
            cmd.Parameters.AddWithValue("@name", tableName);
            object value = cmd.ExecuteScalar();
            return value != null && value != DBNull.Value;
        }

        private static Dictionary<long, TTCuonDayEditState> LayTrangThaiTTCuonDayDeSua(
            SQLiteConnection conn,
            SQLiteTransaction tran,
            long idNhapKho)
        {
            bool coBangLichSuCatDay = TableExists(conn, tran, "LichSuCatDay");
            bool coBangTTXuatKho = TableExists(conn, tran, "TTXuatKho");

            string lichSuCatExpr = coBangLichSuCatDay
                ? "EXISTS(SELECT 1 FROM LichSuCatDay ls WHERE ls.TTCuonDay_ID = td.id LIMIT 1)"
                : "0";
            string xuatKhoExpr = coBangTTXuatKho
                ? "EXISTS(SELECT 1 FROM TTXuatKho xk WHERE xk.TTCuonDay_ID = td.id LIMIT 1)"
                : "0";

            string sql = $@"
                SELECT
                    td.id,
                    td.TTCuonDay_CD_ID,
                    IFNULL(td.SoCuon, 0) AS SoCuon,
                    IFNULL(td.ChieuDai_1cuon, 0) AS ChieuDai_1cuon,
                    td.SoDau,
                    td.SoCuoi,
                    td.TTLo_ID,
                    IFNULL(td.GhiChu, '') AS GhiChu,
                    CASE WHEN {lichSuCatExpr} THEN 1 ELSE 0 END AS CoLichSuCatDay,
                    CASE WHEN {xuatKhoExpr} THEN 1 ELSE 0 END AS CoTTXuatKho
                FROM TTCuonDay td
                WHERE td.ThongTinNhapKho_ID = @HeaderId
                ORDER BY td.id;";

            var result = new Dictionary<long, TTCuonDayEditState>();
            using var cmd = new SQLiteCommand(sql, conn, tran);
            cmd.Parameters.AddWithValue("@HeaderId", idNhapKho);
            using SQLiteDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var item = new TTCuonDayEditState
                {
                    Id = Convert.ToInt64(reader["id"]),
                    SourceId = reader["TTCuonDay_CD_ID"] == DBNull.Value
                        ? (long?)null
                        : Convert.ToInt64(reader["TTCuonDay_CD_ID"]),
                    SoCuon = Convert.ToInt32(reader["SoCuon"]),
                    ChieuDai1Cuon = Convert.ToInt32(reader["ChieuDai_1cuon"]),
                    SoDau = reader["SoDau"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["SoDau"]),
                    SoCuoi = reader["SoCuoi"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["SoCuoi"]),
                    TTLoId = reader["TTLo_ID"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["TTLo_ID"]),
                    GhiChu = Convert.ToString(reader["GhiChu"]) ?? string.Empty,
                    CoLichSuCatDay = Convert.ToInt32(reader["CoLichSuCatDay"]) == 1,
                    CoTTXuatKho = Convert.ToInt32(reader["CoTTXuatKho"]) == 1
                };
                result[item.Id] = item;
            }

            return result;
        }

        // ════════════════════════════════════════════════════════════════════════
        // CHỨC NĂNG 1 – NHẬP KHO (INSERT TTNhapKho + TTCuonDay), trả về id vừa tạo
        // ════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// INSERT một bản ghi vào TTNhapKho, sau đó INSERT từng dòng cuộn/lô
        /// vào TTCuonDay (ThongTinNhapKho_ID = id vừa tạo), và cập nhật TTThanhPham.
        /// Toàn bộ thực hiện trong 1 transaction – ném ngoại lệ nếu thất bại.
        /// </summary>
        /// <param name="model">Thông tin header nhập kho.</param>
        /// <param name="dsCuon">
        /// Danh sách cuộn / lô chi tiết (từ Frm_DLCuon).
        /// Không được null; có thể rỗng nếu người dùng chưa nhập chi tiết.
        /// </param>
        /// <returns>id (rowid) của bản ghi TTNhapKho vừa tạo.</returns>
        public static long NhapKho(
            DG_TonKhoBTP_v02.Models.NhapKho_Model model,
            List<DG_TonKhoBTP_v02.Models.ThongTinCuonDay> dsCuon)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));
            if (model.TTThanhPham_ID <= 0)
                throw new ArgumentException("TTThanhPham_ID không hợp lệ.", nameof(model));
            if (dsCuon == null || dsCuon.Count == 0)
                throw new InvalidOperationException("Không có dữ liệu cuộn/lô để nhập kho.");

            if (dsCuon.Any(x => x == null || !x.TTCuonDay_CD_ID.HasValue || x.TTCuonDay_CD_ID.Value <= 0))
                throw new InvalidOperationException("Có dòng cuộn/lô không xác định được TTCuonDay_CD_ID nguồn.");

            double tongNhapLanNay = dsCuon.Sum(x => (double)x.SoCuon * x.TongChieuDai);
            if (tongNhapLanNay <= 0)
                throw new InvalidOperationException("Tổng chiều dài nhập kho phải lớn hơn 0.");
            if (tongNhapLanNay - model.SoMet > 0.000001d)
                throw new InvalidOperationException(
                    $"Tổng chiều dài trên danh sách ({tongNhapLanNay:G}) lớn hơn chiều dài còn lại ({model.SoMet:G}).");

            const string sqlGetThanhPham = @"
                SELECT ChieuDaiSau
                FROM TTThanhPham
                WHERE id = @id
                LIMIT 1;";

            // Mỗi lần bấm btnNhapKho luôn tạo một TTNhapKhoTP mới.
            const string sqlInsertHeader = @"
                INSERT INTO TTNhapKhoTP
                    (NgayNhapKho, SoBB, TTThanhPham_ID, TenSP, TongChieuDai, GhiChu, NguoiLam)
                VALUES
                    (@NgayNhapKho, @SoBB, @TTThanhPham_ID, @TenSP, @TongChieuDai, @GhiChu, @NguoiLam);
                SELECT last_insert_rowid();";

            const string sqlGetSource = @"
                SELECT
                    cd.id,
                    IFNULL(cd.SoCuon, 0) AS SoCuonNguon,
                    cd.TongChieuDai,
                    cd.SoDau,
                    cd.SoCuoi,
                    cd.TTLo_ID,
                    COALESCE((
                        SELECT SUM(IFNULL(td.SoCuon, 0))
                        FROM TTCuonDay td
                        WHERE td.TTCuonDay_CD_ID = cd.id
                    ), 0) AS SoCuonDaNhap
                FROM TTCuonDay_CD cd
                INNER JOIN CD_BocVo cbv ON cbv.id = cd.CongDoan_ID
                INNER JOIN CaiDatCDBoc cdb ON cdb.id = cbv.CaiDatCDBoc_ID
                WHERE cd.id = @TTCuonDay_CD_ID
                  AND cdb.TTThanhPham_ID = @TTThanhPham_ID
                LIMIT 1;";

            const string sqlInsertDetail = @"
                INSERT INTO TTCuonDay
                    (SoCuon, ChieuDai_1cuon, SoDau, SoCuoi, GhiChu,
                     ThongTinNhapKho_ID, LoaiDon, KhachHang, TenDuAn,
                     TTLo_ID, Ngay, TTCuonDay_CD_ID)
                VALUES
                    (@SoCuon, @ChieuDai_1cuon, @SoDau, @SoCuoi, @GhiChu,
                     @ThongTinNhapKho_ID, NULL, NULL, NULL,
                     @TTLo_ID, @Ngay, @TTCuonDay_CD_ID);";

            const string sqlUpdateThanhPham = @"
                UPDATE TTThanhPham
                SET KhoiLuongSau = NULL,
                    ChieuDaiSau = CASE
                        WHEN @ChieuDaiConLai < 0 THEN 0
                        ELSE @ChieuDaiConLai
                    END,
                    NhapKho = 1
                WHERE id = @id;";

            using var conn = DB_Base.OpenConnection();
            using var tran = conn.BeginTransaction();

            try
            {
                long headerId;

                // Tạo header mới trước để giữ đúng nghĩa: 1 lần btnNhapKho = 1 TTNhapKhoTP.
                using (var insertHeader = new SQLiteCommand(sqlInsertHeader, conn, tran))
                {
                    insertHeader.Parameters.AddWithValue("@NgayNhapKho",
                        string.IsNullOrWhiteSpace(model.Ngay) ? (object)DBNull.Value : model.Ngay);
                    insertHeader.Parameters.AddWithValue("@SoBB", model.SoBB > 0 ? (object)model.SoBB : DBNull.Value);
                    insertHeader.Parameters.AddWithValue("@TTThanhPham_ID", model.TTThanhPham_ID);
                    insertHeader.Parameters.AddWithValue("@TenSP", model.TenSP ?? string.Empty);
                    insertHeader.Parameters.AddWithValue("@TongChieuDai", model.SoMet);
                    insertHeader.Parameters.AddWithValue("@GhiChu", model.GhiChu ?? string.Empty);
                    insertHeader.Parameters.AddWithValue("@NguoiLam", model.NguoiLam ?? string.Empty);
                    headerId = Convert.ToInt64(insertHeader.ExecuteScalar());
                }

                // Header INSERT đã đưa transaction vào trạng thái ghi.
                // Sau đó kiểm tra lại ChieuDaiSau để tránh dùng snapshot cũ khi có phiên nhập kho đồng thời.
                using (var getTp = new SQLiteCommand(sqlGetThanhPham, conn, tran))
                {
                    getTp.Parameters.AddWithValue("@id", model.TTThanhPham_ID);
                    object value = getTp.ExecuteScalar();
                    if (value == null || value == DBNull.Value)
                        throw new InvalidOperationException($"Không tìm thấy TTThanhPham id={model.TTThanhPham_ID}.");

                    double chieuDaiSauHienTai = Convert.ToDouble(value);
                    if (Math.Abs(chieuDaiSauHienTai - model.SoMet) > 0.000001d)
                    {
                        throw new InvalidOperationException(
                            $"Chiều dài còn lại của MaBin đã thay đổi từ {model.SoMet:G} thành {chieuDaiSauHienTai:G}. " +
                            "Vui lòng tải lại MaBin trước khi nhập kho.");
                    }
                }

                using var getSource = new SQLiteCommand(sqlGetSource, conn, tran);
                getSource.Parameters.Add("@TTCuonDay_CD_ID", DbType.Int64);
                getSource.Parameters.Add("@TTThanhPham_ID", DbType.Int64);

                using var insertDetail = new SQLiteCommand(sqlInsertDetail, conn, tran);
                insertDetail.Parameters.Add("@SoCuon", DbType.Int32);
                insertDetail.Parameters.Add("@ChieuDai_1cuon", DbType.Int32);
                insertDetail.Parameters.Add("@SoDau", DbType.Int32);
                insertDetail.Parameters.Add("@SoCuoi", DbType.Int32);
                insertDetail.Parameters.Add("@GhiChu", DbType.String);
                insertDetail.Parameters.Add("@ThongTinNhapKho_ID", DbType.Int64);
                insertDetail.Parameters.Add("@TTLo_ID", DbType.Int32);
                insertDetail.Parameters.Add("@Ngay", DbType.String);
                insertDetail.Parameters.Add("@TTCuonDay_CD_ID", DbType.Int64);

                double tongNhapThucTe = 0;

                foreach (DG_TonKhoBTP_v02.Models.ThongTinCuonDay requested in dsCuon)
                {
                    long sourceId = requested.TTCuonDay_CD_ID.Value;
                    getSource.Parameters["@TTCuonDay_CD_ID"].Value = sourceId;
                    getSource.Parameters["@TTThanhPham_ID"].Value = model.TTThanhPham_ID;

                    int soCuonNguon;
                    int soCuonDaNhap;
                    int chieuDai1Cuon;
                    int? soDau;
                    int? soCuoi;
                    object ttLoId;

                    using (SQLiteDataReader reader = getSource.ExecuteReader())
                    {
                        if (!reader.Read())
                            throw new InvalidOperationException(
                                $"Không tìm thấy TTCuonDay_CD id={sourceId} thuộc TTThanhPham id={model.TTThanhPham_ID}.");

                        soCuonNguon = reader["SoCuonNguon"] == DBNull.Value ? 0 : Convert.ToInt32(reader["SoCuonNguon"]);
                        soCuonDaNhap = reader["SoCuonDaNhap"] == DBNull.Value ? 0 : Convert.ToInt32(reader["SoCuonDaNhap"]);
                        chieuDai1Cuon = Convert.ToInt32(reader["TongChieuDai"]);
                        soDau = reader["SoDau"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["SoDau"]);
                        soCuoi = reader["SoCuoi"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["SoCuoi"]);
                        ttLoId = reader["TTLo_ID"] == DBNull.Value ? (object)DBNull.Value : Convert.ToInt32(reader["TTLo_ID"]);
                    }

                    int soCuonConLai = soCuonNguon - soCuonDaNhap;
                    if (soCuonConLai < 0) soCuonConLai = 0;

                    if (requested.SoCuon <= 0)
                        throw new InvalidOperationException($"TTCuonDay_CD id={sourceId}: số cuộn nhập phải lớn hơn 0.");

                    if (requested.SoCuon > soCuonConLai)
                    {
                        throw new InvalidOperationException(
                            $"Thông tin cuộn/lô đã thay đổi. TTCuonDay_CD id={sourceId}: " +
                            $"muốn nhập {requested.SoCuon} cuộn nhưng chỉ còn {soCuonConLai} cuộn. " +
                            "Vui lòng tải lại dữ liệu.");
                    }

                    insertDetail.Parameters["@SoCuon"].Value = requested.SoCuon;
                    insertDetail.Parameters["@ChieuDai_1cuon"].Value = chieuDai1Cuon;

                    // Trạng thái trên UI là trạng thái cuối cần ghi. Không dùng ?? fallback vì
                    // thao tác đảo chiều hợp lệ có thể tạo NULL <=> số.
                    int? soDauFinal = requested.SoDau;
                    int? soCuoiFinal = requested.soCuoi;
                    bool laLo = ttLoId != DBNull.Value;
                    if (laLo && (!soDauFinal.HasValue || !soCuoiFinal.HasValue))
                        throw new InvalidOperationException($"TTCuonDay_CD id={sourceId}: Lô phải có đủ Số đầu và Số cuối.");
                    if (laLo && chieuDai1Cuon != Math.Abs(soDauFinal.Value - soCuoiFinal.Value))
                        throw new InvalidOperationException(
                            $"TTCuonDay_CD id={sourceId}: chiều dài lô phải bằng ABS(Số đầu - Số cuối).");

                    insertDetail.Parameters["@SoDau"].Value = soDauFinal.HasValue ? (object)soDauFinal.Value : DBNull.Value;
                    insertDetail.Parameters["@SoCuoi"].Value = soCuoiFinal.HasValue ? (object)soCuoiFinal.Value : DBNull.Value;
                    insertDetail.Parameters["@GhiChu"].Value = requested.Ghichu ?? string.Empty;
                    insertDetail.Parameters["@ThongTinNhapKho_ID"].Value = headerId;
                    insertDetail.Parameters["@TTLo_ID"].Value = ttLoId;
                    insertDetail.Parameters["@Ngay"].Value = string.IsNullOrWhiteSpace(model.Ngay)
                        ? (object)DBNull.Value
                        : model.Ngay;
                    insertDetail.Parameters["@TTCuonDay_CD_ID"].Value = sourceId;

                    tongNhapThucTe += (double)requested.SoCuon * chieuDai1Cuon;
                    if (tongNhapThucTe - model.SoMet > 0.000001d)
                    {
                        throw new InvalidOperationException(
                            $"Tổng chiều dài thực tế theo TTCuonDay_CD ({tongNhapThucTe:G}) " +
                            $"lớn hơn chiều dài còn lại ({model.SoMet:G}). Vui lòng tải lại dữ liệu.");
                    }

                    insertDetail.ExecuteNonQuery();
                }

                double chieuDaiConLai = model.SoMet - tongNhapThucTe;
                if (chieuDaiConLai < 0) chieuDaiConLai = 0;

                using (var updateTp = new SQLiteCommand(sqlUpdateThanhPham, conn, tran))
                {
                    updateTp.Parameters.AddWithValue("@ChieuDaiConLai", chieuDaiConLai);
                    updateTp.Parameters.AddWithValue("@id", model.TTThanhPham_ID);
                    if (updateTp.ExecuteNonQuery() == 0)
                        throw new InvalidOperationException(
                            $"Không cập nhật được TTThanhPham id={model.TTThanhPham_ID} sau khi nhập kho.");
                }

                tran.Commit();
                model.Id = headerId;
                return headerId;
            }
            catch
            {
                tran.Rollback();
                throw;
            }
        }

        // ════════════════════════════════════════════════════════════════════════
        // CHỨC NĂNG 2 – CẬP NHẬT MỘT DÒNG ĐÃ CÓ (UPDATE theo id_NhapKho)
        // ════════════════════════════════════════════════════════════════════════
        public static void CapNhatNhapKho(
            long idNhapKho,
            long ttThanhPhamIdCu,
            double soMetCu,
            DG_TonKhoBTP_v02.Models.NhapKho_Model model,
            List<DG_TonKhoBTP_v02.Models.ThongTinCuonDay> dsCuon,
            bool capNhatTTCuonDay)
        {
            if (idNhapKho <= 0) throw new ArgumentException("idNhapKho không hợp lệ.", nameof(idNhapKho));
            if (model == null) throw new ArgumentNullException(nameof(model));
            if (dsCuon == null || dsCuon.Count == 0)
                throw new InvalidOperationException("Không có dữ liệu cuộn/lô để cập nhật.");
            if (dsCuon.Any(x => x == null || !x.TTCuonDay_CD_ID.HasValue || x.TTCuonDay_CD_ID.Value <= 0))
                throw new InvalidOperationException("Có dòng cuộn/lô không xác định được TTCuonDay_CD_ID nguồn.");

            if (capNhatTTCuonDay)
            {
                if (dsCuon.Any(x => !x.TTCuonDay_ID.HasValue || x.TTCuonDay_ID.Value <= 0))
                    throw new InvalidOperationException("Có dòng cuộn/lô không xác định được TTCuonDay_ID để cập nhật.");

                long duplicateId = dsCuon
                    .GroupBy(x => x.TTCuonDay_ID.Value)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key)
                    .FirstOrDefault();
                if (duplicateId > 0)
                    throw new InvalidOperationException($"TTCuonDay id={duplicateId} bị lặp trong dữ liệu cập nhật.");
            }

            using var conn = DB_Base.OpenConnection();
            using var tran = conn.BeginTransaction();
            try
            {
                double tongCu;
                using (var cmd = new SQLiteCommand(@"
                    SELECT COALESCE(SUM(IFNULL(SoCuon,0) * IFNULL(ChieuDai_1cuon,0)),0)
                    FROM TTCuonDay
                    WHERE ThongTinNhapKho_ID=@id;", conn, tran))
                {
                    cmd.Parameters.AddWithValue("@id", idNhapKho);
                    tongCu = Convert.ToDouble(cmd.ExecuteScalar());
                }

                const string sqlHeader = @"
                    UPDATE TTNhapKhoTP
                    SET NgayNhapKho=@NgayNhapKho,
                        SoBB=@SoBB,
                        TTThanhPham_ID=@TTThanhPham_ID,
                        TenSP=@TenSP,
                        TongChieuDai=@TongChieuDai,
                        GhiChu=@GhiChu,
                        NguoiLam=@NguoiLam
                    WHERE id=@id;";

                using (var cmd = new SQLiteCommand(sqlHeader, conn, tran))
                {
                    cmd.Parameters.AddWithValue("@id", idNhapKho);
                    cmd.Parameters.AddWithValue("@NgayNhapKho", string.IsNullOrWhiteSpace(model.Ngay) ? (object)DBNull.Value : model.Ngay);
                    cmd.Parameters.AddWithValue("@SoBB", model.SoBB > 0 ? (object)model.SoBB : DBNull.Value);
                    cmd.Parameters.AddWithValue("@TTThanhPham_ID", model.TTThanhPham_ID);
                    cmd.Parameters.AddWithValue("@TenSP", model.TenSP ?? string.Empty);
                    cmd.Parameters.AddWithValue("@TongChieuDai", model.SoMet);
                    cmd.Parameters.AddWithValue("@GhiChu", model.GhiChu ?? string.Empty);
                    cmd.Parameters.AddWithValue("@NguoiLam", model.NguoiLam ?? string.Empty);
                    if (cmd.ExecuteNonQuery() == 0)
                        throw new InvalidOperationException($"Không cập nhật được TTNhapKhoTP id={idNhapKho}.");
                }

                if (capNhatTTCuonDay)
                {
                    Dictionary<long, TTCuonDayEditState> hienTai = LayTrangThaiTTCuonDayDeSua(conn, tran, idNhapKho);
                    if (hienTai.Count == 0)
                        throw new InvalidOperationException($"TTNhapKhoTP id={idNhapKho} không có TTCuonDay để cập nhật.");

                    var submitted = dsCuon.ToDictionary(x => x.TTCuonDay_ID.Value, x => x);

                    foreach (var pair in submitted)
                    {
                        long ttCuonDayId = pair.Key;
                        var item = pair.Value;

                        if (!hienTai.TryGetValue(ttCuonDayId, out TTCuonDayEditState old))
                            throw new InvalidOperationException(
                                $"TTCuonDay id={ttCuonDayId} không thuộc TTNhapKhoTP id={idNhapKho}.");

                        if (!old.SourceId.HasValue || old.SourceId.Value != item.TTCuonDay_CD_ID.Value)
                            throw new InvalidOperationException(
                                $"TTCuonDay id={ttCuonDayId}: TTCuonDay_CD_ID nguồn không được thay đổi khi sửa phiếu.");

                        if (old.CoLichSuDownstream)
                        {
                            bool protectedChanged =
                                old.SoCuon != item.SoCuon
                                || old.ChieuDai1Cuon != item.TongChieuDai
                                || old.SoDau != item.SoDau
                                || old.SoCuoi != item.soCuoi
                                || old.TTLoId != item.TTLo_ID;

                            if (protectedChanged)
                            {
                                throw new InvalidOperationException(
                                    $"TTCuonDay id={ttCuonDayId} đã có lịch sử cắt dây hoặc xuất kho; chỉ được sửa GhiChu.");
                            }
                        }
                    }

                    foreach (TTCuonDayEditState old in hienTai.Values)
                    {
                        if (submitted.ContainsKey(old.Id)) continue;
                        if (old.CoLichSuDownstream)
                        {
                            throw new InvalidOperationException(
                                $"Không thể xoá TTCuonDay id={old.Id} vì đã có lịch sử cắt dây hoặc xuất kho.");
                        }
                    }

                    // Kiểm tra tổng số cuộn theo từng nguồn, loại trừ lượng đang thuộc chính phiếu hiện tại.
                    const string sqlSource = @"
                        SELECT
                            IFNULL(cd.SoCuon, 0) AS SoCuonNguon,
                            COALESCE((
                                SELECT SUM(IFNULL(td.SoCuon,0))
                                FROM TTCuonDay td
                                WHERE td.TTCuonDay_CD_ID = cd.id
                                  AND td.ThongTinNhapKho_ID <> @HeaderId
                            ),0) AS DaNhapKhac
                        FROM TTCuonDay_CD cd
                        WHERE cd.id=@SourceId
                        LIMIT 1;";

                    using var getSource = new SQLiteCommand(sqlSource, conn, tran);
                    getSource.Parameters.Add("@HeaderId", DbType.Int64);
                    getSource.Parameters.Add("@SourceId", DbType.Int64);
                    getSource.Parameters["@HeaderId"].Value = idNhapKho;

                    foreach (var group in dsCuon.GroupBy(x => x.TTCuonDay_CD_ID.Value))
                    {
                        long sourceId = group.Key;
                        int tongSoCuonYeuCau = group.Sum(x => x.SoCuon);
                        if (tongSoCuonYeuCau <= 0)
                            throw new InvalidOperationException($"TTCuonDay_CD id={sourceId}: số cuộn cập nhật phải lớn hơn 0.");

                        getSource.Parameters["@SourceId"].Value = sourceId;
                        using var reader = getSource.ExecuteReader();
                        if (!reader.Read())
                            throw new InvalidOperationException($"Không tìm thấy TTCuonDay_CD id={sourceId}.");

                        int soCuonNguon = reader["SoCuonNguon"] == DBNull.Value ? 0 : Convert.ToInt32(reader["SoCuonNguon"]);
                        int daNhapKhac = reader["DaNhapKhac"] == DBNull.Value ? 0 : Convert.ToInt32(reader["DaNhapKhac"]);
                        int conKhaDung = soCuonNguon - daNhapKhac;
                        if (conKhaDung < 0) conKhaDung = 0;

                        if (tongSoCuonYeuCau > conKhaDung)
                        {
                            throw new InvalidOperationException(
                                $"TTCuonDay_CD id={sourceId}: tổng số cuộn cập nhật ({tongSoCuonYeuCau}) " +
                                $"vượt quá số còn khả dụng ({conKhaDung}).");
                        }
                    }

                    foreach (var item in dsCuon)
                    {
                        if (item.SoCuon <= 0)
                            throw new InvalidOperationException($"TTCuonDay id={item.TTCuonDay_ID}: số cuộn phải lớn hơn 0.");
                        if (item.TongChieuDai < 0)
                            throw new InvalidOperationException($"TTCuonDay id={item.TTCuonDay_ID}: chiều dài không hợp lệ.");
                        if (item.TTLo_ID.HasValue && (!item.SoDau.HasValue || !item.soCuoi.HasValue))
                            throw new InvalidOperationException($"TTCuonDay id={item.TTCuonDay_ID}: Lô phải có đủ Số đầu và Số cuối.");
                    }

                    // Xoá đúng các detail bị người dùng loại khỏi Frm_DLCuon, không xoá-all/reinsert.
                    using (var del = new SQLiteCommand(@"
                        DELETE FROM TTCuonDay
                        WHERE id=@Id AND ThongTinNhapKho_ID=@HeaderId;", conn, tran))
                    {
                        del.Parameters.Add("@Id", DbType.Int64);
                        del.Parameters.Add("@HeaderId", DbType.Int64);
                        del.Parameters["@HeaderId"].Value = idNhapKho;

                        foreach (TTCuonDayEditState old in hienTai.Values)
                        {
                            if (submitted.ContainsKey(old.Id)) continue;
                            del.Parameters["@Id"].Value = old.Id;
                            if (del.ExecuteNonQuery() != 1)
                                throw new InvalidOperationException($"Không xoá được TTCuonDay id={old.Id}.");
                        }
                    }

                    const string sqlUpdateDetail = @"
                        UPDATE TTCuonDay
                        SET SoCuon=@SoCuon,
                            ChieuDai_1cuon=@ChieuDai,
                            SoDau=@SoDau,
                            SoCuoi=@SoCuoi,
                            GhiChu=@GhiChu,
                            TTLo_ID=@TTLo_ID,
                            Ngay=@Ngay
                        WHERE id=@Id AND ThongTinNhapKho_ID=@HeaderId;";

                    const string sqlUpdateLockedNote = @"
                        UPDATE TTCuonDay
                        SET GhiChu=@GhiChu
                        WHERE id=@Id AND ThongTinNhapKho_ID=@HeaderId;";

                    using var updateDetail = new SQLiteCommand(sqlUpdateDetail, conn, tran);
                    updateDetail.Parameters.Add("@SoCuon", DbType.Int32);
                    updateDetail.Parameters.Add("@ChieuDai", DbType.Int32);
                    updateDetail.Parameters.Add("@SoDau", DbType.Int32);
                    updateDetail.Parameters.Add("@SoCuoi", DbType.Int32);
                    updateDetail.Parameters.Add("@GhiChu", DbType.String);
                    updateDetail.Parameters.Add("@TTLo_ID", DbType.Int32);
                    updateDetail.Parameters.Add("@Ngay", DbType.String);
                    updateDetail.Parameters.Add("@Id", DbType.Int64);
                    updateDetail.Parameters.Add("@HeaderId", DbType.Int64);
                    updateDetail.Parameters["@HeaderId"].Value = idNhapKho;

                    using var updateLockedNote = new SQLiteCommand(sqlUpdateLockedNote, conn, tran);
                    updateLockedNote.Parameters.Add("@GhiChu", DbType.String);
                    updateLockedNote.Parameters.Add("@Id", DbType.Int64);
                    updateLockedNote.Parameters.Add("@HeaderId", DbType.Int64);
                    updateLockedNote.Parameters["@HeaderId"].Value = idNhapKho;

                    foreach (var item in dsCuon)
                    {
                        long id = item.TTCuonDay_ID.Value;
                        TTCuonDayEditState old = hienTai[id];

                        if (old.CoLichSuDownstream)
                        {
                            updateLockedNote.Parameters["@GhiChu"].Value = item.Ghichu ?? string.Empty;
                            updateLockedNote.Parameters["@Id"].Value = id;
                            if (updateLockedNote.ExecuteNonQuery() != 1)
                                throw new InvalidOperationException($"Không cập nhật được GhiChu của TTCuonDay id={id}.");
                            continue;
                        }

                        updateDetail.Parameters["@SoCuon"].Value = item.SoCuon;
                        updateDetail.Parameters["@ChieuDai"].Value = item.TongChieuDai;
                        updateDetail.Parameters["@SoDau"].Value = item.SoDau.HasValue ? (object)item.SoDau.Value : DBNull.Value;
                        updateDetail.Parameters["@SoCuoi"].Value = item.soCuoi.HasValue ? (object)item.soCuoi.Value : DBNull.Value;
                        updateDetail.Parameters["@GhiChu"].Value = item.Ghichu ?? string.Empty;
                        updateDetail.Parameters["@TTLo_ID"].Value = item.TTLo_ID.HasValue ? (object)item.TTLo_ID.Value : DBNull.Value;
                        updateDetail.Parameters["@Ngay"].Value = string.IsNullOrWhiteSpace(model.Ngay) ? (object)DBNull.Value : model.Ngay;
                        updateDetail.Parameters["@Id"].Value = id;

                        if (updateDetail.ExecuteNonQuery() != 1)
                            throw new InvalidOperationException($"Không cập nhật được TTCuonDay id={id}.");
                    }

                    double tongMoi = dsCuon.Sum(x => (double)x.SoCuon * x.TongChieuDai);
                    double delta = tongCu - tongMoi;
                    using var updateTp = new SQLiteCommand(@"
                        UPDATE TTThanhPham
                        SET ChieuDaiSau = MAX(0, IFNULL(ChieuDaiSau,0) + @Delta)
                        WHERE id=@id;", conn, tran);
                    updateTp.Parameters.AddWithValue("@Delta", delta);
                    updateTp.Parameters.AddWithValue("@id", model.TTThanhPham_ID);
                    if (updateTp.ExecuteNonQuery() == 0)
                        throw new InvalidOperationException($"Không cập nhật được TTThanhPham id={model.TTThanhPham_ID}.");
                }

                tran.Commit();
            }
            catch
            {
                tran.Rollback();
                throw;
            }
        }



        public static DataTable LayTTNhapKhoTPTheoTTThanhPhamId(long ttThanhPhamId)
        {
            const string sql = @"
                SELECT id, SoBB, GhiChu, NguoiLam, NgayNhapKho, TongChieuDai
                FROM TTNhapKhoTP
                WHERE TTThanhPham_ID = @TTThanhPham_ID
                LIMIT 1;";

            DataTable dt = new DataTable();
            using var conn = DB_Base.OpenConnection();
            using var cmd = new SQLiteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@TTThanhPham_ID", ttThanhPhamId);
            using var adapter = new SQLiteDataAdapter(cmd);
            adapter.Fill(dt);
            return dt;
        }

        public static DataTable LayTTNhapKhoTPTheoId(long idNhapKho)
        {
            const string sql = @"
                SELECT
                    nk.id AS id_NhapKho,
                    nk.TTThanhPham_ID,
                    nk.NgayNhapKho AS ngay,
                    nk.SoBB AS soBB,
                    nk.NguoiLam AS nguoiLam,
                    nk.TenSP AS tenSP,
                    nk.TongChieuDai AS soMet,
                    tp.MaBin AS maBin2,
                    nk.GhiChu AS ghiChu
                FROM TTNhapKhoTP nk
                INNER JOIN TTThanhPham tp ON tp.id = nk.TTThanhPham_ID
                WHERE nk.id = @id
                LIMIT 1;";

            DataTable dt = new DataTable();
            using var conn = DB_Base.OpenConnection();
            using var cmd = new SQLiteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", idNhapKho);
            using var adapter = new SQLiteDataAdapter(cmd);
            adapter.Fill(dt);
            return dt;
        }

        // ════════════════════════════════════════════════════════════════════════
        // GIỮ NGUYÊN – LuuDanhSachNhapKho (batch insert từ grid – dùng nếu cần)
        // ════════════════════════════════════════════════════════════════════════

        public static int LuuDanhSachNhapKho(DataGridView grid, string nguoiLam)
        {
            if (grid == null || grid.Rows.Count == 0)
                return 0;

            const string sqlInsert = @"
            INSERT INTO TTNhapKho
                (Ngay, SoBB, TTThanhPham_ID, TenSP, SoMet,
                 LoaiDon, KhachHang, GhiChu,
                 Loai, ChieuCaoLo, TongChieuDai, SoDau, soCuoi, ThongTinCuon,
                 NguoiLam)
            VALUES
                (@Ngay, @SoBB, @TTThanhPham_ID, @TenSP, @SoMet,
                 @LoaiDon, @KhachHang, @GhiChu,
                 @Loai, @ChieuCaoLo, @TongChieuDai, @SoDau, @soCuoi, @ThongTinCuon,
                 @NguoiLam);";

            int rowsInserted = 0;
            List<long> dsTTThanhPhamId = new List<long>();

            using var conn = DB_Base.OpenConnection();
            using var tran = conn.BeginTransaction();

            try
            {
                using var cmdInsert = new SQLiteCommand(sqlInsert, conn, tran);

                cmdInsert.Parameters.Add("@Ngay", DbType.String);
                cmdInsert.Parameters.Add("@SoBB", DbType.Int32);
                cmdInsert.Parameters.Add("@TTThanhPham_ID", DbType.Int64);
                cmdInsert.Parameters.Add("@TenSP", DbType.String);
                cmdInsert.Parameters.Add("@SoMet", DbType.Double);
                cmdInsert.Parameters.Add("@LoaiDon", DbType.String);
                cmdInsert.Parameters.Add("@KhachHang", DbType.String);
                cmdInsert.Parameters.Add("@GhiChu", DbType.String);
                cmdInsert.Parameters.Add("@Loai", DbType.String);
                cmdInsert.Parameters.Add("@ChieuCaoLo", DbType.Double);
                cmdInsert.Parameters.Add("@TongChieuDai", DbType.Double);
                cmdInsert.Parameters.Add("@SoDau", DbType.Int32);
                cmdInsert.Parameters.Add("@soCuoi", DbType.Int32);
                cmdInsert.Parameters.Add("@ThongTinCuon", DbType.String);
                cmdInsert.Parameters.Add("@NguoiLam", DbType.String);

                foreach (DataGridViewRow row in grid.Rows)
                {
                    if (row.IsNewRow) continue;

                    long? ttThanhPhamId = ParseLong(row, "TTThanhPham_ID");
                    if (!ttThanhPhamId.HasValue)
                        throw new InvalidOperationException("Không tìm thấy TTThanhPham_ID để cập nhật nhập kho.");

                    string loai = ParseStr(row, "loai");
                    bool isLo = loai == "Lô";

                    cmdInsert.Parameters["@Ngay"].Value = ParseStr(row, "ngay") ?? (object)DBNull.Value;
                    cmdInsert.Parameters["@SoBB"].Value = ParseInt(row, "soBB") ?? (object)DBNull.Value;
                    cmdInsert.Parameters["@TTThanhPham_ID"].Value = ttThanhPhamId.Value;
                    cmdInsert.Parameters["@TenSP"].Value = ParseStr(row, "tenSP") ?? (object)DBNull.Value;
                    cmdInsert.Parameters["@SoMet"].Value = ParseDbl(row, "soMet") ?? (object)DBNull.Value;
                    cmdInsert.Parameters["@LoaiDon"].Value = ParseStr(row, "loaiDon") ?? (object)DBNull.Value;
                    cmdInsert.Parameters["@KhachHang"].Value = ParseStr(row, "khachHang") ?? (object)DBNull.Value;
                    cmdInsert.Parameters["@GhiChu"].Value = ParseStr(row, "ghiChu") ?? (object)DBNull.Value;
                    cmdInsert.Parameters["@Loai"].Value = loai ?? (object)DBNull.Value;
                    cmdInsert.Parameters["@ChieuCaoLo"].Value = isLo ? ParseDbl(row, "chieuCaoLo") ?? (object)DBNull.Value : DBNull.Value;
                    cmdInsert.Parameters["@TongChieuDai"].Value = isLo ? ParseDbl(row, "tongChieuDai") ?? (object)DBNull.Value : DBNull.Value;
                    cmdInsert.Parameters["@SoDau"].Value = isLo ? ParseInt(row, "soDau") ?? (object)DBNull.Value : DBNull.Value;
                    cmdInsert.Parameters["@soCuoi"].Value = isLo ? ParseInt(row, "soCuoi") ?? (object)DBNull.Value : DBNull.Value;
                    cmdInsert.Parameters["@ThongTinCuon"].Value = isLo ? DBNull.Value : ParseStr(row, "cuon") ?? (object)DBNull.Value;
                    cmdInsert.Parameters["@NguoiLam"].Value = string.IsNullOrWhiteSpace(nguoiLam) ? (object)DBNull.Value : nguoiLam.Trim();

                    cmdInsert.ExecuteNonQuery();
                    dsTTThanhPhamId.Add(ttThanhPhamId.Value);
                    rowsInserted++;
                }

                List<long> dsIdKhongTrung = dsTTThanhPhamId.Distinct().ToList();

                if (dsIdKhongTrung.Count > 0)
                {
                    List<string> paramNames = new List<string>();
                    for (int i = 0; i < dsIdKhongTrung.Count; i++)
                        paramNames.Add("@id" + i);

                    string sqlUpdateThanhPham = $@"
                        UPDATE TTThanhPham
                        SET KhoiLuongSau = 0,
                            ChieuDaiSau = 0,
                            NhapKho = 1
                        WHERE id IN ({string.Join(",", paramNames)});";

                    using var cmdUpdateThanhPham = new SQLiteCommand(sqlUpdateThanhPham, conn, tran);
                    for (int i = 0; i < dsIdKhongTrung.Count; i++)
                        cmdUpdateThanhPham.Parameters.AddWithValue("@id" + i, dsIdKhongTrung[i]);

                    int updated = cmdUpdateThanhPham.ExecuteNonQuery();
                    if (updated == 0)
                        throw new InvalidOperationException("Không cập nhật được dữ liệu TTThanhPham sau khi nhập kho.");
                }

                tran.Commit();
                return rowsInserted;
            }
            catch
            {
                tran.Rollback();
                throw;
            }
        }

        // ════════════════════════════════════════════════════════════════════════
        // CHỨC NĂNG 3 – TÌM KIẾM
        // ════════════════════════════════════════════════════════════════════════

        public static DataTable TimKiemNhapKho(string keyword)
        {
            keyword = keyword?.Trim() ?? string.Empty;

            string keywordDateDMY = keyword;
            string keywordDateISO = keyword;
            int? keywordSoBB = int.TryParse(keyword, out int soBB) ? soBB : (int?)null;

            string[] dateFormats =
            {
                "dd/MM/yyyy", "d/M/yyyy", "yyyy-MM-dd", "dd-MM-yyyy", "d-M-yyyy"
            };

            if (DateTime.TryParseExact(
                    keyword,
                    dateFormats,
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None,
                    out DateTime parsedDate))
            {
                keywordDateDMY = parsedDate.ToString("dd/MM/yyyy");
                keywordDateISO = parsedDate.ToString("yyyy-MM-dd");
            }

            const string sql = @"
                WITH found AS
                (
                    SELECT
                        nk.id AS id_NhapKho,
                        nk.TTThanhPham_ID,
                        CASE
                            WHEN nk.NgayNhapKho LIKE '____-__-__'
                            THEN strftime('%d/%m/%Y', nk.NgayNhapKho)
                            ELSE nk.NgayNhapKho
                        END AS ngay,
                        nk.SoBB AS soBB,
                        nk.NguoiLam AS nguoiLam,
                        nk.TenSP AS tenSP,
                        IFNULL(tp.MaBin, '') AS maBin2,
                        nk.TongChieuDai AS soMet,
                        nk.GhiChu AS ghiChu,
                        bs.TenChiTiet AS tenChiTiet,
                        bs.TieuChuan AS tieuChuan,
                        CAST(IFNULL(bs.T, 0) AS REAL) AS heSoT
                    FROM TTNhapKhoTP nk
                    LEFT JOIN TTThanhPham tp ON tp.id = nk.TTThanhPham_ID
                    LEFT JOIN TTBoSung bs ON bs.DanhSachMaSP_ID = tp.DanhSachSP_ID
                    WHERE
                           TRIM(IFNULL(nk.TenSP, '')) = @keyword COLLATE NOCASE
                        OR TRIM(IFNULL(nk.NgayNhapKho, '')) = @keyword COLLATE NOCASE
                        OR TRIM(IFNULL(nk.NgayNhapKho, '')) = @keywordDateDMY COLLATE NOCASE
                        OR TRIM(IFNULL(nk.NgayNhapKho, '')) = @keywordDateISO COLLATE NOCASE
                        OR (@keywordSoBB IS NOT NULL AND nk.SoBB = @keywordSoBB)
                        OR TRIM(IFNULL(tp.MaBin, '')) = @keyword COLLATE NOCASE
                    ORDER BY nk.id DESC
                    LIMIT 200
                )
                SELECT
                    f.*,
                    cd.LoaiDon AS loaiDon,
                    cd.KhachHang AS khachHang,
                    CASE WHEN cd.TTLo_ID IS NULL THEN 'Cuộn' ELSE 'Lô' END AS loai,
                    lo.KichThuoc AS chieuCaoLo,
                    cd.TenDuAn AS tenDuAn,
                    lo.KhoiLuong AS klLoKhoiLuong,
                    lo.KhoiLuongCaNanPhu AS klLoKhoiLuongCaNanPhu,
                    cd.TTCuonDay_CD_ID AS ct_TTCuonDay_CD_ID,
                    cd.SoCuon AS ct_SoCuon,
                    cd.ChieuDai_1cuon AS ct_TongChieuDai,
                    cd.SoDau AS ct_SoDau,
                    cd.SoCuoi AS ct_soCuoi,
                    cd.GhiChu AS ct_GhiChu,
                    cd.TTLo_ID AS ct_TTLo_ID,
                    lo.KichThuoc AS ct_KichThuocLo
                FROM found f
                LEFT JOIN TTCuonDay cd ON cd.ThongTinNhapKho_ID = f.id_NhapKho
                LEFT JOIN TTLo lo ON lo.id = cd.TTLo_ID
                ORDER BY f.id_NhapKho DESC, cd.id;";

            DataTable dt = new DataTable();
            using var conn = DB_Base.OpenConnection();
            using var cmd = new SQLiteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@keyword", keyword);
            cmd.Parameters.AddWithValue("@keywordDateDMY", keywordDateDMY);
            cmd.Parameters.AddWithValue("@keywordDateISO", keywordDateISO);
            cmd.Parameters.AddWithValue("@keywordSoBB", keywordSoBB.HasValue ? (object)keywordSoBB.Value : DBNull.Value);
            using var adapter = new SQLiteDataAdapter(cmd);
            adapter.Fill(dt);
            return dt;
        }

        /// <summary>
        /// Tìm chính xác một mã bin trong TTThanhPham để lấy TTThanhPham_ID khi import Excel.
        /// Không dùng LIKE để tránh lấy nhầm mã bin gần giống.
        /// </summary>
        public static DataTable LayTTThanhPhamTheoMaBin(string maBin)
        {
            const string sql = @"
                SELECT  tp.id          AS TTThanhPham_ID,
                        tp.MaBin,
                        sp.Ten,
                        tp.ChieuDaiSau,
                        tp.GhiChu,
                        tp.NhapKho
                FROM    TTThanhPham   tp
                LEFT JOIN DanhSachMaSP sp ON sp.id = tp.DanhSachSP_ID
                WHERE   TRIM(tp.MaBin) = TRIM(@MaBin)
                LIMIT   2;";

            DataTable dt = new DataTable();

            using var conn = DB_Base.OpenConnection();
            using var cmd = new SQLiteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@MaBin", maBin ?? string.Empty);

            using var adapter = new SQLiteDataAdapter(cmd);
            adapter.Fill(dt);

            return dt;
        }

        /// <summary>
        /// Lấy lại danh sách nhập kho theo id vừa import, trả về đúng shape dữ liệu mà LoadNhapKhoVaoGrid đang dùng.
        /// </summary>
        public static DataTable LayNhapKhoTheoIds(IEnumerable<long> ids)
        {
            List<long> idList = ids == null
                ? new List<long>()
                : ids.Where(x => x > 0).Distinct().ToList();

            DataTable dt = new DataTable();
            if (idList.Count == 0) return dt;

            List<string> paramNames = new List<string>();
            for (int i = 0; i < idList.Count; i++) paramNames.Add("@id" + i);

            string sql = $@"
                SELECT
                    nk.id AS id_NhapKho,
                    nk.TTThanhPham_ID,
                    CASE
                        WHEN nk.NgayNhapKho LIKE '____-__-__'
                        THEN strftime('%d/%m/%Y', nk.NgayNhapKho)
                        ELSE nk.NgayNhapKho
                    END AS ngay,
                    nk.SoBB AS soBB,
                    nk.NguoiLam AS nguoiLam,
                    nk.TenSP AS tenSP,
                    IFNULL(tp.MaBin, '') AS maBin2,
                    nk.TongChieuDai AS soMet,
                    nk.GhiChu AS ghiChu,
                    cd.LoaiDon AS loaiDon,
                    cd.KhachHang AS khachHang,
                    CASE WHEN cd.TTLo_ID IS NULL THEN 'Cuộn' ELSE 'Lô' END AS loai,
                    lo.KichThuoc AS chieuCaoLo,
                    cd.TenDuAn AS tenDuAn,
                    bs.TenChiTiet AS tenChiTiet,
                    bs.TieuChuan AS tieuChuan,
                    CAST(IFNULL(bs.T, 0) AS REAL) AS heSoT,
                    lo.KhoiLuong AS klLoKhoiLuong,
                    lo.KhoiLuongCaNanPhu AS klLoKhoiLuongCaNanPhu,
                    cd.TTCuonDay_CD_ID AS ct_TTCuonDay_CD_ID,
                    cd.SoCuon AS ct_SoCuon,
                    cd.ChieuDai_1cuon AS ct_TongChieuDai,
                    cd.SoDau AS ct_SoDau,
                    cd.SoCuoi AS ct_soCuoi,
                    cd.GhiChu AS ct_GhiChu,
                    cd.TTLo_ID AS ct_TTLo_ID,
                    lo.KichThuoc AS ct_KichThuocLo
                FROM TTNhapKhoTP nk
                LEFT JOIN TTThanhPham tp ON tp.id = nk.TTThanhPham_ID
                LEFT JOIN TTBoSung bs ON bs.DanhSachMaSP_ID = tp.DanhSachSP_ID
                LEFT JOIN TTCuonDay cd ON cd.ThongTinNhapKho_ID = nk.id
                LEFT JOIN TTLo lo ON lo.id = cd.TTLo_ID
                WHERE nk.id IN ({string.Join(",", paramNames)})
                ORDER BY nk.id DESC, cd.id;";

            using var conn = DB_Base.OpenConnection();
            using var cmd = new SQLiteCommand(sql, conn);
            for (int i = 0; i < idList.Count; i++) cmd.Parameters.AddWithValue("@id" + i, idList[i]);
            using var adapter = new SQLiteDataAdapter(cmd);
            adapter.Fill(dt);
            return dt;
        }

        /// <summary>
        /// Xoá trọn một lần nhập kho (1 TTNhapKhoTP + toàn bộ TTCuonDay trực thuộc).
        ///
        /// Rule nghiệp vụ:
        /// - Chỉ được xoá khi KHÔNG có bất kỳ TTCuonDay nào của lần nhập đã phát sinh
        ///   LichSuCatDay hoặc TTXuatKho.
        /// - Nếu chỉ một TTCuonDay có downstream thì chặn xoá toàn bộ TTNhapKhoTP.
        /// - Khi xoá thành công, hoàn trả tổng chiều dài thực tế của các TTCuonDay
        ///   về TTThanhPham.ChieuDaiSau.
        /// - Không tin snapshot TTThanhPham_ID / số mét từ UI; toàn bộ dữ liệu dùng để xoá
        ///   được đọc lại trong cùng transaction.
        /// </summary>
        public static void XoaMotDong(long idNhapKho)
        {
            if (idNhapKho <= 0)
                throw new ArgumentException("idNhapKho không hợp lệ.", nameof(idNhapKho));

            using var conn = DB_Base.OpenConnection();
            using var tran = conn.BeginTransaction();

            try
            {
                long ttThanhPhamId;

                // Đọc đúng TTNhapKhoTP đang xoá trong transaction.
                using (var cmd = new SQLiteCommand(@"
                    SELECT TTThanhPham_ID
                    FROM TTNhapKhoTP
                    WHERE id = @id
                    LIMIT 1;", conn, tran))
                {
                    cmd.Parameters.AddWithValue("@id", idNhapKho);
                    object value = cmd.ExecuteScalar();

                    if (value == null || value == DBNull.Value)
                        throw new InvalidOperationException(
                            $"Không tìm thấy TTNhapKhoTP id={idNhapKho}.");

                    ttThanhPhamId = Convert.ToInt64(value);
                    if (ttThanhPhamId <= 0)
                        throw new InvalidOperationException(
                            $"TTNhapKhoTP id={idNhapKho} không có TTThanhPham_ID hợp lệ.");
                }

                // Đọc toàn bộ TTCuonDay và trạng thái downstream bằng chính helper
                // đang dùng cho luồng sửa. Helper này tương thích cả DB cũ chưa có
                // bảng LichSuCatDay.
                Dictionary<long, TTCuonDayEditState> dsTTCuonDay =
                    LayTrangThaiTTCuonDayDeSua(conn, tran, idNhapKho);

                if (dsTTCuonDay.Values.Any(x => x.CoLichSuDownstream))
                {
                    throw new InvalidOperationException(
                        "Không thể xoá do đã có lịch sử cắt dây/xuất kho.");
                }

                // Hoàn trả đúng lượng đã nhập của riêng lần nhập này.
                // Không dùng TTNhapKhoTP.TongChieuDai vì đây là snapshot chiều dài
                // còn lại tại thời điểm nhập, không phải tổng lượng thực tế đã nhập.
                double tongChieuDaiHoanTra = dsTTCuonDay.Values.Sum(
                    x => (double)x.SoCuon * x.ChieuDai1Cuon);

                // Xoá detail chủ động thay vì dựa hoàn toàn vào ON DELETE CASCADE.
                // Downstream đã được kiểm tra ở trên nên thao tác này không được phép
                // làm mất lịch sử cắt dây / xuất kho.
                using (var cmd = new SQLiteCommand(@"
                    DELETE FROM TTCuonDay
                    WHERE ThongTinNhapKho_ID = @id;", conn, tran))
                {
                    cmd.Parameters.AddWithValue("@id", idNhapKho);
                    cmd.ExecuteNonQuery();
                }

                // Xoá đúng header TTNhapKhoTP.
                using (var cmd = new SQLiteCommand(@"
                    DELETE FROM TTNhapKhoTP
                    WHERE id = @id;", conn, tran))
                {
                    cmd.Parameters.AddWithValue("@id", idNhapKho);

                    if (cmd.ExecuteNonQuery() != 1)
                        throw new InvalidOperationException(
                            $"Không xoá được TTNhapKhoTP id={idNhapKho}.");
                }

                // Trả lại chiều dài của lần nhập vừa xoá.
                // NhapKho chỉ về 0 khi TTThanhPham không còn bất kỳ lần nhập nào khác.
                using (var cmd = new SQLiteCommand(@"
                    UPDATE TTThanhPham
                    SET ChieuDaiSau = IFNULL(ChieuDaiSau, 0) + @TongHoanTra,
                        NhapKho = CASE
                            WHEN EXISTS (
                                SELECT 1
                                FROM TTNhapKhoTP nk
                                WHERE nk.TTThanhPham_ID = @TTThanhPham_ID
                                LIMIT 1
                            ) THEN 1
                            ELSE 0
                        END
                    WHERE id = @TTThanhPham_ID;", conn, tran))
                {
                    cmd.Parameters.AddWithValue("@TongHoanTra", tongChieuDaiHoanTra);
                    cmd.Parameters.AddWithValue("@TTThanhPham_ID", ttThanhPhamId);

                    if (cmd.ExecuteNonQuery() != 1)
                        throw new InvalidOperationException(
                            $"Không cập nhật được TTThanhPham id={ttThanhPhamId} sau khi xoá nhập kho.");
                }

                tran.Commit();
            }
            catch
            {
                tran.Rollback();
                throw;
            }
        }

        public static List<DG_TonKhoBTP_v02.Models.ThongTinCuonDay> LayThongTinCuonDay(long idNhapKho)
        {
            var result = new List<DG_TonKhoBTP_v02.Models.ThongTinCuonDay>();
            if (idNhapKho <= 0) return result;

            using var conn = DB_Base.OpenConnection();

            bool coBangLichSuCatDay = TableExists(conn, null, "LichSuCatDay");
            bool coBangTTXuatKho = TableExists(conn, null, "TTXuatKho");
            string lichSuCatExpr = coBangLichSuCatDay
                ? "EXISTS(SELECT 1 FROM LichSuCatDay ls WHERE ls.TTCuonDay_ID = cd.id LIMIT 1)"
                : "0";
            string xuatKhoExpr = coBangTTXuatKho
                ? "EXISTS(SELECT 1 FROM TTXuatKho xk WHERE xk.TTCuonDay_ID = cd.id LIMIT 1)"
                : "0";

            string sql = $@"
                SELECT
                    cd.id AS TTCuonDay_ID,
                    cd.TTCuonDay_CD_ID,
                    cd.SoCuon,
                    cd.ChieuDai_1cuon,
                    cd.SoDau,
                    cd.SoCuoi,
                    cd.GhiChu,
                    cd.TTLo_ID,
                    lo.KichThuoc AS KichThuocLo,
                    CASE
                        WHEN cd.TTLo_ID IS NULL THEN 1
                        WHEN lo.id IS NOT NULL THEN 1
                        ELSE 0
                    END AS TTLoHopLe,
                    CASE WHEN ({lichSuCatExpr}) OR ({xuatKhoExpr}) THEN 1 ELSE 0 END AS CoLichSuDownstream
                FROM TTCuonDay cd
                LEFT JOIN TTLo lo ON lo.id = cd.TTLo_ID
                WHERE cd.ThongTinNhapKho_ID = @ThongTinNhapKho_ID
                ORDER BY cd.id;";

            using var cmd = new SQLiteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@ThongTinNhapKho_ID", idNhapKho);
            using SQLiteDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                result.Add(new DG_TonKhoBTP_v02.Models.ThongTinCuonDay
                {
                    TTCuonDay_ID = Convert.ToInt64(reader["TTCuonDay_ID"]),
                    TTCuonDay_CD_ID = reader["TTCuonDay_CD_ID"] == DBNull.Value
                        ? (long?)null
                        : Convert.ToInt64(reader["TTCuonDay_CD_ID"]),
                    CoLichSuDownstream = Convert.ToInt32(reader["CoLichSuDownstream"]) == 1,
                    TTLo_ID = reader["TTLo_ID"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["TTLo_ID"]),
                    KichThuocLo = reader["KichThuocLo"] == DBNull.Value
                        ? string.Empty
                        : (reader["KichThuocLo"].ToString() ?? string.Empty).Trim(),
                    TTLoHopLe = Convert.ToInt32(reader["TTLoHopLe"]) == 1,
                    SoCuon = reader["SoCuon"] == DBNull.Value ? 0 : Convert.ToInt32(reader["SoCuon"]),
                    TongChieuDai = reader["ChieuDai_1cuon"] == DBNull.Value ? 0 : Convert.ToInt32(reader["ChieuDai_1cuon"]),
                    SoDau = reader["SoDau"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["SoDau"]),
                    soCuoi = reader["SoCuoi"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["SoCuoi"]),
                    Ghichu = reader["GhiChu"] == DBNull.Value ? string.Empty : reader["GhiChu"].ToString()
                });
            }

            return result;
        }

        // ════════════════════════════════════════════════════════════════════════
        // HELPERS
        // ════════════════════════════════════════════════════════════════════════

        private static string ParseStr(DataGridViewRow row, string col)
        {
            var v = row.Cells[col].Value?.ToString();
            return string.IsNullOrWhiteSpace(v) ? null : v.Trim();
        }

        private static int? ParseInt(DataGridViewRow row, string col) =>
            int.TryParse(row.Cells[col].Value?.ToString(), out int v) ? v : (int?)null;

        private static long? ParseLong(DataGridViewRow row, string col) =>
            long.TryParse(row.Cells[col].Value?.ToString(), out long v) ? v : (long?)null;

        private static double? ParseDbl(DataGridViewRow row, string col) =>
            double.TryParse(
                row.Cells[col].Value?.ToString(),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.CurrentCulture,
                out double v) ? v : (double?)null;

        // ════════════════════════════════════════════════════════════════════════
        // LẤY THÔNG TIN TTBoSung VÀ TTLo ĐỂ ĐIỀN VÀO GRID
        // ════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Trả về thông tin từ TTBoSung (tenChiTiet, tieuChuan, heSoT)
        /// và TTLo (klKhoiLuong, klKhoiLuongCaNanPhu) theo TTThanhPham_ID và chieuCaoLo.
        /// </summary>
        public static ThongTinBoSungVaLo LayThongTinBoSungVaLo(long ttThanhPhamId, double chieuCaoLo)
        {
            const string sql = @"
                SELECT
                    bs.TenChiTiet               AS tenChiTiet,
                    bs.TieuChuan                AS tieuChuan,
                    CAST(IFNULL(bs.T, 0) AS REAL) AS heSoT,
                    lo.KhoiLuong                AS klKhoiLuong,
                    lo.KhoiLuongCaNanPhu        AS klKhoiLuongCaNanPhu
                FROM TTThanhPham tp
                LEFT JOIN TTBoSung bs ON bs.DanhSachMaSP_ID = tp.DanhSachSP_ID
                LEFT JOIN TTLo lo     ON CAST(lo.KichThuoc AS TEXT) = CAST(@chieuCaoLo AS TEXT)
                WHERE tp.id = @ttThanhPhamId
                LIMIT 1;";

            using var conn = DB_Base.OpenConnection();
            using var cmd = new SQLiteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@ttThanhPhamId", ttThanhPhamId);
            cmd.Parameters.AddWithValue("@chieuCaoLo", chieuCaoLo);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return new ThongTinBoSungVaLo();

            return new ThongTinBoSungVaLo
            {
                TenChiTiet = reader["tenChiTiet"] == DBNull.Value ? string.Empty : reader["tenChiTiet"].ToString(),
                TieuChuan = reader["tieuChuan"] == DBNull.Value ? string.Empty : reader["tieuChuan"].ToString(),
                HeSoT = reader["heSoT"] == DBNull.Value ? 0 : Convert.ToDouble(reader["heSoT"]),
                KlKhoiLuong = reader["klKhoiLuong"] == DBNull.Value ? (double?)null : Convert.ToDouble(reader["klKhoiLuong"]),
                KlKhoiLuongCaNanPhu = reader["klKhoiLuongCaNanPhu"] == DBNull.Value ? (double?)null : Convert.ToDouble(reader["klKhoiLuongCaNanPhu"]),
            };
        }
    }

    /// <summary>DTO chứa kết quả từ TTBoSung và TTLo.</summary>
    public class ThongTinBoSungVaLo
    {
        public string TenChiTiet { get; set; } = string.Empty;
        public string TieuChuan { get; set; } = string.Empty;
        public double HeSoT { get; set; }
        public double? KlKhoiLuong { get; set; }
        public double? KlKhoiLuongCaNanPhu { get; set; }
    }
}