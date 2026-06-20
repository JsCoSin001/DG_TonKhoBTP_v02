using ClosedXML.Excel;
using DG_TonKhoBTP_v02.Core;
using DG_TonKhoBTP_v02.Database;
using DG_TonKhoBTP_v02.Database.KeToan;
using DG_TonKhoBTP_v02.Helper;
using DG_TonKhoBTP_v02.Helper.Reuseable;
using DG_TonKhoBTP_v02.Models;
using DG_TonKhoBTP_v02.Models.KeToan;
using DG_TonKhoBTP_v02.UI.Helper;
using DG_TonKhoBTP_v02.UI.NghiepVuKhac.KeToan;
using System;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using CoreHelper = DG_TonKhoBTP_v02.Helper.Helper;


namespace DG_TonKhoBTP_v02.UI
{
    public partial class UC_CapNhatSP : UserControl
    {

        private CancellationTokenSource _searchCts;

        private static readonly string folderPath = @"\\192.168.4.10\DungChungDG\DanhSach_SP_NCC_Kho";
        private static readonly string filePath = Path.Combine(folderPath, "data.xlsx");
        private int _ruloId = 0;
        private bool _isLoadingData = false;
        private bool _isSyncingTenKhongDau = false;
        private bool _isAutoGeneratingTenKhongDau = true;

        public UC_CapNhatSP()
        {
            InitializeComponent();
            cbxLoaiTimKiem.SelectedItem = cbxLoaiTimKiem.Items[0];
            cbxLoaiSP.SelectedItem = cbxLoaiSP.Items[1];

            this.Disposed += UC_CapNhatSP_Disposed;
            tbTenKhongDau.TextChanged += tbTenKhongDau_TextChanged;
        }

        private async void btnLuu_Click(object sender, EventArgs e)
        {
            // 1) Kiểm tra quyền trước khi làm gì khác
            if (!UserContext.IsAuthenticated
                || (!UserContext.HasRole(RoleNames.Acc) && !UserContext.HasRole(RoleNames.Admin)))
            {
                FrmWaiting.ShowGifAlert($"Bạn cần cấp quyền để thực hiện yêu cầu này.");
                return;
            }


            btnLuu.Enabled = false;

            try
            {
                if (string.IsNullOrWhiteSpace(kieuSP.Text))
                {
                    FrmWaiting.ShowGifAlert("KIỂU SẢN PHẨM KHÔNG HỢP LỆ.");
                    return;
                }

                if (nbrDM_CU_AL.Value == 0)
                {
                    FrmWaiting.ShowGifAlert("Định mức chuyển đổi không phù hợp.");
                    return;
                }

                var sp = new CapNhatSP_Model
                {
                    Ma = ma.Text.Trim().ToUpper(),
                    Ten = ten.Text.Trim(),
                    Ten_KhongDau = ResolveTenKhongDauForSave(ten.Text, tbTenKhongDau.Text),
                    KieuSP = kieuSP.Text.Trim().ToUpper(),
                    DonVi = donVi.Text.Trim().ToUpper(),
                    ChuyenDoi = nbrDM_CU_AL.Value,
                    Active = cbActive.Checked,
                    DateInsert = DateTime.Now
                };

                if (string.IsNullOrWhiteSpace(sp.Ma) ||
                    string.IsNullOrWhiteSpace(sp.Ten) ||
                    string.IsNullOrWhiteSpace(sp.DonVi))
                {
                    FrmWaiting.ShowGifAlert("THIẾU DỮ LIỆU.");
                    return;
                }

                string idText = id.Text?.Trim();
                bool isInsert = string.IsNullOrWhiteSpace(idText);

                int finalId = 0;

                await WaitingHelper.RunWithWaiting(() =>
                {
                    try
                    {
                        if (!isInsert)
                        {
                            if (!int.TryParse(idText, out int parsedId))
                                throw new Exception("ID KHÔNG HỢP LỆ.");

                            finalId = CapNhatSP_DB.UpdateDanhSachMaSP(sp, parsedId);
                        }
                        else
                        {
                            finalId = CapNhatSP_DB.InsertDSMaSP(sp);
                        }
                    }
                    catch (Exception ex)
                    {
                        FrmWaiting.ShowGifAlert(
                            CoreHelper.ShowErrorDatabase(ex, sp.Ma),
                            "LỖI CƠ SỞ DỮ LIỆU");
                        throw;
                    }
                }, "ĐANG LƯU DỮ LIỆU...");


                if (cbxExSanPham.Checked) GhiExcelAsync(finalId, sp.Ma, sp.Ten, "DsSanPham");


                FrmWaiting.ShowGifAlert("THAO TÁC THÀNH CÔNG", "THÔNG BÁO", EnumStore.Icon.Success);

                Clear();
            }
            catch
            {
                // lỗi đã show bên trong rồi
            }
            finally
            {
                btnLuu.Enabled = true;
                btnLuu.Text = "LƯU";
                cbActive.Checked = true;
            }
        }

