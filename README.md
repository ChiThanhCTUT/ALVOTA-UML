# Hệ thống Quản lý Thư viện Trường học (Desktop .NET 8 / WPF)

Hệ thống phần mềm quản lý thư viện trường học (THCS / THPT) hoạt động độc lập trên nền tảng Windows Desktop (`.NET 8`, `WPF`, `SQLite`), được thiết kế theo kiến trúc **Clean Architecture + DDD-Lite + CQRS (MediatR)**.

## 1. Kiến trúc hệ thống

- **`src/Library.Domain`**: Thực thể nghiệp vụ (`Book`, `BookCopy`, `Member`, `Loan`, `LoanItem`, `Reservation`, `Fine`, `Payment`, `InventorySession`, `User`, `Role`, `AuditLog`, `Setting`), `Result<T>` pattern và dịch vụ miền (`CirculationPolicyService`, `FineCalculator`).
- **`src/Library.Application`**: Xử lý các ca sử dụng (Use Cases) theo mô hình CQRS (`MediatR`), kiểm tra dữ liệu đầu vào tự động qua `ValidationBehavior` (`FluentValidation`), phân quyền RBAC và ghi nhận nhật ký kiểm toán (`AuditLog`).
- **`src/Library.Infrastructure`**: Tầng lưu trữ dữ liệu `EF Core 8 + SQLite`, cấu hình Schema/Filtered Unique Index, khởi tạo dữ liệu mẫu (`DatabaseInitializer`), băm mật khẩu `PBKDF2-SHA256`, sinh mã vạch `ZXing.Net`, nhập/xuất Excel (`ClosedXML`), xuất báo cáo/biên lai PDF (`QuestPDF`) và sao lưu dữ liệu (`BackupService`).
- **`src/Library.Desktop`**: Ứng dụng giao diện người dùng `WPF` theo mẫu `MVVM` (`CommunityToolkit.Mvvm`), sử dụng `ScopedMediator` để cô lập vòng đời `LibraryDbContext` cho từng thao tác.

## 2. Chức năng chính

1. **Xác thực & Phân quyền (`AUTH-01` → `AUTH-05`)**: Đăng nhập bảo mật với `PBKDF2`, tự động khóa 5 phút khi nhập sai 5 lần liên tiếp, phân quyền 3 vai trò (`Admin`, `Librarian`, `Assistant`).
2. **Quản lý Danh mục Sách (`CAT-01` → `CAT-08`)**: Quản lý đầu sách, tác giả, thể loại, nhà xuất bản, bản sao (`BookCopy`), sinh mã vạch không trùng lặp và tìm kiếm tiếng Việt có dấu.
3. **Quản lý Bạn đọc (`MEM-01` → `MEM-07`)**: Quản lý thẻ học sinh/giáo viên, tra cứu lịch sử mượn trả và nợ phạt.
4. **Lưu thông Mượn – Trả – Đặt trước (`CIR-01` → `CIR-07`)**: Kiểm tra hạn mức mượn, thời hạn mượn, sách tham khảo đọc tại chỗ (`BR-11`), tự động giữ sách đặt trước 3 ngày khi trả (`BR-10`), gia hạn sách trực tuyến.
5. **Quản lý Phạt & Thanh toán (`FIN-01` → `FIN-05`)**: Tự động tính phạt trễ hạn / hư hỏng / mất sách có áp trần giá bìa, thu phạt một phần hoặc toàn phần, miễn giảm tiền phạt có lưu lý do kiểm toán.
6. **Kiểm kê Kho (`INV-01` → `INV-03`)**: Tạo phiên kiểm kê theo kệ, quét mã vạch đối soát và tổng hợp số lượng sách thực tế / mất / hỏng.
7. **Thống kê, Cấu hình & Sao lưu (`RPT`, `SYS`)**: Bảng điều khiển tổng quan thời gian thực, cấu hình tham số nghiệp vụ động (`Settings`), sao lưu SQLite và nhật ký kiểm toán (`AuditLog`).

## 3. Hướng dẫn chạy và kiểm thử

```powershell
# Biên dịch toàn bộ Solution
dotnet build Library.sln

# Chạy toàn bộ Unit Test & Integration Test
dotnet test Library.sln

# Chạy ứng dụng Desktop
dotnet run --project src/Library.Desktop/Library.Desktop.csproj
```

## 4. Tài liệu Kiểm toán & Kiểm thử

- [Báo cáo Kiểm toán Hệ thống (`SYSTEM_AUDIT_REPORT.md`)](./SYSTEM_AUDIT_REPORT.md)
- [Danh sách Ca Kiểm thử (`TEST_CASES.md`)](./TEST_CASES.md)
- [Kế hoạch Khắc phục Lỗi (`FIX_PLAN.md`)](./FIX_PLAN.md)
