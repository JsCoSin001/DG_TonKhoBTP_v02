using DG_TonKhoBTP_v02.Database.ChatLuong;
using DG_TonKhoBTP_v02.Models;
using DG_TonKhoBTP_v02.Models.SanXuat;
using DG_TonKhoBTP_v02.Helper;
using DG_TonKhoBTP_v02.UI.Helper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using DG_TonKhoBTP_v02.UI.Helper.AutoSearchWithCombobox;

namespace DG_TonKhoBTP_v02.UI.NghiepVuKhac.Kho.NhapKho
{
    public partial class Frm_NhapKhoTP_TheoNgay : Form
    {
        // -1: "Không" (không áp dụng lọc máy); 0: "Toàn bộ" (máy công đoạn 5).
        private const int KhongLocMayId = -1;

        private bool _dangDongBoCbxAll;

        private ComboBoxSearchHelper _tenSanPhamSearchHelper;
        private long? _sanPhamIdDaChon;
        private ComboBoxSearchHelper _lotSearchHelper;
        private long? _ttThanhPhamIdDaChon;
        // Lưu bộ lọc đã thực sự chạy, không đọc lại điều khiển có thể đã bị đổi khi refresh.
        private BoLocNhapKhoTheoNgay? _boLocDaTim;
        // Mỗi Mã Bin có thể đến từ một lần tìm nhanh khác nhau.
        private readonly Dictionary<long, BoLocNhapKhoTheoNgay> _boLocNguonTheoMaBin =
            new Dictionary<long, BoLocNhapKhoTheoNgay>();
        private readonly SemaphoreSlim _khoaGrid = new SemaphoreSlim(1, 1);
        private int _theHeGrid;
        private bool _dangThayTheGrid;
        private bool _dangNhapKho;

        private bool _dangApDungLamMoi;
        private const int WM_SETREDRAW = 0x000B;

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern IntPtr SendMessage(
            IntPtr hWnd,
            int msg,
            IntPtr wParam,
            IntPtr lParam);


        private bool _dangResetMacDinh;
        private bool _daCanhBaoNgayGoiYKhongHopLe;
        // Dấu vết chỉ bao gồm các giá trị thực sự tham gia lọc gợi ý.
        private string _dauVetBoLocGoiY = string.Empty;

        private sealed class BoLocNhapKhoTheoNgay
        {
            public DateTime? NgayBatDau { get; set; }
            public DateTime? NgayKetThuc { get; set; }
            public DateTime? GioiHanHienTai { get; set; }
            public string Ca { get; set; } = string.Empty;
            // -1 = Không (không lọc máy); null = Toàn bộ máy công đoạn 5;
            // > 0 = máy cụ thể, chỉ đối chiếu bằng DanhSachMay_ID.
            public int? DanhSachMayId { get; set; }
            public long? SanPhamId { get; set; }
            public long? TTThanhPhamId { get; set; }

            public BoLocNhapKhoTheoNgay Copy() => new BoLocNhapKhoTheoNgay
            {
                NgayBatDau = NgayBatDau, NgayKetThuc = NgayKetThuc,
                GioiHanHienTai = GioiHanHienTai, Ca = Ca,
                DanhSachMayId = DanhSachMayId,
                SanPhamId = SanPhamId, TTThanhPhamId = TTThanhPhamId
            };
        }

        private static NhapKhoTheoNgaySearchResult TimKiemTheoBoLoc(BoLocNhapKhoTheoNgay boLoc)
        {
            return NhapKho_DB.TimKiemNhapKhoTheoNgay(
                boLoc.NgayBatDau, boLoc.NgayKetThuc, boLoc.Ca,
                boLoc.SanPhamId, boLoc.GioiHanHienTai, boLoc.TTThanhPhamId,
                boLoc.DanhSachMayId);
        }

        private const string CChon = "col_Chon";
        private const string CTTThanhPhamId = "col_TTThanhPham_ID";
        private const string CSourceId = "col_TTCuonDay_CD_ID";
        private const string CTTLoId = "col_TTLo_ID";
        private const string CNgay = "col_Ngay";
        private const string CCa = "col_Ca";
        private const string CMay = "col_May";
        private const string CMaBin = "col_MaBin";
        private const string CMaSP = "col_MaSP";
        private const string CTenSP = "col_TenSP";
        private const string CLoai = "col_Loai";
        private const string CSoLuongCon = "col_SoLuongCon";
        private const string CSoLuongNhap = "col_SoLuongNhap";
        private const string CChieuDai = "col_ChieuDai";
        private const string CSoDau = "col_SoDau";
        private const string CSoCuoi = "col_SoCuoi";
        private const string CTongNhap = "col_TongNhap";
        private const string CGhiChu = "col_GhiChu";
        private const string CSnapshot = "col_ChieuDaiSauSnapshot";
        private const string CDao = "col_DaoChieu";
        private const string CXoa = "col_Xoa";

        private const string CotLotQcExcel = "lot_QC";

        private static readonly string[] CotXuatExcel =
        {
            CNgay,
            CCa,
            CLoai,
            CTenSP,
            CMaBin,
            CotLotQcExcel,
            CTongNhap,
            CSoLuongNhap,
            CChieuDai,
            CSoDau,
            CSoCuoi,
            CGhiChu
        };

        private const string CotIdXuatNoiBo = "TTThanhPham_ID";

        public Frm_NhapKhoTP_TheoNgay()
        {
            InitializeComponent();

            StartPosition = FormStartPosition.CenterScreen;
            WindowState = FormWindowState.Maximized;

            KhoiTaoGrid();
            NapDanhSachMay();
            GanSuKien();
            KhoiTaoTimKiemTenSanPham();
            KhoiTaoTimKiemLot();
            ResetMacDinh();
        }

        // Tái sử dụng danh mục máy đã có trong dự án, không dựa vào tên máy
        // lưu ở ThongTinCaLamViec để xác định máy của bản ghi.
        private void NapDanhSachMay()
        {
            const int maCongDoanBocVo = 5;
            var danhSachMay = new List<DanhSachMay_Model>
            {
                new DanhSachMay_Model
                {
                    Id = KhongLocMayId, MaCongDoan = maCongDoanBocVo, TenMay = "Không"
                },
                new DanhSachMay_Model
                {
                    Id = 0, MaCongDoan = maCongDoanBocVo, TenMay = "Toàn bộ"
                }
            };
            danhSachMay.AddRange(DanhSachMayHelper.LayTheoMaCongDoan(maCongDoanBocVo));

            cbxMay.DisplayMember = nameof(DanhSachMay_Model.TenMay);
            cbxMay.ValueMember = nameof(DanhSachMay_Model.Id);
            cbxMay.DataSource = danhSachMay;
            cbxMay.SelectedIndex = 0;
        }

        private void KhoiTaoTimKiemTenSanPham()
        {
            cbxTimTenSp.DropDownStyle = ComboBoxStyle.DropDown;

            _tenSanPhamSearchHelper = new ComboBoxSearchHelper(
                cbxTimTenSp,
                TimGoiYTenSanPhamTheoBoLocAsync)
            {
                DisplayColumn = "Ten",
                SelectedTextBehavior =
                    ComboBoxSelectedTextBehavior.FillDisplayText,
                DebounceMs = 350
            };

            _tenSanPhamSearchHelper.ItemSelected += async row =>
            {
                _sanPhamIdDaChon = Convert.ToInt64(row["id"]);
                await TimKiemNhanhVaNoiTiepAsync(_sanPhamIdDaChon, null, cbxTimTenSp);
            };

            _tenSanPhamSearchHelper.Cleared += () =>
                _sanPhamIdDaChon = null;

            // Người dùng sửa nội dung thì hủy ID đã chọn.
            // Phải chọn lại item trong danh sách gợi ý.
            cbxTimTenSp.TextUpdate += (s, e) =>
            {
                _sanPhamIdDaChon = null;
                // Hủy truy vấn cũ cả khi người dùng xóa sạch từ khóa.
                if (string.IsNullOrWhiteSpace(cbxTimTenSp.Text))
                    _tenSanPhamSearchHelper?.Reset();
            };

            Disposed += (s, e) =>
                _tenSanPhamSearchHelper?.Dispose();
        }

