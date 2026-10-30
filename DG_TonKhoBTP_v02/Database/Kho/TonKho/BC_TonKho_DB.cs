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
GROUP BY n.TonCuonLe_ID;";

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
SELECT DISTINCT tp.DanhSachSP_ID FROM TTThanhPham tp
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
            var productIds = new List<long>();
            using (var cmd = Make(conn, tx, ProductIdsSql, ("@loai", (int)loai),
                ("@id", (object)id ?? DBNull.Value), ("@name", giaTri ?? "")))
            using (var rd = cmd.ExecuteReader())
                while (rd.Read()) { ct.ThrowIfCancellationRequested(); productIds.Add(rd.GetInt64(0)); }
            var rows = new List<BCTonKho_DongTon>();
            foreach (var productId in productIds)
            {
                ct.ThrowIfCancellationRequested();
                var group = ReadProduct(conn, tx, productId, ct);
                ApplyReservations(conn, tx, productId, group, ct);
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
                    long clId = Number(rd, 0), meters = Number(rd, 1);
                    var item = rows.FirstOrDefault(x => x.TonCuonLeId == clId);
                    if (item == null) continue;
                    item.DatCho = meters;
                    if (meters < 0 || (item.ChieuDaiConLai.HasValue && meters > item.ChieuDaiConLai.Value))
                        item.DanhDauLoi("Đặt chỗ cuộn lẻ vượt chiều dài còn thực tế");
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
