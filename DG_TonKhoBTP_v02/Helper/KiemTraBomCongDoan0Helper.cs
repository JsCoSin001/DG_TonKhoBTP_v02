using DG_TonKhoBTP_v02.Models;
using System;
using System.Collections.Generic;

namespace DG_TonKhoBTP_v02.Helper
{
    public enum LoaiBomCongDoan0
    {
        KhongXacDinh = 0,
        KichThuoc8_0mm = 1,
        KichThuoc2_0 = 2
    }

    public static class KiemTraBomCongDoan0Helper
    {
        private const string ChuoiKichThuoc8_0mm = " 8.0mm ";
        private const string ChuoiKichThuoc2_0 = " 2.0";

        public static LoaiBomCongDoan0 XacDinhLoaiBom(
            IEnumerable<BomComponentData> bomComponents)
        {
            if (bomComponents == null)
                return LoaiBomCongDoan0.KhongXacDinh;

            LoaiBomCongDoan0 loaiBom = LoaiBomCongDoan0.KhongXacDinh;
            bool coComponent = false;

            foreach (BomComponentData component in bomComponents)
            {
                if (component == null)
                    return LoaiBomCongDoan0.KhongXacDinh;

                coComponent = true;
                string tenComponent = component.ComponentTen ?? string.Empty;
                bool chua8_0mm = ChuaChuoi(tenComponent, ChuoiKichThuoc8_0mm);
                bool chua2_0 = ChuaChuoi(tenComponent, ChuoiKichThuoc2_0);

                // Mỗi component phải chứa đúng một trong hai chuỗi quy định.
                if (chua8_0mm == chua2_0)
                    return LoaiBomCongDoan0.KhongXacDinh;

                LoaiBomCongDoan0 loaiComponent = chua8_0mm
                    ? LoaiBomCongDoan0.KichThuoc8_0mm
                    : LoaiBomCongDoan0.KichThuoc2_0;

                if (loaiBom == LoaiBomCongDoan0.KhongXacDinh)
                {
                    loaiBom = loaiComponent;
                    continue;
                }

                // BOM không được đồng thời chứa component thuộc cả hai loại.
                if (loaiBom != loaiComponent)
                    return LoaiBomCongDoan0.KhongXacDinh;
            }

            return coComponent
                ? loaiBom
                : LoaiBomCongDoan0.KhongXacDinh;
        }

        public static bool TenNguyenVatLieuPhuHop(
            LoaiBomCongDoan0 loaiBom,
            string tenNguyenVatLieu)
        {
            if (string.IsNullOrEmpty(tenNguyenVatLieu))
                return false;

            switch (loaiBom)
            {
                case LoaiBomCongDoan0.KichThuoc8_0mm:
                    return ChuaChuoi(
                        tenNguyenVatLieu,
                        ChuoiKichThuoc8_0mm);

                case LoaiBomCongDoan0.KichThuoc2_0:
                    return ChuaChuoi(
                        tenNguyenVatLieu,
                        ChuoiKichThuoc2_0);

                default:
                    return false;
            }
        }

        public static bool NguyenVatLieuPhuHop(
            IEnumerable<BomComponentData> bomComponents,
            string tenNguyenVatLieu)
        {
            LoaiBomCongDoan0 loaiBom = XacDinhLoaiBom(bomComponents);
            return TenNguyenVatLieuPhuHop(loaiBom, tenNguyenVatLieu);
        }

        private static bool ChuaChuoi(string source, string value)
        {
            return source.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
