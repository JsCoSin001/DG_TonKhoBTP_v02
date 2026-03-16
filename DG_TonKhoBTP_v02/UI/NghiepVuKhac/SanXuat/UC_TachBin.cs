using DG_TonKhoBTP_v02.Core;
using DG_TonKhoBTP_v02.Database;
using DG_TonKhoBTP_v02.Dictionary;
using DG_TonKhoBTP_v02.Models;
using DG_TonKhoBTP_v02.Printer;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using helperApp = DG_TonKhoBTP_v02.Helper.Helper;

namespace DG_TonKhoBTP_v02.UI.Actions
{
    public partial class UC_TachBin : UserControl
    {

        private CancellationTokenSource _searchCts;


        private TachBinModel _TachBinModel;

        // Lưu giá trị trước khi edit
        private decimal _prevChieuDai;
        private decimal _prevKhoiLuong;

        private void dgvDsTach_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        {
            var row = dgvDsTach.Rows[e.RowIndex];

            // Khóa dòng cuối
            if (e.RowIndex == dgvDsTach.Rows.Count - 1)
            {
                e.Cancel = true;
                return;
            }

            _prevChieuDai = decimal.TryParse(row.Cells["ChieuDai"].Value?.ToString(), out var cd) ? cd : 0;
            _prevKhoiLuong = decimal.TryParse(row.Cells["KhoiLuong"].Value?.ToString(), out var kl) ? kl : 0;
        }

        private void dgvDsTach_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (_TachBinModel == null) return;

            bool isChieuDai = dgvDsTach.Columns[e.ColumnIndex].Name == "ChieuDai";
            bool isKhoiLuong = dgvDsTach.Columns[e.ColumnIndex].Name == "KhoiLuong";

            if (!isChieuDai && !isKhoiLuong) return;

            var editedRow = dgvDsTach.Rows[e.RowIndex]; // ← khai báo 1 lần duy nhất

            // Kiểm tra giá trị âm hoặc 0
            decimal.TryParse(editedRow.Cells[e.ColumnIndex].Value?.ToString(), out var newVal);
            if (newVal <= 0)
            {
                if (isChieuDai) editedRow.Cells["ChieuDai"].Value = _prevChieuDai;
                if (isKhoiLuong) editedRow.Cells["KhoiLuong"].Value = _prevKhoiLuong;
                FrmWaiting.ShowGifAlert("Giá trị phải lớn hơn 0. Vui lòng kiểm tra lại!");
                return;
            }

            // Tính tổng các dòng trừ dòng cuối
            decimal sumCD = 0, sumKL = 0;
            for (int i = 0; i < dgvDsTach.Rows.Count - 1; i++)
            {
                decimal.TryParse(dgvDsTach.Rows[i].Cells["ChieuDai"].Value?.ToString(), out var cd);
                decimal.TryParse(dgvDsTach.Rows[i].Cells["KhoiLuong"].Value?.ToString(), out var kl);
                sumCD += cd;
                sumKL += kl;
            }

            decimal lastCD = _TachBinModel.ChieuDai - sumCD;
            decimal lastKL = _TachBinModel.KhoiLuong - sumKL;

            if (lastCD <= 0 || lastKL <= 0)
            {
                if (isChieuDai) editedRow.Cells["ChieuDai"].Value = _prevChieuDai;  // ← dùng lại editedRow
                if (isKhoiLuong) editedRow.Cells["KhoiLuong"].Value = _prevKhoiLuong;
                FrmWaiting.ShowGifAlert("Giá trị vượt quá tổng cho phép. Vui lòng kiểm tra lại!");
                return;
            }

            var lastRow = dgvDsTach.Rows[dgvDsTach.Rows.Count - 1];
            lastRow.Cells["ChieuDai"].Value = Math.Round(lastCD, 1);
            lastRow.Cells["KhoiLuong"].Value = Math.Round(lastKL, 1);
            ApplyRowColors();
        }

        //string danhSachSP_ID;
        public UC_TachBin()
        {
            InitializeComponent();

            dgvDsTach.CellBeginEdit += dgvDsTach_CellBeginEdit;
            dgvDsTach.CellEndEdit += dgvDsTach_CellEndEdit;
        }


