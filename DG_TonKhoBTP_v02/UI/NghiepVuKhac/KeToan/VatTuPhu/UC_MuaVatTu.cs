using DG_TonKhoBTP_v02.Core;
using DG_TonKhoBTP_v02.Database;
using DG_TonKhoBTP_v02.Helper.Reuseable;
using DG_TonKhoBTP_v02.UI.Helper;
using DG_TonKhoBTP_v02.UI.Helper.AutoSearchWithCombobox;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace DG_TonKhoBTP_v02.UI.NghiepVuKhac.KeToan
{
    public partial class UC_MuaVatTu : UserControl
    {
        private ComboBoxSearchHelper<DanhSachMaSP> _vatTuSearchHelper;
        public UC_MuaVatTu()
        {
            InitializeComponent();
        }

        private void UC_MuaVatTu_Load(object sender, EventArgs e)
        {
            var repo = new VatTuRepository();

            _vatTuSearchHelper = new ComboBoxSearchHelper<DanhSachMaSP>(
                comboBox: cbxTimTenVatTu,
                searchFunc: keyword => repo.TimKiemAsync(keyword),
                displaySelector: vt => $"{vt.Ten}",
                onItemSelected: vt =>
                {
                    // Gọi khi người dùng click chọn 1 item trong dropdown
                    dgvDSMua.Rows.Add(vt.Id, vt.Ma, vt.Ten, vt.DonVi);

                    // Reset combobox sau khi chọn xong
                    cbxTimTenVatTu.Text = "";
                    cbxTimTenVatTu.Items.Clear();
                }
            );
        }

        private void dgvDSMua_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == dgvDSMua.Columns["colXoa"].Index && e.RowIndex >= 0)
            {
                var confirm = MessageBox.Show("Bạn có chắc muốn xóa dòng này?", "Xác nhận",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (confirm == DialogResult.Yes)
                    dgvDSMua.Rows.RemoveAt(e.RowIndex);
            }
        }

        private void btnDatHang_Click(object sender, EventArgs e)
        {
            var list = LayDuLieuTuDGV();

            VatTuRepository vt = new VatTuRepository();

            vt.InsertDatHang(list);
        }

        private List<DatHangItem> LayDuLieuTuDGV()
        {
            var list = new List<DatHangItem>();

            foreach (DataGridViewRow row in dgvDSMua.Rows)
            {
                list.Add(new DatHangItem
                {
                    Id = Convert.ToInt32(row.Cells["id"].Value),
                    Ma = row.Cells["ma"].Value?.ToString(),
                    Ten = row.Cells["ten"].Value?.ToString(),
                    DonVi = row.Cells["donvi"].Value?.ToString(),
                    SoLuong = Convert.ToDecimal(row.Cells["soLuong"].Value),
                    DonGia = Convert.ToDecimal(row.Cells["donGia"].Value ?? 0),
                    MucDich = row.Cells["mucDich"].Value?.ToString(),
                    NgayGiao = row.Cells["ngayGiao"].Value?.ToString(),
                });
            }

            return list;
        }
    }
}
