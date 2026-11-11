using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DG_TonKhoBTP_v02.Models.Kho.TonKho;
using CoreHelper = DG_TonKhoBTP_v02.Helper.Helper;

namespace DG_TonKhoBTP_v02.Database.Kho.TonKho
{
    /// <summary>Báo cáo tồn thực tế: mọi kết nối qua DB_Base, chỉ SELECT, không thay đổi kế hoạch.</summary>
    internal static class BC_TonKho_DB
    {
        // Thứ tự cột được cố định để phép kiểm thử SQL có thể đối chiếu với database thực tế.
        private const string WholeSql = @"
SELECT cd.id, tp.id, tp.DanhSachSP_ID, COALESCE(sp.Ten,''), COALESCE(tp.MaBin,''),
       nk.NgayNhapKho, cd.ChieuDai_1cuon,
       COALESCE(cd.SoCuon,0),
       COALESCE((SELECT SUM(ls.SoLuong) FROM LichSuLayCuon ls WHERE ls.TTCuonDay_ID=cd.id),0),
       (SELECT COUNT(DISTINCT lc.TonCuonLe_ID) FROM LichSuCat lc
        JOIN TonCuonLe cl ON cl.id=lc.TonCuonLe_ID
        WHERE lc.LoaiNguon='CUON_CHAN' AND cl.TTCuonDay_ID=cd.id)
FROM TTCuonDay cd
JOIN TTNhapKhoTP nk ON nk.id=cd.ThongTinNhapKho_ID
JOIN TTThanhPham tp ON tp.id=nk.TTThanhPham_ID
JOIN DanhSachMaSP sp ON sp.id=tp.DanhSachSP_ID
WHERE tp.NhapKho=1 AND tp.Temp=0 AND cd.SoDau IS NULL AND cd.SoCuoi IS NULL
  AND tp.DanhSachSP_ID=@productId
ORDER BY nk.NgayNhapKho, cd.id;";

        private const string PartialSql = @"
SELECT cl.id, cl.SoDau, cl.SoCuoi,
       CASE WHEN EXISTS(SELECT 1 FROM LichSuCat lc
          WHERE lc.TonCuonLe_ID=cl.id AND lc.LoaiNguon='CUON_CHAN') THEN 1 ELSE 0 END,
       tp.id, tp.DanhSachSP_ID, COALESCE(sp.Ten,''), COALESCE(tp.MaBin,''),
       cl.MaCuon, cd.id, cd.ChieuDai_1cuon, COALESCE(cl.TrangThai,''), nk.NgayNhapKho, cl.TTThanhPham_ID
FROM TonCuonLe cl
JOIN TTCuonDay cd ON cd.id=cl.TTCuonDay_ID
JOIN TTNhapKhoTP nk ON nk.id=cd.ThongTinNhapKho_ID
JOIN TTThanhPham tp ON tp.id=nk.TTThanhPham_ID
JOIN DanhSachMaSP sp ON sp.id=tp.DanhSachSP_ID
WHERE tp.NhapKho=1 AND tp.Temp=0 AND tp.DanhSachSP_ID=@productId
ORDER BY cd.id, cl.id;";

        // Phát hiện cuộn lẻ đã nhập nhưng bản ghi tồn hiện tại bị mất.
        private const string MissingPartialSql = @"
SELECT cd.id, tp.id, COALESCE(sp.Ten,''), COALESCE(tp.MaBin,''), cd.ChieuDai_1cuon
FROM TTCuonDay cd
JOIN TTNhapKhoTP nk ON nk.id=cd.ThongTinNhapKho_ID
JOIN TTThanhPham tp ON tp.id=nk.TTThanhPham_ID
JOIN DanhSachMaSP sp ON sp.id=tp.DanhSachSP_ID
WHERE tp.NhapKho=1 AND tp.Temp=0 AND tp.DanhSachSP_ID=@productId
  AND (cd.SoDau IS NOT NULL OR cd.SoCuoi IS NOT NULL)
  AND NOT EXISTS (SELECT 1 FROM TonCuonLe cl WHERE cl.TTCuonDay_ID=cd.id);";

        private const string OrphanPartialSql = @"
SELECT cl.id, tp.id, COALESCE(sp.Ten,''), COALESCE(tp.MaBin,''),
       COALESCE(cl.MaCuon,''), cl.SoDau, cl.SoCuoi, cl.TTCuonDay_ID
FROM TonCuonLe cl
JOIN TTThanhPham tp ON tp.id=cl.TTThanhPham_ID
JOIN DanhSachMaSP sp ON sp.id=tp.DanhSachSP_ID
LEFT JOIN TTCuonDay cd ON cd.id=cl.TTCuonDay_ID
LEFT JOIN TTNhapKhoTP nk ON nk.id=cd.ThongTinNhapKho_ID
WHERE tp.NhapKho=1 AND tp.Temp=0 AND tp.DanhSachSP_ID=@productId
  AND (cd.id IS NULL OR nk.id IS NULL);";

        // Quy cách được đặt trước chưa lấy, cộng nhóm cắt từ cuộn chẵn chưa bắt đầu.
        private const string ReserveWholeSql = @"
SELECT kh.DanhSachMaSP_ID, qc.ChieuDai1Cuon,
       MAX(0, qc.SoLuongCuonCanLay - COALESCE((SELECT SUM(ls.SoLuong)
         FROM LichSuLayCuon ls WHERE ls.QuyCachCuonChan_ID=qc.id),0))
       + (SELECT COUNT(*) FROM KeHoachCatNhom n
          WHERE n.QuyCachCuonChan_ID=qc.id AND n.TonCuonLe_ID IS NULL
            AND EXISTS (SELECT 1 FROM KeHoachCatChiTiet d
                        WHERE d.KeHoachCatNhom_ID=n.id
                          AND NOT EXISTS (SELECT 1 FROM LichSuCat lc WHERE lc.KeHoachCatChiTiet_ID=d.id))) AS Pending
FROM KeHoachHangQuyCachCuonChan qc
JOIN KeHoachHang kh ON kh.id=qc.KeHoachHang_ID
JOIN KeHoach k ON k.id=kh.KeHoach_ID
WHERE k.TrangThai='ACTIVE' AND kh.DanhSachMaSP_ID=@productId;";

        private const string ReservePartialSql = @"
SELECT n.TonCuonLe_ID, COALESCE(SUM(d.ChieuDaiCanCat),0)
FROM KeHoachCatNhom n
JOIN KeHoachHang kh ON kh.id=n.KeHoachHang_ID
JOIN KeHoach k ON k.id=kh.KeHoach_ID
JOIN KeHoachCatChiTiet d ON d.KeHoachCatNhom_ID=n.id
WHERE n.TonCuonLe_ID IS NOT NULL AND k.TrangThai='ACTIVE'
  AND kh.DanhSachMaSP_ID=@productId
  AND NOT EXISTS (SELECT 1 FROM LichSuCat lc WHERE lc.KeHoachCatChiTiet_ID=d.id)
GROUP BY n.TonCuonLe_ID, kh.DanhSachMaSP_ID;";

