using DG_TonKhoBTP_v02.Database;
using DG_TonKhoBTP_v02.Database.ChatLuong;
using DG_TonKhoBTP_v02.Models;
using DG_TonKhoBTP_v02.UI.Helper.AutoSearchWithCombobox;
using DG_TonKhoBTP_v02.UI.NghiepVuKhac.Kho;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using FontStyle = System.Drawing.FontStyle;

namespace DG_TonKhoBTP_v02.UI.NghiepVuKhac.Kho.NhapKho
{
    public partial class UC_NhapKhoTP : UserControl
    {
        private ComboBoxSearchHelper _maBinSearchHelper;
        private int _editingRowIndex = -1;
        private long _editingIdNhapKho = 0;
        private long? _selectedTTThanhPhamID = null;
        private List<ThongTinCuonDay> thongTinDayNhapKho = new List<ThongTinCuonDay>();
        private bool _ttCuonDayChanged = false;
        private bool _suppressFilterEvents = false;

        private sealed class ThongTinCuonDayGridTag
        {
            public long? TTCuonDayId { get; set; }
            public long? SourceId { get; set; }
        }

        public UC_NhapKhoTP()
        {
            InitializeComponent();
            InitMaBinSearch();
            InitGridFont();

            // dataGridView1 chỉ hiển thị thông tin Cuộn/Lô.
            // Chỉnh sửa cuộn/lô vẫn thực hiện qua Frm_DLCuon.
            dataGridView1.ReadOnly = true;
            grvDSNhapKho.ReadOnly = true;

            grvDSNhapKho.CellDoubleClick += GrvDSNhapKho_CellDoubleClick;
            btnNhapKhoTheoNgay.Click += btnNhapKhoTheoNgay_Click;
            checkBox1.CheckedChanged += checkBox1_CheckedChanged;
            dtNgay.ValueChanged += dtNgay_ValueChanged;
            cbxMaBin.KeyDown += cbxMaBin_KeyDown;
        }

        private void InitMaBinSearch()
        {
            _maBinSearchHelper = new ComboBoxSearchHelper(
                comboBox: cbxMaBin,
                queryFunc: NhapKho_DB.TimKiemMaBinAsync
            );
            _maBinSearchHelper.DisplayColumn = "MaBin";
            _maBinSearchHelper.SelectedTextBehavior = ComboBoxSelectedTextBehavior.FillDisplayText;
            _maBinSearchHelper.CanSearch = _ => !checkBox1.Checked;
            _maBinSearchHelper.ItemSelected += OnMaBinSelected;
            _maBinSearchHelper.Cleared += OnMaBinCleared;
        }

        private static void LayDuLieuTTLoChoFrm(
            IEnumerable<ThongTinCuonDay> data,
            out DataTable ttLoActive,
            out DataTable ttLoReferenced)
        {
            // Mọi truy xuất DB phục vụ Frm_DLCuon được thực hiện tại caller.
            ttLoActive = DatabaseHelper.LayDanhSachTTLoActive() ?? new DataTable();

            List<int> referencedIds = (data ?? Enumerable.Empty<ThongTinCuonDay>())
                .Where(x => x != null && x.TTLo_ID.HasValue && x.TTLo_ID.Value > 0)
                .Select(x => x.TTLo_ID.Value)
                .Distinct()
                .ToList();

            ttLoReferenced = referencedIds.Count == 0
                ? new DataTable()
                : (DatabaseHelper.LayDanhSachTTLoTheoIds(referencedIds) ?? new DataTable());
        }

        private static bool TryGetGridSourceId(DataGridViewRow row, out long sourceId)
        {
            sourceId = 0;
            if (row == null || row.Tag == null) return false;

            if (row.Tag is ThongTinCuonDayGridTag tag)
            {
                if (!tag.SourceId.HasValue || tag.SourceId.Value <= 0) return false;
                sourceId = tag.SourceId.Value;
                return true;
            }

            return long.TryParse(row.Tag.ToString(), out sourceId) && sourceId > 0;
        }

