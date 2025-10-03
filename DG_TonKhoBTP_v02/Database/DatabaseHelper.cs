
using DG_TonKhoBTP_v02.Core;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Data.SQLite;
using System.Linq;
using System.Windows.Forms;

namespace DG_TonKhoBTP_v02.Database
{
    public static class DatabaseHelper
    {
        public static string _connStr;

        // Thiết lập đường dẫn đến cơ sở dữ liệu SQLite
        public static void SetDatabasePath(string path)
        {
            _connStr = $"Data Source={path};Version=3;";
        }

        public static DataTable GetData(string key, string query, string para)
        {
            using (SQLiteConnection conn = new SQLiteConnection(_connStr))
            {
                conn.Open();

                using (SQLiteCommand cmd = new SQLiteCommand(query, conn))
                {
                    //cmd.Parameters.AddWithValue("@" + para, key);
                    cmd.Parameters.AddWithValue("@para", key);

                    using (SQLiteDataAdapter adapter = new SQLiteDataAdapter(cmd))
                    {
                        DataTable resultTable = new DataTable();
                        adapter.Fill(resultTable);
                        return resultTable;
                    }
                }
            }
        }



        public static bool SaveDataSanPham(
            ThongTinCaLamViec caLam, TTThanhPham tp, List<TTNVL> nvl , List<object> chiTietCD
        )
        {
            using var conn = new SQLiteConnection(_connStr);
            conn.Open();
            using var tx = conn.BeginTransaction();

            CaiDatCDBoc caiDat = (CaiDatCDBoc)chiTietCD[1];

            try
            {
                // 1) ThongTinCaLamViec
                long caId = InsertThongTinCaLamViec(conn, tx, caLam);

                // 2) TTThanhPham
                long tpId = InsertTTThanhPham(conn, tx, tp, caId);

                // 3) TTNVL
                InsertTTNVL(conn, tx, tpId, nvl);

                // 4) CaiDatCDBoc
                if (caiDat != null) InsertCaiDatCDBoc(conn, tx, tpId, caiDat);

                // 5) Thêm chi tiết các công đoạn
                switch (chiTietCD[0])
                {
                    case CD_BocLot lot:
                        InsertCDBocLot(conn, tx, tpId, lot);
                        break;

                    case CD_BocVo vo:
                        InsertCDBocVo(conn, tx, tpId, vo);
                        break;

                    case CD_BocMach mach:
                        InsertCDBocMach(conn, tx, tpId, mach);
                        break;

                    case CD_KeoRut keo:
                        InsertCDKeoRut(conn, tx, tpId, keo);
                        break;

                    case CD_BenRuot ben:
                        InsertCDBenRuot(conn, tx, tpId, ben);
                        break;

                    case CD_GhepLoiQB qb:
                        InsertCDGhepLoiQB(conn, tx, tpId, qb);
                        break;

                    default:
                        throw new ArgumentException("Lỗi bất thường.");
                }

                tx.Commit();
                return true;
            }
            catch
            {
                tx.Rollback();
                return false;
            }
        }