        private void Clear()
        {
            _isLoadingData = true;

            try
            {
                ma.Text = "";
                ten.Text = "";
                SetTenKhongDauText(string.Empty);
                kieuSP.SelectedItem = null;
                donVi.SelectedItem = null;
                cbxMaSP.Text = "";
                id.Text = "";
                btnLuu.Text = "Lưu";
                _isAutoGeneratingTenKhongDau = true;
            }
            finally
            {
                _isLoadingData = false;
            }
        }

        private void ma_TextChanged(object sender, EventArgs e)
        {
            string maSP = ma.Text.Trim();
            kieuSP.SelectedItem = null;

            if (!string.IsNullOrEmpty(maSP))
            {
                kieuSP.Text = maSP.Split('.')[0];
                Console.WriteLine(maSP.Split('.')[0]);
            }
        }

        private void btnXoa_Click(object sender, EventArgs e)
        {
            Clear();
        }


        private async void btnShowList_Click(object sender, EventArgs e)
        {

            btnShowList.Enabled = false;

            try
            {
                int loaiSP = cbxLoaiSP.SelectedIndex;
                string query = "SELECT id as STT, Ma, Ten, DonVi, ChuyenDoi, Active FROM DanhSachMaSP";
                string colValue = null;
                string colParamName = "KieuSP";

                // (giữ nguyên logic cũ)
                if (loaiSP != 3 && loaiSP >= 0)
                {
                    colValue = cbxLoaiSP.Items[loaiSP].ToString();
                    query += " WHERE KieuSP = @KieuSP";
                }
                query += " ORDER BY id DESC";

                DataTable dt = null;

                await WaitingHelper.RunWithWaiting(async () =>
                {
                    dt = await Task.Run(() =>
                        CapNhatSP_DB.GetDanhSachMaSP(loaiSP, colValue));

                    if (dt == null || dt.Rows.Count == 0)
                    {
                        FrmWaiting.ShowGifAlert("KHÔNG CÓ DỮ LIỆU.");
                        return;
                    }

                    // 2️⃣ Nếu có chọn xuất Excel → xuất ngay trong vùng “waiting”
                    if (cbXuatExcel.Checked)
                    {
                        // 1) Hỏi đường dẫn trên UI thread (STA) — KHÔNG đưa vào Task.Run
                        using var sfd = new SaveFileDialog
                        {
                            Title = "Xuất báo cáo Excel",
                            Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                            FileName = $"DanhSachMaSP_{DateTime.Now:yyyyMMdd_HHmm}"
                        };
                        if (sfd.ShowDialog() != DialogResult.OK)
                            return;

                        // 2) Ghi file nặng trong vùng waiting (thread nền)
                        await WaitingHelper.RunWithWaiting(async () =>
                        {
                            await Task.Run(() => ExcelExporter.ExportToPath(dt, sfd.FileName));
                        });

                        // 3) Thông báo xong (UI thread)
                        FrmWaiting.ShowGifAlert("Đã xuất Excel thành công!", "Export", EnumStore.Icon.Success);
                        return;
                    }
                    else
                    {
                        grvDanhSach.DataSource = dt;
                        grvDanhSach.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                        grvDanhSach.Font = new System.Drawing.Font("Segoe UI", 12, FontStyle.Regular);
                        grvDanhSach.ColumnHeadersDefaultCellStyle.Font = new System.Drawing.Font("Segoe UI", 12, FontStyle.Bold);
                        grvDanhSach.Columns[0].Width = 100;
                        //if (grvDanhSach.Columns.Count > 0) grvDanhSach.Columns[0].Width = 100;
                        //if (grvDanhSach.Columns.Count > 1) grvDanhSach.Columns[1].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                        //if (grvDanhSach.Columns.Count > 2) grvDanhSach.Columns[2].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                    }
                });



            }
            catch (Exception ex)
            {
                FrmWaiting.ShowGifAlert($"Có lỗi xảy ra khi tải danh sách: {ex.Message}");
            }
            finally
            {
                btnShowList.Enabled = true;
            }
        }

        private async void cbxMaSP_TextUpdate(object sender, EventArgs e)
        {
            string tenTP = cbxMaSP.Text.Trim();
            if (string.IsNullOrEmpty(tenTP)) return;

            var oldCts = _searchCts;
            oldCts?.Cancel();
            oldCts?.Dispose();

            _searchCts = new CancellationTokenSource();
            var token = _searchCts.Token;

            try
            {
                await Task.Delay(500, token);
                await ShowDanhSachLuaChon(tenTP, token);
            }
            catch (OperationCanceledException)
            {
                // User gõ tiếp nên bỏ qua request cũ.
            }
            catch (ObjectDisposedException)
            {
                // Control đã đóng.
            }
        }

        private async Task ShowDanhSachLuaChon(string keyword, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                cbxMaSP.DroppedDown = false;
                ReleaseCbxMaSPDataSource();
                return;
            }

            int loaiTimKiem = cbxLoaiTimKiem.SelectedIndex;
            string currentText = keyword;

            DataTable dt = await Task.Run(() =>
            {
                return CapNhatSP_DB.TimDanhSachLuaChon(loaiTimKiem, keyword);
            }, ct);

            ct.ThrowIfCancellationRequested();

            cbxMaSP.SelectionChangeCommitted -= GanGiaTri_SelectionChangeCommitted;
            cbxMaSP.DroppedDown = false;
            ReleaseCbxMaSPDataSource();

            if (dt == null || dt.Rows.Count == 0)
            {
                dt?.Dispose();
                return;
            }

            cbxMaSP.DataSource = dt;
            cbxMaSP.DisplayMember = CapNhatSP_DB.GetDisplayMemberLuaChon(loaiTimKiem);
            cbxMaSP.ValueMember = "id";

            cbxMaSP.DroppedDown = true;
            cbxMaSP.Text = currentText;
            cbxMaSP.SelectionStart = cbxMaSP.Text.Length;
            cbxMaSP.SelectionLength = 0;

            cbxMaSP.SelectionChangeCommitted += GanGiaTri_SelectionChangeCommitted;
        }
        private void GanGiaTri_SelectionChangeCommitted(object sender, EventArgs e)
        {
            if (cbxMaSP.SelectedItem == null || !(cbxMaSP.SelectedItem is DataRowView)) return;

            DataRowView row = (DataRowView)cbxMaSP.SelectedItem;

            int kieu = cbxLoaiTimKiem.SelectedIndex;

            switch (kieu)
            {
                case 0: // Mã SP
                case 1: // Tên SP
                    setValue_DSMaSP(row);
                    break;

                case 2: // Nhà cung cấp
                    tbxMaNcc.Text = row["ma"].ToString();
                    tbxTenNcc.Text = row["TenNcc"].ToString();
                    tbxID.Text = row["id"].ToString();
                    tbxLuuNcc.Text = "Cập nhật";
                    break;

                case 3: // Kho
                    tbxKiHieuKho.Text = row["kiHieu"].ToString();
                    tbxTenKho.Text = row["tenKho"].ToString();
                    tbxIDKho.Text = row["id"].ToString();
                    btnLuuKho.Text = "Cập nhật";
                    break;

                case 4: // Rulo
                    SetValue_Rulo(row);
                    break;
            }

            cbxMaSP.SelectedIndex = -1;
            cbxMaSP.Text = string.Empty;
            cbxMaSP.DroppedDown = false;
        }

