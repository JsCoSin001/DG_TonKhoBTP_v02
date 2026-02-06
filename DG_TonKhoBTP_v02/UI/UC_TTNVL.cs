using DG_TonKhoBTP_v02.Core;
using DG_TonKhoBTP_v02.Database;
using DG_TonKhoBTP_v02.Dictionary;
using DG_TonKhoBTP_v02.Helper.Reuseable;
using DG_TonKhoBTP_v02.Models;
using DG_TonKhoBTP_v02.UI.Helper;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Color = System.Drawing.Color;
using CoreHelper = DG_TonKhoBTP_v02.Helper.Helper;

namespace DG_TonKhoBTP_v02.UI
{
    public partial class UC_TTNVL : UserControl, IFormSection, IDataReceiver
    {
        //private CancellationTokenSource _searchCts;

        List<ColumnDefinition> _columns;

        public decimal? klDongThua = null;

        public Func<decimal> GetKhoiLuong { get; set; }

        public Func<string> GetTenMay { get; set; }

        private bool _warnedThisFocus = false;

        public Action FocusKhoiLuong { get; set; }

        bool isShow = false;
        int tongCotCanHide = 10;

        public bool RawMaterial { get; set; } = false;
        public void SetStatusRawMaterial(bool value) => RawMaterial = value;

        //void HideRow(TableLayoutPanel tlp, int row)
        //{
        //    if (row < 0 || row >= tlp.RowCount) return;

        //    // ẩn tất cả control trong row
        //    for (int col = 0; col < tlp.ColumnCount; col++)
        //    {
        //        var ctl = tlp.GetControlFromPosition(col, row);
        //        if (ctl != null) ctl.Visible = false;
        //    }

        //    tlp.RowStyles[row].SizeType = SizeType.Absolute;
        //    tlp.RowStyles[row].Height = 0;
        //}

        void ShowRow(TableLayoutPanel tlp, int row, float height)
        {
            if (row < 0 || row >= tlp.RowCount) return;

            // hiện tất cả control trong row
            for (int col = 0; col < tlp.ColumnCount; col++)
            {
                var ctl = tlp.GetControlFromPosition(col, row);
                if (ctl != null) ctl.Visible = true;
            }

            tlp.RowStyles[row].SizeType = SizeType.Absolute; // hoặc AutoSize/Percent tùy layout của bạn
            tlp.RowStyles[row].Height = height;
        }

        public UC_TTNVL(List<ColumnDefinition> columns)
        {
            InitializeComponent();

            setVisibleTableNVL(true);

            _columns = columns;

            // BẢO HIỂM: mỗi lần bind xong sẽ ép thứ tự theo _columns và Delete cuối
            dtgTTNVL.DataBindingComplete += (s, e) =>
            {
                EnsureColumnOrderAndDeleteLast();
            };


            TaoBang(columns);

            //HideRow(tbTimKiem, 1);
            //HideRow(tbTimKiem, 2);            

            // Bắt lỗi nhập sai định dạng
            dtgTTNVL.DataError += dtgTTNVL_DataError;

            // Hạn chế nhập ký tự không hợp lệ cho các cột số
            dtgTTNVL.EditingControlShowing += dtgTTNVL_EditingControlShowing;

            DebugPrintColumnsByDefinitions();
        }

        // ===================== FIX CORE: luôn ép thứ tự cột & Delete cuối =====================
        private void EnsureColumnOrderAndDeleteLast()
        {
            if (dtgTTNVL.Columns == null || dtgTTNVL.Columns.Count == 0) return;

            // Ép thứ tự hiển thị theo _columns (theo TÊN, không theo index)
            for (int i = 0; i < _columns.Count; i++)
            {
                string name = _columns[i].Name;
                if (dtgTTNVL.Columns.Contains(name))
                    dtgTTNVL.Columns[name].DisplayIndex = i;
            }

            // Đảm bảo Delete tồn tại và luôn nằm cuối
            EnsureDeleteColumnLast();
        }

        private void EnsureDeleteColumnLast()
        {
            // Nếu Delete đã tồn tại nhưng lỡ bị nhảy lên đầu, chỉ cần ép DisplayIndex cuối
            if (!dtgTTNVL.Columns.Contains("Delete"))
            {
                DataGridViewButtonColumn btnDelete = new DataGridViewButtonColumn
                {
                    Name = "Delete",
                    HeaderText = "",
                    Text = "Xoá",
                    UseColumnTextForButtonValue = true,
                    Width = 60,
                    SortMode = DataGridViewColumnSortMode.NotSortable
                };
                dtgTTNVL.Columns.Add(btnDelete);
            }

            dtgTTNVL.Columns["Delete"].DisplayIndex = dtgTTNVL.Columns.Count - 1;
            dtgTTNVL.Columns["Delete"].SortMode = DataGridViewColumnSortMode.NotSortable;
        }

