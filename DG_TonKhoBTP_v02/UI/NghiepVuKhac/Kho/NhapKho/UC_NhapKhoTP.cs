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

        private List<ThongTinCuonDay> thongTinDayNhapKho = new List<ThongTinCuonDay>();
        private bool _ttCuonDayChanged = false;

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
            // Việc chỉnh sửa vẫn thực hiện qua Frm_DLCuon.
            dataGridView1.ReadOnly = true;

            grvDSNhapKho.CellDoubleClick += GrvDSNhapKho_CellDoubleClick;

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

                if (!TryGetGridSourceId(row, out long sourceId))
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

                int? soDau = int.TryParse(row.Cells["col_SoDau"].Value?.ToString(), out int parsedSoDau)
                    ? parsedSoDau
                    : (int?)null;
                int? soCuoi = int.TryParse(row.Cells["col_SoCuoi"].Value?.ToString(), out int parsedSoCuoi)
                    ? parsedSoCuoi
                    : (int?)null;

                result.Add(new ThongTinCuonDay
                {
                    TTCuonDay_ID = GetGridTTCuonDayId(row),
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
            cbxMaBin.Text = row["MaBin"]?.ToString() ?? string.Empty;
            tbTenSP.Text = row["Ten"]?.ToString() ?? string.Empty;
            tbMaBin.Text = row["MaBin"]?.ToString() ?? string.Empty;

            // Mỗi lần btnNhapKho là một lần nhập mới độc lập.
            // Ghi chú của lần nhập không kế thừa từ TTThanhPham hay TTNhapKhoTP cũ.
            rtbGhiChu.Text = string.Empty;
            tbNguoiLam.ReadOnly = false;

            _selectedTTThanhPhamID = long.TryParse(row["TTThanhPham_ID"]?.ToString(), out long id)
                            ? id : (long?)null;

            if (decimal.TryParse(row["ChieuDaiSau"]?.ToString(), out decimal soMet))
                nbSoMet.Value = Math.Min(soMet, nbSoMet.Maximum);
            else
                nbSoMet.Value = 0;

            // Không tải/preload TTNhapKhoTP cũ. Chỉ lấy dữ liệu cuộn/lô còn lại của MaBin.
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

            if (string.IsNullOrWhiteSpace(tbNguoiLam.Text))
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
            if (dt == null || dt.Rows.Count == 0) return;

            DataRow dr = dt.Rows[0];
            DataGridViewRow target = null;
            foreach (DataGridViewRow row in grvDSNhapKho.Rows)
            {
                if (row.IsNewRow) continue;
                if (long.TryParse(row.Cells["id_NhapKho"].Value?.ToString(), out long existingId) && existingId == headerId)
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
            target.Cells["nguoiLam"].Value = GetDbText(dr, "nguoiLam");
            target.Cells["tenSP"].Value = GetDbText(dr, "tenSP");
            target.Cells["soMet"].Value = GetDbText(dr, "soMet");
            target.Cells["maBin2"].Value = GetDbText(dr, "maBin2");
            target.Cells["ghiChu"].Value = GetDbText(dr, "ghiChu");
            target.Cells["thongSo"].Value = TaoChuoiDSCuonLo(NhapKho_DB.LayThongTinCuonDay(headerId));

            grvDSNhapKho.ClearSelection();
            target.Selected = true;
            if (target.Index >= 0) grvDSNhapKho.FirstDisplayedScrollingRowIndex = target.Index;
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
                FrmWaiting.ShowGifAlert($"Bạn cần cấp quyền để thực hiện yêu cầu này.");
                return;
            }

            // 1 row grvDSNhapKho = 1 TTNhapKhoTP.
            // Xoá là xoá cả lần nhập cùng các TTCuonDay trực thuộc.
            if (_editingRowIndex < 0 || _editingIdNhapKho <= 0)
            {
                FrmWaiting.ShowGifAlert("Vui lòng chọn dòng cần xoá.");
                return;
            }

            if (MessageBox.Show(
                    "Bạn có chắc chắn muốn xoá lần nhập kho này?",
                    "Xác nhận xoá",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning) != DialogResult.Yes)
            {
                return;
            }

            try
            {
                // DB layer tự đọc lại dữ liệu hiện tại, kiểm tra downstream và thực hiện transaction.
                // Không dùng các snapshot TTThanhPham_ID / soMet trên UI để quyết định xoá.
                NhapKho_DB.XoaMotDong(_editingIdNhapKho);

                // Chỉ cập nhật UI sau khi transaction DB đã COMMIT thành công.
                grvDSNhapKho.Rows.RemoveAt(_editingRowIndex);
                ResetForm();
            }
            catch (InvalidOperationException ex)
            {
                // Bao gồm rule nghiệp vụ:
                // "Không thể xoá do đã có lịch sử cắt dây/xuất kho."
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
            row.Cells["nguoiLam"].Value = tbNguoiLam.Text.Trim();
            row.Cells["TTThanhPham_ID"].Value = _selectedTTThanhPhamID?.ToString() ?? string.Empty;
            row.Cells["tenSP"].Value = tbTenSP.Text.Trim();
            row.Cells["maBin2"].Value = tbMaBin.Text.Trim();
            row.Cells["soMet"].Value = nbSoMet.Value.ToString(CultureInfo.InvariantCulture);
            row.Cells["ghiChu"].Value = rtbGhiChu.Text.Trim();
            row.Cells["thongSo"].Value = TaoChuoiDSCuonLo(thongTinDayNhapKho);
        }


        private void ThemDongVaoGrid(long idNhapKho, NhapKho_Model model)
        {
            int rowIndex = grvDSNhapKho.Rows.Add();
            DataGridViewRow row = grvDSNhapKho.Rows[rowIndex];
            row.Height = 35;
            row.Cells["id_NhapKho"].Value = idNhapKho.ToString();
            WriteRowFromForm(row);
            grvDSNhapKho.FirstDisplayedScrollingRowIndex = grvDSNhapKho.RowCount - 1;
        }



        private void GrvDSNhapKho_RowClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

            DataGridViewRow row = grvDSNhapKho.Rows[e.RowIndex];
            _editingIdNhapKho = long.TryParse(row.Cells["id_NhapKho"].Value?.ToString(), out long nkId) ? nkId : 0;
            if (_editingIdNhapKho <= 0) return;

            DataTable dt = NhapKho_DB.LayTTNhapKhoTPTheoId(_editingIdNhapKho);
            if (dt == null || dt.Rows.Count == 0) return;
            DataRow header = dt.Rows[0];

            _selectedTTThanhPhamID = long.TryParse(GetDbText(header, "TTThanhPham_ID"), out long tpId) ? tpId : (long?)null;

            string ngayText = GetDbText(header, "ngay");
            string[] dateFormats = { "dd/MM/yyyy", "d/M/yyyy", "yyyy-MM-dd", "dd-MM-yyyy", "d-M-yyyy" };
            if (DateTime.TryParseExact(ngayText, dateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime ngayVal)
                || DateTime.TryParse(ngayText, out ngayVal))
                dtNgay.Value = ngayVal;

            if (decimal.TryParse(GetDbText(header, "soBB"), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal soBBVal))
                nbSoBB.Value = Math.Min(Math.Max(soBBVal, nbSoBB.Minimum), nbSoBB.Maximum);
            else
                nbSoBB.Value = 0;

            tbNguoiLam.Text = GetDbText(header, "nguoiLam");
            tbTenSP.Text = GetDbText(header, "tenSP");
            tbMaBin.Text = GetDbText(header, "maBin2");

            if (decimal.TryParse(GetDbText(header, "soMet"), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal soMetVal))
                nbSoMet.Value = Math.Min(Math.Max(soMetVal, nbSoMet.Minimum), nbSoMet.Maximum);
            else
                nbSoMet.Value = 0;

            rtbGhiChu.Text = GetDbText(header, "ghiChu");

            try
            {
                thongTinDayNhapKho = NhapKho_DB.LayThongTinCuonDay(_editingIdNhapKho);
                LoadThongTinDayVaoGrid();
                _ttCuonDayChanged = false;
            }
            catch (Exception ex)
            {
                thongTinDayNhapKho = new List<ThongTinCuonDay>();
                LoadThongTinDayVaoGrid();
                _ttCuonDayChanged = false;
                FrmWaiting.ShowGifAlert($"Lỗi khi tải thông tin cuộn/dây:\n{ex.Message}");
            }

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
            if (grvDSNhapKho.DataSource != null) grvDSNhapKho.DataSource = null;
            grvDSNhapKho.Rows.Clear();

            HashSet<long> daThem = new HashSet<long>();
            foreach (DataRow dr in dt.Rows)
            {
                if (!long.TryParse(GetDbText(dr, "id_NhapKho"), out long idNhapKho) || !daThem.Add(idNhapKho))
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
        }

        private void ResetForm(bool resetAll = false)
        {
            _maBinSearchHelper.Reset();
            _selectedTTThanhPhamID = null;
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
            if (!_selectedTTThanhPhamID.HasValue || _selectedTTThanhPhamID.Value <= 0)
            {
                FrmWaiting.ShowGifAlert("Vui lòng chọn MaBin trước khi chỉnh sửa thông tin cuộn/lô nhập kho.");
                return;
            }

            // Luồng D: sửa phiếu cũ dùng mode NhapKhoEdit riêng; không tác động các luồng tạo mới.
            if (_editingIdNhapKho > 0)
            {
                XuLyTTCuonCheDoSuaPhieuCu();
                return;
            }

            try
            {
                // Nguồn hiển thị của Frm_DLCuon là chính dữ liệu hiện đang có trên dataGridView1,
                // không tải lại toàn bộ TTCuonDay_CD và không sửa TTCuonDay_CD.
                if (!TryLayThongTinCuonDayHienTaiTuGrid(out List<ThongTinCuonDay> duLieuHienTai, out string gridError))
                {
                    FrmWaiting.ShowGifAlert(gridError);
                    return;
                }

                if (duLieuHienTai.Count == 0)
                {
                    FrmWaiting.ShowGifAlert("Không có cuộn/lô còn lại để chỉnh số lượng nhập kho.");
                    return;
                }

                // Đọc lại số lượng còn lại thực tế chỉ để làm giới hạn validate.
                // Không dùng dữ liệu này để thay thế nội dung đang hiển thị trên grid.
                List<ThongTinCuonDay> duLieuConLaiTrongDb =
                    NhapKho_DB.LayTTCuonDayConLaiTheoTTThanhPhamId(_selectedTTThanhPhamID.Value)
                    ?? new List<ThongTinCuonDay>();

                Dictionary<long, int> soCuonToiDaTheoNguon = duLieuConLaiTrongDb
                    .Where(x => x != null && x.TTCuonDay_CD_ID.HasValue && x.TTCuonDay_CD_ID.Value > 0)
                    .GroupBy(x => x.TTCuonDay_CD_ID.Value)
                    .ToDictionary(g => g.Key, g => g.First().SoCuon);

                bool coSourceDaThayDoi = duLieuHienTai.Any(x =>
                    x == null
                    || !x.TTCuonDay_CD_ID.HasValue
                    || !soCuonToiDaTheoNguon.ContainsKey(x.TTCuonDay_CD_ID.Value));

                if (coSourceDaThayDoi)
                {
                    FrmWaiting.ShowGifAlert(
                        "Dữ liệu cuộn/lô còn lại đã thay đổi trong database. " +
                        "Vui lòng chọn lại MaBin để tải dữ liệu mới trước khi chỉnh sửa.");
                    return;
                }

                LayDuLieuTTLoChoFrm(duLieuHienTai, out DataTable ttLoActive, out DataTable ttLoReferenced);

                using (Frm_DLCuon frm = new Frm_DLCuon(
                    duLieuHienTai,
                    FrmDLCuonMode.NhapKho,
                    soCuonToiDaTheoNguon,
                    false,
                    ttLoActive,
                    ttLoReferenced))
                {
                    if (frm.ShowDialog() != DialogResult.OK)
                        return;

                    // Chỉ cập nhật dữ liệu tạm trên UI. Không INSERT/UPDATE/DELETE TTCuonDay_CD.
                    thongTinDayNhapKho = frm.ThongTinCuon ?? new List<ThongTinCuonDay>();
                    LoadThongTinDayVaoGrid();
                    _ttCuonDayChanged = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi chỉnh sửa thông tin cuộn/lô nhập kho:\n{ex.Message}",
                    "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static string GetDbText(DataRow row, string columnName)
        {
            if (!row.Table.Columns.Contains(columnName)) return string.Empty;
            if (row[columnName] == DBNull.Value) return string.Empty;
            return row[columnName]?.ToString() ?? string.Empty;
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


        private void btnImportExcel_Click(object sender, EventArgs e)
        {

        }

    }
}