        private void SetValue_Rulo(DataRowView row)
        {
            _ruloId = ToIntSafe(row["id"]);

            SetNumericValue(nbrKichThuocRolo, ToDecimalSafe(row["KichThuoc"]));
            SetNumericValue(nbrKhoiLuongRulo, ToDecimalSafe(row["KhoiLuong"]));
            SetNumericValue(nbrKhoiLuongCaNanPhu, ToDecimalSafe(row["KhoiLuongCaNanPhu"]));

            btnLuuRulo.Text = "Cập nhật";
            nbrKichThuocRolo.Focus();
        }
        private void setValue_DSMaSP(DataRowView row)
        {
            _isLoadingData = true;

            try
            {
                string tenValue = row["ten"].ToString();
                ten.Text = tenValue;
                ma.Text = row["ma"].ToString();
                donVi.Text = row["donVi"].ToString();
                kieuSP.Text = row["kieuSP"].ToString();
                id.Text = row["id"].ToString();
                cbActive.Checked = Convert.ToBoolean(row["active"]);

                string tenKhongDauValue = HasColumn(row, "Ten_KhongDau")
                    ? row["Ten_KhongDau"]?.ToString()
                    : null;

                SetTenKhongDauText(ResolveTenKhongDauForDisplay(tenValue, tenKhongDauValue));
                _isAutoGeneratingTenKhongDau = false;

                btnLuu.Text = "Cập nhật";
            }
            finally
            {
                _isLoadingData = false;
            }
        }

