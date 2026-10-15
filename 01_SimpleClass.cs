using System;

namespace TestCases
{
    // ============================================================
    // KỊCH BẢN 1: LỚP ĐƠN (Simple Class)
    // Mục đích: Kiểm tra công cụ nhận diện class cơ bản,
    //           Access Modifiers, Fields, Properties, Methods.
    // ============================================================

    public class NhanVien
    {
        // Fields (private)
        private string hoTen;
        private int tuoi;
        private double luongCoBan;

        // Properties (public)
        public string HoTen
        {
            get { return hoTen; }
            set { hoTen = value; }
        }

        public int Tuoi
        {
            get { return tuoi; }
            set { tuoi = value; }
        }

        public double LuongCoBan
        {
            get { return luongCoBan; }
            set { luongCoBan = value; }
        }

        // Constructor
        public NhanVien(string hoTen, int tuoi, double luongCoBan)
        {
            this.hoTen = hoTen;
            this.tuoi = tuoi;
            this.luongCoBan = luongCoBan;
        }

        // Methods
        public double TinhLuong()
        {
            return luongCoBan;
        }

        public double TinhLuongThucTe(double heSoChucVu)
        {
            return luongCoBan * heSoChucVu;
        }

        public void HienThiThongTin()
        {
            Console.WriteLine("Họ tên: " + hoTen);
            Console.WriteLine("Tuổi: " + tuoi);
        }

        protected void CapNhatLuong(double luongMoi)
        {
            this.luongCoBan = luongMoi;
        }
    }
}
