using DG_TonKhoBTP_v02.Core;
using DG_TonKhoBTP_v02.Database;
using DG_TonKhoBTP_v02.Database.Kho;
using DG_TonKhoBTP_v02.Database.Kho.TonKho;
using DG_TonKhoBTP_v02.Models.Kho.TonKho;
using DG_TonKhoBTP_v02.UI.Helper.AutoSearchWithCombobox;
using DG_TonKhoBTP_v02.Dictionary;
using DG_TonKhoBTP_v02.Helper;
using DG_TonKhoBTP_v02.Models;
using DG_TonKhoBTP_v02.Models.Kho;
using DG_TonKhoBTP_v02.Printer.Kho;
using DG_TonKhoBTP_v02.UI.Helper;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using CheckBox = System.Windows.Forms.CheckBox;
using Control = System.Windows.Forms.Control;
using CoreHelper = DG_TonKhoBTP_v02.Helper.Helper;

namespace DG_TonKhoBTP_v02.UI
{
    public partial class UC_BCTonKho : UserControl
    {

        private CancellationTokenSource _searchCts;
        private ComboBoxSearchHelper _tonKhoSearchHelper;
        private CancellationTokenSource _tonKhoLoadCts;
        private int _tonKhoVersion;
        private BCTonKho_GoiY _tonKhoSelection;
        private bool _showingInventory;

        public UC_BCTonKho()
        {
            InitializeComponent();
            cbxKeoRut.Tag = ThongTinChungCongDoan.KeoRut;
            cbxBen.Tag = ThongTinChungCongDoan.BenRuot;
            cbxQuanMica.Tag = ThongTinChungCongDoan.Mica;
            cbxBocCachDien.Tag = ThongTinChungCongDoan.BocMach;
            cbxGhepLoi.Tag = ThongTinChungCongDoan.GhepLoi;
            cbxBocLot.Tag = ThongTinChungCongDoan.BocLot;
            cbxQuanBang.Tag = ThongTinChungCongDoan.QuanBang;
            cbxBocTP.Tag = ThongTinChungCongDoan.BocVo;
            InitializeTonKhoSearch();
        }

        private void InitializeTonKhoSearch()
        {
            _tonKhoSearchHelper = new ComboBoxSearchHelper(btnTimKiem,
                (keyword, token) => BC_TonKho_DB.TimGoiYAsync(keyword, cbxKieuTon.Checked, token))
            {
                DisplayColumn = "GiaTri",
                SelectedTextBehavior = ComboBoxSelectedTextBehavior.FillDisplayText,
                DebounceMs = 400
            };
            _tonKhoSearchHelper.ItemSelected += OnTonKhoItemSelected;
            _tonKhoSearchHelper.Cleared += OnTonKhoCleared;
            btnTimKiem.TextUpdate += OnTonKhoTextUpdate;
            cbxKieuTon.CheckedChanged += OnTonKhoFilterChanged;
            Disposed += OnTonKhoDisposed;
        }

        private void CancelTonKhoRequest()
        {
            unchecked { _tonKhoVersion++; }
            _tonKhoLoadCts?.Cancel();
            // Không Dispose CTS trước khi callback truy vấn cũ hoàn tất.
            _tonKhoLoadCts = null;
        }

        private void OnTonKhoTextUpdate(object sender, EventArgs e)
        {
            _tonKhoSelection = null;
            CancelTonKhoRequest();
            if (_showingInventory) ClearTonKhoGrid();
        }

        private void OnTonKhoCleared()
        {
            _tonKhoSelection = null;
            CancelTonKhoRequest();
            if (_showingInventory) ClearTonKhoGrid();
        }

        private async void OnTonKhoItemSelected(DataRowView row)
        {
            if (row == null) return;
            _tonKhoSelection = new BCTonKho_GoiY
            {
                Loai = (BCTonKho_LoaiTimKiem)Convert.ToInt32(row["Loai"]),
                Id = row["Id"] == DBNull.Value ? (long?)null : Convert.ToInt64(row["Id"]),
                GiaTri = Convert.ToString(row["GiaTri"]) ?? string.Empty
            };
            await ReloadTonKhoAsync();
        }

        private async void OnTonKhoFilterChanged(object sender, EventArgs e)
        {
            CancelTonKhoRequest();
            if (_tonKhoSelection != null)
                await ReloadTonKhoAsync();
            // Nếu chưa chọn, tìm kiếm tiếp sẽ dùng giá trị checkbox mới.
        }

