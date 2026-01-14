using System;
using System.Collections.Generic;
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

    public class CongDoan
    {
        public int Id { get; set; }

        public string TenCongDoan { get; set; }

        public List<string> DanhSachMay { get; set; }

        public List<ColumnDefinition> Columns { get; set; }

        public List<string> ListMa_Accept { get; set; }

        public List<string> ListData_Report { get; set; }

        public CongDoan(int id,string tenCongDoan, List<string> danhSachMay, List<ColumnDefinition> columns, List<string> dsAccept, List<string> dsListData)
        {
            this.Id = id;
            TenCongDoan = tenCongDoan.ToUpper();
            DanhSachMay = new List<string>(danhSachMay);
            Columns = new List<ColumnDefinition>(columns);
            ListMa_Accept = dsAccept;
            ListData_Report = dsListData;
        }

        public CongDoan(CongDoan other)
        {
            Id = other.Id;
            TenCongDoan = other.TenCongDoan;
            DanhSachMay = new List<string>(other.DanhSachMay);
            Columns = new List<ColumnDefinition>(other.Columns);
            ListMa_Accept = other.ListMa_Accept;
            ListData_Report = other.ListData_Report;
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
        public string Lot { get; set; }
        public string Bin { get; set; }
        public string DanhSachSP_ID { get; set; }
        public string qc { get; set; }
        public decimal KhoiLuongSau { get; set; }
        public decimal ChieuDaiSau { get; set; }
    }
    



}
