using DG_TonKhoBTP_v02.Models.KeToan.VatTuKhac;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Text;

namespace DG_TonKhoBTP_v02.Database.KeToan.VatTuKhac
{
    internal static class BaoCao_DB
    {
        public static DataTable GetNguoiThucHien()
        {
            return DB_Base.GetData("SELECT username FROM users");
        }

        public static DataTable TinhTonKho(DateTime? ngayBatDau, DateTime? ngayKetThuc, int? idKho = null)
        {
            bool coLocNgay = ngayBatDau.HasValue && ngayKetThuc.HasValue;

            string khoFilter = (idKho.HasValue && idKho.Value > 0)
                ? "AND lx.DanhSachKho_ID = @IdKho" : "";

            string tonDauKyNgayFilter = coLocNgay ? "WHERE lx.Ngay < @NgayBatDau" : "WHERE 1=0";
            string phatSinhNgayFilter = coLocNgay ? "AND lx.Ngay >= @NgayBatDau AND lx.Ngay <= @NgayKetThuc" : "";

            string sql = $@"
                WITH
                TonDauKy AS (
                    SELECT
                        lx.DanhSachKho_ID,
                        ttdh.DanhSachMaSP_ID,
                        SUM(lx.SoLuong) AS TonDau
                    FROM LichSuXuatNhap lx
                    INNER JOIN ThongTinDatHang ttdh ON ttdh.id = lx.ThongTinDatHang_ID
                    {tonDauKyNgayFilter}
                      AND lx.DanhSachKho_ID IS NOT NULL
                      AND lx.ThongTinDatHang_ID IS NOT NULL
                      {khoFilter}
                    GROUP BY lx.DanhSachKho_ID, ttdh.DanhSachMaSP_ID
                ),
                PhatSinhKy AS (
                    SELECT
                        lx.DanhSachKho_ID,
                        ttdh.DanhSachMaSP_ID,
                        SUM(CASE WHEN lx.SoLuong > 0 THEN  lx.SoLuong ELSE 0 END) AS TongNhap,
                        SUM(CASE WHEN lx.SoLuong < 0 THEN -lx.SoLuong ELSE 0 END) AS TongXuat
                    FROM LichSuXuatNhap lx
                    INNER JOIN ThongTinDatHang ttdh ON ttdh.id = lx.ThongTinDatHang_ID
                    WHERE lx.DanhSachKho_ID IS NOT NULL
                      AND lx.ThongTinDatHang_ID IS NOT NULL
                      {phatSinhNgayFilter}
                      {khoFilter}
                    GROUP BY lx.DanhSachKho_ID, ttdh.DanhSachMaSP_ID
                ),
                AllKeys AS (
                    SELECT DanhSachKho_ID, DanhSachMaSP_ID FROM TonDauKy
                    UNION
                    SELECT DanhSachKho_ID, DanhSachMaSP_ID FROM PhatSinhKy
                )
                SELECT
                    sp.Ten                                                AS TenVatTu,
                    sp.Ma                                                 AS MaVatTu,
                    sp.DonVi                                              AS DonVi,
                    kho.TenKho                                            AS TenKho,
                    COALESCE(tdk.TonDau,   0)                             AS TonDauKy,
                    COALESCE(psk.TongNhap, 0)                             AS TongNhap,
                    COALESCE(psk.TongXuat, 0)                             AS TongXuat,
                    COALESCE(tdk.TonDau, 0) + COALESCE(psk.TongNhap, 0)
                                            - COALESCE(psk.TongXuat, 0)   AS TonCuoiKy
                FROM AllKeys ak
                INNER JOIN DanhSachMaSP sp  ON sp.id  = ak.DanhSachMaSP_ID
                INNER JOIN DanhSachKho  kho ON kho.id = ak.DanhSachKho_ID
                LEFT  JOIN TonDauKy    tdk  ON tdk.DanhSachMaSP_ID = ak.DanhSachMaSP_ID
                                           AND tdk.DanhSachKho_ID  = ak.DanhSachKho_ID
                LEFT  JOIN PhatSinhKy  psk  ON psk.DanhSachMaSP_ID = ak.DanhSachMaSP_ID
                                           AND psk.DanhSachKho_ID  = ak.DanhSachKho_ID
                ORDER BY sp.Ma;
            ";

            var dt = new DataTable();
            dt.Columns.Add("Tên Vật Tư", typeof(string));
            dt.Columns.Add("Mã Vật Tư", typeof(string));
            dt.Columns.Add("Đơn vị", typeof(string));
            dt.Columns.Add("Tên Kho", typeof(string));
            dt.Columns.Add("Tồn Đầu Kỳ", typeof(decimal));
            dt.Columns.Add("Tổng Nhập", typeof(decimal));
            dt.Columns.Add("Tổng Xuất", typeof(decimal));
            dt.Columns.Add("Tồn Cuối Kỳ", typeof(decimal));

            using var connection = DB_Base.OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = sql;

            if (coLocNgay)
            {
                command.Parameters.AddWithValue("@NgayBatDau", ngayBatDau.Value.ToString("yyyy-MM-dd"));
                command.Parameters.AddWithValue("@NgayKetThuc", ngayKetThuc.Value.ToString("yyyy-MM-dd 23:59:59"));
            }

            if (idKho.HasValue && idKho.Value > 0)
                command.Parameters.AddWithValue("@IdKho", idKho.Value);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                dt.Rows.Add(
                    reader.IsDBNull(0) ? string.Empty : reader.GetString(0),
                    reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                    reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                    reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                    reader.IsDBNull(4) ? 0m : reader.GetDecimal(4),
                    reader.IsDBNull(5) ? 0m : reader.GetDecimal(5),
                    reader.IsDBNull(6) ? 0m : reader.GetDecimal(6),
                    reader.IsDBNull(7) ? 0m : reader.GetDecimal(7)
                );
            }

