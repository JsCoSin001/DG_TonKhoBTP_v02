using DG_TonKhoBTP_v02.Database.Kho.XuatKho;
using DG_TonKhoBTP_v02.Models.Kho.XuatKho;
using DG_TonKhoBTP_v02.UI.Helper.AutoSearchWithCombobox;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DG_TonKhoBTP_v02.UI.NghiepVuKhac.Kho
{
    public partial class UC_CatDay : UserControl
    {
        private ComboBoxSearchHelper _searchHelper;
        private bool _isResetting;

        public UC_CatDay()
        {
            InitializeComponent();
            KhoiTaoGiaoDien();
            KhoiTaoTimKiem();
            DangKySuKien();
        }

        private void KhoiTaoGiaoDien()
        {
            // Chưa chọn kiểu tìm kiếm thì không cho nhập nội dung tìm.
            // cbxKetQua chỉ do chương trình điền sau khi user xác nhận suggestion
            // (hoặc khi nhập Chiều dài), nên luôn khóa nhập tay.
            cbxTimKiem.Enabled = false;
            cbxKetQua.Enabled = false;
            dtNgayBD.Enabled = false;
            dtNgayKT.Enabled = false;

            CauHinhGrid();
        }

        private void CauHinhGrid()
        {
            dataGridView1.AllowUserToAddRows = false;
            dataGridView1.RowTemplate.Height = 35;

            ID.ReadOnly = true;
            lot.ReadOnly = true;
            ten.ReadOnly = true;
            tenKH.ReadOnly = true;
            soLuong.ReadOnly = true;
            soDau.ReadOnly = true;
            soCuoi.ReadOnly = true;
            cd_1.ReadOnly = true;
            tong_cd.ReadOnly = true;
            loai.ReadOnly = true;
            detail.ReadOnly = true;

            // Hai cột này dành cho nghiệp vụ cắt ở bước sau.
            cd_cat.ReadOnly = false;
            slCuonLay.ReadOnly = false;
        }

        private void KhoiTaoTimKiem()
        {
            _searchHelper = new ComboBoxSearchHelper(
                comboBox: cbxTimKiem,
                queryFunc: TimSuggestionAsync);

            _searchHelper.DisplayColumn = "GiaTri";
            _searchHelper.SelectedTextBehavior = ComboBoxSelectedTextBehavior.Clear;
            _searchHelper.CanSearch = _ =>
            {
                CatDay_SearchType? type = LayKieuTimKiem();
                return type.HasValue && type.Value != CatDay_SearchType.ChieuDai;
            };

            _searchHelper.ItemSelected += SearchHelper_ItemSelected;
            _searchHelper.Cleared += SearchHelper_Cleared;
        }

        private Task<DataTable> TimSuggestionAsync(string keyword, CancellationToken ct)
        {
            CatDay_SearchType? type = LayKieuTimKiem();
            if (!type.HasValue || type.Value == CatDay_SearchType.ChieuDai)
            {
                DataTable dt = new DataTable();
                dt.Columns.Add("GiaTri", typeof(string));
                return Task.FromResult(dt);
            }

            return CatDay_DB.TimKiemGiaTriAsync(type.Value, keyword, ct);
        }

        private void DangKySuKien()
        {
            cbxKieuTimKiem.SelectedIndexChanged += CbxKieuTimKiem_SelectedIndexChanged;
            cbxTimKiem.TextUpdate += CbxTimKiem_TextUpdate;
            checkBox1.CheckedChanged += CheckBox1_CheckedChanged;
            checkBox2.CheckedChanged += CheckBox2_CheckedChanged;
            btnTimKiem.Click += BtnTimKiem_Click;
            btnTimToanBo.Click += BtnTimToanBo_Click;
            btnReset.Click += BtnReset_Click;
            btnCat.Click += BtnCat_Click;
            dataGridView1.CellValidating += DataGridView1_CellValidating;
            Disposed += UC_CatDay_Disposed;
        }

        private void CbxKieuTimKiem_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isResetting)
                return;

            _searchHelper?.Reset();
            cbxTimKiem.Text = string.Empty;
            cbxKetQua.Text = string.Empty;

            // Chỉ cho phép nhập sau khi đã chọn một kiểu tìm kiếm hợp lệ.
            cbxTimKiem.Enabled = LayKieuTimKiem().HasValue;
        }

        private void CbxTimKiem_TextUpdate(object sender, EventArgs e)
        {
            CatDay_SearchType? type = LayKieuTimKiem();
            if (!type.HasValue)
            {
                cbxKetQua.Text = string.Empty;
                return;
            }

            if (type.Value == CatDay_SearchType.ChieuDai)
            {
                // Chiều dài không dùng ComboBoxSearchHelper.
                // cbxKetQua vẫn là criterion chính thức khi bấm btnTimKiem.
                cbxKetQua.Text = cbxTimKiem.Text?.Trim() ?? string.Empty;
                return;
            }

            // User bắt đầu gõ search mới => selection cũ không còn hiệu lực.
            cbxKetQua.Text = string.Empty;
        }

        private void SearchHelper_ItemSelected(DataRowView row)
        {
            if (row == null || row.DataView?.Table?.Columns.Contains("GiaTri") != true)
            {
                cbxKetQua.Text = string.Empty;
                return;
            }

            cbxKetQua.Text = Convert.ToString(row["GiaTri"])?.Trim() ?? string.Empty;
        }

        private void SearchHelper_Cleared()
        {
            if (_isResetting)
                return;

            CatDay_SearchType? type = LayKieuTimKiem();
            if (!type.HasValue || type.Value != CatDay_SearchType.ChieuDai)
                cbxKetQua.Text = string.Empty;
        }

        private void CheckBox1_CheckedChanged(object sender, EventArgs e)
        {
            dtNgayBD.Enabled = checkBox1.Checked;
        }

        private void CheckBox2_CheckedChanged(object sender, EventArgs e)
        {
            dtNgayKT.Enabled = checkBox2.Checked;
        }

        private void BtnTimKiem_Click(object sender, EventArgs e)
        {
            if (!TryBuildSearchCriteria(out CatDay_SearchCriteria criteria))
                return;

            try
            {
                CatDay_SearchResult result = CatDay_DB.TimKiem(criteria);
                HienThiKetQua(result.Rows);
                ThongBaoDuLieuBatThuong(result.DataIssues);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Không thể tìm dữ liệu cắt dây.\n{ex.Message}",
                    "Lỗi",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void BtnTimToanBo_Click(object sender, EventArgs e)
        {
            try
            {
                CatDay_SearchResult result = CatDay_DB.LayToanBo();
                HienThiKetQua(result.Rows);
                ThongBaoDuLieuBatThuong(result.DataIssues);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Không thể lấy toàn bộ dữ liệu cắt dây.\n{ex.Message}",
                    "Lỗi",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void BtnReset_Click(object sender, EventArgs e)
        {
            _isResetting = true;
            try
            {
                _searchHelper?.Reset();
                cbxKieuTimKiem.SelectedIndex = -1;
                cbxTimKiem.Text = string.Empty;
                cbxTimKiem.Enabled = false;
                cbxKetQua.Text = string.Empty;

                checkBox1.Checked = false;
                checkBox2.Checked = false;
                dtNgayBD.Enabled = false;
                dtNgayKT.Enabled = false;
                dtNgayBD.Value = DateTime.Today;
                dtNgayKT.Value = DateTime.Today;

                cbXuatExcel.Checked = false;
                cbxXuatWord.Checked = false;

                dataGridView1.Rows.Clear();
            }
            finally
            {
                _isResetting = false;
            }
        }

        private bool TryBuildSearchCriteria(out CatDay_SearchCriteria criteria)
        {
            criteria = null;

            bool coNgayBatDau = checkBox1.Checked;
            bool coNgayKetThuc = checkBox2.Checked;

            if (coNgayBatDau && coNgayKetThuc && dtNgayBD.Value.Date > dtNgayKT.Value.Date)
            {
                MessageBox.Show(
                    "Ngày bắt đầu không được lớn hơn ngày kết thúc.",
                    "Dữ liệu tìm kiếm không hợp lệ",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return false;
            }

            CatDay_SearchType? type = LayKieuTimKiem();
            string searchValue = cbxKetQua.Text?.Trim() ?? string.Empty;

            bool coThuocTinhTimKiem = type.HasValue && !string.IsNullOrWhiteSpace(searchValue);
            bool coDieuKienNgay = coNgayBatDau || coNgayKetThuc;

            if (!coThuocTinhTimKiem && !coDieuKienNgay)
            {
                MessageBox.Show(
                    "Vui lòng chọn ít nhất một điều kiện tìm kiếm. Nếu cần lấy tất cả, hãy dùng nút 'Lấy toàn bộ'.",
                    "Thiếu điều kiện tìm kiếm",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return false;
            }

            int? chieuDaiToiThieu = null;
            if (coThuocTinhTimKiem && type.Value == CatDay_SearchType.ChieuDai)
            {
                if (!int.TryParse(searchValue, out int value) || value < 0)
                {
                    MessageBox.Show(
                        "Chiều dài tìm kiếm phải là số nguyên lớn hơn hoặc bằng 0.",
                        "Chiều dài không hợp lệ",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return false;
                }

                chieuDaiToiThieu = value;
            }

            criteria = new CatDay_SearchCriteria
            {
                SearchType = coThuocTinhTimKiem ? type : null,
                SearchValue = coThuocTinhTimKiem ? searchValue : string.Empty,
                ChieuDaiToiThieu = chieuDaiToiThieu,
                NgayBatDau = coNgayBatDau ? dtNgayBD.Value.Date : (DateTime?)null,
                NgayKetThuc = coNgayKetThuc ? dtNgayKT.Value.Date : (DateTime?)null,
                LayToanBo = false
            };

            return true;
        }

        private CatDay_SearchType? LayKieuTimKiem()
        {
            switch (cbxKieuTimKiem.Text?.Trim())
            {
                case "Chiều dài":
                    return CatDay_SearchType.ChieuDai;
                case "LOT":
                    return CatDay_SearchType.Lot;
                case "Tên sản phẩm":
                    return CatDay_SearchType.TenSanPham;
                case "Khách hàng":
                    return CatDay_SearchType.KhachHang;
                default:
                    return null;
            }
        }

        private void HienThiKetQua(IReadOnlyCollection<CatDay_Row> rows)
        {
            dataGridView1.Rows.Clear();

            if (rows == null || rows.Count == 0)
                return;

            foreach (CatDay_Row item in rows)
            {
                int rowIndex = dataGridView1.Rows.Add();
                DataGridViewRow row = dataGridView1.Rows[rowIndex];
                GanDuLieuTonVaoDong(row, item, true);
            }
        }

        private static string LayTextCell(DataGridViewRow row, string columnName)
        {
            return Convert.ToString(row?.Cells[columnName]?.Value)?.Trim() ?? string.Empty;
        }

        private void GanDuLieuTonVaoDong(DataGridViewRow row, CatDay_Row item, bool xoaInput)
        {
            if (row == null || item == null)
                return;

            string cuonXuatCu = LayTextCell(row, "slCuonLay");
            string cdCatCu = LayTextCell(row, "cd_cat");

            row.Tag = item.TTCuonDay_ID;
            row.Cells["ID"].Value = item.TTCuonDay_ID;
            row.Cells["lot"].Value = item.Lot;
            row.Cells["ten"].Value = item.TenSP;
            row.Cells["tenKH"].Value = item.KhachHang;
            row.Cells["soLuong"].Value = item.SoCuon;
            row.Cells["soDau"].Value = item.SoDau.HasValue ? (object)item.SoDau.Value : string.Empty;
            row.Cells["soCuoi"].Value = item.SoCuoi.HasValue ? (object)item.SoCuoi.Value : string.Empty;
            row.Cells["cd_1"].Value = item.ChieuDaiConLai;
            row.Cells["tong_cd"].Value = item.TongChieuDai;
            row.Cells["loai"].Value = item.Loai;

            bool duocNhap = item.NhomTonHopLe && item.DuLieuTonHopLe;
            bool laNhomSoLuong = item.NhomTon == CatDay_InventoryGroup.SoLuong;

            if (!duocNhap)
            {
                // Dữ liệu nguồn đang bất thường/partial-null: giữ dòng để user biết lỗi
                // nhưng không cho nhập tiếp trên một trạng thái tồn không hợp lệ.
                row.Cells["slCuonLay"].ReadOnly = true;
                row.Cells["cd_cat"].ReadOnly = true;
            }
            else
            {
                row.Cells["slCuonLay"].ReadOnly = !laNhomSoLuong;
                row.Cells["cd_cat"].ReadOnly = laNhomSoLuong;
            }

            if (xoaInput)
            {
                row.Cells["slCuonLay"].Value = string.Empty;
                row.Cells["cd_cat"].Value = string.Empty;
            }
            else
            {
                row.Cells["slCuonLay"].Value = cuonXuatCu;
                row.Cells["cd_cat"].Value = cdCatCu;
            }

            row.Cells["detail"].Value = "Xem chi tiết";
        }

        private void ThongBaoDuLieuBatThuong(IReadOnlyCollection<CatDay_DataIssue> issues)
        {
            if (issues == null || issues.Count == 0)
                return;

            var sb = new StringBuilder();
            sb.AppendLine($"Phát hiện {issues.Count} TTCuonDay có dữ liệu tồn bất thường và đã được loại khỏi kết quả tìm kiếm.");
            sb.AppendLine();

            foreach (CatDay_DataIssue issue in issues)
            {
                string raw = issue.RawRemaining.HasValue
                    ? $" — Giá trị còn: {issue.RawRemaining.Value}"
                    : string.Empty;

                sb.AppendLine(
                    $"TTCuonDay ID {issue.TTCuonDay_ID} — LOT {issue.Lot}{raw} — {issue.NoiDung}");
            }

            MessageBox.Show(
                sb.ToString().TrimEnd(),
                "Cảnh báo dữ liệu bất thường",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }

        private void DataGridView1_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0)
                return;

            DataGridViewColumn column = dataGridView1.Columns[e.ColumnIndex];
            if (column == null || (column.Name != "slCuonLay" && column.Name != "cd_cat"))
                return;

            DataGridViewRow row = dataGridView1.Rows[e.RowIndex];
            DataGridViewCell cell = row.Cells[e.ColumnIndex];
            if (cell.ReadOnly)
                return;

            string text = Convert.ToString(e.FormattedValue)?.Trim() ?? string.Empty;

            // Blank = không chọn dòng, nên được phép để trống.
            if (string.IsNullOrWhiteSpace(text))
                return;

            if (!int.TryParse(text, out int giaTri) || giaTri <= 0)
            {
                e.Cancel = true;
                MessageBox.Show(
                    column.Name == "slCuonLay"
                        ? "Cuộn xuất phải là số nguyên lớn hơn 0."
                        : "CD cắt phải là số nguyên lớn hơn 0.",
                    "Dữ liệu không hợp lệ",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            if (column.Name == "slCuonLay")
            {
                if (!long.TryParse(LayTextCell(row, "soLuong"), out long soCuonCon) || soCuonCon <= 0)
                {
                    e.Cancel = true;
                    MessageBox.Show(
                        "Dòng này không còn số cuộn hợp lệ để xuất.",
                        "Dữ liệu không hợp lệ",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                if (giaTri > soCuonCon)
                {
                    e.Cancel = true;
                    MessageBox.Show(
                        $"Cuộn xuất không được lớn hơn số cuộn còn lại ({soCuonCon}).",
                        "Dữ liệu không hợp lệ",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }

                return;
            }

            if (!long.TryParse(LayTextCell(row, "cd_1"), out long chieuDaiCon) || chieuDaiCon <= 0)
            {
                e.Cancel = true;
                MessageBox.Show(
                    "Dòng này không còn chiều dài hợp lệ để cắt.",
                    "Dữ liệu không hợp lệ",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            if (giaTri > chieuDaiCon)
            {
                e.Cancel = true;
                MessageBox.Show(
                    $"CD cắt không được lớn hơn CD 1 đơn vị ({chieuDaiCon}).",
                    "Dữ liệu không hợp lệ",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        private sealed class CatDay_GridSelection
        {
            public DataGridViewRow Row { get; set; }
            public CatDay_LenLenhInput Input { get; set; }
            public string LocalError { get; set; } = string.Empty;
        }

        private void BtnCat_Click(object sender, EventArgs e)
        {
            // Nếu ô đang sửa không qua được CellValidating thì không được tiếp tục lên lệnh.
            if (!dataGridView1.EndEdit())
                return;

            List<CatDay_GridSelection> selected = ThuThapDongDaChon();
            if (selected.Count == 0)
            {
                MessageBox.Show(
                    "Vui lòng nhập ít nhất một dòng cần Cắt/Lấy.",
                    "Chưa chọn dữ liệu",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            btnCat.Enabled = false;
            try
            {
                foreach (CatDay_GridSelection localError in selected.Where(x => !string.IsNullOrWhiteSpace(x.LocalError)))
                {
                    CatDay_DB.GhiLogLenLenh(
                        "WARN",
                        $"TTCuonDay_ID={localError.Input?.TTCuonDay_ID ?? 0}; {localError.LocalError}");
                }

                List<CatDay_LenLenhInput> dbInputs = selected
                    .Where(x => string.IsNullOrWhiteSpace(x.LocalError) && x.Input != null)
                    .Select(x => x.Input)
                    .ToList();

                CatDay_LenLenhResult dbResult = dbInputs.Count == 0
                    ? new CatDay_LenLenhResult()
                    : CatDay_DB.LenLenh(
                        dbInputs,
                        UserContext.UserName,
                        DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));

                var dbErrorById = dbResult.Loi
                    .GroupBy(x => x.TTCuonDay_ID)
                    .ToDictionary(g => g.Key, g => g.First());

                var errorRows = new HashSet<DataGridViewRow>();

                foreach (CatDay_GridSelection item in selected)
                {
                    if (!string.IsNullOrWhiteSpace(item.LocalError))
                    {
                        errorRows.Add(item.Row);
                        continue;
                    }

                    if (item.Input != null
                        && dbErrorById.TryGetValue(item.Input.TTCuonDay_ID, out CatDay_LenLenhDongResult dbError))
                    {
                        errorRows.Add(item.Row);

                        if (dbError.DuLieuMoi != null)
                        {
                            // Cùng nhóm: giữ input user để sửa. Đổi nhóm: xóa input cũ và khóa/mở lại cột.
                            GanDuLieuTonVaoDong(
                                item.Row,
                                dbError.DuLieuMoi,
                                dbError.NhomTonDaThayDoi);
                        }
                    }
                }

                if (errorRows.Count == 0)
                {
                    BtnReset_Click(this, EventArgs.Empty);
                    return;
                }

                GiuLaiCacDong(errorRows);

                MessageBox.Show(
                    $"Có {errorRows.Count} dòng lỗi.",
                    "Kết quả lên lệnh",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                // Lỗi kỹ thuật DB: DB layer đã rollback toàn bộ. Giữ tất cả dòng user đã chọn.
                CatDay_DB.GhiLogLenLenh("ERROR", "Lỗi kỹ thuật tại UI.", ex);
                GiuLaiCacDong(new HashSet<DataGridViewRow>(selected.Select(x => x.Row)));

                MessageBox.Show(
                    "Không thể lên lệnh do lỗi hệ thống. Không có dòng nào được lưu.",
                    "Lỗi",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                btnCat.Enabled = true;
            }
        }

        private List<CatDay_GridSelection> ThuThapDongDaChon()
        {
            var result = new List<CatDay_GridSelection>();

            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (row == null || row.IsNewRow)
                    continue;

                string soLuongText = LayTextCell(row, "slCuonLay");
                string chieuDaiText = LayTextCell(row, "cd_cat");

                if (string.IsNullOrWhiteSpace(soLuongText) && string.IsNullOrWhiteSpace(chieuDaiText))
                    continue;

                long id = 0;
                if (row.Tag != null)
                    long.TryParse(Convert.ToString(row.Tag), out id);
                if (id <= 0)
                    long.TryParse(LayTextCell(row, "ID"), out id);

                var selection = new CatDay_GridSelection
                {
                    Row = row,
                    Input = new CatDay_LenLenhInput { TTCuonDay_ID = id }
                };

                if (id <= 0)
                {
                    selection.LocalError = "Không xác định được TTCuonDay_ID của dòng.";
                    result.Add(selection);
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(soLuongText) && !string.IsNullOrWhiteSpace(chieuDaiText))
                {
                    selection.LocalError = "Một dòng không được nhập đồng thời Cuộn xuất và CD cắt.";
                    result.Add(selection);
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(soLuongText))
                {
                    if (!int.TryParse(soLuongText, out int soLuong) || soLuong <= 0)
                        selection.LocalError = "Cuộn xuất phải là số nguyên lớn hơn 0.";
                    else
                        selection.Input.SoLuong = soLuong;
                }
                else
                {
                    if (!int.TryParse(chieuDaiText, out int chieuDaiCat) || chieuDaiCat <= 0)
                        selection.LocalError = "CD cắt phải là số nguyên lớn hơn 0.";
                    else
                        selection.Input.ChieuDaiCat = chieuDaiCat;
                }

                result.Add(selection);
            }

            // Duplicate được xét trên TOÀN BỘ các dòng đã chọn, kể cả dòng đang có lỗi input.
            // Nếu cùng TTCuonDay_ID xuất hiện nhiều dòng thì không cho bất kỳ dòng nào của ID đó đi xuống DB.
            HashSet<long> duplicateIds = new HashSet<long>(
                result
                    .Where(x => x.Input != null && x.Input.TTCuonDay_ID > 0)
                    .GroupBy(x => x.Input.TTCuonDay_ID)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key));

            foreach (CatDay_GridSelection item in result.Where(
                x => x.Input != null && duplicateIds.Contains(x.Input.TTCuonDay_ID)))
            {
                item.LocalError = "TTCuonDay_ID xuất hiện nhiều hơn một dòng trong cùng batch; không được gộp hoặc xử lý tuần tự.";
            }

            return result;
        }

        private void GiuLaiCacDong(HashSet<DataGridViewRow> rowsCanGiu)
        {
            for (int i = dataGridView1.Rows.Count - 1; i >= 0; i--)
            {
                DataGridViewRow row = dataGridView1.Rows[i];
                if (!rowsCanGiu.Contains(row))
                    dataGridView1.Rows.RemoveAt(i);
            }
        }

        private void UC_CatDay_Disposed(object sender, EventArgs e)
        {
            _searchHelper?.Dispose();
            _searchHelper = null;
        }
    }
}
