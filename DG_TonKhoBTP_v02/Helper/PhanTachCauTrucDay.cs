using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace DG_TonKhoBTP_v02.Helper
{
    internal class PhanTachCauTrucDay
    {
        /// <summary>
        /// Phân tích tên sản phẩm thành các thành phần cấu trúc.
        ///
        /// Cấu trúc chung:
        /// [Thông tin đầu]? [Cụm 1](+[Cụm 2])*(-[Thông tin phụ])*
        ///
        /// Mỗi cụm:
        /// [Thành phần]([xX/][Thành phần])*
        /// </summary>
        public static KetQuaPhanTich PhanTich(string ten)
        {
            if (ten == null)
            {
                throw new ArgumentNullException(
                    nameof(ten),
                    "Tên sản phẩm không được là null.");
            }

            string tenGoc = ten;

            string tenDaChuanHoa = Regex.Replace(
                ten.Trim(),
                @"\s+",
                " ");

            if (string.IsNullOrWhiteSpace(tenDaChuanHoa))
            {
                throw new ArgumentException(
                    "Tên sản phẩm không được để trống.",
                    nameof(ten));
            }

            string thongTinDau = null;
            string phanConLai;

            int viTriKhoangTrangDauTien =
                tenDaChuanHoa.IndexOf(' ');

            if (viTriKhoangTrangDauTien >= 0)
            {
                thongTinDau = tenDaChuanHoa
                    .Substring(0, viTriKhoangTrangDauTien)
                    .Trim();

                phanConLai = tenDaChuanHoa
                    .Substring(viTriKhoangTrangDauTien + 1)
                    .Trim();

                if (string.IsNullOrEmpty(thongTinDau))
                {
                    thongTinDau = null;
                }
            }
            else
            {
                phanConLai = tenDaChuanHoa;
            }

            KetQuaTach ketQuaTachDauTru =
                TachNgoaiNgoac(
                    phanConLai,
                    new HashSet<char> { '-' });

            string phanChinh =
                ketQuaTachDauTru.CacPhan.FirstOrDefault()
                ?? string.Empty;

            List<string> danhSachThongTinPhu =
                ketQuaTachDauTru.CacPhan
                    .Skip(1)
                    .Where(phan =>
                        !string.IsNullOrWhiteSpace(phan))
                    .ToList();

            KetQuaTach ketQuaTachDauCong =
                TachNgoaiNgoac(
                    phanChinh,
                    new HashSet<char> { '+' });

            List<string> noiDungCacCum =
                ketQuaTachDauCong.CacPhan
                    .Where(phan =>
                        !string.IsNullOrWhiteSpace(phan))
                    .ToList();

            List<CumCauTruc> danhSachCum =
                noiDungCacCum
                    .Select(PhanTichCum)
                    .ToList();

            int soDauCong = Math.Max(
                noiDungCacCum.Count - 1,
                0);

            int soDauX = phanChinh.Count(
                kyTu => kyTu == 'x' || kyTu == 'X');

            int soDauGachCheo = phanChinh.Count(
                kyTu => kyTu == '/');

            string maDang;
            string moTaDang;

            if (thongTinDau == null)
            {
                maDang = "D1";
                moTaDang = "Không có thông tin đầu";
            }
            else if (soDauCong > 0)
            {
                maDang = "D2";
                moTaDang = "Nhiều cụm nối bằng dấu +";
            }
            else if (soDauX >= 2)
            {
                maDang = "D3";
                moTaDang = "Một cụm có nhiều dấu x hoặc X";
            }
            else if (soDauX == 1)
            {
                maDang = "D4";
                moTaDang = "Một cụm có đúng một dấu x hoặc X";
            }
            else if (soDauGachCheo > 0)
            {
                maDang = "D5";
                moTaDang = "Cụm có dấu /";
            }
            else
            {
                maDang = "D6";
                moTaDang =
                    "Cụm đơn, không có dấu x, X, + hoặc /";
            }

            return new KetQuaPhanTich
            {
                TenGoc = tenGoc,
                TenDaChuanHoa = tenDaChuanHoa,
                MaDang = maDang,
                MoTaDang = moTaDang,
                ThongTinDau = thongTinDau,
                PhanChinh = phanChinh,
                DanhSachCum = danhSachCum,
                DanhSachThongTinPhu = danhSachThongTinPhu,
                ThongKe = new ThongKeCauTruc
                {
                    SoCum = danhSachCum.Count,
                    SoDauCong = soDauCong,
                    SoDauX = soDauX,
                    SoDauGachCheo = soDauGachCheo,
                    SoThongTinPhu =
                        danhSachThongTinPhu.Count
                }
            };
        }

        private static CumCauTruc PhanTichCum(
            string noiDungCum)
        {
            KetQuaTach ketQuaTach =
                TachNgoaiNgoac(
                    noiDungCum,
                    new HashSet<char>
                    {
                        'x',
                        'X',
                        '/'
                    });

            List<ThanhPhanTrongCum> danhSachMuc =
                new List<ThanhPhanTrongCum>();

            for (
                int viTri = 0;
                viTri < ketQuaTach.CacPhan.Count;
                viTri++)
            {
                danhSachMuc.Add(
                    new ThanhPhanTrongCum
                    {
                        GiaTri =
                            ketQuaTach.CacPhan[viTri],

                        DauPhiaTruoc =
                            viTri == 0
                                ? null
                                : ketQuaTach
                                    .CacDauPhanCach[
                                        viTri - 1]
                                    .ToString()
                    });
            }

            return new CumCauTruc
            {
                NoiDungGoc = noiDungCum,
                CacThanhPhan =
                    ketQuaTach.CacPhan,
                CacDauPhanCach =
                    ketQuaTach.CacDauPhanCach
                        .Select(kyTu => kyTu.ToString())
                        .ToList(),
                DanhSachMuc = danhSachMuc
            };
        }

        private static KetQuaTach TachNgoaiNgoac(
            string noiDung,
            HashSet<char> cacDauPhanCach)
        {
            List<string> cacPhan =
                new List<string>();

            List<char> danhSachDau =
                new List<char>();

            StringBuilder boDem =
                new StringBuilder();

            int capDoNgoac = 0;

            foreach (char kyTu in noiDung)
            {
                if (kyTu == '(')
                {
                    capDoNgoac++;
                }
                else if (kyTu == ')' && capDoNgoac > 0)
                {
                    capDoNgoac--;
                }

                bool laDauCanTach =
                    capDoNgoac == 0
                    && cacDauPhanCach.Contains(kyTu);

                if (laDauCanTach)
                {
                    cacPhan.Add(
                        boDem.ToString().Trim());

                    danhSachDau.Add(kyTu);

                    boDem.Clear();
                }
                else
                {
                    boDem.Append(kyTu);
                }
            }

            cacPhan.Add(
                boDem.ToString().Trim());

            return new KetQuaTach
            {
                CacPhan = cacPhan,
                CacDauPhanCach = danhSachDau
            };
        }

        private sealed class KetQuaTach
        {
            public List<string> CacPhan { get; set; }

            public List<char> CacDauPhanCach { get; set; }

            public KetQuaTach()
            {
                CacPhan = new List<string>();
                CacDauPhanCach = new List<char>();
            }
        }
    }

    public sealed class KetQuaPhanTich
    {
        public string TenGoc { get; set; }

        public string TenDaChuanHoa { get; set; }

        public string MaDang { get; set; }

        public string MoTaDang { get; set; }

        public string ThongTinDau { get; set; }

        public string PhanChinh { get; set; }

        public List<CumCauTruc> DanhSachCum { get; set; }

        public List<string> DanhSachThongTinPhu { get; set; }

        public ThongKeCauTruc ThongKe { get; set; }

        public KetQuaPhanTich()
        {
            TenGoc = string.Empty;
            TenDaChuanHoa = string.Empty;
            MaDang = string.Empty;
            MoTaDang = string.Empty;
            ThongTinDau = null;
            PhanChinh = string.Empty;

            DanhSachCum =
                new List<CumCauTruc>();

            DanhSachThongTinPhu =
                new List<string>();

            ThongKe =
                new ThongKeCauTruc();
        }
    }

    public sealed class CumCauTruc
    {
        public string NoiDungGoc { get; set; }

        public List<string> CacThanhPhan { get; set; }

        public List<string> CacDauPhanCach { get; set; }

        public List<ThanhPhanTrongCum> DanhSachMuc { get; set; }

        public CumCauTruc()
        {
            NoiDungGoc = string.Empty;

            CacThanhPhan =
                new List<string>();

            CacDauPhanCach =
                new List<string>();

            DanhSachMuc =
                new List<ThanhPhanTrongCum>();
        }
    }

    public sealed class ThanhPhanTrongCum
    {
        public string GiaTri { get; set; }

        public string DauPhiaTruoc { get; set; }

        public ThanhPhanTrongCum()
        {
            GiaTri = string.Empty;
            DauPhiaTruoc = null;
        }
    }

    public sealed class ThongKeCauTruc
    {
        public int SoCum { get; set; }

        public int SoDauCong { get; set; }

        public int SoDauX { get; set; }

        public int SoDauGachCheo { get; set; }

        public int SoThongTinPhu { get; set; }
    }
}
