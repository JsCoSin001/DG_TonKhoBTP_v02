using DG_TonKhoBTP_v02.Database;
using DG_TonKhoBTP_v02.Models;
using DG_TonKhoBTP_v02.Models.KeToan.VatTuKhac;
using DG_TonKhoBTP_v02.UI.Helper.AutoSearchWithCombobox;
using System;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using DG_TonKhoBTP_v02.Printer;
using System.Collections.Generic;
using static DG_TonKhoBTP_v02.Printer.A4.PrinterModel;

namespace DG_TonKhoBTP_v02.UI.NghiepVuKhac.KeToan.VatTuKhac
{
    public partial class UC_NhapXuatVatTu_v2 : UserControl
    {
        private string NguoiLam => UserContext.UserName;

        private readonly KieuNhapXuat_Model _model;

        private ComboBoxSearchHelper _searchHelper;
        private ComboBoxSearchHelper _khoHangSearchHelper;
        private ComboBoxSearchHelper _nccSearchHelper;
        private ComboBoxSearchHelper _editSearchHelper;

        private int? _selectedDanhSachKhoId;
        private string _selectedTenKho;

        private int? _selectedDanhSachNccId;
        private string _selectedTenNcc;

        private int? _selectedDanhSachMaSPId;
        private int? _selectedThongTinDatHangId;
        private int? _selectedDanhSachDatHangId;
        private decimal _selectedSoLuongYeuCau;
        private decimal _selectedSoLuongTon;

        private string _currentTenPhieu;
        private int? _currentDanhSachDatHangId;

        private bool _isEditMode;
        private string _lastEditSearchValidationKey;
        private string _lastTimVatTuSearchValidationKey;

        private int? _editingLichSuXuatNhapId;
        private string _editingTenPhieu;
        private int? _editingOldDanhSachMaSPId;
        private int? _editingOldDanhSachKhoId;
        private bool _editingIsGroupXuat;
        private DateTime? _editingOldNgay;
        private bool _isChangingEditDateProgrammatically;

        private bool IsDichVu =>
            _model.IsDichVu
            || string.Equals(_model.Ten, TenKieuNhapXuat.DICH_VU, StringComparison.OrdinalIgnoreCase);

        private bool IsNhapKhoKhac =>
            _model.IsNhap
            && !_model.IsDichVu
            && _model.IsKhac;

        private bool IsXuat =>
            !_model.IsNhap
            && !_model.IsDichVu;

        public UC_NhapXuatVatTu_v2(KieuNhapXuat_Model model)
        {
            InitializeComponent();

            _model = model ?? throw new ArgumentNullException(nameof(model));

            ngayNhap_Xuat.Value = DateTime.Today;

            InitUITheoKieuNhapXuat();
            InitNumericControls();
            InitGrid();
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
            if (_isChangingEditDateProgrammatically)
                return;

            if (_isEditMode)
            {
                if (!_editingOldNgay.HasValue)
                    return;

                DateTime ngayCu = _editingOldNgay.Value.Date;
                DateTime ngayMoi = ngayNhap_Xuat.Value.Date;

                if (ngayCu.Year != ngayMoi.Year || ngayCu.Month != ngayMoi.Month)
                {
                    _isChangingEditDateProgrammatically = true;
                    try
                    {
                        ngayNhap_Xuat.Value = ngayCu;
                    }
                    finally
                    {
                        _isChangingEditDateProgrammatically = false;
                    }

                    FrmWaiting.ShowGifAlert(
                        "Chỉ được phép sửa ngày trong cùng tháng của phiếu.",
                        myIcon: EnumStore.Icon.Warning);
                }

                // Khi edit chỉ thay đổi ngày, không tự tính lại mã phiếu.
                return;
            }

            if (!string.IsNullOrWhiteSpace(_currentTenPhieu))
                return;

            CapNhatTenPhieu();
        }

        private void CapNhatTenPhieu()
        {
            if (_isEditMode || !string.IsNullOrWhiteSpace(_currentTenPhieu))
                return;

            DateTime parsedNgay = ngayNhap_Xuat.Value.Date;
            string prefix = GetTienToPhieu();
            tbMaPhieu.Text = SoChungTu_DB.GetMaDuKien(prefix, parsedNgay);
        }

        private string GetTienToPhieu()
        {
            return _model.IsNhap ? "KNK" : "KXK";
        }

