using System;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.Windows.Forms;

namespace DG_TonKhoBTP_v02.Helper.Reuseable
{
    public class AutoCompleteDropDown : Form
    {
        private ListBox listBox;
        public DataRow SelectedRow { get; private set; }
        public event Action<DataRow> RowSelected;


        public AutoCompleteDropDown()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.ShowInTaskbar = false;
            this.TopMost = true;
            this.StartPosition = FormStartPosition.Manual;
            this.KeyPreview = true;

            listBox = new ListBox { Dock = DockStyle.Fill };
            listBox.MouseDoubleClick += (s, e) => SelectCurrentItem();
            listBox.KeyDown += ListBox_KeyDown;
            this.Controls.Add(listBox);
            this.Deactivate += (s, e) => this.Hide();
        }



        public void LoadData(DataTable dt, string displayColumn)
        {
            // Phải unbind trước khi bind lại
            listBox.DataSource = null;
            listBox.DataSource = dt;
            listBox.DisplayMember = displayColumn;
            listBox.ValueMember = "";

            this.Height = Math.Min(dt.Rows.Count * listBox.ItemHeight + 6, 200);
        }

        private void ListBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter) SelectCurrentItem();
            if (e.KeyCode == Keys.Escape) this.Hide();
        }

        private void SelectCurrentItem()
        {
            if (listBox.SelectedItem is DataRowView drv)
            {
                SelectedRow = drv.Row;
                this.DialogResult = DialogResult.OK;
                this.Hide();
            }
        }

        public DataRow GetSelected() =>
            listBox.SelectedItem is DataRowView drv ? drv.Row : null;
    }
}
