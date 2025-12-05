using DG_TonKhoBTP_v02.Core;
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
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using PrinterModel = DG_TonKhoBTP_v02.Models.PrinterModel;
using Validator = DG_TonKhoBTP_v02.Helper.Validator;

namespace DG_TonKhoBTP_v02.UI
{
    public partial class UC_SubmitForm : UserControl, IFormSection
    {
        public string SectionName => nameof(UC_SubmitForm);
        private readonly Timer _timerThongBao = new Timer();

        private static readonly string _printer = Properties.Settings.Default.PrinterName;
        
        public UC_SubmitForm()
        {
            InitializeComponent();

            bool inTem = _printer != "";

            cbInTem.Checked = inTem;

            if (_printer != "")
            {
                cbInTem.Text = _printer;
            }
            else
            {
                cbInTem.Checked = true;
                cbInTem.Enabled = false;
                cbInTem.Text = "Không in tem";
            }
        }

        private void btnLuu_Click(object sender, EventArgs e)
        {
            string tb = Helper.Helper.TaoThongBao(lblTrangThai);
            if (tb != "")
            {
                if (!string.IsNullOrEmpty(tb))
                {
                    _timerThongBao.Stop();  // đảm bảo reset
                    _timerThongBao.Start(); // đếm 5 giây để tự ẩn label
                }
                return;
            }

            btnLuu.Enabled = false;

            // --- Lấy form + snapshot ---
            var host = this.FindForm();
            if (host == null) { btnLuu.Enabled = true; return; }

            FormSnapshot snap = null;
            try { snap = DG_TonKhoBTP_v02.Core.FormSnapshotBuilder.Capture(host); }
            catch { }

            if (snap == null || snap.Sections.Count < 4)
            {
                btnLuu.Enabled = true;
                return;
            }

            var ucSanPham = Helper.Helper.FindControlRecursive<UC_TTSanPham>(host);
            if (ucSanPham != null)
            {
                var extra = ucSanPham.GetAggregateSections();
                foreach (var kv in extra)
                    snap.Sections[kv.Key] = kv.Value;
            }

            // --- Validate Ca Làm Việc ---
            var thongTinCaLamViec = (ThongTinCaLamViec)snap.Sections["UC_TTCaLamViec"];
            if (!Validator.TTCaLamViec(thongTinCaLamViec))
            {
                MessageBox.Show("Thông tin ở ca làm việc đang thiếu dữ liệu".ToUpper(), "THÔNG BÁO",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                btnLuu.Enabled = true;
                return;
            }

            // --- Validate NVL ---
            var list_TTNVL = snap.Sections["UC_TTNVL"] as List<TTNVL>;
            if (!Validator.TTNVL(list_TTNVL))
            {
                MessageBox.Show("Thông tin NGUYÊN LIỆU chưa hợp lệ".ToUpper(), "THÔNG BÁO",
                                      MessageBoxButtons.OK, MessageBoxIcon.Error);
                btnLuu.Enabled = true;
                return;
            }

            // --- Validate TP công đoạn ---
            var thongTinThanhPham = (TTThanhPham)snap.Sections["UC_TTThanhPham"];
            if (!Validator.TTThanhPham(thongTinThanhPham))
            {
                MessageBox.Show("Thiếu THÔNG TIN TP của CÔNG ĐOẠN", "THÔNG BÁO",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                btnLuu.Enabled = true;
                return;
            }

            // --- Validate chi tiết công đoạn ---
            var chiTietCD = Validator.KiemTraChiTietCongDoan(snap);
            if (chiTietCD[0] == null)
            {
                MessageBox.Show("Chi tiết công đoạn chưa hợp lệ".ToUpper(), "THÔNG BÁO",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                btnLuu.Enabled = true;
                return;
            }

            // --- Dữ liệu in ---
            PrinterModel BuildPrinter()
            {
                // Lưu ý: ParseExact có thể ném exception; ta đang gọi trong background try/catch
                return new PrinterModel
                {
                    NgaySX = DateTime.ParseExact(thongTinCaLamViec.Ngay, "yyyy-MM-dd", CultureInfo.InvariantCulture).ToString("dd/MM/yyyy"),
                    //NgaySX = thongTinCaLamViec.Ngay,
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

            // --- Lưu + in ---
            var editmodel = (EditModel)snap.Sections["UC_Edit"];
            int idEdit = editmodel.Id;
            string err = string.Empty;
            bool isSuccess = false;
                        
            bool shouldPrint = false;

            if (_printer != "" && cbInTem.Checked)
            {
                shouldPrint = true;
            }

            string message = shouldPrint ? "Đang lưu dữ liệu và in tem..." : "Đang lưu dữ liệu...";



            bool saveSuccess = false;      // kết quả lưu
            bool hasPrintError = false;    // có lỗi in không
            string saveError = null;       // nội dung lỗi lưu (nếu có)
            string printError = null;      // nội dung lỗi in (nếu có)

            var waiting = new FrmWaiting(message.ToUpper());

            try
            {
                waiting.TopMost = true;
                waiting.StartPosition = FormStartPosition.CenterScreen;
                waiting.Show();
                waiting.Refresh();
                Application.DoEvents();

                Task.Run(() =>
                {
                    try
                    {
                        // 1) Lưu dữ liệu
                        if (idEdit == 0)
                            saveSuccess = DatabaseHelper.SaveDataSanPham(
                                thongTinCaLamViec, thongTinThanhPham, list_TTNVL, chiTietCD, out err);
                        else
                            saveSuccess = DatabaseHelper.UpdateDataSanPham(
                                idEdit, thongTinCaLamViec, thongTinThanhPham, list_TTNVL, chiTietCD, out err);

                        if (!saveSuccess)
                        {
                            // Lưu không thành công: err thường đã có message từ DAL
                            saveError = string.IsNullOrEmpty(err) ? "LƯU KHÔNG THÀNH CÔNG." : err;
                            return; // ❌ không in khi lưu lỗi
                        }

                        // 2) In tem nếu lưu ok và có chọn in
                        if (shouldPrint)
                        {
                            try
                            {
                                // In tem thành phẩm
                                var printer = BuildPrinter();
                                PrintHelper.PrintLabel(printer); // lỗi in sẽ ném Exception

                                List<string> dsBin = new List<string>();

                                // In tem nguyên liệu (nếu có)
                                foreach (TTNVL nvl in list_TTNVL)
                                {
                                    if ((nvl.DonVi == "KG" && nvl.KlConLai == 0) ||
                                        (nvl.DonVi == "M" && nvl.CdConLai == 0) ||
                                        nvl.CdBatDau == -1 || nvl.KlBatDau == -1)
                                        continue;

                                    dsBin.Add(nvl.BinNVL);
                                }

                                List<PrinterModel> nvl_printer = DatabaseHelper.GetPrinterDataByListBin(dsBin);

                                if (nvl_printer != null && nvl_printer.Count > 0)
                                {
                                    foreach (PrinterModel item in nvl_printer)
                                    {
                                        PrintHelper.PrintLabel(item); // lỗi in tiếp tục ném Exception
                                    }
                                }
                            }
                            catch (Exception exPrint)
                            {
                                // LƯU VẪN OK, chỉ có lỗi IN
                                hasPrintError = true;
                                printError = exPrint.Message;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Lỗi “bất ngờ” trong logic lưu/in
                        if (!saveSuccess)
                        {
                            // Chưa lưu hoặc lưu thất bại
                            saveSuccess = false;
                            saveError = "LỖI LƯU DỮ LIỆU: " + ex.Message;
                        }
                        else
                        {
                            // Đã lưu thành công, lỗi xảy ra trong lúc in
                            hasPrintError = true;
                            printError = "\n" + ex.Message;
                        }
                    }
                    finally
                    {
                        // 3) Quay lại UI (luôn luôn chạy trên UI thread)
                        this.BeginInvoke(new Action(() =>
                        {
                            try { waiting.Close(); waiting.Dispose(); } catch { }

                            // 🔔 Thông báo 1: KẾT QUẢ LƯU
                            if (saveSuccess)
                            {
                                string msgSave = (idEdit > 0 ? "SỬA" : "LƯU") + " THÀNH CÔNG";
                                MessageBox.Show(this, msgSave, "THÔNG BÁO",
                                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                            else
                            {
                                string msgSave = string.IsNullOrEmpty(saveError)
                                    ? "LƯU KHÔNG THÀNH CÔNG."
                                    : saveError;

                                MessageBox.Show(this, msgSave, "LỖI",
                                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }

                            // 🔔 Thông báo 2: LỖI IN (nếu có và chỉ khi lưu thành công)
                            if (saveSuccess && hasPrintError && !string.IsNullOrEmpty(printError))
                            {
                                MessageBox.Show(this, printError.ToUpper(), "LỖI IN",
                                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }

                            // Nếu lưu OK thì clear form
                            if (saveSuccess)
                                ControlCleaner.ClearAll(host);

                            btnLuu.Enabled = true; // bật lại nút
                        }));
                    }
                });
            }
            catch (Exception ex)
            {
                // Lỗi xảy ra trước khi Task bắt đầu (UI thread)
                try { waiting?.Close(); waiting?.Dispose(); } catch { }

                MessageBox.Show(this,
                    "Lỗi: " + ex.Message,
                    "Lỗi",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

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

        public object GetData()
        {
            return new Submit
            {
                IsChecked = cbInTem.Checked
            };
        }

        public void ClearInputs()
        {
            cbInTem.Checked = true;
        }

        private void UC_SubmitForm_Load(object sender, EventArgs e)
        {
            _timerThongBao.Interval = 5000;
            _timerThongBao.Tick += (s, args) =>
            {
                lblTrangThai.Visible = false;
                _timerThongBao.Stop();
            };
        }
    }
}
