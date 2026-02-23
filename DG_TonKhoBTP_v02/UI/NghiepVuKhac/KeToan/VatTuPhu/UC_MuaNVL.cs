using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DG_TonKhoBTP_v02.UI.NghiepVuKhac.KeToan
{
    public partial class UC_MuaNVL : UserControl
    {
        public UC_MuaNVL()
        {
            InitializeComponent();
            dataGridView1.RowTemplate.Height = 40;
            dataGridView1.Columns[1].ReadOnly = true; 
            dataGridView1.Columns[3].ReadOnly = true; 
            dataGridView1.Columns[7].ReadOnly = true;

           
        }

    }
}
