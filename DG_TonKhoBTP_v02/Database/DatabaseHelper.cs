
using DG_TonKhoBTP_v02.Core;
using DG_TonKhoBTP_v02.Dictionary;
using DG_TonKhoBTP_v02.DL_Ben;
using DG_TonKhoBTP_v02.Helper;
using DG_TonKhoBTP_v02.Models;
using DG_TonKhoBTP_v02.UI;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;
using static DG_TonKhoBTP_v02.Models.KeHoach;
using CoreHelper = DG_TonKhoBTP_v02.Helper.Helper;

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

        public static string GetStringConnector
        {
            get { return _connStr; }
        }


        public static bool TryPing(string dbPath, int timeoutSeconds = 3)
        {

            if (string.IsNullOrWhiteSpace(dbPath))
            {
                return false;
            }

            dbPath = System.IO.Path.GetFullPath(dbPath);

            if (!File.Exists(dbPath))
            {
                return false;
            }

            try
            {
                // Read Only để chỉ kiểm tra (không ghi/không tạo mới)
                // FailIfMissing để báo lỗi rõ nếu file thiếu
                // Default Timeout áp dụng cho các thao tác lock/chờ
                string connStr =
                    $"Data Source={dbPath};Version=3;Read Only=True;FailIfMissing=True;Default Timeout={timeoutSeconds};";

                using (var conn = new SQLiteConnection(connStr))
                {
                    conn.Open();

                    using (var cmd = new SQLiteCommand("SELECT 1;", conn))
                    {
                        cmd.CommandTimeout = timeoutSeconds;
                        cmd.ExecuteScalar();
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        

        public static string GenerateLotCode()
        {
            // Lấy 2 số cuối của năm hiện tại
            string yearSuffix = DateTime.Now.ToString("yy");

            // Lấy năm hiện tại để filter
            int currentYear = DateTime.Now.Year;

            using (var connection = new SQLiteConnection(_connStr))
            {
                connection.Open();

                // Đếm số lượng lot đã được tạo trong năm hiện tại
                string query = @"
                    SELECT COUNT(*) 
                    FROM KeHoachSX 
                    WHERE strftime('%Y', InsertedAt) = @currentYear";

                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@currentYear", currentYear.ToString());

                    long count = (long)command.ExecuteScalar();

                    // Số thứ tự mới = count + 1
                    int sequenceNumber = (int)count + 1;

                    // Format thành chuỗi 4 chữ số (padding với số 0)
                    string sequenceSuffix = sequenceNumber.ToString("D4");

                    // Kết hợp thành mã Lot
                    string lotCode = yearSuffix + sequenceSuffix;

                    return lotCode;
                }
            }
        }

        #region Kế hoạch sản xuất

        public static int TaoKeHoachMoi(DataGridView dgv)
        {
            if (dgv == null || dgv.Rows.Count == 0)
            {
                FrmWaiting.ShowGifAlert("KHÔNG CÓ DỮ LIỆU ĐỂ THÊM");
                return 0;
            }

            var errors = new List<string>();
            int successCount = 0;

            try
            {
                // BƯỚC 1: Map DataGridView → List<KeHoachSX>
                var keHoachList = MapDataGridViewToKeHoachList(dgv);

                if (keHoachList.Count == 0)
                {
                    FrmWaiting.ShowGifAlert("KHÔNG CÓ DỮ LIỆU ĐỂ THÊM");
                    return 0;
                }

                // BƯỚC 2: Batch lookup Ma → ID
                var lookupErrors = AssignDanhSachMaSP_IDs(keHoachList);
                errors.AddRange(lookupErrors);

                // BƯỚC 3: Insert - Để database validate
                var insertErrors = BulkInsertKeHoachSX(keHoachList, out successCount);
                errors.AddRange(insertErrors);

                // Hiển thị kết quả
                ShowInsertResult(successCount, errors);

                return successCount;
            }
            catch (Exception ex)
            {
                FrmWaiting.ShowGifAlert(CoreHelper.ShowErrorDatabase(ex, "KẾ HOẠCH"), "LỖI HỆ THỐNG");
                return 0;
            }
        }

        // ============================================================
        // BƯỚC 1: MAP DATAGRIDVIEW → LIST<KEHOACHSX>
        // ============================================================

        /// <summary>
        /// Map DataGridView → List<KeHoachSX>
        /// ✅ KHÔNG VALIDATE - Để database làm việc đó
        /// ✅ CHỈ CHUẨN HÓA DỮ LIỆU (uppercase, trim, format date)
        /// </summary>
        private static List<KeHoachSX> MapDataGridViewToKeHoachList(DataGridView dgv)
        {
            var keHoachList = new List<KeHoachSX>();

            foreach (DataGridViewRow row in dgv.Rows)
            {
                if (row.IsNewRow) continue;

                try
                {
                    // ✅ SỬ DỤNG MapRowToObject có sẵn
                    var keHoach = new KeHoachSX();
                    CoreHelper.MapRowToObject(row, keHoach);

                    // Chuẩn hóa dữ liệu
                    NormalizeKeHoachSX(keHoach);

                    keHoachList.Add(keHoach);
                }
                catch (Exception ex)
                {
                    // Chỉ log lỗi mapping, không dừng
                    System.Diagnostics.Debug.WriteLine($"Lỗi map dòng {row.Index + 1}: {ex.Message}");
                }
            }

            return keHoachList;
        }

        /// <summary>
        /// Chuẩn hóa dữ liệu KeHoachSX (không validate)
        /// </summary>
        private static void NormalizeKeHoachSX(KeHoachSX keHoach)
        {
            // Chuẩn hóa Ma, Lot sang chữ hoa
            if (!string.IsNullOrWhiteSpace(keHoach.Ma))
                keHoach.Ma = keHoach.Ma.ToUpper().Trim();

            if (!string.IsNullOrWhiteSpace(keHoach.Lot))
                keHoach.Lot = keHoach.Lot.ToUpper().Trim();

            // Chuẩn hóa ngày về format yyyy-MM-dd
            keHoach.NgayNhan = NormalizeDate(keHoach.NgayNhan);
            keHoach.NgayGiao = NormalizeDate(keHoach.NgayGiao);

            // Trim các string khác
            if (!string.IsNullOrWhiteSpace(keHoach.Ten))
                keHoach.Ten = keHoach.Ten.Trim();

            if (!string.IsNullOrWhiteSpace(keHoach.Mau))
                keHoach.Mau = keHoach.Mau.Trim();

            if (!string.IsNullOrWhiteSpace(keHoach.GhiChu))
                keHoach.GhiChu = keHoach.GhiChu.Trim();
        }

        /// <summary>
        /// Chuẩn hóa ngày về format yyyy-MM-dd (giống GetNgayHienTai)
        /// </summary>
        private static string NormalizeDate(string dateStr)
        {
            if (string.IsNullOrWhiteSpace(dateStr))
                return null;

            if (DateTime.TryParse(dateStr, out DateTime date))
                return date.ToString("yyyy-MM-dd");

            return dateStr;
        }

        // ============================================================
        // BƯỚC 2: BATCH LOOKUP Ma → ID
        // ============================================================

        /// <summary>
        /// Batch lookup Ma → DanhSachMaSP_ID
        /// </summary>
        private static List<string> AssignDanhSachMaSP_IDs(List<KeHoachSX> keHoachList)
        {
            var errors = new List<string>();

            try
            {
                // Thu thập unique Ma
                var uniqueMaList = keHoachList
                    .Select(k => k.Ma)
                    .Where(ma => !string.IsNullOrWhiteSpace(ma))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                if (uniqueMaList.Count == 0)
                    return errors;

                // Batch lookup trong 1 query
                Dictionary<string, int> maToIdMap;
                using (var conn = new SQLiteConnection(_connStr))
                {
                    conn.Open();
                    maToIdMap = BatchLookupMaToId(uniqueMaList, conn);
                }

                // Gán ID vào objects
                foreach (var keHoach in keHoachList)
                {
                    if (maToIdMap.TryGetValue(keHoach.Ma, out int id))
                    {
                        keHoach.DanhSachMaSP_ID = id;
                    }
                    else
                    {
                        // Không tìm thấy Ma → đánh dấu không hợp lệ
                        keHoach.DanhSachMaSP_ID = 0;
                        errors.Add($"Mã '{keHoach.Ma}' ({keHoach.Ten ?? ""}): Không tìm thấy trong danh sách sản phẩm");
                    }
                }
            }
            catch (Exception ex)
            {
                errors.Add($"Lỗi lookup mã SP: {CoreHelper.ShowErrorDatabase(ex)}");
            }

            return errors;
        }

        /// <summary>
        /// Batch lookup: Query tất cả Ma trong 1 query
        /// </summary>
        private static Dictionary<string, int> BatchLookupMaToId(
            HashSet<string> maList,
            SQLiteConnection conn)
        {
            var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            if (maList == null || maList.Count == 0)
                return result;

            // SQLite giới hạn 999 parameters → chia batch
            const int maxParams = 900;
            var maArray = maList.ToArray();

            for (int i = 0; i < maArray.Length; i += maxParams)
            {
                var batch = maArray.Skip(i).Take(maxParams).ToList();

                // Tạo SQL: WHERE Ma IN (@p0, @p1, ...)
                var paramNames = Enumerable.Range(0, batch.Count)
                    .Select(idx => $"@p{idx}")
                    .ToList();

                string sql = $@"
                    SELECT Ma, id 
                    FROM DanhSachMaSP 
                    WHERE Ma IN ({string.Join(", ", paramNames)});
                ";

                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    for (int j = 0; j < batch.Count; j++)
                        cmd.Parameters.AddWithValue($"@p{j}", batch[j]);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string ma = reader.GetString(0);
                            int id = reader.GetInt32(1);
                            result[ma] = id;
                        }
                    }
                }
            }

            return result;
        }

        // ============================================================
        // BƯỚC 3: BULK INSERT - ĐỂ DATABASE VALIDATE
        // ============================================================

        /// <summary>
        /// Bulk insert với transaction
        /// ✅ ĐỂ DATABASE VALIDATE: NOT NULL, UNIQUE, FOREIGN KEY...
        /// ✅ ShowErrorDatabase sẽ format lỗi thân thiện
        /// </summary>
        private static List<string> BulkInsertKeHoachSX(
            List<KeHoachSX> keHoachList,
            out int successCount)
        {
            var errors = new List<string>();
            successCount = 0;

            // Chỉ insert các record có DanhSachMaSP_ID hợp lệ
            var validKeHoachList = keHoachList
                .Where(k => k.DanhSachMaSP_ID > 0)
                .ToList();

            if (validKeHoachList.Count == 0)
                return errors;

            using (var conn = new SQLiteConnection(_connStr))
            {
                conn.Open();

                // ✅ TRANSACTION - Quan trọng nhất cho hiệu suất
                using (var tran = conn.BeginTransaction())
                {
                    try
                    {
                        // ✅ TÁI SỬ DỤNG COMMAND
                        using (var cmd = CreateInsertKeHoachSXCommand(conn, tran))
                        {
                            foreach (var keHoach in validKeHoachList)
                            {
                                try
                                {
                                    BindInsertKeHoachSXParameters(cmd, keHoach);
                                    cmd.ExecuteNonQuery();
                                    successCount++;
                                }
                                catch (Exception ex)
                                {
                                    // ✅ DATABASE TRẢ VỀ LỖI → ShowErrorDatabase xử lý
                                    // - NOT NULL constraint → "Thiếu dữ liệu bắt buộc (Lot)"
                                    // - UNIQUE constraint → "DỮ LIỆU đã tồn tại (Lot)"
                                    // - CHECK constraint → "Dữ liệu vi phạm ràng buộc"
                                    string errorMsg = CoreHelper.ShowErrorDatabase(ex, keHoach.Lot);
                                    errors.Add($"Lot '{keHoach.Lot}': {errorMsg}");
                                }
                            }
                        }

                        tran.Commit();
                    }
                    catch (Exception ex)
                    {
                        tran.Rollback();
                        errors.Add($"Lỗi hệ thống: {CoreHelper.ShowErrorDatabase(ex)}");
                    }
                }
            }

            return errors;
        }

        /// <summary>
        /// Tạo prepared statement cho INSERT
        /// </summary>
        private static SQLiteCommand CreateInsertKeHoachSXCommand(
            SQLiteConnection conn,
            SQLiteTransaction tran)
        {
            var cmd = new SQLiteCommand(conn);
            cmd.Transaction = tran;
            cmd.CommandText = @"
                INSERT INTO KeHoachSX (
                    DanhSachMaSP_ID, NgayNhan, Lot, SLHangDat, SLHangBan,
                    Mau, NgayGiao, GhiChu, TinhTrangKH, TinhTrangSX
                ) VALUES (
                    @DanhSachMaSP_ID, @NgayNhan, @Lot, @SLHangDat, @SLHangBan,
                    @Mau, @NgayGiao, @GhiChu, @TinhTrangKH, @TinhTrangSX
                );
            ";

            cmd.Parameters.Add("@DanhSachMaSP_ID", DbType.Int32);
            cmd.Parameters.Add("@NgayNhan", DbType.String);
            cmd.Parameters.Add("@Lot", DbType.String);
            cmd.Parameters.Add("@SLHangDat", DbType.Double);
            cmd.Parameters.Add("@SLHangBan", DbType.Double);
            cmd.Parameters.Add("@Mau", DbType.String);
            cmd.Parameters.Add("@NgayGiao", DbType.String);
            cmd.Parameters.Add("@GhiChu", DbType.String);
            cmd.Parameters.Add("@TinhTrangKH", DbType.Int32);
            cmd.Parameters.Add("@TinhTrangSX", DbType.Int32);

            return cmd;
        }

        /// <summary>
        /// Bind parameters từ object
        /// </summary>
        private static void BindInsertKeHoachSXParameters(SQLiteCommand cmd, KeHoachSX keHoach)
        {
            cmd.Parameters["@DanhSachMaSP_ID"].Value = keHoach.DanhSachMaSP_ID;
            cmd.Parameters["@NgayNhan"].Value = keHoach.NgayNhan ?? (object)DBNull.Value;
            cmd.Parameters["@Lot"].Value = keHoach.Lot ?? (object)DBNull.Value;
            cmd.Parameters["@SLHangDat"].Value = keHoach.SLHangDat ?? (object)DBNull.Value;
            cmd.Parameters["@SLHangBan"].Value = keHoach.SLHangBan ?? (object)DBNull.Value;
            cmd.Parameters["@Mau"].Value = keHoach.Mau ?? (object)DBNull.Value;
            cmd.Parameters["@NgayGiao"].Value = keHoach.NgayGiao ?? (object)DBNull.Value;
            cmd.Parameters["@GhiChu"].Value = keHoach.GhiChu ?? (object)DBNull.Value;
            cmd.Parameters["@TinhTrangKH"].Value = keHoach.TinhTrang;
            cmd.Parameters["@MucDoUuTienKH"].Value = keHoach.MucDoUuTienKH;
            cmd.Parameters["@TrangThaiSX"].Value = keHoach.TrangThaiSX;
        }

        /// <summary>
        /// Hiển thị kết quả
        /// </summary>
        private static void ShowInsertResult(int successCount, List<string> errors)
        {
            if (errors.Count > 0)
            {
                string errorMsg = $"{successCount} kế hoạch được thêm\n\n";
                errorMsg += $"Có ({errors.Count}) lỗi\n";
                errorMsg += string.Join("\n", errors.Take(10));

                if (errors.Count > 10)
                    errorMsg += $"\n... và {errors.Count - 10} lỗi khác";

                FrmWaiting.ShowGifAlert(errorMsg, "KẾT QUẢ");
            }
            else if (successCount > 0)
            {
                FrmWaiting.ShowGifAlert(
                    $"{successCount} kế hoạch được thêm",
                    "THÀNH CÔNG",EnumStore.Icon.Success
                );
            }
            else
            {
                FrmWaiting.ShowGifAlert("KHÔNG CÓ DỮ LIỆU NÀO ĐƯỢC THÊM", "THÔNG BÁO");
            }
        }

        public static DbResult InsertKeHoachSX(KeHoachSX dto)
        {
            try
            {
                using var conn = new SQLiteConnection(_connStr);
                conn.Open();

                using var tx = conn.BeginTransaction();

                using var cmd = conn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = @"
                    INSERT INTO KeHoachSX
                    (
                        DanhSachMaSP_ID, NgayNhan, Lot, SLHangDat, SLHangBan, Mau,
                        NgayGiao, GhiChu, TenKhachHang, TinhTrangKH, TrangThaiSX, MucDoUuTienKH
                    )
                    VALUES
                    (
                        @DanhSachMaSP_ID, @NgayNhan, @Lot, @SLHangDat, @SLHangBan, @Mau,
                        @NgayGiao, @GhiChu, @TenKhachHang, @TinhTrangKH, @TrangThaiSX, @MucDoUuTienKH
                    );
                    SELECT last_insert_rowid();
                    ";

                BindParams(cmd, dto, isUpdate: false);

                var newIdObj = cmd.ExecuteScalar();
                var newId = Convert.ToInt64(newIdObj);

                tx.Commit();

                return new DbResult
                {
                    Ok = true,
                    Id = newId,
                    Message = "thành công."
                };
            }
            catch (Exception ex)
            {
                return new DbResult
                {
                    Ok = false,
                    Id = null,
                    Message = CoreHelper.ShowErrorDatabase(ex)
                };
            }
        }

        public static DbResult UpdateKeHoachSX( KeHoachSX dto)
        {
            try
            {
                using var conn = new SQLiteConnection(_connStr);
                conn.Open();

                using var tx = conn.BeginTransaction();

                using var cmd = conn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = @"
                UPDATE KeHoachSX
                SET
                    DanhSachMaSP_ID = @DanhSachMaSP_ID,
                    NgayNhan        = @NgayNhan,
                    SLHangDat       = @SLHangDat,
                    SLHangBan       = @SLHangBan,
                    Mau             = @Mau,
                    NgayGiao        = @NgayGiao,
                    GhiChu          = @GhiChu,
                    TenKhachHang    = @TenKhachHang,
                    TinhTrangKH     = @TinhTrangKH,
                    TrangThaiSX     = @TrangThaiSX,
                    MucDoUuTienKH    = @MucDoUuTienKH
                WHERE Lot = @Lot;
                ";

                BindParams(cmd, dto, isUpdate: true);

                var affected = cmd.ExecuteNonQuery();
                if (affected == 0)
                {
                    tx.Rollback();
                    return new DbResult
                    {
                        Ok = false,
                        Id = dto.Id,
                        Message = "Không tìm thấy bản ghi để cập nhật (id không tồn tại)."
                    };
                }

                tx.Commit();

                return new DbResult
                {
                    Ok = true,
                    Id = dto.Id,
                    Message = "Thành công."
                };
            }
            catch (Exception ex)
            {
                return new DbResult
                {
                    Ok = false,
                    Id = dto.Id,
                   Message = CoreHelper.ShowErrorDatabase(ex)
                };
            }
        }

        //public static List<ResultFindKeHoachSX> SearchKeHoachSX(TimKiemKeHoachSX f)
        //{
        //    var result = new List<ResultFindKeHoachSX>();
        //    var (sql, pars) = BuildSqlSearchKeHoachSX(f);

        //    using (var conn = new SQLiteConnection(_connStr))
        //    {
        //        conn.Open();
        //        using (var cmd = new SQLiteCommand(sql, conn))
        //        {
        //            // add parameters
        //            foreach (var kv in pars)
        //                cmd.Parameters.AddWithValue(kv.Key, kv.Value ?? DBNull.Value);

        //            using (var rd = cmd.ExecuteReader())
        //            {
        //                while (rd.Read())
        //                {
        //                    ResultFindKeHoachSX item = new ResultFindKeHoachSX();

        //                    int TinhTrangCuaKH = rd["TinhTrangKH"] != DBNull.Value ? Convert.ToInt32(rd["TinhTrangKH"]) : 0;
        //                    int MucDoUuTienKH = rd["MucDoUuTienKH"] != DBNull.Value ? Convert.ToInt32(rd["MucDoUuTienKH"]) : 0;
        //                    int TrangThaiThucHienKH = rd["TrangThaiSX"] != DBNull.Value ? Convert.ToInt32(rd["TrangThaiSX"]) : 0;

        //                    item.Ten = rd["Ten"] != DBNull.Value ? rd["Ten"].ToString() : "";

        //                    item.NgayNhan = rd["NgayNhan"] != DBNull.Value ? rd["NgayNhan"].ToString() : "";
        //                    item.Lot = rd["Lot"] != DBNull.Value ? rd["Lot"].ToString() : "";

        //                    item.SLHangDat = rd["SLHangDat"] != DBNull.Value ? (double?)Convert.ToDouble(rd["SLHangDat"]) : null;
        //                    item.SLHangBan = rd["SLHangBan"] != DBNull.Value ? (double?)Convert.ToDouble(rd["SLHangBan"]) : null;
        //                    item.SLTong = item.SLHangDat + item.SLHangBan;

        //                    item.Mau = rd["Mau"] != DBNull.Value ? rd["Mau"].ToString() : "";
        //                    item.NgayGiao = rd["NgayGiao"] != DBNull.Value ? rd["NgayGiao"].ToString() : "";

        //                    item.GhiChu = rd["GhiChu"] != DBNull.Value ? rd["GhiChu"].ToString() : "";
        //                    item.TenKhachHang = rd["TenKhachHang"] != DBNull.Value ? rd["TenKhachHang"].ToString() : null;

        //                    item.KieuKH = EnumStore.TrangThaiBanHanhKH[TinhTrangCuaKH]; 

        //                    item.DoUuTien = EnumStore.MucDoUuTien[MucDoUuTienKH];

        //                    item.TrangThaiDon = EnumStore.TrangThaiThucHienTheoKH[TrangThaiThucHienKH];

        //                    result.Add(item);
        //                }
        //            }
        //        }
        //    }

        //    return result;
        //}

        public static DataTable SearchKeHoachSX_DataTable(TimKiemKeHoachSX f)
        {
            var (sql, pars) = BuildSqlSearchKeHoachSX(f);

            var dt = new DataTable();
            dt.Columns.Add("DoUuTien", typeof(string));
            dt.Columns.Add("TrangThaiDon", typeof(string));
            dt.Columns.Add("Ten", typeof(string));
            dt.Columns.Add("NgayNhan", typeof(string));
            dt.Columns.Add("Lot", typeof(string));
            dt.Columns.Add("SLHangDat", typeof(double));
            dt.Columns.Add("SLHangBan", typeof(double));
            dt.Columns.Add("SLTong", typeof(double));
            dt.Columns.Add("Mau", typeof(string));
            dt.Columns.Add("NgayGiao", typeof(string));
            dt.Columns.Add("GhiChu", typeof(string));
            dt.Columns.Add("TenKhachHang", typeof(string));
            dt.Columns.Add("KieuKH", typeof(string));

            using (var conn = new SQLiteConnection(_connStr))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    foreach (var kv in pars)
                        cmd.Parameters.AddWithValue(kv.Key, kv.Value ?? DBNull.Value);

                    using (var rd = cmd.ExecuteReader())
                    {
                        while (rd.Read())
                        {
                            int tinhTrangKH = rd["TinhTrangKH"] != DBNull.Value ? Convert.ToInt32(rd["TinhTrangKH"]) : 0;
                            int mucDoUuTienKH = rd["MucDoUuTienKH"] != DBNull.Value ? Convert.ToInt32(rd["MucDoUuTienKH"]) : 0;
                            int trangThaiSX = rd["TrangThaiSX"] != DBNull.Value ? Convert.ToInt32(rd["TrangThaiSX"]) : 0;

                            string ten = rd["Ten"] != DBNull.Value ? rd["Ten"].ToString() : "";
                            string ngayNhan = rd["NgayNhan"] != DBNull.Value ? rd["NgayNhan"].ToString() : "";
                            string lot = rd["Lot"] != DBNull.Value ? rd["Lot"].ToString() : "";

                            double? slDat = rd["SLHangDat"] != DBNull.Value ? (double?)Convert.ToDouble(rd["SLHangDat"]) : null;
                            double? slBan = rd["SLHangBan"] != DBNull.Value ? (double?)Convert.ToDouble(rd["SLHangBan"]) : null;
                            double? slTong = (slDat ?? 0) + (slBan ?? 0);

                            string mau = rd["Mau"] != DBNull.Value ? rd["Mau"].ToString() : "";
                            string ngayGiao = rd["NgayGiao"] != DBNull.Value ? rd["NgayGiao"].ToString() : "";

                            string ghiChu = rd["GhiChu"] != DBNull.Value ? rd["GhiChu"].ToString() : "";
                            string tenKH = rd["TenKhachHang"] != DBNull.Value ? rd["TenKhachHang"].ToString() : null;

                            string kieuKH = EnumStore.TrangThaiBanHanhKH.TryGetValue(tinhTrangKH, out var v1) ? v1 : "";

                            string doUuTien = EnumStore.MucDoUuTien.TryGetValue(mucDoUuTienKH, out var v2) ? v2 : "";

                            string trangThaiDon = EnumStore.TrangThaiThucHienTheoKH.TryGetValue(trangThaiSX, out var v3) ? v3 : "";


                            dt.Rows.Add(doUuTien, trangThaiDon, ten, ngayNhan, lot,
                                        slDat.HasValue ? (object)slDat.Value : DBNull.Value,
                                        slBan.HasValue ? (object)slBan.Value : DBNull.Value,
                                        (object)slTong,
                                        mau, ngayGiao, ghiChu, (object)tenKH ?? DBNull.Value,
                                        kieuKH);
                        }
                    }
                }
            }

            return dt;
        }

        private static (string sql, Dictionary<string, object> pars) BuildSqlSearchKeHoachSX(TimKiemKeHoachSX f)
        {
            var pars = new Dictionary<string, object>();
            var where = new List<string>();

            var sb = new StringBuilder();
            sb.Append(@"
                SELECT
                    k.id,
                    k.DanhSachMaSP_ID,
                    ds.Ma,
                    ds.Ten,
                    k.NgayNhan,
                    k.Lot,
                    k.SLHangDat,
                    k.SLHangBan,
                    k.Mau,
                    k.NgayGiao,
                    k.GhiChu,
                    k.TenKhachHang,
                    k.TinhTrangKH,
                    k.MucDoUuTienKH,
                    k.TrangThaiSX
                FROM KeHoachSX k
                LEFT JOIN DanhSachMaSP ds ON ds.id = k.DanhSachMaSP_ID
            ");

            // ===== int filters =====
            if (f.TrangThaiThucHienKH.HasValue)
            {
                where.Add("k.TrangThaiSX = @TrangThaiSX");
                pars["@TrangThaiSX"] = f.TrangThaiThucHienKH.Value;
            }

            if (f.TinhTrangCuaKH.HasValue)
            {
                where.Add("k.TinhTrangKH = @TinhTrangKH");
                pars["@TinhTrangKH"] = f.TinhTrangCuaKH.Value;
            }

            if (f.MucDoUuTienKH.HasValue)
            {
                where.Add("k.MucDoUuTienKH = @MucDoUuTienKH");
                pars["@MucDoUuTienKH"] = f.MucDoUuTienKH.Value;
            }

            // ===== text filters =====
            if (!string.IsNullOrWhiteSpace(f.Lot))
            {
                where.Add("k.Lot = @Lot");
                pars["@Lot"] = f.Lot.Trim();
            }

            if (!string.IsNullOrWhiteSpace(f.TenKhachHang))
            {
                where.Add("k.TenKhachHang LIKE @TenKhachHang");
                pars["@TenKhachHang"] = "%" + f.TenKhachHang.Trim() + "%";
            }

            if (!string.IsNullOrWhiteSpace(f.Mau))
            {
                where.Add("k.Mau LIKE @Mau");
                pars["@Mau"] = "%" + f.Mau.Trim() + "%";
            }

            if (!string.IsNullOrWhiteSpace(f.GhiChu))
            {
                where.Add("k.GhiChu LIKE @GhiChu");
                pars["@GhiChu"] = "%" + f.GhiChu.Trim() + "%";
            }

            // ===== date filters =====
            // DB lưu TEXT dạng "dd/MM/yyyy" => so sánh chuỗi đã format
            if (!string.IsNullOrWhiteSpace(f.NgayNhan))
            {
                where.Add("k.NgayNhan = @NgayNhan");
                pars["@NgayNhan"] = f.NgayNhan.Trim();
            }

            if (!string.IsNullOrWhiteSpace(f.NgayGiao))
            {
                where.Add("k.NgayGiao = @NgayGiao");
                pars["@NgayGiao"] = f.NgayGiao.Trim();
            }

            // ===== numeric filters =====
            if (f.SLTong.HasValue)
            {
                where.Add("(IFNULL(k.SLHangDat,0) + IFNULL(k.SLHangBan,0)) = @TongMin");
                pars["@TongMin"] = f.SLTong.Value;
            }

            if (f.SLHangBan.HasValue)
            {
                where.Add("IFNULL(k.SLHangBan,0) = @HangBanMin");
                pars["@HangBanMin"] = f.SLHangBan.Value;
            }

            if (f.SLHangDat.HasValue )
            {
                where.Add("IFNULL(k.SLHangDat,0) == @HangDatMin");
                pars["@HangDatMin"] = f.SLHangDat.Value;
            }

            if (!string.IsNullOrWhiteSpace(f.Ten))
            {
                where.Add("ds.Ten LIKE @Ten");
                pars["@Ten"] = "%" + f.Ten.Trim() + "%";
            }

            if (where.Count > 0)
            {
                sb.Append("WHERE ");
                sb.Append(string.Join(" AND ", where));
                sb.AppendLine();
            }

            sb.AppendLine("ORDER BY k.MucDoUuTienKH ASC;");
            return (sb.ToString(), pars);
        }

        // ========= PRIVATE HELPERS =========

        private static void BindParams(SQLiteCommand cmd, KeHoachSX dto, bool isUpdate)
        {
            cmd.Parameters.Clear();

            
                

            AddParam(cmd, "@DanhSachMaSP_ID", DbType.Int64, dto.DanhSachMaSP_ID);
            AddParam(cmd, "@NgayNhan", DbType.String, dto.NgayNhan);
            AddParam(cmd, "@Lot", DbType.Int64, dto.Lot);
            // Lot & NgayGiao: nếu null/"" => DB sẽ báo NOT NULL constraint failed (đúng ý bạn)
            AddParam(cmd, "@SLHangDat", DbType.Double, dto.SLHangDat);
            AddParam(cmd, "@SLHangBan", DbType.Double, dto.SLHangBan);
            AddParam(cmd, "@Mau", DbType.String, dto.Mau);

            AddParam(cmd, "@NgayGiao", DbType.String, dto.NgayGiao);

            AddParam(cmd, "@GhiChu", DbType.String, dto.GhiChu);
            AddParam(cmd, "@TenKhachHang", DbType.String, dto.TenKhachHang);

            AddParam(cmd, "@TinhTrangKH", DbType.Int32, dto.TinhTrang);
            AddParam(cmd, "@TrangThaiSX", DbType.Int32, dto.TrangThaiSX);
            AddParam(cmd, "@MucDoUuTienKH", DbType.Int32, dto.MucDoUuTienKH);
        }

        private static void AddParam(SQLiteCommand cmd, string name, DbType type, object? value)
        {
            var p = cmd.CreateParameter();
            p.ParameterName = name;
            p.DbType = type;
            p.Value = value ?? DBNull.Value;
            cmd.Parameters.Add(p);
        }


        #endregion

        #region Lấy dữ liệu
        // Lấy dữ liệu theo 1 điều kiện
        public static DataTable GetData(string query , string key = null,  string para = null)
        {
            using (SQLiteConnection conn = new SQLiteConnection(_connStr))
            {
                conn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(query, conn))
                {
                    if (!string.IsNullOrWhiteSpace(para) && key != null)
                    {
                        cmd.Parameters.AddWithValue("@" + para, key);
                    }

                    using (SQLiteDataAdapter adapter = new SQLiteDataAdapter(cmd))
                    {
                        DataTable resultTable = new DataTable();
                        adapter.Fill(resultTable);
                        return resultTable;
                    }
                }
            }
        }

        public static DataTable GetDataByCongDoan(DateTime selectedDate, CongDoan cd,int ca, string nguoiKiemTra)
        {
            string key = selectedDate.ToString("yyyy-MM-dd");

            string sqlSelect = CoreHelper.TaoSqL_LayThongTinBaoCaoChung();

            string sqlLayChiTietCD = CoreHelper.TaoSQL_LayChiTiet_1CD(cd.Id);

            string sqlTenNVL = CoreHelper.TaoSQL_LayDuLieuNVL(cd.Columns);

            string sqlJoin = CoreHelper.TaoSQL_TaoKetNoiCacBang();

            string sqlDk1 = " WHERE date(tclv.Ngay) = date(@para) ";

            string sqlDk2 = " AND ttp.CongDoan = " + cd.Id;

            string sqlDk3 = " AND tclv.Ca = " + ca;

            if (!string.IsNullOrWhiteSpace(nguoiKiemTra))
            {
                sqlDk3 += " AND tclv.NguoiLam = '" + nguoiKiemTra + "' ";
            }

            // 6) ORDER BY
            string sqlOrder = " ORDER BY tclv.Ngay DESC, ttp.id DESC;";

            // 7) Kết hợp hoàn chỉnh
            string query = sqlSelect + " ," + sqlLayChiTietCD + " ," + sqlTenNVL + sqlJoin + sqlDk1 + sqlDk2 + sqlDk3 + sqlOrder;

            return GetData(query, key, "para");
        }

        // Lấy dữ liệu theo ngày tháng
        public static DataTable GetDataByMonth(DateTime selectedDate, CongDoan cd)
        {
            string key = selectedDate.ToString("yyyy-MM-dd");
            
            string sqlSelect = CoreHelper.TaoSqL_LayThongTinBaoCaoChung();

            string sqlLayChiTietCD = CoreHelper.TaoSQL_LayChiTiet_1CD(cd.Id);
            
            string sqlTenNVL = CoreHelper.TaoSQL_LayDuLieuNVL(cd.Columns);

            string sqlJoin = CoreHelper.TaoSQL_TaoKetNoiCacBang();

            string sqlDk1 = " WHERE strftime('%Y-%m', tclv.Ngay) = strftime('%Y-%m', @para) ";

            string sqlDk2 = " AND ttp.CongDoan = " + cd.Id;

            // 6) ORDER BY
            string sqlOrder = " ORDER BY tclv.Ngay DESC, ttp.id DESC;";

            // 7) Kết hợp hoàn chỉnh
            string query = sqlSelect + " ," + sqlLayChiTietCD + " ," + sqlTenNVL + sqlJoin + sqlDk1 + sqlDk2 + sqlOrder;

            return GetData(query, key, "para");
        }
        // Lấy dữ liệu theo ID
        public static DataTable GetDataByID(string key, CongDoan cd, int kieuDL)
        {
            // Tạo select
            string sqlSelect = CoreHelper.TaoSqL_LayThongTinBaoCaoChung_Edit();

            // Lấy thông tin chi tiết công đoạn
            string sqlLayChiTietCD = CoreHelper.TaoSQL_LayChiTiet_1CD(cd.Id);

            // Lấy dữ liệu nvl theo công đoạn
            string sqlTenNVL = CoreHelper.TaoSQL_LayDuLieuNVL(cd.Columns);

            // Tạo câu nối các bảng
            string sqlJoin = CoreHelper.TaoSQL_TaoKetNoiCacBang();

            sqlJoin += "LEFT JOIN TTThanhPham ttp_bin ON ttp_bin.MaBin = nvl.BinNVL ";

            // Tạo điều kiện lọc theo ID
            string sqlDk1 = " WHERE ttp.id = @id";

            string sqlDk2 = " AND ttp.CongDoan = " + cd.Id;

            string sqlDk3 = kieuDL == 0 ? " AND ( (ds.DonVi = 'M'  AND IFNULL(ttp.ChieuDaiSau, 0) <> 0)  OR (ds.DonVi = 'KG' AND IFNULL(ttp.KhoiLuongSau, 0) <> 0)) " : "";

            // Kết hợp câu truy vấn
            string query = sqlSelect + " ,"+ sqlLayChiTietCD + " ," + sqlTenNVL + sqlJoin + sqlDk1 + sqlDk2 + sqlDk3;

            return GetData(query, key, "id");
        }
        // Lấy báo cáo sản xuất
        public static DataTable GetDataBaoCaoSX(DateTime ngayBatDau, DateTime ngayKetThuc, List<CongDoan> selectedCongDoans)
        {
            // Tạo phần SELECT chung
            string sqlSelect = CoreHelper.TaoSqL_LayThongTinBaoCaoChung();

            // Lấy dữ liệu NVL theo danh sách công đoạn
            string sqlTenNVL = CoreHelper.TaoSQL_LayDuLieuNVL(selectedCongDoans.Select(cd => cd.Columns).ToArray());

            // Lấy chi tiết công đoạn
            var(sqlLayChiTietCD, loaiCD) = CoreHelper.TaoSQL_LayChiTiet_NhieuCD(selectedCongDoans) ;


            // Câu nối các bảng
            string sqlJoin = CoreHelper.TaoSQL_TaoKetNoiCacBang();

            // Format ngày sang dạng SQLite hiểu được
            string ngayBD = ngayBatDau.Date.AddHours(5).AddMinutes(59).ToString("yyyy-MM-dd HH:mm:ss");

            string ngayKT = ngayKetThuc.Date.AddDays(1).AddHours(6).ToString("yyyy-MM-dd HH:mm:ss");

            // Điều kiện WHERE – chèn trực tiếp giá trị ngày
            string sqlDkNgay = $" WHERE date(tclv.Ngay) >= date('{ngayBD}') AND date(tclv.Ngay) <= date('{ngayKT}')";


            // Sắp xếp
            string sqlOrder = " ORDER BY tclv.Ngay DESC, ttp.id DESC;";

            // Ghép chuỗi hoàn chỉnh
            string query = sqlSelect + " ," + sqlLayChiTietCD + " ," + sqlTenNVL + sqlJoin + sqlDkNgay + loaiCD + sqlOrder;

            return GetData(query);
        }

        public static DataTable GetTonKhoCD( List<CongDoan> selectedCongDoans)
        {
            // Tạo phần SELECT chung
            string sqlSelect = CoreHelper.TaoSqL_LayThongTinBaoCaoChung();

            // Lấy dữ liệu NVL theo danh sách công đoạn
            string sqlTenNVL = CoreHelper.TaoSQL_LayDuLieuNVL(selectedCongDoans.Select(cd => cd.Columns).ToArray());

            // Lấy chi tiết công đoạn
            var (sqlLayChiTietCD, loaiCD) = CoreHelper.TaoSQL_LayChiTiet_NhieuCD(selectedCongDoans);

            // Câu nối các bảng
            string sqlJoin = CoreHelper.TaoSQL_TaoKetNoiCacBang();

            loaiCD = loaiCD.Replace("AND", "WHERE")
                    + @"
                        AND (
                        (ds.DonVi = 'KG' AND ttp.KhoiLuongSau > 0)
                        OR
                        (ds.DonVi = 'M' AND ttp.ChieuDaiSau > 0)
                      )";


            // Sắp xếp
            string sqlOrder = " ORDER BY tclv.Ngay DESC, ttp.id DESC;";

            // Ghép chuỗi hoàn chỉnh
            string query = sqlSelect + " ," + sqlLayChiTietCD + " ," + sqlTenNVL + sqlJoin +  loaiCD + sqlOrder;

            return GetData(query);
        }

        public static DataTable GetThongTinNVLTheoMaBin(string mabin)
        {
            string query = @"
                SELECT                     
                    TP.id AS STT,
                    SP_NVL.Ten AS TenNVL,
                    NVL.BinNVL as MaBin,
                    SP_NVL.Ma AS MaNVL,
                    NVL.KlBatDau,
                    NVL.CdBatDau,
                    NVL.KlConLai,
                    NVL.CdConLai,
                    NVL.DuongKinhSoiDong,
                    NVL.SoSoi,
                    NVL.KetCauLoi,
                    NVL.DuongKinhSoiMach,
                    NVL.BanRongBang,
                    NVL.DoDayBang
                FROM TTThanhPham AS TP
                LEFT JOIN TTNVL AS NVL ON TP.id = NVL.TTThanhPham_ID
                LEFT JOIN DanhSachMaSP AS SP_NVL ON NVL.DanhSachMaSP_ID = SP_NVL.id
                WHERE TP.MaBin = @mabin;
            ";

            return GetData(query, mabin, "mabin");
        }

        public static List<PrinterModel> GetPrinterDataByListBin(List<string> listBin)
        {
            List<PrinterModel> result = new List<PrinterModel>();


            // 1. Tạo danh sách parameter @bin0, @bin1,...
            var paramNames = listBin.Select((bin, index) => "@bin" + index).ToList();
            string inClause = string.Join(",", paramNames);

            // 2. SQL truy vấn
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
                JOIN ThongTinCaLamViec t ON t.TTThanhPham_id = tp.id
                JOIN DanhSachMaSP d ON tp.DanhSachSP_ID = d.id
                WHERE tp.MaBin IN ({inClause});
                ";

            using (SQLiteConnection conn = new SQLiteConnection(_connStr))
            {
                conn.Open();

                using (SQLiteCommand cmd = new SQLiteCommand(query, conn))
                {
                    // 3. Gán giá trị cho từng parameter
                    for (int i = 0; i < listBin.Count; i++)
                    {
                        cmd.Parameters.AddWithValue("@bin" + i, listBin[i]);
                    }

                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {                        


                            var model = new PrinterModel
                            {
                                NgaySX = DateTime.Parse(reader["NgaySX"].ToString()).ToString("dd/MM/yyyy"),
                                CaSX = reader["CaSX"].ToString(),
                                KhoiLuong = reader["KhoiLuong"].ToString(),
                                ChieuDai = reader["ChieuDai"].ToString(),
                                TenSP = reader["TenSP"].ToString(),
                                MaBin = reader["MaBin"].ToString(),
                                MaSP = reader["MaSP"].ToString(), 
                                DanhGia = "",
                                QC = reader["QC"].ToString(),
                                TenCN = reader["TenCN"].ToString(),
                                GhiChu = reader["GhiChu"].ToString()
                            };

                            result.Add(model);
                        }
                    }
                }
            }

            return result;
        }

        public static ConfigDB GetConfig()
        {

            using (var conn = new SQLiteConnection(_connStr))
            {
                conn.Open();

                string sql = "SELECT Active,Author, Message FROM ConfigDB ORDER BY ID DESC  LIMIT 1";

                using (var cmd = new SQLiteCommand(sql, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new ConfigDB
                        {
                            Active = reader.IsDBNull(0) ? false : reader.GetBoolean(0),
                            Author = reader.IsDBNull(1) ? null : reader.GetString(1),
                            Message = reader.IsDBNull(2) ? null : reader.GetString(2)
                        };
                    }
                }
            }

            return null; // Trường hợp bảng rỗng
        }
        #endregion

        #region Update dữ liệu

        public static int UpdateTrangThaiSX(HashSet<(string Lot, int TrangThai, string Ten)> items,
            string userUpdate)
        {
            int result = 0;
            using var conn = new SQLiteConnection(_connStr);
            SQLiteTransaction tx = null;

            try
            {
                conn.Open();                 
                tx = conn.BeginTransaction(); 
                result = UpdateTrangThaiSX_ByLots(conn,tx ,items, userUpdate);
                tx.Commit();
            }
            catch { }
            return result;
        }

        public static int UpdateTrangThaiSX_ByLots( SQLiteConnection conn, SQLiteTransaction tx, HashSet<(string Lot, int TrangThai, string Ten)> items, string userUpdate)
        {
            var list = items
                .Where(x => !string.IsNullOrWhiteSpace(x.Lot))
                .Select(x => (Lot: x.Lot.Trim(), TrangThai: x.TrangThai, Ten: (x.Ten ?? "").Trim()))
                .Where(x => x.Ten.Length > 0)
                .ToList();

            if (list.Count == 0) return 0;

            const string sql = @"
            UPDATE KeHoachSX
            SET UpdateTrangThaiSX = @userUpdate,
                TrangThaiSX = @tt
            WHERE Lot = @lot
              AND EXISTS (
                  SELECT 1
                  FROM DanhSachMaSP sp
                  WHERE sp.id = KeHoachSX.DanhSachMaSP_ID
                    AND sp.Ten = @ten
              );";

            using var cmd = new SQLiteCommand(sql, conn, tx);

            var pUser = cmd.CreateParameter();
            pUser.ParameterName = "@userUpdate";
            pUser.DbType = DbType.String;
            cmd.Parameters.Add(pUser);

            var pTt = cmd.CreateParameter();
            pTt.ParameterName = "@tt";
            pTt.DbType = DbType.Int32;
            cmd.Parameters.Add(pTt);

            var pLot = cmd.CreateParameter();
            pLot.ParameterName = "@lot";
            pLot.DbType = DbType.String;
            cmd.Parameters.Add(pLot);

            var pTen = cmd.CreateParameter();
            pTen.ParameterName = "@ten";
            pTen.DbType = DbType.String;
            cmd.Parameters.Add(pTen);

            int affectedTotal = 0;

            foreach (var (lot, tt, ten) in list)
            {
                pUser.Value = userUpdate ?? string.Empty;
                pLot.Value = lot;
                pTt.Value = tt;
                pTen.Value = ten;

                affectedTotal += cmd.ExecuteNonQuery();
            }

            return affectedTotal;
        }


        /// <summary>
        /// Helper nội bộ: add params vào SQLiteCommand.
        /// Hỗ trợ truyền key có/không có '@'.
        /// </summary>
        private static void AddParams(SQLiteCommand cmd, Dictionary<string, object?>? parameters)
        {
            cmd.Parameters.Clear();

            if (parameters == null || parameters.Count == 0) return;

            foreach (var kv in parameters)
            {
                var name = (kv.Key ?? "").Trim();
                if (name.Length == 0) continue;
                if (!name.StartsWith("@")) name = "@" + name;

                var p = cmd.CreateParameter();
                p.ParameterName = name;

                // auto map null -> DBNull
                p.Value = kv.Value ?? DBNull.Value;

                cmd.Parameters.Add(p);
            }
        }
        public static bool UpdateNguoiKiemTra(List<int> listStt, string nguoiKT)
        {
            if (listStt == null || listStt.Count == 0)
                return false;

            int rows = 0;

            try
            {
                using (var con = new SQLiteConnection(_connStr))
                {
                    con.Open();

                    using (var cmd = con.CreateCommand())
                    {
                        // tạo param hàng loạt @p0, @p1, ...
                        List<string> paramNames = new List<string>();
                        int i = 0;

                        foreach (int stt in listStt)
                        {
                            string p = "@p" + i;
                            paramNames.Add(p);
                            cmd.Parameters.AddWithValue(p, stt);
                            i++;
                        }

                        // param tên tổ trưởng
                        cmd.Parameters.AddWithValue("@nguoiKT", nguoiKT);

                        cmd.CommandText = $@"
                            UPDATE ThongTinCaLamViec
                            SET ToTruong = @nguoiKT
                            WHERE TTThanhPham_id IN ({string.Join(",", paramNames)});
                        ";

                        rows = cmd.ExecuteNonQuery();
                    }
                }

                return rows > 0;
            }
            catch (Exception ex)
            {
               return false;
            }
        }

        private static void UpdateKL_CD_TTThanhPham(SQLiteConnection conn, SQLiteTransaction tx, List<TTNVL> nvlList, long thongTinSpId)
        {
            const string sql = @"
                UPDATE TTThanhPham
                SET KhoiLuongSau = @KhoiLuongSau,
                    ChieuDaiSau = @ChieuDaiSau,
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
                cmd.Parameters["@KhoiLuongSau"].Value = nvl.KlConLai;
                cmd.Parameters["@ChieuDaiSau"].Value = nvl.CdConLai;
                cmd.Parameters["@LastEdit_ID"].Value = thongTinSpId;
                cmd.Parameters["@QC"].Value = nvl.QC;
                cmd.Parameters["@MaBin"].Value = nvl.BinNVL;

                cmd.ExecuteNonQuery(); 
            }
        }

        // Main update function
        public static bool UpdateDataSanPham(int tpId, ThongTinCaLamViec caLam, TTThanhPham tp, List<TTNVL> nvl, List<object> chiTietCD, out string errorMsg)
        {
            using var conn = new SQLiteConnection(_connStr);
            conn.Open();
            using var tx = conn.BeginTransaction();

            CaiDatCDBoc caiDat = (CaiDatCDBoc)chiTietCD[1];

            errorMsg = string.Empty;

            try
            {
                // 1) ThongTinCaLamViec
                UpdateThongTinCaLamViec(conn, tx, caLam, tpId);

                // 2) TTThanhPham
                UpdateTTThanhPham(conn, tx, tp, tpId,nvl);

                // restore lại
                RestoreFromNVL(conn, tx,tpId);

                // 3.1) Update Khối lượng sau, Chiều dài sau và thêm ID được update ở TTThanhPham 
                UpdateKhoiLuongConLai_TTThanhPham(conn, tx, nvl, tpId);

                // 3.2) TTNVL -> Xoá nvl cũ và Tạo mới
                Del_InsertTTNVL(conn, tx, tpId, nvl);

                // 3.3) Dùng cho db v1
                DatabasehelperVer01.UpdateTonKho_NVL_Lan2(nvl,tp.MaBin);

                // 4) CaiDatCDBoc - Nếu có
                if (caiDat != null) UpdateCaiDatCDBoc(conn, tx, tpId, caiDat);

                // 5) Thêm chi tiết các công đoạn
                switch (chiTietCD[0])
                {
                    case CD_BenRuot ben:
                        UpdateCDBenRuot(conn, tx, tpId, ben);
                        break;

                    case CD_KeoRut keo:
                        UpdateCDKeoRut(conn, tx, tpId, keo);
                        //updateVersion1(tpId, tp, caLam);
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

                        // Cập nhật trạng thái của kế hoạch sx
                        var parts = CoreHelper.CatMaBin(tp.MaBin);
                        string lot = parts.Length == 5 ? parts[1] : parts[0];
                        string ttUpdate = $"{caLam.NguoiLam}_Ca {caLam.Ca}";

                        int trangThai = 2;  
                        var items = new HashSet<(string Lot, int TrangThai, string Ten)>{
                            (Lot: lot, TrangThai: trangThai, Ten: tp.TenTP)
                        };

                        UpdateTrangThaiSX_ByLots(conn, tx, items, ttUpdate);


                        break;

                    default:
                        throw new ArgumentException("Lỗi bất thường.");
                }

                tx.Commit();
                return true;
            }
            catch (Exception ex)
            {
                tx.Rollback();

                errorMsg = CoreHelper.ShowErrorDatabase(ex, tp.MaBin);

                return false;
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

        private static void UpdateKhoiLuongConLai_TTThanhPham( SQLiteConnection conn, SQLiteTransaction tx, List<TTNVL> nvlList,long thongTinSpId)
        {
            if (conn == null) throw new ArgumentNullException(nameof(conn));
            if (tx == null) throw new ArgumentNullException(nameof(tx));
            if (nvlList == null || nvlList.Count == 0) return;

            using var cmd = new SQLiteCommand(@"
            UPDATE TTThanhPham
               SET  KhoiLuongSau = @kl,
                    QC = @QC,
                    ChieuDaiSau  = @cd,
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

                pKL.Value = nvl.KlConLai;
                QC.Value = nvl.QC;
                pCD.Value = nvl.CdConLai;
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

            using var cmd = new SQLiteCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@DayVoTB", m.DayVoTB);
            cmd.Parameters.AddWithValue("@InAn", m.InAn ?? string.Empty);
            cmd.Parameters.AddWithValue("@TTThanhPham_ID", id);
            cmd.ExecuteNonQuery();
        }

        private static void UpdateCDBocMach(SQLiteConnection conn, SQLiteTransaction tx, long id, CD_BocMach m)
        {
            const string sql = @"
            UPDATE CD_BocMach
            SET 
                NgoaiQuan = @NgoaiQuan,
                LanDanhThung = @LanDanhThung,
                SoMet = @SoMet
            WHERE CaiDatCDBoc_ID IN (
                SELECT id FROM CaiDatCDBoc WHERE TTThanhPham_ID = @TTThanhPham_ID
            );";

            using var cmd = new SQLiteCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@NgoaiQuan", m.NgoaiQuan ?? "1");
            cmd.Parameters.AddWithValue("@LanDanhThung", m.LanDanhThung);
            cmd.Parameters.AddWithValue("@SoMet", m.SoMet);
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
            const string sql = @"
            UPDATE CD_GhepLoiQB
            SET BuocXoan = @BuocXoan,
                ChieuXoan = @ChieuXoan,
                GoiCachMep = @GoiCachMep,
                DKBTP = @DKBTP
            WHERE TTThanhPham_ID = @TTThanhPham_ID;";

            using var cmd = new SQLiteCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@BuocXoan", m.BuocXoan);
            cmd.Parameters.AddWithValue("@ChieuXoan", m.ChieuXoan ?? "Z");
            cmd.Parameters.AddWithValue("@GoiCachMep", m.GoiCachMep);
            cmd.Parameters.AddWithValue("@DKBTP", m.DKBTP);
            cmd.Parameters.AddWithValue("@TTThanhPham_ID", thongTinSpId);
            cmd.ExecuteNonQuery();
        }

        public static string UpdateDanhSachMaSP(DanhSachMaSP sp, int key)
        {
            string query = @"
                UPDATE DanhSachMaSP
                SET 
                    Ten = @Ten,
                    Ma = @Ma,
                    DonVi = @DonVi,
                    KieuSP = @KieuSP,
                    DateInsert = @DateInsert
                WHERE id = @Id;
            ";

            try
            {
                using (var conn = new SQLiteConnection(_connStr))
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Ten", sp.Ten);
                        cmd.Parameters.AddWithValue("@Ma", sp.Ma);
                        cmd.Parameters.AddWithValue("@DonVi", sp.DonVi);
                        cmd.Parameters.AddWithValue("@KieuSP", sp.KieuSP);
                        cmd.Parameters.AddWithValue("@DateInsert", sp.DateInsert);
                        cmd.Parameters.AddWithValue("@Id", key);

                        int rowsAffected = cmd.ExecuteNonQuery();

                        // Nếu cập nhật thành công → trả về chuỗi rỗng
                        if (rowsAffected > 0)
                            return "";
                        else
                            return $"Không tìm thấy bản ghi với Tên = {sp.Ten}";
                    }
                }
            }
            catch (Exception ex)
            {
                // Trả về lỗi từ helper
                return CoreHelper.ShowErrorDatabase(ex, sp.Ten);
            }
        }

        public static string UpdateKLConLai_BanTran(BanTran bt)
        {
            string query = @"
                UPDATE TTThanhPham
                SET 
                    KhoiLuongSau = @KhoiLuongSau,
                    KLBanTran = COALESCE(KLBanTran, 0) + @KhoiLuongBanTran
                WHERE MaBin = @MaBin;
            ";

            try
            {
                using (var conn = new SQLiteConnection(_connStr))
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@KhoiLuongSau", bt.KhoiLuongSau);
                        cmd.Parameters.AddWithValue("@KhoiLuongBanTran", bt.KhoiLuongBanTran);
                        cmd.Parameters.AddWithValue("@MaBin", bt.MaBin);

                        int rowsAffected = cmd.ExecuteNonQuery();

                        if (rowsAffected > 0)
                            return ""; // cập nhật thành công → trả về chuỗi rỗng
                        else
                            return $"Không tìm thấy bản ghi với MaBin = {bt.MaBin}";
                    }
                }
            }
            catch (Exception ex)
            {
                // Gọi helper hiển thị lỗi giống phong cách bạn đang dùng
                return CoreHelper.ShowErrorDatabase(ex, bt.MaBin);
            }
        }
        #endregion

        #region Insert dữ liệu các công đoạn

        public static bool SaveTachBin(ThongTinCaLamViec caLam, List<TTThanhPham> list_tp, List<TTNVL> nvl, out string errorMsg)
        {
            errorMsg = string.Empty;
            using var conn = new SQLiteConnection(_connStr);
            SQLiteTransaction tx = null;

            try
            {
                conn.Open();                  
                tx = conn.BeginTransaction();

                foreach (TTThanhPham tp in list_tp)
                {                

                    // 1) Tạo mới thông tin thành phẩm
                    long tpId = InsertTTThanhPham(conn, tx, tp, nvl);

                    // 2) ThongTinCaLamViec
                    InsertThongTinCaLamViec(conn, tx, caLam, tpId);

                    // 3) TTNVL -> Tạo mới
                    InsertTTNVL(conn, tx, tpId, nvl);

                    // 3.1) Update Khối lượng sau, Chiều dài sau và thêm ID được update ở TTThanhPham 
                    List<TTNVL> t = new List<TTNVL>
                    {
                        new TTNVL{
                            BinNVL = nvl[0].BinNVL,
                            KlConLai = 0,
                            CdConLai = 0,
                            QC = tp.QC
                        }
                    };
                    
                    UpdateKL_CD_TTThanhPham(conn, tx, t, tpId);
                }
                tx.Commit();



                return true;
            }
            catch (Exception ex)
            {
                try { tx?.Rollback(); } catch { /* nuốt lỗi rollback để không che mất lỗi chính */ }

                errorMsg = CoreHelper.ShowErrorDatabase(ex);
                return false;
            }

        }

        public static bool SaveDataSanPham( ThongTinCaLamViec caLam, TTThanhPham tp, List<TTNVL> nvl, List<object> chiTietCD, out string errorMsg)
        {
            errorMsg = string.Empty;

            // Guard clauses
            if (tp == null)
            {
                errorMsg = "Thiếu thông tin thành phẩm.";
                return false;
            }
            if (chiTietCD == null || chiTietCD.Count == 0 || chiTietCD[0] == null)
            {
                errorMsg = "Thiếu chi tiết công đoạn.";
                return false;
            }

            long idCaiDatCDBoc = 0;
            using var conn = new SQLiteConnection(_connStr);
            SQLiteTransaction tx = null;

            try
            {
                conn.Open();                  // ✅ MỞ TRƯỚC
                tx = conn.BeginTransaction(); // ✅ RỒI MỚI BẮT ĐẦU TRANSACTION

                // 1) TTThanhPham -> Tạo mới thành phẩm
                long tpId = InsertTTThanhPham(conn, tx, tp, nvl);

                // 2) ThongTinCaLamViec -> tạo mới thông tin ca làm việc
                InsertThongTinCaLamViec(conn, tx, caLam, tpId);

                // 3) TTNVL -> Tạo mới nguyên vật liệu
                InsertTTNVL(conn, tx, tpId, nvl);

                // 3.2) Tạo backup
                //InsertBackup(conn, tx, nvl, tpId);

                // 3.1) Update Khối lượng sau, Chiều dài sau và thêm ID được update ở TTThanhPham 
                UpdateKL_CD_TTThanhPham(conn, tx, nvl, tpId);

                // 3.2) Dùng cho db v1
                DatabasehelperVer01.UpdateTonKho_NVL_Lan1(nvl);

                // 4) CaiDatCDBoc (chỉ áp dụng cho nhóm bóc)
                var congDoan = chiTietCD[0];

                if (congDoan is CD_BocLot || congDoan is CD_BocVo || congDoan is CD_BocMach)
                {
                    // chỉ lấy caiDat nếu có phần tử thứ 2 và đúng kiểu
                    CaiDatCDBoc caiDat = (chiTietCD.Count > 1) ? chiTietCD[1] as CaiDatCDBoc : null;
                    if (caiDat != null)
                        idCaiDatCDBoc = InsertCaiDatCDBoc(conn, tx, tpId, caiDat);
                }

                // 5) Thêm chi tiết các công đoạn
                switch (congDoan)
                {
                    case CD_KeoRut keo:
                        InsertCDKeoRut(conn, tx, tpId, keo);

                        // Tạo mới dữ liệu cho db v1
                        //insertVersion1(tp,caLam, nvl);
                        break;

                    case CD_BenRuot ben:
                        InsertCDBenRuot(conn, tx, tpId, ben);
                        // Tạo mới dữ liệu cho db v1
                        //insertVersion1(tp, caLam, nvl);
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

                        // Cập nhật trạng thái của kế hoạch sx
                        var parts = CoreHelper.CatMaBin(tp.MaBin);
                        string lot = parts.Length == 5 ? parts[1] : parts[0];
                        string ttUpdate = $"{caLam.NguoiLam}_Ca {caLam.Ca}";

                        int trangThai = 2;  // EnumStore.TrangThaiThucHienTheoKH[2] = "Đã xong"
                        var items = new HashSet<(string Lot, int TrangThai, string Ten)>{
                            (Lot: lot, TrangThai: trangThai, Ten: tp.TenTP)
                        };

                        UpdateTrangThaiSX_ByLots(conn, tx, items, ttUpdate);


                        break;

                    default:
                        throw new ArgumentException("Lỗi bất thường: Công đoạn không hợp lệ.");
                }

                tx.Commit(); // ✅ nhớ commit
                return true;
            }
            catch (Exception ex)
            {
                try { tx?.Rollback(); } catch { /* nuốt lỗi rollback để không che mất lỗi chính */ }

                errorMsg = CoreHelper.ShowErrorDatabase(ex, tp.MaBin);
                return false;
            }
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
        
        private static long InsertThongTinCaLamViec(SQLiteConnection conn, SQLiteTransaction tx, ThongTinCaLamViec m, long id)
        {
            const string sql = @"
            INSERT INTO ThongTinCaLamViec (Ngay,TTThanhPham_id, May, Ca, NguoiLam, ToTruong, QuanDoc)
            VALUES (@Ngay, @TTThanhPham_id, @May, @Ca, @NguoiLam, @ToTruong, @QuanDoc);
            SELECT last_insert_rowid();";
            using var cmd = new SQLiteCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@Ngay", m.Ngay);
            cmd.Parameters.AddWithValue("@TTThanhPham_id", id);
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
                (TTThanhPham_ID, BinNVL,QC, DanhSachMaSP_ID, KlBatDau, CdBatDau, KlConLai, CdConLai, DuongKinhSoiDong, SoSoi, KetCauLoi, DuongKinhSoiMach, BanRongBang, DoDayBang)
            VALUES
                (@TTThanhPham_ID, @BinNVL,@QC,@DanhSachMaSP_ID, @KlBatDau, @CdBatDau, @KlConLai, @CdConLai, @DuongKinhSoiDong, @SoSoi, @KetCauLoi, @DuongKinhSoiMach, @BanRongBang, @DoDayBang);";

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
            var pBanRongBang = cmd.Parameters.Add("@BanRongBang", DbType.Double);
            var pDoDayBang = cmd.Parameters.Add("@DoDayBang", DbType.Double);

            foreach (TTNVL m in items)
            {
                pThongTinSP_ID.Value = thongTinSpId;
                pBinNVL.Value = m.BinNVL ?? string.Empty;
                DanhSachMaSP_ID.Value = m.DanhSachMaSP_ID;
                KlBatDau.Value = m.KlBatDau;
                CdBatDau.Value = m.CdBatDau;
                KlConLai.Value = m.KlConLai;
                CdConLai.Value = m.CdConLai;
                QC.Value = m.QC;
                pDuongKinhSoiDong.Value = m.DuongKinhSoiDong;
                pSoSoi.Value = m.SoSoi;
                pKetCauLoi.Value = m.KetCauLoi;
                pDuongKinhSoiMach.Value = m.DuongKinhSoiMach;
                pBanRongBang.Value = m.BanRongBang;
                pDoDayBang.Value = m.DoDayBang;

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

        private static void InsertCDBocVo(SQLiteConnection conn, SQLiteTransaction tx, long id, CD_BocVo m)
        {
            const string sql = @"
            INSERT INTO CD_BocVo (CaiDatCDBoc_ID, DayVoTB, InAn)
            VALUES (@CaiDatCDBoc_ID, @DayVoTB, @InAn);";

            using var cmd = new SQLiteCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@CaiDatCDBoc_ID", id);
            cmd.Parameters.AddWithValue("@DayVoTB", m.DayVoTB);
            cmd.Parameters.AddWithValue("@InAn", m.InAn ?? string.Empty);
            cmd.ExecuteNonQuery();
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
            INSERT INTO CD_BocMach (CaiDatCDBoc_ID, NgoaiQuan, LanDanhThung, SoMet)
            VALUES (@CaiDatCDBoc_ID, @NgoaiQuan, @LanDanhThung, @SoMet);";

            using var cmd = new SQLiteCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@CaiDatCDBoc_ID", id);
            cmd.Parameters.AddWithValue("@NgoaiQuan", m.NgoaiQuan ?? "1"); // default theo schema
            cmd.Parameters.AddWithValue("@LanDanhThung", m.LanDanhThung);
            cmd.Parameters.AddWithValue("@SoMet", m.SoMet);
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
            (TTThanhPham_ID, BuocXoan, ChieuXoan, GoiCachMep, DKBTP)
            VALUES
            (@TTThanhPham_ID, @BuocXoan, @ChieuXoan, @GoiCachMep, @DKBTP);";

            using var cmd = new SQLiteCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@TTThanhPham_ID", thongTinSpId);
            cmd.Parameters.AddWithValue("@BuocXoan", m.BuocXoan);
            cmd.Parameters.AddWithValue("@ChieuXoan", m.ChieuXoan);
            cmd.Parameters.AddWithValue("@GoiCachMep", m.GoiCachMep);
            cmd.Parameters.AddWithValue("@DKBTP", m.DKBTP);
            cmd.ExecuteNonQuery();
        }

        public static string InsertDSMaSP(DanhSachMaSP sp)
        {
            try
            {
                using var conn = new SQLiteConnection(_connStr);
                conn.Open();
                using var tx = conn.BeginTransaction();

                string sql = @"
                    INSERT INTO DanhSachMaSP (Ten, Ma, DonVi, KieuSP, DateInsert)
                    VALUES (@Ten, @Ma, @DonVi, @KieuSP, @DateInsert);
                ";

                using (var cmd = new SQLiteCommand(sql, conn, tx))
                {
                    cmd.Parameters.AddWithValue("@Ten", sp.Ten);
                    cmd.Parameters.AddWithValue("@Ma", sp.Ma);
                    cmd.Parameters.AddWithValue("@DonVi", sp.DonVi);
                    cmd.Parameters.AddWithValue("@KieuSP", sp.KieuSP);
                    cmd.Parameters.AddWithValue("@DateInsert", sp.DateInsert ?? DateTime.Now);

                    cmd.ExecuteNonQuery();
                }

                tx.Commit();
                return ""; 
            }
            catch (Exception ex)
            {
                return CoreHelper.ShowErrorDatabase(ex, sp.Ma);
            }
        }
        #endregion

        #region setup config
        public static bool InsertConfig(ConfigDB config)
        {
            string query = "INSERT INTO ConfigDB (Active, Author, Message, Ngay) VALUES (@active, @author, @message, @ngay)";
            bool flg = false;
            try
            {
                using (SQLiteConnection conn = new SQLiteConnection(_connStr))
                {
                    conn.Open();

                    using (SQLiteCommand cmd = new SQLiteCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@active", config.Active);
                        cmd.Parameters.AddWithValue("@author", config.Author);
                        cmd.Parameters.AddWithValue("@message", config.Message);
                        cmd.Parameters.AddWithValue("@ngay", config.Ngay);

                        cmd.ExecuteNonQuery();
                    }

                    conn.Close();
                    flg = true;
                }
            }
            catch (Exception) { }

            return flg;

        }
        #endregion

        #region User
       
        // 1) Tạo user mới + gán roles (chỉ INSERT)
        public static bool CreateUserWithRoles(string username, string passwordHash, string name, List<int> roleIds, bool is_active = true)
        {

            roleIds = NormalizeRoleIds(roleIds);

            using var conn = new SQLiteConnection(_connStr);
            conn.Open();
            using var tran = conn.BeginTransaction();

            try
            {                

                long userId;

                using (var ins = new SQLiteCommand(@"
                INSERT INTO users(username, password_hash, name, is_active)
                VALUES(@u, @ph, @n, @ia);", conn, tran))
                {
                    ins.Parameters.AddWithValue("@u", username);
                    ins.Parameters.AddWithValue("@ph", passwordHash);
                    ins.Parameters.AddWithValue("@n", name);
                    ins.Parameters.AddWithValue("@ia", is_active ? 1 : 0);
                    ins.ExecuteNonQuery();
                    userId = conn.LastInsertRowId;
                }

                SyncUserRoles(conn, tran, userId, roleIds);

                tran.Commit();
                return true;
            }
            catch
            {
                tran.Rollback();
                throw; 
            }
        }

        public static bool UpdateUserWithRoles(string username, string? passwordHash, string name, List<int> roleIds, bool is_active = true)
        {
            roleIds = NormalizeRoleIds(roleIds);

            using var conn = new SQLiteConnection(_connStr);
            conn.Open();
            using var tran = conn.BeginTransaction();

            try
            {
                long userId = GetUserIdByUsername(conn, tran, username);
                if (userId <= 0) throw new InvalidOperationException("Không tìm thấy user để cập nhật.");

                if (!string.IsNullOrWhiteSpace(passwordHash))
                {
                    using var upd = new SQLiteCommand(@"
                    UPDATE users
                    SET password_hash=@ph,
                        name=@n,
                        is_active=@ia
                    WHERE user_id=@id;", conn, tran);

                    upd.Parameters.AddWithValue("@ph", passwordHash);
                    upd.Parameters.AddWithValue("@n", name);
                    upd.Parameters.AddWithValue("@ia", is_active ? 1 : 0);
                    upd.Parameters.AddWithValue("@id", userId);
                    upd.ExecuteNonQuery();
                }
                else
                {
                    using var upd = new SQLiteCommand(@"
                    UPDATE users
                    SET name=@n,
                        is_active=@ia
                    WHERE user_id=@id;", conn, tran);

                    upd.Parameters.AddWithValue("@n", name);
                    upd.Parameters.AddWithValue("@ia", is_active ? 1 : 0);
                    upd.Parameters.AddWithValue("@id", userId);
                    upd.ExecuteNonQuery();
                }

                SyncUserRoles(conn, tran, userId, roleIds);

                tran.Commit();
                return true;
            }
            catch
            {
                tran.Rollback();
                throw;
            }
        }
        private static List<int> NormalizeRoleIds(List<int>? roleIds)
        {
            return (roleIds ?? new List<int>())
                .Where(r => r > 0)
                .Distinct()
                .ToList();
        }

        public static async Task<List<string>> QueryAsync(string typed, CancellationToken ct)
        {
            string Esc(string s) => s.Replace(@"\", @"\\").Replace("%", @"\%").Replace("_", @"\_");
            var list = new List<string>();
            var prefix = Esc(typed) + "%";

            using (var conn = new SQLiteConnection(_connStr)) // 👈 using (không có await)
            {
                await conn.OpenAsync(ct);
                using (var cmd = conn.CreateCommand()) // 👈 using (không có await)
                {
                    cmd.CommandText = @"
                SELECT username
                FROM users
                WHERE @t = '' OR username LIKE @p ESCAPE '\'
                ORDER BY username
                LIMIT 20;";
                    cmd.Parameters.AddWithValue("@t", typed);
                    cmd.Parameters.AddWithValue("@p", prefix);

                    using (var r = await cmd.ExecuteReaderAsync(ct)) // 👈 using (không có await)
                    {
                        while (await r.ReadAsync(ct))
                        {
                            list.Add(r.GetString(0));
                        }
                    }
                }
            }
            return list;
        }

        private static long GetUserIdByUsername(SQLiteConnection conn, SQLiteTransaction tran, string username)
        {
            using var cmd = new SQLiteCommand("SELECT user_id FROM users WHERE username=@u LIMIT 1;", conn, tran);
            cmd.Parameters.AddWithValue("@u", username);
            var obj = cmd.ExecuteScalar();
            if (obj == null || obj == DBNull.Value) return -1;
            return Convert.ToInt64(obj);
        }

        // Sync roles: xóa role không còn + thêm role mới (INSERT OR IGNORE)
        private static void SyncUserRoles(SQLiteConnection conn, SQLiteTransaction tran, long userId, List<int> roleIds)
        {
            // DELETE role không còn được chọn
            if (roleIds.Count == 0)
            {
                using var delAll = new SQLiteCommand("DELETE FROM user_roles WHERE user_id=@uid;", conn, tran);
                delAll.Parameters.AddWithValue("@uid", userId);
                delAll.ExecuteNonQuery();
            }
            else
            {
                var paramNames = roleIds.Select((_, i) => $"@r{i}").ToArray();
                var sqlDel = $"DELETE FROM user_roles WHERE user_id=@uid AND role_id NOT IN ({string.Join(",", paramNames)});";

                using var del = new SQLiteCommand(sqlDel, conn, tran);
                del.Parameters.AddWithValue("@uid", userId);
                for (int i = 0; i < roleIds.Count; i++)
                    del.Parameters.AddWithValue(paramNames[i], roleIds[i]);

                del.ExecuteNonQuery();
            }

            // INSERT role đã chọn
            using var insUR = new SQLiteCommand(
                "INSERT OR IGNORE INTO user_roles(user_id, role_id) VALUES(@uid, @rid);",
                conn, tran);

            var pUid = insUR.Parameters.Add("@uid", DbType.Int64);
            var pRid = insUR.Parameters.Add("@rid", DbType.Int32);
            pUid.Value = userId;

            insUR.Prepare();

            foreach (var rid in roleIds)
            {
                pRid.Value = rid;
                insUR.ExecuteNonQuery();
            }
        }

        public static async Task<UserInfo> GetUserWithRolesByUsernameAsync(string username, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(username)) return null;

            using (var conn = new SQLiteConnection(_connStr))
            {
                await conn.OpenAsync(ct);

                // 1) user
                UserInfo u = null;
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                    SELECT user_id, username, name, is_active, created_at
                    FROM users
                    WHERE username = @u
                    LIMIT 1;";
                    cmd.Parameters.AddWithValue("@u", username.Trim());

                    using (var r = await cmd.ExecuteReaderAsync(ct))
                    {
                        if (!await r.ReadAsync(ct)) return null;

                        u = new UserInfo
                        {
                            UserId = r.GetInt32(0),
                            Username = r.GetString(1),
                            Name = r.IsDBNull(2) ? "" : r.GetString(2),
                            IsActive = r.GetInt32(3) == 1,
                            CreatedAt = r.IsDBNull(4) ? "" : r.GetString(4)
                        };
                    }
                }

                // 2) roles
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT r.role_name
                        FROM roles r
                        JOIN user_roles ur ON ur.role_id = r.role_id
                        WHERE ur.user_id = @id
                        ORDER BY r.role_id;";
                    cmd.Parameters.AddWithValue("@id", u.UserId);

                    using (var r = await cmd.ExecuteReaderAsync(ct))
                        while (await r.ReadAsync(ct)) u.Roles.Add(r.GetString(0));
                }

                return u;
            }
        }



        // treeeeview
        public static List<UserWithRoles> GetUsersWithSameRoles(int currentUserId)
        {
            var users = new List<UserWithRoles>();

            using (var conn = new SQLiteConnection(_connStr))
            {
                conn.Open();

                string query = @"
                SELECT DISTINCT 
                    u.user_id,
                    u.username,
                    u.name,
                    u.is_active
                FROM users u
                INNER JOIN user_roles ur ON u.user_id = ur.user_id
                WHERE u.is_active = 1
                  AND ur.role_id IN (
                      SELECT role_id 
                      FROM user_roles 
                      WHERE user_id = @currentUserId
                  )
                ORDER BY u.name, u.username";

                using (var cmd = new SQLiteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@currentUserId", currentUserId);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var user = new UserWithRoles
                            {
                                UserId = reader.GetInt32(0),
                                Username = reader.GetString(1),
                                Name = reader.IsDBNull(2) ? reader.GetString(1) : reader.GetString(2),
                                IsActive = reader.GetInt32(3) == 1,
                                Roles = new List<RoleInfo>()
                            };

                            // Load roles cho user này
                            user.Roles = GetUserRoles(conn, user.UserId);

                            users.Add(user);
                        }
                    }
                }
            }

            return users;
        }

        /// <summary>
        /// Lấy danh sách roles của một user
        /// </summary>
        private static List<RoleInfo> GetUserRoles(SQLiteConnection conn, int userId)
        {
            var roles = new List<RoleInfo>();

            string query = @"
            SELECT 
                r.role_id,
                r.role_name,
                r.description
            FROM roles r
            INNER JOIN user_roles ur ON r.role_id = ur.role_id
            WHERE ur.user_id = @userId
            ORDER BY r.role_name";

            using (var cmd = new SQLiteCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@userId", userId);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        roles.Add(new RoleInfo
                        {
                            RoleId = reader.GetInt32(0),
                            RoleName = reader.GetString(1),
                            Description = reader.IsDBNull(2) ? null : reader.GetString(2)
                        });
                    }
                }
            }

            return roles;
        }

        /// <summary>
        /// Load với filter theo role cụ thể
        /// </summary>
        private static List<UserWithRoles> GetUsersByRole(string roleName)
        {
            var users = new List<UserWithRoles>();

            using (var conn = new SQLiteConnection(_connStr))
            {
                conn.Open();

                string query = @"
                SELECT DISTINCT 
                    u.user_id,
                    u.username,
                    u.name,
                    u.is_active
                FROM users u
                INNER JOIN user_roles ur ON u.user_id = ur.user_id
                INNER JOIN roles r ON ur.role_id = r.role_id
                WHERE u.is_active = 1
                  AND r.role_name = @roleName
                ORDER BY u.name, u.username";

                using (var cmd = new SQLiteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@roleName", roleName);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var user = new UserWithRoles
                            {
                                UserId = reader.GetInt32(0),
                                Username = reader.GetString(1),
                                Name = reader.IsDBNull(2) ? reader.GetString(1) : reader.GetString(2),
                                IsActive = reader.GetInt32(3) == 1,
                                Roles = GetUserRoles(conn, reader.GetInt32(0))
                            };

                            users.Add(user);
                        }
                    }
                }
            }

            return users;
        }


        //end tree

        public static void LoadQuyenTheoRole(int roleId, DataGridView grvQuyen)
        {
            // Lấy toàn bộ permission + đánh dấu những permission đã thuộc roleId
            const string sql = @"
                SELECT 
                    p.permission_id,
                    p.permission_code,
                    p.permission_name,
                    CASE WHEN EXISTS (
                        SELECT 1 
                        FROM role_permissions rp
                        WHERE rp.role_id = @roleId
                          AND rp.permission_id = p.permission_id
                    ) THEN 1 ELSE 0 END AS IsChecked
                FROM permissions p;
            ";

            // Nếu grid có row trống cuối cùng, tắt nó để không bị Add nhầm
            grvQuyen.AllowUserToAddRows = false;

            grvQuyen.AutoGenerateColumns = false;
            grvQuyen.Rows.Clear();

            using (var conn = new SQLiteConnection(_connStr))
            using (var cmd = new SQLiteCommand(sql, conn))
            {
                conn.Open();
                cmd.Parameters.AddWithValue("@roleId", roleId);

                using (var rd = cmd.ExecuteReader())
                {
                    while (rd.Read())
                    {
                        int rowIndex = grvQuyen.Rows.Add();

                        grvQuyen.Rows[rowIndex].Cells[0].Value = rd.GetInt32(rd.GetOrdinal("IsChecked")) == 1;
                        grvQuyen.Rows[rowIndex].Cells[1].Value = EnumStore.TransferPermissionName[rd["permission_code"]?.ToString()];
                        grvQuyen.Rows[rowIndex].Cells[2].Value = rd["permission_name"]?.ToString();
                        grvQuyen.Rows[rowIndex].Cells[3].Value = rd["permission_code"]?.ToString();

                        grvQuyen.Rows[rowIndex].Tag = rd.GetInt32(rd.GetOrdinal("permission_id"));

                    }
                }
            }
        }

        // Giữ nguyên hàm cũ để tương thích ngược
        public static void SaveRolePermissions_ByGrid(int roleId, DataGridView grvQuyen)
        {
            // Commit trạng thái checkbox vừa click
            grvQuyen.EndEdit();
            var selected = new HashSet<int>();
            // 1) Lấy permission_id được tick từ grid
            foreach (DataGridViewRow row in grvQuyen.Rows)
            {
                if (row.IsNewRow) continue;
                bool isChecked = row.Cells["cb"].Value != null &&
                                 row.Cells["cb"].Value != DBNull.Value &&
                                 Convert.ToBoolean(row.Cells["cb"].Value);
                if (!isChecked) continue;
                if (row.Tag == null) continue;
                selected.Add(Convert.ToInt32(row.Tag));
            }

            using (var conn = new SQLiteConnection(_connStr))
            {
                conn.Open();
                using (var tran = conn.BeginTransaction())
                {
                    // 2) Lấy permission_id hiện có của role trong DB
                    var existing = new HashSet<int>();
                    using (var cmd = new SQLiteCommand(
                        @"SELECT permission_id FROM role_permissions WHERE role_id = @roleId;", conn, tran))
                    {
                        cmd.Parameters.AddWithValue("@roleId", roleId);
                        using (var rd = cmd.ExecuteReader())
                        {
                            while (rd.Read())
                                existing.Add(Convert.ToInt32(rd["permission_id"]));
                        }
                    }

                    // 3) INSERT những cái mới tick
                    using (var cmdIns = new SQLiteCommand(
                        @"INSERT OR IGNORE INTO role_permissions(role_id, permission_id)
                    VALUES (@roleId, @pid);", conn, tran))
                    {
                        var pRole = cmdIns.Parameters.Add("@roleId", System.Data.DbType.Int32);
                        var pPid = cmdIns.Parameters.Add("@pid", System.Data.DbType.Int32);
                        foreach (var pid in selected)
                        {
                            if (existing.Contains(pid)) continue;
                            pRole.Value = roleId;
                            pPid.Value = pid;
                            cmdIns.ExecuteNonQuery();
                        }
                    }

                    // 4) DELETE những cái bị bỏ tick
                    using (var cmdDel = new SQLiteCommand(
                        @"DELETE FROM role_permissions 
                  WHERE role_id = @roleId AND permission_id = @pid;", conn, tran))
                    {
                        var pRole = cmdDel.Parameters.Add("@roleId", System.Data.DbType.Int32);
                        var pPid = cmdDel.Parameters.Add("@pid", System.Data.DbType.Int32);
                        foreach (var pid in existing)
                        {
                            if (selected.Contains(pid)) continue;
                            pRole.Value = roleId;
                            pPid.Value = pid;
                            cmdDel.ExecuteNonQuery();
                        }
                    }

                    tran.Commit();
                }
            }
        }

        //public static void SaveRolePermissions_ByGrid(int roleId, DataGridView grvQuyen)
        //{
        //    // Commit trạng thái checkbox vừa click
        //    grvQuyen.EndEdit();

        //    var selected = new HashSet<int>();

        //    // 1) Lấy permission_id được tick từ grid
        //    foreach (DataGridViewRow row in grvQuyen.Rows)
        //    {
        //        if (row.IsNewRow) continue;

        //        bool isChecked = row.Cells["cb"].Value != null &&
        //                         row.Cells["cb"].Value != DBNull.Value &&
        //                         Convert.ToBoolean(row.Cells["cb"].Value);

        //        if (!isChecked) continue;

        //        if (row.Tag == null) continue; // không có permission_id thì bỏ qua

        //        selected.Add(Convert.ToInt32(row.Tag));
        //    }

        //    using (var conn = new SQLiteConnection(_connStr))
        //    {
        //        conn.Open();
        //        using (var tran = conn.BeginTransaction())
        //        {
        //            // 2) Lấy permission_id hiện có của role trong DB
        //            var existing = new HashSet<int>();
        //            using (var cmd = new SQLiteCommand(
        //                @"SELECT permission_id FROM role_permissions WHERE role_id = @roleId;", conn, tran))
        //            {
        //                cmd.Parameters.AddWithValue("@roleId", roleId);
        //                using (var rd = cmd.ExecuteReader())
        //                {
        //                    while (rd.Read())
        //                        existing.Add(Convert.ToInt32(rd["permission_id"]));
        //                }
        //            }

        //            // 3) INSERT những cái mới tick
        //            using (var cmdIns = new SQLiteCommand(
        //                @"INSERT OR IGNORE INTO role_permissions(role_id, permission_id)
        //                    VALUES (@roleId, @pid);", conn, tran))
        //            {
        //                var pRole = cmdIns.Parameters.Add("@roleId", System.Data.DbType.Int32);
        //                var pPid = cmdIns.Parameters.Add("@pid", System.Data.DbType.Int32);

        //                foreach (var pid in selected)
        //                {
        //                    if (existing.Contains(pid)) continue;

        //                    pRole.Value = roleId;
        //                    pPid.Value = pid;
        //                    cmdIns.ExecuteNonQuery();
        //                }
        //            }

        //            // 4) DELETE những cái bị bỏ tick
        //            using (var cmdDel = new SQLiteCommand(
        //                @"DELETE FROM role_permissions 
        //                WHERE role_id = @roleId AND permission_id = @pid;", conn, tran))
        //            {
        //                var pRole = cmdDel.Parameters.Add("@roleId", System.Data.DbType.Int32);
        //                var pPid = cmdDel.Parameters.Add("@pid", System.Data.DbType.Int32);

        //                foreach (var pid in existing)
        //                {
        //                    if (selected.Contains(pid)) continue;

        //                    pRole.Value = roleId;
        //                    pPid.Value = pid;
        //                    cmdDel.ExecuteNonQuery();
        //                }
        //            }

        //            tran.Commit();
        //        }
        //    }



        //    MessageBox.Show("Đã lưu quyền cho role!", "Thông báo",
        //        MessageBoxButtons.OK, MessageBoxIcon.Information);
        //}


        // Đăng nhập
        public static LoginResult Login(string usernameInput, string passwordInput)
        {
            LoginResult result = new LoginResult();

            using (SQLiteConnection conn = new SQLiteConnection(_connStr))
            {
                conn.Open();

                int userId = 0;
                string storedHash = null;
                string name = null;

                // ===== QUERY 1: XÁC THỰC USER =====
                string sqlUser = @"
                    SELECT user_id, name, password_hash
                    FROM users
                    WHERE username = @username
                      AND is_active = 1
                    LIMIT 1;
                ";

                using (SQLiteCommand cmd = new SQLiteCommand(sqlUser, conn))
                {
                    cmd.Parameters.AddWithValue("@username", usernameInput);

                    using (SQLiteDataReader rd = cmd.ExecuteReader())
                    {
                        if (!rd.Read())
                        {
                            result.Message = "Sai tài khoản hoặc mật khẩu.";
                            return result;
                        }

                        userId = Convert.ToInt32(rd["user_id"]);
                        name = Convert.ToString(rd["name"]);
                        storedHash = rd["password_hash"].ToString();
                    }
                }

                // Verify BCrypt
                if (!BCrypt.Net.BCrypt.Verify(passwordInput, storedHash))
                {
                    result.Message = "Sai tài khoản hoặc mật khẩu.";
                    return result;
                }

                // ===== QUERY 2: LOAD ROLES + PERMISSIONS =====
                string sqlPerms = @"
                    SELECT
                        r.role_name,
                        r.description AS role_description,
                        p.permission_code
                    FROM user_roles ur
                    JOIN roles r ON r.role_id = ur.role_id
                    LEFT JOIN role_permissions rp ON rp.role_id = r.role_id
                    LEFT JOIN permissions p ON p.permission_id = rp.permission_id
                    WHERE ur.user_id = @user_id;
                ";

                var rolesDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                var permsDict = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

                using (SQLiteCommand cmd = new SQLiteCommand(sqlPerms, conn))
                {
                    cmd.Parameters.AddWithValue("@user_id", userId);

                    using (SQLiteDataReader rd = cmd.ExecuteReader())
                    {
                        while (rd.Read())
                        {
                            // role_name -> description
                            if (rd["role_name"] != DBNull.Value)
                            {
                                string roleName = rd["role_name"].ToString();
                                string roleDesc = rd["role_description"] == DBNull.Value ? null : rd["role_description"].ToString();

                                if (!rolesDict.ContainsKey(roleName))
                                    rolesDict.Add(roleName, roleDesc);

                                // role_name -> set(permission_code)
                                if (rd["permission_code"] != DBNull.Value)
                                {
                                    string permCode = rd["permission_code"].ToString();

                                    if (!permsDict.TryGetValue(roleName, out var set))
                                    {
                                        set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                                        permsDict[roleName] = set;
                                    }

                                    set.Add(permCode);
                                }
                                else
                                {
                                    // đảm bảo role vẫn có key dù chưa gán permission nào
                                    if (!permsDict.ContainsKey(roleName))
                                        permsDict[roleName] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                                }
                            }
                        }
                    }
                }

                // ===== HOÀN TẤT LOGIN =====
                result.Success = true;
                result.UserId = userId;
                result.Name = name;
                result.RolesDict = rolesDict;
                result.PermissionsDict = permsDict;
                result.Message = "OK";

                return result;
            }
        }

        #endregion


        #region KẾ hoạch ===================

        public static Dictionary<string, decimal> LayNVLCanTheoKeHoach(Dictionary<string, decimal> demandPlan, bool kTinhTon)
        {
            using (var connection = new SQLiteConnection(_connStr))
            {
                connection.Open();

                // 1. Lấy danh sách mã sản phẩm từ demandPlan
                var demandKeys = string.Join(",", demandPlan.Keys.Select(key => $"'{key}'"));

                // 2. SQL để lấy dữ liệu từ bảng DanhSachMaSP và BOMStructure
                string sql = $@"
                SELECT sp.Ma, sp.DonVi, sp.KieuSP, sp.Ten, sp.id, bom.ParentProduct, bom.Component, bom.TyLe, bom.TyLeHoanDoi
                FROM DanhSachMaSP sp
                INNER JOIN BOMStructure bom ON bom.Component = sp.id
                WHERE sp.Ten IN ({demandKeys}) AND sp.KieuSP = 'TP'";

                var command = new SQLiteCommand(sql, connection);
                var reader = command.ExecuteReader();

                // 3. Tạo dictionary chứa thông tin về vật liệu (donvi, tyle, v.v.)
                var materials = new Dictionary<string, (string DonVi, string KieuSP, decimal TyLe, decimal TyLeHoanDoi)>();

                while (reader.Read())
                {
                    string ma = reader["Ma"].ToString();
                    string donVi = reader["DonVi"].ToString();
                    string kieuSP = reader["KieuSP"].ToString();
                    decimal tyLe = Convert.ToDecimal(reader["TyLe"]);
                    decimal tyLeHoanDoi = Convert.ToDecimal(reader["TyLeHoanDoi"]);

                    materials[ma] = (donVi, kieuSP, tyLe, tyLeHoanDoi);
                }

                // 4. Tính toán nguyên vật liệu (NVL) cần thiết dựa trên demandPlan
                var result = new Dictionary<string, decimal>();

                foreach (var demand in demandPlan)
                {
                    string ten = demand.Key;
                    decimal quantity = demand.Value;

                    // Kiểm tra nếu vật liệu có trong dữ liệu
                    if (materials.ContainsKey(ten))
                    {
                        var material = materials[ten];

                        if (kTinhTon)
                        {
                            // Trường hợp 1: Tính toán nguyên vật liệu trực tiếp
                            // Tính toán theo TyLe và TyLeHoanDoi
                            decimal nvlRequired = quantity * material.TyLe;
                            decimal nvlConverted = nvlRequired * material.TyLeHoanDoi;
                            result[ten] = nvlConverted;
                        }
                        else
                        {
                            // Trường hợp 2: Tính toán với BTP đã sản xuất
                            // Trừ đi lượng BTP đã sản xuất (dựa vào KhoiLuongSau hoặc ChieuDaiSau)
                            decimal btpProduced = GetProducedBTP(ten, connection); // Truyền kết nối đã mở
                            decimal btpRequired = quantity - btpProduced;
                            decimal nvlRequired = btpRequired * material.TyLe;
                            decimal nvlConverted = nvlRequired * material.TyLeHoanDoi;
                            result[ten] = nvlConverted;
                        }
                    }
                }

                return result;
            }
        }

        private static decimal GetProducedBTP(string maSP, SQLiteConnection connection)
        {
            // Sử dụng kết nối đã mở thay vì mở kết nối mới
            string sql = $@"
            SELECT 
                CASE 
                    WHEN sp.DonVi = 'KG' THEN sp.KhoiLuongSau
                    WHEN sp.DonVi = 'M' THEN sp.ChieuDaiSau
                    ELSE 0 
                END AS ProducedQuantity
            FROM DanhSachMaSP sp
            WHERE sp.Ma = '{maSP}'";

            var command = new SQLiteCommand(sql, connection);
            var result = command.ExecuteScalar();

            // Nếu có giá trị trả về, chuyển sang kiểu decimal, nếu không trả về 0
            return result != DBNull.Value ? Convert.ToDecimal(result) : 0;
        }

        // Hàm hiển thị kết quả (có thể thay thế bằng logic giao diện người dùng của bạn)
        private static void DisplayResults(Dictionary<string, decimal> result)
        {
            foreach (var res in result)
            {
                Console.WriteLine($"Vật liệu: {res.Key}, Cần thiết: {res.Value} kg");
            }
        }

        #endregion


        #region Hạ Bin
        public static void Update_KhoiLuongSau_ChieuDaiSau(string maBin, decimal khoiLuongSau, decimal chieuDaiSau)
        {
            using (var connection = new SQLiteConnection(_connStr))
            {
                connection.Open();
                string sql = @"
                UPDATE TTThanhPham
                SET KhoiLuongSau = @khoiLuongSau,
                    ChieuDaiSau  = @chieuDaiSau
                WHERE MaBin = @maBin;";

                using var command = new SQLiteCommand(sql, connection);
                command.Parameters.AddWithValue("@khoiLuongSau", khoiLuongSau);
                command.Parameters.AddWithValue("@chieuDaiSau", chieuDaiSau);
                command.Parameters.AddWithValue("@maBin", maBin);

                int affected = command.ExecuteNonQuery();
                if (affected == 0)
                    throw new Exception($"Không tìm thấy MaBin: {maBin}");
            }
        }


        #endregion

    }


}