        private async void tbxLuuNcc_Click(object sender, EventArgs e)
        {
            // 1) Kiểm tra quyền trước khi làm gì khác
            if (!UserContext.IsAuthenticated
                || (!UserContext.HasRole(RoleNames.Acc) && !UserContext.HasRole(RoleNames.Admin)))
            {
                FrmWaiting.ShowGifAlert($"Bạn cần cấp quyền để thực hiện yêu cầu này.");
                return;
            }


            string id = tbxID.Text.Trim();
            string ma = tbxMaNcc.Text.Trim();
            string tenNcc = tbxTenNcc.Text.Trim();

            if (string.IsNullOrWhiteSpace(ma))
            {
                FrmWaiting.ShowGifAlert("MÃ NHÀ CUNG CẤP KHÔNG ĐƯỢC ĐỂ TRỐNG.");
                tbxMaNcc.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(tenNcc))
            {
                FrmWaiting.ShowGifAlert("TÊN NHÀ CUNG CẤP KHÔNG ĐƯỢC ĐỂ TRỐNG.");
                tbxTenNcc.Focus();
                return;
            }

            bool isSuccess = false;
            int finalId = 0;
            string actionName = string.IsNullOrWhiteSpace(id) ? "THÊM MỚI" : "CẬP NHẬT";

            try
            {
                await WaitingHelper.RunWithWaiting(() =>
                {
                    try
                    {
                        finalId = CapNhatSP_DB.UpsertDanhSachNCC(id, ma, tenNcc);
                        isSuccess = true;
                    }
                    catch (Exception ex)
                    {
                        isSuccess = false;
                        FrmWaiting.ShowGifAlert(
                            CoreHelper.ShowErrorDatabase(ex, "NHÀ CUNG CẤP"),
                            "LỖI CƠ SỞ DỮ LIỆU");
                    }
                }, $"{actionName} NHÀ CUNG CẤP, VUI LÒNG ĐỢI...");

                if (!isSuccess) return;

                if (cbxExNCC.Checked) GhiExcelAsync(finalId, ma, tenNcc, "DsNcc");
                FrmWaiting.ShowGifAlert($"{actionName} NHÀ CUNG CẤP THÀNH CÔNG.", "THÔNG BÁO");

                tbxID.Clear();
                tbxMaNcc.Clear();
                tbxTenNcc.Clear();
                tbxMaNcc.Focus();
            }
            catch (Exception ex)
            {
                FrmWaiting.ShowGifAlert($"Có lỗi xảy ra: {ex.Message}", "LỖI", EnumStore.Icon.Warning);
            }
        }

