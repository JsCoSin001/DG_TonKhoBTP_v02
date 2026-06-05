using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Globalization;

namespace DG_TonKhoBTP_v02.Database.Kho
{
    internal static class NhapKhoNVL_DB
    {
        private static readonly Random _random = new Random();
        private static readonly object _randomLock = new object();

        internal static NhapKhoNVL_Dong LuuMotDong(NhapKhoNVL_Dong input, string zeroDigit = "X")
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            using var conn = DB_Base.OpenConnection();
            using var tran = conn.BeginTransaction();

            try
            {
                NhapKhoNVL_SanPham sp = TimSanPhamNhapKhoTheoTenKhongDau(conn, tran, input.TenKhongDau);

                string maBin = string.IsNullOrWhiteSpace(input.MaBin)
                    ? TaoQrNhapKhoNVL(conn, tran, zeroDigit)
                    : input.MaBin.Trim();

                const string sql = @"
                    INSERT INTO TTThanhPham
                        (DanhSachSP_ID, MaBin,
                         KhoiLuongTruoc, KhoiLuongSau,
                         ChieuDaiTruoc, ChieuDaiSau,
                         CongDoan, GhiChu, DateInsert, NhapKho)
                    VALUES
                        (@DanhSachSP_ID, @MaBin,
                         @KhoiLuong, @KhoiLuong,
                         @ChieuDai, @ChieuDai,
                         @CongDoan, @GhiChu, @DateInsert, @NhapKho);
                    SELECT last_insert_rowid();";

                long newId;
                using (var cmd = new SQLiteCommand(sql, conn, tran))
                {
                    cmd.Parameters.AddWithValue("@DanhSachSP_ID", sp.Id);
                    cmd.Parameters.AddWithValue("@MaBin", maBin);
                    cmd.Parameters.AddWithValue("@KhoiLuong", input.KhoiLuong);
                    cmd.Parameters.AddWithValue("@ChieuDai", input.ChieuDai);
                    cmd.Parameters.AddWithValue("@CongDoan", "0");
                    cmd.Parameters.AddWithValue("@GhiChu", string.IsNullOrWhiteSpace(input.GhiChu) ? (object)DBNull.Value : input.GhiChu.Trim());
                    cmd.Parameters.AddWithValue("@DateInsert", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
                    cmd.Parameters.AddWithValue("@NhapKho", 0);

                    newId = Convert.ToInt64(cmd.ExecuteScalar(), CultureInfo.InvariantCulture);
                }

                tran.Commit();

                return new NhapKhoNVL_Dong
                {
                    TTThanhPhamId = newId,
                    DanhSachSPId = sp.Id,
                    Ten = sp.Ten,
                    TenKhongDau = sp.TenKhongDau,
                    MaSP = sp.Ma,
                    KhoiLuong = input.KhoiLuong,
                    ChieuDai = input.ChieuDai,
                    MaBin = maBin,
                    GhiChu = input.GhiChu ?? string.Empty
                };
            }
            catch
            {
                tran.Rollback();
                throw;
            }
        }

        internal static NhapKhoNVL_Dong CapNhatMotDong(NhapKhoNVL_Dong input)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            if (!input.TTThanhPhamId.HasValue || input.TTThanhPhamId.Value <= 0)
                throw new InvalidOperationException("Không tìm thấy TTThanhPham_ID để cập nhật.");

            if (string.IsNullOrWhiteSpace(input.MaBin))
                throw new InvalidOperationException("QR không được để trống khi sửa.");

            using var conn = DB_Base.OpenConnection();
            using var tran = conn.BeginTransaction();

