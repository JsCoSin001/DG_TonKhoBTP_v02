using DG_TonKhoBTP_v02.Core;
using DG_TonKhoBTP_v02.Database;
using DG_TonKhoBTP_v02.Helper;
using DG_TonKhoBTP_v02.Models;
using DG_TonKhoBTP_v02.Printer;
using DG_TonKhoBTP_v02.UI.Helper;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using CoreHelper = DG_TonKhoBTP_v02.Helper.Helper;
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
                cbInTem.Text = "InTem";
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
            var swTotal = Stopwatch.StartNew();
            Debug.WriteLine("=== [BTN LƯU] BẮT ĐẦU ===");

            FrmWaiting waiting = null;

            void CloseWaitingSafe()
            {
                try
                {
                    if (waiting != null && !waiting.IsDisposed)
                        waiting.CloseAndDispose();
                }
                catch { /* ignore */ }
            }

            void ExitEarly()
            {
                CloseWaitingSafe();
                btnLuu.Enabled = true;
            }

            try
            {
                // ✅ HIỂN THỊ WAITING NGAY TỪ ĐẦU
                waiting = new FrmWaiting("ĐANG XỬ LÝ...");
                waiting.ShowAndRefresh();
                Debug.WriteLine($"Hiển thị waiting: {swTotal.ElapsedMilliseconds} ms");

                btnLuu.Enabled = false;
                Debug.WriteLine("btnLuu.Enabled = false");

                // === VALIDATION & CAPTURE DATA ===
                string tb = CoreHelper.TaoThongBao(lblTrangThai);
                Debug.WriteLine($"TaoThongBao: {swTotal.ElapsedMilliseconds} ms");

                if (!string.IsNullOrEmpty(tb))
                {
                    if (!string.IsNullOrEmpty(tb))
                    {
                        _timerThongBao.Stop();
                        _timerThongBao.Start();
                    }

                    Debug.WriteLine($"Thoát sớm vì tb != \"\": {swTotal.ElapsedMilliseconds} ms");
                    ExitEarly();
                    return;
                }

                var swStep = Stopwatch.StartNew();
                var host = this.FindForm();
                Debug.WriteLine($"FindForm: {swStep.ElapsedMilliseconds} ms (tổng: {swTotal.ElapsedMilliseconds} ms)");

                if (host == null)
                {
                    Debug.WriteLine($"host == null, thoát: {swTotal.ElapsedMilliseconds} ms");
                    ExitEarly();
                    return;
                }

                swStep.Restart();
                FormSnapshot snap = null;
                try
                {
                    snap = DG_TonKhoBTP_v02.Core.FormSnapshotBuilder.Capture(host);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Lỗi Capture snapshot: {ex.Message}");
                    ExitEarly();
                    return;
                }

                // === CHUẨN BỊ DỮ LIỆU ===
                var editmodel = (EditModel)snap.Sections["UC_Edit"];
                int kieuXL = editmodel.KieuXuLy;
                int idEdit = kieuXL == 2 ? editmodel.Id : 0;

                // ✅ UI: Ẩn waiting trước khi hỏi MessageBox
                if (idEdit != 0)
                {
                    try
                    {
                        waiting.Hide();
                        Application.DoEvents();
                    }
                    catch { }

                    DialogResult result = MessageBox.Show(
                        "BẠN ĐANG SỬA DỮ LIỆU?",
                        "Xác nhận",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question
                    );

                    if (result == DialogResult.No)
                    {
                        Debug.WriteLine("User chọn NO (không sửa) -> thoát");
                        ExitEarly();
                        return;
                    }

                    // user chọn Yes -> hiện waiting lại
                    try
                    {
                        waiting.Show();
                        waiting.ShowAndRefresh();
                    }
                    catch { }
                }

                Debug.WriteLine($"Capture snapshot: {swStep.ElapsedMilliseconds} ms (tổng: {swTotal.ElapsedMilliseconds} ms)");

                if (snap == null || snap.Sections.Count < 4)
                {
                    Debug.WriteLine($"Snapshot null/invalid: {swTotal.ElapsedMilliseconds} ms");
                    ExitEarly();
                    return;
                }

                // Lấy UC_TTSanPham & gộp Sections
                swStep.Restart();
                var ucSanPham = CoreHelper.FindControlRecursive<UC_TTSanPham>(host);
                if (ucSanPham != null)
                {
                    var extra = ucSanPham.GetAggregateSections();
                    foreach (var kv in extra)
                        snap.Sections[kv.Key] = kv.Value;
                }
                Debug.WriteLine($"Merge UC_TTSanPham: {swStep.ElapsedMilliseconds} ms (tổng: {swTotal.ElapsedMilliseconds} ms)");

                #region Validate dữ liệu

                #region Validate Ca Làm Việc
                swStep.Restart();
                ThongTinCaLamViec thongTinCaLamViec = (ThongTinCaLamViec)snap.Sections["UC_TTCaLamViec"];
                int sttLoi = Validator.TTCaLamViec(thongTinCaLamViec);
                Debug.WriteLine($"Validator.TTCaLamViec: {swStep.ElapsedMilliseconds} ms (tổng: {swTotal.ElapsedMilliseconds} ms)");

                if (sttLoi > 0)
                {
                    CloseWaitingSafe();
                    FrmWaiting.ShowGifAlert(EnumStore.ErrorCaLamViec[sttLoi]);
                    btnLuu.Enabled = true;
                    Debug.WriteLine($"Lỗi TTCaLamViec (sttLoi={sttLoi}), thoát: {swTotal.ElapsedMilliseconds} ms");
                    return;
                }
                #endregion

                #region Validate NVL
                swStep.Restart();
                List<TTNVL> list_TTNVL = snap.Sections["UC_TTNVL"] as List<TTNVL>;
                string loiNVL = Validator.TTNVL(list_TTNVL, thongTinCaLamViec.May);
                Debug.WriteLine($"Validator.TTNVL: {swStep.ElapsedMilliseconds} ms (tổng: {swTotal.ElapsedMilliseconds} ms)");

                if (!string.IsNullOrEmpty(loiNVL))
                {
                    CloseWaitingSafe();
                    FrmWaiting.ShowGifAlert(loiNVL);
                    btnLuu.Enabled = true;
                    Debug.WriteLine($"Lỗi TTNVL, thoát: {swTotal.ElapsedMilliseconds} ms");
                    return;
                }
                #endregion

                #region Validate TP công đoạn
                swStep.Restart();
                TTThanhPham thongTinThanhPham = (TTThanhPham)snap.Sections["UC_TTThanhPham"];
                sttLoi = Validator.TTThanhPham(thongTinThanhPham);
                Debug.WriteLine($"Validator.TTThanhPham: {swStep.ElapsedMilliseconds} ms (tổng: {swTotal.ElapsedMilliseconds} ms)");

                if (sttLoi > 0)
                {
                    CloseWaitingSafe();
                    FrmWaiting.ShowGifAlert(EnumStore.ErrorTP[sttLoi]);
                    btnLuu.Enabled = true;
                    Debug.WriteLine($"Lỗi TTThanhPham (sttLoi={sttLoi}), thoát: {swTotal.ElapsedMilliseconds} ms");
                    return;
                }

                // Cập nhật hàn nối
                int cd = thongTinThanhPham.CongDoan.Id;
                bool isHanNoi = true;

                foreach (TTNVL nvl in list_TTNVL)
                {
                    if (cd != nvl.CongDoan)
                    {
                        isHanNoi = false;
                        break;
                    }
                }

                if (isHanNoi && thongTinThanhPham.CongDoan.Id != 0)
                {
                    try
                    {
                        // ✅ UI: Tắt/ẩn waiting trước khi mở dialog
                        waiting.Hide();
                        Application.DoEvents();

                        using (var f = new GetUserInputValue_Simple())
                        {
                            f.StartPosition = FormStartPosition.CenterParent;
                            var result = f.ShowDialog(this);

                            if (result == DialogResult.OK)
                                thongTinThanhPham.HanNoi = (double)f.TongDongThuaValue;

                            if (result == DialogResult.Cancel)
                            {
                                // ✅ UI: Cancel thì phải dọn waiting + enable nút rồi thoát
                                Debug.WriteLine("User Cancel hàn nối -> thoát");
                                ExitEarly();
                                return;
                            }
                        }

                        // ✅ UI: Sau khi dialog đóng -> hiện waiting lại
                        waiting.Show();
                        waiting.ShowAndRefresh();

                        foreach (TTNVL nvl in list_TTNVL)
                        {
                            nvl.KlConLai = 0;
                            nvl.CdConLai = 0;
                        }
                    }
                    catch (Exception exHN)
                    {
                        Debug.WriteLine($"Lỗi hàn nối: {exHN.Message}");
                        // giữ logic "catch {}" như bạn, nhưng vẫn đảm bảo waiting đang hiển thị
                        try { waiting.Show(); waiting.ShowAndRefresh(); } catch { }
                    }
                }
                #endregion

                #region Validate chi tiết công đoạn
                swStep.Restart();
                var chiTietCD = Validator.KiemTraChiTietCongDoan(snap);
                Debug.WriteLine($"Validator.KiemTraChiTietCongDoan: {swStep.ElapsedMilliseconds} ms (tổng: {swTotal.ElapsedMilliseconds} ms)");

                if (chiTietCD[0] == null)
                {
                    CloseWaitingSafe();
                    FrmWaiting.ShowGifAlert("Chi tiết công đoạn chưa hợp lệ");
                    btnLuu.Enabled = true;
                    Debug.WriteLine($"Chi tiết công đoạn chưa hợp lệ, thoát: {swTotal.ElapsedMilliseconds} ms");
                    return;
                }
                

                #endregion

                #endregion

                // ✅ CẬP NHẬT MESSAGE SAU KHI VALIDATE XONG
                bool shouldPrint = (_printer != "" && cbInTem.Checked);
                waiting.MessageText = shouldPrint
                    ? "ĐANG LƯU DỮ LIỆU VÀ IN TEM..."
                    : "ĐANG LƯU DỮ LIỆU...";


                // ========== TEM CHO SP MỚI ==========
                PrinterModel BuildPrinter()
                {

                    string ghiChu = CoreHelper.ConvertTiengVietKhongDau(thongTinThanhPham.GhiChu);

                    if (EnumStore.MayTheoCongDoan.TryGetValue("Ben_CU_AL", out var dsMay) &&
                       dsMay.Contains(thongTinCaLamViec.May, StringComparer.OrdinalIgnoreCase) && 
                       chiTietCD[0] is CD_BenRuot obj)
                    {
                        ghiChu = $"{obj.DKSoi}x{obj.SoSoi?.ToString() ?? ""} sợi\n" + ghiChu;
                    }


                    return new PrinterModel
                    {
                        NgaySX = DateTime.ParseExact(thongTinCaLamViec.Ngay, "yyyy-MM-dd", CultureInfo.InvariantCulture)
                                         .ToString("dd/MM/yyyy"),
                        CaSX = thongTinCaLamViec.Ca,
                        KhoiLuong = thongTinThanhPham.KhoiLuongSau.ToString(),
                        ChieuDai = thongTinThanhPham.ChieuDaiSau.ToString(),
                        TenSP = thongTinThanhPham.TenTP,
                        MaBin = thongTinThanhPham.MaBin,
                        MaSP = thongTinThanhPham.MaTP,
                        DanhGia = "",
                        TenCN = CoreHelper.ConvertTiengVietKhongDau(thongTinCaLamViec.NguoiLam),
                        GhiChu = ghiChu
                    };
                }

                // === CHẠY TASK LƯU + IN ===
                Task.Run(() =>
                {
                    var swTask = Stopwatch.StartNew();
                    Debug.WriteLine("=== [BTN LƯU] Task.Run BẮT ĐẦU ===");

                    bool saveSuccessLocal = false;
                    bool hasPrintErrorLocal = false;
                    string saveErrorLocal = null;
                    string printErrorLocal = null;

                    try
                    {
                        // 1) Lưu dữ liệu
                        var swDb = Stopwatch.StartNew();
                        string err = string.Empty;

                        if (idEdit == 0)
                        {
                            saveSuccessLocal = DatabaseHelper.SaveDataSanPham(
                                thongTinCaLamViec, thongTinThanhPham, list_TTNVL, chiTietCD, out err);
                            Debug.WriteLine($"SaveDataSanPham: {swDb.ElapsedMilliseconds} ms");
                        }
                        else
                        {
                            saveSuccessLocal = DatabaseHelper.UpdateDataSanPham(
                                idEdit, thongTinCaLamViec, thongTinThanhPham, list_TTNVL, chiTietCD, out err);
                            Debug.WriteLine($"UpdateDataSanPham: {swDb.ElapsedMilliseconds} ms");
                        }

                        if (!saveSuccessLocal)
                        {
                            saveErrorLocal = string.IsNullOrEmpty(err) ? "LƯU KHÔNG THÀNH CÔNG." : err;
                            Debug.WriteLine($"Lưu thất bại sau: {swTask.ElapsedMilliseconds} ms, lỗi: {saveErrorLocal}");
                            return;
                        }

                        // 2) In tem nếu lưu ok và có chọn in
                        if (shouldPrint)
                        {
                            try
                            {
                                var swPrintTP = Stopwatch.StartNew();

                                // In tem thành phẩm
                                var printer = BuildPrinter();
                                PrintHelper.PrintLabel(printer);
                                Debug.WriteLine($"In tem thành phẩm: {swPrintTP.ElapsedMilliseconds} ms");

                                // In tem NVL
                                if (thongTinCaLamViec.Id == 0) return; // Không in tem NVL ở công đoạn rút

                                List<string> dsBin = new List<string>();
                                foreach (TTNVL nvl in list_TTNVL)
                                {
                                    // Không in tem nếu NVL <= 0
                                    if ((nvl.DonVi == "KG" && nvl.KlConLai == 0) ||
                                        (nvl.DonVi == "M" && nvl.CdConLai == 0))
                                        continue;

                                    dsBin.Add(nvl.BinNVL);
                                }

                                if (dsBin.Count == 0) return;

                                var swGetPrinterData = Stopwatch.StartNew();
                                List<PrinterModel> nvl_printer = DatabaseHelper.GetPrinterDataByListBin(dsBin);
                                Debug.WriteLine($"GetPrinterDataByListBin: {swGetPrinterData.ElapsedMilliseconds} ms");

                                if (nvl_printer != null && nvl_printer.Count > 0)
                                {
                                    var swPrintNVL = Stopwatch.StartNew();
                                    foreach (PrinterModel item in nvl_printer)
                                        PrintHelper.PrintLabel(item);

                                    Debug.WriteLine($"In tem NVL: {swPrintNVL.ElapsedMilliseconds} ms");
                                }
                            }
                            catch (Exception exPrint)
                            {
                                hasPrintErrorLocal = true;
                                printErrorLocal = exPrint.Message;
                                Debug.WriteLine($"Lỗi in tem: {exPrint.Message}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Exception trong Task.Run: {ex}");

                        if (!saveSuccessLocal)
                        {
                            saveSuccessLocal = false;
                            saveErrorLocal = "LỖI LƯU DỮ LIỆU: " + ex.Message;
                        }
                        else
                        {
                            hasPrintErrorLocal = true;
                            printErrorLocal = "\n" + ex.Message;
                        }
                    }
                    finally
                    {
                        Debug.WriteLine($"Task.Run kết thúc sau: {swTask.ElapsedMilliseconds} ms; Tổng: {swTotal.ElapsedMilliseconds} ms");

                        // 3) Quay lại UI thread
                        this.BeginInvoke(new Action(() =>
                        {
                            var swUi = Stopwatch.StartNew();

                            CloseWaitingSafe();
                            Debug.WriteLine("Đã đóng FrmWaiting");

                            // 🔔 Thông báo kết quả lưu
                            string msgSave = (idEdit > 0 ? "SỬA" : "LƯU");
                            string iconAlert = EnumStore.Icon.Warning;

                            if (saveSuccessLocal)
                            {
                                msgSave += " THÀNH CÔNG ";
                                iconAlert = EnumStore.Icon.Success;
                            }
                            else
                            {
                                msgSave += " KHÔNG THÀNH CÔNG\nLỗi: " + saveErrorLocal;
                            }

                            FrmWaiting.ShowGifAlert(msgSave, "THÔNG BÁO", iconAlert);

                            // 🔔 Thông báo lỗi in (nếu có)
                            if (saveSuccessLocal && hasPrintErrorLocal && !string.IsNullOrEmpty(printErrorLocal))
                                FrmWaiting.ShowGifAlert(printErrorLocal.ToUpper(), "LỖI IN");

                            // Clear form nếu lưu thành công
                            if (saveSuccessLocal)
                            {
                                var swClear = Stopwatch.StartNew();
                                ControlCleaner.ClearAll(host);
                                Debug.WriteLine($"ClearAll host: {swClear.ElapsedMilliseconds} ms");
                            }

                            // ✅ UI: enable nút đúng thời điểm (sau khi xong hết)
                            btnLuu.Enabled = true;

                            Debug.WriteLine($"UI cập nhật xong - thời gian: {swUi.ElapsedMilliseconds} ms; Tổng: {swTotal.ElapsedMilliseconds} ms");
                        }));
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception ngoài Task.Run: {ex}");

                CloseWaitingSafe();
                FrmWaiting.ShowGifAlert("LỖI: " + ex.Message, "LỖI");

                btnLuu.Enabled = true;
                Debug.WriteLine($"Kết thúc trong catch, tổng thời gian: {swTotal.ElapsedMilliseconds} ms");
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
