using DG_TonKhoBTP_v02.Database.SanXuat;
using DG_TonKhoBTP_v02.Models;
using DG_TonKhoBTP_v02.Models.SanXuat;
using DG_TonKhoBTP_v02.UI.Helper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DG_TonKhoBTP_v02.UI
{
    /// <summary>Báo cáo tháng: một dòng cho mỗi thành phẩm, hai nút xem chi tiết.</summary>
    public class UC_MonthyReport : Form
    {
        private readonly DataGridView dataGridView1;
        private readonly Panel filterPanel;
        private readonly Dictionary<string, TextBox> _filterBoxes = new Dictionary<string, TextBox>();
        private DataView? _dataView;
        private CongDoan? _congDoan;
        private bool _detailsBusy;

        public UC_MonthyReport()
        {
            Text = "Báo cáo sản xuất tháng";
            StartPosition = FormStartPosition.CenterScreen;
            ShowIcon = false;
            Padding = new Padding(5);
            WindowState = FormWindowState.Maximized;
            MinimumSize = new Size(900, 480);

            filterPanel = new Panel { Dock = DockStyle.Top, Height = 35, BackColor = Color.WhiteSmoke };
            dataGridView1 = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                AutoGenerateColumns = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                ColumnHeadersHeight = 40,
                RowHeadersVisible = false,
                BackgroundColor = Color.White,
                SelectionMode = DataGridViewSelectionMode.CellSelect,
                MultiSelect = false
            };
            dataGridView1.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 11F);
            dataGridView1.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.True;
            dataGridView1.DefaultCellStyle.Font = new Font("Segoe UI", 10F);
            dataGridView1.RowTemplate.Height = 35;
            dataGridView1.Scroll += (s, e) => AlignFilterBoxes();
            dataGridView1.ColumnWidthChanged += (s, e) => AlignFilterBoxes();
            dataGridView1.ColumnDisplayIndexChanged += (s, e) => AlignFilterBoxes();
            dataGridView1.CellContentClick += OnDetailClick;
            Shown += (s, e) => AlignFilterBoxes();
            Resize += (s, e) => AlignFilterBoxes();
            Controls.Add(dataGridView1);
            Controls.Add(filterPanel);
            ConfigureColumns();
        }

        // Chiều rộng cố định của từng cột được truyền tại ConfigureColumns().
        // Chỉ MaBin và TenSanPham sẽ đổi sang AllCells; GhiChu sang Fill.
        private static DataGridViewTextBoxColumn TextColumn(string key, string title, string format = "", int width = 120)
        {
            var col = new DataGridViewTextBoxColumn
            {
                Name = key, DataPropertyName = key, HeaderText = title,
                SortMode = DataGridViewColumnSortMode.Automatic,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                Width = width
            };
            if (format.Length != 0) col.DefaultCellStyle.Format = format;
            return col;
        }

        private void ConfigureColumns()
        {
            dataGridView1.Columns.Clear();
            dataGridView1.Columns.Add(TextColumn("STT", "STT", width: 75));
            dataGridView1.Columns.Add(TextColumn("NgayKetThuc", "Ngày hạ lô", "dd/MM/yyyy", width: 130));
            dataGridView1.Columns.Add(TextColumn("CaKetThuc", "Ca hạ lô", width: 90));
            dataGridView1.Columns.Add(TextColumn("May", "Máy", width: 80));
            dataGridView1.Columns.Add(TextColumn("NguoiKetThuc", "Người hạ lô", width: 145));
            dataGridView1.Columns.Add(TextColumn("MaBin", "Mã bin"));
            dataGridView1.Columns.Add(TextColumn("TenSanPham", "Tên sản phẩm"));
            dataGridView1.Columns.Add(TextColumn("KhoiLuongSau", "Khối lượng sau", "#,##0.##", width: 135));
            dataGridView1.Columns.Add(TextColumn("ChieuDaiSau", "Chiều dài sau", "#,##0.##", width: 135));
            dataGridView1.Columns.Add(TextColumn("DonVi", "ĐVT", width: 70));
            dataGridView1.Columns.Add(TextColumn("TrangThai", "Trạng thái", width: 115));
            dataGridView1.Columns.Add(TextColumn("GhiChu", "Ghi chú"));
            // Hai cột nhận diện vừa theo toàn bộ các dòng (không chỉ dòng đang nhìn thấy).
            dataGridView1.Columns["MaBin"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            dataGridView1.Columns["TenSanPham"].MinimumWidth = 200;

            dataGridView1.Columns["TenSanPham"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            // Ghi chú sử dụng toàn bộ phần bề rộng còn lại, không ép các cột khác giãn ra.
            dataGridView1.Columns["GhiChu"].MinimumWidth = 200;
            dataGridView1.Columns["GhiChu"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            dataGridView1.Columns.Add(new DataGridViewButtonColumn
            {
                Name = "ViewMaterials", HeaderText = "Chi tiết NVL", Text = "Xem NVL",
                UseColumnTextForButtonValue = true, SortMode = DataGridViewColumnSortMode.NotSortable,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None, Width = 110
            });
            dataGridView1.Columns.Add(new DataGridViewButtonColumn
            {
                Name = "ViewShift", HeaderText = "Chi tiết ca", Text = "Xem ca",
                UseColumnTextForButtonValue = true, SortMode = DataGridViewColumnSortMode.NotSortable,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None, Width = 100
            });
        }

        public void LoadData(IReadOnlyList<MonthyReport_Model> rows, CongDoan congDoan)
        {
            if (rows == null) throw new ArgumentNullException(nameof(rows));
            _congDoan = congDoan ?? throw new ArgumentNullException(nameof(congDoan));

            var table = new DataTable("MonthyReport");
            table.Columns.Add("STT", typeof(long));
            table.Columns.Add("NgayKetThuc", typeof(DateTime));
            table.Columns.Add("CaKetThuc", typeof(string));
            table.Columns.Add("May", typeof(string));
            table.Columns.Add("NguoiKetThuc", typeof(string));
            table.Columns.Add("MaBin", typeof(string));
            table.Columns.Add("TenSanPham", typeof(string));
            table.Columns.Add("KhoiLuongSau", typeof(double));
            table.Columns.Add("ChieuDaiSau", typeof(double));
            table.Columns.Add("DonVi", typeof(string));
            table.Columns.Add("TrangThai", typeof(string));
            table.Columns.Add("GhiChu", typeof(string));
            // Chỉ để tìm kiếm theo cách người dùng nhìn thấy ngày; không sinh cột trên lưới.
            table.Columns.Add("NgayKetThucLoc", typeof(string));

            foreach (var row in rows)
            {
                var rec = table.NewRow();
                rec["STT"] = row.Id;
                rec["NgayKetThuc"] = DisplayValue(row.NgayKetThuc);
                rec["CaKetThuc"] = DisplayValue(row.CaKetThuc);
                rec["May"] = DisplayValue(row.May);
                rec["NguoiKetThuc"] = DisplayValue(row.NguoiKetThuc);
                rec["MaBin"] = DisplayValue(row.MaBin);
                rec["TenSanPham"] = DisplayValue(row.TenSanPham);
                rec["KhoiLuongSau"] = DisplayValue(row.KhoiLuongSau);
                rec["ChieuDaiSau"] = DisplayValue(row.ChieuDaiSau);
                rec["DonVi"] = DisplayValue(row.DonVi);
                rec["TrangThai"] = row.Temp == 0 ? "Chính thức" : row.Temp == 1 ? "Tạm thời" : "";
                rec["GhiChu"] = DisplayValue(row.GhiChu);
                rec["NgayKetThucLoc"] = row.NgayKetThuc?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) ?? "";
                table.Rows.Add(rec);
            }

            _dataView = new DataView(table) { Sort = "[NgayKetThuc] DESC, [STT] DESC" };
            dataGridView1.DataSource = _dataView;
            BuildFilterBoxes();
        }

        // Chỉ làm trống ở tầng hiển thị, không thay đổi DTO hay dữ liệu SQLite.
        internal static object DisplayValue(object? value)
        {
            if (value == null || value == DBNull.Value) return DBNull.Value;
            if (value is string s && s == "-1") return DBNull.Value;
            if (value is double d && d == -1) return DBNull.Value;
            if (value is float f && f == -1) return DBNull.Value;
            if (value is decimal dec && dec == -1) return DBNull.Value;
            if (value is int i && i == -1) return DBNull.Value;
            if (value is long l && l == -1) return DBNull.Value;
            return value;
        }

        private void BuildFilterBoxes()
        {
            foreach (var old in _filterBoxes.Values) { filterPanel.Controls.Remove(old); old.Dispose(); }
            _filterBoxes.Clear();
            foreach (DataGridViewColumn col in dataGridView1.Columns)
            {
                if (col is DataGridViewButtonColumn) continue;
                var box = new TextBox
                {
                    Name = "Filter_" + col.Name,
                    Tag = col.DataPropertyName,
                    Font = new Font("Segoe UI", 9F),
                    BorderStyle = BorderStyle.FixedSingle
                };
                box.TextChanged += (s, e) => ApplyFilter();
                filterPanel.Controls.Add(box);
                _filterBoxes.Add(col.Name, box);
            }
            AlignFilterBoxes();
        }

        private void AlignFilterBoxes()
        {
            if (dataGridView1.IsDisposed) return;
            foreach (var item in _filterBoxes)
            {
                var col = dataGridView1.Columns[item.Key];
                var rect = dataGridView1.GetColumnDisplayRectangle(col.Index, true);
                var box = item.Value;
                box.SetBounds(dataGridView1.Left + rect.Left - filterPanel.Left, 2,
                    rect.Width, filterPanel.Height - 4);
                box.Visible = col.Visible && rect.Width > 0;
            }
        }

        private static string EscapeFilter(string value)
        {
            // Escaping từng ký tự tránh escape lại những dấu ngoặc mới được thêm vào.
            var escaped = new StringBuilder(value.Length);
            foreach (char ch in value)
            {
                switch (ch)
                {
                    case '\'': escaped.Append("''"); break;
                    case '%': escaped.Append("[%]"); break;
                    case '*': escaped.Append("[*]"); break;
                    case '[': escaped.Append("[[]"); break;
                    case ']': escaped.Append("[]]"); break;
                    default: escaped.Append(ch); break;
                }
            }
            return escaped.ToString();
        }

        private void ApplyFilter()
        {
            if (_dataView == null) return;
            var conditions = new List<string>();
            foreach (var filter in _filterBoxes)
            {
                string term = filter.Value.Text.Trim();
                if (term.Length == 0) continue;
                string dataName = filter.Key == "NgayKetThuc" ? "NgayKetThucLoc" : filter.Value.Tag.ToString();
                // Bộ lọc chỉ gắn vào các cột dữ liệu, không có ô lọc cho nút.
                conditions.Add("CONVERT([" + dataName + "], 'System.String') LIKE '%" + EscapeFilter(term) + "%'");
            }
            _dataView.RowFilter = string.Join(" AND ", conditions);
        }

        private async void OnDetailClick(object sender, DataGridViewCellEventArgs e)
        {
            if (_detailsBusy || IsDisposed || e.RowIndex < 0 || e.ColumnIndex < 0 || _congDoan == null) return;
            string action = dataGridView1.Columns[e.ColumnIndex].Name;
            if (action != "ViewMaterials" && action != "ViewShift") return;
            // ID lấy theo dòng đang được bind, không dựa vào chỉ số hàng (có thể đã lọc/sắp xếp).
            if (!(dataGridView1.Rows[e.RowIndex].DataBoundItem is DataRowView selected)) return;
            long id = Convert.ToInt64(selected["STT"], CultureInfo.InvariantCulture);
            string maBin = Convert.ToString(selected["MaBin"], CultureInfo.InvariantCulture) ?? "";
            _detailsBusy = true;
            try
            {
                if (action == "ViewMaterials")
                {
                    var rows = await WaitingHelper.RunWithWaiting(
                        () => Task.Run(() => MonthyReport_DB.GetMaterialDetails(id)), "ĐANG TẢI CHI TIẾT NGUYÊN LIỆU...");
                    if (IsDisposed || Disposing) return;
                    if (rows.Count == 0)
                    {
                        FrmWaiting.ShowGifAlert("Không có NVL cho thành phẩm này.");
                        return;
                    }
                    using (var detail = new Frm_MonthyReportDetail(this))
                    {
                        detail.LoadMaterials(rows, _congDoan, id, maBin);
                        detail.ShowDialog(this);
                    }
                }
                else
                {
                    var rows = await WaitingHelper.RunWithWaiting(
                        () => Task.Run(() => MonthyReport_DB.GetShiftDetails(id)), "ĐANG TẢI CHI TIẾT CA LÀM VIỆC...");
                    if (IsDisposed || Disposing) return;
                    if (rows.Count == 0)
                    {
                        FrmWaiting.ShowGifAlert("Không có thông tin ca làm việc cho thành phẩm này.");
                        return;
                    }
                    using (var detail = new Frm_MonthyReportDetail(this))
                    {
                        detail.LoadShift(rows, id, maBin);
                        detail.ShowDialog(this);
                    }
                }
            }
            catch (Exception ex)
            {
                if (!IsDisposed && !Disposing)
                    FrmWaiting.ShowGifAlert("Không thể tải chi tiết: " + ex.Message, "LỖI");
            }
            finally
            {
                _detailsBusy = false;
            }
        }
    }
}
