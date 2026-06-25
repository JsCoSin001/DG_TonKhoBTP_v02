using DG_TonKhoBTP_v02.Database.KeToan.VatTuKhac;
using DG_TonKhoBTP_v02.Models;
using DG_TonKhoBTP_v02.Models.KeToan.VatTuKhac;
using DG_TonKhoBTP_v02.Printer;
using DG_TonKhoBTP_v02.UI.Helper.AutoSearchWithCombobox;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static DG_TonKhoBTP_v02.Printer.A4.PrinterModel;
using CoreHelper = DG_TonKhoBTP_v02.Helper.Helper;

namespace DG_TonKhoBTP_v02.UI.NghiepVuKhac.KeToan.VatTuKhac
{
    public partial class UC_MuaVatTu_v2 : UserControl
    {
        private string _MaDonMuaDichVu => "PRS";
        private string _MaDonMuaVatTu => "PRM";

        private const string COL_MA_DON = "colMaDon";
        private const string COL_THONG_TIN_DAT_HANG_ID = "colThongTinDatHangId";
        private const string COL_DANH_SACH_DAT_HANG_ID = "colDanhSachDatHangId";
        private const string COL_DANH_SACH_MA_SP_ID = "colDanhSachMaSPId";
        private const string COL_TEN_VAT_TU_KHONG_DAU = "colTenVatTuKhongDau";
        private const string COL_MA_VAT_TU = "colMaVatTu";
        private const string COL_TEN_VAT_TU = "colTenVatTu";
        private const string COL_DON_VI = "colDonVi";
        private const string COL_SO_LUONG = "colSoLuong";
        private const string COL_MUC_DICH = "colMucDich";
        private const string COL_NGAY_DE_NGHI = "colNgayDeNghi";
        private const string COL_NGAY_GIAO = "colNgayGiao";
        private const string COL_SL_TON = "colSLTon";
        private const string COL_XOA = "colXoa";

        private int _KieuDon = 1; // 1: Đơn mua vật tư, 2: Đơn dịch vụ
        private bool _isEditMode;
        private int _editingThongTinDatHangId;
        private string _editingMaDon;
        private bool _eventsHooked;

        private ComboBoxSearchHelper _searchVatTuHelper;
        private ComboBoxSearchHelper _searchDonHelper;

        public UC_MuaVatTu_v2(int kieuDon = 1)
        {
            InitializeComponent();
            _KieuDon = kieuDon;
        }

        private void UC_MuaVatTu_v2_Load(object sender, EventArgs e)
        {
            InitFormOnce();
            ResetFormToDefault();
        }

        private void InitFormOnce()
        {
            lblTitle.Text = _KieuDon == 1 ? "ĐỀ NGHỊ MUA VẬT TƯ" : "ĐỀ NGHỊ DỊCH VỤ";

            ConfigureControlsByKieuDon();
            ConfigureGrid();
            HookEventsOnce();
            InitSearchHelpers();
        }

        private void ResetFormToDefault()
        {
            // Reset biến global liên quan chế độ sửa
            SetEditMode(false);

            // Reset danh sách đang hiển thị
            dgvDSMua.Rows.Clear();
            dgvDSMua.ClearSelection();


            // Reset ngày về mặc định
            dtNgay.Value = DateTime.Today;

            // Reset ô tìm kiếm đơn
            if (_searchDonHelper != null)
                _searchDonHelper.Reset();
            else
            {
                comboBox1.SelectedIndex = -1;
                comboBox1.Text = string.Empty;
            }

            // Reset input vật tư/dịch vụ
            ClearInputControls(keepMaDon: false);

            // Nếu muốn ngày giao mặc định là hôm nay
            cbNgayGiao.Checked = true;
            dtNgayGiao.Enabled = true;
            dtNgayGiao.Value = DateTime.Today;

            // Sinh mã đơn mới theo ngày đề nghị hiện tại
            SetNewMaDon(dtNgay.Value);


            cbxTimThemTheoTen.Focus();

            ConfigureControlsByKieuDon();
        }

