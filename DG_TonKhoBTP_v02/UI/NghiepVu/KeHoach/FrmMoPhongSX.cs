using DG_TonKhoBTP_v02.Database;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DG_TonKhoBTP_v02.UI.NghiepVu.KeHoach
{
    public partial class FrmMoPhongSX : Form
    {
        public FrmMoPhongSX()
        {
            InitializeComponent();
        }

        public static void EnableRowNumber(DataGridView dgv, string sttColumnName = "STT", int sttWidth = 50)
        {
            if (dgv == null) throw new ArgumentNullException(nameof(dgv));

            // 1) Thêm cột STT nếu chưa có
            if (!dgv.Columns.Contains(sttColumnName))
            {
                var col = new DataGridViewTextBoxColumn
                {
                    Name = sttColumnName,
                    HeaderText = sttColumnName,
                    Width = sttWidth,
                    ReadOnly = true,
                    SortMode = DataGridViewColumnSortMode.NotSortable // STT không nên sort
                };

                dgv.Columns.Insert(0, col);
            }
            else
            {
                // Nếu đã có thì đưa nó lên đầu
                dgv.Columns[sttColumnName].DisplayIndex = 0;
                dgv.Columns[sttColumnName].SortMode = DataGridViewColumnSortMode.NotSortable;
            }

            // 2) Tránh đăng ký event nhiều lần (gọi hàm nhiều lần vẫn an toàn)
            dgv.RowPostPaint -= Dgv_RowPostPaint_RowNumber;
            dgv.RowPostPaint += Dgv_RowPostPaint_RowNumber;

            // 3) Lưu tên cột STT vào Tag để handler dùng lại
            dgv.Tag = sttColumnName;

            // 4) Refresh để thấy STT ngay
            dgv.Invalidate();
        }

        private static void Dgv_RowPostPaint_RowNumber(object sender, DataGridViewRowPostPaintEventArgs e)
        {
            var dgv = sender as DataGridView;
            if (dgv == null) return;

            // Bỏ qua dòng "new row" nếu AllowUserToAddRows = true
            if (dgv.Rows[e.RowIndex].IsNewRow) return;

            string sttColumnName = dgv.Tag as string ?? "STT";
            if (!dgv.Columns.Contains(sttColumnName)) return;

            // STT theo thứ tự hiển thị: 1,2,3...
            dgv.Rows[e.RowIndex].Cells[sttColumnName].Value = (e.RowIndex + 1).ToString();
        }

        public static void EnableEditableOrderColumn(DataGridView dgv, string colName = "ThuTu", int width = 60)
        {
            if (dgv.Columns.Contains(colName))
            {
                dgv.Columns[colName].DisplayIndex = 0;
                dgv.Columns[colName].ReadOnly = false;
                dgv.Columns[colName].Width = width;
                return;
            }

            var col = new DataGridViewTextBoxColumn
            {
                Name = colName,
                HeaderText = colName,
                Width = width,
                ReadOnly = false
            };
            dgv.Columns.Insert(0, col);
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

        private void button1_Click(object sender, EventArgs e)
        {

        }
    }
}