        private async Task ReloadTonKhoAsync()
        {
            if (_tonKhoSelection == null || IsDisposed || Disposing) return;
            CancelTonKhoRequest();
            int version = _tonKhoVersion;
            var choice = _tonKhoSelection;
            bool includeDepleted = cbxKieuTon.Checked;
            var cts = new CancellationTokenSource();
            _tonKhoLoadCts = cts;
            try
            {
                var result = await WaitingHelper.RunWithWaiting(() =>
                    BC_TonKho_DB.LayChiTietTonAsync(choice.Loai, choice.Id, choice.GiaTri,
                        includeDepleted, cts.Token), "ĐANG TẢI TỒN KHO...");
                if (cts.IsCancellationRequested || version != _tonKhoVersion ||
                    IsDisposed || Disposing || _tonKhoSelection != choice || cbxKieuTon.Checked != includeDepleted)
                    return;
                RenderTonKho(result);
                if (result.CoBatThuong)
                    FrmWaiting.ShowGifAlert("Có dữ liệu bất thường.", "THÔNG BÁO", EnumStore.Icon.Warning);
            }
            catch (OperationCanceledException) { /* Lựa chọn mới thay thế truy vấn cũ. */ }
            catch (Exception ex)
            {
                if (version == _tonKhoVersion && !IsDisposed && !Disposing)
                    FrmWaiting.ShowGifAlert("Không thể tải tồn kho: " + ex.Message, "LỖI", EnumStore.Icon.Warning);
            }
            finally
            {
                if (_tonKhoLoadCts == cts) _tonKhoLoadCts = null;
                cts.Dispose();
            }
        }

        private void ClearTonKhoGrid()
        {
            if (!_showingInventory) return;
            grvShowBaoCao.DataSource = null;
            grvShowBaoCao.Rows.Clear();
            grvShowBaoCao.Columns.Clear();
            _showingInventory = false;
        }

        private void RenderTonKho(BCTonKho_KetQua result)
        {
            grvShowBaoCao.DataSource = null;
            grvShowBaoCao.Columns.Clear();
            grvShowBaoCao.AutoGenerateColumns = false;
            grvShowBaoCao.AllowUserToAddRows = false;
            grvShowBaoCao.ReadOnly = true;
            grvShowBaoCao.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells;
            grvShowBaoCao.Columns.Add("TenSanPham", "Tên sản phẩm");
            grvShowBaoCao.Columns.Add("MaBinLot", "Mã Bin/LOT");
            grvShowBaoCao.Columns.Add("SoLuong", "Số lượng");
            grvShowBaoCao.Columns.Add("Kieu", "Kiểu");
            grvShowBaoCao.Columns.Add("SoDau", "Số đầu");
            grvShowBaoCao.Columns.Add("SoCuoi", "Số cuối");
            grvShowBaoCao.Columns.Add("ChieuDaiBanDau", "Chiều dài ban đầu");
            grvShowBaoCao.Columns.Add("ChieuDaiConLai", "Chiều dài còn lại");
            grvShowBaoCao.Columns.Add("DatCho", "Đặt chỗ");
            grvShowBaoCao.Columns.Add(new DataGridViewButtonColumn
            {
                Name = "LichSu", HeaderText = "Lịch sử", Text = "Xem", UseColumnTextForButtonValue = true
            });
            foreach (var item in result.Dong)
            {
                var displayReservation = item.DatCho.ToString("N0") + " " + item.DonViDatCho
                    + (item.DatChoDuKien && item.DatCho != 0 ? " (dự kiến)" : "");
                int index = grvShowBaoCao.Rows.Add(item.TenSanPham, item.MaHienThi,
                    item.SoLuong, item.Kieu == BCTonKho_KieuDong.CuonChan ? "Cuộn chẵn" : "Cuộn lẻ",
                    item.SoDau.HasValue ? item.SoDau.Value.ToString("N0") : "",
                    item.SoCuoi.HasValue ? item.SoCuoi.Value.ToString("N0") : "",
                    item.ChieuDaiBanDau.HasValue ? item.ChieuDaiBanDau.Value.ToString("N0") : "",
                    item.ChieuDaiConLai.HasValue ? item.ChieuDaiConLai.Value.ToString("N0") : "",
                    displayReservation, "Xem");
                var gridRow = grvShowBaoCao.Rows[index];
                gridRow.Tag = item; // ID nguồn để sau này phát triển nút Xem.
                if (item.LaBatThuong)
                {
                    var danger = System.Drawing.Color.FromArgb(177, 48, 59);
                    gridRow.DefaultCellStyle.BackColor = danger;
                    gridRow.DefaultCellStyle.ForeColor = System.Drawing.Color.White;
                    gridRow.DefaultCellStyle.SelectionBackColor = danger;
                    gridRow.DefaultCellStyle.SelectionForeColor = System.Drawing.Color.White;
                    foreach (DataGridViewCell cell in gridRow.Cells) cell.ToolTipText = item.LyDoBatThuong;
                }
            }
            _showingInventory = true;
        }

