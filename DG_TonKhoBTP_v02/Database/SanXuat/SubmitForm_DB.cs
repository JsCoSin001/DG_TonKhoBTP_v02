using DG_TonKhoBTP_v02.Core;
using DG_TonKhoBTP_v02.Models;
using DG_TonKhoBTP_v02.Models.SanXuat;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Globalization;
using System.Linq;
using CoreHelper = DG_TonKhoBTP_v02.Helper.Helper;

namespace DG_TonKhoBTP_v02.Database.SanXuat
{
    internal static class SubmitForm_DB
    {
        public static List<PrinterModel> GetPrinterDataByListBin(List<string> listBin)
        {
            var result = new List<PrinterModel>();

            if (listBin == null || listBin.Count == 0)
                return result;

            var paramNames = listBin.Select((bin, index) => "@bin" + index).ToList();
            string inClause = string.Join(",", paramNames);

            string query = $@"
                SELECT  
                    t.Ngay AS NgaySX,
                    t.Ca AS CaSX,
                    tp.QC AS QC,
                    tp.KhoiLuongSau AS KhoiLuong,
                    tp.ChieuDaiSau AS ChieuDai,
                    d.ten AS TenSP,
                    tp.MaBin AS MaBin,
                    d.ma AS MaSP,
                    t.NguoiLam AS TenCN,
                    tp.GhiChu AS GhiChu
                FROM TTThanhPham tp
                LEFT JOIN ThongTinCaLamViec t ON t.TTThanhPham_id = tp.id
                JOIN DanhSachMaSP d ON tp.DanhSachSP_ID = d.id
                WHERE tp.MaBin IN ({inClause});
                ";

            using (SQLiteConnection conn = DB_Base.OpenConnection())
            using (SQLiteCommand cmd = new SQLiteCommand(query, conn))
            {
                for (int i = 0; i < listBin.Count; i++)
                    cmd.Parameters.AddWithValue("@bin" + i, listBin[i]);

                using (SQLiteDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string qc = CoreHelper.GetString(reader, "QC").Trim();
                        string ghiChu = CoreHelper.GetString(reader, "GhiChu");

                        result.Add(new PrinterModel
                        {
                            NgaySX = DateTime.TryParse(CoreHelper.GetString(reader, "NgaySX"), out DateTime d)
                                ? d.ToString("dd/MM/yyyy")
                                : "",
                            CaSX = CoreHelper.GetString(reader, "CaSX"),
                            KhoiLuong = CoreHelper.GetString(reader, "KhoiLuong"),
                            ChieuDai = CoreHelper.GetString(reader, "ChieuDai"),
                            TenSP = CoreHelper.GetString(reader, "TenSP"),
                            MaBin = CoreHelper.GetString(reader, "MaBin"),
                            MaSP = CoreHelper.GetString(reader, "MaSP"),
                            DanhGia = "",
                            QC = qc,
                            TenCN = CoreHelper.GetString(reader, "TenCN"),
                            GhiChu = ghiChu
                        });
                    }
                }
            }

            return result;
        }