        private void AutoTaoMaBin(object sender, EventArgs e)
        {
            if (_TachBinModel == null) return;


            decimal soBinMoi = nbrSLTach.Value;

            if (soBinMoi <= 0)
            {
                FrmWaiting.ShowGifAlert("Số lượng Bin không phù hợp");
                return;
            }

            if (String.IsNullOrEmpty(tbNguoiTach.Text))
            {
                FrmWaiting.ShowGifAlert("Thiếu dữ liệu người tách");
                return;
            }

            decimal cdTach = (decimal)_TachBinModel.ChieuDai / soBinMoi;

            decimal klTach = (decimal)_TachBinModel.KhoiLuong / soBinMoi;

            string lot = tbxLot.Text;

            if (!helperApp.SplitByLastDash(lot, out var p1, out var p2))
            {
                FrmWaiting.ShowGifAlert("Lot không đúng định dạng để tách Bin. Vui lòng kiểm tra lại!");
                return;
            }


            List<TachBinModel> dsBinTach = new List<TachBinModel>();

            //dsPrinter = new List<PrinterModel>();

            for (int i = 1; i <= soBinMoi; i++)
            {

                TachBinModel bin = new TachBinModel();
                bin.Lot = p1;
                bin.Bin = p2 + "_" + i;
                bin.KhoiLuong = Math.Round(klTach, 1);
                bin.QC = _TachBinModel.QC;
                bin.DanhSachSP_ID = _TachBinModel.DanhSachSP_ID;
                bin.ChieuDai = Math.Round(cdTach, 1);
                dsBinTach.Add(bin);
            }

            dgvDsTach.AutoGenerateColumns = false;
            dgvDsTach.DataSource = null;
            dgvDsTach.DataSource = dsBinTach;
            dgvDsTach.ClearSelection();
            btnInTem.Enabled = true;

            ApplyRowColors();

        }

        private async void btnInTem_Click(object sender, EventArgs e)
        {
            btnInTem.Enabled = false;

            try
            {

                List<TTNVL> ls_nvl = new List<TTNVL>
            {
                new TTNVL{
                    Ngay = _TachBinModel.NgaySX,
                    Ca = _TachBinModel.CaSX,
                    DanhSachMaSP_ID = (int)(_TachBinModel.DanhSachSP_ID),
                    NguoiLam = _TachBinModel.NguoiThucHien,
                    TenNVL = _TachBinModel.TenSP,
                    GhiChu = _TachBinModel.GhiChu,
                    BinNVL = _TachBinModel.Lot,
                    CdBatDau =  (double)(_TachBinModel.ChieuDai) ,
                    KlBatDau = (double)(_TachBinModel.KhoiLuong),
                    QC = _TachBinModel.QC,
                    CongDoan = ThongTinChungCongDoan.BenRuot.Id,
                    CdConLai = 0,
                    KlConLai = 0
                }
            };

                ThongTinCaLamViec ttCa = new ThongTinCaLamViec
                {
                    Ca = _TachBinModel.CaSX,
                    Ngay = _TachBinModel.NgaySX,
                    NguoiLam = _TachBinModel.NguoiThucHien,
                    May = "Tách Bin",
                };

                List<TTThanhPham> ttTP = new List<TTThanhPham>();

                List<PrinterModel> dsPrinter = new List<PrinterModel>();


                foreach (DataGridViewRow row in dgvDsTach.Rows)
                {
                    if (!row.IsNewRow) // bỏ dòng trống cuối
                    {
                        double chieuDaiSau = 0;
                        double khoiLuongSau = 0;

                        if (row.Cells["ChieuDai"].Value != null)
                            chieuDaiSau = Convert.ToDouble(row.Cells["ChieuDai"].Value);

                        if (row.Cells["KhoiLuong"].Value != null)
                            khoiLuongSau = Convert.ToDouble(row.Cells["KhoiLuong"].Value);

                        string bin = row.Cells["Bin"].Value?.ToString() ?? "";
                        string s = _TachBinModel.Lot;

                        TTThanhPham tp = new TTThanhPham
                        {
                            MaTP = _TachBinModel.MaSP,
                            TenTP = _TachBinModel.TenSP,
                            MaBin = s.Substring(0, s.LastIndexOf('-')) + "-" + bin,
                            DonVi = "KG",
                            KhoiLuongTruoc = (double)_TachBinModel.KhoiLuong,
                            KhoiLuongSau = khoiLuongSau,
                            ChieuDaiTruoc = (double)_TachBinModel.ChieuDai,
                            ChieuDaiSau = chieuDaiSau,
                            GhiChu = _TachBinModel.GhiChu,
                            QC = _TachBinModel.QC,
                            CongDoan = ThongTinChungCongDoan.BenRuot,
                            DanhSachSP_ID = (int)(_TachBinModel.DanhSachSP_ID)
                        };
                        ttTP.Add(tp);


                        PrinterModel pr = new PrinterModel();

                        pr.NgaySX = helperApp.GetNgayHienTai();
                        pr.CaSX = helperApp.GetShiftValue();
                        pr.KhoiLuong = khoiLuongSau == 0 ? "" : khoiLuongSau.ToString();
                        pr.ChieuDai = chieuDaiSau == 0 ? "" : khoiLuongSau.ToString();
                        pr.TenSP = _TachBinModel.TenSP;
                        pr.QC = _TachBinModel.QC;

                        pr.MaBin = tp.MaBin;
                        pr.MaSP = _TachBinModel.MaSP;
                        pr.DanhGia = "";
                        pr.TenCN = tbNguoiTach.Text.Trim();
                        pr.GhiChu = _TachBinModel.GhiChu;

                        dsPrinter.Add(pr);
                    }
                }


                string err = string.Empty;

                var w = new FrmWaiting("Đang xử lý, vui lòng đợi...");
                w.ShowAndRefresh();

                try
                {
                    await Task.Run(() =>
                    {
                        DatabaseHelper.SaveTachBin(ttCa, ttTP, ls_nvl, out err);
                        if (!string.IsNullOrEmpty(err))
                        {
                            FrmWaiting.ShowGifAlert(err);
                            return;
                        }

                        //in tem
                        foreach (PrinterModel p in dsPrinter)
                            PrintHelper.PrintLabel(p);
                    });

                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Lỗi");
                }
                finally
                {
                    w.SafeClose();
                    w.CloseAndDispose();
                }
            }
            finally
            {
                btnInTem.Enabled = false;
                dgvDsTach.DataSource = null;
                dgvDsTach.Rows.Clear();

            }
        }

