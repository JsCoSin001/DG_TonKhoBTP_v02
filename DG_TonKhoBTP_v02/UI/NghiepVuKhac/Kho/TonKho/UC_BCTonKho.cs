using DG_TonKhoBTP_v02.Core;
using DG_TonKhoBTP_v02.Database;
using DG_TonKhoBTP_v02.Database.Kho;
using DG_TonKhoBTP_v02.Database.Kho.TonKho;
using DG_TonKhoBTP_v02.Models.Kho.TonKho;
using DG_TonKhoBTP_v02.UI.Helper.AutoSearchWithCombobox;
using DG_TonKhoBTP_v02.UI.NghiepVuKhac.Kho.TonKho;
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
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
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
        private const string CotXemTonKho = "Button_ExcelMoi";

        private bool _tonKhoExcelDangTai;
        private CancellationTokenSource _searchCts;
        private ComboBoxSearchHelper _tonKhoSearchHelper;
        private CancellationTokenSource _tonKhoLoadCts;
        private int _tonKhoVersion;
        private BCTonKho_GoiY _tonKhoSelection;
        private bool _showingInventory;

        public UC_BCTonKho()
        {
            InitializeComponent();
            grvShowBaoCao.CellContentClick += OnInventoryReportCellContentClick;
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
            RenderTonKhoGrid(result.Dong.Select(item => (item.TaoDongBaoCao(), (object)item)));
        }

        private void RenderTonKhoGrid(IEnumerable<(BCTonKho_ExcelDong Dong, object Nguon)> rows)
        {
            const string dinhDangSo = "#,##0.############################";
            var dinhDangViet = CultureInfo.GetCultureInfo("vi-VN");
            grvShowBaoCao.DataSource = null;
            grvShowBaoCao.Rows.Clear();
            grvShowBaoCao.Columns.Clear();
            grvShowBaoCao.AutoGenerateColumns = false;
            grvShowBaoCao.AllowUserToAddRows = false;
            grvShowBaoCao.ReadOnly = true;
            grvShowBaoCao.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            grvShowBaoCao.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
            grvShowBaoCao.RowTemplate.Height = 35;
            grvShowBaoCao.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            grvShowBaoCao.ColumnHeadersHeight = 40;

            var columns = new (string Name, string Header, int Width, bool Numeric)[]
            {
                ("TenSP_ExcelMoi", "Tên sản phẩm", 250, false),
                ("MaSP_ExcelMoi", "Mã", 130, false),
                ("DonVi_ExcelMoi", "Đvị", 65, false),
                ("MaBinLot_ExcelMoi", "LOT / Mã cuộn", 190, false),
                ("Loai_ExcelMoi", "Loại", 95, false),
                ("SoCuon_ExcelMoi", "Số cuộn", 85, true),
                ("ChieuDai1Cuon_ExcelMoi", "Chiều dài 1C", 150, true),
                ("TonThucTe_ExcelMoi", "Tồn thực tế", 125, true),
                ("DatTruoc_ExcelMoi", "Đặt trước", 125, true),
                ("KhaDung_ExcelMoi", "Tồn khả dụng", 125, true),
                ("SoDau_ExcelMoi", "Số đầu", 90, true),
                ("SoCuoi_ExcelMoi", "Số cuối", 90, true)
            };
            foreach (var (name, header, width, numeric) in columns)
            {
                var column = grvShowBaoCao.Columns[grvShowBaoCao.Columns.Add(name, header)];
                column.Width = width;
                if (!numeric) continue;
                column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                column.DefaultCellStyle.Format = dinhDangSo;
                column.DefaultCellStyle.FormatProvider = dinhDangViet;
            }
            grvShowBaoCao.Columns.Add(new DataGridViewButtonColumn
            {
                Name = CotXemTonKho, HeaderText = "Thao tác", Text = "Xem",
                UseColumnTextForButtonValue = true, Width = 75
            });

            foreach (var (item, nguon) in rows)
            {
                bool cuonChan = item.Loai == "Cuộn";
                decimal datTruoc = item.ChieuDaiTonDatTruoc;
                if (cuonChan)
                    datTruoc = nguon is BCTonKho_DongTon timKiem
                        ? timKiem.DatCho
                        : item.ChieuDaiMoiCuon > 0 ? datTruoc / item.ChieuDaiMoiCuon : 0;

                int index = grvShowBaoCao.Rows.Add(item.TenSanPham, item.MaSanPham, item.DonVi,
                    item.LotHienThiBang, cuonChan ? "Cuộn chẵn" : "Cuộn lẻ", item.SoCuon,
                    item.ThieuChieuDaiHienTai ? (long?)null : item.ChieuDaiMoiCuon,
                    item.ThieuChieuDaiHienTai ? (decimal?)null : item.ChieuDaiTonThucTe,
                    datTruoc,
                    item.ThieuChieuDaiHienTai ? (decimal?)null : item.ChieuDaiTonKhaDung,
                    item.SoDau, item.SoCuoi);
                var gridRow = grvShowBaoCao.Rows[index];
                gridRow.Tag = nguon;
                gridRow.Cells["DatTruoc_ExcelMoi"].Style.Format = dinhDangSo + (cuonChan ? "' cuộn'" : "' m'");
                if (item.LaBatThuong)
                {
                    var danger = System.Drawing.Color.FromArgb(177, 48, 59);
                    gridRow.DefaultCellStyle.BackColor = danger;
                    gridRow.DefaultCellStyle.ForeColor = System.Drawing.Color.White;
                    gridRow.DefaultCellStyle.SelectionBackColor = danger;
                    gridRow.DefaultCellStyle.SelectionForeColor = System.Drawing.Color.White;
                    foreach (DataGridViewCell cell in gridRow.Cells) cell.ToolTipText = item.CanhBao;
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
            FrmWaiting.ShowGifAlert("Cần thêm dữ liệu để thực hiện chức năng này");

            return;

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


        // Hàm mới, không thay đổi chức năng xuất báo cáo đang có.
        // Có thể nối vào sự kiện nút sau khi thống nhất vị trí kích hoạt.
        public async Task TonKho_Excel(bool flgExcel = false)
        {
            if (_tonKhoExcelDangTai || IsDisposed || Disposing) return;
            _tonKhoExcelDangTai = true;
            int version = _tonKhoVersion;
            try
            {
                CancelTonKhoRequest();
                _tonKhoSelection = null;
                // Giữ bảng hiện tại trong lúc tải báo cáo hoặc xuất Excel.
                _tonKhoSearchHelper?.Reset(notifyCleared: false);
                version = _tonKhoVersion;

                var ds = await WaitingHelper.RunWithWaiting(
                    () => Task.Run(() =>
                        BC_TonKho_DB.LayTonKhoExcelMoi(kemChiTiet: !flgExcel)
                    ),
                    "ĐANG TÍNH TOÁN DỮ LIỆU TỒN KHO..."
                );
                if (IsDisposed || Disposing || (!flgExcel && version != _tonKhoVersion)) return;
                if (ds.Count == 0)
                {
                    if (!flgExcel) ClearTonKhoGrid();
                    MessageBox.Show("Không có dữ liệu tồn kho phù hợp.", "Tồn kho",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                if (flgExcel)
                {
                    if (!UserContext.IsAuthenticated)
                    {
                        MessageBox.Show("Cần đăng nhập để xuất Excel.", "Xuất báo cáo",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    // Helper đã có của dự án; dữ liệu xuất từ cùng danh sách tính tồn.
                    var bang = new DataTable();
                    bang.Columns.Add("Tên sản phẩm", typeof(string));
                    bang.Columns.Add("Mã sản phẩm", typeof(string));
                    bang.Columns.Add("Đơn vị", typeof(string));
                    bang.Columns.Add("LOT", typeof(string));
                    bang.Columns.Add("Loại", typeof(string));
                    bang.Columns.Add("Số cuộn", typeof(long));
                    bang.Columns.Add("Chiều dài 1 C", typeof(long));
                    bang.Columns.Add("Tồn kho", typeof(decimal));
                    bang.Columns.Add("Đặt trước", typeof(decimal));
                    bang.Columns.Add("Tồn khả dụng", typeof(decimal));
                    bang.Columns.Add("Số đầu", typeof(long));
                    bang.Columns.Add("Số cuối", typeof(long));


                    // Theo phương án 8B: Excel chỉ xuất các cột dữ liệu, KHÔNG có cột button.
                    foreach (var item in ds)
                        bang.Rows.Add(item.TenSanPham, item.MaSanPham, item.DonVi,
                            item.MaBinLot, item.Loai, item.SoCuon, item.ChieuDaiMoiCuon,
                            item.ChieuDaiTonThucTe, item.ChieuDaiTonDatTruoc,
                            item.ChieuDaiTonKhaDung,
                            item.SoDau.HasValue ? (object)item.SoDau.Value : DBNull.Value,
                            item.SoCuoi.HasValue ? (object)item.SoCuoi.Value : DBNull.Value);

                    // ===== CHỌN VỊ TRÍ LƯU FILE EXCEL =====
                    string duongDanExcel;

                    using (var dialog = new SaveFileDialog
                    {
                        Title = "Xuất báo cáo Excel",
                        Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                        FileName = $"TonKho_{DateTime.Now:yyyyMMdd_HHmm}.xlsx",
                        DefaultExt = "xlsx",
                        AddExtension = true
                    })
                    {
                        if (dialog.ShowDialog() != DialogResult.OK)
                            return;

                        duongDanExcel = dialog.FileName;
                    }

                    // ===== XUẤT EXCEL KÈM WAITING =====
                    try
                    {
                        await WaitingHelper.RunWithWaiting<bool>(
                            () => Task.Run(() =>
                            {
                                ExcelExporter.ExportToPath(bang, duongDanExcel);
                                return true;
                            }),
                            "ĐANG XUẤT FILE EXCEL..."
                        );

                        // Thông báo sau khi Waiting đã đóng
                        FrmWaiting.ShowGifAlert(
                            "XUẤT FILE EXCEL THÀNH CÔNG!",
                            "THÔNG BÁO",
                            EnumStore.Icon.Success
                        );
                    }
                    catch (Exception ex)
                    {
                        FrmWaiting.ShowGifAlert(
                            $"Lỗi khi xuất Excel: {ex.Message}",
                            "LỖI",
                            EnumStore.Icon.Warning
                        );
                    }

                    return;

                }

                RenderTonKhoGrid(ds.Select(item => (item, (object)item)));
            }
            catch (Exception ex)
            {
                if (!IsDisposed && !Disposing && (flgExcel || version == _tonKhoVersion))
                    MessageBox.Show("Không thể lập báo cáo tồn kho: " + ex.Message,
                        "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _tonKhoExcelDangTai = false;
            }
        }

        private void OnInventoryReportCellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0 ||
                e.RowIndex >= grvShowBaoCao.Rows.Count || e.ColumnIndex >= grvShowBaoCao.Columns.Count ||
                grvShowBaoCao.Columns[e.ColumnIndex].Name != CotXemTonKho)
                return;

            var row = grvShowBaoCao.Rows[e.RowIndex];
            try
            {
                var dong = row.Tag is BCTonKho_DongTon selected
                    ? BC_TonKho_DB.LayChiTietDongTimKiem(selected)
                    : row.Tag as BCTonKho_ExcelDong;
                if (dong?.ChiTiet == null) return;
                using var detail = new Frm_ChiTietTonKho(dong);
                detail.ShowDialog(FindForm());
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể mở chi tiết tồn kho: " + ex.Message,
                    "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void btnXemTonKho_Click(object sender, EventArgs e)
        {
            btnXemTonKho.Enabled = false;

            try
            {
                await TonKho_Excel( flgExcel: btnXuatExcel.Checked );
            }
            catch (Exception ex)
            {
                FrmWaiting.ShowGifAlert( $"Lỗi: {ex.Message}", "LỖI", EnumStore.Icon.Warning );
            }
            finally
            {
                if (!IsDisposed) btnXemTonKho.Enabled = true;
            }
        }

        private async void btnXemTonKho_Click1(object sender, EventArgs e)
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
