using DG_TonKhoBTP_v02.Core;
using DG_TonKhoBTP_v02.Database;
using DG_TonKhoBTP_v02.Dictionary;
using DG_TonKhoBTP_v02.Helper;
using DG_TonKhoBTP_v02.Models;
using DG_TonKhoBTP_v02.UI.Helper;
using DG_TonKhoBTP_v02.UI.ThanhPhamCD;
using System;
using System.Collections.Generic;
using CoreHelper = DG_TonKhoBTP_v02.Helper.Helper;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DG_TonKhoBTP_v02.UI
{
    public partial class UC_TTThanhPham : UserControl, IFormSection, IDataReceiver
    {
        private CancellationTokenSource _searchCts;

        private bool _userNavigatingSuggestions = false;
        private bool _suppressTextChange = false;
        private bool _dangLoadDuLieuBanDau;
        private bool _dangTaiThanhPham;
        public string tenCongDoan { get; set; }
        public CongDoan congDoan;
        private List<BomComponentData> _bomComponents;
        private int _bomLoadVersion;
        private PheLieuData _pheLieuDraft = new PheLieuData();

        public void SetTenCongDoan(string value) => tenCongDoan = value;


        public event Action<string, string> SoLOTChanged;
        public string SoLOTValue => soLOT.Text;


        /// <summary>
        /// Phát khi người dùng chọn một thành phẩm từ danh sách gợi ý.
        /// </summary>
        public event Action<ThanhPhamData> ThanhPhamChanged;

        /// <summary>
        /// Phát khi người dùng thay đổi Khối lượng hoặc Chiều dài.
        /// </summary>
        public event Action<ThanhPhamData> ThanhPhamSoLieuChanged;


        public ThanhPhamData GetThanhPhamData()
        {
            int.TryParse(id.Text, out int danhSachSpId);

            return new ThanhPhamData
            {
                DanhSachSPId = danhSachSpId,
                MaTP = ma.Text,
                TenTP = ten.Text,
                DonVi = donVi.Text,
                KhoiLuong = khoiLuong.Value,
                ChieuDai = chieuDai.Value,
                ChuyenDoi = nbrChuyenDoi.Value,
                GhiChu = GhiChu?.Text ?? string.Empty,
                SoLOT = soLOT?.Text ?? string.Empty,
                TenMay = LayTenMayTuSoLOT(),
                BomComponents = CloneBomComponents(_bomComponents)
            };
        }

        private static List<BomComponentData> CloneBomComponents(
            IEnumerable<BomComponentData> source)
        {
            if (source == null) return null;

            return source.Select(x => new BomComponentData
            {
                ComponentId = x.ComponentId,
                ComponentMa = x.ComponentMa,
                ComponentTen = x.ComponentTen,
                ComponentKieuSP = x.ComponentKieuSP,
                LaNVLBatBuoc = x.LaNVLBatBuoc,
                TyLe = x.TyLe,
                TyLeHoanDoi = x.TyLeHoanDoi
            }).ToList();
        }

        private static PheLieuData ClonePheLieu(PheLieuData source)
        {
            if (source == null) return new PheLieuData();

            return new PheLieuData
            {
                DayPhe_NL = source.DayPhe_NL,
                NhuaPhe_NL = source.NhuaPhe_NL,
                DongPhe_NL = source.DongPhe_NL,
                GhiChuDayPhe_NL = source.GhiChuDayPhe_NL ?? string.Empty,
                GhiChuNhuaPhe_NL = source.GhiChuNhuaPhe_NL ?? string.Empty,
                GhiChuDongPhe_NL = source.GhiChuDongPhe_NL ?? string.Empty,
                DayPhe_TP = source.DayPhe_TP,
                NhuaPhe_TP = source.NhuaPhe_TP,
                DongPhe_TP = source.DongPhe_TP,
                GhiChuDayPhe_TP = source.GhiChuDayPhe_TP ?? string.Empty,
                GhiChuNhuaPhe_TP = source.GhiChuNhuaPhe_TP ?? string.Empty,
                GhiChuDongPhe_TP = source.GhiChuDongPhe_TP ?? string.Empty
            };
        }

        /// <summary>
        /// Trạng thái nhập phế chỉ dựa trên 6 giá trị số.
        /// Ghi chú không được tính là đã nhập phế.
        /// </summary>
        public bool HasPheLieuData => _pheLieuDraft != null && _pheLieuDraft.HasData();

        private void UpdatePheLieuButtonState()
        {
            if (btnNhapPhe == null) return;
            btnNhapPhe.Text = HasPheLieuData ? "Đã nhập" : "Chưa nhập";
        }

        private static void ClearPheLieuNotesIfNoData(PheLieuData data)
        {
            if (data == null || data.HasData()) return;

            // Theo nghiệp vụ: nếu cả 6 giá trị số bằng 0 thì toàn bộ ghi chú
            // không được xem là dữ liệu phế và sẽ bị bỏ qua.
            data.GhiChuDayPhe_NL = string.Empty;
            data.GhiChuNhuaPhe_NL = string.Empty;
            data.GhiChuDongPhe_NL = string.Empty;
            data.GhiChuDayPhe_TP = string.Empty;
            data.GhiChuNhuaPhe_TP = string.Empty;
            data.GhiChuDongPhe_TP = string.Empty;
        }

        private static bool BomComponentsEqual(
            IReadOnlyCollection<BomComponentData> left,
            IReadOnlyCollection<BomComponentData> right)
        {
            if (ReferenceEquals(left, right)) return true;
            if (left == null || right == null) return left == null && right == null;
            if (left.Count != right.Count) return false;

            return left.OrderBy(x => x.ComponentId)
                .Zip(right.OrderBy(x => x.ComponentId), (a, b) =>
                    a.ComponentId == b.ComponentId &&
                    string.Equals(a.ComponentMa, b.ComponentMa, StringComparison.Ordinal) &&
                    string.Equals(a.ComponentTen, b.ComponentTen, StringComparison.Ordinal) &&
                    string.Equals(a.ComponentKieuSP, b.ComponentKieuSP, StringComparison.Ordinal) &&
                    a.LaNVLBatBuoc == b.LaNVLBatBuoc &&
                    a.TyLe == b.TyLe &&
                    a.TyLeHoanDoi == b.TyLeHoanDoi)
                .All(x => x);
        }

        private string LayTenMayTuSoLOT()
        {
            return SoLOTValue?.Split('-')[0] ?? string.Empty;
        }

        private void RaiseThanhPhamChanged()
        {
            if (_dangLoadDuLieuBanDau)
                return;

            ThanhPhamChanged?.Invoke(GetThanhPhamData());
        }

        private void RaiseThanhPhamSoLieuChanged()
        {
            if (_dangLoadDuLieuBanDau)
                return;

            ThanhPhamSoLieuChanged?.Invoke(GetThanhPhamData());
        }


        public UC_TTThanhPham(CongDoan cd)
        {
            InitializeComponent();

            SetTenCongDoan(cd.TenCongDoan);
            congDoan = cd;

            timTenTPCongDoan.KeyDown += timNVL_KeyDown;
            btnNhapPhe.Click += btnNhapPhe_Click;
            UpdatePheLieuButtonState();
        }

        public void CapNhatGhiChuDongGoi(string ghiChu)
        {
            if (GhiChu == null) return;
            GhiChu.Text = ghiChu ?? string.Empty;
        }

        public void FocusKhoiLuong()
        {
            khoiLuong.Focus();
            khoiLuong.Select(0, khoiLuong.Text.Length);
        }

        public void ChonMay(string value)
        {
            may.Text = value;
        }

        private void CapNhatSoLot()
        {
            soLOT.Text = CoreHelper.LOTGenerated(may, maHanhTrinh, sttCongDoan, sttLo, soBin);

            SoLOTChanged?.Invoke(SoLOTValue, may.Text);
        }

        private void maHanhTrinh_ValueChanged(object sender, EventArgs e)
        {
            CapNhatSoLot();
        }

        private void sttCongDoan_SelectedIndexChanged(object sender, EventArgs e)
        {
            CapNhatSoLot();
        }

        private void sttLo_ValueChanged(object sender, EventArgs e)
        {
            CapNhatSoLot();
        }

        private void soBin_ValueChanged(object sender, EventArgs e)
        {
            CapNhatSoLot();
        }

        #region Lấy và load dữ liệu vào form

        public string SectionName => nameof(UC_TTThanhPham);

        public object GetData()
        {
            int.TryParse(id.Text, out int danhSachSpId);

            return new TTThanhPham
            {
                DanhSachSP_ID = danhSachSpId,
                TenTP = ten.Text,
                MaTP = ma.Text,
                DonVi = donVi.Text,
                CongDoan = congDoan,
                MaBin = soLOT?.Text ?? string.Empty,
                KhoiLuongTruoc = (double)khoiLuong.Value,
                KhoiLuongSau = (double)khoiLuong.Value,
                ChieuDaiTruoc = (double)chieuDai.Value,
                ChieuDaiSau = (double)chieuDai.Value,
                GhiChu = GhiChu?.Text ?? string.Empty,
                DateInsert = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                BomComponents = CloneBomComponents(_bomComponents),
                PheLieu = ClonePheLieu(_pheLieuDraft)
            };
        }

        public void ClearInputs()
        {
            bool trangThaiLoadTruocDo = _dangLoadDuLieuBanDau;
            _dangLoadDuLieuBanDau = true;
            try
            {
                _searchCts?.Cancel();

                timTenTPCongDoan.DataSource = null;
                timTenTPCongDoan.Items.Clear();
                timTenTPCongDoan.Text = string.Empty;
                timTenTPCongDoan.DroppedDown = false;

                ResetController_TimTenSP();

                may.SelectedIndex = -1;
                maHanhTrinh.Value = maHanhTrinh.Minimum;
                sttCongDoan.SelectedIndex = -1;
                sttLo.Value = sttLo.Minimum;
                soBin.Value = soBin.Minimum;
                soLOT.Text = string.Empty;
                khoiLuong.Value = khoiLuong.Minimum;
                chieuDai.Value = chieuDai.Minimum;
                _pheLieuDraft = new PheLieuData();
                UpdatePheLieuButtonState();
                GhiChu.Text = string.Empty;
            }
            finally
            {
                _dangLoadDuLieuBanDau = trangThaiLoadTruocDo;
            }
        }

        #endregion

        private async void timNVL_TextUpdate(object sender, EventArgs e)
        {
            if (_suppressTextChange) return;

            string tenTP = timTenTPCongDoan.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(tenTP))
            {
                _userNavigatingSuggestions = false;
                timTenTPCongDoan.DroppedDown = false;
                timTenTPCongDoan.DataSource = null;
                return;
            }

            _searchCts?.Cancel();
            _searchCts?.Dispose();
            _searchCts = new CancellationTokenSource();
            var token = _searchCts.Token;

            try
            {
                await Task.Delay(500, token);
                await ShowDanhSachLuaChon(tenTP, token);
            }
            catch (OperationCanceledException)
            {
            }
            catch
            {
                timTenTPCongDoan.DroppedDown = false;
            }
        }

        private async Task ShowDanhSachLuaChon(string keyword, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                timTenTPCongDoan.DroppedDown = false;
                return;
            }

            string para = "ten";
            string likeConditions = string.Join(" OR ", congDoan.ListMa_Accept.Select(m => $"Ma LIKE '{m}'"));

            string query = $@"
                SELECT id, ten, ma, donvi, chuyenDoi
                FROM DanhSachMaSP
                WHERE ten LIKE '%' || @{para} || '%'
                  AND Active = 1 
                  AND ({likeConditions});
            ";

            DataTable sp = await Task.Run(() =>
            {
                return DatabaseHelper.GetData(query, keyword, para);
            }, ct);

            ct.ThrowIfCancellationRequested();

            // Snapshot text tại thời điểm query xong —
            // dùng timNVL.Text thay vì keyword để bắt kịp ký tự user gõ thêm
            // trong lúc DB đang chạy.
            string currentText = timTenTPCongDoan.Text;

            // Gỡ event trước khi thay đổi DataSource
            timTenTPCongDoan.SelectionChangeCommitted -= timNVL_SelectionChangeCommitted;
            timTenTPCongDoan.TextUpdate -= timNVL_TextUpdate;

            _suppressTextChange = true;
            try
            {
                timTenTPCongDoan.DroppedDown = false;
                timTenTPCongDoan.DataSource = null;

                if (sp == null || sp.Rows.Count == 0)
                {
                    _userNavigatingSuggestions = false;
                    timTenTPCongDoan.Text = currentText;
                    timTenTPCongDoan.SelectionStart = timTenTPCongDoan.Text.Length;
                    timTenTPCongDoan.SelectionLength = 0;
                    return;
                }

                // ── FIX CHÍNH ──────────────────────────────────────────────────────
                // KHÔNG dùng DataSource binding vì với DropDownStyle.DropDown,
                // khi gọi DroppedDown = true WinForms nội bộ sync Text theo
                // DisplayMember của item đang highlight → luôn overwrite text
                // người dùng gõ bằng item[0], bất kể đã set SelectedIndex = -1.
                //
                // Giải pháp: nạp data vào Items trực tiếp (không qua DataSource),
                // lưu DataRowView gốc trong Tag của một wrapper object.
                // Cách này ComboBox không có DisplayMember → không tự sync Text.
                // ───────────────────────────────────────────────────────────────
                timTenTPCongDoan.DisplayMember = "";
                timTenTPCongDoan.ValueMember = "";
                timTenTPCongDoan.DataSource = null;

                timTenTPCongDoan.Items.Clear();
                foreach (DataRow row in sp.Rows)
                {
                    var drv = sp.DefaultView[sp.Rows.IndexOf(row)];
                    timTenTPCongDoan.Items.Add(new DataRowViewWrapper(drv));
                }

                _userNavigatingSuggestions = false;

                // Mở dropdown qua BeginInvoke để tách khỏi stack hiện tại:
                // finally bên dưới sẽ gắn lại event TRƯỚC khi DroppedDown chạy,
                // đảm bảo _suppressTextChange đã = false đúng thời điểm.
                string textToRestore = currentText;
                BeginInvoke((MethodInvoker)(() =>
                {
                    if (ct.IsCancellationRequested) return;

                    _suppressTextChange = true;
                    try
                    {
                        timTenTPCongDoan.SelectedIndex = -1;
                        timTenTPCongDoan.DroppedDown = true;
                        // Set Text SAU DroppedDown — lúc này không còn DisplayMember
                        // nên WinForms không thể overwrite Text theo item nào.
                        timTenTPCongDoan.Text = textToRestore;
                        timTenTPCongDoan.SelectionStart = textToRestore.Length;
                        timTenTPCongDoan.SelectionLength = 0;
                    }
                    finally
                    {
                        _suppressTextChange = false;
                    }

                    Cursor.Current = Cursors.Default;
                    Cursor.Show();
                }));
            }
            finally
            {
                _suppressTextChange = false;
                timTenTPCongDoan.TextUpdate += timNVL_TextUpdate;
                timTenTPCongDoan.SelectionChangeCommitted += timNVL_SelectionChangeCommitted;
            }
        }

        private async Task FillSelectedThanhPhamAsync(DataRowView row)
        {
            if (row == null || _dangTaiThanhPham)
                return;

            if (!int.TryParse(row["id"]?.ToString(), out int selectedProductId) ||
                selectedProductId <= 0)
            {
                return;
            }

            _dangTaiThanhPham = true;
            try
            {
                int loadVersion = Interlocked.Increment(ref _bomLoadVersion);
                List<BomComponentData> candidateBom;
                try
                {
                    candidateBom = await WaitingHelper.RunWithWaiting(
                        () => Task.Run(() =>
                            DatabaseHelper.GetActiveBomComponents(selectedProductId)),
                        "ĐANG TẢI THÔNG TIN THÀNH PHẨM...");
                }
                catch
                {
                    if (loadVersion != _bomLoadVersion)
                        return;

                    FrmWaiting.ShowGifAlert(
                        "Cơ sở dữ liệu đang bận, thử lại sau ít phút");
                    ResetSearchSelectionForRetry();
                    return;
                }

                if (loadVersion != _bomLoadVersion)
                    return;

                string oldId = id.Text;
                string oldMa = ma.Text;
                string oldTen = ten.Text;

                ten.Text = row["ten"]?.ToString() ?? string.Empty;
                ma.Text = row["ma"]?.ToString() ?? string.Empty;
                id.Text = row["id"]?.ToString() ?? string.Empty;
                donVi.Text = row["donvi"]?.ToString() ?? string.Empty;
                nbrChuyenDoi.Value = Convert.ToDecimal(row["chuyenDoi"] ?? 1);
                _bomComponents = candidateBom;

                ResetSearchSelectionForRetry();

                // Chỉ phát trigger khi sản phẩm được chọn thực sự khác ID, mã hoặc tên.
                if (oldId != id.Text || oldMa != ma.Text || oldTen != ten.Text)
                {
                    RaiseThanhPhamChanged();
                }
            }
            finally
            {
                _dangTaiThanhPham = false;
            }
        }

        private void ResetSearchSelectionForRetry()
        {
            _suppressTextChange = true;
            try
            {
                _userNavigatingSuggestions = false;
                timTenTPCongDoan.DroppedDown = false;
                timTenTPCongDoan.SelectedIndex = -1;
                timTenTPCongDoan.DataSource = null;
                timTenTPCongDoan.Items.Clear();
                timTenTPCongDoan.Text = string.Empty;
            }
            finally
            {
                _suppressTextChange = false;
            }

            timTenTPCongDoan.Focus();
        }

        private bool XacNhanTiepTucNeuKhacCongDoanBOM(DataRowView row)
        {
            if (row == null) return true;

            if (!int.TryParse(row["id"]?.ToString(), out int selectedProductId) || selectedProductId <= 0)
                return true;

            int currentCongDoanId = congDoan?.Id ?? 0;
            if (currentCongDoanId <= 0 || currentCongDoanId == 9)
                return true;

            int? congDoanThucTe;
            try
            {
                congDoanThucTe = DatabaseHelper.KiemTraKhacBietCongDoanBOM(selectedProductId, currentCongDoanId);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Không kiểm tra được BOM công đoạn.\n{ex.Message}",
                    "Lỗi kiểm tra BOM",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return false;
            }

            if (!congDoanThucTe.HasValue)
                return true;

            // Theo rule đã chốt: không có BOM thì vẫn cho đi tiếp, không thông báo;
            // khi lưu sẽ ghi KhacBietBOM.CongDoanThucTe = -1.
            if (congDoanThucTe.Value == -1)
                return true;

            string tenSanPham = row["ten"]?.ToString() ?? string.Empty;
            string tenCongDoanHienTai = congDoan?.TenCongDoan ?? currentCongDoanId.ToString();

            string message =
                $"Sản phẩm '{tenSanPham}' không phù hợp với công đoạn hiện tại '{tenCongDoanHienTai}'. Bạn có muốn tiếp tục chọn sản phẩm này không?";

            DialogResult result = MessageBox.Show(
                message,
                "Xác nhận khác biệt BOM",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            return result == DialogResult.Yes;
        }

        private void ClearThanhPhamSelectionForRetry()
        {
            _searchCts?.Cancel();

            _suppressTextChange = true;
            try
            {
                _userNavigatingSuggestions = false;
                timTenTPCongDoan.DroppedDown = false;
                timTenTPCongDoan.SelectedIndex = -1;
                timTenTPCongDoan.DataSource = null;
                timTenTPCongDoan.Items.Clear();
                timTenTPCongDoan.Text = string.Empty;

                ResetController_TimTenSP();
            }
            finally
            {
                _suppressTextChange = false;
            }

            RaiseThanhPhamChanged();
            timTenTPCongDoan.Focus();
        }

        private async void timNVL_SelectionChangeCommitted(object sender, EventArgs e)
        {
            // Items được nạp bằng DataRowViewWrapper (không dùng DataSource binding)
            if (timTenTPCongDoan.SelectedItem is DataRowViewWrapper wrapper)
                await FillSelectedThanhPhamAsync(wrapper.Row);
        }

        private async void timNVL_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Down)
            {
                if (!timTenTPCongDoan.DroppedDown && timTenTPCongDoan.DataSource != null)
                {
                    timTenTPCongDoan.DroppedDown = true;
                }

                if (timTenTPCongDoan.Items.Count > 0)
                {
                    _userNavigatingSuggestions = true;

                    if (timTenTPCongDoan.SelectedIndex < 0)
                        timTenTPCongDoan.SelectedIndex = 0;
                }

                e.Handled = true;
                return;
            }

            if (e.KeyCode == Keys.Enter)
            {
                if (_userNavigatingSuggestions && timTenTPCongDoan.SelectedItem is DataRowViewWrapper wrapper)
                {
                    await FillSelectedThanhPhamAsync(wrapper.Row);
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
            }
        }

        private void ResetController_TimTenSP()
        {
            Interlocked.Increment(ref _bomLoadVersion);
            _bomComponents = null;
            id.Text = string.Empty;
            ma.Text = string.Empty;
            ten.Text = string.Empty;
            donVi.Text = string.Empty;
            nbrChuyenDoi.Value = 1;
        }

        public void LoadData(DataTable dt, int kieuDL)
        {
            _dangLoadDuLieuBanDau = true;
            try
            {
                ResetController_TimTenSP();
                _pheLieuDraft = new PheLieuData();
                UpdatePheLieuButtonState();

                if (dt == null || dt.Rows.Count == 0) return;

                var row = dt.Rows[0];
                string bin = row["MaBin"]?.ToString() ?? string.Empty;

                _bomComponents = null;
                if (dt.ExtendedProperties.ContainsKey(BomDataTableProperties.Loaded) &&
                    Convert.ToBoolean(dt.ExtendedProperties[BomDataTableProperties.Loaded]))
                {
                    _bomComponents = CloneBomComponents(
                        dt.ExtendedProperties[BomDataTableProperties.Components]
                            as IEnumerable<BomComponentData>);
                }

                CoreHelper.SetIfPresent(row, "DanhSachMaSP_ID", val => id.Text = Convert.ToString(val));
                CoreHelper.SetIfPresent(row, "Ma", val => ma.Text = Convert.ToString(val));
                CoreHelper.SetIfPresent(row, "Ten", val => ten.Text = Convert.ToString(val));
                CoreHelper.SetIfPresent(row, "donvi", val => donVi.Text = Convert.ToString(val));
                CoreHelper.SetIfPresent(row, "KhoiLuongTruoc", val => khoiLuong.Value = Convert.ToDecimal(val));
                CoreHelper.SetIfPresent(row, "ChieuDaiTruoc", val => chieuDai.Value = Convert.ToDecimal(val));
                CoreHelper.SetIfPresent(row, "GhiChu", val => GhiChu.Text = Convert.ToString(val));
                CoreHelper.SetIfPresent(row, "ChuyenDoi", val => nbrChuyenDoi.Value = Convert.ToDecimal(val));

                // Sao chép thành phẩm không mang dữ liệu phế sang bản ghi mới.
                _pheLieuDraft = kieuDL == 1
                    ? new PheLieuData()
                    : ReadPheLieuFromRow(row);

                ClearPheLieuNotesIfNoData(_pheLieuDraft);
                UpdatePheLieuButtonState();

                string[] mabin = CoreHelper.CatMaBin(bin);

                if (mabin.Length == 5)
                {
                    maHanhTrinh.Value = Convert.ToDecimal(mabin[1]);
                    sttCongDoan.Text = mabin[2];
                    sttLo.Value = Convert.ToDecimal(mabin[3]);
                    soBin.Value = Convert.ToDecimal(mabin[4]);
                }

                soLOT.Text = bin;
            }
            finally
            {
                _dangLoadDuLieuBanDau = false;
            }
        }

        private static PheLieuData ReadPheLieuFromRow(DataRow row)
        {
            var data = new PheLieuData();

            CoreHelper.SetIfPresent(row, "DayPhe_NL", val => data.DayPhe_NL = Convert.ToDouble(val));
            CoreHelper.SetIfPresent(row, "NhuaPhe_NL", val => data.NhuaPhe_NL = Convert.ToDouble(val));
            CoreHelper.SetIfPresent(row, "DongPhe_NL", val => data.DongPhe_NL = Convert.ToDouble(val));
            CoreHelper.SetIfPresent(row, "GhiChuDayPhe_NL", val => data.GhiChuDayPhe_NL = Convert.ToString(val));
            CoreHelper.SetIfPresent(row, "GhiChuNhuaPhe_NL", val => data.GhiChuNhuaPhe_NL = Convert.ToString(val));
            CoreHelper.SetIfPresent(row, "GhiChuDongPhe_NL", val => data.GhiChuDongPhe_NL = Convert.ToString(val));

            CoreHelper.SetIfPresent(row, "DayPhe_TP", val => data.DayPhe_TP = Convert.ToDouble(val));
            CoreHelper.SetIfPresent(row, "NhuaPhe_TP", val => data.NhuaPhe_TP = Convert.ToDouble(val));
            CoreHelper.SetIfPresent(row, "DongPhe_TP", val => data.DongPhe_TP = Convert.ToDouble(val));
            CoreHelper.SetIfPresent(row, "GhiChuDayPhe_TP", val => data.GhiChuDayPhe_TP = Convert.ToString(val));
            CoreHelper.SetIfPresent(row, "GhiChuNhuaPhe_TP", val => data.GhiChuNhuaPhe_TP = Convert.ToString(val));
            CoreHelper.SetIfPresent(row, "GhiChuDongPhe_TP", val => data.GhiChuDongPhe_TP = Convert.ToString(val));

            return data;
        }

        /// <summary>
        /// Mở form nhập phế. Hàm public để UC_SubmitForm có thể tự mở form
        /// khi người dùng chọn No tại cảnh báo chưa nhập phế liệu.
        /// </summary>
        public void OpenPheLieuForm()
        {
            using (var frm = new Frm_PheLieu(ClonePheLieu(_pheLieuDraft)))
            {
                Form owner = FindForm();
                DialogResult result = owner != null
                    ? frm.ShowDialog(owner)
                    : frm.ShowDialog();

                if (result == DialogResult.OK)
                {
                    _pheLieuDraft = ClonePheLieu(frm.PheLieu);
                    ClearPheLieuNotesIfNoData(_pheLieuDraft);
                    UpdatePheLieuButtonState();
                }
            }
        }

        private void btnNhapPhe_Click(object sender, EventArgs e)
        {
            OpenPheLieuForm();
        }

        private void khoiLuong_ValueChanged(object sender, EventArgs e)
        {
            RaiseThanhPhamSoLieuChanged();
        }

        private void may_TextChanged(object sender, EventArgs e)
        {
            CapNhatSoLot();
        }

        private void chieuDai_ValueChanged(object sender, EventArgs e)
        {
            RaiseThanhPhamSoLieuChanged();
        }
    }

    // ── Wrapper giữ DataRowView nhưng hiển thị cột "ten" trong ComboBox ──────
    // Dùng thay cho DataSource binding để tránh WinForms tự sync Text theo
    // DisplayMember khi DroppedDown = true với DropDownStyle.DropDown.
    internal class DataRowViewWrapper
    {
        public DataRowView Row { get; }
        public DataRowViewWrapper(DataRowView row) => Row = row;
        public override string ToString() => Row["ten"]?.ToString() ?? string.Empty;
    }
}