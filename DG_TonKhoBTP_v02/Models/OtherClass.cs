using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DG_TonKhoBTP_v02.Models
{
    public class ColumnDefinition
    {
        public string Name { get; set; }          // Tên cột trong DataTable
        public Type DataType { get; set; }        // Kiểu dữ liệu (typeof(int), typeof(double), ...)
        public string Header { get; set; }        // Tên hiển thị
    }

    public class DataTableEventArgs : EventArgs
    {
        public DataTable Data { get; }
        public int KieuEdit { get; }

        public DataTableEventArgs(DataTable data, int kieuEdit)
        {
            Data = data;
            KieuEdit = kieuEdit;
        }
    }


    public class DonKhacInfo
    {
        public string MaDon { get; set; }
        public string NguoiDat { get; set; }
        public string NguoiGiaoNhan { get; set; }
        public string LyDoChung { get; set; }
        public DateTime Ngay { get; set; }
        public int KhoId { get; set; }
        public string NguoiLam { get; set; }
        public bool IsNhapKho { get; set; } = true;
    }

    public class DonKhacItem
    {
        public long? DanhSachMaSpId { get; set; }
        public string TenVatTu { get; set; }
        public string TenVatTuKhongDau { get; set; }
        public decimal SoLuong { get; set; }
        public string MucDichMua { get; set; }
        public string GhiChu { get; set; }
        public decimal? DonGia { get; set; }
    }

    public class CongDoan
    {
        public int Id { get; set; }

        public string TenCongDoan { get; set; }

        public List<string> DanhSachMay { get; set; }

        public List<ColumnDefinition> Columns { get; set; }

        public List<string> ListMa_Accept { get; set; }

        public List<string> ListData_Report { get; set; }


        // Thuộc tính này dùng để lưu trữ giá trị MinReset cho từng công đoạn
        // Nếu nhỏ hơn hoặc bằng giá trị này thì khối lượng và chiều dài còn lại sẽ đc đặt về 0
        public Dictionary<string, decimal> MinReset { get; set; }

        // Constructor để khởi tạo tất cả các thuộc tính
        public CongDoan(int id,string tenCongDoan, List<string> danhSachMay, List<ColumnDefinition> columns, List<string> dsAccept, List<string> dsListData, Dictionary<string, decimal> minReset)
        {
            this.Id = id;
            TenCongDoan = tenCongDoan.ToUpper();
            DanhSachMay = new List<string>(danhSachMay);
            Columns = new List<ColumnDefinition>(columns);
            ListMa_Accept = dsAccept;
            ListData_Report = dsListData;
            MinReset = minReset;
        }

        // Copy constructor để tạo một instance mới từ một instance đã tồn tại
        public CongDoan(CongDoan other)
        {
            Id = other.Id;
            TenCongDoan = other.TenCongDoan;
            DanhSachMay = new List<string>(other.DanhSachMay);
            Columns = new List<ColumnDefinition>(other.Columns);
            ListMa_Accept = other.ListMa_Accept;
            ListData_Report = other.ListData_Report;
            MinReset = other.MinReset;
        }

    }

    public class BanTran
    {
        public string MaBin { get; set; }
        public double KhoiLuongSau { get; set; }
        public double KhoiLuongBanTran { get; set; }
    }

    public class ConfigDB
    {
        public int ID { get; set; } = 1;
        public string Author { get; set; }
        public bool Active { get; set; }
        public string Message { get; set; }
        public string Ngay { get; set; }
    }

    public class Item { public string Ten { get; set; } public string Ma { get; set; } }

    public sealed class DbResult
    {
        public bool Ok { get; set; }
        public string Message { get; set; } = "";
        public long? Id { get; set; }
    }

    public class TachBinModel
    {
        public int ID { get; set; }
        public string NgaySX { get; set; }
        public decimal DanhSachSP_ID { get; set; }
        public string CaSX { get; set; }
        public string Bin { get; set; }
        public decimal KhoiLuong { get; set; }
        public decimal ChieuDai { get; set; }
        public string TenSP { get; set; }
        public string QC { get; set; }
        public string Lot { get; set; }
        public string MaSP { get; set; }
        public string NguoiThucHien { get; set; }
        public string GhiChu { get; set; }
    }





}
