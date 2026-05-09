using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using DG_TonKhoBTP_v02.Database.ChatLuong;
using DG_TonKhoBTP_v02.Models;
using DG_TonKhoBTP_v02.UI.Helper.AutoSearchWithCombobox;

namespace DG_TonKhoBTP_v02.UI.NghiepVuKhac.ChatLuong
{
    public partial class UC_NhapKho : UserControl
    {
        // ── Helper tìm kiếm cho cbxMaBin ────────────────────────────────────────
        private ComboBoxSearchHelper _maBinSearchHelper;

        // ── Lưu index dòng đang được chọn để sửa (-1 = không có) ────────────────
        private int _editingRowIndex = -1;

        // ── id của bản ghi TTNhapKho đang sửa (0 = chưa lưu vào DB) ────────────
        // Khi = 0: dòng grid chưa tồn tại trong DB (tạo mới nhưng chưa lưu – hiếm xảy ra)
        // Khi > 0: dòng đã có trong DB → btnSua sẽ UPDATE
        private long _editingIdNhapKho = 0;

        private long? _selectedTTThanhPhamID = null;

        public UC_NhapKho()
        {
            InitializeComponent();
            InitMaBinSearch();
            InitGridFont();

            grvDSNhapKho.CellClick += GrvDSNhapKho_CellClick;
        }

        // ════════════════════════════════════════════════════════════════════════
        // COMBOBOX TÌM KIẾM MÃ BIN
        // ════════════════════════════════════════════════════════════════════════

        private void InitMaBinSearch()
        {
            _maBinSearchHelper = new ComboBoxSearchHelper(
                comboBox: cbxMaBin,
                queryFunc: NhapKho.TimKiemMaBinAsync
            );
            _maBinSearchHelper.DisplayColumn = "MaBin";
            _maBinSearchHelper.ItemSelected += OnMaBinSelected;
            _maBinSearchHelper.Cleared += OnMaBinCleared;
        }

        private void OnMaBinSelected(DataRowView row)
        {
            cbxMaBin.Text = row["MaBin"]?.ToString() ?? string.Empty;
            tbTenSP.Text = row["Ten"]?.ToString() ?? string.Empty;
            tbMaBin.Text = row["MaBin"]?.ToString() ?? string.Empty;
            rtbGhiChu.Text = row["GhiChu"]?.ToString() ?? string.Empty;

            _selectedTTThanhPhamID = long.TryParse(row["TTThanhPham_ID"]?.ToString(), out long id)
                            ? id : (long?)null;

            if (decimal.TryParse(row["ChieuDaiSau"]?.ToString(), out decimal soMet))
                nbSoMet.Value = Math.Min(soMet, nbSoMet.Maximum);
            else
                nbSoMet.Value = 0;

            nbSoMet.Focus();
            nbSoMet.Select(0, int.MaxValue);
        }

        private void OnMaBinCleared()
        {
            tbTenSP.Text = string.Empty;
            tbMaBin.Text = string.Empty;
            rtbGhiChu.Text = string.Empty;
            nbSoMet.Value = 0;
            _selectedTTThanhPhamID = null;
        }

        // ════════════════════════════════════════════════════════════════════════
        // VALIDATE
        // ════════════════════════════════════════════════════════════════════════

