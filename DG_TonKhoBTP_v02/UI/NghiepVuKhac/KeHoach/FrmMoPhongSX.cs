using DG_TonKhoBTP_v02.Database;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static DG_TonKhoBTP_v02.Models.KeHoach;

namespace DG_TonKhoBTP_v02.UI.NghiepVu.KeHoach
{
    public partial class FrmMoPhongSX : Form
    {
        public FrmMoPhongSX()
        {
            InitializeComponent();
        }


        private void btnLayKQ_Click(object sender, EventArgs e)
        {
            //string s = "WHERE k.TrangThaiSX IN(";
            string s = "";
            s += cbChuaBanHanh.Checked ? "0," : "";
            s += cbDaBanHanh.Checked ? "1," : "";
            s += cbDaXong.Checked ? "2," : "";
            s += cbHuy.Checked ? "3," : "";
            s = s.Substring(0, s.Length - 1);

            if (s.Length > 0) s = "WHERE k.TrangThaiSX IN(" + s + ")";

            try
            {
                string query = @"
                    SELECT
                        d.ma  AS MaSP,
                        d.ten AS TenSP,
                        k.NgayNhan,
                        k.Lot,
                        k.SLHangDat,
                        k.SLHangBan,
                        k.Mau,
                        k.NgayGiao,
                        k.GhiChu,
                        k.TenKhachHang,
                        k.GhiChu_QuanDoc
                    FROM KeHoachSX k
                    LEFT JOIN DanhSachMaSP d ON d.id = k.DanhSachMaSP_ID "
                        + s + 
                        @" ORDER BY k.MucDoUuTienKH ASC, k.NgayGiao ASC, k.id ASC; ";

                // Lấy dữ liệu bằng hàm bạn có sẵn
                DataTable dt = DatabaseHelper.GetData(query);

                // Thêm cột STT vào DataTable (để người dùng có thể edit)
                if (!dt.Columns.Contains("STT"))
                {
                    DataColumn colSTT = new DataColumn("STT", typeof(int));
                    dt.Columns.Add(colSTT);
                    colSTT.SetOrdinal(0); // đưa STT lên cột đầu tiên
                }

                // Gán STT 1..N (chỉ gán 1 lần để không ghi đè khi user edit)
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    // Chỉ gán nếu đang rỗng/0 (tránh trường hợp bạn load lại mà muốn giữ STT cũ)
                    if (dt.Rows[i]["STT"] == DBNull.Value || Convert.ToInt32(dt.Rows[i]["STT"]) == 0)
                        dt.Rows[i]["STT"] = i + 1;
                }

                // Đổ vào DataGridView
                dsKH.AutoGenerateColumns = true;
                dsKH.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;

                dsKH.DataSource = dt;


                // Cho phép sửa STT
                dsKH.Columns["STT"].ReadOnly = false;

                //dsKH.Columns["STT"].SortMode = DataGridViewColumnSortMode.NotSortable;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải dữ liệu: " + ex.Message);
            }
        }


        public  Dictionary<string, decimal> LaydsKH()
        {
            // Tạo Dictionary demandPlan từ DataGridView dsKH
            var demandPlan = new Dictionary<string, decimal>();

            foreach (DataGridViewRow row in dsKH.Rows)
            {
                // Lấy các giá trị từ DataGridView (cột 1 là "TenSP", cột 6 và cột 7 là các cột có giá trị cần tính)
                if (row.Cells[0].Value != null && row.Cells[0].Value.ToString() != "")
                {
                    string tenSP = row.Cells[2].Value.ToString();
                    decimal value6 = Convert.ToDecimal(row.Cells[5].Value ?? 0);
                    decimal value7 = Convert.ToDecimal(row.Cells[6].Value ?? 0);

                    // Điều kiện: Chỉ tính khi cột số 1 có giá trị khác rỗng hoặc = 0
                    if (row.Cells[0].Value.ToString() != "")
                    {
                        decimal totalValue = value6 + value7;

                        // Thêm vào demandPlan mà không cộng dồn
                        demandPlan[tenSP] = totalValue; // Mỗi dòng sẽ có một kết quả riêng
                    }
                }
            }

            return demandPlan;
        }

        private void btnDuToanNVL_Click(object sender, EventArgs e)
        {

            Dictionary<string, decimal> dsKH_data = LaydsKH();
            Dictionary<string, decimal> dsNVL = DatabaseHelper.LayNVLCanTheoKeHoach(dsKH_data, cbBaoGomNVL.Checked);
            Console.WriteLine(dsNVL);
        }
    }
}
