using DG_TonKhoBTP_v02.Core;
using DG_TonKhoBTP_v02.Database;
using DG_TonKhoBTP_v02.Models;
using DG_TonKhoBTP_v02.Printer;
using System;
using System.Data;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Forms;
using CorHelper = DG_TonKhoBTP_v02.Helper.Helper;

namespace DG_TonKhoBTP_v02.UI.NghiepVuKhac.Kho
{
    public partial class UC_HaLo : Form
    {
        PrinterModel printer;
        public UC_HaLo()
        {
            InitializeComponent();
        }

        private async void cbxLoHa_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter) return;
            e.Handled = true;
            e.SuppressKeyPress = true;

            printer = new PrinterModel();

            string keyword = cbxLoHa.Text?.Trim().ToUpper();

            cbxLoHa.Text = "";   // luôn clear sau khi scan

            if (string.IsNullOrEmpty(keyword)) return;

            string para = "MaBin";
            string sql = @"
            SELECT
                tlv.Ngay                      AS NgaySX,
                tlv.Ca                        AS CaSX,
                CAST(tp.KhoiLuongSau AS TEXT) AS KhoiLuong,
                CAST(tp.ChieuDaiSau  AS TEXT) AS ChieuDai,
                sp.Ten                        AS TenSP,
                tp.QC                         AS QC,
                tp.MaBin                      AS MaBin,
                sp.Ma                         AS MaSP,
                tp.CongDoan                   AS DanhGia,
                tlv.NguoiLam                  AS TenCN,
                COALESCE(tp.GhiChu, '')       AS GhiChu
            FROM TTThanhPham tp
            LEFT JOIN ThongTinCaLamViec tlv
                   ON tlv.TTThanhPham_id = tp.id
            JOIN DanhSachMaSP sp
                 ON sp.id = tp.DanhSachSP_ID
            WHERE tp.MaBin = @MaBin COLLATE NOCASE
              AND (
                    (sp.DonVi = 'KG' AND COALESCE(tp.KhoiLuongSau, 0) <> 0)
                 OR (sp.DonVi = 'M'  AND COALESCE(tp.ChieuDaiSau, 0) <> 0)
                  );";

            DataTable result;

            try
            {
                result = await Task.Run(() =>
                    DatabaseHelper.GetData(sql, keyword, para)
                );
            }
            catch (Exception ex)
            {
                FrmWaiting.ShowGifAlert("Lỗi truy vấn dữ liệu: " + ex.Message);
                return;
            }

            if (result == null || result.Rows.Count == 0)
            {
                FrmWaiting.ShowGifAlert("KHÔNG TÌM THẤY LOT HOẶC LOT ĐÃ HẾT.");
                cbxLoHa.Focus();
                return;
            }

            tbTenSP.Text = result.Rows[0]["TenSP"].ToString();
            mabin.Text = result.Rows[0]["MaBin"].ToString();
            nbrChieuDaiHT.Value = Convert.ToDecimal(result.Rows[0]["ChieuDai"]);
            nbrKhoiLuongHT.Value = Convert.ToDecimal(result.Rows[0]["KhoiLuong"]);

            nbrKhoiLuongCL.Focus();
            nbrKhoiLuongCL.Select(0, nbrKhoiLuongCL.Text.Length);


            printer.NgaySX = result.Rows[0]["NgaySX"].ToString();
            printer.CaSX = result.Rows[0]["CaSX"].ToString();
            printer.TenSP = result.Rows[0]["TenSP"].ToString();
            printer.QC = result.Rows[0]["QC"].ToString();
            printer.MaBin = result.Rows[0]["MaBin"].ToString();
            printer.MaSP = result.Rows[0]["MaSP"].ToString();
            printer.DanhGia = result.Rows[0]["DanhGia"].ToString();
            printer.TenCN = result.Rows[0]["TenCN"].ToString();
            printer.GhiChu = result.Rows[0]["GhiChu"].ToString();
        }

        private void btnLuu_Click(object sender, EventArgs e)
        {
            try
            {
                decimal chieuDaiCL = nbrChieuDaiCL.Value;
                decimal khoiLuongCL = nbrKhoiLuongCL.Value;
                decimal chieuDaiHT = nbrChieuDaiHT.Value;
                decimal khoiLuongHT = nbrKhoiLuongHT.Value;

                string maBin = mabin.Text?.Trim();

                if (chieuDaiCL > chieuDaiHT || khoiLuongCL > khoiLuongHT)
                {
                    FrmWaiting.ShowGifAlert("Dữ liệu nhập không hợp lệ");
                    return;
                }




                DatabaseHelper.Update_KhoiLuongSau_ChieuDaiSau(maBin, khoiLuongCL, chieuDaiCL);

                FrmWaiting.ShowGifAlert("Cập nhật thành công!");
                clearAll();

                if (khoiLuongCL != 0 || chieuDaiCL != 0)
                {
                    printer.ChieuDai = chieuDaiCL.ToString(CultureInfo.InvariantCulture);
                    printer.KhoiLuong = khoiLuongCL.ToString(CultureInfo.InvariantCulture);
                    PrintHelper.PrintLabel(printer);
                }



            }
            catch (Exception ex)
            {
                FrmWaiting.ShowGifAlert(CorHelper.ShowErrorDatabase(ex, "Mã bin"));
            }
        }

        private void btnXoa_Click(object sender, EventArgs e)
        {
            clearAll();
        }

        private void clearAll()
        {
            cbxLoHa.Text = "";
            tbTenSP.Text = "";
            mabin.Text = "";
            nbrChieuDaiHT.Value = 0;
            nbrKhoiLuongHT.Value = 0;
            nbrChieuDaiCL.Value = 0;
            nbrKhoiLuongCL.Value = 0;

            cbxLoHa.Focus();
        }
    }
}
