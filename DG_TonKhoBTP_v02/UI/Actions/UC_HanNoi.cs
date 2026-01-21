using DG_TonKhoBTP_v02.Database;
using DG_TonKhoBTP_v02.DL_Ben;
using DG_TonKhoBTP_v02.Models;
using DocumentFormat.OpenXml;
using System;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using CoreHelper = DG_TonKhoBTP_v02.Helper.Helper;



namespace DG_TonKhoBTP_v02.UI.Actions
{
    public partial class UC_HanNoi : UserControl, ICustomUserControl
    {
        public event Action<DataTable> OnDataReady;
        public CongDoan CongDoan { get; private set; }
        private string _callTimer;

        public UC_HanNoi()
        {
            InitializeComponent();
            timer1.Interval = 300;
            KhoiTao();
        }

        private void KhoiTao()
        {
            var list = EnumStore.MayTheoCongDoan["KeoRut"]
            .Concat(EnumStore.MayTheoCongDoan["Ben_CU_AL"])
            .Distinct()
            .ToList();

            may.DataSource = list;

            dgDsLot.Columns.Add("ID", "ID");
            dgDsLot.Columns.Add("lot", "Lô");
            dgDsLot.Columns.Add("kl", "Khối lượng");
            dgDsLot.Columns.Add("ten", "Tên sản phẩm");
            dgDsLot.Columns["ten"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            dgDsLot.Columns["ID"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            dgDsLot.Columns["lot"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;

            // ===== Thêm cột nút Xóa =====
            DataGridViewButtonColumn btnDelete = new DataGridViewButtonColumn();
            btnDelete.Name = "btnDelete";
            btnDelete.HeaderText = "Xóa";
            btnDelete.Text = "X";
            btnDelete.UseColumnTextForButtonValue = true;
            btnDelete.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;

            dgDsLot.Columns.Add(btnDelete);
            dgDsLot.AllowUserToAddRows = false;
            //dgDsLot.CellContentClick += dgDsLot_CellContentClick; // sự kiện cho nút
            dgDsLot.CellClick += dgDsLot_CellClick;               // tùy chọn: nếu muốn bắt click cả ô
        }

        private void cbLot_TextUpdate(object sender, EventArgs e)
        {
            _callTimer = "cbLot";
            timer1.Stop();
            timer1.Start();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            timer1.Stop();

            switch (_callTimer)
            {
                case "cbTenSP":
                    LoadAutoCompleteTenSP(cbTenSP.Text);
                    break;
                 case "cbLot":
                        LoadAutoCompleteLot(cbLot.Text);
                        break;
                default:
                    Console.WriteLine("Lỗi tại function Timer1_tick");
                    MessageBox.Show("Lỗi tại function Timer1_tick");
                    break;
            }

        }

        private void dgDsLot_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && dgDsLot.Columns[e.ColumnIndex].Name == "btnDelete")
            {
                var confirm = MessageBox.Show("Bạn có chắc muốn xóa dòng này?",
                                              "Xác nhận xóa",
                                              MessageBoxButtons.YesNo,MessageBoxIcon.Question);
                if (confirm == DialogResult.Yes)
                {
                    dgDsLot.Rows.RemoveAt(e.RowIndex);
                }
            }
        }

        private void LoadAutoCompleteTenSP(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                //ResetController_TimTenSP();
                cbTenSP.DroppedDown = false;
                return;
            }
            string para = "Ten";

            string query = @"
            SELECT
                DanhSachMaSP.ID AS ID,
                DanhSachMaSP.Ten as ten,
                DanhSachMaSP.Ma as ma
            FROM DanhSachMaSP
            WHERE DanhSachMaSP.Ten LIKE '%' || @" + para + @" || '%';";

            DataTable dsMaSP = DatabaseHelper.GetData(query, keyword,  para);
            cbTenSP.DroppedDown = false;
            cbTenSP.SelectionChangeCommitted -= cbTenSP_SelectionChangeCommitted; // tránh trùng event
            // check data return
            if (dsMaSP.Rows.Count != 0)
            {
                cbTenSP.DataSource = dsMaSP;
                cbTenSP.DisplayMember = "ten";
                string currentText = keyword;
                cbTenSP.DroppedDown = true;
                cbTenSP.Text = currentText;
                cbTenSP.SelectionStart = cbTenSP.Text.Length;
                cbTenSP.SelectionLength = 0;
                cbTenSP.SelectionChangeCommitted += cbTenSP_SelectionChangeCommitted;
            }
        }

