using DG_TonKhoBTP_v02.Database.ChatLuong;
using DG_TonKhoBTP_v02.Models;
using DG_TonKhoBTP_v02.UI.Helper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DG_TonKhoBTP_v02.UI.NghiepVuKhac.Kho.NhapKho
{
    public partial class Frm_NhapKhoTP_TheoNgay : Form
    {
        private bool _dangDongBoCbxAll;

        private const string CChon = "col_Chon";
        private const string CTTThanhPhamId = "col_TTThanhPham_ID";
        private const string CSourceId = "col_TTCuonDay_CD_ID";
        private const string CTTLoId = "col_TTLo_ID";
        private const string CNgay = "col_Ngay";
        private const string CCa = "col_Ca";
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

        public Frm_NhapKhoTP_TheoNgay()
        {
            InitializeComponent();
            KhoiTaoGrid();
            GanSuKien();
            ResetMacDinh();
        }

        private void KhoiTaoGrid()
        {
            dsNhapKho.AutoGenerateColumns = false;
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
            dsNhapKho.Columns.Add(CreateTextColumn(CMaBin, "Mã Bin", 130));
            dsNhapKho.Columns.Add(CreateTextColumn(CMaSP, "Mã SP", 110));
            dsNhapKho.Columns.Add(CreateTextColumn(CTenSP, "Tên SP", 180));
            dsNhapKho.Columns.Add(CreateTextColumn(CLoai, "Loại", 90));
            dsNhapKho.Columns.Add(CreateTextColumn(CSoLuongCon, "SL còn", 75));
            dsNhapKho.Columns.Add(new DataGridViewTextBoxColumn { Name = CSoLuongNhap, HeaderText = "SL nhập", Width = 80, ReadOnly = false });
            dsNhapKho.Columns.Add(CreateTextColumn(CChieuDai, "CD 1 cuộn/lô", 105));
            dsNhapKho.Columns.Add(CreateTextColumn(CSoDau, "Số đầu", 75));
            dsNhapKho.Columns.Add(CreateTextColumn(CSoCuoi, "Số cuối", 75));
            dsNhapKho.Columns.Add(CreateTextColumn(CTongNhap, "Tổng nhập", 95));
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
            btnNhapKho.Click += btnNhapKho_Click;
            cbxAll.CheckedChanged += cbxAll_CheckedChanged;
            dsNhapKho.CellContentClick += dsNhapKho_CellContentClick;
            dsNhapKho.CellValueChanged += dsNhapKho_CellValueChanged;
            dsNhapKho.CurrentCellDirtyStateChanged += dsNhapKho_CurrentCellDirtyStateChanged;
            dsNhapKho.CellValidating += dsNhapKho_CellValidating;
        }

        private void ResetMacDinh()
        {
            dtNgayBD.Value = DateTime.Today.AddDays(-1);
            dtNgayKT.Value = DateTime.Today;
            cbxCa.SelectedItem = "Toàn bộ";
            if (cbxCa.SelectedIndex < 0 && cbxCa.Items.Count > 0) cbxCa.SelectedIndex = 0;
            _dangDongBoCbxAll = true;
            cbxAll.Checked = false;
            _dangDongBoCbxAll = false;
            dsNhapKho.Rows.Clear();
        }

        private async void btnTimKiem_Click(object sender, EventArgs e)
        {
            if (dtNgayBD.Value.Date > dtNgayKT.Value.Date)
            {
                FrmWaiting.ShowGifAlert("Ngày bắt đầu không được lớn hơn ngày kết thúc.");
                return;
            }

            DateTime bd = dtNgayBD.Value.Date;
            DateTime kt = dtNgayKT.Value.Date;
            string ca = cbxCa.Text;

            try
            {
                NhapKhoTheoNgaySearchResult result = await WaitingHelper.RunWithWaiting(
                    () => Task.Run(() => NhapKho_DB.TimKiemNhapKhoTheoNgay(bd, kt, ca)),
                    "ĐANG TÌM DỮ LIỆU NHẬP KHO...");

                NapGrid(result.Items);

                if (result.MaBinBatThuong.Count > 0)
                {
                    FrmWaiting.ShowGifAlert( "Một số LOT bị loại do lỗi" );

                    Console.WriteLine(string.Join(", ", result.MaBinBatThuong));
                }
                else if (result.Items.Count == 0)
                {
                    FrmWaiting.ShowGifAlert("Không tìm thấy Mã Bin còn dữ liệu phù hợp với điều kiện tìm kiếm.");
                }
            }
            catch (Exception ex)
            {
                FrmWaiting.ShowGifAlert($"Lỗi khi tìm dữ liệu nhập kho theo ngày:\n{ex.Message}", "LỖI");
            }
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

        private void dsNhapKho_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            string name = dsNhapKho.Columns[e.ColumnIndex].Name;
            if (name == CChon && !_dangDongBoCbxAll) DongBoCbxAllTuGrid();
            if (name == CSoLuongNhap) CapNhatTongDong(dsNhapKho.Rows[e.RowIndex]);
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
                dsNhapKho.Rows.RemoveAt(e.RowIndex);
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
            if (!UserContext.IsAuthenticated
                || (!UserContext.HasRole(RoleNames.Wh) && !UserContext.HasRole(RoleNames.Admin)))
            {
                FrmWaiting.ShowGifAlert("Bạn cần cấp quyền để thực hiện yêu cầu này.");
                return;
            }

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

            string username = LayUsernameTuUserContext();
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

            if (failed.Count == 0)
            {
                ResetMacDinh();
            }
            else
            {
                HashSet<long> successIds = new HashSet<long>(results.Where(x => x.Success).Select(x => x.TTThanhPhamId));
                for (int i = dsNhapKho.Rows.Count - 1; i >= 0; i--)
                    if (successIds.Contains(ParseLong(dsNhapKho.Rows[i].Cells[CTTThanhPhamId].Value)))
                        dsNhapKho.Rows.RemoveAt(i);

                await RefreshMaBinLoi(new HashSet<long>(failed.Select(x => x.TTThanhPhamId)));
            }

            string message;

            if (failed.Count == 0)
            {
                message =$"Nhập kho hoàn tất." ;
            }
            else
            {
                message =$"Lỗi: {failed.Count} Mã Bin\n\n" +
                            $"Vui lòng kiểm tra các dòng còn lại trên danh sách.";
            }

            FrmWaiting.ShowGifAlert(
                message,
                failed.Count == 0 ? "HOÀN TẤT" : "KẾT QUẢ");
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

        private async Task RefreshMaBinLoi(HashSet<long> failedIds)
        {
            DateTime bd = dtNgayBD.Value.Date;
            DateTime kt = dtNgayKT.Value.Date;
            string ca = cbxCa.Text;

            NhapKhoTheoNgaySearchResult fresh;
            try
            {
                fresh = await WaitingHelper.RunWithWaiting(
                    () => Task.Run(() => NhapKho_DB.TimKiemNhapKhoTheoNgay(bd, kt, ca)),
                    "ĐANG CẬP NHẬT LẠI CÁC MÃ BIN LỖI...");
            }
            catch
            {
                // Nếu không refresh được, giữ snapshot lỗi cũ; không reset điều kiện tìm kiếm.
                foreach (DataGridViewRow row in dsNhapKho.Rows)
                    if (failedIds.Contains(ParseLong(row.Cells[CTTThanhPhamId].Value)))
                        row.Cells[CChon].Value = false;
                DongBoCbxAllTuGrid();
                return;
            }

            // Xóa snapshot cũ của Mã Bin lỗi, nhưng giữ nguyên các dòng chưa chọn của Mã Bin khác.
            var oldById = dsNhapKho.Rows.Cast<DataGridViewRow>()
                .Where(r => failedIds.Contains(ParseLong(r.Cells[CTTThanhPhamId].Value)))
                .GroupBy(r => ParseLong(r.Cells[CTTThanhPhamId].Value))
                .ToDictionary(g => g.Key, g => g.Select(CloneDisplay).ToList());

            for (int i = dsNhapKho.Rows.Count - 1; i >= 0; i--)
                if (failedIds.Contains(ParseLong(dsNhapKho.Rows[i].Cells[CTTThanhPhamId].Value)))
                    dsNhapKho.Rows.RemoveAt(i);

            foreach (long id in failedIds)
            {
                List<NhapKhoTheoNgayDong> freshRows = fresh.Items.Where(x => x.TTThanhPham_ID == id).ToList();
                if (freshRows.Count > 0)
                {
                    foreach (var item in freshRows) ThemDong(item, false);
                }
                else if (oldById.TryGetValue(id, out List<NhapKhoTheoNgayDong> oldRows))
                {
                    // DB không còn trả MaBin này (ví dụ đã hết hàng/đã trở thành bất thường).
                    // Giữ để người dùng thấy Mã Bin lỗi, nhưng vô hiệu số lượng nhập.
                    foreach (var item in oldRows)
                    {
                        item.SoLuongCon = 0;
                        ThemDong(item, false);
                        dsNhapKho.Rows[dsNhapKho.Rows.Count - 1].Cells[CSoLuongNhap].Value = 0;
                        CapNhatTongDong(dsNhapKho.Rows[dsNhapKho.Rows.Count - 1]);
                    }
                }
            }
            DongBoCbxAllTuGrid();
        }

        private NhapKhoTheoNgayDong CloneDisplay(DataGridViewRow row)
        {
            return new NhapKhoTheoNgayDong
            {
                TTThanhPham_ID = ParseLong(row.Cells[CTTThanhPhamId].Value),
                Ngay = Convert.ToString(row.Cells[CNgay].Value) ?? string.Empty,
                Ca = Convert.ToString(row.Cells[CCa].Value) ?? string.Empty,
                MaBin = Convert.ToString(row.Cells[CMaBin].Value) ?? string.Empty,
                MaSP = Convert.ToString(row.Cells[CMaSP].Value) ?? string.Empty,
                TenSP = Convert.ToString(row.Cells[CTenSP].Value) ?? string.Empty,
                ChieuDaiSauSnapshot = ParseDouble(row.Cells[CSnapshot].Value),
                TTCuonDay_CD_ID = ParseLong(row.Cells[CSourceId].Value),
                TTLo_ID = ParseNullableInt(row.Cells[CTTLoId].Value),
                SoLuongCon = int.TryParse(Convert.ToString(row.Cells[CSoLuongCon].Value), out int sc) ? sc : 0,
                ChieuDai1Cuon = int.TryParse(Convert.ToString(row.Cells[CChieuDai].Value), out int cd) ? cd : 0,
                SoDau = ParseNullableInt(row.Cells[CSoDau].Value),
                SoCuoi = ParseNullableInt(row.Cells[CSoCuoi].Value),
                GhiChu = Convert.ToString(row.Cells[CGhiChu].Value) ?? string.Empty
            };
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

        // Giữ event cũ của Designer để không làm thay đổi bố cục/form ngoài phạm vi chức năng.
        private void label3_Click(object sender, EventArgs e) { }
    }
}
