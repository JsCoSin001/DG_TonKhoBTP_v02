using DG_TonKhoBTP_v02.Database;
using DG_TonKhoBTP_v02.Database.ChatLuong;
using DG_TonKhoBTP_v02.Models;
using DG_TonKhoBTP_v02.Printer.TemXuatHang;
using DG_TonKhoBTP_v02.UI.Helper.AutoSearchWithCombobox;
using DG_TonKhoBTP_v02.UI.NghiepVuKhac.Kho;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CoreHelper = DG_TonKhoBTP_v02.Helper.Helper;
using FontStyle = System.Drawing.FontStyle;

namespace DG_TonKhoBTP_v02.UI.NghiepVuKhac.ChatLuong
{
    public partial class UC_NhapKho : UserControl
    {
        private ComboBoxSearchHelper _maBinSearchHelper;

        private int _editingRowIndex = -1;

        private long _editingIdNhapKho = 0;

        private long? _selectedTTThanhPhamID = null;


        private List<ThongTinCuonDay> thongTinDayNhapKho = new List<ThongTinCuonDay>();
        private bool _ttCuonDayChanged = false;

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
                queryFunc: NhapKho_DB.TimKiemMaBinAsync
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

            if (rdLo.Checked && nrChieuCaoLo.Value == 0) { MarkError(nrChieuCaoLo); valid = false; }

            if (thongTinDayNhapKho == null || thongTinDayNhapKho.Count == 0 || string.IsNullOrWhiteSpace(tbxThongTinDay.Text))
            {
                MarkError(tbxThongTinDay);
                valid = false;
            }
            if (!valid)
                FrmWaiting.ShowGifAlert("Kiểm tra dữ liệu tại ô được tô đỏ.");

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
            tbxThongTinDay.BackColor = n;
            rtbGhiChu.BackColor = n;
            nrChieuCaoLo.BackColor = n;
        }

        // ════════════════════════════════════════════════════════════════════════
        // CHỨC NĂNG 1 – NHẬP KHO (INSERT ngay vào DB rồi thêm dòng vào grid)
        // ════════════════════════════════════════════════════════════════════════