        private void cbTenSP_SelectionChangeCommitted(object sender, EventArgs e)
        {
            //ResetController_TimTenSP();
            if (cbTenSP.SelectedItem == null || !(cbTenSP.SelectedItem is DataRowView)) return;
            DataRowView row = (DataRowView)cbTenSP.SelectedItem;
            //string tenSP = row["ten"].ToString();
            //string maSP = row["ma"].ToString();
            string id = row["ID"].ToString();
            cbTenSP.Text = "";
            cbTenSP.SelectedIndex = -1;
            nmIDTenSP.Value = Convert.ToInt32(id);
        }

        private void LoadAutoCompleteLot(string keyword)
        {

            if (string.IsNullOrWhiteSpace(keyword))
            {
                cbLot.DroppedDown = false;
                return;
            }

            string para = "MaBin";
            string query = @"
            SELECT
                TTThanhPham.ID AS ID,
                TTThanhPham.MaBin as lot,
                TTThanhPham.KhoiLuongSau as kl,
                DanhSachMaSP.Ten as ten
            FROM
                TTThanhPham
            INNER JOIN
                DanhSachMaSP ON TTThanhPham.DanhSachSP_ID = DanhSachMaSP.ID
            WHERE
                TTThanhPham.KhoiLuongSau <> 0
                AND TTThanhPham.MaBin LIKE '%' || @" + para + " || '%'";


            DataTable tonKho = DatabaseHelper.GetData(query, keyword, para);

            cbLot.DroppedDown = false;

            cbLot.SelectionChangeCommitted -= cbLot_SelectionChangeCommitted; // tránh trùng event
            // check data return
            if (tonKho.Rows.Count != 0)
            {
                cbLot.DataSource = tonKho;
                cbLot.DisplayMember = "Lot";

                string currentText = keyword;

                cbLot.DroppedDown = true;
                cbLot.Text = currentText;
                cbLot.SelectionStart = cbLot.Text.Length;
                cbLot.SelectionLength = 0;

                cbLot.SelectionChangeCommitted += cbLot_SelectionChangeCommitted;
            }

        }

        private void cbLot_SelectionChangeCommitted(object sender, EventArgs e)
        {
            //ResetController_TimLOT(); 
            decimal kl = 0;

            if (cbLot.SelectedItem == null || !(cbLot.SelectedItem is DataRowView)) return;
            //cbTenSP.Enabled = false;

            DataRowView row = (DataRowView)cbLot.SelectedItem;
            string lotValue = row["Lot"].ToString();

            kl = Convert.ToDecimal(row["kl"]);
            string selectedLot = row["Lot"].ToString();
            string id = row["ID"].ToString();
            string tenSP = row["ten"].ToString();

            cbLot.Text = "";
            cbLot.SelectedIndex = -1;

            nmKl.Value = kl;
            lblTenSP.Text = tenSP;

            DataRowView dong = (DataRowView)row;

            bool isDuplicate = dgDsLot.Rows.Cast<DataGridViewRow>()
                .Any(r => r.Cells["ID"].Value?.ToString() == id);

            if (!isDuplicate)
            {
                object[] values = row.Row.ItemArray;
                dgDsLot.Rows.Add(values);
            }
            else
            {
                MessageBox.Show("Lô này đã được thêm vào danh sách.");
            }
        }

        private void may_SelectedIndexChanged(object sender, EventArgs e)
        {
            lblLot.Text = CoreHelper.LOTGenerated(may, maHT, STTCD, sttBin, soBin);
        }

        private void maHT_ValueChanged(object sender, EventArgs e)
        {
            lblLot.Text = CoreHelper.LOTGenerated(may, maHT, STTCD, sttBin, soBin);
        }

        private void STTCD_SelectedIndexChanged(object sender, EventArgs e)
        {
            lblLot.Text = CoreHelper.LOTGenerated(may, maHT, STTCD, sttBin, soBin);
        }

        private void sttBin_ValueChanged(object sender, EventArgs e)
        {
            lblLot.Text = CoreHelper.LOTGenerated(may, maHT, STTCD, sttBin, soBin);
        }

        private void soBin_ValueChanged(object sender, EventArgs e)
        {
            lblLot.Text = CoreHelper.LOTGenerated(may, maHT, STTCD, sttBin, soBin);
        }

        private void cbTenSP_TextUpdate(object sender, EventArgs e)
        {
            _callTimer = "cbTenSP";
            timer1.Stop();
            timer1.Start();
        }

