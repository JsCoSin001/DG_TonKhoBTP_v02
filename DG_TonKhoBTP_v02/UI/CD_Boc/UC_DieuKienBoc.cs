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
    public partial class UC_DieuKienBoc : UserControl, ISectionProvider<CaiDatCDBoc>, IDataReceiver
    {
        public UC_DieuKienBoc()
        {
            InitializeComponent();
        }

        #region Lấy và load dữ liệu vào form
        public string SectionName => nameof(UC_DieuKienBoc);

        public CaiDatCDBoc GetSectionData()
        {
            return new CaiDatCDBoc
            {
                DKKhuon1 = dkKhuon1.Value == 0m ? null: (double?)dkKhuon1.Value,
                DKKhuon2 = dkKhuon2.Value == 0m ? null : (double?)dkKhuon2.Value,
                TTNhua = string.IsNullOrEmpty(ttNhua.Text)? null: ttNhua.Text,
                NhuaPhe = nhuaPhe.Value == 0m ? null : (double?)nhuaPhe.Value,
                GhiChuNhuaPhe = ghiChuNhuaPhe?.Text ?? string.Empty,
                DayPhe = dayPhe.Value == 0m ? null : (double?)dayPhe.Value,
                GhiChuDayPhe = ghiChuDayPhe?.Text ?? string.Empty,
                KTDKLan1 = KtDkLan1.Value == 0m ? null : (double?)KtDkLan1.Value,
                KTDKLan2 = KtDkLan2.Value == 0m ? null : (double?)KtDkLan2.Value,
                KTDKLan3 = KtDkLan3.Value == 0m ? null : (double?)KtDkLan3.Value,
                DiemMongLan1 = DiemMongLan1.Value == 0m ? null : (double?)DiemMongLan1.Value,
                DiemMongLan2 = DiemMongLan2.Value == 0m ? null : (double?)DiemMongLan2.Value
            };
        }

        public void LoadData(DataTable dt)
        {
            if (dt == null || dt.Rows.Count == 0) return;
            var row = dt.Rows[0];

            CoreHelper.SetIfPresent(row, "DKKhuon1", val => dkKhuon1.Value = Convert.ToDecimal(val));
            CoreHelper.SetIfPresent(row, "DKKhuon2", val => dkKhuon2.Value = Convert.ToDecimal(val));
            CoreHelper.SetIfPresent(row, "TTNhua", val => ttNhua.Text = Convert.ToString(val));
            CoreHelper.SetIfPresent(row, "NhuaPhe", val => nhuaPhe.Value = Convert.ToDecimal(val));
            CoreHelper.SetIfPresent(row, "GhiChuNhuaPhe", val => ghiChuNhuaPhe.Text = Convert.ToString(val));
            CoreHelper.SetIfPresent(row, "DayPhe", val => dayPhe.Value = Convert.ToDecimal(val));
            CoreHelper.SetIfPresent(row, "GhiChuDayPhe", val => ghiChuDayPhe.Text = Convert.ToString(val));
            CoreHelper.SetIfPresent(row, "KTDKLan1", val => KtDkLan1.Value = Convert.ToDecimal(val));
            CoreHelper.SetIfPresent(row, "KTDKLan2", val => KtDkLan2.Value = Convert.ToDecimal(val));
            CoreHelper.SetIfPresent(row, "KTDKLan3", val => KtDkLan3.Value = Convert.ToDecimal(val));
            CoreHelper.SetIfPresent(row, "DiemMongLan1", val => DiemMongLan1.Value = Convert.ToDecimal(val));
            CoreHelper.SetIfPresent(row, "DiemMongLan2", val => DiemMongLan2.Value = Convert.ToDecimal(val));
        }
        #endregion
    }
}
