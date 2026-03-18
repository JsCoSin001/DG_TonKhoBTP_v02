using System.Collections.Generic;

namespace DG_TonKhoBTP_v02.Printer.A4
{
    public class PrinterModel
    {
        // ─── Thông tin chung dùng cho mọi loại phiếu ───────────────────────

        public class CompanyInfo
        {
            public string CompanyName { get; set; } = "CÔNG TY CỔ PHẦN ĐÔNG GIANG";
            public string Address { get; set; } = "KCN Phố Nối A, Xã Như Quỳnh, Tỉnh Hưng Yên, Việt Nam";
            public string LogoPath { get; set; } = System.IO.Path.Combine(
                                                          System.AppDomain.CurrentDomain.BaseDirectory, "Assets", "logo.png");
            public string FormCode { get; set; }  = "BM-12-01";
            public string IssueDate { get; set; } = "01/06/2018";
            public string Revision { get; set; } = "0";
        }

        public class DocumentInfo
        {
            public string Title { get; set; }
            public string OrderDate { get; set; }
            public string OrderCode { get; set; }
        }

        public class SignatureInfo
        {
            public string DirectorTitle { get; set; } = "GIÁM ĐỐC CÔNG TY";
            public string FactoryDirectorTitle { get; set; } = "GIÁM ĐỐC NHÀ MÁY";
            public string CheckerTitle { get; set; } = "NGƯỜI KIỂM TRA";
            public string RequesterTitle { get; set; } = "NGƯỜI ĐỀ NGHỊ";

            public string DirectorName { get; set; }
            public string FactoryDirectorName { get; set; }
            public string CheckerName { get; set; }
            public string RequesterName { get; set; }
        }

        // ─── Phiếu Mua Dịch Vụ (Mẫu B) ────────────────────────────────────

        public class PurchaseRequestPrintData
        {
            public CompanyInfo Company { get; set; } = new CompanyInfo();
            public DocumentInfo Document { get; set; } = new DocumentInfo();
            public List<ServiceItem> Items { get; set; } = new List<ServiceItem>();
            public SignatureInfo Signature { get; set; } = new SignatureInfo();
        }

        public class ServiceItem
        {
            public int No { get; set; }
            public string ServiceName { get; set; }
            public string Purpose { get; set; }
            public string RequiredDate { get; set; }
        }

        // ─── Phiếu Mua Vật Tư (Mẫu A) ─────────────────────────────────────

        public class MaterialRequestPrintData
        {
            public CompanyInfo Company { get; set; } = new CompanyInfo();
            public DocumentInfo Document { get; set; } = new DocumentInfo();
            public List<MaterialItem> Items { get; set; } = new List<MaterialItem>();
            public SignatureInfo Signature { get; set; } = new SignatureInfo();
        }

        public class MaterialItem
        {
            public int No { get; set; }
            public string MaterialCode { get; set; }
            public string MaterialName { get; set; }
            public string Unit { get; set; }
            public string Quantity { get; set; }
            public string UnitPrice { get; set; }
            public string Purpose { get; set; }
            public string RequiredDate { get; set; }
            public string CurrentStock { get; set; }
        }


        // ─── Phiếu Nhập Kho ─────────────────────────────────────────────────

        public class WarehouseReceiptPrintData
        {
            public CompanyInfo Company { get; set; } = new CompanyInfo();
            public string NgayIn { get; set; }  // "Ngày 03 tháng 02 năm 2026"
            public string SoPO { get; set; }
            public string SoPhieu { get; set; }  // KNK26/02-0008
            public string NguoiGiao { get; set; }
            public string NhaCungCap { get; set; }
            public string LyDoNhap { get; set; }
            public string KhoHang { get; set; }
            public List<WarehouseReceiptItem> Items { get; set; } = new List<WarehouseReceiptItem>();
            public SignatureInfo Signature { get; set; } = new SignatureInfo();
        }

        public class WarehouseReceiptItem
        {
            public int No { get; set; }
            public string Ten { get; set; }
            public string Ma { get; set; }
            public string DonVi { get; set; }
            public string YeuCau { get; set; }
            public string ThucNhan { get; set; }
            public string GhiChu { get; set; }
        }

        // ─── Phiếu Xuất Kho ─────────────────────────────────────────────────

        public class WarehouseIssuesPrintData
        {
            public CompanyInfo Company { get; set; } = new CompanyInfo();
            public string NgayIn { get; set; }
            public string So { get; set; }       // Số phiếu (Nợ:)
            public string Co { get; set; }       // Có: (mặc định "N156" hoặc để trống)
            public string SoPhieu { get; set; }  // KXK26/02-0001
            public string NguoiNhan { get; set; }
            public string LyDoXuat { get; set; }
            public string XuatTaiKho { get; set; }
            public List<WarehouseIssuesItem> Items { get; set; } = new List<WarehouseIssuesItem>();
            public SignatureInfo Signature { get; set; } = new SignatureInfo();
        }

        public class WarehouseIssuesItem
        {
            public int No { get; set; }
            public string Ten { get; set; }
            public string Ma { get; set; }
            public string DonVi { get; set; }
            public string SoLuong { get; set; }
            public string DonGia { get; set; }   // mặc định ""
            public string ThanhTien { get; set; } // mặc định ""
            public string GhiChu { get; set; }
        }
    }
}