        private void btnLuu_Click(object sender, EventArgs e)
        {
            try
            {
                if (_isEditMode)
                    UpdateCurrentRow();
                else
                    AddNewRow();
            }
            catch (Exception ex)
            {
                FrmWaiting.ShowGifAlert("Đã có lỗi xảy ra, vui lòng thử lại.");
            }
        }

        private void cbNgayGiao_CheckedChanged(object sender, EventArgs e)
        {
            dtNgayGiao.Enabled = cbNgayGiao.Checked;
        }

        private void AddNewRow()
        {
            if (!TryBuildHeaderAndDetail(out DanhSachDatHangModel header, out ThongTinDatHangModel detail))
                return;

            int newDetailId = MuaVatTu_DB.InsertThongTinDatHang(header, detail);
            MuaVatTuGridRowModel gridRow = MuaVatTu_DB.GetGridRowByThongTinDatHangId(newDetailId);
            if (gridRow == null)
                throw new InvalidOperationException("Đã lưu nhưng không đọc lại được dữ liệu vừa lưu.");

            if (MuaVatTu_DB.ExistsDuplicateDetail(
                    gridRow.DanhSachDatHangId,
                    gridRow.ThongTinDatHangId,
                    _KieuDon,
                    gridRow.DanhSachMaSPId,
                    gridRow.TenVatTuKhongDau))
            {
                DialogResult confirm = MessageBox.Show(
                    "Dữ liệu này đã tồn tại trong cùng mã đơn. Bạn có muốn tạo thêm một dòng riêng biệt không?",
                    "Xác nhận dữ liệu trùng",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (confirm != DialogResult.Yes)
                {
                    MuaVatTu_DB.DeleteThongTinDatHangAndHeaderIfEmpty(newDetailId);
                    return;
                }
            }

            AddGridRow(gridRow);
            ClearInputControls(keepMaDon: true);
        }

        private void UpdateCurrentRow()
        {
            if (_editingThongTinDatHangId <= 0)
                throw new InvalidOperationException("Không xác định được dòng cần cập nhật.");

            if (MuaVatTu_DB.HasLichSuXuatNhap(_editingThongTinDatHangId))
            {
                FrmWaiting.ShowGifAlert("Không thể update do đã có dữ liệu trong hệ thông");
                SetEditMode(false);
                ClearInputControls(keepMaDon: false);
                SetNewMaDon(dtNgay.Value);
                return;
            }

            if (!TryBuildHeaderAndDetail(out DanhSachDatHangModel header, out ThongTinDatHangModel detail))
                return;

            detail.Id = _editingThongTinDatHangId;

            MuaVatTuGridRowModel oldRow = MuaVatTu_DB.GetGridRowByThongTinDatHangId(_editingThongTinDatHangId);
            if (oldRow == null)
                throw new InvalidOperationException("Không tìm thấy dòng cần cập nhật.");

            detail.DanhSachDatHang_ID = oldRow.DanhSachDatHangId;

            if (MuaVatTu_DB.ExistsDuplicateDetail(
                    oldRow.DanhSachDatHangId,
                    _editingThongTinDatHangId,
                    _KieuDon,
                    detail.DanhSachMaSP_ID,
                    detail.TenVatTu_KhongDau))
            {
                DialogResult confirm = MessageBox.Show(
                    "Dữ liệu sau khi sửa sẽ bị trùng trong cùng mã đơn. Bạn có muốn tiếp tục cập nhật không?",
                    "Xác nhận dữ liệu trùng",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (confirm != DialogResult.Yes)
                    return;
            }

            MuaVatTu_DB.UpdateDanhSachDatHangNgayThem(_editingMaDon, header.NgayThem);
            MuaVatTu_DB.UpdateThongTinDatHang(detail);

            MuaVatTuGridRowModel updatedRow = MuaVatTu_DB.GetGridRowByThongTinDatHangId(_editingThongTinDatHangId);
            UpdateGridRow(updatedRow);
            RefreshNgayDeNghiForMaDon(_editingMaDon, header.NgayThem);

            SetEditMode(false);
            ClearInputControls(keepMaDon: true);
            SetNewMaDon(dtNgay.Value);
        }

        private bool TryBuildHeaderAndDetail(out DanhSachDatHangModel header, out ThongTinDatHangModel detail)
        {
            header = null;
            detail = null;

            string maDon = TrimToNull(tbMaDon.Text);
            if (maDon == null)
            {
                FrmWaiting.ShowGifAlert("Bắt buộc phải có mã đơn.");

                tbMaDon.Focus();
                return false;
            }

            if (nbrSLMua.Value <= 0)
            {
                FrmWaiting.ShowGifAlert("Số lượng mua phải lớn hơn 0.");
                nbrSLMua.Focus();
                return false;
            }

            int? danhSachMaSPId = null;
            string tenVatTu = null;
            string tenVatTuKhongDau = null;

            if (_KieuDon == 1)
            {
                if (!int.TryParse(tbID.Text, out int id) || id <= 0)
                {
                    FrmWaiting.ShowGifAlert("Thiếu dữ liệu");
                    cbxTimThemTheoTen.Focus();
                    return false;
                }

                danhSachMaSPId = id;
            }
            else
            {
                tenVatTu = CoreHelper.TrimToNull(tbTen.Text);
                if (tenVatTu == null)
                {
                    FrmWaiting.ShowGifAlert("Thiếu dữ liệu");
                    tbTen.Focus();
                    return false;
                }

                tenVatTuKhongDau = CoreHelper.BoDauTiengViet(tenVatTu);
            }

            header = new DanhSachDatHangModel
            {
                MaDon = maDon,
                LoaiDon = _KieuDon,
                DateInsert = DateTime.Now,
                NguoiDat = UserContext.UserName,
                NgayThem = dtNgay.Value.Date
            };

            detail = new ThongTinDatHangModel
            {
                DanhSachMaSP_ID = danhSachMaSPId,
                TenVatTu = tenVatTu,
                TenVatTu_KhongDau = tenVatTuKhongDau,
                SoLuongMua = nbrSLMua.Value,
                MucDichMua = TrimToNull(tbMucDichMua.Text),
                NgayGiao = cbNgayGiao.Checked ? dtNgayGiao.Value.Date : (DateTime?)null,
                Date_Insert = DateTime.Now,
                DonGia = null
            };

            return true;
        }

        private void ConfigureControlsByKieuDon()
        {
            tbMaDon.Enabled = false;
            dtNgayGiao.Enabled = cbNgayGiao.Checked;

            cbxTimThemTheoTen.DropDownStyle = ComboBoxStyle.DropDown;
            comboBox1.DropDownStyle = ComboBoxStyle.DropDown;

            if (_KieuDon == 1)
            {
                cbxTimThemTheoTen.Enabled = true;
                tbTen.Enabled = false;
            }
            else
            {
                cbxTimThemTheoTen.Enabled = false;
                tbTen.Enabled = true;
            }
        }

        private void ConfigureGrid()
        {
            foreach (DataGridViewColumn column in dgvDSMua.Columns)
                column.ReadOnly = true;

            dgvDSMua.Columns[COL_XOA].ReadOnly = false;
            dgvDSMua.AllowUserToAddRows = false;
            dgvDSMua.RowHeadersVisible = false;
        }

        private void HookEventsOnce()
        {
            if (_eventsHooked)
                return;

            dgvDSMua.CellContentClick += dgvDSMua_CellContentClick;
            dgvDSMua.CellDoubleClick += dgvDSMua_CellDoubleClick;
            Disposed += UC_MuaVatTu_v2_Disposed;

            _eventsHooked = true;
        }

        private void InitSearchHelpers()
        {
            if (_searchVatTuHelper == null)
            {
                _searchVatTuHelper = new ComboBoxSearchHelper(cbxTimThemTheoTen, SearchVatTuAsync)
                {
                    DisplayColumn = "ten"
                };
                _searchVatTuHelper.ItemSelected += SearchVatTuHelper_ItemSelected;
                _searchVatTuHelper.Cleared += ClearVatTuInfoControls;
            }

            if (_searchDonHelper == null)
            {
                _searchDonHelper = new ComboBoxSearchHelper(comboBox1, SearchDonAsync)
                {
                    DisplayColumn = "display_text"
                };
                _searchDonHelper.ItemSelected += SearchDonHelper_ItemSelected;
            }
        }

        private Task<DataTable> SearchVatTuAsync(string keyword, CancellationToken ct)
        {
            string keywordKhongDau = CoreHelper.BoDauTiengViet(keyword);
            return Task.Run(() => MuaVatTu_DB.SearchDanhSachMaSP(keyword, keywordKhongDau), ct);
        }

        private Task<DataTable> SearchDonAsync(string keyword, CancellationToken ct)
        {
            string keywordKhongDau = CoreHelper.BoDauTiengViet(keyword);
            string nguoiDat = UserContext.UserName;
            return Task.Run(() => MuaVatTu_DB.SearchDonDatHang(keyword, keywordKhongDau, _KieuDon, nguoiDat), ct);
        }

        private void SearchVatTuHelper_ItemSelected(DataRowView row)
        {
            tbID.Text = Convert.ToString(row["id"]);
            tbTen.Text = Convert.ToString(row["ten"]);
            tbMa.Text = Convert.ToString(row["ma"]);
            tbDonVi.Text = Convert.ToString(row["donvi"]);
        }

        private void SearchDonHelper_ItemSelected(DataRowView row)
        {
            string maDon = Convert.ToString(row["ma_don"]);
            if (string.IsNullOrWhiteSpace(maDon))
                return;

            LoadGridByMaDon(maDon);
        }

        private void LoadGridByMaDon(string maDon)
        {
            var rows = MuaVatTu_DB.GetGridRowsByMaDon(maDon, _KieuDon, UserContext.UserName);

            dgvDSMua.Rows.Clear();
            foreach (var row in rows)
                AddGridRow(row);

            if (rows.Count > 0)
            {
                tbMaDon.Text = rows[0].MaDon;
                if (rows[0].NgayThem.HasValue)
                    dtNgay.Value = rows[0].NgayThem.Value;
            }

            SetEditMode(false);
            ClearInputControls(keepMaDon: true);
        }

        private void dgvDSMua_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0)
                return;

            if (dgvDSMua.Columns[e.ColumnIndex].Name != COL_XOA)
                return;

            DeleteGridRow(e.RowIndex);
        }

