using DG_TonKhoBTP_v02.Core;
using DG_TonKhoBTP_v02.Database;
using DG_TonKhoBTP_v02.Dictionary;
using DG_TonKhoBTP_v02.Helper.Reuseable;
using DG_TonKhoBTP_v02.Models;
using DG_TonKhoBTP_v02.UI.Helper;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Color = System.Drawing.Color;
using CoreHelper = DG_TonKhoBTP_v02.Helper.Helper;

namespace DG_TonKhoBTP_v02.UI
{
    public partial class UC_TTNVL : UserControl, IFormSection, IDataReceiver
    {
        private readonly BindingList<TTNVLRow> _nvlRows = new BindingList<TTNVLRow>();
        private readonly BindingSource _nvlSource = new BindingSource();

        private List<ColumnDefinition> _columns;

        public decimal? klDongThua = null;

        public Func<ThanhPhamData> GetThanhPhamData { get; set; }

        private bool _warnedThisFocus = false;

        public Action FocusKhoiLuong { get; set; }

        private bool isShow = false;
        private int tongCotCanHide = 10;

        private CongDoan _CD;

        public bool RawMaterial { get; set; } = false;
        public void SetStatusRawMaterial(bool value) => RawMaterial = value;

        public UC_TTNVL(List<ColumnDefinition> columns, CongDoan cd)
        {
            InitializeComponent();

            setVisibleTableNVL(true);

            _columns = columns;
            _CD = cd;

            _nvlSource.DataSource = _nvlRows;

            // BẢO HIỂM: mỗi lần bind xong sẽ ép thứ tự theo _columns và Delete cuối
            dtgTTNVL.DataBindingComplete += (s, e) =>
            {
                EnsureColumnOrderAndDeleteLast();
            };

            TaoBang(columns);

            // Bắt lỗi nhập sai định dạng
            dtgTTNVL.DataError += dtgTTNVL_DataError;

            // Hạn chế nhập ký tự không hợp lệ cho các cột số
            dtgTTNVL.EditingControlShowing += dtgTTNVL_EditingControlShowing;

            dtgTTNVL.CellFormatting += dtgTTNVL_CellFormatting;

            DebugPrintColumnsByDefinitions();
        }

        // ===================== CORE: luôn ép thứ tự cột & Delete cuối =====================
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

            if (dtgTTNVL.Columns.Contains("Delete"))
            {
                var d = dtgTTNVL.Columns["Delete"];
                Console.WriteLine($"(extra) Name={d.Name}, Header={d.HeaderText}, DisplayIndex={d.DisplayIndex}");
            }
        }
        // =================================================================================

        private void TaoBang(List<ColumnDefinition> columns)
        {
            dtgTTNVL.AutoGenerateColumns = false;
            dtgTTNVL.Columns.Clear();
            dtgTTNVL.Tag = typeof(TTNVLRow);

            foreach (var col in columns)
            {
                string propertyName = ResolvePropertyName<TTNVLRow>(col.Name) ?? col.Name;
                Type valueType = ResolvePropertyType<TTNVLRow>(propertyName) ?? col.DataType;

                var dgvCol = new DataGridViewTextBoxColumn
                {
                    Name = col.Name,
                    DataPropertyName = propertyName,
                    HeaderText = col.Header ?? string.Empty,
                    ValueType = Nullable.GetUnderlyingType(valueType) ?? valueType,
                    SortMode = DataGridViewColumnSortMode.NotSortable
                };

                dtgTTNVL.Columns.Add(dgvCol);
            }

            dtgTTNVL.DataSource = _nvlSource;

            SetColumnHeaders(dtgTTNVL, columns);

            dtgTTNVL.DefaultCellStyle.Font = new Font("Microsoft Sans Serif", 10, FontStyle.Regular);
            dtgTTNVL.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dtgTTNVL.AllowUserToResizeRows = false;
            dtgTTNVL.RowTemplate.Height = 30;

            EnsureColumnOrderAndDeleteLast();

            dtgTTNVL.CellClick -= dtgTTNVL_CellClick;
            dtgTTNVL.CellClick += dtgTTNVL_CellClick;
        }

        private static string ResolvePropertyName<T>(string columnName)
        {
            return typeof(T).GetProperties()
                .FirstOrDefault(p => string.Equals(p.Name, columnName, StringComparison.OrdinalIgnoreCase))
                ?.Name;
        }

