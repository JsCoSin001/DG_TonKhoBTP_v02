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

        private PrinterModel printerModel;
        List<PrinterModel> dsPrinter;
        string danhSachSP_ID;
        public UC_TachBin()
        {
            InitializeComponent();
        }

        private async void cbxTimKiem_TextUpdate(object sender, EventArgs e)
        {
            string key = cbxTimKiem.Text;

            if (string.IsNullOrWhiteSpace(key)) return;
            
            reset();

            // --- thêm debounce + cancel ---
            _searchCts?.Cancel();
            _searchCts = new CancellationTokenSource();
            var token = _searchCts.Token;

            try
            {
                // debounce: đợi user dừng gõ 250ms mới chạy
                await Task.Delay(250, token);

                // gọi async thay vì sync
                await ShowDanhSachLuaChon(key, token);
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
            string para = "lot";
            string query = @"
                     SELECT TTThanhPham.id, MaBin, KhoiLuongSau, ChieuDaiSau, QC , DanhSachSP_ID ,Ten,ma, ghiChu
                     FROM TTThanhPham 
                     JOIN DanhSachMaSP on DanhSachMaSP.id = TTThanhPham.DanhSachSP_ID
                     WHERE
                        MaBin LIKE '%' || @lot || '%' and KhoiLuongSau <> 0 and active = 1; ";

            DataTable sp = await Task.Run(() =>
            {
                return DatabaseHelper.GetData(query, keyword, para);
            }, ct);

            ct.ThrowIfCancellationRequested();

            cbxTimKiem.DroppedDown = false;

            cbxTimKiem.SelectionChangeCommitted -= cbxTimKiem_SelectionChangeCommitted;
            if (sp.Rows.Count == 0) return;

            cbxTimKiem.DataSource = sp;
            cbxTimKiem.DisplayMember = "MaBin";

            string currentText = keyword;

            cbxTimKiem.DroppedDown = true;
            cbxTimKiem.Text = currentText;
            cbxTimKiem.SelectionStart = cbxTimKiem.Text.Length;
            cbxTimKiem.SelectionLength = 0;

            cbxTimKiem.SelectionChangeCommitted += cbxTimKiem_SelectionChangeCommitted;
        }

        private void cbxTimKiem_SelectionChangeCommitted(object sender, EventArgs e)
        {

            if (cbxTimKiem.SelectedItem == null || !(cbxTimKiem.SelectedItem is DataRowView sel))
                return;


            // Reset combobox SỚM – sau này return kiểu gì cũng đã reset rồi
            cbxTimKiem.SelectedIndex = -1;
            cbxTimKiem.Text = string.Empty;

            tbxLot.Text = sel["MaBin"].ToString();
            danhSachSP_ID = sel["DanhSachSP_ID"].ToString();

            printerModel = new PrinterModel();
            printerModel.id = sel["id"].ToString();
            printerModel.NgaySX = helperApp.GetNgayHienTai();
            printerModel.CaSX = helperApp.GetShiftValue();
            printerModel.KhoiLuong = sel["KhoiLuongSau"].ToString();
            printerModel.ChieuDai = sel["ChieuDaiSau"].ToString();
            printerModel.TenSP = sel["Ten"].ToString();
            printerModel.QC = sel["QC"].ToString();
            printerModel.MaBin = tbxLot.Text;
            printerModel.MaSP = sel["ma"].ToString();
            printerModel.DanhGia = "";
            printerModel.TenCN = tbNguoiTach.Text.Trim();
            printerModel.GhiChu = sel["ghiChu"].ToString() + " - Đã tách";

        }

        private void reset()
        {
            tbxLot.Text = "";
        }

        private void AutoTaoMaBin(object sender, EventArgs e)
        {
            if (printerModel == null) return;


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

            decimal cdTach  = decimal.Parse(printerModel.ChieuDai, CultureInfo.InvariantCulture) / soBinMoi;

            decimal klTach = decimal.Parse(printerModel.KhoiLuong, CultureInfo.InvariantCulture) / soBinMoi; 

            string lot = tbxLot.Text;

            if (!helperApp.SplitByLastDash(lot, out var p1, out var p2))
            {
                FrmWaiting.ShowGifAlert("Lot không đúng định dạng để tách Bin. Vui lòng kiểm tra lại!");
                return;
            }


            List<TachBinModel> dsBinTach = new List<TachBinModel>();

            dsPrinter = new List<PrinterModel>();

            for (int i = 1; i <= soBinMoi; i++)
            {

                TachBinModel bin = new TachBinModel();
                bin.Lot = p1;
                bin.Bin = p2 + "_" + i;
                bin.KhoiLuongSau = Math.Round(klTach, 1);
                bin.qc = printerModel.QC;
                bin.DanhSachSP_ID = danhSachSP_ID;
                bin.ChieuDaiSau = Math.Round(cdTach, 1);
                dsBinTach.Add(bin);

                PrinterModel pr = new PrinterModel(printerModel);
                pr.KhoiLuong = bin.KhoiLuongSau.ToString();
                pr.ChieuDai = bin.ChieuDaiSau.ToString();
                pr.MaBin = bin.Lot + "-" + bin.Bin;
                dsPrinter.Add(pr);
            }

            dgvDsTach.AutoGenerateColumns = false;
            dgvDsTach.DataSource = null;
            dgvDsTach.DataSource = dsBinTach;

            btnInTem.Enabled = true;

        }

        private async void btnInTem_Click(object sender, EventArgs e)
        {
            btnInTem.Enabled = false;

            try
            {
                if ( dsPrinter == null ||dsPrinter.Count == 0)
                {
                    FrmWaiting.ShowGifAlert("Chưa có dữ liệu để in tem!");
                    return;
                }

                List<TTNVL> ls_nvl = new List<TTNVL>
                {                    
                    new TTNVL{
                        Ngay = printerModel.NgaySX,
                        Ca = printerModel.CaSX,
                        DanhSachMaSP_ID = int.Parse(danhSachSP_ID),
                        NguoiLam = printerModel.TenCN,
                        TenNVL = printerModel.TenSP,
                        GhiChu = printerModel.GhiChu,
                        BinNVL = printerModel.MaBin,
                        CdBatDau = double.Parse(printerModel.ChieuDai, CultureInfo.InvariantCulture) ,
                        KlBatDau = double.Parse(printerModel.KhoiLuong, CultureInfo.InvariantCulture),
                        QC = printerModel.QC,
                        CongDoan = ThongTinChungCongDoan.BenRuot.Id,
                        CdConLai = 0,
                        KlConLai = 0
                    }
                };

                ThongTinCaLamViec ttCa = new ThongTinCaLamViec
                {
                    Ca = printerModel.CaSX,
                    Ngay = printerModel.NgaySX,
                    NguoiLam = printerModel.TenCN,
                    May = "Tách Bin",
                };

                List<TTThanhPham> ttTP = new List<TTThanhPham>();

                foreach (var item in dsPrinter)
                {
                    TTThanhPham tp = new TTThanhPham
                    {
                        MaTP = item.MaSP,
                        TenTP = item.TenSP,
                        MaBin = item.MaBin,
                        DonVi = "kg",
                        KhoiLuongSau = double.Parse(item.KhoiLuong, CultureInfo.InvariantCulture),
                        KhoiLuongTruoc = double.Parse(item.KhoiLuong, CultureInfo.InvariantCulture),
                        ChieuDaiSau = double.Parse(item.ChieuDai, CultureInfo.InvariantCulture),
                        ChieuDaiTruoc = double.Parse(item.ChieuDai, CultureInfo.InvariantCulture),
                        GhiChu = item.GhiChu,
                        QC = item.QC,
                        CongDoan = ThongTinChungCongDoan.BenRuot,
                        DanhSachSP_ID = int.Parse(danhSachSP_ID)
                    };
                    ttTP.Add(tp);
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
                btnInTem.Enabled = true;
            }
        }
    }
}
