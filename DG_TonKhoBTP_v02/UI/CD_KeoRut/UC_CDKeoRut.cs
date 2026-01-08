using DG_TonKhoBTP_v02.Core;
using DG_TonKhoBTP_v02.Dictionary;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using CoreHelper = DG_TonKhoBTP_v02.Helper.Helper;
using System.Windows.Forms;

namespace DG_TonKhoBTP_v02.UI
{
    public partial class UC_CDKeoRut : UserControl, ISectionProvider<CD_KeoRut>, IDataReceiver
    {
        public UC_CDKeoRut()
        {
            InitializeComponent();
        }

        public string SectionName => nameof(UC_CDKeoRut);

        public CD_KeoRut GetSectionData()
        {
            return new CD_KeoRut
            {
                TTThanhPhan_ID = 0, // bind FK khi lưu DB
                DKTrucX = dkTrucX.Value == 0m ? null : (double?)dkTrucX.Value,
                DKTrucY = dkTrucY.Value == 0m ? null : (double?)dkTrucY.Value,
                NgoaiQuan = ngoaiQuan.SelectedIndex == -1 ? null : ngoaiQuan.Text,
                TocDo = tocDo.Value == 0m ? null : (double?)tocDo.Value,
                DienApU = (double)dienApU.Value,
                DongDienU = (double)dongDienU.Value
            };
        }

        public void LoadData(DataTable dt)
        {
            if (dt == null || dt.Rows.Count == 0) return;
            var row = dt.Rows[0];

            CoreHelper.SetIfPresent(row, "DKTrucX", val => dkTrucX.Value = Convert.ToDecimal(val));
            CoreHelper.SetIfPresent(row, "DKTrucY", val => dkTrucY.Value = Convert.ToDecimal(val));
            CoreHelper.SetIfPresent(row, "KeoRut_NgoaiQuan", val => ngoaiQuan.Text = Convert.ToString(val));
            CoreHelper.SetIfPresent(row, "TocDo", val => tocDo.Value = Convert.ToDecimal(val));
            CoreHelper.SetIfPresent(row, "DienApU", val => dienApU.Value = Convert.ToDecimal(val));
            CoreHelper.SetIfPresent(row, "DongDienU", val => dongDienU.Value = Convert.ToDecimal(val));
        }
    }
}
