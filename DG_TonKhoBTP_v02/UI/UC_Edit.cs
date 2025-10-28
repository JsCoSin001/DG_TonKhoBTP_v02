using DG_TonKhoBTP_v02.Core;
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
    public partial class UC_Edit : UserControl, IFormSection
    {
        CongDoan _cd;


        public event EventHandler<DataTable> DataTableSubmitted;

        public UC_Edit(CongDoan cd)
        {
            InitializeComponent();
            _cd = cd;
        }

        private void btnTim_Click(object sender, EventArgs e)
        {
            long stt = (long)sttLoi.Value;
            btnTim.Enabled = false;
            try
            {
                if (stt <= 0)
                {
                    MessageBox.Show("STT KHÔNG HỢP LỆ!", "Thông báo",
                                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                DataTable dt = Database.DatabaseHelper.GetDataByID(stt.ToString(), _cd);

                // check if dt has rows
                if (dt.Rows.Count == 0)
                {
                    MessageBox.Show("STT LỖI KHÔNG TỒN TAỊ!", "Thông báo",
                                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                DataTableSubmitted?.Invoke(this, dt);
            }
            finally
            {
                btnTim.Enabled = true;
            }
        }

        public string SectionName => nameof(UC_Edit);

        public object GetData()
        {
            return new EditModel
            {
                Id = (int)sttLoi.Value
            };
        }

        public void ClearInputs()
        {
            sttLoi.Value = 0;
        }
    }
}
