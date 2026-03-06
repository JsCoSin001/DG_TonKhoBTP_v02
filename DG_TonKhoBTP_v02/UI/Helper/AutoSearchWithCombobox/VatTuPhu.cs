using DG_TonKhoBTP_v02.Core;
using DG_TonKhoBTP_v02.Database;
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
    public class VatTuRepository
    {
        // Limit cố định – không truyền từ ngoài vào
        private const int Limit = 30;

        // SQL tìm kiếm theo Tên hoặc Mã, giới hạn 30 dòng
        // @kw = pattern LIKE "%keyword%"
        

        // ------------------------------------------------------------------ //
        //  GetData(query, key, para):
        //    key  = giá trị truyền vào
        //    para = tên parameter không có @  →  "@kw" trong SQL thì para = "kw"
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Tìm kiếm vật tư theo tên hoặc mã (không phân biệt hoa thường).
        /// Sử dụng DatabaseHelper.GetData có sẵn, limit cố định 30.
        /// </summary>
        public async Task<List<DanhSachMaSP>> TimKiemAsync(string keyword)
        {
            var result = new List<DanhSachMaSP>();
            if (string.IsNullOrWhiteSpace(keyword)) return result;

            string pattern = $"%{keyword.Trim()}%";

            await Task.Run(() =>
            {
                string SqlTimKiem = "SELECT Id, Ten, Ma, DonVi FROM DanhSachMaSP WHERE Ten LIKE '%' || @keyword || '%' LIMIT 50";
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

        public void InsertDatHang(List<DatHangItem> list)
        {
            if (list == null || list.Count == 0) return;

            const string sql = @"
            INSERT INTO DatHang (DanhSachMaSP_ID, SoLuongMua, DonGia, MucDichMua, NgayGiao, LoaiDat, Date_Insert)
            VALUES (@id, @soLuong, @donGia, @mucDich, @ngayGiao, 1, @dateInsert);";

            try
            {
                using var conn = new SQLiteConnection(DatabaseHelper.GetStringConnector);
                conn.Open();
                using var tran = conn.BeginTransaction();
                using var cmd = new SQLiteCommand(sql, conn, tran);

                foreach (var item in list)
                {
                    cmd.Parameters.Clear();
                    cmd.Parameters.AddWithValue("@id", item.Id);
                    cmd.Parameters.AddWithValue("@soLuong", item.SoLuong);
                    cmd.Parameters.AddWithValue("@donGia", item.DonGia);
                    cmd.Parameters.AddWithValue("@mucDich", (object)item.MucDich ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ngayGiao", (object)item.NgayGiao ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@dateInsert", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    cmd.ExecuteNonQuery();
                }

                tran.Commit();
                MessageBox.Show("Lưu thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                FrmWaiting.ShowGifAlert(CoreHelper.ShowErrorDatabase(ex, "ĐẶT HÀNG"));
            }
        }
    }
}