        private void KhoiTaoTimKiemLot()
        {
            cbxLOT.DropDownStyle = ComboBoxStyle.DropDown;
            _lotSearchHelper = new ComboBoxSearchHelper(
                cbxLOT,
                TimGoiYLotTheoBoLocAsync)
            {
                DisplayColumn = "MaBin",
                SelectedTextBehavior = ComboBoxSelectedTextBehavior.FillDisplayText,
                DebounceMs = 350
            };

            _lotSearchHelper.ItemSelected += async row =>
            {
                _ttThanhPhamIdDaChon = Convert.ToInt64(row["id"]);
                await TimKiemNhanhVaNoiTiepAsync(null, _ttThanhPhamIdDaChon, cbxLOT);
            };
            _lotSearchHelper.Cleared += () => _ttThanhPhamIdDaChon = null;
            // Chỉ ID của mục chọn từ dropdown là hợp lệ; sửa text phải chọn lại.
            cbxLOT.TextUpdate += (s, e) =>
            {
                _ttThanhPhamIdDaChon = null;
                if (string.IsNullOrWhiteSpace(cbxLOT.Text))
                    _lotSearchHelper?.Reset();
            };
            Disposed += (s, e) => _lotSearchHelper?.Dispose();
        }

        private void KhoiTaoGrid()
        {
            dsNhapKho.AutoGenerateColumns = false;
            dsNhapKho.Font = new System.Drawing.Font("Tahoma", 11F, System.Drawing.FontStyle.Regular);
            dsNhapKho.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
            dsNhapKho.RowTemplate.Height = 35;
            dsNhapKho.AllowUserToAddRows = false;
            dsNhapKho.AllowUserToDeleteRows = false;
            dsNhapKho.MultiSelect = false;
            dsNhapKho.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dsNhapKho.Columns.Clear();

            dsNhapKho.Columns.Add(new DataGridViewCheckBoxColumn { Name = CChon, HeaderText = "Chọn", Width = 55, ReadOnly = false });
            dsNhapKho.Columns.Add(Hidden(CTTThanhPhamId));
            dsNhapKho.Columns.Add(Hidden(CSourceId));
            dsNhapKho.Columns.Add(Hidden(CTTLoId));
            dsNhapKho.Columns.Add(Hidden(CSnapshot));
            dsNhapKho.Columns.Add(CreateTextColumn(CNgay, "Ngày", 100));
            dsNhapKho.Columns.Add(CreateTextColumn(CCa, "Ca", 55));
            dsNhapKho.Columns.Add(CreateTextColumn(CMay, "Máy", 70));
            dsNhapKho.Columns.Add(CreateTextColumn(CMaBin, "Mã Bin", 150));
            dsNhapKho.Columns.Add(CreateTextColumn(CMaSP, "Mã SP", 110));
            dsNhapKho.Columns.Add(CreateTextColumn(CTenSP, "Tên SP", 180));
            dsNhapKho.Columns.Add(CreateTextColumn(CLoai, "Loại", 90));
            dsNhapKho.Columns.Add(CreateTextColumn(CSoLuongCon, "SL còn", 110));
            dsNhapKho.Columns.Add(new DataGridViewTextBoxColumn { Name = CSoLuongNhap, HeaderText = "SL nhập", Width = 100, ReadOnly = false });
            dsNhapKho.Columns.Add(CreateTextColumn(CChieuDai, "CD đơn vị", 100));
            dsNhapKho.Columns.Add(CreateTextColumn(CSoDau, "Số đầu", 100));
            dsNhapKho.Columns.Add(CreateTextColumn(CSoCuoi, "Số cuối", 100));
            dsNhapKho.Columns.Add(CreateTextColumn(CTongNhap, "Tổng nhập", 110));
            dsNhapKho.Columns.Add(new DataGridViewTextBoxColumn { Name = CGhiChu, HeaderText = "Ghi chú", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, ReadOnly = true });
            dsNhapKho.Columns.Add(new DataGridViewButtonColumn { Name = CDao, HeaderText = "Đảo chiều", Text = "Đảo", UseColumnTextForButtonValue = true, Width = 85 });
            dsNhapKho.Columns.Add(new DataGridViewButtonColumn { Name = CXoa, HeaderText = "Xóa", Text = "Xóa", UseColumnTextForButtonValue = true, Width = 65 });
        }

        private static DataGridViewTextBoxColumn CreateTextColumn(string name, string header, int width)
            => new DataGridViewTextBoxColumn { Name = name, HeaderText = header, Width = width, ReadOnly = true };

        private static DataGridViewTextBoxColumn Hidden(string name)
            => new DataGridViewTextBoxColumn { Name = name, Visible = false, ReadOnly = true };

        private void GanSuKien()
        {
            btnTimKiem.Click += btnTimKiem_Click;
            cbxNgayBD.CheckedChanged += (s, e) =>
            {
                dtNgayBD.Enabled = cbxNgayBD.Checked;
                KhiThayDoiBoLocGoiY();
            };
            cbxNgayKT.CheckedChanged += (s, e) =>
            {
                dtNgayKT.Enabled = cbxNgayKT.Checked;
                KhiThayDoiBoLocGoiY();
            };
            dtNgayBD.ValueChanged += (s, e) =>
            {
                if (cbxNgayBD.Checked) KhiThayDoiBoLocGoiY();
            };
            dtNgayKT.ValueChanged += (s, e) =>
            {
                if (cbxNgayKT.Checked) KhiThayDoiBoLocGoiY();
            };
            cbxCa.SelectedIndexChanged += (s, e) => KhiThayDoiBoLocGoiY();
            cbxMay.SelectedIndexChanged += (s, e) => KhiThayDoiBoLocGoiY();
            // ComboBox Ca vẫn cho phép nhập text bằng tay.
            cbxCa.TextUpdate += (s, e) => KhiThayDoiBoLocGoiY();
            btnNhapKho.Click += btnNhapKho_Click;
            cbxAll.CheckedChanged += cbxAll_CheckedChanged;
            dsNhapKho.CellContentClick += dsNhapKho_CellContentClick;
            dsNhapKho.CellValueChanged += dsNhapKho_CellValueChanged;
            dsNhapKho.CurrentCellDirtyStateChanged += dsNhapKho_CurrentCellDirtyStateChanged;
            dsNhapKho.CellValidating += dsNhapKho_CellValidating;
        }

        private string TaoDauVetBoLocGoiY()
        {
            string bd = cbxNgayBD.Checked
                ? dtNgayBD.Value.Date.ToString("yyyy-MM-dd") : "*";
            string kt = cbxNgayKT.Checked
                ? dtNgayKT.Value.Date.ToString("yyyy-MM-dd") : "*";
            var may = cbxMay.SelectedItem as DanhSachMay_Model;
            return bd + "|" + kt + "|" + (cbxCa.Text ?? string.Empty).Trim()
                + "|" + (may?.Id.ToString(CultureInfo.InvariantCulture) ?? "?");
        }

        private void KhiThayDoiBoLocGoiY()
        {
            if (_dangResetMacDinh || IsDisposed) return;
            string dauVetMoi = TaoDauVetBoLocGoiY();
            if (string.Equals(dauVetMoi, _dauVetBoLocGoiY, StringComparison.Ordinal))
                return;

            _dauVetBoLocGoiY = dauVetMoi;
            // Chỉ cho phép cảnh báo lại khi bộ lọc đã trở về hợp lệ.
            if (TryDocBoLocGoiY(out _, out _))
                _daCanhBaoNgayGoiYKhongHopLe = false;
            ResetHaiComboBoxKhiDoiBoLoc();
        }

        private void ResetHaiComboBoxKhiDoiBoLoc()
        {
            // Hủy truy vấn gợi ý đang chờ, đóng popup và xóa ID lựa chọn cũ.
            // Tuyệt đối không can thiệp các dòng đang điền trong dsNhapKho.
            _sanPhamIdDaChon = null;
            _ttThanhPhamIdDaChon = null;
            _tenSanPhamSearchHelper?.Reset();
            _lotSearchHelper?.Reset();
        }

