using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DG_TonKhoBTP_v02.UI.NghiepVuKhac.KeToan.VatTuPhu
{
    public partial class FrmVatTuPhu : Form
    {
        public FrmVatTuPhu()
        {
            InitializeComponent();
            this.WindowState = FormWindowState.Maximized;
        }

        private void FrmVatTuPhu_Load(object sender, EventArgs e)
        {
            showMuaVatTu();
        }

        private void muaVậtTưToolStripMenuItem_Click(object sender, EventArgs e)
        {
            showMuaVatTu();
        }

        private void showMuaVatTu(int kieuForm = 1)
        {
            UC_MuaVatTu uc_MuaVatTu = new UC_MuaVatTu(kieuForm);
            uc_MuaVatTu.Dock = DockStyle.Fill;
            pnMainVatTuPhu.Controls.Clear();
            pnMainVatTuPhu.Controls.Add(uc_MuaVatTu);
        }

        private void muaDịchVụToolStripMenuItem_Click(object sender, EventArgs e)
        {
            showMuaVatTu(2);
        }
    }
}