        private static long? GetGridTTCuonDayId(DataGridViewRow row)
        {
            if (row?.Tag is ThongTinCuonDayGridTag tag
                && tag.TTCuonDayId.HasValue
                && tag.TTCuonDayId.Value > 0)
                return tag.TTCuonDayId.Value;

            return null;
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
                // Giữ cả identity TTCuonDay và lineage TTCuonDay_CD trong Tag, không hiển thị ra UI.
                row.Tag = new ThongTinCuonDayGridTag
                {
                    TTCuonDayId = item.TTCuonDay_ID,
                    SourceId = item.TTCuonDay_CD_ID
                };
                row.Cells["col_Loai"].Value = loai;
                row.Cells["col_ChieuDai"].Value = item.TongChieuDai;
                row.Cells["col_SoLuong"].Value = item.SoCuon;
                row.Cells["col_SoDau"].Value = item.SoDau;
                row.Cells["col_SoCuoi"].Value = item.soCuoi;
                row.Cells["cl_GhiChu"].Value = item.Ghichu ?? string.Empty;
            }
        }

        private bool TryLayThongTinCuonDayHienTaiTuGrid(out List<ThongTinCuonDay> result, out string error)
        {
            result = new List<ThongTinCuonDay>();
            error = string.Empty;

            List<ThongTinCuonDay> current = thongTinDayNhapKho ?? new List<ThongTinCuonDay>();
            var byTTCuonDayId = current
                .Where(x => x != null && x.TTCuonDay_ID.HasValue && x.TTCuonDay_ID.Value > 0)
                .GroupBy(x => x.TTCuonDay_ID.Value)
                .ToDictionary(g => g.Key, g => g.First());

            var bySourceId = current
                .Where(x => x != null && x.TTCuonDay_CD_ID.HasValue && x.TTCuonDay_CD_ID.Value > 0)
                .GroupBy(x => x.TTCuonDay_CD_ID.Value)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (row.IsNewRow) continue;

                if (!TryGetGridSourceId(row, out long sourceId))
                {
                    error = "Có dòng cuộn/lô không xác định được TTCuonDay_CD_ID nguồn.";
                    return false;
                }

                long? ttCuonDayId = GetGridTTCuonDayId(row);
                ThongTinCuonDay source = null;

                if (ttCuonDayId.HasValue)
                {
                    byTTCuonDayId.TryGetValue(ttCuonDayId.Value, out source);
                }
                else if (bySourceId.TryGetValue(sourceId, out List<ThongTinCuonDay> candidates)
                    && candidates.Count == 1)
                {
                    source = candidates[0];
                }

                if (source == null)
                {
                    error = ttCuonDayId.HasValue
                        ? $"Không tìm thấy TTCuonDay id={ttCuonDayId.Value} trong danh sách hiện tại."
                        : $"Không xác định duy nhất dữ liệu nguồn TTCuonDay_CD id={sourceId} trong danh sách hiện tại.";
                    return false;
                }

                if (!int.TryParse(row.Cells["col_SoLuong"].Value?.ToString(), out int soCuon) || soCuon <= 0)
                {
                    error = $"TTCuonDay_CD id={sourceId}: số cuộn không hợp lệ.";
                    return false;
                }

                int tongChieuDai = source.TongChieuDai;
                int? soDau = source.SoDau;
                int? soCuoi = source.soCuoi;

                if (int.TryParse(row.Cells["col_ChieuDai"].Value?.ToString(), out int parsedTongChieuDai))
                    tongChieuDai = parsedTongChieuDai;
                if (int.TryParse(row.Cells["col_SoDau"].Value?.ToString(), out int parsedSoDau))
                    soDau = parsedSoDau;
                if (int.TryParse(row.Cells["col_SoCuoi"].Value?.ToString(), out int parsedSoCuoi))
                    soCuoi = parsedSoCuoi;

                result.Add(new ThongTinCuonDay
                {
                    TTCuonDay_ID = ttCuonDayId ?? source.TTCuonDay_ID,
                    TTCuonDay_CD_ID = sourceId,
                    CoLichSuDownstream = source.CoLichSuDownstream,
                    TTLo_ID = source.TTLo_ID,
                    KichThuocLo = source.KichThuocLo ?? string.Empty,
                    TTLoHopLe = source.TTLoHopLe,
                    SoCuon = soCuon,
                    TongChieuDai = tongChieuDai,
                    SoDau = soDau,
                    soCuoi = soCuoi,
                    Ghichu = row.Cells["cl_GhiChu"].Value?.ToString() ?? source.Ghichu ?? string.Empty
                });
            }