        private static Type ResolvePropertyType<T>(string propertyName)
        {
            return typeof(T).GetProperties()
                .FirstOrDefault(p => string.Equals(p.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                ?.PropertyType;
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
                    dtgTTNVL.EndEdit();
                    _nvlSource.EndEdit();
                    _nvlSource.RemoveAt(e.RowIndex);
                }
            }
        }

        public void OnSoLOTChanged(string soLot)
        {
            ClearGridKeepHeader();
            string may = soLot?.Split('-')[0] ?? "";
        }

        public void OnThanhPhamChanged(ThanhPhamData data)
        {
            ClearGridKeepHeader();
        }

        private void SetColumnHeaders(DataGridView dgv, List<ColumnDefinition> columns)
        {
            int defaultWidth = 100;
            int defaulHeight = 30;

            int extraCols = columns.Count - ThongTinChungCongDoan.BaseColumns().Count;

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

            foreach (DataGridViewColumn col in dgv.Columns)
                col.SortMode = DataGridViewColumnSortMode.NotSortable;

            foreach (var def in columns)
            {
                if (!dgv.Columns.Contains(def.Name)) continue;
                dgv.Columns[def.Name].HeaderText = def.Header ?? "";
                dgv.Columns[def.Name].Width = defaultWidth;
            }

            // Ẩn/hiện + readonly các cột từ 0 đến tongCotCanHide (tránh đụng Delete)
            var colsByDisplay = dgv.Columns.Cast<DataGridViewColumn>()
                               .OrderBy(c => c.DisplayIndex)
                               .ToList();

            for (int i = 0; i <= tongCotCanHide && i < colsByDisplay.Count; i++)
            {
                if (colsByDisplay[i].Name == "Delete") continue;
                colsByDisplay[i].Visible = isShow;
                colsByDisplay[i].ReadOnly = true;
            }

            // Chọn cột fill theo _columns, tránh lỗi out-of-range
            int fillDefIndex = tongCotCanHide + 1;
            if (fillDefIndex < 0) fillDefIndex = 0;
            if (fillDefIndex > columns.Count - 1) fillDefIndex = columns.Count - 1;

            string fillColName = columns[fillDefIndex].Name;
            if (dgv.Columns.Contains(fillColName))
            {
                dgv.Columns[fillColName].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                dgv.Columns[fillColName].ReadOnly = true;
            }

            dgv.EnableHeadersVisualStyles = false;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Microsoft Sans Serif", 10, FontStyle.Regular);
            dgv.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            EnsureColumnOrderAndDeleteLast();
        }

        private void dtgTTNVL_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            e.ThrowException = false;
            e.Cancel = true;

            string colName = e.ColumnIndex >= 0
                ? ((DataGridView)sender).Columns[e.ColumnIndex].HeaderText
                : string.Empty;

