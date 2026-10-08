using DG_TonKhoBTP_v02.Database.Kho.XuatKho;
using DG_TonKhoBTP_v02.Models;
using DG_TonKhoBTP_v02.Models.Kho.XuatKho;
using DG_TonKhoBTP_v02.UI.Helper;
using DG_TonKhoBTP_v02.UI.Helper.AutoSearchWithCombobox;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DG_TonKhoBTP_v02.UI.NghiepVuKhac.Kho.XuatKho
{
    public partial class UC_LapKeHoachCatDay : UserControl
    {
        private const string COL_TEN_SP = "colTenSP";
        private const string COL_TON_THUC_TE = "colTonThucTe";
        private const string COL_DAT_TRUOC = "colDatTruoc";
        private const string COL_CD_1C = "colChieuDai1Cuon";
        private const string COL_SO_DAU = "colSoDau";
        private const string COL_SO_CUOI = "colSoCuoi";
        private const string COL_SO_LUONG_LAY = "colSoLuongLay";
        private const string COL_CD_CAT = "colChieuDaiCat";
        private const string COL_CAT_LE = "colCatLe";
        private const string COL_TRANG_THAI = "colTrangThai";
        private const string COL_LUU = "colLuu";
        private const string COL_XOA = "colXoa";

        private static readonly Color MauLuuMoi = Color.FromArgb(40, 167, 69);
        private static readonly Color MauCapNhat = Color.FromArgb(0, 123, 255);
        private static readonly Color MauXoa = Color.FromArgb(220, 53, 69);
        private static readonly Color MauDisabled = Color.FromArgb(200, 200, 200);
        private static readonly Color MauDisabledText = Color.FromArgb(100, 100, 100);
        private static readonly Color MauHetTon = Color.MistyRose;

        private readonly Dictionary<string, LapKeHoachCatDay_GridRow> _rows =
            new Dictionary<string, LapKeHoachCatDay_GridRow>(StringComparer.OrdinalIgnoreCase);

        private ComboBoxSearchHelper _searchHelper;
        private LapKeHoachCatDay_SearchType _searchType = LapKeHoachCatDay_SearchType.ChieuDai;
        private long? _keHoachId;
        private bool _editMode;
        private bool _loadingPlan;
        private string _maKeHoachDaNap = string.Empty;

        private LapKeHoachCatDay_GridRow _rowDangEditChieuDai;
        private string _chuoiCatTruocEdit = string.Empty;
        private int _soLuongTruocEdit;

        private sealed class TrangThaiNhapTam
        {
            public int SoLuongCanLay { get; set; }
            public string ChuoiChieuDaiCat { get; set; } = string.Empty;
            public List<LapKeHoachCatDay_NhomCat> NhomCatPopup { get; set; } = new List<LapKeHoachCatDay_NhomCat>();
        }

        public UC_LapKeHoachCatDay()
        {
            InitializeComponent();
            KhoiTaoGrid();
            KhoiTaoTimKiem();
            GanSuKien();

            if (cbxKieuTimKiem.Items.Count > 0)
                cbxKieuTimKiem.SelectedIndex = 0;
        }

        private void KhoiTaoGrid()
        {
            grvKetQuaTimKiem.AutoGenerateColumns = false;
            grvKetQuaTimKiem.AllowUserToAddRows = false;
            grvKetQuaTimKiem.AllowUserToDeleteRows = false;
            grvKetQuaTimKiem.MultiSelect = false;
            grvKetQuaTimKiem.RowHeadersVisible = false;
            grvKetQuaTimKiem.SelectionMode = DataGridViewSelectionMode.CellSelect;
            grvKetQuaTimKiem.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            grvKetQuaTimKiem.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            grvKetQuaTimKiem.Columns.Clear();

            grvKetQuaTimKiem.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = COL_TEN_SP,
                HeaderText = "Tên SP",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = 160,
                ReadOnly = true
            });
            grvKetQuaTimKiem.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = COL_TON_THUC_TE,
                HeaderText = "Tồn thực tế",
                Width = 105,
                ReadOnly = true
            });
            grvKetQuaTimKiem.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = COL_DAT_TRUOC,
                HeaderText = "Đặt trước",
                Width = 100,
                ReadOnly = true
            });
            grvKetQuaTimKiem.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = COL_CD_1C,
                HeaderText = "Chiều dài 1 cuộn",
                Width = 125,
                ReadOnly = true
            });
            grvKetQuaTimKiem.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = COL_SO_DAU,
                HeaderText = "Số đầu",
                Width = 90,
                ReadOnly = true
            });
            grvKetQuaTimKiem.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = COL_SO_CUOI,
                HeaderText = "Số cuối",
                Width = 90,
                ReadOnly = true
            });
            grvKetQuaTimKiem.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = COL_SO_LUONG_LAY,
                HeaderText = "Số lượng lấy",
                Width = 100
            });
            grvKetQuaTimKiem.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = COL_CD_CAT,
                HeaderText = "Chiều dài cắt",
                Width = 180
            });
            grvKetQuaTimKiem.Columns.Add(new DataGridViewButtonColumn
            {
                Name = COL_CAT_LE,
                HeaderText = "Cắt lẻ",
                Width = 115,
                Text = "Cắt lẻ",
                UseColumnTextForButtonValue = false,
                FlatStyle = FlatStyle.Flat
            });
            grvKetQuaTimKiem.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = COL_TRANG_THAI,
                HeaderText = "Trạng thái",
                Width = 150,
                ReadOnly = true
            });
            grvKetQuaTimKiem.Columns.Add(new DataGridViewButtonColumn
            {
                Name = COL_LUU,
                HeaderText = "",
                Width = 105,
                UseColumnTextForButtonValue = false,
                FlatStyle = FlatStyle.Flat
            });
            grvKetQuaTimKiem.Columns.Add(new DataGridViewButtonColumn
            {
                Name = COL_XOA,
                HeaderText = "",
                Width = 85,
                UseColumnTextForButtonValue = false,
                FlatStyle = FlatStyle.Flat
            });
        }

        private void KhoiTaoTimKiem()
        {
            _searchHelper = new ComboBoxSearchHelper(
                cbxKey,
                (keyword, ct) => LapKeHoachCatDay_DB.TimGoiYAsync(_searchType, keyword, ct))
            {
                DisplayColumn = "GiaTri",
                SelectedTextBehavior = ComboBoxSelectedTextBehavior.FillDisplayText,
                DebounceMs = 350,
                CanSearch = keyword =>
                    _searchType == LapKeHoachCatDay_SearchType.Lot ||
                    _searchType == LapKeHoachCatDay_SearchType.TenSanPham
            };

            _searchHelper.ItemSelected += row =>
            {
                string value = Convert.ToString(row["GiaTri"], CultureInfo.InvariantCulture) ?? string.Empty;
                _ = TimKiemVaThemAsync(value, false);
            };

            Disposed += (s, e) => _searchHelper?.Dispose();
        }

        private void GanSuKien()
        {
            cbxKieuTimKiem.SelectedIndexChanged += CbxKieuTimKiem_SelectedIndexChanged;
            cbxKey.KeyDown += CbxKey_KeyDown;
            btnTimToanBo.Click += BtnTimToanBo_Click;

            tbLenhXuatHang.KeyDown += TbLenhXuatHang_KeyDown;
            tbLenhXuatHang.Leave += TbLenhXuatHang_Leave;

            grvKetQuaTimKiem.CellContentClick += GrvKetQuaTimKiem_CellContentClick;
            grvKetQuaTimKiem.CellBeginEdit += GrvKetQuaTimKiem_CellBeginEdit;
            grvKetQuaTimKiem.CellEndEdit += GrvKetQuaTimKiem_CellEndEdit;
        }

        private void CbxKieuTimKiem_SelectedIndexChanged(object sender, EventArgs e)
        {
            _searchType = cbxKieuTimKiem.SelectedIndex switch
            {
                0 => LapKeHoachCatDay_SearchType.ChieuDai,
                1 => LapKeHoachCatDay_SearchType.Lot,
                2 => LapKeHoachCatDay_SearchType.TenSanPham,
                3 => LapKeHoachCatDay_SearchType.KhachHang,
                _ => LapKeHoachCatDay_SearchType.ChieuDai
            };

            _searchHelper?.Reset();
            cbxKey.DropDownStyle = ComboBoxStyle.DropDown;
        }

        private async void CbxKey_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter)
                return;

            e.Handled = true;
            e.SuppressKeyPress = true;

            if (_searchType == LapKeHoachCatDay_SearchType.KhachHang)
            {
                FrmWaiting.ShowGifAlert("Chức năng tìm kiếm theo khách hàng chưa được phát triển trong giai đoạn này.");
                return;
            }

            if (_searchType == LapKeHoachCatDay_SearchType.ChieuDai)
            {
                if (!int.TryParse((cbxKey.Text ?? string.Empty).Trim(), out int cd) || cd <= 0)
                {
                    FrmWaiting.ShowGifAlert("Chiều dài tìm kiếm không hợp lệ");
                    return;
                }

                await TimKiemVaThemAsync(cbxKey.Text, false);
                return;
            }

            // Cho phép Enter trực tiếp nếu user đã nhập đúng giá trị nhưng không click item dropdown.
            await TimKiemVaThemAsync(cbxKey.Text, false);
        }

        private async void BtnTimToanBo_Click(object sender, EventArgs e)
        {
            await TimKiemVaThemAsync(string.Empty, true);
        }

        private async void TbLenhXuatHang_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter)
                return;

            e.Handled = true;
            e.SuppressKeyPress = true;
            await NapKeHoachTheoMaAsync();
        }

        private async void TbLenhXuatHang_Leave(object sender, EventArgs e)
        {
            await NapKeHoachTheoMaAsync();
        }

        private async Task NapKeHoachTheoMaAsync()
        {
            if (_loadingPlan)
                return;

            string ma = (tbLenhXuatHang.Text ?? string.Empty).Trim();
            if (string.Equals(ma, _maKeHoachDaNap, StringComparison.OrdinalIgnoreCase))
                return;

            if (string.IsNullOrWhiteSpace(ma))
            {
                ChuyenSangTaoMoi(clearHeader: false);
                return;
            }

            _loadingPlan = true;
            try
            {
                LapKeHoachCatDay_KeHoachContext context = await WaitingHelper.RunWithWaiting(
                    () => Task.Run(() => LapKeHoachCatDay_DB.LayKeHoachTheoMa(ma)),
                    "ĐANG TẢI KẾ HOẠCH...");

                _rows.Clear();
                grvKetQuaTimKiem.Rows.Clear();
                _maKeHoachDaNap = ma;

                if (!context.TonTai)
                {
                    _editMode = false;
                    _keHoachId = null;
                    cbxNguoiNhan.Text = string.Empty;
                    tbGhiChu.Clear();
                    return;
                }

                _editMode = true;
                _keHoachId = context.Header.Id;
                tbLenhXuatHang.Text = context.Header.MaKeHoach;
                cbxNguoiNhan.Text = context.Header.NguoiNhan;
                tbGhiChu.Text = context.Header.GhiChu;

                bool keHoachDangHoatDong = string.Equals(
                    context.Header.TrangThai, "ACTIVE", StringComparison.OrdinalIgnoreCase);
                foreach (var row in context.Rows)
                {
                    if (!keHoachDangHoatDong)
                    {
                        row.CanEdit = false;
                        row.CanDelete = false;
                    }
                    ThemHoacCapNhatDongGrid(row, false);
                }
            }
            catch (Exception)
            {
                FrmWaiting.ShowGifAlert("Không thể tải thông tin kế hoạch. Vui lòng thử lại.");
            }
            finally
            {
                _loadingPlan = false;
            }
        }

        private async Task TimKiemVaThemAsync(string searchValue, bool layToanBo)
        {
            if (_searchType == LapKeHoachCatDay_SearchType.KhachHang && !layToanBo)
            {
                FrmWaiting.ShowGifAlert("Chức năng tìm kiếm theo khách hàng chưa được phát triển trong giai đoạn này.");
                return;
            }

            LapKeHoachCatDay_SearchCriteria criteria;
            if (layToanBo)
            {
                criteria = new LapKeHoachCatDay_SearchCriteria
                {
                    SearchType = LapKeHoachCatDay_SearchType.TatCa,
                    KeHoach_IDDangSua = _keHoachId
                };
            }
            else if (_searchType == LapKeHoachCatDay_SearchType.ChieuDai)
            {
                if (!int.TryParse((searchValue ?? string.Empty).Trim(), out int cd) || cd <= 0)
                {
                    FrmWaiting.ShowGifAlert("Chiều dài tìm kiếm không hợp lệ");
                    return;
                }

                criteria = new LapKeHoachCatDay_SearchCriteria
                {
                    SearchType = _searchType,
                    SearchValue = searchValue,
                    ChieuDaiToiThieu = cd,
                    KeHoach_IDDangSua = _keHoachId
                };
            }
            else
            {
                if (string.IsNullOrWhiteSpace(searchValue))
                {
                    FrmWaiting.ShowGifAlert("Không tìm thấy dữ liệu phù hợp với điều kiện tìm kiếm.");
                    return;
                }

                criteria = new LapKeHoachCatDay_SearchCriteria
                {
                    SearchType = _searchType,
                    SearchValue = searchValue.Trim(),
                    KeHoach_IDDangSua = _keHoachId
                };
            }

            try
            {
                List<LapKeHoachCatDay_GridRow> found = await WaitingHelper.RunWithWaiting(
                    () => Task.Run(() => LapKeHoachCatDay_DB.TimKiem(criteria)),
                    "ĐANG TÌM KIẾM...");

                if (found == null || found.Count == 0)
                {
                    if (!layToanBo && _searchType == LapKeHoachCatDay_SearchType.Lot)
                        FrmWaiting.ShowGifAlert("Không tìm thấy LOT/Mã bin phù hợp.");
                    else
                        FrmWaiting.ShowGifAlert("Không tìm thấy dữ liệu phù hợp với điều kiện tìm kiếm.");
                    return;
                }

                bool coTrung = false;
                DataGridViewRow duplicateRow = null;
                foreach (var row in found)
                {
                    if (_rows.TryGetValue(row.RowKey, out _))
                    {
                        coTrung = true;
                        duplicateRow ??= TimGridRowTheoKey(row.RowKey);
                        continue;
                    }

                    ThemHoacCapNhatDongGrid(row, false);
                }

                if (coTrung)
                {
                    if (duplicateRow != null)
                    {
                        grvKetQuaTimKiem.ClearSelection();
                        duplicateRow.Selected = true;
                        grvKetQuaTimKiem.FirstDisplayedScrollingRowIndex = Math.Max(0, duplicateRow.Index);
                    }
                    FrmWaiting.ShowGifAlert("Dữ liệu này đã có trong danh sách.");
                }
            }
            catch (Exception)
            {
                FrmWaiting.ShowGifAlert("Không thể tải dữ liệu tìm kiếm.");
            }
        }

        private void ThemHoacCapNhatDongGrid(LapKeHoachCatDay_GridRow model, bool replaceExisting)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.RowKey))
                return;

            if (_rows.TryGetValue(model.RowKey, out _))
            {
                if (!replaceExisting)
                    return;
                DataGridViewRow oldRow = TimGridRowTheoKey(model.RowKey);
                if (oldRow != null)
                    grvKetQuaTimKiem.Rows.Remove(oldRow);
            }

            _rows[model.RowKey] = model;

            int index = grvKetQuaTimKiem.Rows.Add();
            DataGridViewRow row = grvKetQuaTimKiem.Rows[index];
            row.Tag = model;
            GanGiaTriDong(row, model);
            ApDungTrangThaiDong(row, model);
        }

        private void GanGiaTriDong(DataGridViewRow row, LapKeHoachCatDay_GridRow model)
        {
            string tenHienThi = model.TenSP;
            if (!string.IsNullOrWhiteSpace(model.MaNguon))
                tenHienThi += " - " + model.MaNguon;

            row.Cells[COL_TEN_SP].Value = tenHienThi;
            row.Cells[COL_TON_THUC_TE].Value = model.LoaiDong == LapKeHoachCatDay_LoaiDong.CuonChan
                ? model.TonThucTe + " cuộn"
                : model.TonThucTe + " m";
            row.Cells[COL_DAT_TRUOC].Value = model.LoaiDong == LapKeHoachCatDay_LoaiDong.CuonChan
                ? model.DatTruoc + " cuộn"
                : model.DatTruoc + " m";
            row.Cells[COL_CD_1C].Value = model.ChieuDai1Cuon;
            row.Cells[COL_SO_DAU].Value = model.SoDau.HasValue ? (object)model.SoDau.Value : string.Empty;
            row.Cells[COL_SO_CUOI].Value = model.SoCuoi.HasValue ? (object)model.SoCuoi.Value : string.Empty;
            row.Cells[COL_SO_LUONG_LAY].Value = model.SoLuongCanLay > 0 ? (object)model.SoLuongCanLay : string.Empty;
            row.Cells[COL_CD_CAT].Value = model.TaoChuoiHienThiChieuDaiCat();
            row.Cells[COL_TRANG_THAI].Value = model.TrangThai;

            row.Cells[COL_CAT_LE].Value = model.NhomCatPopup.Count > 0
                ? "Cắt lẻ (" + model.NhomCatPopup.Count + " cuộn)"
                : "Cắt lẻ";
            row.Cells[COL_LUU].Value = model.DaTonTaiTrongDB ? "Cập nhật" : "Lưu mới";
            row.Cells[COL_XOA].Value = "Xóa";
        }

        private void ApDungTrangThaiDong(DataGridViewRow row, LapKeHoachCatDay_GridRow model)
        {
            bool isFull = model.LoaiDong == LapKeHoachCatDay_LoaiDong.CuonChan;
            bool directCutEmpty = string.IsNullOrWhiteSpace(model.ChuoiChieuDaiCat);

            row.Cells[COL_SO_LUONG_LAY].ReadOnly = !isFull || !model.CanEdit;
            row.Cells[COL_CD_CAT].ReadOnly = !model.CanEdit;

            bool catLeEnabled = isFull && model.CanEdit && directCutEmpty;
            StyleButtonCell(row.Cells[COL_CAT_LE], catLeEnabled, Color.FromArgb(255, 193, 7), Color.Black);

            bool saveEnabled = model.CanEdit;
            StyleButtonCell(
                row.Cells[COL_LUU],
                saveEnabled,
                model.DaTonTaiTrongDB ? MauCapNhat : MauLuuMoi,
                Color.White);

            bool deleteEnabled = model.DaTonTaiTrongDB && model.CanDelete;
            StyleButtonCell(row.Cells[COL_XOA], deleteEnabled, MauXoa, Color.White);

            if (string.Equals(model.TrangThai, LapKeHoachCatDay_TrangThai.HetTon, StringComparison.Ordinal))
                row.DefaultCellStyle.BackColor = MauHetTon;
            else
                row.DefaultCellStyle.BackColor = Color.White;

            if (model.HoanThanh)
            {
                row.Cells[COL_SO_LUONG_LAY].ReadOnly = true;
                row.Cells[COL_CD_CAT].ReadOnly = true;
                StyleButtonCell(row.Cells[COL_CAT_LE], false, MauDisabled, MauDisabledText);
                StyleButtonCell(row.Cells[COL_LUU], false, MauDisabled, MauDisabledText);
                StyleButtonCell(row.Cells[COL_XOA], false, MauDisabled, MauDisabledText);
            }
        }

        private static void StyleButtonCell(DataGridViewCell cell, bool enabled, Color enabledBackColor, Color enabledForeColor)
        {
            cell.Style.BackColor = enabled ? enabledBackColor : MauDisabled;
            cell.Style.ForeColor = enabled ? enabledForeColor : MauDisabledText;
            cell.Style.SelectionBackColor = cell.Style.BackColor;
            cell.Style.SelectionForeColor = cell.Style.ForeColor;
            cell.ReadOnly = !enabled;
        }

        private void GrvKetQuaTimKiem_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        {
            if (e.RowIndex < 0)
                return;

            if (!(grvKetQuaTimKiem.Rows[e.RowIndex].Tag is LapKeHoachCatDay_GridRow model))
            {
                e.Cancel = true;
                return;
            }

            if (!model.CanEdit)
            {
                e.Cancel = true;
                return;
            }

            string col = grvKetQuaTimKiem.Columns[e.ColumnIndex].Name;
            if (col == COL_SO_LUONG_LAY)
            {
                if (model.LoaiDong != LapKeHoachCatDay_LoaiDong.CuonChan)
                {
                    e.Cancel = true;
                    return;
                }
                _soLuongTruocEdit = model.SoLuongCanLay;
            }
            else if (col == COL_CD_CAT)
            {
                _rowDangEditChieuDai = model;
                _chuoiCatTruocEdit = model.ChuoiChieuDaiCat;
                grvKetQuaTimKiem.Rows[e.RowIndex].Cells[COL_CD_CAT].Value = model.ChuoiChieuDaiCat;
            }
        }

        private void GrvKetQuaTimKiem_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0)
                return;

            DataGridViewRow gridRow = grvKetQuaTimKiem.Rows[e.RowIndex];
            if (!(gridRow.Tag is LapKeHoachCatDay_GridRow model))
                return;

            string col = grvKetQuaTimKiem.Columns[e.ColumnIndex].Name;
            if (col == COL_SO_LUONG_LAY)
            {
                string raw = Convert.ToString(gridRow.Cells[COL_SO_LUONG_LAY].Value, CultureInfo.InvariantCulture) ?? string.Empty;
                if (string.IsNullOrWhiteSpace(raw))
                {
                    model.SoLuongCanLay = 0;
                }
                else if (!int.TryParse(raw.Trim(), out int qty) || qty <= 0)
                {
                    FrmWaiting.ShowGifAlert("Số lượng lấy không hợp lệ");
                    model.SoLuongCanLay = _soLuongTruocEdit;
                }
                else if (qty < model.SoLuongDaLay)
                {
                    FrmWaiting.ShowGifAlert("Nội dung này đã được thực hiện và không thể chỉnh sửa.");
                    model.SoLuongCanLay = _soLuongTruocEdit;
                }
                else
                {
                    int soNhom = model.NhomCatPopup.Count > 0
                        ? model.NhomCatPopup.Count
                        : (string.IsNullOrWhiteSpace(model.ChuoiChieuDaiCat) ? 0 : 1);
                    int remainingWhole = Math.Max(0, qty - model.SoLuongDaLay);
                    if (remainingWhole + soNhom > 0 && model.TonThucTe <= 0)
                    {
                        FrmWaiting.ShowGifAlert("Đã hết tồn kho");
                        model.SoLuongCanLay = _soLuongTruocEdit;
                    }
                    else if (remainingWhole + soNhom > model.TonKhaDung)
                    {
                        FrmWaiting.ShowGifAlert("Số cuộn khả dụng không đủ");
                        model.SoLuongCanLay = _soLuongTruocEdit;
                    }
                    else
                    {
                        model.SoLuongCanLay = qty;
                    }
                }

                gridRow.Cells[COL_SO_LUONG_LAY].Value = model.SoLuongCanLay > 0 ? (object)model.SoLuongCanLay : string.Empty;
                return;
            }

            if (col == COL_CD_CAT)
            {
                string raw = Convert.ToString(gridRow.Cells[COL_CD_CAT].Value, CultureInfo.InvariantCulture) ?? string.Empty;

                if (model.LoaiDong == LapKeHoachCatDay_LoaiDong.CuonChan &&
                    model.NhomCatPopup.Count > 0 &&
                    !string.IsNullOrWhiteSpace(raw))
                {
                    if (model.NhomCatPopup.Any(x => x.CoThucHien))
                    {
                        FrmWaiting.ShowGifAlert("Sửa thất bại do đã tiến hành cắt.");
                        gridRow.Cells[COL_CD_CAT].Value = model.TaoChuoiHienThiChieuDaiCat();
                        return;
                    }

                    DialogResult answer = MessageBox.Show(
                        model.NhomCatPopup.Count + " cuộn đang được cắt lẻ, tiếp tục sẽ chuyển về cắt 1 cuộn.",
                        "Xác nhận",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);
                    if (answer != DialogResult.Yes)
                    {
                        gridRow.Cells[COL_CD_CAT].Value = model.TaoChuoiHienThiChieuDaiCat();
                        return;
                    }

                    model.NhomCatPopup.Clear();
                    gridRow.Cells[COL_CAT_LE].Value = "Cắt lẻ";
                }

                if (string.IsNullOrWhiteSpace(raw))
                {
                    model.ChuoiChieuDaiCat = string.Empty;
                    gridRow.Cells[COL_CD_CAT].Value = model.TaoChuoiHienThiChieuDaiCat();
                    ApDungTrangThaiDong(gridRow, model);
                    _rowDangEditChieuDai = null;
                    return;
                }

                if (!LapKeHoachCatDay_ChieuDaiParser.TryNormalize(
                    raw,
                    out string normalized,
                    out List<int> values,
                    out string invalid,
                    out bool syntaxError))
                {
                    FrmWaiting.ShowGifAlert(LapKeHoachCatDay_ChieuDaiParser.TaoThongBaoLoi(invalid, syntaxError));
                    model.ChuoiChieuDaiCat = _chuoiCatTruocEdit;
                }
                else if (model.LoaiDong == LapKeHoachCatDay_LoaiDong.CuonChan)
                {
                    if (values.Count == 0)
                    {
                        FrmWaiting.ShowGifAlert("Không xác định được chiều dài cần cắt. Vui lòng kiểm tra lại dữ liệu đã nhập.");
                        model.ChuoiChieuDaiCat = _chuoiCatTruocEdit;
                    }
                    else if (values.Sum() > model.ChieuDai1Cuon)
                    {
                        FrmWaiting.ShowGifAlert("Tổng chiều dài các đoạn cắt không hợp lệ");
                        model.ChuoiChieuDaiCat = _chuoiCatTruocEdit;
                    }
                    else
                    {
                        int remainingWhole = Math.Max(0, model.SoLuongCanLay - model.SoLuongDaLay);
                        if (model.TonThucTe <= 0)
                        {
                            FrmWaiting.ShowGifAlert("Đã hết tồn kho");
                            model.ChuoiChieuDaiCat = _chuoiCatTruocEdit;
                        }
                        else if (remainingWhole + 1 > model.TonKhaDung)
                        {
                            FrmWaiting.ShowGifAlert("Số cuộn khả dụng không đủ");
                            model.ChuoiChieuDaiCat = _chuoiCatTruocEdit;
                        }
                        else
                        {
                            model.ChuoiChieuDaiCat = normalized;
                        }
                    }
                }
                else
                {
                    int tong = values.Sum();
                    if (tong > 0 && model.TonThucTe <= 0)
                    {
                        FrmWaiting.ShowGifAlert("Đã hết tồn kho");
                        model.ChuoiChieuDaiCat = _chuoiCatTruocEdit;
                    }
                    else if (tong > model.TonKhaDung)
                    {
                        FrmWaiting.ShowGifAlert("Chiều dài khả dụng của cuộn " + model.MaNguon + " không đủ");
                        model.ChuoiChieuDaiCat = _chuoiCatTruocEdit;
                    }
                    else
                    {
                        model.ChuoiChieuDaiCat = normalized;
                    }
                }

                gridRow.Cells[COL_CD_CAT].Value = model.TaoChuoiHienThiChieuDaiCat();
                ApDungTrangThaiDong(gridRow, model);
                _rowDangEditChieuDai = null;
            }
        }

        private async void GrvKetQuaTimKiem_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0)
                return;

            DataGridViewRow gridRow = grvKetQuaTimKiem.Rows[e.RowIndex];
            if (!(gridRow.Tag is LapKeHoachCatDay_GridRow model))
                return;

            string col = grvKetQuaTimKiem.Columns[e.ColumnIndex].Name;
            if (col == COL_CAT_LE)
            {
                MoPopupCatLe(gridRow, model);
            }
            else if (col == COL_LUU)
            {
                await LuuHoacCapNhatDongAsync(gridRow, model);
            }
            else if (col == COL_XOA)
            {
                await XoaDongAsync(model);
            }
        }

        private void MoPopupCatLe(DataGridViewRow gridRow, LapKeHoachCatDay_GridRow model)
        {
            if (model.LoaiDong != LapKeHoachCatDay_LoaiDong.CuonChan || !model.CanEdit)
                return;
            if (!string.IsNullOrWhiteSpace(model.ChuoiChieuDaiCat))
                return;

            using var frm = new Frm_CatLeCuonChan(
                model.TenSP,
                model.ChieuDai1Cuon,
                model.TonThucTe,
                model.DatTruoc,
                model.SoLuongCanLay,
                model.SoLuongDaLay,
                model.NhomCatPopup);

            if (frm.ShowDialog(FindForm()) != DialogResult.OK)
                return;

            List<LapKeHoachCatDay_NhomCat> ketQua = frm.KetQua.Select(x => x.Clone()).ToList();
            if (ketQua.Count == 1)
            {
                // Theo quy ước đã chốt: popup chỉ còn 1 cuộn thì chuyển về nhập trực tiếp.
                model.ChuoiChieuDaiCat = string.Join(";", ketQua[0].ChiTiet
                    .Where(x => !x.DaThucHien)
                    .Select(x => x.ChieuDai.ToString(CultureInfo.InvariantCulture)));
                model.NhomCatPopup.Clear();
            }
            else
            {
                model.ChuoiChieuDaiCat = string.Empty;
                model.NhomCatPopup = ketQua;
            }

            gridRow.Cells[COL_CD_CAT].Value = model.TaoChuoiHienThiChieuDaiCat();
            gridRow.Cells[COL_CAT_LE].Value = model.NhomCatPopup.Count > 0
                ? "Cắt lẻ (" + model.NhomCatPopup.Count + " cuộn)"
                : "Cắt lẻ";
            ApDungTrangThaiDong(gridRow, model);
        }

        private async Task LuuHoacCapNhatDongAsync(DataGridViewRow gridRow, LapKeHoachCatDay_GridRow model)
        {
            if (!model.CanEdit)
                return;

            grvKetQuaTimKiem.EndEdit();

            string ma = (tbLenhXuatHang.Text ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(ma))
            {
                FrmWaiting.ShowGifAlert("Vui lòng nhập mã kế hoạch trước khi lưu.");
                return;
            }

            if (model.DaTonTaiTrongDB && DongKhongConNoiDungChuaThucHien(model))
            {
                await XoaDongAsync(model);
                return;
            }

            if (!KiemTraDuLieuDongTruocKhiLuu(model))
                return;

            var request = new LapKeHoachCatDay_SaveRequest
            {
                KeHoach_IDDuKien = _keHoachId,
                MaKeHoach = ma,
                NguoiNhan = cbxNguoiNhan.Text ?? string.Empty,
                GhiChu = tbGhiChu.Text ?? string.Empty,
                NguoiTao = UserContext.UserName ?? string.Empty,
                Row = model
            };

            bool wasPersisted = model.DaTonTaiTrongDB;
            bool coThucHienTruocKhiLuu = model.CoThucHienB3;
            try
            {
                LapKeHoachCatDay_SaveResult result = await WaitingHelper.RunWithWaiting(
                    () => Task.Run(() => LapKeHoachCatDay_DB.LuuDong(request)),
                    wasPersisted ? "ĐANG CẬP NHẬT..." : "ĐANG LƯU KẾ HOẠCH...");

                if (!result.ThanhCong)
                {
                    FrmWaiting.ShowGifAlert(string.IsNullOrWhiteSpace(result.Loi) ? "Không thể lưu kế hoạch. Dữ liệu chưa được thay đổi." : result.Loi);
                    return;
                }

                if (result.TaoMoiKeHoach)
                {
                    FrmWaiting.ShowGifAlert("Kế hoạch " + ma + " lưu thành công.");
                    ResetVeTrangThaiMoi();
                    return;
                }

                if (wasPersisted)
                {
                    FrmWaiting.ShowGifAlert(coThucHienTruocKhiLuu
                        ? "Cập nhật thành công"
                        : "Đã cập nhật nội dung kế hoạch.");
                }
                else
                {
                    FrmWaiting.ShowGifAlert("Kế hoạch " + ma + " đã được cập nhật");
                }

                try
                {
                    await LamMoiGridSauThaoTacAsync(ma, model.RowKey);
                }
                catch (Exception)
                {
                    FrmWaiting.ShowGifAlert("Dữ liệu đã cũ, cần reload để cập nhật");
                }
            }
            catch (Exception)
            {
                string msg = wasPersisted
                    ? "Không thể cập nhật kế hoạch. Dữ liệu chưa được thay đổi."
                    : "Không thể lưu kế hoạch. Dữ liệu chưa được thay đổi.";
                FrmWaiting.ShowGifAlert(msg);
            }
        }


        private async Task LamMoiGridSauThaoTacAsync(string maKeHoach, string rowKeyVuaLuu)
        {
            // Bảo toàn mọi dữ liệu người dùng đang nhập ở các dòng KHÁC dòng vừa lưu,
            // kể cả dòng đã tồn tại trong DB nhưng đang được sửa mà chưa bấm Cập nhật.
            var trangThaiNhap = _rows.Values
                .Where(x => !string.Equals(x.RowKey, rowKeyVuaLuu, StringComparison.OrdinalIgnoreCase))
                .ToDictionary(
                    x => x.RowKey,
                    x => new TrangThaiNhapTam
                    {
                        SoLuongCanLay = x.SoLuongCanLay,
                        ChuoiChieuDaiCat = x.ChuoiChieuDaiCat ?? string.Empty,
                        NhomCatPopup = (x.NhomCatPopup ?? new List<LapKeHoachCatDay_NhomCat>())
                            .Select(g => g.Clone())
                            .ToList()
                    },
                    StringComparer.OrdinalIgnoreCase);

            List<LapKeHoachCatDay_GridRow> dongChuaLuu = _rows.Values
                .Where(x => !x.DaTonTaiTrongDB &&
                            !string.Equals(x.RowKey, rowKeyVuaLuu, StringComparison.OrdinalIgnoreCase))
                .ToList();

            LapKeHoachCatDay_KeHoachContext context = await WaitingHelper.RunWithWaiting(
                () => Task.Run(() =>
                {
                    LapKeHoachCatDay_KeHoachContext loaded = LapKeHoachCatDay_DB.LayKeHoachTheoMa(maKeHoach);
                    if (loaded.TonTai)
                        LapKeHoachCatDay_DB.LamMoiTonChoCacDong(dongChuaLuu, loaded.Header.Id);
                    return loaded;
                }),
                "ĐANG CẬP NHẬT DỮ LIỆU...");

            if (!context.TonTai)
            {
                ChuyenSangTaoMoi(clearHeader: false);
                return;
            }

            _rows.Clear();
            grvKetQuaTimKiem.Rows.Clear();
            _editMode = true;
            _keHoachId = context.Header.Id;
            _maKeHoachDaNap = context.Header.MaKeHoach;
            tbLenhXuatHang.Text = context.Header.MaKeHoach;
            cbxNguoiNhan.Text = context.Header.NguoiNhan;
            tbGhiChu.Text = context.Header.GhiChu;

            bool keHoachDangHoatDong = string.Equals(
                context.Header.TrangThai, "ACTIVE", StringComparison.OrdinalIgnoreCase);

            foreach (LapKeHoachCatDay_GridRow row in context.Rows)
            {
                if (trangThaiNhap.TryGetValue(row.RowKey, out TrangThaiNhapTam state))
                {
                    // Giữ nguyên nội dung người dùng đang nhập, kể cả khi refresh mới làm
                    // nguồn hết khả dụng. Trạng thái/CanEdit mới vẫn được giữ để khóa thao tác.
                    row.SoLuongCanLay = state.SoLuongCanLay;
                    row.ChuoiChieuDaiCat = state.ChuoiChieuDaiCat;
                    row.NhomCatPopup = state.NhomCatPopup.Select(g => g.Clone()).ToList();
                }

                if (!keHoachDangHoatDong)
                {
                    row.CanEdit = false;
                    row.CanDelete = false;
                }
                ThemHoacCapNhatDongGrid(row, false);
            }

            foreach (LapKeHoachCatDay_GridRow row in dongChuaLuu)
            {
                if (_rows.ContainsKey(row.RowKey))
                    continue;

                if (!keHoachDangHoatDong)
                {
                    row.CanEdit = false;
                    row.CanDelete = false;
                }
                ThemHoacCapNhatDongGrid(row, false);
            }
        }


        private static bool DongKhongConNoiDungChuaThucHien(LapKeHoachCatDay_GridRow model)
        {
            if (model.LoaiDong == LapKeHoachCatDay_LoaiDong.CuonChan)
            {
                return model.SoLuongCanLay == 0
                    && model.SoLuongDaLay == 0
                    && string.IsNullOrWhiteSpace(model.ChuoiChieuDaiCat)
                    && (model.NhomCatPopup == null || model.NhomCatPopup.Count == 0);
            }

            return string.IsNullOrWhiteSpace(model.ChuoiChieuDaiCat)
                && model.LayCacDoanDaThucHien().Count == 0;
        }

        private bool KiemTraDuLieuDongTruocKhiLuu(LapKeHoachCatDay_GridRow model)
        {
            if (model.LoaiDong == LapKeHoachCatDay_LoaiDong.CuonChan)
            {
                int remainingWhole = Math.Max(0, model.SoLuongCanLay - model.SoLuongDaLay);
                int groups = model.NhomCatPopup.Count > 0
                    ? model.NhomCatPopup.Count
                    : (string.IsNullOrWhiteSpace(model.ChuoiChieuDaiCat) ? 0 : 1);

                if (remainingWhole <= 0 && groups <= 0 && model.SoLuongDaLay <= 0)
                {
                    FrmWaiting.ShowGifAlert("Kiểm tra lại dữ liệu.");
                    return false;
                }

                if (remainingWhole + groups > 0 && model.TonThucTe <= 0)
                {
                    FrmWaiting.ShowGifAlert("Đã hết tồn kho");
                    return false;
                }

                if (remainingWhole + groups > model.TonKhaDung)
                {
                    FrmWaiting.ShowGifAlert("Số cuộn khả dụng không đủ");
                    return false;
                }
            }
            else
            {
                if (!LapKeHoachCatDay_ChieuDaiParser.TryNormalize(
                    model.ChuoiChieuDaiCat,
                    out string normalized,
                    out List<int> values,
                    out string invalid,
                    out bool syntaxError))
                {
                    FrmWaiting.ShowGifAlert(LapKeHoachCatDay_ChieuDaiParser.TaoThongBaoLoi(invalid, syntaxError));
                    return false;
                }

                model.ChuoiChieuDaiCat = normalized;
                int executed = model.LayCacDoanDaThucHien().Count;
                if (values.Count == 0 && executed == 0)
                {
                    FrmWaiting.ShowGifAlert("Kiểm tra lại dữ liệu.");
                    return false;
                }

                if (values.Sum() > 0 && model.TonThucTe <= 0)
                {
                    FrmWaiting.ShowGifAlert("Đã hết tồn kho");
                    return false;
                }

                if (values.Sum() > model.TonKhaDung)
                {
                    FrmWaiting.ShowGifAlert("Chiều dài khả dụng của cuộn " + model.MaNguon + " không đủ");
                    return false;
                }
            }

            return true;
        }

        private async Task XoaDongAsync(LapKeHoachCatDay_GridRow model)
        {
            if (!_keHoachId.HasValue || !model.DaTonTaiTrongDB || !model.CanDelete)
                return;

            var request = new LapKeHoachCatDay_DeleteRequest
            {
                KeHoach_ID = _keHoachId.Value,
                Row = model
            };

            try
            {
                LapKeHoachCatDay_DeletePreview preview = await WaitingHelper.RunWithWaiting(
                    () => Task.Run(() => LapKeHoachCatDay_DB.KiemTraXoaDong(request)),
                    "ĐANG KIỂM TRA...");

                if (!preview.CoTheXoa)
                {
                    FrmWaiting.ShowGifAlert(string.IsNullOrWhiteSpace(preview.Loi)
                        ? "Không thể xóa vì nội dung này đã có dữ liệu thực hiện."
                        : preview.Loi);
                    return;
                }

                string confirm;
                if (preview.SeXoaCaKeHoach)
                    confirm = "Nếu tiếp tục mã kế hoạch " + tbLenhXuatHang.Text.Trim() + " sẽ xoá khỏi hệ thống.";
                else if (preview.CoThucHienMotPhan)
                    confirm = "Mã kế hoạch " + tbLenhXuatHang.Text.Trim() + " đang thực hiện, nếu tiếp tục chỉ xoá các phần chưa thực hiện.";
                else
                    confirm = "Bạn muốn loại bỏ dòng này khỏi kế hoạch?";

                DialogResult answer = MessageBox.Show(confirm, "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (answer != DialogResult.Yes)
                    return;

                LapKeHoachCatDay_DeleteResult result = await WaitingHelper.RunWithWaiting(
                    () => Task.Run(() => LapKeHoachCatDay_DB.XoaDong(request)),
                    "ĐANG XÓA...");

                if (!result.ThanhCong)
                {
                    FrmWaiting.ShowGifAlert(string.IsNullOrWhiteSpace(result.Loi)
                        ? "Không thể xóa dữ liệu. Dữ liệu hiện tại được giữ nguyên."
                        : result.Loi);
                    return;
                }

                string maKeHoach = tbLenhXuatHang.Text.Trim();
                if (result.DaXoaKeHoach)
                {
                    FrmWaiting.ShowGifAlert("Đã xóa kế hoạch " + maKeHoach + ".");
                    ResetVeTrangThaiMoi();
                    return;
                }

                FrmWaiting.ShowGifAlert("Xoá thành công");
                try
                {
                    await LamMoiGridSauThaoTacAsync(maKeHoach, null);
                }
                catch (Exception)
                {
                    FrmWaiting.ShowGifAlert("Dữ liệu đã cũ, cần reload để cập nhật");
                }
            }
            catch (Exception)
            {
                FrmWaiting.ShowGifAlert("Không thể xóa dữ liệu. Dữ liệu hiện tại được giữ nguyên.");
            }
        }

        private DataGridViewRow TimGridRowTheoKey(string rowKey)
        {
            foreach (DataGridViewRow row in grvKetQuaTimKiem.Rows)
            {
                if (row.Tag is LapKeHoachCatDay_GridRow model &&
                    string.Equals(model.RowKey, rowKey, StringComparison.OrdinalIgnoreCase))
                    return row;
            }
            return null;
        }

        private void ChuyenSangTaoMoi(bool clearHeader)
        {
            _editMode = false;
            _keHoachId = null;
            _maKeHoachDaNap = string.Empty;
            _rows.Clear();
            grvKetQuaTimKiem.Rows.Clear();

            if (clearHeader)
            {
                tbLenhXuatHang.Clear();
                cbxNguoiNhan.Text = string.Empty;
                tbGhiChu.Clear();
            }
        }

        private void ResetVeTrangThaiMoi()
        {
            ChuyenSangTaoMoi(clearHeader: true);
            _searchHelper?.Reset();
            cbxKey.Text = string.Empty;
            if (cbxKieuTimKiem.Items.Count > 0)
                cbxKieuTimKiem.SelectedIndex = 0;
            tbLenhXuatHang.Focus();
        }
    }
}
