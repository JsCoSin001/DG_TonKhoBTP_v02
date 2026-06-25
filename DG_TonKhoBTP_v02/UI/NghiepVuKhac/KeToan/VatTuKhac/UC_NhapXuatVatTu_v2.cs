using DG_TonKhoBTP_v02.Database;
using DG_TonKhoBTP_v02.Models;
using DG_TonKhoBTP_v02.Models.KeToan.VatTuKhac;
using DG_TonKhoBTP_v02.UI.Helper.AutoSearchWithCombobox;
using System;
using System.Data;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DG_TonKhoBTP_v02.UI.NghiepVuKhac.KeToan.VatTuKhac
{
    public partial class UC_NhapXuatVatTu_v2 : UserControl
    {
        private string NguoiLam => UserContext.UserName;

        private readonly KieuNhapXuat_Model _model;

        private ComboBoxSearchHelper _searchHelper;
        private ComboBoxSearchHelper _khoHangSearchHelper;
        private ComboBoxSearchHelper _nccSearchHelper;

        private int? _selectedDanhSachKhoId;
        private string _selectedTenKho;

        private int? _selectedDanhSachNccId;
        private string _selectedTenNcc;

        private int? _selectedDanhSachMaSPId;
        private int? _selectedThongTinDatHangId;
        private decimal _selectedSoLuongYeuCau;
        private decimal _selectedSoLuongTon;

        private bool IsDichVu =>
            _model.IsDichVu
            || string.Equals(_model.Ten, TenKieuNhapXuat.DICH_VU, StringComparison.OrdinalIgnoreCase);

        public UC_NhapXuatVatTu_v2(KieuNhapXuat_Model model)
        {
            InitializeComponent();

            _model = model ?? throw new ArgumentNullException(nameof(model));

            InitUITheoKieuNhapXuat();
            InitComboBoxSearch();

            InitTenPhieu();

            Disposed += UC_NhapXuatVatTu_v2_Disposed;
        }

        private void InitTenPhieu()
        {
            ngayNhap_Xuat.ValueChanged -= ngayNhap_Xuat_ValueChanged;
            ngayNhap_Xuat.ValueChanged += ngayNhap_Xuat_ValueChanged;

            CapNhatTenPhieu();
        }

        private void ngayNhap_Xuat_ValueChanged(object sender, EventArgs e)
        {
            CapNhatTenPhieu();
        }

        private void CapNhatTenPhieu()
        {
            DateTime parsedNgay = ngayNhap_Xuat.Value.Date;
            bool isNhapKho = _model.IsNhap;

            int soThuTu = NhapXuatVatTu_DB.GetSoLuongXuatNhapThangHienTai(isNhapKho, parsedNgay) + 1;

            string prefix = _model.IsNhap ? "KNK" : "KXK";
            string tenPhieu = $"{prefix}{parsedNgay:yy/MM}-{soThuTu:D4}";

            tbMaPhieu.Text = tenPhieu;
        }

        private void InitUITheoKieuNhapXuat()
        {
            lblTitle.Text = _model.Ten;

            cbxTim.DropDownStyle = ComboBoxStyle.DropDown;
            cbxKhoHang.DropDownStyle = ComboBoxStyle.DropDown;
            cbxNcc.DropDownStyle = ComboBoxStyle.DropDown;

            if (IsDichVu)
            {
                label7.Text = "Tên dịch vụ";
                label17.Text = "Mã";
                label9.Text = "Đơn vị";
            }
            else
            {
                label7.Text = "Tên vật tư";
                label17.Text = "Mã vật tư";
                label9.Text = "Đơn vị";
            }

            label2.Text = _model.IsNhap || IsDichVu ? "Ngày nhập" : "Ngày xuất";
            label1.Text = _model.IsNhap || IsDichVu ? "Người giao" : "Người nhận";
            label11.Text = _model.IsNhap || IsDichVu ? "SL Nhập" : "SL Xuất";
        }

        private void InitComboBoxSearch()
        {
            _searchHelper = new ComboBoxSearchHelper(cbxTim, QuerySearchAsync)
            {
                DisplayColumn = "ten",
                DebounceMs = 400
            };

            _searchHelper.ItemSelected += SearchHelper_ItemSelected;
            _searchHelper.Cleared += SearchHelper_Cleared;

            _khoHangSearchHelper = new ComboBoxSearchHelper(cbxKhoHang, QueryKhoHangSearchAsync)
            {
                DisplayColumn = "TenKho",
                DebounceMs = 400,
                SelectedTextBehavior = ComboBoxSelectedTextBehavior.FillDisplayText
            };

            _khoHangSearchHelper.ItemSelected += KhoHangSearchHelper_ItemSelected;
            _khoHangSearchHelper.Cleared += KhoHangSearchHelper_Cleared;
            cbxKhoHang.TextUpdate += cbxKhoHang_TextUpdate;

            _nccSearchHelper = new ComboBoxSearchHelper(cbxNcc, QueryNccSearchAsync)
            {
                DisplayColumn = "TenNCC",
                DebounceMs = 400,
                SelectedTextBehavior = ComboBoxSelectedTextBehavior.FillDisplayText
            };

            _nccSearchHelper.ItemSelected += NccSearchHelper_ItemSelected;
            _nccSearchHelper.Cleared += NccSearchHelper_Cleared;
            cbxNcc.TextUpdate += cbxNcc_TextUpdate;
        }

        private Task<DataTable> QuerySearchAsync(string keyword, CancellationToken cancellationToken)
        {
            int? danhSachKhoId = GetSelectedKhoId();
            string nguoiLam = NguoiLam;

            return Task.Run(() =>
                NhapXuatVatTu_DB.TimVatTuDichVu(
                    _model,
                    keyword,
                    danhSachKhoId,
                    nguoiLam,
                    cancellationToken),
                cancellationToken);
        }

        private Task<DataTable> QueryKhoHangSearchAsync(string keyword, CancellationToken cancellationToken)
        {
            return Task.Run(() =>
                NhapXuatVatTu_DB.TimKhoHang(keyword, cancellationToken),
                cancellationToken);
        }

        private Task<DataTable> QueryNccSearchAsync(string keyword, CancellationToken cancellationToken)
        {
            return Task.Run(() =>
                NhapXuatVatTu_DB.TimNhaCungCap(keyword, cancellationToken),
                cancellationToken);
        }

        private void KhoHangSearchHelper_ItemSelected(DataRowView row)
        {
            if (row == null)
                return;

            _selectedDanhSachKhoId = GetNullableInt(row, "id");
            _selectedTenKho = GetString(row, "TenKho");

            //_searchHelper?.Reset();
        }

        private void KhoHangSearchHelper_Cleared()
        {
            ClearSelectedKhoHang();
            _searchHelper?.Reset();
        }

        private void cbxKhoHang_TextUpdate(object sender, EventArgs e)
        {
            if (!_selectedDanhSachKhoId.HasValue && string.IsNullOrWhiteSpace(_selectedTenKho))
                return;

            ClearSelectedKhoHang();
            _searchHelper?.Reset();
        }

        private void ClearSelectedKhoHang()
        {
            _selectedDanhSachKhoId = null;
            _selectedTenKho = string.Empty;
        }

        private void NccSearchHelper_ItemSelected(DataRowView row)
        {
            if (row == null)
                return;

            _selectedDanhSachNccId = GetNullableInt(row, "id");
            _selectedTenNcc = GetString(row, "TenNCC");
        }

        private void NccSearchHelper_Cleared()
        {
            ClearSelectedNcc();
        }

        private void cbxNcc_TextUpdate(object sender, EventArgs e)
        {
            if (!_selectedDanhSachNccId.HasValue && string.IsNullOrWhiteSpace(_selectedTenNcc))
                return;

            ClearSelectedNcc();
        }

        private void ClearSelectedNcc()
        {
            _selectedDanhSachNccId = null;
            _selectedTenNcc = string.Empty;
        }

        private void SearchHelper_ItemSelected(DataRowView row)
        {
            if (row == null)
                return;

            _selectedThongTinDatHangId = GetNullableInt(row, "ThongTinDatHang_ID");
            _selectedDanhSachMaSPId = GetNullableInt(row, "DanhSachMaSP_ID");
            _selectedSoLuongYeuCau = GetDecimal(row, "SoLuongYeuCau");
            _selectedSoLuongTon = GetDecimal(row, "SoLuongTon");

            tbTen.Text = GetString(row, "ten");

            if (IsDichVu)
            {
                tbID.Clear();
                tbMa.Clear();
                tbDonVi.Clear();

                _selectedDanhSachMaSPId = null;
                SetNumericValue(nbrSLYeuCau, 0);
            }
            else
            {
                tbID.Text = _selectedDanhSachMaSPId?.ToString() ?? string.Empty;
                tbMa.Text = GetString(row, "ma");
                tbDonVi.Text = GetString(row, "donvi");

                SetNumericValue(nbrSLYeuCau, _selectedSoLuongYeuCau);
            }
        }

        private void SearchHelper_Cleared()
        {
            ClearSelectedVatTu();
        }

        private void ClearSelectedVatTu()
        {
            _selectedDanhSachMaSPId = null;
            _selectedThongTinDatHangId = null;
            _selectedSoLuongYeuCau = 0;
            _selectedSoLuongTon = 0;

            tbID.Clear();
            tbTen.Clear();
            tbMa.Clear();
            tbDonVi.Clear();

            SetNumericValue(nbrSLYeuCau, 0);
        }

        private int? GetSelectedKhoId()
        {
            if (_selectedDanhSachKhoId.HasValue)
                return _selectedDanhSachKhoId.Value;

            int? idFromSelectedValue = TryConvertToNullableInt(cbxKhoHang.SelectedValue);
            if (idFromSelectedValue.HasValue)
                return idFromSelectedValue.Value;

            if (cbxKhoHang.SelectedItem is DataRowView drv)
            {
                int? idFromRow = GetNullableInt(drv, "id");
                if (idFromRow.HasValue)
                    return idFromRow.Value;

                idFromRow = GetNullableInt(drv, "DanhSachKho_ID");
                if (idFromRow.HasValue)
                    return idFromRow.Value;
            }

            int? idFromObject = TryReadIntProperty(cbxKhoHang.SelectedItem, "id");
            if (idFromObject.HasValue)
                return idFromObject.Value;

            idFromObject = TryReadIntProperty(cbxKhoHang.SelectedItem, "Id");
            if (idFromObject.HasValue)
                return idFromObject.Value;

            return null;
        }

        private static int? TryReadIntProperty(object source, string propertyName)
        {
            if (source == null)
                return null;

            var properties = source.GetType().GetProperties();

            foreach (var prop in properties)
            {
                if (!string.Equals(prop.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                    continue;

                object value = prop.GetValue(source, null);
                return TryConvertToNullableInt(value);
            }

            return null;
        }

        private static int? GetNullableInt(DataRowView row, string columnName)
        {
            if (row == null || row.Row == null || row.Row.Table == null)
                return null;

            if (!row.Row.Table.Columns.Contains(columnName))
                return null;

            return TryConvertToNullableInt(row[columnName]);
        }

        private static int? TryConvertToNullableInt(object value)
        {
            if (value == null || value == DBNull.Value)
                return null;

            if (value is int intValue)
                return intValue;

            if (value is long longValue)
                return Convert.ToInt32(longValue);

            if (value is decimal decimalValue)
                return Convert.ToInt32(decimalValue);

            if (int.TryParse(Convert.ToString(value), out int parsed))
                return parsed;

            return null;
        }

        private static decimal GetDecimal(DataRowView row, string columnName)
        {
            if (row == null || row.Row == null || row.Row.Table == null)
                return 0;

            if (!row.Row.Table.Columns.Contains(columnName))
                return 0;

            object value = row[columnName];

            if (value == null || value == DBNull.Value)
                return 0;

            if (value is decimal decimalValue)
                return decimalValue;

            if (value is double doubleValue)
                return Convert.ToDecimal(doubleValue);

            if (value is float floatValue)
                return Convert.ToDecimal(floatValue);

            if (value is int intValue)
                return intValue;

            if (value is long longValue)
                return longValue;

            string text = Convert.ToString(value);

            if (decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal invariantResult))
                return invariantResult;

            if (decimal.TryParse(text, NumberStyles.Any, CultureInfo.CurrentCulture, out decimal currentResult))
                return currentResult;

            return 0;
        }

        private static string GetString(DataRowView row, string columnName)
        {
            if (row == null || row.Row == null || row.Row.Table == null)
                return string.Empty;

            if (!row.Row.Table.Columns.Contains(columnName))
                return string.Empty;

            object value = row[columnName];

            if (value == null || value == DBNull.Value)
                return string.Empty;

            return Convert.ToString(value);
        }

        private static void SetNumericValue(NumericUpDown control, decimal value)
        {
            if (control == null)
                return;

            if (value > control.Maximum)
                control.Maximum = value;

            if (value < control.Minimum)
                control.Minimum = value;

            control.Value = value;
        }

        private void UC_NhapXuatVatTu_v2_Disposed(object sender, EventArgs e)
        {
            if (_searchHelper != null)
            {
                _searchHelper.ItemSelected -= SearchHelper_ItemSelected;
                _searchHelper.Cleared -= SearchHelper_Cleared;
                _searchHelper.Dispose();
                _searchHelper = null;
            }

            if (_khoHangSearchHelper != null)
            {
                _khoHangSearchHelper.ItemSelected -= KhoHangSearchHelper_ItemSelected;
                _khoHangSearchHelper.Cleared -= KhoHangSearchHelper_Cleared;
                _khoHangSearchHelper.Dispose();
                _khoHangSearchHelper = null;
            }

            if (_nccSearchHelper != null)
            {
                _nccSearchHelper.ItemSelected -= NccSearchHelper_ItemSelected;
                _nccSearchHelper.Cleared -= NccSearchHelper_Cleared;
                _nccSearchHelper.Dispose();
                _nccSearchHelper = null;
            }

            cbxKhoHang.TextUpdate -= cbxKhoHang_TextUpdate;
            cbxNcc.TextUpdate -= cbxNcc_TextUpdate;

            _selectedDanhSachKhoId = null;
            _selectedTenKho = string.Empty;
            _selectedDanhSachNccId = null;
            _selectedTenNcc = string.Empty;
            _selectedDanhSachMaSPId = null;
            _selectedThongTinDatHangId = null;
            _selectedSoLuongYeuCau = 0;
            _selectedSoLuongTon = 0;
        }

        private void btnLuu_Click(object sender, EventArgs e)
        {
            // Khi bổ sung insert/update LichSuXuatNhap, dùng các state đã chọn:
            // DanhSachKho_ID = _selectedDanhSachKhoId
            // DanhSachNCC_ID = _selectedDanhSachNccId
            // NhaCC = _selectedTenNcc
        }
    }
}