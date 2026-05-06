using ClosedXML.Excel;
using DG_TonKhoBTP_v02.Core;
using DG_TonKhoBTP_v02.Database;
using DG_TonKhoBTP_v02.Helper;
using DG_TonKhoBTP_v02.Helper.Reuseable;
using DG_TonKhoBTP_v02.Models;
using DG_TonKhoBTP_v02.UI.Helper;
using System;
using System.Data;
using System.Drawing;
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


        public UC_CapNhatSP()
        {
            InitializeComponent();
            cbxLoaiTimKiem.SelectedItem = cbxLoaiTimKiem.Items[0];
            cbxLoaiSP.SelectedItem = cbxLoaiSP.Items[1];
        }

        private async void btnLuu_Click(object sender, EventArgs e)
        {
            btnLuu.Enabled = false;

            try
            {
                if (string.IsNullOrWhiteSpace(kieuSP.Text))
                {
                    FrmWaiting.ShowGifAlert("KIỂU SẢN PHẨM KHÔNG HỢP LỆ.");
                    return;
                }

                var sp = new DanhSachMaSP
                {
                    Ma = ma.Text.Trim().ToUpper(),
                    Ten = ten.Text.Trim(),
                    Ten_KhongDau = CoreHelper.BoDauTiengViet(ten.Text.Trim()),
                    KieuSP = kieuSP.Text.Trim().ToUpper(),
                    DonVi = donVi.Text.Trim().ToUpper(),
                    ChuyenDoi = nbrDM_CU_AL.Value == 0 ? 1 : nbrDM_CU_AL.Value,
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

                            finalId = DatabaseHelper.UpdateDanhSachMaSP(sp, parsedId);
                        }
                        else
                        {       
                            finalId = DatabaseHelper.InsertDSMaSP(sp);
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
            }
        }

        private void Clear()
        {
            ma.Text = "";
            ten.Text = "";
            kieuSP.SelectedItem = null;
            donVi.SelectedItem = null;
            cbxMaSP.Text = "";
            id.Text = "";
            btnLuu.Text = "Lưu";
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
                string query = "SELECT * FROM DanhSachMaSP";
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
                        DatabaseHelper.GetData(query, colValue, colParamName));

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
            //ResetController_TimTenSP();
            string tenTP = cbxMaSP.Text.Trim();
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

        private async Task ShowDanhSachLuaChon(string keyword, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                cbxMaSP.DroppedDown = false;
                return;
            }


            string cot = "";
            string table = "DanhSachMaSP";

            int loaiTimKiem = cbxLoaiTimKiem.SelectedIndex;
            string tenHT = "Ma";

            switch (loaiTimKiem)
            {
                case 0: // Mã
                    cot = "Ma";
                    break;
                case 1: // Tên
                    cot = "Ten_KhongDau";
                    tenHT = "Ten";
                    break;
                case 2: // Kiểu SP
                    cot = "TenNCC_KhongDau";
                    tenHT = "TenNCC";
                    table = "DanhSachNCC";
                    break;
                default:
                    cot = "TenKho_KhongDau";
                    tenHT = "TenKho";
                    table = "DanhSachKho";
                    break;
            }

            keyword = CoreHelper.BoDauTiengViet(keyword);

            string query = "SELECT * FROM " + table +
                " WHERE " + cot + " LIKE '%' || @Key || '%' COLLATE NOCASE";

            string para = "Key";


            DataTable sp = await Task.Run(() =>
            {
                return DatabaseHelper.GetData(query, keyword, para);
            }, ct);

            ct.ThrowIfCancellationRequested();

            cbxMaSP.DroppedDown = false;

            cbxMaSP.SelectionChangeCommitted -= GanGiaTri_SelectionChangeCommitted;
            if (sp.Rows.Count == 0) return;

            cbxMaSP.DataSource = sp;
            cbxMaSP.DisplayMember = tenHT;

            string currentText = keyword;

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
                case 0: // Mã
                case 1: // Tên
                    setValue_DSMaSP(row);
                    break;
                case 2: // Kiểu SP
                    tbxMaNcc.Text = row["ma"].ToString();
                    tbxTenNcc.Text = row["TenNcc"].ToString();
                    tbxID.Text = row["id"].ToString();
                    tbxLuuNcc.Text = "Cập nhật";
                    break;
                default: // Kho
                    tbxKiHieuKho.Text = row["kiHieu"].ToString();
                    tbxTenKho.Text = row["tenKho"].ToString();
                    tbxIDKho.Text = row["id"].ToString();
                    btnLuuKho.Text = "Cập nhật";
                    break;
            }


            cbxMaSP.SelectedIndex = -1;
            cbxMaSP.Text = string.Empty;

        }

        private void setValue_DSMaSP(DataRowView row)
        {
            ten.Text = row["ten"].ToString();
            ma.Text = row["ma"].ToString();
            donVi.Text = row["donVi"].ToString();
            kieuSP.Text = row["kieuSP"].ToString();
            id.Text = row["id"].ToString();

            btnLuu.Text = "Cập nhật";
        }

        private async void tbxLuuNcc_Click(object sender, EventArgs e)
        {
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

            await WaitingHelper.RunWithWaiting(() =>
            {
                try
                {
                    finalId = DatabaseHelper.UpsertDanhSachNCC(id, ma, tenNcc);
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

            // Ghi Excel chạy nền, không block UI
            if(cbxExNCC.Checked) GhiExcelAsync(finalId, ma, tenNcc, "DsNcc");

            FrmWaiting.ShowGifAlert($"{actionName} NHÀ CUNG CẤP THÀNH CÔNG.", "THÔNG BÁO");

            tbxID.Clear();
            tbxMaNcc.Clear();
            tbxTenNcc.Clear();
            tbxMaNcc.Focus();

            // LoadDanhSachNCC();
        }

        private async void btnLuuKho_Click(object sender, EventArgs e)
        {
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

            await WaitingHelper.RunWithWaiting(() =>
            {
                try
                {
                    int finalId = DatabaseHelper.UpsertDanhSachKho(id, kiHieu, tenKho, ghiChu);
                    if(cbxExKho.Checked) GhiExcelAsync(finalId, kiHieu, tenKho, "DsKho");
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
            //tbxGhiChuKho.Clear();
            tbxKiHieuKho.Focus();
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
    }

   
}
