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
    public partial class UC_CDBocMach : UserControl, ISectionProvider<CD_BocMach>, IDataReceiver
    {
        public UC_CDBocMach()
        {
            InitializeComponent();
        }

        public string SectionName => nameof(UC_CDBocMach);

        public CD_BocMach GetSectionData()
        {
            return new CD_BocMach
            {
                TTThanhPhan_ID = 0, // bind khi lưu DB
                NgoaiQuan = string.IsNullOrEmpty(ngoaiQuan.Text) ? null : ngoaiQuan.Text,
                LanDanhThung = (int) lanDanhThung.Value ,
                SoMet =(double?)soMet.Value,
                Mau = string.IsNullOrWhiteSpace(tbMau.Text) ? null : tbMau.Text.Trim()
            };
        }

        public void LoadData(DataTable dt, int kieuEdit)
        {
            if (dt == null || dt.Rows.Count == 0) return;
            var row = dt.Rows[0];

            CoreHelper.SetIfPresent(row, "BocMach_NgoaiQuan", val => ngoaiQuan.Text = Convert.ToString(val));
            CoreHelper.SetIfPresent(row, "LanDanhThung", val => lanDanhThung.Value = Convert.ToDecimal(val));
            CoreHelper.SetIfPresent(row, "SoMet", val => soMet.Text = Convert.ToString(val));
            CoreHelper.SetIfPresent(row, "Mau", val => tbMau.Text = Convert.ToString(val));

        }
    }
}
