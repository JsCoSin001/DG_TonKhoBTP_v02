using DG_TonKhoBTP_v02.Core;
using DG_TonKhoBTP_v02.Database;
using DG_TonKhoBTP_v02.Helper;
using DG_TonKhoBTP_v02.Models;
using DG_TonKhoBTP_v02.Printer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
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
            var swTotal = Stopwatch.StartNew();
            Debug.WriteLine("=== [BTN LƯU] BẮT ĐẦU ===");

            // ✅ HIỂN THỊ WAITING NGAY TỪ ĐẦU
            var waiting = new FrmWaiting("ĐANG XỬ LÝ...");
            waiting.ShowAndRefresh();
            Debug.WriteLine($"Hiển thị waiting: {swTotal.ElapsedMilliseconds} ms");

            try
            {
                btnLuu.Enabled = false;
                Debug.WriteLine("btnLuu.Enabled = false");

                // === VALIDATION & CAPTURE DATA ===
                string tb = Helper.Helper.TaoThongBao(lblTrangThai);
                Debug.WriteLine($"TaoThongBao: {swTotal.ElapsedMilliseconds} ms");

                if (tb != "")
                {
                    waiting.CloseAndDispose();

                    if (!string.IsNullOrEmpty(tb))
                    {
                        _timerThongBao.Stop();
                        _timerThongBao.Start();
                    }
                    btnLuu.Enabled = true;
                    Debug.WriteLine($"Thoát sớm vì tb != \"\": {swTotal.ElapsedMilliseconds} ms");
                    return;
                }

                var swStep = Stopwatch.StartNew();
                var host = this.FindForm();
                Debug.WriteLine($"FindForm: {swStep.ElapsedMilliseconds} ms (tổng: {swTotal.ElapsedMilliseconds} ms)");

                if (host == null)
                {
                    waiting.Close();
                    waiting.Dispose();
                    btnLuu.Enabled = true;
                    Debug.WriteLine($"host == null, thoát: {swTotal.ElapsedMilliseconds} ms");
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
                    waiting.Close();
                    waiting.Dispose();
                    btnLuu.Enabled = true;
                    return;
                }
                Debug.WriteLine($"Capture snapshot: {swStep.ElapsedMilliseconds} ms (tổng: {swTotal.ElapsedMilliseconds} ms)");

                if (snap == null || snap.Sections.Count < 4)
                {
                    waiting.Close();
                    waiting.Dispose();
                    btnLuu.Enabled = true;
                    Debug.WriteLine($"Snapshot null/invalid: {swTotal.ElapsedMilliseconds} ms");
                    return;
                }

                // Lấy UC_TTSanPham & gộp Sections
                swStep.Restart();
                var ucSanPham = Helper.Helper.FindControlRecursive<UC_TTSanPham>(host);
                if (ucSanPham != null)
                {
                    var extra = ucSanPham.GetAggregateSections();
                    foreach (var kv in extra)
                        snap.Sections[kv.Key] = kv.Value;
                }
                Debug.WriteLine($"Merge UC_TTSanPham: {swStep.ElapsedMilliseconds} ms (tổng: {swTotal.ElapsedMilliseconds} ms)");

                // Validate Ca Làm Việc
                swStep.Restart();
                var thongTinCaLamViec = (ThongTinCaLamViec)snap.Sections["UC_TTCaLamViec"];
                int sttLoi = Validator.TTCaLamViec(thongTinCaLamViec);
                Debug.WriteLine($"Validator.TTCaLamViec: {swStep.ElapsedMilliseconds} ms (tổng: {swTotal.ElapsedMilliseconds} ms)");

                if (sttLoi > 0)
                {
                    waiting.Close();
                    waiting.Dispose();
                    FrmWaiting.ShowGifAlert(ErrorStore.ErrorCaLamViec[sttLoi]);
                    btnLuu.Enabled = true;
                    Debug.WriteLine($"Lỗi TTCaLamViec (sttLoi={sttLoi}), thoát: {swTotal.ElapsedMilliseconds} ms");
                    return;
                }

                // Validate NVL
                swStep.Restart();
                var list_TTNVL = snap.Sections["UC_TTNVL"] as List<TTNVL>;
                sttLoi = Validator.TTNVL(list_TTNVL);
                Debug.WriteLine($"Validator.TTNVL: {swStep.ElapsedMilliseconds} ms (tổng: {swTotal.ElapsedMilliseconds} ms)");

                if (sttLoi > 0)
                {
                    waiting.Close();
                    waiting.Dispose();
                    FrmWaiting.ShowGifAlert(ErrorStore.ErrorNVL[sttLoi]);
                    btnLuu.Enabled = true;
                    Debug.WriteLine($"Lỗi TTNVL (sttLoi={sttLoi}), thoát: {swTotal.ElapsedMilliseconds} ms");
                    return;
                }

                // Validate TP công đoạn
                swStep.Restart();
                var thongTinThanhPham = (TTThanhPham)snap.Sections["UC_TTThanhPham"];
                sttLoi = Validator.TTThanhPham(thongTinThanhPham);
                Debug.WriteLine($"Validator.TTThanhPham: {swStep.ElapsedMilliseconds} ms (tổng: {swTotal.ElapsedMilliseconds} ms)");

                if (sttLoi > 0)
                {
                    waiting.Close();
                    waiting.Dispose();
                    FrmWaiting.ShowGifAlert(ErrorStore.ErrorTP[sttLoi]);
                    btnLuu.Enabled = true;
                    Debug.WriteLine($"Lỗi TTThanhPham (sttLoi={sttLoi}), thoát: {swTotal.ElapsedMilliseconds} ms");
                    return;
                }

                // Validate chi tiết công đoạn
                swStep.Restart();
                var chiTietCD = Validator.KiemTraChiTietCongDoan(snap);
                Debug.WriteLine($"Validator.KiemTraChiTietCongDoan: {swStep.ElapsedMilliseconds} ms (tổng: {swTotal.ElapsedMilliseconds} ms)");

                if (chiTietCD[0] == null)
                {
                    waiting.Close();
                    waiting.Dispose();
                    FrmWaiting.ShowGifAlert("Chi tiết công đoạn chưa hợp lệ");
                    btnLuu.Enabled = true;
                    Debug.WriteLine($"Chi tiết công đoạn chưa hợp lệ, thoát: {swTotal.ElapsedMilliseconds} ms");
                    return;
                }

                // ✅ CẬP NHẬT MESSAGE SAU KHI VALIDATE XONG
                bool shouldPrint = (_printer != "" && cbInTem.Checked);
                waiting.MessageText = shouldPrint
                    ? "ĐANG LƯU DỮ LIỆU VÀ IN TEM..."
                    : "ĐANG LƯU DỮ LIỆU...";

                // === CHUẨN BỊ DỮ LIỆU ===
                var editmodel = (EditModel)snap.Sections["UC_Edit"];
                int idEdit = editmodel.Id;

                bool saveSuccess = false;
                bool hasPrintError = false;
                string saveError = null;
                string printError = null;

                PrinterModel BuildPrinter()
                {
                    return new PrinterModel
                    {
                        NgaySX = DateTime.ParseExact(
                            thongTinCaLamViec.Ngay,
                            "yyyy-MM-dd",
                            CultureInfo.InvariantCulture
                        ).ToString("dd/MM/yyyy"),
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

                // === CHẠY TASK LƯU + IN ===
                Task.Run(() =>
                {
                    var swTask = Stopwatch.StartNew();
                    Debug.WriteLine("=== [BTN LƯU] Task.Run BẮT ĐẦU ===");

                    try
                    {
                        // 1) Lưu dữ liệu
                        var swDb = Stopwatch.StartNew();
                        string err = string.Empty;

                        if (idEdit == 0)
                        {
                            saveSuccess = DatabaseHelper.SaveDataSanPham(
                                thongTinCaLamViec, thongTinThanhPham, list_TTNVL, chiTietCD, out err);
                            Debug.WriteLine($"SaveDataSanPham: {swDb.ElapsedMilliseconds} ms");
                        }
                        else
                        {
                            saveSuccess = DatabaseHelper.UpdateDataSanPham(
                                idEdit, thongTinCaLamViec, thongTinThanhPham, list_TTNVL, chiTietCD, out err);
                            Debug.WriteLine($"UpdateDataSanPham: {swDb.ElapsedMilliseconds} ms");
                        }

                        if (!saveSuccess)
                        {
                            saveError = string.IsNullOrEmpty(err) ? "LƯU KHÔNG THÀNH CÔNG." : err;
                            Debug.WriteLine($"Lưu thất bại sau: {swTask.ElapsedMilliseconds} ms, lỗi: {saveError}");
                            return;
                        }

                        // 2) In tem nếu lưu ok và có chọn in
                        if (shouldPrint)
                        {
                            try
                            {
                                var swPrintTP = Stopwatch.StartNew();
                                var printer = BuildPrinter();
                                PrintHelper.PrintLabel(printer);
                                Debug.WriteLine($"In tem thành phẩm: {swPrintTP.ElapsedMilliseconds} ms");

                                List<string> dsBin = new List<string>();
                                foreach (TTNVL nvl in list_TTNVL)
                                {
                                    if ((nvl.DonVi == "KG" && nvl.KlConLai == 0) ||
                                        (nvl.DonVi == "M" && nvl.CdConLai == 0) ||
                                        nvl.CdBatDau == -1 || nvl.KlBatDau == -1)
                                        continue;
                                    dsBin.Add(nvl.BinNVL);
                                }

                                var swGetPrinterData = Stopwatch.StartNew();
                                List<PrinterModel> nvl_printer = DatabaseHelper.GetPrinterDataByListBin(dsBin);
                                Debug.WriteLine($"GetPrinterDataByListBin: {swGetPrinterData.ElapsedMilliseconds} ms");

                                if (nvl_printer != null && nvl_printer.Count > 0)
                                {
                                    var swPrintNVL = Stopwatch.StartNew();
                                    foreach (PrinterModel item in nvl_printer)
                                    {
                                        PrintHelper.PrintLabel(item);
                                    }
                                    Debug.WriteLine($"In tem NVL: {swPrintNVL.ElapsedMilliseconds} ms");
                                }
                            }
                            catch (Exception exPrint)
                            {
                                hasPrintError = true;
                                printError = exPrint.Message;
                                Debug.WriteLine($"Lỗi in tem: {exPrint.Message}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Exception trong Task.Run: {ex}");

                        if (!saveSuccess)
                        {
                            saveSuccess = false;
                            saveError = "LỖI LƯU DỮ LIỆU: " + ex.Message;
                        }
                        else
                        {
                            hasPrintError = true;
                            printError = "\n" + ex.Message;
                        }
                    }
                    finally
                    {
                        Debug.WriteLine($"Task.Run kết thúc sau: {swTask.ElapsedMilliseconds} ms; Tổng: {swTotal.ElapsedMilliseconds} ms");

                        // 3) Quay lại UI thread
                        this.BeginInvoke(new Action(() =>
                        {
                            var swUi = Stopwatch.StartNew();

                            waiting.CloseAndDispose();
                            Debug.WriteLine("Đã đóng FrmWaiting");

                            // 🔔 Thông báo kết quả lưu
                            string msgSave = (idEdit > 0 ? "SỬA" : "LƯU");
                            string iconAlert = "warning";

                            if (saveSuccess)
                            {
                                msgSave += " THÀNH CÔNG ";
                                iconAlert = "ok";
                            }
                            else
                            {
                                msgSave += " KHÔNG THÀNH CÔNG\nLỗi: " + saveError;
                            }

                            FrmWaiting.ShowGifAlert(msgSave, "THÔNG BÁO", iconAlert);

                            // 🔔 Thông báo lỗi in (nếu có)
                            if (saveSuccess && hasPrintError && !string.IsNullOrEmpty(printError))
                            {
                                FrmWaiting.ShowGifAlert(printError.ToUpper(), "LỖI IN");
                            }

                            // Clear form nếu lưu thành công
                            if (saveSuccess)
                            {
                                var swClear = Stopwatch.StartNew();
                                ControlCleaner.ClearAll(host);
                                Debug.WriteLine($"ClearAll host: {swClear.ElapsedMilliseconds} ms");
                            }

                            btnLuu.Enabled = true;
                            Debug.WriteLine($"UI cập nhật xong - thời gian: {swUi.ElapsedMilliseconds} ms; Tổng: {swTotal.ElapsedMilliseconds} ms");
                        }));
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception ngoài Task.Run: {ex}");

                waiting?.CloseAndDispose();

                FrmWaiting.ShowGifAlert("LỖI: " + ex.Message, "LỖI");
                btnLuu.Enabled = true;
                Debug.WriteLine($"Kết thúc trong catch, tổng thời gian: {swTotal.ElapsedMilliseconds} ms");
            }
        }

        // ver 2
        //private void btnLuu_Click(object sender, EventArgs e)
        //{
        //    var swTotal = Stopwatch.StartNew();
        //    Debug.WriteLine("=== [BTN LƯU] BẮT ĐẦU ===");

        //    btnLuu.Enabled = false;

        //    // ✅ HIỂN THỊ WAITING NGAY TỪ ĐẦU
        //    var waiting = new FrmWaiting("ĐANG XỬ LÝ...");
        //    waiting.TopMost = true;
        //    waiting.StartPosition = FormStartPosition.CenterScreen;
        //    waiting.Show();
        //    waiting.Refresh();
        //    Application.DoEvents();
        //    Debug.WriteLine($"Hiển thị waiting: {swTotal.ElapsedMilliseconds} ms");

        //    try
        //    {
        //        Debug.WriteLine("btnLuu.Enabled = false");

        //        // === VALIDATION & CAPTURE DATA ===
        //        string tb = Helper.Helper.TaoThongBao(lblTrangThai);
        //        Debug.WriteLine($"TaoThongBao: {swTotal.ElapsedMilliseconds} ms");

        //        if (tb != "")
        //        {
        //            waiting.Close();
        //            waiting.Dispose();

        //            if (!string.IsNullOrEmpty(tb))
        //            {
        //                _timerThongBao.Stop();
        //                _timerThongBao.Start();
        //            }
        //            btnLuu.Enabled = true;
        //            Debug.WriteLine($"Thoát sớm vì tb != \"\": {swTotal.ElapsedMilliseconds} ms");
        //            return;
        //        }

        //        var swStep = Stopwatch.StartNew();
        //        var host = this.FindForm();
        //        Debug.WriteLine($"FindForm: {swStep.ElapsedMilliseconds} ms (tổng: {swTotal.ElapsedMilliseconds} ms)");

        //        if (host == null)
        //        {
        //            waiting.Close();
        //            waiting.Dispose();
        //            btnLuu.Enabled = true;
        //            Debug.WriteLine($"host == null, thoát: {swTotal.ElapsedMilliseconds} ms");
        //            return;
        //        }

        //        swStep.Restart();
        //        FormSnapshot snap = null;
        //        try
        //        {
        //            snap = DG_TonKhoBTP_v02.Core.FormSnapshotBuilder.Capture(host);
        //        }
        //        catch (Exception ex)
        //        {
        //            Debug.WriteLine($"Lỗi Capture snapshot: {ex.Message}");
        //            waiting.Close();
        //            waiting.Dispose();
        //            btnLuu.Enabled = true;
        //            //FrmWaiting.ShowGifAlert("LỖI CAPTURE DỮ LIỆU: " + ex.Message, "LỖI");
        //            return;
        //        }
        //        Debug.WriteLine($"Capture snapshot: {swStep.ElapsedMilliseconds} ms (tổng: {swTotal.ElapsedMilliseconds} ms)");

        //        if (snap == null || snap.Sections.Count < 4)
        //        {
        //            waiting.Close();
        //            waiting.Dispose();
        //            btnLuu.Enabled = true;
        //            Debug.WriteLine($"Snapshot null/invalid: {swTotal.ElapsedMilliseconds} ms");
        //            return;
        //        }

        //        // Lấy UC_TTSanPham & gộp Sections
        //        swStep.Restart();
        //        var ucSanPham = Helper.Helper.FindControlRecursive<UC_TTSanPham>(host);
        //        if (ucSanPham != null)
        //        {
        //            var extra = ucSanPham.GetAggregateSections();
        //            foreach (var kv in extra)
        //                snap.Sections[kv.Key] = kv.Value;
        //        }
        //        Debug.WriteLine($"Merge UC_TTSanPham: {swStep.ElapsedMilliseconds} ms (tổng: {swTotal.ElapsedMilliseconds} ms)");

        //        // Validate Ca Làm Việc
        //        swStep.Restart();
        //        var thongTinCaLamViec = (ThongTinCaLamViec)snap.Sections["UC_TTCaLamViec"];
        //        int sttLoi = Validator.TTCaLamViec(thongTinCaLamViec);
        //        Debug.WriteLine($"Validator.TTCaLamViec: {swStep.ElapsedMilliseconds} ms (tổng: {swTotal.ElapsedMilliseconds} ms)");

        //        if (sttLoi > 0)
        //        {
        //            waiting.Close();
        //            waiting.Dispose();
        //            FrmWaiting.ShowGifAlert(ErrorStore.ErrorCaLamViec[sttLoi]);
        //            btnLuu.Enabled = true;
        //            Debug.WriteLine($"Lỗi TTCaLamViec (sttLoi={sttLoi}), thoát: {swTotal.ElapsedMilliseconds} ms");
        //            return;
        //        }

        //        // Validate NVL
        //        swStep.Restart();
        //        var list_TTNVL = snap.Sections["UC_TTNVL"] as List<TTNVL>;
        //        sttLoi = Validator.TTNVL(list_TTNVL);
        //        Debug.WriteLine($"Validator.TTNVL: {swStep.ElapsedMilliseconds} ms (tổng: {swTotal.ElapsedMilliseconds} ms)");

        //        if (sttLoi > 0)
        //        {
        //            waiting.Close();
        //            waiting.Dispose();
        //            FrmWaiting.ShowGifAlert(ErrorStore.ErrorNVL[sttLoi]);
        //            btnLuu.Enabled = true;
        //            Debug.WriteLine($"Lỗi TTNVL (sttLoi={sttLoi}), thoát: {swTotal.ElapsedMilliseconds} ms");
        //            return;
        //        }

        //        // Validate TP công đoạn
        //        swStep.Restart();
        //        var thongTinThanhPham = (TTThanhPham)snap.Sections["UC_TTThanhPham"];
        //        sttLoi = Validator.TTThanhPham(thongTinThanhPham);
        //        Debug.WriteLine($"Validator.TTThanhPham: {swStep.ElapsedMilliseconds} ms (tổng: {swTotal.ElapsedMilliseconds} ms)");

        //        if (sttLoi > 0)
        //        {
        //            waiting.Close();
        //            waiting.Dispose();
        //            FrmWaiting.ShowGifAlert(ErrorStore.ErrorTP[sttLoi]);
        //            btnLuu.Enabled = true;
        //            Debug.WriteLine($"Lỗi TTThanhPham (sttLoi={sttLoi}), thoát: {swTotal.ElapsedMilliseconds} ms");
        //            return;
        //        }

        //        // Validate chi tiết công đoạn
        //        swStep.Restart();
        //        var chiTietCD = Validator.KiemTraChiTietCongDoan(snap);
        //        Debug.WriteLine($"Validator.KiemTraChiTietCongDoan: {swStep.ElapsedMilliseconds} ms (tổng: {swTotal.ElapsedMilliseconds} ms)");

        //        if (chiTietCD[0] == null)
        //        {
        //            waiting.Close();
        //            waiting.Dispose();
        //            FrmWaiting.ShowGifAlert("Chi tiết công đoạn chưa hợp lệ");
        //            btnLuu.Enabled = true;
        //            Debug.WriteLine($"Chi tiết công đoạn chưa hợp lệ, thoát: {swTotal.ElapsedMilliseconds} ms");
        //            return;
        //        }

        //        // ✅ CẬP NHẬT MESSAGE SAU KHI VALIDATE XONG
        //        bool shouldPrint = (_printer != "" && cbInTem.Checked);
        //        waiting.MessageText = shouldPrint
        //            ? "ĐANG LƯU DỮ LIỆU VÀ IN TEM..."
        //            : "ĐANG LƯU DỮ LIỆU...";
        //        waiting.Refresh();
        //        Application.DoEvents();

        //        // === CHUẨN BỊ DỮ LIỆU ===
        //        var editmodel = (EditModel)snap.Sections["UC_Edit"];
        //        int idEdit = editmodel.Id;

        //        bool saveSuccess = false;
        //        bool hasPrintError = false;
        //        string saveError = null;
        //        string printError = null;

        //        PrinterModel BuildPrinter()
        //        {
        //            return new PrinterModel
        //            {
        //                NgaySX = DateTime.ParseExact(
        //                    thongTinCaLamViec.Ngay,
        //                    "yyyy-MM-dd",
        //                    CultureInfo.InvariantCulture
        //                ).ToString("dd/MM/yyyy"),
        //                CaSX = thongTinCaLamViec.Ca,
        //                KhoiLuong = thongTinThanhPham.KhoiLuongSau.ToString(),
        //                ChieuDai = thongTinThanhPham.ChieuDaiSau.ToString(),
        //                TenSP = thongTinThanhPham.TenTP,
        //                MaBin = thongTinThanhPham.MaBin,
        //                MaSP = thongTinThanhPham.MaTP,
        //                DanhGia = "",
        //                TenCN = Helper.Helper.ConvertTiengVietKhongDau(thongTinCaLamViec.NguoiLam),
        //                GhiChu = Helper.Helper.ConvertTiengVietKhongDau(thongTinThanhPham.GhiChu)
        //            };
        //        }

        //        // === CHẠY TASK LƯU + IN ===
        //        Task.Run(() =>
        //        {
        //            var swTask = Stopwatch.StartNew();
        //            Debug.WriteLine("=== [BTN LƯU] Task.Run BẮT ĐẦU ===");

        //            try
        //            {
        //                // 1) Lưu dữ liệu
        //                var swDb = Stopwatch.StartNew();
        //                string err = string.Empty;

        //                if (idEdit == 0)
        //                {
        //                    saveSuccess = DatabaseHelper.SaveDataSanPham(
        //                        thongTinCaLamViec, thongTinThanhPham, list_TTNVL, chiTietCD, out err);
        //                    Debug.WriteLine($"SaveDataSanPham: {swDb.ElapsedMilliseconds} ms");
        //                }
        //                else
        //                {
        //                    saveSuccess = DatabaseHelper.UpdateDataSanPham(
        //                        idEdit, thongTinCaLamViec, thongTinThanhPham, list_TTNVL, chiTietCD, out err);
        //                    Debug.WriteLine($"UpdateDataSanPham: {swDb.ElapsedMilliseconds} ms");
        //                }

        //                if (!saveSuccess)
        //                {
        //                    saveError = string.IsNullOrEmpty(err) ? "LƯU KHÔNG THÀNH CÔNG." : err;
        //                    Debug.WriteLine($"Lưu thất bại sau: {swTask.ElapsedMilliseconds} ms, lỗi: {saveError}");
        //                    return;
        //                }

        //                // 2) In tem nếu lưu ok và có chọn in
        //                if (shouldPrint)
        //                {
        //                    try
        //                    {
        //                        var swPrintTP = Stopwatch.StartNew();
        //                        var printer = BuildPrinter();
        //                        PrintHelper.PrintLabel(printer);
        //                        Debug.WriteLine($"In tem thành phẩm: {swPrintTP.ElapsedMilliseconds} ms");

        //                        List<string> dsBin = new List<string>();
        //                        foreach (TTNVL nvl in list_TTNVL)
        //                        {
        //                            if ((nvl.DonVi == "KG" && nvl.KlConLai == 0) ||
        //                                (nvl.DonVi == "M" && nvl.CdConLai == 0) ||
        //                                nvl.CdBatDau == -1 || nvl.KlBatDau == -1)
        //                                continue;
        //                            dsBin.Add(nvl.BinNVL);
        //                        }

        //                        var swGetPrinterData = Stopwatch.StartNew();
        //                        List<PrinterModel> nvl_printer = DatabaseHelper.GetPrinterDataByListBin(dsBin);
        //                        Debug.WriteLine($"GetPrinterDataByListBin: {swGetPrinterData.ElapsedMilliseconds} ms");

        //                        if (nvl_printer != null && nvl_printer.Count > 0)
        //                        {
        //                            var swPrintNVL = Stopwatch.StartNew();
        //                            foreach (PrinterModel item in nvl_printer)
        //                            {
        //                                PrintHelper.PrintLabel(item);
        //                            }
        //                            Debug.WriteLine($"In tem NVL: {swPrintNVL.ElapsedMilliseconds} ms");
        //                        }
        //                    }
        //                    catch (Exception exPrint)
        //                    {
        //                        hasPrintError = true;
        //                        printError = exPrint.Message;
        //                        Debug.WriteLine($"Lỗi in tem: {exPrint.Message}");
        //                    }
        //                }
        //            }
        //            catch (Exception ex)
        //            {
        //                Debug.WriteLine($"Exception trong Task.Run: {ex}");

        //                if (!saveSuccess)
        //                {
        //                    saveSuccess = false;
        //                    saveError = "LỖI LƯU DỮ LIỆU: " + ex.Message;
        //                }
        //                else
        //                {
        //                    hasPrintError = true;
        //                    printError = "\n" + ex.Message;
        //                }
        //            }
        //            finally
        //            {
        //                Debug.WriteLine($"Task.Run kết thúc sau: {swTask.ElapsedMilliseconds} ms; Tổng: {swTotal.ElapsedMilliseconds} ms");

        //                // 3) Quay lại UI thread
        //                this.BeginInvoke(new Action(() =>
        //                {
        //                    var swUi = Stopwatch.StartNew();
        //                    try
        //                    {
        //                        waiting.Close();
        //                        waiting.Dispose();
        //                        Debug.WriteLine("Đã đóng FrmWaiting");
        //                    }
        //                    catch (Exception exClose)
        //                    {
        //                        Debug.WriteLine($"Lỗi đóng FrmWaiting: {exClose.Message}");
        //                    }

        //                    // 🔔 Thông báo kết quả lưu
        //                    string msgSave = (idEdit > 0 ? "SỬA" : "LƯU");
        //                    string iconAlert = "warning";

        //                    if (saveSuccess)
        //                    {
        //                        msgSave += " THÀNH CÔNG ";
        //                        iconAlert = "ok";
        //                    }
        //                    else
        //                    {
        //                        msgSave += " KHÔNG THÀNH CÔNG\nLỗi: " + saveError;
        //                    }

        //                    FrmWaiting.ShowGifAlert(msgSave, "THÔNG BÁO", iconAlert);

        //                    // 🔔 Thông báo lỗi in (nếu có)
        //                    if (saveSuccess && hasPrintError && !string.IsNullOrEmpty(printError))
        //                    {
        //                        FrmWaiting.ShowGifAlert(printError.ToUpper(), "LỖI IN");
        //                    }

        //                    // Clear form nếu lưu thành công
        //                    if (saveSuccess)
        //                    {
        //                        var swClear = Stopwatch.StartNew();
        //                        ControlCleaner.ClearAll(host);
        //                        Debug.WriteLine($"ClearAll host: {swClear.ElapsedMilliseconds} ms");
        //                    }

        //                    btnLuu.Enabled = true;
        //                    Debug.WriteLine($"UI cập nhật xong - thời gian: {swUi.ElapsedMilliseconds} ms; Tổng: {swTotal.ElapsedMilliseconds} ms");
        //                }));
        //            }
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        Debug.WriteLine($"Exception ngoài Task.Run: {ex}");

        //        try
        //        {
        //            waiting?.Close();
        //            waiting?.Dispose();
        //        }
        //        catch (Exception exClose)
        //        {
        //            Debug.WriteLine($"Lỗi đóng FrmWaiting trong catch: {exClose.Message}");
        //        }

        //        FrmWaiting.ShowGifAlert("LỖI: " + ex.Message, "LỖI");
        //        btnLuu.Enabled = true;
        //        Debug.WriteLine($"Kết thúc trong catch, tổng thời gian: {swTotal.ElapsedMilliseconds} ms");
        //    }
        //}

        // ver 1
        //private void btnLuu_Click(object sender, EventArgs e)
        //{
        //    var swTotal = Stopwatch.StartNew();
        //    Debug.WriteLine("=== [BTN LƯU] BẮT ĐẦU ===");

        //    string tb = Helper.Helper.TaoThongBao(lblTrangThai);
        //    Debug.WriteLine($"TaoThongBao: {swTotal.ElapsedMilliseconds} ms");

        //    if (tb != "")
        //    {
        //        if (!string.IsNullOrEmpty(tb))
        //        {
        //            _timerThongBao.Stop();  // đảm bảo reset
        //            _timerThongBao.Start(); // đếm 5 giây để tự ẩn label
        //        }
        //        Debug.WriteLine($"Thoát sớm vì tb != \"\": {swTotal.ElapsedMilliseconds} ms");
        //        return;
        //    }

        //    btnLuu.Enabled = false;
        //    Debug.WriteLine("btnLuu.Enabled = false");

        //    // --- Lấy form + snapshot ---
        //    var swStep = Stopwatch.StartNew();
        //    var host = this.FindForm();
        //    Debug.WriteLine($"FindForm: {swStep.ElapsedMilliseconds} ms (tổng: {swTotal.ElapsedMilliseconds} ms)");

        //    if (host == null)
        //    {
        //        btnLuu.Enabled = true;
        //        Debug.WriteLine($"host == null, thoát: {swTotal.ElapsedMilliseconds} ms");
        //        return;
        //    }

        //    swStep.Restart();
        //    FormSnapshot snap = null;
        //    try
        //    {
        //        snap = DG_TonKhoBTP_v02.Core.FormSnapshotBuilder.Capture(host);
        //    }
        //    catch (Exception ex)
        //    {
        //        Debug.WriteLine($"Lỗi Capture snapshot: {ex.Message}");
        //    }
        //    Debug.WriteLine($"Capture snapshot: {swStep.ElapsedMilliseconds} ms (tổng: {swTotal.ElapsedMilliseconds} ms)");

        //    if (snap == null || snap.Sections.Count < 4)
        //    {
        //        btnLuu.Enabled = true;
        //        Debug.WriteLine($"Snapshot null/invalid: {swTotal.ElapsedMilliseconds} ms");
        //        return;
        //    }

        //    // --- Lấy UC_TTSanPham & gộp Sections ---
        //    swStep.Restart();
        //    var ucSanPham = Helper.Helper.FindControlRecursive<UC_TTSanPham>(host);
        //    if (ucSanPham != null)
        //    {
        //        var extra = ucSanPham.GetAggregateSections();
        //        foreach (var kv in extra)
        //            snap.Sections[kv.Key] = kv.Value;
        //    }
        //    Debug.WriteLine($"Merge UC_TTSanPham vào snapshot: {swStep.ElapsedMilliseconds} ms (tổng: {swTotal.ElapsedMilliseconds} ms)");

        //    // --- Validate Ca Làm Việc ---
        //    swStep.Restart();
        //    var thongTinCaLamViec = (ThongTinCaLamViec)snap.Sections["UC_TTCaLamViec"];
        //    int sttLoi = Validator.TTCaLamViec(thongTinCaLamViec);
        //    Debug.WriteLine($"Validator.TTCaLamViec: {swStep.ElapsedMilliseconds} ms (tổng: {swTotal.ElapsedMilliseconds} ms)");

        //    if (sttLoi > 0)
        //    {
        //        FrmWaiting.ShowGifAlert(ErrorStore.ErrorCaLamViec[sttLoi]);
        //        btnLuu.Enabled = true;
        //        Debug.WriteLine($"Lỗi TTCaLamViec (sttLoi={sttLoi}), thoát: {swTotal.ElapsedMilliseconds} ms");
        //        return;
        //    }

        //    // --- Validate NVL ---
        //    swStep.Restart();
        //    var list_TTNVL = snap.Sections["UC_TTNVL"] as List<TTNVL>;
        //    sttLoi = Validator.TTNVL(list_TTNVL);
        //    Debug.WriteLine($"Validator.TTNVL: {swStep.ElapsedMilliseconds} ms (tổng: {swTotal.ElapsedMilliseconds} ms)");

        //    if (sttLoi > 0)
        //    {
        //        FrmWaiting.ShowGifAlert(ErrorStore.ErrorNVL[sttLoi]);
        //        btnLuu.Enabled = true;
        //        Debug.WriteLine($"Lỗi TTNVL (sttLoi={sttLoi}), thoát: {swTotal.ElapsedMilliseconds} ms");
        //        return;
        //    }

        //    // --- Validate TP công đoạn ---
        //    swStep.Restart();
        //    var thongTinThanhPham = (TTThanhPham)snap.Sections["UC_TTThanhPham"];
        //    sttLoi = Validator.TTThanhPham(thongTinThanhPham);
        //    Debug.WriteLine($"Validator.TTThanhPham: {swStep.ElapsedMilliseconds} ms (tổng: {swTotal.ElapsedMilliseconds} ms)");

        //    if (sttLoi > 0)
        //    {
        //        FrmWaiting.ShowGifAlert(ErrorStore.ErrorTP[sttLoi]);
        //        btnLuu.Enabled = true;
        //        Debug.WriteLine($"Lỗi TTThanhPham (sttLoi={sttLoi}), thoát: {swTotal.ElapsedMilliseconds} ms");
        //        return;
        //    }

        //    // --- Validate chi tiết công đoạn ---
        //    swStep.Restart();
        //    var chiTietCD = Validator.KiemTraChiTietCongDoan(snap);
        //    Debug.WriteLine($"Validator.KiemTraChiTietCongDoan: {swStep.ElapsedMilliseconds} ms (tổng: {swTotal.ElapsedMilliseconds} ms)");

        //    if (chiTietCD[0] == null)
        //    {
        //        FrmWaiting.ShowGifAlert("Chi tiết công đoạn chưa hợp lệ");
        //        btnLuu.Enabled = true;
        //        Debug.WriteLine($"Chi tiết công đoạn chưa hợp lệ, thoát: {swTotal.ElapsedMilliseconds} ms");
        //        return;
        //    }

        //    // --- Dữ liệu in ---
        //    PrinterModel BuildPrinter()
        //    {
        //        var swLocal = Stopwatch.StartNew();
        //        var model = new PrinterModel
        //        {
        //            NgaySX = DateTime.ParseExact(
        //                thongTinCaLamViec.Ngay,
        //                "yyyy-MM-dd",
        //                CultureInfo.InvariantCulture
        //            ).ToString("dd/MM/yyyy"),
        //            CaSX = thongTinCaLamViec.Ca,
        //            KhoiLuong = thongTinThanhPham.KhoiLuongSau.ToString(),
        //            ChieuDai = thongTinThanhPham.ChieuDaiSau.ToString(),
        //            TenSP = thongTinThanhPham.TenTP,
        //            MaBin = thongTinThanhPham.MaBin,
        //            MaSP = thongTinThanhPham.MaTP,
        //            DanhGia = "",
        //            TenCN = Helper.Helper.ConvertTiengVietKhongDau(thongTinCaLamViec.NguoiLam),
        //            GhiChu = Helper.Helper.ConvertTiengVietKhongDau(thongTinThanhPham.GhiChu)
        //        };
        //        Debug.WriteLine($"BuildPrinter: {swLocal.ElapsedMilliseconds} ms");
        //        return model;
        //    }

        //    // --- Lưu + in ---
        //    var editmodel = (EditModel)snap.Sections["UC_Edit"];
        //    int idEdit = editmodel.Id;
        //    string err = string.Empty;

        //    bool shouldPrint = false;
        //    if (_printer != "" && cbInTem.Checked)
        //    {
        //        shouldPrint = true;
        //    }

        //    string message = shouldPrint ? "Đang lưu dữ liệu và in tem..." : "Đang lưu dữ liệu...";

        //    bool saveSuccess = false;      // kết quả lưu
        //    bool hasPrintError = false;    // có lỗi in không
        //    string saveError = null;       // nội dung lỗi lưu (nếu có)
        //    string printError = null;      // nội dung lỗi in (nếu có)

        //    swStep.Restart();
        //    var waiting = new FrmWaiting(message.ToUpper());
        //    Debug.WriteLine($"Tạo FrmWaiting: {swStep.ElapsedMilliseconds} ms (tổng: {swTotal.ElapsedMilliseconds} ms)");

        //    try
        //    {
        //        waiting.TopMost = true;
        //        waiting.StartPosition = FormStartPosition.CenterScreen;

        //        var swWaiting = Stopwatch.StartNew();
        //        waiting.Show();
        //        waiting.Refresh();
        //        Application.DoEvents();
        //        Debug.WriteLine($"Show FrmWaiting: {swWaiting.ElapsedMilliseconds} ms (tổng: {swTotal.ElapsedMilliseconds} ms)");

        //        Task.Run(() =>
        //        {
        //            var swTask = Stopwatch.StartNew();
        //            Debug.WriteLine("=== [BTN LƯU] Task.Run BẮT ĐẦU ===");

        //            try
        //            {
        //                // 1) Lưu dữ liệu
        //                var swDb = Stopwatch.StartNew();
        //                if (idEdit == 0)
        //                {
        //                    saveSuccess = DatabaseHelper.SaveDataSanPham(
        //                        thongTinCaLamViec, thongTinThanhPham, list_TTNVL, chiTietCD, out err);
        //                    Debug.WriteLine($"SaveDataSanPham: {swDb.ElapsedMilliseconds} ms");
        //                }
        //                else
        //                {
        //                    saveSuccess = DatabaseHelper.UpdateDataSanPham(
        //                        idEdit, thongTinCaLamViec, thongTinThanhPham, list_TTNVL, chiTietCD, out err);
        //                    Debug.WriteLine($"UpdateDataSanPham: {swDb.ElapsedMilliseconds} ms");
        //                }

        //                if (!saveSuccess)
        //                {
        //                    saveError = string.IsNullOrEmpty(err) ? "LƯU KHÔNG THÀNH CÔNG." : err;
        //                    Debug.WriteLine($"Lưu thất bại sau: {swTask.ElapsedMilliseconds} ms, lỗi: {saveError}");
        //                    return; // ❌ không in khi lưu lỗi
        //                }

        //                // 2) In tem nếu lưu ok và có chọn in
        //                if (shouldPrint)
        //                {
        //                    try
        //                    {
        //                        var swPrintTP = Stopwatch.StartNew();
        //                        // In tem thành phẩm
        //                        var printer = BuildPrinter();
        //                        PrintHelper.PrintLabel(printer); // lỗi in sẽ ném Exception
        //                        Debug.WriteLine($"In tem thành phẩm: {swPrintTP.ElapsedMilliseconds} ms");

        //                        List<string> dsBin = new List<string>();

        //                        foreach (TTNVL nvl in list_TTNVL)
        //                        {
        //                            if ((nvl.DonVi == "KG" && nvl.KlConLai == 0) ||
        //                                (nvl.DonVi == "M" && nvl.CdConLai == 0) ||
        //                                nvl.CdBatDau == -1 || nvl.KlBatDau == -1)
        //                                continue;

        //                            dsBin.Add(nvl.BinNVL);
        //                        }

        //                        var swGetPrinterData = Stopwatch.StartNew();
        //                        List<PrinterModel> nvl_printer = DatabaseHelper.GetPrinterDataByListBin(dsBin);
        //                        Debug.WriteLine($"GetPrinterDataByListBin: {swGetPrinterData.ElapsedMilliseconds} ms");

        //                        if (nvl_printer != null && nvl_printer.Count > 0)
        //                        {
        //                            var swPrintNVL = Stopwatch.StartNew();
        //                            foreach (PrinterModel item in nvl_printer)
        //                            {
        //                                PrintHelper.PrintLabel(item); // lỗi in tiếp tục ném Exception
        //                            }
        //                            Debug.WriteLine($"In tem NVL: {swPrintNVL.ElapsedMilliseconds} ms");
        //                        }
        //                    }
        //                    catch (Exception exPrint)
        //                    {
        //                        hasPrintError = true;
        //                        printError = exPrint.Message;
        //                        Debug.WriteLine($"Lỗi in tem: {exPrint.Message}");
        //                    }
        //                }
        //            }
        //            catch (Exception ex)
        //            {
        //                // Lỗi “bất ngờ” trong logic lưu/in
        //                Debug.WriteLine($"Exception trong Task.Run: {ex}");

        //                if (!saveSuccess)
        //                {
        //                    // Chưa lưu hoặc lưu thất bại
        //                    saveSuccess = false;
        //                    saveError = "LỖI LƯU DỮ LIỆU: " + ex.Message;
        //                }
        //                else
        //                {
        //                    // Đã lưu thành công, lỗi xảy ra trong lúc in
        //                    hasPrintError = true;
        //                    printError = "\n" + ex.Message;
        //                }
        //            }
        //            finally
        //            {
        //                Debug.WriteLine($"Task.Run kết thúc sau: {swTask.ElapsedMilliseconds} ms; Tổng từ btnLuu_Click: {swTotal.ElapsedMilliseconds} ms");

        //                // 3) Quay lại UI (luôn luôn chạy trên UI thread)
        //                this.BeginInvoke(new Action(() =>
        //                {
        //                    var swUi = Stopwatch.StartNew();
        //                    try
        //                    {
        //                        waiting.Close();
        //                        waiting.Dispose();
        //                        Debug.WriteLine("Đã đóng FrmWaiting");
        //                    }
        //                    catch (Exception exClose)
        //                    {
        //                        Debug.WriteLine($"Lỗi đóng FrmWaiting: {exClose.Message}");
        //                    }

        //                    // 🔔 Thông báo 1: KẾT QUẢ LƯU
        //                    string msgSave = (idEdit > 0 ? "SỬA" : "LƯU");
        //                    string iconAlert = "warning";

        //                    if (saveSuccess)
        //                    {
        //                        msgSave += " THÀNH CÔNG ";
        //                        iconAlert = "ok";
        //                    }
        //                    else
        //                    {
        //                        msgSave += " KHÔNG THÀNH CÔNG\nLỗi: " + saveError;
        //                    }

        //                    FrmWaiting.ShowGifAlert(msgSave, "THÔNG BÁO", iconAlert);

        //                    // 🔔 Thông báo 2: LỖI IN (nếu có và chỉ khi lưu thành công)
        //                    if (saveSuccess && hasPrintError && !string.IsNullOrEmpty(printError))
        //                    {
        //                        FrmWaiting.ShowGifAlert(printError.ToUpper(), "LỖI IN");
        //                    }

        //                    // Nếu lưu OK thì clear form
        //                    if (saveSuccess)
        //                    {
        //                        var swClear = Stopwatch.StartNew();
        //                        ControlCleaner.ClearAll(host);
        //                        Debug.WriteLine($"ClearAll host: {swClear.ElapsedMilliseconds} ms");
        //                    }

        //                    btnLuu.Enabled = true; // bật lại nút
        //                    Debug.WriteLine($"UI cập nhật xong (btnLuu.Enabled = true) - thời gian UI: {swUi.ElapsedMilliseconds} ms; Tổng: {swTotal.ElapsedMilliseconds} ms");
        //                }));

        //            }
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        // Lỗi xảy ra trước khi Task bắt đầu (UI thread)
        //        Debug.WriteLine($"Exception ngoài Task.Run: {ex}");

        //        try
        //        {
        //            waiting?.Close();
        //            waiting?.Dispose();
        //        }
        //        catch (Exception exClose)
        //        {
        //            Debug.WriteLine($"Lỗi đóng FrmWaiting trong catch ngoài: {exClose.Message}");
        //        }

        //        FrmWaiting.ShowGifAlert("LỖI: " + ex.Message, "LỖI");

        //        btnLuu.Enabled = true;
        //        Debug.WriteLine($"Kết thúc trong catch ngoài, tổng thời gian: {swTotal.ElapsedMilliseconds} ms");
        //    }
        //}


        // ver 0
        //private void btnLuu_Click(object sender, EventArgs e)
        //{
        //    string tb = Helper.Helper.TaoThongBao(lblTrangThai);

        //    if (tb != "")
        //    {
        //        if (!string.IsNullOrEmpty(tb))
        //        {
        //            _timerThongBao.Stop();  // đảm bảo reset
        //            _timerThongBao.Start(); // đếm 5 giây để tự ẩn label
        //        }
        //        return;
        //    }

        //    btnLuu.Enabled = false;

        //    // --- Lấy form + snapshot ---
        //    var host = this.FindForm();
        //    if (host == null) { btnLuu.Enabled = true; return; }

        //    FormSnapshot snap = null;
        //    try { snap = DG_TonKhoBTP_v02.Core.FormSnapshotBuilder.Capture(host); }
        //    catch { }

        //    if (snap == null || snap.Sections.Count < 4)
        //    {
        //        btnLuu.Enabled = true;
        //        return;
        //    }

        //    var ucSanPham = Helper.Helper.FindControlRecursive<UC_TTSanPham>(host);
        //    if (ucSanPham != null)
        //    {
        //        var extra = ucSanPham.GetAggregateSections();
        //        foreach (var kv in extra)
        //            snap.Sections[kv.Key] = kv.Value;
        //    }

        //    // --- Validate Ca Làm Việc ---
        //    var thongTinCaLamViec = (ThongTinCaLamViec)snap.Sections["UC_TTCaLamViec"];
        //    int sttLoi = Validator.TTCaLamViec(thongTinCaLamViec);
        //    if (sttLoi > 0)
        //    {
        //        FrmWaiting.ShowGifAlert(ErrorStore.ErrorCaLamViec[sttLoi]);
        //        btnLuu.Enabled = true;
        //        return;
        //    }

        //    // --- Validate NVL ---
        //    var list_TTNVL = snap.Sections["UC_TTNVL"] as List<TTNVL>;
        //    sttLoi = Validator.TTNVL(list_TTNVL);
        //    if (sttLoi > 0)
        //    {
        //        FrmWaiting.ShowGifAlert(ErrorStore.ErrorNVL[sttLoi]);
        //        btnLuu.Enabled = true;
        //        return;
        //    }

        //    // --- Validate TP công đoạn ---
        //    var thongTinThanhPham = (TTThanhPham)snap.Sections["UC_TTThanhPham"];
        //    sttLoi = Validator.TTThanhPham(thongTinThanhPham);
        //    if (sttLoi > 0)
        //    {
        //        FrmWaiting.ShowGifAlert(ErrorStore.ErrorTP[sttLoi]);
        //        btnLuu.Enabled = true;
        //        return;
        //    }

        //    // --- Validate chi tiết công đoạn ---
        //    var chiTietCD = Validator.KiemTraChiTietCongDoan(snap);
        //    if (chiTietCD[0] == null)
        //    {
        //        FrmWaiting.ShowGifAlert("Chi tiết công đoạn chưa hợp lệ");
        //        btnLuu.Enabled = true;
        //        return;
        //    }

        //    // --- Dữ liệu in ---
        //    PrinterModel BuildPrinter()
        //    {
        //        // Lưu ý: ParseExact có thể ném exception; ta đang gọi trong background try/catch
        //        return new PrinterModel
        //        {
        //            NgaySX = DateTime.ParseExact(thongTinCaLamViec.Ngay, "yyyy-MM-dd", CultureInfo.InvariantCulture).ToString("dd/MM/yyyy"),
        //            //NgaySX = thongTinCaLamViec.Ngay,
        //            CaSX = thongTinCaLamViec.Ca,
        //            KhoiLuong = thongTinThanhPham.KhoiLuongSau.ToString(),
        //            ChieuDai = thongTinThanhPham.ChieuDaiSau.ToString(),
        //            TenSP = thongTinThanhPham.TenTP,
        //            MaBin = thongTinThanhPham.MaBin,
        //            MaSP = thongTinThanhPham.MaTP,
        //            DanhGia = "",
        //            TenCN = Helper.Helper.ConvertTiengVietKhongDau(thongTinCaLamViec.NguoiLam),
        //            GhiChu = Helper.Helper.ConvertTiengVietKhongDau(thongTinThanhPham.GhiChu)
        //        };
        //    }

        //    // --- Lưu + in ---
        //    var editmodel = (EditModel)snap.Sections["UC_Edit"];
        //    int idEdit = editmodel.Id;
        //    string err = string.Empty;
        //    //bool isSuccess = false;

        //    bool shouldPrint = false;

        //    if (_printer != "" && cbInTem.Checked)
        //    {
        //        shouldPrint = true;
        //    }

        //    string message = shouldPrint ? "Đang lưu dữ liệu và in tem..." : "Đang lưu dữ liệu...";


        //    bool saveSuccess = false;      // kết quả lưu
        //    bool hasPrintError = false;    // có lỗi in không
        //    string saveError = null;       // nội dung lỗi lưu (nếu có)
        //    string printError = null;      // nội dung lỗi in (nếu có)

        //    var waiting = new FrmWaiting(message.ToUpper());

        //    try
        //    {
        //        waiting.TopMost = true;
        //        waiting.StartPosition = FormStartPosition.CenterScreen;
        //        waiting.Show();
        //        waiting.Refresh();
        //        Application.DoEvents();

        //        Task.Run(() =>
        //        {
        //            try
        //            {
        //                // 1) Lưu dữ liệu
        //                if (idEdit == 0)
        //                    saveSuccess = DatabaseHelper.SaveDataSanPham(
        //                        thongTinCaLamViec, thongTinThanhPham, list_TTNVL, chiTietCD, out err);
        //                else
        //                    saveSuccess = DatabaseHelper.UpdateDataSanPham(
        //                        idEdit, thongTinCaLamViec, thongTinThanhPham, list_TTNVL, chiTietCD, out err);

        //                if (!saveSuccess)
        //                {
        //                    // Lưu không thành công: err thường đã có message từ DAL
        //                    saveError = string.IsNullOrEmpty(err) ? "LƯU KHÔNG THÀNH CÔNG." : err;
        //                    return; // ❌ không in khi lưu lỗi
        //                }

        //                // 2) In tem nếu lưu ok và có chọn in
        //                if (shouldPrint)
        //                {
        //                    try
        //                    {
        //                        // In tem thành phẩm
        //                        var printer = BuildPrinter();
        //                        PrintHelper.PrintLabel(printer); // lỗi in sẽ ném Exception

        //                        List<string> dsBin = new List<string>();

        //                        // In tem nguyên liệu (nếu có)
        //                        foreach (TTNVL nvl in list_TTNVL)
        //                        {
        //                            if ((nvl.DonVi == "KG" && nvl.KlConLai == 0) ||
        //                                (nvl.DonVi == "M" && nvl.CdConLai == 0) ||
        //                                nvl.CdBatDau == -1 || nvl.KlBatDau == -1)
        //                                continue;

        //                            dsBin.Add(nvl.BinNVL);
        //                        }

        //                        List<PrinterModel> nvl_printer = DatabaseHelper.GetPrinterDataByListBin(dsBin);

        //                        if (nvl_printer != null && nvl_printer.Count > 0)
        //                        {
        //                            foreach (PrinterModel item in nvl_printer)
        //                            {
        //                                PrintHelper.PrintLabel(item); // lỗi in tiếp tục ném Exception
        //                            }
        //                        }
        //                    }
        //                    catch (Exception exPrint)
        //                    {
        //                        // LƯU VẪN OK, chỉ có lỗi IN
        //                        hasPrintError = true;
        //                        printError = exPrint.Message;
        //                    }
        //                }
        //            }
        //            catch (Exception ex)
        //            {
        //                // Lỗi “bất ngờ” trong logic lưu/in
        //                if (!saveSuccess)
        //                {
        //                    // Chưa lưu hoặc lưu thất bại
        //                    saveSuccess = false;
        //                    saveError = "LỖI LƯU DỮ LIỆU: " + ex.Message;
        //                }
        //                else
        //                {
        //                    // Đã lưu thành công, lỗi xảy ra trong lúc in
        //                    hasPrintError = true;
        //                    printError = "\n" + ex.Message;
        //                }
        //            }
        //            finally
        //            {
        //                // 3) Quay lại UI (luôn luôn chạy trên UI thread)
        //                this.BeginInvoke(new Action(() =>
        //                {
        //                    try { waiting.Close(); waiting.Dispose(); } catch { }

        //                    // 🔔 Thông báo 1: KẾT QUẢ LƯU
        //                    string msgSave = (idEdit > 0 ? "SỬA" : "LƯU");
        //                    string iconAlert = "warning";

        //                    if (saveSuccess)
        //                    {
        //                        msgSave += " THÀNH CÔNG ";
        //                        iconAlert = "ok";
        //                    }
        //                    else
        //                    {
        //                        msgSave += " KHÔNG THÀNH CÔNG\nLỗi: " + saveError;
        //                    }

        //                    FrmWaiting.ShowGifAlert(msgSave,"THÔNG BÁO", iconAlert);

        //                    // 🔔 Thông báo 2: LỖI IN (nếu có và chỉ khi lưu thành công)
        //                    if (saveSuccess && hasPrintError && !string.IsNullOrEmpty(printError))
        //                    {
        //                        FrmWaiting.ShowGifAlert(printError.ToUpper(), "LỖI IN");
        //                    }

        //                    // Nếu lưu OK thì clear form
        //                    if (saveSuccess)
        //                        ControlCleaner.ClearAll(host);

        //                    btnLuu.Enabled = true; // bật lại nút
        //                }));

        //            }
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        // Lỗi xảy ra trước khi Task bắt đầu (UI thread)
        //        try { waiting?.Close(); waiting?.Dispose(); } catch { }

        //        FrmWaiting.ShowGifAlert("LỖI: " + ex.Message, "LỖI");

        //        btnLuu.Enabled = true;
        //    }

        //}


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