        private void ResetMacDinh()
        {
            _dangResetMacDinh = true;
            try
            {
                _boLocDaTim = null;
                _boLocNguonTheoMaBin.Clear();
                _theHeGrid++;
                cbxNgayBD.Checked = true;
                cbxNgayKT.Checked = true;
                dtNgayBD.Enabled = cbxNgayBD.Checked;
                dtNgayKT.Enabled = cbxNgayKT.Checked;
                _sanPhamIdDaChon = null;
                _tenSanPhamSearchHelper?.Reset();
                _ttThanhPhamIdDaChon = null;
                _lotSearchHelper?.Reset();
                dtNgayBD.Value = DateTime.Today.AddDays(-1);
                dtNgayKT.Value = DateTime.Today;
                cbxCa.SelectedItem = "Toàn bộ";
                if (cbxCa.SelectedIndex < 0 && cbxCa.Items.Count > 0)
                    cbxCa.SelectedIndex = 0;
                if (cbxMay.Items.Count > 0)
                    cbxMay.SelectedIndex = 0;
                _dangDongBoCbxAll = true;
                cbxAll.Checked = false;
                _dangDongBoCbxAll = false;
                dsNhapKho.Rows.Clear();
                _daCanhBaoNgayGoiYKhongHopLe = false;
            }
            finally
            {
                _dangResetMacDinh = false;
                _dauVetBoLocGoiY = TaoDauVetBoLocGoiY();
            }
        }

        private bool TryTaoBoLoc(long? sanPhamId, long? ttThanhPhamId,
            out BoLocNhapKhoTheoNgay boLoc, out string loi)
        {
            DateTime now = DateTime.Now;
            DateTime? bd = cbxNgayBD.Checked ? dtNgayBD.Value.Date : (DateTime?)null;
            DateTime? kt = cbxNgayKT.Checked ? dtNgayKT.Value.Date : (DateTime?)null;
            boLoc = null;
            loi = null;
            // Nếu danh mục máy chưa được tải thì không tìm kiếm ngầm như "Toàn bộ".
            if (!(cbxMay.SelectedItem is DanhSachMay_Model may))
            {
                loi = "Danh sách máy bọc vỏ chưa sẵn sàng.";
                return false;
            }
            if (bd.HasValue && kt.HasValue && bd.Value > kt.Value)
            {
                loi = "Ngày bắt đầu không được lớn hơn ngày kết thúc.";
                return false;
            }
            if (bd.HasValue && !kt.HasValue && bd.Value > now)
            {
                loi = "Ngày bắt đầu không được ở tương lai.";
                return false;
            }
            boLoc = new BoLocNhapKhoTheoNgay
            {
                NgayBatDau = bd,
                NgayKetThuc = kt,
                GioiHanHienTai = bd.HasValue && !kt.HasValue ? now : (DateTime?)null,
                Ca = cbxCa.Text,
                DanhSachMayId = may.Id == 0 ? (int?)null : may.Id,
                SanPhamId = sanPhamId,
                TTThanhPhamId = ttThanhPhamId
            };
            return true;
        }

        private bool TryDocBoLocGoiY(out BoLocNhapKhoTheoNgay boLoc, out string loi)
            => TryTaoBoLoc(null, null, out boLoc, out loi);

        private bool TaoBoLoc(long? sanPhamId, long? ttThanhPhamId,
            out BoLocNhapKhoTheoNgay boLoc)
        {
            if (TryTaoBoLoc(sanPhamId, ttThanhPhamId, out boLoc, out string loi))
                return true;
            FrmWaiting.ShowGifAlert(loi);
            return false;
        }

        // Các delegate này được helper gọi từ UI thread sau 350 ms. Bộ lọc được
        // chụp một lần trước khi chuyển sang truy vấn DB nền; không đọc UI trong Task.Run.
        private Task<DataTable> TimGoiYTenSanPhamTheoBoLocAsync(string keyword, CancellationToken ct)
        {
            if (ct.IsCancellationRequested || IsDisposed)
                return Task.FromCanceled<DataTable>(new CancellationToken(true));
            if (!TryDocBoLocGoiY(out BoLocNhapKhoTheoNgay boLoc, out string loi))
            {
                ThongBaoNgayGoiYKhongHopLe(loi);
                return Task.FromResult(new DataTable());
            }
            return NhapKho_DB.TimGoiYTenSanPhamNhapKhoAsync(keyword,
                boLoc.NgayBatDau, boLoc.NgayKetThuc, boLoc.Ca,
                boLoc.GioiHanHienTai, boLoc.DanhSachMayId, ct);
        }

        private Task<DataTable> TimGoiYLotTheoBoLocAsync(string keyword, CancellationToken ct)
        {
            if (ct.IsCancellationRequested || IsDisposed)
                return Task.FromCanceled<DataTable>(new CancellationToken(true));
            if (!TryDocBoLocGoiY(out BoLocNhapKhoTheoNgay boLoc, out string loi))
            {
                ThongBaoNgayGoiYKhongHopLe(loi);
                return Task.FromResult(new DataTable());
            }
            return NhapKho_DB.TimGoiYLotNhapKhoAsync(keyword,
                boLoc.NgayBatDau, boLoc.NgayKetThuc, boLoc.Ca,
                boLoc.GioiHanHienTai, boLoc.DanhSachMayId, ct);
        }

        private void ThongBaoNgayGoiYKhongHopLe(string loi)
        {
            if (_daCanhBaoNgayGoiYKhongHopLe || IsDisposed) return;
            _daCanhBaoNgayGoiYKhongHopLe = true;
            FrmWaiting.ShowGifAlert(loi);
        }

        private async Task TimKiemNhanhVaNoiTiepAsync(long? sanPhamId,
            long? ttThanhPhamId, ComboBox cbx)
        {
            // Helper đã điền text; bôi đen ngay để người dùng gõ mục kế tiếp.
            cbx.SelectionStart = 0;
            cbx.SelectionLength = cbx.Text.Length;
            if (_dangThayTheGrid || _dangNhapKho || IsDisposed) return;
            if (!TaoBoLoc(sanPhamId, ttThanhPhamId, out BoLocNhapKhoTheoNgay boLoc)) return;
            int theHe = _theHeGrid;
            await _khoaGrid.WaitAsync();
            try
            {
                if (theHe != _theHeGrid || _dangThayTheGrid || _dangNhapKho || IsDisposed) return;
                if (!dsNhapKho.EndEdit()) return;
                dsNhapKho.Enabled = false;
                NhapKhoTheoNgaySearchResult result = await WaitingHelper.RunWithWaiting(
                    () => Task.Run(() => TimKiemTheoBoLoc(boLoc)),
                    "ĐANG TÌM DỮ LIỆU NHẬP KHO...");
                if (theHe != _theHeGrid || IsDisposed) return;
                NoiTiepGrid(result.Items, boLoc);
                if (result.MaBinBatThuong.Count > 0)
                    FrmWaiting.ShowGifAlert("Một số LOT bị loại do lỗi");
                else if (result.Items.Count == 0)
                    FrmWaiting.ShowGifAlert("Không tìm thấy dữ liệu phù hợp.");
            }
            catch (Exception ex)
            {
                if (!IsDisposed)
                    FrmWaiting.ShowGifAlert($"Lỗi khi tìm nhanh dữ liệu nhập kho:\n{ex.Message}", "LỖI");
            }
            finally
            {
                if (!IsDisposed) dsNhapKho.Enabled = true;
                _khoaGrid.Release();
            }
        }

