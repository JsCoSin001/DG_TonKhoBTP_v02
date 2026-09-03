using DG_TonKhoBTP_v02.Core;
using System;
using System.Data;
using System.Windows.Forms;
using CoreHelper = DG_TonKhoBTP_v02.Helper.Helper;
using CDChieuXaData = DG_TonKhoBTP_v02.Core.CD_ChieuXa;

namespace DG_TonKhoBTP_v02.UI.CD_ChieuXa
{
    public partial class UC_ChieuXa : UserControl, ISectionProvider<CDChieuXaData>, IDataReceiver
    {
        public UC_ChieuXa()
        {
            InitializeComponent();
        }

        public string SectionName => nameof(UC_ChieuXa);

        public CDChieuXaData GetSectionData()
        {
            return new CDChieuXaData
            {
                TTThanhPhan_ID = 0,
                LucCangThu = lucCangThu.Value > 0m ? (double?)lucCangThu.Value : null,
                LucCangTha = lucCangTha.Value > 0m ? (double?)lucCangTha.Value : null,
                SoVong = soVong.Value > 0m ? (int?)soVong.Value : null,
                TocDo = tocDo.Value > 0m ? (double?)tocDo.Value : null,
                NLCX = nangLuong.Value > 0m ? (double?)nangLuong.Value : null,
                DongDien = dongDien.Value > 0m ? (double?)dongDien.Value : null,
                LieuChieu = lieuChieu.Value > 0m ? (double?)lieuChieu.Value : null,
                NgoaiQuan = ngoaiQuan.SelectedIndex >= 0 ? ngoaiQuan.Text : null,
                DoChiuNhiet = chiuNhiet.SelectedIndex >= 0 ? chiuNhiet.Text : null
            };
        }

        public void LoadData(DataTable dt, int kieuEdit)
        {
            if (dt == null || dt.Rows.Count == 0) return;

            DataRow row = dt.Rows[0];
            CoreHelper.SetIfPresent(row, "ChieuXa_LucCangThu", val => lucCangThu.Value = Convert.ToDecimal(val));
            CoreHelper.SetIfPresent(row, "ChieuXa_LucCangTha", val => lucCangTha.Value = Convert.ToDecimal(val));
            CoreHelper.SetIfPresent(row, "ChieuXa_SoVong", val => soVong.Value = Convert.ToDecimal(val));
            CoreHelper.SetIfPresent(row, "ChieuXa_TocDo", val => tocDo.Value = Convert.ToDecimal(val));
            CoreHelper.SetIfPresent(row, "ChieuXa_NLCX", val => nangLuong.Value = Convert.ToDecimal(val));
            CoreHelper.SetIfPresent(row, "ChieuXa_DongDien", val => dongDien.Value = Convert.ToDecimal(val));
            CoreHelper.SetIfPresent(row, "ChieuXa_LieuChieu", val => lieuChieu.Value = Convert.ToDecimal(val));
            CoreHelper.SetIfPresent(row, "ChieuXa_NgoaiQuan", val => ngoaiQuan.Text = Convert.ToString(val));
            CoreHelper.SetIfPresent(row, "ChieuXa_DoChiuNhiet", val => chiuNhiet.Text = Convert.ToString(val));
        }
    }
}
