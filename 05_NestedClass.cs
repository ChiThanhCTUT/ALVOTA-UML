using System;

namespace TestCases
{
    // ============================================================
    // KỊCH BẢN 5: NESTED CLASS (Class lồng trong class)
    // Mục đích: Kiểm tra công cụ nhận diện class lồng nhau,
    //           access modifier của nested class.
    // ============================================================

    public class CongTy
    {
        private string tenCongTy;
        private int namThanhLap;

        public string TenCongTy
        {
            get { return tenCongTy; }
            set { tenCongTy = value; }
        }

        public int NamThanhLap
        {
            get { return namThanhLap; }
            set { namThanhLap = value; }
        }

        public CongTy(string tenCongTy, int namThanhLap)
        {
            this.tenCongTy = tenCongTy;
            this.namThanhLap = namThanhLap;
        }

        // Nested Class (public)
        public class PhongBan
        {
            private string tenPhong;
            private int soNhanVien;

            public string TenPhong
            {
                get { return tenPhong; }
                set { tenPhong = value; }
            }

            public int SoNhanVien
            {
                get { return soNhanVien; }
                set { soNhanVien = value; }
            }

            public PhongBan(string tenPhong, int soNhanVien)
            {
                this.tenPhong = tenPhong;
                this.soNhanVien = soNhanVien;
            }

            public void HienThiThongTin()
            {
                Console.WriteLine("Phòng: " + tenPhong + " - Số NV: " + soNhanVien);
            }

            // Nested Class lồng cấp 2
            public class NhanVienPhongBan
            {
                private string hoTen;
                private string chucVu;

                public string HoTen
                {
                    get { return hoTen; }
                    set { hoTen = value; }
                }

                public string ChucVu
                {
                    get { return chucVu; }
                    set { chucVu = value; }
                }

                public NhanVienPhongBan(string hoTen, string chucVu)
                {
                    this.hoTen = hoTen;
                    this.chucVu = chucVu;
                }
            }
        }

        // Nested Class (private)
        private class ThuKy
        {
            private string hoTen;

            public string HoTen
            {
                get { return hoTen; }
                set { hoTen = value; }
            }

            public ThuKy(string hoTen)
            {
                this.hoTen = hoTen;
            }

            public void PhanCong()
            {
                Console.WriteLine("Thư ký " + hoTen + " đang phân công.");
            }
        }

        // Method sử dụng nested class private
        public void TaoThuKy(string hoTen)
        {
            ThuKy tk = new ThuKy(hoTen);
            tk.PhanCong();
        }
    }
}