        private void InitUITheoKieuNhapXuat()
        {
            lblTitle.Text = _model.Ten;

            cbxTim.DropDownStyle = ComboBoxStyle.DropDown;
            cbxKhoHang.DropDownStyle = ComboBoxStyle.DropDown;
            cbxNcc.DropDownStyle = ComboBoxStyle.DropDown;
            cbxTimKiem_Edit.DropDownStyle = ComboBoxStyle.DropDown;

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

        private void InitNumericControls()
        {
            nbrSLYeuCau.Minimum = 0;
            nbrSLYeuCau.Maximum = 999999999;

            nbrNhanThucTe.Minimum = 0;
            nbrNhanThucTe.Maximum = 999999999;

            nbrDonGia.Minimum = 0;
            nbrDonGia.Maximum = 999999999;
        }

        private void InitGrid()
        {
            dgvChiTietDon.AutoGenerateColumns = false;
            dgvChiTietDon.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvChiTietDon.MultiSelect = false;
            dgvChiTietDon.CellDoubleClick -= dgvChiTietDon_CellDoubleClick;
            dgvChiTietDon.CellDoubleClick += dgvChiTietDon_CellDoubleClick;

            dgvChiTietDon.CellContentClick -= dgvChiTietDon_CellContentClick;
            dgvChiTietDon.CellContentClick += dgvChiTietDon_CellContentClick;
        }

        private void InitComboBoxSearch()
        {
            _searchHelper = new ComboBoxSearchHelper(cbxTim, QuerySearchAsync)
            {
                DisplayColumn = "ten",
                DebounceMs = 400,
                CanSearch = CanSearchTimVatTu
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

            // Ô tìm kiếm phiếu đã lưu.
            _editSearchHelper = new ComboBoxSearchHelper(cbxTimKiem_Edit, QueryEditSearchAsync)
            {
                DisplayColumn = "TenPhieu",
                DebounceMs = 400,
                SelectedTextBehavior = ComboBoxSelectedTextBehavior.FillDisplayText,
                CanSearch = CanSearchEditPhieu
            };

            _editSearchHelper.ItemSelected += EditSearchHelper_ItemSelected;
            _editSearchHelper.Cleared += EditSearchHelper_Cleared;
        }

        private Task<DataTable> QuerySearchAsync(string keyword, CancellationToken cancellationToken)
        {
            int? danhSachKhoId = GetSelectedKhoId();
            string nguoiLam = NguoiLam;

            return Task.Run(() =>
                NhapXuatVatTu_DB.TimVatTu(
                    _model,
                    keyword,
                    danhSachKhoId,
                    nguoiLam,
                    cancellationToken),
                cancellationToken);
        }

        private bool CanSearchTimVatTu(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return false;

            if (string.IsNullOrWhiteSpace(NguoiLam))
            {
                ShowTimVatTuSearchValidationMessage(
                    "LOGIN_REQUIRED",
                    "Bạn cần đăng nhập trước khi tìm vật tư.");

                return false;
            }

            if (_model == null)
            {
                ShowTimVatTuSearchValidationMessage(
                    "MODEL_REQUIRED",
                    "Không xác định được kiểu nhập/xuất.");

                return false;
            }

            ResetTimVatTuSearchValidationMessage();
            return true;
        }

        private void ShowTimVatTuSearchValidationMessage(string validationKey, string message)
        {
            if (string.Equals(_lastTimVatTuSearchValidationKey, validationKey, StringComparison.Ordinal))
                return;

            _lastTimVatTuSearchValidationKey = validationKey;

            FrmWaiting.ShowGifAlert(message, myIcon: EnumStore.Icon.Warning);
        }

        private void ResetTimVatTuSearchValidationMessage()
        {
            _lastTimVatTuSearchValidationKey = string.Empty;
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

        private Task<DataTable> QueryEditSearchAsync(string keyword, CancellationToken cancellationToken)
        {
            string nguoiLam = NguoiLam;

            return Task.Run(() =>
                NhapXuatVatTu_DB.TimPhieuDaLuu(
                    _model,
                    keyword,
                    null,
                    nguoiLam,
                    cancellationToken),
                cancellationToken);
        }

        private bool CanSearchEditPhieu(string keyword)
        {
            if (string.IsNullOrWhiteSpace(NguoiLam))
            {
                ShowEditSearchValidationMessage(
                    "LOGIN_REQUIRED",
                    "Bạn cần đăng nhập trước khi sử dụng chức năng này.");
                return false;
            }

            if (_model == null)
            {
                ShowEditSearchValidationMessage(
                    "MODEL_REQUIRED",
                    "Không xác định được kiểu nhập/xuất.");
                return false;
            }


            ResetEditSearchValidationMessage();
            return true;
        }

        private void ShowEditSearchValidationMessage(string validationKey, string message)
        {
            if (string.Equals(_lastEditSearchValidationKey, validationKey, StringComparison.Ordinal))
                return;

            _lastEditSearchValidationKey = validationKey;

            FrmWaiting.ShowGifAlert("Thiếu điều kiện tìm kiếm");
        }

        private void ResetEditSearchValidationMessage()
        {
            _lastEditSearchValidationKey = string.Empty;
        }

        private void KhoHangSearchHelper_ItemSelected(DataRowView row)
        {
            if (row == null)
                return;

            _selectedDanhSachKhoId = GetNullableInt(row, "id");
            _selectedTenKho = GetString(row, "TenKho");

            ResetTimVatTuSearchValidationMessage();
            ResetEditSearchValidationMessage();

        }

        private void KhoHangSearchHelper_Cleared()
        {
            ClearSelectedKhoHang();
        }

        private void cbxKhoHang_TextUpdate(object sender, EventArgs e)
        {
            if (!_selectedDanhSachKhoId.HasValue && string.IsNullOrWhiteSpace(_selectedTenKho))
                return;

            ClearSelectedKhoHang();
        }

        private void ClearSelectedKhoHang()
        {
            _selectedDanhSachKhoId = null;
            _selectedTenKho = string.Empty;

            ResetTimVatTuSearchValidationMessage();
            ResetEditSearchValidationMessage();
        }

        private void CapNhatTonTheoKhoHienTaiNeuCan()
        {
            // Không dùng nữa.
            // Kho chỉ dùng để lưu DanhSachKho_ID khi bấm Lưu, không tham gia tìm kiếm/tính tồn/số lượng.
        }

        private void NccSearchHelper_ItemSelected(DataRowView row)
        {
            if (row == null)
                return;

            _selectedDanhSachNccId = GetNullableInt(row, "id");
            _selectedTenNcc = GetString(row, "TenNCC");
        }

        private void SearchHelper_Cleared()
        {
            ResetTimVatTuSearchValidationMessage();
            ClearSelectedVatTu();
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
            _selectedSoLuongYeuCau = Math.Abs(GetDecimal(row, "SoLuongYeuCau"));
            _selectedSoLuongTon = Math.Abs(GetDecimal(row, "SoLuongTon"));

            tbTen.Text = GetString(row, "ten");

            if (IsDichVu)
            {
                tbID.Clear();
                tbMa.Clear();
                tbDonVi.Clear();

                _selectedDanhSachMaSPId = null;
                SetNumericValue(nbrSLYeuCau, _selectedSoLuongYeuCau);
                SetNumericValue(nbrDonGia, GetDecimal(row, "DonGia"));
            }
            else
            {
                tbID.Text = _selectedDanhSachMaSPId?.ToString() ?? string.Empty;
                tbMa.Text = GetString(row, "ma");
                tbDonVi.Text = GetString(row, "donvi");

                decimal soLuongHienThi = IsXuat ? _selectedSoLuongTon : _selectedSoLuongYeuCau;
                SetNumericValue(nbrSLYeuCau, soLuongHienThi);

                if (IsXuat && soLuongHienThi > 0)
                    SetNumericMaximum(nbrNhanThucTe, soLuongHienThi);

                SetNumericValue(nbrDonGia, GetDecimal(row, "DonGia"));
            }
        }


        private void ClearSelectedVatTu()
        {
            _selectedDanhSachMaSPId = null;
            _selectedThongTinDatHangId = null;
            _selectedDanhSachDatHangId = null;
            _selectedSoLuongYeuCau = 0;
            _selectedSoLuongTon = 0;

            tbID.Clear();
            tbTen.Clear();
            tbMa.Clear();
            tbDonVi.Clear();

            SetNumericValue(nbrSLYeuCau, 0);
        }

        private void EditSearchHelper_ItemSelected(DataRowView row)
        {
            if (row == null)
                return;

            string tenPhieu = GetString(row, "TenPhieu");
            if (string.IsNullOrWhiteSpace(tenPhieu))
                return;

            LoadChiTietPhieu(tenPhieu);
        }

        private void EditSearchHelper_Cleared()
        {
            ResetEditSearchValidationMessage();
        }

        private void LoadChiTietPhieu(string tenPhieu)
        {
            DataTable dt = NhapXuatVatTu_DB.LoadChiTietTheoTenPhieu(
                _model,
                tenPhieu,
                null,
                NguoiLam);

            dgvChiTietDon.DataSource = dt;
            BoChonDongDgvChiTietDon();
            CapNhatTrangThaiNgayTheoChiTietPhieu();
        }
        private void BoChonDongDgvChiTietDon()
        {
            if (dgvChiTietDon == null || dgvChiTietDon.IsDisposed)
                return;

            dgvChiTietDon.BeginInvoke((MethodInvoker)(() =>
            {
                dgvChiTietDon.ClearSelection();
                dgvChiTietDon.CurrentCell = null;
            }));
        }

        private void btnLuu_Click(object sender, EventArgs e)
        {
            try
            {
                if (!TryBuildSaveModel(out NhapXuatVatTu_SaveModel saveModel))
                    return;

                bool wasEditMode = _isEditMode;
                NhapXuatVatTu_SaveResult saveResult;

                if (wasEditMode)
                {
                    saveResult = NhapXuatVatTu_DB.CapNhat(saveModel);
                    FrmWaiting.ShowGifAlert("Cập nhật thành công", myIcon: EnumStore.Icon.Success);
                }
                else
                {
                    saveResult = NhapXuatVatTu_DB.LuuMoi(saveModel);
                    _currentTenPhieu = saveResult.TenPhieu;

                    if (saveResult.DanhSachDatHangId.HasValue)
                        _currentDanhSachDatHangId = saveResult.DanhSachDatHangId;

                    tbMaPhieu.Text = saveResult.TenPhieu;
                }

                LoadChiTietPhieu(saveResult.TenPhieu);
                ResetFormSauKhiLuu();
            }
            catch (Exception ex)
            {
                FrmWaiting.ShowGifAlert("Lưu thất bại");
            }
        }

        private void btnHoanThanh_Click(object sender, EventArgs e)
        {
            if (!CoChiTietTrongGrid())
            {
                FrmWaiting.ShowGifAlert("Chưa có chi tiết nào trong phiếu", myIcon: EnumStore.Icon.Warning);
                return;
            }

            ResetFormKhiHoanThanh();
        }

        private bool TryBuildSaveModel(out NhapXuatVatTu_SaveModel saveModel)
        {
            saveModel = null;

            bool taoPhieuMoi = !_isEditMode && string.IsNullOrWhiteSpace(_currentTenPhieu);

            string tenPhieu = _isEditMode && !string.IsNullOrWhiteSpace(_editingTenPhieu)
                ? _editingTenPhieu
                : taoPhieuMoi
                    ? tbMaPhieu.Text?.Trim()
                    : _currentTenPhieu;

            if (string.IsNullOrWhiteSpace(tenPhieu))
            {
                FrmWaiting.ShowGifAlert("Mã phiếu không được để trống", myIcon: EnumStore.Icon.Warning);
                return false;
            }
            int? danhSachKhoId = IsDichVu ? null : GetSelectedKhoId();
            if (!IsDichVu && !danhSachKhoId.HasValue)
            {
                FrmWaiting.ShowGifAlert("Vui lòng chọn kho hàng.", myIcon: EnumStore.Icon.Warning);
                return false;
            }

            decimal soLuong = nbrNhanThucTe.Value;
            if (soLuong <= 0)
            {
                FrmWaiting.ShowGifAlert("Số lượng nhập/xuất phải lớn hơn 0.", myIcon: EnumStore.Icon.Warning);
                return false;
            }

            if (!ValidateNhaCungCap())
                return false;

            if (!ValidateVatTuDichVuDangChon())
                return false;

            if (IsXuat && !ValidateSoLuongXuat(soLuong))
                return false;

            if (_isEditMode
                && _editingOldNgay.HasValue
                && (_editingOldNgay.Value.Year != ngayNhap_Xuat.Value.Year
                    || _editingOldNgay.Value.Month != ngayNhap_Xuat.Value.Month))
            {
                FrmWaiting.ShowGifAlert(
                    "Chỉ được phép sửa ngày trong cùng tháng của phiếu.",
                    myIcon: EnumStore.Icon.Warning);
                return false;
            }

            int? danhSachDatHangId = IsNhapKhoKhac
                && !_isEditMode
                && _currentDanhSachDatHangId.HasValue
                    ? _currentDanhSachDatHangId
                    : _selectedDanhSachDatHangId;

            saveModel = new NhapXuatVatTu_SaveModel
            {
                Mode = _model,
                TaoPhieuMoi = taoPhieuMoi,
                LichSuXuatNhapId = _editingLichSuXuatNhapId,
                OldTenPhieu = _editingTenPhieu,
                OldDanhSachMaSPId = _editingOldDanhSachMaSPId,
                OldDanhSachKhoId = _editingOldDanhSachKhoId,
                OldNgay = _editingOldNgay,
                ThongTinDatHangId = _selectedThongTinDatHangId,
                DanhSachDatHangId = danhSachDatHangId,
                DanhSachMaSPId = _selectedDanhSachMaSPId,
                DanhSachKhoId = danhSachKhoId,
                DanhSachNCCId = _selectedDanhSachNccId,
                Ngay = ngayNhap_Xuat.Value.Date,
                NguoiGiaoNhan = tbNguoiGiao.Text?.Trim(),
                LyDo = tbxLyDo.Text?.Trim(),
                SoLuongNguoiNhap = soLuong,
                TenPhieu = tenPhieu,
                GhiChu = tbGhiChu.Text?.Trim(),
                NhaCC = _selectedDanhSachNccId.HasValue ? _selectedTenNcc : null,
                DonGia = nbrDonGia.Value,
                NguoiLam = NguoiLam
            };

            return true;
        }

        private bool ValidateNhaCungCap()
        {
            string nccText = cbxNcc.Text?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(nccText))
                return true;

            if (_selectedDanhSachNccId.HasValue
                && string.Equals(nccText, _selectedTenNcc, StringComparison.OrdinalIgnoreCase))
                return true;

            FrmWaiting.ShowGifAlert("Đối tượng thụ hưởng không hợp lệ");
            return false;
        }

        private bool ValidateVatTuDichVuDangChon()
        {
            if (IsDichVu)
            {
                if (!_selectedThongTinDatHangId.HasValue)
                {
                    FrmWaiting.ShowGifAlert("Vui lòng chọn dịch vụ cần xác nhận.");

                    return false;
                }

                return true;
            }

            if (!_selectedDanhSachMaSPId.HasValue)
            {
                FrmWaiting.ShowGifAlert("Vui lòng chọn vật tư từ danh sách tìm kiếm.", myIcon: EnumStore.Icon.Warning);

                return false;
            }

            if (!IsNhapKhoKhac && !IsXuat && !_selectedThongTinDatHangId.HasValue)
            {
                FrmWaiting.ShowGifAlert("Vui lòng chọn vật tư theo đơn đề nghị.", myIcon: EnumStore.Icon.Warning);
                return false;
            }

            return true;
        }

        private bool ValidateSoLuongXuat(decimal soLuong)
        {
            if (!_selectedDanhSachMaSPId.HasValue)
                return false;

            string excludeTenPhieu = _isEditMode ? _editingTenPhieu : null;

            decimal tonKhaDung = NhapXuatVatTu_DB.TinhTongTonVatTu(
                _selectedDanhSachMaSPId.Value,
                excludeTenPhieu,
                _editingOldDanhSachMaSPId);

            SetNumericValue(nbrSLYeuCau, tonKhaDung);
            SetNumericMaximum(nbrNhanThucTe, tonKhaDung > 0 ? tonKhaDung : 1);

            if (soLuong > tonKhaDung)
            {
                SetNumericValue(nbrNhanThucTe, tonKhaDung);
                FrmWaiting.ShowGifAlert("Số lượng xuất vượt tồn");
                return false;
            }

            return true;
        }

        private void dgvChiTietDon_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0)
                return;

            DataGridViewColumn column = dgvChiTietDon.Columns[e.ColumnIndex];
            if (column == null || !string.Equals(column.Name, "xoa", StringComparison.OrdinalIgnoreCase))
                return;

            DataGridViewRow gridRow = dgvChiTietDon.Rows[e.RowIndex];
            if (!(gridRow.DataBoundItem is DataRowView row))
                return;

            XoaDongChiTiet(row);
        }