        private void DeleteGridRow(int rowIndex)
        {
            if (!TryGetGridInt(rowIndex, COL_THONG_TIN_DAT_HANG_ID, out int thongTinDatHangId))
                return;

            if (MuaVatTu_DB.HasLichSuXuatNhap(thongTinDatHangId))
            {
                FrmWaiting.ShowGifAlert("Dòng này đã có dữ liệu trong Lịch sử xuất nhập nên không được xoá.");
                return;
            }

            DialogResult confirm = MessageBox.Show("Bạn có chắc muốn xoá dòng này không?",
                "Xác nhận xoá", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes)
                return;

            MuaVatTu_DB.DeleteThongTinDatHangAndHeaderIfEmpty(thongTinDatHangId);
            dgvDSMua.Rows.RemoveAt(rowIndex);

            if (_isEditMode && _editingThongTinDatHangId == thongTinDatHangId)
            {
                SetEditMode(false);
                ClearInputControls(keepMaDon: true);
            }
        }

        private void dgvDSMua_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0)
                return;

            if (dgvDSMua.Columns[e.ColumnIndex].Name == COL_XOA)
                return;

            if (!TryGetGridInt(e.RowIndex, COL_THONG_TIN_DAT_HANG_ID, out int thongTinDatHangId))
                return;