        private void DebugPrintColumnsByDefinitions()
        {
            // In theo _columns để tránh lệch do Delete / index thay đổi
            for (int i = 0; i < _columns.Count; i++)
            {
                string name = _columns[i].Name;
                if (!dtgTTNVL.Columns.Contains(name))
                {
                    Console.WriteLine($"i={i}, Name={name}, (NOT FOUND IN DGV)");
                    continue;
                }

                var c = dtgTTNVL.Columns[name];
                Console.WriteLine($"i={i}, Name={c.Name}, Header={c.HeaderText}, DisplayIndex={c.DisplayIndex}");
            }

            // In thêm Delete nếu có
            if (dtgTTNVL.Columns.Contains("Delete"))
            {
                var d = dtgTTNVL.Columns["Delete"];
                Console.WriteLine($"(extra) Name={d.Name}, Header={d.HeaderText}, DisplayIndex={d.DisplayIndex}");
            }
        }
        // =====================================================================================

        private void TaoBang(List<ColumnDefinition> columns)
        {
            DataTable dt = new DataTable("ThongTin");

            // Tạo cột từ danh sách
            foreach (var col in columns) dt.Columns.Add(col.Name, col.DataType);

            dtgTTNVL.AutoGenerateColumns = true;
            dtgTTNVL.DataSource = dt;

            // Gọi lại hàm SetColumnHeaders để cấu hình
            SetColumnHeaders(dtgTTNVL, columns);

            // Tuỳ chỉnh style
            dtgTTNVL.DefaultCellStyle.Font = new System.Drawing.Font("Microsoft Sans Serif", 10, FontStyle.Regular);
            dtgTTNVL.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dtgTTNVL.AllowUserToResizeRows = false;

            dtgTTNVL.RowTemplate.Height = 30;

            // Đảm bảo Delete cuối + ép thứ tự theo _columns
            EnsureColumnOrderAndDeleteLast();

            dtgTTNVL.CellClick -= dtgTTNVL_CellClick;
            dtgTTNVL.CellClick += dtgTTNVL_CellClick;
        }

