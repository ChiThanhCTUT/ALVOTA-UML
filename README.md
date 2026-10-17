# T4.1 — Bộ Mã Nguồn C# Mẫu Chuẩn Test Case

## Mục đích
Bộ file `.cs` này dùng làm **đầu vào thử nghiệm** cho công cụ dịch ngược C# → UML Class Diagram mà nhóm đang xây dựng.

---

## Danh sách file test case

| File | Kịch bản | Kiểm tra khả năng nhận diện |
|---|---|---|
| `01_SimpleClass.cs` | Lớp đơn | Class, Fields, Properties, Methods, Access Modifiers |
| `02_MultiLevelInheritance.cs` | Kế thừa đa cấp (3 cấp) | Chuỗi kế thừa A→B→C, `virtual`/`override` |
| `03_Interface.cs` | Interface | Interface, class cài đặt 1 & 2 interface, quan hệ realizes |
| `04_GenericCollection.cs` | Generic List & Dictionary | Thuộc tính kiểu `List<T>`, `Dictionary<K,V>` |
| `05_NestedClass.cs` | Nested Class | Class lồng trong class (cấp 1 & cấp 2), access modifier |

---

## Cách sử dụng
1. Copy toàn bộ file `.cs` vào thư mục test của công cụ.
2. Chạy công cụ trên từng file.
3. So sánh sơ đồ UML sinh ra với **kết quả mong đợi** bên dưới.

---

## Kết quả mong đợi (Expected UML)

### 01_SimpleClass.cs
```
┌─────────────────────────┐
│        NhanVien         │
├─────────────────────────┤
│ - hoTen: string         │
│ - tuoi: int             │
│ - luongCoBan: double    │
├─────────────────────────┤
│ + HoTen: string         │
│ + Tuoi: int             │
│ + LuongCoBan: double    │
│ + NhanVien(string,int,double)│
│ + TinhLuong(): double   │
│ + TinhLuongThucTe(double): double│
│ + HienThiThongTin(): void│
│ # CapNhatLuong(double): void│
└─────────────────────────┘
```

### 02_MultiLevelInheritance.cs
```
DongVat ◄──| Cho ◄──| ChoCon
(virtual An, Keu)    (override Keu)    (override Keu, HuanLuyen)
```

### 03_Interface.cs
```
IHinhHoc ◄··· HinhTron
IHinhHoc ◄··· HinhChuNhat
IToMau   ◄··· HinhChuNhat
```

### 04_GenericCollection.cs
```
QuanLySach ──> List<string>
QuanLySach ──> Dictionary<int,string>
QuanLySach ──> List<TacGia>
```

### 05_NestedClass.cs
```
CongTy
  └── PhongBan (nested)
        └── NhanVienPhongBan (nested cấp 2)
  └── ThuKy (nested private)
```