        private void XoaDongChiTiet(DataRowView row)
        {
            if (row == null)
                return;

            int canEdit = GetInt(row, "CanEdit", 1);
            if (canEdit != 1)
            {
                FrmWaiting.ShowGifAlert("Liên hệ Kế Toán để mở khoá phiếu nếu cần xoá.");
                return;
            }

            string tenPhieu = GetString(row, "TenPhieu");
            string tenVatTu = GetString(row, "ten");
            decimal soLuong = Math.Abs(GetDecimal(row, "thucNhan"));

            string noiDungXacNhan = string.IsNullOrWhiteSpace(tenVatTu)
                ? $"Bạn có chắc muốn xoá dòng trong phiếu {tenPhieu}?"
                : $"Bạn có chắc muốn xoá {tenVatTu} - SL {soLuong:0.###} trong phiếu {tenPhieu}?";

            DialogResult confirm = MessageBox.Show(
                noiDungXacNhan,
                "Xác nhận xoá",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes)
                return;

            try
            {
                var deleteModel = new NhapXuatVatTu_DeleteModel
                {
                    Mode = _model,
                    LichSuXuatNhapId = GetNullableInt(row, "LichSuXuatNhap_ID"),
                    TenPhieu = tenPhieu,
                    DanhSachMaSPId = GetNullableInt(row, "DanhSachMaSP_ID"),
                    DanhSachKhoId = GetNullableInt(row, "DanhSachKho_ID"),
                    ThongTinDatHangId = GetNullableInt(row, "ThongTinDatHang_ID"),
                    DanhSachDatHangId = GetNullableInt(row, "DanhSachDatHang_ID"),
                    NguoiLam = NguoiLam
                };

                NhapXuatVatTu_DeleteResult result = NhapXuatVatTu_DB.Xoa(deleteModel);

                if (result.DeletedRows <= 0)
                {
                    FrmWaiting.ShowGifAlert("Không có dòng dữ liệu nào được xoá.", myIcon: EnumStore.Icon.Warning);
                    return;
                }

                XoaDongKhoiGridHoacReload(row, tenPhieu);

                if (_isEditMode && string.Equals(_editingTenPhieu, tenPhieu, StringComparison.OrdinalIgnoreCase))
                    ResetFormSauKhiLuu();

                FrmWaiting.ShowGifAlert("Xoá thành công", myIcon: EnumStore.Icon.Success);
            }
            catch (Exception ex)
            {
                FrmWaiting.ShowGifAlert("Xoá thất bại", myIcon: EnumStore.Icon.Warning);
            }
        }