        private async void btnLuuKho_Click(object sender, EventArgs e)
        {
            // 1) Kiểm tra quyền trước khi làm gì khác
            if (!UserContext.IsAuthenticated
                || (!UserContext.HasRole(RoleNames.Acc) && !UserContext.HasRole(RoleNames.Admin)))
            {
                FrmWaiting.ShowGifAlert($"Bạn cần cấp quyền để thực hiện yêu cầu này.");
                return;
            }

            string id = tbxIDKho.Text.Trim();
            string kiHieu = tbxKiHieuKho.Text.Trim();
            string tenKho = tbxTenKho.Text.Trim();
            string ghiChu = "";

            if (string.IsNullOrWhiteSpace(kiHieu))
            {
                FrmWaiting.ShowGifAlert("KÍ HIỆU KHO KHÔNG ĐƯỢC ĐỂ TRỐNG.");
                tbxKiHieuKho.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(tenKho))
            {
                FrmWaiting.ShowGifAlert("TÊN KHO KHÔNG ĐƯỢC ĐỂ TRỐNG.");
                tbxTenKho.Focus();
                return;
            }

            bool isSuccess = false;
            string actionName = string.IsNullOrWhiteSpace(id) ? "THÊM MỚI" : "CẬP NHẬT";

            try
            {
                await WaitingHelper.RunWithWaiting(() =>
                {
                    try
                    {
                        int finalId = CapNhatSP_DB.UpsertDanhSachKho(id, kiHieu, tenKho, ghiChu);
                        if (cbxExKho.Checked) GhiExcelAsync(finalId, kiHieu, tenKho, "DsKho");
                        isSuccess = true;
                    }
                    catch (Exception ex)
                    {
                        isSuccess = false;
                        FrmWaiting.ShowGifAlert(
                            CoreHelper.ShowErrorDatabase(ex, "KHO"),
                            "LỖI CƠ SỞ DỮ LIỆU");
                    }
                }, $"{actionName} KHO, VUI LÒNG ĐỢI...");

                if (!isSuccess) return;

                FrmWaiting.ShowGifAlert($"{actionName} KHO THÀNH CÔNG.", "THÔNG BÁO");

                tbxIDKho.Clear();
                tbxKiHieuKho.Clear();
                tbxTenKho.Clear();
                tbxKiHieuKho.Focus();
            }
            catch (Exception ex)
            {
                FrmWaiting.ShowGifAlert($"Có lỗi xảy ra: {ex.Message}", "LỖI", EnumStore.Icon.Warning);
            }
        }


        private void GhiExcelAsync(int finalId, string ma, string ten, string sheetName)
        {
            Task.Run(() =>
            {
                try
                {
                    var model = new Kho_NCC
                    {
                        ID = finalId,
                        Ma = ma,
                        Ten = ten
                    };

                    ExcelHelper.InsertModelToSheet(model, sheetName);
                }
                catch (Exception ex)
                {
                    if (this.InvokeRequired)
                    {
                        this.Invoke(new Action(() =>
                        {
                            FrmWaiting.ShowGifAlert(
                                "Ghi file Excel thất bại:\n" + ex.Message,
                                "LỖI EXCEL");
                        }));
                    }
                    else
                    {
                        FrmWaiting.ShowGifAlert(
                            "Ghi file Excel thất bại:\n" + ex.Message,
                            "LỖI EXCEL");
                    }
                }
            });
        }

        private void boSungThem_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Frm_BoSungThemDuLieu frm = new Frm_BoSungThemDuLieu();
            frm.ShowDialog();
        }