        private void OnTonKhoDisposed(object sender, EventArgs e)
        {
            CancelTonKhoRequest();
            if (_tonKhoSearchHelper != null)
            {
                _tonKhoSearchHelper.ItemSelected -= OnTonKhoItemSelected;
                _tonKhoSearchHelper.Cleared -= OnTonKhoCleared;
                _tonKhoSearchHelper.Dispose();
                _tonKhoSearchHelper = null;
            }
            btnTimKiem.TextUpdate -= OnTonKhoTextUpdate;
            cbxKieuTon.CheckedChanged -= OnTonKhoFilterChanged;
        }

        private async Task ShowDanhSachLuaChon(string keyword, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                cbxTimMaBin.DroppedDown = false;
                return;
            }
            string para = "key";

            string query = $@"
                SELECT 
                    TTThanhPham.MaBin,
                    TTThanhPham.KhoiLuongSau
                FROM 
                    TTThanhPham                
                WHERE 
                    TTThanhPham.Temp = 0
                    AND TTThanhPham.KhoiLuongSau <> 0
                    AND TTThanhPham.MaBin LIKE '%' || @{para} || '%';
            ";


            DataTable sp = await Task.Run(() =>
            {
                return DatabaseHelper.GetData(query, keyword, para);
            }, ct);

            ct.ThrowIfCancellationRequested();

            cbxTimMaBin.DroppedDown = false;

            cbxTimMaBin.SelectionChangeCommitted -= cbxTimMaBin_SelectionChangeCommitted; // tránh trùng event
            if (sp.Rows.Count == 0) return;

            cbxTimMaBin.DataSource = sp;
            cbxTimMaBin.DisplayMember = "MaBin";

            string currentText = keyword;

            cbxTimMaBin.DroppedDown = true;
            cbxTimMaBin.Text = currentText;
            cbxTimMaBin.SelectionStart = cbxTimMaBin.Text.Length;
            cbxTimMaBin.SelectionLength = 0;

            cbxTimMaBin.SelectionChangeCommitted += cbxTimMaBin_SelectionChangeCommitted;
        }

        private void cbxTimMaBin_SelectionChangeCommitted(object sender, EventArgs e)
        {
            if (cbxTimMaBin.SelectedItem == null || !(cbxTimMaBin.SelectedItem is DataRowView)) return;
            DataRowView row = (DataRowView)cbxTimMaBin.SelectedItem;

            tbMaBin.Text = row["MaBin"].ToString();
            klHienTai.Text = row["KhoiLuongSau"].ToString();

            cbxTimMaBin.SelectedIndex = -1;
            cbxTimMaBin.Text = string.Empty;
        }

        private async void cbxTimMaBin_TextUpdate(object sender, EventArgs e)
        {
            ClearForm();
            string tenTP = cbxTimMaBin.Text;
            if (string.IsNullOrEmpty(tenTP)) return;

            // --- thêm debounce + cancel ---
            _searchCts?.Cancel();
            _searchCts = new CancellationTokenSource();
            var token = _searchCts.Token;

            try
            {
                // debounce: đợi user dừng gõ 250ms mới chạy
                await Task.Delay(500, token);

                // gọi async thay vì sync
                await ShowDanhSachLuaChon(tenTP, token);
            }
            catch (OperationCanceledException)
            {
                // bị huỷ vì user gõ tiếp, bỏ qua
            }
        }

        private void klHienTai_ValueChanged(object sender, EventArgs e)
        {
            //TinhTonKho();
        }

