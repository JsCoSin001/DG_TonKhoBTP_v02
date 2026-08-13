using DG_TonKhoBTP_v02.Models;
using System;
using System.Collections.Generic;

namespace DG_TonKhoBTP_v02.Helper
{
    public enum LoaiBomCongDoan0
    {
        KhongXacDinh = 0,
        KichThuoc8_0mm = 1,
        KichThuoc2_0 = 2,
        KichThuoc2_6 = 3,
        KichThuoc9_5 = 4
    }

    public static class KiemTraBomCongDoan0Helper
    {
        private const string ChuoiKichThuoc8_0mm = " 8.0mm ";
        private const string ChuoiKichThuoc2_0 = " 2.0";
        private const string ChuoiKichThuoc2_6 = " 2.6";
        private const string ChuoiKichThuoc9_5 = " 9.5";

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

                LoaiBomCongDoan0 loaiComponent = XacDinhLoaiComponent(
                    component.ComponentTen ?? string.Empty);

                // Mỗi component phải xác định được đúng một trong bốn kích thước hỗ trợ.
                if (loaiComponent == LoaiBomCongDoan0.KhongXacDinh)
                    return LoaiBomCongDoan0.KhongXacDinh;

                if (loaiBom == LoaiBomCongDoan0.KhongXacDinh)
                {
                    loaiBom = loaiComponent;
                    continue;
                }

                // Tất cả component trong BOM phải cùng một loại kích thước.
                if (loaiBom != loaiComponent)
                    return LoaiBomCongDoan0.KhongXacDinh;
            }

            return coComponent
                ? loaiBom
                : LoaiBomCongDoan0.KhongXacDinh;
        }

        private static LoaiBomCongDoan0 XacDinhLoaiComponent(string tenComponent)
        {
            bool chua8_0mm = ChuaChuoi(tenComponent, ChuoiKichThuoc8_0mm);
            bool chua2_0 = ChuaChuoi(tenComponent, ChuoiKichThuoc2_0);
            bool chua2_6 = ChuaChuoi(tenComponent, ChuoiKichThuoc2_6);
            bool chua9_5 = ChuaChuoi(tenComponent, ChuoiKichThuoc9_5);

            int soLoaiPhuHop = 0;
            if (chua8_0mm) soLoaiPhuHop++;
            if (chua2_0) soLoaiPhuHop++;
            if (chua2_6) soLoaiPhuHop++;
            if (chua9_5) soLoaiPhuHop++;

            if (soLoaiPhuHop != 1)
                return LoaiBomCongDoan0.KhongXacDinh;

            if (chua8_0mm)
                return LoaiBomCongDoan0.KichThuoc8_0mm;

            if (chua2_0)
                return LoaiBomCongDoan0.KichThuoc2_0;

            if (chua2_6)
                return LoaiBomCongDoan0.KichThuoc2_6;

            return LoaiBomCongDoan0.KichThuoc9_5;
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

                case LoaiBomCongDoan0.KichThuoc2_6:
                    return ChuaChuoi(
                        tenNguyenVatLieu,
                        ChuoiKichThuoc2_6);

                case LoaiBomCongDoan0.KichThuoc9_5:
                    return ChuaChuoi(
                        tenNguyenVatLieu,
                        ChuoiKichThuoc9_5);

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