            try
            {
                NhapKhoNVL_SanPham sp = TimSanPhamNhapKhoTheoTenKhongDau(conn, tran, input.TenKhongDau);

                const string sql = @"
                    UPDATE TTThanhPham
                    SET DanhSachSP_ID   = @DanhSachSP_ID,
                        MaBin           = @MaBin,
                        KhoiLuongTruoc  = @KhoiLuong,
                        KhoiLuongSau    = @KhoiLuong,
                        ChieuDaiTruoc   = @ChieuDai,
                        ChieuDaiSau     = @ChieuDai,
                        GhiChu          = @GhiChu
                    WHERE id = @TTThanhPham_ID;";

                using (var cmd = new SQLiteCommand(sql, conn, tran))
                {
                    cmd.Parameters.AddWithValue("@DanhSachSP_ID", sp.Id);
                    cmd.Parameters.AddWithValue("@MaBin", input.MaBin.Trim());
                    cmd.Parameters.AddWithValue("@KhoiLuong", input.KhoiLuong);
                    cmd.Parameters.AddWithValue("@ChieuDai", input.ChieuDai);
                    cmd.Parameters.AddWithValue("@GhiChu", string.IsNullOrWhiteSpace(input.GhiChu) ? (object)DBNull.Value : input.GhiChu.Trim());
                    cmd.Parameters.AddWithValue("@TTThanhPham_ID", input.TTThanhPhamId.Value);

                    if (cmd.ExecuteNonQuery() == 0)
                        throw new InvalidOperationException($"Không cập nhật được TTThanhPham id={input.TTThanhPhamId.Value}.");
                }

                tran.Commit();

                return new NhapKhoNVL_Dong
                {
                    TTThanhPhamId = input.TTThanhPhamId.Value,
                    DanhSachSPId = sp.Id,
                    Ten = sp.Ten,
                    TenKhongDau = sp.TenKhongDau,
                    MaSP = sp.Ma,
                    KhoiLuong = input.KhoiLuong,
                    ChieuDai = input.ChieuDai,
                    MaBin = input.MaBin.Trim(),
                    GhiChu = input.GhiChu ?? string.Empty
                };
            }
            catch
            {
                tran.Rollback();
                throw;
            }
        }

        internal static NhapKhoNVL_Dong TimTheoMaBin(string maBin)
        {
            maBin = maBin?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(maBin))
                return null;

            const string sql = @"
                SELECT  tp.id              AS TTThanhPham_ID,
                        tp.DanhSachSP_ID   AS DanhSachSP_ID,
                        tp.MaBin           AS MaBin,
                        tp.KhoiLuongSau    AS KhoiLuong,
                        tp.ChieuDaiSau     AS ChieuDai,
                        tp.GhiChu          AS GhiChu,
                        sp.Ten             AS Ten,
                        sp.Ten_KhongDau    AS TenKhongDau,
                        sp.Ma              AS MaSP
                FROM    TTThanhPham tp
                JOIN    DanhSachMaSP sp ON sp.id = tp.DanhSachSP_ID
                WHERE   TRIM(tp.MaBin) = TRIM(@MaBin) COLLATE NOCASE
                LIMIT   1;";

            using var conn = DB_Base.OpenConnection();
            using var cmd = new SQLiteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@MaBin", maBin);

            using SQLiteDataReader reader = cmd.ExecuteReader();
            if (!reader.Read())
                return null;

