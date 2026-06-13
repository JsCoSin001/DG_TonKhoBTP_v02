using System;

namespace DG_TonKhoBTP_v02.Models.KeToan
{
    public class KiemTraDuLieuSXNhap_Model
    {
        public int id_KhacBietBom { get; set; }
        public int ttthanhpham_id { get; set; }

        // ID công đoạn lấy trực tiếp từ database, dùng để đổi sang tên công đoạn ở UI.
        public int? CongDoanTP_ID { get; set; }
        public int? CongDoanTTe_ID { get; set; }

        // Dùng để phân biệt:
        // - CongDoanTTe_ID == null từ database: hiển thị N.A nhưng không tô vàng.
        // - Có ID nhưng không tìm thấy trong danh sách công đoạn: hiển thị N.A và tô vàng.
        public bool IsCongDoanTTe_NullFromDB { get; set; }

        // Các property bên dưới đặt tên đúng theo DataPropertyName trong DataGridView Designer.
        public string congDoanTP { get; set; }
        public string lotTP { get; set; }
        public string maTP { get; set; }
        public string tenTP { get; set; }

        public string congDoanTTe { get; set; }
        public string lotNVL { get; set; }
        public string maNVL { get; set; }
        public string tenNVL { get; set; }

        public string confirm { get; set; } = "Xác nhận";

        // Dùng để tô vàng ô công đoạn nếu có ID nhưng không tìm thấy trong ThongTinChungCongDoan.TatCaCongDoan.
        public bool IsCongDoanTP_NA { get; set; }
        public bool IsCongDoanTTe_NA { get; set; }
    }
}