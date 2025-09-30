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

        public CongDoan(int id,string tenCongDoan, List<string> danhSachMay, List<ColumnDefinition> columns)
        {
            this.Id = id;
            TenCongDoan = ("BÁO CÁO CÔNG ĐOẠN " + tenCongDoan).ToUpper();            
            DanhSachMay = new List<string>(danhSachMay);
            Columns = new List<ColumnDefinition>(columns);
        }
    }
}
