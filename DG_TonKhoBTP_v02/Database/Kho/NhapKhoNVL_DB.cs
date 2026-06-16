using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Globalization;
using DG_TonKhoBTP_v02.Models;

namespace DG_TonKhoBTP_v02.Database.Kho
{
    internal static class NhapKhoNVL_DB
    {
        private const int CONG_DOAN_NGUYEN_LIEU_ID = -1;

        internal static NhapKhoNVL_Dong LuuMotDong(NhapKhoNVL_Dong input)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            using var conn = DB_Base.OpenConnection();
            using var tran = conn.BeginTransaction();

            try
            {
                NhapKhoNVL_SanPham sp = TimSanPhamNhapKhoTheoTenKhongDauVaCongDoan(
                    conn,
                    tran,
                    input.TenKhongDau,
                    input.CongDoanId);

                string maBin = input.MaBin.Trim();

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
                    cmd.Parameters.AddWithValue("@CongDoan", input.CongDoanId);
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
                    CongDoanId = input.CongDoanId,
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
                NhapKhoNVL_SanPham sp = TimSanPhamNhapKhoTheoTenKhongDauVaCongDoan(
                    conn,
                    tran,
                    input.TenKhongDau,
                    input.CongDoanId);

                const string sql = @"
                    UPDATE TTThanhPham
                    SET DanhSachSP_ID   = @DanhSachSP_ID,
                        MaBin           = @MaBin,
                        KhoiLuongTruoc  = @KhoiLuong,
                        KhoiLuongSau    = @KhoiLuong,
                        ChieuDaiTruoc   = @ChieuDai,
                        ChieuDaiSau     = @ChieuDai,
                        CongDoan        = @CongDoan,
                        GhiChu          = @GhiChu
                    WHERE id = @TTThanhPham_ID;";

                using (var cmd = new SQLiteCommand(sql, conn, tran))
                {
                    cmd.Parameters.AddWithValue("@DanhSachSP_ID", sp.Id);
                    cmd.Parameters.AddWithValue("@MaBin", input.MaBin.Trim());
                    cmd.Parameters.AddWithValue("@KhoiLuong", input.KhoiLuong);
                    cmd.Parameters.AddWithValue("@ChieuDai", input.ChieuDai);
                    cmd.Parameters.AddWithValue("@CongDoan", input.CongDoanId);
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
                    CongDoanId = input.CongDoanId,
                    GhiChu = input.GhiChu ?? string.Empty
                };
            }
            catch
            {
                tran.Rollback();
                throw;
            }
        }

