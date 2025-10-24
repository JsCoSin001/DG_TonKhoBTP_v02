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
    public partial class UC_DieuKienBoc : UserControl, ISectionProvider<CaiDatCDBoc>, IDataReceiver
    {
        public UC_DieuKienBoc()
        {
            InitializeComponent();
        }

        #region AI generated
        public string SectionName => nameof(UC_DieuKienBoc);

        public CaiDatCDBoc GetSectionData()
        {
            return new CaiDatCDBoc
            {
                DKKhuon1 = (double)dkKhuon1.Value,
                DKKhuon2 = (double)dkKhuon2.Value,
                TTNhua = ttNhua?.Text ?? string.Empty,
                NhuaPhe = (double)nhuaPhe.Value,
                GhiChuNhuaPhe = ghiChuNhuaPhe?.Text ?? string.Empty,
                DayPhe = (double)dayPhe.Value,
                GhiChuDayPhe = ghiChuDayPhe?.Text ?? string.Empty,
                KTDKLan1 = (double)KtDkLan1.Value,
                KTDKLan2 = (double)KtDkLan2.Value,
                KTDKLan3 = (double)KtDkLan3.Value,
                DiemMongLan1 = (double)DiemMongLan1.Value,
                DiemMongLan2 = (double)DiemMongLan2.Value
            };
        }

        public void LoadData(DataTable dt)
        {
            if (dt == null || dt.Rows.Count == 0) return;
            var row = dt.Rows[0];

            Helper.Helper.SetIfPresent(row, "cdb_DKKhuon1", val => dkKhuon1.Value = Convert.ToDecimal(val));
            Helper.Helper.SetIfPresent(row, "cdb_DKKhuon2", val => dkKhuon2.Value = Convert.ToDecimal(val));
            Helper.Helper.SetIfPresent(row, "cdb_TTNhua", val => ttNhua.Text = Convert.ToString(val));
            Helper.Helper.SetIfPresent(row, "cdb_NhuaPhe", val => nhuaPhe.Value = Convert.ToDecimal(val));
            Helper.Helper.SetIfPresent(row, "cdb_GhiChuNhuaPhe", val => ghiChuNhuaPhe.Text = Convert.ToString(val));
            Helper.Helper.SetIfPresent(row, "cdb_DayPhe", val => dayPhe.Value = Convert.ToDecimal(val));
            Helper.Helper.SetIfPresent(row, "cdb_GhiChuDayPhe", val => ghiChuDayPhe.Text = Convert.ToString(val));
            Helper.Helper.SetIfPresent(row, "cdb_KTDKLan1", val => KtDkLan1.Value = Convert.ToDecimal(val));
            Helper.Helper.SetIfPresent(row, "cdb_KTDKLan2", val => KtDkLan2.Value = Convert.ToDecimal(val));
            Helper.Helper.SetIfPresent(row, "cdb_KTDKLan3", val => KtDkLan3.Value = Convert.ToDecimal(val));
            Helper.Helper.SetIfPresent(row, "cdb_DiemMongLan1", val => DiemMongLan1.Value = Convert.ToDecimal(val));
            Helper.Helper.SetIfPresent(row, "cdb_DiemMongLan2", val => DiemMongLan2.Value = Convert.ToDecimal(val));
        }
        #endregion
    }
}
