using DG_TonKhoBTP_v02.Helper;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace QLDuLieuTonKho_BTP
{
    public partial class Uc_HomePage : UserControl
    {
        public Uc_HomePage()
        {
            InitializeComponent();
            ApplyBranding();
        }

        private void ApplyBranding()
        {
            BrandingService.Apply(
                pictureBox1,
                null,
                label1,
                null,
                BrandingService.HomeLogoPath);
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }
    }
}
