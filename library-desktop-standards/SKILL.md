---
name: library-desktop-standards
description: Chuẩn code C#/.NET 8 và quy trình làm việc cho dự án "Quản lý thư viện trường học" bản Desktop (WPF MVVM, Clean Architecture, MediatR, EF Core, SQLite/SQL Server, không có web/API). Dùng skill này mỗi khi người dùng viết, sinh, sửa, refactor hoặc review code C# của dự án thư viện; tạo entity, EF configuration, migration, command/query/handler, validator, ViewModel, View XAML, unit test; soạn commit message, mô tả Pull Request, checklist review; hỏi về chuẩn code, đặt tên, kiến trúc, phân công nhóm, bàn giao, CI hoặc phát hành. Kích hoạt ngay cả khi người dùng chỉ nói "viết hàm mượn sách", "tính phạt trễ hạn", "review giúp đoạn code này", "tạo màn hình mượn trả", "đặt tên cho đúng chuẩn", "PR này ổn chưa", dù không nhắc tên skill. Use for any C#/WPF code generation, review, scaffolding or Git/PR task in this desktop school-library project.
---

# Chuẩn code dự án Quản lý thư viện (Desktop, C# / .NET 8)

Skill này biến chuẩn code của dự án thành hành vi cụ thể khi viết và review code. Nhiều nhóm làm song song, nên ranh giới tầng, quy ước đặt tên và cách xử lý lỗi phải cố định; nếu không, chi phí ghép nối tăng nhanh. Hiểu lý do giúp xử lý cả tình huống chuẩn chưa nói tới.

Phạm vi: ứng dụng **desktop WPF**, chạy offline, không có web hay API. Nếu người dùng yêu cầu Blazor/ASP.NET, nói rõ ngoài phạm vi skill này và hỏi có muốn mở rộng không.

## Bước 1 – Xác định loại tác vụ

| Người dùng muốn | Làm gì |
|---|---|
| Viết code mới | Làm theo "Quy trình viết code" |
| Review/sửa code có sẵn | Làm theo "Quy trình review", xuất báo cáo đúng khung |
| Tạo khung (entity, handler, ViewModel, test) | Mở `assets/templates/` lấy mẫu rồi điều chỉnh |
| Chi tiết tầng, đặt tên, EF Core, WPF, bảo mật | Đọc `references/standards.md` |
| Commit, PR, nhánh, CI, DoD, phát hành, phân công | Đọc `references/workflow.md` |

Nếu thiếu thông tin quyết định thiết kế (module nào, quy tắc nghiệp vụ nào), hỏi **một** câu ngắn. Nếu suy ra được từ ngữ cảnh thì nêu giả định rồi làm luôn.

## Bước 2 – Quy trình viết code

1. **Xác định tầng**: Domain / Application / Infrastructure / Desktop.
2. **Gắn yêu cầu**: ghi mã SRS (`CIR-02`, `BR-05`…) vào comment hoặc mô tả PR khi hiện thực quy tắc nghiệp vụ.
3. **Viết code** theo quy tắc cốt lõi bên dưới, dùng mẫu trong `assets/templates/` làm điểm xuất phát.
4. **Viết kèm test** (xUnit + FluentAssertions, tên `Method_Scenario_ExpectedResult`). Quy tắc nghiệp vụ cần test; sửa lỗi phải có test tái hiện lỗi.
5. **Tự kiểm tra** bằng checklist review trước khi trả kết quả.
6. **Ngoại lệ**: nếu bắt buộc lệch chuẩn, đánh dấu `// STANDARD-EXCEPTION(ADR-xx)` và nhắc người dùng cần viết ADR.

## Quy tắc cốt lõi

**Kiến trúc**
- Phụ thuộc chỉ đi vào trong: `Desktop → Application → Domain`; `Infrastructure → Application`. Domain không tham chiếu EF Core hay WPF.
- ViewModel không truy cập `DbContext`; chỉ gửi Command/Query qua `IMediator`.
- CQRS: Command (ghi) và Query (đọc) tách riêng, mỗi use case một handler, kiểm tra đầu vào bằng FluentValidation.
- Không trả Entity ra ngoài Application; dùng DTO.

