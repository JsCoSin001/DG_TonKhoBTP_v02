using DG_TonKhoBTP_v02.Core;
using DG_TonKhoBTP_v02.Database;
using DG_TonKhoBTP_v02.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CoreHelper = DG_TonKhoBTP_v02.Helper.Helper;

namespace DG_TonKhoBTP_v02.UI.Helper.AutoSearchWithCombobox
{
    public class DatHangItem
    {
        public int Id { get; set; }
        public string Ma { get; set; }
        public string Ten { get; set; }
        public string DonVi { get; set; }
        public decimal SoLuong { get; set; }
        public decimal DonGia { get; set; }
        public string MucDich { get; set; }
        public string NgayGiao { get; set; }
    }

    public class DanhSachDatHang
    {
        public int? Id { get; set; }
        public string MaDon { get; set; }
        public int? LoaiDon { get; set; } // 1: Đơn mua vật tư, 2: Đơn dịch vụ
        public string DateInsert { get; set; } = DateTime.Now.ToString("yyyy-MM-dd");
    }


    public class ThongTinDatHang
    {
        public int? Id { get; set; }
        public int? DanhSachMaSP_ID { get; set; }
        public int? DanhSachDatHang_ID { get; set; }
        public string? MaVatTu { get; set; }
        public string? TenVatTu { get; set; }
        public string? DonVi { get; set; }
        public decimal? SoLuongMua { get; set; }
        public decimal? DonGia { get; set; }
        public string MucDichMua { get; set; }
        public string NgayGiao { get; set; }
        public string SoLuongTon { get; set; }
        public string DateInsert { get; set; }
    }

    public class LichSuXuatNhap
    {
        public int? Id { get; set; }
        public int? DanhSachMaSP_ID { get; set; }
        public int? ThongTinDatHang { get; set; }
        public string NgayXuatNhap { get; set; }
        public string NguoiGiaoNhan { get; set; }
        public string LyDo { get; set; }
        public decimal SoLuong { get; set; }
        public string MaDon { get; set; }
    }


    public class MuaVatTuSearchItem
    {
        public bool IsDonHang { get; set; }

        public int? Id { get; set; }

        // Dùng cho vật tư
        public string Ma { get; set; }
        public string Ten { get; set; }
        public string DonVi { get; set; }

        // Dùng cho đơn hàng
        public string MaDon { get; set; }

        public string DisplayText
        {
            get
            {
                if (IsDonHang)
                    return MaDon;

                return $"{Ten}";
            }
        }
    }


    public class VatTuRepository
    {
        private const int Limit = 30;

        public async Task<List<MuaVatTuSearchItem>> TimKiemTheoCheDoAsync(string keyword, bool taoMoiDon, int kieuDon)
        {
            if (taoMoiDon)
                return await TimKiemChoMuaVatTuAsync(keyword);

            return await TimKiemDonHangAsync(keyword, kieuDon);
        }



        public DanhSachMaSP GetVatTuById(int id)
        {
            string sql = "SELECT Id, Ten, Ma, DonVi FROM DanhSachMaSP WHERE Id = @Id LIMIT 1";
            DataTable dt = DatabaseHelper.GetData(sql, id.ToString(), "Id");

            if (dt.Rows.Count == 0) return null;

            DataRow row = dt.Rows[0];

            return new DanhSachMaSP
            {
                Id = Convert.ToInt32(row["Id"]),
                Ma = row["Ma"]?.ToString(),
                Ten = row["Ten"]?.ToString(),
                DonVi = row["DonVi"]?.ToString()
            };
        }

