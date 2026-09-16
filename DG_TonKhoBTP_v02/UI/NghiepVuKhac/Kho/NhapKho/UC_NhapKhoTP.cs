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
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using CoreHelper = DG_TonKhoBTP_v02.Helper.Helper;
using FontStyle = System.Drawing.FontStyle;

namespace DG_TonKhoBTP_v02.UI.NghiepVuKhac.Kho.NhapKho
{
    public partial class UC_NhapKhoTP : UserControl
    {
        private ComboBoxSearchHelper _maBinSearchHelper;

        private int _editingRowIndex = -1;

        private long _editingIdNhapKho = 0;

        private long? _selectedTTThanhPhamID = null;
        private bool _headerNhapKhoDaTonTai = false;


        private List<ThongTinCuonDay> thongTinDayNhapKho = new List<ThongTinCuonDay>();
        private bool _ttCuonDayChanged = false;

        public UC_NhapKhoTP()
        {
            InitializeComponent();
            InitMaBinSearch();
            InitGridFont();
            InitImportExcelButton();

            // dataGridView1 chỉ hiển thị thông tin Cuộn/Lô.
            // Việc chỉnh sửa vẫn thực hiện qua Frm_DLCuon.
            dataGridView1.ReadOnly = true;

            grvDSNhapKho.CellDoubleClick += GrvDSNhapKho_CellDoubleClick;

            grvDSNhapKho.CurrentCellDirtyStateChanged += GrvDSNhapKho_CurrentCellDirtyStateChanged;
            grvDSNhapKho.CellValueChanged += GrvDSNhapKho_CellValueChanged;
            grvDSNhapKho.EditingControlShowing += GrvDSNhapKho_EditingControlShowing;
        }


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


        private void LoadThongTinCuonDayTuCongDoan()
        {
            thongTinDayNhapKho = new List<ThongTinCuonDay>();
            _ttCuonDayChanged = false;
            LoadThongTinDayVaoGrid();

            if (!_selectedTTThanhPhamID.HasValue || _selectedTTThanhPhamID.Value <= 0)
                return;

            try
            {
                // Chỉ hiển thị phần TTCuonDay_CD chưa được nhập kho.
                // Số cuộn còn lại = số cuộn nguồn - tổng số cuộn đã có trong TTCuonDay.
                thongTinDayNhapKho = NhapKho_DB.LayTTCuonDayConLaiTheoTTThanhPhamId(_selectedTTThanhPhamID.Value)
                    ?? new List<ThongTinCuonDay>();

                LoadThongTinDayVaoGrid();
            }
            catch (Exception ex)
            {
                thongTinDayNhapKho = new List<ThongTinCuonDay>();
                LoadThongTinDayVaoGrid();
                FrmWaiting.ShowGifAlert($"Lỗi khi tải thông tin cuộn/lô từ công đoạn:\n{ex.Message}");
            }
        }

        private void LoadThongTinDayVaoGrid()
        {
            dataGridView1.Rows.Clear();

            if (thongTinDayNhapKho == null || thongTinDayNhapKho.Count == 0)
                return;

            foreach (ThongTinCuonDay item in thongTinDayNhapKho)
            {
                string loai;
                if (!item.TTLo_ID.HasValue)
                {
                    loai = "Cuộn";
                }
                else if (!item.TTLoHopLe)
                {
                    loai = "Lô (không hợp lệ)";
                }
                else
                {
                    string kichThuoc = (item.KichThuocLo ?? string.Empty).Trim();
                    loai = string.IsNullOrWhiteSpace(kichThuoc)
                        ? "Lô"
                        : $"Lô {kichThuoc}";
                }

                int rowIndex = dataGridView1.Rows.Add();
                DataGridViewRow row = dataGridView1.Rows[rowIndex];
                // ID nguồn là khóa dùng để chống nhập trùng; không hiển thị ra UI.
                row.Tag = item.TTCuonDay_CD_ID;
                row.Cells["col_Loai"].Value = loai;
                row.Cells["col_ChieuDai"].Value = item.TongChieuDai;
                row.Cells["col_SoLuong"].Value = item.SoCuon;
                row.Cells["col_SoDau"].Value = item.SoDau;
                row.Cells["col_SoCuoi"].Value = item.soCuoi;
                row.Cells["cl_GhiChu"].Value = item.Ghichu ?? string.Empty;
            }
        }

        private bool CoTTLoKhongHopLe()
        {
            return thongTinDayNhapKho != null
                && thongTinDayNhapKho.Any(x => x.TTLo_ID.HasValue && !x.TTLoHopLe);
        }

