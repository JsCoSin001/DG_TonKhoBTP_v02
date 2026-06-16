namespace DG_TonKhoBTP_v02.Models.KeToan
{
    /// <summary>
    /// Model ánh xạ bảng BOMStructure trong SQLite.
    /// </summary>
    public class BOMStructure_Model
    {
        public long Id { get; set; }
        public long ParentProduct { get; set; }
        public long Component { get; set; }
        public decimal TyLe { get; set; }
        public decimal TyLeHoanDoi { get; set; }
        public int CongDoan { get; set; }
        public int Active { get; set; }

        /// <summary>Lấy từ DanhSachMaSP.Ma của Component — chỉ dùng để hiển thị.</summary>
        public string Ma { get; set; }

        /// <summary>Lấy từ DanhSachMaSP.Ten của Component — chỉ dùng để hiển thị.</summary>
        public string Ten { get; set; }

        /// <summary>Tên công đoạn — chỉ dùng để hiển thị trên grid.</summary>
        public string TenCongDoan { get; set; }

        /// <summary>Text trạng thái Active — chỉ dùng để hiển thị trên grid.</summary>
        public string ActiveText { get; set; }
    }
}