        public List<ThongTinDatHang> GetChiTietDonHang(string maDon)
        {
            var result = new List<ThongTinDatHang>();
            string sql = @"
        SELECT 
            ttdh.Id,
            ttdh.DanhSachMaSP_ID,
            ttdh.DanhSachDatHang_ID,
            ttdh.TenVatTu,              -- ← THÊM
            ttdh.SoLuongMua,
            ttdh.DonGia,
            ttdh.MucDichMua,
            ttdh.NgayGiao,
            ttdh.Date_Insert
        FROM ThongTinDatHang ttdh
        INNER JOIN DanhSachDatHang dsdh ON dsdh.Id = ttdh.DanhSachDatHang_ID
        WHERE dsdh.MaDon = @MaDon;
    ";
            DataTable dt = DatabaseHelper.GetData(sql, maDon, "MaDon");
            foreach (DataRow row in dt.Rows)
            {
                result.Add(new ThongTinDatHang
                {
                    Id = row["Id"] == DBNull.Value ? (int?)null : Convert.ToInt32(row["Id"]),
                    DanhSachMaSP_ID = row["DanhSachMaSP_ID"] == DBNull.Value ? (int?)null : Convert.ToInt32(row["DanhSachMaSP_ID"]),
                    DanhSachDatHang_ID = row["DanhSachDatHang_ID"] == DBNull.Value ? (int?)null : Convert.ToInt32(row["DanhSachDatHang_ID"]),
                    TenVatTu = row["TenVatTu"] == DBNull.Value ? null : row["TenVatTu"].ToString(), // ← THÊM
                    SoLuongMua = row["SoLuongMua"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(row["SoLuongMua"]),
                    DonGia = row["DonGia"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(row["DonGia"]),
                    MucDichMua = row["MucDichMua"] == DBNull.Value ? null : row["MucDichMua"].ToString(),
                    NgayGiao = row["NgayGiao"] == DBNull.Value ? null : row["NgayGiao"].ToString(),
                    DateInsert = row["Date_Insert"] == DBNull.Value ? null : row["Date_Insert"].ToString()
                });
            }
            return result;
        }





        public async Task<List<MuaVatTuSearchItem>> TimKiemChoMuaVatTuAsync(string keyword)
        {
            var result = new List<MuaVatTuSearchItem>();
            if (string.IsNullOrWhiteSpace(keyword)) return result;

            string pattern = keyword.Trim();

            await Task.Run(() =>
            {
                string sql = "SELECT Id, Ten, Ma, DonVi " +
                         "FROM DanhSachMaSP " +
                         "WHERE Ten LIKE '%' || @keyword || '%' " +
                         "LIMIT " + Limit;

                DataTable dt = DatabaseHelper.GetData(sql, pattern, "keyword");

                foreach (DataRow row in dt.Rows)
                {
                    result.Add(new MuaVatTuSearchItem
                    {
                        IsDonHang = false,
                        Id = Convert.ToInt32(row["Id"]),
                        Ma = row["Ma"]?.ToString(),
                        Ten = row["Ten"]?.ToString(),
                        DonVi = row["DonVi"]?.ToString()
                    });
                }
            });

            return result;
        }

        public async Task<List<MuaVatTuSearchItem>> TimKiemDonHangAsync(string keyword, int kieuDon)
        {
            var result = new List<MuaVatTuSearchItem>();
            if (string.IsNullOrWhiteSpace(keyword)) return result;

            string pattern = keyword.Trim();

            await Task.Run(() =>
            {
                string sql = "SELECT Id, MaDon " +
                             "FROM DanhSachDatHang " +
                             "WHERE MaDon LIKE '%' || @keyword || '%' " +
                             "AND LoaiDon =  " + kieuDon + " " +
                             "LIMIT " + Limit;

                DataTable dt = DatabaseHelper.GetData(sql, pattern, "keyword");

                foreach (DataRow row in dt.Rows)
                {
                    result.Add(new MuaVatTuSearchItem
                    {
                        IsDonHang = true,
                        Id = Convert.ToInt32(row["Id"]),
                        MaDon = row["MaDon"]?.ToString()
                    });
                }
            });

            return result;
        }

        public async Task<List<DanhSachMaSP>> TimKiemAsync(string keyword)
        {
            var result = new List<DanhSachMaSP>();
            if (string.IsNullOrWhiteSpace(keyword)) return result;

            string pattern = $"%{keyword.Trim()}%";

            await Task.Run(() =>
            {
                string SqlTimKiem = "SELECT Id, Ten, Ma, DonVi FROM DanhSachMaSP WHERE Ten LIKE '%' || @keyword || '%' LIMIT "+ Limit;
                DataTable dt = DatabaseHelper.GetData(SqlTimKiem, pattern, "keyword");

                foreach (DataRow row in dt.Rows)
                {
                    result.Add(new DanhSachMaSP
                    {
                        Id = Convert.ToInt32(row["id"]),
                        Ma = row["Ma"].ToString(),
                        Ten = row["Ten"].ToString(),
                        DonVi = row["DonVi"].ToString(),
                    });
                }
            });

            return result;
        }

        public long InsertDonDatHang(DanhSachDatHang don, List<ThongTinDatHang> chiTietList)
        {
            using var conn = new SQLiteConnection(DatabaseHelper.GetStringConnector);
            conn.Open();

            using var tx = conn.BeginTransaction();

            try
            {
                long danhSachDatHangId = InsertDanhSachDatHang(conn, tx, don);

                foreach (var item in chiTietList)
                {
                    item.DanhSachDatHang_ID = (int)danhSachDatHangId;
                    InsertThongTinDatHang(conn, tx, item);
                }

                tx.Commit();
                return danhSachDatHangId;
            }
            catch
            {
                try { tx.Rollback(); } catch { }
                throw;
            }
        }

        public void UpdateDonDatHang(DanhSachDatHang don, List<ThongTinDatHang> chiTietList)
        {
            using var conn = new SQLiteConnection(DatabaseHelper.GetStringConnector);
            conn.Open();

            using var tx = conn.BeginTransaction();

            try
            {
                long danhSachDatHangId = GetDanhSachDatHangIdByMaDon(conn, tx, don.MaDon);

                using (var cmd = new SQLiteCommand(@"
                    UPDATE DanhSachDatHang
                    SET LoaiDon = @LoaiDon
                    WHERE id = @Id;
                ", conn, tx))
                {
                    AddParam(cmd, "@LoaiDon", DbType.Int32, don.LoaiDon);
                    AddParam(cmd, "@Id", DbType.Int64, danhSachDatHangId);

                    int rows = cmd.ExecuteNonQuery();
                    if (rows == 0)
                        throw new InvalidOperationException($"Không tìm thấy đơn hàng để cập nhật: {don.MaDon}");
                }

                using (var cmd = new SQLiteCommand(@"
                    DELETE FROM ThongTinDatHang
                    WHERE DanhSachDatHang_ID = @DanhSachDatHang_ID;
                ", conn, tx))
                {
                    AddParam(cmd, "@DanhSachDatHang_ID", DbType.Int64, danhSachDatHangId);
                    cmd.ExecuteNonQuery();
                }

                foreach (var item in chiTietList)
                {
                    item.DanhSachDatHang_ID = (int)danhSachDatHangId;
                    InsertThongTinDatHang(conn, tx, item);
                }

                tx.Commit();
            }
            catch
            {
                try { tx.Rollback(); } catch { }
                throw;
            }
        }

        private long InsertDanhSachDatHang(SQLiteConnection conn, SQLiteTransaction tx, DanhSachDatHang don)
        {
            using var cmd = new SQLiteCommand(@"
                INSERT INTO DanhSachDatHang (MaDon, LoaiDon, DateInsert)
                VALUES (@MaDon, @LoaiDon, @DateInsert);
                SELECT last_insert_rowid();
            ", conn, tx);

            AddParam(cmd, "@MaDon", DbType.String, CoreHelper.TrimToNull(don.MaDon));
            AddParam(cmd, "@LoaiDon", DbType.Int32, don.LoaiDon);
            AddParam(cmd, "@DateInsert", DbType.String, don.DateInsert);

            object result = cmd.ExecuteScalar();
            return Convert.ToInt64(result);
        }

        private void InsertThongTinDatHang(SQLiteConnection conn, SQLiteTransaction tx, ThongTinDatHang item)
        {
            using var cmd = new SQLiteCommand(@"
            INSERT INTO ThongTinDatHang
            (
                DanhSachMaSP_ID,
                DanhSachDatHang_ID,
                TenVatTu,
                SoLuongMua,
                DonGia,
                MucDichMua,
                NgayGiao,
                Date_Insert
            )
            VALUES
            (
                @DanhSachMaSP_ID,
                @DanhSachDatHang_ID,
                @TenVatTu,
                @SoLuongMua,
                @DonGia,
                @MucDichMua,
                @NgayGiao,
                @Date_Insert
            );
        ", conn, tx);

            AddParam(cmd, "@DanhSachMaSP_ID", DbType.Int32, item.DanhSachMaSP_ID);
            AddParam(cmd, "@DanhSachDatHang_ID", DbType.Int32, item.DanhSachDatHang_ID);
            AddParam(cmd, "@TenVatTu", DbType.String, item.TenVatTu);
            AddParam(cmd, "@SoLuongMua", DbType.Decimal, item.SoLuongMua);
            AddParam(cmd, "@DonGia", DbType.Decimal, item.DonGia);
            AddParam(cmd, "@MucDichMua", DbType.String, CoreHelper.TrimToNull(item.MucDichMua));
            AddParam(cmd, "@NgayGiao", DbType.String, NormalizeDate(item.NgayGiao));
            AddParam(cmd, "@Date_Insert", DbType.String,
                string.IsNullOrWhiteSpace(item.DateInsert)
                    ? DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                    : item.DateInsert);

            cmd.ExecuteNonQuery();
        }

        private long GetDanhSachDatHangIdByMaDon(SQLiteConnection conn, SQLiteTransaction tx, string maDon)
        {
            using var cmd = new SQLiteCommand(@"
            SELECT id
            FROM DanhSachDatHang
            WHERE MaDon = @MaDon
            LIMIT 1;
        ", conn, tx);

            AddParam(cmd, "@MaDon", DbType.String, CoreHelper.TrimToNull(maDon));

            object result = cmd.ExecuteScalar();

            if (result == null || result == DBNull.Value)
                throw new InvalidOperationException($"Không tìm thấy mã đơn: {maDon}");

            return Convert.ToInt64(result);
        }

        private void AddParam(SQLiteCommand cmd, string name, DbType type, object value)
        {
            var p = cmd.CreateParameter();
            p.ParameterName = name;
            p.DbType = type;
            p.Value = value ?? DBNull.Value;
            cmd.Parameters.Add(p);
        }

        private string NormalizeDate(string dateStr)
        {
            if (string.IsNullOrWhiteSpace(dateStr))
                return null;

            if (DateTime.TryParse(dateStr, out DateTime d))
                return d.ToString("yyyy-MM-dd");

            return dateStr.Trim();
        }
    }
}
