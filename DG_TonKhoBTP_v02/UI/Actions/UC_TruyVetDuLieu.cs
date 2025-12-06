using ClosedXML.Excel;
using DG_TonKhoBTP_v02.Database;
using DG_TonKhoBTP_v02.Dictionary;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DG_TonKhoBTP_v02.UI.Actions
{
    public partial class UC_TruyVetDuLieu : UserControl
    {
        private CancellationTokenSource _searchCts;
        private CancellationTokenSource _ctsThanhPham; // Thêm CTS cho grid ThanhPham
        private CancellationTokenSource _ctsNVL;       // Thêm CTS cho grid NVL
        private bool _isLoadingThanhPham = false;      // Flag cho ThanhPham
        private bool _isLoadingNVL = false;            // Flag cho NVL
        string selectedCol;
        public UC_TruyVetDuLieu()
        {
            InitializeComponent();
            cbxLoaiTimKiem.SelectedIndex = 0;
        }

        private async void cbxTimKiem_TextUpdate(object sender, EventArgs e)
        {
            //ResetController_TimTenSP();
            string searchData = cbxTimKiem.Text;

            if (string.IsNullOrEmpty(searchData)) return;

            // --- thêm debounce + cancel ---
            _searchCts?.Cancel();
            _searchCts = new CancellationTokenSource();
            var token = _searchCts.Token;

            try
            {
                // debounce: đợi user dừng gõ 250ms mới chạy
                await Task.Delay(500, token);

                // gọi async thay vì sync
                await ShowDanhSachLuaChon(searchData, token);
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
                cbxTimKiem.DroppedDown = false;
                return;
            }

            string para = "key";
            bool stt = (cbxLoaiTimKiem.SelectedIndex > 1);
            string query = Helper.Helper.TaoSQL_LayDLTruyVet(stt, para, out selectedCol);

            FrmWaiting waiting = null;
            DataTable sp = null;

            try
            {
                // Hiển thị form chờ
                waiting = new FrmWaiting("Đang tải dữ liệu...");
                waiting.TopMost = true;
                waiting.StartPosition = FormStartPosition.CenterScreen;
                waiting.Show();
                waiting.Refresh();

                // Query database
                sp = await Task.Run(() =>
                {
                    ct.ThrowIfCancellationRequested();
                    return DatabaseHelper.GetData(query, keyword, para);
                }, ct);

                // ✅ Kiểm tra cancel NGAY sau khi có dữ liệu, TRƯỚC khi đóng form
                ct.ThrowIfCancellationRequested();
            }
            catch (OperationCanceledException)
            {
                // Bị hủy, return luôn, không xử lý UI
                return;
            }
            finally
            {
                // Đảm bảo đóng form chờ trong mọi trường hợp
                waiting?.SafeClose();
            }

            // ✅ Đến đây chắc chắn có dữ liệu hợp lệ và không bị cancel
            cbxTimKiem.DroppedDown = false;
            cbxTimKiem.SelectionChangeCommitted -= cbxTimKiem_SelectionChangeCommitted;

            if (sp.Rows.Count == 0) return;

            cbxTimKiem.DataSource = sp;
            cbxTimKiem.DisplayMember = selectedCol;

            string currentText = keyword;

            cbxTimKiem.DroppedDown = true;
            cbxTimKiem.Text = currentText;
            cbxTimKiem.SelectionStart = cbxTimKiem.Text.Length;
            cbxTimKiem.SelectionLength = 0;

            cbxTimKiem.SelectionChangeCommitted += cbxTimKiem_SelectionChangeCommitted;
        }

        private void cbxLoaiTimKiem_SelectedIndexChanged(object sender, EventArgs e)
        {
            cbxTimKiem.SelectedIndex = -1;
            cbxTimKiem.Text = string.Empty;
        }


        //private async void grvChiTietThanhPham_CellClick(object sender, DataGridViewCellEventArgs e)
        //{
            //if (e.RowIndex < 0) return;
            //if (_isLoadingThanhPham) return; // Nếu đang load thì bỏ qua

            //try
            //{
            //    _isLoadingThanhPham = true;
            //    grvChiTietThanhPham.Enabled = false; // Disable grid

            //    DataGridViewRow row = grvChiTietThanhPham.Rows[e.RowIndex];
            //    string cellValue = row.Cells["MaBin"].Value.ToString();

            //    // Hủy request cũ nếu có
            //    _ctsThanhPham?.Cancel();
            //    _ctsThanhPham = new CancellationTokenSource();
            //    var token = _ctsThanhPham.Token;

            //    await getSelectedColAsync(cellValue, grvChiTietNVL, true, token);
            //}
            //catch (OperationCanceledException)
            //{
            //    // Bị hủy, bỏ qua
            //}
            //finally
            //{
            //    grvChiTietThanhPham.Enabled = true; // Enable lại
            //    _isLoadingThanhPham = false;
            //}
        //}

        private async void cbxTimKiem_SelectionChangeCommitted(object sender, EventArgs e)
        {
            if (cbxTimKiem.SelectedItem == null) return;

            if (cbxTimKiem.SelectedItem is DataRowView selectedRow)
            {
                DataTable dtSelected = selectedRow.Row.Table.Clone();
                dtSelected.ImportRow(selectedRow.Row);

                if (grvChiTietThanhPham.DataSource is BindingSource bsGrid)
                {
                    bsGrid.DataSource = dtSelected;
                }
                else
                {
                    grvChiTietThanhPham.DefaultCellStyle.Font = new System.Drawing.Font("Tahoma", 11F, FontStyle.Regular);
                    grvChiTietThanhPham.ColumnHeadersDefaultCellStyle.Font = new System.Drawing.Font("Tahoma", 11F, FontStyle.Regular);
                    grvChiTietThanhPham.DataSource = dtSelected;
                }

                if (grvChiTietThanhPham.Rows.Count > 0)
                {
                    grvChiTietThanhPham.ClearSelection();
                    string maBin = grvChiTietThanhPham.Rows[0].Cells["mabin"].Value.ToString();
                    try
                    {
                        await getSelectedColAsync(maBin, grvChiTietNVL, true, CancellationToken.None);
                    }
                    catch (OperationCanceledException)
                    {
                        // Bỏ qua nếu bị cancel
                    }
                }
            }

            cbxTimKiem.SelectedIndex = -1;
            cbxTimKiem.Text = string.Empty;
        }


        private async Task getSelectedColAsync( string maBin, DataGridView dgrDich, bool loai_MaBin = true, CancellationToken ct = default) // Thêm parameter
        {
            if (string.IsNullOrEmpty(maBin)) return;

            FrmWaiting waiting = null;
            DataTable dlNVVL = null;

            try
            {
                waiting = new FrmWaiting("Đang tải dữ liệu...");
                waiting.TopMost = true;
                waiting.StartPosition = FormStartPosition.CenterScreen;
                waiting.Show();
                waiting.Refresh();

                dlNVVL = await Task.Run(() =>
                {
                    // Kiểm tra cancel trước khi query
                    ct.ThrowIfCancellationRequested();

                    if (loai_MaBin)
                    {
                        return DatabaseHelper.GetThongTinNVLTheoMaBin(maBin);
                    }
                    else
                    {
                        string para = "key";
                        string tempCol;
                        string query = Helper.Helper.TaoSQL_LayDLTruyVet(loai_MaBin, para, out tempCol);
                        return DatabaseHelper.GetData(query, maBin, para);
                    }
                }, ct);

                // Kiểm tra cancel sau khi query xong
                ct.ThrowIfCancellationRequested();
            }
            catch (OperationCanceledException)
            {
                // Request bị hủy, throw lại để event handler bắt
                throw;
            }
            finally
            {
                waiting?.SafeClose();
            }

            // Cập nhật UI
            if (dlNVVL != null)
            {
                dgrDich.AutoGenerateColumns = true;
                dgrDich.DefaultCellStyle.Font = new System.Drawing.Font("Tahoma", 11F, FontStyle.Regular);
                dgrDich.ColumnHeadersDefaultCellStyle.Font = new System.Drawing.Font("Tahoma", 11F, FontStyle.Regular);
                dgrDich.DataSource = dlNVVL;

            }


            // ============================
            // XỬ LÝ CÔNG ĐOẠN NGAY TRONG HÀM
            // ============================

            if (grvChiTietThanhPham.Rows.Count > 0 && grvChiTietThanhPham.Columns.Contains("CongDoan"))
            {
                var value = grvChiTietThanhPham.Rows[0].Cells["CongDoan"].Value;

                if (value != null && int.TryParse(value.ToString(), out int idCongDoan))
                {
                    string tenCD = ThongTinChungCongDoan.GetTenCongDoanById(idCongDoan);
                    if (!string.IsNullOrEmpty(tenCD))
                    {
                        tenCD = char.ToUpper(tenCD[0]) + tenCD.Substring(1).ToLower();
                    }

                    grvChiTietThanhPham.Rows[0].Cells["CongDoan"].Value = tenCD;
                }
            }
        }

        private void grvChiTietThanhPham_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            Console.WriteLine("sdfsdf");
        }

        private void grvChiTietNVL_DoubleClick(object sender, EventArgs e)
        {

        }

        private async void grvChiTietNVL_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (_isLoadingNVL) return; // Nếu đang load thì bỏ qua

            try
            {
                _isLoadingNVL = true;
                grvChiTietNVL.Enabled = false; // Disable grid

                DataGridViewRow row = grvChiTietNVL.Rows[e.RowIndex];
                string cellValue = row.Cells["MaBin"].Value.ToString();
                string maNVL = row.Cells["MaNVL"].Value.ToString();
                maNVL = maNVL.Split('.')[0];

                // Hủy request cũ nếu có
                _ctsNVL?.Cancel();
                _ctsNVL = new CancellationTokenSource();
                var token = _ctsNVL.Token;

                if (maNVL == "NVL")
                {
                    FrmWaiting.ShowGifAlert("ĐỐI TƯỢNG ĐÃ LÀ NVL, KHÔNG TÌM ĐƯỢC THÊM THÔNG TIN");
                    return;
                }

                // Chạy tuần tự 2 lần query, cả 2 đều có thể bị cancel
                await getSelectedColAsync(cellValue, grvChiTietThanhPham, false, token);
                await getSelectedColAsync(cellValue, grvChiTietNVL, true, token);
            }
            catch (OperationCanceledException)
            {
                // Bị hủy, bỏ qua
            }
            finally
            {
                grvChiTietNVL.Enabled = true; // Enable lại
                _isLoadingNVL = false;
            }
        }
    }
}
