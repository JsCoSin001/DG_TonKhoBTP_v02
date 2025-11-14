using DG_TonKhoBTP_v02.Core;
using DG_TonKhoBTP_v02.Database;
using DG_TonKhoBTP_v02.Helper;
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

        string table = "Ma";
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
                    MessageBox.Show("KIỂU SẢN PHẨM KHÔNG HỢP LỆ.", "Lỗi",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                    MessageBox.Show("THIẾU DỮ LIỆU.", "Lỗi",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // >>>> LẤY GIÁ TRỊ UI TRƯỚC KHI VÀO Task.Run <<<<
                var cbxText = cbxMaSP.Text?.Trim();
                var idText = id.Text?.Trim();

                await WaitingHelper.RunWithWaiting(async () =>
                {
                    string result = await Task.Run(() =>
                    {
                        bool isInsert = string.IsNullOrEmpty(idText);

                        if (!isInsert)
                        {
                            if (!int.TryParse(idText, out int parsedId))
                                return "ID KHÔNG HỢP LỆ.";

                            return DatabaseHelper.UpdateDanhSachMaSP(sp, parsedId);
                        }

                        return DatabaseHelper.InsertDSMaSP(sp);
                    });

                    if (string.IsNullOrEmpty(result))
                    {
                        MessageBox.Show("THAO TÁC THÀNH CÔNG", "THÔNG BÁO",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                        Clear(); // nhớ: Clear() chỉ đụng UI trên UI thread (ở đây là OK)
                    }
                    else
                    {
                        MessageBox.Show(result, "LỖI",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Đã xảy ra lỗi: {ex.Message}", "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                        MessageBox.Show("KHÔNG CÓ DỮ LIỆU.", "THÔNG BÁO",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                        MessageBox.Show("Đã xuất Excel thành công!", "Export",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                MessageBox.Show($"Có lỗi xảy ra khi tải danh sách: {ex.Message}",
                    "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