        private void ApplyRowColors()
        {
            if (dgvDsTach.Rows.Count == 0) return;

            // Màu vàng cho cột ChieuDai và KhoiLuong (tất cả dòng trừ dòng cuối)
            int colCD = dgvDsTach.Columns["ChieuDai"].Index;
            int colKL = dgvDsTach.Columns["KhoiLuong"].Index;
            int lastIdx = dgvDsTach.Rows.Count - 1;

            for (int i = 0; i < lastIdx; i++)
            {
                dgvDsTach.Rows[i].Cells[colCD].Style.BackColor = Color.Yellow;
                dgvDsTach.Rows[i].Cells[colKL].Style.BackColor = Color.Yellow;
            }

            // Màu xám cho toàn bộ dòng cuối
            dgvDsTach.Rows[lastIdx].DefaultCellStyle.BackColor = Color.LightGray;
        }

        private void cbxTimKiem_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter) return;

            _TachBinModel = null;

            e.SuppressKeyPress = true;
            e.Handled = true;

            string lot = cbxTimKiem.Text?.Trim();
            if (string.IsNullOrWhiteSpace(lot))
                return;

            string query = @"
            SELECT TTThanhPham.id, MaBin, KhoiLuongSau, ChieuDaiSau, QC , DanhSachSP_ID ,Ten,ma, ghiChu
            FROM TTThanhPham 
            JOIN DanhSachMaSP on DanhSachMaSP.id = TTThanhPham.DanhSachSP_ID
            WHERE
                MaBin = @lot
                and KhoiLuongSau <> 0 
                and active = 1; ";

            DataTable dt = DatabaseHelper.GetData(query, lot, "lot");
            if (dt == null || dt.Rows.Count == 0)
                return;

            cbxTimKiem.Text = "";
            DataRow sel = dt.Rows[0];
            _TachBinModel = new TachBinModel();

            tbxLot.Text = sel["MaBin"]?.ToString() ?? "";           

            decimal khoiLuong = sel["KhoiLuongSau"] != DBNull.Value
            ? Convert.ToDecimal(sel["KhoiLuongSau"])
            : 0m;

            decimal chieuDai = sel["ChieuDaiSau"] != DBNull.Value
            ? Convert.ToDecimal(sel["ChieuDaiSau"])
            : 0m;

            nbrKhoiLuong.Value = khoiLuong;
            nbrChieuDai.Value = chieuDai;

            lblTen.Text = sel["Ten"]?.ToString() ?? "";

            _TachBinModel.ID = Convert.ToInt32(sel["id"]);
            _TachBinModel.NgaySX = helperApp.GetNgayHienTai();
            _TachBinModel.CaSX = helperApp.GetShiftValue();
            _TachBinModel.KhoiLuong = khoiLuong;
            _TachBinModel.ChieuDai = chieuDai;
            _TachBinModel.TenSP = sel["Ten"]?.ToString() ?? "";
            _TachBinModel.QC = sel["QC"]?.ToString() ?? "";
            _TachBinModel.Lot = sel["MaBin"]?.ToString() ?? "";
            _TachBinModel.MaSP = sel["ma"]?.ToString() ?? "";
            _TachBinModel.NguoiThucHien = tbNguoiTach.Text.Trim();
            _TachBinModel.GhiChu = (sel["ghiChu"]?.ToString() ?? "") + " - Đã tách";
            _TachBinModel.DanhSachSP_ID = Convert.ToDecimal(sel["DanhSachSP_ID"]);

        }
    }
}