        private async void btnXemDSRulo_Click(object sender, EventArgs e)
        {
            btnXemDSRulo.Enabled = false;

            try
            {
                DataTable dt = null;

                await WaitingHelper.RunWithWaiting(() =>
                {
                    dt = TTLo_DB.GetAll();
                }, "ĐANG TẢI DANH SÁCH RU LÔ...");

                if (dt == null || dt.Rows.Count == 0)
                {
                    dt?.Dispose();
                    FrmWaiting.ShowGifAlert("KHÔNG CÓ DỮ LIỆU RU LÔ.");
                    return;
                }

                grvDanhSach.DataSource = dt;
                grvDanhSach.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                grvDanhSach.Font = new System.Drawing.Font("Segoe UI", 12, FontStyle.Regular);
                grvDanhSach.ColumnHeadersDefaultCellStyle.Font = new System.Drawing.Font("Segoe UI", 12, FontStyle.Bold);

                if (grvDanhSach.Columns.Count > 0)
                    grvDanhSach.Columns[0].Width = 100;
            }
            catch (Exception ex)
            {
                FrmWaiting.ShowGifAlert(
                    CoreHelper.ShowErrorDatabase(ex, "RU LÔ"),
                    "LỖI CƠ SỞ DỮ LIỆU");
            }
            finally
            {
                btnXemDSRulo.Enabled = true;
            }
        }

        private async void btnLuuRulo_Click(object sender, EventArgs e)
        {

            // 1) Kiểm tra quyền trước khi làm gì khác
            if (!UserContext.IsAuthenticated
                || (!UserContext.HasRole(RoleNames.Wh) && !UserContext.HasRole(RoleNames.Acc) && !UserContext.HasRole(RoleNames.Admin)))
            {
                FrmWaiting.ShowGifAlert($"Bạn cần cấp quyền để thực hiện yêu cầu này.");
                return;
            }


            btnLuuRulo.Enabled = false;

            try
            {
                var rulo = new TTLo_Model
                {
                    Id = _ruloId,
                    KichThuoc = nbrKichThuocRolo.Value.ToString("0.################", CultureInfo.InvariantCulture),
                    KhoiLuong = Convert.ToDouble(nbrKhoiLuongRulo.Value),
                    KhoiLuongCaNanPhu = Convert.ToDouble(nbrKhoiLuongCaNanPhu.Value)
                };

                if (string.IsNullOrWhiteSpace(rulo.KichThuoc) || rulo.KichThuoc == "0")
                {
                    FrmWaiting.ShowGifAlert("KÍCH THƯỚC RU LÔ KHÔNG HỢP LỆ.");
                    nbrKichThuocRolo.Focus();
                    return;
                }

                bool isInsert = _ruloId <= 0;
                bool isSuccess = false;
                int finalId = _ruloId;
                string actionName = isInsert ? "THÊM MỚI" : "CẬP NHẬT";

                await WaitingHelper.RunWithWaiting(() =>
                {
                    try
                    {
                        if (isInsert)
                        {
                            finalId = TTLo_DB.Insert(rulo);
                            _ruloId = finalId;
                        }
                        else
                        {
                            int affectedRows = TTLo_DB.Update(rulo);
                            if (affectedRows <= 0)
                                throw new Exception("KHÔNG TÌM THẤY RU LÔ CẦN CẬP NHẬT.");
                            finalId = rulo.Id;
                        }
                        isSuccess = true;
                    }
                    catch (Exception ex)
                    {
                        isSuccess = false;
                        FrmWaiting.ShowGifAlert(
                            CoreHelper.ShowErrorDatabase(ex, rulo.KichThuoc),
                            "LỖI CƠ SỞ DỮ LIỆU");
                    }
                }, $"{actionName} RU LÔ, VUI LÒNG ĐỢI...");

                if (!isSuccess) return;

                FrmWaiting.ShowGifAlert(
                    $"{actionName} RU LÔ THÀNH CÔNG.",
                    "THÔNG BÁO",
                    EnumStore.Icon.Success);

                ClearRulo();
            }
            catch (Exception ex)
            {
                FrmWaiting.ShowGifAlert($"Có lỗi xảy ra: {ex.Message}", "LỖI", EnumStore.Icon.Warning);
            }
            finally
            {
                btnLuuRulo.Enabled = true;
                btnLuuRulo.Text = "Lưu";
            }
        }

