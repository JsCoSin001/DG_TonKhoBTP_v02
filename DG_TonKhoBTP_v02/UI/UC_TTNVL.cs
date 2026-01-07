
using DG_TonKhoBTP_v02.Core;
using DG_TonKhoBTP_v02.Database;
using DG_TonKhoBTP_v02.Dictionary;
using DG_TonKhoBTP_v02.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Color = System.Drawing.Color;

namespace DG_TonKhoBTP_v02.UI
{
    public partial class UC_TTNVL : UserControl, IFormSection, IDataReceiver
    {
        private CancellationTokenSource _searchCts;

        List<ColumnDefinition> _columns;

        public Func<decimal> GetKhoiLuong { get; set; }

        public Func<string> GetSoLOT { get; set; }

        private bool _warnedThisFocus = false;

        public Action FocusKhoiLuong { get; set; }

        private bool _autoFilling = false;


        bool isShow = false;
        int tongCotCanHide = 10;

        public bool RawMaterial { get; set; } = false;
        public void SetStatusRawMaterial(bool value) => RawMaterial = value;

        public UC_TTNVL(List<ColumnDefinition> columns)
        {
            InitializeComponent();

            setVisibleTableNVL(false);

            _columns = columns;

            TaoBang(columns);

            // Bắt lỗi nhập sai định dạng
            dtgTTNVL.DataError += dtgTTNVL_DataError;

            // Hạn chế nhập ký tự không hợp lệ cho các cột số
            dtgTTNVL.EditingControlShowing += dtgTTNVL_EditingControlShowing;
        }

        private void TaoBang(List<ColumnDefinition> columns)
        {
            DataTable dt = new DataTable("ThongTin");


            // Tạo cột từ danh sách
            foreach (var col in columns) dt.Columns.Add(col.Name, col.DataType);

            dtgTTNVL.DataSource = dt;


            // Gọi lại hàm SetColumnHeaders để cấu hình
            SetColumnHeaders(dtgTTNVL, columns);

            // Tuỳ chỉnh style
            dtgTTNVL.DefaultCellStyle.Font = new System.Drawing.Font("Microsoft Sans Serif", 10, FontStyle.Regular);
            dtgTTNVL.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dtgTTNVL.AllowUserToResizeRows = false;

            dtgTTNVL.RowTemplate.Height = 30;

            // Thêm cột nút Xoá
            if (!dtgTTNVL.Columns.Contains("Delete"))
            {
                DataGridViewButtonColumn btnDelete = new DataGridViewButtonColumn();
                btnDelete.Name = "Delete";
                btnDelete.HeaderText = "";
                btnDelete.Text = "Xoá";
                btnDelete.UseColumnTextForButtonValue = true;
                btnDelete.Width = 60;
                dtgTTNVL.Columns.Add(btnDelete);
            }

            dtgTTNVL.CellClick += dtgTTNVL_CellClick;
        }

        private void dtgTTNVL_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex == dtgTTNVL.Columns["Delete"].Index)
            {
                // Xác nhận xoá
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
                    defaultWidth = 85;
                    defaulHeight = 45;
                    break;
            }

            // Chiều cao header
            dgv.ColumnHeadersHeight = defaulHeight;
            dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;

            // Duyệt TẤT CẢ các cột có trong DataGridView
            for (int i = 0; i < dgv.Columns.Count; i++)
            {
                // Gán header và width cho các cột dữ liệu (nằm trong headers)
                if (i < headers.Length)
                {
                    dgv.Columns[i].HeaderText = headers[i];
                    dgv.Columns[i].Width = defaultWidth;
                }

                // TẮT SORT cho mọi cột (kể cả cột Delete)
                dgv.Columns[i].SortMode = DataGridViewColumnSortMode.NotSortable;
            }

            // Ẩn/hiện + readonly các cột từ 0 đến tongCotCanHide (có kiểm tra tránh lỗi)
            for (int i = 0; i <= tongCotCanHide && i < dgv.Columns.Count; i++)
            {
                dgv.Columns[i].Visible = isShow;
                dgv.Columns[i].ReadOnly = true;
            }