        private void XoaDongKhoiGridHoacReload(DataRowView row, string tenPhieu)
        {
            if (row == null)
                return;

            DataTable table = row.Row?.Table;
            if (table == null)
                return;

            row.Row.Delete();
            table.AcceptChanges();

            if (table.Rows.Count == 0)
            {
                dgvChiTietDon.DataSource = table.Clone();

                if (string.Equals(_currentTenPhieu, tenPhieu, StringComparison.OrdinalIgnoreCase))
                {
                    _currentTenPhieu = null;
                    _currentDanhSachDatHangId = null;
                    tbMaPhieu.Clear();
                    CapNhatTenPhieu();
                }
            }

            CapNhatTrangThaiNgayTheoChiTietPhieu();
        }

        private void dgvChiTietDon_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0)
                return;

            if (e.ColumnIndex >= 0)
            {
                DataGridViewColumn column = dgvChiTietDon.Columns[e.ColumnIndex];
                if (column != null && string.Equals(column.Name, "xoa", StringComparison.OrdinalIgnoreCase))
                    return;
            }

            DataGridViewRow gridRow = dgvChiTietDon.Rows[e.RowIndex];
            if (!(gridRow.DataBoundItem is DataRowView row))
                return;

            int canEdit = GetInt(row, "CanEdit", 1);
            if (canEdit != 1)
            {
                FrmWaiting.ShowGifAlert("Liên hệ Kế Toán để mở các mã cần thao tác.");

                return;
            }

