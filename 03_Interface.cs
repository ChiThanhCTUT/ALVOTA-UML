using System;

namespace TestCases
{
    // ============================================================
    // KỊCH BẢN 3: INTERFACE
    // Mục đích: Kiểm tra công cụ nhận diện Interface,
    //           class cài đặt interface, quan hệ realizes.
    // ============================================================

    // Interface
    public interface IHinhHoc
    {
        double TinhChuVi();
        double TinhDienTich();
        void HienThi();
    }

    // Interface thứ hai (đa hình thức)
    public interface IToMau
    {
        string MauSac { get; set; }
        void ToMau();
    }

    // Class cài đặt 1 interface
    public class HinhTron : IHinhHoc
    {
        private double banKinh;

        public double BanKinh
        {
            get { return banKinh; }
            set { banKinh = value; }
        }

        public HinhTron(double banKinh)
        {
            this.banKinh = banKinh;
        }

        public double TinhChuVi()
        {
            return 2 * Math.PI * banKinh;
        }

        public double TinhDienTich()
        {
            return Math.PI * banKinh * banKinh;
        }

        public void HienThi()
        {
            Console.WriteLine("Hình tròn bán kính: " + banKinh);
        }
    }

    // Class cài đặt 2 interface cùng lúc
    public class HinhChuNhat : IHinhHoc, IToMau
    {
        private double chieuDai;
        private double chieuRong;
        private string mauSac;

        public string MauSac
        {
            get { return mauSac; }
            set { mauSac = value; }
        }

        public HinhChuNhat(double chieuDai, double chieuRong, string mauSac)
        {
            this.chieuDai = chieuDai;
            this.chieuRong = chieuRong;
            this.mauSac = mauSac;
        }

        public double TinhChuVi()
        {
            return 2 * (chieuDai + chieuRong);
        }

        public double TinhDienTich()
        {
            return chieuDai * chieuRong;
        }

        public void HienThi()
        {
            Console.WriteLine("Hình chữ nhật " + chieuDai + "x" + chieuRong);
        }

        public void ToMau()
        {
            Console.WriteLine("Tô màu: " + mauSac);
        }
    }
}