**C#**
- `Nullable` bật, file-scoped namespace, `TreatWarningsAsErrors`.
- Đặt tên tiếng Anh: PascalCase (kiểu, method, property), camelCase (tham số, biến), `_camelCase` (field private), `I`+PascalCase (interface), hậu tố `Async`, bool bắt đầu `Is/Has/Can`.
- Method ≤ 40 dòng, ≤ 4 tham số, độ phức tạp ≤ 10; class ≤ 400 dòng.
- DI qua constructor; cấm Service Locator và `static` giữ trạng thái.
- Cấm `async void` (trừ event handler), cấm `.Result`/`.Wait()`; luôn truyền `CancellationToken`.
- Không số/chuỗi "ma thuật"; chuỗi giao diện đặt trong `.resx`.
- Dùng `TimeProvider`, không gọi `DateTime.Now/UtcNow` trong nghiệp vụ (để test được).

**Lỗi**
- Lỗi nghiệp vụ dự kiến trả `Result<T>`/`Error` có mã `MODULE.ERROR` (ví dụ `CIR.MEMBER_BLOCKED`), không ném exception.
- Exception chỉ cho lỗi bất thường; không nuốt exception; rethrow bằng `throw;`.

**Dữ liệu (EF Core)**
- Code First, cấu hình bằng `IEntityTypeConfiguration<T>`; mọi bảng có `Id, CreatedAtUtc, CreatedBy, UpdatedAtUtc, UpdatedBy, IsDeleted, RowVersion`; giờ lưu UTC; xóa mềm.
- Truy vấn chỉ đọc dùng `AsNoTracking()`; tránh N+1; danh sách luôn phân trang.
- Mượn/trả/phạt nằm trong **một transaction**, dùng `RowVersion` chống xung đột.
- Không sửa migration đã merge; không SQL thô trừ khi cần và phải tham số hóa.

**WPF (MVVM)**
- CommunityToolkit.Mvvm; code-behind rỗng.
- ViewModel không tham chiếu control hay `MessageBox` (dùng `IDialogService`).
- Không treo UI thread; màu/font lấy từ ResourceDictionary (Design Token), không hard-code.
- Đủ trạng thái rỗng / đang tải / lỗi; hỗ trợ bàn phím; ô quét mã vạch giữ focus.

**Bảo mật**
- Không đưa bí mật (mật khẩu, khóa, chuỗi kết nối) vào code; mật khẩu băm BCrypt/PBKDF2.
- Không log dữ liệu cá nhân; kiểm tra quyền ở tầng Application, không chỉ ẩn nút.
- Thao tác nhạy cảm (miễn/giảm phạt, xóa, đặt lại mật khẩu, khôi phục dữ liệu) ghi audit log.

## Quy trình review

Đọc kỹ code, đối chiếu checklist rồi báo cáo:

- [ ] Đúng yêu cầu SRS, không làm thừa phạm vi
- [ ] Đúng tầng, đúng chiều phụ thuộc
- [ ] Đặt tên, độ dài method/class theo chuẩn
- [ ] Lỗi xử lý bằng `Result`/mã lỗi; không nuốt exception; có `CancellationToken`
- [ ] Truy vấn không N+1, có phân trang, `AsNoTracking` khi chỉ đọc; transaction + `RowVersion` cho nghiệp vụ ghi
- [ ] Không bí mật/dữ liệu cá nhân trong code hoặc log; có kiểm tra quyền
- [ ] Có test phù hợp, tên test đúng quy ước
- [ ] UI đúng Design Token, đủ trạng thái rỗng/lỗi/đang tải, hỗ trợ bàn phím

**Dùng đúng khung báo cáo này:**

```
## Kết quả review
**Kết luận:** ✅ Đạt / ⚠️ Cần sửa nhỏ / ❌ Chưa đạt

### Lỗi chặn (phải sửa)
- [mục chuẩn] vị trí – vấn đề – cách sửa

### Nên cải thiện
- ...

### Điểm tốt
- ...

### Đoạn code đã sửa (nếu có)
```

Nêu lỗi chặn trước, chỉ nêu điểm thật sự đáng nói. Khi sửa code, giữ nguyên hành vi nghiệp vụ trừ khi lỗi chính là hành vi đó, và nói rõ.

## Quy ước làm việc với người dùng

- Trả lời bằng tiếng Việt; giữ nguyên tên kỹ thuật, tên kiểu, mã lỗi bằng tiếng Anh.
- Code trả về đầy đủ, chạy được trong ngữ cảnh đã nêu; ghi rõ `using`/namespace cần thiết.
- Khi yêu cầu mâu thuẫn với chuẩn, nêu mâu thuẫn và đề xuất cách tuân thủ; chỉ làm ngoại lệ khi người dùng xác nhận và gắn `STANDARD-EXCEPTION`.
