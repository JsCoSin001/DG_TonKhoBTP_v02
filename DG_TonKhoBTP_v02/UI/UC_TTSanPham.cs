using DG_TonKhoBTP_v02.Helper;
using DG_TonKhoBTP_v02.Models;
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
    public partial class UC_TTSanPham : UserControl
    {
       
        public UC_TTSanPham()
        {
            InitializeComponent();
            this.StartForm();
        }

        private void StartForm()
        {
            UC_CaiDatMay uC_CaiDatMay = new UC_CaiDatMay();
            uC_CaiDatMay.Dock = DockStyle.Top;

            UC_DieuKienBoc uC_DieuKienBoc = new UC_DieuKienBoc();
            uC_DieuKienBoc.Dock = DockStyle.Top;


            UC_CDKeoRut uc_CongDoan = new UC_CDKeoRut();
            uc_CongDoan.Dock = DockStyle.Top;

            UC_SubmitForm uC_SubmitForm = new UC_SubmitForm();
            uC_SubmitForm.Dock = DockStyle.Top;

            this.panel1.Controls.Add(uC_SubmitForm);
            this.panel1.Controls.Add(uc_CongDoan);
            this.panel1.Controls.Add(uC_DieuKienBoc);
            this.panel1.Controls.Add(uC_CaiDatMay);

        }


    }
}
