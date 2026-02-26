using DG_TonKhoBTP_v02.Database;
using DG_TonKhoBTP_v02.Helper.Reuseable;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DG_TonKhoBTP_v02.UI.NghiepVuKhac.KeToan
{
    public partial class UC_MuaVatTu : UserControl
    {
        private AutoCompleteDropDown dropDown;
        private int searchColumnIndex = 1; // Cột thứ 2 (index = 1)

        public UC_MuaVatTu()
        {
            InitializeComponent();
            InitDropDown();
            SetupGrid();
        }

        private void SetupGrid()
        {           
            // Events
            dgvDSMua.EditingControlShowing += Grid_EditingControlShowing;
            dgvDSMua.CellEndEdit += Grid_CellEndEdit;
        }

        // ===================== Dropdown =====================
        private void InitDropDown()
        {
            dropDown = new AutoCompleteDropDown();
            dropDown.RowSelected += (row) => {
                int rowIdx = dgvDSMua.CurrentCell.RowIndex;
                dgvDSMua.Rows[rowIdx].Cells["ma"].Value = row["Ma"];
                dgvDSMua.Rows[rowIdx].Cells["ten"].Value = row["Ten"];
                dgvDSMua.Rows[rowIdx].Cells["donVi"].Value = row["DonVi"];
                dropDown.Hide();
            };
        }

        private DataTable SearchProducts(string keyword)
        {
            string query = "SELECT Id, Ten, Ma, DonVi FROM DanhSachMaSP WHERE Ten LIKE '%' || @keyword || '%' LIMIT 50";
            return DatabaseHelper.GetData(query, keyword, "keyword");
        }

        private TextBox currentEditBox;

        private void Grid_EditingControlShowing(object sender,
            DataGridViewEditingControlShowingEventArgs e)
        {
            // Chỉ xử lý cột tìm kiếm
            if (dgvDSMua.CurrentCell?.ColumnIndex != searchColumnIndex) return;

            if (e.Control is TextBox tb)
            {
                // Hủy event cũ tránh duplicate
                if (currentEditBox != null)
                    currentEditBox.TextChanged -= SearchBox_TextChanged;

                currentEditBox = tb;
                currentEditBox.TextChanged += SearchBox_TextChanged;
            }
        }

        private void SearchBox_TextChanged(object sender, EventArgs e)
        {
            var tb = sender as TextBox;
            string keyword = tb?.Text?.Trim();

            if (string.IsNullOrEmpty(keyword))
            {
                dropDown.Hide();
                return;
            }

            var dt = SearchProducts(keyword);
            if (dt.Rows.Count == 0)
            {
                dropDown.Hide();
                return;
            }


            Console.WriteLine($"Rows: {dt.Rows.Count}");
            foreach (DataColumn col in dt.Columns)
                Console.WriteLine($"Column: {col.ColumnName}");



            // Tính vị trí hiển thị dropdown
            var cell = dgvDSMua.CurrentCell;
            var cellRect = dgvDSMua.GetCellDisplayRectangle(
                cell.ColumnIndex, cell.RowIndex, false);
            var screenPos = dgvDSMua.PointToScreen(
                new Point(cellRect.Left, cellRect.Bottom));

            dropDown.LoadData(dt, "Ten");
            dropDown.Location = screenPos;
            dropDown.Width = Math.Max(cellRect.Width, 250);

            // Debug size
            Console.WriteLine($"Dropdown size: {dropDown.Width} x {dropDown.Height}");
            Console.WriteLine($"Location: {dropDown.Location}");

            if (!dropDown.Visible)
                dropDown.Show();

            dropDown.BringToFront();
            tb.Focus();
        }

        private void Grid_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            dropDown.Hide();
        }

        // ===================== Chọn từ Dropdown =====================
        // Gọi hàm này khi user nhấn Enter/DoubleClick trên dropdown
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Down && dropDown.Visible)
            {
                dropDown.Focus();
                return true;
            }

            if (keyData == Keys.Enter && dropDown.Visible)
            {
                FillRowFromDropDown();
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void FillRowFromDropDown()
        {
            var row = dropDown.GetSelected();
            if (row == null) return;

            int rowIdx = dgvDSMua.CurrentCell.RowIndex;

            dgvDSMua.Rows[rowIdx].Cells["ma"].Value = row["Ma"];
            dgvDSMua.Rows[rowIdx].Cells["ten"].Value = row["ten"];
            dgvDSMua.Rows[rowIdx].Cells["donVi"].Value = row["DonVi"];

            dropDown.Hide();

            // Chuyển focus sang cột tiếp theo
            //dgvDSMua.CurrentCell =
            //    dgvDSMua.Rows[rowIdx].Cells["colPrice"];
        }
    }
}
