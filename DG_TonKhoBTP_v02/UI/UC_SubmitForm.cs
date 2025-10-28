﻿using DG_TonKhoBTP_v02.Core;
using DG_TonKhoBTP_v02.Database;
using DG_TonKhoBTP_v02.Helper;
using DG_TonKhoBTP_v02.Models;
using DG_TonKhoBTP_v02.Printer;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Validator = DG_TonKhoBTP_v02.Helper.Validator;

namespace DG_TonKhoBTP_v02.UI
{
    public partial class UC_SubmitForm : UserControl
    {
        public UC_SubmitForm()
        {
            InitializeComponent();
        }

        private void btnLuu_Click(object sender, EventArgs e)
        {
            try
            {
                btnLuu.Enabled = false;

                #region Lấy thông tin từ các section
                var host = this.FindForm();
                if (host == null) return;

                FormSnapshot snap = null;
                try
                {
                    snap = DG_TonKhoBTP_v02.Core.FormSnapshotBuilder.Capture(host);
                }
                catch (Exception) {}

                if (snap == null || snap.Sections.Count < 4 )
                {
                    return;
                }

                // lấy UC_TTSanPham ở đâu đó trong form
                var ucSanPham = Helper.Helper.FindControlRecursive<UC_TTSanPham>(host);
                if (ucSanPham != null)
                {
                    // TỰ-ĐỘNG gom tất cả providers hiện có
                    var extra = ucSanPham.GetAggregateSections();
                    foreach (var kv in extra)
                        snap.Sections[kv.Key] = kv.Value; // vd "CaiDatCDBoc", "CD_BenRuot", "CD_BocLot"...
                }
                #endregion

                //Tuỳ chọn: Lưu tạm
                //DG_TonKhoBTP_v02.Core.StateStore.CurrentSnapshot = snap;

                #region Kiểm tra tính hợp lệ của thongTinCaLamViec
                ThongTinCaLamViec thongTinCaLamViec = (ThongTinCaLamViec) snap.Sections["UC_TTCaLamViec"];

                if (!Validator.TTCaLamViec(thongTinCaLamViec))
                {
                    MessageBox.Show("Thông tin ở ca làm việc đang thiếu dữ liệu", "THÔNG BÁO", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                #endregion

                #region Kiểm tra tính hợp lệ dữ liệu của uc NVL
                var list_TTNVL = snap.Sections["UC_TTNVL"] as List<TTNVL>;

                if (!Validator.TTNVL(list_TTNVL))
                {
                    MessageBox.Show("Thông tin NGUYÊN LIỆU chưa hợp lệ", "THÔNG BÁO", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                #endregion

                #region Kiểm tra tính hợp lệ của dữ liệu thành phẩm công đoạn
                TTThanhPham thongTinThanhPham = (TTThanhPham) snap.Sections["UC_TTThanhPham"];

                if (!Validator.TTThanhPham(thongTinThanhPham))
                {
                    MessageBox.Show("Thiếu THÔNG TIN TP của CÔNG ĐOẠN", "THÔNG BÁO", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                #endregion

                #region Kiểm tra tính hợp lệ dữ liệu của chi tiết các công đoạn
                List<object> chiTietCD = Validator.KiemTraChiTietCongDoan(snap);

                if (chiTietCD[0] == null)
                {
                    MessageBox.Show("Chi tiết công đoạn chưa hợp lệ", "THÔNG BÁO", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                #endregion

                #region Lưu và thông báo trạng thái lưu
                EditModel editmodel = (EditModel)snap.Sections["UC_Edit"];
                int idEdit = editmodel.Id;
                string err = string.Empty;
                bool isSuccess = false;

                // Tạo dữ liệu in sẵn (hoặc tạo sau khi lưu tuỳ ý)
                PrinterModel BuildPrinter()
                {
                    return new PrinterModel
                    {
                        NgaySX = DateTime.ParseExact(thongTinCaLamViec.Ngay, "yyyy-MM-dd", null).ToString("dd/MM/yyyy"),
                        CaSX = thongTinCaLamViec.Ca,
                        KhoiLuong = thongTinThanhPham.KhoiLuongSau.ToString(),
                        ChieuDai = thongTinThanhPham.ChieuDaiSau.ToString(),
                        TenSP = thongTinThanhPham.TenTP,
                        MaBin = thongTinThanhPham.MaBin,
                        MaSP = thongTinThanhPham.MaTP,
                        DanhGia = "",
                        TenCN = Helper.Helper.ConvertTiengVietKhongDau(thongTinCaLamViec.NguoiLam),
                        GhiChu = Helper.Helper.ConvertTiengVietKhongDau(thongTinThanhPham.GhiChu)
                    };
                }

                var waiting = new FrmWaiting("Đang lưu dữ liệu và in tem...");
                try
                {
                    // Thiết lập để form luôn nổi trên
                    waiting.TopMost = true;
                    waiting.StartPosition = FormStartPosition.CenterScreen;
                    waiting.Show();
                    waiting.Refresh(); // Force refresh để vẽ ngay
                    Application.DoEvents();

                    Task.Run(() =>
                    {
                        // 1) Lưu dữ liệu
                        if (idEdit > 0)
                            isSuccess = DatabaseHelper.UpdateDataSanPham(idEdit, thongTinCaLamViec, thongTinThanhPham, list_TTNVL, chiTietCD, out err);
                        else
                            isSuccess = DatabaseHelper.SaveDataSanPham(thongTinCaLamViec, thongTinThanhPham, list_TTNVL, chiTietCD, out err);

                        // 2) In tem nếu lưu ok
                        if (isSuccess)
                        {
                            var printer = BuildPrinter();
                            PrintHelper.PrintLabel(printer);
                        }

                        // 3) Quay lại UI: đóng waiting + báo kết quả + clear form
                        this.BeginInvoke(new Action(() =>
                        {
                            try
                            {
                                waiting.Close();
                                waiting.Dispose();
                            }
                            catch { }

                            string title = isSuccess ? "THÔNG BÁO" : "Lỗi";
                            MessageBoxIcon icon = isSuccess ? MessageBoxIcon.Information : MessageBoxIcon.Error;
                            string message = isSuccess ? (idEdit > 0 ? "SỬA" : "LƯU") + " THÀNH CÔNG" : err;
                            MessageBox.Show(message, title, MessageBoxButtons.OK, icon);

                            if (isSuccess) ControlCleaner.ClearAll(host);
                        }));
                    });
                }
                catch (Exception ex)
                {
                    waiting?.Close();
                    waiting?.Dispose();
                    MessageBox.Show("Lỗi: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                #endregion
            }
            finally
            {
                btnLuu.Enabled = true;
            }
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            var host = this.FindForm();
            if (host == null) return;

            ControlCleaner.ClearAll(host);

            // Ngoài ra gọi ClearInputs() của từng section, nếu cần tinh chỉnh riêng
            foreach (Control c in host.Controls) ClearSectionRecursive(c);
        }

        private void ClearSectionRecursive(Control root)
        {
            if (root is IFormSection fs) fs.ClearInputs();
            foreach (Control child in root.Controls)
                ClearSectionRecursive(child);
        }
    }
}