            return new NhapKhoNVL_Dong
            {
                TTThanhPhamId = LayLong(reader, "TTThanhPham_ID"),
                DanhSachSPId = LayLong(reader, "DanhSachSP_ID"),
                MaBin = LayString(reader, "MaBin"),
                KhoiLuong = LayDouble(reader, "KhoiLuong"),
                ChieuDai = LayDouble(reader, "ChieuDai"),
                GhiChu = LayString(reader, "GhiChu"),
                Ten = LayString(reader, "Ten"),
                TenKhongDau = LayString(reader, "TenKhongDau"),
                MaSP = LayString(reader, "MaSP")
            };
        }

        internal static bool MaBinDaTonTai(string maBin)
        {
            using var conn = DB_Base.OpenConnection();
            return MaBinDaTonTai(conn, null, maBin);
        }

        internal static string TaoQrNhapKhoNVL(string zeroDigit = "X")
        {
            using var conn = DB_Base.OpenConnection();
            return TaoQrNhapKhoNVL(conn, null, zeroDigit);
        }

        private static string TaoQrNhapKhoNVL(SQLiteConnection conn, SQLiteTransaction tran, string zeroDigit = "X")
        {
            zeroDigit = string.IsNullOrWhiteSpace(zeroDigit) ? "X" : zeroDigit.Trim().ToUpperInvariant();

            for (int i = 0; i < 1000; i++)
            {
                string maBin = TaoQrNhapKhoNVLKhongKiemTraTrung(zeroDigit);
                if (!MaBinDaTonTai(conn, tran, maBin))
                    return maBin;
            }

            throw new InvalidOperationException("Không tạo được QR nhập kho NVL không trùng sau 1000 lần thử.");
        }

        private static string TaoQrNhapKhoNVLKhongKiemTraTrung(string zeroDigit)
        {
            int firstDigit;
            int middlePart;
            int partAfterSlash;
            int part4;
            int part5;

            lock (_randomLock)
            {
                firstDigit = _random.Next(1, 10);
                middlePart = _random.Next(300001, 500000);
                partAfterSlash = _random.Next(1, 10);
                part4 = _random.Next(0, 100);
                part5 = _random.Next(0, 100);
            }

            return $"{zeroDigit}{firstDigit}-{middlePart}/{partAfterSlash}-{part4:D2}-{part5:D2}";
        }

        private static bool MaBinDaTonTai(SQLiteConnection conn, SQLiteTransaction tran, string maBin)
        {
            const string sql = @"
                SELECT 1
                FROM TTThanhPham
                WHERE TRIM(MaBin) = TRIM(@MaBin) COLLATE NOCASE
                LIMIT 1;";

            using var cmd = new SQLiteCommand(sql, conn, tran);
            cmd.Parameters.AddWithValue("@MaBin", maBin ?? string.Empty);
            object result = cmd.ExecuteScalar();
            return result != null && result != DBNull.Value;
        }

        private static NhapKhoNVL_SanPham TimSanPhamNhapKhoTheoTenKhongDau(
            SQLiteConnection conn,
            SQLiteTransaction tran,
            string tenKhongDau)
        {
            tenKhongDau = tenKhongDau?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(tenKhongDau))
                throw new InvalidOperationException("Tên sản phẩm không được để trống.");

            const string sql = @"
                SELECT  id,
                        Ten,
                        Ten_KhongDau,
                        Ma
                FROM    DanhSachMaSP
                WHERE   UPPER(TRIM(IFNULL(Ten_KhongDau, ''))) = UPPER(TRIM(@TenKhongDau))
                    AND UPPER(KieuSP) <> 'TP'
                    AND IFNULL(Active, 1) = 1
                LIMIT   2;";

            List<NhapKhoNVL_SanPham> result = new List<NhapKhoNVL_SanPham>();

            using (var cmd = new SQLiteCommand(sql, conn, tran))
            {
                cmd.Parameters.AddWithValue("@TenKhongDau", tenKhongDau);

                using SQLiteDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    result.Add(new NhapKhoNVL_SanPham
                    {
                        Id = LayLong(reader, "id") ?? 0,
                        Ten = LayString(reader, "Ten"),
                        TenKhongDau = LayString(reader, "Ten_KhongDau"),
                        Ma = LayString(reader, "Ma")
                    });
                }
            }

            if (result.Count == 0)
                throw new InvalidOperationException($"Không tìm thấy sản phẩm NVL/BTP theo Ten_KhongDau = '{tenKhongDau}'.");

            if (result.Count > 1)
                throw new InvalidOperationException($"Tên sản phẩm '{tenKhongDau}' đang trùng nhiều dòng trong DanhSachMaSP.");

            return result[0];
        }

        private static string LayString(SQLiteDataReader reader, string col)
        {
            object value = reader[col];
            return value == DBNull.Value ? string.Empty : Convert.ToString(value, CultureInfo.CurrentCulture) ?? string.Empty;
        }

        private static long? LayLong(SQLiteDataReader reader, string col)
        {
            object value = reader[col];
            if (value == DBNull.Value) return null;
            return Convert.ToInt64(value, CultureInfo.InvariantCulture);
        }

        private static double LayDouble(SQLiteDataReader reader, string col)
        {
            object value = reader[col];
            if (value == DBNull.Value) return 0;
            return Convert.ToDouble(value, CultureInfo.CurrentCulture);
        }
    }

    internal sealed class NhapKhoNVL_Dong
    {
        public long? TTThanhPhamId { get; set; }
        public long? DanhSachSPId { get; set; }
        public string Ten { get; set; } = string.Empty;
        public string TenKhongDau { get; set; } = string.Empty;
        public string MaSP { get; set; } = string.Empty;
        public double KhoiLuong { get; set; }
        public double ChieuDai { get; set; }
        public string MaBin { get; set; } = string.Empty;
        public string GhiChu { get; set; } = string.Empty;
    }

    internal sealed class NhapKhoNVL_SanPham
    {
        public long Id { get; set; }
        public string Ten { get; set; } = string.Empty;
        public string TenKhongDau { get; set; } = string.Empty;
        public string Ma { get; set; } = string.Empty;
    }
}
