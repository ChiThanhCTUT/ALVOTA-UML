using System;
using System.Collections.Generic;

namespace TestCases
{
    // ============================================================
    // KỊCH BẢN 4: GENERIC LIST & DICTIONARY
    // Mục đích: Kiểm tra công cụ nhận diện thuộc tính kiểu
    //           Collection (List<T>, Dictionary<K,V>).
    // ============================================================

    public class QuanLySach
    {
        // List<T> – danh sách các object
        private List<string> danhSachTenSach;

        // Dictionary<K,V> – map
        private Dictionary<int, string> maSachVaTen;

        // List<object> – danh sách các object kiểu khác
        private List<TacGia> danhSachTacGia;

        // Properties
        public List<string> DanhSachTenSach
        {
            get { return danhSachTenSach; }
            set { danhSachTenSach = value; }
        }

        public Dictionary<int, string> MaSachVaTen
        {
            get { return maSachVaTen; }
            set { maSachVaTen = value; }
        }

        public List<TacGia> DanhSachTacGia
        {
            get { return danhSachTacGia; }
            set { danhSachTacGia = value; }
        }

        // Constructor
        public QuanLySach()
        {
            danhSachTenSach = new List<string>();
            maSachVaTen = new Dictionary<int, string>();
            danhSachTacGia = new List<TacGia>();
        }

        // Methods
        public void ThemSach(string tenSach, int ma)
        {
            danhSachTenSach.Add(tenSach);
            maSachVaTen.Add(ma, tenSach);
        }

        public void ThemTacGia(TacGia tg)
        {
            danhSachTacGia.Add(tg);
        }

        public void HienThiTatCa()
        {
            Console.WriteLine("=== Danh sách sách ===");
            foreach (var sach in danhSachTenSach)
            {
                Console.WriteLine(sach);
            }
        }
    }

    // Class phụ dùng trong Generic List
    public class TacGia
    {
        private string hoTen;
        private List<string> cacQuyenSach;

        public string HoTen
        {
            get { return hoTen; }
            set { hoTen = value; }
        }

        public List<string> CacQuyenSach
        {
            get { return cacQuyenSach; }
            set { cacQuyenSach = value; }
        }

        public TacGia(string hoTen)
        {
            this.hoTen = hoTen;
            this.cacQuyenSach = new List<string>();
        }

        public void ThemQuyenSach(string tenSach)
        {
            cacQuyenSach.Add(tenSach);
        }
    }
}
