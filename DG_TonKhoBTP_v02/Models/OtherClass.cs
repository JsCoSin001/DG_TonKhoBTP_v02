﻿using System;
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

        public CongDoan(int id,string tenCongDoan, List<string> danhSachMay, List<ColumnDefinition> columns, List<string> dsAccept)
        {
            this.Id = id;
            TenCongDoan = tenCongDoan.ToUpper();            
            DanhSachMay = new List<string>(danhSachMay);
            Columns = new List<ColumnDefinition>(columns);
            ListMa_Accept = dsAccept;
        }

        public CongDoan(CongDoan other)
        {
            Id = other.Id;
            TenCongDoan = other.TenCongDoan;
            DanhSachMay = new List<string>(other.DanhSachMay);
            Columns = new List<ColumnDefinition>(other.Columns);
            ListMa_Accept = other.ListMa_Accept; 
        }

    }

    public class BanTran
    {
        public string MaBin { get; set; }
        public double KhoiLuongSau { get; set; }
        public double KhoiLuongBanTran { get; set; }
    }



}
