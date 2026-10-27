using DG_TonKhoBTP_v02.Database.Kho.XuatKho;
using DG_TonKhoBTP_v02.Models;
using DG_TonKhoBTP_v02.Models.Kho.XuatKho;
using DG_TonKhoBTP_v02.UI.Helper;
using DG_TonKhoBTP_v02.UI.Helper.AutoSearchWithCombobox;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DG_TonKhoBTP_v02.UI.NghiepVuKhac.Kho.XuatKho
{
    public partial class UC_LapKeHoachCatDay : UserControl
    {
        private const int BUTTON_RADIUS = 6;

        private const string COL_TEN_SP = "colTenSP";
        private const string COL_MA_NGUON = "colMaNguon";
        private const string COL_TON_THUC_TE = "colTonThucTe";
        private const string COL_DAT_TRUOC = "colDatTruoc";
        private const string COL_TON_CON_LAI = "colTonConLai";
        private const string COL_CD_1C = "colChieuDai1Cuon";
        private const string COL_SO_DAU = "colSoDau";
        private const string COL_SO_CUOI = "colSoCuoi";
        private const string COL_SO_LUONG_LAY = "colSoLuongLay";
        private const string COL_CD_CAT = "colChieuDaiCat";
        private const string COL_CAT_LE = "colCatLe";
        private const string COL_TRANG_THAI = "colTrangThai";
        private const string COL_CHI_TIET = "colChiTiet";
        private const string COL_XOA = "colXoa";

        private static readonly Color MauXoa = Color.FromArgb(220, 53, 69);
        private static readonly Color MauDisabled = Color.FromArgb(200, 200, 200);
        private static readonly Color MauDisabledText = Color.FromArgb(100, 100, 100);
        private static readonly Color MauHetTon = Color.MistyRose;

        private readonly Dictionary<string, LapKeHoachCatDay_GridRow> _rows =
            new Dictionary<string, LapKeHoachCatDay_GridRow>(StringComparer.OrdinalIgnoreCase);

        private ComboBoxSearchHelper _tenSanPhamSearchHelper;
        private ComboBoxSearchHelper _lotSearchHelper;
        private ComboBoxSearchHelper _nguoiCatSearchHelper;
        private string _nguoiCatGoc = string.Empty;
        private long? _keHoachId;
        private bool _editMode;
        private bool _loadingPlan;
        private string _maKeHoachDaNap = string.Empty;
        private readonly Dictionary<string, string> _dongGoc =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, LapKeHoachCatDay_GridRow> _choXoa =
            new Dictionary<string, LapKeHoachCatDay_GridRow>(StringComparer.OrdinalIgnoreCase);
        private string _nguoiNhanGoc = string.Empty;
        private string _ghiChuGoc = string.Empty;
        private bool _dangLuu;
        private bool _dangXemChiTiet;
        private long? _executionRevision;
        private readonly HashSet<string> _executionDraftReview = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private LapKeHoachCatDay_GridRow _rowDangEditChieuDai;
        private string _chuoiCatTruocEdit = string.Empty;
        private int _soLuongTruocEdit;

        private sealed class TrangThaiNhapTam
        {
            public int SoLuongCanLay { get; set; }
            public string ChuoiChieuDaiCat { get; set; } = string.Empty;
            public List<LapKeHoachCatDay_NhomCat> NhomCatPopup { get; set; } = new List<LapKeHoachCatDay_NhomCat>();
        }

        private sealed class ButtonAppearance
        {
            public Color BackColor { get; set; }
            public Color ForeColor { get; set; }
        }

        public UC_LapKeHoachCatDay()
        {
            InitializeComponent();
            KhoiTaoGrid();
            KhoiTaoTimKiem();
            GanSuKien();

        }

        private static GraphicsPath CreateRoundedButtonPath(Rectangle bounds)
        {
            var path = new GraphicsPath();
            if (bounds.Width <= 0 || bounds.Height <= 0)
                return path;

            int diameter = Math.Min(BUTTON_RADIUS * 2, Math.Min(bounds.Width, bounds.Height));
            if (diameter < 2)
            {
                path.AddRectangle(bounds);
                return path;
            }

            path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
            path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }

        private void KhoiTaoGrid()
        {
            // ==============================
            // 1. CẤU HÌNH FONT CHỮ
            // ==============================

            grvKetQuaTimKiem.ColumnHeadersDefaultCellStyle.Font =
                new Font("Tahoma", 10F, FontStyle.Regular);

            grvKetQuaTimKiem.DefaultCellStyle.Font =
                new Font("Tahoma", 10F, FontStyle.Regular);

            // ==============================
            // 2. CHIỀU CAO HEADER
            // ==============================

            grvKetQuaTimKiem.ColumnHeadersHeightSizeMode =
                DataGridViewColumnHeadersHeightSizeMode.DisableResizing;

            grvKetQuaTimKiem.ColumnHeadersHeight = 32;

            // ==============================
            // 3. CẤU HÌNH GRID
            // ==============================

            grvKetQuaTimKiem.AutoGenerateColumns = false;
            grvKetQuaTimKiem.AllowUserToAddRows = false;
            grvKetQuaTimKiem.AllowUserToDeleteRows = false;
            grvKetQuaTimKiem.MultiSelect = false;
            grvKetQuaTimKiem.RowHeadersVisible = false;

            grvKetQuaTimKiem.SelectionMode =
                DataGridViewSelectionMode.CellSelect;

            // ==============================
            // 4. CHIỀU CAO DÒNG DỮ LIỆU
            // ==============================

            // Tự tăng chiều cao nếu nội dung dài
            grvKetQuaTimKiem.AutoSizeRowsMode =
                DataGridViewAutoSizeRowsMode.AllCells;

            // Chiều cao tối thiểu 40 px
            grvKetQuaTimKiem.RowTemplate.MinimumHeight = 40;

            // Cho phép nội dung xuống dòng
            grvKetQuaTimKiem.DefaultCellStyle.WrapMode =
                DataGridViewTriState.True;

            // ==============================
            // 5. KHỞI TẠO CÁC CỘT
            // ==============================

            grvKetQuaTimKiem.Columns.Clear();

            // Tên sản phẩm
            grvKetQuaTimKiem.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = COL_TEN_SP,
                HeaderText = "Tên SP",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = 160,
                ReadOnly = true
            });

            // Mã nguồn chỉ hiển thị cho cuộn lẻ
            grvKetQuaTimKiem.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = COL_MA_NGUON,
                HeaderText = "Lot cuộn lẻ",
                Width = 175,
                ReadOnly = true
            });

            // Chiều dài một cuộn
            grvKetQuaTimKiem.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = COL_CD_1C,
                HeaderText = "1C đơn vị",
                Width = 125,
                ReadOnly = true
            });

            // Tồn thực tế
            grvKetQuaTimKiem.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = COL_TON_THUC_TE,
                HeaderText = "Tồn thực tế",
                Width = 105,
                ReadOnly = true
            });

            // Đặt trước
            grvKetQuaTimKiem.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = COL_DAT_TRUOC,
                HeaderText = "Đặt trước",
                Width = 100,
                ReadOnly = true
            });

            // Tồn còn lại
            grvKetQuaTimKiem.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = COL_TON_CON_LAI,
                HeaderText = "Tồn còn lại",
                Width = 105,
                ReadOnly = true
            });

            // Số đầu
            grvKetQuaTimKiem.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = COL_SO_DAU,
                HeaderText = "Số đầu",
                Width = 90,
                ReadOnly = true
            });

            // Số cuối
            grvKetQuaTimKiem.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = COL_SO_CUOI,
                HeaderText = "Số cuối",
                Width = 90,
                ReadOnly = true
            });

            // Số lượng lấy
            grvKetQuaTimKiem.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = COL_SO_LUONG_LAY,
                HeaderText = "SL cuộn lấy",
                Width = 100
            });

            // Chiều dài cắt
            grvKetQuaTimKiem.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = COL_CD_CAT,
                HeaderText = "CD cắt",
                Width = 100
            });

            // Nút cắt lẻ
            grvKetQuaTimKiem.Columns.Add(new DataGridViewButtonColumn
            {
                Name = COL_CAT_LE,
                HeaderText = "Cắt lẻ",
                Width = 115,
                Text = "Cắt lẻ",
                UseColumnTextForButtonValue = false,
                FlatStyle = FlatStyle.Flat
            });

            // Trạng thái
            grvKetQuaTimKiem.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = COL_TRANG_THAI,
                HeaderText = "Trạng thái",
                Width = 150,
                ReadOnly = true
            });

            // Xem chi tiet tinh trang da luu, khong hien toan bo lich su tren bang chinh.
            grvKetQuaTimKiem.Columns.Add(new DataGridViewButtonColumn
            {
                Name = COL_CHI_TIET,
                HeaderText = "Chi tiết",
                Width = 100,
                UseColumnTextForButtonValue = false,
                FlatStyle = FlatStyle.Flat
            });

            // Nút xóa
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
            // Hai helper rieng biet, khong thay doi ComboBoxSearchHelper dung chung.
            // Chon goi y chi dien gia tri; KHONG tu tim kiem.
            cbxTimTenSP.DropDownStyle = ComboBoxStyle.DropDown;
            cbxTimLot.DropDownStyle = ComboBoxStyle.DropDown;
            cbxNguoiCat.DropDownStyle = ComboBoxStyle.DropDown;

            _nguoiCatSearchHelper = new ComboBoxSearchHelper(
                cbxNguoiCat, LapKeHoachCatDay_DB.TimGoiYNguoiCatAsync)
            {
                DisplayColumn = "username",
                SelectedTextBehavior = ComboBoxSelectedTextBehavior.FillDisplayText,
                DebounceMs = 350
            };

            _tenSanPhamSearchHelper = new ComboBoxSearchHelper(
                cbxTimTenSP,
                (keyword, ct) => LapKeHoachCatDay_DB.TimGoiYAsync(
                    LapKeHoachCatDay_SearchType.TenSanPham, keyword, ct))
            {
                DisplayColumn = "GiaTri",
                SelectedTextBehavior = ComboBoxSelectedTextBehavior.FillDisplayText,
                DebounceMs = 350
            };

            _lotSearchHelper = new ComboBoxSearchHelper(
                cbxTimLot,
                (keyword, ct) => LapKeHoachCatDay_DB.TimGoiYAsync(
                    LapKeHoachCatDay_SearchType.Lot, keyword, ct))
            {
                DisplayColumn = "GiaTri",
                SelectedTextBehavior = ComboBoxSelectedTextBehavior.FillDisplayText,
                DebounceMs = 350
            };

            Disposed += (s, e) =>
            {
                _tenSanPhamSearchHelper?.Dispose();
                _lotSearchHelper?.Dispose();
                _nguoiCatSearchHelper?.Dispose();
            };
        }

        private void GanSuKien()
        {
            button1.Click += BtnTimKiem_Click;
            btnTimToanBo.Click += BtnTimToanBo_Click;
            btnLuuKeHoach.Click += btnLuuKeHoach_Click;

            tbLenhXuatHang.KeyDown += TbLenhXuatHang_KeyDown;
            tbLenhXuatHang.Leave += TbLenhXuatHang_Leave;

            grvKetQuaTimKiem.CellPainting += GrvKetQuaTimKiem_CellPainting;
            grvKetQuaTimKiem.CellContentClick += GrvKetQuaTimKiem_CellContentClick;
            grvKetQuaTimKiem.CellBeginEdit += GrvKetQuaTimKiem_CellBeginEdit;
            grvKetQuaTimKiem.CellEndEdit += GrvKetQuaTimKiem_CellEndEdit;
        }

        private async void BtnTimKiem_Click(object sender, EventArgs e)
        {
            await TimKiemVaThemAsync(false);
        }

        private async void BtnTimToanBo_Click(object sender, EventArgs e)
        {
            await TimKiemVaThemAsync(true);
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

            // Chua tai ma nao: gan ma cho ban nhap moi, khong xoa cac dong dang nhap.
            if (!_keHoachId.HasValue && string.IsNullOrWhiteSpace(_maKeHoachDaNap)
                && CoThayDoiChuaLuu() && !string.IsNullOrWhiteSpace(ma))
            {
                _maKeHoachDaNap = ma;
                return;
            }

            if (CoThayDoiChuaLuu())
            {
                DialogResult answer = MessageBox.Show(
                    "Dữ liệu kế hoạch đang có thay đổi chưa lưu. Tiếp tục sẽ bỏ các thay đổi này. Bạn có muốn chuyển kế hoạch?",
                    "Thay đổi chưa lưu", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (answer != DialogResult.Yes)
                {
                    tbLenhXuatHang.Text = _maKeHoachDaNap;
                    return;
                }
            }

            if (string.IsNullOrWhiteSpace(ma))
            {
                ChuyenSangTaoMoi(clearHeader: false);
                return;
            }

            _loadingPlan = true;
            try
            {
                LapKeHoachCatDay_KeHoachContext context = await WaitingHelper.RunWithWaiting(
                    () => Task.Run(() => LapKeHoachCatDay_DB.LayKeHoachTheoMaVoiPhienBan(ma)),
                    "ĐANG TẢI KẾ HOẠCH...");

                _rows.Clear();
                grvKetQuaTimKiem.Rows.Clear();
                _choXoa.Clear();
                _dongGoc.Clear();
                _executionRevision = context.ExecutionRevision;
                _executionDraftReview.Clear();
                _maKeHoachDaNap = ma;

                _nguoiCatSearchHelper.Reset();
                if (!context.TonTai)
                {
                    _editMode = false;
                    _keHoachId = null;
                    cbxNguoiNhan.Text = string.Empty;
                    tbGhiChu.Clear();
                    GhiNhanBanGoc();
                    return;
                }

                _editMode = true;
                _keHoachId = context.Header.Id;
                tbLenhXuatHang.Text = context.Header.MaKeHoach;
                cbxNguoiNhan.Text = context.Header.NguoiNhan;
                cbxNguoiCat.Text = context.Header.NguoiCat;
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
                GhiNhanBanGoc();
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

        private async Task TimKiemVaThemAsync(bool layToanBo)
        {
            LapKeHoachCatDay_SearchCriteria criteria;
            if (layToanBo)
            {
                // Giu nguyen nghiep vu tim toan bo: bo qua cac o dieu kien.
                criteria = new LapKeHoachCatDay_SearchCriteria
                {
                    SearchType = LapKeHoachCatDay_SearchType.TatCa,
                    KeHoach_IDDangSua = _keHoachId
                };
            }
            else
            {
                string ten = (cbxTimTenSP.Text ?? string.Empty).Trim();
                string lot = (cbxTimLot.Text ?? string.Empty).Trim();
                // cbxTenKhachHang chua su dung: bat ke gia tri nao deu KHONG loc.
                if (nbrTimSoLuong.Value > int.MaxValue)
                {
                    FrmWaiting.ShowGifAlert("Chiều dài tìm kiếm vượt quá giới hạn cho phép.");
                    return;
                }

                int minLength = decimal.ToInt32(nbrTimSoLuong.Value);
                if (ten.Length == 0 && lot.Length == 0 && minLength == 0)
                {
                    FrmWaiting.ShowGifAlert("Vui lòng nhập điều kiện hoặc chọn Tìm toàn bộ.");
                    return;
                }

                criteria = new LapKeHoachCatDay_SearchCriteria
                {
                    SearchType = LapKeHoachCatDay_SearchType.KetHop,
                    TenSanPham = ten,
                    Lot = lot,
                    ChieuDaiToiThieu = minLength > 0 ? (int?)minLength : null,
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
                    if (!layToanBo && !string.IsNullOrWhiteSpace(criteria.Lot))
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
            catch (InvalidOperationException ex)
            {
                FrmWaiting.ShowGifAlert(ex.Message);
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

            // Tim lai dong da danh dau cho xoa se hoan tac lenh xoa cho, giu identity DB/B3.
            if (_choXoa.TryGetValue(model.RowKey, out LapKeHoachCatDay_GridRow recovered))
            {
                model = recovered;
                _choXoa.Remove(model.RowKey);
                _executionDraftReview.Remove(model.RowKey);
            }

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

        // Khi dang xem/sua ke hoach da luu, chi hien so cuon cat le tren nut.
        private string TaoNhanNutCatLe(LapKeHoachCatDay_GridRow model)
        {
            int soCuon = model.NhomCatPopup.Count;
            if (soCuon <= 0)
                return "Cắt lẻ";

            return _editMode
                ? soCuon + " cuộn"
                : "Cắt lẻ (" + soCuon + " cuộn)";
        }

        private void GanGiaTriDong(DataGridViewRow row, LapKeHoachCatDay_GridRow model)
        {
            row.Cells[COL_TEN_SP].Value = model.TenSP;
            row.Cells[COL_MA_NGUON].Value = model.LoaiDong == LapKeHoachCatDay_LoaiDong.CuonLe
                ? model.MaNguon
                : (model.NhomCatNguonDaChuyenLe.Count > 0
                    ? "Đã chuyển lẻ (" + model.NhomCatNguonDaChuyenLe.Count + " nhóm)"
                    : string.Empty);
            row.Cells[COL_TON_THUC_TE].Value = model.LoaiDong == LapKeHoachCatDay_LoaiDong.CuonChan
                ? model.TonThucTe + " cuộn"
                : model.TonThucTe + " m";
            row.Cells[COL_DAT_TRUOC].Value = model.LoaiDong == LapKeHoachCatDay_LoaiDong.CuonChan
                ? model.DatTruoc + " cuộn"
                : model.DatTruoc + " m";
            row.Cells[COL_CD_1C].Value = model.ChieuDai1Cuon;
            row.Cells[COL_TON_CON_LAI].Value = (model.TonThucTe - model.DatTruoc)
                + (model.LoaiDong == LapKeHoachCatDay_LoaiDong.CuonChan ? " cuộn" : " m");
            row.Cells[COL_SO_DAU].Value = model.SoDau.HasValue ? (object)model.SoDau.Value : string.Empty;
            row.Cells[COL_SO_CUOI].Value = model.SoCuoi.HasValue ? (object)model.SoCuoi.Value : string.Empty;
            row.Cells[COL_SO_LUONG_LAY].Value = model.SoLuongCanLay > 0 ? (object)model.SoLuongCanLay : string.Empty;
            row.Cells[COL_CD_CAT].Value = model.ChuoiChieuDaiCat;
            row.Cells[COL_TRANG_THAI].Value = model.CanhBaoHetTon
                ? model.TrangThai + " | Hết tồn"
                : model.TrangThai;
            row.Cells[COL_TRANG_THAI].ToolTipText = model.CanhBaoHetTon
                ? "Tiến độ: " + model.TrangThai + ". Cảnh báo: Hết tồn kho."
                : model.TrangThai;

            row.Cells[COL_CHI_TIET].Value = model.DaTonTaiTrongDB ? "Chi tiết" : "—";
            row.Cells[COL_CAT_LE].Value = TaoNhanNutCatLe(model);
            row.Cells[COL_XOA].Value = "Xóa";
        }

        private void ApDungTrangThaiDong( DataGridViewRow row, LapKeHoachCatDay_GridRow model)
        {
            ToMauTrangThai(row.Cells[COL_TRANG_THAI], model.TrangThai);
            bool isFull = model.LoaiDong == LapKeHoachCatDay_LoaiDong.CuonChan;
            bool directCutEmpty = string.IsNullOrWhiteSpace(model.ChuoiChieuDaiCat);

            // 1. Phân quyền nhập liệu
            row.Cells[COL_SO_LUONG_LAY].ReadOnly = !isFull || !model.CanEdit;
            row.Cells[COL_CD_CAT].ReadOnly = !model.CanEdit;

            // 2. Tô màu xám ô "Số lượng lấy" đối với cuộn lẻ
            // Giữ màu xám ngay cả khi người dùng chọn ô
            row.Cells[COL_SO_LUONG_LAY].Style.BackColor =
                isFull ? Color.Empty : MauDisabled;

            row.Cells[COL_SO_LUONG_LAY].Style.SelectionBackColor =
                isFull ? Color.Empty : MauDisabled;

            row.Cells[COL_SO_LUONG_LAY].Style.SelectionForeColor =
                isFull ? Color.Empty : Color.Black;

            // 3. Nút Cắt lẻ
            bool catLeEnabled = isFull && model.CanEdit && directCutEmpty;

            StyleButtonCell(
                row.Cells[COL_CAT_LE],
                catLeEnabled,
                Color.FromArgb(255, 193, 7),
                Color.Black);

            // Chi xem khi dong da luu va co khoa DB.
            StyleButtonCell(row.Cells[COL_CHI_TIET],
                model.DaTonTaiTrongDB && _keHoachId.HasValue && model.KeHoachHang_ID.HasValue,
                Color.FromArgb(45, 107, 180), Color.White);

            // 4. Nút Xóa
            bool deleteEnabled = !model.DaTonTaiTrongDB ||
                (model.CanEdit && model.CanDelete);

            StyleButtonCell(
                row.Cells[COL_XOA],
                deleteEnabled,
                MauXoa,
                Color.White);

            // 5. Màu nền dòng theo tình trạng tồn kho
            if (model.CanhBaoHetTon ||
                string.Equals(
                    model.TrangThai,
                    LapKeHoachCatDay_TrangThai.HetTon,
                    StringComparison.Ordinal))
            {
                row.DefaultCellStyle.BackColor = MauHetTon;
            }
            else
            {
                row.DefaultCellStyle.BackColor = Color.White;
            }

            // 6. Khóa thao tác khi đã hoàn thành
            if (model.HoanThanh && !_executionDraftReview.Contains(model.RowKey))
            {
                row.Cells[COL_SO_LUONG_LAY].ReadOnly = true;
                row.Cells[COL_CD_CAT].ReadOnly = true;

                StyleButtonCell(
                    row.Cells[COL_CAT_LE],
                    false,
                    MauDisabled,
                    MauDisabledText);

                StyleButtonCell(
                    row.Cells[COL_XOA],
                    false,
                    MauDisabled,
                    MauDisabledText);
            }
        }

        private static void ToMauTrangThai(DataGridViewCell cell, string trangThai)
        {
            Color backColor;
            Color foreColor;
            switch (trangThai)
            {
                case LapKeHoachCatDay_TrangThai.ChuaThucHien:
                    backColor = Color.FromArgb(255, 237, 213);
                    foreColor = Color.FromArgb(156, 64, 0);
                    break;
                case LapKeHoachCatDay_TrangThai.DangThucHien:
                    backColor = Color.FromArgb(219, 234, 254);
                    foreColor = Color.FromArgb(0, 76, 163);
                    break;
                case LapKeHoachCatDay_TrangThai.HoanThanh:
                case LapKeHoachCatDay_TrangThai.DaThucHien:
                    backColor = Color.FromArgb(220, 252, 231);
                    foreColor = Color.FromArgb(0, 100, 47);
                    break;
                case LapKeHoachCatDay_TrangThai.HetTon:
                    backColor = Color.FromArgb(254, 226, 226);
                    foreColor = Color.FromArgb(166, 24, 34);
                    break;
                default:
                    backColor = Color.FromArgb(233, 236, 239);
                    foreColor = Color.FromArgb(73, 80, 87);
                    break;
            }

            cell.Style.BackColor = backColor;
            cell.Style.ForeColor = foreColor;
            cell.Style.SelectionBackColor = backColor;
            cell.Style.SelectionForeColor = foreColor;
        }

        private static void StyleButtonCell(DataGridViewCell cell, bool enabled, Color enabledBackColor, Color enabledForeColor)
        {
            // Mau nut luu rieng, khong to mau ca o DataGridView.
            var appearance = new ButtonAppearance
            {
                BackColor = enabled ? enabledBackColor : MauDisabled,
                ForeColor = enabled ? enabledForeColor : MauDisabledText
            };
            cell.Tag = appearance;

            // Giữ màu nền của hàng ở bên ngoài nút, kể cả khi ô được chọn.
            cell.Style.Padding = new Padding(6, 4, 6, 4);
            cell.Style.BackColor = Color.Empty;
            cell.Style.SelectionBackColor = Color.Empty;
            cell.Style.ForeColor = appearance.ForeColor;
            cell.Style.SelectionForeColor = appearance.ForeColor;
            cell.ReadOnly = !enabled;
        }

        private void GrvKetQuaTimKiem_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0)
                return;

            string columnName = grvKetQuaTimKiem.Columns[e.ColumnIndex].Name;
            if (columnName != COL_CAT_LE && columnName != COL_XOA && columnName != COL_CHI_TIET)
                return;

            DataGridViewRow row = grvKetQuaTimKiem.Rows[e.RowIndex];
            DataGridViewCell cell = row.Cells[e.ColumnIndex];
            if (!(cell.Tag is ButtonAppearance appearance))
                return;

            // Nền ô đi theo màu hàng (trắng hoặc cảnh báo hết tồn), không theo màu nút.
            Color rowBackColor = row.DefaultCellStyle.BackColor;
            if (rowBackColor.IsEmpty)
                rowBackColor = grvKetQuaTimKiem.DefaultCellStyle.BackColor;
            if (rowBackColor.IsEmpty)
                rowBackColor = SystemColors.Window;

            using (SolidBrush cellBrush = new SolidBrush(rowBackColor))
                e.Graphics.FillRectangle(cellBrush, e.CellBounds);
            e.Paint(e.CellBounds, DataGridViewPaintParts.Border);

            Padding buttonPadding = cell.Style.Padding;
            Rectangle buttonBounds = new Rectangle(
                e.CellBounds.Left + buttonPadding.Left,
                e.CellBounds.Top + buttonPadding.Top,
                e.CellBounds.Width - buttonPadding.Horizontal - 1,
                e.CellBounds.Height - buttonPadding.Vertical - 1);

            if (buttonBounds.Width > 1 && buttonBounds.Height > 1)
            {
                // Bo tron chinh hinh nut; nen cua o DataGridView van theo mau hang.
                Rectangle borderBounds = new Rectangle(buttonBounds.Left, buttonBounds.Top,
                    buttonBounds.Width - 1, buttonBounds.Height - 1);
                using (GraphicsPath path = CreateRoundedButtonPath(borderBounds))
                {
                    SmoothingMode previousMode = e.Graphics.SmoothingMode;
                    try
                    {
                        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                        using (SolidBrush buttonBrush = new SolidBrush(appearance.BackColor))
                            e.Graphics.FillPath(buttonBrush, path);
                        using (Pen borderPen = new Pen(ControlPaint.Dark(appearance.BackColor)))
                            e.Graphics.DrawPath(borderPen, path);
                    }
                    finally
                    {
                        e.Graphics.SmoothingMode = previousMode;
                    }
                }

                Rectangle textBounds = Rectangle.Inflate(buttonBounds, -3, -1);
                if (textBounds.Width > 0 && textBounds.Height > 0)
                {
                    string buttonText = Convert.ToString(e.FormattedValue, CultureInfo.CurrentCulture) ?? string.Empty;
                    TextRenderer.DrawText(
                        e.Graphics,
                        buttonText,
                        e.CellStyle.Font ?? grvKetQuaTimKiem.Font,
                        textBounds,
                        appearance.ForeColor,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                        TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
                }
            }

            e.Handled = true;
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
                if (string.IsNullOrWhiteSpace(raw) || raw.Trim() == model.SoLuongCanLay.ToString(CultureInfo.InvariantCulture))
                    ConfirmExecutionDraftReview(model);
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
                        gridRow.Cells[COL_CD_CAT].Value = model.ChuoiChieuDaiCat;
                        return;
                    }

                    DialogResult answer = MessageBox.Show(
                        model.NhomCatPopup.Count + " cuộn đang được cắt lẻ, tiếp tục sẽ chuyển về cắt 1 cuộn.",
                        "Xác nhận",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);
                    if (answer != DialogResult.Yes)
                    {
                        gridRow.Cells[COL_CD_CAT].Value = model.ChuoiChieuDaiCat;
                        return;
                    }

                    model.NhomCatPopup.Clear();
                    gridRow.Cells[COL_CAT_LE].Value = "Cắt lẻ";
                }

                if (string.IsNullOrWhiteSpace(raw))
                {
                    model.ChuoiChieuDaiCat = string.Empty;
                    gridRow.Cells[COL_CD_CAT].Value = model.ChuoiChieuDaiCat;
                    ApDungTrangThaiDong(gridRow, model);
                    _rowDangEditChieuDai = null;
                    ConfirmExecutionDraftReview(model);
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
                    else if (values.Sum() >= model.ChieuDai1Cuon)
                    {
                        FrmWaiting.ShowGifAlert("Tổng chiều dài cắt phải nhỏ hơn chiều dài một cuộn; nếu bằng nhau hãy chọn lấy nguyên cuộn.");
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

                gridRow.Cells[COL_CD_CAT].Value = model.ChuoiChieuDaiCat;
                ApDungTrangThaiDong(gridRow, model);
                _rowDangEditChieuDai = null;
                if (LapKeHoachCatDay_ChieuDaiParser.TryNormalize(raw, out string reviewed, out _, out _, out _)
                    && reviewed == model.ChuoiChieuDaiCat) ConfirmExecutionDraftReview(model);
            }
        }

        private async void GrvKetQuaTimKiem_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0)
                return;

            DataGridViewRow gridRow = grvKetQuaTimKiem.Rows[e.RowIndex];
            if (!(gridRow.Tag is LapKeHoachCatDay_GridRow model))
                return;

            string col = grvKetQuaTimKiem.Columns[e.ColumnIndex].Name;
            if (col == COL_CHI_TIET)
            {
                await MoChiTietThucHienAsync(model);
            }
            else if (col == COL_CAT_LE)
            {
                MoPopupCatLe(gridRow, model);
            }
            else if (col == COL_XOA)
            {
                XoaDongTrenGiaoDien(model);
            }
        }

        private async Task MoChiTietThucHienAsync(LapKeHoachCatDay_GridRow model)
        {
            if (_dangXemChiTiet || _loadingPlan || !model.DaTonTaiTrongDB ||
                !_keHoachId.HasValue || !model.KeHoachHang_ID.HasValue)
                return;

            long? qcId = model.LoaiDong == LapKeHoachCatDay_LoaiDong.CuonChan
                ? model.QuyCachCuonChan_ID : null;
            long? leId = model.LoaiDong == LapKeHoachCatDay_LoaiDong.CuonLe
                ? model.TonCuonLe_ID : null;
            if (!qcId.HasValue && !leId.HasValue) return;
            // Chi tiet doc lai DB khi click, khong ghi de cac du lieu nguoi dung dang sua tren grid.
            _dangXemChiTiet = true;
            try
            {
                long keHoachId = _keHoachId.Value;
                long hangId = model.KeHoachHang_ID.Value;
                var query = new ExecutionDetailQuery(keHoachId, hangId, qcId, leId);
                ExecutionDetailSnapshot snapshot = await WaitingHelper.RunWithWaiting(
                    () => Task.Run(() => ExecutionEdit_DB.Load(query)), "ĐANG TẢI CHI TIẾT THỰC HIỆN...");
                using (var frm = new Frm_ChiTietThucHienKeHoach(query, model.TenSP, snapshot))
                {
                    frm.ShowDialog(FindForm());
                    if (frm.LastSaveResult?.Success == true)
                        RefreshExecutionFactsPreservingDraft(frm.LastSaveResult);
                }
            }
            catch (InvalidOperationException ex)
            {
                FrmWaiting.ShowGifAlert(ex.Message);
            }
            catch (Exception)
            {
                FrmWaiting.ShowGifAlert("Không thể tải chi tiết thực hiện kế hoạch. Vui lòng kiểm tra kết nối và dữ liệu.");
            }
            finally { _dangXemChiTiet = false; }
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

            gridRow.Cells[COL_CD_CAT].Value = model.ChuoiChieuDaiCat;
            gridRow.Cells[COL_CAT_LE].Value = TaoNhanNutCatLe(model);
            _executionDraftReview.Remove(model.RowKey);
            ApDungTrangThaiDong(gridRow, model);
        }

        private static string ExecutionFacts(LapKeHoachCatDay_GridRow row)
        {
            return row.TonThucTe + ":" + row.DatTruoc + ":" + row.SoLuongDaLay + ":" +
                string.Join("|", row.NhomCatDaLuu.Concat(row.NhomCatNguonDaChuyenLe).Select(g =>
                    g.Id + ":" + g.TonCuonLe_ID + ":" + string.Join(",", g.ChiTiet.Select(d => d.Id + ":" + d.ChieuDai + ":" + d.DaThucHien))));
        }

        internal void RefreshExecutionFactsPreservingDraft(ExecutionEditResult result)
        {
            if (!result.Success || !_keHoachId.HasValue) return;
            var context = LapKeHoachCatDay_DB.LayKeHoachTheoMaVoiPhienBan(_maKeHoachDaNap);
            if (!context.TonTai || context.Header.Id != _keHoachId.Value)
                throw new InvalidOperationException("Kế hoạch đã thay đổi. Bản nháp được giữ để đối chiếu.");
            bool active = context.Header.TrangThai == "ACTIVE";
            var freshRows = context.Rows.ToDictionary(x => x.RowKey, StringComparer.OrdinalIgnoreCase);
            foreach (var fresh in context.Rows)
            {
                if (_choXoa.TryGetValue(fresh.RowKey, out var deleted))
                {
                    if (ExecutionFacts(deleted) != ExecutionFacts(fresh)) _executionDraftReview.Add(fresh.RowKey);
                    PreservePlanningInput(fresh, deleted);
                    _choXoa[fresh.RowKey] = fresh;
                    continue;
                }
                bool dirty = _rows.TryGetValue(fresh.RowKey, out var old) && (old.DaTonTaiTrongDB ? DongBiSua(old) : CoNoiDungNhap(old));
                if (dirty)
                {
                    if (ExecutionFacts(old) != ExecutionFacts(fresh)) _executionDraftReview.Add(fresh.RowKey);
                    PreservePlanningInput(fresh, old);
                    if (_executionDraftReview.Contains(fresh.RowKey)) fresh.CanEdit = active;
                }
                else _dongGoc[fresh.RowKey] = TaoDauVanTay(fresh);
                if (!active) { fresh.CanEdit = false; fresh.CanDelete = false; }
                ThemHoacCapNhatDongGrid(fresh, true);
                if (_executionDraftReview.Contains(fresh.RowKey))
                {
                    var gridRow = TimGridRowTheoKey(fresh.RowKey);
                    gridRow.Cells[COL_TRANG_THAI].Value = fresh.TrangThai + " | Đối chiếu bản nháp";
                    gridRow.Cells[COL_TRANG_THAI].ToolTipText = "Tiến độ đã thay đổi. Kiểm tra lại chiều dài / số lượng đang nhập rồi kết thúc sửa ô để xác nhận.";
                }
            }
            foreach (var old in _rows.Values.Where(x => x.DaTonTaiTrongDB && !freshRows.ContainsKey(x.RowKey)).ToList())
            {
                if (DongBiSua(old))
                {
                    _executionDraftReview.Add(old.RowKey);
                    // Keep the input for comparison, but deletion now only discards a local
                    // obsolete draft; it must never delete the surviving source plan group.
                    old.DaTonTaiTrongDB = false; old.CanEdit = false; old.CanDelete = true;
                    var gridRow = TimGridRowTheoKey(old.RowKey);
                    if (gridRow != null)
                    {
                        GanGiaTriDong(gridRow, old); ApDungTrangThaiDong(gridRow, old);
                        gridRow.Cells[COL_TRANG_THAI].Value = "Nguồn đã hoàn tác | Xóa để bỏ bản nháp dòng này";
                    }
                }
                else { var gridRow = TimGridRowTheoKey(old.RowKey); if (gridRow != null) grvKetQuaTimKiem.Rows.Remove(gridRow); _rows.Remove(old.RowKey); }
            }
            _executionRevision = context.ExecutionRevision;
        }

        private static void PreservePlanningInput(LapKeHoachCatDay_GridRow target, LapKeHoachCatDay_GridRow draft)
        {
            target.SoLuongCanLay = draft.SoLuongCanLay;
            target.ChuoiChieuDaiCat = draft.ChuoiChieuDaiCat;
            target.NhomCatPopup = draft.NhomCatPopup.Select(x => x.Clone()).ToList();
        }

        private void ConfirmExecutionDraftReview(LapKeHoachCatDay_GridRow row)
        {
            if (_executionDraftReview.Contains(row.RowKey) && row.CanEdit && KiemTraDuLieuDongTruocKhiLuu(row))
            {
                _executionDraftReview.Remove(row.RowKey);
                var gridRow = TimGridRowTheoKey(row.RowKey);
                if (gridRow != null) { GanGiaTriDong(gridRow, row); ApDungTrangThaiDong(gridRow, row); }
            }
        }

        private async void btnLuuKeHoach_Click(object sender, EventArgs e)
        {
            await LuuToanBoKeHoachAsync();
        }

        private static bool CoNoiDungNhap(LapKeHoachCatDay_GridRow row)
        {
            return row.LoaiDong == LapKeHoachCatDay_LoaiDong.CuonChan
                ? row.SoLuongCanLay > 0 || !string.IsNullOrWhiteSpace(row.ChuoiChieuDaiCat)
                    || (row.NhomCatPopup != null && row.NhomCatPopup.Count > 0)
                : !string.IsNullOrWhiteSpace(row.ChuoiChieuDaiCat);
        }

        private static string TaoDauVanTay(LapKeHoachCatDay_GridRow row)
        {
            string nhom = string.Join("|", (row.NhomCatPopup ?? new List<LapKeHoachCatDay_NhomCat>())
                .Select(g => (g.Id?.ToString(CultureInfo.InvariantCulture) ?? "0") + ":" +
                    string.Join(",", g.ChiTiet.Select(x =>
                        (x.Id?.ToString(CultureInfo.InvariantCulture) ?? "0") + ":" +
                        x.ChieuDai.ToString(CultureInfo.InvariantCulture) + ":" + x.DaThucHien))));
            return row.SoLuongCanLay.ToString(CultureInfo.InvariantCulture) + "#" +
                (row.ChuoiChieuDaiCat ?? string.Empty) + "#" + nhom;
        }

        private void GhiNhanBanGoc()
        {
            _dongGoc.Clear();
            foreach (LapKeHoachCatDay_GridRow row in _rows.Values.Where(x => x.DaTonTaiTrongDB))
                _dongGoc[row.RowKey] = TaoDauVanTay(row);
            _nguoiNhanGoc = cbxNguoiNhan.Text ?? string.Empty;
            _nguoiCatGoc = cbxNguoiCat.Text ?? string.Empty;
            _ghiChuGoc = tbGhiChu.Text ?? string.Empty;
        }

        private bool DongBiSua(LapKeHoachCatDay_GridRow row)
        {
            return !_dongGoc.TryGetValue(row.RowKey, out string old)
                || !string.Equals(old, TaoDauVanTay(row), StringComparison.Ordinal);
        }

        private bool CoThayDoiChuaLuu()
        {
            return _choXoa.Count > 0
                || _rows.Values.Any(row => row.DaTonTaiTrongDB
                    ? DongBiSua(row) : CoNoiDungNhap(row))
                || !string.Equals(_nguoiNhanGoc, cbxNguoiNhan.Text ?? "", StringComparison.Ordinal)
                || !string.Equals(_nguoiCatGoc, cbxNguoiCat.Text ?? "", StringComparison.Ordinal)
                || !string.Equals(_ghiChuGoc, tbGhiChu.Text ?? "", StringComparison.Ordinal);
        }

        private async Task LuuToanBoKeHoachAsync()
        {
            if (_dangLuu || _loadingPlan)
                return;
            grvKetQuaTimKiem.EndEdit();
            if (_executionDraftReview.Count > 0)
            {
                FrmWaiting.ShowGifAlert("Tiến độ đã thay đổi. Hãy đối chiếu các dòng có cảnh báo và kết thúc sửa ô số lượng / chiều dài; dòng chờ xóa cần tìm lại để kiểm tra trước khi xóa lại.");
                return;
            }
            if (string.IsNullOrWhiteSpace(cbxNguoiCat.Text))
            {
                FrmWaiting.ShowGifAlert("Người cắt không được trống.");
                cbxNguoiCat.Focus();
                return;
            }
            string ma = (tbLenhXuatHang.Text ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(ma))
            {
                FrmWaiting.ShowGifAlert("Mã kế hoạch không được trống.");
                return;
            }
            if (!string.IsNullOrEmpty(_maKeHoachDaNap) &&
                !string.Equals(ma, _maKeHoachDaNap, StringComparison.OrdinalIgnoreCase))
            {
                FrmWaiting.ShowGifAlert("Nhấn Enter để tải mã kế hoạch trước khi lưu.");
                return;
            }

            var toSave = new List<LapKeHoachCatDay_GridRow>();
            var toDelete = _choXoa.Values.ToList();
            foreach (LapKeHoachCatDay_GridRow row in _rows.Values)
            {
                if (row.DaTonTaiTrongDB)
                {
                    if (!DongBiSua(row))
                        continue;
                    if (DongKhongConNoiDungChuaThucHien(row))
                    {
                        if (!row.CanDelete)
                        {
                            FrmWaiting.ShowGifAlert("Không thể xóa phần kế hoạch đã thực hiện: " + row.TenSP);
                            return;
                        }
                        toDelete.Add(row);
                        continue;
                    }
                    if (!row.CanEdit || !KiemTraDuLieuDongTruocKhiLuu(row))
                        return;
                    toSave.Add(row);
                }
                else if (CoNoiDungNhap(row))
                {
                    if (!row.CanEdit || !KiemTraDuLieuDongTruocKhiLuu(row))
                        return;
                    toSave.Add(row);
                }
            }

            bool headerChanged = _keHoachId.HasValue &&
                (!string.Equals(_nguoiNhanGoc, cbxNguoiNhan.Text ?? "", StringComparison.Ordinal)
                 || !string.Equals(_nguoiCatGoc, cbxNguoiCat.Text ?? "", StringComparison.Ordinal)
                 || !string.Equals(_ghiChuGoc, tbGhiChu.Text ?? "", StringComparison.Ordinal));
            if (!_keHoachId.HasValue && toSave.Count == 0)
            {
                FrmWaiting.ShowGifAlert("Cần ít nhất một dòng kế hoạch.");
                return;
            }
            if (_keHoachId.HasValue && toSave.Count == 0 && toDelete.Count == 0 && !headerChanged)
            {
                FrmWaiting.ShowGifAlert("Kế hoạch chưa có thay đổi để lưu.");
                return;
            }

            var request = new LapKeHoachCatDay_BatchSaveRequest
            {
                KeHoach_IDDuKien = _keHoachId,
                ExpectedExecutionRevision = _executionRevision,
                MaKeHoach = ma,
                NguoiNhan = cbxNguoiNhan.Text ?? string.Empty,
                NguoiCat = cbxNguoiCat.Text ?? string.Empty,
                GhiChu = tbGhiChu.Text ?? string.Empty,
                NguoiTao = UserContext.UserName ?? string.Empty,
                DongCanLuu = toSave,
                DongCanXoa = toDelete
            };

            _dangLuu = true;
            try
            {
                LapKeHoachCatDay_SaveResult result = await WaitingHelper.RunWithWaiting(
                    () => Task.Run(() => LapKeHoachCatDay_DB.LuuKeHoachBatch(request)),
                    "ĐANG LƯU TOÀN BỘ KẾ HOẠCH...");
                if (!result.ThanhCong)
                {
                    if (result.DuLieuDaCu && _keHoachId.HasValue)
                        RefreshExecutionFactsPreservingDraft(new ExecutionEditResult { Success = true });
                    FrmWaiting.ShowGifAlert(string.IsNullOrWhiteSpace(result.Loi)
                        ? "Không thể lưu kế hoạch. Không có thay đổi nào được ghi." : result.Loi);
                    return;
                }
                FrmWaiting.ShowGifAlert("Đã lưu kế hoạch " + ma + " thành công.", myIcon:EnumStore.Icon.Success);
                ResetVeTrangThaiMoi();
            }
            catch (Exception)
            {
                FrmWaiting.ShowGifAlert("Không thể lưu toàn bộ kế hoạch. Không có thay đổi nào được ghi.");
            }
            finally
            {
                _dangLuu = false;
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

        private void XoaDongTrenGiaoDien(LapKeHoachCatDay_GridRow model)
        {
            if (model == null ||
                (model.DaTonTaiTrongDB && (!model.CanEdit || !model.CanDelete)))
                return;

            string message = !model.DaTonTaiTrongDB
                ? "Bạn muốn xoá sản phẩm này khỏi danh sách?"
                : model.CoThucHienB3
                    ? "Phần chưa thực hiện sẽ được xoá khỏi hệ thống?"
                    : "Bạn muốn xoá sản phẩm này khỏi hệ thống?";
            if (MessageBox.Show(message, "Xác nhận", MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            RemovePlanningRow(model);
        }

        private void RemovePlanningRow(LapKeHoachCatDay_GridRow model)
        {
            if (model.DaTonTaiTrongDB) _choXoa[model.RowKey] = model;
            _executionDraftReview.Remove(model.RowKey);
            _rows.Remove(model.RowKey);
            DataGridViewRow gridRow = TimGridRowTheoKey(model.RowKey);
            if (gridRow != null)
                grvKetQuaTimKiem.Rows.Remove(gridRow);
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
            _dongGoc.Clear();
            _choXoa.Clear();
            _executionRevision = null;
            _executionDraftReview.Clear();
            _nguoiNhanGoc = string.Empty;
            _nguoiCatGoc = string.Empty;
            _ghiChuGoc = string.Empty;

            _nguoiCatSearchHelper?.Reset();
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
            _tenSanPhamSearchHelper?.Reset();
            _lotSearchHelper?.Reset();
            cbxTenKhachHang.Text = string.Empty;
            nbrTimSoLuong.Value = 0;
            tbLenhXuatHang.Focus();
        }
    }
}