        private void BtnNhapKho_Click(object sender, EventArgs e)
        {
            if (!ValidateInputs()) return;

            if (thongTinDayNhapKho == null || thongTinDayNhapKho.Count == 0)
            {
                MarkError(tbxThongTinDay);
                FrmWaiting.ShowGifAlert("Vui lòng nhập thông tin cuộn/dây trước khi lưu.");
                return;
            }

            try
            {
                string nguoiLam = UserContext.IsAuthenticated ? UserContext.UserName : "";

                NhapKho_Model model = new NhapKho_Model
                {
                    Ngay = dtNgay.Value.ToString("yyyy-MM-dd"),
                    SoBB = (int)nbSoBB.Value,
                    TTThanhPham_ID = _selectedTTThanhPhamID.Value,
                    TenSP = tbTenSP.Text.Trim(),
                    SoMet = (double)nbSoMet.Value,

                    LoaiDon = rdHangDat.Checked ? "Hàng đặt" : "Hàng bán",
                    KhachHang = tbKhachHang.Text.Trim(),
                    GhiChu = rtbGhiChu.Text.Trim(),

                    Loai = rdLo.Checked ? "Lô" : "Cuộn",
                    ChieuCaoLo = rdLo.Checked ? (double)nrChieuCaoLo.Value : 0,

                    NguoiLam = nguoiLam
                };

                List<ThongTinCuonDay> dsCuon = new List<ThongTinCuonDay>(thongTinDayNhapKho);

                long newId = NhapKho_DB.NhapKho(model, dsCuon);

                ThemDongVaoGrid(newId, model);

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
                NhapKho_DB.XoaMotDong(_editingIdNhapKho, ttThanhPhamId, soMet);

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

            if (_editingIdNhapKho <= 0)
            {
                MessageBox.Show("Dòng này chưa được lưu vào cơ sở dữ liệu, không thể cập nhật.",
                    "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                string nguoiLam = UserContext.IsAuthenticated ? UserContext.UserName : "";

                DataGridViewRow editingRow = grvDSNhapKho.Rows[_editingRowIndex];

                long ttThanhPhamIdCu = long.TryParse(
                    editingRow.Cells["TTThanhPham_ID"].Value?.ToString(),
                    out long cuId)
                    ? cuId
                    : 0;

                double soMetCu = double.TryParse(
                    editingRow.Cells["soMet"].Value?.ToString(),
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out double cuMet)
                    ? cuMet
                    : 0;

                if (ttThanhPhamIdCu <= 0)
                {
                    MessageBox.Show("Không tìm thấy TTThanhPham_ID cũ để cập nhật.",
                        "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                NhapKho_Model model = new NhapKho_Model
                {
                    Id = _editingIdNhapKho,
                    Ngay = dtNgay.Value.ToString("yyyy-MM-dd"),
                    SoBB = (int)nbSoBB.Value,
                    TTThanhPham_ID = _selectedTTThanhPhamID.Value,
                    TenSP = tbTenSP.Text.Trim(),
                    SoMet = (double)nbSoMet.Value,

                    LoaiDon = rdHangDat.Checked ? "Hàng đặt" : "Hàng bán",
                    KhachHang = tbKhachHang.Text.Trim(),
                    GhiChu = rtbGhiChu.Text.Trim(),

                    Loai = rdLo.Checked ? "Lô" : "Cuộn",
                    ChieuCaoLo = rdLo.Checked ? (double)nrChieuCaoLo.Value : 0,

                    NguoiLam = nguoiLam
                };

                List<ThongTinCuonDay> dsCuon = thongTinDayNhapKho == null
                    ? new List<ThongTinCuonDay>()
                    : new List<ThongTinCuonDay>(thongTinDayNhapKho);

                NhapKho_DB.CapNhatNhapKho(
                    idNhapKho: _editingIdNhapKho,
                    ttThanhPhamIdCu: ttThanhPhamIdCu,
                    soMetCu: soMetCu,
                    model: model,
                    dsCuon: dsCuon,
                    capNhatTTCuonDay: _ttCuonDayChanged
                );

                WriteRowFromForm(grvDSNhapKho.Rows[_editingRowIndex]);
                grvDSNhapKho.Rows[_editingRowIndex].Cells["cuon"].Value = tbxThongTinDay.Text;

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

            row.Cells["soMet"].Value = nbSoMet.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);

            row.Cells["loaiDon"].Value = loaiDonVal;
            row.Cells["khachHang"].Value = tbKhachHang.Text.Trim();
            row.Cells["loai"].Value = loaiVal;
            row.Cells["chieuCaoLo"].Value = rdLo.Checked ? nrChieuCaoLo.Value.ToString() : string.Empty;
            row.Cells["ghiChu"].Value = rtbGhiChu.Text.Trim();
        }

        private void ThemDongVaoGrid(long idNhapKho, NhapKho_Model model)
        {
            int rowIndex = grvDSNhapKho.Rows.Add();
            DataGridViewRow row = grvDSNhapKho.Rows[rowIndex];
            row.Height = 35;

            // Ghi id trước để WriteRowFromForm không vô tình xóa mất
            row.Cells["id_NhapKho"].Value = idNhapKho.ToString();
            row.Cells["cuon"].Value = tbxThongTinDay.Text;
            row.Cells["soMet"].Value = model.SoMet;

            WriteRowFromForm(row);

            grvDSNhapKho.FirstDisplayedScrollingRowIndex = grvDSNhapKho.RowCount - 1;
        }

        // ════════════════════════════════════════════════════════════════════════
        // CLICK DÒNG GRID → load form (dùng cho cả Chức năng 2 và 3)
        // ════════════════════════════════════════════════════════════════════════

        private void GrvDSNhapKho_RowClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (e.ColumnIndex < 0) return;

            DataGridViewRow row = grvDSNhapKho.Rows[e.RowIndex];

            _editingIdNhapKho = long.TryParse(
                row.Cells["id_NhapKho"].Value?.ToString(),
                out long nkId)
                ? nkId
                : 0;

            _selectedTTThanhPhamID = long.TryParse(
                row.Cells["TTThanhPham_ID"].Value?.ToString(),
                out long tpId)
                ? tpId
                : (long?)null;

            string ngayText = row.Cells["ngay"].Value?.ToString() ?? string.Empty;

            string[] dateFormats =
            {
                "dd/MM/yyyy", "d/M/yyyy",
                "yyyy-MM-dd",
                "dd-MM-yyyy", "d-M-yyyy"
            };

            if (DateTime.TryParseExact(
                    ngayText,
                    dateFormats,
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None,
                    out DateTime ngayVal))
            {
                dtNgay.Value = ngayVal;
            }
            else if (DateTime.TryParse(ngayText, out ngayVal))
            {
                dtNgay.Value = ngayVal;
            }

            if (decimal.TryParse(
                    row.Cells["soBB"].Value?.ToString(),
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out decimal soBBVal))
            {
                nbSoBB.Value = Math.Min(Math.Max(soBBVal, nbSoBB.Minimum), nbSoBB.Maximum);
            }
            else
            {
                nbSoBB.Value = 0;
            }

            cbxMaBin.Text = row.Cells["maBin2"].Value?.ToString() ?? string.Empty;
            tbTenSP.Text = row.Cells["tenSP"].Value?.ToString() ?? string.Empty;
            tbMaBin.Text = row.Cells["maBin2"].Value?.ToString() ?? string.Empty;

            if (decimal.TryParse(
                row.Cells["soMet"].Value?.ToString(),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out decimal soMetVal))
            {
                nbSoMet.Value = Math.Min(Math.Max(soMetVal, nbSoMet.Minimum), nbSoMet.Maximum);
            }
            else
            {
                nbSoMet.Value = 0;
            }

            bool isHangDat = row.Cells["loaiDon"].Value?.ToString() == "Hàng đặt";
            rdHangDat.Checked = isHangDat;
            rdHangBan.Checked = !isHangDat;

            tbKhachHang.Text = row.Cells["khachHang"].Value?.ToString() ?? string.Empty;

            bool isLo = row.Cells["loai"].Value?.ToString() == "Lô";
            rdLo.Checked = isLo;
            rdCuon.Checked = !isLo;

            if (isLo)
            {
                if (decimal.TryParse(
                        row.Cells["chieuCaoLo"].Value?.ToString(),
                        System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out decimal chieuCaoLoVal))
                {
                    nrChieuCaoLo.Value = Math.Min(Math.Max(chieuCaoLoVal, nrChieuCaoLo.Minimum), nrChieuCaoLo.Maximum);
                }
                else
                {
                    nrChieuCaoLo.Value = 0;
                }

                nrChieuCaoLo.Enabled = true;
            }
            else
            {
                nrChieuCaoLo.Value = 0;
                nrChieuCaoLo.Enabled = false;
            }

            try
            {
                thongTinDayNhapKho = _editingIdNhapKho > 0
                    ? NhapKho_DB.LayThongTinCuonDay(_editingIdNhapKho)
                    : new List<ThongTinCuonDay>();

                bool isCuon = rdCuon.Checked;

                tbxThongTinDay.Text = thongTinDayNhapKho.Count > 0
                    ? CoreHelper.TaoChuoiThongTinCuonDay(thongTinDayNhapKho, isCuon, nrChieuCaoLo.Value)
                    : string.Empty;

                _ttCuonDayChanged = false;
            }
            catch (Exception ex)
            {
                thongTinDayNhapKho = new List<ThongTinCuonDay>();
                tbxThongTinDay.Text = string.Empty;
                _ttCuonDayChanged = false;

                FrmWaiting.ShowGifAlert($"Lỗi khi tải thông tin cuộn/dây:\n{ex.Message}");

            }

            rtbGhiChu.Text = row.Cells["ghiChu"].Value?.ToString() ?? string.Empty;

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
                DataTable dt = NhapKho_DB.TimKiemNhapKho(keyword);
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

            Dictionary<string, List<DataRow>> groups = new Dictionary<string, List<DataRow>>();
            List<string> orderKeys = new List<string>();

            foreach (DataRow dr in dt.Rows)
            {
                string key = GetDbText(dr, "id_NhapKho");

                if (!groups.TryGetValue(key, out List<DataRow> rows))
                {
                    rows = new List<DataRow>();
                    groups[key] = rows;
                    orderKeys.Add(key);
                }

                rows.Add(dr);
            }

            foreach (string key in orderKeys)
            {
                List<DataRow> rows = groups[key];
                if (rows.Count == 0) continue;

                DataRow first = rows[0];

                string loai = GetDbText(first, "loai");
                bool isCuon = loai == "Cuộn";
                decimal chieuCaoLo = ParseDbDecimal(first, "chieuCaoLo");

                List<ThongTinCuonDay> dsCuon = BuildThongTinCuonDay(rows);

                string thongTinDay = dsCuon.Count > 0
                    ? CoreHelper.TaoChuoiThongTinCuonDay(dsCuon, isCuon, chieuCaoLo)
                    : GetDbText(first, "cuonCu");

                int rowIndex = grvDSNhapKho.Rows.Add();
                DataGridViewRow row = grvDSNhapKho.Rows[rowIndex];
                row.Height = 35;

                row.Cells["id_NhapKho"].Value = GetDbText(first, "id_NhapKho");
                row.Cells["TTThanhPham_ID"].Value = GetDbText(first, "TTThanhPham_ID");
                row.Cells["ngay"].Value = GetDbText(first, "ngay");
                row.Cells["soBB"].Value = GetDbText(first, "soBB");
                row.Cells["tenSP"].Value = GetDbText(first, "tenSP");
                row.Cells["soMet"].Value = GetDbText(first, "soMet");
                row.Cells["maBin2"].Value = GetDbText(first, "maBin2");
                row.Cells["loaiDon"].Value = GetDbText(first, "loaiDon");
                row.Cells["khachHang"].Value = GetDbText(first, "khachHang");
                row.Cells["loai"].Value = loai;
                row.Cells["chieuCaoLo"].Value = GetDbNumberText(first, "chieuCaoLo");
                row.Cells["cuon"].Value = thongTinDay;
                row.Cells["ghiChu"].Value = GetDbText(first, "ghiChu");
                
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

        private void ResetForm(bool resetAll = false)
        {
            _maBinSearchHelper.Reset();
            _selectedTTThanhPhamID = null;
            _editingIdNhapKho = 0;

            tbTenSP.Text = string.Empty;
            tbMaBin.Text = string.Empty;
            nbSoMet.Value = 0;
            tbKhachHang.Text = string.Empty;
            rtbGhiChu.Text = string.Empty;

            thongTinDayNhapKho.Clear();
            _ttCuonDayChanged = false;

            tbxThongTinDay.Text = string.Empty;

            nrChieuCaoLo.Value = 0;
            nbSoBB.Value = resetAll ? 0 : nbSoBB.Value;

            rdHangDat.Checked = true;
            rdLo.Checked = true;

            rdHangBan.Checked = false;
            rdCuon.Checked = false;

            _editingRowIndex = -1;
            SetEditMode(false);
            cbxMaBin.Focus();
        }

        private void btnResetForm_Click(object sender, EventArgs e) => ResetForm(resetAll: true);

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
            nbSoBB.Focus();
            nbSoBB.Select(0, int.MaxValue);
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            _maBinSearchHelper?.Dispose();
            base.OnHandleDestroyed(e);
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

        private void btnTTCuon_Click(object sender, EventArgs e)
        {
            bool isCuon = rdCuon.Checked;

            if (!isCuon && nrChieuCaoLo.Value == 0)
            {
                FrmWaiting.ShowGifAlert("Vui lòng nhập chiều cao lô trước.");
                return;
            }

            List<ThongTinCuonDay> duLieuHienTai = thongTinDayNhapKho == null
                ? new List<ThongTinCuonDay>()
                : new List<ThongTinCuonDay>(thongTinDayNhapKho);

            using (Frm_DLCuon frm = new Frm_DLCuon(
                isCuon: isCuon,
                thongTinCuonHienTai: duLieuHienTai
            ))
            {
                if (frm.ShowDialog() != DialogResult.OK)
                    return;

                List<ThongTinCuonDay> duLieuMoi =
                    frm.ThongTinCuon ?? new List<ThongTinCuonDay>();

                decimal tongCD = duLieuMoi.Sum(x => (decimal)x.SoCuon * x.TongChieuDai);
                decimal soMet = nbSoMet.Value;

                if (tongCD != soMet)
                {
                    FrmWaiting.ShowGifAlert("Tổng chiều dài các cuộn không hợp lệ");

                    duLieuMoi.Clear();
                    return;
                }

                thongTinDayNhapKho = duLieuMoi;

                tbxThongTinDay.Text = CoreHelper.TaoChuoiThongTinCuonDay(
                    thongTinDayNhapKho,
                    isCuon,
                    nrChieuCaoLo.Value
                );

                _ttCuonDayChanged = true;
            }
        }

        private void rdCuon_CheckedChanged(object sender, EventArgs e)
        {
            nrChieuCaoLo.Value = 0;
            nrChieuCaoLo.Enabled = !rdCuon.Checked;

            tbxThongTinDay.Text = string.Empty;
            thongTinDayNhapKho.Clear();

            if (_editingIdNhapKho > 0)
                _ttCuonDayChanged = true;
        }


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

        private static List<ThongTinCuonDay> BuildThongTinCuonDay(List<DataRow> rows)
        {
            List<ThongTinCuonDay> result = new List<ThongTinCuonDay>();

            foreach (DataRow row in rows)
            {
                string tongChieuDaiText = GetDbText(row, "ct_TongChieuDai");

                // Không có dòng chi tiết TTCuonDay.
                if (string.IsNullOrWhiteSpace(tongChieuDaiText))
                    continue;

                result.Add(new ThongTinCuonDay
                {
                    SoCuon = ParseDbInt(row, "ct_SoCuon"),
                    TongChieuDai = ParseDbInt(row, "ct_TongChieuDai"),
                    SoDau = ParseDbInt(row, "ct_SoDau"),
                    SoCuoi = ParseDbInt(row, "ct_SoCuoi"),
                    Ghichu = GetDbText(row, "ct_GhiChu")
                });
            }

            return result;
        }

        private static int ParseDbInt(DataRow row, string columnName)
        {
            string text = GetDbText(row, columnName);

            return int.TryParse(
                text,
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out int value)
                ? value
                : 0;
        }

        private static decimal ParseDbDecimal(DataRow row, string columnName)
        {
            string text = GetDbText(row, columnName);

            return decimal.TryParse(
                text,
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out decimal value)
                ? value
                : 0;
        }



        // ── Cấu hình cố định (khai báo 1 lần, dùng lại) ─────────────────────
        // Đường dẫn ảnh: thay bằng đường dẫn thực tế trong project của bạn
        private static readonly LabelPrintConfig _printConfig = LabelPrintConfig.CreateDefault(
            logoSmallPath: @"Assets\Tem\Goldcup-logo.png",    
            certLogoPath: @"Assets\Tem\Certificate.png",      
            kcsLogoPath: @"Assets\Tem\kcs.png",      
            publishedDate: "Ban hành: 13/12/2025"
        );

        private void btnXemDL_Click(object sender, EventArgs e)
        {
            // 1. Xây dựng danh sách tem cần in
            //    (thực tế: lấy từ database / DataGridView / BindingSource...)
            var labels = new List<LabelData>
            {
                new LabelData
                {
                    ProductType    = "Cáp ngầm nhôm 0.6/1kV\nAL/XLPE/PVC/DSTA/PVC 4x150mm2",
                    ProductCode    = "E10-261743/7-01",
                    Length         = "180 m",
                    LengthRange    = "( 0000 ~ 0180 )",
                    CableWeight    = "730 Kg",
                    TotalWeight    = "945 Kg",
                    InspectionDate = "23/04/2026",
                    Inspector      = "Nguyễn Huy Toàn-DG112",
                    QualityResult  = "Đạt",
                    Standard       = "IEC 60502-1",
                    Project        = ""
                }, 
                new LabelData
                {
                    ProductType    = "Cáp ngầm nhôm 0.6/1kV\nAL/XLPE/PVC/DSTA/PVC 4x150mm2",
                    ProductCode    = "E10-261743/7-01",
                    Length         = "180 m",
                    LengthRange    = "( 0000 ~ 0180 )",
                    CableWeight    = "730 Kg",
                    TotalWeight    = "945 Kg",
                    InspectionDate = "23/04/2026",
                    Inspector      = "Nguyễn Huy Toàn-DG112",
                    QualityResult  = "Đạt",
                    Standard       = "IEC 60502-1",
                    Project        = ""
                },
                new LabelData
                {
                    ProductType    = "Cáp ngầm nhôm 0.6/1kV\nAL/XLPE/PVC/DSTA/PVC 4x150mm2",
                    ProductCode    = "E10-261743/7-01",
                    Length         = "180 m",
                    LengthRange    = "( 0000 ~ 0180 )",
                    CableWeight    = "730 Kg",
                    TotalWeight    = "945 Kg",
                    InspectionDate = "23/04/2026",
                    Inspector      = "Nguyễn Huy Toàn-DG112",
                    QualityResult  = "Đạt",
                    Standard       = "IEC 60502-1",
                    Project        = ""
                },
                new LabelData
                {
                    ProductType    = "Cáp ngầm nhôm 0.6/1kV\nAL/XLPE/PVC/DSTA/PVC 4x150mm2",
                    ProductCode    = "E10-261743/7-01",
                    Length         = "180 m",
                    LengthRange    = "( 0000 ~ 0180 )",
                    CableWeight    = "730 Kg",
                    TotalWeight    = "945 Kg",
                    InspectionDate = "23/04/2026",
                    Inspector      = "Nguyễn Huy Toàn-DG112",
                    QualityResult  = "Đạt",
                    Standard       = "IEC 60502-1",
                    Project        = ""
                },
                new LabelData
                {
                    ProductType    = "Cáp ngầm nhôm 0.6/1kV\nAL/XLPE/PVC/DSTA/PVC 4x150mm2",
                    ProductCode    = "E10-261743/7-01",
                    Length         = "180 m",
                    LengthRange    = "( 0000 ~ 0180 )",
                    CableWeight    = "730 Kg",
                    TotalWeight    = "945 Kg",
                    InspectionDate = "23/04/2026",
                    Inspector      = "Nguyễn Huy Toàn-DG112",
                    QualityResult  = "Đạt",
                    Standard       = "IEC 60502-1",
                    Project        = ""
                },
                new LabelData
                {
                    ProductType    = "Cáp ngầm nhôm 0.6/1kV\nAL/XLPE/PVC/DSTA/PVC 4x150mm2",
                    ProductCode    = "E10-261743/7-01",
                    Length         = "180 m",
                    LengthRange    = "( 0000 ~ 0180 )",
                    CableWeight    = "730 Kg",
                    TotalWeight    = "945 Kg",
                    InspectionDate = "23/04/2026",
                    Inspector      = "Nguyễn Huy Toàn-DG112",
                    QualityResult  = "Đạt",
                    Standard       = "IEC 60502-1",
                    Project        = ""
                },
                new LabelData
                {
                    ProductType    = "Cáp ngầm nhôm 0.6/1kV\nAL/XLPE/PVC/DSTA/PVC 4x150mm2",
                    ProductCode    = "E10-261743/7-01",
                    Length         = "180 m",
                    LengthRange    = "( 0000 ~ 0180 )",
                    CableWeight    = "730 Kg",
                    TotalWeight    = "945 Kg",
                    InspectionDate = "23/04/2026",
                    Inspector      = "Nguyễn Huy Toàn-DG112",
                    QualityResult  = "Đạt",
                    Standard       = "IEC 60502-1",
                    Project        = ""
                },

            };

            // 2. Gọi service in
            using (var svc = new LabelPrintService())
            {
                // ShowPreview = true → mở cửa sổ xem trước, người dùng chọn "Print" để in thật
                // ShowPreview = false → in thẳng ra máy in mặc định
                _printConfig.ShowPreview = true;

                svc.Print(labels, _printConfig, ownerForm: this.FindForm());
            }
        }

        // ── Lấy danh sách LabelData từ DataGridView (ví dụ thực tế hơn) ─────
        private List<LabelData> GetLabelsFromGrid(DataGridView dgv)
        {
            var result = new List<LabelData>();
            foreach (DataGridViewRow row in dgv.SelectedRows)
            {
                result.Add(new LabelData
                {
                    ProductType = row.Cells["colProductType"].Value?.ToString() ?? "",
                    ProductCode = row.Cells["colProductCode"].Value?.ToString() ?? "",
                    Length = row.Cells["colLength"].Value?.ToString() ?? "",
                    LengthRange = row.Cells["colLengthRange"].Value?.ToString() ?? "",
                    CableWeight = row.Cells["colCableWeight"].Value?.ToString() ?? "",
                    TotalWeight = row.Cells["colTotalWeight"].Value?.ToString() ?? "",
                    InspectionDate = row.Cells["colInspectionDate"].Value?.ToString() ?? "",
                    Inspector = row.Cells["colInspector"].Value?.ToString() ?? "",
                    QualityResult = row.Cells["colQualityResult"].Value?.ToString() ?? "",
                    Standard = row.Cells["colStandard"].Value?.ToString() ?? "",
                    Project = row.Cells["colProject"].Value?.ToString() ?? ""
                });
            }
            return result;
        }
    }
}