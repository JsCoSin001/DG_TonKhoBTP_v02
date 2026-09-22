using DG_TonKhoBTP_v02.Database.Kho.XuatKho;
using DG_TonKhoBTP_v02.Models.Kho.XuatKho;
using DG_TonKhoBTP_v02.UI.Helper.AutoSearchWithCombobox;
using System;
using System.Collections.Generic;
using System.Data;
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

            ID.ReadOnly = true;
            lot.ReadOnly = true;
            ten.ReadOnly = true;
            soLuong.ReadOnly = true;
            soDau.ReadOnly = true;
            soCuoi.ReadOnly = true;
            cd_1.ReadOnly = true;
            tong_cd.ReadOnly = true;
            detail.ReadOnly = true;

            // Hai cột này dành cho nghiệp vụ cắt ở bước sau.
            cd_cat.ReadOnly = false;
            ngayCat.ReadOnly = false;
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
                row.Tag = item.TTCuonDay_ID;

                row.Cells["ID"].Value = item.TTCuonDay_ID;
                row.Cells["lot"].Value = item.Lot;
                row.Cells["ten"].Value = item.TenSP;
                row.Cells["soLuong"].Value = item.SoCuon;
                row.Cells["soDau"].Value = item.SoDau;
                row.Cells["soCuoi"].Value = item.SoCuoi;
                row.Cells["cd_1"].Value = item.ChieuDaiConLai;
                row.Cells["tong_cd"].Value = item.TongChieuDai;

                // Hai trường này dành cho nghiệp vụ cắt sau,
                // tuyệt đối không điền từ LichSuCatDay cũ.
                row.Cells["cd_cat"].Value = string.Empty;
                row.Cells["ngayCat"].Value = string.Empty;
                row.Cells["detail"].Value = string.Empty;
            }
        }

        private void ThongBaoDuLieuBatThuong(IReadOnlyCollection<CatDay_DataIssue> issues)
        {
            if (issues == null || issues.Count == 0)
                return;

            var sb = new StringBuilder();
            sb.AppendLine($"Phát hiện {issues.Count} dữ liệu có chiều dài còn lại âm và đã được loại khỏi kết quả tìm kiếm.");
            sb.AppendLine();

            foreach (CatDay_DataIssue issue in issues)
            {
                sb.AppendLine(
                    $"TTCuonDay ID {issue.TTCuonDay_ID} — LOT {issue.Lot} — Remaining {issue.RawRemaining}");
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

            if (dataGridView1.Columns[e.ColumnIndex].Name != "cd_cat")
                return;

            string text = Convert.ToString(e.FormattedValue)?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(text))
                return;

            if (!int.TryParse(text, out int chieuDaiCat) || chieuDaiCat < 0)
            {
                e.Cancel = true;
                MessageBox.Show(
                    "CD cắt phải là số nguyên lớn hơn hoặc bằng 0.",
                    "CD cắt không hợp lệ",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            object remainingValue = dataGridView1.Rows[e.RowIndex].Cells["cd_1"].Value;
            if (!long.TryParse(Convert.ToString(remainingValue), out long chieuDaiConLai))
                return;

            if (chieuDaiCat > chieuDaiConLai)
            {
                e.Cancel = true;
                MessageBox.Show(
                    $"CD cắt không được lớn hơn chiều dài còn lại của một cuộn ({chieuDaiConLai}).",
                    "CD cắt không hợp lệ",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        private void UC_CatDay_Disposed(object sender, EventArgs e)
        {
            _searchHelper?.Dispose();
            _searchHelper = null;
        }
    }
}