        private async void btnTimKiem_Click(object sender, EventArgs e)
        {
            if (_dangThayTheGrid || _dangNhapKho) return;
            if (!dsNhapKho.EndEdit()) return;
            if (!string.IsNullOrWhiteSpace(cbxTimTenSp.Text) && !_sanPhamIdDaChon.HasValue)
            {
                FrmWaiting.ShowGifAlert("Cần chọn lại tên sản phẩm.");
                return;
            }
            if (!string.IsNullOrWhiteSpace(cbxLOT.Text) && !_ttThanhPhamIdDaChon.HasValue)
            {
                FrmWaiting.ShowGifAlert("Cần chọn lại LOT.");
                return;
            }
            long? sanPhamId = string.IsNullOrWhiteSpace(cbxTimTenSp.Text)
                ? (long?)null : _sanPhamIdDaChon;
            long? ttThanhPhamId = string.IsNullOrWhiteSpace(cbxLOT.Text)
                ? (long?)null : _ttThanhPhamIdDaChon;
            if (!TaoBoLoc(sanPhamId, ttThanhPhamId, out BoLocNhapKhoTheoNgay boLoc)) return;

            if (dsNhapKho.Rows.Count > 0)
            {
                DialogResult xacNhan = MessageBox.Show(this,
                    "Dữ liệu sẽ bị xoá nếu bạn nhấn Yes", "Xác nhận tìm kiếm",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (xacNhan != DialogResult.Yes) return;
            }

            // Hủy hiệu lực mọi lượt nối tiếp đã chạy trước khi người dùng xác nhận Yes.
            _dangThayTheGrid = true;
            _theHeGrid++;
            cbxTimTenSp.Enabled = false;
            cbxLOT.Enabled = false;
            btnNhapKho.Enabled = false;
            await _khoaGrid.WaitAsync();
            try
            {
                dsNhapKho.Enabled = false;
                NhapKhoTheoNgaySearchResult result = await WaitingHelper.RunWithWaiting(
                    () => Task.Run(() => TimKiemTheoBoLoc(boLoc)),
                    "ĐANG TÌM DỮ LIỆU NHẬP KHO...");
                if (IsDisposed) return;
                NapGridVaLuuBoLoc(result.Items, boLoc);
                if (result.MaBinBatThuong.Count > 0)
                    FrmWaiting.ShowGifAlert("Một số LOT bị loại do lỗi");
                else if (result.Items.Count == 0)
                    FrmWaiting.ShowGifAlert("Không tìm thấy dữ liệu phù hợp.");
            }
            catch (Exception ex)
            {
                if (!IsDisposed)
                    FrmWaiting.ShowGifAlert($"Lỗi khi tìm dữ liệu nhập kho theo ngày:\n{ex.Message}", "LỖI");
            }
            finally
            {
                _khoaGrid.Release();
                _dangThayTheGrid = false;
                if (!IsDisposed)
                {
                    dsNhapKho.Enabled = true;
                    cbxTimTenSp.Enabled = true;
                    cbxLOT.Enabled = true;
                    btnNhapKho.Enabled = true;
                }
            }
        }

        private static Tuple<long, long, int?> KhoaChiTiet(NhapKhoTheoNgayDong item)
            => Tuple.Create(item.TTThanhPham_ID, item.TTCuonDay_CD_ID, item.TTLo_ID);

        private static Tuple<long, long, int?> KhoaChiTiet(DataGridViewRow row)
            => Tuple.Create(ParseLong(row.Cells[CTTThanhPhamId].Value),
                ParseLong(row.Cells[CSourceId].Value), ParseNullableInt(row.Cells[CTTLoId].Value));

        private void NoiTiepGrid(IEnumerable<NhapKhoTheoNgayDong> items,
            BoLocNhapKhoTheoNgay boLoc)
        {
            var keys = new HashSet<Tuple<long, long, int?>>(dsNhapKho.Rows
                .Cast<DataGridViewRow>().Select(KhoaChiTiet));
            foreach (NhapKhoTheoNgayDong item in items ?? Enumerable.Empty<NhapKhoTheoNgayDong>())
            {
                var key = KhoaChiTiet(item);
                if (!keys.Add(key))
                    continue;
                ThemDong(item, false);
                if (!_boLocNguonTheoMaBin.ContainsKey(item.TTThanhPham_ID))
                    _boLocNguonTheoMaBin[item.TTThanhPham_ID] = boLoc.Copy();
            }
            DongBoCbxAllTuGrid();
        }

        private void NapGridVaLuuBoLoc(IEnumerable<NhapKhoTheoNgayDong> items,
            BoLocNhapKhoTheoNgay boLoc)
        {
            _boLocDaTim = boLoc.Copy();
            _boLocNguonTheoMaBin.Clear();
            // Dedupe luôn cả kết quả từ nhiều nguồn ca trước khi nạp lại.
            NapGrid(Enumerable.Empty<NhapKhoTheoNgayDong>());
            NoiTiepGrid(items, boLoc);
        }

        private void NapGrid(IEnumerable<NhapKhoTheoNgayDong> items)
        {
            dsNhapKho.Rows.Clear();
            foreach (NhapKhoTheoNgayDong item in items ?? Enumerable.Empty<NhapKhoTheoNgayDong>())
                ThemDong(item, false);
            DongBoCbxAllTuGrid();
        }

        private void ThemDong(NhapKhoTheoNgayDong item, bool isChecked)
        {
            int i = dsNhapKho.Rows.Add();
            DataGridViewRow row = dsNhapKho.Rows[i];
            row.Cells[CChon].Value = isChecked;
            row.Cells[CTTThanhPhamId].Value = item.TTThanhPham_ID;
            row.Cells[CSourceId].Value = item.TTCuonDay_CD_ID;
            row.Cells[CTTLoId].Value = item.TTLo_ID.HasValue ? (object)item.TTLo_ID.Value : null;
            row.Cells[CSnapshot].Value = item.ChieuDaiSauSnapshot;
            row.Cells[CNgay].Value = item.Ngay;
            row.Cells[CCa].Value = item.Ca;
            row.Cells[CMay].Value = item.May;
            row.Cells[CMaBin].Value = item.MaBin;
            row.Cells[CMaSP].Value = item.MaSP;
            row.Cells[CTenSP].Value = item.TenSP;
            row.Cells[CLoai].Value = item.TTLo_ID.HasValue
                ? (string.IsNullOrWhiteSpace(item.KichThuocLo) ? "Lô" : "Lô " + item.KichThuocLo)
                : "Cuộn";
            row.Cells[CSoLuongCon].Value = item.SoLuongCon;
            row.Cells[CSoLuongNhap].Value = item.SoLuongCon;
            row.Cells[CChieuDai].Value = item.ChieuDai1Cuon;
            row.Cells[CSoDau].Value = item.SoDau.HasValue ? (object)item.SoDau.Value : null;
            row.Cells[CSoCuoi].Value = item.SoCuoi.HasValue ? (object)item.SoCuoi.Value : null;
            row.Cells[CGhiChu].Value = item.GhiChu ?? string.Empty;
            CapNhatTongDong(row);
        }

        private void cbxAll_CheckedChanged(object sender, EventArgs e)
        {
            if (_dangDongBoCbxAll) return;

            bool isChecked = cbxAll.Checked;

            _dangDongBoCbxAll = true;
            try
            {
                foreach (DataGridViewRow row in dsNhapKho.Rows)
                    row.Cells[CChon].Value = isChecked;

                dsNhapKho.EndEdit();
            }
            finally
            {
                _dangDongBoCbxAll = false;
            }
        }

        private void dsNhapKho_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (dsNhapKho.IsCurrentCellDirty && dsNhapKho.CurrentCell?.OwningColumn?.Name == CChon)
                dsNhapKho.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }


        private void dsNhapKho_CellValueChanged(
            object sender,
            DataGridViewCellEventArgs e)
        {
            if (_dangApDungLamMoi ||
                e.RowIndex < 0 ||
                e.ColumnIndex < 0)
                return;

            string name = dsNhapKho.Columns[e.ColumnIndex].Name;

            if (name == CChon && !_dangDongBoCbxAll)
                DongBoCbxAllTuGrid();

            if (name == CSoLuongNhap)
                CapNhatTongDong(dsNhapKho.Rows[e.RowIndex]);
        }


        private void DongBoCbxAllTuGrid()
        {
            _dangDongBoCbxAll = true;
            cbxAll.Checked = dsNhapKho.Rows.Count > 0
                && dsNhapKho.Rows.Cast<DataGridViewRow>().All(r => Convert.ToBoolean(r.Cells[CChon].Value ?? false));
            _dangDongBoCbxAll = false;
        }

