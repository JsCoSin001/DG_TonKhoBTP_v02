using DG_TonKhoBTP_v02.Core;
using DG_TonKhoBTP_v02.Database;
using DG_TonKhoBTP_v02.Dictionary;
using DG_TonKhoBTP_v02.Helper;
using DG_TonKhoBTP_v02.Models;
using System;
using CoreHelper = DG_TonKhoBTP_v02.Helper.Helper;
using System.Data;
using System.Linq;
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

        public event Action<(decimal KhoiLuong, decimal ChieuDai, string donVi)> KL_CD_Changed;
        public decimal KhoiLuongValue => khoiLuong.Value;
        public decimal ChieuDaiValue => chieuDai.Value;
        public string DonVi => donVi.Text;

        public event Action<string> SoLOTChanged;
        public string SoLOTValue => soLOT.Text;

        public UC_TTThanhPham(CongDoan cd)
        {
            InitializeComponent();

            SetTenCongDoan(cd.TenCongDoan);
            congDoan = cd;

            timNVL.KeyDown += timNVL_KeyDown;
        }

        public void FocusKhoiLuong()
        {
            khoiLuong.Focus();
            khoiLuong.Select(0, khoiLuong.Text.Length);
        }

        public void ChonMay(string value)
        {
            may.Text = value;
            CapNhatSoLot();
        }

        private void CapNhatSoLot()
        {
            soLOT.Text = CoreHelper.LOTGenerated(may, maHanhTrinh, sttCongDoan, sttLo, soBin);
        }

        private void maHanhTrinh_ValueChanged(object sender, EventArgs e)
        {
            CapNhatSoLot();
        }

        private void sttCongDoan_SelectedIndexChanged(object sender, EventArgs e)
        {
            CapNhatSoLot();
        }

        private void sttLo_ValueChanged(object sender, EventArgs e)
        {
            CapNhatSoLot();
        }

        private void soBin_ValueChanged(object sender, EventArgs e)
        {
            CapNhatSoLot();
        }

        #region Lấy và load dữ liệu vào form

        public string SectionName => nameof(UC_TTThanhPham);

        public object GetData()
        {
            int.TryParse(id.Text, out int danhSachSpId);

            return new TTThanhPham
            {
                DanhSachSP_ID = danhSachSpId,
                TenTP = ten.Text,
                MaTP = ma.Text,
                DonVi = donVi.Text,
                CongDoan = congDoan,
                MaBin = soLOT?.Text ?? string.Empty,
                KhoiLuongTruoc = (double)khoiLuong.Value,
                KhoiLuongSau = (double)khoiLuong.Value,
                ChieuDaiTruoc = (double)chieuDai.Value,
                ChieuDaiSau = (double)chieuDai.Value,
                Phe = (double)phe.Value,
                GhiChu = GhiChu?.Text ?? string.Empty,
                DateInsert = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };
        }

        public void ClearInputs()
        {
            _searchCts?.Cancel();

            timNVL.DataSource = null;
            timNVL.Items.Clear();
            timNVL.Text = string.Empty;
            timNVL.DroppedDown = false;

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

            string tenTP = timNVL.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(tenTP))
            {
                timNVL.DroppedDown = false;
                timNVL.DataSource = null;
                return;
            }

            _searchCts?.Cancel();
            _searchCts?.Dispose();
            _searchCts = new CancellationTokenSource();
            var token = _searchCts.Token;

            try
            {
                await Task.Delay(250, token);
                await ShowDanhSachLuaChon(tenTP, token);
            }
            catch (OperationCanceledException)
            {
            }
            catch
            {
                timNVL.DroppedDown = false;
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
            string likeConditions = string.Join(" OR ", congDoan.ListMa_Accept.Select(m => $"Ma LIKE '{m}'"));

            string query = $@"
                SELECT id, ten, ma, donvi
                FROM DanhSachMaSP
                WHERE ten LIKE '%' || @{para} || '%'
                  AND Ma NOT LIKE 'NVL.%'
                  AND ({likeConditions});
            ";

            DataTable sp = await Task.Run(() =>
            {
                return DatabaseHelper.GetData(query, keyword, para);
            }, ct);

            ct.ThrowIfCancellationRequested();

            string currentText = keyword;

            timNVL.SelectionChangeCommitted -= timNVL_SelectionChangeCommitted;

            timNVL.DroppedDown = false;
            timNVL.DataSource = null;

            if (sp == null || sp.Rows.Count == 0)
            {
                timNVL.Text = currentText;
                timNVL.SelectionStart = timNVL.Text.Length;
                timNVL.SelectionLength = 0;
                return;
            }

            timNVL.DisplayMember = "ten";
            timNVL.ValueMember = "id";
            timNVL.DataSource = sp;

            timNVL.Text = currentText;
            timNVL.SelectionStart = timNVL.Text.Length;
            timNVL.SelectionLength = 0;

            timNVL.DroppedDown = true;
            Cursor.Current = Cursors.Default;
            Cursor.Show();

            timNVL.SelectionChangeCommitted += timNVL_SelectionChangeCommitted;
        }

        private void FillSelectedThanhPham(DataRowView row)
        {
            if (row == null) return;

            ten.Text = row["ten"]?.ToString() ?? string.Empty;
            ma.Text = row["ma"]?.ToString() ?? string.Empty;
            id.Text = row["id"]?.ToString() ?? string.Empty;
            donVi.Text = row["donvi"]?.ToString() ?? string.Empty;

            timNVL.DroppedDown = false;
            timNVL.SelectedIndex = -1;
            timNVL.Text = string.Empty;
        }

        private void timNVL_SelectionChangeCommitted(object sender, EventArgs e)
        {
            if (!(timNVL.SelectedItem is DataRowView row)) return;
            FillSelectedThanhPham(row);
        }

        private void timNVL_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Down)
            {
                if (!timNVL.DroppedDown && timNVL.DataSource != null)
                {
                    timNVL.DroppedDown = true;
                }
                return;
            }

            if (e.KeyCode == Keys.Enter)
            {
                if (timNVL.SelectedItem is DataRowView row)
                {
                    FillSelectedThanhPham(row);
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
            }
        }

        private void ResetController_TimTenSP()
        {
            id.Text = string.Empty;
            ma.Text = string.Empty;
            ten.Text = string.Empty;
            donVi.Text = string.Empty;
        }

        public void LoadData(DataTable dt, int kieuDL)
        {
            if (dt == null || dt.Rows.Count == 0) return;

            var row = dt.Rows[0];
            string bin = row["MaBin"]?.ToString() ?? string.Empty;

            CoreHelper.SetIfPresent(row, "DanhSachMaSP_ID", val => id.Text = Convert.ToString(val));
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
            KL_CD_Changed?.Invoke((KhoiLuongValue, ChieuDaiValue, donVi.Text));
        }

        private void may_TextChanged(object sender, EventArgs e)
        {
            SoLOTChanged?.Invoke(may.Text);
        }

        private void chieuDai_ValueChanged(object sender, EventArgs e)
        {
            KL_CD_Changed?.Invoke((KhoiLuongValue, ChieuDaiValue, donVi.Text));
        }
    }
}