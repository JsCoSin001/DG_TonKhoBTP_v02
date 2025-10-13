using DG_TonKhoBTP_v02.Core;
using DG_TonKhoBTP_v02.Database;
using DG_TonKhoBTP_v02.Dictionary;
using DG_TonKhoBTP_v02.Helper; 
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

namespace DG_TonKhoBTP_v02.UI
{
    public partial class UC_TTThanhPham : UserControl, IFormSection
    {
        private CancellationTokenSource _searchCts;

        public string CongDoan { get; set; }
        public void SetTenCongDoan(string value) => CongDoan = value;

        public UC_TTThanhPham(string tenCD)
        {
            InitializeComponent();
            SetTenCongDoan(tenCD);
        }

        public void ChonMay(string value)
        {
            may.Text = value;
        }

        private void maHanhTrinh_ValueChanged(object sender, EventArgs e)
        {
            maBin.Text = Helper.Helper.LOTGenerated(may, maHanhTrinh, sttCongDoan, sttLo, soBin);
        }

        private void sttCongDoan_SelectedIndexChanged(object sender, EventArgs e)
        {
            maBin.Text = Helper.Helper.LOTGenerated(may, maHanhTrinh, sttCongDoan, sttLo, soBin);
        }

        private void sttLo_ValueChanged(object sender, EventArgs e)
        {
            maBin.Text = Helper.Helper.LOTGenerated(may, maHanhTrinh, sttCongDoan, sttLo, soBin);
        }

        private void soBin_ValueChanged(object sender, EventArgs e)
        {
            maBin.Text = Helper.Helper.LOTGenerated(may, maHanhTrinh, sttCongDoan, sttLo, soBin);
        }

        #region AI generated
        public string SectionName => nameof(UC_TTThanhPham);

        public object GetData()
        {
            return new TTThanhPham
            {
                 
                DanhSachSP_ID = int.Parse(id.Text),
                ThongTinCaLamViec_ID = 0,
                CongDoan = CongDoan,
                MaBin = maBin?.Text ?? string.Empty,
                KhoiLuongTruoc = (double)khoiLuong.Value, // Tạo mới đặt KL trước = kl sau
                KhoiLuongSau = (double)khoiLuong.Value,
                ChieuDaiTruoc = (double)chieuDai.Value, // Tạo mới đặt CD trước = CD sau
                ChieuDaiSau = (double)chieuDai.Value,
                Phe = (double)phe.Value,
                GhiChu = GhiChu?.Text ?? string.Empty,
                DateInsert = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };
        }

        public void ClearInputs()
        {
            timNVL.Text = string.Empty;
            ResetController_TimTenSP();
            may.SelectedIndex = -1;
            maHanhTrinh.Value = maHanhTrinh.Minimum;
            sttCongDoan.SelectedIndex = -1;
            sttLo.Value = sttLo.Minimum;
            soBin.Value = soBin.Minimum;
            maBin.Text = string.Empty;
            khoiLuong.Value = khoiLuong.Minimum;
            chieuDai.Value = chieuDai.Minimum;
            phe.Value = phe.Minimum;
            GhiChu.Text = string.Empty;
        }



        #endregion

        private async void timNVL_TextUpdate(object sender, EventArgs e)
        {
            ResetController_TimTenSP();
            string tenTP = timNVL.Text;
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
                timNVL.DroppedDown = false;
                return;
            }
            string para = "ten";

            string query = @"
                SELECT id, ten, ma
                FROM DanhSachMaSP
                WHERE ten LIKE '%' || @para || '%'
                  AND ma NOT LIKE 'NVL.%';
            ";

            // --- sửa: chạy query trong Task.Run để không block UI ---
            DataTable sp = await Task.Run(() =>
            {
                return DatabaseHelper.GetData(keyword, query, para);
            }, ct);

            ct.ThrowIfCancellationRequested();

            timNVL.DroppedDown = false;

            timNVL.SelectionChangeCommitted -= timNVL_SelectionChangeCommitted; // tránh trùng event
            if (sp.Rows.Count == 0) return;

            timNVL.DataSource = sp;
            timNVL.DisplayMember = "ten";

            string currentText = keyword;

            timNVL.DroppedDown = true;
            timNVL.Text = currentText;
            timNVL.SelectionStart = timNVL.Text.Length;
            timNVL.SelectionLength = 0;

            timNVL.SelectionChangeCommitted += timNVL_SelectionChangeCommitted;
        }

        private void timNVL_SelectionChangeCommitted(object sender, EventArgs e)
        {
            if (timNVL.SelectedItem == null || !(timNVL.SelectedItem is DataRowView)) return;
            DataRowView row = (DataRowView)timNVL.SelectedItem;

            ten.Text = row["ten"].ToString();
            ma.Text = row["ma"].ToString();
            id.Text = row["id"].ToString();

            timNVL.SelectedIndex = -1;
            timNVL.Text = string.Empty;
        }


        private void ResetController_TimTenSP()
        {
            id.Text = string.Empty;
            ma.Text = string.Empty;
            ten.Text = string.Empty;
        }

        
    }
}
