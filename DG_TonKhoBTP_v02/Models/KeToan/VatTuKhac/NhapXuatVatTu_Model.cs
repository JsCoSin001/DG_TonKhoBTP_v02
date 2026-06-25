using System;
using System.Collections.Generic;

namespace DG_TonKhoBTP_v02.Models.KeToan.VatTuKhac
{
    public class KieuNhapXuat_Model
    {
        public int Id { get; set; }
        public string Ten { get; set; }
        public bool IsNhap { get; set; }
        public bool IsDichVu { get; set; }
        public bool IsKhac { get; set; }
    }

    public static class TenKieuNhapXuat
    {
        public const string NHAP_KHO_THEO_DON = "NHẬP KHO THEO ĐƠN ĐỀ NGHỊ";
        public const string NHAP_KHO_KHAC = "NHẬP KHÁC";
        public const string XUAT_THEO_DON = "XUẤT THEO ĐƠN ĐỀ NGHỊ";
        public const string XUAT_KHAC = "XUẤT KHÁC";
        public const string DICH_VU = "XÁC NHẬN DỊCH VỤ";
    }
}