        private void dtgTTNVL_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && dtgTTNVL.Columns.Contains("Delete") &&
                e.ColumnIndex == dtgTTNVL.Columns["Delete"].Index)
            {
                var confirm = MessageBox.Show("Bạn có chắc muốn xoá dòng này?",
                                              "Xác nhận xoá",
                                              MessageBoxButtons.YesNo,
                                              MessageBoxIcon.Question);

                if (confirm == DialogResult.Yes)
                {
                    dtgTTNVL.Rows.RemoveAt(e.RowIndex);
                }
            }
        }

        public void OnSoLOTChanged(string soLot)
        {
            ClearGridKeepHeader();

            string may = soLot?.Split('-')[0] ?? "";


            //if (!EnumStore.dsTenMayBoQuaKiemTraKhoiLuongConLai.Contains(may))
            //{
            //    // hide row 1 và row 2
            //    HideRow(tbTimKiem, 1);
            //    HideRow(tbTimKiem, 2);
            //}
            //else
            //{
            //    // show lại row 1 và row 2
            //    ShowRow(tbTimKiem, 1, 40);
            //    ShowRow(tbTimKiem, 2, 40);
            //}
            
        }

        private void SetColumnHeaders(DataGridView dgv, List<ColumnDefinition> columns)
        {
            // Lấy header từ danh sách cột truyền vào
            string[] headers = columns.Select(c => c.Header).ToArray();

            int defaultWidth = 100;
            int defaulHeight = 30;

            int extraCols = headers.Length - ThongTinChungCongDoan.BaseColumns().Count;

            switch (extraCols)
            {
                case 0:
                    defaultWidth = 150;
                    break;
                case 1:
                case 2:
                    defaultWidth = 100;
                    break;
                case 4:
                    defaultWidth = 70;
                    defaulHeight = 45;
                    break;
            }

            dgv.ColumnHeadersHeight = defaulHeight;
            dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;

            // TẮT SORT cho mọi cột
            for (int i = 0; i < dgv.Columns.Count; i++)
                dgv.Columns[i].SortMode = DataGridViewColumnSortMode.NotSortable;

            // ====== FIX: gán Header theo TÊN cột (không theo index) để không bị lệch khi có Delete ======
            foreach (var def in columns)
            {
                if (!dgv.Columns.Contains(def.Name)) continue;
                dgv.Columns[def.Name].HeaderText = def.Header ?? "";
                dgv.Columns[def.Name].Width = defaultWidth;
            }
            // ==========================================================================================

            // Ẩn/hiện + readonly các cột từ 0 đến tongCotCanHide (tránh đụng Delete)
            var colsByDisplay = dgv.Columns.Cast<DataGridViewColumn>()
                               .OrderBy(c => c.DisplayIndex)
                               .ToList();

            for (int i = 0; i <= tongCotCanHide && i < colsByDisplay.Count; i++)
            {
                if (colsByDisplay[i].Name == "Delete") continue;
                colsByDisplay[i].Visible = isShow;      // isShow=false => ẩn
                colsByDisplay[i].ReadOnly = true;
            }

            // ====== FIX: chọn cột fill theo _columns (tên), tránh bị lệch vì Delete ======
            int fillDefIndex = tongCotCanHide;
            if (fillDefIndex < 0) fillDefIndex = 0;
            if (fillDefIndex > columns.Count - 1) fillDefIndex = columns.Count - 1;

            string fillColName = columns[fillDefIndex+1].Name;
            if (dgv.Columns.Contains(fillColName))
            {
                dgv.Columns[fillColName].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                dgv.Columns[fillColName].ReadOnly = true;
            }
            // ==========================================================================================

            // Style header
            dgv.EnableHeadersVisualStyles = false;
            dgv.ColumnHeadersDefaultCellStyle.Font = new System.Drawing.Font("Microsoft Sans Serif", 10, FontStyle.Regular);
            dgv.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            // đảm bảo thứ tự + Delete cuối
            EnsureColumnOrderAndDeleteLast();
        }

        private void dtgTTNVL_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            e.ThrowException = false;
            e.Cancel = true;

            string colName = ((DataGridView)sender).Columns[e.ColumnIndex].HeaderText;

            if (e.Exception is FormatException)
            {
                FrmWaiting.ShowGifAlert($"Giá trị không hợp lệ ở cột \"{colName}\". Vui lòng nhập số hợp lệ.");
            }
            else
            {
                FrmWaiting.ShowGifAlert("Có lỗi xảy ra: " + e.Exception.Message);
            }
        }

        private void dtgTTNVL_EditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
        {
            if (dtgTTNVL.CurrentCell.OwningColumn.ValueType == typeof(double) ||
                dtgTTNVL.CurrentCell.OwningColumn.ValueType == typeof(int))
            {
                if (e.Control is TextBox tb)
                {
                    tb.KeyPress -= OnlyNumber_KeyPress;
                    tb.KeyPress += OnlyNumber_KeyPress;
                }
            }
        }

        private void OnlyNumber_KeyPress(object sender, KeyPressEventArgs e)
        {
            char dec = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator[0];

            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && e.KeyChar != dec)
                e.Handled = true;

            if (sender is TextBox tb && e.KeyChar == dec && tb.Text.Contains(dec))
                e.Handled = true;
        }

        

        private void setVisibleTableNVL(bool showTable)
        {
            dtgTTNVL.Visible = showTable;
            lblTieuDe.Visible = showTable;
        }

        #region Hiển thị dữ liệu từ DataTable
        public void LoadData(DataTable dt)
        {
            if (dt == null) return;

            setVisibleTableNVL(true);

            if (!dtgTTNVL.IsHandleCreated)
            {
                dtgTTNVL.HandleCreated += (_, __) => LoadData(dt);
                return;
            }

            dtgTTNVL.BeginInvoke(new Action(() =>
            {
                var dtNew = new DataTable("ThongTin");
                foreach (var col in _columns)
                    dtNew.Columns.Add(col.Name, col.DataType);

                foreach (DataRow src in dt.Rows)
                {
                    var row = dtNew.NewRow();
                    foreach (var col in _columns)
                    {
                        if (dt.Columns.Contains(col.Name))
                            row[col.Name] = src[col.Name];
                    }
                    dtNew.Rows.Add(row);
                }

                dtgTTNVL.SuspendLayout();
                try
                {
                    dtgTTNVL.DataSource = null;
                    dtgTTNVL.Columns.Clear();

                    dtgTTNVL.AutoGenerateColumns = true;
                    dtgTTNVL.DataSource = dtNew;

                    SetColumnHeaders(dtgTTNVL, _columns);

                    // ép thứ tự theo _columns + Delete cuối
                    EnsureColumnOrderAndDeleteLast();

                    dtgTTNVL.Refresh();
                }
                finally
                {
                    dtgTTNVL.ResumeLayout();
                }
            }));
        }
        #endregion

        #region Lấy và load dữ liệu vào form code for IFormSection
        public string SectionName => nameof(UC_TTNVL);

        public object GetData()
        {
            var list = new List<TTNVL>();

            foreach (DataGridViewRow row in dtgTTNVL.Rows)
            {
                if (row.IsNewRow) continue;

                TTNVL item = new TTNVL();
                CoreHelper.MapRowToObject(row, item);
                list.Add(item);
            }
            return list;
        }

        public void ClearInputs()
        {
            DataGridViewUtils.ClearSmart(dtgTTNVL);
        }
        #endregion

        private async void cbxTimKiem_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter) return;
            e.Handled = true;
            e.SuppressKeyPress = true;

            EnsureColumnOrderAndDeleteLast();

            string keyword = cbxTimKiem.Text?.Trim().Split('_')[0];

            cbxTimKiem.Text = "";   // luôn clear sau khi scan

            if (string.IsNullOrEmpty(keyword)) return;
            if (!TenMayDaNhap()) return;

            string para = "ten";
            string query = RawMaterial
                ? CoreHelper.TaoSQL_LayDLNVL_TTThanhPham()
                : CoreHelper.TaoSQL_LayDLTTThanhPham();

            DataTable result;

            try
            {
                result = await Task.Run(() =>
                    DatabaseHelper.GetData(query, keyword, para)
                );
            }
            catch (Exception ex)
            {
                FrmWaiting.ShowGifAlert("Lỗi truy vấn dữ liệu: " + ex.Message);
                return;
            }

            if (result == null || result.Rows.Count == 0)
            {
                FrmWaiting.ShowGifAlert("Không tìm thấy dữ liệu cho mã QR vừa quét.");
                return;
            }

            // ===== ADD DATA =====
            AddRowsToGrid(result);
        }


        private void AddRowsToGrid(DataTable source)
        {
            DataTable table = null;

            if (dtgTTNVL.DataSource is BindingSource bs)
                table = bs.DataSource as DataTable;
            else
                table = dtgTTNVL.DataSource as DataTable;

            if (table == null)
            {
                FrmWaiting.ShowGifAlert("DataGridView chưa bind DataTable.");
                return;
            }

            foreach (DataRow src in source.Rows)
            {
                string key = src["id"] == DBNull.Value ? "" : src["id"].ToString();

                bool exists = table.AsEnumerable().Any(r =>
                    (r["id"] == DBNull.Value ? "" : r["id"].ToString()) == key);

                if (exists) continue;

                DataRow newRow = table.NewRow();

                foreach (DataColumn col in table.Columns)
                {
                    if (source.Columns.Contains(col.ColumnName))
                        newRow[col.ColumnName] = src[col.ColumnName];
                }

                table.Rows.Add(newRow);

                // ===== UI phụ =====
                tbTem1.Text = newRow["BinNVL"]?.ToString();
                nbrTemp2.Value = Convert.ToDecimal(
                    newRow["KlBatDau"] == DBNull.Value ? 0 : newRow["KlBatDau"]
                );

                // ================== TÔ MÀU (copy từ cbxTimKiem) ==================
                int addedIndex = table.Rows.IndexOf(newRow);

                if (addedIndex >= 0 && addedIndex < dtgTTNVL.Rows.Count)
                {
                    string maSP = newRow["MaNVL"]?.ToString() ?? string.Empty;
                    int dotIndex = maSP.IndexOf(".");
                    if (dotIndex > 0)
                        maSP = maSP.Substring(0, dotIndex);

                    if (maSP != "NVL")
                    {
                        int start = 3;
                        int baseCol = tongCotCanHide + start;

                        int targetCol =
                            newRow["DonVi"]?.ToString() == "M"
                                ? baseCol
                                : baseCol + 1;

                        bool special =
                            EnumStore.dsTenMayBoQuaKiemTraKhoiLuongConLai
                                .Contains(ReadTenMay());

                        if (special)
                        {
                            object obj = newRow["KlBatDau"];
                            decimal klBatDau =
                                (obj == null || obj == DBNull.Value)
                                    ? 0m
                                    : Convert.ToDecimal(obj);

                            decimal gtConLai_New =
                                (klBatDau <= -1m) ? (klBatDau - 1m) : -1m;

                            dtgTTNVL.Rows[addedIndex]
                                      .Cells["KlConLai"].Value = gtConLai_New;
                        }

                        bool autoFilling =
                            EnumStore.dsTenMayTuDongTinhKLConLai
                                .Contains(ReadTenMay());

                        if (autoFilling && klDongThua != null)
                            PhanBoKLConLai(dtgTTNVL, klDongThua.Value, targetCol);

                        if (!autoFilling && !special)
                            dtgTTNVL.Rows[addedIndex]
                                      .Cells[targetCol]
                                      .Style.BackColor = Color.Yellow;

                        // ===== tô tới cột dữ liệu cuối cùng =====
                        int lastDataIndex = -1;
                        string lastDataName =
                            _columns.Count > 0
                                ? _columns[_columns.Count - 1].Name
                                : null;

                        if (!string.IsNullOrEmpty(lastDataName)
                            && dtgTTNVL.Columns.Contains(lastDataName))
                        {
                            lastDataIndex =
                                dtgTTNVL.Columns[lastDataName].Index;
                        }

                        if (lastDataIndex >= 0)
                        {
                            for (int i = baseCol + 2; i <= lastDataIndex; i++)
                            {
                                dtgTTNVL.Rows[addedIndex]
                                          .Cells[i]
                                          .Style.BackColor = Color.Yellow;
                            }
                        }
                    }

                    dtgTTNVL.FirstDisplayedScrollingRowIndex = addedIndex;
                }
                // ================================================================
            }

            EnsureColumnOrderAndDeleteLast();
            dtgTTNVL.Refresh();
        }



        public void OnKhoiLuongChanged(decimal newValue)
        {
            ClearGridKeepHeader();
        }

        private void ClearGridKeepHeader()
        {
            if (dtgTTNVL.DataSource is DataTable dt)
            {
                dt.Rows.Clear();
                return;
            }

            if (dtgTTNVL.DataSource is BindingSource bs && bs.DataSource is DataTable dt2)
            {
                dt2.Rows.Clear();
                return;
            }
            klDongThua = null;

            dtgTTNVL.Rows.Clear();
        }

        private bool CheckKhoiLuongBeforeTyping()
        {
            var v = ReadKhoiLuong();
            if (v != 0m) return true;

            if (!_warnedThisFocus)
            {
                _warnedThisFocus = true;
                FrmWaiting.ShowGifAlert("Nhập khối lượng TP trước khi nhập nguyên liệu.");
            }

            BeginInvoke(new Action(() =>
            {
                cbxTimKiem.Text = "";
                FocusKhoiLuong?.Invoke();
            }));

            return false;
        }

        private bool TenMayDaNhap()
        {
            if (ReadTenMay() != "") return true;
            FrmWaiting.ShowGifAlert("LOT SX cần được hoàn thiện trước khi nhập nguyên liệu.");
            return false;
        }

        private decimal ReadKhoiLuong()
            => GetKhoiLuong?.Invoke() ?? 0m;

        private string ReadTenMay()
            => GetTenMay?.Invoke() ?? "";

        private void cbxTimKiem_Enter(object sender, EventArgs e)
        {
            _warnedThisFocus = false;
        }

        //public void SetKhoiLuongDongThua()
        //{
        //    using var f = new GetUserInputValue_Simple();
        //    f.StartPosition = FormStartPosition.CenterScreen;
        //    var result = f.ShowDialog(this);

        //    if (result == DialogResult.OK)
        //    {
        //        klDongThua = f.TongDongThuaValue;
        //        tbKLDongThua.Visible = true;
        //        nmrKlDongThua.Value = klDongThua.Value;
        //    }
        //}

        private static void PhanBoKLConLai(DataGridView dtgTTNVL, decimal klDongThua, int colIndex)
        {
            int rowCount = dtgTTNVL.Rows.Cast<DataGridViewRow>()
                                        .Count(r => !r.IsNewRow);

            if (rowCount <= 0) return;

            decimal giaTriMoiDong = klDongThua / rowCount;

            foreach (DataGridViewRow row in dtgTTNVL.Rows)
            {
                if (row.IsNewRow) continue;
                row.Cells[colIndex].Value = giaTriMoiDong;
                row.Cells[colIndex].ReadOnly = true;
            }
        }

        private void nmrKlDongThua_Leave(object sender, EventArgs e)
        {
        }

        private void UC_TTNVL_Load(object sender, EventArgs e)
        {
            DataGridViewClipboardHelper.Attach(dtgTTNVL,
                includeHeaderWhenCopy: false,
                enableTsvBlockPaste: true,
                useDBNullForEmpty: true
            );
        }
    }
}