        private void klBanTran_ValueChanged(object sender, EventArgs e)
        {
            TinhTonKho();
        }

        private void klConLai_ValueChanged(object sender, EventArgs e)
        {
            //TinhTonKho();
        }

        private void TinhTonKho()
        {
            decimal klHienTaiValue = klHienTai.Value;
            decimal klBanTranValue = klBanTran.Value;
            decimal klConLaiValue = klHienTaiValue - klBanTranValue < 0 ? 0 : klHienTaiValue - klBanTranValue;
            klConLai.Value = klConLaiValue;
        }

        private void ClearForm()
        {
            tbMaBin.Text = string.Empty;
            klHienTai.Value = 0;
            klBanTran.Value = 0;
            klConLai.Value = 0;
        }

        private async void btnLuu_Click(object sender, EventArgs e)
        {
            // Kiểm tra xem database có sẵn dùng không
            if (CoreHelper.TaoThongBao() != "") return;

            // Kiểm tra quyền
            //string k = EnumStore.Group["CapNhatTonKho"];
            //string per = "CAN_UPDATE";

            //if (!CoreHelper.CheckLoginAndPermission(k, per)) return;

            if (string.IsNullOrWhiteSpace(tbMaBin.Text))
            {
                FrmWaiting.ShowGifAlert($"VUI LÒNG CHỌN MÃ BIN");
                return;
            }

            var bt = new BanTran
            {
                MaBin = tbMaBin.Text,
                KhoiLuongSau = (double)klConLai.Value,
                KhoiLuongBanTran = (double)klBanTran.Value
            };

            btnLuu.Enabled = false;

            try
            {
                // Trả về kết quả từ WaitingHelper
                string message = await WaitingHelper.RunWithWaiting(async () =>
                {
                    // Chạy cập nhật nặng ở thread pool
                    return await Task.Run(() => DatabaseHelper.UpdateKLConLai_BanTran(bt));
                }, "ĐANG CẬP NHẬT DỮ LIỆU...");

                // >>>> HIỂN THỊ MESSAGEBOX SAU KHI WAITING FORM ĐÃ ĐÓNG <<
                string icon = EnumStore.Icon.Warning;
                if (string.IsNullOrEmpty(message))
                {
                    message = "THAO TÁC THÀNH CÔNG";
                    icon = EnumStore.Icon.Success;
                    ClearForm();
                }                

                FrmWaiting.ShowGifAlert(message, "THÔNG BÁO", icon);
            }
            catch (Exception ex)
            {
                FrmWaiting.ShowGifAlert($"Có lỗi xảy ra: {ex.Message}");
            }
            finally
            {
                btnLuu.Enabled = true;
            }
        }


        private void cbxAllSelected_CheckedChanged(object sender, EventArgs e)
        {
            bool isChecked = cbxAllSelected.Checked;

            foreach (Control control in tbCheckBox.Controls)
            {
                // Kiểm tra nếu control là CheckBox
                if (control is CheckBox checkbox && checkbox != cbxBaoCaoTon)
                {
                    checkbox.Checked = isChecked;
                }
            }
        }

        private async void btnBCSX_Click(object sender, EventArgs e)
        {
            CancelTonKhoRequest();
            DateTime batDau = dtBatDau.Value;
            DateTime ketThuc = dtKetThuc.Value;

            if (ketThuc <= batDau)
            {
                FrmWaiting.ShowGifAlert("Thời gian kết thúc phải lớn hơn thời gian bắt đầu!");
                return;
            }

            var selectedCongDoans = CoreHelper.GetCheckedCongDoans(tbCheckBox);
            if (selectedCongDoans == null || selectedCongDoans.Count == 0)
            {
                FrmWaiting.ShowGifAlert("Vui lòng chọn ít nhất một công đoạn!");
                return;
            }

            string fileName = selectedCongDoans.Count == 1
                ? $"Report_{selectedCongDoans[0].TenCongDoan}"
                : "Report_MultiCongDoan";

            try
            {
                await WaitingHelper.RunWithWaiting(async () =>
                {
                    DataTable dt = await Task.Run(() =>
                    {
                        if (cbxBaoCaoTon.Checked)
                            return DatabaseHelper.GetTonKhoCD(selectedCongDoans);
                        else
                            return DatabaseHelper.GetDataBaoCaoSX(batDau, ketThuc, selectedCongDoans);
                    });

                    if (dt == null || dt.Rows.Count == 0)
                    {
                        FrmWaiting.ShowGifAlert("KHÔNG CÓ DỮ LIỆU");
                        return;
                    }

                    if (btnXuatExcel.Checked)
                    {
                        try
                        {
                            ExportExcelFile(dt, fileName);
                        }
                        catch (UnauthorizedAccessException ex)
                        {
                            FrmWaiting.ShowGifAlert(ex.Message, "THÔNG BÁO", EnumStore.Icon.Warning);
                        }
                        catch (Exception ex)
                        {
                            FrmWaiting.ShowGifAlert($"Lỗi khi xuất Excel: {ex.Message}", "THÔNG BÁO", EnumStore.Icon.Warning);
                        }
                    }
                    else
                    {
                        ShowResult(dt);
                    }
                });
            }
            catch (Exception ex)
            {
                FrmWaiting.ShowGifAlert($"Có lỗi xảy ra: {ex.Message}", "LỖI", EnumStore.Icon.Warning);
            }
        }


