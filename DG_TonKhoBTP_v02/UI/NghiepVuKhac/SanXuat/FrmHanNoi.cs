using DG_TonKhoBTP_v02.Database;
using DG_TonKhoBTP_v02.DL_Ben;
using DG_TonKhoBTP_v02.Models;
using DocumentFormat.OpenXml;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Input;
using CoreHelper = DG_TonKhoBTP_v02.Helper.Helper;



namespace DG_TonKhoBTP_v02.UI.Actions
{
    public partial class FrmHanNoi : Form
    {
        public event Action<DataTable> OnDataReady;
        public CongDoan CongDoan { get; private set; }
        private string _callTimer;

        public FrmHanNoi()
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

            dgDsLot.Columns["kl"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            dgDsLot.Columns["kl"].Width = 150; // chỉnh số theo ý bạn


            // ===== Thêm cột nút Xóa =====
            DataGridViewButtonColumn btnDelete = new DataGridViewButtonColumn();
            btnDelete.Name = "btnDelete";
            btnDelete.HeaderText = "Xóa";
            btnDelete.Text = "X";
            btnDelete.UseColumnTextForButtonValue = true;
            btnDelete.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            btnDelete.Width = 60; // chỉnh kích thước mong muốn

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
                        //LoadAutoCompleteLot(cbLot.Text);
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
            //string maSP = row["ma"].ToString();
            string id = row["ID"].ToString();
            cbTenSP.Text = "";
            tbTen.Text = row["ten"].ToString();
            cbTenSP.SelectedIndex = -1;
            nmIDTenSP.Value = Convert.ToInt32(id);
            nbChieuDai.Focus();
            nbChieuDai.Select(0, nbChieuDai.Text.Length);
        }

        private void TimDL(string keyword)
        {
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
                AND TTThanhPham.MaBin = @MaBin";

            DataTable tonKho = DatabaseHelper.GetData(query, keyword, para);

            // 1) Tạo bộ nhớ để kiểm tra trùng lot (keyword)
            var existedLots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // 2) Lấy các lot đã có sẵn trên DataGridView (để tránh add trùng)
            foreach (DataGridViewRow r in dgDsLot.Rows)
            {
                if (r.IsNewRow) continue;
                var lotVal = r.Cells["lot"].Value?.ToString();   // "lot" là Name cột của bạn
                if (!string.IsNullOrWhiteSpace(lotVal))
                    existedLots.Add(lotVal.Trim());
            }

            // 3) Đổ thêm dữ liệu mới, chỉ add nếu lot chưa tồn tại
            foreach (DataRow dr in tonKho.Rows)
            {
                string lot = dr["lot"]?.ToString()?.Trim();
                if (string.IsNullOrWhiteSpace(lot)) continue;

                if (existedLots.Add(lot)) // Add thành công => chưa trùng
                {
                    int idx = dgDsLot.Rows.Add();

                    // Lưu ý: "ID", "lot", "kl", "ten" là Name cột trên dgDsLot
                    dgDsLot.Rows[idx].Cells["ID"].Value = dr["ID"];
                    dgDsLot.Rows[idx].Cells["lot"].Value = dr["lot"];
                    dgDsLot.Rows[idx].Cells["kl"].Value = dr["kl"];
                    dgDsLot.Rows[idx].Cells["ten"].Value = dr["ten"];
                }
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

            Console.WriteLine("");


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
            lblLot.Text = "";
            tbLotGop.Text = "";
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            ResetAllController();
        }

        private async void btnDsGopBin_Click(object sender, EventArgs e)
        {
            //string query = @"
            //SELECT
            //    parent.ID                   AS ID_HienTai,
            //    parent.Lot                  AS Lot_HienTai,
            //    parent.KhoiLuongDauVao      AS KL_HienTai,
            //    spp.Ten                     AS TenSP_HienTai,   
            //    child.ID                    AS ID_HanNoi,
            //    child.Lot 					AS Lot_HanNoi, 
            //    sp.Ten                      AS Ten_HanNoi,   
            //    child.ChieuDai
            //FROM TonKho AS child
            //JOIN TonKho AS parent
            //    ON parent.ID = child.HanNoi
            //LEFT JOIN DanhSachMaSP AS sp
            //    ON sp.ID = child.MaSP_ID
            //LEFT JOIN DanhSachMaSP AS spp      
            //    ON spp.ID = parent.MaSP_ID
            //LEFT JOIN (
            //    SELECT TonKho_ID, MAX(Ngay) AS Ngay
            //    FROM DL_CD_Boc
            //    GROUP BY TonKho_ID
            //) AS boc
            //    ON boc.TonKho_ID = child.ID
            //WHERE child.HanNoi <> 0
            //ORDER BY parent.ID  DESC, child.ID;
            //";

            //DataTable table = DatabasehelperVer01.GetDataFromSQL(query);

            //if (table.Rows.Count < 1)
            //{
            //    MessageBox.Show("Không có dữ liệu", "THÔNG BÁO", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            //    return;
            //}

            //if (!cbXuatExcel.Checked)
            //{
            //    OnDataReady?.Invoke(table);
            //    return;
            //}


            //cbXuatExcel.Checked = false;
            
            //cbXuatExcel.Checked = true;


            //string defaultFileName = "DanhSachGopBin";
            //await ExcelHelper.ExportWithLoading(table, defaultFileName);

        }


        private void tbLotGop_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter) return;

            string lot = tbLotGop.Text.Trim();

            if (lot == "")
            {
                FrmWaiting.ShowGifAlert("LOT hàn nối chưa được nhập");
                return;
            }
            tbLotGop.Text = "";

            TimDL(lot);
        }
    }
}