        private const string PendingPartialSegmentsSourceSql = @"
FROM KeHoachCatNhom n
JOIN KeHoachHang kh ON kh.id=n.KeHoachHang_ID
JOIN KeHoach k ON k.id=kh.KeHoach_ID
JOIN KeHoachCatChiTiet ct ON ct.KeHoachCatNhom_ID=n.id
WHERE n.TonCuonLe_ID IS NOT NULL AND k.TrangThai='ACTIVE'
  AND (@cuonId IS NULL OR n.TonCuonLe_ID=@cuonId)
  AND NOT EXISTS (SELECT 1 FROM LichSuCat ls
                  WHERE ls.KeHoachCatChiTiet_ID=ct.id)";

        private const string PendingPartialSegmentsSql = @"
SELECT n.TonCuonLe_ID, kh.DanhSachMaSP_ID,
       CASE WHEN typeof(ct.ChieuDaiCanCat) IN ('integer','real') THEN ct.ChieuDaiCanCat END,
       k.id, COALESCE(k.MaKeHoach,''), ct.id" + PendingPartialSegmentsSourceSql + @"
ORDER BY ct.id;";

        private const string SuggestionSql = @"
SELECT 1 AS Loai, tp.id AS Id, tp.MaBin AS GiaTri
FROM TTThanhPham tp JOIN TTNhapKhoTP nk ON nk.TTThanhPham_ID=tp.id
WHERE tp.NhapKho=1 AND tp.Temp=0 AND tp.MaBin LIKE @pattern ESCAPE '\'
UNION
SELECT 2, NULL, TRIM(sp.Ten)
FROM DanhSachMaSP sp JOIN TTThanhPham tp ON tp.DanhSachSP_ID=sp.id
JOIN TTNhapKhoTP nk ON nk.TTThanhPham_ID=tp.id
WHERE tp.NhapKho=1 AND tp.Temp=0 AND TRIM(sp.Ten)<>''
  AND (sp.Ten LIKE @pattern ESCAPE '\' OR sp.Ten_KhongDau LIKE @plain ESCAPE '\')
UNION
SELECT 3, cl.id, cl.MaCuon
FROM TonCuonLe cl JOIN TTThanhPham tp ON tp.id=cl.TTThanhPham_ID
JOIN TTNhapKhoTP nk ON nk.TTThanhPham_ID=tp.id
WHERE tp.NhapKho=1 AND tp.Temp=0 AND cl.MaCuon LIKE @pattern ESCAPE '\'
ORDER BY Loai, GiaTri;";

        private const string ProductIdsSql = @"
SELECT DISTINCT tp.DanhSachSP_ID, COALESCE(sp.Ten,''), COALESCE(sp.Ma,''), COALESCE(sp.DonVi,'')
FROM TTThanhPham tp
JOIN TTNhapKhoTP nk ON nk.TTThanhPham_ID=tp.id
JOIN DanhSachMaSP sp ON sp.id=tp.DanhSachSP_ID
WHERE tp.NhapKho=1 AND tp.Temp=0
  AND ((@loai=1 AND tp.id=@id)
    OR (@loai=2 AND TRIM(sp.Ten)=TRIM(@name))
    OR (@loai=3 AND EXISTS (SELECT 1 FROM TonCuonLe cl
           WHERE cl.id=@id AND cl.TTThanhPham_ID=tp.id)));";

        internal static Task<DataTable> TimGoiYAsync(string keyword, bool gomHetTon, CancellationToken ct)
        {
            return Task.Run(() =>
            {
                var table = new DataTable();
                table.Columns.Add("Loai", typeof(int));
                table.Columns.Add("Id", typeof(long));
                table.Columns.Add("GiaTri", typeof(string));
                if (string.IsNullOrWhiteSpace(keyword)) return table;
                ct.ThrowIfCancellationRequested();
                string pattern = LikePattern(keyword.Trim());
                string plain = LikePattern(CoreHelper.BoDauTiengViet(keyword.Trim()) ?? keyword.Trim());
                using (var conn = DB_Base.OpenConnection())
                using (var tx = conn.BeginTransaction(true))
                {
                    var matches = new List<BCTonKho_GoiY>();
                    using (var cmd = Make(conn, tx, SuggestionSql, ("@pattern", pattern), ("@plain", plain)))
                    using (var rd = cmd.ExecuteReader())
                    {
                        while (rd.Read())
                        {
                            ct.ThrowIfCancellationRequested();
                            matches.Add(new BCTonKho_GoiY {
                                Loai = (BCTonKho_LoaiTimKiem)Convert.ToInt32(rd[0], CultureInfo.InvariantCulture),
                                Id = NullableInt(rd, 1), GiaTri = rd.GetString(2) });
                        }
                    }
                    // Lọc thực tồn theo chính đường tính của báo cáo chi tiết.
                    var cache = new Dictionary<string, List<BCTonKho_DongTon>>();
                    var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var m in matches)
                    {
                        ct.ThrowIfCancellationRequested();
                        var cacheKey = ((int)m.Loai).ToString(CultureInfo.InvariantCulture) + ":" + (m.Loai == BCTonKho_LoaiTimKiem.SanPham ? m.GiaTri : m.Id.ToString());
                        if (!cache.TryGetValue(cacheKey, out var rows))
                        {
                            rows = ReadSelected(conn, tx, m.Loai, m.Id, m.GiaTri, ct);
                            cache[cacheKey] = rows;
                        }
                        if (!FilterRows(rows, gomHetTon).Any()) continue;
                        // Cuộn lẻ nhập thẳng có mã trùng Bin: chỉ gợi ý Bin.
                        if (m.Loai == BCTonKho_LoaiTimKiem.CuonLe && matches.Any(b =>
                            b.Loai == BCTonKho_LoaiTimKiem.Bin && string.Equals(b.GiaTri, m.GiaTri, StringComparison.OrdinalIgnoreCase))) continue;
                        string key = m.Loai == BCTonKho_LoaiTimKiem.SanPham ? "ten:" + m.GiaTri : "id:" + m.GiaTri;
                        if (!seen.Add(key)) continue;
                        table.Rows.Add((int)m.Loai, (object)m.Id ?? DBNull.Value, m.GiaTri);
                        if (table.Rows.Count >= 50) break;
                    }
                    tx.Commit();
                }
                return table;
            }, ct);
        }

