using System;

namespace TestCases
{
    // ============================================================
    // KỊCH BẢN 2: KẾ THỪA ĐA CẤP (Multi-level Inheritance)
    // Mục đích: Kiểm tra công cụ nhận diện chuỗi kế thừa
    //           A → B → C (3 cấp), Access Modifiers khi kế thừa.
    // ============================================================

    // Cấp 1: Lớp cơ sở (Base Class)
    public class DongVat
    {
        protected string ten;
        protected int tuoi;

        public string Ten
        {
            get { return ten; }
            set { ten = value; }
        }

        public int Tuoi
        {
            get { return tuoi; }
            set { tuoi = value; }
        }

        public DongVat(string ten, int tuoi)
        {
            this.ten = ten;
            this.tuoi = tuoi;
        }

        public virtual void An()
        {
            Console.WriteLine(ten + " đang ăn.");
        }

        public virtual void Keu()
        {
            Console.WriteLine(ten + " đang kêu.");
        }
    }

    // Cấp 2: Lớp trung gian (Middle Class) – kế thừa từ DongVat
    public class Cho : DongVat
    {
        private string giong;

        public string Giong
        {
            get { return giong; }
            set { giong = value; }
        }

        public Cho(string ten, int tuoi, string giong)
            : base(ten, tuoi)
        {
            this.giong = giong;
        }

        public override void Keu()
        {
            Console.WriteLine(ten + " sủa: Gâu gâu!");
        }

        public void HienThiGiong()
        {
            Console.WriteLine("Giống: " + giong);
        }
    }

    // Cấp 3: Lớp con (Child Class) – kế thừa từ Cho
    public class ChoCon : Cho
    {
        private bool daHuanLuyen;

        public bool DaHuanLuyen
        {
            get { return daHuanLuyen; }
            set { daHuanLuyen = value; }
        }

        public ChoCon(string ten, int tuoi, string giong, bool daHuanLuyen)
            : base(ten, tuoi, giong)
        {
            this.daHuanLuyen = daHuanLuyen;
        }

        public void HuanLuyen()
        {
            this.daHuanLuyen = true;
            Console.WriteLine(ten + " đã được huấn luyện.");
        }

        public override void Keu()
        {
            Console.WriteLine(ten + " sủa nhỏ: Gâu gâu!");
        }
    }
}