        private bool TryBuildDanhSachNhapKhoTuGrid(out List<ThongTinCuonDay> result, out string error)
        {
            result = new List<ThongTinCuonDay>();
            error = string.Empty;

            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (row.IsNewRow) continue;

                if (row.Tag == null || !long.TryParse(row.Tag.ToString(), out long sourceId) || sourceId <= 0)
                {
                    error = "Có dòng cuộn/lô không xác định được TTCuonDay_CD_ID nguồn.";
                    return false;
                }

                if (!int.TryParse(row.Cells["col_SoLuong"].Value?.ToString(), out int soCuon) || soCuon <= 0)
                {
                    error = $"TTCuonDay_CD id={sourceId}: số cuộn không hợp lệ.";
                    return false;
                }

                if (!int.TryParse(row.Cells["col_ChieuDai"].Value?.ToString(), out int chieuDai1Cuon) || chieuDai1Cuon < 0)
                {
                    error = $"TTCuonDay_CD id={sourceId}: chiều dài 1 cuộn/lô không hợp lệ.";
                    return false;
                }

                int.TryParse(row.Cells["col_SoDau"].Value?.ToString(), out int soDau);
                int.TryParse(row.Cells["col_SoCuoi"].Value?.ToString(), out int soCuoi);

                result.Add(new ThongTinCuonDay
                {
                    TTCuonDay_CD_ID = sourceId,
                    SoCuon = soCuon,
                    // Trong luồng nhập kho, TongChieuDai mang nghĩa chiều dài của 1 cuộn/lô.
                    TongChieuDai = chieuDai1Cuon,
                    SoDau = soDau,
                    soCuoi = soCuoi,
                    Ghichu = row.Cells["cl_GhiChu"].Value?.ToString() ?? string.Empty
                });
            }

            return true;
        }


