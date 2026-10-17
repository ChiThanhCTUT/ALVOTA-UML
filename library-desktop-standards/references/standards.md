# Chi tiết chuẩn code

## Mục lục
1. Cấu trúc solution
2. Đặt tên
3. Result và mã lỗi
4. EF Core
5. WPF MVVM
6. Bảo mật và log
7. Kiểm thử

## 1. Cấu trúc solution

```
Library.sln
├── src/
│   ├── Library.Domain          # Entity, Value Object, Domain Service
│   ├── Library.Application     # Command/Query, Handler, DTO, Validator, Interface
│   ├── Library.Infrastructure  # EF Core, Repository, Export PDF/Excel, Backup, Log
│   └── Library.Desktop         # WPF: View, ViewModel, Dialog, Resource
└── tests/
    ├── Library.Domain.Tests
    ├── Library.Application.Tests
    └── Library.Infrastructure.Tests
```

Tổ chức theo tính năng: `Application/Features/<Module>/Commands|Queries/<UseCase>/` gồm `Command`, `Handler`, `Validator`.

`Directory.Build.props`: `Nullable=enable`, `ImplicitUsings=enable`, `TreatWarningsAsErrors=true`, `EnforceCodeStyleInBuild=true`.

## 2. Đặt tên

| Đối tượng | Quy ước | Ví dụ |
|---|---|---|
| Class, method, property | PascalCase | `BookCopy`, `CalculateFine` |
| Interface | `I` + PascalCase | `IBookRepository` |
| Tham số, biến | camelCase | `memberId` |
| Field private | `_camelCase` | `_dbContext` |
| Hằng số | PascalCase | `MaxRenewalCount` |
| Async | hậu tố `Async` | `BorrowAsync` |
| Bool | `Is/Has/Can` | `IsOverdue` |
| Command/Query | `<Verb><Noun>Command/Query` | `BorrowBookCommand`, `GetBookByIdQuery` |
| ViewModel/View | `<Name>ViewModel` / `<Name>View` | `LoanViewModel` |
| Test | `Method_Scenario_ExpectedResult` | `Borrow_MemberHasOverdue_ReturnsHasOverdueError` |

## 3. Result và mã lỗi

Định dạng `MODULE.ERROR`. Mã mẫu: `CIR.MEMBER_BLOCKED`, `CIR.LIMIT_REACHED`, `CIR.HAS_OVERDUE`, `CIR.COPY_UNAVAILABLE`, `CIR.RENEW_NOT_ALLOWED`, `FIN.DEBT_EXCEEDED`, `CAT.DUPLICATE_BARCODE`, `AUTH.ACCOUNT_LOCKED`.

Lỗi nghiệp vụ: `return Result.Failure<T>(Error.X)`. Exception chỉ cho lỗi bất thường. Bắt exception ở biên (ViewModel/lệnh), ghi log, hiện thông báo thân thiện qua `IDialogService`.

## 4. EF Core

- Cấu hình bằng `IEntityTypeConfiguration<T>`, không trộn data annotation.
- Global query filter cho `IsDeleted`.
- `RowVersion` là `byte[]` đánh dấu `IsRowVersion()`; bắt `DbUpdateConcurrencyException` và trả lỗi xung đột thân thiện.
- Migration đặt tên mô tả (`AddReservationTable`); không sửa migration đã merge.
- Ràng buộc duy nhất: `BookCopy.Barcode`, `Member.CardNumber`, `Book.Isbn` (khi có giá trị).
- Một `BookCopy` chỉ có tối đa một `LoanItem` chưa trả tại một thời điểm.

## 5. WPF MVVM

- `[ObservableProperty]`, `[RelayCommand]` của CommunityToolkit.Mvvm; command bất đồng bộ nhận `CancellationToken`.
- Dịch vụ giao diện qua interface: `IDialogService`, `INavigationService`.
- Màn hình Mượn/Trả: ô nhập luôn giữ focus, Enter xác nhận, phát âm báo thành công/lỗi.
- Giải phóng tài nguyên: hủy đăng ký event, `IDisposable` khi cần.
- Chuỗi giao diện trong `.resx`; style lấy từ ResourceDictionary.

## 6. Bảo mật và log

- Bí mật dùng DPAPI/User Secrets; chuỗi kết nối được mã hóa.
- Serilog ghi file xoay vòng theo ngày, giữ tối đa 30 ngày; structured logging (`"Loan {LoanId} created"`), không nối chuỗi.
- Không log họ tên, SĐT, ngày sinh, mật khẩu, token.
- Audit log: ai, lúc nào, làm gì, đối tượng nào.

## 7. Kiểm thử

- xUnit, FluentAssertions, Moq/NSubstitute; mô hình Arrange – Act – Assert.
- Domain/Application phủ ≥ 80%; mọi quy tắc `BR-xx` có test.
- Integration test dùng EF Core SQLite in-memory cho luồng mượn → trả → phạt; có test đồng thời (hai máy cùng mượn một bản sao).
- Thời gian dùng `FakeTimeProvider`.
