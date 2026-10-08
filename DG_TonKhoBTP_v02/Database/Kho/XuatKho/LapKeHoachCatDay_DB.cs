using DG_TonKhoBTP_v02.Models.Kho.XuatKho;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DG_TonKhoBTP_v02.Database.Kho.XuatKho
{
    /// <summary>
    /// Database layer riêng cho B2 - lập kế hoạch cắt dây.
    /// Mọi connection đều mở qua DB_Base.OpenConnection() để dùng chung cơ chế kết nối/PRAGMA của project.
    /// </summary>
    internal static class LapKeHoachCatDay_DB
    {
        private sealed class FullInventorySnapshot
        {
            public long DanhSachMaSP_ID { get; set; }
            public string TenSP { get; set; } = string.Empty;
            public int TonThucTe { get; set; }
            public int DatTruoc { get; set; }
            public int ChieuDaiChuan { get; set; }
        }

        private sealed class PartialInventorySnapshot
        {
            public long TonCuonLe_ID { get; set; }
            public long DanhSachMaSP_ID { get; set; }
            public long TTThanhPham_ID { get; set; }
            public long TTCuonDay_ID { get; set; }
            public string TenSP { get; set; } = string.Empty;
            public string MaCuon { get; set; } = string.Empty;
            public int SoDau { get; set; }
            public int SoCuoi { get; set; }
            public int TonThucTe { get; set; }
            public int DatTruoc { get; set; }
            public int? ChieuDaiChuan { get; set; }
            public string TrangThai { get; set; } = string.Empty;
        }

        private sealed class InventoryBatchSnapshot
        {
            public Dictionary<long, FullInventorySnapshot> FullByProduct { get; } =
                new Dictionary<long, FullInventorySnapshot>();

            public Dictionary<long, PartialInventorySnapshot> PartialById { get; } =
                new Dictionary<long, PartialInventorySnapshot>();
        }

        public static Task<DataTable> TimGoiYAsync(
            LapKeHoachCatDay_SearchType searchType,
            string keyword,
            CancellationToken ct)
        {
            keyword = (keyword ?? string.Empty).Trim();

            return Task.Run(() =>
            {
                ct.ThrowIfCancellationRequested();
                var dt = new DataTable();
                dt.Columns.Add("GiaTri", typeof(string));

                using var conn = DB_Base.OpenConnection();
                using var cmd = conn.CreateCommand();

                if (searchType == LapKeHoachCatDay_SearchType.Lot)
                {
                    // Gom tồn theo MaBin trước rồi mới lọc suggestion, tránh chạy nhiều
                    // correlated subquery khi người dùng đang gõ.
                    cmd.CommandText = @"
                        WITH
                        Nhap AS (
                            SELECT tp.id AS TTThanhPham_ID,
                                   TRIM(tp.MaBin) AS MaBin,
                                   COALESCE(SUM(
                                       CASE
                                           WHEN cd.SoDau IS NULL
                                            AND cd.SoCuoi IS NULL
                                            AND COALESCE(cd.SoCuon, 0) > 0
                                               THEN cd.SoCuon
                                           ELSE 0
                                       END
                                   ), 0) AS TongNhap
                            FROM TTThanhPham tp
                            LEFT JOIN TTNhapKhoTP nk ON nk.TTThanhPham_ID = tp.id
                            LEFT JOIN TTCuonDay cd ON cd.ThongTinNhapKho_ID = nk.id
                            WHERE tp.MaBin IS NOT NULL
                              AND TRIM(tp.MaBin) <> ''
                              AND tp.MaBin LIKE @kw
                            GROUP BY tp.id, TRIM(tp.MaBin)
                        ),
                        DaLay AS (
                            SELECT nk.TTThanhPham_ID,
                                   COALESCE(SUM(ls.SoLuong), 0) AS SoLuong
                            FROM LichSuLayCuon ls
                            JOIN TTCuonDay cd ON cd.id = ls.TTCuonDay_ID
                            JOIN TTNhapKhoTP nk ON nk.id = cd.ThongTinNhapKho_ID
                            GROUP BY nk.TTThanhPham_ID
                        ),
                        DaChuyenLe AS (
                            SELECT tcl.TTThanhPham_ID,
                                   COUNT(DISTINCT lc.TonCuonLe_ID) AS SoLuong
                            FROM LichSuCat lc
                            JOIN TonCuonLe tcl ON tcl.id = lc.TonCuonLe_ID
                            WHERE lc.LoaiNguon = 'CUON_CHAN'
                            GROUP BY tcl.TTThanhPham_ID
                        )
                        SELECT GiaTri
                        FROM (
                            SELECT n.MaBin AS GiaTri
                            FROM Nhap n
                            LEFT JOIN DaLay dl ON dl.TTThanhPham_ID = n.TTThanhPham_ID
                            LEFT JOIN DaChuyenLe dc ON dc.TTThanhPham_ID = n.TTThanhPham_ID
                            WHERE n.TongNhap
                                  - COALESCE(dl.SoLuong, 0)
                                  - COALESCE(dc.SoLuong, 0) > 0

                            UNION

                            SELECT TRIM(tcl.MaCuon) AS GiaTri
                            FROM TonCuonLe tcl
                            WHERE tcl.MaCuon IS NOT NULL
                              AND TRIM(tcl.MaCuon) <> ''
                              AND tcl.MaCuon LIKE @kw
                              AND COALESCE(NULLIF(TRIM(tcl.TrangThai), ''), 'ACTIVE') = 'ACTIVE' COLLATE NOCASE
                              AND ABS(COALESCE(tcl.SoCuoi, 0) - COALESCE(tcl.SoDau, 0)) > 0
                        ) x
                        WHERE GiaTri IS NOT NULL AND TRIM(GiaTri) <> ''
                        ORDER BY GiaTri COLLATE NOCASE
                        LIMIT 50;";
                }
                else if (searchType == LapKeHoachCatDay_SearchType.TenSanPham)
                {
                    cmd.CommandText = @"
                        WITH
                        Nhap AS (
                            SELECT tp.DanhSachSP_ID AS ProductId,
                                   COALESCE(SUM(
                                       CASE
                                           WHEN cd.SoDau IS NULL
                                            AND cd.SoCuoi IS NULL
                                            AND COALESCE(cd.SoCuon, 0) > 0
                                               THEN cd.SoCuon
                                           ELSE 0
                                       END
                                   ), 0) AS TongNhap
                            FROM TTThanhPham tp
                            JOIN TTNhapKhoTP nk ON nk.TTThanhPham_ID = tp.id
                            JOIN TTCuonDay cd ON cd.ThongTinNhapKho_ID = nk.id
                            GROUP BY tp.DanhSachSP_ID
                        ),
                        DaLay AS (
                            SELECT tp.DanhSachSP_ID AS ProductId,
                                   COALESCE(SUM(ls.SoLuong), 0) AS SoLuong
                            FROM LichSuLayCuon ls
                            JOIN TTCuonDay cd ON cd.id = ls.TTCuonDay_ID
                            JOIN TTNhapKhoTP nk ON nk.id = cd.ThongTinNhapKho_ID
                            JOIN TTThanhPham tp ON tp.id = nk.TTThanhPham_ID
                            GROUP BY tp.DanhSachSP_ID
                        ),
                        DaChuyenLe AS (
                            SELECT tp.DanhSachSP_ID AS ProductId,
                                   COUNT(DISTINCT lc.TonCuonLe_ID) AS SoLuong
                            FROM LichSuCat lc
                            JOIN TonCuonLe tcl ON tcl.id = lc.TonCuonLe_ID
                            JOIN TTThanhPham tp ON tp.id = tcl.TTThanhPham_ID
                            WHERE lc.LoaiNguon = 'CUON_CHAN'
                            GROUP BY tp.DanhSachSP_ID
                        ),
                        CoCuonLe AS (
                            SELECT DISTINCT tp.DanhSachSP_ID AS ProductId
                            FROM TonCuonLe tcl
                            JOIN TTThanhPham tp ON tp.id = tcl.TTThanhPham_ID
                            WHERE COALESCE(NULLIF(TRIM(tcl.TrangThai), ''), 'ACTIVE') = 'ACTIVE' COLLATE NOCASE
                              AND ABS(COALESCE(tcl.SoCuoi, 0) - COALESCE(tcl.SoDau, 0)) > 0
                        )
                        SELECT DISTINCT TRIM(sp.Ten) AS GiaTri
                        FROM DanhSachMaSP sp
                        LEFT JOIN Nhap n ON n.ProductId = sp.id
                        LEFT JOIN DaLay dl ON dl.ProductId = sp.id
                        LEFT JOIN DaChuyenLe dc ON dc.ProductId = sp.id
                        LEFT JOIN CoCuonLe cl ON cl.ProductId = sp.id
                        WHERE sp.Ten IS NOT NULL
                          AND TRIM(sp.Ten) <> ''
                          AND sp.Ten LIKE @kw
                          AND (
                              COALESCE(n.TongNhap, 0)
                              - COALESCE(dl.SoLuong, 0)
                              - COALESCE(dc.SoLuong, 0) > 0
                              OR cl.ProductId IS NOT NULL
                          )
                        ORDER BY GiaTri COLLATE NOCASE
                        LIMIT 50;";
                }
                else
                {
                    return dt;
                }

                cmd.Parameters.AddWithValue("@kw", "%" + keyword + "%");
                ct.ThrowIfCancellationRequested();

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    ct.ThrowIfCancellationRequested();
                    dt.Rows.Add(Convert.ToString(reader["GiaTri"], CultureInfo.InvariantCulture));
                }

                return dt;
            }, ct);
        }

        public static List<LapKeHoachCatDay_GridRow> TimKiem(LapKeHoachCatDay_SearchCriteria criteria)
        {
            if (criteria == null)
                return new List<LapKeHoachCatDay_GridRow>();

            using var conn = DB_Base.OpenConnection();
            EnsurePlanningSchema(conn);

            switch (criteria.SearchType)
            {
                case LapKeHoachCatDay_SearchType.Lot:
                    return TimTheoLot(conn, criteria.SearchValue, criteria.KeHoach_IDDangSua);

                case LapKeHoachCatDay_SearchType.TenSanPham:
                    return TimTheoTenSanPham(conn, criteria.SearchValue, criteria.KeHoach_IDDangSua);

                case LapKeHoachCatDay_SearchType.ChieuDai:
                    return TimTheoChieuDai(conn, criteria.ChieuDaiToiThieu.GetValueOrDefault(), criteria.KeHoach_IDDangSua);

                case LapKeHoachCatDay_SearchType.TatCa:
                    return LayTatCaTon(conn, criteria.KeHoach_IDDangSua);

                case LapKeHoachCatDay_SearchType.KhachHang:
                    return TimTheoKhachHang(conn, criteria.SearchValue, criteria.KeHoach_IDDangSua);

                default:
                    return new List<LapKeHoachCatDay_GridRow>();
            }
        }

        public static void LamMoiTonChoCacDong(
            IList<LapKeHoachCatDay_GridRow> rows,
            long? excludePlanId)
        {
            if (rows == null || rows.Count == 0)
                return;

            using var conn = DB_Base.OpenConnection();
            EnsurePlanningSchema(conn);

            InventoryBatchSnapshot batch = LayKhoBatch(conn, excludePlanId);
            Dictionary<long, FullInventorySnapshot> fullTheoNguon = LayTonCuonChanTheoTTThanhPhamBatch(
                conn,
                rows.Where(x => x != null &&
                                x.LoaiDong == LapKeHoachCatDay_LoaiDong.CuonChan &&
                                x.TTThanhPham_IDNguon.HasValue)
                    .Select(x => x.TTThanhPham_IDNguon.Value),
                batch.FullByProduct);

            foreach (LapKeHoachCatDay_GridRow row in rows)
            {
                if (row == null)
                    continue;

                if (row.LoaiDong == LapKeHoachCatDay_LoaiDong.CuonChan)
                {
                    FullInventorySnapshot current = null;
                    if (row.TTThanhPham_IDNguon.HasValue)
                        fullTheoNguon.TryGetValue(row.TTThanhPham_IDNguon.Value, out current);

                    if (current == null)
                        batch.FullByProduct.TryGetValue(row.DanhSachMaSP_ID, out current);

                    if (current == null)
                    {
                        row.TonThucTe = 0;
                        row.DatTruoc = 0;
                        row.TrangThai = LapKeHoachCatDay_TrangThai.HetTon;
                        row.CanEdit = false;
                        row.CanDelete = false;
                        continue;
                    }

                    row.TonThucTe = current.TonThucTe;
                    row.DatTruoc = current.DatTruoc;
                    if (current.ChieuDaiChuan > 0)
                    {
                        row.ChieuDai1Cuon = current.ChieuDaiChuan;
                        row.ChieuDaiChuanKeHoach = current.ChieuDaiChuan;
                    }

                    row.TrangThai = current.TonThucTe <= 0
                        ? LapKeHoachCatDay_TrangThai.HetTon
                        : LapKeHoachCatDay_TrangThai.ChuaLuu;
                    row.CanEdit =
                        current.TonThucTe - current.DatTruoc > 0 &&
                        current.ChieuDaiChuan > 0;
                    row.CanDelete = false;
                    continue;
                }

                PartialInventorySnapshot partial = null;
                if (row.TonCuonLe_ID.HasValue)
                    batch.PartialById.TryGetValue(row.TonCuonLe_ID.Value, out partial);

                if (partial == null)
                {
                    row.TonThucTe = 0;
                    row.DatTruoc = 0;
                    row.TrangThai = LapKeHoachCatDay_TrangThai.HetTon;
                    row.CanEdit = false;
                    row.CanDelete = false;
                    continue;
                }

                bool active = LaCuonLeDangHoatDong(partial.TrangThai);
                row.TTThanhPham_IDNguon = partial.TTThanhPham_ID;
                row.TTCuonDay_IDNguon = partial.TTCuonDay_ID;
                row.TenSP = partial.TenSP;
                row.MaNguon = partial.MaCuon;
                row.TonThucTe = partial.TonThucTe;
                row.DatTruoc = partial.DatTruoc;
                row.ChieuDai1Cuon = partial.TonThucTe;
                row.SoDau = partial.SoDau;
                row.SoCuoi = partial.SoCuoi;
                row.TrangThai = !active || partial.TonThucTe <= 0
                    ? LapKeHoachCatDay_TrangThai.HetTon
                    : LapKeHoachCatDay_TrangThai.ChuaLuu;
                row.CanEdit = active && partial.TonThucTe - partial.DatTruoc > 0;
                row.CanDelete = false;
            }
        }

        public static LapKeHoachCatDay_KeHoachContext LayKeHoachTheoMa(string maKeHoach)
        {
            var result = new LapKeHoachCatDay_KeHoachContext();
            string ma = (maKeHoach ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(ma))
                return result;

            using var conn = DB_Base.OpenConnection();
            EnsurePlanningSchema(conn);

            const string sqlHeader = @"
                SELECT id, MaKeHoach, NgayKeHoach, TrangThai,
                       COALESCE(NguoiNhan, '') AS NguoiNhan,
                       COALESCE(GhiChu, '') AS GhiChu,
                       COALESCE(NguoiTao, '') AS NguoiTao
                FROM KeHoach
                WHERE TRIM(MaKeHoach) = TRIM(@ma) COLLATE NOCASE
                LIMIT 1;";

            using (var cmd = new SQLiteCommand(sqlHeader, conn))
            {
                cmd.Parameters.AddWithValue("@ma", ma);
                using var reader = cmd.ExecuteReader();
                if (!reader.Read())
                    return result;

                result.TonTai = true;
                result.Header = new LapKeHoachCatDay_KeHoachHeader
                {
                    Id = Convert.ToInt64(reader["id"], CultureInfo.InvariantCulture),
                    MaKeHoach = Convert.ToString(reader["MaKeHoach"], CultureInfo.InvariantCulture) ?? string.Empty,
                    NgayKeHoach = Convert.ToString(reader["NgayKeHoach"], CultureInfo.InvariantCulture) ?? string.Empty,
                    TrangThai = Convert.ToString(reader["TrangThai"], CultureInfo.InvariantCulture) ?? "ACTIVE",
                    NguoiNhan = Convert.ToString(reader["NguoiNhan"], CultureInfo.InvariantCulture) ?? string.Empty,
                    GhiChu = Convert.ToString(reader["GhiChu"], CultureInfo.InvariantCulture) ?? string.Empty,
                    NguoiTao = Convert.ToString(reader["NguoiTao"], CultureInfo.InvariantCulture) ?? string.Empty
                };
            }

            long planId = result.Header.Id;

            const string sqlHang = @"
                SELECT kh.id,
                       kh.DanhSachMaSP_ID,
                       kh.SoLuongCuonCanLay,
                       kh.ChieuDai1Cuon_KeHoach,
                       sp.Ten
                FROM KeHoachHang kh
                JOIN DanhSachMaSP sp ON sp.id = kh.DanhSachMaSP_ID
                WHERE kh.KeHoach_ID = @planId
                ORDER BY sp.Ten COLLATE NOCASE, kh.id;";

            var hangRows = new List<(long Id, long ProductId, int SoLuong, int? Standard, string Ten)>();
            using (var cmdHang = new SQLiteCommand(sqlHang, conn))
            {
                cmdHang.Parameters.AddWithValue("@planId", planId);
                using var hangReader = cmdHang.ExecuteReader();
                while (hangReader.Read())
                {
                    hangRows.Add((
                        Convert.ToInt64(hangReader["id"], CultureInfo.InvariantCulture),
                        Convert.ToInt64(hangReader["DanhSachMaSP_ID"], CultureInfo.InvariantCulture),
                        Convert.ToInt32(hangReader["SoLuongCuonCanLay"], CultureInfo.InvariantCulture),
                        hangReader["ChieuDai1Cuon_KeHoach"] == DBNull.Value
                            ? (int?)null
                            : Convert.ToInt32(hangReader["ChieuDai1Cuon_KeHoach"], CultureInfo.InvariantCulture),
                        Convert.ToString(hangReader["Ten"], CultureInfo.InvariantCulture) ?? string.Empty));
                }
            }

            Dictionary<long, List<LapKeHoachCatDay_NhomCat>> groupsByHang =
                LayNhomCatTheoKeHoach(conn, planId);
            Dictionary<long, int> soLuongDaLayByHang =
                LaySoLuongDaLayTheoKeHoach(conn, planId);

            // Một batch cho toàn bộ tồn/đặt trước khi load kế hoạch.
            // excludePlanId làm cột Đặt trước chỉ phản ánh các kế hoạch khác.
            InventoryBatchSnapshot inventory = LayKhoBatch(conn, planId);

            foreach (var kh in hangRows)
            {
                groupsByHang.TryGetValue(kh.Id, out List<LapKeHoachCatDay_NhomCat> groups);
                groups ??= new List<LapKeHoachCatDay_NhomCat>();

                List<LapKeHoachCatDay_NhomCat> fullGroups = groups
                    .Where(x => !x.TonCuonLe_ID.HasValue)
                    .OrderBy(x => x.Id)
                    .ToList();

                soLuongDaLayByHang.TryGetValue(kh.Id, out int soLuongDaLay);

                if (kh.SoLuong > 0 || fullGroups.Count > 0)
                {
                    if (!inventory.FullByProduct.TryGetValue(kh.ProductId, out FullInventorySnapshot ton))
                    {
                        ton = new FullInventorySnapshot
                        {
                            DanhSachMaSP_ID = kh.ProductId,
                            TenSP = kh.Ten,
                            TonThucTe = 0,
                            DatTruoc = 0,
                            ChieuDaiChuan = kh.Standard.GetValueOrDefault()
                        };
                    }

                    int standard = kh.Standard.GetValueOrDefault() > 0
                        ? kh.Standard.Value
                        : ton.ChieuDaiChuan;

                    var row = new LapKeHoachCatDay_GridRow
                    {
                        RowKey = TaoRowKeyCuonChan(kh.ProductId),
                        LoaiDong = LapKeHoachCatDay_LoaiDong.CuonChan,
                        DanhSachMaSP_ID = kh.ProductId,
                        KeHoachHang_ID = kh.Id,
                        TenSP = kh.Ten,
                        TonThucTe = ton.TonThucTe,
                        DatTruoc = ton.DatTruoc,
                        ChieuDai1Cuon = standard,
                        ChieuDaiChuanKeHoach = standard > 0 ? (int?)standard : null,
                        SoLuongCanLay = kh.SoLuong,
                        SoLuongDaLay = soLuongDaLay,
                        DaTonTaiTrongDB = true,
                        NhomCatDaLuu = fullGroups.Select(x => x.Clone()).ToList()
                    };

                    if (fullGroups.Count == 1)
                    {
                        row.ChuoiChieuDaiCat = string.Join(";", fullGroups[0].ChiTiet
                            .Where(x => !x.DaThucHien)
                            .Select(x => x.ChieuDai.ToString(CultureInfo.InvariantCulture)));
                    }
                    else if (fullGroups.Count > 1)
                    {
                        row.NhomCatPopup = fullGroups.Select(x => x.Clone()).ToList();
                    }

                    GanTrangThaiCuonChan(row);
                    result.Rows.Add(row);
                }

                foreach (IGrouping<long, LapKeHoachCatDay_NhomCat> byPartial in groups
                    .Where(x => x.TonCuonLe_ID.HasValue)
                    .GroupBy(x => x.TonCuonLe_ID.Value)
                    .OrderBy(x => x.Key))
                {
                    if (!inventory.PartialById.TryGetValue(byPartial.Key, out PartialInventorySnapshot ton))
                        continue;

                    var boundGroups = byPartial
                        .OrderBy(x => x.Id)
                        .Select(x => x.Clone())
                        .ToList();
                    var unexecuted = boundGroups
                        .SelectMany(x => x.ChiTiet)
                        .Where(x => !x.DaThucHien)
                        .ToList();

                    var row = new LapKeHoachCatDay_GridRow
                    {
                        RowKey = TaoRowKeyCuonLe(byPartial.Key),
                        LoaiDong = LapKeHoachCatDay_LoaiDong.CuonLe,
                        DanhSachMaSP_ID = kh.ProductId,
                        KeHoachHang_ID = kh.Id,
                        TonCuonLe_ID = byPartial.Key,
                        TTThanhPham_IDNguon = ton.TTThanhPham_ID,
                        TTCuonDay_IDNguon = ton.TTCuonDay_ID,
                        TenSP = kh.Ten,
                        MaNguon = ton.MaCuon,
                        TonThucTe = ton.TonThucTe,
                        DatTruoc = ton.DatTruoc,
                        ChieuDai1Cuon = ton.TonThucTe,
                        ChieuDaiChuanKeHoach = kh.Standard,
                        SoDau = ton.SoDau,
                        SoCuoi = ton.SoCuoi,
                        DaTonTaiTrongDB = true,
                        NhomCatDaLuu = boundGroups,
                        ChuoiChieuDaiCat = string.Join(";", unexecuted
                            .Select(x => x.ChieuDai.ToString(CultureInfo.InvariantCulture)))
                    };

                    GanTrangThaiCuonLe(row);
                    result.Rows.Add(row);
                }
            }

            return result;
        }

        public static LapKeHoachCatDay_SaveResult LuuDong(LapKeHoachCatDay_SaveRequest request)
        {
            var result = new LapKeHoachCatDay_SaveResult();
            if (request == null || request.Row == null)
            {
                result.Loi = "Kiểm tra lại dữ liệu.";
                return result;
            }

            string maKeHoach = (request.MaKeHoach ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(maKeHoach))
            {
                result.Loi = "Vui lòng nhập mã kế hoạch trước khi lưu.";
                return result;
            }

            using var conn = DB_Base.OpenConnection();
            EnsurePlanningSchema(conn);
            using var tx = conn.BeginTransaction();

            try
            {
                long? planIdInDb = LayKeHoachIdTheoMa(conn, tx, maKeHoach);

                if (!request.KeHoach_IDDuKien.HasValue && planIdInDb.HasValue)
                {
                    result.DuLieuDaCu = true;
                    result.Loi = "Dữ liệu đã cũ, cần reload để cập nhật";
                    tx.Rollback();
                    return result;
                }

                if (request.KeHoach_IDDuKien.HasValue &&
                    (!planIdInDb.HasValue || planIdInDb.Value != request.KeHoach_IDDuKien.Value))
                {
                    result.DuLieuDaCu = true;
                    result.Loi = "Dữ liệu đã cũ, cần reload để cập nhật";
                    tx.Rollback();
                    return result;
                }

                bool taoMoi = !planIdInDb.HasValue;
                long planId;

                if (taoMoi)
                {
                    const string insertPlan = @"
                        INSERT INTO KeHoach
                            (MaKeHoach, NgayKeHoach, TrangThai, NguoiNhan, GhiChu, NguoiTao, DateInsert)
                        VALUES
                            (@ma, @ngay, 'ACTIVE', @nguoiNhan, @ghiChu, @nguoiTao, CURRENT_TIMESTAMP);";

                    using var cmd = new SQLiteCommand(insertPlan, conn, tx);
                    cmd.Parameters.AddWithValue("@ma", maKeHoach);
                    cmd.Parameters.AddWithValue("@ngay", DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                    cmd.Parameters.AddWithValue("@nguoiNhan", (request.NguoiNhan ?? string.Empty).Trim());
                    cmd.Parameters.AddWithValue("@ghiChu", request.GhiChu ?? string.Empty);
                    cmd.Parameters.AddWithValue("@nguoiTao", request.NguoiTao ?? string.Empty);
                    cmd.ExecuteNonQuery();
                    planId = conn.LastInsertRowId;
                }
                else
                {
                    planId = planIdInDb.Value;
                    const string updatePlan = @"
                        UPDATE KeHoach
                        SET NguoiNhan = @nguoiNhan,
                            GhiChu = @ghiChu
                        WHERE id = @id;";

                    using var cmd = new SQLiteCommand(updatePlan, conn, tx);
                    cmd.Parameters.AddWithValue("@nguoiNhan", (request.NguoiNhan ?? string.Empty).Trim());
                    cmd.Parameters.AddWithValue("@ghiChu", request.GhiChu ?? string.Empty);
                    cmd.Parameters.AddWithValue("@id", planId);
                    cmd.ExecuteNonQuery();
                }

                long productId = request.Row.DanhSachMaSP_ID;
                long? khId = LayKeHoachHangId(conn, tx, planId, productId);
                int? standardSnapshot = khId.HasValue
                    ? LayChieuDaiKeHoachHang(conn, tx, khId.Value)
                    : (int?)null;

                // ChieuDai1Cuon_KeHoach chỉ là snapshot của cuộn chẵn.
                // Kế hoạch chỉ dùng cuộn lẻ được phép để NULL và không cần truy chiều dài chuẩn.
                if (request.Row.LoaiDong == LapKeHoachCatDay_LoaiDong.CuonChan &&
                    (!standardSnapshot.HasValue || standardSnapshot.Value <= 0))
                {
                    if (!TryLayChieuDaiChuanSanPham(conn, productId, tx, out int resolvedStandard))
                    {
                        result.Loi = "Kiểm tra lại dữ liệu.";
                        tx.Rollback();
                        return result;
                    }
                    standardSnapshot = resolvedStandard;
                }

                if (!khId.HasValue)
                {
                    const string insertHang = @"
                        INSERT INTO KeHoachHang
                            (KeHoach_ID, DanhSachMaSP_ID, SoLuongCuonCanLay, ChieuDai1Cuon_KeHoach, GhiChu, DateInsert)
                        VALUES
                            (@planId, @productId, 0, @standard, '', CURRENT_TIMESTAMP);";

                    using var cmd = new SQLiteCommand(insertHang, conn, tx);
                    cmd.Parameters.AddWithValue("@planId", planId);
                    cmd.Parameters.AddWithValue("@productId", productId);
                    cmd.Parameters.AddWithValue("@standard", (object)standardSnapshot ?? DBNull.Value);
                    cmd.ExecuteNonQuery();
                    khId = conn.LastInsertRowId;
                }
                else if (request.Row.LoaiDong == LapKeHoachCatDay_LoaiDong.CuonChan &&
                         standardSnapshot.HasValue &&
                         !LayChieuDaiKeHoachHang(conn, tx, khId.Value).HasValue)
                {
                    using var cmd = new SQLiteCommand(
                        "UPDATE KeHoachHang SET ChieuDai1Cuon_KeHoach = @standard WHERE id = @id;", conn, tx);
                    cmd.Parameters.AddWithValue("@standard", standardSnapshot.Value);
                    cmd.Parameters.AddWithValue("@id", khId.Value);
                    cmd.ExecuteNonQuery();
                }

                string validationError;
                bool stale;
                if (request.Row.LoaiDong == LapKeHoachCatDay_LoaiDong.CuonChan)
                {
                    if (!standardSnapshot.HasValue || standardSnapshot.Value <= 0)
                    {
                        result.Loi = "Kiểm tra lại dữ liệu.";
                        tx.Rollback();
                        return result;
                    }

                    if (!LuuDongCuonChan(conn, tx, planId, khId.Value, standardSnapshot.Value, request.Row, out validationError, out stale))
                    {
                        result.DuLieuDaCu = stale;
                        result.Loi = validationError;
                        tx.Rollback();
                        return result;
                    }
                }
                else
                {
                    if (!LuuDongCuonLe(conn, tx, planId, khId.Value, request.Row, out validationError, out stale))
                    {
                        result.DuLieuDaCu = stale;
                        result.Loi = validationError;
                        tx.Rollback();
                        return result;
                    }
                }

                tx.Commit();
                result.ThanhCong = true;
                result.TaoMoiKeHoach = taoMoi;
                result.KeHoach_ID = planId;
                return result;
            }
            catch
            {
                try { tx.Rollback(); } catch { }
                throw;
            }
        }

        public static LapKeHoachCatDay_DeletePreview KiemTraXoaDong(LapKeHoachCatDay_DeleteRequest request)
        {
            var result = new LapKeHoachCatDay_DeletePreview();
            if (request == null || request.Row == null || request.KeHoach_ID <= 0)
            {
                result.Loi = "Kiểm tra lại dữ liệu.";
                return result;
            }

            using var conn = DB_Base.OpenConnection();
            EnsurePlanningSchema(conn);

            long? khId = LayKeHoachHangId(conn, null, request.KeHoach_ID, request.Row.DanhSachMaSP_ID);
            if (!khId.HasValue)
            {
                result.Loi = "Dữ liệu đã cũ, cần reload để cập nhật";
                return result;
            }

            if (request.Row.LoaiDong == LapKeHoachCatDay_LoaiDong.CuonChan)
            {
                int planned = LaySoLuongKeHoachHang(conn, null, khId.Value);
                int executed = LaySoLuongDaLay(conn, khId.Value);
                int unstartedGroups = DemNhomChuaBatDau(conn, null, khId.Value);
                result.CoTheXoa = planned > executed || unstartedGroups > 0;
                result.CoThucHienMotPhan = executed > 0;
            }
            else
            {
                if (!request.Row.TonCuonLe_ID.HasValue)
                {
                    result.Loi = "Kiểm tra lại dữ liệu.";
                    return result;
                }

                int unfinished = DemChiTietChuaThucHien(conn, null, khId.Value, request.Row.TonCuonLe_ID.Value);
                int executed = DemChiTietDaThucHien(conn, null, khId.Value, request.Row.TonCuonLe_ID.Value);
                result.CoTheXoa = unfinished > 0;
                result.CoThucHienMotPhan = executed > 0;
            }

            if (!result.CoTheXoa)
            {
                result.Loi = "Không thể xóa vì nội dung này đã có dữ liệu thực hiện.";
                return result;
            }

            result.SeXoaCaKeHoach = !KeHoachCoLichSu(conn, null, request.KeHoach_ID)
                && !ConNoiDungKhacSauKhiXoa(conn, null, request.KeHoach_ID, khId.Value, request.Row);

            return result;
        }

        public static LapKeHoachCatDay_DeleteResult XoaDong(LapKeHoachCatDay_DeleteRequest request)
        {
            var result = new LapKeHoachCatDay_DeleteResult();
            if (request == null || request.Row == null || request.KeHoach_ID <= 0)
            {
                result.Loi = "Kiểm tra lại dữ liệu.";
                return result;
            }

            using var conn = DB_Base.OpenConnection();
            EnsurePlanningSchema(conn);
            using var tx = conn.BeginTransaction();

            try
            {
                long? khId = LayKeHoachHangId(conn, tx, request.KeHoach_ID, request.Row.DanhSachMaSP_ID);
                if (!khId.HasValue)
                {
                    result.Loi = "Dữ liệu đã cũ, cần reload để cập nhật";
                    tx.Rollback();
                    return result;
                }

                if (request.Row.LoaiDong == LapKeHoachCatDay_LoaiDong.CuonChan)
                {
                    int executed = LaySoLuongDaLay(conn, khId.Value, tx);
                    const string updateQty = "UPDATE KeHoachHang SET SoLuongCuonCanLay = @qty WHERE id = @id;";
                    using (var cmd = new SQLiteCommand(updateQty, conn, tx))
                    {
                        cmd.Parameters.AddWithValue("@qty", executed);
                        cmd.Parameters.AddWithValue("@id", khId.Value);
                        cmd.ExecuteNonQuery();
                    }

                    XoaNhomCuonChanChuaBatDau(conn, tx, khId.Value);
                }
                else
                {
                    if (!request.Row.TonCuonLe_ID.HasValue)
                    {
                        result.Loi = "Kiểm tra lại dữ liệu.";
                        tx.Rollback();
                        return result;
                    }

                    XoaChiTietChuaThucHienTheoCuonLe(conn, tx, khId.Value, request.Row.TonCuonLe_ID.Value);
                    XoaNhomRong(conn, tx, khId.Value, request.Row.TonCuonLe_ID.Value);
                }

                XoaKeHoachHangNeuRong(conn, tx, khId.Value);

                if (!KeHoachCoLichSu(conn, tx, request.KeHoach_ID) && !KeHoachConNoiDung(conn, tx, request.KeHoach_ID))
                {
                    using var cmd = new SQLiteCommand("DELETE FROM KeHoach WHERE id = @id;", conn, tx);
                    cmd.Parameters.AddWithValue("@id", request.KeHoach_ID);
                    cmd.ExecuteNonQuery();
                    result.DaXoaKeHoach = true;
                }

                tx.Commit();
                result.ThanhCong = true;
                return result;
            }
            catch
            {
                try { tx.Rollback(); } catch { }
                throw;
            }
        }

        // ───────────────────────── Search ─────────────────────────

        private static List<LapKeHoachCatDay_GridRow> TimTheoKhachHang(SQLiteConnection conn, string khachHang, long? excludePlanId)
        {
            // Placeholder B2: giữ function riêng để phase sau bổ sung nghiệp vụ khách hàng
            // mà không thay đổi contract TimKiem hiện tại.
            return new List<LapKeHoachCatDay_GridRow>();
        }

        private static List<LapKeHoachCatDay_GridRow> TimTheoLot(SQLiteConnection conn, string lot, long? excludePlanId)
        {
            string value = (lot ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(value))
                return new List<LapKeHoachCatDay_GridRow>();

            const string findPartial = @"
                SELECT id
                FROM TonCuonLe
                WHERE TRIM(MaCuon) = TRIM(@value) COLLATE NOCASE
                  AND COALESCE(NULLIF(TRIM(TrangThai), ''), 'ACTIVE') = 'ACTIVE' COLLATE NOCASE
                  AND ABS(COALESCE(SoCuoi, 0) - COALESCE(SoDau, 0)) > 0
                LIMIT 1;";
            using (var cmd = new SQLiteCommand(findPartial, conn))
            {
                cmd.Parameters.AddWithValue("@value", value);
                object idObj = cmd.ExecuteScalar();
                if (idObj != null && idObj != DBNull.Value)
                {
                    var ton = LayTonCuonLe(conn, Convert.ToInt64(idObj, CultureInfo.InvariantCulture), excludePlanId);
                    if (ton == null || ton.TonThucTe <= 0 || !LaCuonLeDangHoatDong(ton.TrangThai))
                        return new List<LapKeHoachCatDay_GridRow>();
                    return new List<LapKeHoachCatDay_GridRow> { TaoDongCuonLeMoi(ton) };
                }
            }

            const string findProduct = @"
                SELECT tp.id AS TTThanhPham_ID, tp.DanhSachSP_ID, sp.Ten
                FROM TTThanhPham tp
                JOIN DanhSachMaSP sp ON sp.id = tp.DanhSachSP_ID
                WHERE TRIM(tp.MaBin) = TRIM(@value) COLLATE NOCASE
                LIMIT 1;";

            using var findCmd = new SQLiteCommand(findProduct, conn);
            findCmd.Parameters.AddWithValue("@value", value);
            using var reader = findCmd.ExecuteReader();
            if (!reader.Read())
                return new List<LapKeHoachCatDay_GridRow>();

            long productId = Convert.ToInt64(reader["DanhSachSP_ID"], CultureInfo.InvariantCulture);
            long sourceTpId = Convert.ToInt64(reader["TTThanhPham_ID"], CultureInfo.InvariantCulture);
            string ten = Convert.ToString(reader["Ten"], CultureInfo.InvariantCulture) ?? string.Empty;
            reader.Close();

            // Tìm LOT/MaBin cuộn chẵn phải phản ánh đúng tồn của chính MaBin được chọn.
            // Reservation vẫn là theo mã sản phẩm vì B2 chưa khóa một cuộn chẵn vật lý cụ thể.
            FullInventorySnapshot full = LayTonCuonChanTheoTTThanhPham(conn, sourceTpId, productId, excludePlanId);
            if (full.TonThucTe <= 0 || full.ChieuDaiChuan <= 0)
                return new List<LapKeHoachCatDay_GridRow>();

            var row = TaoDongCuonChanMoi(full);
            row.TTThanhPham_IDNguon = sourceTpId;
            row.MaNguon = value;
            row.TenSP = string.IsNullOrWhiteSpace(ten) ? full.TenSP : ten;
            return new List<LapKeHoachCatDay_GridRow> { row };
        }

        private static List<LapKeHoachCatDay_GridRow> TimTheoTenSanPham(SQLiteConnection conn, string ten, long? excludePlanId)
        {
            string value = (ten ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(value))
                return new List<LapKeHoachCatDay_GridRow>();

            const string sql = @"
                SELECT id
                FROM DanhSachMaSP
                WHERE TRIM(Ten) = TRIM(@ten) COLLATE NOCASE
                ORDER BY id
                LIMIT 1;";
            using var cmd = new SQLiteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@ten", value);
            object idObj = cmd.ExecuteScalar();
            if (idObj == null || idObj == DBNull.Value)
                return new List<LapKeHoachCatDay_GridRow>();

            return LayTonTheoSanPham(conn, Convert.ToInt64(idObj, CultureInfo.InvariantCulture), excludePlanId);
        }

        private static List<LapKeHoachCatDay_GridRow> TimTheoChieuDai(SQLiteConnection conn, int minLength, long? excludePlanId)
        {
            if (minLength <= 0)
                return new List<LapKeHoachCatDay_GridRow>();

            return LayTatCaTon(conn, excludePlanId)
                .Where(x => x.LoaiDong == LapKeHoachCatDay_LoaiDong.CuonChan
                    ? x.ChieuDai1Cuon >= minLength
                    : x.TonThucTe >= minLength)
                .ToList();
        }

        private static List<LapKeHoachCatDay_GridRow> LayTatCaTon(SQLiteConnection conn, long? excludePlanId)
        {
            InventoryBatchSnapshot batch = LayKhoBatch(conn, excludePlanId);

            var rows = new List<LapKeHoachCatDay_GridRow>();

            foreach (FullInventorySnapshot full in batch.FullByProduct.Values
                .Where(x => x.TonThucTe > 0 && x.ChieuDaiChuan > 0)
                .OrderBy(x => x.TenSP, StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(x => x.DanhSachMaSP_ID))
            {
                rows.Add(TaoDongCuonChanMoi(full));
            }

            foreach (PartialInventorySnapshot partial in batch.PartialById.Values
                .Where(x => x.TonThucTe > 0 && LaCuonLeDangHoatDong(x.TrangThai))
                .OrderBy(x => x.TenSP, StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(x => x.MaCuon, StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(x => x.TonCuonLe_ID))
            {
                rows.Add(TaoDongCuonLeMoi(partial));
            }

            return rows;
        }

        private static List<LapKeHoachCatDay_GridRow> LayTonTheoSanPham(SQLiteConnection conn, long productId, long? excludePlanId)
        {
            InventoryBatchSnapshot batch = LayKhoBatch(conn, excludePlanId, productId);
            var rows = new List<LapKeHoachCatDay_GridRow>();

            if (batch.FullByProduct.TryGetValue(productId, out FullInventorySnapshot full) &&
                full.TonThucTe > 0 &&
                full.ChieuDaiChuan > 0)
            {
                rows.Add(TaoDongCuonChanMoi(full));
            }

            foreach (PartialInventorySnapshot partial in batch.PartialById.Values
                .Where(x => x.DanhSachMaSP_ID == productId &&
                            x.TonThucTe > 0 &&
                            LaCuonLeDangHoatDong(x.TrangThai))
                .OrderBy(x => x.MaCuon, StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(x => x.TonCuonLe_ID))
            {
                rows.Add(TaoDongCuonLeMoi(partial));
            }

            return rows;
        }

        private static InventoryBatchSnapshot LayKhoBatch(
            SQLiteConnection conn,
            long? excludePlanId,
            long? onlyProductId = null,
            SQLiteTransaction tx = null)
        {
            var result = new InventoryBatchSnapshot();

            const string fullSql = @"
                WITH
                Nhap AS (
                    SELECT tp.DanhSachSP_ID AS ProductId,
                           COALESCE(SUM(
                               CASE
                                   WHEN cd.SoDau IS NULL
                                    AND cd.SoCuoi IS NULL
                                    AND COALESCE(cd.SoCuon, 0) > 0
                                       THEN cd.SoCuon
                                   ELSE 0
                               END
                           ), 0) AS TongNhap,
                           COUNT(DISTINCT
                               CASE
                                   WHEN cd.SoDau IS NULL
                                    AND cd.SoCuoi IS NULL
                                    AND cd.ChieuDai_1cuon > 0
                                       THEN cd.ChieuDai_1cuon
                                   ELSE NULL
                               END
                           ) AS SoLoaiChieuDai,
                           MIN(
                               CASE
                                   WHEN cd.SoDau IS NULL
                                    AND cd.SoCuoi IS NULL
                                    AND cd.ChieuDai_1cuon > 0
                                       THEN cd.ChieuDai_1cuon
                                   ELSE NULL
                               END
                           ) AS ChieuDaiMin
                    FROM TTThanhPham tp
                    JOIN TTNhapKhoTP nk ON nk.TTThanhPham_ID = tp.id
                    JOIN TTCuonDay cd ON cd.ThongTinNhapKho_ID = nk.id
                    WHERE (@onlyProductId IS NULL OR tp.DanhSachSP_ID = @onlyProductId)
                    GROUP BY tp.DanhSachSP_ID
                ),
                DaLay AS (
                    SELECT tp.DanhSachSP_ID AS ProductId,
                           COALESCE(SUM(ls.SoLuong), 0) AS SoLuong
                    FROM LichSuLayCuon ls
                    JOIN TTCuonDay cd ON cd.id = ls.TTCuonDay_ID
                    JOIN TTNhapKhoTP nk ON nk.id = cd.ThongTinNhapKho_ID
                    JOIN TTThanhPham tp ON tp.id = nk.TTThanhPham_ID
                    WHERE (@onlyProductId IS NULL OR tp.DanhSachSP_ID = @onlyProductId)
                    GROUP BY tp.DanhSachSP_ID
                ),
                DaChuyenLe AS (
                    SELECT tp.DanhSachSP_ID AS ProductId,
                           COUNT(DISTINCT lc.TonCuonLe_ID) AS SoLuong
                    FROM LichSuCat lc
                    JOIN TonCuonLe tcl ON tcl.id = lc.TonCuonLe_ID
                    JOIN TTThanhPham tp ON tp.id = tcl.TTThanhPham_ID
                    WHERE lc.LoaiNguon = 'CUON_CHAN'
                      AND (@onlyProductId IS NULL OR tp.DanhSachSP_ID = @onlyProductId)
                    GROUP BY tp.DanhSachSP_ID
                ),
                DaLayTheoHang AS (
                    SELECT KeHoachHang_ID,
                           COALESCE(SUM(SoLuong), 0) AS SoLuong
                    FROM LichSuLayCuon
                    GROUP BY KeHoachHang_ID
                ),
                DatNguyen AS (
                    SELECT kh.DanhSachMaSP_ID AS ProductId,
                           COALESCE(SUM(
                               CASE
                                   WHEN kh.SoLuongCuonCanLay > COALESCE(dl.SoLuong, 0)
                                       THEN kh.SoLuongCuonCanLay - COALESCE(dl.SoLuong, 0)
                                   ELSE 0
                               END
                           ), 0) AS SoLuong
                    FROM KeHoachHang kh
                    JOIN KeHoach k ON k.id = kh.KeHoach_ID
                    LEFT JOIN DaLayTheoHang dl ON dl.KeHoachHang_ID = kh.id
                    WHERE k.TrangThai = 'ACTIVE'
                      AND (@excludePlanId IS NULL OR k.id <> @excludePlanId)
                      AND (@onlyProductId IS NULL OR kh.DanhSachMaSP_ID = @onlyProductId)
                    GROUP BY kh.DanhSachMaSP_ID
                ),
                DatNhom AS (
                    SELECT kh.DanhSachMaSP_ID AS ProductId,
                           COUNT(DISTINCT n.id) AS SoLuong
                    FROM KeHoachCatNhom n
                    JOIN KeHoachHang kh ON kh.id = n.KeHoachHang_ID
                    JOIN KeHoach k ON k.id = kh.KeHoach_ID
                    WHERE k.TrangThai = 'ACTIVE'
                      AND (@excludePlanId IS NULL OR k.id <> @excludePlanId)
                      AND (@onlyProductId IS NULL OR kh.DanhSachMaSP_ID = @onlyProductId)
                      AND n.TonCuonLe_ID IS NULL
                      AND EXISTS (
                          SELECT 1
                          FROM KeHoachCatChiTiet d
                          LEFT JOIN LichSuCat lc ON lc.KeHoachCatChiTiet_ID = d.id
                          WHERE d.KeHoachCatNhom_ID = n.id
                            AND lc.id IS NULL
                      )
                    GROUP BY kh.DanhSachMaSP_ID
                )
                SELECT sp.id AS ProductId,
                       COALESCE(sp.Ten, '') AS TenSP,
                       COALESCE(n.TongNhap, 0) AS TongNhap,
                       COALESCE(dl.SoLuong, 0) AS DaLay,
                       COALESCE(dc.SoLuong, 0) AS DaChuyenLe,
                       COALESCE(dn.SoLuong, 0) + COALESCE(dnh.SoLuong, 0) AS DatTruoc,
                       CASE
                           WHEN COALESCE(n.SoLoaiChieuDai, 0) = 1
                               THEN COALESCE(n.ChieuDaiMin, 0)
                           ELSE 0
                       END AS ChieuDaiChuan
                FROM DanhSachMaSP sp
                LEFT JOIN Nhap n ON n.ProductId = sp.id
                LEFT JOIN DaLay dl ON dl.ProductId = sp.id
                LEFT JOIN DaChuyenLe dc ON dc.ProductId = sp.id
                LEFT JOIN DatNguyen dn ON dn.ProductId = sp.id
                LEFT JOIN DatNhom dnh ON dnh.ProductId = sp.id
                WHERE (@onlyProductId IS NULL OR sp.id = @onlyProductId)
                  AND COALESCE(n.TongNhap, 0) > 0;";

            using (var cmd = new SQLiteCommand(fullSql, conn, tx))
            {
                cmd.Parameters.AddWithValue("@excludePlanId", (object)excludePlanId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@onlyProductId", (object)onlyProductId ?? DBNull.Value);

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    long productId = Convert.ToInt64(reader["ProductId"], CultureInfo.InvariantCulture);
                    int imported = Convert.ToInt32(reader["TongNhap"], CultureInfo.InvariantCulture);
                    int taken = Convert.ToInt32(reader["DaLay"], CultureInfo.InvariantCulture);
                    int converted = Convert.ToInt32(reader["DaChuyenLe"], CultureInfo.InvariantCulture);

                    result.FullByProduct[productId] = new FullInventorySnapshot
                    {
                        DanhSachMaSP_ID = productId,
                        TenSP = Convert.ToString(reader["TenSP"], CultureInfo.InvariantCulture) ?? string.Empty,
                        TonThucTe = Math.Max(0, imported - taken - converted),
                        DatTruoc = Convert.ToInt32(reader["DatTruoc"], CultureInfo.InvariantCulture),
                        ChieuDaiChuan = Convert.ToInt32(reader["ChieuDaiChuan"], CultureInfo.InvariantCulture)
                    };
                }
            }

            const string partialSql = @"
                WITH DatLe AS (
                    SELECT n.TonCuonLe_ID,
                           COALESCE(SUM(d.ChieuDaiCanCat), 0) AS ChieuDai
                    FROM KeHoachCatNhom n
                    JOIN KeHoachHang kh ON kh.id = n.KeHoachHang_ID
                    JOIN KeHoach k ON k.id = kh.KeHoach_ID
                    JOIN KeHoachCatChiTiet d ON d.KeHoachCatNhom_ID = n.id
                    LEFT JOIN LichSuCat lc ON lc.KeHoachCatChiTiet_ID = d.id
                    WHERE n.TonCuonLe_ID IS NOT NULL
                      AND k.TrangThai = 'ACTIVE'
                      AND (@excludePlanId IS NULL OR k.id <> @excludePlanId)
                      AND (@onlyProductId IS NULL OR kh.DanhSachMaSP_ID = @onlyProductId)
                      AND lc.id IS NULL
                    GROUP BY n.TonCuonLe_ID
                )
                SELECT tcl.id,
                       tcl.MaCuon,
                       tcl.TTThanhPham_ID,
                       tcl.TTCuonDay_ID,
                       tcl.SoDau,
                       tcl.SoCuoi,
                       COALESCE(tcl.TrangThai, '') AS TrangThai,
                       sp.id AS DanhSachMaSP_ID,
                       COALESCE(sp.Ten, '') AS TenSP,
                       COALESCE(dl.ChieuDai, 0) AS DatTruoc
                FROM TonCuonLe tcl
                JOIN TTThanhPham tp ON tp.id = tcl.TTThanhPham_ID
                JOIN DanhSachMaSP sp ON sp.id = tp.DanhSachSP_ID
                LEFT JOIN DatLe dl ON dl.TonCuonLe_ID = tcl.id
                WHERE (@onlyProductId IS NULL OR sp.id = @onlyProductId)
                ORDER BY sp.Ten COLLATE NOCASE, tcl.MaCuon COLLATE NOCASE, tcl.id;";

            using (var cmd = new SQLiteCommand(partialSql, conn, tx))
            {
                cmd.Parameters.AddWithValue("@excludePlanId", (object)excludePlanId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@onlyProductId", (object)onlyProductId ?? DBNull.Value);

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    int soDau = Convert.ToInt32(reader["SoDau"], CultureInfo.InvariantCulture);
                    int soCuoi = Convert.ToInt32(reader["SoCuoi"], CultureInfo.InvariantCulture);
                    long id = Convert.ToInt64(reader["id"], CultureInfo.InvariantCulture);

                    result.PartialById[id] = new PartialInventorySnapshot
                    {
                        TonCuonLe_ID = id,
                        DanhSachMaSP_ID = Convert.ToInt64(reader["DanhSachMaSP_ID"], CultureInfo.InvariantCulture),
                        TTThanhPham_ID = Convert.ToInt64(reader["TTThanhPham_ID"], CultureInfo.InvariantCulture),
                        TTCuonDay_ID = Convert.ToInt64(reader["TTCuonDay_ID"], CultureInfo.InvariantCulture),
                        TenSP = Convert.ToString(reader["TenSP"], CultureInfo.InvariantCulture) ?? string.Empty,
                        MaCuon = Convert.ToString(reader["MaCuon"], CultureInfo.InvariantCulture) ?? string.Empty,
                        SoDau = soDau,
                        SoCuoi = soCuoi,
                        TonThucTe = Math.Abs(soCuoi - soDau),
                        DatTruoc = Convert.ToInt32(reader["DatTruoc"], CultureInfo.InvariantCulture),
                        ChieuDaiChuan = null,
                        TrangThai = Convert.ToString(reader["TrangThai"], CultureInfo.InvariantCulture) ?? string.Empty
                    };
                }
            }

            return result;
        }

        private static bool LaCuonLeDangHoatDong(string trangThai)
        {
            return string.Equals((trangThai ?? string.Empty).Trim(), "ACTIVE", StringComparison.OrdinalIgnoreCase);
        }

        private static Dictionary<long, FullInventorySnapshot> LayTonCuonChanTheoTTThanhPhamBatch(
            SQLiteConnection conn,
            IEnumerable<long> ttThanhPhamIds,
            IReadOnlyDictionary<long, FullInventorySnapshot> fullByProduct,
            SQLiteTransaction tx = null)
        {
            var ids = (ttThanhPhamIds ?? Enumerable.Empty<long>())
                .Where(x => x > 0)
                .Distinct()
                .ToList();

            var result = new Dictionary<long, FullInventorySnapshot>();
            if (ids.Count == 0)
                return result;

            var parameterNames = ids.Select((x, i) => "@tp" + i.ToString(CultureInfo.InvariantCulture)).ToList();
            string inClause = string.Join(",", parameterNames);

            string sql = @"
                WITH
                Nhap AS (
                    SELECT nk.TTThanhPham_ID AS TTThanhPham_ID,
                           COALESCE(SUM(
                               CASE
                                   WHEN cd.SoDau IS NULL
                                    AND cd.SoCuoi IS NULL
                                    AND COALESCE(cd.SoCuon, 0) > 0
                                       THEN cd.SoCuon
                                   ELSE 0
                               END
                           ), 0) AS TongNhap
                    FROM TTNhapKhoTP nk
                    JOIN TTCuonDay cd ON cd.ThongTinNhapKho_ID = nk.id
                    WHERE nk.TTThanhPham_ID IN (" + inClause + @")
                    GROUP BY nk.TTThanhPham_ID
                ),
                DaLay AS (
                    SELECT nk.TTThanhPham_ID AS TTThanhPham_ID,
                           COALESCE(SUM(ls.SoLuong), 0) AS SoLuong
                    FROM LichSuLayCuon ls
                    JOIN TTCuonDay cd ON cd.id = ls.TTCuonDay_ID
                    JOIN TTNhapKhoTP nk ON nk.id = cd.ThongTinNhapKho_ID
                    WHERE nk.TTThanhPham_ID IN (" + inClause + @")
                    GROUP BY nk.TTThanhPham_ID
                ),
                DaChuyenLe AS (
                    SELECT tcl.TTThanhPham_ID AS TTThanhPham_ID,
                           COUNT(DISTINCT lc.TonCuonLe_ID) AS SoLuong
                    FROM LichSuCat lc
                    JOIN TonCuonLe tcl ON tcl.id = lc.TonCuonLe_ID
                    WHERE lc.LoaiNguon = 'CUON_CHAN'
                      AND tcl.TTThanhPham_ID IN (" + inClause + @")
                    GROUP BY tcl.TTThanhPham_ID
                )
                SELECT tp.id AS TTThanhPham_ID,
                       tp.DanhSachSP_ID AS ProductId,
                       COALESCE(sp.Ten, '') AS TenSP,
                       COALESCE(n.TongNhap, 0) AS TongNhap,
                       COALESCE(dl.SoLuong, 0) AS DaLay,
                       COALESCE(dc.SoLuong, 0) AS DaChuyenLe
                FROM TTThanhPham tp
                JOIN DanhSachMaSP sp ON sp.id = tp.DanhSachSP_ID
                LEFT JOIN Nhap n ON n.TTThanhPham_ID = tp.id
                LEFT JOIN DaLay dl ON dl.TTThanhPham_ID = tp.id
                LEFT JOIN DaChuyenLe dc ON dc.TTThanhPham_ID = tp.id
                WHERE tp.id IN (" + inClause + @");";

            using var cmd = new SQLiteCommand(sql, conn, tx);
            for (int i = 0; i < ids.Count; i++)
                cmd.Parameters.AddWithValue(parameterNames[i], ids[i]);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                long tpId = Convert.ToInt64(reader["TTThanhPham_ID"], CultureInfo.InvariantCulture);
                long productId = Convert.ToInt64(reader["ProductId"], CultureInfo.InvariantCulture);
                int imported = Convert.ToInt32(reader["TongNhap"], CultureInfo.InvariantCulture);
                int taken = Convert.ToInt32(reader["DaLay"], CultureInfo.InvariantCulture);
                int converted = Convert.ToInt32(reader["DaChuyenLe"], CultureInfo.InvariantCulture);

                fullByProduct.TryGetValue(productId, out FullInventorySnapshot productSnapshot);

                result[tpId] = new FullInventorySnapshot
                {
                    DanhSachMaSP_ID = productId,
                    TenSP = Convert.ToString(reader["TenSP"], CultureInfo.InvariantCulture) ?? string.Empty,
                    TonThucTe = Math.Max(0, imported - taken - converted),
                    DatTruoc = productSnapshot?.DatTruoc ?? 0,
                    ChieuDaiChuan = productSnapshot?.ChieuDaiChuan ?? 0
                };
            }

            return result;
        }

        private static LapKeHoachCatDay_GridRow TaoDongCuonChanMoi(FullInventorySnapshot ton)
        {
            bool canEdit = ton.TonThucTe - ton.DatTruoc > 0 && ton.ChieuDaiChuan > 0;
            return new LapKeHoachCatDay_GridRow
            {
                RowKey = TaoRowKeyCuonChan(ton.DanhSachMaSP_ID),
                LoaiDong = LapKeHoachCatDay_LoaiDong.CuonChan,
                DanhSachMaSP_ID = ton.DanhSachMaSP_ID,
                TenSP = ton.TenSP,
                TonThucTe = ton.TonThucTe,
                DatTruoc = ton.DatTruoc,
                ChieuDai1Cuon = ton.ChieuDaiChuan,
                ChieuDaiChuanKeHoach = ton.ChieuDaiChuan > 0 ? (int?)ton.ChieuDaiChuan : null,
                DaTonTaiTrongDB = false,
                TrangThai = LapKeHoachCatDay_TrangThai.ChuaLuu,
                CanEdit = canEdit,
                CanDelete = false
            };
        }

        private static LapKeHoachCatDay_GridRow TaoDongCuonLeMoi(PartialInventorySnapshot ton)
        {
            bool canEdit = ton.TonThucTe - ton.DatTruoc > 0;
            return new LapKeHoachCatDay_GridRow
            {
                RowKey = TaoRowKeyCuonLe(ton.TonCuonLe_ID),
                LoaiDong = LapKeHoachCatDay_LoaiDong.CuonLe,
                DanhSachMaSP_ID = ton.DanhSachMaSP_ID,
                TonCuonLe_ID = ton.TonCuonLe_ID,
                TTThanhPham_IDNguon = ton.TTThanhPham_ID,
                TTCuonDay_IDNguon = ton.TTCuonDay_ID,
                TenSP = ton.TenSP,
                MaNguon = ton.MaCuon,
                TonThucTe = ton.TonThucTe,
                DatTruoc = ton.DatTruoc,
                ChieuDai1Cuon = ton.TonThucTe,
                ChieuDaiChuanKeHoach = null,
                SoDau = ton.SoDau,
                SoCuoi = ton.SoCuoi,
                DaTonTaiTrongDB = false,
                TrangThai = LapKeHoachCatDay_TrangThai.ChuaLuu,
                CanEdit = canEdit,
                CanDelete = false
            };
        }

        // ───────────────────────── Inventory ─────────────────────────

        private static FullInventorySnapshot LayTonCuonChan(SQLiteConnection conn, long productId, long? excludePlanId, SQLiteTransaction tx = null)
        {
            var result = new FullInventorySnapshot { DanhSachMaSP_ID = productId };

            using (var cmd = new SQLiteCommand("SELECT COALESCE(Ten, '') FROM DanhSachMaSP WHERE id = @id LIMIT 1;", conn, tx))
            {
                cmd.Parameters.AddWithValue("@id", productId);
                result.TenSP = Convert.ToString(cmd.ExecuteScalar(), CultureInfo.InvariantCulture) ?? string.Empty;
            }

            const string importSql = @"
                SELECT COALESCE(SUM(CASE WHEN cd.SoCuon > 0 THEN cd.SoCuon ELSE 0 END), 0) AS TongNhap
                FROM TTCuonDay cd
                JOIN TTNhapKhoTP nk ON nk.id = cd.ThongTinNhapKho_ID
                JOIN TTThanhPham tp ON tp.id = nk.TTThanhPham_ID
                WHERE tp.DanhSachSP_ID = @productId
                  AND cd.SoDau IS NULL
                  AND cd.SoCuoi IS NULL;";

            int imported;
            using (var cmd = new SQLiteCommand(importSql, conn, tx))
            {
                cmd.Parameters.AddWithValue("@productId", productId);
                using var reader = cmd.ExecuteReader();
                reader.Read();
                imported = Convert.ToInt32(reader["TongNhap"], CultureInfo.InvariantCulture);
            }

            const string takenSql = @"
                SELECT COALESCE(SUM(ls.SoLuong), 0)
                FROM LichSuLayCuon ls
                JOIN TTCuonDay cd ON cd.id = ls.TTCuonDay_ID
                JOIN TTNhapKhoTP nk ON nk.id = cd.ThongTinNhapKho_ID
                JOIN TTThanhPham tp ON tp.id = nk.TTThanhPham_ID
                WHERE tp.DanhSachSP_ID = @productId;";
            int taken = ExecuteInt(conn, tx, takenSql, ("@productId", (object)productId));

            const string cutSql = @"
                SELECT COUNT(DISTINCT lc.TonCuonLe_ID)
                FROM LichSuCat lc
                JOIN TonCuonLe tcl ON tcl.id = lc.TonCuonLe_ID
                JOIN TTCuonDay cd ON cd.id = tcl.TTCuonDay_ID
                JOIN TTNhapKhoTP nk ON nk.id = cd.ThongTinNhapKho_ID
                JOIN TTThanhPham tp ON tp.id = nk.TTThanhPham_ID
                WHERE lc.LoaiNguon = 'CUON_CHAN'
                  AND tp.DanhSachSP_ID = @productId;";
            int converted = ExecuteInt(conn, tx, cutSql, ("@productId", (object)productId));

            result.TonThucTe = Math.Max(0, imported - taken - converted);
            result.DatTruoc = LayDatTruocCuonChan(conn, tx, productId, excludePlanId);
            result.ChieuDaiChuan = LayChieuDaiChuanSanPham(conn, productId, tx);
            return result;
        }

        private static FullInventorySnapshot LayTonCuonChanTheoTTThanhPham(
            SQLiteConnection conn,
            long ttThanhPhamId,
            long productId,
            long? excludePlanId,
            SQLiteTransaction tx = null)
        {
            var result = new FullInventorySnapshot { DanhSachMaSP_ID = productId };

            using (var cmd = new SQLiteCommand("SELECT COALESCE(Ten, '') FROM DanhSachMaSP WHERE id = @id LIMIT 1;", conn, tx))
            {
                cmd.Parameters.AddWithValue("@id", productId);
                result.TenSP = Convert.ToString(cmd.ExecuteScalar(), CultureInfo.InvariantCulture) ?? string.Empty;
            }

            const string importSql = @"
                SELECT COALESCE(SUM(CASE WHEN cd.SoCuon > 0 THEN cd.SoCuon ELSE 0 END), 0) AS TongNhap
                FROM TTCuonDay cd
                JOIN TTNhapKhoTP nk ON nk.id = cd.ThongTinNhapKho_ID
                WHERE nk.TTThanhPham_ID = @tpId
                  AND cd.SoDau IS NULL
                  AND cd.SoCuoi IS NULL;";
            int imported = ExecuteInt(conn, tx, importSql, ("@tpId", (object)ttThanhPhamId));

            const string takenSql = @"
                SELECT COALESCE(SUM(ls.SoLuong), 0)
                FROM LichSuLayCuon ls
                JOIN TTCuonDay cd ON cd.id = ls.TTCuonDay_ID
                JOIN TTNhapKhoTP nk ON nk.id = cd.ThongTinNhapKho_ID
                WHERE nk.TTThanhPham_ID = @tpId;";
            int taken = ExecuteInt(conn, tx, takenSql, ("@tpId", (object)ttThanhPhamId));

            const string convertedSql = @"
                SELECT COUNT(DISTINCT lc.TonCuonLe_ID)
                FROM LichSuCat lc
                JOIN TonCuonLe tcl ON tcl.id = lc.TonCuonLe_ID
                WHERE lc.LoaiNguon = 'CUON_CHAN'
                  AND tcl.TTThanhPham_ID = @tpId;";
            int converted = ExecuteInt(conn, tx, convertedSql, ("@tpId", (object)ttThanhPhamId));

            result.TonThucTe = Math.Max(0, imported - taken - converted);
            result.DatTruoc = LayDatTruocCuonChan(conn, tx, productId, excludePlanId);
            result.ChieuDaiChuan = LayChieuDaiChuanSanPham(conn, productId, tx);
            return result;
        }

        private static List<PartialInventorySnapshot> LayDanhSachTonCuonLe(SQLiteConnection conn, long productId, long? excludePlanId)
        {
            const string sql = @"
                SELECT tcl.id,
                       tcl.MaCuon,
                       tcl.TTThanhPham_ID,
                       tcl.TTCuonDay_ID,
                       tcl.SoDau,
                       tcl.SoCuoi,
                       COALESCE(tcl.TrangThai, '') AS TrangThai,
                       sp.id AS DanhSachMaSP_ID,
                       sp.Ten
                FROM TonCuonLe tcl
                JOIN TTThanhPham tp ON tp.id = tcl.TTThanhPham_ID
                JOIN DanhSachMaSP sp ON sp.id = tp.DanhSachSP_ID
                WHERE sp.id = @productId
                  AND COALESCE(NULLIF(TRIM(tcl.TrangThai), ''), 'ACTIVE') = 'ACTIVE' COLLATE NOCASE
                  AND ABS(COALESCE(tcl.SoCuoi, 0) - COALESCE(tcl.SoDau, 0)) > 0
                ORDER BY tcl.MaCuon COLLATE NOCASE, tcl.id;";

            var list = new List<PartialInventorySnapshot>();
            using (var cmd = new SQLiteCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@productId", productId);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    int soDau = Convert.ToInt32(reader["SoDau"], CultureInfo.InvariantCulture);
                    int soCuoi = Convert.ToInt32(reader["SoCuoi"], CultureInfo.InvariantCulture);
                    list.Add(new PartialInventorySnapshot
                    {
                        TonCuonLe_ID = Convert.ToInt64(reader["id"], CultureInfo.InvariantCulture),
                        DanhSachMaSP_ID = Convert.ToInt64(reader["DanhSachMaSP_ID"], CultureInfo.InvariantCulture),
                        TTThanhPham_ID = Convert.ToInt64(reader["TTThanhPham_ID"], CultureInfo.InvariantCulture),
                        TTCuonDay_ID = Convert.ToInt64(reader["TTCuonDay_ID"], CultureInfo.InvariantCulture),
                        TenSP = Convert.ToString(reader["Ten"], CultureInfo.InvariantCulture) ?? string.Empty,
                        MaCuon = Convert.ToString(reader["MaCuon"], CultureInfo.InvariantCulture) ?? string.Empty,
                        SoDau = soDau,
                        SoCuoi = soCuoi,
                        TonThucTe = Math.Abs(soCuoi - soDau),
                        TrangThai = Convert.ToString(reader["TrangThai"], CultureInfo.InvariantCulture) ?? string.Empty
                    });
                }
            }

            foreach (var item in list)
            {
                item.DatTruoc = LayDatTruocCuonLe(conn, null, item.TonCuonLe_ID, excludePlanId);
                item.ChieuDaiChuan = null;
            }

            return list;
        }

        private static PartialInventorySnapshot LayTonCuonLe(SQLiteConnection conn, long tonCuonLeId, long? excludePlanId, SQLiteTransaction tx = null)
        {
            const string sql = @"
                SELECT tcl.id,
                       tcl.MaCuon,
                       tcl.TTThanhPham_ID,
                       tcl.TTCuonDay_ID,
                       tcl.SoDau,
                       tcl.SoCuoi,
                       COALESCE(tcl.TrangThai, '') AS TrangThai,
                       sp.id AS DanhSachMaSP_ID,
                       sp.Ten
                FROM TonCuonLe tcl
                JOIN TTThanhPham tp ON tp.id = tcl.TTThanhPham_ID
                JOIN DanhSachMaSP sp ON sp.id = tp.DanhSachSP_ID
                WHERE tcl.id = @id
                LIMIT 1;";

            PartialInventorySnapshot result;
            using (var cmd = new SQLiteCommand(sql, conn, tx))
            {
                cmd.Parameters.AddWithValue("@id", tonCuonLeId);
                using var reader = cmd.ExecuteReader();
                if (!reader.Read())
                    return null;

                int soDau = Convert.ToInt32(reader["SoDau"], CultureInfo.InvariantCulture);
                int soCuoi = Convert.ToInt32(reader["SoCuoi"], CultureInfo.InvariantCulture);
                result = new PartialInventorySnapshot
                {
                    TonCuonLe_ID = tonCuonLeId,
                    DanhSachMaSP_ID = Convert.ToInt64(reader["DanhSachMaSP_ID"], CultureInfo.InvariantCulture),
                    TTThanhPham_ID = Convert.ToInt64(reader["TTThanhPham_ID"], CultureInfo.InvariantCulture),
                    TTCuonDay_ID = Convert.ToInt64(reader["TTCuonDay_ID"], CultureInfo.InvariantCulture),
                    TenSP = Convert.ToString(reader["Ten"], CultureInfo.InvariantCulture) ?? string.Empty,
                    MaCuon = Convert.ToString(reader["MaCuon"], CultureInfo.InvariantCulture) ?? string.Empty,
                    SoDau = soDau,
                    SoCuoi = soCuoi,
                    TonThucTe = Math.Abs(soCuoi - soDau),
                    TrangThai = Convert.ToString(reader["TrangThai"], CultureInfo.InvariantCulture) ?? string.Empty
                };
            }

            result.DatTruoc = LayDatTruocCuonLe(conn, tx, tonCuonLeId, excludePlanId);
            result.ChieuDaiChuan = null;
            return result;
        }

        private static int LayDatTruocCuonChan(SQLiteConnection conn, SQLiteTransaction tx, long productId, long? excludePlanId)
        {
            const string wholeSql = @"
                SELECT COALESCE(SUM(
                    CASE
                        WHEN kh.SoLuongCuonCanLay > COALESCE(x.DaLay, 0)
                            THEN kh.SoLuongCuonCanLay - COALESCE(x.DaLay, 0)
                        ELSE 0
                    END
                ), 0)
                FROM KeHoachHang kh
                JOIN KeHoach k ON k.id = kh.KeHoach_ID
                LEFT JOIN (
                    SELECT KeHoachHang_ID, COALESCE(SUM(SoLuong), 0) AS DaLay
                    FROM LichSuLayCuon
                    GROUP BY KeHoachHang_ID
                ) x ON x.KeHoachHang_ID = kh.id
                WHERE kh.DanhSachMaSP_ID = @productId
                  AND k.TrangThai = 'ACTIVE'
                  AND (@excludePlanId IS NULL OR k.id <> @excludePlanId);";

            int whole = ExecuteInt(conn, tx, wholeSql,
                ("@productId", (object)productId),
                ("@excludePlanId", (object)excludePlanId ?? DBNull.Value));

            const string groupSql = @"
                SELECT COUNT(DISTINCT n.id)
                FROM KeHoachCatNhom n
                JOIN KeHoachHang kh ON kh.id = n.KeHoachHang_ID
                JOIN KeHoach k ON k.id = kh.KeHoach_ID
                WHERE kh.DanhSachMaSP_ID = @productId
                  AND k.TrangThai = 'ACTIVE'
                  AND (@excludePlanId IS NULL OR k.id <> @excludePlanId)
                  AND n.TonCuonLe_ID IS NULL
                  AND EXISTS (
                      SELECT 1
                      FROM KeHoachCatChiTiet d
                      LEFT JOIN LichSuCat lc ON lc.KeHoachCatChiTiet_ID = d.id
                      WHERE d.KeHoachCatNhom_ID = n.id
                        AND lc.id IS NULL
                  );";

            int groups = ExecuteInt(conn, tx, groupSql,
                ("@productId", (object)productId),
                ("@excludePlanId", (object)excludePlanId ?? DBNull.Value));

            return whole + groups;
        }

        private static int LayDatTruocCuonLe(SQLiteConnection conn, SQLiteTransaction tx, long tonCuonLeId, long? excludePlanId)
        {
            const string sql = @"
                SELECT COALESCE(SUM(d.ChieuDaiCanCat), 0)
                FROM KeHoachCatChiTiet d
                JOIN KeHoachCatNhom n ON n.id = d.KeHoachCatNhom_ID
                JOIN KeHoachHang kh ON kh.id = n.KeHoachHang_ID
                JOIN KeHoach k ON k.id = kh.KeHoach_ID
                LEFT JOIN LichSuCat lc ON lc.KeHoachCatChiTiet_ID = d.id
                WHERE n.TonCuonLe_ID = @tonCuonLeId
                  AND k.TrangThai = 'ACTIVE'
                  AND (@excludePlanId IS NULL OR k.id <> @excludePlanId)
                  AND lc.id IS NULL;";

            return ExecuteInt(conn, tx, sql,
                ("@tonCuonLeId", (object)tonCuonLeId),
                ("@excludePlanId", (object)excludePlanId ?? DBNull.Value));
        }

        private static bool TryLayChieuDaiChuanSanPham(
            SQLiteConnection conn,
            long productId,
            SQLiteTransaction tx,
            out int standard)
        {
            standard = 0;
            const string sql = @"
                SELECT DISTINCT cd.ChieuDai_1cuon
                FROM TTCuonDay cd
                JOIN TTNhapKhoTP nk ON nk.id = cd.ThongTinNhapKho_ID
                JOIN TTThanhPham tp ON tp.id = nk.TTThanhPham_ID
                WHERE tp.DanhSachSP_ID = @productId
                  AND cd.SoDau IS NULL
                  AND cd.SoCuoi IS NULL
                  AND cd.ChieuDai_1cuon > 0
                ORDER BY cd.ChieuDai_1cuon
                LIMIT 2;";

            var values = new List<int>();
            using var cmd = new SQLiteCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@productId", productId);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                values.Add(Convert.ToInt32(reader[0], CultureInfo.InvariantCulture));

            if (values.Count != 1)
                return false;

            standard = values[0];
            return standard > 0;
        }

        private static int LayChieuDaiChuanSanPham(SQLiteConnection conn, long productId, SQLiteTransaction tx = null)
        {
            return TryLayChieuDaiChuanSanPham(conn, productId, tx, out int standard) ? standard : 0;
        }

        // ───────────────────────── Load groups/status ─────────────────────────

        private static Dictionary<long, List<LapKeHoachCatDay_NhomCat>> LayNhomCatTheoKeHoach(
            SQLiteConnection conn,
            long planId)
        {
            const string sql = @"
                SELECT kh.id AS KeHoachHang_ID,
                       n.id AS Nhom_ID,
                       n.TonCuonLe_ID,
                       d.id AS ChiTiet_ID,
                       d.ChieuDaiCanCat,
                       CASE WHEN lc.id IS NULL THEN 0 ELSE 1 END AS DaThucHien
                FROM KeHoachHang kh
                JOIN KeHoachCatNhom n ON n.KeHoachHang_ID = kh.id
                JOIN KeHoachCatChiTiet d ON d.KeHoachCatNhom_ID = n.id
                LEFT JOIN LichSuCat lc ON lc.KeHoachCatChiTiet_ID = d.id
                WHERE kh.KeHoach_ID = @planId
                ORDER BY kh.id, n.id, d.id;";

            var groupMaps = new Dictionary<long, Dictionary<long, LapKeHoachCatDay_NhomCat>>();

            using var cmd = new SQLiteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@planId", planId);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                long khId = Convert.ToInt64(reader["KeHoachHang_ID"], CultureInfo.InvariantCulture);
                long groupId = Convert.ToInt64(reader["Nhom_ID"], CultureInfo.InvariantCulture);

                if (!groupMaps.TryGetValue(khId, out Dictionary<long, LapKeHoachCatDay_NhomCat> map))
                {
                    map = new Dictionary<long, LapKeHoachCatDay_NhomCat>();
                    groupMaps.Add(khId, map);
                }

                if (!map.TryGetValue(groupId, out LapKeHoachCatDay_NhomCat group))
                {
                    group = new LapKeHoachCatDay_NhomCat
                    {
                        Id = groupId,
                        TonCuonLe_ID = reader["TonCuonLe_ID"] == DBNull.Value
                            ? (long?)null
                            : Convert.ToInt64(reader["TonCuonLe_ID"], CultureInfo.InvariantCulture)
                    };
                    map.Add(groupId, group);
                }

                group.ChiTiet.Add(new LapKeHoachCatDay_ChiTietCat
                {
                    Id = Convert.ToInt64(reader["ChiTiet_ID"], CultureInfo.InvariantCulture),
                    KeHoachCatNhom_ID = groupId,
                    ChieuDai = Convert.ToInt32(reader["ChieuDaiCanCat"], CultureInfo.InvariantCulture),
                    DaThucHien = Convert.ToInt32(reader["DaThucHien"], CultureInfo.InvariantCulture) == 1
                });
            }

            return groupMaps.ToDictionary(
                x => x.Key,
                x => x.Value.Values.OrderBy(g => g.Id).ToList());
        }

        private static Dictionary<long, int> LaySoLuongDaLayTheoKeHoach(
            SQLiteConnection conn,
            long planId)
        {
            const string sql = @"
                SELECT kh.id AS KeHoachHang_ID,
                       COALESCE(SUM(ls.SoLuong), 0) AS SoLuongDaLay
                FROM KeHoachHang kh
                LEFT JOIN LichSuLayCuon ls ON ls.KeHoachHang_ID = kh.id
                WHERE kh.KeHoach_ID = @planId
                GROUP BY kh.id;";

            var result = new Dictionary<long, int>();
            using var cmd = new SQLiteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@planId", planId);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                long khId = Convert.ToInt64(reader["KeHoachHang_ID"], CultureInfo.InvariantCulture);
                result[khId] = Convert.ToInt32(reader["SoLuongDaLay"], CultureInfo.InvariantCulture);
            }

            return result;
        }

        private static List<LapKeHoachCatDay_NhomCat> LayNhomCatTheoKeHoachHang(SQLiteConnection conn, long khId)
        {
            const string sql = @"
                SELECT n.id AS Nhom_ID,
                       n.TonCuonLe_ID,
                       d.id AS ChiTiet_ID,
                       d.ChieuDaiCanCat,
                       CASE WHEN lc.id IS NULL THEN 0 ELSE 1 END AS DaThucHien
                FROM KeHoachCatNhom n
                JOIN KeHoachCatChiTiet d ON d.KeHoachCatNhom_ID = n.id
                LEFT JOIN LichSuCat lc ON lc.KeHoachCatChiTiet_ID = d.id
                WHERE n.KeHoachHang_ID = @khId
                ORDER BY n.id, d.id;";

            var map = new Dictionary<long, LapKeHoachCatDay_NhomCat>();
            using var cmd = new SQLiteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@khId", khId);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                long groupId = Convert.ToInt64(reader["Nhom_ID"], CultureInfo.InvariantCulture);
                if (!map.TryGetValue(groupId, out var group))
                {
                    group = new LapKeHoachCatDay_NhomCat
                    {
                        Id = groupId,
                        TonCuonLe_ID = reader["TonCuonLe_ID"] == DBNull.Value
                            ? (long?)null
                            : Convert.ToInt64(reader["TonCuonLe_ID"], CultureInfo.InvariantCulture)
                    };
                    map.Add(groupId, group);
                }

                group.ChiTiet.Add(new LapKeHoachCatDay_ChiTietCat
                {
                    Id = Convert.ToInt64(reader["ChiTiet_ID"], CultureInfo.InvariantCulture),
                    KeHoachCatNhom_ID = groupId,
                    ChieuDai = Convert.ToInt32(reader["ChieuDaiCanCat"], CultureInfo.InvariantCulture),
                    DaThucHien = Convert.ToInt32(reader["DaThucHien"], CultureInfo.InvariantCulture) == 1
                });
            }

            return map.Values.OrderBy(x => x.Id).ToList();
        }

        private static void GanTrangThaiCuonChan(LapKeHoachCatDay_GridRow row)
        {
            int conLay = Math.Max(0, row.SoLuongCanLay - row.SoLuongDaLay);
            bool conNhom = row.NhomCatDaLuu.Any(x => x.ChiTiet.Any(d => !d.DaThucHien));
            bool coThucHien = row.SoLuongDaLay > 0 || row.NhomCatDaLuu.Any(x => x.CoThucHien);
            bool hoanThanh = row.DaTonTaiTrongDB && conLay == 0 && !conNhom;

            if (hoanThanh)
                row.TrangThai = LapKeHoachCatDay_TrangThai.HoanThanh;
            else if (row.TonThucTe <= 0)
                row.TrangThai = LapKeHoachCatDay_TrangThai.HetTon;
            else if (coThucHien)
                row.TrangThai = LapKeHoachCatDay_TrangThai.DangThucHien;
            else
                row.TrangThai = LapKeHoachCatDay_TrangThai.ChuaThucHien;

            row.CanEdit = !hoanThanh;
            row.CanDelete = !hoanThanh && (conLay > 0 || conNhom);
        }

        private static void GanTrangThaiCuonLe(LapKeHoachCatDay_GridRow row)
        {
            var allDetails = row.NhomCatDaLuu.SelectMany(x => x.ChiTiet).ToList();
            bool hasAny = allDetails.Count > 0;
            bool allDone = hasAny && allDetails.All(x => x.DaThucHien);
            bool anyDone = allDetails.Any(x => x.DaThucHien);
            bool anyUndone = allDetails.Any(x => !x.DaThucHien);

            if (allDone)
                row.TrangThai = LapKeHoachCatDay_TrangThai.HoanThanh;
            else if (row.TonThucTe <= 0)
                row.TrangThai = LapKeHoachCatDay_TrangThai.HetTon;
            else if (anyDone)
                row.TrangThai = LapKeHoachCatDay_TrangThai.DangThucHien;
            else
                row.TrangThai = LapKeHoachCatDay_TrangThai.ChuaThucHien;

            row.CanEdit = !allDone && anyUndone;
            row.CanDelete = !allDone && anyUndone;
        }

        // ───────────────────────── Save row ─────────────────────────

        private static bool LuuDongCuonChan(
            SQLiteConnection conn,
            SQLiteTransaction tx,
            long planId,
            long khId,
            int standard,
            LapKeHoachCatDay_GridRow row,
            out string error,
            out bool stale)
        {
            error = string.Empty;
            stale = false;

            FullInventorySnapshot current = row.TTThanhPham_IDNguon.HasValue
                ? LayTonCuonChanTheoTTThanhPham(conn, row.TTThanhPham_IDNguon.Value, row.DanhSachMaSP_ID, planId, tx)
                : LayTonCuonChan(conn, row.DanhSachMaSP_ID, planId, tx);
            if (current.TonThucTe != row.TonThucTe || current.DatTruoc != row.DatTruoc)
            {
                stale = true;
                error = "Dữ liệu đã cũ, cần reload để cập nhật";
                return false;
            }

            int executedWhole = LaySoLuongDaLay(conn, khId, tx);
            if (row.SoLuongCanLay < executedWhole)
            {
                error = "Nội dung này đã được thực hiện và không thể chỉnh sửa.";
                return false;
            }

            var desiredGroups = new List<List<int>>();

            if (row.NhomCatPopup != null && row.NhomCatPopup.Count > 0)
            {
                foreach (var group in row.NhomCatPopup)
                {
                    var values = group.ChiTiet.Where(x => !x.DaThucHien).Select(x => x.ChieuDai).ToList();
                    if (values.Count == 0)
                        continue;
                    if (values.Any(x => x <= 0) || values.Sum() > standard)
                    {
                        error = "Tổng chiều dài các đoạn cắt không hợp lệ";
                        return false;
                    }
                    desiredGroups.Add(values);
                }
            }
            else if (!string.IsNullOrWhiteSpace(row.ChuoiChieuDaiCat))
            {
                if (!LapKeHoachCatDay_ChieuDaiParser.TryNormalize(
                    row.ChuoiChieuDaiCat,
                    out _,
                    out List<int> values,
                    out string invalid,
                    out bool syntax))
                {
                    error = LapKeHoachCatDay_ChieuDaiParser.TaoThongBaoLoi(invalid, syntax);
                    return false;
                }

                if (values.Count == 0)
                {
                    error = "Không xác định được chiều dài cần cắt. Vui lòng kiểm tra lại dữ liệu đã nhập.";
                    return false;
                }
                if (values.Sum() > standard)
                {
                    error = "Tổng chiều dài các đoạn cắt không hợp lệ";
                    return false;
                }
                desiredGroups.Add(values);
            }

            int remainingWhole = row.SoLuongCanLay - executedWhole;
            int required = remainingWhole + desiredGroups.Count;
            int available = Math.Max(0, current.TonThucTe - current.DatTruoc);

            if (required <= 0 && executedWhole <= 0)
            {
                error = "Kiểm tra lại dữ liệu.";
                return false;
            }

            if (required > 0 && current.TonThucTe <= 0)
            {
                error = "Đã hết tồn kho";
                return false;
            }

            if (required > available)
            {
                error = "Số cuộn khả dụng không đủ";
                return false;
            }

            using (var cmd = new SQLiteCommand("UPDATE KeHoachHang SET SoLuongCuonCanLay = @qty WHERE id = @id;", conn, tx))
            {
                cmd.Parameters.AddWithValue("@qty", row.SoLuongCanLay);
                cmd.Parameters.AddWithValue("@id", khId);
                cmd.ExecuteNonQuery();
            }

            XoaNhomCuonChanChuaBatDau(conn, tx, khId);

            foreach (List<int> groupValues in desiredGroups)
            {
                long groupId = InsertNhom(conn, tx, khId, null);
                foreach (int length in groupValues)
                    InsertChiTiet(conn, tx, groupId, length);
            }

            return true;
        }

        private static bool LuuDongCuonLe(
            SQLiteConnection conn,
            SQLiteTransaction tx,
            long planId,
            long khId,
            LapKeHoachCatDay_GridRow row,
            out string error,
            out bool stale)
        {
            error = string.Empty;
            stale = false;

            if (!row.TonCuonLe_ID.HasValue)
            {
                error = "Kiểm tra lại dữ liệu.";
                return false;
            }

            PartialInventorySnapshot current = LayTonCuonLe(conn, row.TonCuonLe_ID.Value, planId, tx);
            if (current == null)
            {
                stale = true;
                error = "Dữ liệu đã cũ, cần reload để cập nhật";
                return false;
            }

            if (current.TonThucTe != row.TonThucTe || current.DatTruoc != row.DatTruoc)
            {
                stale = true;
                error = "Dữ liệu đã cũ, cần reload để cập nhật";
                return false;
            }

            if (!LapKeHoachCatDay_ChieuDaiParser.TryNormalize(
                row.ChuoiChieuDaiCat,
                out _,
                out List<int> desired,
                out string invalid,
                out bool syntaxError))
            {
                error = LapKeHoachCatDay_ChieuDaiParser.TaoThongBaoLoi(invalid, syntaxError);
                return false;
            }

            int executedCount = DemChiTietDaThucHien(conn, tx, khId, row.TonCuonLe_ID.Value);
            if (!string.IsNullOrWhiteSpace(row.ChuoiChieuDaiCat) && desired.Count == 0)
            {
                error = "Không xác định được chiều dài cần cắt. Vui lòng kiểm tra lại dữ liệu đã nhập.";
                return false;
            }
            if (desired.Count == 0 && executedCount == 0)
            {
                error = "Kiểm tra lại dữ liệu.";
                return false;
            }

            int requiredLength = desired.Sum();
            int available = Math.Max(0, current.TonThucTe - current.DatTruoc);
            if (requiredLength > 0 && current.TonThucTe <= 0)
            {
                error = "Đã hết tồn kho";
                return false;
            }
            if (requiredLength > available)
            {
                error = "Chiều dài khả dụng của cuộn " + current.MaCuon + " không đủ";
                return false;
            }

            XoaChiTietChuaThucHienTheoCuonLe(conn, tx, khId, row.TonCuonLe_ID.Value);
            XoaNhomRong(conn, tx, khId, row.TonCuonLe_ID.Value);

            if (desired.Count > 0)
            {
                long? groupId = LayMotNhomCoLichSuTheoCuonLe(conn, tx, khId, row.TonCuonLe_ID.Value);
                if (!groupId.HasValue)
                    groupId = InsertNhom(conn, tx, khId, row.TonCuonLe_ID.Value);

                foreach (int length in desired)
                    InsertChiTiet(conn, tx, groupId.Value, length);
            }

            return true;
        }

        private static long InsertNhom(SQLiteConnection conn, SQLiteTransaction tx, long khId, long? tonCuonLeId)
        {
            const string sql = @"
                INSERT INTO KeHoachCatNhom (KeHoachHang_ID, TonCuonLe_ID, GhiChu, DateInsert)
                VALUES (@khId, @tonCuonLeId, '', CURRENT_TIMESTAMP);";
            using var cmd = new SQLiteCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@khId", khId);
            cmd.Parameters.AddWithValue("@tonCuonLeId", (object)tonCuonLeId ?? DBNull.Value);
            cmd.ExecuteNonQuery();
            return conn.LastInsertRowId;
        }

        private static void InsertChiTiet(SQLiteConnection conn, SQLiteTransaction tx, long groupId, int length)
        {
            const string sql = @"
                INSERT INTO KeHoachCatChiTiet (KeHoachCatNhom_ID, ChieuDaiCanCat, GhiChu, DateInsert)
                VALUES (@groupId, @length, '', CURRENT_TIMESTAMP);";
            using var cmd = new SQLiteCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@groupId", groupId);
            cmd.Parameters.AddWithValue("@length", length);
            cmd.ExecuteNonQuery();
        }

        // ───────────────────────── Delete helpers ─────────────────────────

        private static void XoaNhomCuonChanChuaBatDau(SQLiteConnection conn, SQLiteTransaction tx, long khId)
        {
            const string sql = @"
                DELETE FROM KeHoachCatNhom
                WHERE KeHoachHang_ID = @khId
                  AND TonCuonLe_ID IS NULL
                  AND NOT EXISTS (
                      SELECT 1
                      FROM KeHoachCatChiTiet d
                      JOIN LichSuCat lc ON lc.KeHoachCatChiTiet_ID = d.id
                      WHERE d.KeHoachCatNhom_ID = KeHoachCatNhom.id
                  );";
            using var cmd = new SQLiteCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@khId", khId);
            cmd.ExecuteNonQuery();
        }

        private static void XoaChiTietChuaThucHienTheoCuonLe(SQLiteConnection conn, SQLiteTransaction tx, long khId, long tonCuonLeId)
        {
            const string sql = @"
                DELETE FROM KeHoachCatChiTiet
                WHERE id IN (
                    SELECT d.id
                    FROM KeHoachCatChiTiet d
                    JOIN KeHoachCatNhom n ON n.id = d.KeHoachCatNhom_ID
                    LEFT JOIN LichSuCat lc ON lc.KeHoachCatChiTiet_ID = d.id
                    WHERE n.KeHoachHang_ID = @khId
                      AND n.TonCuonLe_ID = @tonCuonLeId
                      AND lc.id IS NULL
                );";
            using var cmd = new SQLiteCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@khId", khId);
            cmd.Parameters.AddWithValue("@tonCuonLeId", tonCuonLeId);
            cmd.ExecuteNonQuery();
        }

        private static void XoaNhomRong(SQLiteConnection conn, SQLiteTransaction tx, long khId, long tonCuonLeId)
        {
            const string sql = @"
                DELETE FROM KeHoachCatNhom
                WHERE KeHoachHang_ID = @khId
                  AND TonCuonLe_ID = @tonCuonLeId
                  AND NOT EXISTS (
                      SELECT 1 FROM KeHoachCatChiTiet d WHERE d.KeHoachCatNhom_ID = KeHoachCatNhom.id
                  );";
            using var cmd = new SQLiteCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@khId", khId);
            cmd.Parameters.AddWithValue("@tonCuonLeId", tonCuonLeId);
            cmd.ExecuteNonQuery();
        }

        private static void XoaKeHoachHangNeuRong(SQLiteConnection conn, SQLiteTransaction tx, long khId)
        {
            const string sql = @"
                DELETE FROM KeHoachHang
                WHERE id = @khId
                  AND SoLuongCuonCanLay = 0
                  AND NOT EXISTS (SELECT 1 FROM KeHoachCatNhom n WHERE n.KeHoachHang_ID = KeHoachHang.id)
                  AND NOT EXISTS (SELECT 1 FROM LichSuLayCuon ls WHERE ls.KeHoachHang_ID = KeHoachHang.id);";
            using var cmd = new SQLiteCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@khId", khId);
            cmd.ExecuteNonQuery();
        }

        private static bool ConNoiDungKhacSauKhiXoa(
            SQLiteConnection conn,
            SQLiteTransaction tx,
            long planId,
            long currentKhId,
            LapKeHoachCatDay_GridRow row)
        {
            const string otherKhSql = @"
                SELECT COUNT(*)
                FROM KeHoachHang kh
                WHERE kh.KeHoach_ID = @planId
                  AND kh.id <> @khId
                  AND (
                      kh.SoLuongCuonCanLay > 0
                      OR EXISTS (SELECT 1 FROM KeHoachCatNhom n WHERE n.KeHoachHang_ID = kh.id)
                  );";
            if (ExecuteInt(conn, tx, otherKhSql,
                ("@planId", (object)planId), ("@khId", (object)currentKhId)) > 0)
                return true;

            if (row.LoaiDong == LapKeHoachCatDay_LoaiDong.CuonChan)
            {
                const string partialSql = @"
                    SELECT COUNT(*)
                    FROM KeHoachCatNhom
                    WHERE KeHoachHang_ID = @khId
                      AND TonCuonLe_ID IS NOT NULL;";
                return ExecuteInt(conn, tx, partialSql, ("@khId", (object)currentKhId)) > 0;
            }

            const string remainingCurrentKh = @"
                SELECT CASE
                    WHEN kh.SoLuongCuonCanLay > 0 THEN 1
                    WHEN EXISTS (
                        SELECT 1 FROM KeHoachCatNhom n
                        WHERE n.KeHoachHang_ID = kh.id
                          AND (n.TonCuonLe_ID IS NULL OR n.TonCuonLe_ID <> @tonId)
                    ) THEN 1
                    ELSE 0
                END
                FROM KeHoachHang kh
                WHERE kh.id = @khId;";

            return ExecuteInt(conn, tx, remainingCurrentKh,
                ("@khId", (object)currentKhId),
                ("@tonId", (object)(row.TonCuonLe_ID ?? -1))) > 0;
        }

        private static bool KeHoachConNoiDung(SQLiteConnection conn, SQLiteTransaction tx, long planId)
        {
            const string sql = @"
                SELECT COUNT(*)
                FROM KeHoachHang kh
                WHERE kh.KeHoach_ID = @planId
                  AND (
                      kh.SoLuongCuonCanLay > 0
                      OR EXISTS (SELECT 1 FROM KeHoachCatNhom n WHERE n.KeHoachHang_ID = kh.id)
                      OR EXISTS (SELECT 1 FROM LichSuLayCuon ls WHERE ls.KeHoachHang_ID = kh.id)
                  );";
            return ExecuteInt(conn, tx, sql, ("@planId", (object)planId)) > 0;
        }

        private static bool KeHoachCoLichSu(SQLiteConnection conn, SQLiteTransaction tx, long planId)
        {
            const string sql = @"
                SELECT CASE WHEN
                    EXISTS (
                        SELECT 1
                        FROM LichSuLayCuon ls
                        JOIN KeHoachHang kh ON kh.id = ls.KeHoachHang_ID
                        WHERE kh.KeHoach_ID = @planId
                    )
                    OR EXISTS (
                        SELECT 1
                        FROM LichSuCat lc
                        JOIN KeHoachCatChiTiet d ON d.id = lc.KeHoachCatChiTiet_ID
                        JOIN KeHoachCatNhom n ON n.id = d.KeHoachCatNhom_ID
                        JOIN KeHoachHang kh ON kh.id = n.KeHoachHang_ID
                        WHERE kh.KeHoach_ID = @planId
                    )
                THEN 1 ELSE 0 END;";
            return ExecuteInt(conn, tx, sql, ("@planId", (object)planId)) == 1;
        }

        // ───────────────────────── Scalar helpers ─────────────────────────

        private static long? LayKeHoachIdTheoMa(SQLiteConnection conn, SQLiteTransaction tx, string ma)
        {
            using var cmd = new SQLiteCommand(
                "SELECT id FROM KeHoach WHERE TRIM(MaKeHoach) = TRIM(@ma) COLLATE NOCASE LIMIT 1;",
                conn, tx);
            cmd.Parameters.AddWithValue("@ma", ma);
            object obj = cmd.ExecuteScalar();
            return obj == null || obj == DBNull.Value ? (long?)null : Convert.ToInt64(obj, CultureInfo.InvariantCulture);
        }

        private static long? LayKeHoachHangId(SQLiteConnection conn, SQLiteTransaction tx, long planId, long productId)
        {
            using var cmd = new SQLiteCommand(
                "SELECT id FROM KeHoachHang WHERE KeHoach_ID = @planId AND DanhSachMaSP_ID = @productId LIMIT 1;",
                conn, tx);
            cmd.Parameters.AddWithValue("@planId", planId);
            cmd.Parameters.AddWithValue("@productId", productId);
            object obj = cmd.ExecuteScalar();
            return obj == null || obj == DBNull.Value ? (long?)null : Convert.ToInt64(obj, CultureInfo.InvariantCulture);
        }

        private static int? LayChieuDaiKeHoachHang(SQLiteConnection conn, SQLiteTransaction tx, long khId)
        {
            using var cmd = new SQLiteCommand(
                "SELECT ChieuDai1Cuon_KeHoach FROM KeHoachHang WHERE id = @id;", conn, tx);
            cmd.Parameters.AddWithValue("@id", khId);
            object obj = cmd.ExecuteScalar();
            return obj == null || obj == DBNull.Value
                ? (int?)null
                : Convert.ToInt32(obj, CultureInfo.InvariantCulture);
        }

        private static int LaySoLuongKeHoachHang(SQLiteConnection conn, SQLiteTransaction tx, long khId)
        {
            return ExecuteInt(conn, tx,
                "SELECT COALESCE(SoLuongCuonCanLay, 0) FROM KeHoachHang WHERE id = @id;",
                ("@id", (object)khId));
        }

        private static int LaySoLuongDaLay(SQLiteConnection conn, long khId, SQLiteTransaction tx = null)
        {
            return ExecuteInt(conn, tx,
                "SELECT COALESCE(SUM(SoLuong), 0) FROM LichSuLayCuon WHERE KeHoachHang_ID = @id;",
                ("@id", (object)khId));
        }

        private static int DemNhomChuaBatDau(SQLiteConnection conn, SQLiteTransaction tx, long khId)
        {
            const string sql = @"
                SELECT COUNT(*)
                FROM KeHoachCatNhom n
                WHERE n.KeHoachHang_ID = @khId
                  AND n.TonCuonLe_ID IS NULL
                  AND EXISTS (SELECT 1 FROM KeHoachCatChiTiet d WHERE d.KeHoachCatNhom_ID = n.id);";
            return ExecuteInt(conn, tx, sql, ("@khId", (object)khId));
        }

        private static int DemChiTietChuaThucHien(SQLiteConnection conn, SQLiteTransaction tx, long khId, long tonCuonLeId)
        {
            const string sql = @"
                SELECT COUNT(*)
                FROM KeHoachCatChiTiet d
                JOIN KeHoachCatNhom n ON n.id = d.KeHoachCatNhom_ID
                LEFT JOIN LichSuCat lc ON lc.KeHoachCatChiTiet_ID = d.id
                WHERE n.KeHoachHang_ID = @khId
                  AND n.TonCuonLe_ID = @tonId
                  AND lc.id IS NULL;";
            return ExecuteInt(conn, tx, sql,
                ("@khId", (object)khId), ("@tonId", (object)tonCuonLeId));
        }

        private static int DemChiTietDaThucHien(SQLiteConnection conn, SQLiteTransaction tx, long khId, long tonCuonLeId)
        {
            const string sql = @"
                SELECT COUNT(*)
                FROM KeHoachCatChiTiet d
                JOIN KeHoachCatNhom n ON n.id = d.KeHoachCatNhom_ID
                JOIN LichSuCat lc ON lc.KeHoachCatChiTiet_ID = d.id
                WHERE n.KeHoachHang_ID = @khId
                  AND n.TonCuonLe_ID = @tonId;";
            return ExecuteInt(conn, tx, sql,
                ("@khId", (object)khId), ("@tonId", (object)tonCuonLeId));
        }

        private static long? LayMotNhomCoLichSuTheoCuonLe(SQLiteConnection conn, SQLiteTransaction tx, long khId, long tonCuonLeId)
        {
            const string sql = @"
                SELECT n.id
                FROM KeHoachCatNhom n
                WHERE n.KeHoachHang_ID = @khId
                  AND n.TonCuonLe_ID = @tonId
                  AND EXISTS (
                      SELECT 1
                      FROM KeHoachCatChiTiet d
                      JOIN LichSuCat lc ON lc.KeHoachCatChiTiet_ID = d.id
                      WHERE d.KeHoachCatNhom_ID = n.id
                  )
                ORDER BY n.id
                LIMIT 1;";
            using var cmd = new SQLiteCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@khId", khId);
            cmd.Parameters.AddWithValue("@tonId", tonCuonLeId);
            object obj = cmd.ExecuteScalar();
            return obj == null || obj == DBNull.Value ? (long?)null : Convert.ToInt64(obj, CultureInfo.InvariantCulture);
        }

        private static int ExecuteInt(SQLiteConnection conn, SQLiteTransaction tx, string sql, params (string Name, object Value)[] parameters)
        {
            using var cmd = new SQLiteCommand(sql, conn, tx);
            foreach (var p in parameters)
                cmd.Parameters.AddWithValue(p.Name, p.Value ?? DBNull.Value);
            object obj = cmd.ExecuteScalar();
            return obj == null || obj == DBNull.Value ? 0 : Convert.ToInt32(obj, CultureInfo.InvariantCulture);
        }

        private static string TaoRowKeyCuonChan(long productId) => "FULL|" + productId.ToString(CultureInfo.InvariantCulture);
        private static string TaoRowKeyCuonLe(long tonCuonLeId) => "PARTIAL|" + tonCuonLeId.ToString(CultureInfo.InvariantCulture);

        private static void EnsurePlanningSchema(SQLiteConnection conn)
        {
            string[] tables =
            {
                "KeHoach", "KeHoachHang", "TonCuonLe", "KeHoachCatNhom",
                "KeHoachCatChiTiet", "LichSuLayCuon", "LichSuCat"
            };

            foreach (string table in tables)
            {
                using var cmd = new SQLiteCommand(
                    "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name=@name;", conn);
                cmd.Parameters.AddWithValue("@name", table);
                if (Convert.ToInt32(cmd.ExecuteScalar(), CultureInfo.InvariantCulture) == 0)
                    throw new InvalidOperationException("Database chưa có bảng " + table + ".");
            }

            bool hasNguoiNhan = false;
            using (var cmd = new SQLiteCommand("PRAGMA table_info(KeHoach);", conn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    if (string.Equals(Convert.ToString(reader["name"], CultureInfo.InvariantCulture), "NguoiNhan", StringComparison.OrdinalIgnoreCase))
                    {
                        hasNguoiNhan = true;
                        break;
                    }
                }
            }

            if (!hasNguoiNhan)
                throw new InvalidOperationException("Database chưa có cột KeHoach.NguoiNhan.");
        }
    }
}
