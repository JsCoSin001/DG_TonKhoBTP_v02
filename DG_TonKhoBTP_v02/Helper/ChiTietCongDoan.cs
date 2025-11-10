using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Threading.Tasks;

namespace DG_TonKhoBTP_v02.Helper
{
    public static class ChiTietCongDoanBoc
    {
        public static readonly string[] DSTenCotRieng = new string[]
        {
            // 0: Kéo rút
            "ckr.DKTrucX, ckr.DKTrucY, ckr.NgoaiQuan AS KeoRut_NgoaiQuan, ckr.TocDo, ckr.DienApU, ckr.DongDienU",

            // 1: Bện ruột
            "cbr.DKSoi as DKSoi, cbr.SoSoi as BenRuot_SoSoi, cbr.ChieuXoan AS BenRuot_ChieuXoan, cbr.BuocBen as BuocBen ",

            // 2: Ghép lõi - Quấn băng
            "cgl.BuocXoan, cgl.ChieuXoan as GhepLoi_ChieuXoan, cgl.GoiCachMep, cgl.DKBTP",

            // 3: Bọc mạch
            "cbm.NgoaiQuan AS BocMach_NgoaiQuan, cbm.LanDanhThung, cbm.SoMet",

            // 4: Bóc lót
            "cbl.DoDayTBLot",

            // 5: Bóc vỏ
            "cbv.DayVoTB, cbv.InAn"
        };

        public static readonly string DSTenCotChung = @"    
            MangNuoc, PuliDanDay, BoDemMet, MayIn,
            v1, v2, v3, v4, v5, v6,
            Co,  Dau1, Dau2, Khuon, BinhSay, DKKhuon1, DKKhuon2,
            TTNhua, NhuaPhe, GhiChuNhuaPhe, DayPhe, GhiChuDayPhe,
            KTDKLan1, KTDKLan2, KTDKLan3, DiemMongLan1, DiemMongLan2
        ";
    }
}
