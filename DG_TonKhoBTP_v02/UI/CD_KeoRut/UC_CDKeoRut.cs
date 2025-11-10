using DG_TonKhoBTP_v02.Core;
using DG_TonKhoBTP_v02.Dictionary;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
                DKTrucX = (double)dkTrucX.Value,
                DKTrucY = (double)dkTrucY.Value,
                NgoaiQuan = ngoaiQuan.Text,
                TocDo = (double)tocDo.Value,
                DienApU = (double)dienApU.Value,
                DongDienU = (double)dongDienU.Value
            };
        }

        public void LoadData(DataTable dt)
        {
            if (dt == null || dt.Rows.Count == 0) return;
            var row = dt.Rows[0];

            Helper.Helper.SetIfPresent(row, "DKTrucX", val => dkTrucX.Value = Convert.ToDecimal(val));
            Helper.Helper.SetIfPresent(row, "DKTrucY", val => dkTrucY.Value = Convert.ToDecimal(val));
            Helper.Helper.SetIfPresent(row, "KeoRut_NgoaiQuan", val => ngoaiQuan.Text = Convert.ToString(val));
            Helper.Helper.SetIfPresent(row, "TocDo", val => tocDo.Value = Convert.ToDecimal(val));
            Helper.Helper.SetIfPresent(row, "DienApU", val => dienApU.Value = Convert.ToDecimal(val));
            Helper.Helper.SetIfPresent(row, "DongDienU", val => dongDienU.Value = Convert.ToDecimal(val));
        }
    }
}