        private void dsNhapKho_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            if (e.RowIndex < 0 || dsNhapKho.Columns[e.ColumnIndex].Name != CSoLuongNhap) return;
            if (!int.TryParse(Convert.ToString(e.FormattedValue), out int value) || value < 0)
            {
                e.Cancel = true;
                FrmWaiting.ShowGifAlert("Số lượng nhập phải là số nguyên không âm. Dòng được chọn để nhập phải lớn hơn 0.");
            }
        }

        private void dsNhapKho_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            string name = dsNhapKho.Columns[e.ColumnIndex].Name;
            DataGridViewRow row = dsNhapKho.Rows[e.RowIndex];

            if (name == CXoa)
            {
                long maBinId = ParseLong(row.Cells[CTTThanhPhamId].Value);
                dsNhapKho.Rows.RemoveAt(e.RowIndex);
                if (!dsNhapKho.Rows.Cast<DataGridViewRow>()
                    .Any(r => ParseLong(r.Cells[CTTThanhPhamId].Value) == maBinId))
                    _boLocNguonTheoMaBin.Remove(maBinId);
                DongBoCbxAllTuGrid();
                return;
            }

            if (name == CDao)
            {
                int? soDau = ParseNullableInt(row.Cells[CSoDau].Value);
                int? soCuoi = ParseNullableInt(row.Cells[CSoCuoi].Value);
                if (!soDau.HasValue && !soCuoi.HasValue)
                {
                    FrmWaiting.ShowGifAlert("Dòng này không có Số đầu/Số cuối để đảo chiều.");
                    return;
                }

                row.Cells[CSoDau].Value = soCuoi.HasValue ? (object)soCuoi.Value : null;
                row.Cells[CSoCuoi].Value = soDau.HasValue ? (object)soDau.Value : null;
            }
        }

        private static int? ParseNullableInt(object value)
            => int.TryParse(Convert.ToString(value), out int n) ? n : (int?)null;

        private static long ParseLong(object value)
            => long.TryParse(Convert.ToString(value), out long n) ? n : 0;

        private static double ParseDouble(object value)
            => double.TryParse(Convert.ToString(value), NumberStyles.Any, CultureInfo.InvariantCulture, out double n)
                || double.TryParse(Convert.ToString(value), out n) ? n : 0;

        private void CapNhatTongDong(DataGridViewRow row)
        {
            int soLuong = int.TryParse(Convert.ToString(row.Cells[CSoLuongNhap].Value), out int sl) ? sl : 0;
            int chieuDai = int.TryParse(Convert.ToString(row.Cells[CChieuDai].Value), out int cd) ? cd : 0;
            row.Cells[CTongNhap].Value = (soLuong * chieuDai).ToString(CultureInfo.InvariantCulture);
        }

        private bool ValidateDongDuocChon(DataGridViewRow row, out string error)
        {
            error = string.Empty;
            string maBin = Convert.ToString(row.Cells[CMaBin].Value) ?? string.Empty;
            int soCon = int.TryParse(Convert.ToString(row.Cells[CSoLuongCon].Value), out int sc) ? sc : 0;
            int soNhap = int.TryParse(Convert.ToString(row.Cells[CSoLuongNhap].Value), out int sn) ? sn : 0;
            if (soNhap <= 0 || soNhap > soCon)
            {
                error = $"{maBin}: Số lượng nhập phải từ 1 đến {soCon}.";
                return false;
            }

            int? ttLoId = ParseNullableInt(row.Cells[CTTLoId].Value);
            int? soDau = ParseNullableInt(row.Cells[CSoDau].Value);
            int? soCuoi = ParseNullableInt(row.Cells[CSoCuoi].Value);
            int chieuDai = int.TryParse(Convert.ToString(row.Cells[CChieuDai].Value), out int cd) ? cd : -1;
            if (ttLoId.HasValue)
            {
                if (!soDau.HasValue || !soCuoi.HasValue)
                {
                    error = $"{maBin}: Lô phải có đủ Số đầu và Số cuối.";
                    return false;
                }
                if (chieuDai != Math.Abs(soDau.Value - soCuoi.Value))
                {
                    error = $"{maBin}: Chiều dài lô phải bằng ABS(Số đầu - Số cuối).";
                    return false;
                }
            }
            return true;
        }

        private async void btnNhapKho_Click(object sender, EventArgs e)
        {
            if (_dangNhapKho || _dangThayTheGrid) return;
            if (!UserContext.IsAuthenticated
                || (!UserContext.HasRole(RoleNames.Wh) && !UserContext.HasRole(RoleNames.Admin)))
            {
                FrmWaiting.ShowGifAlert("Bạn cần cấp quyền để thực hiện yêu cầu này.");
                return;
            }
            _dangNhapKho = true;
            btnNhapKho.Enabled = false;
            btnTimKiem.Enabled = false;
            cbxTimTenSp.Enabled = false;
            cbxLOT.Enabled = false;
            await _khoaGrid.WaitAsync();
            try
            {
            // Xác nhận ô đang sửa trước khi đọc checkbox và số lượng nhập.
            if (!dsNhapKho.EndEdit()) return;
            dsNhapKho.Enabled = false;
            bool canXuatExcel = cbxXuatExcel.Checked;

            List<DataGridViewRow> selected = dsNhapKho.Rows.Cast<DataGridViewRow>()
                .Where(r => Convert.ToBoolean(r.Cells[CChon].Value ?? false))
                .ToList();
            if (selected.Count == 0)
            {
                FrmWaiting.ShowGifAlert("Chưa có dòng nào được xác nhận nhập kho.");
                return;
            }

            var localErrors = new List<string>();
            foreach (DataGridViewRow row in selected)
                if (!ValidateDongDuocChon(row, out string err)) localErrors.Add(err);
            if (localErrors.Count > 0)
            {
                FrmWaiting.ShowGifAlert(string.Join("\n", localErrors), "DỮ LIỆU KHÔNG HỢP LỆ");
                return;
            }

            string username = UserContext.UserName;

            if (string.IsNullOrWhiteSpace(username))
            {
                FrmWaiting.ShowGifAlert("Không lấy được username của phiên đăng nhập từ UserContext.");
                return;
            }

            // Snapshot dữ liệu UI trước khi chạy background.
            List<BatchGroup> groups = selected
                .GroupBy(r => ParseLong(r.Cells[CTTThanhPhamId].Value))
                .Select(g => TaoBatchGroup(g.Key, g.ToList(), username))
                .ToList();

            // Bản sao tách khỏi DataGridView: grid sẽ được làm mới sau khi Commit.
            DataTable? excelSnapshot = canXuatExcel ? TaoBangXuatExcel(selected) : null;

            List<BatchResult> results;
            try
            {
                results = await WaitingHelper.RunWithWaiting(
                    () => Task.Run(() => XuLyBatch(groups)),
                    "ĐANG LƯU NHẬP KHO, VUI LÒNG ĐỢI...");
            }
            catch (Exception ex)
            {
                FrmWaiting.ShowGifAlert($"Lỗi khi thực hiện nhập kho theo ngày:\n{ex.Message}", "LỖI");
                return;
            }

            int success = results.Count(x => x.Success);
            List<BatchResult> failed = results.Where(x => !x.Success).ToList();
            HashSet<long> successIds = new HashSet<long>(
                results.Where(x => x.Success).Select(x => x.TTThanhPhamId));
            DataTable? excelDaNhap = excelSnapshot == null
                ? null
                : LocDongXuatThanhCong(excelSnapshot, successIds);

            await RefreshNhomDaXuLyAsync(successIds,
                new HashSet<long>(failed.Select(x => x.TTThanhPhamId)),
                new HashSet<Tuple<long, long, int?>>(selected.Select(KhoaChiTiet)));

            string message;
            string icon;

            if (failed.Count == 0)
            {
                message =$"Nhập kho hoàn tất." ;
                icon = EnumStore.Icon.Success;
            }
            else
            {
                message =$"Lỗi: {failed.Count} Mã Bin\n\n" +
                            $"Vui lòng kiểm tra các dòng còn lại trên danh sách.";
                icon = EnumStore.Icon.Warning;
            }

            FrmWaiting.ShowGifAlert(
                message,
                failed.Count == 0 ? "HOÀN TẤT" : "KẾT QUẢ",
                icon
                );

            // Chỉ xuất các dòng thuộc nhóm đã Commit thành công;
            // lỗi/hủy Excel không ảnh hưởng nghiệp vụ nhập kho đã thực hiện.
            if (excelDaNhap != null && excelDaNhap.Rows.Count > 0)
                XuatExcelSauNhapKho(excelDaNhap);
            }
            finally
            {
                _khoaGrid.Release();
                _dangNhapKho = false;
                if (!IsDisposed)
                {
                    dsNhapKho.Enabled = true;
                    btnNhapKho.Enabled = true;
                    btnTimKiem.Enabled = true;
                    cbxTimTenSp.Enabled = true;
                    cbxLOT.Enabled = true;
                }
            }
        }

        // Lấy chuỗi trước dấu '-' cuối cùng của Mã Bin
        private static string LayLotQcTuMaBin(string maBin)
        {
            int viTriDauGachCuoi = maBin.LastIndexOf('-');

            return viTriDauGachCuoi >= 0
                ? maBin.Substring(0, viTriDauGachCuoi)
                : maBin;
        }

        private DataTable TaoBangXuatExcel(IEnumerable<DataGridViewRow> selected)
        {
            var bang = new DataTable();
            foreach (string name in CotXuatExcel)
            {
                bang.Columns.Add(
                    name == CotLotQcExcel
                        ? CotLotQcExcel
                        : dsNhapKho.Columns[name].HeaderText,
                    typeof(string)
                );
            }
            // ID chỉ sử dụng để lọc nhóm đã lưu; không bao giờ đưa vào file.
            bang.Columns.Add(CotIdXuatNoiBo, typeof(long));

            foreach (DataGridViewRow row in selected)
            {
                DataRow dong = bang.NewRow();

                string maBin = Convert.ToString(
                    row.Cells[CMaBin].Value
                ) ?? string.Empty;

                for (int i = 0; i < CotXuatExcel.Length; i++)
                {
                    dong[i] = CotXuatExcel[i] == CotLotQcExcel
                        ? LayLotQcTuMaBin(maBin)
                        : (Convert.ToString(
                            row.Cells[CotXuatExcel[i]].Value
                          ) ?? string.Empty);
                }
                dong[CotIdXuatNoiBo] = ParseLong(row.Cells[CTTThanhPhamId].Value);
                bang.Rows.Add(dong);
            }
            return bang;
        }

        private static DataTable LocDongXuatThanhCong(DataTable snapshot, HashSet<long> successIds)
        {
            // Tạo DataTable mới KHÔNG chứa cột ID nội bộ.
            DataTable daNhap = snapshot.Clone();
            daNhap.Columns.Remove(CotIdXuatNoiBo);
            foreach (DataRow dong in snapshot.Rows)
            {
                if (!successIds.Contains(Convert.ToInt64(dong[CotIdXuatNoiBo])))
                    continue;
                DataRow moi = daNhap.NewRow();
                for (int i = 0; i < CotXuatExcel.Length; i++)
                    moi[i] = dong[i];
                daNhap.Rows.Add(moi);
            }
            return daNhap;
        }

        private void XuatExcelSauNhapKho(DataTable daNhap)
        {
            using var sfd = new SaveFileDialog
            {
                Title = "Xuất báo cáo nhập kho thành phẩm",
                Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                DefaultExt = "xlsx",
                AddExtension = true,
                FileName = $"NhapKhoTP_{DateTime.Now:yyyyMMdd_HHmm}"
            };

            while (true)
            {
                // Hủy chọn đường dẫn là yêu cầu của người dùng, không phải lỗi ghi file.
                if (sfd.ShowDialog(this) != DialogResult.OK)
                {
                    cbxXuatExcel.Checked = false;
                    return;
                }

                try
                {
                    ExcelExporter.ExportToPath(daNhap, sfd.FileName);
                }
                catch (Exception)
                {
                    DialogResult traLoi = MessageBox.Show(
                        this,
                        "Lưu thất bại. Bạn có muốn lưu file excel ở nơi khác hoặc tên khác không?",
                        "Xuất Excel",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);
                    if (traLoi != DialogResult.Yes)
                    {
                        cbxXuatExcel.Checked = false;
                        return;
                    }
                    // Lặp lại hộp chọn tên/nơi lưu, chỉ xuất lại từ bảng đã lưu.
                    continue;
                }

                cbxXuatExcel.Checked = false;
                FrmWaiting.ShowGifAlert("Đã xuất Excel thành công!", "Export", EnumStore.Icon.Success);
                return;
            }
        }

        private BatchGroup TaoBatchGroup(long ttThanhPhamId, List<DataGridViewRow> rows, string username)
        {
            DataGridViewRow first = rows[0];
            var group = new BatchGroup
            {
                TTThanhPhamId = ttThanhPhamId,
                MaBin = Convert.ToString(first.Cells[CMaBin].Value) ?? string.Empty,
                Model = new NhapKho_Model
                {
                    Ngay = DateTime.Now.ToString("yyyy-MM-dd"),
                    SoBB = 0,
                    TTThanhPham_ID = ttThanhPhamId,
                    TenSP = Convert.ToString(first.Cells[CTenSP].Value) ?? string.Empty,
                    // Giữ nguyên nghiệp vụ đã chốt: header lưu snapshot ChieuDaiSau trước lần nhập.
                    SoMet = ParseDouble(first.Cells[CSnapshot].Value),
                    GhiChu = string.Empty,
                    NguoiLam = username
                }
            };

            foreach (DataGridViewRow row in rows)
            {
                group.Details.Add(new ThongTinCuonDay
                {
                    TTCuonDay_CD_ID = ParseLong(row.Cells[CSourceId].Value),
                    TTLo_ID = ParseNullableInt(row.Cells[CTTLoId].Value),
                    SoCuon = int.Parse(Convert.ToString(row.Cells[CSoLuongNhap].Value)),
                    TongChieuDai = int.Parse(Convert.ToString(row.Cells[CChieuDai].Value)),
                    SoDau = ParseNullableInt(row.Cells[CSoDau].Value),
                    soCuoi = ParseNullableInt(row.Cells[CSoCuoi].Value),
                    Ghichu = Convert.ToString(row.Cells[CGhiChu].Value) ?? string.Empty
                });
            }
            return group;
        }

        private static List<BatchResult> XuLyBatch(IEnumerable<BatchGroup> groups)
        {
            var results = new List<BatchResult>();
            foreach (BatchGroup group in groups)
            {
                try
                {
                    NhapKho_DB.NhapKho(group.Model, group.Details);
                    results.Add(new BatchResult { TTThanhPhamId = group.TTThanhPhamId, MaBin = group.MaBin, Success = true });
                }
                catch (Exception ex)
                {
                    results.Add(new BatchResult { TTThanhPhamId = group.TTThanhPhamId, MaBin = group.MaBin, Success = false, Error = ex.Message });
                }
            }
            return results;
        }


        private sealed class KetQuaTaiLaiMaBin
        {
            public long MaBinId { get; set; }

            public List<NhapKhoTheoNgayDong> Items { get; set; }
                = new List<NhapKhoTheoNgayDong>();

            public string Loi { get; set; } = string.Empty;
            public bool CoLoi { get; set; }
        }

        private object GiaTriCot(
            DataGridViewRow row, string tenCot)
        {
            return row.Cells[dsNhapKho.Columns[tenCot].Index].Value;
        }

        private void GanGiaTriCot(
            DataGridViewRow row, string tenCot, object giaTri)
        {
            row.Cells[dsNhapKho.Columns[tenCot].Index].Value = giaTri;
        }

        private static DataGridViewRow SaoChepDongGrid(
            DataGridViewRow nguon)
        {
            var banSao = (DataGridViewRow)nguon.Clone();

            for (int i = 0; i < nguon.Cells.Count; i++)
                banSao.Cells[i].Value = nguon.Cells[i].Value;

            return banSao;
        }

        private DataGridViewRow TaoDongTam(
            NhapKhoTheoNgayDong item)
        {
            var row = new DataGridViewRow();
            row.CreateCells(dsNhapKho);
            row.Height = dsNhapKho.RowTemplate.Height;

            GanGiaTriCot(row, CChon, false);
            GanGiaTriCot(row, CTTThanhPhamId, item.TTThanhPham_ID);
            GanGiaTriCot(row, CSourceId, item.TTCuonDay_CD_ID);
            GanGiaTriCot(row, CTTLoId,
                item.TTLo_ID.HasValue
                    ? (object)item.TTLo_ID.Value : null);
            GanGiaTriCot(row, CSnapshot, item.ChieuDaiSauSnapshot);
            GanGiaTriCot(row, CNgay, item.Ngay);
            GanGiaTriCot(row, CCa, item.Ca);
            GanGiaTriCot(row, CMay, item.May);
            GanGiaTriCot(row, CMaBin, item.MaBin);
            GanGiaTriCot(row, CMaSP, item.MaSP);
            GanGiaTriCot(row, CTenSP, item.TenSP);

            GanGiaTriCot(row, CLoai,
                item.TTLo_ID.HasValue
                    ? (string.IsNullOrWhiteSpace(item.KichThuocLo)
                        ? "Lô" : "Lô " + item.KichThuocLo)
                    : "Cuộn");

            GanGiaTriCot(row, CSoLuongCon, item.SoLuongCon);
            GanGiaTriCot(row, CSoLuongNhap, item.SoLuongCon);
            GanGiaTriCot(row, CChieuDai, item.ChieuDai1Cuon);

            GanGiaTriCot(row, CSoDau,
                item.SoDau.HasValue ? (object)item.SoDau.Value : null);

            GanGiaTriCot(row, CSoCuoi,
                item.SoCuoi.HasValue ? (object)item.SoCuoi.Value : null);

            GanGiaTriCot(row, CGhiChu, item.GhiChu ?? string.Empty);

            GanGiaTriCot(row, CTongNhap,
                (item.SoLuongCon * item.ChieuDai1Cuon)
                .ToString(CultureInfo.InvariantCulture));

            return row;
        }

        private async Task RefreshNhomDaXuLyAsync(
            HashSet<long> successIds,
            HashSet<long> failedIds,
            HashSet<Tuple<long, long, int?>> selectedKeys)
        {
            var handled = new HashSet<long>(successIds);
            handled.UnionWith(failedIds);

            if (handled.Count == 0 || IsDisposed)
                return;

            // 1. Chụp toàn bộ Grid hiện tại.
            List<DataGridViewRow> oldRows =
                dsNhapKho.Rows.Cast<DataGridViewRow>().ToList();

            var oldById = oldRows
                .Where(r => handled.Contains(
                    ParseLong(r.Cells[CTTThanhPhamId].Value)))
                .GroupBy(r =>
                    ParseLong(r.Cells[CTTThanhPhamId].Value))
                .ToDictionary(g => g.Key, g => g.ToList());

            var requests = handled
                .Where(oldById.ContainsKey)
                .Select(id =>
                    new KeyValuePair<long, BoLocNhapKhoTheoNgay>(
                        id,
                        (_boLocNguonTheoMaBin.TryGetValue(
                            id, out BoLocNhapKhoTheoNgay saved)
                            ? saved
                            : (_boLocDaTim ??
                                new BoLocNhapKhoTheoNgay
                                { Ca = "Toàn bộ" })).Copy()))
                .ToList();

            foreach (var request in requests)
                request.Value.TTThanhPhamId = request.Key;

            // 2. Truy vấn tất cả nhóm trước.
            // Chưa thay đổi bất kỳ dòng nào trên Grid.
            List<KetQuaTaiLaiMaBin> queried =
                await WaitingHelper.RunWithWaiting(
                    () => Task.Run(() =>
                    {
                        var ketQua = new List<KetQuaTaiLaiMaBin>();

                        foreach (var request in requests)
                        {
                            var nhom = new KetQuaTaiLaiMaBin
                            {
                                MaBinId = request.Key
                            };

                            try
                            {
                                NhapKhoTheoNgaySearchResult fresh =
                                    TimKiemTheoBoLoc(request.Value);

                                nhom.Items = fresh.Items
                                    .Where(x =>
                                        x.TTThanhPham_ID == request.Key)
                                    .GroupBy(KhoaChiTiet)
                                    .Select(g => g.First())
                                    .ToList();
                            }
                            catch (Exception ex)
                            {
                                nhom.CoLoi = true;
                                nhom.Loi = ex.Message;
                            }

                            ketQua.Add(nhom);
                        }

                        return ketQua;
                    }),
                    "ĐANG CẬP NHẬT DANH SÁCH NHẬP KHO...");

            if (IsDisposed)
                return;

            // 3. Dựng danh sách kết quả trước khi cập nhật UI.
            var resultById = queried
                .ToDictionary(x => x.MaBinId);

            var freshById = queried
                .Where(x => !x.CoLoi)
                .ToDictionary(
                    x => x.MaBinId,
                    x => x.Items.ToDictionary(KhoaChiTiet));

            var lastIndexById = oldRows
                .Select((row, index) => new { row, index })
                .Where(x => handled.Contains(
                    ParseLong(x.row.Cells[CTTThanhPhamId].Value)))
                .GroupBy(x =>
                    ParseLong(x.row.Cells[CTTThanhPhamId].Value))
                .ToDictionary(g => g.Key, g => g.Max(x => x.index));

            var seenKeys =
                new HashSet<Tuple<long, long, int?>>();

            var plannedRows =
                new List<DataGridViewRow>(oldRows.Count);

            var warnings = new List<string>();
            var removeFilterIds = new HashSet<long>();

            foreach (KetQuaTaiLaiMaBin nhom in queried)
            {
                if (nhom.CoLoi)
                {
                    if (successIds.Contains(nhom.MaBinId))
                        removeFilterIds.Add(nhom.MaBinId);

                    warnings.Add(
                        $"Mã Bin ID {nhom.MaBinId}: " +
                        $"không tải lại được ({nhom.Loi}).");
                }
                else if (
                    failedIds.Contains(nhom.MaBinId) &&
                    nhom.Items.Count == 0)
                {
                    warnings.Add(
                        $"Mã Bin ID {nhom.MaBinId}: " +
                        "không còn dữ liệu hợp lệ, cần kiểm tra lại.");
                }
                else if (nhom.Items.Count == 0)
                {
                    removeFilterIds.Add(nhom.MaBinId);
                }
            }

            for (int i = 0; i < oldRows.Count; i++)
            {
                DataGridViewRow old = oldRows[i];
                long id = ParseLong(
                    old.Cells[CTTThanhPhamId].Value);

                // Nhóm chưa xử lý: giữ nguyên dữ liệu.
                if (!resultById.TryGetValue(
                    id, out KetQuaTaiLaiMaBin nhom))
                {
                    plannedRows.Add(SaoChepDongGrid(old));
                    continue;
                }

                // Truy vấn lỗi sau khi nhập.
                if (nhom.CoLoi)
                {
                    if (failedIds.Contains(id))
                    {
                        DataGridViewRow backup =
                            SaoChepDongGrid(old);

                        GanGiaTriCot(backup, CChon, false);
                        plannedRows.Add(backup);
                    }

                    // Nhóm đã ghi kho không được đưa lại
                    // bằng dữ liệu cũ để tránh nhập trùng.
                    continue;
                }

                // Nhóm nhập thất bại và không còn dữ liệu hợp lệ.
                if (failedIds.Contains(id) &&
                    nhom.Items.Count == 0)
                {
                    DataGridViewRow blocked =
                        SaoChepDongGrid(old);

                    GanGiaTriCot(blocked, CChon, false);
                    GanGiaTriCot(blocked, CSoLuongCon, 0);
                    GanGiaTriCot(blocked, CSoLuongNhap, 0);
                    GanGiaTriCot(blocked, CTongNhap, "0");

                    plannedRows.Add(blocked);
                    continue;
                }

                var oldKey = KhoaChiTiet(old);

                if (freshById[id].TryGetValue(
                        oldKey, out NhapKhoTheoNgayDong fresh)
                    && seenKeys.Add(oldKey))
                {
                    DataGridViewRow restored = TaoDongTam(fresh);

                    // Giữ chỉnh sửa chưa nhập, nhưng giới hạn
                    // không vượt số lượng thực tế còn lại.
                    if (!successIds.Contains(id) ||
                        !selectedKeys.Contains(oldKey))
                    {
                        int oldQty =
                            ParseNullableInt(
                                old.Cells[CSoLuongNhap].Value) ?? 0;

                        int validQty =
                            Math.Max(0,
                                Math.Min(oldQty, fresh.SoLuongCon));

                        GanGiaTriCot(
                            restored, CSoLuongNhap, validQty);

                        int? dau =
                            ParseNullableInt(old.Cells[CSoDau].Value);

                        int? cuoi =
                            ParseNullableInt(old.Cells[CSoCuoi].Value);

                        if (!fresh.TTLo_ID.HasValue ||
                            (dau.HasValue && cuoi.HasValue &&
                             Math.Abs(
                                 (long)dau.Value - cuoi.Value)
                             == fresh.ChieuDai1Cuon))
                        {
                            GanGiaTriCot(restored, CSoDau,
                                dau.HasValue
                                    ? (object)dau.Value : null);

                            GanGiaTriCot(restored, CSoCuoi,
                                cuoi.HasValue
                                    ? (object)cuoi.Value : null);
                        }

                        GanGiaTriCot(restored, CTongNhap,
                            (validQty * fresh.ChieuDai1Cuon)
                            .ToString(CultureInfo.InvariantCulture));

                        if (oldQty > fresh.SoLuongCon)
                        {
                            warnings.Add(
                                $"Mã Bin ID {id}: " +
                                "số lượng đã sửa vượt số còn, " +
                                "cần kiểm tra lại.");
                        }
                    }

                    plannedRows.Add(restored);
                }

                // Dòng mới của DB được chèn sau vị trí
                // cuối cùng của nhóm cũ.
                if (lastIndexById[id] == i)
                {
                    foreach (NhapKhoTheoNgayDong item in nhom.Items)
                    {
                        if (seenKeys.Add(KhoaChiTiet(item)))
                            plannedRows.Add(TaoDongTam(item));
                    }
                }
            }

            // 4. Lưu vị trí đang xem.
            int topIndex =
                dsNhapKho.FirstDisplayedScrollingRowIndex;

            int currentIndex =
                dsNhapKho.CurrentCell?.RowIndex ?? -1;

            int currentColumn =
                dsNhapKho.CurrentCell?.ColumnIndex ?? -1;

            int horizontalScroll =
                dsNhapKho.HorizontalScrollingOffset;

            var topKey =
                topIndex >= 0 && topIndex < oldRows.Count
                    ? KhoaChiTiet(oldRows[topIndex]) : null;

            var currentKey =
                currentIndex >= 0 && currentIndex < oldRows.Count
                    ? KhoaChiTiet(oldRows[currentIndex]) : null;

            // 5. Cập nhật đồng loạt, khóa vẽ tạm thời.
            bool redrawDisabled = false;
            _dangApDungLamMoi = true;
            dsNhapKho.SuspendLayout();

            try
            {
                if (dsNhapKho.IsHandleCreated)
                {
                    SendMessage(
                        dsNhapKho.Handle,
                        WM_SETREDRAW,
                        IntPtr.Zero,
                        IntPtr.Zero);

                    redrawDisabled = true;
                }

                dsNhapKho.Rows.Clear();

                if (plannedRows.Count > 0)
                    dsNhapKho.Rows.AddRange(
                        plannedRows.ToArray());

                foreach (long id in removeFilterIds)
                    _boLocNguonTheoMaBin.Remove(id);

                if (dsNhapKho.Rows.Count > 0)
                {
                    int wantedCurrent = currentKey == null
                        ? -1
                        : plannedRows.FindIndex(r =>
                            KhoaChiTietTrongDongTam(r)
                                .Equals(currentKey));

                    if (wantedCurrent < 0 && currentIndex >= 0)
                        wantedCurrent = Math.Min(
                            currentIndex,
                            dsNhapKho.Rows.Count - 1);

                    if (wantedCurrent >= 0 &&
                        currentColumn >= 0 &&
                        currentColumn < dsNhapKho.Columns.Count)
                    {
                        dsNhapKho.CurrentCell =
                            dsNhapKho.Rows[wantedCurrent]
                                .Cells[currentColumn];
                    }

                    int wantedTop = topKey == null
                        ? -1
                        : plannedRows.FindIndex(r =>
                            KhoaChiTietTrongDongTam(r)
                                .Equals(topKey));

                    if (wantedTop < 0 && topIndex >= 0)
                        wantedTop = Math.Min(
                            topIndex,
                            dsNhapKho.Rows.Count - 1);

                    if (wantedTop >= 0)
                    {
                        try
                        {
                            dsNhapKho.FirstDisplayedScrollingRowIndex =
                                wantedTop;
                        }
                        catch (InvalidOperationException)
                        {
                        }
                        catch (ArgumentOutOfRangeException)
                        {
                        }
                    }

                    try
                    {
                        dsNhapKho.HorizontalScrollingOffset =
                            horizontalScroll;
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                    }
                }
                else
                {
                    dsNhapKho.ClearSelection();
                }
            }
            finally
            {
                dsNhapKho.ResumeLayout(true);

                if (redrawDisabled && !dsNhapKho.IsDisposed)
                {
                    SendMessage(
                        dsNhapKho.Handle,
                        WM_SETREDRAW,
                        new IntPtr(1),
                        IntPtr.Zero);

                    dsNhapKho.Invalidate(true);
                }

                _dangApDungLamMoi = false;
            }

            dsNhapKho.Refresh();
            DongBoCbxAllTuGrid();

            if (warnings.Count > 0)
            {
                FrmWaiting.ShowGifAlert(
                    string.Join("\n", warnings),
                    "KIỂM TRA LẠI");
            }
        }

        private Tuple<long, long, int?> KhoaChiTietTrongDongTam(
            DataGridViewRow row)
        {
            return Tuple.Create(
                ParseLong(GiaTriCot(row, CTTThanhPhamId)),
                ParseLong(GiaTriCot(row, CSourceId)),
                ParseNullableInt(GiaTriCot(row, CTTLoId)));
        }

        private static string LayUsernameTuUserContext()
        {
            Type t = typeof(UserContext);
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
            string[] directNames = { "Username", "UserName", "CurrentUsername", "CurrentUserName" };
            foreach (string name in directNames)
            {
                PropertyInfo p = t.GetProperty(name, flags);
                string value = Convert.ToString(p?.GetValue(null))?.Trim();
                if (!string.IsNullOrWhiteSpace(value)) return value;
                FieldInfo f = t.GetField(name, flags);
                value = Convert.ToString(f?.GetValue(null))?.Trim();
                if (!string.IsNullOrWhiteSpace(value)) return value;
            }

            // Nếu UserContext expose object hiện tại thay vì string trực tiếp.
            foreach (string holder in new[] { "Current", "CurrentUser", "User", "Login", "Session" })
            {
                object obj = t.GetProperty(holder, flags)?.GetValue(null) ?? t.GetField(holder, flags)?.GetValue(null);
                if (obj == null) continue;
                Type ot = obj.GetType();
                foreach (string name in new[] { "Username", "UserName", "Name" })
                {
                    string value = Convert.ToString(ot.GetProperty(name)?.GetValue(obj))?.Trim();
                    if (!string.IsNullOrWhiteSpace(value)) return value;
                }
            }
            return string.Empty;
        }

        private sealed class BatchGroup
        {
            public long TTThanhPhamId { get; set; }
            public string MaBin { get; set; } = string.Empty;
            public NhapKho_Model Model { get; set; }
            public List<ThongTinCuonDay> Details { get; } = new List<ThongTinCuonDay>();
        }

        private sealed class BatchResult
        {
            public long TTThanhPhamId { get; set; }
            public string MaBin { get; set; } = string.Empty;
            public bool Success { get; set; }
            public string Error { get; set; } = string.Empty;
        }
    }
}