        public static NhapKhoNVL_Dong TimTheoMaBin(string maBin)
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
                        tp.CongDoan        AS CongDoan,
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
                CongDoanId = LayInt(reader, "CongDoan"),
                GhiChu = LayString(reader, "GhiChu"),
                Ten = LayString(reader, "Ten"),
                TenKhongDau = LayString(reader, "TenKhongDau"),
                MaSP = LayString(reader, "MaSP")
            };
        }

        internal static string TaoQrNhapKhoNVL(string zeroDigit, int plus = 0)
        {
            DateTime now = DateTime.Now;

            int middlePart = int.Parse(now.ToString("yyMMdd", CultureInfo.InvariantCulture), CultureInfo.InvariantCulture) + 100000;

            int partAfterSlash = now.Hour;
            int part4 = now.Minute;
            int part5 = now.Second + plus;

            return $"{zeroDigit}-{middlePart}/{partAfterSlash:D2}-{part4:D2}-{part5:D2}";
        }

        private static NhapKhoNVL_SanPham TimSanPhamNhapKhoTheoTenKhongDauVaCongDoan(
            SQLiteConnection conn,
            SQLiteTransaction tran,
            string tenKhongDau,
            int congDoanId)
        {
            tenKhongDau = tenKhongDau?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(tenKhongDau))
                throw new InvalidOperationException("Tên sản phẩm không được để trống.");

            if (congDoanId == CONG_DOAN_NGUYEN_LIEU_ID)
                return TimSanPhamNguyenLieuTheoTenKhongDau(conn, tran, tenKhongDau);

            return TimSanPhamTheoTenKhongDauVaCongDoan(conn, tran, tenKhongDau, congDoanId);
        }

        private static NhapKhoNVL_SanPham TimSanPhamTheoTenKhongDauVaCongDoan(
            SQLiteConnection conn,
            SQLiteTransaction tran,
            string tenKhongDau,
            int congDoanId)
        {
            const string sql = @"
                SELECT  sp.id,
                        sp.Ten,
                        sp.Ten_KhongDau,
                        sp.Ma
                FROM    DanhSachMaSP sp
                WHERE   UPPER(TRIM(IFNULL(sp.Ten_KhongDau, ''))) = UPPER(TRIM(@TenKhongDau))
                    AND UPPER(TRIM(sp.KieuSP)) = 'BTP'
                    AND IFNULL(sp.Active, 1) = 1
                LIMIT   2;";

            List<NhapKhoNVL_SanPham> result = new List<NhapKhoNVL_SanPham>();

            using (var cmd = new SQLiteCommand(sql, conn, tran))
            {
                cmd.Parameters.AddWithValue("@TenKhongDau", tenKhongDau);
                cmd.Parameters.AddWithValue("@CongDoan", congDoanId);

                using SQLiteDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    result.Add(TaoSanPhamNhapKhoTuReader(reader));
                }
            }

            if (result.Count == 0)
                throw new InvalidOperationException("Tên sản phẩm không phù hợp với công đoạn đã chọn.");

            if (result.Count > 1)
                throw new InvalidOperationException("Tên sản phẩm không phù hợp với công đoạn đã chọn.");

            return result[0];
        }

        private static NhapKhoNVL_SanPham TimSanPhamNguyenLieuTheoTenKhongDau(
            SQLiteConnection conn,
            SQLiteTransaction tran,
            string tenKhongDau)
        {
            const string sql = @"
                SELECT  sp.id,
                        sp.Ten,
                        sp.Ten_KhongDau,
                        sp.Ma
                FROM    DanhSachMaSP sp
                WHERE   UPPER(TRIM(IFNULL(sp.Ten_KhongDau, ''))) = UPPER(TRIM(@TenKhongDau))
                    AND UPPER(TRIM(IFNULL(sp.KieuSP, ''))) = 'NVL'
                    AND IFNULL(sp.Active, 1) = 1
                LIMIT   2;";

            List<NhapKhoNVL_SanPham> result = new List<NhapKhoNVL_SanPham>();

            using (var cmd = new SQLiteCommand(sql, conn, tran))
            {
                cmd.Parameters.AddWithValue("@TenKhongDau", tenKhongDau);

                using SQLiteDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    result.Add(TaoSanPhamNhapKhoTuReader(reader));
                }
            }

            if (result.Count == 0)
                throw new InvalidOperationException("Tên nguyên liệu không tồn tại hoặc không hợp lệ.");

            if (result.Count > 1)
                throw new InvalidOperationException("Tên nguyên liệu không tồn tại hoặc không hợp lệ.");

            return result[0];
        }

        private static NhapKhoNVL_SanPham TaoSanPhamNhapKhoTuReader(SQLiteDataReader reader)
        {
            return new NhapKhoNVL_SanPham
            {
                Id = LayLong(reader, "id") ?? 0,
                Ten = LayString(reader, "Ten"),
                TenKhongDau = LayString(reader, "Ten_KhongDau"),
                Ma = LayString(reader, "Ma")
            };
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

        private static int LayInt(SQLiteDataReader reader, string col)
        {
            object value = reader[col];

            if (value == DBNull.Value)
                return 0;

            string text = Convert.ToString(value, CultureInfo.InvariantCulture)?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(text))
                return 0;

            return int.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out int result)
                ? result
                : 0;
        }

        private static double LayDouble(SQLiteDataReader reader, string col)
        {
            object value = reader[col];
            if (value == DBNull.Value) return 0;
            return Convert.ToDouble(value, CultureInfo.CurrentCulture);
        }
    }


}