        private static long InsertThongTinCaLamViec(SQLiteConnection conn, SQLiteTransaction tx, ThongTinCaLamViec m)
        {
            const string sql = @"
            INSERT INTO ThongTinCaLamViec (Ngay, May, Ca, NguoiLam, ToTruong, QuanDoc)
            VALUES (@Ngay, @May, @Ca, @NguoiLam, @ToTruong, @QuanDoc);
            SELECT last_insert_rowid();";
            using var cmd = new SQLiteCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@Ngay", m.Ngay);
            cmd.Parameters.AddWithValue("@May", m.May);
            cmd.Parameters.AddWithValue("@Ca", m.Ca);
            cmd.Parameters.AddWithValue("@NguoiLam", m.NguoiLam);
            cmd.Parameters.AddWithValue("@ToTruong", (object?)m.ToTruong ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@QuanDoc", (object?)m.QuanDoc ?? DBNull.Value);
            return (long)(cmd.ExecuteScalar() ?? 0L);
        }

        private static long InsertTTThanhPham(SQLiteConnection conn, SQLiteTransaction tx, TTThanhPham m, long thongTinCaLamViecId)
        {
            const string sql = @"
            INSERT INTO TTThanhPham
                (DanhSachSP_ID, ThongTinCaLamViec_ID, MaBin, KhoiLuongTruoc, KhoiLuongSau, ChieuDaiTruoc, ChieuDaiSau, Phe, GhiChu, DateInsert)
            VALUES
                (@DanhSachSP_ID, @ThongTinCaLamViec_ID, @MaBin, @KhoiLuongTruoc, @KhoiLuongSau, @ChieuDaiTruoc, @ChieuDaiSau, @Phe, @GhiChu, @DateInsert);
            SELECT last_insert_rowid();";
            using var cmd = new SQLiteCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@DanhSachSP_ID", m.DanhSachSP_ID);
            cmd.Parameters.AddWithValue("@ThongTinCaLamViec_ID", thongTinCaLamViecId);
            cmd.Parameters.AddWithValue("@MaBin", m.MaBin);
            cmd.Parameters.AddWithValue("@KhoiLuongTruoc", m.KhoiLuongTruoc);
            cmd.Parameters.AddWithValue("@KhoiLuongSau", m.KhoiLuongSau);
            cmd.Parameters.AddWithValue("@ChieuDaiTruoc", m.ChieuDaiTruoc);
            cmd.Parameters.AddWithValue("@ChieuDaiSau", m.ChieuDaiSau);
            cmd.Parameters.AddWithValue("@Phe", m.Phe);
            cmd.Parameters.AddWithValue("@GhiChu", (object?)m.GhiChu ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@DateInsert", (object?)m.DateInsert ?? DBNull.Value);
            return (long)(cmd.ExecuteScalar() ?? 0L);
        }

        private static void InsertTTNVL(SQLiteConnection conn, SQLiteTransaction tx, long thongTinSpId, List<TTNVL> items)
        {
            if (items == null || items.Count == 0) return;

            const string sql = @"
            INSERT INTO TTNVL
                (ThongTinSP_ID, BinNVL, ConLai, DuongKinhSoiDong, SoSoi, KetCauLoi, DuongKinhSoiMach, BanRongBang, DoDayBang)
            VALUES
                (@ThongTinSP_ID, @BinNVL, @ConLai, @DuongKinhSoiDong, @SoSoi, @KetCauLoi, @DuongKinhSoiMach, @BanRongBang, @DoDayBang);";

            using var cmd = new SQLiteCommand(sql, conn, tx);

            var pThongTinSP_ID = cmd.Parameters.Add("@ThongTinSP_ID", DbType.Int64);
            var pBinNVL = cmd.Parameters.Add("@BinNVL", DbType.String);
            var pConLai = cmd.Parameters.Add("@ConLai", DbType.Double);
            var pDuongKinhSoiDong = cmd.Parameters.Add("@DuongKinhSoiDong", DbType.Double);
            var pSoSoi = cmd.Parameters.Add("@SoSoi", DbType.Int32);
            var pKetCauLoi = cmd.Parameters.Add("@KetCauLoi", DbType.Double);
            var pDuongKinhSoiMach = cmd.Parameters.Add("@DuongKinhSoiMach", DbType.Double);
            var pBanRongBang = cmd.Parameters.Add("@BanRongBang", DbType.Double);
            var pDoDayBang = cmd.Parameters.Add("@DoDayBang", DbType.Double);

            foreach (TTNVL m in items)
            {
                pThongTinSP_ID.Value = thongTinSpId;
                pBinNVL.Value = m.BinNVL ?? string.Empty;
                pConLai.Value = m.ConLai;
                pDuongKinhSoiDong.Value = m.DuongKinhSoiDong;
                pSoSoi.Value = m.SoSoi;
                pKetCauLoi.Value = m.KetCauLoi;
                pDuongKinhSoiMach.Value = m.DuongKinhSoiMach;
                pBanRongBang.Value = m.BanRongBang;
                pDoDayBang.Value = m.DoDayBang;

                cmd.ExecuteNonQuery();
            }
        }

        private static void InsertCDBocLot(SQLiteConnection conn, SQLiteTransaction tx, long thongTinSpId, CD_BocLot m)
        {
            const string sql = @"
            INSERT INTO CD_BocLot (ThongTinSP_ID, DoDayTBLot)
            VALUES (@ThongTinSP_ID, @DoDayTBLot);";

            using var cmd = new SQLiteCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@ThongTinSP_ID", thongTinSpId);
            cmd.Parameters.AddWithValue("@DoDayTBLot", m.DoDayTBLot);
            cmd.ExecuteNonQuery();
        }

        private static void InsertCDBocVo(SQLiteConnection conn, SQLiteTransaction tx, long thongTinSpId, CD_BocVo m)
        {
            const string sql = @"
            INSERT INTO CD_BocVo (ThongTinSP_ID, DayVoTB, InAn)
            VALUES (@ThongTinSP_ID, @DayVoTB, @InAn);";

            using var cmd = new SQLiteCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@ThongTinSP_ID", thongTinSpId);
            cmd.Parameters.AddWithValue("@DayVoTB", m.DayVoTB);
            cmd.Parameters.AddWithValue("@InAn", m.InAn ?? string.Empty);
            cmd.ExecuteNonQuery();
        }

        private static void InsertCaiDatCDBoc(SQLiteConnection conn, SQLiteTransaction tx, long thongTinSpId, CaiDatCDBoc m)
        {
            const string sql = @"
            INSERT INTO CaiDatCDBoc
            (ThongTinSPT_ID, MangNuoc, PuliDanDay, BoDemMet, MayIn,
             v1, v2, v3, v4, v5, v6, Co, Dau1, Dau2, Khuon, BinhSay,
             DKKhuon1, DKKhuon2, TTNhua, NhuaPhe, GhiChuNhuaPhe, DayPhe, GhiChuDayPhe,
             KTDKLan1, KTDKLan2, KTDKLan3, DiemMongLan1, DiemMongLan2)
            VALUES
            (@ThongTinSPT_ID, @MangNuoc, @PuliDanDay, @BoDemMet, @MayIn,
             @v1, @v2, @v3, @v4, @v5, @v6, @Co, @Dau1, @Dau2, @Khuon, @BinhSay,
             @DKKhuon1, @DKKhuon2, @TTNhua, @NhuaPhe, @GhiChuNhuaPhe, @DayPhe, @GhiChuDayPhe,
             @KTDKLan1, @KTDKLan2, @KTDKLan3, @DiemMongLan1, @DiemMongLan2);";

            using var cmd = new SQLiteCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@ThongTinSPT_ID", thongTinSpId);
            cmd.Parameters.AddWithValue("@MangNuoc", m.MangNuoc ? 1 : 0);
            cmd.Parameters.AddWithValue("@PuliDanDay", m.PuliDanDay ? 1 : 0);
            cmd.Parameters.AddWithValue("@BoDemMet", m.BoDemMet ? 1 : 0);
            cmd.Parameters.AddWithValue("@MayIn", m.MayIn.HasValue ? (object)(m.MayIn.Value ? 1 : 0) : DBNull.Value);

            cmd.Parameters.AddWithValue("@v1", (object?)m.v1 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@v2", (object?)m.v2 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@v3", (object?)m.v3 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@v4", (object?)m.v4 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@v5", (object?)m.v5 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@v6", (object?)m.v6 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Co", (object?)m.Co ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Dau1", (object?)m.Dau1 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Dau2", (object?)m.Dau2 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Khuon", (object?)m.Khuon ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@BinhSay", (object?)m.BinhSay ?? DBNull.Value);

            cmd.Parameters.AddWithValue("@DKKhuon1", m.DKKhuon1);
            cmd.Parameters.AddWithValue("@DKKhuon2", m.DKKhuon2);
            cmd.Parameters.AddWithValue("@TTNhua", m.TTNhua ?? string.Empty);
            cmd.Parameters.AddWithValue("@NhuaPhe", m.NhuaPhe);
            cmd.Parameters.AddWithValue("@GhiChuNhuaPhe", (object?)m.GhiChuNhuaPhe ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@DayPhe", m.DayPhe);
            cmd.Parameters.AddWithValue("@GhiChuDayPhe", (object?)m.GhiChuDayPhe ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@KTDKLan1", m.KTDKLan1);
            cmd.Parameters.AddWithValue("@KTDKLan2", m.KTDKLan2);
            cmd.Parameters.AddWithValue("@KTDKLan3", m.KTDKLan3);
            cmd.Parameters.AddWithValue("@DiemMongLan1", m.DiemMongLan1);
            cmd.Parameters.AddWithValue("@DiemMongLan2", m.DiemMongLan2);

            cmd.ExecuteNonQuery();
        }

        private static void InsertCDBocMach(SQLiteConnection conn, SQLiteTransaction tx, long thongTinSpId, CD_BocMach m)
        {
            const string sql = @"
            INSERT INTO CD_BocMach (ThongTinSP_ID, NgoaiQuan, LanDanhThung, SoMet)
            VALUES (@ThongTinSP_ID, @NgoaiQuan, @LanDanhThung, @SoMet);";

            using var cmd = new SQLiteCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@ThongTinSP_ID", thongTinSpId);
            cmd.Parameters.AddWithValue("@NgoaiQuan", m.NgoaiQuan ?? "1"); // default theo schema
            cmd.Parameters.AddWithValue("@LanDanhThung", m.LanDanhThung);
            cmd.Parameters.AddWithValue("@SoMet", m.SoMet);
            cmd.ExecuteNonQuery();
        }

        private static void InsertCDKeoRut(SQLiteConnection conn, SQLiteTransaction tx, long thongTinSpId, CD_KeoRut m)
        {
            const string sql = @"
            INSERT INTO CD_KeoRut
            (ThongTinSP_ID, DKTrucX, DKTrucY, NgoaiQuan, TocDo, DienApU, DongDienU)
            VALUES
            (@ThongTinSP_ID, @DKTrucX, @DKTrucY, @NgoaiQuan, @TocDo, @DienApU, @DongDienU);";

            using var cmd = new SQLiteCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@ThongTinSP_ID", thongTinSpId);
            cmd.Parameters.AddWithValue("@DKTrucX", m.DKTrucX);
            cmd.Parameters.AddWithValue("@DKTrucY", m.DKTrucY);
            cmd.Parameters.AddWithValue("@NgoaiQuan", m.NgoaiQuan ?? string.Empty);
            cmd.Parameters.AddWithValue("@TocDo", m.TocDo);
            cmd.Parameters.AddWithValue("@DienApU", m.DienApU);
            cmd.Parameters.AddWithValue("@DongDienU", m.DongDienU);
            cmd.ExecuteNonQuery();
        }

        private static void InsertCDBenRuot(SQLiteConnection conn, SQLiteTransaction tx, long thongTinSpId, CD_BenRuot m)
        {
            // Lưu ý cột "Chiều Xoắn" có dấu và khoảng trắng -> cần trích dẫn bằng dấu "
            const string sql = @"
            INSERT INTO CD_BenRuot
            (ThongTinSP_ID, DKSoi, SoSoi, ""Chiều Xoắn"", BuocBen)
            VALUES
            (@ThongTinSP_ID, @DKSoi, @SoSoi, @ChieuXoan, @BuocBen);";

            using var cmd = new SQLiteCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@ThongTinSP_ID", thongTinSpId);
            cmd.Parameters.AddWithValue("@DKSoi", m.DKSoi);
            cmd.Parameters.AddWithValue("@SoSoi", (object?)m.SoSoi ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ChieuXoan", m.ChieuXoan ?? "Z");
            cmd.Parameters.AddWithValue("@BuocBen", m.BuocBen);
            cmd.ExecuteNonQuery();
        }

        private static void InsertCDGhepLoiQB(SQLiteConnection conn, SQLiteTransaction tx, long thongTinSpId, CD_GhepLoiQB m)
        {
            const string sql = @"
            INSERT INTO CD_GhepLoiQB
            (ThongTinSP_ID, BuocXoan, ChieuXoan, GoiCachMep, DKBTP)
            VALUES
            (@ThongTinSP_ID, @BuocXoan, @ChieuXoan, @GoiCachMep, @DKBTP);";

            using var cmd = new SQLiteCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@ThongTinSP_ID", thongTinSpId);
            cmd.Parameters.AddWithValue("@BuocXoan", m.BuocXoan);
            cmd.Parameters.AddWithValue("@ChieuXoan", m.ChieuXoan ?? "Z");
            cmd.Parameters.AddWithValue("@GoiCachMep", m.GoiCachMep);
            cmd.Parameters.AddWithValue("@DKBTP", m.DKBTP);
            cmd.ExecuteNonQuery();
        }


    }


}