        internal static Task<BCTonKho_KetQua> LayChiTietTonAsync(BCTonKho_LoaiTimKiem loai, long? id,
            string giaTri, bool gomHetTon, CancellationToken ct)
        {
            return Task.Run(() =>
            {
                ct.ThrowIfCancellationRequested();
                using (var conn = DB_Base.OpenConnection())
                using (var tx = conn.BeginTransaction(true))
                {
                    var all = ReadSelected(conn, tx, loai, id, giaTri, ct);
                    var visible = FilterRows(all, gomHetTon).OrderBy(x => x.TenSanPham)
                        .ThenBy(x => x.MaBinGoc).ThenBy(x => x.Kieu)
                        .ThenBy(x => x.TTCuonDayId).ThenBy(x => x.TonCuonLeId).ToList();
                    tx.Commit();
                    ct.ThrowIfCancellationRequested();
                    return new BCTonKho_KetQua { Dong = visible,
                        CoBatThuong = visible.Any(x => x.LaBatThuong) };
                }
            }, ct);
        }

        private static IEnumerable<BCTonKho_DongTon> FilterRows(IEnumerable<BCTonKho_DongTon> rows, bool gomHetTon)
        {
            return rows.Where(x => gomHetTon || x.LaBatThuong ||
                (x.Kieu == BCTonKho_KieuDong.CuonChan ? x.SoLuong > 0 : x.ChieuDaiConLai > 0));
        }

        private static List<BCTonKho_DongTon> ReadSelected(SQLiteConnection conn, SQLiteTransaction tx,
            BCTonKho_LoaiTimKiem loai, long? id, string giaTri, CancellationToken ct)
        {
            if (loai != BCTonKho_LoaiTimKiem.SanPham && !id.HasValue)
                return new List<BCTonKho_DongTon>();
            var products = new List<(long Id, string Ten, string Ma, string DonVi)>();
            using (var cmd = Make(conn, tx, ProductIdsSql, ("@loai", (int)loai),
                ("@id", (object)id ?? DBNull.Value), ("@name", giaTri ?? "")))
            using (var rd = cmd.ExecuteReader())
                while (rd.Read())
                {
                    ct.ThrowIfCancellationRequested();
                    products.Add((rd.GetInt64(0), String(rd, 1), String(rd, 2), String(rd, 3)));
                }
            var rows = new List<BCTonKho_DongTon>();
            foreach (var product in products)
            {
                ct.ThrowIfCancellationRequested();
                var group = ReadProduct(conn, tx, product.Id, ct);
                ApplyReservations(conn, tx, product.Id, group, ct);
                foreach (var row in group)
                {
                    row.TenSanPham = product.Ten;
                    row.MaSanPham = product.Ma;
                    row.DonVi = product.DonVi;
                }
                rows.AddRange(group);
            }
            if (loai == BCTonKho_LoaiTimKiem.Bin)
                return rows.Where(x => x.TTThanhPhamId == id).ToList();
            if (loai == BCTonKho_LoaiTimKiem.CuonLe)
                return rows.Where(x => x.TonCuonLeId == id).ToList();
            return rows;
        }

        private static List<BCTonKho_DongTon> ReadProduct(SQLiteConnection conn, SQLiteTransaction tx, long productId, CancellationToken ct)
        {
            var rows = new List<BCTonKho_DongTon>();
            using (var cmd = Make(conn, tx, WholeSql, ("@productId", productId)))
            using (var rd = cmd.ExecuteReader())
                while (rd.Read())
                {
                    ct.ThrowIfCancellationRequested();
                    long imported = Number(rd, 7), taken = Number(rd, 8), converted = Number(rd, 9);
                    var item = new BCTonKho_DongTon {
                        TTCuonDayId = Number(rd, 0), TTThanhPhamId = Number(rd, 1), SanPhamId = productId,
                        TenSanPham = String(rd, 3), MaBinGoc = String(rd, 4), MaHienThi = String(rd, 4),
                        NgayNhap = String(rd, 5), Kieu = BCTonKho_KieuDong.CuonChan,
                        SoLuong = imported - taken - converted, ChieuDaiBanDau = NullableInt(rd, 6),
                        ChieuDaiConLai = NullableInt(rd, 6), DonViDatCho = "cuộn" };
                    if (imported < 0 || taken < 0 || converted < 0 || item.SoLuong < 0)
                        item.DanhDauLoi("Số cuộn nhập/lấy/chuyển không hợp lệ");
                    if (!item.ChieuDaiConLai.HasValue || item.ChieuDaiConLai <= 0)
                        item.DanhDauLoi("Thiếu chiều dài cuộn chẵn hợp lệ");
                    rows.Add(item);
                }
            using (var cmd = Make(conn, tx, PartialSql, ("@productId", productId)))
            using (var rd = cmd.ExecuteReader())
                while (rd.Read())
                {
                    ct.ThrowIfCancellationRequested();
                    long? dau = NullableInt(rd, 1), cuoi = NullableInt(rd, 2);
                    bool converted = Number(rd, 3) == 1;
                    string state = String(rd, 11).Trim().ToUpperInvariant();
                    var item = new BCTonKho_DongTon {
                        TonCuonLeId = Number(rd, 0), SoDau = dau, SoCuoi = cuoi,
                        TTThanhPhamId = Number(rd, 4), SanPhamId = productId,
                        TenSanPham = String(rd, 6), MaBinGoc = String(rd, 7), MaHienThi = String(rd, 8),
                        TTCuonDayId = Number(rd, 9), NgayNhap = String(rd, 12), Kieu = BCTonKho_KieuDong.CuonLe,
                        ChieuDaiBanDau = converted ? null : NullableInt(rd, 10),
                        ChieuDaiConLai = dau.HasValue && cuoi.HasValue ? Math.Abs(cuoi.Value - dau.Value) : (long?)null,
                        SoLuong = dau.HasValue && cuoi.HasValue && dau != cuoi ? 1 : 0,
                        DonViDatCho = "m", TrangThai = state };
                    if (Number(rd, 13) != Number(rd, 4))
                        item.DanhDauLoi("Mã nguồn cuộn lẻ không khớp nguồn nhập kho");
                    if (!dau.HasValue || !cuoi.HasValue) item.DanhDauLoi("Thiếu số đầu hoặc số cuối");
                    if (state != "ACTIVE" && state != "HET") item.DanhDauLoi("Trạng thái cuộn không hợp lệ");
                    if (state == "ACTIVE" && item.ChieuDaiConLai == 0) item.DanhDauLoi("Cuộn ACTIVE nhưng đã hết chiều dài");
                    if (state == "HET" && item.ChieuDaiConLai > 0) item.DanhDauLoi("Cuộn HET nhưng vẫn còn chiều dài");
                    if (string.IsNullOrWhiteSpace(item.MaHienThi)) item.DanhDauLoi("Thiếu mã cuộn lẻ");
                    rows.Add(item);
                }
            using (var cmd = Make(conn, tx, MissingPartialSql, ("@productId", productId)))
            using (var rd = cmd.ExecuteReader())
                while (rd.Read())
                {
                    ct.ThrowIfCancellationRequested();
                    var missing = new BCTonKho_DongTon {
                        TTCuonDayId = Number(rd, 0), TTThanhPhamId = Number(rd, 1), SanPhamId = productId,
                        TenSanPham = String(rd, 2), MaBinGoc = String(rd, 3), MaHienThi = String(rd, 3),
                        Kieu = BCTonKho_KieuDong.CuonLe, ChieuDaiBanDau = NullableInt(rd, 4),
                        DonViDatCho = "m" };
                    missing.DanhDauLoi("Cuộn lẻ đã nhập không có bản ghi TonCuonLe");
                    rows.Add(missing);
                }
            using (var cmd = Make(conn, tx, OrphanPartialSql, ("@productId", productId)))
            using (var rd = cmd.ExecuteReader())
                while (rd.Read())
                {
                    ct.ThrowIfCancellationRequested();
                    var orphan = new BCTonKho_DongTon {
                        TonCuonLeId = Number(rd, 0), TTThanhPhamId = Number(rd, 1),
                        SanPhamId = productId, TenSanPham = String(rd, 2),
                        MaBinGoc = String(rd, 3), MaHienThi = String(rd, 4),
                        SoDau = NullableInt(rd, 5), SoCuoi = NullableInt(rd, 6),
                        TTCuonDayId = NullableInt(rd, 7), Kieu = BCTonKho_KieuDong.CuonLe,
                        DonViDatCho = "m" };
                    if (orphan.SoDau.HasValue && orphan.SoCuoi.HasValue)
                    {
                        orphan.ChieuDaiConLai = Math.Abs(orphan.SoCuoi.Value - orphan.SoDau.Value);
                        orphan.SoLuong = orphan.ChieuDaiConLai > 0 ? 1 : 0;
                    }
                    orphan.DanhDauLoi("Cuộn lẻ thiếu nguồn nhập kho hợp lệ");
                    rows.Add(orphan);
                }
            return rows;
        }