        private bool ValidateInputs()
        {
            bool valid = true;
            ResetValidationColors();

            if (nbSoBB.Value == 0) { MarkError(nbSoBB); valid = false; }
            if (string.IsNullOrWhiteSpace(cbxMaBin.Text)) { MarkError(cbxMaBin); valid = false; }
            if (string.IsNullOrWhiteSpace(tbTenSP.Text)) { MarkError(tbTenSP); valid = false; }
            if (string.IsNullOrWhiteSpace(tbMaBin.Text)) { MarkError(tbMaBin); valid = false; }
            if (nbSoMet.Value == 0) { MarkError(nbSoMet); valid = false; }

            if (rdHangDat.Checked && string.IsNullOrWhiteSpace(tbKhachHang.Text))
            {
                MarkError(tbKhachHang);
                valid = false;
            }

            if (!_selectedTTThanhPhamID.HasValue || _selectedTTThanhPhamID <= 0)
            {
                MarkError(cbxMaBin);
                valid = false;
            }

            if (rdLo.Checked)
            {
                if (nrChieuCaoLo.Value == 0) { MarkError(nrChieuCaoLo); valid = false; }
                if (nrTongChieuDai.Value == 0) { MarkError(nrTongChieuDai); valid = false; }
                if (nbSoDau.Value == 0) { MarkError(nbSoDau); valid = false; }
                if (nbSoCuoi.Value == 0) { MarkError(nbSoCuoi); valid = false; }

                if (nbSoDau.Value > 0 && nbSoCuoi.Value > 0 && nbSoDau.Value >= nbSoCuoi.Value)
                {
                    MarkError(nbSoDau); MarkError(nbSoCuoi);
                    FrmWaiting.ShowGifAlert("Số đầu phải nhỏ hơn số cuối.");
                    return false;
                }

                if (nbSoDau.Value > 0 && nbSoCuoi.Value > 0 && nrTongChieuDai.Value > 0)
                {
                    decimal soLuongTinh = nbSoCuoi.Value - nbSoDau.Value + 1;
                    if (nrTongChieuDai.Value != soLuongTinh)
                    {
                        MarkError(nrTongChieuDai);
                        FrmWaiting.ShowGifAlert("Tổng chiều dài không khớp với số đầu và số cuối.");
                        return false;
                    }
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(tbCuon.Text)) { MarkError(tbCuon); valid = false; }
            }

            if (!valid)
                FrmWaiting.ShowGifAlert("Vui lòng điền đầy đủ thông tin (các ô viền đỏ).");

            return valid;
        }

        private static void MarkError(Control ctl) => ctl.BackColor = Color.MistyRose;

        private void ResetValidationColors()
        {
            Color n = SystemColors.Window;
            nbSoBB.BackColor = n;
            cbxMaBin.BackColor = n;
            tbTenSP.BackColor = n;
            tbMaBin.BackColor = n;
            nbSoMet.BackColor = n;
            tbKhachHang.BackColor = n;
            nbSoDau.BackColor = n;
            nbSoCuoi.BackColor = n;
            nrChieuCaoLo.BackColor = n;
            nrTongChieuDai.BackColor = n;
            tbCuon.BackColor = n;
        }

        // ════════════════════════════════════════════════════════════════════════
        // CHỨC NĂNG 1 – NHẬP KHO (INSERT ngay vào DB rồi thêm dòng vào grid)
        // ════════════════════════════════════════════════════════════════════════

