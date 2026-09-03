using ClosedXML.Excel;
using DG_TonKhoBTP_v02.Core;
using DG_TonKhoBTP_v02.Database;
using DG_TonKhoBTP_v02.Dictionary;
using DG_TonKhoBTP_v02.Helper;
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
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Media;
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

        private const double HE_SO_CHUYEN_DOI_CD = 1.01;

        // Trạng thái điều phối cập nhật KL/CD còn lại.
        private bool _dangLoadDuLieuBanDau;
        private bool _dangXuLyQuet;
        private bool _dangCapNhatGiaTriConLai;
        private bool _dangNhapTayToanBang;

        private enum LyDoCapNhatGiaTriConLai
        {
            ThanhPhamThayDoi,
            SoLieuThanhPhamThayDoi,
            ThemHoacXoaDong
        }

        // Lưu quyền ReadOnly ban đầu để mở 2 cột còn lại khi toàn bảng nhập tay.
        private bool _daLuuQuyenCotConLai;
        private bool _klConLaiReadOnlyMacDinh = true;
        private bool _cdConLaiReadOnlyMacDinh = true;

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

                if (_daLuuQuyenCotConLai)
                    ApDungQuyenNhapTayChoTatCaDong();
            };

            TaoBang(columns);
            LuuQuyenMacDinhCuaCotConLai();

            // Bắt lỗi nhập sai định dạng
            dtgTTNVL.DataError += dtgTTNVL_DataError;

            // Hạn chế nhập ký tự không hợp lệ cho các cột số
            dtgTTNVL.EditingControlShowing += dtgTTNVL_EditingControlShowing;

            // Ô trống phải được ghi nhận là null, không được tự đổi thành 0.
            dtgTTNVL.CellParsing += dtgTTNVL_CellParsing;

            dtgTTNVL.CellFormatting += dtgTTNVL_CellFormatting;
            dtgTTNVL.CellValueChanged += dtgTTNVL_CellValueChanged;

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

                    ThanhPhamData thanhPham = GetThanhPhamData?.Invoke()
                        ?? new ThanhPhamData();
                    CapNhatGiaTriConLaiToanBang(
                        thanhPham,
                        LyDoCapNhatGiaTriConLai.ThemHoacXoaDong);
                }
            }
        }

        public void OnSoLOTChanged(string soLot, string may)
        {
            // Khi sửa, NVL cũ được phép đi cùng số LOT hoặc máy mới.
            if (isEdit.Value == 2)
                return;

            if (EnumStore.LaMayChoPhepTaiSuDungNVL(may))
                return;

            ClearGridKeepHeader();
        }

        public void OnThanhPhamChanged(ThanhPhamData data)
        {
            if (_dangLoadDuLieuBanDau)
                return;

            // Khi sửa, luôn giữ các dòng NVL nhưng vẫn kiểm tra lại theo BOM mới.
            if (isEdit.Value == 2)
            {
                RecalculateBomForExistingRows(data);
                CapNhatGiaTriConLaiToanBang(
                    data,
                    LyDoCapNhatGiaTriConLai.ThanhPhamThayDoi);
                return;
            }

            if (EnumStore.LaMayChoPhepTaiSuDungNVL(data.TenMay))
            {
                RecalculateBomForExistingRows(data);
                CapNhatGiaTriConLaiToanBang(
                    data,
                    LyDoCapNhatGiaTriConLai.ThanhPhamThayDoi);
                return;
            }

            // Giữ nguyên hành vi cũ: trường hợp không được tái sử dụng NVL thì xóa bảng.
            ClearGridKeepHeader();
        }

        /// <summary>
        /// Chỉ dùng cho thay đổi Khối lượng/Chiều dài thành phẩm.
        /// Không áp dụng quy tắc xóa NVL khi đổi thành phẩm.
        /// </summary>
        public void OnThanhPhamSoLieuChanged(ThanhPhamData data)
        {
            if (_dangLoadDuLieuBanDau)
                return;

            CapNhatGiaTriConLaiToanBang(
                data,
                LyDoCapNhatGiaTriConLai.SoLieuThanhPhamThayDoi);
        }

        private void ApplyBomToRow(TTNVLRow row, ThanhPhamData thanhPham)
        {
            if (row == null) return;

            if (!CongDoanPolicy.CanKiemTraBom(_CD))
            {
                row.TyLe = 1d;
                row.TyLeHoanDoi = 1d;
                row.IsCorrect = true;
                return;
            }

            BomComponentData matched = null;
            if (row.DanhSachMaSP_ID.HasValue && thanhPham?.BomComponents != null)
            {
                matched = thanhPham.BomComponents.FirstOrDefault(x =>
                    x != null && x.ComponentId == row.DanhSachMaSP_ID.Value);
            }

            row.TyLe = matched == null
                ? 1d
                : Convert.ToDouble(matched.TyLe);
            row.TyLeHoanDoi = matched == null
                ? 1d
                : Convert.ToDouble(matched.TyLeHoanDoi);

            if (_CD?.Id == 0)
            {
                row.IsCorrect = KiemTraBomCongDoan0Helper.NguyenVatLieuPhuHop(
                    thanhPham?.BomComponents,
                    row.TenNVL);
                return;
            }

            row.IsCorrect = matched != null;
        }

        private void RecalculateBomForExistingRows(ThanhPhamData thanhPham)
        {
            foreach (TTNVLRow row in _nvlRows)
            {
                ApplyBomToRow(row, thanhPham);
            }

            _nvlSource.ResetBindings(false);
            RefreshBomRowStyles();
            dtgTTNVL.Refresh();
        }

        private void RefreshBomRowStyles()
        {
            Color normalColor = dtgTTNVL.DefaultCellStyle.ForeColor;

            foreach (DataGridViewRow dgvRow in dtgTTNVL.Rows)
            {
                if (!(dgvRow.DataBoundItem is TTNVLRow row))
                    continue;

                dgvRow.DefaultCellStyle.ForeColor = row.IsCorrect
                    ? normalColor
                    : Color.Red;
            }
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

            string colName = dtgTTNVL.Columns[e.ColumnIndex].DataPropertyName;

            ApDungMauGiaTriConLai(e.RowIndex, colName, e.CellStyle);

            if (e.Value == null) return;

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

        private void dtgTTNVL_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

            string propertyName = dtgTTNVL.Columns[e.ColumnIndex].DataPropertyName;
            if (propertyName != nameof(TTNVLRow.KlConLai) &&
                propertyName != nameof(TTNVLRow.CdConLai) &&
                propertyName != nameof(TTNVLRow.DonVi))
            {
                return;
            }

            InvalidateGiaTriConLaiCells(e.RowIndex);
        }

        private void InvalidateGiaTriConLaiCells(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= dtgTTNVL.Rows.Count) return;

            if (dtgTTNVL.Columns.Contains(nameof(TTNVLRow.KlConLai)))
            {
                dtgTTNVL.InvalidateCell(
                    dtgTTNVL.Columns[nameof(TTNVLRow.KlConLai)].Index,
                    rowIndex);
            }

            if (dtgTTNVL.Columns.Contains(nameof(TTNVLRow.CdConLai)))
            {
                dtgTTNVL.InvalidateCell(
                    dtgTTNVL.Columns[nameof(TTNVLRow.CdConLai)].Index,
                    rowIndex);
            }
        }

        private void ApDungMauGiaTriConLai(
            int rowIndex,
            string propertyName,
            DataGridViewCellStyle cellStyle)
        {
            if (rowIndex < 0 || rowIndex >= dtgTTNVL.Rows.Count) return;
            if (!(dtgTTNVL.Rows[rowIndex].DataBoundItem is TTNVLRow row)) return;

            string donVi = ChuanHoaDonVi(row.DonVi);

            bool canhBaoKl =
                propertyName == nameof(TTNVLRow.KlConLai) &&
                donVi == "KG" &&
                row.KlConLai == 0;

            bool canhBaoCd =
                propertyName == nameof(TTNVLRow.CdConLai) &&
                donVi == "M" &&
                row.CdConLai == 0;

            if (canhBaoKl || canhBaoCd)
            {
                cellStyle.BackColor = Color.Red;
                cellStyle.ForeColor = Color.White;
                return;
            }

            // Nền không cảnh báo: giữ nền hiện tại, chỉ áp dụng màu chữ theo BOM.
            // Chiếu Xạ không kiểm tra BOM nên IsCorrect luôn true.
            cellStyle.ForeColor = row.IsCorrect ? Color.Black : Color.Red;
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

        private void dtgTTNVL_CellParsing(object sender, DataGridViewCellParsingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

            string propertyName = dtgTTNVL.Columns[e.ColumnIndex].DataPropertyName;
            bool laCotConLai =
                propertyName == nameof(TTNVLRow.KlConLai) ||
                propertyName == nameof(TTNVLRow.CdConLai);

            if (!laCotConLai) return;

            if (string.IsNullOrWhiteSpace(Convert.ToString(e.Value)))
            {
                e.Value = null;
                e.ParsingApplied = true;
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
            _dangLoadDuLieuBanDau = true;

            try
            {
                dtgTTNVL.BeginInvoke(new Action(() =>
                {
                    try
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

                        if (kieuDL == 1 && !EnumStore.LaMayChoPhepTaiSuDungNVL(may))
                            return;

                        dtgTTNVL.SuspendLayout();
                        try
                        {
                            _nvlRows.Clear();

                            ThanhPhamData thanhPham = GetThanhPhamData?.Invoke()
                                ?? new ThanhPhamData();

                            foreach (DataRow src in dt.Rows)
                            {
                                TTNVLRow row = MapDataRowToNvlRow(src);
                                ApplyBomToRow(row, thanhPham);
                                _nvlRows.Add(row);
                            }

                            // Lần load đầu của edit/tái sử dụng phải giữ nguyên KL/CD đã lưu.
                            // Chỉ xác định trạng thái nhập tay để các trigger sau xử lý đúng.
                            _dangNhapTayToanBang = CoNguyenVatLieuNhapTay();

                            _nvlSource.ResetBindings(false);
                            SetColumnHeaders(dtgTTNVL, _columns);
                            EnsureColumnOrderAndDeleteLast();
                            ApDungQuyenNhapTayChoTatCaDong();
                            RefreshBomRowStyles();
                            dtgTTNVL.Refresh();
                        }
                        finally
                        {
                            dtgTTNVL.ResumeLayout();
                        }
                    }
                    finally
                    {
                        _dangLoadDuLieuBanDau = false;
                    }
                }));
            }
            catch
            {
                _dangLoadDuLieuBanDau = false;
                throw;
            }
        }
        #endregion

        #region Lấy và load dữ liệu vào form code for IFormSection
        public string SectionName => nameof(UC_TTNVL);

        public object GetData()
        {
            dtgTTNVL.EndEdit();
            _nvlSource.EndEdit();

            // Công đoạn 9 không sử dụng NVL trong cả tạo mới và chỉnh sửa.
            // Trả danh sách rỗng trước khi chạy bất kỳ kiểm tra NVL nào.
            if (_CD?.Id == 9)
                return new List<TTNVLRow>();

            if (!ValidateRequiredVisibleInputColumns())
                throw new InvalidOperationException(
                    "Thông tin nguyên vật liệu chưa hợp lệ.");

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
            _dangNhapTayToanBang = false;
            _nvlRows.Clear();
            _nvlSource.ResetBindings(false);
        }

        public void DisableSearchForCongDoan9()
        {
            cbxTimKiem.Text = string.Empty;
            cbxTimKiem.Enabled = false;
            cbxTimKiem.TabStop = false;
            dtgTTNVL.Enabled = false;

            _dangNhapTayToanBang = false;
            _nvlRows.Clear();
            _nvlSource.ResetBindings(false);
        }
        #endregion

        private async void cbxTimKiem_KeyDown(object sender, KeyEventArgs e)
        {
            // Phòng vệ bổ sung: công đoạn 9 không được nhập hoặc quét NVL.
            if (_CD?.Id == 9)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                return;
            }

            if (e.KeyCode != Keys.Enter)
                return;

            e.Handled = true;
            e.SuppressKeyPress = true;

            if (_dangXuLyQuet)
                return;

            _dangXuLyQuet = true;
            cbxTimKiem.Enabled = false;

            try
            {
                EnsureColumnOrderAndDeleteLast();

                ThanhPhamData thanhPham = GetThanhPhamData?.Invoke()
                    ?? new ThanhPhamData();

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
                if (string.IsNullOrWhiteSpace(keyword))
                    return;

                cbxTimKiem.Text = string.Empty;

                if (!TenMayDaNhap())
                    return;

                bool cdHanNoi = _CD.Id == 9 && isEdit.Value == 2;

                // Lấy mã nếu Ngọc Khánh gửi.
                string[] isNgocKhanh = keyword.Split(';');
                keyword = isNgocKhanh.Count() == 26 ? isNgocKhanh[7] : keyword;

                var parameters = new Dictionary<string, object>
                {
                    { "ten", keyword }
                };

                string query = CoreHelper.TaoSQL_LayDLTTThanhPham(cdHanNoi);
                DataTable result;

                try
                {
                    result = await WaitingHelper.RunWithWaiting(
                        () => Task.Run(() => DatabaseHelper.GetNVL(query, parameters)),
                        "ĐANG TÌM VÀ XỬ LÝ NGUYÊN VẬT LIỆU...");
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
            finally
            {
                _dangXuLyQuet = false;

                if (_CD?.Id != 9)
                {
                    cbxTimKiem.Enabled = true;
                    cbxTimKiem.Focus();
                }
            }
        }

        private void AddRowsToGrid(DataTable source, ThanhPhamData thanhPham)
        {
            if (source == null || source.Rows.Count == 0) return;

            bool coThemDong = false;

            foreach (DataRow src in source.Rows)
            {
                TTNVLRow newItem = MapDataRowToNvlRow(src);
                ApplyBomToRow(newItem, thanhPham);
                string key = newItem.Id?.ToString() ?? string.Empty;

                bool exists = _nvlRows.Any(r =>
                    (r.Id?.ToString() ?? string.Empty) == key && !string.IsNullOrEmpty(key));

                if (exists)
                {
                    FrmWaiting.ShowGifAlert("Mã này đã được quét rồi.");
                    ResetNvlState();
                    continue;
                }

                if (CongDoanPolicy.CanCanhBaoSaiBomKhiQuet(_CD) && newItem.IsCorrect == false)
                {
                    DialogResult confirm = MessageBox.Show(
                        $"Mã bin này không phù hợp với {thanhPham.SoLOT}\nNếu tiếp tục chọn Yes, hoặc quét lại chọn No",
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

                bool shouldMarkRequiredCells = true;

                _nvlRows.Add(newItem);
                coThemDong = true;

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

            if (coThemDong)
            {
                CapNhatGiaTriConLaiToanBang(
                    thanhPham,
                    LyDoCapNhatGiaTriConLai.ThemHoacXoaDong);
            }

            EnsureColumnOrderAndDeleteLast();
            ApDungQuyenNhapTayChoTatCaDong();
            dtgTTNVL.Refresh();
        }

        private bool LaCongDoanHanNoi()
        {
            return _CD != null && _CD.Id == 9;
        }

        private bool CoNguyenVatLieuNhapTay()
        {
            return _nvlRows.Any(nvl => NvlNhapTayPolicy.ApDung(nvl));
        }

        private void CapNhatGiaTriConLaiToanBang(
            ThanhPhamData thanhPham,
            LyDoCapNhatGiaTriConLai lyDo)
        {
            if (_dangLoadDuLieuBanDau || _dangCapNhatGiaTriConLai)
                return;

            _dangCapNhatGiaTriConLai = true;
            try
            {
                if (_nvlRows.Count == 0)
                {
                    _dangNhapTayToanBang = false;
                    ApDungQuyenNhapTayChoTatCaDong();
                    return;
                }

                if (LaCongDoanHanNoi())
                {
                    _dangNhapTayToanBang = false;
                    DatGiaTriConLaiChoToanBang(0d, 0d);
                    LamMoiSauKhiCapNhatGiaTriConLai();
                    return;
                }

                // Khi bảng đã nhập tay, thay đổi thành phẩm/KL/CD không được ghi đè dữ liệu.
                // Riêng thêm hoặc xóa dòng phải đánh giá lại toàn bộ bảng.
                if (_dangNhapTayToanBang &&
                    lyDo != LyDoCapNhatGiaTriConLai.ThemHoacXoaDong)
                {
                    ApDungQuyenNhapTayChoTatCaDong();
                    return;
                }

                if (CoNguyenVatLieuNhapTay())
                {
                    _dangNhapTayToanBang = true;
                    DatGiaTriConLaiChoToanBang(null, null);
                    LamMoiSauKhiCapNhatGiaTriConLai();
                    return;
                }

                _dangNhapTayToanBang = false;
                TinhGiaTriConLaiTheoCongDoan(thanhPham);
                LamMoiSauKhiCapNhatGiaTriConLai();
            }
            finally
            {
                _dangCapNhatGiaTriConLai = false;
            }
        }


        private void TinhGiaTriConLaiTheoCongDoan(ThanhPhamData thanhPham)
        {
            if (_CD == null)
                return;

            if (_CD.Id == 0)
            {
                TinhGiaTriConLai_CD_KeoRut(_nvlRows, thanhPham);
                return;
            }

            if (_CD.Id == 1)
            {
                TinhGiaTriConLai_CD_Ben(_nvlRows);
                return;
            }

            if (_CD.Id == 10)
            {
                TinhGiaTriConLai_CD_ChieuXa(_nvlRows);
                return;
            }

            if (_CD.Id > 1 && _CD.Id != 9)
            {
                TinhGiaTriConLai_CD_Khac(_nvlRows, thanhPham);
            }
        }

        /// <summary>
        /// TODO: Điền công thức tính KL/CD còn lại cho công đoạn 0 ở giai đoạn sau.
        /// </summary>
        private static void TinhGiaTriConLai_CD_KeoRut( IList<TTNVLRow> nvlRows, ThanhPhamData thanhPham)
        {

            if (thanhPham == null)
                throw new ArgumentNullException(nameof(thanhPham));

            // Tính dựa vào tỷ lệ giữa diện tích tiết diện của NVL và diện tích tiết diện của thành phẩm.
            IList<KetQuaGiaTriConLai> ketQuaDaTinh = new List<KetQuaGiaTriConLai>();

            string tenTP = PhanTachCauTrucDay.PhanTich(thanhPham.TenTP).PhanChinh;
            //double tietDienTP = LayGiaTriSo_CD_Rut(tenTP);

            int soLuong = LaySoLuongSauX(tenTP);

            foreach (TTNVLRow item in nvlRows)
            {
                KetQuaGiaTriConLai kq = new KetQuaGiaTriConLai();

                string tenNVL = item.TenNVL;

                KetQuaPhanTich ketQua = PhanTachCauTrucDay.PhanTich(tenNVL);
                double dkNVL  = LayGiaTriSo_CD_Rut(ketQua.PhanChinh);

                double tietDienNVL = Math.Pow(dkNVL / 2, 2) * Math.PI;

                kq.KlConLai = Math.Max(0, (item.KlConLai ?? 0) - (double)thanhPham.KhoiLuong/soLuong);

                kq.CdConLai = tietDienNVL > 0 ? kq.KlConLai * 1000 / (8.96 * tietDienNVL) : null;

                ketQuaDaTinh.Add(kq);
            }

            GanKetQuaGiaTriConLai(nvlRows, ketQuaDaTinh);
        }

        private static int LaySoLuongSauX(string ten)
        {
            if (string.IsNullOrWhiteSpace(ten))
                return 1;

            int viTriX = ten.LastIndexOfAny(new[] { 'x', 'X' });

            if (viTriX < 0 || viTriX >= ten.Length - 1)
                return 1;

            string phanSauX = ten.Substring(viTriX + 1).Trim();

            return int.TryParse(phanSauX, out int soLuong) && soLuong > 0
                ? soLuong
                : 1;
        }


        private static double LayGiaTriSo_CD_Rut(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return 0;

            Match match = Regex.Match(input, @"[-+]?\d+(?:[.,]\d+)?");

            if (!match.Success)
                return 0;

            string giaTri = match.Value.Replace(',', '.');

            if (double.TryParse(
                giaTri,
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out double result))
            {
                return result;
            }

            return 0;
        }

        /// <summary>
        /// TODO: Điền công thức tính KL/CD còn lại cho công đoạn 1 ở giai đoạn sau.
        /// Đặt hết về 0 vì công đoạn 1 không cần tính KL/CD còn lại.
        /// </summary>
        private static void TinhGiaTriConLai_CD_Ben( IList<TTNVLRow> nvlRows)
        {
            GanKetQuaGiaTriConLai(nvlRows);
        }

        /// <summary>
        /// Công đoạn Chiếu Xạ áp dụng cùng quy tắc với Bện:
        /// khi hệ thống tính lại, KL/CD còn lại của toàn bộ NVL bằng 0.
        /// </summary>
        private static void TinhGiaTriConLai_CD_ChieuXa(IList<TTNVLRow> nvlRows)
        {
            GanKetQuaGiaTriConLai(nvlRows);
        }

        /// <summary>
        /// TODO: Điền công thức tính KL/CD còn lại cho công đoạn > 1, khác 9.
        /// </summary>
        private static void TinhGiaTriConLai_CD_Khac( IList<TTNVLRow> nvlRows, ThanhPhamData thanhPham)
        {

            string donViThanhPham = ChuanHoaDonVi(thanhPham.DonVi);

            var ketQuaDaTinh =
                new List<KetQuaGiaTriConLai>(nvlRows.Count);

            foreach (TTNVLRow nvl in nvlRows)
            {
                // Giá trị mặc định khi chưa có công thức phù hợp.
                var ketQua = new KetQuaGiaTriConLai
                {
                    KlConLai = null,
                    CdConLai = null
                };

                ketQuaDaTinh.Add(ketQua);

                if (nvl == null)
                    continue;

                string donViNvl = ChuanHoaDonVi(nvl.DonVi);

                bool cungDonVi = string.Equals( donViNvl, donViThanhPham, StringComparison.OrdinalIgnoreCase);


                // Cùng đơn vị M.
                if (donViThanhPham == "M" && cungDonVi)
                {
                    TinhGiaTriConLai_CungDonViM(nvl, thanhPham, ketQua);
                    continue;
                }

                // Đơn vị khác nhau: KG => M.
                if (!cungDonVi)
                {
                    try
                    {
                        TinhGiaTriConLai_CD_Khac_KhacDonVi( nvl, thanhPham, ketQua);
                    }
                    catch
                    {
                        ketQua.KlConLai = null;
                        ketQua.CdConLai = null;
                    }

                    continue;
                }
            }

            GanKetQuaGiaTriConLai(nvlRows, ketQuaDaTinh);
        }

        private static void TinhGiaTriConLai_CD_Khac_KhacDonVi( TTNVLRow nvl, ThanhPhamData thanhPham, KetQuaGiaTriConLai ketQua)
        {
            double chieuDaiNvl = nvl.CdConLai ?? 0;
            double khoiLuongNVL = nvl.KlConLai ?? 0;

            // Chuyển đổi từ m sang kg
            double khoiLuongTP = Convert.ToDouble(thanhPham.ChieuDai * thanhPham.ChuyenDoi);

            ketQua.KlConLai = Math.Round( Math.Max(0, khoiLuongNVL - khoiLuongTP),5);

            ketQua.CdConLai = chieuDaiNvl == 0 ? 0 : Math.Round(Math.Max(0, chieuDaiNvl - Convert.ToDouble(thanhPham.ChieuDai)) ,5)  ;

        }

        private static void TinhGiaTriConLai_CungDonViM( TTNVLRow nvl,  ThanhPhamData thanhPham, KetQuaGiaTriConLai ketQua)
        {
            double chieuDaiNvl = nvl.CdConLai ?? 0;
            double chieuDaiThanhPham = Convert.ToDouble(thanhPham.ChieuDai);

            double chieuDaiConLai = chieuDaiNvl - HE_SO_CHUYEN_DOI_CD * chieuDaiThanhPham;

            ketQua.CdConLai = Math.Max(0, chieuDaiConLai);
            ketQua.KlConLai = null;
        }

        private static string ChuanHoaDonVi(string donVi)
        {
            return (donVi ?? string.Empty)
                .Trim()
                .ToUpperInvariant();
        }

        private void DatGiaTriConLaiChoToanBang(double? klConLai, double? cdConLai)
        {
            foreach (TTNVLRow nvl in _nvlRows)
            {
                nvl.KlConLai = klConLai;
                nvl.CdConLai = cdConLai;
            }
        }

        private void LamMoiSauKhiCapNhatGiaTriConLai()
        {
            _nvlSource.ResetBindings(false);
            ApDungQuyenNhapTayChoTatCaDong();
            RefreshBomRowStyles();
            dtgTTNVL.Refresh();
        }

        private void LuuQuyenMacDinhCuaCotConLai()
        {
            if (_daLuuQuyenCotConLai) return;

            if (dtgTTNVL.Columns.Contains(nameof(TTNVLRow.KlConLai)))
            {
                _klConLaiReadOnlyMacDinh =
                    dtgTTNVL.Columns[nameof(TTNVLRow.KlConLai)].ReadOnly;
            }

            if (dtgTTNVL.Columns.Contains(nameof(TTNVLRow.CdConLai)))
            {
                _cdConLaiReadOnlyMacDinh =
                    dtgTTNVL.Columns[nameof(TTNVLRow.CdConLai)].ReadOnly;
            }

            _daLuuQuyenCotConLai = true;
        }

        private void ApDungQuyenNhapTayChoTatCaDong()
        {
            if (!_daLuuQuyenCotConLai) return;

            DataGridViewColumn cotKL = dtgTTNVL.Columns.Contains(nameof(TTNVLRow.KlConLai))
                ? dtgTTNVL.Columns[nameof(TTNVLRow.KlConLai)]
                : null;

            DataGridViewColumn cotCD = dtgTTNVL.Columns.Contains(nameof(TTNVLRow.CdConLai))
                ? dtgTTNVL.Columns[nameof(TTNVLRow.CdConLai)]
                : null;

            bool choPhepNhapTay = _dangNhapTayToanBang && !LaCongDoanHanNoi();

            if (cotKL != null)
                cotKL.ReadOnly = choPhepNhapTay ? false : _klConLaiReadOnlyMacDinh;

            if (cotCD != null)
                cotCD.ReadOnly = choPhepNhapTay ? false : _cdConLaiReadOnlyMacDinh;

            foreach (DataGridViewRow dgvRow in dtgTTNVL.Rows)
            {
                if (!(dgvRow.DataBoundItem is TTNVLRow))
                    continue;

                if (cotKL != null)
                {
                    dgvRow.Cells[nameof(TTNVLRow.KlConLai)].ReadOnly =
                        choPhepNhapTay ? false : _klConLaiReadOnlyMacDinh;
                }

                if (cotCD != null)
                {
                    dgvRow.Cells[nameof(TTNVLRow.CdConLai)].ReadOnly =
                        choPhepNhapTay ? false : _cdConLaiReadOnlyMacDinh;
                }
            }
        }

        private TTNVLRow MapDataRowToNvlRow(DataRow src)
        {
            return new TTNVLRow
            {
                Id = GetInt(src, "id"),
                TTThanhPhan_ID = GetInt(src, "TTThanhPhan_ID"),
                DanhSachMaSP_ID = GetInt(src, "NVL_DanhSachMaSP_ID")
                    ?? GetInt(src, "DanhSachMaSP_ID"),

                BinNVL = GetString(src, "BinNVL"),
                CongDoan = GetInt(src, "CongDoan") ?? -1,
                KlBatDau = GetDouble(src, "KlBatDau") ?? -1,
                CdBatDau = GetDouble(src, "CdBatDau") ?? -1,
                KlConLai = GetDouble(src, "KlConLai"),
                CdConLai = GetDouble(src, "CdConLai"),
                DuongKinhSoiDong = GetDouble(src, "DuongKinhSoiDong") ?? -1,
                SoSoi = GetInt(src, "SoSoi") ?? -1,
                KetCauLoi = GetDouble(src, "KetCauLoi") ?? -1,
                DuongKinhSoiMach = GetDouble(src, "DuongKinhSoiMach") ?? -1,
                QC = GetString(src, "QC"),

                MaNVL = GetString(src, "MaNVL"),
                DonVi = GetString(src, "DonViNVL"),
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
            // Phòng vệ nếu sự kiện khối lượng riêng được nối lại trong tương lai.
            ThanhPhamData thanhPham = GetThanhPhamData?.Invoke()
                ?? new ThanhPhamData();
            OnThanhPhamSoLieuChanged(thanhPham);
        }

        private void ClearGridKeepHeader()
        {
            ResetNvlState();
            _dangNhapTayToanBang = false;
            _nvlRows.Clear();
            _nvlSource.ResetBindings(false);
            ApDungQuyenNhapTayChoTatCaDong();
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

        private static void GanKetQuaGiaTriConLai( IList<TTNVLRow> nvlRows, IList<KetQuaGiaTriConLai> ketQuaDaTinh = null)
        {
            if (nvlRows == null)
                throw new ArgumentNullException(nameof(nvlRows));

            if (nvlRows.Count == 0)
                return;

            // Không truyền kết quả hoặc danh sách kết quả rỗng:
            // đặt toàn bộ giá trị còn lại bằng 0.
            if (ketQuaDaTinh == null || ketQuaDaTinh.Count == 0)
            {
                foreach (TTNVLRow nvl in nvlRows)
                {
                    if (nvl == null)
                        continue;

                    nvl.KlConLai = 0;
                    nvl.CdConLai = 0;
                }

                return;
            }

            // Có truyền kết quả thì số kết quả phải khớp số dòng.
            if (ketQuaDaTinh.Count != nvlRows.Count)
            {
                throw new InvalidOperationException(
                    $"Không thể gán kết quả. Số dòng NVL là {nvlRows.Count}, " +
                    $"nhưng số kết quả là {ketQuaDaTinh.Count}.");
            }

            for (int i = 0; i < nvlRows.Count; i++)
            {
                TTNVLRow nvl = nvlRows[i];

                if (nvl == null)
                    continue;

                KetQuaGiaTriConLai ketQua = ketQuaDaTinh[i];

                // Dòng không có kết quả thì gán cả hai trường bằng 0.
                if (ketQua == null)
                {
                    nvl.KlConLai = 0;
                    nvl.CdConLai = 0;
                    continue;
                }

                // Giá trị nào không có thì riêng giá trị đó bằng 0.
                nvl.KlConLai = ketQua.KlConLai ?? 0;
                nvl.CdConLai = ketQua.CdConLai ?? 0;
            }
        }


        private sealed class KetQuaGiaTriConLai
        {
            public double? KlConLai { get; set; }
            public double? CdConLai { get; set; }
        }

    }

}

