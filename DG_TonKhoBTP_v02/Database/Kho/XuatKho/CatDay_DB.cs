using DG_TonKhoBTP_v02.Models.Kho.XuatKho;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DG_TonKhoBTP_v02.Database.Kho.XuatKho
{
    internal static class CatDay_DB
    {
        private const int SuggestionLimit = 50;
        private static readonly object LenLenhLogLock = new object();

        /// <summary>
        /// Ghi log thực tế ra file Logs\CatDay.log. Lỗi ghi log không được phép làm hỏng
        /// nghiệp vụ chính; khi không ghi được file thì fallback sang Trace.
        /// </summary>
        internal static void GhiLogLenLenh(string mucDo, string noiDung, Exception ex = null)
        {
            string dong =
                $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{(mucDo ?? "INFO").ToUpperInvariant()}] " +
                $"[CatDay][LenLenh] {noiDung ?? string.Empty}" +
                (ex == null ? string.Empty : $" | Exception={ex}") +
                Environment.NewLine;

            Exception primaryError = null;
            try
            {
                GhiDongLogVaoThuMuc(
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs"),
                    dong);
                return;
            }
            catch (Exception exPrimary)
            {
                primaryError = exPrimary;
            }

            try
            {
                // Fallback khi thư mục cài đặt không có quyền ghi.
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                GhiDongLogVaoThuMuc(
                    Path.Combine(localAppData, "DG_TonKhoBTP_v02", "Logs"),
                    dong);
            }
            catch (Exception fallbackError)
            {
                Trace.TraceError(
                    $"[CatDay][LenLenh][LogFallback] {noiDung}; " +
                    $"PrimaryLogError={primaryError}; FallbackLogError={fallbackError}");
            }
        }

        private static void GhiDongLogVaoThuMuc(string logDir, string dong)
        {
            Directory.CreateDirectory(logDir);
            string logPath = Path.Combine(logDir, "CatDay.log");
            lock (LenLenhLogLock)
            {
                File.AppendAllText(logPath, dong, Encoding.UTF8);
            }
        }

        /// <summary>
        /// Search-as-you-type cho cbxTimKiem.
        /// Chỉ trả về LOT / Tên SP / Khách hàng có ít nhất một TTCuonDay CÒN TỒN.
        /// Tồn được tính theo đúng 2 nhóm:
        /// - SoDau/SoCuoi đều NULL: tồn theo số lượng.
        /// - SoDau/SoCuoi đều có giá trị: tồn theo chiều dài.
        /// </summary>
        public static Task<DataTable> TimKiemGiaTriAsync(
            CatDay_SearchType searchType,
            string keyword,
            CancellationToken ct)
        {
            keyword = keyword?.Trim() ?? string.Empty;

            return Task.Run(() =>
            {
                ct.ThrowIfCancellationRequested();

                if (searchType == CatDay_SearchType.ChieuDai)
                    return TaoBangSuggestionRong();

                string keywordForQuery = searchType == CatDay_SearchType.KhachHang
                    ? ChuyenKhongDau(keyword)
                    : keyword;

                string sql = XayDungSqlSuggestion(searchType);

                DataTable dt = new DataTable();
                using var conn = DB_Base.OpenConnection();
                using var cmd = new SQLiteCommand(sql, conn);
                cmd.Parameters.AddWithValue("@kw", TaoLikeContains(keywordForQuery));
                cmd.Parameters.AddWithValue("@limit", SuggestionLimit);

                ct.ThrowIfCancellationRequested();

                using var adapter = new SQLiteDataAdapter(cmd);
                adapter.Fill(dt);

                ct.ThrowIfCancellationRequested();
                return dt;
            }, ct);
        }

        /// <summary>
        /// Tìm theo điều kiện UI, nhưng chỉ trả về TTCuonDay còn tồn.
        /// Ngày tìm kiếm luôn dựa trên TTCuonDay.Ngay.
        /// </summary>
        public static CatDay_SearchResult TimKiem(CatDay_SearchCriteria criteria)
        {
            if (criteria == null)
                throw new ArgumentNullException(nameof(criteria));

            criteria.LayToanBo = false;
            return ThucThiTimKiem(criteria);
        }

        /// <summary>
        /// Lấy toàn bộ TTCuonDay còn tồn theo quy tắc 2 nhóm.
        /// </summary>
        public static CatDay_SearchResult LayToanBo()
        {
            return ThucThiTimKiem(new CatDay_SearchCriteria
            {
                LayToanBo = true
            });
        }

        private static CatDay_SearchResult ThucThiTimKiem(CatDay_SearchCriteria criteria)
        {
            string sql = XayDungSql(criteria);
            var result = new CatDay_SearchResult();

            using var conn = DB_Base.OpenConnection();
            using var cmd = new SQLiteCommand(sql, conn);
            GanThamSo(cmd, criteria);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                long ttCuonDayId = Convert.ToInt64(reader["TTCuonDay_ID"], CultureInfo.InvariantCulture);
                string lot = DbText(reader["Lot"]);

                long? soDauGoc = DbInt64Nullable(reader["SoDau"]);
                long? soCuoiGoc = DbInt64Nullable(reader["SoCuoi"]);
                long? soCuonGoc = DbInt64Nullable(reader["SoCuon"]);
                long chieuDaiMotDonVi = Convert.ToInt64(reader["ChieuDai_1cuon"], CultureInfo.InvariantCulture);
                long tongChieuDaiCat = Convert.ToInt64(reader["TongChieuDaiCat"], CultureInfo.InvariantCulture);
                long tongSoLuong = Convert.ToInt64(reader["TongSoLuong"], CultureInfo.InvariantCulture);
                bool coChieuDaiCatAm = Convert.ToInt32(reader["CoChieuDaiCatAm"], CultureInfo.InvariantCulture) != 0;
                bool coSoLuongAm = Convert.ToInt32(reader["CoSoLuongAm"], CultureInfo.InvariantCulture) != 0;

                // Chỉ một trong SoDau/SoCuoi NULL là dữ liệu không thuộc 2 nhóm hợp lệ.
                if (soDauGoc.HasValue != soCuoiGoc.HasValue)
                {
                    ThemDataIssue(
                        result,
                        ttCuonDayId,
                        lot,
                        null,
                        "TTCuonDay chỉ có một trong SoDau/SoCuoi; không xác định được nhóm quản lý tồn.");
                    continue;
                }

                string loai = reader["TTLo_ID"] == DBNull.Value ? "Cuộn" : "Lô";
                string khachHang = DbText(reader["KhachHang"]);

                // NHÓM SỐ LƯỢNG: SoDau và SoCuoi đều NULL.
                if (!soDauGoc.HasValue && !soCuoiGoc.HasValue)
                {
                    if (coSoLuongAm)
                    {
                        ThemDataIssue(
                            result,
                            ttCuonDayId,
                            lot,
                            null,
                            "Có LichSuCatDay.SoLuong âm; đã loại toàn bộ TTCuonDay khỏi kết quả.");
                        continue;
                    }

                    if (!soCuonGoc.HasValue || soCuonGoc.Value <= 0)
                    {
                        ThemDataIssue(
                            result,
                            ttCuonDayId,
                            lot,
                            soCuonGoc,
                            "TTCuonDay.SoCuon không hợp lệ cho nhóm quản lý theo số lượng.");
                        continue;
                    }

                    if (chieuDaiMotDonVi <= 0)
                    {
                        ThemDataIssue(
                            result,
                            ttCuonDayId,
                            lot,
                            chieuDaiMotDonVi,
                            "TTCuonDay.ChieuDai_1cuon phải lớn hơn 0 đối với nhóm quản lý theo số lượng.");
                        continue;
                    }

                    long soCuonCon = soCuonGoc.Value - tongSoLuong;
                    if (soCuonCon < 0)
                    {
                        ThemDataIssue(
                            result,
                            ttCuonDayId,
                            lot,
                            soCuonCon,
                            "Số cuộn còn lại âm: TTCuonDay.SoCuon - SUM(LichSuCatDay.SoLuong) < 0.");
                        continue;
                    }

                    // Chỉ hiển thị khi còn tồn.
                    if (soCuonCon == 0)
                        continue;

                    // Tìm theo chiều dài ở nhóm số lượng dùng ChieuDai_1cuon.
                    if (criteria.SearchType == CatDay_SearchType.ChieuDai
                        && criteria.ChieuDaiToiThieu.HasValue
                        && chieuDaiMotDonVi < criteria.ChieuDaiToiThieu.Value)
                    {
                        continue;
                    }

                    if (soCuonCon > int.MaxValue)
                    {
                        ThemDataIssue(
                            result,
                            ttCuonDayId,
                            lot,
                            soCuonCon,
                            "Số cuộn còn lại vượt phạm vi dữ liệu mà giao diện có thể hiển thị.");
                        continue;
                    }

                    result.Rows.Add(new CatDay_Row
                    {
                        TTCuonDay_ID = ttCuonDayId,
                        Lot = lot,
                        TenSP = DbText(reader["TenSP"]),
                        KhachHang = khachHang,
                        Loai = loai,
                        NhomTon = CatDay_InventoryGroup.SoLuong,
                        SoCuon = (int)soCuonCon,
                        SoDau = null,
                        SoCuoi = null,
                        ChieuDaiConLai = chieuDaiMotDonVi,
                        TongChieuDai = soCuonCon * chieuDaiMotDonVi
                    });

                    continue;
                }

                // NHÓM CHIỀU DÀI: SoDau và SoCuoi đều có giá trị.
                if (coChieuDaiCatAm)
                {
                    ThemDataIssue(
                        result,
                        ttCuonDayId,
                        lot,
                        null,
                        "Có LichSuCatDay.ChieuDaiCat âm; đã loại toàn bộ TTCuonDay khỏi kết quả.");
                    continue;
                }

                // Đã chốt: nhóm chiều dài phải có đúng 1 cuộn.
                if (!soCuonGoc.HasValue || soCuonGoc.Value != 1)
                {
                    ThemDataIssue(
                        result,
                        ttCuonDayId,
                        lot,
                        soCuonGoc,
                        "Nhóm quản lý theo chiều dài nhưng TTCuonDay.SoCuon khác 1.");
                    continue;
                }

                long soDau = soDauGoc.Value;
                long soCuoi = soCuoiGoc.Value;

                // SoDau == SoCuoi được coi là hết tồn, không cảnh báo.
                if (soDau == soCuoi)
                    continue;

                long chieuDaiGoc = Math.Abs(soCuoi - soDau);
                long chieuDaiCon = chieuDaiGoc - tongChieuDaiCat;

                if (chieuDaiCon < 0)
                {
                    ThemDataIssue(
                        result,
                        ttCuonDayId,
                        lot,
                        chieuDaiCon,
                        "Chiều dài còn lại âm: ABS(SoCuoi - SoDau) - SUM(ChieuDaiCat) < 0.");
                    continue;
                }

                // Chỉ hiển thị khi còn tồn.
                if (chieuDaiCon == 0)
                    continue;

                // Tìm theo chiều dài ở nhóm chiều dài dùng chiều dài CÒN LẠI.
                if (criteria.SearchType == CatDay_SearchType.ChieuDai
                    && criteria.ChieuDaiToiThieu.HasValue
                    && chieuDaiCon < criteria.ChieuDaiToiThieu.Value)
                {
                    continue;
                }

                int heSo = soDau < soCuoi ? 1 : -1;
                long soCuoiHienThi = soCuoi - (heSo * tongChieuDaiCat);

                result.Rows.Add(new CatDay_Row
                {
                    TTCuonDay_ID = ttCuonDayId,
                    Lot = lot,
                    TenSP = DbText(reader["TenSP"]),
                    KhachHang = khachHang,
                    Loai = loai,
                    NhomTon = CatDay_InventoryGroup.ChieuDai,
                    SoCuon = 1,
                    SoDau = soDau,
                    SoCuoi = soCuoiHienThi,
                    ChieuDaiConLai = chieuDaiCon,
                    TongChieuDai = chieuDaiCon
                });
            }

            return result;
        }


        /// <summary>
        /// Lên lệnh Cắt/Lấy cho một batch. Mỗi input tạo tối đa một LichSuCatDay.
        /// Toàn batch dùng chung một connection + transaction; lỗi nghiệp vụ theo dòng
        /// không rollback các dòng hợp lệ, còn lỗi kỹ thuật DB sẽ rollback toàn bộ.
        /// </summary>
        public static CatDay_LenLenhResult LenLenh(
            IReadOnlyCollection<CatDay_LenLenhInput> inputs,
            string nguoiLenLenh,
            string ngayLenLenh)
        {
            if (inputs == null)
                throw new ArgumentNullException(nameof(inputs));

            var result = new CatDay_LenLenhResult();
            if (inputs.Count == 0)
                return result;

            var duplicateIds = new HashSet<long>(
                inputs
                    .Where(x => x != null && x.TTCuonDay_ID > 0)
                    .GroupBy(x => x.TTCuonDay_ID)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key));

            foreach (long id in duplicateIds)
            {
                ThemLoiLenLenh(
                    result,
                    id,
                    "TTCuonDay_ID xuất hiện nhiều hơn một dòng trong cùng batch; không được gộp hoặc xử lý tuần tự.",
                    null,
                    false);
            }

            List<CatDay_LenLenhInput> canXuLy = inputs
                .Where(x => x != null && !duplicateIds.Contains(x.TTCuonDay_ID))
                .ToList();

            if (canXuLy.Count == 0)
                return result;

            const string sqlTrangThai = @"
                SELECT
                    cd.id AS TTCuonDay_ID,
                    COALESCE(tp.MaBin, '') AS Lot,
                    nk.TenSP AS TenSP,
                    cd.KhachHang AS KhachHang,
                    cd.TTLo_ID AS TTLo_ID,
                    cd.SoCuon AS SoCuon,
                    cd.ChieuDai_1cuon AS ChieuDai_1cuon,
                    cd.SoDau AS SoDau,
                    cd.SoCuoi AS SoCuoi,
                    COALESCE(SUM(CASE WHEN lscd.ChieuDaiCat IS NULL THEN 0 ELSE lscd.ChieuDaiCat END), 0) AS TongChieuDaiCat,
                    COALESCE(SUM(CASE WHEN lscd.SoLuong IS NULL THEN 0 ELSE lscd.SoLuong END), 0) AS TongSoLuong,
                    MAX(CASE WHEN lscd.ChieuDaiCat < 0 THEN 1 ELSE 0 END) AS CoChieuDaiCatAm,
                    MAX(CASE WHEN lscd.SoLuong < 0 THEN 1 ELSE 0 END) AS CoSoLuongAm
                FROM TTCuonDay cd
                JOIN TTNhapKhoTP nk ON nk.id = cd.ThongTinNhapKho_ID
                LEFT JOIN TTThanhPham tp ON tp.id = nk.TTThanhPham_ID
                LEFT JOIN LichSuCatDay lscd ON lscd.TTCuonDay_ID = cd.id
                WHERE cd.id = @id
                GROUP BY
                    cd.id,
                    tp.MaBin,
                    nk.TenSP,
                    cd.KhachHang,
                    cd.TTLo_ID,
                    cd.SoCuon,
                    cd.ChieuDai_1cuon,
                    cd.SoDau,
                    cd.SoCuoi
                LIMIT 1;";

            const string sqlInsert = @"
                INSERT INTO LichSuCatDay
                    (TTCuonDay_ID, NguoiCat, SoDau, SoCuoi, GhiChu,
                     ChieuDaiCat, TinhTrang, NgayCat, SoLuong, ""NgườiLenLenh"", NgayLenLenh)
                VALUES
                    (@TTCuonDay_ID, NULL, @SoDau, @SoCuoi, NULL,
                     @ChieuDaiCat, 'Đã lên lệnh', NULL, @SoLuong, @NguoiLenLenh, @NgayLenLenh);";

            using var conn = DB_Base.OpenConnection();
            // Serializable của System.Data.SQLite lấy write lock ngay từ đầu; việc đọc lại tồn
            // và INSERT vì vậy nằm trong cùng phạm vi khóa như đã chốt ở 36A.
            using var tran = conn.BeginTransaction(IsolationLevel.Serializable);

            try
            {
                using var cmdTrangThai = new SQLiteCommand(sqlTrangThai, conn, tran);
                cmdTrangThai.Parameters.Add("@id", DbType.Int64);

                using var cmdInsert = new SQLiteCommand(sqlInsert, conn, tran);
                cmdInsert.Parameters.Add("@TTCuonDay_ID", DbType.Int64);
                cmdInsert.Parameters.Add("@SoDau", DbType.Int64);
                cmdInsert.Parameters.Add("@SoCuoi", DbType.Int64);
                cmdInsert.Parameters.Add("@ChieuDaiCat", DbType.Int32);
                cmdInsert.Parameters.Add("@SoLuong", DbType.Int32);
                cmdInsert.Parameters.Add("@NguoiLenLenh", DbType.String);
                cmdInsert.Parameters.Add("@NgayLenLenh", DbType.String);

                foreach (CatDay_LenLenhInput input in canXuLy)
                {
                    if (input.TTCuonDay_ID <= 0)
                    {
                        ThemLoiLenLenh(result, input.TTCuonDay_ID, "TTCuonDay_ID không hợp lệ.", null, false);
                        continue;
                    }

                    if (input.SoLuong.HasValue == input.ChieuDaiCat.HasValue)
                    {
                        ThemLoiLenLenh(
                            result,
                            input.TTCuonDay_ID,
                            "Mỗi dòng phải có đúng một loại input: Cuộn xuất hoặc CD cắt.",
                            null,
                            false);
                        continue;
                    }

                    cmdTrangThai.Parameters["@id"].Value = input.TTCuonDay_ID;
                    CatDay_CurrentState state;
                    using (SQLiteDataReader reader = cmdTrangThai.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            ThemLoiLenLenh(
                                result,
                                input.TTCuonDay_ID,
                                "TTCuonDay không còn tồn tại tại thời điểm lên lệnh.",
                                null,
                                false);
                            continue;
                        }

                        state = DocCurrentState(reader);
                    }

                    CatDay_InventoryGroup nhomInput = input.SoLuong.HasValue
                        ? CatDay_InventoryGroup.SoLuong
                        : CatDay_InventoryGroup.ChieuDai;

                    CatDay_InventoryGroup? nhomDb = LayNhomTonHopLe(state);
                    // Nếu DB rơi vào partial-null thì nhóm cũ cũng không còn hợp lệ; coi như
                    // trạng thái nhóm đã thay đổi để UI xóa input cũ và khóa cả hai cột.
                    bool nhomTonDaThayDoi = !nhomDb.HasValue || nhomDb.Value != nhomInput;

                    if (!TryTaoSnapshot(state, out CatDay_Row snapshot, out string loiTrangThai))
                    {
                        // TryTaoSnapshot luôn trả snapshot tham khảo khi TTCuonDay vẫn tồn tại,
                        // kể cả dữ liệu bất thường/partial-null, để UI không giữ snapshot cũ.
                        ThemLoiLenLenh(
                            result,
                            input.TTCuonDay_ID,
                            loiTrangThai,
                            snapshot,
                            nhomTonDaThayDoi);
                        continue;
                    }

                    if (snapshot.NhomTon != nhomInput)
                    {
                        ThemLoiLenLenh(
                            result,
                            input.TTCuonDay_ID,
                            "Nhóm quản lý tồn trong DB đã thay đổi so với input trên grid.",
                            snapshot,
                            true);
                        continue;
                    }

                    if (snapshot.NhomTon == CatDay_InventoryGroup.SoLuong)
                    {
                        int soLuong = input.SoLuong.GetValueOrDefault();
                        if (soLuong <= 0)
                        {
                            ThemLoiLenLenh(result, input.TTCuonDay_ID, "Cuộn xuất phải là số nguyên lớn hơn 0.", snapshot, false);
                            continue;
                        }

                        if (snapshot.SoCuon <= 0)
                        {
                            ThemLoiLenLenh(result, input.TTCuonDay_ID, "TTCuonDay đã hết tồn số lượng.", snapshot, false);
                            continue;
                        }

                        if (soLuong > snapshot.SoCuon)
                        {
                            ThemLoiLenLenh(
                                result,
                                input.TTCuonDay_ID,
                                $"Cuộn xuất ({soLuong}) vượt tồn hiện tại ({snapshot.SoCuon}).",
                                snapshot,
                                false);
                            continue;
                        }

                        GanInsertParameters(
                            cmdInsert,
                            input.TTCuonDay_ID,
                            null,
                            null,
                            null,
                            soLuong,
                            nguoiLenLenh,
                            ngayLenLenh);
                    }
                    else
                    {
                        int chieuDaiCat = input.ChieuDaiCat.GetValueOrDefault();
                        if (chieuDaiCat <= 0)
                        {
                            ThemLoiLenLenh(result, input.TTCuonDay_ID, "CD cắt phải là số nguyên lớn hơn 0.", snapshot, false);
                            continue;
                        }

                        if (snapshot.ChieuDaiConLai <= 0)
                        {
                            ThemLoiLenLenh(result, input.TTCuonDay_ID, "TTCuonDay đã hết tồn chiều dài.", snapshot, false);
                            continue;
                        }

                        if (chieuDaiCat > snapshot.ChieuDaiConLai)
                        {
                            ThemLoiLenLenh(
                                result,
                                input.TTCuonDay_ID,
                                $"CD cắt ({chieuDaiCat}) vượt chiều dài còn lại ({snapshot.ChieuDaiConLai}).",
                                snapshot,
                                false);
                            continue;
                        }

                        int heSo = state.SoDau.Value < state.SoCuoi.Value ? 1 : -1;
                        long currentSoCuoi = snapshot.SoCuoi.Value;
                        long soCuoiMoi = currentSoCuoi - ((long)heSo * chieuDaiCat);

                        GanInsertParameters(
                            cmdInsert,
                            input.TTCuonDay_ID,
                            currentSoCuoi,
                            soCuoiMoi,
                            chieuDaiCat,
                            null,
                            nguoiLenLenh,
                            ngayLenLenh);
                    }

                    if (cmdInsert.ExecuteNonQuery() != 1)
                        throw new InvalidOperationException($"Không INSERT được LichSuCatDay cho TTCuonDay_ID={input.TTCuonDay_ID}.");

                    result.ThanhCongIds.Add(input.TTCuonDay_ID);
                }

                tran.Commit();
                return result;
            }
            catch (Exception ex)
            {
                try
                {
                    tran.Rollback();
                }
                catch (Exception rollbackEx)
                {
                    GhiLogLenLenh("ERROR", "Rollback lỗi.", rollbackEx);
                }

                GhiLogLenLenh("ERROR", "Lỗi kỹ thuật, rollback toàn batch.", ex);
                throw;
            }
        }

        private sealed class CatDay_CurrentState
        {
            public long TTCuonDay_ID { get; set; }
            public string Lot { get; set; } = string.Empty;
            public string TenSP { get; set; } = string.Empty;
            public string KhachHang { get; set; } = string.Empty;
            public bool LaLo { get; set; }
            public long? SoCuon { get; set; }
            public long ChieuDaiMotDonVi { get; set; }
            public long? SoDau { get; set; }
            public long? SoCuoi { get; set; }
            public long TongChieuDaiCat { get; set; }
            public long TongSoLuong { get; set; }
            public bool CoChieuDaiCatAm { get; set; }
            public bool CoSoLuongAm { get; set; }
        }

        private static CatDay_CurrentState DocCurrentState(SQLiteDataReader reader)
        {
            return new CatDay_CurrentState
            {
                TTCuonDay_ID = Convert.ToInt64(reader["TTCuonDay_ID"], CultureInfo.InvariantCulture),
                Lot = DbText(reader["Lot"]),
                TenSP = DbText(reader["TenSP"]),
                KhachHang = DbText(reader["KhachHang"]),
                LaLo = reader["TTLo_ID"] != DBNull.Value,
                SoCuon = DbInt64Nullable(reader["SoCuon"]),
                ChieuDaiMotDonVi = Convert.ToInt64(reader["ChieuDai_1cuon"], CultureInfo.InvariantCulture),
                SoDau = DbInt64Nullable(reader["SoDau"]),
                SoCuoi = DbInt64Nullable(reader["SoCuoi"]),
                TongChieuDaiCat = Convert.ToInt64(reader["TongChieuDaiCat"], CultureInfo.InvariantCulture),
                TongSoLuong = Convert.ToInt64(reader["TongSoLuong"], CultureInfo.InvariantCulture),
                CoChieuDaiCatAm = Convert.ToInt32(reader["CoChieuDaiCatAm"], CultureInfo.InvariantCulture) != 0,
                CoSoLuongAm = Convert.ToInt32(reader["CoSoLuongAm"], CultureInfo.InvariantCulture) != 0
            };
        }

        private static CatDay_InventoryGroup? LayNhomTonHopLe(CatDay_CurrentState state)
        {
            if (state == null || state.SoDau.HasValue != state.SoCuoi.HasValue)
                return null;

            return state.SoDau.HasValue
                ? CatDay_InventoryGroup.ChieuDai
                : CatDay_InventoryGroup.SoLuong;
        }

        /// <summary>
        /// Snapshot tham khảo cho UI khi dữ liệu DB hiện tại không còn hợp lệ.
        /// Không dùng snapshot này để quyết định INSERT. Mục tiêu là luôn phản ánh trạng thái
        /// DB mới nhất thay vì giữ snapshot cũ trên grid.
        /// </summary>
        private static CatDay_Row TaoSnapshotThamKhao(
            CatDay_CurrentState state,
            CatDay_InventoryGroup? nhomTon)
        {
            string loai = state.LaLo ? "Lô" : "Cuộn";

            // Partial-null: không xác định được nhóm tồn. Hiển thị trực tiếp dữ liệu nguồn
            // mới nhất và khóa cả hai cột nhập ở UI.
            if (!nhomTon.HasValue)
            {
                long soCuonRaw = state.SoCuon ?? 0;
                int soCuonHienThi = GioiHanInt32(soCuonRaw);
                long tongThamKhao = 0;
                try
                {
                    tongThamKhao = checked(soCuonRaw * state.ChieuDaiMotDonVi);
                }
                catch (OverflowException)
                {
                    tongThamKhao = (soCuonRaw >= 0) == (state.ChieuDaiMotDonVi >= 0)
                        ? long.MaxValue
                        : long.MinValue;
                }

                return new CatDay_Row
                {
                    TTCuonDay_ID = state.TTCuonDay_ID,
                    Lot = state.Lot,
                    TenSP = state.TenSP,
                    KhachHang = state.KhachHang,
                    Loai = loai,
                    NhomTon = CatDay_InventoryGroup.SoLuong, // placeholder; NhomTonHopLe=false nên UI không dùng để mở cột.
                    NhomTonHopLe = false,
                    DuLieuTonHopLe = false,
                    SoCuon = soCuonHienThi,
                    SoDau = state.SoDau,
                    SoCuoi = state.SoCuoi,
                    ChieuDaiConLai = state.ChieuDaiMotDonVi,
                    TongChieuDai = tongThamKhao
                };
            }

            if (nhomTon.Value == CatDay_InventoryGroup.SoLuong)
            {
                long soCuonCon = (state.SoCuon ?? 0) - state.TongSoLuong;
                long tong = 0;
                try
                {
                    tong = checked(soCuonCon * state.ChieuDaiMotDonVi);
                }
                catch (OverflowException)
                {
                    tong = (soCuonCon >= 0) == (state.ChieuDaiMotDonVi >= 0)
                        ? long.MaxValue
                        : long.MinValue;
                }

                return new CatDay_Row
                {
                    TTCuonDay_ID = state.TTCuonDay_ID,
                    Lot = state.Lot,
                    TenSP = state.TenSP,
                    KhachHang = state.KhachHang,
                    Loai = loai,
                    NhomTon = CatDay_InventoryGroup.SoLuong,
                    NhomTonHopLe = true,
                    DuLieuTonHopLe = false,
                    SoCuon = GioiHanInt32(soCuonCon),
                    SoDau = null,
                    SoCuoi = null,
                    ChieuDaiConLai = state.ChieuDaiMotDonVi,
                    TongChieuDai = tong
                };
            }

            long soDau = state.SoDau.GetValueOrDefault();
            long soCuoi = state.SoCuoi.GetValueOrDefault();
            int heSo = soDau < soCuoi ? 1 : (soDau > soCuoi ? -1 : 0);
            long soCuoiHienTai = heSo == 0
                ? soCuoi
                : soCuoi - ((long)heSo * state.TongChieuDaiCat);
            long chieuDaiCon = Math.Abs(soCuoi - soDau) - state.TongChieuDaiCat;

            return new CatDay_Row
            {
                TTCuonDay_ID = state.TTCuonDay_ID,
                Lot = state.Lot,
                TenSP = state.TenSP,
                KhachHang = state.KhachHang,
                Loai = loai,
                NhomTon = CatDay_InventoryGroup.ChieuDai,
                NhomTonHopLe = true,
                DuLieuTonHopLe = false,
                SoCuon = GioiHanInt32(state.SoCuon ?? 0),
                SoDau = soDau,
                SoCuoi = soCuoiHienTai,
                ChieuDaiConLai = chieuDaiCon,
                TongChieuDai = chieuDaiCon
            };
        }

        private static int GioiHanInt32(long value)
        {
            if (value > int.MaxValue) return int.MaxValue;
            if (value < int.MinValue) return int.MinValue;
            return (int)value;
        }

        private static bool TryTaoSnapshot(
            CatDay_CurrentState state,
            out CatDay_Row snapshot,
            out string loi)
        {
            snapshot = null;
            loi = string.Empty;

            CatDay_InventoryGroup? nhomTon = LayNhomTonHopLe(state);
            if (!nhomTon.HasValue)
            {
                snapshot = TaoSnapshotThamKhao(state, null);
                loi = "TTCuonDay chỉ có một trong SoDau/SoCuoi; không xác định được nhóm quản lý tồn.";
                return false;
            }

            string loai = state.LaLo ? "Lô" : "Cuộn";

            if (nhomTon.Value == CatDay_InventoryGroup.SoLuong)
            {
                if (state.CoSoLuongAm)
                {
                    snapshot = TaoSnapshotThamKhao(state, nhomTon);
                    loi = "Có LichSuCatDay.SoLuong âm.";
                    return false;
                }

                if (!state.SoCuon.HasValue || state.SoCuon.Value <= 0)
                {
                    snapshot = TaoSnapshotThamKhao(state, nhomTon);
                    loi = "TTCuonDay.SoCuon không hợp lệ cho nhóm quản lý theo số lượng.";
                    return false;
                }

                if (state.ChieuDaiMotDonVi <= 0)
                {
                    snapshot = TaoSnapshotThamKhao(state, nhomTon);
                    loi = "TTCuonDay.ChieuDai_1cuon phải lớn hơn 0 cho nhóm quản lý theo số lượng.";
                    return false;
                }

                long soCuonCon = state.SoCuon.Value - state.TongSoLuong;
                if (soCuonCon < 0 || soCuonCon > int.MaxValue)
                {
                    snapshot = TaoSnapshotThamKhao(state, nhomTon);
                    loi = soCuonCon < 0
                        ? "Số cuộn còn lại âm."
                        : "Số cuộn còn lại vượt phạm vi dữ liệu hỗ trợ.";
                    return false;
                }

                snapshot = new CatDay_Row
                {
                    TTCuonDay_ID = state.TTCuonDay_ID,
                    Lot = state.Lot,
                    TenSP = state.TenSP,
                    KhachHang = state.KhachHang,
                    Loai = loai,
                    NhomTon = CatDay_InventoryGroup.SoLuong,
                    NhomTonHopLe = true,
                    DuLieuTonHopLe = true,
                    SoCuon = (int)soCuonCon,
                    SoDau = null,
                    SoCuoi = null,
                    ChieuDaiConLai = state.ChieuDaiMotDonVi,
                    TongChieuDai = soCuonCon * state.ChieuDaiMotDonVi
                };
                return true;
            }

            if (state.CoChieuDaiCatAm)
            {
                snapshot = TaoSnapshotThamKhao(state, nhomTon);
                loi = "Có LichSuCatDay.ChieuDaiCat âm.";
                return false;
            }

            if (!state.SoCuon.HasValue || state.SoCuon.Value != 1)
            {
                snapshot = TaoSnapshotThamKhao(state, nhomTon);
                loi = "Nhóm quản lý theo chiều dài nhưng TTCuonDay.SoCuon khác 1.";
                return false;
            }

            long soDau = state.SoDau.Value;
            long soCuoi = state.SoCuoi.Value;

            // Đã chốt: SoDau == SoCuoi là hết tồn, không phải dữ liệu bất thường.
            // Phải áp dụng giống phần tìm kiếm để search và btnCat không cho hai kết quả khác nhau.
            if (soDau == soCuoi)
            {
                snapshot = new CatDay_Row
                {
                    TTCuonDay_ID = state.TTCuonDay_ID,
                    Lot = state.Lot,
                    TenSP = state.TenSP,
                    KhachHang = state.KhachHang,
                    Loai = loai,
                    NhomTon = CatDay_InventoryGroup.ChieuDai,
                    NhomTonHopLe = true,
                    DuLieuTonHopLe = true,
                    SoCuon = 1,
                    SoDau = soDau,
                    SoCuoi = soCuoi,
                    ChieuDaiConLai = 0,
                    TongChieuDai = 0
                };
                return true;
            }

            long chieuDaiGoc = Math.Abs(soCuoi - soDau);
            long chieuDaiCon = chieuDaiGoc - state.TongChieuDaiCat;

            if (chieuDaiCon < 0)
            {
                snapshot = TaoSnapshotThamKhao(state, nhomTon);
                loi = "Chiều dài còn lại âm.";
                return false;
            }

            int heSo = soDau < soCuoi ? 1 : -1;
            long soCuoiHienTai = heSo == 0
                ? soCuoi
                : soCuoi - ((long)heSo * state.TongChieuDaiCat);

            snapshot = new CatDay_Row
            {
                TTCuonDay_ID = state.TTCuonDay_ID,
                Lot = state.Lot,
                TenSP = state.TenSP,
                KhachHang = state.KhachHang,
                Loai = loai,
                NhomTon = CatDay_InventoryGroup.ChieuDai,
                NhomTonHopLe = true,
                DuLieuTonHopLe = true,
                SoCuon = 1,
                SoDau = soDau,
                SoCuoi = soCuoiHienTai,
                ChieuDaiConLai = chieuDaiCon,
                TongChieuDai = chieuDaiCon
            };
            return true;
        }

        private static void GanInsertParameters(
            SQLiteCommand cmd,
            long ttCuonDayId,
            long? soDau,
            long? soCuoi,
            int? chieuDaiCat,
            int? soLuong,
            string nguoiLenLenh,
            string ngayLenLenh)
        {
            cmd.Parameters["@TTCuonDay_ID"].Value = ttCuonDayId;
            cmd.Parameters["@SoDau"].Value = soDau.HasValue ? (object)soDau.Value : DBNull.Value;
            cmd.Parameters["@SoCuoi"].Value = soCuoi.HasValue ? (object)soCuoi.Value : DBNull.Value;
            cmd.Parameters["@ChieuDaiCat"].Value = chieuDaiCat.HasValue ? (object)chieuDaiCat.Value : DBNull.Value;
            cmd.Parameters["@SoLuong"].Value = soLuong.HasValue ? (object)soLuong.Value : DBNull.Value;
            cmd.Parameters["@NguoiLenLenh"].Value = nguoiLenLenh ?? string.Empty;
            cmd.Parameters["@NgayLenLenh"].Value = ngayLenLenh ?? string.Empty;
        }

        private static void ThemLoiLenLenh(
            CatDay_LenLenhResult result,
            long ttCuonDayId,
            string lyDo,
            CatDay_Row duLieuMoi,
            bool nhomTonDaThayDoi)
        {
            result.Loi.Add(new CatDay_LenLenhDongResult
            {
                TTCuonDay_ID = ttCuonDayId,
                ThanhCong = false,
                NhomTonDaThayDoi = nhomTonDaThayDoi,
                LyDo = lyDo ?? string.Empty,
                DuLieuMoi = duLieuMoi
            });

            GhiLogLenLenh("WARN", $"TTCuonDay_ID={ttCuonDayId}; {lyDo}");
        }

        private static string XayDungSql(CatDay_SearchCriteria criteria)
        {
            // Lọc trước các điều kiện không phụ thuộc lịch sử để giảm số TTCuonDay
            // cần aggregate. Riêng tìm theo chiều dài phải aggregate trước mới biết tồn.
            var sql = new StringBuilder(@"
                WITH base AS (
                    SELECT
                        cd.id AS TTCuonDay_ID,
                        COALESCE(tp.MaBin, '') AS Lot,
                        nk.TenSP AS TenSP,
                        cd.SoCuon AS SoCuon,
                        cd.SoDau AS SoDau,
                        cd.SoCuoi AS SoCuoi,
                        cd.ChieuDai_1cuon AS ChieuDai_1cuon,
                        cd.Ngay AS Ngay,
                        cd.KhachHang AS KhachHang,
                        cd.KhachHang_KhongDau AS KhachHang_KhongDau,
                        cd.TTLo_ID AS TTLo_ID
                    FROM TTCuonDay cd
                    JOIN TTNhapKhoTP nk
                        ON nk.id = cd.ThongTinNhapKho_ID
                    LEFT JOIN TTThanhPham tp
                        ON tp.id = nk.TTThanhPham_ID
                    WHERE 1 = 1
            ");

            if (!criteria.LayToanBo && criteria.SearchType.HasValue)
            {
                switch (criteria.SearchType.Value)
                {
                    case CatDay_SearchType.Lot:
                        sql.AppendLine("  AND TRIM(COALESCE(tp.MaBin, '')) = TRIM(@searchValue) COLLATE NOCASE");
                        break;

                    case CatDay_SearchType.TenSanPham:
                        sql.AppendLine("  AND TRIM(COALESCE(nk.TenSP, '')) = TRIM(@searchValue) COLLATE NOCASE");
                        break;

                    case CatDay_SearchType.KhachHang:
                        sql.AppendLine("  AND TRIM(COALESCE(cd.KhachHang_KhongDau, '')) = TRIM(@searchValueKhongDau) COLLATE NOCASE");
                        break;

                    case CatDay_SearchType.ChieuDai:
                        // Phải tính tồn xong mới so được chiều dài.
                        break;
                }
            }

            // Đã chốt: tìm ngày dựa trên TTCuonDay.Ngay.
            if (criteria.NgayBatDau.HasValue)
                sql.AppendLine("  AND date(cd.Ngay) >= date(@ngayBatDau)");

            if (criteria.NgayKetThuc.HasValue)
                sql.AppendLine("  AND date(cd.Ngay) <= date(@ngayKetThuc)");

            sql.Append(@"
                ),
                du_lieu AS (
                    SELECT
                        b.TTCuonDay_ID,
                        b.Lot,
                        b.TenSP,
                        b.SoCuon,
                        b.SoDau,
                        b.SoCuoi,
                        b.ChieuDai_1cuon,
                        b.Ngay,
                        b.KhachHang,
                        b.KhachHang_KhongDau,
                        b.TTLo_ID,
                        COALESCE(SUM(CASE WHEN lscd.ChieuDaiCat IS NULL THEN 0 ELSE lscd.ChieuDaiCat END), 0) AS TongChieuDaiCat,
                        COALESCE(SUM(CASE WHEN lscd.SoLuong IS NULL THEN 0 ELSE lscd.SoLuong END), 0) AS TongSoLuong,
                        MAX(CASE WHEN lscd.ChieuDaiCat < 0 THEN 1 ELSE 0 END) AS CoChieuDaiCatAm,
                        MAX(CASE WHEN lscd.SoLuong < 0 THEN 1 ELSE 0 END) AS CoSoLuongAm
                    FROM base b
                    LEFT JOIN LichSuCatDay lscd
                        ON lscd.TTCuonDay_ID = b.TTCuonDay_ID
                    GROUP BY
                        b.TTCuonDay_ID,
                        b.Lot,
                        b.TenSP,
                        b.SoCuon,
                        b.SoDau,
                        b.SoCuoi,
                        b.ChieuDai_1cuon,
                        b.Ngay,
                        b.KhachHang,
                        b.KhachHang_KhongDau,
                        b.TTLo_ID
                )
                SELECT
                    TTCuonDay_ID,
                    Lot,
                    TenSP,
                    SoCuon,
                    SoDau,
                    SoCuoi,
                    ChieuDai_1cuon,
                    Ngay,
                    KhachHang,
                    KhachHang_KhongDau,
                    TTLo_ID,
                    TongChieuDaiCat,
                    TongSoLuong,
                    CoChieuDaiCatAm,
                    CoSoLuongAm
                FROM du_lieu
                ORDER BY Ngay DESC, TTCuonDay_ID DESC;
            ");

            return sql.ToString();
        }

        private static string XayDungSqlSuggestion(CatDay_SearchType searchType)
        {
            string dieuKienKeyword;
            string cotGiaTri;

            switch (searchType)
            {
                case CatDay_SearchType.Lot:
                    dieuKienKeyword = @"
                        AND tp.MaBin IS NOT NULL
                        AND TRIM(tp.MaBin) <> ''
                        AND tp.MaBin LIKE @kw ESCAPE '\'";
                    cotGiaTri = "Lot";
                    break;

                case CatDay_SearchType.TenSanPham:
                    dieuKienKeyword = @"
                        AND nk.TenSP IS NOT NULL
                        AND TRIM(nk.TenSP) <> ''
                        AND nk.TenSP LIKE @kw ESCAPE '\'";
                    cotGiaTri = "TenSP";
                    break;

                case CatDay_SearchType.KhachHang:
                    dieuKienKeyword = @"
                        AND cd.KhachHang IS NOT NULL
                        AND TRIM(cd.KhachHang) <> ''
                        AND COALESCE(cd.KhachHang_KhongDau, '') LIKE @kw ESCAPE '\'";
                    cotGiaTri = "KhachHang";
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(searchType));
            }

            var sql = new StringBuilder(@"
                WITH base AS (
                    SELECT
                        cd.id AS TTCuonDay_ID,
                        COALESCE(tp.MaBin, '') AS Lot,
                        nk.TenSP AS TenSP,
                        cd.SoCuon AS SoCuon,
                        cd.SoDau AS SoDau,
                        cd.SoCuoi AS SoCuoi,
                        cd.ChieuDai_1cuon AS ChieuDai_1cuon,
                        cd.KhachHang AS KhachHang
                    FROM TTCuonDay cd
                    JOIN TTNhapKhoTP nk
                        ON nk.id = cd.ThongTinNhapKho_ID
                    LEFT JOIN TTThanhPham tp
                        ON tp.id = nk.TTThanhPham_ID
                    WHERE 1 = 1
            ");

            sql.AppendLine(dieuKienKeyword);
            sql.Append(@"
                ),
                du_lieu AS (
                    SELECT
                        b.TTCuonDay_ID,
                        b.Lot,
                        b.TenSP,
                        b.SoCuon,
                        b.SoDau,
                        b.SoCuoi,
                        b.ChieuDai_1cuon,
                        b.KhachHang,
                        COALESCE(SUM(CASE WHEN lscd.ChieuDaiCat IS NULL THEN 0 ELSE lscd.ChieuDaiCat END), 0) AS TongChieuDaiCat,
                        COALESCE(SUM(CASE WHEN lscd.SoLuong IS NULL THEN 0 ELSE lscd.SoLuong END), 0) AS TongSoLuong,
                        MAX(CASE WHEN lscd.ChieuDaiCat < 0 THEN 1 ELSE 0 END) AS CoChieuDaiCatAm,
                        MAX(CASE WHEN lscd.SoLuong < 0 THEN 1 ELSE 0 END) AS CoSoLuongAm
                    FROM base b
                    LEFT JOIN LichSuCatDay lscd
                        ON lscd.TTCuonDay_ID = b.TTCuonDay_ID
                    GROUP BY
                        b.TTCuonDay_ID,
                        b.Lot,
                        b.TenSP,
                        b.SoCuon,
                        b.SoDau,
                        b.SoCuoi,
                        b.ChieuDai_1cuon,
                        b.KhachHang
                ),
                con_ton AS (
                    SELECT *
                    FROM du_lieu
                    WHERE
                        (
                            SoDau IS NULL
                            AND SoCuoi IS NULL
                            AND SoCuon IS NOT NULL
                            AND SoCuon > 0
                            AND ChieuDai_1cuon > 0
                            AND CoSoLuongAm = 0
                            AND (CAST(SoCuon AS INTEGER) - TongSoLuong) > 0
                        )
                        OR
                        (
                            SoDau IS NOT NULL
                            AND SoCuoi IS NOT NULL
                            AND SoCuon = 1
                            AND SoDau <> SoCuoi
                            AND CoChieuDaiCatAm = 0
                            AND (ABS(CAST(SoCuoi AS INTEGER) - CAST(SoDau AS INTEGER)) - TongChieuDaiCat) > 0
                        )
                )
                SELECT DISTINCT TRIM(");

            sql.Append(cotGiaTri);
            sql.Append(@") AS GiaTri
                FROM con_ton
                WHERE ");
            sql.Append(cotGiaTri);
            sql.Append(@" IS NOT NULL
                  AND TRIM(");
            sql.Append(cotGiaTri);
            sql.Append(@") <> ''
                ORDER BY GiaTri COLLATE NOCASE
                LIMIT @limit;");

            return sql.ToString();
        }

        private static void GanThamSo(SQLiteCommand cmd, CatDay_SearchCriteria criteria)
        {
            if (!criteria.LayToanBo && criteria.SearchType.HasValue)
            {
                switch (criteria.SearchType.Value)
                {
                    case CatDay_SearchType.Lot:
                    case CatDay_SearchType.TenSanPham:
                        cmd.Parameters.AddWithValue("@searchValue", criteria.SearchValue?.Trim() ?? string.Empty);
                        break;

                    case CatDay_SearchType.KhachHang:
                        cmd.Parameters.AddWithValue(
                            "@searchValueKhongDau",
                            ChuyenKhongDau(criteria.SearchValue?.Trim() ?? string.Empty));
                        break;

                    case CatDay_SearchType.ChieuDai:
                        // Chiều dài được so sau khi đã tính tồn trong C#.
                        break;
                }
            }

            if (criteria.NgayBatDau.HasValue)
                cmd.Parameters.AddWithValue(
                    "@ngayBatDau",
                    criteria.NgayBatDau.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));

            if (criteria.NgayKetThuc.HasValue)
                cmd.Parameters.AddWithValue(
                    "@ngayKetThuc",
                    criteria.NgayKetThuc.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        }

        private static void ThemDataIssue(
            CatDay_SearchResult result,
            long ttCuonDayId,
            string lot,
            long? rawRemaining,
            string noiDung)
        {
            result.DataIssues.Add(new CatDay_DataIssue
            {
                TTCuonDay_ID = ttCuonDayId,
                Lot = lot,
                RawRemaining = rawRemaining,
                NoiDung = noiDung ?? string.Empty
            });
        }

        private static DataTable TaoBangSuggestionRong()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("GiaTri", typeof(string));
            return dt;
        }

        private static long? DbInt64Nullable(object value)
        {
            if (value == null || value == DBNull.Value)
                return null;

            return Convert.ToInt64(value, CultureInfo.InvariantCulture);
        }

        private static string DbText(object value)
        {
            return value == null || value == DBNull.Value
                ? string.Empty
                : Convert.ToString(value, CultureInfo.InvariantCulture)?.Trim() ?? string.Empty;
        }

        /// <summary>
        /// Tạo pattern contains nhưng coi %, _ và \ là ký tự thường của keyword.
        /// </summary>
        private static string TaoLikeContains(string keyword)
        {
            keyword ??= string.Empty;
            return "%" + keyword
                .Replace("\\", "\\\\")
                .Replace("%", "\\%")
                .Replace("_", "\\_") + "%";
        }

        private static string ChuyenKhongDau(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            string normalized = value.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(normalized.Length);

            foreach (char c in normalized)
            {
                UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(c);
                if (category == UnicodeCategory.NonSpacingMark)
                    continue;

                if (c == 'đ')
                    sb.Append('d');
                else if (c == 'Đ')
                    sb.Append('D');
                else
                    sb.Append(c);
            }

            return sb.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}