            if (e.Exception is FormatException)
            {
                FrmWaiting.ShowGifAlert($"Giá trị không hợp lệ ở cột \"{colName}\". Vui lòng nhập số hợp lệ.");
            }
            else
            {
                FrmWaiting.ShowGifAlert("Có lỗi xảy ra: " + e.Exception.Message);
            }
        }

        private void dtgTTNVL_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (e.Value == null) return;

            string colName = dtgTTNVL.Columns[e.ColumnIndex].DataPropertyName;

            switch (colName)
            {
                case nameof(TTNVLRow.CongDoan):
                case nameof(TTNVLRow.KlBatDau):
                case nameof(TTNVLRow.CdBatDau):
                case nameof(TTNVLRow.KlConLai):
                case nameof(TTNVLRow.CdConLai):
                case nameof(TTNVLRow.DuongKinhSoiDong):
                case nameof(TTNVLRow.SoSoi):
                case nameof(TTNVLRow.KetCauLoi):
                case nameof(TTNVLRow.DuongKinhSoiMach):
                    if (e.Value.ToString() == "-1")
                    {
                        e.Value = string.Empty;
                        e.FormattingApplied = true;
                    }
                    break;
            }
        }

        private void dtgTTNVL_EditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
        {
            var col = dtgTTNVL.CurrentCell?.OwningColumn;
            if (col == null) return;

            if (col.ValueType == typeof(double) ||
                col.ValueType == typeof(float) ||
                col.ValueType == typeof(decimal) ||
                col.ValueType == typeof(int) ||
                col.ValueType == typeof(long) ||
                col.ValueType == typeof(short))
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
        public void LoadData(DataTable dt, int kieuDL)
        {
            ClearInputs();

            isEdit.Value = 0;

            if (dt == null) return;

            setVisibleTableNVL(true);

            if (!dtgTTNVL.IsHandleCreated)
            {
                dtgTTNVL.HandleCreated += (_, __) => LoadData(dt, kieuDL);
                return;
            }

            isEdit.Value = kieuDL;

            dtgTTNVL.BeginInvoke(new Action(() =>
            {
                string bin = string.Empty;

                if (dt.Rows.Count > 0 && HasColumn(dt, "MaBin"))
                {
                    bin = GetString(dt.Rows[0], "MaBin");
                }

                string may = string.Empty;
                string[] maBinParts = CoreHelper.CatMaBin(bin);

                if (maBinParts.Length > 0)
                {
                    may = maBinParts[0];
                }

                string[] arr = { "B10", "B13", "B14", "B15", "B16", "MD16A4", "R10", "R12" };

                if (kieuDL == 1 && !arr.Contains(may)) return;

                dtgTTNVL.SuspendLayout();
                try
                {
                    _nvlRows.Clear();

                    foreach (DataRow src in dt.Rows)
                    {
                        _nvlRows.Add(MapDataRowToNvlRow(src));
                    }

                    _nvlSource.ResetBindings(false);
                    SetColumnHeaders(dtgTTNVL, _columns);
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
            dtgTTNVL.EndEdit();
            _nvlSource.EndEdit();

            if (!ValidateRequiredVisibleInputColumns())
                throw new InvalidOperationException("Thông tin nguyên vật liệu chưa hợp lệ.");

            return _nvlRows.ToList();
        }

        private bool ValidateRequiredVisibleInputColumns()
        {
            string[] requiredColumns =
            {
                nameof(TTNVLRow.DuongKinhSoiDong),
                nameof(TTNVLRow.SoSoi),
                nameof(TTNVLRow.KetCauLoi),
                nameof(TTNVLRow.DuongKinhSoiMach)
            };

            foreach (string colName in requiredColumns)
            {
                // Chỉ validate cột thật sự có trên DataGridView.
                // Ví dụ GhepLoi_QB chỉ có DuongKinhSoiDong và DuongKinhSoiMach.
                if (!dtgTTNVL.Columns.Contains(colName))
                    continue;

                DataGridViewColumn col = dtgTTNVL.Columns[colName];

                // Nếu cột bị ẩn thì không bắt nhập.
                if (!col.Visible)
                    continue;

                for (int i = 0; i < _nvlRows.Count; i++)
                {
                    TTNVLRow row = _nvlRows[i];

                    // Theo logic hiện tại: chỉ bắt nhập với BTP, không bắt với NVL.
                    string ma = row.MaNVL ?? string.Empty;
                    bool isNVL = ma.Split('.')[0].Equals("NVL", StringComparison.OrdinalIgnoreCase);
                    if (isNVL)
                        continue;

                    bool empty = false;

                    switch (colName)
                    {
                        case nameof(TTNVLRow.DuongKinhSoiDong):
                            empty = !row.DuongKinhSoiDong.HasValue || row.DuongKinhSoiDong.Value <= 0;
                            break;

                        case nameof(TTNVLRow.SoSoi):
                            empty = !row.SoSoi.HasValue || row.SoSoi.Value <= 0;
                            break;

                        case nameof(TTNVLRow.KetCauLoi):
                            empty = !row.KetCauLoi.HasValue || row.KetCauLoi.Value <= 0;
                            break;

                        case nameof(TTNVLRow.DuongKinhSoiMach):
                            empty = !row.DuongKinhSoiMach.HasValue || row.DuongKinhSoiMach.Value <= 0;
                            break;
                    }

                    if (empty)
                    {
                        dtgTTNVL.ClearSelection();

                        if (i < dtgTTNVL.Rows.Count)
                        {
                            dtgTTNVL.Rows[i].Selected = true;
                            dtgTTNVL.CurrentCell = dtgTTNVL.Rows[i].Cells[colName];
                            dtgTTNVL.BeginEdit(true);
                        }

                        FrmWaiting.ShowGifAlert(
                            $"Vui lòng nhập \"{col.HeaderText}\" tại dòng {i + 1}."
                        );

                        return false;
                    }
                }
            }

            return true;
        }

        public void ClearInputs()
        {
            ResetNvlState();
            _nvlRows.Clear();
            _nvlSource.ResetBindings(false);
        }
        #endregion

        private async void cbxTimKiem_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter) return;
            e.Handled = true;
            e.SuppressKeyPress = true;

            EnsureColumnOrderAndDeleteLast();

            var thanhPham = GetThanhPhamData?.Invoke() ?? new ThanhPhamData();

            if (string.IsNullOrWhiteSpace(thanhPham.DonVi))
            {
                FrmWaiting.ShowGifAlert("Thông tin thành phẩm công đoạn cần hoàn thiện trước.");
                return;
            }

            if (thanhPham.DonVi == "M" && thanhPham.ChieuDai == 0m)
            {
                FrmWaiting.ShowGifAlert("Vui lòng nhập Chiều dài trước khi quét mã QR.");
                return;
            }

            if (thanhPham.DonVi == "KG" && thanhPham.KhoiLuong == 0m)
            {
                FrmWaiting.ShowGifAlert("Vui lòng nhập Khối lượng trước khi quét mã QR.");
                return;
            }

            string keyword = cbxTimKiem.Text?.Trim();

            if (string.IsNullOrWhiteSpace(keyword)) return;

            DataTable result = new DataTable();

            cbxTimKiem.Text = string.Empty;

            if (!TenMayDaNhap()) return;

            bool cdHanNoi = _CD.Id == 9 && isEdit.Value == 2;

            var parameters = new Dictionary<string, object>
            {
                { "ten", keyword },
                { "ParentProductId", thanhPham.DanhSachSPId }
            };

            bool isMaterial = keyword.Split('.')[0].ToUpper() == "NVL";

            if (isMaterial != RawMaterial)
            {
                FrmWaiting.ShowGifAlert("Mã QR vừa quét không phù hợp với công đoạn.");
                return;
            }

            string query = isMaterial
                ? CoreHelper.TaoSQL_LayDLNVL()
                : CoreHelper.TaoSQL_LayDLTTThanhPham(cdHanNoi);

            try
            {
                result = await Task.Run(() => DatabaseHelper.GetNVL(query, parameters));
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

            AddRowsToGrid(result, thanhPham);
        }

        private void AddRowsToGrid(DataTable source, ThanhPhamData thanhPham)
        {
            if (source == null || source.Rows.Count == 0) return;

            foreach (DataRow src in source.Rows)
            {
                TTNVLRow newItem = MapDataRowToNvlRow(src);
                string key = newItem.Id?.ToString() ?? string.Empty;

                bool exists = _nvlRows.Any(r =>
                    (r.Id?.ToString() ?? string.Empty) == key && !string.IsNullOrEmpty(key));

                if (exists)
                {
                    FrmWaiting.ShowGifAlert("Mã này đã được quét rồi.");
                    ResetNvlState();
                    continue;
                }

                if (newItem.IsCorrect == false)
                {
                    DialogResult confirm = MessageBox.Show(
                        $"Sản phẩm này không phù hợp với thành phẩm: {thanhPham.SoLOT}\nNếu tiếp tục chọn Yes, hoặc quét lại chọn No",
                        "Xác nhận NVL/BTP khác BOM",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning,
                        MessageBoxDefaultButton.Button2);

                    if (confirm == DialogResult.No)
                    {
                        ResetNvlState();
                        continue;
                    }
                }

                // ===== UI phụ =====
                tbTem1.Text = newItem.BinNVL ?? string.Empty;
                SetNbrTemp2Value(Convert.ToDecimal(newItem.KlBatDau ?? 0));

                string maSP = newItem.MaNVL ?? string.Empty;
                int dotIndex = maSP.IndexOf(".");
                if (dotIndex > 0)
                    maSP = maSP.Substring(0, dotIndex);

                bool shouldMarkRequiredCells = false;

                if (maSP != "NVL")
                {
                    decimal klBatDau = Convert.ToDecimal(newItem.KlBatDau ?? 0);
                    decimal cdBatDau = Convert.ToDecimal(newItem.CdBatDau ?? 0);
                    string dvNVL = newItem.DonVi ?? string.Empty;

                    decimal kl;
                    if (dvNVL != thanhPham.DonVi)
                    {
                        decimal tyLe = thanhPham.ChuyenDoi;

                        kl = klBatDau - tyLe * thanhPham.ChieuDai < 0
                            ? 0
                            : klBatDau - tyLe * thanhPham.ChieuDai;
                    }
                    else
                    {
                        kl = klBatDau - thanhPham.KhoiLuong < 0
                            ? 0
                            : klBatDau - thanhPham.KhoiLuong;
                    }

                    decimal cd = cdBatDau - thanhPham.ChieuDai < 0
                        ? 0
                        : cdBatDau - thanhPham.ChieuDai;

                    newItem.KlConLai = _CD.Id == 9 ? 0 : Convert.ToDouble(kl);
                    newItem.CdConLai = _CD.Id == 9 ? 0 : Convert.ToDouble(cd);

                    shouldMarkRequiredCells = true;
                }

                _nvlRows.Add(newItem);

                int addedIndex = _nvlRows.IndexOf(newItem);
                if (addedIndex >= 0 && addedIndex < dtgTTNVL.Rows.Count)
                {
                    if (shouldMarkRequiredCells)
                    {
                        int start = 3;
                        int baseCol = tongCotCanHide + start;

                        string lastDataName = _columns.Count > 0
                            ? _columns[_columns.Count - 1].Name
                            : null;

                        int lastDataIndex = -1;
                        if (!string.IsNullOrEmpty(lastDataName) && dtgTTNVL.Columns.Contains(lastDataName))
                        {
                            lastDataIndex = dtgTTNVL.Columns[lastDataName].Index;
                        }

                        if (lastDataIndex >= 0)
                        {
                            for (int i = baseCol + 2; i <= lastDataIndex; i++)
                            {
                                if (i >= 0 && i < dtgTTNVL.Columns.Count)
                                    dtgTTNVL.Rows[addedIndex].Cells[i].Style.BackColor = Color.Yellow;
                            }
                        }
                    }

                    if (newItem.IsCorrect == false)
                    {
                        dtgTTNVL.Rows[addedIndex].DefaultCellStyle.ForeColor = Color.Red;
                    }

                    dtgTTNVL.FirstDisplayedScrollingRowIndex = addedIndex;
                }
            }

            EnsureColumnOrderAndDeleteLast();
            dtgTTNVL.Refresh();
        }

        private TTNVLRow MapDataRowToNvlRow(DataRow src)
        {
            return new TTNVLRow
            {
                Id = GetInt(src, "id"),
                TTThanhPhan_ID = GetInt(src, "TTThanhPhan_ID"),
                DanhSachMaSP_ID = GetInt(src, "DanhSachMaSP_ID"),

                BinNVL = GetString(src, "BinNVL"),
                CongDoan = GetInt(src, "CongDoan") ?? -1,
                KlBatDau = GetDouble(src, "KlBatDau") ?? -1,
                CdBatDau = GetDouble(src, "CdBatDau") ?? -1,
                KlConLai = GetDouble(src, "KlConLai") ?? -1,
                CdConLai = GetDouble(src, "CdConLai") ?? -1,
                DuongKinhSoiDong = GetDouble(src, "DuongKinhSoiDong") ?? -1,
                SoSoi = GetInt(src, "SoSoi") ?? -1,
                KetCauLoi = GetDouble(src, "KetCauLoi") ?? -1,
                DuongKinhSoiMach = GetDouble(src, "DuongKinhSoiMach") ?? -1,
                QC = GetString(src, "QC"),

                MaNVL = GetString(src, "MaNVL"),
                DonVi = GetString(src, "DonVi"),
                Ngay = GetString(src, "Ngay"),
                Ca = GetString(src, "Ca"),
                NguoiLam = GetString(src, "NguoiLam"),
                TenNVL = GetString(src, "TenNVL"),
                GhiChu = GetString(src, "GhiChu"),
                TyLe = GetDouble(src, "TyLe") ?? 1,
                TyLeHoanDoi = GetDouble(src, "TyLeHoanDoi") ?? 1,
                IsCorrect = GetBool(src, "IsCorrect", true)
            };
        }

        private static bool HasColumn(DataTable table, string columnName)
        {
            return table != null && table.Columns.Cast<DataColumn>()
                .Any(c => string.Equals(c.ColumnName, columnName, StringComparison.OrdinalIgnoreCase));
        }

        private static object GetRaw(DataRow row, string columnName)
        {
            if (row?.Table == null) return null;

            var col = row.Table.Columns.Cast<DataColumn>()
                .FirstOrDefault(c => string.Equals(c.ColumnName, columnName, StringComparison.OrdinalIgnoreCase));

            if (col == null) return null;

            object value = row[col];
            return value == DBNull.Value ? null : value;
        }

        private static string GetString(DataRow row, string columnName, string defaultValue = "")
        {
            object raw = GetRaw(row, columnName);
            return raw == null ? defaultValue : raw.ToString();
        }

        private static int? GetInt(DataRow row, string columnName)
        {
            object raw = GetRaw(row, columnName);
            if (raw == null) return null;

            try
            {
                if (raw is int i) return i;
                if (raw is long l) return Convert.ToInt32(l);
                if (raw is double d) return Convert.ToInt32(d);
                if (raw is decimal m) return Convert.ToInt32(m);

                string s = raw.ToString();
                if (string.IsNullOrWhiteSpace(s)) return null;
                return Convert.ToInt32(Convert.ToDecimal(s, CultureInfo.InvariantCulture));
            }
            catch
            {
                return null;
            }
        }

        private static double? GetDouble(DataRow row, string columnName)
        {
            object raw = GetRaw(row, columnName);
            if (raw == null) return null;

            try
            {
                if (raw is double d) return d;
                if (raw is float f) return f;
                if (raw is decimal m) return Convert.ToDouble(m);
                if (raw is int i) return i;
                if (raw is long l) return l;

                string s = raw.ToString();
                if (string.IsNullOrWhiteSpace(s)) return null;
                return Convert.ToDouble(s, CultureInfo.InvariantCulture);
            }
            catch
            {
                try
                {
                    return Convert.ToDouble(raw);
                }
                catch
                {
                    return null;
                }
            }
        }

        private static bool GetBool(DataRow row, string columnName, bool defaultValue)
        {
            object raw = GetRaw(row, columnName);
            if (raw == null) return defaultValue;

            try
            {
                if (raw is bool b) return b;
                if (raw is int i) return i != 0;
                if (raw is long l) return l != 0;
                if (raw is double d) return Math.Abs(d) > double.Epsilon;
                if (raw is decimal m) return m != 0;

                string s = raw.ToString()?.Trim();
                if (string.IsNullOrWhiteSpace(s)) return defaultValue;
                if (s == "1") return true;
                if (s == "0") return false;
                if (bool.TryParse(s, out bool parsed)) return parsed;

                return defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }

        public void OnKhoiLuongChanged(decimal newValue)
        {
            ClearGridKeepHeader();
        }

        private void ClearGridKeepHeader()
        {
            ResetNvlState();
            _nvlRows.Clear();
            _nvlSource.ResetBindings(false);
        }

        private void ResetNvlState()
        {
            klDongThua = null;
            _warnedThisFocus = false;

            cbxTimKiem.Text = string.Empty;
            tbTem1.Text = string.Empty;

            // NumericUpDown không có null. Dùng klDongThua = null làm trạng thái nghiệp vụ,
            // còn UI cho nhìn trống thay vì gán Minimum âm rất lớn.
            SetNbrTemp2Value(0);
            nbrTemp2.Text = string.Empty;
        }

        private void SetNbrTemp2Value(decimal value)
        {
            if (value < nbrTemp2.Minimum) value = nbrTemp2.Minimum;
            if (value > nbrTemp2.Maximum) value = nbrTemp2.Maximum;
            nbrTemp2.Value = value;
        }

        private bool TenMayDaNhap()
        {
            if (ReadTenMay() != "") return true;
            FrmWaiting.ShowGifAlert("LOT SX cần được hoàn thiện trước khi nhập nguyên liệu.");
            return false;
        }

        private string ReadTenMay()
        {
            return GetThanhPhamData?.Invoke()?.TenMay ?? string.Empty;
        }

        private void cbxTimKiem_Enter(object sender, EventArgs e)
        {
            _warnedThisFocus = false;
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