        private void ShowResult(DataTable master)
        {
            CancelTonKhoRequest();
            _tonKhoSelection = null;
            _tonKhoSearchHelper?.Reset();
            ClearTonKhoGrid();
            grvShowBaoCao.AutoGenerateColumns = true;
            grvShowBaoCao.DataSource = master;
            grvShowBaoCao.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells;
            grvShowBaoCao.AllowUserToAddRows = false;
            grvShowBaoCao.ReadOnly = true;
        }

        private void ExportExcelFile(DataTable master, string defaultFileName)
        {
            if (!UserContext.IsAuthenticated)
                throw new UnauthorizedAccessException(EnumStore.ThongBao.YeuCauDangNhap);

            ExcelExporter.Export(master, defaultFileName);
        }

        private async void btnTonDong_Click(object sender, EventArgs e)
        {
            CancelTonKhoRequest();
            _tonKhoSelection = null;
            _tonKhoSearchHelper?.Reset();
            ClearTonKhoGrid();
            grvShowBaoCao.DataSource = null;

            string query = @"
        SELECT 
            TTThanhPham.MaBin,
            DanhSachMaSP.Ma,
            DanhSachMaSP.Ten,
            DanhSachMaSP.DonVi,
            TTThanhPham.KhoiLuongTruoc as KLTruoc,
            TTThanhPham.KhoiLuongSau as KLSau,
            TTThanhPham.ChieuDaiTruoc as CDTruoc,
            TTThanhPham.ChieuDaiSau as CDSau,
            TTThanhPham.KLBanTran,
            TTThanhPham.GhiChu
        FROM TTThanhPham
        INNER JOIN DanhSachMaSP 
            ON TTThanhPham.DanhSachSP_ID = DanhSachMaSP.id
        WHERE 
            TTThanhPham.Temp = 0
            AND (
                (DanhSachMaSP.DonVi = 'KG' AND TTThanhPham.KhoiLuongSau <> 0)
                OR
                (DanhSachMaSP.DonVi = 'M' AND TTThanhPham.ChieuDaiSau <> 0)
            )
            AND (DanhSachMaSP.Ten LIKE 'C %R%' OR DanhSachMaSP.Ten LIKE 'C-AWG %' 
                OR DanhSachMaSP.Ten = 'C 1.02' OR DanhSachMaSP.Ten = 'C 1.20' 
                OR DanhSachMaSP.Ten LIKE 'A %R%')
            AND DanhSachMaSP.Ten NOT LIKE '%/T' 
            AND TTThanhPham.MaBin NOT LIKE 'R%-%' 
            AND TTThanhPham.MaBin NOT LIKE 'MD%-%'
        ORDER BY TTThanhPham.id DESC";

            string col = null;
            string fileName = "BaoCaoTonKho_" + DateTime.Now.ToString("ddMMMyyyy");

            try
            {
                await WaitingHelper.RunWithWaiting(async () =>
                {
                    DataTable dt = await Task.Run(() => DatabaseHelper.GetData(query, col, "KieuSP"));

                    if (dt == null || dt.Rows.Count == 0)
                    {
                        FrmWaiting.ShowGifAlert("KHÔNG CÓ DỮ LIỆU");
                        return;
                    }

                    if (btnXuatExcel.Checked)
                    {
                        try
                        {
                            ExportExcelFile(dt, fileName);
                        }
                        catch (UnauthorizedAccessException ex)
                        {
                            FrmWaiting.ShowGifAlert(ex.Message, "THÔNG BÁO", EnumStore.Icon.Warning);
                        }
                        catch (Exception ex)
                        {
                            FrmWaiting.ShowGifAlert($"Lỗi khi xuất Excel: {ex.Message}", "THÔNG BÁO", EnumStore.Icon.Warning);
                        }
                    }
                    else
                    {
                        ShowResult(dt);
                    }
                });
            }
            catch (Exception ex)
            {
                FrmWaiting.ShowGifAlert($"Có lỗi xảy ra: {ex.Message}", "LỖI", EnumStore.Icon.Warning);
            }
        }