        private void LoadHeaderNhapKhoNeuCo()
        {
            _headerNhapKhoDaTonTai = false;
            tbNguoiLam.ReadOnly = false;

            if (!_selectedTTThanhPhamID.HasValue || _selectedTTThanhPhamID.Value <= 0)
                return;

            DataTable dt = NhapKho_DB.LayTTNhapKhoTPTheoTTThanhPhamId(_selectedTTThanhPhamID.Value);
            if (dt == null || dt.Rows.Count == 0)
                return;

            DataRow header = dt.Rows[0];
            _headerNhapKhoDaTonTai = true;

            if (header["SoBB"] != DBNull.Value
                && decimal.TryParse(header["SoBB"].ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal soBB))
            {
                nbSoBB.Value = Math.Min(Math.Max(soBB, nbSoBB.Minimum), nbSoBB.Maximum);
            }

            rtbGhiChu.Text = header["GhiChu"] == DBNull.Value
                ? string.Empty
                : header["GhiChu"].ToString();

            tbNguoiLam.Text = header["NguoiLam"] == DBNull.Value
                ? string.Empty
                : header["NguoiLam"].ToString();

            // NguoiLam chỉ thuộc lần tạo header đầu tiên (quyết định 6A).
            tbNguoiLam.ReadOnly = true;
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

            LoadHeaderNhapKhoNeuCo();
            LoadThongTinCuonDayTuCongDoan();

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
            _headerNhapKhoDaTonTai = false;
            tbNguoiLam.ReadOnly = false;
            thongTinDayNhapKho = new List<ThongTinCuonDay>();
            LoadThongTinDayVaoGrid();
            _ttCuonDayChanged = false;
        }

        
        private bool ValidateInputs()
        {
            bool valid = true;
            ResetValidationColors();

            // SoBB là tùy chọn ở nghiệp vụ mới.
            if (string.IsNullOrWhiteSpace(cbxMaBin.Text)) { MarkError(cbxMaBin); valid = false; }
            if (string.IsNullOrWhiteSpace(tbTenSP.Text)) { MarkError(tbTenSP); valid = false; }
            if (string.IsNullOrWhiteSpace(tbMaBin.Text)) { MarkError(tbMaBin); valid = false; }
            if (nbSoMet.Value <= 0) { MarkError(nbSoMet); valid = false; }

            if (!_headerNhapKhoDaTonTai && string.IsNullOrWhiteSpace(tbNguoiLam.Text))
            {
                MarkError(tbNguoiLam);
                valid = false;
            }

            if (!_selectedTTThanhPhamID.HasValue || _selectedTTThanhPhamID.Value <= 0)
            {
                MarkError(cbxMaBin);
                valid = false;
            }

            if (CoTTLoKhongHopLe())
            {
                FrmWaiting.ShowGifAlert("Thông tin cuộn/lô có TTLo_ID không còn tồn tại. Vui lòng mở Thông tin đóng gói và chọn lại loại lô hợp lệ.");
                return false;
            }

            if (!TryBuildDanhSachNhapKhoTuGrid(out List<ThongTinCuonDay> dsGrid, out string gridError)
                || dsGrid.Count == 0)
            {
                if (!string.IsNullOrWhiteSpace(gridError))
                    FrmWaiting.ShowGifAlert(gridError);
                else
                    FrmWaiting.ShowGifAlert("Chưa có cuộn/lô còn lại để nhập kho.");
                return false;
            }

            decimal tongChieuDaiGrid = dsGrid.Sum(x => (decimal)x.SoCuon * x.TongChieuDai);
            if (tongChieuDaiGrid > nbSoMet.Value)
            {
                FrmWaiting.ShowGifAlert(
                    $"Tổng chiều dài trong danh sách ({tongChieuDaiGrid:G}) lớn hơn chiều dài còn lại ({nbSoMet.Value:G}).");
                return false;
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
            rtbGhiChu.BackColor = n;
            tbNguoiLam.BackColor = n;
        }

        

        private void BtnNhapKho_Click(object sender, EventArgs e)
        {
            if (!UserContext.IsAuthenticated
                || (!UserContext.HasRole(RoleNames.Wh) && !UserContext.HasRole(RoleNames.Admin)))
            {
                FrmWaiting.ShowGifAlert("Bạn cần cấp quyền để thực hiện yêu cầu này.");
                return;
            }

            if (!ValidateInputs()) return;

            if (!TryBuildDanhSachNhapKhoTuGrid(out List<ThongTinCuonDay> dsCuon, out string gridError))
            {
                FrmWaiting.ShowGifAlert(gridError);
                return;
            }

            try
            {
                NhapKho_Model model = new NhapKho_Model
                {
                    Ngay = dtNgay.Value.ToString("yyyy-MM-dd"),
                    SoBB = (int)nbSoBB.Value,
                    TTThanhPham_ID = _selectedTTThanhPhamID.Value,
                    TenSP = tbTenSP.Text.Trim(),
                    // SoMet là snapshot TTThanhPham.ChieuDaiSau tại thời điểm người dùng chọn MaBin.
                    SoMet = (double)nbSoMet.Value,
                    GhiChu = rtbGhiChu.Text.Trim(),
                    NguoiLam = tbNguoiLam.Text.Trim()
                };

                long headerId = NhapKho_DB.NhapKho(model, dsCuon);

                // Theo quyết định đã chốt: sau COMMIT đọc lại dữ liệu từ DB, không tự dựng row từ object vừa lưu.
                LoadDongNhapKhoTuDb(headerId);

                ResetForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi lưu nhập kho:\n{ex.Message}",
                    "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private void LoadDongNhapKhoTuDb(long headerId)
        {
            DataTable dt = NhapKho_DB.LayTTNhapKhoTPTheoId(headerId);
            if (dt == null || dt.Rows.Count == 0)
                return;

            DataRow dr = dt.Rows[0];
            DataGridViewRow target = null;

            foreach (DataGridViewRow row in grvDSNhapKho.Rows)
            {
                if (row.IsNewRow) continue;
                if (long.TryParse(row.Cells["id_NhapKho"].Value?.ToString(), out long existingId)
                    && existingId == headerId)
                {
                    target = row;
                    break;
                }
            }

            if (target == null)
            {
                int rowIndex = grvDSNhapKho.Rows.Add();
                target = grvDSNhapKho.Rows[rowIndex];
                target.Height = 35;
            }

            target.Cells["id_NhapKho"].Value = GetDbText(dr, "id_NhapKho");
            target.Cells["TTThanhPham_ID"].Value = GetDbText(dr, "TTThanhPham_ID");
            target.Cells["ngay"].Value = GetDbText(dr, "ngay");
            target.Cells["soBB"].Value = GetDbText(dr, "soBB");
            target.Cells["nguoiLam"].Value = GetDbText(dr, "nguoiLam");
            target.Cells["tenSP"].Value = GetDbText(dr, "tenSP");
            target.Cells["soMet"].Value = GetDbText(dr, "soMet");
            target.Cells["maBin2"].Value = GetDbText(dr, "maBin2");
            target.Cells["ghiChu"].Value = GetDbText(dr, "ghiChu");

            // Các cột nghiệp vụ cũ không còn được nhập ở UC mới.
            target.Cells["loaiDon"].Value = string.Empty;
            target.Cells["khachHang"].Value = string.Empty;
            target.Cells["loai"].Value = string.Empty;
            target.Cells["chieuCaoLo"].Value = string.Empty;
            target.Cells["duAn"].Value = string.Empty;

            if (long.TryParse(GetDbText(dr, "TTThanhPham_ID"), out long ttThanhPhamId)
                && double.TryParse(GetDbText(dr, "soMet"), NumberStyles.Any, CultureInfo.InvariantCulture, out double soMet))
            {
                DiemCacCotBoSungVaoRow(target, ttThanhPhamId, 0, soMet);
            }

            grvDSNhapKho.ClearSelection();
            target.Selected = true;
            if (target.Index >= 0)
                grvDSNhapKho.FirstDisplayedScrollingRowIndex = target.Index;
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (!UserContext.IsAuthenticated
                || (!UserContext.HasRole(RoleNames.Wh) && !UserContext.HasRole(RoleNames.Admin)))
            {
                FrmWaiting.ShowGifAlert($"Bạn cần cấp quyền để thực hiện yêu cầu này.");
                return;
            }

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
            if (!UserContext.IsAuthenticated
                || (!UserContext.HasRole(RoleNames.Wh) && !UserContext.HasRole(RoleNames.Admin)))
            {
                FrmWaiting.ShowGifAlert($"Bạn cần cấp quyền để thực hiện yêu cầu này.");
                return;
            }

            if (_editingRowIndex < 0 || _editingRowIndex >= grvDSNhapKho.Rows.Count)
            {
                MessageBox.Show("Không có dòng nào được chọn để sửa.",
                    "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (_editingIdNhapKho <= 0)
            {
                MessageBox.Show("Dòng này chưa được lưu vào cơ sở dữ liệu, không thể cập nhật.",
                    "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {                

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
                    TTThanhPham_ID = _selectedTTThanhPhamID.HasValue
                        ? _selectedTTThanhPhamID.Value
                        : ttThanhPhamIdCu,
                    TenSP = tbTenSP.Text.Trim(),
                    SoMet = (double)nbSoMet.Value,

                    GhiChu = rtbGhiChu.Text.Trim(),
                    NguoiLam = tbNguoiLam.Text.Trim()
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

                LoadDongNhapKhoTuDb(_editingIdNhapKho);
                ResetForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi cập nhật:\n{ex.Message}",
                    "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private void WriteRowFromForm(DataGridViewRow row)
        {
            row.Cells["ngay"].Value = dtNgay.Value.ToString("dd/MM/yyyy");
            row.Cells["soBB"].Value = nbSoBB.Value > 0 ? nbSoBB.Value.ToString() : string.Empty;
            row.Cells["nguoiLam"].Value = tbNguoiLam.Text.Trim();
            row.Cells["TTThanhPham_ID"].Value = _selectedTTThanhPhamID?.ToString() ?? string.Empty;
            row.Cells["tenSP"].Value = tbTenSP.Text.Trim();
            row.Cells["maBin2"].Value = tbMaBin.Text.Trim();
            row.Cells["soMet"].Value = nbSoMet.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);

            // Các trường này không còn thuộc UI nhập kho ở schema mới.
            row.Cells["loaiDon"].Value = string.Empty;
            row.Cells["khachHang"].Value = string.Empty;
            row.Cells["loai"].Value = string.Empty;
            row.Cells["chieuCaoLo"].Value = string.Empty;
            row.Cells["ghiChu"].Value = rtbGhiChu.Text.Trim();
            row.Cells["duAn"].Value = string.Empty;
        }

        private void ThemDongVaoGrid(long idNhapKho, NhapKho_Model model)
        {
            int rowIndex = grvDSNhapKho.Rows.Add();
            DataGridViewRow row = grvDSNhapKho.Rows[rowIndex];
            row.Height = 35;

            // Ghi id trước để WriteRowFromForm không vô tình xóa mất
            row.Cells["id_NhapKho"].Value = idNhapKho.ToString();
            //row.Cells["cuon"].Value = LayGiaTriCuonTuThongTinDay(tbxThongTinDay.Text);
            row.Cells["soMet"].Value = model.SoMet;

            WriteRowFromForm(row);

            // ── Điền các cột bổ sung: TTBoSung + TTLo ──────────────────────────
            DiemCacCotBoSungVaoRow(row, model.TTThanhPham_ID, model.ChieuCaoLo, model.SoMet);

            grvDSNhapKho.FirstDisplayedScrollingRowIndex = grvDSNhapKho.RowCount - 1;
        }

        /// <summary>
        /// Query TTBoSung + TTLo rồi điền vào các cột:
        /// tenChiTiet, tieuChuan, khoiLuongCap, klLo (ComboBox), klTong.
        /// </summary>
        private void DiemCacCotBoSungVaoRow(DataGridViewRow row, long ttThanhPhamId, double chieuCaoLo, double soMet)
        {
            try
            {
                var info = NhapKho_DB.LayThongTinBoSungVaLo(ttThanhPhamId, chieuCaoLo);

                bool heSoTBang0 = info.HeSoT == 0;
                double khoiLuongCap = heSoTBang0 ? 0 : soMet * info.HeSoT;

                row.Cells["tenChiTiet"].Value = info.TenChiTiet;
                row.Cells["tieuChuan"].Value = info.TieuChuan;
                row.Cells["khoiLuongCap"].Value = khoiLuongCap.ToString("G6", System.Globalization.CultureInfo.InvariantCulture);

                if (heSoTBang0)
                    DanhDauHeSoTBang0(row, hienThongBao: true);
                else
                    XoaDanhDauHeSoTBang0(row);

                // ── Xây dựng ComboBox klLo ────────────────────────────────────
                var cbCell = row.Cells["klLo"] as DataGridViewComboBoxCell;
                if (cbCell != null)
                {
                    cbCell.Items.Clear();

                    double klDefault = 0;

                    if (info.KlKhoiLuong.HasValue)
                    {
                        string itemKL = info.KlKhoiLuong.Value.ToString("G6", System.Globalization.CultureInfo.InvariantCulture);
                        cbCell.Items.Add(itemKL);
                        klDefault = info.KlKhoiLuong.Value;
                    }

                    if (info.KlKhoiLuongCaNanPhu.HasValue)
                    {
                        string itemCNP = info.KlKhoiLuongCaNanPhu.Value.ToString("G6", System.Globalization.CultureInfo.InvariantCulture);
                        cbCell.Items.Add(itemCNP);
                    }

                    // Giá trị mặc định = KhoiLuong
                    if (cbCell.Items.Count > 0)
                        cbCell.Value = cbCell.Items[0];

                    // Nếu HeSoT = 0 thì KL Tổng đặt là 0 theo yêu cầu.
                    double klTong = heSoTBang0 ? 0 : khoiLuongCap + klDefault;
                    row.Cells["klTong"].Value = klTong.ToString("G6", System.Globalization.CultureInfo.InvariantCulture);
                }
                else
                {
                    row.Cells["klTong"].Value = heSoTBang0
                        ? "0"
                        : khoiLuongCap.ToString("G6", System.Globalization.CultureInfo.InvariantCulture);
                }
            }
            catch (Exception ex)
            {
                // Không để lỗi phụ làm hỏng luồng nhập kho chính
                System.Diagnostics.Debug.WriteLine($"[DiemCacCotBoSungVaoRow] {ex.Message}");
            }
        }


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
            tbNguoiLam.Text = row.Cells["nguoiLam"].Value?.ToString() ?? string.Empty;
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

            try
            {
                thongTinDayNhapKho = _editingIdNhapKho > 0
                    ? NhapKho_DB.LayThongTinCuonDay(_editingIdNhapKho)
                    : new List<ThongTinCuonDay>();

                //tbxThongTinDay.Text = thongTinDayNhapKho.Count > 0
                //    ? CoreHelper.TaoChuoiThongTinCuonDay(thongTinDayNhapKho)
                //    : string.Empty;

                _ttCuonDayChanged = false;
            }
            catch (Exception ex)
            {
                thongTinDayNhapKho = new List<ThongTinCuonDay>();
                //tbxThongTinDay.Text = string.Empty;
                _ttCuonDayChanged = false;

                FrmWaiting.ShowGifAlert($"Lỗi khi tải thông tin cuộn/dây:\n{ex.Message}");

            }

            rtbGhiChu.Text = row.Cells["ghiChu"].Value?.ToString() ?? string.Empty;

            _editingRowIndex = e.RowIndex;
            SetEditMode(true);
            ResetValidationColors();
        }

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
            int heSoT0Count = 0;

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
                row.Cells["NguoiLam"].Value = GetDbText(first, "nguoiLam");
                row.Cells["tenSP"].Value = GetDbText(first, "tenSP");
                row.Cells["soMet"].Value = GetDbText(first, "soMet");
                row.Cells["maBin2"].Value = GetDbText(first, "maBin2");
                row.Cells["loaiDon"].Value = GetDbText(first, "loaiDon");
                row.Cells["khachHang"].Value = GetDbText(first, "khachHang");
                row.Cells["loai"].Value = loai;
                row.Cells["chieuCaoLo"].Value = GetDbNumberText(first, "chieuCaoLo");
                row.Cells["cuon"].Value = LayGiaTriCuonTuThongTinDay(thongTinDay);
                row.Cells["ghiChu"].Value = GetDbText(first, "ghiChu");
                row.Cells["duAn"].Value = GetDbText(first, "tenDuAn");

                // ── Điền cột TTBoSung + TTLo (đã JOIN sẵn trong query) ──────────
                double soMetRow = ParseDbDecimal(first, "soMet") == 0 ? 0 : (double)ParseDbDecimal(first, "soMet");
                double heSoT = 0;
                bool coGiaTriHeSoT = first.Table.Columns.Contains("heSoT") && first["heSoT"] != DBNull.Value;
                if (coGiaTriHeSoT)
                    double.TryParse(first["heSoT"].ToString(),
                        System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out heSoT);

                bool heSoTBang0 = coGiaTriHeSoT && heSoT == 0;
                double khoiLuongCap = heSoTBang0 ? 0 : soMetRow * heSoT;

                row.Cells["tenChiTiet"].Value = GetDbText(first, "tenChiTiet");
                row.Cells["tieuChuan"].Value = GetDbText(first, "tieuChuan");
                row.Cells["khoiLuongCap"].Value = khoiLuongCap.ToString("G6", System.Globalization.CultureInfo.InvariantCulture);

                if (heSoTBang0)
                {
                    DanhDauHeSoTBang0(row, hienThongBao: false);
                    heSoT0Count++;
                }
                else
                {
                    XoaDanhDauHeSoTBang0(row);
                }

                // ComboBox klLo
                var cbCell = row.Cells["klLo"] as DataGridViewComboBoxCell;
                if (cbCell != null)
                {
                    cbCell.Items.Clear();
                    double klDefault = 0;

                    if (first.Table.Columns.Contains("klLoKhoiLuong") && first["klLoKhoiLuong"] != DBNull.Value)
                    {
                        double.TryParse(first["klLoKhoiLuong"].ToString(),
                            System.Globalization.NumberStyles.Any,
                            System.Globalization.CultureInfo.InvariantCulture, out double klKL);
                        string itemKL = klKL.ToString("G6", System.Globalization.CultureInfo.InvariantCulture);
                        cbCell.Items.Add(itemKL);
                        klDefault = klKL;
                    }

                    if (first.Table.Columns.Contains("klLoKhoiLuongCaNanPhu") && first["klLoKhoiLuongCaNanPhu"] != DBNull.Value)
                    {
                        double.TryParse(first["klLoKhoiLuongCaNanPhu"].ToString(),
                            System.Globalization.NumberStyles.Any,
                            System.Globalization.CultureInfo.InvariantCulture, out double klCNP);
                        cbCell.Items.Add(klCNP.ToString("G6", System.Globalization.CultureInfo.InvariantCulture));
                    }

                    if (cbCell.Items.Count > 0)
                        cbCell.Value = cbCell.Items[0];

                    row.Cells["klTong"].Value = (heSoTBang0 ? 0 : khoiLuongCap + klDefault).ToString(
                        "G6", System.Globalization.CultureInfo.InvariantCulture);
                }
                else
                {
                    row.Cells["klTong"].Value = heSoTBang0
                        ? "0"
                        : khoiLuongCap.ToString("G6", System.Globalization.CultureInfo.InvariantCulture);
                }

            }

            if (heSoT0Count > 0)
            {
                FrmWaiting.ShowGifAlert($"Có {heSoT0Count} dòng có hệ số T = 0. Đã tô vàng ô KL cáp và đặt KL Tổng = 0.");
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

        private void SetEditMode(bool editMode)
        {
            btnSua.Enabled = editMode;
            btnDelete.Enabled = editMode;
            btnNhapKho.Enabled = !editMode;
            SetImportExcelButtonEnabled(!editMode);
        }

        private void ResetForm(bool resetAll = false)
        {
            _maBinSearchHelper.Reset();
            _selectedTTThanhPhamID = null;
            _headerNhapKhoDaTonTai = false;
            _editingIdNhapKho = 0;
            tbNguoiLam.ReadOnly = false;

            tbTenSP.Text = string.Empty;
            tbMaBin.Text = string.Empty;
            nbSoMet.Value = 0;
            rtbGhiChu.Text = string.Empty;

            thongTinDayNhapKho.Clear();
            _ttCuonDayChanged = false;

            //tbxThongTinDay.Text = string.Empty;

            nbSoBB.Value = resetAll ? 0 : nbSoBB.Value;


            _editingRowIndex = -1;
            SetEditMode(false);
            cbxMaBin.Focus();
        }

        private void btnResetForm_Click(object sender, EventArgs e) => ResetForm(resetAll: true);


        private void GrvDSNhapKho_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            GrvDSNhapKho_RowClick(sender, e);
        }

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
            grvDSNhapKho.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;

            if (grvDSNhapKho.Columns.Contains("tenChiTiet"))
                grvDSNhapKho.Columns["tenChiTiet"].DefaultCellStyle.WrapMode = DataGridViewTriState.True;
        }

        private void GrvDSNhapKho_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            // Commit ngay khi người dùng chọn item trong ComboBox
            if (grvDSNhapKho.IsCurrentCellDirty &&
                grvDSNhapKho.CurrentCell?.OwningColumn?.Name == "klLo")
            {
                grvDSNhapKho.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        }

        private void GrvDSNhapKho_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

            string columnName = grvDSNhapKho.Columns[e.ColumnIndex]?.Name;
            if (columnName != "klLo" && columnName != "khoiLuongCap") return;

            TinhLaiKlTong(grvDSNhapKho.Rows[e.RowIndex]);
        }

        private void TinhLaiKlTong(DataGridViewRow row)
        {
            if (row == null || row.IsNewRow) return;

            string klLoText = row.Cells["klLo"].Value?.ToString() ?? "0";
            string klCapText = row.Cells["khoiLuongCap"].Value?.ToString() ?? "0";

            double klLo = ParseDoubleOrZero(klLoText);
            double klCap = ParseDoubleOrZero(klCapText);

            row.Cells["klTong"].Value = (klCap + klLo).ToString(
                "G6", System.Globalization.CultureInfo.InvariantCulture);
        }

        private static double ParseDoubleOrZero(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return 0;

            return double.TryParse(text, System.Globalization.NumberStyles.Any,
                       System.Globalization.CultureInfo.InvariantCulture, out double value)
                   || double.TryParse(text, System.Globalization.NumberStyles.Any,
                       System.Globalization.CultureInfo.CurrentCulture, out value)
                ? value
                : 0;
        }

        private void GrvDSNhapKho_EditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
        {
            if (e.Control is TextBox tb)
            {
                tb.KeyDown -= TenChiTietEditingTextBox_KeyDown;
                tb.Multiline = false;
                tb.AcceptsReturn = false;
                tb.WordWrap = false;

                string columnName = grvDSNhapKho.CurrentCell?.OwningColumn?.Name;
                if (columnName == "tenChiTiet")
                {
                    tb.Multiline = true;
                    tb.AcceptsReturn = true;
                    tb.WordWrap = true;
                    tb.KeyDown += TenChiTietEditingTextBox_KeyDown;
                }
            }
        }

        private void TenChiTietEditingTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter || !e.Shift) return;

            if (sender is TextBox tb)
            {
                tb.SelectedText = Environment.NewLine;
                e.SuppressKeyPress = true;
                e.Handled = true;
            }
        }

        private static string LayGiaTriCuonTuThongTinDay(string thongTinDay)
        {
            if (string.IsNullOrWhiteSpace(thongTinDay)) return string.Empty;

            Match match = Regex.Match(thongTinDay, @"\(([^()\s]+-[^()\s]+)\)");
            return match.Success ? match.Groups[1].Value.Trim() : string.Empty;
        }

        private static void DanhDauHeSoTBang0(DataGridViewRow row, bool hienThongBao)
        {
            if (row?.DataGridView == null || !row.DataGridView.Columns.Contains("khoiLuongCap")) return;

            DataGridViewCell cell = row.Cells["khoiLuongCap"];
            cell.Style.BackColor = Color.Yellow;

            if (hienThongBao)
                FrmWaiting.ShowGifAlert("Hệ số T = 0. Đã tô vàng ô KL cáp và đặt KL Tổng = 0.");
        }

        private static void XoaDanhDauHeSoTBang0(DataGridViewRow row)
        {
            if (row?.DataGridView == null || !row.DataGridView.Columns.Contains("khoiLuongCap")) return;

            DataGridViewCell cell = row.Cells["khoiLuongCap"];
            cell.Style.BackColor = Color.Empty;
        }

        private void btnTTCuon_Click(object sender, EventArgs e)
        {
            if (!_selectedTTThanhPhamID.HasValue || _selectedTTThanhPhamID.Value <= 0)
            {
                FrmWaiting.ShowGifAlert("Vui lòng chọn MaBin trước khi chỉnh sửa thông tin đóng gói.");
                return;
            }

            try
            {
                // Frm_DLCuon phải chỉnh nguồn đầy đủ TTCuonDay_CD, không phải chỉ phần còn lại.
                List<ThongTinCuonDay> duLieuNguon =
                    DatabaseHelper.LayTTCuonDayCDTheoTTThanhPhamId(_selectedTTThanhPhamID.Value)
                    ?? new List<ThongTinCuonDay>();

                using (Frm_DLCuon frm = new Frm_DLCuon(thongTinCuonHienTai: duLieuNguon))
                {
                    if (frm.ShowDialog() != DialogResult.OK)
                        return;

                    List<ThongTinCuonDay> duLieuMoi =
                        frm.ThongTinCuon ?? new List<ThongTinCuonDay>();

                    // Cập nhật trực tiếp TTCuonDay_CD. DB sẽ chặn:
                    // - giảm SoCuon thấp hơn số đã nhập;
                    // - sửa thuộc tính kỹ thuật của dòng đã có lịch sử nhập;
                    // - xoá dòng đã có lịch sử nhập.
                    NhapKho_DB.CapNhatTTCuonDayCDTheoTTThanhPhamId(
                        _selectedTTThanhPhamID.Value,
                        duLieuMoi);

                    // Sau khi sửa nguồn, tải lại CHỈ phần còn lại để chuẩn bị cho lần nhập hiện tại.
                    LoadThongTinCuonDayTuCongDoan();
                    _ttCuonDayChanged = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi cập nhật thông tin cuộn/lô:\n{ex.Message}",
                    "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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
                    TTCuonDay_CD_ID = ParseDbLongNullable(row, "ct_TTCuonDay_CD_ID"),
                    TTLo_ID = ParseDbIntNullable(row, "ct_TTLo_ID"),
                    KichThuocLo = GetDbText(row, "ct_KichThuocLo"),
                    TTLoHopLe = !ParseDbIntNullable(row, "ct_TTLo_ID").HasValue
                        || !string.IsNullOrWhiteSpace(GetDbText(row, "ct_KichThuocLo")),
                    SoCuon = ParseDbInt(row, "ct_SoCuon"),
                    TongChieuDai = ParseDbInt(row, "ct_TongChieuDai"),
                    SoDau = ParseDbInt(row, "ct_SoDau"),
                    soCuoi = ParseDbInt(row, "ct_soCuoi"),
                    Ghichu = GetDbText(row, "ct_GhiChu")
                });
            }

            return result;
        }