            // Cột sau cùng "chính" để Fill (kiểm tra index cho chắc chắn)
            int fillColumnIndex = tongCotCanHide + 1;
            if (fillColumnIndex >= 0 && fillColumnIndex < dgv.Columns.Count)
            {
                dgv.Columns[fillColumnIndex].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                dgv.Columns[fillColumnIndex].ReadOnly = true;
            }

            // Style header
            dgv.EnableHeadersVisualStyles = false;
            dgv.ColumnHeadersDefaultCellStyle.Font = new System.Drawing.Font("Microsoft Sans Serif", 10, FontStyle.Regular);
            dgv.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        }

        private void dtgTTNVL_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            e.ThrowException = false;
            e.Cancel = true; // Giữ ô để sửa

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
            char dec = System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator[0];

            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && e.KeyChar != dec)
                e.Handled = true;

            // chỉ cho phép 1 dấu thập phân
            if (sender is TextBox tb && e.KeyChar == dec && tb.Text.Contains(dec))
                e.Handled = true;
        }

        private async void tbxTimKiem_TextUpdate(object sender, EventArgs e)
        {
            //ResetController_TimTenSP();
            string tenNL = cbxTimKiem.Text;


            if (string.IsNullOrWhiteSpace(tenNL) || !CheckKhoiLuongBeforeTyping() || !CheckSoLotBeforeTyping()) return;

            // --- thêm debounce + cancel ---
            _searchCts?.Cancel();
            _searchCts = new CancellationTokenSource();
            var token = _searchCts.Token;

            try
            {
                // debounce: đợi user dừng gõ 250ms mới chạy
                await Task.Delay(250, token);

                // gọi async thay vì sync
                await ShowDanhSachLuaChon(tenNL, token);
            }
            catch (OperationCanceledException)
            {
                // bị huỷ vì user gõ tiếp, bỏ qua
            }
        }

        private async Task ShowDanhSachLuaChon(string keyword, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                cbxTimKiem.DroppedDown = false;
                return;
            }
            string para = "ten";
            string query;

            if (RawMaterial)
            {
                query = Helper.Helper.TaoSQL_LayDLNVL_TTThanhPham();
            }
            else
            {
                query = Helper.Helper.TaoSQL_LayDLTTThanhPham();
            }

            DataTable sp = await Task.Run(() =>
            {
                return DatabaseHelper.GetData( query, keyword, para);
            }, ct);

            ct.ThrowIfCancellationRequested();

            cbxTimKiem.DroppedDown = false;

            cbxTimKiem.SelectionChangeCommitted -= cbxTimKiem_SelectionChangeCommitted; 
            if (sp.Rows.Count == 0) return;

            cbxTimKiem.DataSource = sp;
            cbxTimKiem.DisplayMember = "BinNVL";

            string currentText = keyword;

            cbxTimKiem.DroppedDown = true;
            cbxTimKiem.Text = currentText;
            cbxTimKiem.SelectionStart = cbxTimKiem.Text.Length;
            cbxTimKiem.SelectionLength = 0;

            cbxTimKiem.SelectionChangeCommitted += cbxTimKiem_SelectionChangeCommitted;
        }

        private void cbxTimKiem_SelectionChangeCommitted(object sender, EventArgs e)
        {
            if (cbxTimKiem.SelectedItem == null || !(cbxTimKiem.SelectedItem is DataRowView sel))
                return;
            setVisibleTableNVL(true);

            // Reset combobox SỚM – sau này return kiểu gì cũng đã reset rồi
            cbxTimKiem.SelectedIndex = -1;
            cbxTimKiem.Text = string.Empty;

            // Lấy DataTable đang bind với DataGridView (kể cả khi dùng BindingSource)
            DataTable table = null;
            BindingSource bs = dtgTTNVL.DataSource as BindingSource;
            if (bs != null)
                table = bs.DataSource as DataTable;
            else
                table = dtgTTNVL.DataSource as DataTable;

            if (table == null)
            {
                FrmWaiting.ShowGifAlert("DataGridView chưa bind với DataTable.");
                return;
            }

            // Chống trùng theo 'id'
            string key = sel["id"] == DBNull.Value ? string.Empty : Convert.ToString(sel["id"]);
            bool exists = table.AsEnumerable().Any(r => (r["id"] == DBNull.Value ? string.Empty : Convert.ToString(r["id"])) == key);
            if (exists)
            {
                FrmWaiting.ShowGifAlert("Lot này đã có trong vào danh sách.");
                return;
            }

            // Tạo dòng mới và gán giá trị
            DataRow newRow = table.NewRow();
            newRow["CongDoan"] = sel["CongDoan"];
            newRow["KlBatDau"] = sel["KlBatDau"];
            newRow["CdBatDau"] = sel["CdBatDau"];
            newRow["DonVi"] = sel["DonVi"];
            newRow["id"] = sel["id"];
            newRow["MaNVL"] = sel["MaNVL"];
            newRow["Ngay"] = sel["Ngay"];
            newRow["Ca"] = sel["Ca"];
            newRow["NguoiLam"] = sel["NguoiLam"];
            newRow["GhiChu"] = sel["GhiChu"];
            newRow["DanhSachMaSP_ID"] = sel["DanhSachMaSP_ID"];
            newRow["BinNVL"] = sel["BinNVL"];
            table.Rows.Add(newRow);

            int addedIndex = table.Rows.IndexOf(newRow);

            if (addedIndex >= 0 && addedIndex < dtgTTNVL.Rows.Count)
            {
                dtgTTNVL.ClearSelection();

                string maSP = newRow["MaNVL"].ToString();
                int dotIndex = maSP.IndexOf(".");
                if (dotIndex > 0)
                    maSP = maSP.Substring(0, dotIndex);

                int start = 3;

                if (maSP == "NVL") return; 

                // vị trí bắt đầu tô màu
                int baseCol = tongCotCanHide + start;


                // số cột chứa đơn vị là m
                //int targetCol =  baseCol + 1;
                int targetCol = newRow["DonVi"].ToString() == "M" ? baseCol : baseCol + 1;

                // Số cột chứa đơn vị là kg
                //if (newRow["DonVi"].ToString() == "M")
                //{
                //    targetCol = baseCol;

                //    if (table.Rows.Count == 1)
                //    {
                //        CalcAndSet_KlBatDauMinusKhoiLuong(table, addedIndex, targetCol);
                //    }
                //    else
                //    {
                //        int prevRowIndex = addedIndex - 1;
                //        if (prevRowIndex >= 0)
                //        {
                //            SetValue_TargetCell_ByIndex(table, prevRowIndex, targetCol, null);
                //        }
                //    }

                //}

                dtgTTNVL.Rows[addedIndex].Cells[targetCol].Style.BackColor = Color.Yellow;

                // Tô các cột còn lại
                for (int i = baseCol + 2; i <= _columns.Count; i++)
                {
                    Console.WriteLine(i);
                    dtgTTNVL.Rows[addedIndex].Cells[i].Style.BackColor = Color.Yellow;
                }    
                   

                dtgTTNVL.FirstDisplayedScrollingRowIndex = addedIndex;
            }
        }


        private void SetValue_TargetCell_ByIndex(DataTable table, int rowIndex, int gridColIndex, decimal? value)
        {
            string dataCol = dtgTTNVL.Columns[gridColIndex].DataPropertyName;
            if (string.IsNullOrWhiteSpace(dataCol))
                dataCol = dtgTTNVL.Columns[gridColIndex].Name;

            table.Rows[rowIndex][dataCol] = value.HasValue ? (object)value.Value : DBNull.Value;
        }

        private void CalcAndSet_KlBatDauMinusKhoiLuong(DataTable table, int addedIndex, int targetCol)
        {
            // Lấy KlBatDau từ dòng vừa add
            decimal klBatDau = 0m;
            object objKl = table.Rows[addedIndex]["KlBatDau"];
            if (objKl != null && objKl != DBNull.Value)
                klBatDau = Convert.ToDecimal(objKl);

            // Lấy khoiLuong hiện tại từ UC_TTThanhPham (qua delegate đã nối)
            decimal khoiLuong = GetKhoiLuong?.Invoke() ?? 0m;

            decimal result = klBatDau - khoiLuong;
            if (result >= 0) SetValue_TargetCell_ByIndex(table, addedIndex, targetCol, result);


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

            // 1) Tạo dtNew theo _columns
            var dtNew = new DataTable("ThongTin");
            foreach (var col in _columns)
                dtNew.Columns.Add(col.Name, col.DataType);

            foreach (DataRow src in dt.Rows)
            {
                var row = dtNew.NewRow();
                foreach (var col in _columns)
                    if (dt.Columns.Contains(col.Name)) row[col.Name] = src[col.Name];
                dtNew.Rows.Add(row);
            }

            dtgTTNVL.SuspendLayout();

            try
            {
                // 2) Dọn cột cũ để tránh duplicate / lệch mapping
                dtgTTNVL.DataSource = null;
                dtgTTNVL.Columns.Clear();             // quan trọng

                // 3) Bind
                dtgTTNVL.AutoGenerateColumns = true;
                dtgTTNVL.DataSource = dtNew;

                // 4) Gọi lại cấu hình header/width…
                SetColumnHeaders(dtgTTNVL, _columns);

                // 5) Ép lại thứ tự hiển thị theo _columns (đảm bảo index ổn định cho SetColumnHeaders)
                for (int i = 0; i < _columns.Count; i++)
                {
                    var name = _columns[i].Name;
                    if (dtgTTNVL.Columns.Contains(name))
                        dtgTTNVL.Columns[name].DisplayIndex = i;
                }

                // 6) Đảm bảo cột Delete là cột CUỐI CÙNG
                if (!dtgTTNVL.Columns.Contains("Delete"))
                {
                    var btnDelete = new DataGridViewButtonColumn
                    {
                        Name = "Delete",
                        HeaderText = "",
                        Text = "Xoá",
                        UseColumnTextForButtonValue = true,
                        Width = 60
                    };
                    dtgTTNVL.Columns.Add(btnDelete);
                }

                // Đặt DisplayIndex cho Delete là cuối
                dtgTTNVL.Columns["Delete"].DisplayIndex = dtgTTNVL.Columns.Count - 1;

                // (Giữ nguyên các tuỳ chỉnh khác của bạn)
                dtgTTNVL.CellClick -= dtgTTNVL_CellClick;
                dtgTTNVL.CellClick += dtgTTNVL_CellClick;
            }
            finally
            {
                dtgTTNVL.ResumeLayout();
            }
        }
        #endregion

        #region Lấy và load dữ liệu vào form code for IFormSection
        public string SectionName => nameof(UC_TTNVL);

        public object GetData()
        {
            var list = new List<TTNVL>();

            foreach (DataGridViewRow row in dtgTTNVL.Rows)
            {
                // Bỏ qua dòng "New Row" (dòng trống cuối cùng của DataGridView)
                if (row.IsNewRow) continue;

                var item = new TTNVL(); // Lúc này các field đang có default = -1
                Helper.Helper.MapRowToObject(row, item); // Map từ row vào object
                list.Add(item);
            }
            return list;
            
        }

        public void ClearInputs()
        {
            // Xoá dữ liệu theo cách an toàn cho mọi kiểu bind
            DataGridViewUtils.ClearSmart(dtgTTNVL);

            // Reset control khác nếu cần
            if (cbxTimKiem != null)
            {
                //cbxTimKiem.SelectedIndex = -1;
                //cbxTimKiem.Text = string.Empty;
            }
        }

        #endregion

        private void cbxTimKiem_KeyDown(object sender, KeyEventArgs e)
        {
            if (!CheckKhoiLuongBeforeTyping())
            {
                e.SuppressKeyPress = true;
                e.Handled = true;
                return;
            }
        }

        public void OnKhoiLuongChanged(decimal newValue)
        {
            ClearGridKeepHeader();
        }

        private void ClearGridKeepHeader()
        {
            // Nếu đang bind datasource -> xóa data nguồn để giữ cột/header (đặc biệt khi auto-generate)
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

            // Unbound grid
            dtgTTNVL.Rows.Clear(); // KHÔNG đụng Columns => không mất header
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
                FocusKhoiLuong?.Invoke();   // <<< focus sang khoiLuong
            }));

            return false;
        }

        private bool CheckSoLotBeforeTyping()
        {
            if (ReadSoLot() != "") return true;
            FrmWaiting.ShowGifAlert("LOT SX cần được hoàn thiện trước khi nhập nguyên liệu.");
            return false;
        }


        private decimal ReadKhoiLuong()
        => GetKhoiLuong?.Invoke() ?? 0m;

        private string ReadSoLot()
        => GetSoLOT?.Invoke() ?? "";

        private void cbxTimKiem_Enter(object sender, EventArgs e)
        {
            _warnedThisFocus = false;
        }


        private void dtgTTNVL_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            return; 
            if (_autoFilling) return;

            // chỉ xử lý khi edit cột KlConLai
            var col = dtgTTNVL.Columns[e.ColumnIndex];
            if (col.Name != "KlConLai" && col.DataPropertyName != "KlConLai")
                return;

            var table = GetBoundTable();
            if (table == null) return;

            // 1) tính tổng KlBatDau
            decimal totalKlBatDau = table.AsEnumerable()
                .Where(r => r.RowState != DataRowState.Deleted)
                .Sum(r => ToDec(r["KlBatDau"]));

            // 2) tính tổng KlConLai đã nhập
            decimal sumKlConLai = table.AsEnumerable()
                .Where(r => r.RowState != DataRowState.Deleted)
                .Where(r => !IsNullOrEmptyCell(r["KlConLai"]))
                .Sum(r => ToDec(r["KlConLai"]));

            // 3) đếm dòng còn trống KlConLai
            int emptyIndex = -1, emptyCount = 0;
            for (int i = 0; i < table.Rows.Count; i++)
            {
                var r = table.Rows[i];
                if (r.RowState == DataRowState.Deleted) continue;

                if (IsNullOrEmptyCell(r["KlConLai"]))
                {
                    emptyCount++;
                    emptyIndex = i;
                }
            }

            decimal khoiLuong = ReadKhoiLuong();

            // 4) nếu còn đúng 1 dòng trống -> auto fill
            if (emptyCount == 1 && emptyIndex >= 0)
            {
                decimal klConLaiAuto = totalKlBatDau - sumKlConLai - khoiLuong;

                _autoFilling = true;
                table.Rows[emptyIndex]["KlConLai"] = klConLaiAuto;
                _autoFilling = false;

                // cập nhật lại tổng KlConLai sau khi auto fill
                sumKlConLai += klConLaiAuto;
                emptyCount = 0;
            }

            // 5) nếu đã điền đủ (không còn dòng trống) -> kiểm tra cân bằng
            if (emptyCount == 0)
            {
                decimal diff = totalKlBatDau - (sumKlConLai + khoiLuong);

                // cho phép sai số rất nhỏ (tránh lỗi do làm tròn)
                if (Math.Abs(diff) > 0.0001m)
                {
                    FrmWaiting.ShowGifAlert("Khối lượng còn lại không hợp lệ. Hãy kiểm tra và nhập lại");

                    ResetAllKlConLai(table);
                }
            }
        }

        private void ResetAllKlConLai(DataTable table)
        {
            _autoFilling = true;
            foreach (DataRow r in table.Rows)
            {
                if (r.RowState == DataRowState.Deleted) continue;
                r["KlConLai"] = DBNull.Value; // reset về null
            }
            _autoFilling = false;
        }

        private static bool IsNullOrEmptyCell(object o)
        {
            if (o == null || o == DBNull.Value) return true;
            if (o is string s) return string.IsNullOrWhiteSpace(s);
            return false;
        }

        private static decimal ToDec(object o)
        {
            if (o == null || o == DBNull.Value) return 0m;
            return Convert.ToDecimal(o);
        }

        private DataTable GetBoundTable()
        {
            if (dtgTTNVL.DataSource is BindingSource bs) return bs.DataSource as DataTable;
            return dtgTTNVL.DataSource as DataTable;
        }

    }


}