        private static void ApplyReservations(SQLiteConnection conn, SQLiteTransaction tx, long productId,
            List<BCTonKho_DongTon> rows, CancellationToken ct)
        {
            var qtyByLength = new Dictionary<long, long>();
            using (var cmd = Make(conn, tx, ReserveWholeSql, ("@productId", productId)))
            using (var rd = cmd.ExecuteReader())
                while (rd.Read())
                {
                    ct.ThrowIfCancellationRequested();
                    long length = Number(rd, 1), qty = Number(rd, 2);
                    if (!qtyByLength.ContainsKey(length)) qtyByLength[length] = 0;
                    qtyByLength[length] += qty;
                }
            foreach (var entry in qtyByLength)
            {
                long need = entry.Value;
                if (need <= 0) continue;
                var sources = rows.Where(x => x.Kieu == BCTonKho_KieuDong.CuonChan &&
                    x.ChieuDaiConLai == entry.Key).OrderBy(x => x.NgayNhap, StringComparer.Ordinal)
                    .ThenBy(x => x.TTCuonDayId).ToList();
                if (sources.Count == 0)
                {
                    // Không có nguồn chẵn: giữ dấu vết nhu cầu ngay trong báo cáo thay vì làm mất đặt chỗ.
                    var orphan = new BCTonKho_DongTon { SanPhamId = productId, Kieu = BCTonKho_KieuDong.CuonChan,
                        ChieuDaiBanDau = entry.Key, ChieuDaiConLai = entry.Key, DatCho = need,
                        DatChoDuKien = true, DonViDatCho = "cuộn" };
                    orphan.DanhDauLoi("Kế hoạch đặt chỗ không có nguồn cuộn chẵn phù hợp");
                    rows.Add(orphan);
                    continue;
                }
                foreach (var source in sources)
                {
                    if (need == 0) break;
                    long alloc = Math.Min(need, Math.Max(0, source.SoLuong));
                    source.DatCho += alloc;
                    source.DatChoDuKien = source.DatCho > 0;
                    need -= alloc;
                }
                if (need > 0)
                {
                    var tail = sources[sources.Count - 1];
                    tail.DatCho += need;
                    tail.DatChoDuKien = true;
                    tail.DanhDauLoi("Đặt chỗ vượt tồn cuộn chẵn " + need + " cuộn");
                }
            }
            using (var cmd = Make(conn, tx, ReservePartialSql, ("@productId", productId)))
            using (var rd = cmd.ExecuteReader())
                while (rd.Read())
                {
                    ct.ThrowIfCancellationRequested();
                    long clId = Number(rd, 0);
                    decimal meters = Convert.ToDecimal(rd[1]);
                    var item = rows.FirstOrDefault(x => x.TonCuonLeId == clId);
                    if (item == null) continue;
                    item.DatCho = meters;
                    if (meters < 0 || (item.ChieuDaiConLai.HasValue && meters > item.ChieuDaiConLai.Value))
                        item.DanhDauLoi("Đặt chỗ cuộn lẻ vượt chiều dài còn thực tế");
                }
        }