        private void ClearRulo()
        {
            _ruloId = 0;

            SetNumericValue(nbrKichThuocRolo, 0);
            SetNumericValue(nbrKhoiLuongRulo, 0);
            SetNumericValue(nbrKhoiLuongCaNanPhu, 0);

            btnLuuRulo.Text = "Lưu";
            nbrKichThuocRolo.Focus();
        }

        private static int ToIntSafe(object value)
        {
            if (value == null || value == DBNull.Value) return 0;

            if (int.TryParse(value.ToString(), out int result))
                return result;

            return 0;
        }

        private static decimal ToDecimalSafe(object value)
        {
            if (value == null || value == DBNull.Value) return 0;

            string text = value.ToString()?.Trim();
            if (string.IsNullOrWhiteSpace(text)) return 0;

            if (decimal.TryParse(text, NumberStyles.Any, CultureInfo.CurrentCulture, out decimal currentResult))
                return currentResult;

            if (decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal invariantResult))
                return invariantResult;

            return 0;
        }

        private static void SetNumericValue(NumericUpDown control, decimal value)
        {
            if (control == null) return;

            if (value < control.Minimum)
                control.Minimum = value;

            if (value > control.Maximum)
                control.Maximum = value;

            control.Value = value;
        }

        private void ReleaseCbxMaSPDataSource()
        {
            if (cbxMaSP == null) return;

            var oldDataSource = cbxMaSP.DataSource as IDisposable;
            cbxMaSP.DataSource = null;
            cbxMaSP.DisplayMember = string.Empty;
            cbxMaSP.ValueMember = string.Empty;
            oldDataSource?.Dispose();
        }

        private void UC_CapNhatSP_Disposed(object sender, EventArgs e)
        {
            var cts = _searchCts;
            _searchCts = null;

            if (cts != null)
            {
                cts.Cancel();
                cts.Dispose();
            }

            ReleaseCbxMaSPDataSource();
        }

        private void ten_TextChanged(object sender, EventArgs e)
        {
            if (_isLoadingData) return;
            if (!_isAutoGeneratingTenKhongDau) return;

            SetTenKhongDauText(CoreHelper.BoDauTiengViet(ten.Text.Trim()));
        }

        private void tbTenKhongDau_TextChanged(object sender, EventArgs e)
        {
            if (_isLoadingData || _isSyncingTenKhongDau) return;

            _isAutoGeneratingTenKhongDau = false;
        }

        private void SetTenKhongDauText(string value)
        {
            _isSyncingTenKhongDau = true;

            try
            {
                tbTenKhongDau.Text = value ?? string.Empty;
            }
            finally
            {
                _isSyncingTenKhongDau = false;
            }
        }

        private static string ResolveTenKhongDauForSave(string tenValue, string tenKhongDauInput)
        {
            string source = string.IsNullOrWhiteSpace(tenKhongDauInput)
                ? tenValue
                : tenKhongDauInput;

            return NormalizeTenKhongDau(source);
        }

        private static string ResolveTenKhongDauForDisplay(string tenValue, string tenKhongDauValue)
        {
            string source = string.IsNullOrWhiteSpace(tenKhongDauValue)
                ? tenValue
                : tenKhongDauValue;

            return NormalizeTenKhongDau(source);
        }

        private static string NormalizeTenKhongDau(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;

            return CoreHelper.BoDauTiengViet(value.Trim())?.Trim() ?? string.Empty;
        }

        private static bool HasColumn(DataRowView row, string columnName)
        {
            return row?.DataView?.Table?.Columns?.Contains(columnName) == true;
        }
    }
}