        private void BtnNhapKho_Click(object sender, EventArgs e)
        {
            if (!ValidateInputs()) return;

            try
            {
                string nguoiLam = UserContext.IsAuthenticated ? UserContext.UserName : "";

                long newId = NhapKho.LuuMotDong(
                    ngay: dtNgay.Value.ToString("dd/MM/yyyy"),
                    soBB: (int)nbSoBB.Value,
                    ttThanhPhamId: _selectedTTThanhPhamID.Value,
                    tenSP: tbTenSP.Text.Trim(),
                    soMet: (double)nbSoMet.Value,
                    loaiDon: rdHangDat.Checked ? "Hàng đặt" : "Hàng bán",
                    khachHang: tbKhachHang.Text.Trim(),
                    ghiChu: rtbGhiChu.Text.Trim(),
                    loai: rdLo.Checked ? "Lô" : "Cuộn",
                    chieuCaoLo: rdLo.Checked ? (double)nrChieuCaoLo.Value : 0,
                    tongChieuDai: rdLo.Checked ? (double)nrTongChieuDai.Value : 0,
                    soDau: rdLo.Checked ? (int)nbSoDau.Value : 0,
                    soCuoi: rdLo.Checked ? (int)nbSoCuoi.Value : 0,
                    thongTinCuon: rdCuon.Checked ? tbCuon.Text.Trim() : string.Empty,
                    nguoiLam: nguoiLam
                );

                // Thêm dòng vào grid và ghi id vừa được DB tạo ra
                ThemDongVaoGrid(newId);
                ResetForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi lưu nhập kho:\n{ex.Message}",
                    "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ════════════════════════════════════════════════════════════════════════
        // CHỨC NĂNG 2 – SỬA (UPDATE bản ghi đã có trong DB)
        // ════════════════════════════════════════════════════════════════════════

        private void btnDelete_Click(object sender, EventArgs e)
        {
            // Kiểm tra có dòng đang được chọn không
            if (_editingRowIndex < 0 || _editingIdNhapKho <= 0)
            {
                FrmWaiting.ShowGifAlert("Vui lòng chọn dòng cần xoá.");
                return;
            }

            DataGridViewRow row = grvDSNhapKho.Rows[_editingRowIndex];

            long ttThanhPhamId = long.TryParse(row.Cells["TTThanhPham_ID"].Value?.ToString(), out long tpId)
                ? tpId : 0;

            double soMet = double.TryParse(
                row.Cells["soMet"].Value?.ToString(),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out double met) ? met : 0;

            if (ttThanhPhamId <= 0)
            {
                FrmWaiting.ShowGifAlert("Dòng này không có dữ liệu hợp lệ để xoá.");
                return;
            }

            if (MessageBox.Show("Bạn có chắc chắn muốn xoá dòng này?",
                    "Xác nhận xoá", MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning) == DialogResult.No) return;


            try
            {
                NhapKho.XoaMotDong(_editingIdNhapKho, ttThanhPhamId, soMet);

                grvDSNhapKho.Rows.RemoveAt(_editingRowIndex);
                ResetForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi xoá:\n{ex.Message}",
                    "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnSua_Click(object sender, EventArgs e)
        {
            if (_editingRowIndex < 0 || _editingRowIndex >= grvDSNhapKho.Rows.Count)
            {
                MessageBox.Show("Không có dòng nào được chọn để sửa.",
                    "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (!ValidateInputs()) return;

            // Nếu dòng chưa có id_NhapKho (không nên xảy ra trong flow mới) → báo lỗi
            if (_editingIdNhapKho <= 0)
            {
                MessageBox.Show("Dòng này chưa được lưu vào cơ sở dữ liệu, không thể cập nhật.",
                    "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                string nguoiLam = UserContext.IsAuthenticated ? UserContext.UserName : "";

                // Đọc TTThanhPham_ID cũ và SoMet cũ trực tiếp từ dòng grid đang sửa
                // → không cần SELECT lại DB trong CapNhatMotDong
                DataGridViewRow editingRow = grvDSNhapKho.Rows[_editingRowIndex];

                long ttThanhPhamIdCu = long.TryParse(editingRow.Cells["TTThanhPham_ID"].Value?.ToString(), out long cuId)
                    ? cuId : 0;

                double soMetCu = double.TryParse(
                    editingRow.Cells["soMet"].Value?.ToString(),
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out double cuMet) ? cuMet : 0;

                NhapKho.CapNhatMotDong(
                    idNhapKho: _editingIdNhapKho,
                    ttThanhPhamIdCu: ttThanhPhamIdCu,
                    soMetCu: soMetCu,
                    ngay: dtNgay.Value.ToString("dd/MM/yyyy"),
                    soBB: (int)nbSoBB.Value,
                    ttThanhPhamIdMoi: _selectedTTThanhPhamID.Value,
                    tenSP: tbTenSP.Text.Trim(),
                    soMet: (double)nbSoMet.Value,
                    loaiDon: rdHangDat.Checked ? "Hàng đặt" : "Hàng bán",
                    khachHang: tbKhachHang.Text.Trim(),
                    ghiChu: rtbGhiChu.Text.Trim(),
                    loai: rdLo.Checked ? "Lô" : "Cuộn",
                    chieuCaoLo: rdLo.Checked ? (double)nrChieuCaoLo.Value : 0,
                    tongChieuDai: rdLo.Checked ? (double)nrTongChieuDai.Value : 0,
                    soDau: rdLo.Checked ? (int)nbSoDau.Value : 0,
                    soCuoi: rdLo.Checked ? (int)nbSoCuoi.Value : 0,
                    thongTinCuon: rdCuon.Checked ? tbCuon.Text.Trim() : string.Empty,
                    nguoiLam: nguoiLam
                );

                // Cập nhật lại dữ liệu hiển thị trên dòng grid tương ứng
                WriteRowFromForm(grvDSNhapKho.Rows[_editingRowIndex]);

                grvDSNhapKho.FirstDisplayedScrollingRowIndex = _editingRowIndex;
                grvDSNhapKho.ClearSelection();
                grvDSNhapKho.Rows[_editingRowIndex].Selected = true;

                ResetForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi cập nhật:\n{ex.Message}",
                    "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ════════════════════════════════════════════════════════════════════════
        // GHI DỮ LIỆU FORM → DÒNG GRID
        // ════════════════════════════════════════════════════════════════════════

        private void WriteRowFromForm(DataGridViewRow row)
        {
            string loaiDonVal = rdHangDat.Checked ? "Hàng đặt" : "Hàng bán";
            string loaiVal = rdLo.Checked ? "Lô" : "Cuộn";

            row.Cells["ngay"].Value = dtNgay.Value.ToString("dd/MM/yyyy");
            row.Cells["soBB"].Value = nbSoBB.Value.ToString();
            row.Cells["TTThanhPham_ID"].Value = _selectedTTThanhPhamID?.ToString() ?? string.Empty;
            row.Cells["tenSP"].Value = tbTenSP.Text.Trim();
            row.Cells["maBin2"].Value = tbMaBin.Text.Trim();
            row.Cells["soMet"].Value = nbSoMet.Value.ToString("G", System.Globalization.CultureInfo.InvariantCulture);
            row.Cells["loaiDon"].Value = loaiDonVal;
            row.Cells["khachHang"].Value = tbKhachHang.Text.Trim();
            row.Cells["soDau"].Value = rdLo.Checked ? nbSoDau.Value.ToString() : string.Empty;
            row.Cells["soCuoi"].Value = rdLo.Checked ? nbSoCuoi.Value.ToString() : string.Empty;
            row.Cells["loai"].Value = loaiVal;
            row.Cells["chieuCaoLo"].Value = rdLo.Checked ? nrChieuCaoLo.Value.ToString() : string.Empty;
            row.Cells["tongChieuDai"].Value = rdLo.Checked ? nrTongChieuDai.Value.ToString() : string.Empty;
            row.Cells["cuon"].Value = rdCuon.Checked ? tbCuon.Text.Trim() : string.Empty;
            row.Cells["ghiChu"].Value = rtbGhiChu.Text.Trim();
            // id_NhapKho KHÔNG ghi lại ở đây – giữ nguyên giá trị đã set khi thêm/load
        }

        private void ThemDongVaoGrid(long idNhapKho)
        {
            int rowIndex = grvDSNhapKho.Rows.Add();
            DataGridViewRow row = grvDSNhapKho.Rows[rowIndex];
            row.Height = 35;

            // Ghi id trước để WriteRowFromForm không vô tình xóa mất
            row.Cells["id_NhapKho"].Value = idNhapKho.ToString();

            WriteRowFromForm(row);

            grvDSNhapKho.FirstDisplayedScrollingRowIndex = grvDSNhapKho.RowCount - 1;
        }

        // ════════════════════════════════════════════════════════════════════════
        // CLICK DÒNG GRID → load form (dùng cho cả Chức năng 2 và 3)
        // ════════════════════════════════════════════════════════════════════════

        private void GrvDSNhapKho_RowClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (grvDSNhapKho.Columns[e.ColumnIndex].Name == "colXoa") return;

            DataGridViewRow row = grvDSNhapKho.Rows[e.RowIndex];

            // ── Lấy id_NhapKho từ grid (phục vụ UPDATE) ─────────────────────────
            _editingIdNhapKho = long.TryParse(row.Cells["id_NhapKho"].Value?.ToString(), out long nkId)
                ? nkId : 0;

            // ── _selectedTTThanhPhamID (phục vụ validate) ────────────────────────
            _selectedTTThanhPhamID = long.TryParse(row.Cells["TTThanhPham_ID"].Value?.ToString(), out long tpId)
                ? tpId : (long?)null;

            // ── Ngày ─────────────────────────────────────────────────────────────
            if (DateTime.TryParseExact(
                    row.Cells["ngay"].Value?.ToString(), "dd/MM/yyyy",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out DateTime ngayVal))
                dtNgay.Value = ngayVal;

            // ── Số BB ─────────────────────────────────────────────────────────────
            if (decimal.TryParse(row.Cells["soBB"].Value?.ToString(), out decimal soBBVal))
                nbSoBB.Value = Math.Min(soBBVal, nbSoBB.Maximum);

            // ── Mã Bin / Tên SP ───────────────────────────────────────────────────
            cbxMaBin.Text = row.Cells["maBin2"].Value?.ToString() ?? string.Empty;
            tbTenSP.Text = row.Cells["tenSP"].Value?.ToString() ?? string.Empty;
            tbMaBin.Text = row.Cells["maBin2"].Value?.ToString() ?? string.Empty;

            // ── Số mét ───────────────────────────────────────────────────────────
            if (decimal.TryParse(row.Cells["soMet"].Value?.ToString(),
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out decimal soMetVal))
                nbSoMet.Value = Math.Min(soMetVal, nbSoMet.Maximum);

            // ── Loại đơn (Hàng đặt / Hàng bán) ──────────────────────────────────
            bool isHangDat = row.Cells["loaiDon"].Value?.ToString() == "Hàng đặt";
            rdHangDat.Checked = isHangDat;
            rdHangBan.Checked = !isHangDat;

            // ── Khách hàng ────────────────────────────────────────────────────────
            tbKhachHang.Text = row.Cells["khachHang"].Value?.ToString() ?? string.Empty;

            // ── Loại Lô / Cuộn ────────────────────────────────────────────────────
            bool isLo = row.Cells["loai"].Value?.ToString() == "Lô";
            rdLo.Checked = isLo;
            rdCuon.Checked = !isLo;

            if (isLo)
            {
                nbSoDau.Value = decimal.TryParse(row.Cells["soDau"].Value?.ToString(), out decimal sd) ? Math.Min(sd, nbSoDau.Maximum) : 0;
                nbSoCuoi.Value = decimal.TryParse(row.Cells["soCuoi"].Value?.ToString(), out decimal sc) ? Math.Min(sc, nbSoCuoi.Maximum) : 0;
                nrChieuCaoLo.Value = decimal.TryParse(row.Cells["chieuCaoLo"].Value?.ToString(), out decimal cc) ? Math.Min(cc, nrChieuCaoLo.Maximum) : 0;
                nrTongChieuDai.Value = decimal.TryParse(row.Cells["tongChieuDai"].Value?.ToString(), out decimal td) ? Math.Min(td, nrTongChieuDai.Maximum) : 0;
                tbCuon.Text = string.Empty;
            }
            else
            {
                tbCuon.Text = row.Cells["cuon"].Value?.ToString() ?? string.Empty;
                nrChieuCaoLo.Value = 0;
                nrTongChieuDai.Value = 0;
                nbSoDau.Value = 0;
                nbSoCuoi.Value = 0;
            }

            // ── Ghi chú ───────────────────────────────────────────────────────────
            rtbGhiChu.Text = row.Cells["ghiChu"].Value?.ToString() ?? string.Empty;

            // ── Chuyển sang chế độ Sửa ────────────────────────────────────────────
            _editingRowIndex = e.RowIndex;
            SetEditMode(true);
            ResetValidationColors();
        }

        // ════════════════════════════════════════════════════════════════════════
        // CHỨC NĂNG 3 – TÌM KIẾM
        // ════════════════════════════════════════════════════════════════════════

        private void tbTimKiem_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter) return;
            e.SuppressKeyPress = true;
            TimKiemNhapKhoVaLoadGrid();
        }

        private void TimKiemNhapKhoVaLoadGrid()
        {
            string keyword = tbTimKiem.Text.Trim();

            if (string.IsNullOrWhiteSpace(keyword))
            {
                MessageBox.Show("Vui lòng nhập từ khoá tìm kiếm.",
                    "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                tbTimKiem.Focus();
                return;
            }

            try
            {
                DataTable dt = NhapKho.TimKiemNhapKho(keyword);
                LoadNhapKhoVaoGrid(dt);

                if (dt.Rows.Count == 0)
                    MessageBox.Show("Không tìm thấy dữ liệu phù hợp.",
                        "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tìm kiếm dữ liệu:\n{ex.Message}",
                    "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadNhapKhoVaoGrid(DataTable dt)
        {
            if (grvDSNhapKho.DataSource != null)
                grvDSNhapKho.DataSource = null;

            grvDSNhapKho.Rows.Clear();

            foreach (DataRow dr in dt.Rows)
            {
                int rowIndex = grvDSNhapKho.Rows.Add();
                DataGridViewRow row = grvDSNhapKho.Rows[rowIndex];
                row.Height = 35;

                // id_NhapKho phải được ghi để btnSua biết UPDATE row nào trong DB
                row.Cells["id_NhapKho"].Value = GetDbText(dr, "id_NhapKho");
                row.Cells["TTThanhPham_ID"].Value = GetDbText(dr, "TTThanhPham_ID");
                row.Cells["ngay"].Value = GetDbText(dr, "ngay");
                row.Cells["soBB"].Value = GetDbText(dr, "soBB");
                row.Cells["tenSP"].Value = GetDbText(dr, "tenSP");
                row.Cells["maBin2"].Value = GetDbText(dr, "maBin2");
                row.Cells["soMet"].Value = GetDbNumberText(dr, "soMet");
                row.Cells["loaiDon"].Value = GetDbText(dr, "loaiDon");
                row.Cells["khachHang"].Value = GetDbText(dr, "khachHang");
                row.Cells["soDau"].Value = GetDbText(dr, "soDau");
                row.Cells["soCuoi"].Value = GetDbText(dr, "soCuoi");
                row.Cells["loai"].Value = GetDbText(dr, "loai");
                row.Cells["chieuCaoLo"].Value = GetDbNumberText(dr, "chieuCaoLo");
                row.Cells["tongChieuDai"].Value = GetDbNumberText(dr, "tongChieuDai");
                row.Cells["cuon"].Value = GetDbText(dr, "cuon");
                row.Cells["ghiChu"].Value = GetDbText(dr, "ghiChu");
            }

            _editingRowIndex = -1;
            _editingIdNhapKho = 0;
            SetEditMode(false);

            if (grvDSNhapKho.Rows.Count > 0)
            {
                grvDSNhapKho.ClearSelection();
                grvDSNhapKho.Rows[0].Selected = true;
                grvDSNhapKho.FirstDisplayedScrollingRowIndex = 0;
            }
        }

        // ════════════════════════════════════════════════════════════════════════
        // CHUYỂN CHẾ ĐỘ THÊM ↔ SỬA
        // ════════════════════════════════════════════════════════════════════════

        private void SetEditMode(bool editMode)
        {
            btnSua.Enabled = editMode;
            btnDelete.Enabled = editMode;
            btnNhapKho.Enabled = !editMode;
        }

        // ════════════════════════════════════════════════════════════════════════
        // RESET FORM
        // ════════════════════════════════════════════════════════════════════════

        private void ResetForm()
        {
            _maBinSearchHelper.Reset();
            _selectedTTThanhPhamID = null;
            _editingIdNhapKho = 0;

            tbTenSP.Text = string.Empty;
            tbMaBin.Text = string.Empty;
            nbSoMet.Value = 0;
            tbKhachHang.Text = string.Empty;
            rtbGhiChu.Text = string.Empty;

            nrChieuCaoLo.Value = 0;
            nrTongChieuDai.Value = 0;
            nbSoDau.Value = 0;
            nbSoCuoi.Value = 0;
            tbCuon.Text = string.Empty;

            _editingRowIndex = -1;
            SetEditMode(false);
            cbxMaBin.Focus();
        }

        private void btnResetForm_Click(object sender, EventArgs e) => ResetForm();

        // ════════════════════════════════════════════════════════════════════════
        // CỘT XOÁ TRONG GRID
        // ════════════════════════════════════════════════════════════════════════


        private void GrvDSNhapKho_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            GrvDSNhapKho_RowClick(sender, e);
        }

        // ════════════════════════════════════════════════════════════════════════
        // LOAD
        // ════════════════════════════════════════════════════════════════════════

        private void UC_QcDuyetNhapKho_Load(object sender, EventArgs e)
        {
            SetEditMode(false);
            rdLo_CheckedChanged(null, EventArgs.Empty);
            nbSoBB.Focus();
            nbSoBB.Select(0, int.MaxValue);
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            _maBinSearchHelper?.Dispose();
            base.OnHandleDestroyed(e);
        }

        private void rdLo_CheckedChanged(object sender, EventArgs e)
        {
            if (!rdLo.Checked) return;

            nrChieuCaoLo.Enabled = true;
            nrTongChieuDai.Enabled = true;
            nbSoDau.Enabled = true;
            nbSoCuoi.Enabled = true;

            tbCuon.Enabled = false;
            tbCuon.Text = string.Empty;
        }

        private void rdCuon_CheckedChanged(object sender, EventArgs e)
        {
            if (!rdCuon.Checked) return;

            nrChieuCaoLo.Enabled = false; nrChieuCaoLo.Value = 0;
            nrTongChieuDai.Enabled = false; nrTongChieuDai.Value = 0;
            nbSoDau.Enabled = false; nbSoDau.Value = 0;
            nbSoCuoi.Enabled = false; nbSoCuoi.Value = 0;

            tbCuon.Enabled = true;
        }

        // ════════════════════════════════════════════════════════════════════════
        // FONT GRID
        // ════════════════════════════════════════════════════════════════════════

        private void InitGridFont()
        {
            Font gridFont = new Font("Tahoma", 11.25F, FontStyle.Regular, GraphicsUnit.Point);
            Font headerFont = new Font("Tahoma", 11.25F, FontStyle.Regular, GraphicsUnit.Point);

            groupBox1.Font = gridFont;
            grvDSNhapKho.Font = gridFont;
            grvDSNhapKho.DefaultCellStyle.Font = gridFont;
            grvDSNhapKho.RowsDefaultCellStyle.Font = gridFont;
            grvDSNhapKho.AlternatingRowsDefaultCellStyle.Font = gridFont;

            grvDSNhapKho.EnableHeadersVisualStyles = false;
            grvDSNhapKho.ColumnHeadersDefaultCellStyle.Font = headerFont;
            grvDSNhapKho.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            grvDSNhapKho.RowTemplate.Height = 35;
            grvDSNhapKho.ColumnHeadersHeight = 38;
        }

        // ════════════════════════════════════════════════════════════════════════
        // HELPERS ĐỌC GIÁ TRỊ TỪ DataRow
        // ════════════════════════════════════════════════════════════════════════

        private static string GetDbText(DataRow row, string columnName)
        {
            if (!row.Table.Columns.Contains(columnName)) return string.Empty;
            if (row[columnName] == DBNull.Value) return string.Empty;
            return row[columnName]?.ToString() ?? string.Empty;
        }

        private static string GetDbNumberText(DataRow row, string columnName)
        {
            if (!row.Table.Columns.Contains(columnName)) return string.Empty;
            if (row[columnName] == DBNull.Value) return string.Empty;

            try
            {
                decimal value = Convert.ToDecimal(row[columnName],
                    System.Globalization.CultureInfo.InvariantCulture);
                return value.ToString("G", System.Globalization.CultureInfo.InvariantCulture);
            }
            catch
            {
                return row[columnName]?.ToString() ?? string.Empty;
            }
        }

       
    }
}