            if (MuaVatTu_DB.HasLichSuXuatNhap(thongTinDatHangId))
            {
                FrmWaiting.ShowGifAlert("Dòng này đã có dữ liệu trong Lịch sử xuất nhập nên không được sửa.");
                return;
            }

            MuaVatTuGridRowModel row = MuaVatTu_DB.GetGridRowByThongTinDatHangId(thongTinDatHangId);
            if (row == null)
            {
                FrmWaiting.ShowGifAlert("Không tìm thấy dữ liệu của dòng đã chọn.");

                return;
            }

            FillControlsForEdit(row);
            SetEditMode(true, row.ThongTinDatHangId, row.MaDon);
        }

        private void FillControlsForEdit(MuaVatTuGridRowModel row)
        {
            tbMaDon.Text = row.MaDon;
            if (row.NgayThem.HasValue)
                dtNgay.Value = row.NgayThem.Value;

            tbID.Text = row.DanhSachMaSPId?.ToString() ?? string.Empty;
            tbMa.Text = row.MaVatTu ?? string.Empty;
            tbTen.Text = row.TenVatTu ?? string.Empty;
            tbDonVi.Text = row.DonVi ?? string.Empty;
            nbrSLMua.Value = ClampNumericValue(row.SoLuongMua);
            tbMucDichMua.Text = row.MucDichMua ?? string.Empty;

            if (row.NgayGiao.HasValue)
            {
                cbNgayGiao.Checked = true;
                dtNgayGiao.Value = row.NgayGiao.Value;
            }
            else
            {
                cbNgayGiao.Checked = false;
            }

            ConfigureControlsByKieuDon();
        }

        private decimal ClampNumericValue(decimal value)
        {
            if (value < nbrSLMua.Minimum)
                return nbrSLMua.Minimum;
            if (value > nbrSLMua.Maximum)
                return nbrSLMua.Maximum;
            return value;
        }

        private void AddGridRow(MuaVatTuGridRowModel row)
        {
            int rowIndex = dgvDSMua.Rows.Add();
            FillGridRow(dgvDSMua.Rows[rowIndex], row);
        }

        private void UpdateGridRow(MuaVatTuGridRowModel row)
        {
            if (row == null)
                return;

            foreach (DataGridViewRow gridRow in dgvDSMua.Rows)
            {
                if (TryGetCellInt(gridRow, COL_THONG_TIN_DAT_HANG_ID, out int thongTinDatHangId)
                    && thongTinDatHangId == row.ThongTinDatHangId)
                {
                    FillGridRow(gridRow, row);
                    return;
                }
            }
        }

        private void FillGridRow(DataGridViewRow gridRow, MuaVatTuGridRowModel row)
        {
            gridRow.Cells[COL_MA_DON].Value = row.MaDon;
            gridRow.Cells[COL_THONG_TIN_DAT_HANG_ID].Value = row.ThongTinDatHangId;
            gridRow.Cells[COL_DANH_SACH_DAT_HANG_ID].Value = row.DanhSachDatHangId;
            gridRow.Cells[COL_DANH_SACH_MA_SP_ID].Value = row.DanhSachMaSPId;
            gridRow.Cells[COL_TEN_VAT_TU_KHONG_DAU].Value = row.TenVatTuKhongDau;
            gridRow.Cells[COL_MA_VAT_TU].Value = row.MaVatTu;
            gridRow.Cells[COL_TEN_VAT_TU].Value = row.TenVatTu;
            gridRow.Cells[COL_DON_VI].Value = row.DonVi;
            gridRow.Cells[COL_SO_LUONG].Value = row.SoLuongMua;
            gridRow.Cells[COL_MUC_DICH].Value = row.MucDichMua;
            gridRow.Cells[COL_NGAY_DE_NGHI].Value = row.NgayThem?.ToString("dd/MM/yyyy");
            gridRow.Cells[COL_NGAY_GIAO].Value = row.NgayGiao?.ToString("dd/MM/yyyy");
            gridRow.Cells[COL_SL_TON].Value = row.SLTon;
        }

        private void RefreshNgayDeNghiForMaDon(string maDon, DateTime ngayDeNghi)
        {
            string ngayText = ngayDeNghi.ToString("dd/MM/yyyy");

            foreach (DataGridViewRow row in dgvDSMua.Rows)
            {
                if (row.IsNewRow)
                    continue;

                string rowMaDon = GetCellText(row, COL_MA_DON);
                if (string.Equals(rowMaDon, maDon, StringComparison.OrdinalIgnoreCase))
                    row.Cells[COL_NGAY_DE_NGHI].Value = ngayText;
            }
        }

        private void ClearInputControls(bool keepMaDon)
        {
            if (!keepMaDon)
                tbMaDon.Clear();

            if (_searchVatTuHelper != null)
                _searchVatTuHelper.Reset();
            else
                cbxTimThemTheoTen.Text = string.Empty;

            ClearVatTuInfoControls();
            tbMucDichMua.Clear();
            nbrSLMua.Value = nbrSLMua.Minimum;
            cbNgayGiao.Checked = true;
            dtNgayGiao.Enabled = true;
            dtNgayGiao.Value = DateTime.Today;
            ConfigureControlsByKieuDon();
        }

        private void ClearVatTuInfoControls()
        {
            tbID.Clear();
            tbMa.Clear();
            tbTen.Clear();
            tbDonVi.Clear();
        }

        private void SetEditMode(bool isEditMode, int thongTinDatHangId = 0, string maDon = null)
        {
            _isEditMode = isEditMode;
            _editingThongTinDatHangId = isEditMode ? thongTinDatHangId : 0;
            _editingMaDon = isEditMode ? maDon : null;
            btnLuu.Text = isEditMode ? "Update" : "Lưu";
        }

        private void SetNewMaDon(DateTime now)
        {
            int soLuongDon = MuaVatTu_DB.CountDanhSachDatHangInMonth(now);
            tbMaDon.Text = $"{GenerateMaDon(_KieuDon, now)}-{(soLuongDon + 1):0000}";
        }

        private string GenerateMaDon(int kieuDon = 1, DateTime? nowValue = null)
        {
            DateTime now = nowValue ?? DateTime.Now;
            string don = kieuDon == 1 ? _MaDonMuaVatTu : _MaDonMuaDichVu;
            string y = now.Year.ToString();
            y = y.Substring(y.Length - 2);
            string m = now.Month.ToString("D2");
            don += y + "/" + m;
            return don;
        }

        private bool TryGetGridInt(int rowIndex, string columnName, out int value)
        {
            value = 0;
            if (rowIndex < 0 || rowIndex >= dgvDSMua.Rows.Count)
                return false;

            return TryGetCellInt(dgvDSMua.Rows[rowIndex], columnName, out value);
        }

        private bool TryGetCellInt(DataGridViewRow row, string columnName, out int value)
        {
            value = 0;
            if (row == null)
                return false;

            object cellValue = row.Cells[columnName].Value;
            return cellValue != null && int.TryParse(cellValue.ToString(), out value);
        }

        private string TrimToNull(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;
            return value.Trim();
        }

        private void btnInPhieu_Click(object sender, EventArgs e)
        {
            try
            {
                if (_isEditMode)
                {
                    FrmWaiting.ShowGifAlert("Bạn đang ở chế độ sửa, vui lòng Update hoặc hủy trước khi in.");
                    return;
                }

                var printableRows = dgvDSMua.Rows
                    .Cast<DataGridViewRow>()
                    .Where(row => !row.IsNewRow)
                    .Where(row => !string.IsNullOrWhiteSpace(GetCellText(row, COL_MA_DON)))
                    .ToList();

                if (printableRows.Count == 0)
                {
                    FrmWaiting.ShowGifAlert("Không có dữ liệu để in");

                    return;
                }

                var groups = printableRows
                    .GroupBy(row => GetCellText(row, COL_MA_DON))
                    .ToList();

                foreach (var group in groups)
                {
                    string maDon = group.Key;
                    string ngayDeNghi = GetFirstNonEmptyCellText(group, COL_NGAY_DE_NGHI);
                    List<DataGridViewRow> rows = group.ToList();

                    if (_KieuDon == 1)
                        ShowMaterialRequestPreview(maDon, ngayDeNghi, rows);
                    else
                        ShowPurchaseRequestPreview(maDon, ngayDeNghi, rows);
                }

                ResetFormToDefault();
            }
            catch (Exception ex)
            {
                FrmWaiting.ShowGifAlert("Đã có lỗi xảy ra khi in, vui lòng thử lại.");
            }
        }

        private void ShowMaterialRequestPreview(string maDon, string ngayDeNghi, List<DataGridViewRow> rows)
        {
            var items = rows.Select((row, index) => new MaterialItem
            {
                No = index + 1,
                MaterialCode = GetCellText(row, COL_MA_VAT_TU),
                MaterialName = GetCellText(row, COL_TEN_VAT_TU),
                Unit = GetCellText(row, COL_DON_VI),
                Quantity = GetDecimalCellText(row, COL_SO_LUONG),
                Purpose = GetCellText(row, COL_MUC_DICH),
                RequiredDate = GetCellText(row, COL_NGAY_GIAO),
                CurrentStock = GetDecimalCellText(row, COL_SL_TON)
            }).ToList();

            var data = new MaterialRequestPrintData
            {
                Company = new CompanyInfo(),
                Document = new DocumentInfo
                {
                    Title = "GIẤY ĐỀ NGHỊ MUA VẬT TƯ",
                    OrderDate = ngayDeNghi,
                    OrderCode = maDon
                },
                Items = items,
                Signature = new SignatureInfo()
            };

            new MaterialRequestPrintService(data).ShowPreview(this);
        }

        private void ShowPurchaseRequestPreview(string maDon, string ngayDeNghi, List<DataGridViewRow> rows)
        {
            var items = rows.Select((row, index) => new ServiceItem
            {
                No = index + 1,
                ServiceName = GetCellText(row, COL_TEN_VAT_TU),
                Purpose = GetCellText(row, COL_MUC_DICH),
                RequiredDate = GetCellText(row, COL_NGAY_GIAO)
            }).ToList();

            var data = new PurchaseRequestPrintData
            {
                Company = new CompanyInfo
                {
                    FormCode = "BM-12-04",
                    IssueDate = "05/05/2009"
                },
                Document = new DocumentInfo
                {
                    Title = "GIẤY ĐỀ NGHỊ MUA DỊCH VỤ",
                    OrderDate = ngayDeNghi,
                    OrderCode = maDon
                },
                Items = items,
                Signature = new SignatureInfo()
            };

            new PurchaseRequestPrintService(data).ShowPreview(this);
        }

        private string GetFirstNonEmptyCellText(IEnumerable<DataGridViewRow> rows, string columnName)
        {
            foreach (DataGridViewRow row in rows)
            {
                string value = GetCellText(row, columnName);
                if (!string.IsNullOrWhiteSpace(value))
                    return value;
            }

            return string.Empty;
        }

        private string GetCellText(DataGridViewRow row, string columnName)
        {
            object value = row.Cells[columnName].Value;
            if (value == null || value == DBNull.Value)
                return string.Empty;

            if (value is DateTime date)
                return date.ToString("dd/MM/yyyy");

            return value.ToString()?.Trim() ?? string.Empty;
        }

        private string GetDecimalCellText(DataGridViewRow row, string columnName)
        {
            object value = row.Cells[columnName].Value;
            if (value == null || value == DBNull.Value)
                return string.Empty;

            if (value is decimal decimalValue)
                return decimalValue.ToString("0.##");

            if (value is double doubleValue)
                return Convert.ToDecimal(doubleValue).ToString("0.##");

            if (value is float floatValue)
                return Convert.ToDecimal(floatValue).ToString("0.##");

            string text = value.ToString()?.Trim();
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out decimal currentCultureValue))
                return currentCultureValue.ToString("0.##");

            if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal invariantCultureValue))
                return invariantCultureValue.ToString("0.##");

            return text;
        }

        private void UC_MuaVatTu_v2_Disposed(object sender, EventArgs e)
        {
            _searchVatTuHelper?.Dispose();
            _searchDonHelper?.Dispose();
        }

        private void btnHoanThanh_Click(object sender, EventArgs e)
        {
            ResetFormToDefault();
        }
    }
}
