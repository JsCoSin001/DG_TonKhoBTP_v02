using DG_TonKhoBTP_v02.Core;
using DG_TonKhoBTP_v02.Database;
using DG_TonKhoBTP_v02.Dictionary;
using DG_TonKhoBTP_v02.Helper;
using DG_TonKhoBTP_v02.Models;
using System;
using CoreHelper = DG_TonKhoBTP_v02.Helper.Helper;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DG_TonKhoBTP_v02.UI
{
    public partial class UC_TTThanhPham : UserControl, IFormSection, IDataReceiver
    {
        private CancellationTokenSource _searchCts;

        public string tenCongDoan { get; set; }
        public CongDoan congDoan;


        public void SetTenCongDoan(string value) => tenCongDoan = value;

        public event Action<decimal> KhoiLuongChanged;
        public decimal KhoiLuongValue => khoiLuong.Value;

        public event Action<string> SoLOTChanged;
        public string SoLOTValue => soLOT.Text;

        public UC_TTThanhPham(CongDoan cd)
        {
            InitializeComponent();
            SetTenCongDoan(cd.TenCongDoan);
            congDoan = cd;
        }

        public void FocusKhoiLuong()
        {
            khoiLuong.Focus();
            khoiLuong.Select(0, khoiLuong.Text.Length); // chọn hết để gõ lại nhanh
        }


        public void ChonMay(string value)
        {
            may.Text = value;
            soLOT.Text = CoreHelper.LOTGenerated(may, maHanhTrinh, sttCongDoan, sttLo, soBin);
        }

        private void maHanhTrinh_ValueChanged(object sender, EventArgs e)
        {
            soLOT.Text = CoreHelper.LOTGenerated(may, maHanhTrinh, sttCongDoan, sttLo, soBin);
        }

        private void sttCongDoan_SelectedIndexChanged(object sender, EventArgs e)
        {
            soLOT.Text = CoreHelper.LOTGenerated(may, maHanhTrinh, sttCongDoan, sttLo, soBin);
        }

        private void sttLo_ValueChanged(object sender, EventArgs e)
        {
            soLOT.Text = CoreHelper.LOTGenerated(may, maHanhTrinh, sttCongDoan, sttLo, soBin);
        }

        private void soBin_ValueChanged(object sender, EventArgs e)
        {
            soLOT.Text = CoreHelper.LOTGenerated(may, maHanhTrinh, sttCongDoan, sttLo, soBin);
        }

        #region Lấy và load dữ liệu vào form
        public string SectionName => nameof(UC_TTThanhPham);

        public object GetData()
        {
            return new TTThanhPham
            {
                 
                DanhSachSP_ID = int.Parse(id.Text),
                TenTP = ten.Text,
                MaTP = ma.Text,
                DonVi = donVi.Text,
                CongDoan = congDoan,
                MaBin = soLOT?.Text ?? string.Empty,
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
            soLOT.Text = string.Empty;
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

            var likeConditions = string.Join(" OR ", congDoan.ListMa_Accept.Select((m, i) => $"Ma LIKE '{m}'"));

            string query = $@"
                SELECT id, ten, ma, donvi
                FROM DanhSachMaSP
                WHERE ten LIKE '%' || @{para} || '%'
                    AND Ma NOT LIKE 'NVL.%'
                    AND ({likeConditions});
            ";

            DataTable sp = await Task.Run(() =>
            {
                return DatabaseHelper.GetData( query, keyword, para);
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
            donVi.Text = row["donvi"].ToString();

            timNVL.SelectedIndex = -1;
            timNVL.Text = string.Empty;
        }


        private void ResetController_TimTenSP()
        {
            id.Text = string.Empty;
            ma.Text = string.Empty;
            ten.Text = string.Empty;
            donVi.Text = string.Empty;
        }

        public void LoadData(DataTable dt)
        {
            if (dt == null || dt.Rows.Count == 0) return;
            var row = dt.Rows[0];

            string bin = row["MaBin"].ToString();

            CoreHelper.SetIfPresent(row, "id", val => id.Text = Convert.ToString(val));
            CoreHelper.SetIfPresent(row, "Ma", val => ma.Text = Convert.ToString(val));
            CoreHelper.SetIfPresent(row, "Ten", val => ten.Text = Convert.ToString(val));
            CoreHelper.SetIfPresent(row, "donvi", val => donVi.Text = Convert.ToString(val));
            CoreHelper.SetIfPresent(row, "KhoiLuongTruoc", val => khoiLuong.Value = Convert.ToDecimal(val));
            CoreHelper.SetIfPresent(row, "ChieuDaiTruoc", val => chieuDai.Value = Convert.ToDecimal(val));
            CoreHelper.SetIfPresent(row, "Phe", val => phe.Value = Convert.ToDecimal(val));
            CoreHelper.SetIfPresent(row, "GhiChu", val => GhiChu.Text = Convert.ToString(val));

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

        private void khoiLuong_ValueChanged(object sender, EventArgs e)
        {
            KhoiLuongChanged?.Invoke(khoiLuong.Value);
        }

        private void soLOT_TextChanged(object sender, EventArgs e)
        {
            SoLOTChanged?.Invoke(soLOT.Text);
        }
    }
}
