using DG_TonKhoBTP_v02.Database.KeToan.VatTuKhac;
using DG_TonKhoBTP_v02.Models;
using DG_TonKhoBTP_v02.Models.KeToan.VatTuKhac;
using DG_TonKhoBTP_v02.UI.Helper.AutoSearchWithCombobox;
using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using CoreHelper = DG_TonKhoBTP_v02.Helper.Helper;

namespace DG_TonKhoBTP_v02.UI.NghiepVuKhac.KeToan.VatTuKhac
{
    public partial class UC_MuaVatTu_v2 : UserControl
    {
        private string _MaDonMuaDichVu => "PRS";
        private string _MaDonMuaVatTu => "PRM";

        private const string COL_MA_DON = "colMaDon";
        private const string COL_THONG_TIN_DAT_HANG_ID = "colThongTinDatHangId";
        private const string COL_MA_VAT_TU = "colMaVatTu";
        private const string COL_TEN_VAT_TU = "colTenVatTu";
        private const string COL_DON_VI = "colDonVi";
        private const string COL_SO_LUONG = "colSoLuong";
        private const string COL_MUC_DICH = "colMucDich";
        private const string COL_NGAY_GIAO = "colNgayGiao";
        private const string COL_SL_TON = "colSLTon";
        private const string COL_XOA = "colXoa";
        private const string COL_DANH_SACH_DAT_HANG_ID = "colDanhSachDatHangId";
        private const string COL_DANH_SACH_MA_SP_ID = "colDanhSachMaSPId";
        private const string COL_TEN_VAT_TU_KHONG_DAU = "colTenVatTuKhongDau";

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
            lblTitle.Text = _KieuDon == 1 ? "ĐỀ NGHỊ MUA VẬT TƯ" : "ĐỀ NGHỊ DỊCH VỤ";

