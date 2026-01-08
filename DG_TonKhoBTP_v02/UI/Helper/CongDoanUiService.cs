using DG_TonKhoBTP_v02.Models;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace DG_TonKhoBTP_v02.UI.Helper
{
    public class CongDoanUiService
    {
        private readonly Func<bool> _shouldReturnEarly; // vd: () => CoreHelper.KiemTraEmpty(_URL)
        private readonly Action<Button> _highlightMenuButton;
        private readonly Panel _pnShow;

        private readonly Func<Control, UC_TTNVL> _findNVL;
        private readonly Func<Control, UC_TTThanhPham> _findTP;

        private readonly Func<List<ColumnDefinition>, UC_TTSanPham, CongDoan, bool, Panel> _createBottomPanel;
        private readonly Action<CongDoan, Panel> _showUI;

        public CongDoanUiService(
            Func<bool> shouldReturnEarly,
            Action<Button> highlightMenuButton,
            Panel pnShow,
            Func<Control, UC_TTNVL> findNVL,
            Func<Control, UC_TTThanhPham> findTP,
            Func<List<ColumnDefinition>, UC_TTSanPham, CongDoan, bool, Panel> createBottomPanel,
            Action<CongDoan, Panel> showUI
        )
        {
            _shouldReturnEarly = shouldReturnEarly ?? throw new ArgumentNullException(nameof(shouldReturnEarly));
            _highlightMenuButton = highlightMenuButton ?? throw new ArgumentNullException(nameof(highlightMenuButton));
            _pnShow = pnShow ?? throw new ArgumentNullException(nameof(pnShow));

            _findNVL = findNVL ?? throw new ArgumentNullException(nameof(findNVL));
            _findTP = findTP ?? throw new ArgumentNullException(nameof(findTP));

            _createBottomPanel = createBottomPanel ?? throw new ArgumentNullException(nameof(createBottomPanel));
            _showUI = showUI ?? throw new ArgumentNullException(nameof(showUI));
        }

        /// <summary>
        /// Dùng cho các công đoạn dạng: thongTinCD + UC_TTSanPham + UI_BottomPanel + ShowUI
        /// </summary>
        public void InitCongDoanUI(
            Button clickedButton,
            CongDoan thongTinCD,
            Func<UC_TTSanPham> createSanPham,
            bool rawMaterial,
            string errorMessagePrefix,
            Action<Control> afterShowUI = null
        )
        {
            if (clickedButton == null) return;
            if (_shouldReturnEarly()) return;

            _highlightMenuButton(clickedButton);
            clickedButton.Enabled = false;

            using (var waiting = new FrmWaiting("ĐANG KHỞI TẠO GIAO DIỆN..."))
            {
                try
                {
                    waiting.ShowAndRefresh();

                    _pnShow.SuspendLayout();
                    _pnShow.Visible = false;

                    List<ColumnDefinition> columns = thongTinCD.Columns;
                    var ucSanPham = createSanPham();

                    Panel pnBottom = _createBottomPanel(columns, ucSanPham, thongTinCD, rawMaterial);

                    _showUI(thongTinCD, pnBottom);

                    afterShowUI?.Invoke(_pnShow);
                }
                catch (Exception ex)
                {
                    FrmWaiting.ShowGifAlert($"Lỗi khởi tạo giao diện {errorMessagePrefix}: {ex.Message}");
                }
                finally
                {
                    _pnShow.Visible = true;
                    _pnShow.ResumeLayout(true);
                    waiting?.CloseAndDispose();
                    clickedButton.Enabled = true;
                }
            }
        }

        /// <summary>
        /// Dùng cho các màn hình custom kiểu: pnShow.Controls.Clear(); pnShow.Controls.Add(new UC_xxx)
        /// </summary>
        public void InitCustomUI(
            Button clickedButton,
            string errorMessagePrefix,
            Func<Control> createControl
        )
        {
            if (clickedButton == null) return;
            if (_shouldReturnEarly()) return;

            _highlightMenuButton(clickedButton);
            clickedButton.Enabled = false;

            using (var waiting = new FrmWaiting("ĐANG KHỞI TẠO GIAO DIỆN..."))
            {
                try
                {
                    waiting.ShowAndRefresh();

                    _pnShow.SuspendLayout();
                    _pnShow.Visible = false;

                    _pnShow.Controls.Clear();

                    var ctrl = createControl();
                    if (ctrl != null)
                    {
                        ctrl.Dock = DockStyle.Fill;
                        _pnShow.Controls.Add(ctrl);
                    }
                }
                catch (Exception ex)
                {
                    FrmWaiting.ShowGifAlert($"Lỗi khởi tạo giao diện {errorMessagePrefix}: {ex.Message}");
                }
                finally
                {
                    _pnShow.Visible = true;
                    _pnShow.ResumeLayout(true);
                    waiting?.CloseAndDispose();
                    clickedButton.Enabled = true;
                }
            }
        }

        /// <summary>
        /// Hook chung NVL <-> Thành phẩm (KeoRút, BẻnRuột dùng)
        /// </summary>
        public void HookNvlThanhPham(Control root)
        {
            var ucNVL = _findNVL(root);
            var ucTP = _findTP(root);

            if (ucNVL == null || ucTP == null)
                return;

            ucNVL.GetKhoiLuong = () => ucTP.KhoiLuongValue;

            ucNVL.GetTenMay = () =>
            {
                string may = ucTP.SoLOTValue?.Split('-')[0] ?? "";
                return may;
            };

            ucTP.SoLOTChanged -= ucNVL.OnSoLOTChanged;
            ucTP.SoLOTChanged += ucNVL.OnSoLOTChanged;
        }
    }
}
