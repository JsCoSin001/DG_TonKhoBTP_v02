using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DG_TonKhoBTP_v02.Helper; 

namespace DG_TonKhoBTP_v02.UI
{
    public partial class UC_TTThanhPham : UserControl
    {
        public UC_TTThanhPham()
        {
            InitializeComponent();
        }

        public void ChonMay(string value)
        {
            may.Text = value;
        }

        private void maHanhTrinh_ValueChanged(object sender, EventArgs e)
        {
            maBin.Text = Helper.Helper.LOTGenerated(may, maHanhTrinh, sttCongDoan, sttLo, soBin);
        }

        private void sttCongDoan_SelectedIndexChanged(object sender, EventArgs e)
        {
            maBin.Text = Helper.Helper.LOTGenerated(may, maHanhTrinh, sttCongDoan, sttLo, soBin);
        }

        private void sttLo_ValueChanged(object sender, EventArgs e)
        {
            maBin.Text = Helper.Helper.LOTGenerated(may, maHanhTrinh, sttCongDoan, sttLo, soBin);
        }

        private void soBin_ValueChanged(object sender, EventArgs e)
        {
            maBin.Text = Helper.Helper.LOTGenerated(may, maHanhTrinh, sttCongDoan, sttLo, soBin);
        }
    }
}