            ConfigureControlsByKieuDon();
            ConfigureGrid();
            HookEventsOnce();
            InitSearchHelpers();
            SetNewMaDon(dtNgay.Value);
            SetEditMode(false);
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
                MessageBox.Show(ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

            int newDetailId = NhapXuatVatTu_DB.InsertThongTinDatHang(header, detail);
            MuaVatTuGridRowModel gridRow = NhapXuatVatTu_DB.GetGridRowByThongTinDatHangId(newDetailId);
            if (gridRow == null)
                throw new InvalidOperationException("Đã lưu nhưng không đọc lại được dữ liệu vừa lưu.");

            if (NhapXuatVatTu_DB.ExistsDuplicateDetail(
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
                    NhapXuatVatTu_DB.DeleteThongTinDatHangAndHeaderIfEmpty(newDetailId);
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

            if (NhapXuatVatTu_DB.HasLichSuXuatNhap(_editingThongTinDatHangId))
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

            MuaVatTuGridRowModel oldRow = NhapXuatVatTu_DB.GetGridRowByThongTinDatHangId(_editingThongTinDatHangId);
            if (oldRow == null)
                throw new InvalidOperationException("Không tìm thấy dòng cần cập nhật.");

            detail.DanhSachDatHang_ID = oldRow.DanhSachDatHangId;

            if (NhapXuatVatTu_DB.ExistsDuplicateDetail(
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

            NhapXuatVatTu_DB.UpdateDanhSachDatHangNgayThem(_editingMaDon, header.NgayThem);
            NhapXuatVatTu_DB.UpdateThongTinDatHang(detail);

            MuaVatTuGridRowModel updatedRow = NhapXuatVatTu_DB.GetGridRowByThongTinDatHangId(_editingThongTinDatHangId);
            UpdateGridRow(updatedRow);

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
                MessageBox.Show("Bắt buộc phải có mã đơn.", "Thiếu dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                tbMaDon.Focus();
                return false;
            }

            if (nbrSLMua.Value <= 0)
            {
                MessageBox.Show("Số lượng mua phải lớn hơn 0.", "Thiếu dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                    MessageBox.Show("Bắt buộc phải chọn vật tư từ combobox tìm kiếm.", "Thiếu dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                    MessageBox.Show("Bắt buộc phải nhập tên.", "Thiếu dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
            // Các cột đã được đổi tên trực tiếp trong Designer, vì vậy không rename runtime nữa.
            // Code bên dưới chỉ kiểm tra/tạo thêm các cột ẩn cần dùng cho edit/delete/update.
            AddHiddenColumnIfMissing(COL_DANH_SACH_DAT_HANG_ID);
            AddHiddenColumnIfMissing(COL_DANH_SACH_MA_SP_ID);
            AddHiddenColumnIfMissing(COL_TEN_VAT_TU_KHONG_DAU);

            EnsureGridColumnExists(COL_MA_DON);
            EnsureGridColumnExists(COL_THONG_TIN_DAT_HANG_ID);
            EnsureGridColumnExists(COL_MA_VAT_TU);
            EnsureGridColumnExists(COL_TEN_VAT_TU);
            EnsureGridColumnExists(COL_DON_VI);
            EnsureGridColumnExists(COL_SO_LUONG);
            EnsureGridColumnExists(COL_MUC_DICH);
            EnsureGridColumnExists(COL_NGAY_GIAO);
            EnsureGridColumnExists(COL_SL_TON);
            EnsureGridColumnExists(COL_XOA);
            EnsureGridColumnExists(COL_DANH_SACH_DAT_HANG_ID);
            EnsureGridColumnExists(COL_DANH_SACH_MA_SP_ID);
            EnsureGridColumnExists(COL_TEN_VAT_TU_KHONG_DAU);

            foreach (DataGridViewColumn column in dgvDSMua.Columns)
                column.ReadOnly = true;

            dgvDSMua.Columns[COL_XOA].ReadOnly = false;
            dgvDSMua.AllowUserToAddRows = false;
            dgvDSMua.RowHeadersVisible = false;
        }

        private void EnsureGridColumnExists(string columnName)
        {
            if (dgvDSMua.Columns.Contains(columnName))
                return;

            throw new InvalidOperationException($"Thiếu cột '{columnName}' trong dgvDSMua. Vui lòng kiểm tra lại Name của cột trong Designer.");
        }

        private void AddHiddenColumnIfMissing(string columnName)
        {
            if (dgvDSMua.Columns.Contains(columnName))
                return;

            var column = new DataGridViewTextBoxColumn
            {
                Name = columnName,
                HeaderText = columnName,
                Visible = false
            };
            dgvDSMua.Columns.Add(column);
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
            return Task.Run(() => NhapXuatVatTu_DB.SearchDanhSachMaSP(keyword, keywordKhongDau), ct);
        }

        private Task<DataTable> SearchDonAsync(string keyword, CancellationToken ct)
        {
            string keywordKhongDau = CoreHelper.BoDauTiengViet(keyword);
            string nguoiDat = UserContext.UserName;
            return Task.Run(() => NhapXuatVatTu_DB.SearchDonDatHang(keyword, keywordKhongDau, _KieuDon, nguoiDat), ct);
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
            var rows = NhapXuatVatTu_DB.GetGridRowsByMaDon(maDon, _KieuDon, UserContext.UserName);

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

            if (NhapXuatVatTu_DB.HasLichSuXuatNhap(thongTinDatHangId))
            {
                MessageBox.Show("Dòng này đã có dữ liệu trong Lịch sử xuất nhập nên không được xoá.",
                    "Không thể xoá", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DialogResult confirm = MessageBox.Show("Bạn có chắc muốn xoá dòng này không?",
                "Xác nhận xoá", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes)
                return;

            NhapXuatVatTu_DB.DeleteThongTinDatHangAndHeaderIfEmpty(thongTinDatHangId);
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

            if (NhapXuatVatTu_DB.HasLichSuXuatNhap(thongTinDatHangId))
            {
                MessageBox.Show("Dòng này đã có dữ liệu trong Lịch sử xuất nhập nên không được sửa.",
                    "Không thể sửa", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            MuaVatTuGridRowModel row = NhapXuatVatTu_DB.GetGridRowByThongTinDatHangId(thongTinDatHangId);
            if (row == null)
            {
                MessageBox.Show("Không tìm thấy dữ liệu của dòng đã chọn.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                if (Convert.ToInt32(gridRow.Cells[COL_THONG_TIN_DAT_HANG_ID].Value) == row.ThongTinDatHangId)
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
            gridRow.Cells[COL_MA_VAT_TU].Value = row.MaVatTu;
            gridRow.Cells[COL_TEN_VAT_TU].Value = row.TenVatTu;
            gridRow.Cells[COL_DON_VI].Value = row.DonVi;
            gridRow.Cells[COL_SO_LUONG].Value = row.SoLuongMua;
            gridRow.Cells[COL_MUC_DICH].Value = row.MucDichMua;
            gridRow.Cells[COL_NGAY_GIAO].Value = row.NgayGiao?.ToString("dd/MM/yyyy");
            gridRow.Cells[COL_SL_TON].Value = row.SLTon;
            gridRow.Cells[COL_DANH_SACH_DAT_HANG_ID].Value = row.DanhSachDatHangId;
            gridRow.Cells[COL_DANH_SACH_MA_SP_ID].Value = row.DanhSachMaSPId;
            gridRow.Cells[COL_TEN_VAT_TU_KHONG_DAU].Value = row.TenVatTuKhongDau;
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
            dtNgayGiao.Value = DateTime.Now;
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
            int soLuongDon = NhapXuatVatTu_DB.CountDanhSachDatHangInMonth(now);
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

            object cellValue = dgvDSMua.Rows[rowIndex].Cells[columnName].Value;
            return cellValue != null && int.TryParse(cellValue.ToString(), out value);
        }

        private string TrimToNull(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;
            return value.Trim();
        }

        private void UC_MuaVatTu_v2_Disposed(object sender, EventArgs e)
        {
            _searchVatTuHelper?.Dispose();
            _searchDonHelper?.Dispose();
        }
    }
}