            return true;
        }

        private void OnMaBinSelected(DataRowView row)
        {
            string maBin = row?["MaBin"]?.ToString()?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(maBin)) return;

            _suppressFilterEvents = true;
            try
            {
                if (checkBox1.Checked)
                    checkBox1.Checked = false;
                dtNgay.Enabled = false;
                cbxMaBin.Text = maBin;
            }
            finally
            {
                _suppressFilterEvents = false;
            }

            TimKiemTheoMaBin(maBin);
        }

        private void OnMaBinCleared()
        {
            if (_suppressFilterEvents || checkBox1.Checked)
                return;

            ClearGridAndEditingDetails();
        }

        private void cbxMaBin_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Handled || e.KeyCode != Keys.Enter)
                return;

            string maBin = cbxMaBin.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(maBin))
                return;

            e.Handled = true;
            e.SuppressKeyPress = true;

            _suppressFilterEvents = true;
            try
            {
                if (checkBox1.Checked)
                    checkBox1.Checked = false;
                dtNgay.Enabled = false;
            }
            finally
            {
                _suppressFilterEvents = false;
            }

            TimKiemTheoMaBin(maBin);
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (_suppressFilterEvents) return;

            dtNgay.Enabled = checkBox1.Checked;

            if (checkBox1.Checked)
            {
                TimKiemTheoNgay();
                return;
            }

            string maBin = cbxMaBin.Text?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(maBin))
                TimKiemTheoMaBin(maBin);
            else
                ClearGridAndEditingDetails();
        }

        private void dtNgay_ValueChanged(object sender, EventArgs e)
        {
            if (_suppressFilterEvents || !checkBox1.Checked)
                return;

            TimKiemTheoNgay();
        }

        private void TimKiemTheoNgay()
        {
            try
            {
                DataTable dt = NhapKho_DB.TimKiemNhapKhoTheoNgay(dtNgay.Value.Date);
                LoadNhapKhoVaoGrid(dt);
                // Theo quyết định đã chốt: tìm theo ngày chỉ hiển thị danh sách.
                ClearEditingDetails();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tìm dữ liệu nhập kho theo ngày:\n{ex.Message}",
                    "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void TimKiemTheoMaBin(string maBin)
        {
            try
            {
                DataTable dt = NhapKho_DB.TimKiemNhapKhoTheoMaBin(maBin);
                LoadNhapKhoVaoGrid(dt);

                // Nếu MaBin chỉ có đúng một lần nhập thì hiển thị luôn chi tiết.
                // Nếu có nhiều lần nhập thì chỉ hiển thị grvDSNhapKho và chờ double-click.
                if (dt.Rows.Count == 1
                    && long.TryParse(GetDbText(dt.Rows[0], "id_NhapKho"), out long idNhapKho)
                    && idNhapKho > 0)
                {
                    LoadChiTietNhapKho(idNhapKho, TimRowIndexTheoId(idNhapKho));
                }
                else
                {
                    ClearEditingDetails();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tìm dữ liệu nhập kho theo MaBin:\n{ex.Message}",
                    "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RefreshCurrentFilter()
        {
            if (checkBox1.Checked)
            {
                TimKiemTheoNgay();
                return;
            }

            string maBin = cbxMaBin.Text?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(maBin))
                TimKiemTheoMaBin(maBin);
            else
                ClearGridAndEditingDetails();
        }

        private void LoadNhapKhoVaoGrid(DataTable dt)
        {
            if (grvDSNhapKho.DataSource != null)
                grvDSNhapKho.DataSource = null;

            grvDSNhapKho.Rows.Clear();

            if (dt != null)
            {
                foreach (DataRow dr in dt.Rows)
                {
                    if (!long.TryParse(GetDbText(dr, "id_NhapKho"), out long idNhapKho) || idNhapKho <= 0)
                        continue;

                    int rowIndex = grvDSNhapKho.Rows.Add();
                    DataGridViewRow row = grvDSNhapKho.Rows[rowIndex];
                    row.Height = 35;
                    row.Cells["id_NhapKho"].Value = idNhapKho;
                    row.Cells["TTThanhPham_ID"].Value = GetDbText(dr, "TTThanhPham_ID");
                    row.Cells["ngay"].Value = GetDbText(dr, "ngay");
                    row.Cells["nguoiLam"].Value = GetDbText(dr, "nguoiLam");
                    row.Cells["tenSP"].Value = GetDbText(dr, "tenSP");
                    row.Cells["soMet"].Value = GetDbText(dr, "soMet");
                    row.Cells["maBin2"].Value = GetDbText(dr, "maBin2");
                    row.Cells["ghiChu"].Value = GetDbText(dr, "ghiChu");
                    row.Cells["thongSo"].Value = TaoChuoiDSCuonLo(NhapKho_DB.LayThongTinCuonDay(idNhapKho));
                }
            }

            grvDSNhapKho.ClearSelection();
            if (grvDSNhapKho.Rows.Count > 0)
                grvDSNhapKho.FirstDisplayedScrollingRowIndex = 0;

            _editingRowIndex = -1;
            _editingIdNhapKho = 0;
            SetEditMode(false);
        }

        private int TimRowIndexTheoId(long idNhapKho)
        {
            foreach (DataGridViewRow row in grvDSNhapKho.Rows)
            {
                if (row.IsNewRow) continue;
                if (long.TryParse(row.Cells["id_NhapKho"].Value?.ToString(), out long currentId)
                    && currentId == idNhapKho)
                    return row.Index;
            }
            return -1;
        }

        private void LoadChiTietNhapKho(long idNhapKho, int rowIndex)
        {
            if (idNhapKho <= 0) return;

            try
            {
                DataTable dt = NhapKho_DB.LayTTNhapKhoTPTheoId(idNhapKho);
                if (dt == null || dt.Rows.Count == 0)
                {
                    FrmWaiting.ShowGifAlert("Dữ liệu nhập kho không còn tồn tại. Vui lòng tìm lại.");
                    RefreshCurrentFilter();
                    return;
                }

                DataRow header = dt.Rows[0];
                _editingIdNhapKho = idNhapKho;
                _editingRowIndex = rowIndex;
                _selectedTTThanhPhamID = long.TryParse(GetDbText(header, "TTThanhPham_ID"), out long tpId)
                    ? tpId
                    : (long?)null;

                // dtNgay và cbxMaBin chỉ là bộ lọc tìm kiếm, tuyệt đối không nạp dữ liệu phiếu vào hai control này.
                tbTenSP.Text = GetDbText(header, "tenSP");
                tbMaBin.Text = GetDbText(header, "maBin2");

                if (decimal.TryParse(GetDbText(header, "soMet"), NumberStyles.Any,
                    CultureInfo.InvariantCulture, out decimal soMetVal))
                    nbSoMet.Value = Math.Min(Math.Max(soMetVal, nbSoMet.Minimum), nbSoMet.Maximum);
                else
                    nbSoMet.Value = 0;

                tbNguoiLam.Text = GetDbText(header, "nguoiLam");
                tbNguoiLam.ReadOnly = true;
                rtbGhiChu.Text = GetDbText(header, "ghiChu");

                thongTinDayNhapKho = NhapKho_DB.LayThongTinCuonDay(idNhapKho)
                    ?? new List<ThongTinCuonDay>();
                LoadThongTinDayVaoGrid();
                _ttCuonDayChanged = false;

                SetEditMode(true);

                if (rowIndex >= 0 && rowIndex < grvDSNhapKho.Rows.Count)
                {
                    grvDSNhapKho.ClearSelection();
                    grvDSNhapKho.Rows[rowIndex].Selected = true;
                    grvDSNhapKho.FirstDisplayedScrollingRowIndex = rowIndex;
                }
            }
            catch (Exception ex)
            {
                ClearEditingDetails();
                MessageBox.Show($"Lỗi khi tải dữ liệu nhập kho:\n{ex.Message}",
                    "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static string TaoChuoiDSCuonLo(IEnumerable<ThongTinCuonDay> dsCuon)
        {
            if (dsCuon == null) return string.Empty;

            List<string> parts = new List<string>();
            foreach (ThongTinCuonDay item in dsCuon)
            {
                if (item == null) continue;

                if (item.TTLo_ID.HasValue)
                {
                    if (!item.SoDau.HasValue || !item.soCuoi.HasValue) continue;
                    string kichThuoc = (item.KichThuocLo ?? string.Empty).Trim();
                    parts.Add($"L{kichThuoc}: {item.SoDau.Value} -> {item.soCuoi.Value}");
                }
                else
                {
                    parts.Add($"{item.SoCuon}C x {item.TongChieuDai}");
                }
            }
            return string.Join(" + ", parts);
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (!UserContext.IsAuthenticated
                || (!UserContext.HasRole(RoleNames.Wh) && !UserContext.HasRole(RoleNames.Admin)))
            {
                FrmWaiting.ShowGifAlert("Bạn cần cấp quyền để thực hiện yêu cầu này.");
                return;
            }

            if (_editingIdNhapKho <= 0)
            {
                FrmWaiting.ShowGifAlert("Vui lòng chọn dòng cần xoá.");
                return;
            }

            if (MessageBox.Show(
                    "Bạn có chắc chắn muốn xoá lần nhập kho này?",
                    "Xác nhận xoá",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            try
            {
                // Giữ nguyên nghiệp vụ hiện tại: DB layer tự kiểm tra downstream,
                // xoá TTNhapKhoTP/TTCuonDay và hoàn trả ChieuDaiSau cho TTThanhPham.
                long idDaXoa = _editingIdNhapKho;
                NhapKho_DB.XoaMotDong(idDaXoa);

                int rowIndex = TimRowIndexTheoId(idDaXoa);
                if (rowIndex >= 0 && rowIndex < grvDSNhapKho.Rows.Count)
                    grvDSNhapKho.Rows.RemoveAt(rowIndex);

                ClearEditingDetails();
            }
            catch (InvalidOperationException ex)
            {
                FrmWaiting.ShowGifAlert(ex.Message);
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
                FrmWaiting.ShowGifAlert("Bạn cần cấp quyền để thực hiện yêu cầu này.");
                return;
            }

            if (_editingIdNhapKho <= 0)
            {
                MessageBox.Show("Không có lần nhập kho nào được chọn để sửa.",
                    "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                List<ThongTinCuonDay> dsCuon = thongTinDayNhapKho == null
                    ? new List<ThongTinCuonDay>()
                    : new List<ThongTinCuonDay>(thongTinDayNhapKho);

                // Theo phase hiện tại chỉ GhiChu và dữ liệu TTCuonDay được phép sửa.
                // Các giá trị Ngày/MaBin/TTThanhPham/Tên SP/Số mét/Người làm không lấy từ control tìm kiếm để ghi DB.
                NhapKho_Model model = new NhapKho_Model
                {
                    GhiChu = rtbGhiChu.Text.Trim()
                };

                NhapKho_DB.CapNhatNhapKho(
                    idNhapKho: _editingIdNhapKho,
                    ttThanhPhamIdCu: 0,
                    soMetCu: 0,
                    model: model,
                    dsCuon: dsCuon,
                    capNhatTTCuonDay: _ttCuonDayChanged
                );

                RefreshCurrentFilter();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi cập nhật:\n{ex.Message}",
                    "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void GrvDSNhapKho_RowClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= grvDSNhapKho.Rows.Count)
                return;

            DataGridViewRow row = grvDSNhapKho.Rows[e.RowIndex];
            if (!long.TryParse(row.Cells["id_NhapKho"].Value?.ToString(), out long idNhapKho)
                || idNhapKho <= 0)
                return;

            LoadChiTietNhapKho(idNhapKho, e.RowIndex);
        }

        private void SetEditMode(bool editMode)
        {
            btnSua.Enabled = editMode;
            btnDelete.Enabled = editMode;
        }

        private void ClearEditingDetails()
        {
            _editingRowIndex = -1;
            _editingIdNhapKho = 0;
            _selectedTTThanhPhamID = null;
            _ttCuonDayChanged = false;

            tbTenSP.Text = string.Empty;
            tbMaBin.Text = string.Empty;
            nbSoMet.Value = 0;
            tbNguoiLam.Text = string.Empty;
            tbNguoiLam.ReadOnly = true;
            rtbGhiChu.Text = string.Empty;

            thongTinDayNhapKho = new List<ThongTinCuonDay>();
            LoadThongTinDayVaoGrid();
            SetEditMode(false);
        }

        private void ClearGridAndEditingDetails()
        {
            grvDSNhapKho.Rows.Clear();
            ClearEditingDetails();
        }

        private void ResetForm()
        {
            _suppressFilterEvents = true;
            try
            {
                checkBox1.Checked = false;
                dtNgay.Enabled = false;
                _maBinSearchHelper?.Reset();
            }
            finally
            {
                _suppressFilterEvents = false;
            }

            ClearGridAndEditingDetails();
            cbxMaBin.Focus();
        }

        private void btnResetForm_Click(object sender, EventArgs e) => ResetForm();

        private void GrvDSNhapKho_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            GrvDSNhapKho_RowClick(sender, e);
        }

        private void UC_QcDuyetNhapKho_Load(object sender, EventArgs e)
        {
            _suppressFilterEvents = true;
            try
            {
                checkBox1.Checked = false;
                dtNgay.Enabled = false;
            }
            finally
            {
                _suppressFilterEvents = false;
            }

            tbNguoiLam.ReadOnly = true;
            ClearGridAndEditingDetails();
            cbxMaBin.Focus();
        }

        private void btnNhapKhoTheoNgay_Click(object sender, EventArgs e)
        {
            if (!UserContext.IsAuthenticated
                || (!UserContext.HasRole(RoleNames.Wh) && !UserContext.HasRole(RoleNames.Admin)))
            {
                FrmWaiting.ShowGifAlert("Bạn cần cấp quyền để thực hiện yêu cầu này.");
                return;
            }

            using (var frm = new Frm_NhapKhoTP_TheoNgay())
                frm.ShowDialog(this);
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

        }

        private void XuLyTTCuonCheDoSuaPhieuCu()
        {
            try
            {
                // Luồng D lấy đúng working-state hiện tại từ dataGridView1.
                // Không đọc lại TTCuonDay_CD và không ghi DB khi đóng Frm_DLCuon.
                if (!TryLayThongTinCuonDayHienTaiTuGrid(
                    out List<ThongTinCuonDay> duLieuHienTai,
                    out string gridError))
                {
                    FrmWaiting.ShowGifAlert(gridError);
                    return;
                }

                if (duLieuHienTai.Count == 0)
                {
                    FrmWaiting.ShowGifAlert("Không có cuộn/lô để chỉnh sửa.");
                    return;
                }

                LayDuLieuTTLoChoFrm(duLieuHienTai, out DataTable ttLoActive, out DataTable ttLoReferenced);

                using (Frm_DLCuon frm = new Frm_DLCuon(
                    duLieuHienTai,
                    FrmDLCuonMode.NhapKhoEdit,
                    null,
                    false,
                    ttLoActive,
                    ttLoReferenced))
                {
                    if (frm.ShowDialog() != DialogResult.OK)
                        return;

                    // Frm_DLCuon chỉ trả trạng thái cuối. DB chỉ được cập nhật khi người dùng bấm btnSua.
                    thongTinDayNhapKho = frm.ThongTinCuon ?? new List<ThongTinCuonDay>();
                    LoadThongTinDayVaoGrid();
                    _ttCuonDayChanged = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi chỉnh sửa thông tin cuộn/lô:\n{ex.Message}",
                    "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnTTCuon_Click(object sender, EventArgs e)
        {
            if (_editingIdNhapKho <= 0
                || !_selectedTTThanhPhamID.HasValue
                || _selectedTTThanhPhamID.Value <= 0)
            {
                FrmWaiting.ShowGifAlert("Vui lòng chọn một lần nhập kho cần sửa trước khi chỉnh sửa thông tin cuộn/lô.");
                return;
            }

            XuLyTTCuonCheDoSuaPhieuCu();
        }

        private static string GetDbText(DataRow row, string columnName)
        {
            if (!row.Table.Columns.Contains(columnName)) return string.Empty;
            if (row[columnName] == DBNull.Value) return string.Empty;
            return row[columnName]?.ToString() ?? string.Empty;
        }

    }
}