            FillFormForEdit(row);
        }

        private void FillFormForEdit(DataRowView row)
        {
            _isEditMode = true;
            _editingLichSuXuatNhapId = GetNullableInt(row, "LichSuXuatNhap_ID");
            _editingTenPhieu = GetString(row, "TenPhieu");
            _editingOldDanhSachMaSPId = GetNullableInt(row, "DanhSachMaSP_ID");
            _editingOldDanhSachKhoId = GetNullableInt(row, "DanhSachKho_ID");
            _editingIsGroupXuat = GetInt(row, "IsGroupXuat", 0) == 1;

            _selectedDanhSachDatHangId = GetNullableInt(row, "DanhSachDatHang_ID");
            _selectedThongTinDatHangId = GetNullableInt(row, "ThongTinDatHang_ID");
            _selectedDanhSachMaSPId = GetNullableInt(row, "DanhSachMaSP_ID");
            _selectedDanhSachKhoId = GetNullableInt(row, "DanhSachKho_ID");
            _selectedTenKho = GetString(row, "TenKho");
            _selectedDanhSachNccId = GetNullableInt(row, "DanhSachNCC_ID");
            _selectedTenNcc = GetString(row, "NhaCungCap");

            tbMaPhieu.Text = _editingTenPhieu;

            DateTime ngay;
            if (TryParseDbDate(GetString(row, "ngay"), out ngay))
            {
                _editingOldNgay = ngay.Date;
                CauHinhNgayEditTrongCungThang(ngay.Date);
                ngayNhap_Xuat.Enabled = true;
            }
            else
            {
                _editingOldNgay = null;
                ngayNhap_Xuat.Enabled = false;
            }

            tbNguoiGiao.Text = GetString(row, "NguoiGiao_Nhan");
            tbxLyDo.Text = GetString(row, "LyDo");
            tbGhiChu.Text = GetString(row, "ghiChu");

            cbxKhoHang.Text = _selectedTenKho;
            cbxNcc.Text = _selectedTenNcc;

            tbID.Text = _selectedDanhSachMaSPId?.ToString() ?? string.Empty;
            tbTen.Text = GetString(row, "ten");
            tbMa.Text = GetString(row, "ma");
            tbDonVi.Text = GetString(row, "donVi");

            decimal soLuongThucNhan = Math.Abs(GetDecimal(row, "thucNhan"));
            decimal donGia = GetDecimal(row, "donGia");

            if (IsXuat && _selectedDanhSachMaSPId.HasValue)
            {
                decimal tonKhaDung = NhapXuatVatTu_DB.TinhTongTonVatTu(
                    _selectedDanhSachMaSPId.Value,
                    _editingTenPhieu,
                    _editingOldDanhSachMaSPId);

                SetNumericValue(nbrSLYeuCau, tonKhaDung);
                SetNumericMaximum(nbrNhanThucTe, tonKhaDung > 0 ? tonKhaDung : 1);
            }
            else
            {
                SetNumericValue(nbrSLYeuCau, Math.Abs(GetDecimal(row, "yeuCau")));
            }

            SetNumericValue(nbrNhanThucTe, soLuongThucNhan);
            SetNumericValue(nbrDonGia, donGia);

            btnLuu.Text = "Cập nhật";
        }

        private void ResetFormSauKhiLuu()
        {
            ResetEditState();
            ResetChiTietDangNhap();
            CapNhatTrangThaiNgayTheoChiTietPhieu();
            cbxTim.Focus();
        }

        private void ResetFormKhiHoanThanh()
        {
            _currentTenPhieu = null;
            _currentDanhSachDatHangId = null;
            ResetEditState();

            tbNguoiGiao.Clear();
            tbxLyDo.Clear();
            tbGhiChu.Clear();

            cbxKhoHang.Text = string.Empty;
            cbxNcc.Text = string.Empty;
            cbxTimKiem_Edit.Text = string.Empty;

            ClearSelectedKhoHang();
            ClearSelectedNcc();
            ResetChiTietDangNhap();
            ClearGridGiuCot();

            ngayNhap_Xuat.Enabled = true;
            ngayNhap_Xuat.Value = DateTime.Now;
            CapNhatTenPhieu();
        }

        private void ResetChiTietDangNhap()
        {
            _searchHelper?.Reset();
            cbxTim.Text = string.Empty;
            ClearSelectedVatTu();
            SetNumericValue(nbrNhanThucTe, 0);
            SetNumericValue(nbrDonGia, 0);
        }

        private void ClearGridGiuCot()
        {
            if (dgvChiTietDon.DataSource is DataTable table)
            {
                dgvChiTietDon.DataSource = table.Clone();
                return;
            }

            dgvChiTietDon.DataSource = null;
            BoChonDongDgvChiTietDon();
        }

        private bool CoChiTietTrongGrid()
        {
            if (dgvChiTietDon.DataSource is DataTable table)
            {
                foreach (DataRow row in table.Rows)
                {
                    if (row.RowState != DataRowState.Deleted)
                        return true;
                }

                return false;
            }

            foreach (DataGridViewRow row in dgvChiTietDon.Rows)
            {
                if (!row.IsNewRow)
                    return true;
            }

            return false;
        }

        private void CapNhatTrangThaiNgayTheoChiTietPhieu()
        {
            ngayNhap_Xuat.Enabled = _isEditMode || !CoChiTietTrongGrid();
        }

        private void CauHinhNgayEditTrongCungThang(DateTime ngayGoc)
        {
            DateTime ngayDauThang = new DateTime(ngayGoc.Year, ngayGoc.Month, 1);
            DateTime ngayCuoiThang = ngayDauThang.AddMonths(1).AddDays(-1);

            _isChangingEditDateProgrammatically = true;
            try
            {
                // Reset giới hạn cũ trước khi chuyển sang một phiếu thuộc tháng khác.
                ngayNhap_Xuat.MinDate = DateTimePicker.MinimumDateTime;
                ngayNhap_Xuat.MaxDate = DateTimePicker.MaximumDateTime;
                ngayNhap_Xuat.Value = ngayGoc;
                ngayNhap_Xuat.MinDate = ngayDauThang;
                ngayNhap_Xuat.MaxDate = ngayCuoiThang;
            }
            finally
            {
                _isChangingEditDateProgrammatically = false;
            }
        }

        private void ResetEditState()
        {
            _isEditMode = false;
            _editingLichSuXuatNhapId = null;
            _editingTenPhieu = string.Empty;
            _editingOldDanhSachMaSPId = null;
            _editingOldDanhSachKhoId = null;
            _editingIsGroupXuat = false;
            _editingOldNgay = null;

            _isChangingEditDateProgrammatically = true;
            try
            {
                ngayNhap_Xuat.MinDate = DateTimePicker.MinimumDateTime;
                ngayNhap_Xuat.MaxDate = DateTimePicker.MaximumDateTime;
            }
            finally
            {
                _isChangingEditDateProgrammatically = false;
            }

            btnLuu.Text = "Lưu";
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

        private static int GetInt(DataRowView row, string columnName, int defaultValue)
        {
            int? value = GetNullableInt(row, columnName);
            return value ?? defaultValue;
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

        private static bool TryParseDbDate(string text, out DateTime result)
        {
            if (DateTime.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
                return true;

            return DateTime.TryParse(text, out result);
        }

        private static void SetNumericMaximum(NumericUpDown control, decimal maximum)
        {
            if (control == null)
                return;

            if (maximum < control.Minimum)
                maximum = control.Minimum;

            if (control.Value > maximum)
                control.Value = maximum;

            control.Maximum = maximum;
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

        private void NccSearchHelper_Cleared()
        {
            ClearSelectedNcc();
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

            if (_editSearchHelper != null)
            {
                _editSearchHelper.ItemSelected -= EditSearchHelper_ItemSelected;
                _editSearchHelper.Cleared -= EditSearchHelper_Cleared;
                _editSearchHelper.Dispose();
                _editSearchHelper = null;
            }

            cbxKhoHang.TextUpdate -= cbxKhoHang_TextUpdate;
            cbxNcc.TextUpdate -= cbxNcc_TextUpdate;
            dgvChiTietDon.CellDoubleClick -= dgvChiTietDon_CellDoubleClick;
            dgvChiTietDon.CellContentClick -= dgvChiTietDon_CellContentClick;

            _selectedDanhSachKhoId = null;
            _selectedTenKho = string.Empty;
            _selectedDanhSachNccId = null;
            _selectedTenNcc = string.Empty;
            _selectedDanhSachMaSPId = null;
            _selectedThongTinDatHangId = null;
            _selectedDanhSachDatHangId = null;
            _selectedSoLuongYeuCau = 0;
            _selectedSoLuongTon = 0;
        }

        private void btnInPhieu_Click(object sender, EventArgs e)
        {
            try
            {
                if (dgvChiTietDon == null || dgvChiTietDon.Rows.Count == 0)
                {
                    FrmWaiting.ShowGifAlert("Không có dữ liệu để in.", myIcon: EnumStore.Icon.Warning);
                    return;
                }

                DataGridViewRow firstRow = GetFirstDataGridRow();
                if (firstRow == null)
                {
                    FrmWaiting.ShowGifAlert("Không có dữ liệu hợp lệ để in.", myIcon: EnumStore.Icon.Warning);
                    return;
                }

                DateTime ngayPhieu = GetNgayPhieu(firstRow);
                string lyDo = GetLyDoInPhieu(firstRow);

                if (_model.IsNhap)
                    InPhieuNhapKhoV2(firstRow, ngayPhieu, lyDo);
                else
                    InPhieuXuatKhoV2(firstRow, ngayPhieu, lyDo);
            }
            catch (Exception ex)
            {
                FrmWaiting.ShowGifAlert($"Không thể in phiếu: {ex.Message}", myIcon: EnumStore.Icon.Warning);
            }
        }

        private void InPhieuNhapKhoV2(DataGridViewRow firstRow, DateTime ngayPhieu, string lyDo)
        {
            List<WarehouseReceiptItem> items = BuildWarehouseReceiptItems();

            if (items.Count == 0)
            {
                FrmWaiting.ShowGifAlert("Không có dòng chi tiết hợp lệ để in.", myIcon: EnumStore.Icon.Warning);
                return;
            }

            string soPhieu = FirstNotEmpty(
                GetGridString(firstRow, "TenPhieu"),
                tbMaPhieu.Text?.Trim()
            );

            string nguoiGiao = FirstNotEmpty(
                GetGridString(firstRow, "NguoiGiao_Nhan"),
                tbNguoiGiao.Text?.Trim()
            );

            string nhaCungCap = FirstNotEmpty(
                GetGridString(firstRow, "NhaCungCap"),
                cbxNcc.Text?.Trim()
            );

            string khoHang = FirstNotEmpty(
                GetGridString(firstRow, "TenKho"),
                cbxKhoHang.Text?.Trim()
            );

            var data = new WarehouseReceiptPrintData
            {
                NgayIn = ngayPhieu.ToString("'Ngày' dd 'tháng' MM 'năm' yyyy"),
                SoPO = "",
                SoPhieu = soPhieu,
                NguoiGiao = nguoiGiao,
                NhaCungCap = nhaCungCap,
                LyDoNhap = lyDo,
                Ngay = ngayPhieu,
                KhoHang = khoHang,
                Items = items,
                Signature = new SignatureInfo
                {
                    FactoryDirectorTitle = "Giám đốc nhà máy",
                    CheckerTitle = "Kế toán",
                    RequesterTitle = "Thủ kho"
                }
            };

            new WarehouseReceiptPrintService(data).ShowPreview(this);
        }

        private void InPhieuXuatKhoV2(DataGridViewRow firstRow, DateTime ngayPhieu, string lyDo)
        {
            List<WarehouseIssuesItem> items = BuildWarehouseIssuesItems();

            if (items.Count == 0)
            {
                FrmWaiting.ShowGifAlert("Không có dòng chi tiết hợp lệ để in.", myIcon: EnumStore.Icon.Warning);
                return;
            }

            string soPhieu = FirstNotEmpty(
                GetGridString(firstRow, "TenPhieu"),
                tbMaPhieu.Text?.Trim()
            );

            string nguoiNhan = FirstNotEmpty(
                GetGridString(firstRow, "NguoiGiao_Nhan"),
                tbNguoiGiao.Text?.Trim()
            );

            string xuatTaiKho = FirstNotEmpty(
                GetGridString(firstRow, "TenKho"),
                cbxKhoHang.Text?.Trim()
            );

            var data = new WarehouseIssuesPrintData
            {
                TieuDe = _model.IsDichVu
                    ? "XÁC NHẬN DỊCH VỤ"
                    : "PHIẾU XUẤT KHO",
                NgayIn = ngayPhieu.ToString("'Ngày' dd 'tháng' MM 'năm' yyyy"),
                So = "",
                Co = "",
                SoPhieu = soPhieu,
                NguoiNhan = nguoiNhan,
                LyDoXuat = lyDo,
                XuatTaiKho = xuatTaiKho,
                Items = items,
                Signature = new SignatureInfo
                {
                    CheckerTitle = "Kế toán",
                    RequesterTitle = "Người giao hàng",
                    FactoryDirectorTitle = "Thủ kho",
                    DirectorTitle = "Giám đốc"
                }
            };

            new WarehouseIssuesPrintService(data).ShowPreview(this);
        }

        private List<WarehouseReceiptItem> BuildWarehouseReceiptItems()
        {
            var items = new List<WarehouseReceiptItem>();
            int no = 1;

            foreach (DataGridViewRow row in dgvChiTietDon.Rows)
            {
                if (!IsValidDataRow(row))
                    continue;

                string thucNhan = GetGridString(row, "thucNhan");
                if (string.IsNullOrWhiteSpace(thucNhan))
                    continue;

                items.Add(new WarehouseReceiptItem
                {
                    No = no++,
                    Ten = GetGridString(row, "ten"),
                    Ma = GetGridString(row, "ma"),
                    DonVi = GetGridString(row, "donVi"),
                    YeuCau = GetGridString(row, "yeuCau"),
                    ThucNhan = thucNhan,
                    GhiChu = GetGridString(row, "ghiChu")
                });
            }

            return items;
        }

        private List<WarehouseIssuesItem> BuildWarehouseIssuesItems()
        {
            var items = new List<WarehouseIssuesItem>();
            int no = 1;

            foreach (DataGridViewRow row in dgvChiTietDon.Rows)
            {
                if (!IsValidDataRow(row))
                    continue;

                string thucNhan = GetGridString(row, "thucNhan");
                if (string.IsNullOrWhiteSpace(thucNhan))
                    continue;

                items.Add(new WarehouseIssuesItem
                {
                    No = no++,
                    Ten = GetGridString(row, "ten"),
                    Ma = GetGridString(row, "ma"),
                    DonVi = GetGridString(row, "donVi"),
                    SoLuong = thucNhan,
                    DonGia = GetGridString(row, "donGia"),
                    ThanhTien = GetGridString(row, "thanhTien"),
                    GhiChu = GetGridString(row, "ghiChu")
                });
            }

            return items;
        }

        private DataGridViewRow GetFirstDataGridRow()
        {
            foreach (DataGridViewRow row in dgvChiTietDon.Rows)
            {
                if (IsValidDataRow(row))
                    return row;
            }

            return null;
        }

        private DateTime GetNgayPhieu(DataGridViewRow firstRow)
        {
            string ngayText = GetGridString(firstRow, "ngay");

            if (!string.IsNullOrWhiteSpace(ngayText)
                && TryParseDbDate(ngayText, out DateTime parsedNgay))
            {
                return parsedNgay.Date;
            }

            return ngayNhap_Xuat.Value.Date;
        }

        private string GetLyDoInPhieu()
        {
            string lyDo = tbxLyDo.Text?.Trim();

            if (!string.IsNullOrWhiteSpace(lyDo))
                return lyDo;

            return _model.IsKhac ? "Khác" : "Theo đề nghị";
        }

        private string GetLyDoInPhieu(DataGridViewRow firstRow)
        {
            if (string.Equals(
                _model.Ten,
                TenKieuNhapXuat.DICH_VU,
                StringComparison.OrdinalIgnoreCase))
            {
                return FirstNotEmpty(
                    GetGridString(firstRow, "NhaCungCap"),
                    cbxNcc.Text?.Trim()
                );
            }

            return GetLyDoInPhieu();
        }

        private bool IsValidDataRow(DataGridViewRow row)
        {
            if (row == null || row.IsNewRow)
                return false;

            if (row.DataBoundItem is DataRowView drv)
            {
                if (drv.Row == null)
                    return false;

                if (drv.Row.RowState == DataRowState.Deleted
                    || drv.Row.RowState == DataRowState.Detached)
                {
                    return false;
                }
            }

            return true;
        }

        private string GetGridString(DataGridViewRow row, string columnName)
        {
            object value = GetGridValue(row, columnName);

            if (value == null || value == DBNull.Value)
                return string.Empty;

            return Convert.ToString(value)?.Trim() ?? string.Empty;
        }

        private object GetGridValue(DataGridViewRow row, string columnName)
        {
            if (row == null || string.IsNullOrWhiteSpace(columnName))
                return null;

            if (!IsValidDataRow(row))
                return null;

            if (row.DataBoundItem is DataRowView drv
                && drv.Row != null
                && drv.Row.Table != null)
            {
                foreach (DataColumn col in drv.Row.Table.Columns)
                {
                    if (string.Equals(col.ColumnName, columnName, StringComparison.OrdinalIgnoreCase))
                        return drv.Row[col];
                }
            }

            if (dgvChiTietDon != null)
            {
                foreach (DataGridViewColumn col in dgvChiTietDon.Columns)
                {
                    bool matchName = string.Equals(col.Name, columnName, StringComparison.OrdinalIgnoreCase);
                    bool matchDataProperty = string.Equals(col.DataPropertyName, columnName, StringComparison.OrdinalIgnoreCase);

                    if (matchName || matchDataProperty)
                        return row.Cells[col.Name]?.Value;
                }
            }

            return null;
        }

        private static string FirstNotEmpty(params string[] values)
        {
            if (values == null)
                return string.Empty;

            foreach (string value in values)
            {
                if (!string.IsNullOrWhiteSpace(value))
                    return value.Trim();
            }

            return string.Empty;
        }
    }
}