        private void btnGop_Click(object sender, EventArgs e)
        {
            ConfigDB configDB = DatabaseHelper.GetConfig();

            if (!configDB.Active)
            {
                MessageBox.Show(configDB.Message, "THÔNG BÁO", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Lấy danh sách ID từ DataGridView
            var ids = dgDsLot.Rows
               .Cast<DataGridViewRow>()
               .Where(r => !r.IsNewRow && r.Cells["ID"].Value != null)
               .Select(r => Convert.ToInt64(r.Cells["ID"].Value))
               .ToList();

            var tongKL = dgDsLot.Rows
                .Cast<DataGridViewRow>()
                .Where(r => !r.IsNewRow && r.Cells["kl"].Value != null)
                .Sum(r => Convert.ToDecimal(r.Cells["kl"].Value));


            if (lblLot.Text == "")
            {
                MessageBox.Show("LOT chưa hợp lệ.", "THÔNG BÁO", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (ids.Count == 0 || nmIDTenSP.Value == 0)
            {
                MessageBox.Show("Kiểm tra lại danh sách LOT hoặc Tên Sản Phẩm", "THÔNG BÁO", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (nmKLSP.Value == 0)
            {
                MessageBox.Show("Khối lượng sản phẩm không hợp lệ.", "THÔNG BÁO", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (tbNguoiLam.Text == "")
            {
                MessageBox.Show("Người làm không được trống.", "THÔNG BÁO", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }





            #region Áp dụng cho version 1

            TonKho tonKhoNew = new TonKho
            {
                MaSP_ID = Convert.ToInt32(nmIDTenSP.Value),
                Lot = lblLot.Text,
                KhoiLuongConLai = nmKLSP.Value,
                KhoiLuongDauVao = nmKLSP.Value,
                HanNoi = 0,
                ChieuDai = nbChieuDai.Value,
            };

            DL_CD_Ben dL_CD_Ben = new DL_CD_Ben
            {
                Ngay = CoreHelper.GetNgayHienTai(),
                Ca = CoreHelper.GetShiftValue(),
                NguoiLam = tbNguoiLam.Text,
                SoMay = "Hàn nối",
                GhiChu = "Hàn nối",
                KLHanNoi = tongKL,
            };

            bool isUpdateSuccess = DatabasehelperVer01.InsertVaUpdateTonKho_GopLot(tonKhoNew, dL_CD_Ben, ids);

            #endregion
        }

        private void ResetAllController()
        {
            tbNguoiLam.Text = "";
            nbChieuDai.Value = 0;
            may.SelectedIndex = -1;
            maHT.Value = 0;
            STTCD.SelectedIndex = -1;
            sttBin.Value = 0;
            soBin.Value = 0;
            lblLot.Text = "";
            nmIDTenSP.Value = 0;
            cbTenSP.Text = "";
            nmKLSP.Value = 0;
            dgDsLot.Rows.Clear();
            lblTenSP.Text = "";
            nmKl.Value = 0;
            lblLot.Text = "";
            cbLot.Text = "";
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            ResetAllController();
        }

        private async void btnDsGopBin_Click(object sender, EventArgs e)
        {
            string query = @"
            SELECT
                parent.ID                   AS ID_HienTai,
                parent.Lot                  AS Lot_HienTai,
                parent.KhoiLuongDauVao      AS KL_HienTai,
                spp.Ten                     AS TenSP_HienTai,   
                child.ID                    AS ID_HanNoi,
                child.Lot 					AS Lot_HanNoi, 
                sp.Ten                      AS Ten_HanNoi,   
                child.ChieuDai
            FROM TonKho AS child
            JOIN TonKho AS parent
                ON parent.ID = child.HanNoi
            LEFT JOIN DanhSachMaSP AS sp
                ON sp.ID = child.MaSP_ID
            LEFT JOIN DanhSachMaSP AS spp      
                ON spp.ID = parent.MaSP_ID
            LEFT JOIN (
                SELECT TonKho_ID, MAX(Ngay) AS Ngay
                FROM DL_CD_Boc
                GROUP BY TonKho_ID
            ) AS boc
                ON boc.TonKho_ID = child.ID
            WHERE child.HanNoi <> 0
            ORDER BY parent.ID  DESC, child.ID;
            ";

            DataTable table = DatabasehelperVer01.GetDataFromSQL(query);

            if (table.Rows.Count < 1)
            {
                MessageBox.Show("Không có dữ liệu", "THÔNG BÁO", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!cbXuatExcel.Checked)
            {
                OnDataReady?.Invoke(table);
                return;
            }


            cbXuatExcel.Checked = false;
            
            cbXuatExcel.Checked = true;


            string defaultFileName = "DanhSachGopBin";
            //await ExcelHelper.ExportWithLoading(table, defaultFileName);

        }
               
    }
}