        private void cbxBaoCaoTon_Click(object sender, EventArgs e)
        {
            dtBatDau.Enabled = !cbxBaoCaoTon.Checked;
            dtKetThuc.Enabled = !cbxBaoCaoTon.Checked;

        }


        private void btnTaoBC_Khach_Click(object sender, EventArgs e)
        {
            using var dialog = new SaveFileDialog
            {
                Filter = "Word Document (*.docx)|*.docx",
                FileName = $"TồnKhoĐG_{DateTime.Now:yyyyMMdd_HHmmss}.docx"
            };

            if (dialog.ShowDialog() != DialogResult.OK)
                return;

            try
            {
                TonKhoLoReport report = BaoCaoTonKhoLo_DB.LayBaoCaoTonKhoLo();

                if (!report.HasData)
                {
                    FrmWaiting.ShowGifAlert("KHÔNG CÓ DỮ LIỆU TỒN KHO LOẠI LÔ");
                    return;
                }

                TonKhoLoReportDocxWriter.Write(report, dialog.FileName);

                FrmWaiting.ShowGifAlert("Xuất file thành công!", "Thông báo", EnumStore.Icon.Success);
            }
            catch (Exception ex)
            {
                FrmWaiting.ShowGifAlert($"Lỗi: {ex.Message}", "Lỗi", EnumStore.Icon.Warning);
            }
        }


        private async void btnXemTonKho_Click(object sender, EventArgs e)
        {
            using var dialog = new SaveFileDialog
            {
                Title = "Xuất báo cáo tồn kho Excel",
                Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                FileName = $"TonKho_DG_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
            };

            if (dialog.ShowDialog() != DialogResult.OK)
                return;

            string savedPath = dialog.FileName;
            btnXemTonKho.Enabled = false;

            try
            {
                DataTable dtLo = null;
                DataTable dtCuon = null;

                await WaitingHelper.RunWithWaiting(async () =>
                {
                    dtLo = await Task.Run(() => TonKhoExcelReport_DB.LayTonKhoLo());
                    dtCuon = await Task.Run(() => TonKhoExcelReport_DB.LayTonKhoCuon());

                    bool isEmpty = (dtLo == null || dtLo.Rows.Count == 0)
                                && (dtCuon == null || dtCuon.Rows.Count == 0);

                    if (isEmpty)
                    {
                        dtLo = dtCuon = null;
                        return;
                    }

                    await Task.Run(() => TonKhoExcelExporter.ExportToPath(dtLo, dtCuon, savedPath));

                }, "ĐANG XUẤT BÁO CÁO TỒN KHO EXCEL...");

                if (dtLo == null && dtCuon == null)
                    FrmWaiting.ShowGifAlert("KHÔNG CÓ DỮ LIỆU TỒN KHO ĐỂ XUẤT BÁO CÁO.", "THÔNG BÁO", EnumStore.Icon.Warning);
                else
                    FrmWaiting.ShowGifAlert("XUẤT FILE EXCEL THÀNH CÔNG!", "THÔNG BÁO", EnumStore.Icon.Success);
            }
            catch (InvalidOperationException ex) // thiếu Mã LOT hoặc lỗi nghiệp vụ
            {
                FrmWaiting.ShowGifAlert(ex.Message, "LỖI DỮ LIỆU", EnumStore.Icon.Warning);
            }
            catch (Exception ex)
            {
                FrmWaiting.ShowGifAlert($"Có lỗi xảy ra: {ex.Message}", "LỖI", EnumStore.Icon.Warning);
            }
            finally
            {
                btnXemTonKho.Enabled = true;
            }
        }
    }
}
