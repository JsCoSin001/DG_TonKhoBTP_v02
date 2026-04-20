using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

namespace DG_TonKhoBTP_v02.UI
{
    public class UC_MonthyReport : Form
    {
        private DataGridView dataGridView1;
        private Panel filterPanel;
        private DataView _dataView;
        private TextBox[] _filterBoxes;

        public UC_MonthyReport()
        {
            SetupForm();
            SetupLayout();
        }

        private void SetupForm()
        {
            this.Text = "Báo cáo sản xuất tháng";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimizeBox = false;
            this.MaximizeBox = false;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.ControlBox = true;
            this.ShowIcon = false;
            this.Padding = new Padding(5);
            this.WindowState = FormWindowState.Maximized;
        }

        private void SetupLayout()
        {
            // Panel chứa các textbox lọc
            filterPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 35,
                BackColor = Color.WhiteSmoke
            };

            dataGridView1 = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells,
                RowHeadersVisible = false,
                BackgroundColor = Color.White
            };
            dataGridView1.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 12F, FontStyle.Regular);
            dataGridView1.DefaultCellStyle.Font = new Font("Segoe UI", 10F, FontStyle.Regular);
            dataGridView1.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridView1.RowTemplate.Height = 35;

            // Quan trọng: lắng nghe sự kiện scroll ngang để đồng bộ textbox
            dataGridView1.Scroll += (s, e) => AlignFilterBoxes();
            dataGridView1.ColumnWidthChanged += (s, e) => AlignFilterBoxes();

            this.Controls.Add(dataGridView1);      // Fill trước
            this.Controls.Add(filterPanel);        // Top sau (winform add sau = hiển thị trên)
        }

        public void LoadData(DataTable table)
        {
            if (table == null)
            {
                FrmWaiting.ShowGifAlert("Không có dữ liệu để hiển thị");
                return;
            }

            _dataView = table.DefaultView;
            dataGridView1.DataSource = _dataView;

            BuildFilterBoxes(table);
        }

        private void BuildFilterBoxes(DataTable table)
        {
            filterPanel.Controls.Clear();
            _filterBoxes = new TextBox[table.Columns.Count];

            for (int i = 0; i < table.Columns.Count; i++)
            {
                int colIndex = i; // capture cho lambda
                var tb = new TextBox
                {
                    Tag = table.Columns[i].ColumnName,
                    Font = new Font("Segoe UI", 9F),
                    BorderStyle = BorderStyle.FixedSingle
                };
                tb.TextChanged += (s, e) => ApplyFilter();

                filterPanel.Controls.Add(tb);
                _filterBoxes[i] = tb;
            }

            // Căn vị trí sau khi grid đã render
            dataGridView1.ColumnAdded += (s, e) => AlignFilterBoxes();
            this.Shown += (s, e) => AlignFilterBoxes();
        }

        private void AlignFilterBoxes()
        {
            if (_filterBoxes == null) return;

            for (int i = 0; i < _filterBoxes.Length; i++)
            {
                if (i >= dataGridView1.Columns.Count) break;

                var col = dataGridView1.Columns[i];
                // GetColumnDisplayRectangle trả về vị trí thực tế trên lưới
                var rect = dataGridView1.GetColumnDisplayRectangle(col.Index, true);

                _filterBoxes[i].SetBounds(
                    dataGridView1.Left + rect.Left,
                    2,
                    rect.Width,
                    filterPanel.Height - 4
                );
                _filterBoxes[i].Visible = rect.Width > 0; // ẩn nếu cột bị cuộn ra ngoài
            }
        }

        private void ApplyFilter()
        {
            if (_dataView == null || _filterBoxes == null) return;

            var conditions = new System.Collections.Generic.List<string>();

            foreach (var tb in _filterBoxes)
            {
                if (string.IsNullOrWhiteSpace(tb.Text)) continue;

                string colName = tb.Tag.ToString();
                // Escape dấu nháy đơn
                string value = tb.Text.Replace("'", "''");
                conditions.Add($"CONVERT([{colName}], System.String) LIKE '%{value}%'");
            }

            _dataView.RowFilter = string.Join(" AND ", conditions);
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.ClientSize = new System.Drawing.Size(594, 354);
            this.Name = "UC_MonthyReport";
            this.ResumeLayout(false);
        }
    }
}