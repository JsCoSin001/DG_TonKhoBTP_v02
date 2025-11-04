using DG_TonKhoBTP_v02.Core;
using DG_TonKhoBTP_v02.Database;
using DG_TonKhoBTP_v02.Dictionary;
using DG_TonKhoBTP_v02.Helper;
using DG_TonKhoBTP_v02.Models;
using DG_TonKhoBTP_v02.Printer;
using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Office2019.Excel.RichData2;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DG_TonKhoBTP_v02.UI
{
    public partial class UC_TonKho : UserControl
    {

        private CancellationTokenSource _searchCts;
        public UC_TonKho()
        {
            InitializeComponent();
            cbxKeoRut.Tag = ThongTinChungCongDoan.KeoRut;
            cbxBen.Tag = ThongTinChungCongDoan.BenRuot;
            cbxQuanMica.Tag = ThongTinChungCongDoan.Mica;
            cbxBocCachDien.Tag = ThongTinChungCongDoan.BocMach;
            cbxGhepLoi.Tag = ThongTinChungCongDoan.GhepLoi;
            cbxBocLot.Tag = ThongTinChungCongDoan.BocLot;
            cbxQuanBang.Tag = ThongTinChungCongDoan.QuanBang;
            cbxBocTP.Tag = ThongTinChungCongDoan.BocVo;
        }

        private async Task ShowDanhSachLuaChon(string keyword, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                cbxTimMaBin.DroppedDown = false;
                return;
            }
            string para = "key";

            string query = $@"
                SELECT 
                    TTThanhPham.MaBin,
                    TTThanhPham.KhoiLuongSau
                FROM 
                    CD_KeoRut
                JOIN 
                    TTThanhPham 
                    ON CD_KeoRut.TTThanhPham_ID = TTThanhPham.id
                WHERE 
                    TTThanhPham.KhoiLuongSau <> 0
                    AND TTThanhPham.MaBin LIKE '%' || @{para} || '%';
            ";


            DataTable sp = await Task.Run(() =>
            {
                return DatabaseHelper.GetData(query, keyword, para);
            }, ct);

            ct.ThrowIfCancellationRequested();

            cbxTimMaBin.DroppedDown = false;

            cbxTimMaBin.SelectionChangeCommitted -= cbxTimMaBin_SelectionChangeCommitted; // tránh trùng event
            if (sp.Rows.Count == 0) return;

            cbxTimMaBin.DataSource = sp;
            cbxTimMaBin.DisplayMember = "MaBin";

            string currentText = keyword;

            cbxTimMaBin.DroppedDown = true;
            cbxTimMaBin.Text = currentText;
            cbxTimMaBin.SelectionStart = cbxTimMaBin.Text.Length;
            cbxTimMaBin.SelectionLength = 0;

            cbxTimMaBin.SelectionChangeCommitted += cbxTimMaBin_SelectionChangeCommitted;
        }

        private void cbxTimMaBin_SelectionChangeCommitted(object sender, EventArgs e)
        {
            if (cbxTimMaBin.SelectedItem == null || !(cbxTimMaBin.SelectedItem is DataRowView)) return;
            DataRowView row = (DataRowView)cbxTimMaBin.SelectedItem;

            tbMaBin.Text = row["MaBin"].ToString();
            klHienTai.Text = row["KhoiLuongSau"].ToString();

            cbxTimMaBin.SelectedIndex = -1;
            cbxTimMaBin.Text = string.Empty;
        }

        private async void cbxTimMaBin_TextUpdate(object sender, EventArgs e)
        {
            ClearForm();
            string tenTP = cbxTimMaBin.Text;
            if (string.IsNullOrEmpty(tenTP)) return;

            // --- thêm debounce + cancel ---
            _searchCts?.Cancel();
            _searchCts = new CancellationTokenSource();
            var token = _searchCts.Token;

            try
            {
                // debounce: đợi user dừng gõ 250ms mới chạy
                await Task.Delay(250, token);

                // gọi async thay vì sync
                await ShowDanhSachLuaChon(tenTP, token);
            }
            catch (OperationCanceledException)
            {
                // bị huỷ vì user gõ tiếp, bỏ qua
            }
        }

        private void klHienTai_ValueChanged(object sender, EventArgs e)
        {
            //TinhTonKho();
        }

        private void klBanTran_ValueChanged(object sender, EventArgs e)
        {
            TinhTonKho();
        }

        private void klConLai_ValueChanged(object sender, EventArgs e)
        {
            //TinhTonKho();
        }

        private void TinhTonKho()
        {
            decimal klHienTaiValue = klHienTai.Value;
            decimal klBanTranValue = klBanTran.Value;
            decimal klConLaiValue = klHienTaiValue - klBanTranValue < 0 ? 0 : klHienTaiValue - klBanTranValue;
            klConLai.Value = klConLaiValue;
        }

        private void ClearForm()
        {
            tbMaBin.Text = string.Empty;
            klHienTai.Value = 0;
            klBanTran.Value = 0;
            klConLai.Value = 0;
        }

        private void btnLuu_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(tbMaBin.Text))
            {
                MessageBox.Show("VUI LÒNG CHỌN MÃ BIN", "THÔNG BÁO", MessageBoxButtons.OK);
                return;
            }

            BanTran bt = new BanTran
            {
                MaBin = tbMaBin.Text,
                KhoiLuongSau = (double)klConLai.Value,
                KhoiLuongBanTran = (double)klBanTran.Value
            };

            string message = DatabaseHelper.UpdateKLConLai_BanTran(bt);
            if (message == string.Empty)
            {
                message = "THAO TÁC THÀNH CÔNG";
                ClearForm();
            }
            MessageBox.Show(message, "THÔNG BÁO", MessageBoxButtons.OK);
        }

        private void cbxAllSelected_CheckedChanged(object sender, EventArgs e)
        {
            bool isChecked = cbxAllSelected.Checked;

            foreach (Control control in tbCheckBox.Controls)
            {
                // Kiểm tra nếu control là CheckBox
                if (control is CheckBox checkbox)
                {
                    // Gán trạng thái Checked
                    checkbox.Checked = isChecked;
                }
            }
        }

        private async void btnBCSX_Click(object sender, EventArgs e)
        {
            DateTime batDau = dtBatDau.Value;
            DateTime ketThuc = dtKetThuc.Value;

            if (ketThuc <= batDau)
            {
                MessageBox.Show("Thời gian kết thúc phải lớn hơn thời gian bắt đầu!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var selectedCongDoans = Helper.Helper.GetCheckedCongDoans(tbCheckBox);
            if (selectedCongDoans == null || selectedCongDoans.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn ít nhất một công đoạn!", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string fileName = selectedCongDoans.Count == 1
                ? $"Report_{selectedCongDoans[0].TenCongDoan}"
                : "Report_MultiCongDoan";


            await WaitingHelper.RunWithWaiting(async () =>
            {
                DataTable dt = await Task.Run(() =>
                    DatabaseHelper.GetDataBaoCaoSX(batDau, ketThuc, selectedCongDoans));

                if (dt == null || dt.Rows.Count == 0)
                {
                    MessageBox.Show("KHÔNG CÓ DỮ LIỆU", "THÔNG BÁO",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (btnXuatExcel.Checked)
                {
                    await Task.Run(() => ExportExcelFile(dt, fileName));
                    MessageBox.Show("Đã xuất Excel thành công!", "Thông báo",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    ShowResult(dt);
                }
            });
        }


        private void ShowResult(DataTable master)
        {
            grvShowBaoCao.AutoGenerateColumns = true;
            grvShowBaoCao.DataSource = master;
            grvShowBaoCao.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells;
            grvShowBaoCao.AllowUserToAddRows = false;
            grvShowBaoCao.ReadOnly = true;
        }

        private void ExportExcelFile(DataTable master, string defaultFileName)
        {
            try
            {

                ExcelExporter.Export(master, defaultFileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Lỗi khi xuất Excel: {ex.Message}",
                    "Export Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        private async void btnTonDong_Click(object sender, EventArgs e)
        {
            grvShowBaoCao.DataSource = null;

            string query = @"
                SELECT 
                    TTThanhPham.MaBin,
                    DanhSachMaSP.Ma,
                    DanhSachMaSP.Ten,
                    DanhSachMaSP.DonVi,
                    TTThanhPham.KhoiLuongTruoc as KLTruoc,
                    TTThanhPham.KhoiLuongSau as KLSau,
                    TTThanhPham.ChieuDaiTruoc as CDTruoc,
                    TTThanhPham.ChieuDaiSau as CDSau,
                    TTThanhPham.Phe,
                    TTThanhPham.KLBanTran,
                    TTThanhPham.GhiChu
                FROM TTThanhPham
                INNER JOIN DanhSachMaSP 
                    ON TTThanhPham.DanhSachSP_ID = DanhSachMaSP.id
                WHERE 
                    (DanhSachMaSP.DonVi = 'KG' AND TTThanhPham.KhoiLuongSau <> 0)
                    OR
                    (DanhSachMaSP.DonVi = 'M' AND TTThanhPham.ChieuDaiSau <> 0)
                ORDER BY TTThanhPham.id DESC
            ";

            string col = null;
            string fileName = "BaoCaoTonKho_" + DateTime.Now.ToString("ddMMMyyyy");

            await WaitingHelper.RunWithWaiting(async () =>
            {
                DataTable dt = await Task.Run(() => DatabaseHelper.GetData(query, col, "KieuSP"));

                if (dt == null || dt.Rows.Count == 0)
                {
                    MessageBox.Show("KHÔNG CÓ DỮ LIỆU", "THÔNG BÁO",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (btnXuatExcel.Checked)
                {
                    await Task.Run(() => ExportExcelFile(dt, fileName));
                    MessageBox.Show("Đã xuất Excel thành công!", "Thông báo",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    ShowResult(dt);
                }
            });
        }


    }
}
