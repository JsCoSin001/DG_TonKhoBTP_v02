using DG_TonKhoBTP_v02.Database.KeToan;
using DG_TonKhoBTP_v02.Dictionary;
using DG_TonKhoBTP_v02.Models.KeToan;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace DG_TonKhoBTP_v02.UI.NghiepVuKhac.KeToan
{
    public partial class UC_KiemTraDuLieu : UserControl
    {
        private const string TEXT_XAC_NHAN = "Xác nhận";
        private const string TEXT_OK = "OK";
        private const string TEXT_NA = "";

        public UC_KiemTraDuLieu()
        {
            InitializeComponent();
            CauHinhGridBangSoSanhBom();
        }

        private void CauHinhGridBangSoSanhBom()
        {
            grvBangSoSanhBom.AutoGenerateColumns = false;
            grvBangSoSanhBom.AllowUserToAddRows = false;
            grvBangSoSanhBom.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grvBangSoSanhBom.MultiSelect = false;

            // Tăng chiều cao dòng
            grvBangSoSanhBom.RowTemplate.Height = 35;
            grvBangSoSanhBom.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;

            if (grvBangSoSanhBom.Columns.Contains("id_KhacBietBom"))
                grvBangSoSanhBom.Columns["id_KhacBietBom"].Visible = false;

            foreach (DataGridViewColumn col in grvBangSoSanhBom.Columns)
                col.ReadOnly = col.Name != "confirm";

            if (grvBangSoSanhBom.Columns["confirm"] is DataGridViewButtonColumn btnCol)
                btnCol.UseColumnTextForButtonValue = false;

            grvBangSoSanhBom.CellContentClick -= grvBangSoSanhBom_CellContentClick;
            grvBangSoSanhBom.CellContentClick += grvBangSoSanhBom_CellContentClick;
        }

        private void btnLayDS_Click(object sender, EventArgs e)
        {
            try
            {
                List<KiemTraDuLieuSXNhap_Model> ds = KiemTraDuLieuSXNhap_DB.LayDanhSachKhacBietBomChuaXacNhan();

                if (ds.Count == 0)
                {
                    FrmWaiting.ShowGifAlert("Không có dữ liệu phù hợp");
                    return;
                }

                foreach (var item in ds)
                    GanTenCongDoanHienThi(item);

                grvBangSoSanhBom.DataSource = null;
                grvBangSoSanhBom.DataSource = ds;

                //ToMauCongDoanNA();
            }
            catch (Exception ex)
            {
                FrmWaiting.ShowGifAlert("Lỗi khi lấy danh sách khác biệt BOM");
            }
        }

        private void grvBangSoSanhBom_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0)
                return;

            if (grvBangSoSanhBom.Columns[e.ColumnIndex].Name != "confirm")
                return;

            var row = grvBangSoSanhBom.Rows[e.RowIndex];
            if (row.IsNewRow)
                return;

            int idKhacBietBom = Convert.ToInt32(row.Cells["id_KhacBietBom"].Value);
            string currentText = Convert.ToString(row.Cells["confirm"].Value);

            try
            {
                if (currentText == TEXT_OK)
                {
                    var confirmResult = MessageBox.Show(
                        "Bạn đang đổi trạng thái xác nhận",
                        "Xác nhận",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (confirmResult != DialogResult.Yes)
                        return;

                    if (!KiemTraDuLieuSXNhap_DB.CapNhatConfirmedKhacBietBom(idKhacBietBom, false))
                    {
                        FrmWaiting.ShowGifAlert("Cập nhật trạng thái thất bại.");

                        return;
                    }

                    DoiTextNutConfirm(row, TEXT_XAC_NHAN);
                }
                else
                {
                    if (!KiemTraDuLieuSXNhap_DB.CapNhatConfirmedKhacBietBom(idKhacBietBom, true))
                    {
                        FrmWaiting.ShowGifAlert("Cập nhật trạng thái thất bại.");
                        return;
                    }

                    DoiTextNutConfirm(row, TEXT_OK);
                }
            }
            catch (Exception ex)
            {
                FrmWaiting.ShowGifAlert("Lỗi cập nhật trạng thái.");
            }
        }

        private void DoiTextNutConfirm(DataGridViewRow row, string text)
        {
            row.Cells["confirm"].Value = text;

            if (row.DataBoundItem is KiemTraDuLieuSXNhap_Model item)
                item.confirm = text;
        }

        private void GanTenCongDoanHienThi(KiemTraDuLieuSXNhap_Model item)
        {
            item.congDoanTP = LayTenCongDoanOrNA(item.CongDoanTP_ID, true, out bool tpNA);
            item.IsCongDoanTP_NA = tpNA;

            item.congDoanTTe = LayTenCongDoanOrNA(
                item.CongDoanTTe_ID,
                !item.IsCongDoanTTe_NullFromDB,
                out bool tteNA);

            item.IsCongDoanTTe_NA = tteNA;
        }

        private string LayTenCongDoanOrNA(int? idCongDoan, bool toMauKhiNA, out bool isNA)
        {
            if (!idCongDoan.HasValue)
            {
                isNA = false;
                return TEXT_NA;
            }

            var congDoan = ThongTinChungCongDoan.TatCaCongDoan.FirstOrDefault(x => x.Id == idCongDoan.Value);
            if (congDoan == null)
            {
                isNA = toMauKhiNA;
                return TEXT_NA;
            }

            isNA = false;
            return congDoan.TenCongDoan;
        }

        private void ToMauCongDoanNA()
        {
            foreach (DataGridViewRow row in grvBangSoSanhBom.Rows)
            {
                if (row.IsNewRow)
                    continue;

                if (row.DataBoundItem is KiemTraDuLieuSXNhap_Model item)
                {
                    ToMauOTheoTrangThai(row, "congDoanTP", item.IsCongDoanTP_NA);
                    ToMauOTheoTrangThai(row, "congDoanTTe", item.IsCongDoanTTe_NA);
                }
            }
        }

        private void ToMauOTheoTrangThai(DataGridViewRow row, string columnName, bool isNA)
        {
            if (!grvBangSoSanhBom.Columns.Contains(columnName))
                return;

            row.Cells[columnName].Style.BackColor = isNA ? Color.Yellow : Color.Empty;
        }
    }
}
