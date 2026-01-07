using DG_TonKhoBTP_v02.Database;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DG_TonKhoBTP_v02.UI.KeHoach
{
    public partial class KeHoach : Form
    {
        private int defaultHeight = 30;
        public KeHoach()
        {
            InitializeComponent();

            dgPlan.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
            dgPlan.RowTemplate.Height = defaultHeight;

            dgPlan.DefaultValuesNeeded += dataGridView1_DefaultValuesNeeded;

            this.Shown += (s, e) => ApplyRowHeight();   // quan trọng: chạy sau khi form hiện
            dgPlan.RowsAdded += (s, e) => ApplyRowHeight();
        }

        private void dataGridView1_DefaultValuesNeeded(object sender, DataGridViewRowEventArgs e)
        {
            var col = (DataGridViewComboBoxColumn)dgPlan.Columns["status"];
            if (col.Items.Count > 0)
                e.Row.Cells["status"].Value = col.Items[0]; // tự chọn item đầu tiên
        }

        private void ApplyRowHeight()
        {
            foreach (DataGridViewRow r in dgPlan.Rows)
                r.Height = defaultHeight;

            // ép riêng cho dòng trắng (New Row)
            if (dgPlan.AllowUserToAddRows && dgPlan.NewRowIndex >= 0)
                dgPlan.Rows[dgPlan.NewRowIndex].Height = defaultHeight;
        }

        private void btnSavePlan_Click(object sender, EventArgs e)
        {
            DatabaseHelper.TaoKeHoachMoi(dgPlan);
        }
    }
}
