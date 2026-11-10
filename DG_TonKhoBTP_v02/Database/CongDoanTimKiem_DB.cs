using System;
using System.Data;
using System.Data.SQLite;

namespace DG_TonKhoBTP_v02.Database
{
    /// <summary>
    /// Chỉ đọc cấu hình tìm kiếm thành phẩm theo mã công đoạn nghiệp vụ (CongDoan.Id).
    /// Hai bảng cấu hình lưu FK DanhSachCongDoan_id, KHÔNG lưu mã công đoạn này.
    /// </summary>
    public static class CongDoanTimKiem_DB
    {
        private const string ResolveStageSql = @"
SELECT id FROM DanhSachCongDoan WHERE MaCongDoan = @MaCongDoan;";

        private const string HasAllowingRuleSql = @"
SELECT
    EXISTS (SELECT 1 FROM CongDoan_MaTimKiem AS m
            WHERE m.DanhSachCongDoan_id = @DanhSachCongDoan_id
              AND m.Active = 1)
    OR
    EXISTS (SELECT 1 FROM CongDoan_DacBiet AS e
            WHERE e.DanhSachCongDoan_id = @DanhSachCongDoan_id
              AND e.Active = 1 AND e.LoaiQuyTac = 'CHO_PHEP');";

        // Các giá trị nhập đều là tham số. MaTimKiem được lưu như một mẫu LIKE,
        // không ghép vào SQL dưới dạng chuỗi thực thi.
        private const string SearchSql = @"
SELECT sp.id, sp.ten, sp.ma, sp.donvi, sp.chuyenDoi
FROM DanhSachMaSP AS sp
WHERE sp.Active = 1
  AND sp.ten LIKE '%' || @TuKhoa || '%'
  AND (
      EXISTS (
          SELECT 1 FROM CongDoan_MaTimKiem AS m
          WHERE m.DanhSachCongDoan_id = @DanhSachCongDoan_id
            AND m.Active = 1 AND sp.Ma LIKE m.MaTimKiem
      )
      OR EXISTS (
          SELECT 1 FROM CongDoan_DacBiet AS e
          WHERE e.DanhSachCongDoan_id = @DanhSachCongDoan_id
            AND e.DanhSachMaSP_ID = sp.id
            AND e.Active = 1 AND e.LoaiQuyTac = 'CHO_PHEP'
      )
  )
  AND NOT EXISTS (
      SELECT 1 FROM CongDoan_DacBiet AS x
      WHERE x.DanhSachCongDoan_id = @DanhSachCongDoan_id
        AND x.DanhSachMaSP_ID = sp.id
        AND x.Active = 1 AND x.LoaiQuyTac = 'LOAI_TRU'
  );";

        /// <summary>
        /// Tìm theo tên và cấu hình của công đoạn. Trả về cột id/ten/ma/donvi/chuyenDoi
        /// để UI tiếp tục dùng DataRowViewWrapper và tải BOM theo sản phẩm đã chọn.
        /// Không dùng fallback hardcode khi thiếu cấu hình.
        /// </summary>
        public static DataTable TimSanPham(int maCongDoan, string tuKhoa)
        {
            var result = new DataTable();
            if (string.IsNullOrWhiteSpace(tuKhoa))
                return result;

            string connectionString = DatabaseHelper.GetStringConnector;
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException("Chưa thiết lập kết nối cơ sở dữ liệu.");

            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                using var tx = conn.BeginTransaction(IsolationLevel.ReadCommitted);
                long danhSachCongDoanId;
                using (var cmd = new SQLiteCommand(ResolveStageSql, conn, tx))
                {
                    cmd.Parameters.AddWithValue("@MaCongDoan", maCongDoan);
                    using (var rd = cmd.ExecuteReader())
                    {
                        if (!rd.Read())
                            throw new InvalidOperationException($"Không tìm thấy công đoạn có MaCongDoan = {maCongDoan} trong DanhSachCongDoan.");
                        danhSachCongDoanId = Convert.ToInt64(rd[0]);
                        if (rd.Read())
                            throw new InvalidOperationException($"MaCongDoan = {maCongDoan} bị trùng trong DanhSachCongDoan.");
                    }
                }

                using (var cmd = new SQLiteCommand(HasAllowingRuleSql, conn, tx))
                {
                    cmd.Parameters.AddWithValue("@DanhSachCongDoan_id", danhSachCongDoanId);
                    if (Convert.ToInt32(cmd.ExecuteScalar()) == 0)
                        throw new InvalidOperationException($"Công đoạn {maCongDoan} chưa có mẫu mã hoặc sản phẩm CHO_PHEP đang hoạt động. Hãy kiểm tra cấu hình tìm kiếm.");
                }

                using (var cmd = new SQLiteCommand(SearchSql, conn, tx))
                {
                    cmd.Parameters.AddWithValue("@DanhSachCongDoan_id", danhSachCongDoanId);
                    cmd.Parameters.AddWithValue("@TuKhoa", tuKhoa.Trim());
                    using (var adapter = new SQLiteDataAdapter(cmd))
                        adapter.Fill(result);
                }
                tx.Commit();
            }
            return result;
        }
    }
}