        // Báo cáo tồn kho B1 -> kế hoạch -> B3. Đường tính MỚI, độc lập
        // với LayChiTietTonAsync, TonKhoExcelReport_DB và các báo cáo cũ.
        // Mọi phép đọc nằm trong cùng một snapshot; tuyệt đối không cập nhật DB.
        internal static List<BCTonKho_ExcelDong> LayTonKhoExcelMoi(bool kemChiTiet = false)
        {
            var cuonTheoQuyCach = new Dictionary<(long SanPhamId, long ChieuDai), BCTonKho_ExcelDong>();
            var cuonLe = new List<BCTonKho_ExcelDong>();
            var cuonLeTheoId = new Dictionary<long, BCTonKho_ExcelDong>();

            using (var conn = DB_Base.OpenConnection())
            using (var tx = conn.BeginTransaction(true))
            {
                const string sqlCuon = @"
SELECT sp.id, COALESCE(sp.Ten_KhongDau,''), COALESCE(sp.Ma,''), COALESCE(sp.DonVi,''),
       cd.id, cd.SoCuon, cd.ChieuDai_1cuon,
       COALESCE(tp.MaBin,''),
       COALESCE((SELECT SUM(ls.SoLuong) FROM LichSuLayCuon ls
                 WHERE ls.TTCuonDay_ID=cd.id),0),
       COALESCE((SELECT COUNT(*) FROM LichSuCat ls
                 JOIN TonCuonLe cl ON cl.id=ls.TonCuonLe_ID
                 WHERE ls.LoaiNguon='CUON_CHAN' AND cl.TTCuonDay_ID=cd.id),0),
       COALESCE(nk.NgayNhapKho,'')
FROM TTCuonDay cd
JOIN TTNhapKhoTP nk ON nk.id=cd.ThongTinNhapKho_ID
JOIN TTThanhPham tp ON tp.id=nk.TTThanhPham_ID
JOIN DanhSachMaSP sp ON sp.id=tp.DanhSachSP_ID
WHERE tp.NhapKho=1 AND tp.Temp=0 AND cd.SoDau IS NULL AND cd.SoCuoi IS NULL
ORDER BY cd.id;";
                using (var cmd = new SQLiteCommand(sqlCuon, conn, tx))
                using (var rd = cmd.ExecuteReader())
                    while (rd.Read())
                    {
                        long spId = Convert.ToInt64(rd[0]);
                        long cdId = Convert.ToInt64(rd[4]);
                        long soCuon = rd.IsDBNull(5) ? 0 : Convert.ToInt64(rd[5]);
                        long chieuDai = rd.IsDBNull(6) ? 0L : Convert.ToInt64(rd[6]);
                        long daLay = Convert.ToInt64(rd[8]);
                        // B3 ghi LichSuCat.LoaiNguon=CUON_CHAN khi bắt đầu cắt từ cuộn nguyên.
                        // Không suy đoán mọi TonCuonLe liên kết đều là cuộn nguyên đã chuyển lẻ:
                        // dữ liệu nhập/migration cũ có thể có SoCuon=0 nhưng vẫn liên kết TonCuonLe.
                        long daChuyenLe = Convert.ToInt64(rd[9]);
                        var key = (spId, chieuDai);
                        if (!cuonTheoQuyCach.TryGetValue(key, out var dong))
                        {
                            dong = new BCTonKho_ExcelDong {
                                DanhSachMaSPId = spId, TenSanPham = Convert.ToString(rd[1]) ?? "",
                                MaSanPham = Convert.ToString(rd[2]) ?? "",
                                DonVi = Convert.ToString(rd[3]) ?? "",
                                Loai = "Cuộn", ChieuDaiMoiCuon = chieuDai,
                                 ChiTiet = kemChiTiet ? new BCTonKho_ChiTietDong(chieuDai) : null
                            };
                            cuonTheoQuyCach.Add(key, dong);
                        }
                        dong.TTCuonDayIds.Add(cdId);
                        long con = soCuon - daLay - daChuyenLe;
                        if (rd.IsDBNull(5) || soCuon < 0 || daLay < 0 || chieuDai <= 0 || con < 0)
                            dong.ThemCanhBao($"TTCuonDay #{cdId}: nhập {soCuon}, lấy {daLay}, chuyển lẻ {daChuyenLe}, chiều dài {chieuDai}; cần đối soát nguồn.");
                        // Tích SoCuon * ChieuDaiMoiCuon được tính tại Model, không ghi trực tiếp số mét.
                        // Không biến số cuộn vật lý thành âm nếu gặp lịch sử bất thường.
                        long soCuonGop = con > 0 && chieuDai > 0 ? con : 0;
                        string maBin = String(rd, 7).Trim();
                        if (soCuonGop > 0)
                        {
                            dong.SoCuon += soCuonGop;
                            dong.ThemMaBin(maBin);
                        }
                        dong.ChiTiet?.ThemNguon(new BCTonKho_NguonTon
                        {
                            TTCuonDayId = cdId, MaBin = maBin, NgayNhapKho = String(rd, 10), SoCuonNhap = soCuon,
                            SoCuonDaLay = daLay, SoCuonChuyenLe = daChuyenLe, SoCuonGop = soCuonGop
                        });
                    }

                const string sqlLe = @"
SELECT cl.id, sp.id, COALESCE(sp.Ten_KhongDau,''), COALESCE(sp.Ma,''),
       COALESCE(sp.DonVi,''), cl.SoDau, cl.SoCuoi, COALESCE(cl.TrangThai,''),
       cl.TTCuonDay_ID, cd.id, nk.id, tpNguon.id, tp.id,
       cd.SoDau, cd.SoCuoi, COALESCE(cl.MaCuon,''), tpNguon.NhapKho, tpNguon.Temp,
       COALESCE(tpNguon.MaBin, tp.MaBin, '')
FROM TonCuonLe cl
JOIN TTThanhPham tp ON tp.id=cl.TTThanhPham_ID
JOIN DanhSachMaSP sp ON sp.id=tp.DanhSachSP_ID
LEFT JOIN TTCuonDay cd ON cd.id=cl.TTCuonDay_ID
LEFT JOIN TTNhapKhoTP nk ON nk.id=cd.ThongTinNhapKho_ID
LEFT JOIN TTThanhPham tpNguon ON tpNguon.id=nk.TTThanhPham_ID
WHERE tp.NhapKho=1 AND tp.Temp=0
ORDER BY cl.id;";
                using (var cmd = new SQLiteCommand(sqlLe, conn, tx))
                using (var rd = cmd.ExecuteReader())
                    while (rd.Read())
                    {
                        long id = Convert.ToInt64(rd[0]);
                        long? dau = rd.IsDBNull(5) ? (long?)null : Convert.ToInt64(rd[5]);
                        long? cuoi = rd.IsDBNull(6) ? (long?)null : Convert.ToInt64(rd[6]);
                        var dong = new BCTonKho_ExcelDong {
                            TonCuonLeId = id, DanhSachMaSPId = Convert.ToInt64(rd[1]),
                            TenSanPham = Convert.ToString(rd[2]) ?? "",
                            MaSanPham = Convert.ToString(rd[3]) ?? "",
                            DonVi = Convert.ToString(rd[4]) ?? "", Loai = "Lẻ",
                            MaBinLot = Convert.ToString(rd[15]) ?? "",
                            LotNguon = String(rd, 18),
                            SoDau = dau, SoCuoi = cuoi,
                            SoCuon = 1, // Mỗi TonCuonLe là đúng một cuộn lẻ.
                            ThieuChieuDaiHienTai = !dau.HasValue || !cuoi.HasValue,
                            ChiTiet = kemChiTiet ? new BCTonKho_ChiTietDong() : null
                        };
                        if (dau.HasValue && cuoi.HasValue)
                            dong.ChieuDaiMoiCuon = checked((long)Math.Abs((decimal)cuoi.Value - dau.Value));
                        else
                            dong.ThemCanhBao($"TonCuonLe #{id}: thiếu số đầu/số cuối, không thể xác định chiều dài thực tế.");
                        string trangThai = (Convert.ToString(rd[7]) ?? "").Trim().ToUpperInvariant();
                        if (trangThai != "ACTIVE" && trangThai != "HET")
                            dong.ThemCanhBao($"TonCuonLe #{id}: trạng thái không hợp lệ ({trangThai}).");
                        if ((trangThai == "ACTIVE" && dong.ChieuDaiTonThucTe == 0) ||
                            (trangThai == "HET" && dong.ChieuDaiTonThucTe > 0))
                            dong.ThemCanhBao($"TonCuonLe #{id}: trạng thái không khớp chiều dài còn lại.");
                        if (rd.IsDBNull(9) || rd.IsDBNull(10) || rd.IsDBNull(11) ||
                            Convert.ToInt64(rd[11]) != Convert.ToInt64(rd[12]))
                            dong.ThemCanhBao($"TonCuonLe #{id}: nguồn nhập kho không tồn tại hoặc khác chủ sở hữu.");
                        if (!rd.IsDBNull(9) && (rd.IsDBNull(13) != rd.IsDBNull(14)))
                            dong.ThemCanhBao($"TonCuonLe #{id}: nguồn nhập có một mốc đầu/cuối bị thiếu.");
                        if (rd.IsDBNull(16) || rd.IsDBNull(17) ||
                            Convert.ToInt32(rd[16]) != 1 || Convert.ToInt32(rd[17]) != 0)
                            dong.ThemCanhBao($"TonCuonLe #{id}: nguồn nhập không còn hợp lệ.");
                        if (string.IsNullOrWhiteSpace(Convert.ToString(rd[15])))
                            dong.ThemCanhBao($"TonCuonLe #{id}: chưa có mã cuộn.");
                        cuonLe.Add(dong);
                        cuonLeTheoId[id] = dong;
                    }

                // Cuộn lẻ nhập thẳng qua B1 mà bản ghi TonCuonLe bị mất.
                // Không lấy dấu mốc nhập ban đầu làm tồn hiện tại (vì có thể đã cắt).
                const string sqlMatLe = @"
SELECT sp.id, COALESCE(sp.Ten_KhongDau,''), COALESCE(sp.Ma,''), COALESCE(sp.DonVi,''), cd.id, COALESCE(tp.MaBin,'')
FROM TTCuonDay cd
JOIN TTNhapKhoTP nk ON nk.id=cd.ThongTinNhapKho_ID
JOIN TTThanhPham tp ON tp.id=nk.TTThanhPham_ID
JOIN DanhSachMaSP sp ON sp.id=tp.DanhSachSP_ID
WHERE tp.NhapKho=1 AND tp.Temp=0
  AND (cd.SoDau IS NOT NULL OR cd.SoCuoi IS NOT NULL)
  AND NOT EXISTS (SELECT 1 FROM TonCuonLe cl WHERE cl.TTCuonDay_ID=cd.id);";
                using (var cmd = new SQLiteCommand(sqlMatLe, conn, tx))
                using (var rd = cmd.ExecuteReader())
                    while (rd.Read())
                    {
                        var dong = new BCTonKho_ExcelDong {
                            DanhSachMaSPId = Convert.ToInt64(rd[0]),
                            TenSanPham = Convert.ToString(rd[1]) ?? "",
                            MaSanPham = Convert.ToString(rd[2]) ?? "",
                            DonVi = Convert.ToString(rd[3]) ?? "", Loai = "Lẻ",
                            MaBinLot = Convert.ToString(rd[5]) ?? "",
                            ThieuChieuDaiHienTai = true,
                            ChiTiet = kemChiTiet ? new BCTonKho_ChiTietDong() : null
                        };
                        dong.ThemCanhBao($"TTCuonDay #{Convert.ToInt64(rd[4])}: thiếu TonCuonLe; không đủ dữ liệu xác định tồn thực tế.");
                        cuonLe.Add(dong);
                    }

                const string sqlDatCuon = @"
WITH NhuCau AS (
SELECT kh.DanhSachMaSP_ID, qc.ChieuDai1Cuon,
       MAX(0, qc.SoLuongCuonCanLay - COALESCE((SELECT SUM(ls.SoLuong)
           FROM LichSuLayCuon ls WHERE ls.QuyCachCuonChan_ID=qc.id),0)) AS CanLay,
       (SELECT COUNT(*) FROM KeHoachCatNhom n
          WHERE n.QuyCachCuonChan_ID=qc.id AND n.TonCuonLe_ID IS NULL
            AND EXISTS (SELECT 1 FROM KeHoachCatChiTiet ct
                        WHERE ct.KeHoachCatNhom_ID=n.id AND
                        NOT EXISTS (SELECT 1 FROM LichSuCat ls
                                    WHERE ls.KeHoachCatChiTiet_ID=ct.id))) AS CanCat,
       COALESCE(sp.Ten_KhongDau,''), COALESCE(sp.Ma,''), COALESCE(sp.DonVi,''),
       k.id, COALESCE(k.MaKeHoach,'')
 FROM KeHoachHangQuyCachCuonChan qc
JOIN KeHoachHang kh ON kh.id=qc.KeHoachHang_ID
JOIN KeHoach k ON k.id=kh.KeHoach_ID
JOIN DanhSachMaSP sp ON sp.id=kh.DanhSachMaSP_ID
WHERE k.TrangThai='ACTIVE'
)
SELECT NhuCau.*, CanLay + CanCat FROM NhuCau;";
                using (var cmd = new SQLiteCommand(sqlDatCuon, conn, tx))
                using (var rd = cmd.ExecuteReader())
                    while (rd.Read())
                    {
                        long spId = Convert.ToInt64(rd[0]);
                        long chieuDai = rd.IsDBNull(1) ? 0L : Convert.ToInt64(rd[1]);
                        long canCat = Number(rd, 3);
                        long soCuon = Number(rd, 9);
                        // Giữ cách báo cáo làm tròn tổng nhu cầu của dữ liệu cũ.
                        long canLay = soCuon - canCat;
                        var key = (spId, chieuDai);
                        if (!cuonTheoQuyCach.TryGetValue(key, out var dong))
                        {
                            dong = new BCTonKho_ExcelDong {
                                DanhSachMaSPId = spId,
                                TenSanPham = String(rd, 4),
                                MaSanPham = String(rd, 5), DonVi = String(rd, 6),
                                Loai = "Cuộn", ChieuDaiMoiCuon = chieuDai,
                                 ChiTiet = kemChiTiet ? new BCTonKho_ChiTietDong(chieuDai) : null
                            };
                            cuonTheoQuyCach[key] = dong;
                        }
                        if (soCuon < 0 || chieuDai <= 0)
                            dong.ThemCanhBao($"Kế hoạch cuộn chẵn: số lượng {soCuon}, chiều dài {chieuDai} không hợp lệ.");
                        if (soCuon > 0 && chieuDai > 0)
                        {
                            decimal soMet = (decimal)soCuon * chieuDai;
                            dong.ChieuDaiTonDatTruoc += soMet;
                            dong.ChiTiet?.ThemDatTruoc(new BCTonKho_DatTruocKeHoach
                            {
                                KeHoachId = Number(rd, 7), MaKeHoach = String(rd, 8),
                                SoCuonConPhaiLay = canLay, SoCuonDanhDeCat = canCat, SoMet = soMet
                            });
                        }
                    }

                // Giữ tổng theo SUM của SQLite cho cả dữ liệu cũ có chiều dài thập phân.
                const string sqlDatLe = @"
SELECT n.TonCuonLe_ID, kh.DanhSachMaSP_ID, COALESCE(SUM(ct.ChieuDaiCanCat),0),
       k.id, COALESCE(k.MaKeHoach,'')" + PendingPartialSegmentsSourceSql + @"
GROUP BY n.TonCuonLe_ID, kh.DanhSachMaSP_ID, k.id;";
                using (var cmd = new SQLiteCommand(sqlDatLe, conn, tx))
                {
                    cmd.Parameters.AddWithValue("@cuonId", DBNull.Value);
                    using (var rd = cmd.ExecuteReader())
                        while (rd.Read())
                        {
                            long clId = Convert.ToInt64(rd[0]);
                            long spId = Convert.ToInt64(rd[1]);
                            decimal datTruoc = Convert.ToDecimal(rd[2]);
                            if (!cuonLeTheoId.TryGetValue(clId, out var dong))
                                throw new InvalidOperationException($"Kế hoạch còn đặt trước cho TonCuonLe #{clId}, nhưng cuộn không tìm thấy trong nguồn nhập kho hợp lệ.");
                            if (dong.DanhSachMaSPId != spId)
                                throw new InvalidOperationException($"TonCuonLe #{clId}: sản phẩm ở kế hoạch không khớp sản phẩm của cuộn.");
                            dong.ChieuDaiTonDatTruoc += datTruoc;
                            if (datTruoc != 0)
                                dong.ChiTiet?.ThemDatTruoc(new BCTonKho_DatTruocKeHoach
                                {
                                    KeHoachId = Number(rd, 3), MaKeHoach = String(rd, 4), SoMet = datTruoc
                                });
                        }
                }

                foreach (var dong in cuonLeTheoId.Values)
                    if (dong.ChieuDaiTonDatTruoc < 0)
                        dong.ThemCanhBao($"TonCuonLe #{dong.TonCuonLeId}: chiều dài đặt trước âm.");

                if (kemChiTiet)
                {
                    DocDoanDatTruoc(conn, tx, cuonLeTheoId);
                    DocLichSuCat(conn, tx, cuonTheoQuyCach.Values, cuonLeTheoId);
                }

                tx.Commit();
            }

            var ketQua = cuonTheoQuyCach.Values.Concat(cuonLe)
                .Where(x => x.ChieuDaiTonThucTe != 0 || x.ChieuDaiTonDatTruoc != 0 || x.LaBatThuong)
                .OrderBy(x => x.TenSanPham, StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(x => x.MaSanPham, StringComparer.Ordinal)
                .ThenBy(x => x.Loai, StringComparer.Ordinal)
                .ThenBy(x => x.ChieuDaiMoiCuon)
                .ThenBy(x => x.TonCuonLeId)
                .ToList();
            foreach (var dong in ketQua)
                if (dong.ChieuDaiTonKhaDung < 0)
                    dong.ThemCanhBao("Chiều dài đặt trước lớn hơn chiều dài thực tế.");
            return ketQua;
        }

        // Dòng tìm kiếm theo LOT/nguồn/1 cuộn có phạm vi khác báo cáo gộp quy cách.
        // Tái sử dụng các số liệu mà luồng tìm kiếm đã tính, bổ sung lịch sử đúng ID.
        internal static BCTonKho_ExcelDong LayChiTietDongTimKiem(BCTonKho_DongTon item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            bool cuonChan = item.Kieu == BCTonKho_KieuDong.CuonChan;
            var dong = item.TaoDongBaoCao();
            var chiTiet = new BCTonKho_ChiTietDong(cuonChan ? dong.ChieuDaiMoiCuon : 0);
            dong.ChiTiet = chiTiet;
            using (var conn = DB_Base.OpenConnection())
            using (var tx = conn.BeginTransaction(IsolationLevel.ReadCommitted))
            {
                if (cuonChan && item.TTCuonDayId.HasValue)
                {
                    long sourceId = item.TTCuonDayId.Value;
                    dong.TTCuonDayIds.Add(sourceId);
                    using (var cmd = Make(conn, tx, @"
SELECT COALESCE(cd.SoCuon,0),
       COALESCE((SELECT SUM(ls.SoLuong) FROM LichSuLayCuon ls WHERE ls.TTCuonDay_ID=cd.id),0),
       (SELECT COUNT(DISTINCT lc.TonCuonLe_ID) FROM LichSuCat lc
        JOIN TonCuonLe cl ON cl.id=lc.TonCuonLe_ID
        WHERE lc.LoaiNguon='CUON_CHAN' AND cl.TTCuonDay_ID=cd.id),
       COALESCE(nk.NgayNhapKho,'')
FROM TTCuonDay cd LEFT JOIN TTNhapKhoTP nk ON nk.id=cd.ThongTinNhapKho_ID
WHERE cd.id=@id;", ("@id", sourceId)))
                    using (var rd = cmd.ExecuteReader())
                        if (rd.Read()) chiTiet.ThemNguon(new BCTonKho_NguonTon
                        {
                            TTCuonDayId = sourceId, MaBin = item.MaBinGoc,
                            SoCuonNhap = Number(rd, 0), SoCuonDaLay = Number(rd, 1),
                            SoCuonChuyenLe = Number(rd, 2), NgayNhapKho = String(rd, 3),
                            SoCuonGop = Math.Max(0, item.SoLuong)
                        });
                }
                var cuonLeTheoId = new Dictionary<long, BCTonKho_ExcelDong>();
                if (!cuonChan && item.TonCuonLeId.HasValue)
                {
                    cuonLeTheoId.Add(item.TonCuonLeId.Value, dong);
                    DocDoanDatTruoc(conn, tx, cuonLeTheoId, item.TonCuonLeId);
                }
                DocLichSuCat(conn, tx,
                    cuonChan ? new[] { dong } : Array.Empty<BCTonKho_ExcelDong>(), cuonLeTheoId);
                tx.Commit();
            }
            return dong;
        }

        private static void DocDoanDatTruoc(SQLiteConnection conn, SQLiteTransaction tx,
            Dictionary<long, BCTonKho_ExcelDong> cuonLeTheoId, long? cuonId = null)
        {
            if (cuonLeTheoId.Count == 0) return;
            using var cmd = Make(conn, tx, PendingPartialSegmentsSql, ("@cuonId", (object?)cuonId ?? DBNull.Value));
            using var rd = cmd.ExecuteReader();
            while (rd.Read())
            {
                if (!cuonLeTheoId.TryGetValue(Number(rd, 0), out var dong) || dong.DanhSachMaSPId != Number(rd, 1))
                    continue;
                dong.ChiTiet?.ThemDoanDatTruoc(new BCTonKho_DoanDatTruoc
                {
                    ChieuDai = rd.IsDBNull(2) ? (decimal?)null : Convert.ToDecimal(rd[2]),
                    KeHoachId = Number(rd, 3), MaKeHoach = String(rd, 4), KeHoachCatChiTietId = Number(rd, 5)
                });
            }
        }

        private static void DocLichSuCat(SQLiteConnection conn, SQLiteTransaction tx,
            IEnumerable<BCTonKho_ExcelDong> cuonChan, Dictionary<long, BCTonKho_ExcelDong> cuonLe)
        {
            var cuonChanTheoNguon = cuonChan
                .SelectMany(dong => dong.TTCuonDayIds.Select(id => (Id: id, Dong: dong)))
                .ToDictionary(x => x.Id, x => x.Dong);
            const string sql = @"
SELECT ls.id, cl.id, cd.id, cl.MaCuon, ls.LoaiNguon,
       ls.ThoiGianThucHien, ls.NguoiThucHien, k.MaKeHoach,
       CASE WHEN typeof(ls.ChieuDaiCat) = 'integer' THEN ls.ChieuDaiCat END,
       CASE WHEN typeof(ls.SoDauTruoc) = 'integer' THEN ls.SoDauTruoc END,
       CASE WHEN typeof(ls.SoCuoiTruoc) = 'integer' THEN ls.SoCuoiTruoc END,
       CASE WHEN typeof(ls.SoDauCat) = 'integer' THEN ls.SoDauCat END,
       CASE WHEN typeof(ls.SoCuoiCat) = 'integer' THEN ls.SoCuoiCat END,
       CASE WHEN typeof(ls.SoDauSau) = 'integer' THEN ls.SoDauSau END,
       CASE WHEN typeof(ls.SoCuoiSau) = 'integer' THEN ls.SoCuoiSau END
FROM LichSuCat ls
JOIN TonCuonLe cl ON cl.id=ls.TonCuonLe_ID
LEFT JOIN TTThanhPham chuCuon ON chuCuon.id=cl.TTThanhPham_ID
LEFT JOIN TTCuonDay cd ON cd.id=cl.TTCuonDay_ID
LEFT JOIN TTNhapKhoTP nk ON nk.id=cd.ThongTinNhapKho_ID
LEFT JOIN TTThanhPham nguon ON nguon.id=nk.TTThanhPham_ID
LEFT JOIN KeHoachCatChiTiet ct ON ct.id=ls.KeHoachCatChiTiet_ID
LEFT JOIN KeHoachCatNhom n ON n.id=ct.KeHoachCatNhom_ID
LEFT JOIN KeHoachHang kh ON kh.id=n.KeHoachHang_ID
LEFT JOIN KeHoach k ON k.id=kh.KeHoach_ID
WHERE (chuCuon.NhapKho=1 AND chuCuon.Temp=0)
   OR (nguon.NhapKho=1 AND nguon.Temp=0 AND cd.SoDau IS NULL AND cd.SoCuoi IS NULL)
ORDER BY ls.ThoiGianThucHien DESC, ls.id DESC;";
            using var cmd = new SQLiteCommand(sql, conn, tx);
            using var rd = cmd.ExecuteReader();
            while (rd.Read())
            {
                long cuonId = Number(rd, 1);
                long? nguonId = NullableInt(rd, 2);
                BCTonKho_ExcelDong? dongChan = null;
                if (nguonId.HasValue)
                    cuonChanTheoNguon.TryGetValue(nguonId.Value, out dongChan);
                cuonLe.TryGetValue(cuonId, out var dongLe);
                if (dongChan == null && dongLe == null) continue;

                var lanCat = new BCTonKho_LanCat
                {
                    Id = Number(rd, 0), TonCuonLeId = cuonId, TTCuonDayId = nguonId,
                    MaCuon = String(rd, 3), LoaiNguon = String(rd, 4),
                    ThoiGian = String(rd, 5), NguoiThucHien = String(rd, 6),
                    MaKeHoach = String(rd, 7), ChieuDaiCat = NullableInt(rd, 8),
                    SoDauTruoc = NullableInt(rd, 9), SoCuoiTruoc = NullableInt(rd, 10),
                    SoDauCat = NullableInt(rd, 11), SoCuoiCat = NullableInt(rd, 12),
                    SoDauSau = NullableInt(rd, 13), SoCuoiSau = NullableInt(rd, 14)
                };
                dongChan?.ChiTiet?.ThemLanCat(lanCat);
                dongLe?.ChiTiet?.ThemLanCat(lanCat);
            }
        }

        private static SQLiteCommand Make(SQLiteConnection c, SQLiteTransaction t, string sql,
            params (string Name, object Value)[] parameters)
        {
            var cmd = new SQLiteCommand(sql, c, t);
            foreach (var p in parameters) cmd.Parameters.AddWithValue(p.Name, p.Value ?? DBNull.Value);
            return cmd;
        }
        private static string LikePattern(string text) => "%" + text.Replace("\\", "\\\\")
            .Replace("%", "\\%").Replace("_", "\\_") + "%";
        private static string String(SQLiteDataReader rd, int pos) => rd.IsDBNull(pos) ? "" :
            Convert.ToString(rd.GetValue(pos), CultureInfo.InvariantCulture) ?? "";
        private static long Number(SQLiteDataReader rd, int pos) => rd.IsDBNull(pos) ? 0 :
            Convert.ToInt64(rd.GetValue(pos), CultureInfo.InvariantCulture);
        private static long? NullableInt(SQLiteDataReader rd, int pos) => rd.IsDBNull(pos) ? (long?)null :
            Convert.ToInt64(rd.GetValue(pos), CultureInfo.InvariantCulture);
    }
}
