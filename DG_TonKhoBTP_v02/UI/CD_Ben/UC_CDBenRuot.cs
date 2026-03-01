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
    public partial class UC_CDBenRuot : UserControl, ISectionProvider<CD_BenRuot>, IDataReceiver
    {
        private readonly bool _showControls;

        // Mặc định true. Truyền false => ẩn các control trong UC
        public UC_CDBenRuot(bool showControls = true)
        {
            InitializeComponent();
            _showControls = showControls;

            // Ẩn sớm để tránh nháy UI
            ApplyVisibility();
        }

        public string SectionName => nameof(UC_CDBenRuot);

        // Khi control load xong, áp lại 1 lần để chắc chắn
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            ApplyVisibility();
        }

        private void ApplyVisibility()
        {
            bool flg = true;
            lblThongBao.Visible = !flg;
            if (_showControls) return;

            tableBenRuot.Visible = !flg;
            tableBenRuot.Height = 0;
            lblThongBao.Visible = flg;
        }

        public CD_BenRuot GetSectionData()
        {
            // Nếu đang ẩn UI thì thường không lấy dữ liệu => trả object rỗng/null fields
            // (Bạn có thể tùy ý đổi logic này)
            if (!_showControls)
            {
                return new CD_BenRuot
                {
                    TTThanhPhan_ID = 0,
                    DKSoi = -1,
                    SoSoi = -1,
                    ChieuXoan = "Z",
                    BuocBen = -1
                };
            }

            Console.WriteLine(ChieuXoan.Text);
            return new CD_BenRuot
            {
                TTThanhPhan_ID = 0,
                DKSoi = dkSoi.Value == 0 ? (double?)null : (double)dkSoi.Value,
                SoSoi = soSoi.Value == 0 ? (int?)null : (int)soSoi.Value,
                ChieuXoan = string.IsNullOrWhiteSpace(ChieuXoan.Text) ? null : ChieuXoan.Text,
                BuocBen = buocBen.Value == 0 ? (double?)null : (double)buocBen.Value
            };
        }

        public void LoadData(DataTable dt)
        {
            // Nếu đang ẩn UI thì không cần đổ dữ liệu lên control
            if (!_showControls) return;

            if (dt == null || dt.Rows.Count == 0) return;
            var row = dt.Rows[0];

            CoreHelper.SetIfPresent(row, "DKSoi", val => dkSoi.Value = Convert.ToDecimal(val));
            CoreHelper.SetIfPresent(row, "BenRuot_SoSoi", val => soSoi.Value = Convert.ToDecimal(val));
            CoreHelper.SetIfPresent(row, "BenRuot_ChieuXoan", val => ChieuXoan.Text = Convert.ToString(val));
            CoreHelper.SetIfPresent(row, "BuocBen", val => buocBen.Value = Convert.ToDecimal(val));
        }
    }
}
