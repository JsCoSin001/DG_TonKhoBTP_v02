using DG_TonKhoBTP_v02.Core;
using DG_TonKhoBTP_v02.Helper;
using DocumentFormat.OpenXml.Spreadsheet;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Control = System.Windows.Forms.Control;

namespace DG_TonKhoBTP_v02.UI
{
    public partial class UC_TTSanPham : UserControl
    {
        

        public UC_TTSanPham(params Control[] controls)
        {
            InitializeComponent();
            this.StartForm(controls);
        }

        private void StartForm(params Control[] controls)
        {
            foreach (var ctrl in controls)
            {
                ctrl.Dock = DockStyle.Top;
                this.panel1.Controls.Add(ctrl);
            }

        }

        #region AI generated
        public IDictionary<string, object> GetAggregateSections()
        {
            // Quét từ *chính UC_TTSanPham* trở xuống.
            return ProviderAggregator.AggregateSectionsFrom(this);
        }

        #endregion


    }
}