            return dt;
        }

        public static DataTable GetBaoCaoDatHang(string nguoiThucHien, DateTime? ngayBatDau = null, DateTime? ngayKetThuc = null)
        {
            var dt = new DataTable();
            try
            {
                var sql = new StringBuilder(@"
                    SELECT
                        dh.id AS dh_id,
                        tt.id AS ThongTinDatHang_ID,
                        dh.MaDon AS MaDon,
                        dh.NguoiDat AS NguoiDat,
                        CASE
                            WHEN tt.TenVatTu IS NULL OR TRIM(tt.TenVatTu) = ''
                                THEN sp.Ten
                            ELSE tt.TenVatTu
                        END AS TenVatTu,
                        tt.SoLuongMua AS SoLuongMua,
                        COALESCE(hv.TongHangVe, 0) AS TongHangVe,
                        tt.MucDichMua AS MucDichMua,
                        strftime('%d/%m/%Y', tt.NgayGiao) AS NgayGiao,
                        strftime('%d/%m/%Y', tt.Date_Insert) AS Date_Insert,
                        tt.DonGia AS tt_DonGia
                    FROM ThongTinDatHang tt
                    INNER JOIN DanhSachDatHang dh
                        ON tt.DanhSachDatHang_ID = dh.id
                    LEFT JOIN DanhSachMaSP sp
                        ON tt.DanhSachMaSP_ID = sp.id
                    LEFT JOIN (
                        SELECT
                            ThongTinDatHang_ID,
                            SUM(SoLuong) AS TongHangVe
                        FROM LichSuXuatNhap
                        WHERE SoLuong > 0
                        GROUP BY ThongTinDatHang_ID
                    ) hv
                        ON hv.ThongTinDatHang_ID = tt.id
                    WHERE 1 = 1
                ");
                var parameters = new List<SQLiteParameter>();

                if (!string.IsNullOrWhiteSpace(nguoiThucHien))
                {
                    sql.AppendLine(" AND dh.NguoiDat = @NguoiDat ");
                    parameters.Add(new SQLiteParameter("@NguoiDat", nguoiThucHien.Trim()));
                }

                if (ngayBatDau.HasValue)
                {
                    sql.AppendLine(" AND DATE(dh.DateInsert) >= DATE(@NgayBatDau) ");
                    parameters.Add(new SQLiteParameter("@NgayBatDau", ngayBatDau.Value.ToString("yyyy-MM-dd")));
                }

                if (ngayKetThuc.HasValue)
                {
                    sql.AppendLine(" AND DATE(dh.DateInsert) <= DATE(@NgayKetThuc) ");
                    parameters.Add(new SQLiteParameter("@NgayKetThuc", ngayKetThuc.Value.ToString("yyyy-MM-dd")));
                }

                sql.AppendLine(" ORDER BY dh.DateInsert DESC, dh.id DESC, tt.id DESC ");

                using var conn = DB_Base.OpenConnection();
                using var cmd = new SQLiteCommand(sql.ToString(), conn);
                cmd.Parameters.AddRange(parameters.ToArray());
                using var adapter = new SQLiteDataAdapter(cmd);
                adapter.Fill(dt);
            }
            catch (Exception ex)
            {
                throw new Exception($"GetBaoCaoDatHang lỗi: {ex.Message}", ex);
            }

            return dt;
        }

        public static DataTable GetBaoCaoXuatHang(
            int kho,
            string nguoiThucHien,
            DateTime? ngayBatDau = null,
            DateTime? ngayKetThuc = null)
        {
            var dt = new DataTable();

            try
            {
                var sql = new StringBuilder(@"
                    WITH DuLieuXuat AS (
                        SELECT
                            lsxn.id AS lsxn_id,
                            lsxn.DanhSachKho_ID,
                            lsxn.Ngay,
                            lsxn.TenPhieu,
                            lsxn.LyDo,
                            lsxn.SoLuong,
                            CASE
                                WHEN lsxn.DonGia IS NULL
                                     OR TRIM(CAST(lsxn.DonGia AS TEXT)) = ''
                                    THEN NULL
                                ELSE CAST(lsxn.DonGia AS REAL)
                            END AS DonGiaXuat,
                            lsxn.DanhSachNcc_ID,
                            lsxn.GhiChu,
                            lsxn.NguoiGiao_Nhan,
                            lsxn.NguoiLam,
                            COALESCE(lsxn.canEdit, 0) AS canEdit,
                            ttdh.DanhSachMaSP_ID,
                            ttdh.TenVatTu,
                            ttdh.TenVatTu_KhongDau,
                            dssp.Ma,
                            dssp.Ten AS TenVatTuDanhMuc,
                            dssp.DonVi,
                            dsk.TenKho,
                            ncc.TenNcc AS DoiTuongCongNo
                        FROM LichSuXuatNhap lsxn
                        INNER JOIN ThongTinDatHang ttdh
                            ON lsxn.ThongTinDatHang_ID = ttdh.id
                        LEFT JOIN DanhSachMaSP dssp
                            ON ttdh.DanhSachMaSP_ID = dssp.id
                        LEFT JOIN DanhSachKho dsk
                            ON lsxn.DanhSachKho_ID = dsk.id
                        LEFT JOIN DanhSachNcc ncc
                            ON lsxn.DanhSachNcc_ID = ncc.id
                        WHERE lsxn.SoLuong < 0
                ");

                var parameters = new List<SQLiteParameter>();

                if (kho > 0)
                {
                    sql.AppendLine(" AND lsxn.DanhSachKho_ID = @DanhSachKho_ID ");
                    parameters.Add(new SQLiteParameter("@DanhSachKho_ID", kho));
                }

                if (!string.IsNullOrWhiteSpace(nguoiThucHien))
                {
                    sql.AppendLine(" AND lsxn.NguoiLam = @NguoiLam ");
                    parameters.Add(new SQLiteParameter("@NguoiLam", nguoiThucHien.Trim()));
                }

                if (ngayBatDau.HasValue)
                {
                    sql.AppendLine(" AND DATE(lsxn.Ngay) >= DATE(@NgayBatDau) ");
                    parameters.Add(new SQLiteParameter("@NgayBatDau", ngayBatDau.Value.ToString("yyyy-MM-dd")));
                }

                if (ngayKetThuc.HasValue)
                {
                    sql.AppendLine(" AND DATE(lsxn.Ngay) <= DATE(@NgayKetThuc) ");
                    parameters.Add(new SQLiteParameter("@NgayKetThuc", ngayKetThuc.Value.ToString("yyyy-MM-dd")));
                }

                sql.AppendLine(@"
                    )
                    SELECT
                        DanhSachKho_ID,
                        MAX(TenKho) AS TenKho,
                        TenPhieu,
                        REPLACE(
                            GROUP_CONCAT(DISTINCT strftime('%d/%m/%Y', Ngay)),
                            ',',
                            '; '
                        ) AS NgayXuatNhap,
                        MAX(LyDo) AS LyDo,
                        MAX(Ma) AS Ma,
                        MAX(
                            CASE
                                WHEN DanhSachMaSP_ID IS NULL THEN TenVatTu
                                ELSE TenVatTuDanhMuc
                            END
                        ) AS TenVatTu,
                        MAX(CASE WHEN DanhSachMaSP_ID IS NULL THEN '' ELSE DonVi END) AS DonVi,
                        ABS(SUM(SoLuong)) AS SoLuong,
                        MAX(DonGiaXuat) AS DonGiaXuat,
                        MAX(DoiTuongCongNo) AS DoiTuongCongNo,
                        MAX(NguoiGiao_Nhan) AS NguoiGiaoNhan,
                        MAX(NguoiLam) AS NguoiLam,
                        REPLACE(
                            GROUP_CONCAT(
                                DISTINCT CASE
                                    WHEN GhiChu IS NULL OR TRIM(GhiChu) = '' THEN NULL
                                    ELSE TRIM(GhiChu)
                                END
                            ),
                            ',',
                            '; '
                        ) AS GhiChu,
                        DanhSachMaSP_ID,
                        CASE
                            WHEN DanhSachMaSP_ID IS NULL THEN TenVatTu_KhongDau
                            ELSE NULL
                        END AS TenVatTu_KhongDau,
                        GROUP_CONCAT(lsxn_id, ';') AS DanhSachLichSuID,
                        MIN(canEdit) AS canEdit
                    FROM DuLieuXuat
                    GROUP BY
                        DanhSachKho_ID,
                        TenPhieu,
                        CASE WHEN DanhSachMaSP_ID IS NULL THEN 0 ELSE 1 END,
                        DanhSachMaSP_ID,
                        CASE
                            WHEN DanhSachMaSP_ID IS NULL THEN TenVatTu_KhongDau
                            ELSE NULL
                        END
                    ORDER BY MAX(Ngay) DESC, MAX(lsxn_id) DESC;
                ");

                using var conn = DB_Base.OpenConnection();
                using var cmd = new SQLiteCommand(sql.ToString(), conn);
                cmd.Parameters.AddRange(parameters.ToArray());
                using var adapter = new SQLiteDataAdapter(cmd);
                adapter.Fill(dt);
            }
            catch (Exception ex)
            {
                throw new Exception($"GetBaoCaoXuatHang lỗi: {ex.Message}", ex);
            }

            return dt;
        }

        public static DataTable GetBaoCaoLichSuXuatNhap(
            int kho,
            string nguoiThucHien,
            bool isNhap,
            DateTime? ngayBatDau = null,
            DateTime? ngayKetThuc = null)
        {
            var dt = new DataTable();
            try
            {
                var sql = new StringBuilder(@"
                    SELECT
                        lsxn.id AS lsxn_id,
                        lsxn.DanhSachKho_ID AS DanhSachKho_ID,
                        strftime('%d/%m/%Y', lsxn.Ngay) AS NgayXuatNhap,
                        lsxn.TenPhieu AS TenPhieu,
                        lsxn.LyDo AS LyDo,
                        dssp.Ma AS Ma,
                        CASE
                            WHEN ttdh.DanhSachMaSP_ID IS NULL
                                THEN ttdh.TenVatTu
                            ELSE dssp.Ten
                        END AS TenVatTu,
                        lsxn.SoLuong AS SoLuong,
                        lsxn.DonGia AS DonGia,
                        dsk.TenKho AS TenKho,
                        ncc.TenNcc AS DoiTuongCongNo,
                        lsxn.GhiChu AS GhiChu,
                        dsdh.MaDon AS MaDon,
                        dsdh.NguoiDat AS NguoiDat,
                        lsxn.NguoiGiao_Nhan AS NguoiGiaoNhan,
                        strftime('%d/%m/%Y', dsdh.DateInsert) AS NgayDatPR,
                        lsxn.NguoiLam AS NguoiLam,
                        ttdh.SoLuongMua AS SoLuongMua,
                        ttdh.MucDichMua AS MucDichMua,
                        strftime('%d/%m/%Y', ttdh.NgayGiao) AS NgayGiao,
                        strftime('%d/%m/%Y', ttdh.Date_Insert) AS Date_Insert,
                        lsxn.canEdit AS canEdit,
                        ttdh.DonGia AS DonGia
                    FROM LichSuXuatNhap lsxn
                    INNER JOIN ThongTinDatHang ttdh
                        ON lsxn.ThongTinDatHang_ID = ttdh.id
                    INNER JOIN DanhSachDatHang dsdh
                        ON ttdh.DanhSachDatHang_ID = dsdh.id
                    LEFT JOIN DanhSachMaSP dssp
                        ON ttdh.DanhSachMaSP_ID = dssp.id
                    LEFT JOIN DanhSachKho dsk
                        ON lsxn.DanhSachKho_ID = dsk.id
                    LEFT JOIN DanhSachNcc ncc
                        ON lsxn.DanhSachNcc_ID = ncc.id
                    WHERE 1 = 1
                ");
                var parameters = new List<SQLiteParameter>();

                if (kho > 0)
                {
                    sql.AppendLine(" AND lsxn.DanhSachKho_ID = @DanhSachKho_ID ");
                    parameters.Add(new SQLiteParameter("@DanhSachKho_ID", kho));
                }

                if (!string.IsNullOrWhiteSpace(nguoiThucHien))
                {
                    sql.AppendLine(" AND lsxn.NguoiLam = @NguoiLam ");
                    parameters.Add(new SQLiteParameter("@NguoiLam", nguoiThucHien.Trim()));
                }

                if (ngayBatDau.HasValue)
                {
                    sql.AppendLine(" AND DATE(lsxn.Ngay) >= DATE(@NgayBatDau) ");
                    parameters.Add(new SQLiteParameter("@NgayBatDau", ngayBatDau.Value.ToString("yyyy-MM-dd")));
                }

                if (ngayKetThuc.HasValue)
                {
                    sql.AppendLine(" AND DATE(lsxn.Ngay) <= DATE(@NgayKetThuc) ");
                    parameters.Add(new SQLiteParameter("@NgayKetThuc", ngayKetThuc.Value.ToString("yyyy-MM-dd")));
                }

                sql.AppendLine(isNhap ? " AND lsxn.SoLuong > 0 " : " AND lsxn.SoLuong < 0 ");
                sql.AppendLine(" ORDER BY lsxn.Ngay DESC, lsxn.id DESC; ");

                using var conn = DB_Base.OpenConnection();
                using var cmd = new SQLiteCommand(sql.ToString(), conn);
                cmd.Parameters.AddRange(parameters.ToArray());
                using var adapter = new SQLiteDataAdapter(cmd);
                adapter.Fill(dt);
            }
            catch (Exception ex)
            {
                throw new Exception($"GetBaoCaoLichSuXuatNhap lỗi: {ex.Message}", ex);
            }

            return dt;
        }

        public static void UpdateCanEdit(IEnumerable<BaoCao_Model.CanEdit> items)
        {
            using var conn = DB_Base.OpenConnection();
            using var transaction = conn.BeginTransaction();

            try
            {
                const string sql = @"
                    UPDATE LichSuXuatNhap
                    SET CanEdit = @CanEdit
                    WHERE id = @Id
                ";

                using var cmd = new SQLiteCommand(sql, conn, transaction);
                cmd.Parameters.Add("@CanEdit", DbType.Int32);
                cmd.Parameters.Add("@Id", DbType.Int32);

                if (items != null)
                {
                    foreach (var item in items)
                    {
                        if (item == null) continue;

                        cmd.Parameters["@CanEdit"].Value = item.Value;
                        cmd.Parameters["@Id"].Value = item.Id;
                        cmd.ExecuteNonQuery();
                    }
                }

                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                throw new Exception($"UpdateCanEdit lỗi: {ex.Message}", ex);
            }
        }

        public static void DeleteDanhSachDatHang(int id)
        {
            const string sqlDelete = "DELETE FROM DanhSachDatHang WHERE id = @Id;";

            using var conn = DB_Base.OpenConnection();
            using var tx = conn.BeginTransaction();
            try
            {
                using var cmd = new SQLiteCommand(sqlDelete, conn, tx);
                cmd.Parameters.AddWithValue("@Id", id);

                int affected = cmd.ExecuteNonQuery();
                if (affected == 0)
                    throw new Exception($"Không tìm thấy DanhSachDatHang ID = {id} để xóa.");

                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        public static void DeleteLichSuXuatNhap(int id)
        {
            const string sqlDelete = "DELETE FROM LichSuXuatNhap WHERE id = @Id;";

            using var conn = DB_Base.OpenConnection();
            using var tx = conn.BeginTransaction();
            try
            {
                using var cmd = new SQLiteCommand(sqlDelete, conn, tx);
                cmd.Parameters.AddWithValue("@Id", id);

                int affected = cmd.ExecuteNonQuery();
                if (affected == 0)
                    throw new Exception($"Không tìm thấy LichSuXuatNhap ID = {id} để xóa.");

                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        public static void UpdateThongTinDatHang(BaoCao_Model.ThongTinDatHangUpdate model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            const string sql = @"
                UPDATE ThongTinDatHang
                SET
                    TenVatTu   = @TenVatTu,
                    SoLuongMua = @SoLuongMua,
                    DonGia     = @DonGia,
                    MucDichMua = @MucDichMua,
                    NgayGiao   = @NgayGiao
                WHERE id = @Id;
            ";

            using var conn = DB_Base.OpenConnection();
            using var tx = conn.BeginTransaction();

            try
            {
                using var cmd = new SQLiteCommand(sql, conn, tx);
                cmd.Parameters.AddWithValue("@Id", model.Id);
                cmd.Parameters.AddWithValue("@TenVatTu", model.TenVatTu ?? "");
                cmd.Parameters.AddWithValue("@SoLuongMua", model.SoLuongMua);
                cmd.Parameters.AddWithValue("@DonGia", model.DonGia);
                cmd.Parameters.AddWithValue("@MucDichMua", string.IsNullOrWhiteSpace(model.MucDichMua) ? (object)DBNull.Value : model.MucDichMua.Trim());
                cmd.Parameters.AddWithValue("@NgayGiao", string.IsNullOrWhiteSpace(model.NgayGiao) ? (object)DBNull.Value : model.NgayGiao.Trim());

                int affected = cmd.ExecuteNonQuery();
                if (affected == 0)
                    throw new Exception($"Không tìm thấy ThongTinDatHang ID = {model.Id}.");

                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        public static void UpdateLichSuXuatNhap(BaoCao_Model.LichSuXuatNhapUpdate model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            const string sql = @"
                UPDATE LichSuXuatNhap
                SET
                    SoLuong          = @SoLuong,
                    NguoiGiao_Nhan   = @NguoiGiaoNhan,
                    DanhSachKho_ID   = @DanhSachKhoId,
                    LyDo             = @LyDo,
                    Ngay             = @Ngay,
                    TenPhieu         = @TenPhieu,
                    GhiChu           = @GhiChu
                WHERE id = @Id;
            ";

            using var conn = DB_Base.OpenConnection();
            using var tx = conn.BeginTransaction();

            try
            {
                using var cmd = new SQLiteCommand(sql, conn, tx);
                cmd.Parameters.AddWithValue("@Id", model.Id);
                cmd.Parameters.AddWithValue("@SoLuong", model.SoLuong);
                cmd.Parameters.AddWithValue("@NguoiGiaoNhan", string.IsNullOrWhiteSpace(model.NguoiGiaoNhan) ? (object)DBNull.Value : model.NguoiGiaoNhan.Trim());
                cmd.Parameters.AddWithValue("@DanhSachKhoId", model.DanhSachKhoId.HasValue ? (object)model.DanhSachKhoId.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@LyDo", string.IsNullOrWhiteSpace(model.LyDo) ? (object)DBNull.Value : model.LyDo.Trim());
                cmd.Parameters.AddWithValue("@Ngay", string.IsNullOrWhiteSpace(model.Ngay) ? (object)DBNull.Value : model.Ngay.Trim());
                cmd.Parameters.AddWithValue("@TenPhieu", string.IsNullOrWhiteSpace(model.TenPhieu) ? (object)DBNull.Value : model.TenPhieu.Trim());
                cmd.Parameters.AddWithValue("@GhiChu", string.IsNullOrWhiteSpace(model.GhiChu) ? (object)DBNull.Value : model.GhiChu.Trim());

                int affected = cmd.ExecuteNonQuery();
                if (affected == 0)
                    throw new Exception($"Không tìm thấy LichSuXuatNhap ID = {model.Id}.");

                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }
    }
}