        /// <summary>
        /// Lưu bản ghi tạo mới của công đoạn 9.
        /// Chỉ insert TTThanhPham và ThongTinCaLamViec;
        /// không insert TTNVL, KhacBietBOM hoặc bảng chi tiết công đoạn.
        /// </summary>
        public static bool SaveDataCongDoan9(ThongTinCaLamViec caLamViec, TTThanhPham thanhPham, out string error)
        {
            error = string.Empty;

            if (caLamViec == null)
            {
                error = "Thông tin ca làm việc không hợp lệ.";
                return false;
            }

            if (thanhPham == null)
            {
                error = "Thông tin thành phẩm không hợp lệ.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(DatabaseHelper.GetStringConnector))
            {
                error = "Chưa thiết lập kết nối cơ sở dữ liệu.";
                return false;
            }

            using SQLiteConnection conn = DB_Base.OpenConnection();
            using SQLiteTransaction tx = conn.BeginTransaction();

            try
            {
                thanhPham.HanNoi = 1;

                const string insertThanhPhamSql = @"
                    INSERT INTO TTThanhPham
                    (
                        DanhSachSP_ID,
                        QC,
                        MaBin,
                        KhoiLuongTruoc,
                        KhoiLuongSau,
                        ChieuDaiTruoc,
                        ChieuDaiSau,
                        Phe,
                        CongDoan,
                        GhiChu,
                        HanNoi,
                        DateInsert,
                        LastEdit_ID
                    )
                    VALUES
                    (
                        @DanhSachSP_ID,
                        @QC,
                        @MaBin,
                        @KhoiLuongTruoc,
                        @KhoiLuongSau,
                        @ChieuDaiTruoc,
                        @ChieuDaiSau,
                        @Phe,
                        9,
                        @GhiChu,
                        1,
                        @DateInsert,
                        NULL
                    );

                    SELECT last_insert_rowid();";

                long thanhPhamId;

                using (var cmd = new SQLiteCommand(
                    insertThanhPhamSql,
                    conn,
                    tx))
                {
                    cmd.Parameters.AddWithValue(
                        "@DanhSachSP_ID",
                        thanhPham.DanhSachSP_ID);

                    cmd.Parameters.AddWithValue(
                        "@QC",
                        (object)thanhPham.QC ?? DBNull.Value);

                    cmd.Parameters.AddWithValue(
                        "@MaBin",
                        (object)thanhPham.MaBin ?? DBNull.Value);

                    cmd.Parameters.AddWithValue(
                        "@KhoiLuongTruoc",
                        thanhPham.KhoiLuongTruoc);

                    cmd.Parameters.AddWithValue(
                        "@KhoiLuongSau",
                        thanhPham.KhoiLuongSau);

                    cmd.Parameters.AddWithValue(
                        "@ChieuDaiTruoc",
                        thanhPham.ChieuDaiTruoc);

                    cmd.Parameters.AddWithValue(
                        "@ChieuDaiSau",
                        thanhPham.ChieuDaiSau);

                    cmd.Parameters.AddWithValue(
                        "@Phe",
                        thanhPham.Phe);

                    cmd.Parameters.AddWithValue(
                        "@GhiChu",
                        (object)thanhPham.GhiChu ?? DBNull.Value);

                    cmd.Parameters.AddWithValue(
                        "@DateInsert",
                        (object)thanhPham.DateInsert ?? DBNull.Value);

                    object scalar = cmd.ExecuteScalar();
                    thanhPhamId = Convert.ToInt64(scalar ?? 0L);
                }

                if (thanhPhamId <= 0)
                {
                    throw new InvalidOperationException(
                        "Không tạo được bản ghi TTThanhPham cho công đoạn 9.");
                }

                const string insertCaLamViecSql = @"
                    INSERT INTO ThongTinCaLamViec
                    (
                        Ngay,
                        TTThanhPham_id,
                        May,
                        Ca,
                        NguoiLam,
                        ToTruong,
                        QuanDoc
                    )
                    VALUES
                    (
                        @Ngay,
                        @TTThanhPham_id,
                        @May,
                        @Ca,
                        @NguoiLam,
                        @ToTruong,
                        @QuanDoc
                    );";

                using (var cmd = new SQLiteCommand(
                    insertCaLamViecSql,
                    conn,
                    tx))
                {
                    cmd.Parameters.AddWithValue(
                        "@Ngay",
                        (object)caLamViec.Ngay ?? DBNull.Value);

                    cmd.Parameters.AddWithValue(
                        "@TTThanhPham_id",
                        thanhPhamId);

                    cmd.Parameters.AddWithValue(
                        "@May",
                        (object)caLamViec.May ?? DBNull.Value);

                    cmd.Parameters.AddWithValue(
                        "@Ca",
                        (object)caLamViec.Ca ?? DBNull.Value);

                    cmd.Parameters.AddWithValue(
                        "@NguoiLam",
                        (object)caLamViec.NguoiLam ?? DBNull.Value);

                    cmd.Parameters.AddWithValue(
                        "@ToTruong",
                        (object)caLamViec.ToTruong ?? DBNull.Value);

                    cmd.Parameters.AddWithValue(
                        "@QuanDoc",
                        (object)caLamViec.QuanDoc ?? DBNull.Value);

                    int affectedRows = cmd.ExecuteNonQuery();
                    if (affectedRows != 1)
                    {
                        throw new InvalidOperationException(
                            "Không tạo được bản ghi ThongTinCaLamViec.");
                    }
                }

                tx.Commit();
                return true;
            }
            catch (Exception ex)
            {
                try
                {
                    tx.Rollback();
                }
                catch
                {
                    // Không để lỗi rollback che mất lỗi lưu chính.
                }

                error = CoreHelper.ShowErrorDatabase(ex);
                return false;
            }
        }

        /// <summary>
        /// Cập nhật bản ghi công đoạn 9.
        /// Chỉ cập nhật TTThanhPham và ThongTinCaLamViec;
        /// không kiểm tra, insert, update hoặc delete dữ liệu TTNVL.
        /// </summary>
        public static bool UpdateDataCongDoan9(
            int idEdit,
            ThongTinCaLamViec caLamViec,
            TTThanhPham thanhPham,
            string confirmedUsername,
            out string error)
        {
            error = string.Empty;

            if (idEdit <= 0)
            {
                error = "ID bản ghi cần sửa không hợp lệ.";
                return false;
            }

            if (caLamViec == null)
            {
                error = "Thông tin ca làm việc không hợp lệ.";
                return false;
            }

            if (thanhPham == null)
            {
                error = "Thông tin thành phẩm không hợp lệ.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(confirmedUsername))
            {
                error = "Username của tổ trưởng xác nhận không hợp lệ.";
                return false;
            }

            confirmedUsername = confirmedUsername.Trim();

            if (string.IsNullOrWhiteSpace(DatabaseHelper.GetStringConnector))
            {
                error = "Chưa thiết lập kết nối cơ sở dữ liệu.";
                return false;
            }

            using SQLiteConnection conn = DB_Base.OpenConnection();
            using SQLiteTransaction tx = conn.BeginTransaction();

            try
            {
                thanhPham.HanNoi = 1;

                const string updateThanhPhamSql = @"
                    UPDATE TTThanhPham
                    SET
                        DanhSachSP_ID = @DanhSachSP_ID,
                        QC = @QC,
                        MaBin = @MaBin,
                        KhoiLuongTruoc = @KhoiLuongTruoc,
                        KhoiLuongSau = @KhoiLuongSau,
                        ChieuDaiTruoc = @ChieuDaiTruoc,
                        ChieuDaiSau = @ChieuDaiSau,
                        Phe = @Phe,
                        CongDoan = 9,
                        GhiChu = @GhiChu,
                        HanNoi = 1,
                        LastEdit_ID = NULL
                    WHERE id = @id
                      AND CongDoan = 9;";

                using (var cmd = new SQLiteCommand(
                    updateThanhPhamSql,
                    conn,
                    tx))
                {
                    cmd.Parameters.AddWithValue(
                        "@DanhSachSP_ID",
                        thanhPham.DanhSachSP_ID);

                    cmd.Parameters.AddWithValue(
                        "@QC",
                        (object)thanhPham.QC ?? DBNull.Value);

                    cmd.Parameters.AddWithValue(
                        "@MaBin",
                        (object)thanhPham.MaBin ?? DBNull.Value);

                    cmd.Parameters.AddWithValue(
                        "@KhoiLuongTruoc",
                        thanhPham.KhoiLuongTruoc);

                    cmd.Parameters.AddWithValue(
                        "@KhoiLuongSau",
                        thanhPham.KhoiLuongSau);

                    cmd.Parameters.AddWithValue(
                        "@ChieuDaiTruoc",
                        thanhPham.ChieuDaiTruoc);

                    cmd.Parameters.AddWithValue(
                        "@ChieuDaiSau",
                        thanhPham.ChieuDaiSau);

                    cmd.Parameters.AddWithValue(
                        "@Phe",
                        thanhPham.Phe);

                    cmd.Parameters.AddWithValue(
                        "@GhiChu",
                        (object)thanhPham.GhiChu ?? DBNull.Value);

                    cmd.Parameters.AddWithValue(
                        "@id",
                        idEdit);

                    int affectedRows = cmd.ExecuteNonQuery();
                    if (affectedRows != 1)
                    {
                        throw new InvalidOperationException(
                            "Không tìm thấy bản ghi công đoạn 9 cần sửa.");
                    }
                }

                const string updateCaLamViecSql = @"
                    UPDATE ThongTinCaLamViec
                    SET
                        Ngay = @Ngay,
                        May = @May,
                        Ca = @Ca,
                        NguoiLam = @NguoiLam,
                        ToTruong = @ToTruong,
                        QuanDoc = @QuanDoc
                    WHERE TTThanhPham_id = @TTThanhPham_id;";

                int caLamAffected;

                using (var cmd = new SQLiteCommand(
                    updateCaLamViecSql,
                    conn,
                    tx))
                {
                    cmd.Parameters.AddWithValue(
                        "@Ngay",
                        (object)caLamViec.Ngay ?? DBNull.Value);

                    cmd.Parameters.AddWithValue(
                        "@May",
                        (object)caLamViec.May ?? DBNull.Value);

                    cmd.Parameters.AddWithValue(
                        "@Ca",
                        (object)caLamViec.Ca ?? DBNull.Value);

                    cmd.Parameters.AddWithValue(
                        "@NguoiLam",
                        (object)caLamViec.NguoiLam ?? DBNull.Value);

                    cmd.Parameters.AddWithValue(
                        "@ToTruong",
                        (object)caLamViec.ToTruong ?? DBNull.Value);

                    cmd.Parameters.AddWithValue(
                        "@QuanDoc",
                        (object)caLamViec.QuanDoc ?? DBNull.Value);

                    cmd.Parameters.AddWithValue(
                        "@TTThanhPham_id",
                        idEdit);

                    caLamAffected = cmd.ExecuteNonQuery();
                }

                // Phòng trường hợp bản ghi cũ chưa có thông tin ca làm việc.
                if (caLamAffected == 0)
                {
                    const string insertCaLamViecSql = @"
                        INSERT INTO ThongTinCaLamViec
                        (
                            Ngay,
                            TTThanhPham_id,
                            May,
                            Ca,
                            NguoiLam,
                            ToTruong,
                            QuanDoc
                        )
                        VALUES
                        (
                            @Ngay,
                            @TTThanhPham_id,
                            @May,
                            @Ca,
                            @NguoiLam,
                            @ToTruong,
                            @QuanDoc
                        );";

                    using var cmd = new SQLiteCommand(
                        insertCaLamViecSql,
                        conn,
                        tx);

                    cmd.Parameters.AddWithValue(
                        "@Ngay",
                        (object)caLamViec.Ngay ?? DBNull.Value);

                    cmd.Parameters.AddWithValue(
                        "@TTThanhPham_id",
                        idEdit);

                    cmd.Parameters.AddWithValue(
                        "@May",
                        (object)caLamViec.May ?? DBNull.Value);

                    cmd.Parameters.AddWithValue(
                        "@Ca",
                        (object)caLamViec.Ca ?? DBNull.Value);

                    cmd.Parameters.AddWithValue(
                        "@NguoiLam",
                        (object)caLamViec.NguoiLam ?? DBNull.Value);

                    cmd.Parameters.AddWithValue(
                        "@ToTruong",
                        (object)caLamViec.ToTruong ?? DBNull.Value);

                    cmd.Parameters.AddWithValue(
                        "@QuanDoc",
                        (object)caLamViec.QuanDoc ?? DBNull.Value);

                    int insertedRows = cmd.ExecuteNonQuery();
                    if (insertedRows != 1)
                    {
                        throw new InvalidOperationException(
                            "Không cập nhật được ThongTinCaLamViec.");
                    }
                }

                InsertChapNhanSuaDLByToTruong(
                    conn,
                    tx,
                    idEdit,
                    confirmedUsername);

                tx.Commit();
                return true;
            }
            catch (Exception ex)
            {
                try
                {
                    tx.Rollback();
                }
                catch
                {
                    // Không để lỗi rollback che mất lỗi cập nhật chính.
                }

                error = CoreHelper.ShowErrorDatabase(ex);
                return false;
            }
        }

        public static bool SaveDataSanPham(
            ThongTinCaLamViec caLam,
            TTThanhPham tp,
            List<TTNVL> nvl,
            SubmitCongDoanData chiTietCD,
            List<TTNVLRow> nvlRowsForBomDiff,
            out string errorMsg)
        {
            errorMsg = string.Empty;

            if (tp == null)
            {
                errorMsg = "Thiếu thông tin thành phẩm.";
                return false;
            }

            if (chiTietCD == null || chiTietCD.ChiTietCongDoan == null)
            {
                errorMsg = "Thiếu chi tiết công đoạn.";
                return false;
            }

            long idCaiDatCDBoc = 0;
            SQLiteConnection conn = null;
            SQLiteTransaction tx = null;

            try
            {
                conn = DB_Base.OpenConnection();
                tx = conn.BeginTransaction();

                long tpId = InsertTTThanhPham(conn, tx, tp, nvl);
                InsertThongTinCaLamViec(conn, tx, caLam, tpId);
                InsertTTNVL(conn, tx, tpId, nvl);
                UpdateKL_CD_TTThanhPham(conn, tx, nvl, tpId);

                object congDoan = chiTietCD.ChiTietCongDoan;

                if (congDoan is CD_BocLot || congDoan is CD_BocVo || congDoan is CD_BocMach)
                {
                    if (chiTietCD.CaiDatCDBoc != null)
                        idCaiDatCDBoc = InsertCaiDatCDBoc(conn, tx, tpId, chiTietCD.CaiDatCDBoc);
                }

                switch (congDoan)
                {
                    case CD_KeoRut keo:
                        InsertCDKeoRut(conn, tx, tpId, keo);
                        break;

                    case CD_BenRuot ben:
                        InsertCDBenRuot(conn, tx, tpId, ben);
                        break;

                    case CD_GhepLoiQB qb:
                        InsertCDGhepLoiQB(conn, tx, tpId, qb);
                        break;

                    case CD_BocLot bocLot:
                        InsertCDBocLot(conn, tx, idCaiDatCDBoc, bocLot);
                        break;

                    case CD_BocMach mach:
                        InsertCDBocMach(conn, tx, idCaiDatCDBoc, mach);
                        break;

                    case CD_BocVo vo:
                        InsertCDBocVo(conn, tx, idCaiDatCDBoc, vo);
                        break;

                    default:
                        throw new ArgumentException("Lỗi bất thường: Công đoạn không hợp lệ.");
                }

                LuuKhacBietBOM(conn, tx, tpId, nvlRowsForBomDiff, tp);

                tx.Commit();
                return true;
            }
            catch (Exception ex)
            {
                try { tx?.Rollback(); } catch { }

                errorMsg = CoreHelper.ShowErrorDatabase(ex, tp.MaBin);
                return false;
            }
            finally
            {
                tx?.Dispose();
                conn?.Dispose();
            }
        }

        /// <summary>
        /// Lưu sản phẩm theo luồng mới của UC_SubmitForm.
        /// Không gọi LuuKhacBietBOM; thay vào đó ghi từng nội dung lỗi
        /// vào bảng DanhSachLoiNhapLieuSX trong cùng transaction.
        /// </summary>
        public static bool SaveDataSanPhamVoiDanhSachLoiNhapLieu(
            ThongTinCaLamViec caLam,
            TTThanhPham tp,
            List<TTNVL> nvl,
            SubmitCongDoanData chiTietCD,
            List<LoiNhapLieuData> danhSachLoiNhapLieu,
            out string errorMsg)
        {
            errorMsg = string.Empty;

            if (tp == null)
            {
                errorMsg = "Thiếu thông tin thành phẩm.";
                return false;
            }

            if (chiTietCD == null || chiTietCD.ChiTietCongDoan == null)
            {
                errorMsg = "Thiếu chi tiết công đoạn.";
                return false;
            }

            long idCaiDatCDBoc = 0;
            SQLiteConnection conn = null;
            SQLiteTransaction tx = null;

            try
            {
                conn = DB_Base.OpenConnection();
                tx = conn.BeginTransaction();

                long tpId = InsertTTThanhPham(conn, tx, tp, nvl);
                InsertThongTinCaLamViec(conn, tx, caLam, tpId);
                InsertTTNVL(conn, tx, tpId, nvl);
                UpdateKL_CD_TTThanhPham(conn, tx, nvl, tpId);

                object congDoan = chiTietCD.ChiTietCongDoan;

                if (congDoan is CD_BocLot || congDoan is CD_BocVo || congDoan is CD_BocMach)
                {
                    if (chiTietCD.CaiDatCDBoc != null)
                        idCaiDatCDBoc = InsertCaiDatCDBoc(conn, tx, tpId, chiTietCD.CaiDatCDBoc);
                }

                switch (congDoan)
                {
                    case CD_KeoRut keo:
                        InsertCDKeoRut(conn, tx, tpId, keo);
                        break;

                    case CD_BenRuot ben:
                        InsertCDBenRuot(conn, tx, tpId, ben);
                        break;

                    case CD_GhepLoiQB qb:
                        InsertCDGhepLoiQB(conn, tx, tpId, qb);
                        break;

                    case CD_BocLot bocLot:
                        InsertCDBocLot(conn, tx, idCaiDatCDBoc, bocLot);
                        break;

                    case CD_BocMach mach:
                        InsertCDBocMach(conn, tx, idCaiDatCDBoc, mach);
                        break;

                    case CD_BocVo vo:
                        InsertCDBocVo(conn, tx, idCaiDatCDBoc, vo);
                        break;

                    default:
                        throw new ArgumentException("Lỗi bất thường: Công đoạn không hợp lệ.");
                }

                LuuDanhSachLoiNhapLieuSX(
                    conn,
                    tx,
                    tpId,
                    danhSachLoiNhapLieu);

                tx.Commit();
                return true;
            }
            catch (Exception ex)
            {
                try { tx?.Rollback(); } catch { }

                errorMsg = CoreHelper.ShowErrorDatabase(ex, tp.MaBin);
                return false;
            }
            finally
            {
                tx?.Dispose();
                conn?.Dispose();
            }
        }

        public static bool UpdateDataSanPham(
            int tpId,
            ThongTinCaLamViec caLam,
            TTThanhPham tp,
            List<TTNVL> nvl,
            SubmitCongDoanData chiTietCD,
            List<TTNVLRow> nvlRowsForBomDiff,
            string confirmedUsername,
            out string errorMsg)
        {
            errorMsg = string.Empty;

            if (tp == null)
            {
                errorMsg = "Thiếu thông tin thành phẩm.";
                return false;
            }

            if (chiTietCD == null || chiTietCD.ChiTietCongDoan == null)
            {
                errorMsg = "Thiếu chi tiết công đoạn.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(confirmedUsername))
            {
                errorMsg = "Username của tổ trưởng xác nhận không hợp lệ.";
                return false;
            }

            confirmedUsername = confirmedUsername.Trim();

            SQLiteConnection conn = null;
            SQLiteTransaction tx = null;

            try
            {
                conn = DB_Base.OpenConnection();
                tx = conn.BeginTransaction();

                BackupThongTinTruocKhiSua(conn, tx, tpId, tp, caLam.NguoiLam);
                UpdateThongTinCaLamViec(conn, tx, caLam, tpId);
                UpdateTTThanhPham(conn, tx, tp, tpId, nvl);
                RestoreFromNVL(conn, tx, tpId);
                UpdateKhoiLuongConLai_TTThanhPham(conn, tx, nvl, tpId);
                Del_InsertTTNVL(conn, tx, tpId, nvl);

                if (chiTietCD.CaiDatCDBoc != null)
                    UpdateCaiDatCDBoc(conn, tx, tpId, chiTietCD.CaiDatCDBoc);

                switch (chiTietCD.ChiTietCongDoan)
                {
                    case CD_BenRuot ben:
                        UpdateCDBenRuot(conn, tx, tpId, ben);
                        break;

                    case CD_KeoRut keo:
                        UpdateCDKeoRut(conn, tx, tpId, keo);
                        break;

                    case CD_GhepLoiQB qb:
                        UpdateCDGhepLoiQB(conn, tx, tpId, qb);
                        break;

                    case CD_BocMach mach:
                        UpdateCDBocMach(conn, tx, tpId, mach);
                        break;

                    case CD_BocLot lotBL:
                        UpdateCDBocLot(conn, tx, tpId, lotBL);
                        break;

                    case CD_BocVo vo:
                        UpdateCDBocVo(conn, tx, tpId, vo);
                        break;

                    default:
                        throw new ArgumentException("Lỗi bất thường.");
                }

                LuuKhacBietBOM(conn, tx, tpId, nvlRowsForBomDiff, tp);

                InsertChapNhanSuaDLByToTruong(
                    conn,
                    tx,
                    tpId,
                    confirmedUsername);

                tx.Commit();
                return true;
            }
            catch (Exception ex)
            {
                try { tx?.Rollback(); } catch { }

                errorMsg = CoreHelper.ShowErrorDatabase(ex, tp.MaBin);
                return false;
            }
            finally
            {
                tx?.Dispose();
                conn?.Dispose();
            }
        }

        /// <summary>
        /// Cập nhật sản phẩm theo luồng mới của UC_SubmitForm.
        /// Danh sách lỗi cũ được xóa và thay bằng kết quả kiểm tra mới.
        /// Không gọi LuuKhacBietBOM.
        /// </summary>
        public static bool UpdateDataSanPhamVoiDanhSachLoiNhapLieu(
            int tpId,
            ThongTinCaLamViec caLam,
            TTThanhPham tp,
            List<TTNVL> nvl,
            SubmitCongDoanData chiTietCD,
            List<LoiNhapLieuData> danhSachLoiNhapLieu,
            string confirmedUsername,
            out string errorMsg)
        {
            errorMsg = string.Empty;

            if (tp == null)
            {
                errorMsg = "Thiếu thông tin thành phẩm.";
                return false;
            }

            if (chiTietCD == null || chiTietCD.ChiTietCongDoan == null)
            {
                errorMsg = "Thiếu chi tiết công đoạn.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(confirmedUsername))
            {
                errorMsg = "Username của tổ trưởng xác nhận không hợp lệ.";
                return false;
            }

            confirmedUsername = confirmedUsername.Trim();

            SQLiteConnection conn = null;
            SQLiteTransaction tx = null;

            try
            {
                conn = DB_Base.OpenConnection();
                tx = conn.BeginTransaction();

                BackupThongTinTruocKhiSua(conn, tx, tpId, tp, caLam.NguoiLam);
                UpdateThongTinCaLamViec(conn, tx, caLam, tpId);
                UpdateTTThanhPham(conn, tx, tp, tpId, nvl);
                RestoreFromNVL(conn, tx, tpId);
                UpdateKhoiLuongConLai_TTThanhPham(conn, tx, nvl, tpId);
                Del_InsertTTNVL(conn, tx, tpId, nvl);

                if (chiTietCD.CaiDatCDBoc != null)
                    UpdateCaiDatCDBoc(conn, tx, tpId, chiTietCD.CaiDatCDBoc);

                switch (chiTietCD.ChiTietCongDoan)
                {
                    case CD_BenRuot ben:
                        UpdateCDBenRuot(conn, tx, tpId, ben);
                        break;

                    case CD_KeoRut keo:
                        UpdateCDKeoRut(conn, tx, tpId, keo);
                        break;

                    case CD_GhepLoiQB qb:
                        UpdateCDGhepLoiQB(conn, tx, tpId, qb);
                        break;

                    case CD_BocMach mach:
                        UpdateCDBocMach(conn, tx, tpId, mach);
                        break;

                    case CD_BocLot lotBL:
                        UpdateCDBocLot(conn, tx, tpId, lotBL);
                        break;

                    case CD_BocVo vo:
                        UpdateCDBocVo(conn, tx, tpId, vo);
                        break;

                    default:
                        throw new ArgumentException("Lỗi bất thường.");
                }

                LuuDanhSachLoiNhapLieuSX(
                    conn,
                    tx,
                    tpId,
                    danhSachLoiNhapLieu);

                InsertChapNhanSuaDLByToTruong(
                    conn,
                    tx,
                    tpId,
                    confirmedUsername);

                tx.Commit();
                return true;
            }
            catch (Exception ex)
            {
                try { tx?.Rollback(); } catch { }

                errorMsg = CoreHelper.ShowErrorDatabase(ex, tp.MaBin);
                return false;
            }
            finally
            {
                tx?.Dispose();
                conn?.Dispose();
            }
        }

        private static void InsertChapNhanSuaDLByToTruong(
            SQLiteConnection conn,
            SQLiteTransaction tx,
            long ttThanhPhamId,
            string username)
        {
            if (conn == null)
                throw new ArgumentNullException(nameof(conn));

            if (tx == null)
                throw new ArgumentNullException(nameof(tx));

            if (ttThanhPhamId <= 0)
                throw new ArgumentOutOfRangeException(
                    nameof(ttThanhPhamId),
                    "ID thành phẩm cần xác nhận không hợp lệ.");

            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentException(
                    "Username của tổ trưởng xác nhận không hợp lệ.",
                    nameof(username));

            const string sql = @"
                INSERT INTO ChapNhanSuaDL_ByToTruong
                (
                    TTThanhPham_ID,
                    UserName,
                    DateInsert
                )
                VALUES
                (
                    @TTThanhPham_ID,
                    @UserName,
                    @DateInsert
                );";

            using var cmd = new SQLiteCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@TTThanhPham_ID", ttThanhPhamId);
            cmd.Parameters.AddWithValue("@UserName", username.Trim());
            cmd.Parameters.AddWithValue(
                "@DateInsert",
                DateTime.Now.ToString(
                    "yyyy-MM-dd HH:mm:ss",
                    CultureInfo.InvariantCulture));

            int affectedRows = cmd.ExecuteNonQuery();
            if (affectedRows != 1)
            {
                throw new InvalidOperationException(
                    "Không lưu được thông tin tổ trưởng chấp nhận sửa dữ liệu.");
            }
        }

        private static void UpdateKL_CD_TTThanhPham(SQLiteConnection conn, SQLiteTransaction tx, List<TTNVL> nvlList, long thongTinSpId)
        {
            const string sql = @"
                UPDATE TTThanhPham
                SET KhoiLuongSau = COALESCE(@KhoiLuongSau, KhoiLuongSau),
                    ChieuDaiSau = COALESCE(@ChieuDaiSau, ChieuDaiSau),
                    QC = @QC,
                    LastEdit_ID = @LastEdit_ID
                WHERE MaBin = @MaBin;";

            using var cmd = new SQLiteCommand(sql, conn, tx);
            cmd.Parameters.Add("@KhoiLuongSau", System.Data.DbType.Double);
            cmd.Parameters.Add("@ChieuDaiSau", System.Data.DbType.Double);
            cmd.Parameters.Add("@LastEdit_ID", System.Data.DbType.Int64);
            cmd.Parameters.Add("@MaBin", System.Data.DbType.String);
            cmd.Parameters.Add("@QC", System.Data.DbType.String);

            foreach (var nvl in nvlList)
            {
                cmd.Parameters["@KhoiLuongSau"].Value = DbValueOrNull(nvl.KlConLai);
                cmd.Parameters["@ChieuDaiSau"].Value = DbValueOrNull(nvl.CdConLai);
                cmd.Parameters["@LastEdit_ID"].Value = thongTinSpId;
                cmd.Parameters["@QC"].Value = nvl.QC;
                cmd.Parameters["@MaBin"].Value = nvl.BinNVL;

                cmd.ExecuteNonQuery();
            }
        }

        private static void BackupThongTinTruocKhiSua(SQLiteConnection conn, SQLiteTransaction tx, long tpId, TTThanhPham tp, string nguoiSua)
        {
            // 1) Lấy dữ liệu cũ từ TTThanhPham + JOIN DanhSachMaSP để lấy Ten
            const string sqlGetCu = @"
            SELECT 
                ttp.MaBin, 
                ttp.KhoiLuongSau, 
                ttp.ChieuDaiSau,
                ttp.GhiChu,
                ds.Ten
            FROM TTThanhPham ttp
            LEFT JOIN DanhSachMaSP ds ON ds.id = ttp.DanhSachSP_ID
            WHERE ttp.id = @tpId;";

            string lotCu = null;
            decimal klCu = 0;
            decimal cdCu = 0;
            string tenCu = null;
            string ghiChuCu = "";


            using (var cmd = new SQLiteCommand(sqlGetCu, conn, tx))
            {
                cmd.Parameters.AddWithValue("@tpId", tpId);
                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    lotCu = reader["MaBin"]?.ToString();
                    klCu = reader["KhoiLuongSau"] != DBNull.Value ? Convert.ToDecimal(reader["KhoiLuongSau"]) : 0;
                    cdCu = reader["ChieuDaiSau"] != DBNull.Value ? Convert.ToDecimal(reader["ChieuDaiSau"]) : 0;
                    tenCu = reader["Ten"] != DBNull.Value ? reader["Ten"].ToString() : null;
                    ghiChuCu = reader["GhiChu"] != DBNull.Value ? reader["GhiChu"].ToString() : "";
                }
            }

            // 2) Insert vào LichSuSuaDoiThongTin
            const string sqlInsertLichSu = @"
            INSERT INTO LichSuSuaDoiThongTin
                (TTThanhPham_ID, NguoiSua, Ten_Cu, Ten_Moi, LOT_Cu, LOT_Moi, KL_Cu, KL_Moi, CD_Cu, CD_Moi, DateInsert, TenMay, GhiChu_Cu, GhiChu_Moi)
            VALUES
                (@TTThanhPham_ID, @NguoiSua, @Ten_Cu,@Ten_Moi, @LOT_Cu, @LOT_Moi, @KL_Cu, @KL_Moi, @CD_Cu, @CD_Moi, @DateInsert, @TenMay, @GhiChu_Cu, @GhiChu_Moi);
            SELECT last_insert_rowid();";

            long lichSuId;
            using (var cmd = new SQLiteCommand(sqlInsertLichSu, conn, tx))
            {
                cmd.Parameters.AddWithValue("@TTThanhPham_ID", tpId);
                cmd.Parameters.AddWithValue("@NguoiSua", string.IsNullOrWhiteSpace(nguoiSua) ? "Unknown" : nguoiSua.Trim());
                cmd.Parameters.AddWithValue("@Ten_Cu", tenCu ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Ten_Moi", tp.TenTP ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@LOT_Cu", lotCu ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@LOT_Moi", tp.MaBin ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@KL_Cu", klCu);
                cmd.Parameters.AddWithValue("@KL_Moi", tp.KhoiLuongSau);
                cmd.Parameters.AddWithValue("@CD_Cu", cdCu);
                cmd.Parameters.AddWithValue("@CD_Moi", tp.ChieuDaiSau);
                cmd.Parameters.AddWithValue("@DateInsert", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                cmd.Parameters.AddWithValue("@TenMay", DG_TonKhoBTP_v02.Properties.Settings.Default.TenMay);
                cmd.Parameters.AddWithValue("@GhiChu_Cu", ghiChuCu);
                cmd.Parameters.AddWithValue("@GhiChu_Moi", tp.GhiChu);

                lichSuId = Convert.ToInt64(cmd.ExecuteScalar());
            }

            // 3) Lấy danh sách NVL cũ từ TTNVL theo tpId
            const string sqlGetNVL = @"
            SELECT BinNVL
            FROM TTNVL
            WHERE TTThanhPham_ID = @tpId;";

            var binNVLList = new List<string>();
            using (var cmd = new SQLiteCommand(sqlGetNVL, conn, tx))
            {
                cmd.Parameters.AddWithValue("@tpId", tpId);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var bin = reader["BinNVL"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(bin))
                        binNVLList.Add(bin);
                }
            }

            // 4) Insert từng BinNVL vào ListNVLThayDoi
            const string sqlInsertNVL = @"
                INSERT INTO ListNVLThayDoi (LichSuSuaDoiThongTin_ID, LOT)
                VALUES (@LichSuSuaDoiThongTin_ID, @LOT);";

            using (var cmd = new SQLiteCommand(sqlInsertNVL, conn, tx))
            {
                var pLichSuId = cmd.Parameters.Add("@LichSuSuaDoiThongTin_ID", DbType.Int64);
                var pLot = cmd.Parameters.Add("@LOT", DbType.String);

                pLichSuId.Value = lichSuId;

                foreach (var bin in binNVLList)
                {
                    pLot.Value = bin;
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private static void UpdateThongTinCaLamViec(SQLiteConnection conn, SQLiteTransaction tx, ThongTinCaLamViec m, int id)
        {
            string sqlUpdate = @"UPDATE ThongTinCaLamViec 
                        SET Ngay = @Ngay,
                            May = @May,
                            Ca = @Ca,
                            NguoiLam = @NguoiLam,
                            ToTruong = @ToTruong,
                            QuanDoc = @QuanDoc
                        WHERE TTThanhPham_id = @id";

            using (var cmd = new SQLiteCommand(sqlUpdate, conn, tx))
            {
                cmd.Parameters.AddWithValue("@Ngay", m.Ngay);
                cmd.Parameters.AddWithValue("@May", m.May);
                cmd.Parameters.AddWithValue("@Ca", m.Ca);
                cmd.Parameters.AddWithValue("@NguoiLam", m.NguoiLam);
                cmd.Parameters.AddWithValue("@ToTruong", m.ToTruong ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@QuanDoc", m.QuanDoc ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@id", id);

                int rowsAffected = cmd.ExecuteNonQuery();

                if (rowsAffected == 0)
                {
                    throw new Exception($"Không tìm thấy hoặc không thể update ThongTinCaLamViec cho TTThanhPham id = {id}");
                }
            }
        }

        private static void UpdateTTThanhPham(SQLiteConnection conn, SQLiteTransaction tx, TTThanhPham m, int thongTinCaLamViecId, List<TTNVL> nvl)
        {
            string sqlUpdate = @"UPDATE TTThanhPham 
                                SET DanhSachSP_ID = @DanhSachSP_ID,
                                    MaBin = @MaBin,
                                    KhoiLuongTruoc = @KhoiLuongTruoc,
                                    KhoiLuongSau = @KhoiLuongSau,
                                    ChieuDaiTruoc = @ChieuDaiTruoc,
                                    ChieuDaiSau = @ChieuDaiSau,
                                    HanNoi = @HanNoi,
                                    Phe = @Phe,
                                    GhiChu = @GhiChu
                                WHERE id = @id";
            m.GhiChu = m.GhiChu + "- Đã sửa";


            using (var cmd = new SQLiteCommand(sqlUpdate, conn, tx))
            {
                cmd.Parameters.AddWithValue("@DanhSachSP_ID", m.DanhSachSP_ID);
                cmd.Parameters.AddWithValue("@MaBin", m.MaBin);
                cmd.Parameters.AddWithValue("@KhoiLuongTruoc", m.KhoiLuongTruoc);
                cmd.Parameters.AddWithValue("@KhoiLuongSau", m.KhoiLuongSau);
                cmd.Parameters.AddWithValue("@ChieuDaiTruoc", m.ChieuDaiTruoc);
                cmd.Parameters.AddWithValue("@ChieuDaiSau", m.ChieuDaiSau);
                cmd.Parameters.AddWithValue("@Phe", m.Phe);
                cmd.Parameters.AddWithValue("@HanNoi", m.HanNoi);
                cmd.Parameters.AddWithValue("@GhiChu", m.GhiChu ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@id", thongTinCaLamViecId);

                int rowsAffected = cmd.ExecuteNonQuery();

                if (rowsAffected == 0)
                {
                    throw new Exception($"Không tìm thấy hoặc không thể update TTThanhPham với id = {m.Id}");
                }
            }
        }

        private static void Del_InsertTTNVL(SQLiteConnection conn, SQLiteTransaction tx, long thongTinSpId, List<TTNVL> items)
        {
            // Restore cho dữ liệu cũ 


            // Xoá dữ liệu cũ
            using (var cmd = new SQLiteCommand(conn))
            {
                cmd.Transaction = tx;
                cmd.CommandText = @"DELETE FROM TTNVL WHERE TTThanhPham_ID = @TTThanhPham_ID";
                cmd.Parameters.AddWithValue("@TTThanhPham_ID", thongTinSpId);
                cmd.ExecuteNonQuery();
            }
            // Thêm dữ liệu mới
            InsertTTNVL(conn, tx, thongTinSpId, items);
        }

        private static void UpdateKhoiLuongConLai_TTThanhPham(SQLiteConnection conn, SQLiteTransaction tx, List<TTNVL> nvlList, long thongTinSpId)
        {
            if (conn == null) throw new ArgumentNullException(nameof(conn));
            if (tx == null) throw new ArgumentNullException(nameof(tx));
            if (nvlList == null || nvlList.Count == 0) return;

            using var cmd = new SQLiteCommand(@"
            UPDATE TTThanhPham
               SET  KhoiLuongSau = COALESCE(@kl, KhoiLuongSau),
                    QC = @QC,
                    ChieuDaiSau  = COALESCE(@cd, ChieuDaiSau),
                    LastEdit_id = @lastEditId
             WHERE MaBin       = @mabin ;", conn, tx);

            var pKL = cmd.Parameters.Add("@kl", DbType.Double);
            var QC = cmd.Parameters.Add("@QC", DbType.String);
            var pCD = cmd.Parameters.Add("@cd", DbType.Double);
            var pBin = cmd.Parameters.Add("@mabin", DbType.String);
            var pLE = cmd.Parameters.Add("@lastEditId", DbType.Int64);

            pLE.Value = thongTinSpId;

            foreach (var nvl in nvlList)
            {
                if (nvl == null || string.IsNullOrWhiteSpace(nvl.BinNVL))
                    continue;

                pKL.Value = DbValueOrNull(nvl.KlConLai);
                QC.Value = nvl.QC;
                pCD.Value = DbValueOrNull(nvl.CdConLai);
                pBin.Value = nvl.BinNVL.Trim();

                cmd.ExecuteNonQuery();
            }
        }

        private static void UpdateCaiDatCDBoc(SQLiteConnection conn, SQLiteTransaction tx, long id, CaiDatCDBoc m)
        {
            string query = @"
                UPDATE CaiDatCDBoc
                SET 
                    MangNuoc = @MangNuoc,
                    PuliDanDay = @PuliDanDay,
                    BoDemMet = @BoDemMet,
                    MayIn = @MayIn,
                    v1 = @v1,
                    v2 = @v2,
                    v3 = @v3,
                    v4 = @v4,
                    v5 = @v5,
                    v6 = @v6,
                    Co = @Co,
                    Dau1 = @Dau1,
                    Dau2 = @Dau2,
                    Khuon = @Khuon,
                    BinhSay = @BinhSay,
                    DKKhuon1 = @DKKhuon1,
                    DKKhuon2 = @DKKhuon2,
                    TTNhua = @TTNhua,
                    NhuaPhe = @NhuaPhe,
                    GhiChuNhuaPhe = @GhiChuNhuaPhe,
                    DayPhe = @DayPhe,
                    GhiChuDayPhe = @GhiChuDayPhe,
                    KTDKLan1 = @KTDKLan1,
                    KTDKLan2 = @KTDKLan2,
                    KTDKLan3 = @KTDKLan3,
                    DiemMongLan1 = @DiemMongLan1,
                    DiemMongLan2 = @DiemMongLan2
                WHERE TTThanhPham_ID = @TTThanhPham_ID;
            ";

            using (var cmd = new SQLiteCommand(query, conn, tx))
            {
                cmd.Parameters.AddWithValue("@TTThanhPham_ID", id);


                cmd.Parameters.AddWithValue("@MangNuoc", (object?)m.MangNuoc ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@PuliDanDay", (object?)m.PuliDanDay ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@BoDemMet", (object?)m.BoDemMet ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@MayIn", (object?)m.MayIn ?? DBNull.Value);

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
        }

        private static void UpdateCDBocLot(SQLiteConnection conn, SQLiteTransaction tx, long id, CD_BocLot m)
        {
            const string sql = @"
                UPDATE CD_BocLot
                SET DoDayTBLot = @DoDayTBLot
                WHERE CaiDatCDBoc_ID IN (
                    SELECT id FROM CaiDatCDBoc WHERE TTThanhPham_ID = @TTThanhPham_ID
                );";

            using var cmd = new SQLiteCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@DoDayTBLot", m.DoDayTBLot);
            cmd.Parameters.AddWithValue("@TTThanhPham_ID", id);
            cmd.ExecuteNonQuery();
        }

        private static void UpdateCDBocVo(SQLiteConnection conn, SQLiteTransaction tx, long id, CD_BocVo m)
        {
            const string sql = @"
                UPDATE CD_BocVo
                SET DayVoTB = @DayVoTB,
                    InAn = @InAn
                WHERE CaiDatCDBoc_ID IN (
                    SELECT id FROM CaiDatCDBoc WHERE TTThanhPham_ID = @TTThanhPham_ID
                );";

            using (var cmd = new SQLiteCommand(sql, conn, tx))
            {
                cmd.Parameters.AddWithValue("@DayVoTB", m.DayVoTB);
                cmd.Parameters.AddWithValue("@InAn", m.InAn ?? string.Empty);
                cmd.Parameters.AddWithValue("@TTThanhPham_ID", id);
                cmd.ExecuteNonQuery();
            }

            long cdBocVoId = GetCDBocVoIdByTTThanhPhamId(conn, tx, id);
            if (cdBocVoId <= 0)
                throw new InvalidOperationException("Không tìm thấy CD_BocVo tương ứng để cập nhật thông tin đóng gói.");

            // Update chi tiết đóng gói theo chiến lược đã chốt: xóa cũ rồi insert lại.
            DeleteTTCuonDayCD(conn, tx, cdBocVoId);
            InsertTTCuonDayCD(conn, tx, cdBocVoId, m.TTCuonDay_CD);
        }

        private static void UpdateCDBocMach(SQLiteConnection conn, SQLiteTransaction tx, long id, CD_BocMach m)
        {
            const string sql = @"
            UPDATE CD_BocMach
            SET 
                NgoaiQuan = @NgoaiQuan,
                LanDanhThung = @LanDanhThung,
                SoMet = @SoMet,
                Mau = @Mau
            WHERE CaiDatCDBoc_ID IN (
                SELECT id FROM CaiDatCDBoc WHERE TTThanhPham_ID = @TTThanhPham_ID
            );";

            using var cmd = new SQLiteCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@NgoaiQuan", m.NgoaiQuan ?? "1");
            cmd.Parameters.AddWithValue("@LanDanhThung", m.LanDanhThung);
            cmd.Parameters.AddWithValue("@SoMet", m.SoMet);
            cmd.Parameters.AddWithValue("@Mau", m.Mau);
            cmd.Parameters.AddWithValue("@TTThanhPham_ID", id);
            cmd.ExecuteNonQuery();
        }

        private static void UpdateCDKeoRut(SQLiteConnection conn, SQLiteTransaction tx, long thongTinSpId, CD_KeoRut m)
        {
            const string sql = @"
            UPDATE CD_KeoRut
            SET DKTrucX = @DKTrucX,
                DKTrucY = @DKTrucY,
                NgoaiQuan = @NgoaiQuan,
                TocDo = @TocDo,
                DienApU = @DienApU,
                DongDienU = @DongDienU
            WHERE TTThanhPham_ID = @TTThanhPham_ID;";

            using var cmd = new SQLiteCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@DKTrucX", m.DKTrucX);
            cmd.Parameters.AddWithValue("@DKTrucY", m.DKTrucY);
            cmd.Parameters.AddWithValue("@NgoaiQuan", m.NgoaiQuan ?? string.Empty);
            cmd.Parameters.AddWithValue("@TocDo", m.TocDo);
            cmd.Parameters.AddWithValue("@DienApU", m.DienApU);
            cmd.Parameters.AddWithValue("@DongDienU", m.DongDienU);
            cmd.Parameters.AddWithValue("@TTThanhPham_ID", thongTinSpId);
            cmd.ExecuteNonQuery();
        }

        private static void UpdateCDBenRuot(SQLiteConnection conn, SQLiteTransaction tx, long thongTinSpId, CD_BenRuot m)
        {
            const string sql = @"
            UPDATE CD_BenRuot
            SET DKSoi = @DKSoi,
                SoSoi = @SoSoi,
                ChieuXoan = @ChieuXoan,
                BuocBen = @BuocBen
            WHERE TTThanhPham_ID = @TTThanhPham_ID;";

            using var cmd = new SQLiteCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@DKSoi", m.DKSoi);
            cmd.Parameters.AddWithValue("@SoSoi", (object?)m.SoSoi ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ChieuXoan", m.ChieuXoan ?? "Z");
            cmd.Parameters.AddWithValue("@BuocBen", m.BuocBen);
            cmd.Parameters.AddWithValue("@TTThanhPham_ID", thongTinSpId);
            cmd.ExecuteNonQuery();
        }

        private static void UpdateCDGhepLoiQB(SQLiteConnection conn, SQLiteTransaction tx, long thongTinSpId, CD_GhepLoiQB m)
        {
            //const string sql = @"
            //UPDATE CD_GhepLoiQB
            //SET BuocXoan = @BuocXoan,
            //    ChieuXoan = @ChieuXoan,
            //    GoiCachMep = @GoiCachMep,
            //    DKBTP = @DKBTP
            //WHERE TTThanhPham_ID = @TTThanhPham_ID;";

            const string sql = @"
            UPDATE CD_GhepLoiQB
            SET ChieuXoan = @ChieuXoan,
                GoiCachMep = @GoiCachMep,
                DKBTP = @DKBTP,
                DoRongBang = @DoRongBang,
                DoDayBang = @DoDayBang
            WHERE TTThanhPham_ID = @TTThanhPham_ID;";

            using var cmd = new SQLiteCommand(sql, conn, tx);
            //cmd.Parameters.AddWithValue("@BuocXoan", m.BuocXoan);
            cmd.Parameters.AddWithValue("@ChieuXoan", m.ChieuXoan ?? "Z");
            cmd.Parameters.AddWithValue("@GoiCachMep", m.GoiCachMep);
            cmd.Parameters.AddWithValue("@DKBTP", m.DKBTP);
            cmd.Parameters.AddWithValue("@DoRongBang", m.DoRongBang);
            cmd.Parameters.AddWithValue("@DoDayBang", m.DoDayBang);
            cmd.Parameters.AddWithValue("@TTThanhPham_ID", thongTinSpId);
            cmd.ExecuteNonQuery();
        }

        private static int? KiemTraKhacBietCongDoanBOM(
            SQLiteConnection conn,
            SQLiteTransaction tx,
            int selectedProductId,
            int currentCongDoanId)
        {
            if (conn == null) throw new ArgumentNullException(nameof(conn));

            // Chưa chọn thành phẩm hoặc chưa có công đoạn hợp lệ thì không ghi khác biệt BOM.
            if (selectedProductId <= 0 || currentCongDoanId <= 0 || currentCongDoanId == 9)
                return null;

            const string sql = @"
                SELECT
                    COUNT(1) AS TotalBom,
                    SUM(CASE WHEN ""CongDoan"" = @CurrentCongDoanId THEN 1 ELSE 0 END) AS MatchedBom
                FROM BOMStructure
                WHERE ""ParentProduct"" = @SelectedProductId;";

            using var cmd = new SQLiteCommand(sql, conn);
            cmd.Transaction = tx;
            cmd.Parameters.Add("@SelectedProductId", DbType.Int32).Value = selectedProductId;
            cmd.Parameters.Add("@CurrentCongDoanId", DbType.Int32).Value = currentCongDoanId;

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return -1;

            int totalBom = reader["TotalBom"] == DBNull.Value ? 0 : Convert.ToInt32(reader["TotalBom"]);
            int matchedBom = reader["MatchedBom"] == DBNull.Value ? 0 : Convert.ToInt32(reader["MatchedBom"]);

            if (totalBom == 0)
                return -1;

            if (matchedBom > 0)
                return null;

            return currentCongDoanId;
        }

        /// <summary>
        /// Xóa danh sách lỗi cũ của thành phẩm và insert lại từng lỗi hiện tại.
        /// Cột Confirmed không được truyền vào để database sử dụng giá trị mặc định.
        /// </summary>
        private static void LuuDanhSachLoiNhapLieuSX(
            SQLiteConnection conn,
            SQLiteTransaction tx,
            long ttThanhPhamId,
            List<LoiNhapLieuData> danhSachLoiNhapLieu)
        {
            if (conn == null)
                throw new ArgumentNullException(nameof(conn));

            if (tx == null)
                throw new ArgumentNullException(nameof(tx));

            DamBaoCotLyDoLoiTonTai(conn, tx);

            using (var deleteCommand = new SQLiteCommand(@"
                DELETE FROM ""DanhSachLoiNhapLieuSX""
                WHERE ""TTThanhpham_id"" = @TTThanhpham_id;", conn, tx))
            {
                deleteCommand.Parameters.Add("@TTThanhpham_id", DbType.Int64).Value = ttThanhPhamId;
                deleteCommand.ExecuteNonQuery();
            }

            List<LoiNhapLieuData> danhSachHopLe = (danhSachLoiNhapLieu ??
                    new List<LoiNhapLieuData>())
                .Where(x => x != null && !string.IsNullOrWhiteSpace(x.NoiDungLoi))
                .GroupBy(x => x.NoiDungLoi.Trim(), StringComparer.Ordinal)
                .Select(g => g.First())
                .ToList();

            if (danhSachHopLe.Count == 0)
                return;

            using var insertCommand = new SQLiteCommand(@"
                INSERT INTO ""DanhSachLoiNhapLieuSX""
                (
                    ""TTThanhpham_id"",
                    ""NoiDungLoi"",
                    ""LyDoLoi""
                )
                VALUES
                (
                    @TTThanhpham_id,
                    @NoiDungLoi,
                    @LyDoLoi
                );", conn, tx);

            insertCommand.Parameters.Add("@TTThanhpham_id", DbType.Int64).Value = ttThanhPhamId;
            SQLiteParameter noiDungLoiParameter =
                insertCommand.Parameters.Add("@NoiDungLoi", DbType.String);
            SQLiteParameter lyDoLoiParameter =
                insertCommand.Parameters.Add("@LyDoLoi", DbType.String);

            foreach (LoiNhapLieuData loi in danhSachHopLe)
            {
                noiDungLoiParameter.Value = loi.NoiDungLoi.Trim();
                lyDoLoiParameter.Value = string.IsNullOrWhiteSpace(loi.LyDoLoi)
                    ? string.Empty
                    : loi.LyDoLoi.Trim();
                insertCommand.ExecuteNonQuery();
            }
        }

        private static void DamBaoCotLyDoLoiTonTai(
            SQLiteConnection conn,
            SQLiteTransaction tx)
        {
            bool daTonTai = false;

            using (var pragma = new SQLiteCommand(
                "PRAGMA table_info(\"DanhSachLoiNhapLieuSX\");",
                conn,
                tx))
            using (SQLiteDataReader reader = pragma.ExecuteReader())
            {
                while (reader.Read())
                {
                    string tenCot = Convert.ToString(reader["name"]);
                    if (string.Equals(
                            tenCot,
                            "LyDoLoi",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        daTonTai = true;
                        break;
                    }
                }
            }

            if (daTonTai)
                return;

            using (var alter = new SQLiteCommand(
                "ALTER TABLE \"DanhSachLoiNhapLieuSX\" " +
                "ADD COLUMN \"LyDoLoi\" TEXT;",
                conn,
                tx))
            {
                alter.ExecuteNonQuery();
            }
        }

        private static void LuuKhacBietBOM(SQLiteConnection conn, SQLiteTransaction tx, long ttThanhPhamId, List<TTNVLRow> nvlRows, TTThanhPham tp)
        {
            if (conn == null) throw new ArgumentNullException(nameof(conn));
            if (tx == null) throw new ArgumentNullException(nameof(tx));

            using (var delete = new SQLiteCommand(conn))
            {
                delete.Transaction = tx;
                delete.CommandText = @"
            DELETE FROM KhacBietBOM
            WHERE ""TTThanhpham_ID"" = @TTThanhpham_ID;";
                delete.Parameters.Add("@TTThanhpham_ID", DbType.Int64).Value = ttThanhPhamId;
                delete.ExecuteNonQuery();
            }

            var invalidRows = (nvlRows ?? new List<TTNVLRow>())
                .Where(x => x != null && x.IsCorrect == false)
                .ToList();

            if (invalidRows.Count > 0)
            {
                // Validate trước — fail sớm, không insert nửa vời
                foreach (TTNVLRow item in invalidRows)
                {
                    if (!item.DanhSachMaSP_ID.HasValue)
                        throw new InvalidOperationException("Dòng khác BOM thiếu DanhSachMaSP_ID.");
                    if (string.IsNullOrWhiteSpace(item.BinNVL))
                        throw new InvalidOperationException("Dòng khác BOM thiếu TenBinNVL/BinNVL.");
                }

                // 1 SELECT batch lấy toàn bộ DonVi
                var distinctIds = invalidRows
                    .Select(x => x.DanhSachMaSP_ID!.Value)
                    .Distinct()
                    .ToList();

                var donViMap = LayDonViDanhSachMaSP_Batch(conn, tx, distinctIds);

                // Chuẩn bị data cuối — tính SoLuong luôn, tránh tính lại trong loop insert
                var rows = invalidRows.Select(item =>
                {
                    if (!donViMap.TryGetValue(item.DanhSachMaSP_ID!.Value, out string donVi))
                        throw new InvalidOperationException(
                            $"Không tìm thấy DonVi với id = {item.DanhSachMaSP_ID.Value}.");

                    return (
                        TTThanhPhamId: ttThanhPhamId,
                        TenBinNVL: item.BinNVL.Trim(),
                        DanhSachMaSPId: item.DanhSachMaSP_ID.Value,
                        SoLuong: TinhSoLuongKhacBietBOM(item, donVi)
                    );
                }).ToList();

                // Batch INSERT, mỗi chunk tối đa 64 rows
                // SQLite giới hạn 999 parameters — 64 rows × 5 cột = 320, an toàn
                const int chunkSize = 64;

                for (int offset = 0; offset < rows.Count; offset += chunkSize)
                {
                    var chunk = rows.Skip(offset).Take(chunkSize).ToList();
                    BatchInsertKhacBietBOM(conn, tx, chunk);
                }
            }

            LuuKhacBietCongDoanBOM(conn, tx, ttThanhPhamId, tp);
        }

        private static void LuuKhacBietCongDoanBOM(SQLiteConnection conn, SQLiteTransaction tx, long ttThanhPhamId, TTThanhPham tp)
        {
            if (tp == null) return;

            int selectedProductId = tp.DanhSachSP_ID;
            int currentCongDoanId = tp.CongDoan?.Id ?? 0;

            int? congDoanThucTe = KiemTraKhacBietCongDoanBOM(conn, tx, selectedProductId, currentCongDoanId);
            if (!congDoanThucTe.HasValue)
                return;

            using var cmd = new SQLiteCommand(@"
                INSERT INTO KhacBietBOM
                    (""TTThanhpham_ID"", ""TenBinNVL"", ""DanhSachMaSP_ID"", ""SoLuong"", ""CongDoanThucTe"")
                VALUES
                    (@TTThanhpham_ID, NULL, @DanhSachMaSP_ID, NULL, @CongDoanThucTe);", conn, tx);

            cmd.Parameters.Add("@TTThanhpham_ID", DbType.Int64).Value = ttThanhPhamId;
            cmd.Parameters.Add("@DanhSachMaSP_ID", DbType.Int64).Value = selectedProductId;
            cmd.Parameters.Add("@CongDoanThucTe", DbType.Int32).Value = congDoanThucTe.Value;
            cmd.ExecuteNonQuery();
        }

        private static void BatchInsertKhacBietBOM(
            SQLiteConnection conn,
            SQLiteTransaction tx,
            List<(long TTThanhPhamId, string TenBinNVL, int DanhSachMaSPId, double? SoLuong)> chunk)
        {
            // Xây VALUES (@t0, @b0, @m0, @s0, @c0), (@t1, ...), ...
            var sb = new System.Text.StringBuilder();
            sb.Append(@"
                INSERT INTO KhacBietBOM
                    (""TTThanhpham_ID"", ""TenBinNVL"", ""DanhSachMaSP_ID"", ""SoLuong"", ""CongDoanThucTe"")
                VALUES ");

            for (int i = 0; i < chunk.Count; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append($"(@t{i}, @b{i}, @m{i}, @s{i}, @c{i})");
            }
            sb.Append(';');

            using var cmd = new SQLiteCommand(sb.ToString(), conn, tx);

            for (int i = 0; i < chunk.Count; i++)
            {
                var r = chunk[i];
                cmd.Parameters.Add($"@t{i}", DbType.Int64).Value = r.TTThanhPhamId;
                cmd.Parameters.Add($"@b{i}", DbType.String).Value = r.TenBinNVL;
                cmd.Parameters.Add($"@m{i}", DbType.Int64).Value = r.DanhSachMaSPId;
                cmd.Parameters.Add($"@s{i}", DbType.Double).Value =
                    r.SoLuong.HasValue ? (object)r.SoLuong.Value : DBNull.Value;
                cmd.Parameters.Add($"@c{i}", DbType.Int32).Value = DBNull.Value;
            }

            cmd.ExecuteNonQuery();
        }

        private static Dictionary<int, string> LayDonViDanhSachMaSP_Batch(
            SQLiteConnection conn,
            SQLiteTransaction tx,
            List<int> ids)
        {
            var result = new Dictionary<int, string>(ids.Count);
            if (ids.Count == 0) return result;

            // Tạo "WHERE id IN (@p0, @p1, ...)" động
            var paramNames = ids.Select((_, i) => $"@p{i}").ToList();
            string inClause = string.Join(", ", paramNames);

            using var cmd = new SQLiteCommand(conn);
            cmd.Transaction = tx;
            cmd.CommandText = $@"
                SELECT ""id"", ""DonVi""
                FROM DanhSachMaSP
                WHERE ""id"" IN ({inClause});";

            for (int i = 0; i < ids.Count; i++)
                cmd.Parameters.Add(paramNames[i], DbType.Int32).Value = ids[i];

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                int id = reader.GetInt32(0);
                string dv = reader.IsDBNull(1) ? string.Empty : reader.GetString(1).Trim();

                if (string.IsNullOrWhiteSpace(dv))
                    throw new InvalidOperationException(
                        $"Không tìm thấy DonVi trong DanhSachMaSP với id = {id}.");

                result[id] = dv;
            }

            return result;
        }

        private static double? TinhSoLuongKhacBietBOM(TTNVLRow item, string donVi)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            if (string.Equals(donVi, "KG", StringComparison.OrdinalIgnoreCase))
                return item.KlConLai;

            if (string.Equals(donVi, "M", StringComparison.OrdinalIgnoreCase))
                return item.CdConLai;

            throw new InvalidOperationException(
                $"DonVi '{donVi}' không hợp lệ để tính SoLuong KhacBietBOM. Chỉ hỗ trợ KG hoặc M.");
        }

        private static object DbValueOrNull(object value)
        {
            if (value == null) return DBNull.Value;

            if (value is string s)
            {
                return string.IsNullOrWhiteSpace(s) ? DBNull.Value : (object)s;
            }

            return value;
        }

        private static void RestoreFromNVL(SQLiteConnection conn, SQLiteTransaction tx, long tpId)
        {
            if (conn == null) throw new ArgumentNullException(nameof(conn));
            if (tx == null) throw new ArgumentNullException(nameof(tx));

            // Update TTThanhPham theo đúng điều kiện:
            // tp.LastEdit_id = nvl.TTThanhPham_ID  và  tp.MaBin = nvl.BinNVL
            // đồng thời chỉ update cho đúng tpId (tpId = ttthanhpham_id của dòng TTNVL)
            const string sql = @"
            UPDATE TTThanhPham AS tp
            SET
              KhoiLuongSau = (
                SELECT nvl.KlBatDau
                FROM TTNVL AS nvl
                WHERE nvl.TTThanhPham_ID = tp.LastEdit_id
                  AND nvl.BinNVL = tp.MaBin
              ),
              ChieuDaiSau = (
                SELECT nvl.CdBatDau
                FROM TTNVL AS nvl
                WHERE nvl.TTThanhPham_ID = tp.LastEdit_id
                  AND nvl.BinNVL = tp.MaBin
              ),
              LastEdit_id = NULL 
            WHERE tp.LastEdit_id = @tpId
              AND EXISTS (
                SELECT 1
                FROM TTNVL AS nvl
                WHERE nvl.TTThanhPham_ID = tp.LastEdit_id
                  AND nvl.BinNVL = tp.MaBin
              );";

            using var cmd = new SQLiteCommand(sql, conn, tx);
            cmd.CommandType = CommandType.Text;
            cmd.Parameters.AddWithValue("@tpId", tpId);

            var rowsAffected = cmd.ExecuteNonQuery();
        }

        internal static long InsertThongTinCaLamViec(
            SQLiteConnection conn,
            SQLiteTransaction tx,
            ThongTinCaLamViec m,
            long ttThanhPhamId)
        {
            const string sql = @"
            INSERT INTO ThongTinCaLamViec (Ngay, TTThanhPham_id, May, Ca, NguoiLam, ToTruong, QuanDoc)
            VALUES (@Ngay, @TTThanhPham_id, @May, @Ca, @NguoiLam, @ToTruong, @QuanDoc);
            SELECT last_insert_rowid();";

            using var cmd = new SQLiteCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@Ngay", m.Ngay);
            cmd.Parameters.AddWithValue("@TTThanhPham_id", ttThanhPhamId);
            cmd.Parameters.AddWithValue("@May", m.May);
            cmd.Parameters.AddWithValue("@Ca", m.Ca);
            cmd.Parameters.AddWithValue("@NguoiLam", m.NguoiLam);
            cmd.Parameters.AddWithValue("@ToTruong", (object?)m.ToTruong ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@QuanDoc", (object?)m.QuanDoc ?? DBNull.Value);

            return (long)(cmd.ExecuteScalar() ?? 0L);
        }

        private static long InsertTTThanhPham(SQLiteConnection conn, SQLiteTransaction tx, TTThanhPham m, List<TTNVL> nvl)
        {

            const string sql = @"
            INSERT INTO TTThanhPham
                (DanhSachSP_ID,QC ,  MaBin, KhoiLuongTruoc, KhoiLuongSau, ChieuDaiTruoc, ChieuDaiSau, Phe, CongDoan, GhiChu,HanNoi, DateInsert)
            VALUES
                (@DanhSachSP_ID,@QC,  @MaBin, @KhoiLuongTruoc, @KhoiLuongSau, @ChieuDaiTruoc, @ChieuDaiSau, @Phe, @CongDoan, @GhiChu, @HanNoi, @DateInsert);
            SELECT last_insert_rowid();";


            using var cmd = new SQLiteCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@DanhSachSP_ID", m.DanhSachSP_ID);
            cmd.Parameters.AddWithValue("@QC", m.QC);
            cmd.Parameters.AddWithValue("@MaBin", m.MaBin);
            cmd.Parameters.AddWithValue("@KhoiLuongTruoc", m.KhoiLuongTruoc);
            cmd.Parameters.AddWithValue("@KhoiLuongSau", m.KhoiLuongSau);
            cmd.Parameters.AddWithValue("@ChieuDaiTruoc", m.ChieuDaiTruoc);
            cmd.Parameters.AddWithValue("@ChieuDaiSau", m.ChieuDaiSau);
            cmd.Parameters.AddWithValue("@Phe", m.Phe);
            cmd.Parameters.AddWithValue("@CongDoan", m.CongDoan.Id);
            cmd.Parameters.AddWithValue("@GhiChu", (object?)m.GhiChu ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@HanNoi", m.HanNoi);
            cmd.Parameters.AddWithValue("@DateInsert", (object?)m.DateInsert ?? DBNull.Value);
            return (long)(cmd.ExecuteScalar() ?? 0L);
        }

        private static void InsertTTNVL(SQLiteConnection conn, SQLiteTransaction tx, long thongTinSpId, List<TTNVL> items)
        {
            if (items == null || items.Count == 0) return;

            const string sql = @"
            INSERT INTO TTNVL
                (TTThanhPham_ID, BinNVL,QC, DanhSachMaSP_ID, KlBatDau, CdBatDau, KlConLai, CdConLai, DuongKinhSoiDong, SoSoi, KetCauLoi, DuongKinhSoiMach)
            VALUES
                (@TTThanhPham_ID, @BinNVL,@QC,@DanhSachMaSP_ID, @KlBatDau, @CdBatDau, @KlConLai, @CdConLai, @DuongKinhSoiDong, @SoSoi, @KetCauLoi, @DuongKinhSoiMach);";

            using var cmd = new SQLiteCommand(sql, conn, tx);

            var pThongTinSP_ID = cmd.Parameters.Add("@TTThanhPham_ID", DbType.Int64);
            var pBinNVL = cmd.Parameters.Add("@BinNVL", DbType.String);
            var DanhSachMaSP_ID = cmd.Parameters.Add("@DanhSachMaSP_ID", DbType.Int64);
            var KlBatDau = cmd.Parameters.Add("@KlBatDau", DbType.Double);
            var QC = cmd.Parameters.Add("@QC", DbType.String);
            var CdBatDau = cmd.Parameters.Add("@CdBatDau", DbType.Double);
            var KlConLai = cmd.Parameters.Add("@KlConLai", DbType.Double);
            var CdConLai = cmd.Parameters.Add("@CdConLai", DbType.Double);
            var pDuongKinhSoiDong = cmd.Parameters.Add("@DuongKinhSoiDong", DbType.Double);
            var pSoSoi = cmd.Parameters.Add("@SoSoi", DbType.Int32);
            var pKetCauLoi = cmd.Parameters.Add("@KetCauLoi", DbType.Double);
            var pDuongKinhSoiMach = cmd.Parameters.Add("@DuongKinhSoiMach", DbType.Double);

            foreach (TTNVL m in items)
            {
                pThongTinSP_ID.Value = thongTinSpId;
                pBinNVL.Value = m.BinNVL ?? string.Empty;
                DanhSachMaSP_ID.Value = m.DanhSachMaSP_ID;
                KlBatDau.Value = m.KlBatDau;
                CdBatDau.Value = m.CdBatDau;
                KlConLai.Value = DbValueOrNull(m.KlConLai);
                CdConLai.Value = DbValueOrNull(m.CdConLai);
                QC.Value = m.QC;
                pDuongKinhSoiDong.Value = (object)m.DuongKinhSoiDong ?? DBNull.Value;
                pSoSoi.Value = (object)m.SoSoi ?? DBNull.Value;
                pKetCauLoi.Value = (object)m.KetCauLoi ?? DBNull.Value;
                pDuongKinhSoiMach.Value = (object)m.DuongKinhSoiMach ?? DBNull.Value;

                cmd.ExecuteNonQuery();
            }
        }

        private static long GetCDBocVoIdByTTThanhPhamId(SQLiteConnection conn, SQLiteTransaction tx, long ttThanhPhamId)
        {
            const string sql = @"
                SELECT cbv.id
                FROM CD_BocVo cbv
                INNER JOIN CaiDatCDBoc cdb
                    ON cdb.id = cbv.CaiDatCDBoc_ID
                WHERE cdb.TTThanhPham_ID = @TTThanhPham_ID
                LIMIT 1;";

            using var cmd = new SQLiteCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@TTThanhPham_ID", ttThanhPhamId);

            object value = cmd.ExecuteScalar();
            if (value == null || value == DBNull.Value) return 0;

            return Convert.ToInt64(value);
        }

        private static void DeleteTTCuonDayCD(SQLiteConnection conn, SQLiteTransaction tx, long cdBocVoId)
        {
            const string sql = @"DELETE FROM TTCuonDay_CD WHERE CongDoan_ID = @CongDoan_ID;";

            using var cmd = new SQLiteCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@CongDoan_ID", cdBocVoId);
            cmd.ExecuteNonQuery();
        }

        private static void InsertTTCuonDayCD(SQLiteConnection conn, SQLiteTransaction tx, long cdBocVoId, List<ThongTinCuonDay> items)
        {
            if (cdBocVoId <= 0)
                throw new ArgumentException("CD_BocVo.id không hợp lệ khi lưu thông tin đóng gói.", nameof(cdBocVoId));

            if (items == null || items.Count == 0) return;

            const string sql = @"
                INSERT INTO TTCuonDay_CD
                (SoCuon, TongChieuDai, SoDau, SoCuoi, GhiChu, CongDoan_ID, TTLo_ID)
                VALUES
                (@SoCuon, @TongChieuDai, @SoDau, @SoCuoi, @GhiChu, @CongDoan_ID, @TTLo_ID);";

            using var cmd = new SQLiteCommand(sql, conn, tx);
            cmd.Parameters.Add("@SoCuon", DbType.Int32);
            cmd.Parameters.Add("@TongChieuDai", DbType.Int32);
            cmd.Parameters.Add("@SoDau", DbType.Int32);
            cmd.Parameters.Add("@SoCuoi", DbType.Int32);
            cmd.Parameters.Add("@GhiChu", DbType.String);
            cmd.Parameters.Add("@CongDoan_ID", DbType.Int64);
            cmd.Parameters.Add("@TTLo_ID", DbType.Int32);

            foreach (ThongTinCuonDay item in items)
            {
                if (item == null) continue;

                cmd.Parameters["@SoCuon"].Value = item.SoCuon;
                cmd.Parameters["@TongChieuDai"].Value = item.TongChieuDai;
                cmd.Parameters["@SoDau"].Value = item.SoDau;
                cmd.Parameters["@SoCuoi"].Value = item.soCuoi;
                cmd.Parameters["@GhiChu"].Value = string.IsNullOrWhiteSpace(item.Ghichu)
                    ? (object)DBNull.Value
                    : item.Ghichu.Trim();
                cmd.Parameters["@CongDoan_ID"].Value = cdBocVoId;
                cmd.Parameters["@TTLo_ID"].Value = item.TTLo_ID.HasValue
                    ? (object)item.TTLo_ID.Value
                    : DBNull.Value;

                cmd.ExecuteNonQuery();
            }
        }

        private static void InsertCDBocLot(SQLiteConnection conn, SQLiteTransaction tx, long id, CD_BocLot m)
        {
            const string sql = @"
            INSERT INTO CD_BocLot (CaiDatCDBoc_ID, DoDayTBLot)
            VALUES (@CaiDatCDBoc_ID, @DoDayTBLot);";

            using var cmd = new SQLiteCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@CaiDatCDBoc_ID", id);
            cmd.Parameters.AddWithValue("@DoDayTBLot", m.DoDayTBLot);
            cmd.ExecuteNonQuery();
        }

        private static long InsertCDBocVo(SQLiteConnection conn, SQLiteTransaction tx, long id, CD_BocVo m)
        {
            const string sql = @"
            INSERT INTO CD_BocVo (CaiDatCDBoc_ID, DayVoTB, InAn)
            VALUES (@CaiDatCDBoc_ID, @DayVoTB, @InAn);
            SELECT last_insert_rowid();";

            using var cmd = new SQLiteCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@CaiDatCDBoc_ID", id);
            cmd.Parameters.AddWithValue("@DayVoTB", m.DayVoTB);
            cmd.Parameters.AddWithValue("@InAn", m.InAn ?? string.Empty);

            long cdBocVoId = Convert.ToInt64(cmd.ExecuteScalar() ?? 0L);
            InsertTTCuonDayCD(conn, tx, cdBocVoId, m.TTCuonDay_CD);

            return cdBocVoId;
        }

        private static long InsertCaiDatCDBoc(SQLiteConnection conn, SQLiteTransaction tx, long tpId, CaiDatCDBoc m)
        {
            const string sql = @"
            INSERT INTO CaiDatCDBoc
            (TTThanhPham_ID, MangNuoc, PuliDanDay, BoDemMet, MayIn,
             v1, v2, v3, v4, v5, v6, Co, Dau1, Dau2, Khuon, BinhSay,
             DKKhuon1, DKKhuon2, TTNhua, NhuaPhe, GhiChuNhuaPhe, DayPhe, GhiChuDayPhe,
             KTDKLan1, KTDKLan2, KTDKLan3, DiemMongLan1, DiemMongLan2)
            VALUES
            (@TTThanhPham_ID, @MangNuoc, @PuliDanDay, @BoDemMet, @MayIn,
             @v1, @v2, @v3, @v4, @v5, @v6, @Co, @Dau1, @Dau2, @Khuon, @BinhSay,
             @DKKhuon1, @DKKhuon2, @TTNhua, @NhuaPhe, @GhiChuNhuaPhe, @DayPhe, @GhiChuDayPhe,
             @KTDKLan1, @KTDKLan2, @KTDKLan3, @DiemMongLan1, @DiemMongLan2);";

            using var cmd = new SQLiteCommand(sql, conn, tx);

            cmd.Parameters.AddWithValue("@TTThanhPham_ID", tpId);
            cmd.Parameters.AddWithValue("@MangNuoc", (object?)m.MangNuoc ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@PuliDanDay", (object?)m.PuliDanDay ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@BoDemMet", (object?)m.BoDemMet ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@MayIn", (object?)m.MayIn ?? DBNull.Value);

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

            try
            {
                cmd.ExecuteNonQuery();
                return conn.LastInsertRowId;
            }
            catch (Exception ex)
            {
                throw new Exception("Lỗi khi thêm dữ liệu vào bảng CaiDatCDBoc.", ex);
            }

        }

        private static void InsertCDBocMach(SQLiteConnection conn, SQLiteTransaction tx, long id, CD_BocMach m)
        {
            const string sql = @"
            INSERT INTO CD_BocMach (CaiDatCDBoc_ID, NgoaiQuan, LanDanhThung, SoMet, Mau)
            VALUES (@CaiDatCDBoc_ID, @NgoaiQuan, @LanDanhThung, @SoMet, @Mau);";

            using var cmd = new SQLiteCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@CaiDatCDBoc_ID", id);
            cmd.Parameters.AddWithValue("@NgoaiQuan", m.NgoaiQuan ?? "1"); // default theo schema
            cmd.Parameters.AddWithValue("@LanDanhThung", m.LanDanhThung);
            cmd.Parameters.AddWithValue("@SoMet", m.SoMet);
            cmd.Parameters.AddWithValue("@Mau", m.Mau);
            cmd.ExecuteNonQuery();
        }

        private static void InsertCDKeoRut(SQLiteConnection conn, SQLiteTransaction tx, long thongTinSpId, CD_KeoRut m)
        {
            const string sql = @"
            INSERT INTO CD_KeoRut
            (TTThanhPham_ID, DKTrucX, DKTrucY, NgoaiQuan, TocDo, DienApU, DongDienU)
            VALUES
            (@TTThanhPham_ID, @DKTrucX, @DKTrucY, @NgoaiQuan, @TocDo, @DienApU, @DongDienU);";

            using var cmd = new SQLiteCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@TTThanhPham_ID", thongTinSpId);
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
            (TTThanhPham_ID, DKSoi, SoSoi, ChieuXoan, BuocBen)
            VALUES
            (@TTThanhPham_ID, @DKSoi, @SoSoi, @ChieuXoan, @BuocBen);";

            using var cmd = new SQLiteCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@TTThanhPham_ID", thongTinSpId);
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
            (TTThanhPham_ID, ChieuXoan, GoiCachMep, DKBTP, DoRongBang, DoDayBang)
            VALUES
            (@TTThanhPham_ID,  @ChieuXoan, @GoiCachMep, @DKBTP, @DoRongBang, @DoDayBang);";


            using var cmd = new SQLiteCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@TTThanhPham_ID", thongTinSpId);
            cmd.Parameters.AddWithValue("@DoDayBang", m.DoDayBang);
            cmd.Parameters.AddWithValue("@DoRongBang", m.DoRongBang);
            cmd.Parameters.AddWithValue("@ChieuXoan", m.ChieuXoan);
            cmd.Parameters.AddWithValue("@GoiCachMep", m.GoiCachMep);
            cmd.Parameters.AddWithValue("@DKBTP", m.DKBTP);
            cmd.ExecuteNonQuery();
        }
    }
}