        private static int? ParseDbIntNullable(DataRow row, string columnName)
        {
            string text = GetDbText(row, columnName);
            return int.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out int value)
                ? value
                : (int?)null;
        }

        private static long? ParseDbLongNullable(DataRow row, string columnName)
        {
            string text = GetDbText(row, columnName);
            return long.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out long value)
                ? value
                : (long?)null;
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
            grvDSNhapKho.EndEdit();

            var labels = GetLabelsFromGrid();

            if (labels.Count == 0)
            {
                MessageBox.Show("Không có dữ liệu để in tem.",
                    "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var svc = new LabelPrintService())
            {
                _printConfig.ShowPreview = true;
                svc.Print(labels, _printConfig, ownerForm: this.FindForm());
            }
        }

        // ── Lấy danh sách LabelData từ DataGridView (ví dụ thực tế hơn) ─────
        private List<LabelData> GetLabelsFromGrid()
        {
            var labels = new List<LabelData>();

            foreach (DataGridViewRow row in grvDSNhapKho.Rows)
            {
                if (row.IsNewRow) continue;

                labels.Add(new LabelData
                {
                    ProductType = GetCellText(row, "tenChiTiet"),
                    ProductCode = GetCellText(row, "maBin2"),
                    Length = GetCellText(row, "soMet") + " m",
                    LengthRange = GetCellText(row, "cuon"),
                    CableWeight = GetCellText(row, "khoiLuongCap") + " kg",
                    TotalWeight = GetCellText(row, "klTong") + " kg"    ,
                    InspectionDate = GetCellText(row, "ngay"),
                    Inspector = GetCellText(row, "nguoiLam"),
                    QualityResult = "Đạt",
                    Standard = GetCellText(row, "tieuChuan"),
                    Project = GetCellText(row, "duAn")
                });
            }

            return labels;
        }

        private string GetCellText(DataGridViewRow row, string columnName)
        {
            if (row == null) return string.Empty;
            if (row.DataGridView == null) return string.Empty;
            if (!row.DataGridView.Columns.Contains(columnName)) return string.Empty;

            return row.Cells[columnName].Value?.ToString()?.Trim() ?? string.Empty;
        }

        private void btnSetLoLe_Click(object sender, EventArgs e)
        {

        }

    }
}