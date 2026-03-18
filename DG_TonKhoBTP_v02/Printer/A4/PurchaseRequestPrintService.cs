using DG_TonKhoBTP_v02.Printer.A4;
using System.Collections.Generic;
using System.Windows.Forms;
using static DG_TonKhoBTP_v02.Printer.A4.A4Config;
using static DG_TonKhoBTP_v02.Printer.A4.PrinterModel;

namespace DG_TonKhoBTP_v02.Printer
{
    // ═══════════════════════════════════════════════════════════════════════════
    // Giấy đề nghị Mua Dịch Vụ  (Mẫu B)
    // ═══════════════════════════════════════════════════════════════════════════

    public class PurchaseRequestPrintService
    {
        private readonly PurchaseRequestPrintData _data;

        public PurchaseRequestPrintService(PurchaseRequestPrintData data) => _data = data;

        public void ShowPreview(IWin32Window owner = null)
        {
            var mgr = new PrintManager<PurchaseRequestPrintData>(_data, new A4Printer.PurchaseRequestRenderer());
            mgr.ShowPreview(owner);
        }

        public void Print()
        {
            var mgr = new PrintManager<PurchaseRequestPrintData>(_data, new A4Printer.PurchaseRequestRenderer());
            mgr.Print();
        }

        
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Giấy đề nghị Mua Vật Tư  (Mẫu A)
    // ═══════════════════════════════════════════════════════════════════════════

    public class MaterialRequestPrintService
    {
        private readonly MaterialRequestPrintData _data;

        public MaterialRequestPrintService(MaterialRequestPrintData data) => _data = data;

        public void ShowPreview(IWin32Window owner = null)
        {
            var mgr = new PrintManager<MaterialRequestPrintData>(_data, new A4Printer.MaterialRequestRenderer());
            mgr.ShowPreview(owner);
        }

        public void Print()
        {
            var mgr = new PrintManager<MaterialRequestPrintData>(_data, new A4Printer.MaterialRequestRenderer());
            mgr.Print();
        }

        
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Phiếu Nhập Kho
    // ═══════════════════════════════════════════════════════════════════════════

    public class WarehouseReceiptPrintService
    {
        private readonly WarehouseReceiptPrintData _data;

        public WarehouseReceiptPrintService(WarehouseReceiptPrintData data) => _data = data;

        public void ShowPreview(IWin32Window owner = null)
        {
            var mgr = new PrintManager<WarehouseReceiptPrintData>(_data, new WarehouseReceiptRenderer());
            mgr.ShowPreview(owner);
        }

        public void Print()
        {
            var mgr = new PrintManager<WarehouseReceiptPrintData>(_data, new WarehouseReceiptRenderer());
            mgr.Print();
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Phiếu Xuất Kho
    // ═══════════════════════════════════════════════════════════════════════════

    public class WarehouseIssuesPrintService
    {
        private readonly WarehouseIssuesPrintData _data;

        public WarehouseIssuesPrintService(WarehouseIssuesPrintData data) => _data = data;

        public void ShowPreview(IWin32Window owner = null)
        {
            var mgr = new PrintManager<WarehouseIssuesPrintData>(_data, new WarehouseIssuesRenderer());
            mgr.ShowPreview(owner);
        }

        public void Print()
        {
            var mgr = new PrintManager<WarehouseIssuesPrintData>(_data, new WarehouseIssuesRenderer());
            mgr.Print();
        }
    }
}