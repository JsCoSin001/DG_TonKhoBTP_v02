
using DG_TonKhoBTP_v02.Core;
using DG_TonKhoBTP_v02.Database;
using DG_TonKhoBTP_v02.Dictionary;
using DG_TonKhoBTP_v02.Models;
using DG_TonKhoBTP_v02.UI.Helper;
using DocumentFormat.OpenXml.Wordprocessing;
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
        private CancellationTokenSource _searchCts;

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
                    defaultWidth = 70;
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
            string tenNL = cbxTimKiem.Text;

            if (string.IsNullOrWhiteSpace(tenNL) || !TenMayDaNhap()) return;

            bool tuDongTinh = EnumStore.dsTenMayTuDongTinhKLConLai.Contains(ReadTenMay());

            // Yêu cầu nhập khối lượng đồng thừa
            if (tuDongTinh && klDongThua == null) SetKhoiLuongDongThua();

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
                query = CoreHelper.TaoSQL_LayDLNVL_TTThanhPham();
            }
            else
            {
                query = CoreHelper.TaoSQL_LayDLTTThanhPham();
            }

            DataTable sp = await Task.Run(() =>
            {
                return DatabaseHelper.GetData(query, keyword, para);
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
            newRow["QC"] = sel["QC"];
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

                bool special = EnumStore.dsTenMayBoQuaKiemTraKhoiLuongConLai.Contains(ReadTenMay());

                if (special)
                {
                    object obj = newRow["KlBatDau"];
                    decimal klBatDau = (obj == null || obj == DBNull.Value) ? 0m : Convert.ToDecimal(obj);
                    decimal gtConLai_New = (klBatDau <= -1m) ? (klBatDau - 1m) : -1m;

                    dtgTTNVL.Rows[addedIndex].Cells[targetCol].Value = gtConLai_New;
                    dtgTTNVL.Rows[addedIndex].Cells[targetCol].ReadOnly = true;
                }

                bool autoFilling = EnumStore.dsTenMayTuDongTinhKLConLai.Contains(ReadTenMay());

                if (autoFilling && klDongThua != null)
                    PhanBoKLConLai(dtgTTNVL, klDongThua.Value, targetCol);

                if (!autoFilling && !special)
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

        private void setVisibleTableNVL(bool showTable)
        {
            dtgTTNVL.Visible = showTable;
            lblTieuDe.Visible = showTable;
        }

        #region Hiển thị dữ liệu từ DataTable
        //public void LoadData(DataTable dt)
        //{
        

        //    if (dt == null) return;

        //    setVisibleTableNVL(true);

        //    // 1) Tạo dtNew theo _columns
        //    var dtNew = new DataTable("ThongTin");
        //    foreach (var col in _columns)
        //    {
        //        dtNew.Columns.Add(col.Name, col.DataType);
        //    }

        //    foreach (DataRow src in dt.Rows)
        //    {
        //        var row = dtNew.NewRow();
        //        foreach (var col in _columns)
        //        {
        //            if (dt.Columns.Contains(col.Name)) row[col.Name] = src[col.Name];
        //        }
        //        dtNew.Rows.Add(row);
        //    }

        //    dtgTTNVL.SuspendLayout();

        //    try
        //    {
        //        // 2) Dọn cột cũ để tránh duplicate / lệch mapping
        //        dtgTTNVL.DataSource = null;
        //        dtgTTNVL.Columns.Clear();             // quan trọng

        //        // 3) Bind
        //        dtgTTNVL.AutoGenerateColumns = true;
        //        dtgTTNVL.DataSource = dtNew;

        //        // 4) Gọi lại cấu hình header/width…
        //        SetColumnHeaders(dtgTTNVL, _columns);

        //        // 5) Ép lại thứ tự hiển thị theo _columns (đảm bảo index ổn định cho SetColumnHeaders)
        //        for (int i = 0; i < _columns.Count; i++)
        //        {
        //            var name = _columns[i].Name;
        //            if (dtgTTNVL.Columns.Contains(name))
        //            {
        //                dtgTTNVL.Columns[name].DisplayIndex = i;
        //            }
        //        }

        //        // 6) Đảm bảo cột Delete là cột CUỐI CÙNG
        //        if (!dtgTTNVL.Columns.Contains("Delete"))
        //        {
        //            var btnDelete = new DataGridViewButtonColumn
        //            {
        //                Name = "Delete",
        //                HeaderText = "",
        //                Text = "Xoá",
        //                UseColumnTextForButtonValue = true,
        //                Width = 60
        //            };
        //            dtgTTNVL.Columns.Add(btnDelete);
        //        }

        //        // Đặt DisplayIndex cho Delete là cuối
        //        dtgTTNVL.Columns["Delete"].DisplayIndex = dtgTTNVL.Columns.Count - 1;

        //    }
        //    finally
        //    {
        //        dtgTTNVL.ResumeLayout();
        //    }

        //}

        public void LoadData(DataTable dt)
        {
            if (dt == null) return;

            setVisibleTableNVL(true);

            // Nếu DataGridView chưa tạo Handle (thường xảy ra khi UC vừa mới show)
            if (!dtgTTNVL.IsHandleCreated)
            {
                dtgTTNVL.HandleCreated += (_, __) => LoadData(dt); // gọi lại khi handle sẵn sàng
                return;
            }

            // Đẩy việc bind sang vòng message loop kế tiếp để đảm bảo render đủ rows ngay lần 1
            dtgTTNVL.BeginInvoke(new Action(() =>
            {
                // 1) Tạo dtNew theo _columns
                var dtNew = new DataTable("ThongTin");
                foreach (var col in _columns)
                {
                    dtNew.Columns.Add(col.Name, col.DataType);
                }

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
                    // 3) Dọn cột cũ để tránh duplicate / lệch mapping
                    dtgTTNVL.DataSource = null;
                    dtgTTNVL.Columns.Clear();

                    // 4) Bind từ field
                    dtgTTNVL.AutoGenerateColumns = true;
                    dtgTTNVL.DataSource = dtNew;

                    // 5) Cấu hình header/width…
                    SetColumnHeaders(dtgTTNVL, _columns);

                    // 6) Ép thứ tự hiển thị theo _columns
                    for (int i = 0; i < _columns.Count; i++)
                    {
                        var name = _columns[i].Name;
                        if (dtgTTNVL.Columns.Contains(name))
                            dtgTTNVL.Columns[name].DisplayIndex = i;
                    }

                    // 7) Đảm bảo cột Delete là cuối
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

                    dtgTTNVL.Columns["Delete"].DisplayIndex = dtgTTNVL.Columns.Count - 1;

                    // 8) Ép refresh (không bắt buộc nhưng giúp chắc ăn)
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
                // Bỏ qua dòng "New Row" (dòng trống cuối cùng của DataGridView)
                if (row.IsNewRow) continue;

                TTNVL item = new TTNVL(); // Lúc này các field đang có default = -1
                CoreHelper.MapRowToObject(row, item); // Map từ row vào object


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
            //if (!CheckKhoiLuongBeforeTyping())
            //{
            //    e.SuppressKeyPress = true;
            //    e.Handled = true;
            //    return;
            //}
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
            klDongThua = null;
            //lblKlDongThua.Text = "";

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


        private void SetKhoiLuongDongThua()
        {
            using var f = new GetUserInputValue_Simple();
            f.StartPosition = FormStartPosition.CenterScreen;
            var result = f.ShowDialog(this);

            if (result == DialogResult.OK)
            {
                klDongThua = f.TongDongThuaValue;
                tbKLDongThua.Visible = true;
                nmrKlDongThua.Value = klDongThua.Value;
            }


        }

        // Phân bổ khối lượng đồng thừa đều cho các dòng
        private static void PhanBoKLConLai(DataGridView dtgTTNVL, decimal klDongThua, int colIndex)
        {

            // Đếm số dòng dữ liệu thật (bỏ dòng NewRow nếu có)
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

    }
}
