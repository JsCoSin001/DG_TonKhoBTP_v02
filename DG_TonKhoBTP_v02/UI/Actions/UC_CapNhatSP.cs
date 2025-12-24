using DG_TonKhoBTP_v02.Core;
using DG_TonKhoBTP_v02.Database;
using DG_TonKhoBTP_v02.Helper;
using DG_TonKhoBTP_v02.Models;
using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using DocumentFormat.OpenXml.Spreadsheet;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DG_TonKhoBTP_v02.UI
{
    public partial class UC_CapNhatSP : UserControl
    {

        private CancellationTokenSource _searchCts;

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
                if (kieuSP.SelectedItem == null)
                {
                    FrmWaiting.ShowGifAlert($"KIỂU SẢN PHẨM KHÔNG HỢP LỆ.");
                    return;
                }

                var sp = new DanhSachMaSP
                {
                    Ma = ma.Text.Trim().ToUpper(),
                    Ten = ten.Text.Trim().ToUpper(),
                    KieuSP = kieuSP.Text.Trim().ToUpper(),
                    DonVi = donVi.Text.Trim().ToUpper(),
                    DateInsert = DateTime.Now
                };

                if (string.IsNullOrEmpty(sp.Ma) ||
                    string.IsNullOrEmpty(sp.Ten) ||
                    string.IsNullOrEmpty(sp.DonVi))
                {
                    FrmWaiting.ShowGifAlert("THIẾU DỮ LIỆU.");
                    return;
                }

                // Lấy giá trị UI trước
                var idText = id.Text?.Trim();

                // Chạy logic với waiting form, TRẢ VỀ KẾT QUẢ
                string result = await WaitingHelper.RunWithWaiting(async () =>
                {
                    return await Task.Run(() =>
                    {
                        bool isInsert = string.IsNullOrEmpty(idText);

                        if (!isInsert)
                        {
                            if (!int.TryParse(idText, out int parsedId))  return "ID KHÔNG HỢP LỆ.";
                            return DatabaseHelper.UpdateDanhSachMaSP(sp, parsedId);
                        }

                        if (UserContext.HasPermission("CAN_WRITE") == false)
                        {
                            return "BẠN KHÔNG CÓ QUYỀN THÊM MỚI SẢN PHẨM.";
                        }

                        return DatabaseHelper.InsertDSMaSP(sp);
                    });
                }, "ĐANG LƯU DỮ LIỆU...");

                // >>>> HIỂN THỊ MESSAGEBOX SAU KHI WAITING FORM ĐÃ ĐÓNG <<
                string icon = "warning";
                if (string.IsNullOrEmpty(result))
                {
                    result = "THAO TÁC THÀNH CÔNG";
                    icon = EnumStore.Icon.Success;
                    Clear();
                }

                FrmWaiting.ShowGifAlert(result, "THÔNG BÁO",icon);
            }
            catch (Exception ex)
            {
                FrmWaiting.ShowGifAlert($"Đã xảy ra lỗi: {ex.Message}".ToUpper());
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
                Console.WriteLine(maSP);
                kieuSP.Text = maSP.Split('.')[0];
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

                        if (grvDanhSach.Columns.Count > 0) grvDanhSach.Columns[0].Width = 100;
                        if (grvDanhSach.Columns.Count > 1) grvDanhSach.Columns[1].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                        if (grvDanhSach.Columns.Count > 2) grvDanhSach.Columns[2].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
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
                await Task.Delay(250, token);

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


            string table = cbxLoaiTimKiem.SelectedIndex == 0 ? "Ma" : "Ten";

            string query = "SELECT * FROM DanhSachMaSP " +
               "WHERE " + table + " LIKE '%' || @Key || '%' " +
               "ORDER BY DateInsert DESC ";

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
            cbxMaSP.DisplayMember = table;

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

            ten.Text = row["ten"].ToString();
            ma.Text = row["ma"].ToString();
            donVi.Text = row["donVi"].ToString();
            kieuSP.Text = row["kieuSP"].ToString();
            id.Text = row["id"].ToString();

            cbxMaSP.SelectedIndex = -1;
            cbxMaSP.Text = string.Empty;

            btnLuu.Text = "Cập nhật";
        }

    }
}
