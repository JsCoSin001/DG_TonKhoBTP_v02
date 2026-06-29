using DG_TonKhoBTP_v02.Models.KeToan.VatTuKhac;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Threading;
using CoreHelper = DG_TonKhoBTP_v02.Helper.Helper;

namespace DG_TonKhoBTP_v02.Database
{
    internal static class NhapXuatVatTu_DB
    {
        private const int SearchLimit = 30;

        public static DataTable TimVatTuDichVu(
            KieuNhapXuat_Model model,
            string keyword,
            int? danhSachKhoId,
            string nguoiLam,
            CancellationToken cancellationToken)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            keyword = keyword?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(keyword))
                return CreateEmptySearchTable();

            if (IsDichVu(model))
                return TimDichVu(keyword, cancellationToken);

            if (IsNhapKhoTheoDon(model))
                return TimVatTuNhapKhoTheoDon(keyword, cancellationToken);

            if (IsNhapKhoKhac(model))
                return TimVatTuNhapKhoKhac(keyword, cancellationToken);

            if (IsXuat(model))
                return TimVatTuXuat(keyword, danhSachKhoId, nguoiLam, cancellationToken);

            return CreateEmptySearchTable();
        }

        public static DataTable TimKhoHang(string keyword, CancellationToken cancellationToken)
        {
            keyword = keyword?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(keyword))
                return CreateEmptyKhoHangSearchTable();

            string sql = @"
                SELECT
                    id,
                    TenKho,
                    TenKho_KhongDau
                FROM DanhSachKho
                WHERE
                    TenKho LIKE @kw
                    OR IFNULL(TenKho_KhongDau, '') LIKE @kwNoDau
                ORDER BY TenKho
                LIMIT @limit;
            ";

            return ExecuteSearchQuery(sql, CreateKeywordParameters(keyword), cancellationToken);
        }

        public static DataTable TimNhaCungCap(string keyword, CancellationToken cancellationToken)
        {
            keyword = keyword?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(keyword))
                return CreateEmptyNhaCungCapSearchTable();

            string sql = @"
                SELECT
                    id,
                    TenNCC,
                    TenNCC_KhongDau
                FROM DanhSachNCC
                WHERE
                    TenNCC LIKE @kw
                    OR IFNULL(TenNCC_KhongDau, '') LIKE @kwNoDau
                ORDER BY TenNCC
                LIMIT @limit;
            ";

            return ExecuteSearchQuery(sql, CreateKeywordParameters(keyword), cancellationToken);
        }

        public static int GetSoLuongXuatNhapThangHienTai(bool isNhapKho = false, DateTime? currentDate = null)
        {
            DateTime now = currentDate ?? DateTime.Now;

            string start = new DateTime(now.Year, now.Month, 1).ToString("yyyy-MM-dd");
            string end = new DateTime(now.Year, now.Month, 1).AddMonths(1).ToString("yyyy-MM-dd");

            string sql = @"
                SELECT 
                    IFNULL(MAX(CAST(SUBSTR(TenPhieu, INSTR(TenPhieu, '-') + 1) AS INTEGER)), 0)
                FROM LichSuXuatNhap
                WHERE Ngay >= @start 
                  AND Ngay < @end
            ";

            sql += isNhapKho ? " AND TenPhieu LIKE 'KNK%'" : " AND TenPhieu LIKE 'KXK%'";

            using (var conn = DB_Base.OpenConnection())
            using (var cmd = new SQLiteCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@start", start);
                cmd.Parameters.AddWithValue("@end", end);

                object result = cmd.ExecuteScalar();
                return result == null || result == DBNull.Value ? 0 : Convert.ToInt32(result);
            }
        }

        public static NhapXuatVatTu_SaveResult LuuMoi(NhapXuatVatTu_SaveModel model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            using (var conn = DB_Base.OpenConnection())
            using (var tran = conn.BeginTransaction())
            {
                try
                {
                    if (IsNhapKhoKhac(model.Mode))
                    {
                        int thongTinDatHangId = TaoDonVaThongTinDatHangChoNhapKhac(conn, tran, model);
                        InsertLichSuXuatNhap(conn, tran, model, thongTinDatHangId, Math.Abs(model.SoLuongNguoiNhap));
                    }
                    else if (IsXuat(model.Mode))
                    {
                        InsertPhanBoXuat(conn, tran, model);
                    }
                    else
                    {
                        if (!model.ThongTinDatHangId.HasValue)
                            throw new InvalidOperationException("Chưa chọn dữ liệu đơn/vật tư để lưu.");

                        decimal soLuong = Math.Abs(model.SoLuongNguoiNhap);

                        if (IsDichVu(model.Mode))
                        {
                            InsertLichSuXuatNhap(conn, tran, model, model.ThongTinDatHangId.Value, soLuong);
                            InsertLichSuXuatNhap(conn, tran, model, model.ThongTinDatHangId.Value, -soLuong);
                        }
                        else
                        {
                            InsertLichSuXuatNhap(conn, tran, model, model.ThongTinDatHangId.Value, soLuong);
                        }
                    }

                    tran.Commit();

                    return new NhapXuatVatTu_SaveResult
                    {
                        TenPhieu = model.TenPhieu,
                        TongSoLuong = model.SoLuongNguoiNhap
                    };
                }
                catch
                {
                    tran.Rollback();
                    throw;
                }
            }
        }

        public static NhapXuatVatTu_SaveResult CapNhat(NhapXuatVatTu_SaveModel model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            using (var conn = DB_Base.OpenConnection())
            using (var tran = conn.BeginTransaction())
            {
                try
                {
                    if (IsXuat(model.Mode))
                    {
                        CapNhatPhieuXuatGop(conn, tran, model);
                    }
                    else
                    {
                        CapNhatDongNhapHoacDichVu(conn, tran, model);
                    }

                    tran.Commit();

                    return new NhapXuatVatTu_SaveResult
                    {
                        TenPhieu = model.TenPhieu,
                        TongSoLuong = model.SoLuongNguoiNhap
                    };
                }
                catch
                {
                    tran.Rollback();
                    throw;
                }
            }
        }

        public static NhapXuatVatTu_DeleteResult Xoa(NhapXuatVatTu_DeleteModel model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            if (model.Mode == null)
                throw new InvalidOperationException("Không xác định được kiểu nhập/xuất.");

            if (string.IsNullOrWhiteSpace(model.NguoiLam))
                throw new InvalidOperationException("Không xác định được người làm.");

            using (var conn = DB_Base.OpenConnection())
            using (var tran = conn.BeginTransaction())
            {
                try
                {
                    int deletedRows;

                    if (IsXuat(model.Mode))
                    {
                        if (string.IsNullOrWhiteSpace(model.TenPhieu) || !model.DanhSachMaSPId.HasValue)
                            throw new InvalidOperationException("Không xác định được nhóm phiếu xuất cần xoá.");

                        if (!model.DanhSachKhoId.HasValue)
                            throw new InvalidOperationException("Không xác định được kho của phiếu xuất cần xoá.");

                        if (NhomXuatCoDongKhongChoSua(conn, tran, model.TenPhieu, model.DanhSachMaSPId.Value, model.DanhSachKhoId, model.NguoiLam))
                            throw new InvalidOperationException("Phiếu này không được phép xoá vì CanEdit = 0.");

                        deletedRows = XoaNhomXuat(conn, tran, model.TenPhieu, model.DanhSachMaSPId.Value, model.DanhSachKhoId, model.NguoiLam);
                    }
                    else
                    {
                        if (!model.LichSuXuatNhapId.HasValue)
                            throw new InvalidOperationException("Không xác định được dòng cần xoá.");

                        LichSuXuatNhapDeleteInfo info = GetDeleteInfo(conn, tran, model.LichSuXuatNhapId.Value, model.NguoiLam);
                        if (info == null)
                            throw new InvalidOperationException("Không tìm thấy dòng cần xoá hoặc dòng không thuộc người làm hiện tại.");

                        if (info.CanEdit != 1)
                            throw new InvalidOperationException("Phiếu này không được phép xoá vì CanEdit = 0.");

                        if (IsDichVu(model.Mode))
                        {
                            deletedRows = XoaCapDichVu(conn, tran, model, info);
                        }
                        else
                        {
                            deletedRows = XoaDongLichSu(conn, tran, model.LichSuXuatNhapId.Value, model.NguoiLam);

                            if (deletedRows > 0 && IsNhapKhoKhac(model.Mode))
                                XoaThongTinDatHangNhapKhacNeuKhongConDung(conn, tran, info.ThongTinDatHangId, info.DanhSachDatHangId, model.Mode.Id);
                        }
                    }

                    tran.Commit();

                    return new NhapXuatVatTu_DeleteResult
                    {
                        DeletedRows = deletedRows,
                        TenPhieu = model.TenPhieu
                    };
                }
                catch
                {
                    tran.Rollback();
                    throw;
                }
            }
        }

        public static DataTable TimPhieuDaLuu(
            KieuNhapXuat_Model model,
            string keyword,
            int? danhSachKhoId,
            string nguoiLam,
            CancellationToken cancellationToken)
        {
            keyword = keyword?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(keyword))
                return CreateEmptyTimPhieuTable();

            if (model == null || string.IsNullOrWhiteSpace(nguoiLam))
                return CreateEmptyTimPhieuTable();


            string sql = @"
                SELECT
                    lsxn.TenPhieu AS TenPhieu,
                    MAX(lsxn.Ngay) AS Ngay
                FROM LichSuXuatNhap lsxn
                LEFT JOIN ThongTinDatHang ttdh
                    ON ttdh.id = lsxn.ThongTinDatHang_ID
                LEFT JOIN DanhSachDatHang dsdh
                    ON dsdh.id = ttdh.DanhSachDatHang_ID
                LEFT JOIN DanhSachMaSP dms
                    ON dms.id = ttdh.DanhSachMaSP_ID
                WHERE lsxn.TenPhieu LIKE @kw
                  AND IFNULL(lsxn.NguoiLam, '') = @nguoiLam
            ";

            var parameters = CreateKeywordParameters(keyword);
            parameters["@nguoiLam"] = nguoiLam;

            if (IsDichVu(model))
            {
                sql += @"
                  AND lsxn.DanhSachKho_ID IS NULL
                  AND lsxn.SoLuong > 0
                  AND (
                        ttdh.DanhSachMaSP_ID IS NULL
                        OR ttdh.DanhSachMaSP_ID = 0
                  )
                ";
            }
            else
            {
                if (IsXuat(model))
                {
                    sql += @"
                      AND lsxn.SoLuong < 0
                      AND dms.id IS NOT NULL
                    ";
                }
                else
                {
                    sql += @"
                      AND lsxn.SoLuong > 0
                      AND dms.id IS NOT NULL
                      AND IFNULL(dsdh.LoaiDon, 0) = @loaiDon
                    ";
                    parameters["@loaiDon"] = model.Id;
                }
            }

            sql += @"
                GROUP BY lsxn.TenPhieu
                ORDER BY MAX(lsxn.Ngay) DESC, lsxn.TenPhieu DESC
                LIMIT @limit;
            ";

            return ExecuteSearchQuery(sql, parameters, cancellationToken);
        }

        public static DataTable LoadChiTietTheoTenPhieu(
            KieuNhapXuat_Model model,
            string tenPhieu,
            int? danhSachKhoId,
            string nguoiLam)
        {
            if (model == null || string.IsNullOrWhiteSpace(tenPhieu) || string.IsNullOrWhiteSpace(nguoiLam))
                return CreateEmptyChiTietTable();


            using (var conn = DB_Base.OpenConnection())
            {
                if (IsXuat(model))
                    return LoadChiTietXuatGop(conn, model, tenPhieu, danhSachKhoId, nguoiLam);

                return LoadChiTietNhapHoacDichVu(conn, model, tenPhieu, danhSachKhoId, nguoiLam);
            }
        }

        public static decimal TinhTongTonVatTu(
            int danhSachMaSPId,
            int danhSachKhoId,
            string nguoiLam,
            string excludeTenPhieu = null,
            int? excludeDanhSachMaSPId = null,
            int? excludeDanhSachKhoId = null)
        {
            if (string.IsNullOrWhiteSpace(nguoiLam))
                return 0;

            using (var conn = DB_Base.OpenConnection())
            using (var cmd = new SQLiteCommand(BuildSqlTinhTon(excludeTenPhieu, excludeDanhSachMaSPId, excludeDanhSachKhoId), conn))
            {
                cmd.Parameters.AddWithValue("@danhSachMaSPId", danhSachMaSPId);
                cmd.Parameters.AddWithValue("@danhSachKhoId", danhSachKhoId);
                cmd.Parameters.AddWithValue("@nguoiLam", nguoiLam);

                if (!string.IsNullOrWhiteSpace(excludeTenPhieu) && excludeDanhSachMaSPId.HasValue)
                {
                    cmd.Parameters.AddWithValue("@excludeTenPhieu", excludeTenPhieu);
                    cmd.Parameters.AddWithValue("@excludeDanhSachMaSPId", excludeDanhSachMaSPId.Value);

                    if (excludeDanhSachKhoId.HasValue)
                        cmd.Parameters.AddWithValue("@excludeDanhSachKhoId", excludeDanhSachKhoId.Value);
                }

                object result = cmd.ExecuteScalar();
                return ToDecimal(result);
            }
        }

        private static DataTable TimVatTuNhapKhoTheoDon(string keyword, CancellationToken cancellationToken)
        {
            string sql = @"
                SELECT
                    dms.id AS id,
                    ttdh.id AS ThongTinDatHang_ID,
                    dms.id AS DanhSachMaSP_ID,
                    dms.Ten AS ten,
                    dms.Ma AS ma,
                    dms.DonVi AS donvi,
                    dsdh.MaDon AS MaDon,
                    (ttdh.SoLuongMua - COALESCE(nhap.TongNhap, 0)) AS SoLuongYeuCau,
                    0 AS SoLuongTon,
                    ttdh.DonGia AS DonGia
                FROM ThongTinDatHang ttdh
                INNER JOIN DanhSachMaSP dms
                    ON dms.id = ttdh.DanhSachMaSP_ID
                INNER JOIN DanhSachDatHang dsdh
                    ON dsdh.id = ttdh.DanhSachDatHang_ID
                LEFT JOIN (
                    SELECT
                        ThongTinDatHang_ID,
                        SUM(SoLuong) AS TongNhap
                    FROM LichSuXuatNhap
                    WHERE SoLuong > 0
                    GROUP BY ThongTinDatHang_ID
                ) nhap
                    ON nhap.ThongTinDatHang_ID = ttdh.id
                WHERE dms.Active = 1
                  AND (
                        dms.Ten LIKE @kw
                        OR IFNULL(dms.Ten_KhongDau, '') LIKE @kwNoDau
                  )
                  AND (ttdh.SoLuongMua - COALESCE(nhap.TongNhap, 0)) > 0
                ORDER BY dms.Ten
                LIMIT @limit;
            ";

            return ExecuteSearchQuery(sql, CreateKeywordParameters(keyword), cancellationToken);
        }

        private static DataTable TimVatTuNhapKhoKhac(string keyword, CancellationToken cancellationToken)
        {
            string sql = @"
                SELECT
                    dms.id AS id,
                    NULL AS ThongTinDatHang_ID,
                    dms.id AS DanhSachMaSP_ID,
                    dms.Ten AS ten,
                    dms.Ma AS ma,
                    dms.DonVi AS donvi,
                    '' AS MaDon,
                    0 AS SoLuongYeuCau,
                    0 AS SoLuongTon,
                    NULL AS DonGia
                FROM DanhSachMaSP dms
                WHERE dms.Active = 1
                  AND (
                        dms.Ten LIKE @kw
                        OR IFNULL(dms.Ten_KhongDau, '') LIKE @kwNoDau
                  )
                ORDER BY dms.Ten
                LIMIT @limit;
            ";

            return ExecuteSearchQuery(sql, CreateKeywordParameters(keyword), cancellationToken);
        }

        private static DataTable TimVatTuXuat(
            string keyword,
            int? danhSachKhoId,
            string nguoiLam,
            CancellationToken cancellationToken)
        {
            if (!danhSachKhoId.HasValue || string.IsNullOrWhiteSpace(nguoiLam))
                return CreateEmptySearchTable();

            string sql = @"
                SELECT
                    dms.id AS id,
                    NULL AS ThongTinDatHang_ID,
                    dms.id AS DanhSachMaSP_ID,
                    dms.Ten AS ten,
                    dms.Ma AS ma,
                    dms.DonVi AS donvi,
                    '' AS MaDon,
                    SUM(lsxn.SoLuong) AS SoLuongYeuCau,
                    SUM(lsxn.SoLuong) AS SoLuongTon,
                    NULL AS DonGia
                FROM LichSuXuatNhap lsxn
                INNER JOIN ThongTinDatHang ttdh
                    ON ttdh.id = lsxn.ThongTinDatHang_ID
                INNER JOIN DanhSachMaSP dms
                    ON dms.id = ttdh.DanhSachMaSP_ID
                WHERE dms.Active = 1
                  AND lsxn.DanhSachKho_ID = @danhSachKhoId
                  AND IFNULL(lsxn.NguoiLam, '') = @nguoiLam
                  AND (
                        dms.Ten LIKE @kw
                        OR IFNULL(dms.Ten_KhongDau, '') LIKE @kwNoDau
                  )
                GROUP BY
                    dms.id,
                    dms.Ten,
                    dms.Ma,
                    dms.DonVi
                HAVING SUM(lsxn.SoLuong) > 0
                ORDER BY dms.Ten
                LIMIT @limit;
            ";

            var parameters = CreateKeywordParameters(keyword);
            parameters["@danhSachKhoId"] = danhSachKhoId.Value;
            parameters["@nguoiLam"] = nguoiLam;

            return ExecuteSearchQuery(sql, parameters, cancellationToken);
        }

        private static DataTable TimDichVu(string keyword, CancellationToken cancellationToken)
        {
            string sql = @"
                SELECT
                    NULL AS id,
                    ttdh.id AS ThongTinDatHang_ID,
                    NULL AS DanhSachMaSP_ID,
                    ttdh.TenVatTu AS ten,
                    '' AS ma,
                    '' AS donvi,
                    dsdh.MaDon AS MaDon,
                    0 AS SoLuongYeuCau,
                    0 AS SoLuongTon,
                    ttdh.DonGia AS DonGia
                FROM ThongTinDatHang ttdh
                INNER JOIN DanhSachDatHang dsdh
                    ON dsdh.id = ttdh.DanhSachDatHang_ID
                LEFT JOIN LichSuXuatNhap lsxn
                    ON lsxn.ThongTinDatHang_ID = ttdh.id
                WHERE lsxn.id IS NULL
                  AND (
                        ttdh.DanhSachMaSP_ID IS NULL
                        OR ttdh.DanhSachMaSP_ID = 0
                  )
                  AND (
                        ttdh.TenVatTu LIKE @kw
                        OR IFNULL(ttdh.TenVatTu_KhongDau, '') LIKE @kwNoDau
                  )
                ORDER BY ttdh.TenVatTu
                LIMIT @limit;
            ";

            return ExecuteSearchQuery(sql, CreateKeywordParameters(keyword), cancellationToken);
        }

        private static void CapNhatDongNhapHoacDichVu(SQLiteConnection conn, SQLiteTransaction tran, NhapXuatVatTu_SaveModel model)
        {
            if (!model.LichSuXuatNhapId.HasValue)
                throw new InvalidOperationException("Không xác định được dòng cần cập nhật.");

            if (!DongLichSuChoPhepSua(conn, tran, model.LichSuXuatNhapId.Value))
                throw new InvalidOperationException("Phiếu này không được phép sửa vì CanEdit = 0.");

            int thongTinDatHangId;

            if (IsNhapKhoKhac(model.Mode))
            {
                if (!model.DanhSachDatHangId.HasValue || !model.ThongTinDatHangId.HasValue)
                    throw new InvalidOperationException("Không xác định được đơn nhập khác cần cập nhật.");

                CapNhatDanhSachDatHangNhapKhac(conn, tran, model);
                CapNhatThongTinDatHangNhapKhac(conn, tran, model);
                thongTinDatHangId = model.ThongTinDatHangId.Value;
            }
            else
            {
                if (!model.ThongTinDatHangId.HasValue)
                    throw new InvalidOperationException("Chưa chọn dữ liệu đơn/vật tư để cập nhật.");

                thongTinDatHangId = model.ThongTinDatHangId.Value;
            }

            decimal soLuong = Math.Abs(model.SoLuongNguoiNhap);

            if (IsDichVu(model.Mode))
            {
                LichSuXuatNhapDeleteInfo oldInfo = GetDeleteInfo(conn, tran, model.LichSuXuatNhapId.Value, model.NguoiLam);
                if (oldInfo == null || !oldInfo.ThongTinDatHangId.HasValue)
                    throw new InvalidOperationException("Không xác định được dòng dịch vụ cần cập nhật.");

                string oldTenPhieu = string.IsNullOrWhiteSpace(model.OldTenPhieu)
                    ? model.TenPhieu
                    : model.OldTenPhieu;

                if (DichVuCoDongKhongChoSua(conn, tran, oldTenPhieu, oldInfo.ThongTinDatHangId.Value, model.NguoiLam))
                    throw new InvalidOperationException("Phiếu này không được phép sửa vì CanEdit = 0.");

                CapNhatMotDongLichSu(conn, tran, model, model.LichSuXuatNhapId.Value, thongTinDatHangId, soLuong);

                int updatedNegativeRows = CapNhatDongAmDichVu(
                    conn,
                    tran,
                    model,
                    oldTenPhieu,
                    oldInfo.ThongTinDatHangId.Value,
                    model.LichSuXuatNhapId.Value,
                    thongTinDatHangId,
                    -soLuong);

                if (updatedNegativeRows <= 0)
                    InsertLichSuXuatNhap(conn, tran, model, thongTinDatHangId, -soLuong);

                return;
            }

            CapNhatMotDongLichSu(conn, tran, model, model.LichSuXuatNhapId.Value, thongTinDatHangId, soLuong);
        }

        private static void CapNhatMotDongLichSu(
            SQLiteConnection conn,
            SQLiteTransaction tran,
            NhapXuatVatTu_SaveModel model,
            int lichSuXuatNhapId,
            int thongTinDatHangId,
            decimal soLuong)
        {
            string sql = @"
                UPDATE LichSuXuatNhap
                SET
                    ThongTinDatHang_ID = @thongTinDatHangId,
                    Ngay = @ngay,
                    NguoiGiao_Nhan = @nguoiGiaoNhan,
                    DanhSachKho_ID = @danhSachKhoId,
                    LyDo = @lyDo,
                    SoLuong = @soLuong,
                    TenPhieu = @tenPhieu,
                    GhiChu = @ghiChu,
                    NhaCC = @nhaCC,
                    DonGia = @donGia,
                    NguoiLam = @nguoiLam,
                    DanhSachNCC_ID = @danhSachNCCId
                WHERE id = @id;
            ";

            using (var cmd = new SQLiteCommand(sql, conn, tran))
            {
                AddCommonLichSuParameters(cmd, model, thongTinDatHangId, soLuong);
                cmd.Parameters.AddWithValue("@id", lichSuXuatNhapId);
                cmd.ExecuteNonQuery();
            }
        }

        private static int CapNhatDongAmDichVu(
            SQLiteConnection conn,
            SQLiteTransaction tran,
            NhapXuatVatTu_SaveModel model,
            string oldTenPhieu,
            int oldThongTinDatHangId,
            int lichSuXuatNhapIdDuong,
            int thongTinDatHangIdMoi,
            decimal soLuongAm)
        {
            string sql = @"
                UPDATE LichSuXuatNhap
                SET
                    ThongTinDatHang_ID = @thongTinDatHangId,
                    Ngay = @ngay,
                    NguoiGiao_Nhan = @nguoiGiaoNhan,
                    DanhSachKho_ID = @danhSachKhoId,
                    LyDo = @lyDo,
                    SoLuong = @soLuong,
                    TenPhieu = @tenPhieu,
                    GhiChu = @ghiChu,
                    NhaCC = @nhaCC,
                    DonGia = @donGia,
                    NguoiLam = @nguoiLam,
                    DanhSachNCC_ID = @danhSachNCCId
                WHERE TenPhieu = @oldTenPhieu
                  AND ThongTinDatHang_ID = @oldThongTinDatHangId
                  AND IFNULL(NguoiLam, '') = @oldNguoiLam
                  AND DanhSachKho_ID IS NULL
                  AND SoLuong < 0
                  AND id <> @lichSuXuatNhapIdDuong;
            ";

            using (var cmd = new SQLiteCommand(sql, conn, tran))
            {
                AddCommonLichSuParameters(cmd, model, thongTinDatHangIdMoi, soLuongAm);
                cmd.Parameters.AddWithValue("@oldTenPhieu", oldTenPhieu?.Trim());
                cmd.Parameters.AddWithValue("@oldThongTinDatHangId", oldThongTinDatHangId);
                cmd.Parameters.AddWithValue("@oldNguoiLam", model.NguoiLam ?? string.Empty);
                cmd.Parameters.AddWithValue("@lichSuXuatNhapIdDuong", lichSuXuatNhapIdDuong);
                return cmd.ExecuteNonQuery();
            }
        }

        private static bool DichVuCoDongKhongChoSua(
            SQLiteConnection conn,
            SQLiteTransaction tran,
            string tenPhieu,
            int thongTinDatHangId,
            string nguoiLam)
        {
            string sql = @"
                SELECT COUNT(*)
                FROM LichSuXuatNhap
                WHERE TenPhieu = @tenPhieu
                  AND ThongTinDatHang_ID = @thongTinDatHangId
                  AND IFNULL(NguoiLam, '') = @nguoiLam
                  AND DanhSachKho_ID IS NULL
                  AND IFNULL(CanEdit, 1) <> 1;
            ";

            using (var cmd = new SQLiteCommand(sql, conn, tran))
            {
                cmd.Parameters.AddWithValue("@tenPhieu", tenPhieu?.Trim());
                cmd.Parameters.AddWithValue("@thongTinDatHangId", thongTinDatHangId);
                cmd.Parameters.AddWithValue("@nguoiLam", nguoiLam ?? string.Empty);
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
        }

        private static void CapNhatPhieuXuatGop(SQLiteConnection conn, SQLiteTransaction tran, NhapXuatVatTu_SaveModel model)
        {
            if (string.IsNullOrWhiteSpace(model.OldTenPhieu) || !model.OldDanhSachMaSPId.HasValue)
                throw new InvalidOperationException("Không xác định được nhóm phiếu xuất cần cập nhật.");

            if (NhomXuatCoDongKhongChoSua(conn, tran, model.OldTenPhieu, model.OldDanhSachMaSPId.Value, model.OldDanhSachKhoId, model.NguoiLam))
                throw new InvalidOperationException("Phiếu này không được phép sửa vì CanEdit = 0.");

            XoaNhomXuat(conn, tran, model.OldTenPhieu, model.OldDanhSachMaSPId.Value, model.OldDanhSachKhoId, model.NguoiLam);
            InsertPhanBoXuat(conn, tran, model);
        }

        private static int TaoDonVaThongTinDatHangChoNhapKhac(SQLiteConnection conn, SQLiteTransaction tran, NhapXuatVatTu_SaveModel model)
        {
            int danhSachDatHangId = GetOrCreateDanhSachDatHangNhapKhac(conn, tran, model);
            return InsertThongTinDatHangNhapKhac(conn, tran, model, danhSachDatHangId);
        }

        private static int GetOrCreateDanhSachDatHangNhapKhac(SQLiteConnection conn, SQLiteTransaction tran, NhapXuatVatTu_SaveModel model)
        {
            int? existingId = TimDanhSachDatHangNhapKhacTheoMaDon(conn, tran, model.TenPhieu, model.Mode.Id);
            if (existingId.HasValue)
                return existingId.Value;

            return InsertDanhSachDatHangNhapKhac(conn, tran, model);
        }

        private static int? TimDanhSachDatHangNhapKhacTheoMaDon(SQLiteConnection conn, SQLiteTransaction tran, string maDon, int loaiDon)
        {
            string sql = @"
                SELECT id
                FROM DanhSachDatHang
                WHERE MaDon = @maDon
                  AND IFNULL(LoaiDon, 0) = @loaiDon
                LIMIT 1;
            ";

            using (var cmd = new SQLiteCommand(sql, conn, tran))
            {
                cmd.Parameters.AddWithValue("@maDon", maDon?.Trim());
                cmd.Parameters.AddWithValue("@loaiDon", loaiDon);

                object result = cmd.ExecuteScalar();
                if (result == null || result == DBNull.Value)
                    return null;

                return Convert.ToInt32(result);
            }
        }

        private static int InsertDanhSachDatHangNhapKhac(SQLiteConnection conn, SQLiteTransaction tran, NhapXuatVatTu_SaveModel model)
        {
            string sql = @"
                INSERT INTO DanhSachDatHang
                (
                    MaDon,
                    LoaiDon,
                    DateInsert,
                    NguoiDat,
                    NgayThem
                )
                VALUES
                (
                    @maDon,
                    @loaiDon,
                    @dateInsert,
                    @nguoiDat,
                    @ngayThem
                );
            ";

            using (var cmd = new SQLiteCommand(sql, conn, tran))
            {
                cmd.Parameters.AddWithValue("@maDon", model.TenPhieu);
                cmd.Parameters.AddWithValue("@loaiDon", model.Mode.Id);
                cmd.Parameters.AddWithValue("@dateInsert", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                cmd.Parameters.AddWithValue("@nguoiDat", NullIfWhiteSpace(model.NguoiLam));
                cmd.Parameters.AddWithValue("@ngayThem", model.Ngay.ToString("yyyy-MM-dd"));

                return Convert.ToInt32(ExecuteInsertAndGetId(cmd));
            }
        }

        private static int InsertThongTinDatHangNhapKhac(SQLiteConnection conn, SQLiteTransaction tran, NhapXuatVatTu_SaveModel model, int danhSachDatHangId)
        {
            var vatTu = GetVatTuById(conn, tran, model.DanhSachMaSPId);

            string sql = @"
                INSERT INTO ThongTinDatHang
                (
                    DanhSachMaSP_ID,
                    DanhSachDatHang_ID,
                    TenVatTu,
                    TenVatTu_KhongDau,
                    SoLuongMua,
                    MucDichMua,
                    NgayGiao,
                    Date_Insert,
                    DonGia
                )
                VALUES
                (
                    @danhSachMaSPId,
                    @danhSachDatHangId,
                    @tenVatTu,
                    @tenVatTuKhongDau,
                    @soLuongMua,
                    '',
                    @ngayGiao,
                    @dateInsert,
                    @donGia
                );
            ";

            using (var cmd = new SQLiteCommand(sql, conn, tran))
            {
                cmd.Parameters.AddWithValue("@danhSachMaSPId", model.DanhSachMaSPId.Value);
                cmd.Parameters.AddWithValue("@danhSachDatHangId", danhSachDatHangId);
                cmd.Parameters.AddWithValue("@tenVatTu", vatTu.Ten);
                cmd.Parameters.AddWithValue("@tenVatTuKhongDau", CoreHelper.BoDauTiengViet(vatTu.Ten) ?? vatTu.Ten);
                cmd.Parameters.AddWithValue("@soLuongMua", Math.Abs(model.SoLuongNguoiNhap));
                cmd.Parameters.AddWithValue("@ngayGiao", model.Ngay.ToString("yyyy-MM-dd"));
                cmd.Parameters.AddWithValue("@dateInsert", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                cmd.Parameters.AddWithValue("@donGia", model.DonGia);

                return Convert.ToInt32(ExecuteInsertAndGetId(cmd));
            }
        }

        private static void CapNhatDanhSachDatHangNhapKhac(SQLiteConnection conn, SQLiteTransaction tran, NhapXuatVatTu_SaveModel model)
        {
            string sql = @"
                UPDATE DanhSachDatHang
                SET
                    MaDon = @maDon,
                    LoaiDon = @loaiDon,
                    DateInsert = @dateInsert,
                    NguoiDat = @nguoiDat,
                    NgayThem = @ngayThem
                WHERE id = @id;
            ";

            using (var cmd = new SQLiteCommand(sql, conn, tran))
            {
                cmd.Parameters.AddWithValue("@maDon", model.TenPhieu);
                cmd.Parameters.AddWithValue("@loaiDon", model.Mode.Id);
                cmd.Parameters.AddWithValue("@dateInsert", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                cmd.Parameters.AddWithValue("@nguoiDat", NullIfWhiteSpace(model.NguoiLam));
                cmd.Parameters.AddWithValue("@ngayThem", model.Ngay.ToString("yyyy-MM-dd"));
                cmd.Parameters.AddWithValue("@id", model.DanhSachDatHangId.Value);
                cmd.ExecuteNonQuery();
            }
        }

        private static void CapNhatThongTinDatHangNhapKhac(SQLiteConnection conn, SQLiteTransaction tran, NhapXuatVatTu_SaveModel model)
        {
            var vatTu = GetVatTuById(conn, tran, model.DanhSachMaSPId);

            string sql = @"
                UPDATE ThongTinDatHang
                SET
                    DanhSachMaSP_ID = @danhSachMaSPId,
                    TenVatTu = @tenVatTu,
                    TenVatTu_KhongDau = @tenVatTuKhongDau,
                    SoLuongMua = @soLuongMua,
                    MucDichMua = '',
                    NgayGiao = @ngayGiao,
                    Date_Insert = @dateInsert,
                    DonGia = @donGia
                WHERE id = @id;
            ";

            using (var cmd = new SQLiteCommand(sql, conn, tran))
            {
                cmd.Parameters.AddWithValue("@danhSachMaSPId", model.DanhSachMaSPId.Value);
                cmd.Parameters.AddWithValue("@tenVatTu", vatTu.Ten);
                cmd.Parameters.AddWithValue("@tenVatTuKhongDau", CoreHelper.BoDauTiengViet(vatTu.Ten) ?? vatTu.Ten);
                cmd.Parameters.AddWithValue("@soLuongMua", Math.Abs(model.SoLuongNguoiNhap));
                cmd.Parameters.AddWithValue("@ngayGiao", model.Ngay.ToString("yyyy-MM-dd"));
                cmd.Parameters.AddWithValue("@dateInsert", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                cmd.Parameters.AddWithValue("@donGia", model.DonGia);
                cmd.Parameters.AddWithValue("@id", model.ThongTinDatHangId.Value);
                cmd.ExecuteNonQuery();
            }
        }

        private static void InsertPhanBoXuat(SQLiteConnection conn, SQLiteTransaction tran, NhapXuatVatTu_SaveModel model)
        {
            if (!model.DanhSachMaSPId.HasValue)
                throw new InvalidOperationException("Chưa chọn vật tư để xuất.");

            if (!model.DanhSachKhoId.HasValue)
                throw new InvalidOperationException("Chưa chọn kho hàng để xuất.");

            decimal soLuongCanXuat = Math.Abs(model.SoLuongNguoiNhap);
            decimal tongTon = 0;
            var rows = GetTonTheoThongTinDatHang(conn, tran, model.DanhSachMaSPId.Value, model.DanhSachKhoId.Value, model.NguoiLam);

            foreach (DataRow row in rows.Rows)
                tongTon += ToDecimal(row["SoLuongTon"]);

            if (soLuongCanXuat > tongTon)
                throw new InvalidOperationException($"Số lượng xuất vượt tồn. Tồn hiện tại: {tongTon:0.###}.");

            decimal conLai = soLuongCanXuat;

            foreach (DataRow row in rows.Rows)
            {
                if (conLai <= 0)
                    break;

                int thongTinDatHangId = Convert.ToInt32(row["ThongTinDatHang_ID"]);
                decimal tonDong = ToDecimal(row["SoLuongTon"]);

                if (tonDong <= 0)
                    continue;

                decimal soLuongXuatDong = Math.Min(conLai, tonDong);
                InsertLichSuXuatNhap(conn, tran, model, thongTinDatHangId, -soLuongXuatDong);
                conLai -= soLuongXuatDong;
            }

            if (conLai > 0)
                throw new InvalidOperationException($"Không phân bổ đủ số lượng xuất. Còn thiếu: {conLai:0.###}.");
        }

        private static long InsertLichSuXuatNhap(SQLiteConnection conn, SQLiteTransaction tran, NhapXuatVatTu_SaveModel model, int thongTinDatHangId, decimal soLuong)
        {
            string sql = @"
                INSERT INTO LichSuXuatNhap
                (
                    ThongTinDatHang_ID,
                    Ngay,
                    NguoiGiao_Nhan,
                    DanhSachKho_ID,
                    LyDo,
                    SoLuong,
                    TenPhieu,
                    GhiChu,
                    NhaCC,
                    DonGia,
                    NguoiLam,
                    DanhSachNCC_ID
                )
                VALUES
                (
                    @thongTinDatHangId,
                    @ngay,
                    @nguoiGiaoNhan,
                    @danhSachKhoId,
                    @lyDo,
                    @soLuong,
                    @tenPhieu,
                    @ghiChu,
                    @nhaCC,
                    @donGia,
                    @nguoiLam,
                    @danhSachNCCId
                );
            ";

            using (var cmd = new SQLiteCommand(sql, conn, tran))
            {
                AddCommonLichSuParameters(cmd, model, thongTinDatHangId, soLuong);
                return ExecuteInsertAndGetId(cmd);
            }
        }

        private static void AddCommonLichSuParameters(SQLiteCommand cmd, NhapXuatVatTu_SaveModel model, int thongTinDatHangId, decimal soLuong)
        {
            cmd.Parameters.AddWithValue("@thongTinDatHangId", thongTinDatHangId);
            cmd.Parameters.AddWithValue("@ngay", model.Ngay.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("@nguoiGiaoNhan", NullIfWhiteSpace(model.NguoiGiaoNhan));
            cmd.Parameters.AddWithValue("@danhSachKhoId", (object)model.DanhSachKhoId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@lyDo", NullIfWhiteSpace(model.LyDo));
            cmd.Parameters.AddWithValue("@soLuong", soLuong);
            cmd.Parameters.AddWithValue("@tenPhieu", model.TenPhieu?.Trim());
            cmd.Parameters.AddWithValue("@ghiChu", NullIfWhiteSpace(model.GhiChu));
            cmd.Parameters.AddWithValue("@nhaCC", NullIfWhiteSpace(model.NhaCC));
            cmd.Parameters.AddWithValue("@donGia", model.DonGia);
            cmd.Parameters.AddWithValue("@nguoiLam", NullIfWhiteSpace(model.NguoiLam));
            cmd.Parameters.AddWithValue("@danhSachNCCId", (object)model.DanhSachNCCId ?? DBNull.Value);
        }

        private static bool DongLichSuChoPhepSua(SQLiteConnection conn, SQLiteTransaction tran, int lichSuXuatNhapId)
        {
            string sql = @"
                SELECT IFNULL(CanEdit, 1)
                FROM LichSuXuatNhap
                WHERE id = @id;
            ";

            using (var cmd = new SQLiteCommand(sql, conn, tran))
            {
                cmd.Parameters.AddWithValue("@id", lichSuXuatNhapId);
                object result = cmd.ExecuteScalar();
                return result != null && result != DBNull.Value && Convert.ToInt32(result) == 1;
            }
        }

        private static bool NhomXuatCoDongKhongChoSua(
            SQLiteConnection conn,
            SQLiteTransaction tran,
            string tenPhieu,
            int danhSachMaSPId,
            int? danhSachKhoId,
            string nguoiLam)
        {
            string sql = @"
                SELECT COUNT(*)
                FROM LichSuXuatNhap lsxn
                INNER JOIN ThongTinDatHang ttdh
                    ON ttdh.id = lsxn.ThongTinDatHang_ID
                WHERE lsxn.TenPhieu = @tenPhieu
                  AND ttdh.DanhSachMaSP_ID = @danhSachMaSPId
                  AND IFNULL(lsxn.NguoiLam, '') = @nguoiLam
                  AND lsxn.SoLuong < 0
                  AND IFNULL(lsxn.CanEdit, 1) <> 1
            ";

            if (danhSachKhoId.HasValue)
                sql += " AND lsxn.DanhSachKho_ID = @danhSachKhoId ";

            using (var cmd = new SQLiteCommand(sql, conn, tran))
            {
                cmd.Parameters.AddWithValue("@tenPhieu", tenPhieu);
                cmd.Parameters.AddWithValue("@danhSachMaSPId", danhSachMaSPId);
                cmd.Parameters.AddWithValue("@nguoiLam", nguoiLam ?? string.Empty);

                if (danhSachKhoId.HasValue)
                    cmd.Parameters.AddWithValue("@danhSachKhoId", danhSachKhoId.Value);

                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
        }

        private static int XoaNhomXuat(
            SQLiteConnection conn,
            SQLiteTransaction tran,
            string tenPhieu,
            int danhSachMaSPId,
            int? danhSachKhoId,
            string nguoiLam)
        {
            string sql = @"
                DELETE FROM LichSuXuatNhap
                WHERE id IN
                (
                    SELECT lsxn.id
                    FROM LichSuXuatNhap lsxn
                    INNER JOIN ThongTinDatHang ttdh
                        ON ttdh.id = lsxn.ThongTinDatHang_ID
                    WHERE lsxn.TenPhieu = @tenPhieu
                      AND ttdh.DanhSachMaSP_ID = @danhSachMaSPId
                      AND IFNULL(lsxn.NguoiLam, '') = @nguoiLam
                      AND lsxn.SoLuong < 0
                
            ";

            if (danhSachKhoId.HasValue)
                sql += " AND lsxn.DanhSachKho_ID = @danhSachKhoId ";

            sql += " );";

            using (var cmd = new SQLiteCommand(sql, conn, tran))
            {
                cmd.Parameters.AddWithValue("@tenPhieu", tenPhieu);
                cmd.Parameters.AddWithValue("@danhSachMaSPId", danhSachMaSPId);
                cmd.Parameters.AddWithValue("@nguoiLam", nguoiLam ?? string.Empty);

                if (danhSachKhoId.HasValue)
                    cmd.Parameters.AddWithValue("@danhSachKhoId", danhSachKhoId.Value);

                return cmd.ExecuteNonQuery();
            }
        }

        private static LichSuXuatNhapDeleteInfo GetDeleteInfo(SQLiteConnection conn, SQLiteTransaction tran, int lichSuXuatNhapId, string nguoiLam)
        {
            string sql = @"
                SELECT
                    lsxn.id AS LichSuXuatNhap_ID,
                    lsxn.ThongTinDatHang_ID AS ThongTinDatHang_ID,
                    ttdh.DanhSachDatHang_ID AS DanhSachDatHang_ID,
                    IFNULL(lsxn.CanEdit, 1) AS CanEdit
                FROM LichSuXuatNhap lsxn
                LEFT JOIN ThongTinDatHang ttdh
                    ON ttdh.id = lsxn.ThongTinDatHang_ID
                WHERE lsxn.id = @id
                  AND IFNULL(lsxn.NguoiLam, '') = @nguoiLam
                LIMIT 1;
            ";

            using (var cmd = new SQLiteCommand(sql, conn, tran))
            {
                cmd.Parameters.AddWithValue("@id", lichSuXuatNhapId);
                cmd.Parameters.AddWithValue("@nguoiLam", nguoiLam ?? string.Empty);

                using (var reader = cmd.ExecuteReader())
                {
                    if (!reader.Read())
                        return null;

                    return new LichSuXuatNhapDeleteInfo
                    {
                        LichSuXuatNhapId = Convert.ToInt32(reader["LichSuXuatNhap_ID"]),
                        ThongTinDatHangId = reader["ThongTinDatHang_ID"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["ThongTinDatHang_ID"]),
                        DanhSachDatHangId = reader["DanhSachDatHang_ID"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["DanhSachDatHang_ID"]),
                        CanEdit = Convert.ToInt32(reader["CanEdit"])
                    };
                }
            }
        }

        private static int XoaCapDichVu(
            SQLiteConnection conn,
            SQLiteTransaction tran,
            NhapXuatVatTu_DeleteModel model,
            LichSuXuatNhapDeleteInfo info)
        {
            if (!info.ThongTinDatHangId.HasValue || string.IsNullOrWhiteSpace(model.TenPhieu))
                return XoaDongLichSu(conn, tran, model.LichSuXuatNhapId.Value, model.NguoiLam);

            if (DichVuCoDongKhongChoSua(conn, tran, model.TenPhieu, info.ThongTinDatHangId.Value, model.NguoiLam))
                throw new InvalidOperationException("Phiếu này không được phép xoá vì CanEdit = 0.");

            string sql = @"
                DELETE FROM LichSuXuatNhap
                WHERE TenPhieu = @tenPhieu
                  AND ThongTinDatHang_ID = @thongTinDatHangId
                  AND IFNULL(NguoiLam, '') = @nguoiLam
                  AND DanhSachKho_ID IS NULL;
            ";

            using (var cmd = new SQLiteCommand(sql, conn, tran))
            {
                cmd.Parameters.AddWithValue("@tenPhieu", model.TenPhieu?.Trim());
                cmd.Parameters.AddWithValue("@thongTinDatHangId", info.ThongTinDatHangId.Value);
                cmd.Parameters.AddWithValue("@nguoiLam", model.NguoiLam ?? string.Empty);
                return cmd.ExecuteNonQuery();
            }
        }

        private static int XoaDongLichSu(SQLiteConnection conn, SQLiteTransaction tran, int lichSuXuatNhapId, string nguoiLam)
        {
            string sql = @"
                DELETE FROM LichSuXuatNhap
                WHERE id = @id
                  AND IFNULL(NguoiLam, '') = @nguoiLam;
            ";

            using (var cmd = new SQLiteCommand(sql, conn, tran))
            {
                cmd.Parameters.AddWithValue("@id", lichSuXuatNhapId);
                cmd.Parameters.AddWithValue("@nguoiLam", nguoiLam ?? string.Empty);
                return cmd.ExecuteNonQuery();
            }
        }

        private static void XoaThongTinDatHangNhapKhacNeuKhongConDung(SQLiteConnection conn, SQLiteTransaction tran, int? thongTinDatHangId, int? danhSachDatHangId, int loaiDon)
        {
            if (!thongTinDatHangId.HasValue)
                return;

            if (DemDongLichSuTheoThongTinDatHang(conn, tran, thongTinDatHangId.Value) > 0)
                return;

            XoaThongTinDatHang(conn, tran, thongTinDatHangId.Value);

            if (!danhSachDatHangId.HasValue)
                return;

            if (!DanhSachDatHangLaNhapKhac(conn, tran, danhSachDatHangId.Value, loaiDon))
                return;

            if (DemThongTinDatHangTheoDanhSachDatHang(conn, tran, danhSachDatHangId.Value) > 0)
                return;

            XoaDanhSachDatHang(conn, tran, danhSachDatHangId.Value);
        }

        private static int DemDongLichSuTheoThongTinDatHang(SQLiteConnection conn, SQLiteTransaction tran, int thongTinDatHangId)
        {
            using (var cmd = new SQLiteCommand("SELECT COUNT(*) FROM LichSuXuatNhap WHERE ThongTinDatHang_ID = @id;", conn, tran))
            {
                cmd.Parameters.AddWithValue("@id", thongTinDatHangId);
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        private static int DemThongTinDatHangTheoDanhSachDatHang(SQLiteConnection conn, SQLiteTransaction tran, int danhSachDatHangId)
        {
            using (var cmd = new SQLiteCommand("SELECT COUNT(*) FROM ThongTinDatHang WHERE DanhSachDatHang_ID = @id;", conn, tran))
            {
                cmd.Parameters.AddWithValue("@id", danhSachDatHangId);
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        private static bool DanhSachDatHangLaNhapKhac(SQLiteConnection conn, SQLiteTransaction tran, int danhSachDatHangId, int loaiDon)
        {
            using (var cmd = new SQLiteCommand("SELECT COUNT(*) FROM DanhSachDatHang WHERE id = @id AND IFNULL(LoaiDon, 0) = @loaiDon;", conn, tran))
            {
                cmd.Parameters.AddWithValue("@id", danhSachDatHangId);
                cmd.Parameters.AddWithValue("@loaiDon", loaiDon);
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
        }

        private static void XoaThongTinDatHang(SQLiteConnection conn, SQLiteTransaction tran, int thongTinDatHangId)
        {
            using (var cmd = new SQLiteCommand("DELETE FROM ThongTinDatHang WHERE id = @id;", conn, tran))
            {
                cmd.Parameters.AddWithValue("@id", thongTinDatHangId);
                cmd.ExecuteNonQuery();
            }
        }

        private static void XoaDanhSachDatHang(SQLiteConnection conn, SQLiteTransaction tran, int danhSachDatHangId)
        {
            using (var cmd = new SQLiteCommand("DELETE FROM DanhSachDatHang WHERE id = @id;", conn, tran))
            {
                cmd.Parameters.AddWithValue("@id", danhSachDatHangId);
                cmd.ExecuteNonQuery();
            }
        }

        private static DataTable GetTonTheoThongTinDatHang(SQLiteConnection conn, SQLiteTransaction tran, int danhSachMaSPId, int danhSachKhoId, string nguoiLam)
        {
            string sql = @"
                SELECT
                    ttdh.id AS ThongTinDatHang_ID,
                    SUM(lsxn.SoLuong) AS SoLuongTon
                FROM LichSuXuatNhap lsxn
                INNER JOIN ThongTinDatHang ttdh
                    ON ttdh.id = lsxn.ThongTinDatHang_ID
                INNER JOIN DanhSachMaSP dms
                    ON dms.id = ttdh.DanhSachMaSP_ID
                WHERE dms.id = @danhSachMaSPId
                  AND lsxn.DanhSachKho_ID = @danhSachKhoId
                  AND IFNULL(lsxn.NguoiLam, '') = @nguoiLam
                GROUP BY ttdh.id
                HAVING SUM(lsxn.SoLuong) > 0
                ORDER BY ttdh.id ASC;
            ";

            var dt = new DataTable();

            using (var cmd = new SQLiteCommand(sql, conn, tran))
            {
                cmd.Parameters.AddWithValue("@danhSachMaSPId", danhSachMaSPId);
                cmd.Parameters.AddWithValue("@danhSachKhoId", danhSachKhoId);
                cmd.Parameters.AddWithValue("@nguoiLam", nguoiLam ?? string.Empty);

                using (var adapter = new SQLiteDataAdapter(cmd))
                {
                    adapter.Fill(dt);
                }
            }

            return dt;
        }

        private static string BuildSqlTinhTon(string excludeTenPhieu, int? excludeDanhSachMaSPId, int? excludeDanhSachKhoId)
        {
            string sql = @"
                SELECT IFNULL(SUM(lsxn.SoLuong), 0)
                FROM LichSuXuatNhap lsxn
                INNER JOIN ThongTinDatHang ttdh
                    ON ttdh.id = lsxn.ThongTinDatHang_ID
                WHERE ttdh.DanhSachMaSP_ID = @danhSachMaSPId
                  AND lsxn.DanhSachKho_ID = @danhSachKhoId
                  AND IFNULL(lsxn.NguoiLam, '') = @nguoiLam
            ";

            if (!string.IsNullOrWhiteSpace(excludeTenPhieu) && excludeDanhSachMaSPId.HasValue)
            {
                sql += " AND NOT (lsxn.TenPhieu = @excludeTenPhieu AND ttdh.DanhSachMaSP_ID = @excludeDanhSachMaSPId";

                if (excludeDanhSachKhoId.HasValue)
                    sql += " AND lsxn.DanhSachKho_ID = @excludeDanhSachKhoId";

                sql += ") ";
            }

            return sql;
        }

        private static DataTable LoadChiTietNhapHoacDichVu(
            SQLiteConnection conn,
            KieuNhapXuat_Model model,
            string tenPhieu,
            int? danhSachKhoId,
            string nguoiLam)
        {
            string sql = @"
                SELECT
                    lsxn.id AS id,
                    lsxn.id AS LichSuXuatNhap_ID,
                    CAST(lsxn.id AS TEXT) AS LichSuXuatNhap_IDs,
                    dsdh.id AS DanhSachDatHang_ID,
                    ttdh.id AS ThongTinDatHang_ID,
                    ttdh.DanhSachMaSP_ID AS DanhSachMaSP_ID,
                    lsxn.DanhSachKho_ID AS DanhSachKho_ID,
                    kho.TenKho AS TenKho,
                    lsxn.DanhSachNCC_ID AS DanhSachNCC_ID,
                    lsxn.NhaCC AS NhaCungCap,
                    IFNULL(lsxn.CanEdit, 1) AS CanEdit,
                    0 AS IsGroupXuat,
                    lsxn.TenPhieu AS TenPhieu,
                    lsxn.TenPhieu AS MaDon,
                    CASE
                        WHEN ttdh.DanhSachMaSP_ID IS NULL OR ttdh.DanhSachMaSP_ID = 0 THEN ttdh.TenVatTu
                        ELSE dms.Ten
                    END AS ten,
                    IFNULL(dms.Ma, '') AS ma,
                    IFNULL(dms.DonVi, '') AS donVi,
                    ttdh.SoLuongMua AS yeuCau,
                    lsxn.Ngay AS ngay,
                    ABS(lsxn.SoLuong) AS thucNhan,
                    lsxn.DonGia AS donGia,
                    ABS(lsxn.SoLuong) * IFNULL(lsxn.DonGia, 0) AS thanhTien,
                    lsxn.GhiChu AS ghiChu,
                    lsxn.NguoiGiao_Nhan AS NguoiGiao_Nhan,
                    lsxn.LyDo AS LyDo
                FROM LichSuXuatNhap lsxn
                LEFT JOIN ThongTinDatHang ttdh
                    ON ttdh.id = lsxn.ThongTinDatHang_ID
                LEFT JOIN DanhSachDatHang dsdh
                    ON dsdh.id = ttdh.DanhSachDatHang_ID
                LEFT JOIN DanhSachMaSP dms
                    ON dms.id = ttdh.DanhSachMaSP_ID
                LEFT JOIN DanhSachKho kho
                    ON kho.id = lsxn.DanhSachKho_ID
                WHERE lsxn.TenPhieu = @tenPhieu
                  AND IFNULL(lsxn.NguoiLam, '') = @nguoiLam
            ";

            if (IsDichVu(model))
            {
                sql += @"
                  AND lsxn.DanhSachKho_ID IS NULL
                  AND lsxn.SoLuong > 0
                  AND (
                        ttdh.DanhSachMaSP_ID IS NULL
                        OR ttdh.DanhSachMaSP_ID = 0
                  )
                ";
            }
            else
            {
                if (model.IsNhap)
                {
                    sql += @"
                      AND lsxn.SoLuong > 0
                      AND IFNULL(dsdh.LoaiDon, 0) = @loaiDon
                    ";
                }
            }

            sql += " ORDER BY lsxn.id ASC;";

            using (var cmd = new SQLiteCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@tenPhieu", tenPhieu.Trim());
                cmd.Parameters.AddWithValue("@nguoiLam", nguoiLam ?? string.Empty);

                if (!IsDichVu(model) && model.IsNhap)
                    cmd.Parameters.AddWithValue("@loaiDon", model.Id);

                return FillDataTable(cmd);
            }
        }

        private static DataTable LoadChiTietXuatGop(
            SQLiteConnection conn,
            KieuNhapXuat_Model model,
            string tenPhieu,
            int? danhSachKhoId,
            string nguoiLam)
        {
            string sql = @"
                SELECT
                    MIN(lsxn.id) AS id,
                    MIN(lsxn.id) AS LichSuXuatNhap_ID,
                    GROUP_CONCAT(lsxn.id) AS LichSuXuatNhap_IDs,
                    NULL AS DanhSachDatHang_ID,
                    NULL AS ThongTinDatHang_ID,
                    dms.id AS DanhSachMaSP_ID,
                    lsxn.DanhSachKho_ID AS DanhSachKho_ID,
                    kho.TenKho AS TenKho,
                    MAX(lsxn.DanhSachNCC_ID) AS DanhSachNCC_ID,
                    MAX(lsxn.NhaCC) AS NhaCungCap,
                    MIN(IFNULL(lsxn.CanEdit, 1)) AS CanEdit,
                    1 AS IsGroupXuat,
                    lsxn.TenPhieu AS TenPhieu,
                    lsxn.TenPhieu AS MaDon,
                    dms.Ten AS ten,
                    dms.Ma AS ma,
                    dms.DonVi AS donVi,
                    ABS(SUM(lsxn.SoLuong)) AS yeuCau,
                    MAX(lsxn.Ngay) AS ngay,
                    ABS(SUM(lsxn.SoLuong)) AS thucNhan,
                    MAX(lsxn.DonGia) AS donGia,
                    ABS(SUM(lsxn.SoLuong)) * IFNULL(MAX(lsxn.DonGia), 0) AS thanhTien,
                    MAX(lsxn.GhiChu) AS ghiChu,
                    MAX(lsxn.NguoiGiao_Nhan) AS NguoiGiao_Nhan,
                    MAX(lsxn.LyDo) AS LyDo
                FROM LichSuXuatNhap lsxn
                INNER JOIN ThongTinDatHang ttdh
                    ON ttdh.id = lsxn.ThongTinDatHang_ID
                INNER JOIN DanhSachMaSP dms
                    ON dms.id = ttdh.DanhSachMaSP_ID
                LEFT JOIN DanhSachKho kho
                    ON kho.id = lsxn.DanhSachKho_ID
                WHERE lsxn.TenPhieu = @tenPhieu
                  AND IFNULL(lsxn.NguoiLam, '') = @nguoiLam
                  AND lsxn.SoLuong < 0
                GROUP BY
                    lsxn.TenPhieu,
                    dms.id,
                    dms.Ten,
                    dms.Ma,
                    dms.DonVi,
                    lsxn.DanhSachKho_ID,
                    kho.TenKho
                ORDER BY dms.Ten ASC;
            ";

            using (var cmd = new SQLiteCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@tenPhieu", tenPhieu.Trim());
                cmd.Parameters.AddWithValue("@nguoiLam", nguoiLam ?? string.Empty);
                return FillDataTable(cmd);
            }
        }

        private static VatTuDbRow GetVatTuById(SQLiteConnection conn, SQLiteTransaction tran, int? danhSachMaSPId)
        {
            if (!danhSachMaSPId.HasValue)
                throw new InvalidOperationException("Chưa chọn vật tư.");

            string sql = @"
                SELECT id, Ten, Ma, DonVi
                FROM DanhSachMaSP
                WHERE id = @id
                LIMIT 1;
            ";

            using (var cmd = new SQLiteCommand(sql, conn, tran))
            {
                cmd.Parameters.AddWithValue("@id", danhSachMaSPId.Value);

                using (var reader = cmd.ExecuteReader())
                {
                    if (!reader.Read())
                        throw new InvalidOperationException("Không tìm thấy vật tư đã chọn trong DanhSachMaSP.");

                    return new VatTuDbRow
                    {
                        Id = Convert.ToInt32(reader["id"]),
                        Ten = Convert.ToString(reader["Ten"]),
                        Ma = Convert.ToString(reader["Ma"]),
                        DonVi = Convert.ToString(reader["DonVi"])
                    };
                }
            }
        }

        private static long ExecuteInsertAndGetId(SQLiteCommand cmd)
        {
            cmd.ExecuteNonQuery();

            using (var idCmd = new SQLiteCommand("SELECT last_insert_rowid();", cmd.Connection, cmd.Transaction))
            {
                return Convert.ToInt64(idCmd.ExecuteScalar());
            }
        }

        private static DataTable FillDataTable(SQLiteCommand cmd)
        {
            var dt = new DataTable();
            using (var adapter = new SQLiteDataAdapter(cmd))
            {
                adapter.Fill(dt);
            }
            return dt;
        }

        private static Dictionary<string, object> CreateKeywordParameters(string keyword)
        {
            string keywordKhongDau = CoreHelper.BoDauTiengViet(keyword) ?? keyword;

            return new Dictionary<string, object>
            {
                ["@kw"] = "%" + keyword + "%",
                ["@kwNoDau"] = "%" + keywordKhongDau + "%",
                ["@limit"] = SearchLimit
            };
        }

        private static DataTable ExecuteSearchQuery(string sql, Dictionary<string, object> parameters, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var dt = new DataTable();

            using (var conn = DB_Base.OpenConnection())
            using (var cmd = new SQLiteCommand(sql, conn))
            {
                foreach (var p in parameters)
                    cmd.Parameters.AddWithValue(p.Key, p.Value ?? DBNull.Value);

                using (var adapter = new SQLiteDataAdapter(cmd))
                {
                    adapter.Fill(dt);
                }
            }

            cancellationToken.ThrowIfCancellationRequested();

            return dt;
        }

        private static object NullIfWhiteSpace(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? (object)DBNull.Value : value.Trim();
        }

        private static decimal ToDecimal(object value)
        {
            if (value == null || value == DBNull.Value)
                return 0;

            if (value is decimal decimalValue)
                return decimalValue;

            if (value is double doubleValue)
                return Convert.ToDecimal(doubleValue);

            if (value is float floatValue)
                return Convert.ToDecimal(floatValue);

            if (value is int intValue)
                return intValue;

            if (value is long longValue)
                return longValue;

            decimal.TryParse(Convert.ToString(value), out decimal result);
            return result;
        }

        private static DataTable CreateEmptySearchTable()
        {
            var dt = new DataTable();

            dt.Columns.Add("id", typeof(object));
            dt.Columns.Add("ThongTinDatHang_ID", typeof(object));
            dt.Columns.Add("DanhSachMaSP_ID", typeof(object));
            dt.Columns.Add("ten", typeof(string));
            dt.Columns.Add("ma", typeof(string));
            dt.Columns.Add("donvi", typeof(string));
            dt.Columns.Add("MaDon", typeof(string));
            dt.Columns.Add("SoLuongYeuCau", typeof(decimal));
            dt.Columns.Add("SoLuongTon", typeof(decimal));
            dt.Columns.Add("DonGia", typeof(decimal));

            return dt;
        }

        private static DataTable CreateEmptyKhoHangSearchTable()
        {
            var dt = new DataTable();

            dt.Columns.Add("id", typeof(object));
            dt.Columns.Add("TenKho", typeof(string));
            dt.Columns.Add("TenKho_KhongDau", typeof(string));

            return dt;
        }

        private static DataTable CreateEmptyNhaCungCapSearchTable()
        {
            var dt = new DataTable();

            dt.Columns.Add("id", typeof(object));
            dt.Columns.Add("TenNCC", typeof(string));
            dt.Columns.Add("TenNCC_KhongDau", typeof(string));

            return dt;
        }

        private static DataTable CreateEmptyTimPhieuTable()
        {
            var dt = new DataTable();
            dt.Columns.Add("TenPhieu", typeof(string));
            dt.Columns.Add("Ngay", typeof(string));
            return dt;
        }

        private static DataTable CreateEmptyChiTietTable()
        {
            var dt = new DataTable();

            dt.Columns.Add("id", typeof(object));
            dt.Columns.Add("LichSuXuatNhap_ID", typeof(object));
            dt.Columns.Add("LichSuXuatNhap_IDs", typeof(string));
            dt.Columns.Add("DanhSachDatHang_ID", typeof(object));
            dt.Columns.Add("ThongTinDatHang_ID", typeof(object));
            dt.Columns.Add("DanhSachMaSP_ID", typeof(object));
            dt.Columns.Add("DanhSachKho_ID", typeof(object));
            dt.Columns.Add("TenKho", typeof(string));
            dt.Columns.Add("DanhSachNCC_ID", typeof(object));
            dt.Columns.Add("NhaCungCap", typeof(string));
            dt.Columns.Add("CanEdit", typeof(int));
            dt.Columns.Add("IsGroupXuat", typeof(int));
            dt.Columns.Add("TenPhieu", typeof(string));
            dt.Columns.Add("MaDon", typeof(string));
            dt.Columns.Add("ten", typeof(string));
            dt.Columns.Add("ma", typeof(string));
            dt.Columns.Add("donVi", typeof(string));
            dt.Columns.Add("yeuCau", typeof(decimal));
            dt.Columns.Add("ngay", typeof(string));
            dt.Columns.Add("thucNhan", typeof(decimal));
            dt.Columns.Add("donGia", typeof(decimal));
            dt.Columns.Add("thanhTien", typeof(decimal));
            dt.Columns.Add("ghiChu", typeof(string));
            dt.Columns.Add("NguoiGiao_Nhan", typeof(string));
            dt.Columns.Add("LyDo", typeof(string));

            return dt;
        }

        private static bool IsDichVu(KieuNhapXuat_Model model)
        {
            return model != null
                   && (model.IsDichVu
                       || string.Equals(model.Ten, TenKieuNhapXuat.DICH_VU, StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsNhapKhoTheoDon(KieuNhapXuat_Model model)
        {
            return model != null
                   && model.IsNhap
                   && !model.IsDichVu
                   && !model.IsKhac;
        }

        private static bool IsNhapKhoKhac(KieuNhapXuat_Model model)
        {
            return model != null
                   && model.IsNhap
                   && !model.IsDichVu
                   && model.IsKhac;
        }

        private static bool IsXuat(KieuNhapXuat_Model model)
        {
            return model != null
                   && !model.IsNhap
                   && !model.IsDichVu;
        }

        private sealed class LichSuXuatNhapDeleteInfo
        {
            public int LichSuXuatNhapId { get; set; }
            public int? ThongTinDatHangId { get; set; }
            public int? DanhSachDatHangId { get; set; }
            public int CanEdit { get; set; }
        }

        private sealed class VatTuDbRow
        {
            public int Id { get; set; }
            public string Ten { get; set; }
            public string Ma { get; set; }
            public string DonVi { get; set; }
        }
    }
}
