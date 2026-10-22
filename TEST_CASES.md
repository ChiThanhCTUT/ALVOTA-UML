# DANH SÁCH KỊCH BẢN KIỂM THỬ TOÀN DIỆN (TEST CASES SPECIFICATION)

Tài liệu này định nghĩa bộ kịch bản kiểm thử toàn diện (Unit Test, Integration Test và System/UI Test) cho dự án **Quản lý Thư viện Trường học (Desktop .NET 8)**, bao phủ các trường hợp bình thường, dữ liệu sai/thiếu, trường hợp biên, phân quyền, lỗi đồng thời/CSDL và kiểm thử tích hợp đầu-cuối.

---

## 1. Phân hệ Xác thực & Phân quyền (`Auth`)

| Mã TC | Tên kịch bản | Loại trường hợp | Điều kiện tiên quyết / Đầu vào | Các bước thực hiện | Kết quả mong đợi | Lỗi liên quan |
|---|---|---|---|---|---|---|
| **TC-AUTH-01** | Bắt buộc đăng nhập khi khởi động ứng dụng | Bảo mật / Tích hợp | Ứng dụng vừa khởi chạy lần đầu | 1. Mở ứng dụng `Library.Desktop.exe`.<br>2. Quan sát màn hình đầu tiên và thanh Sidebar. | 1. Màn hình `LoginView` được hiển thị.<br>2. Thanh Sidebar bị ẩn (`IsLoggedIn == false`).<br>3. `ICurrentUserService.IsAuthenticated == false`, `Role == null`. | `BUG-001` |
| **TC-AUTH-02** | Đăng nhập thành công với tài khoản hợp lệ | Bình thường | Tài khoản `librarian` (`IsActive = true`, không bị khóa) | 1. Nhập `Username = "librarian"` và mật khẩu hợp lệ.<br>2. Nhấn phím `Enter` hoặc bấm "ĐĂNG NHẬP". | 1. Mật khẩu được ẩn bằng dấu chấm (`PasswordBox`).<br>2. Đăng nhập thành công, chuyển sang `DashboardView`.<br>3. `FailedLoginAttempts` reset về `0`, `LastLoginAtUtc` được cập nhật. | `BUG-015`, `BUG-016` |
| **TC-AUTH-03** | Đăng nhập thất bại khi để trống Username hoặc Password | Dữ liệu thiếu | Màn hình `LoginView` | 1. Để trống `Username` hoặc `Password`.<br>2. Gửi `LoginCommand`. | Validator chặn và trả về lỗi `"Tên đăng nhập không được để trống."` / `"Mật khẩu không được để trống."`. | `BUG-009` |
| **TC-AUTH-04** | Khóa tạm thời 5 phút sau 5 lần nhập sai mật khẩu liên tiếp (`AUTH-03`) | Biên | Tài khoản `librarian` có `FailedLoginAttempts = 4` | 1. Nhập sai mật khẩu lần thứ 5.<br>2. Thử đăng nhập lại bằng mật khẩu đúng ngay lập tức.<br>3. Tiến thời gian thêm 5 phút 1 giây (`FakeTimeProvider`) rồi đăng nhập đúng. | 1. Lần sai thứ 5 trả về `AUTH.ACCOUNT_LOCKED`, `LockoutEndUtc = now + 5 phút`.<br>2. Khi chưa hết 5 phút, nhập mật khẩu đúng vẫn bị chặn `AUTH.ACCOUNT_LOCKED`.<br>3. Sau 5 phút, đăng nhập thành công. | *(Đã có test cơ bản)* |
| **TC-AUTH-05** | Chặn đăng nhập đối với tài khoản bị vô hiệu hóa (`IsActive = false`) | Bảo mật / Biên | Tài khoản có `IsActive = false` trong bảng `Users` | 1. Gửi `LoginCommand` với đúng tên đăng nhập và mật khẩu của tài khoản `IsActive = false`. | Trả về `Result.Failure` (`AUTH.ACCOUNT_LOCKED` hoặc `AUTH.ACCOUNT_DISABLED`), không cấp phiên đăng nhập. | `BUG-002` |
| **TC-AUTH-06** | Kiểm tra phân quyền `Assistant` và `Librarian` ở tầng Application | Không đủ quyền | Đăng nhập với vai trò `Assistant` hoặc `Librarian` | 1. Với `Assistant`: gọi `CreateBookCommand`, `DeleteBookCommand`, `PayFineCommand`, `WaiveFineCommand`, `UpdateSettingCommand`.<br>2. Với `Librarian`: gọi `CreateUserCommand`, `ResetPasswordCommand`, `RestoreBackupCommand`, `GetAuditLogsQuery`. | Toàn bộ các lời gọi vượt quyền trả về `Result.Failure(DomainErrors.Auth.Unauthorized)` ngay tại tầng Application. | `BUG-010` |
| **TC-AUTH-07** | Tự động đăng xuất sau 15 phút không thao tác (`AUTH-06`) | Biên / Bảo mật | Đang đăng nhập thành công trên Desktop | 1. Không di chuyển chuột và không nhấn phím trong 15 phút (`InactivityAutoLogoutMinutes`). | Hệ thống tự động xóa phiên `CurrentUserService.Clear()` và điều hướng về `LoginView`. | `BUG-015` |

---

## 2. Phân hệ Quản lý Danh mục Sách & Bản sao (`Catalog`)

| Mã TC | Tên kịch bản | Loại trường hợp | Điều kiện tiên quyết / Đầu vào | Các bước thực hiện | Kết quả mong đợi | Lỗi liên quan |
|---|---|---|---|---|---|---|
| **TC-CAT-01** | Thêm mới đầu sách có chọn tác giả và tạo bản sao ban đầu | Bình thường / Tích hợp | Đã có `Category`, `Publisher`, `Author` (nạp qua `GetCatalogLookupsQuery`) | 1. Mở form Thêm sách trên `CatalogView`.<br>2. Nhập Tiêu đề, ISBN, chọn Thể loại, NXB, Tác giả, Giá bìa `85000`, Số bản sao `3`.<br>3. Bấm "Lưu đầu sách". | 1. Không văng `NullReferenceException`.<br>2. Trả về `BookDto` có đầy đủ tên tác giả, 3 bản sao với mã vạch duy nhất.<br>3. Hiển thị sách mới trên `DataGrid`. | `BUG-004`, `BUG-015` |
| **TC-CAT-02** | Chặn thêm/sửa đầu sách có ISBN trùng hoặc Giá bìa âm | Dữ liệu sai | Đã có sách mang ISBN `"978-604-0-12345-6"` | 1. Tạo sách mới với cùng ISBN `"978-604-0-12345-6"`.<br>2. Tạo sách mới với `Price = -10000`. | 1. Trả về lỗi `CAT.DUPLICATE_ISBN`.<br>2. Validator chặn `Price < 0` với thông báo `"Giá bìa không được âm."`. | `BUG-009` |
| **TC-CAT-03** | Thêm lại đầu sách có cùng ISBN sau khi đầu sách cũ đã bị xóa mềm | Biên / Database | Đầu sách có ISBN `"978-111"` đã bị xóa mềm (`IsDeleted = true`) | 1. Gọi `DeleteBookCommand` cho sách có ISBN `"978-111"`.<br>2. Gọi `CreateBookCommand` với ISBN `"978-111"`. | Thêm mới thành công, không bị lỗi `SqliteException: UNIQUE constraint failed: Books.Isbn`. | `BUG-013` |
| **TC-CAT-04** | Chặn xóa đầu sách khi còn bản sao đang được mượn (`CAT-07`) | Nghiệp vụ / Biên | Đầu sách có 2 bản sao: 1 `Available`, 1 `Borrowed` | 1. Gọi `DeleteBookCommand` cho đầu sách đó. | Trả về `Result.Failure(DomainErrors.Catalog.HasActiveLoans)`, sách và các bản sao không bị đánh dấu `IsDeleted`. | `BUG-015` |
| **TC-CAT-05** | Nhập danh sách sách hàng loạt từ file Excel (`CAT-06`) | Tích hợp / Biên | File Excel có 10 dòng: 8 dòng hợp lệ (mỗi dòng 5 bản sao), 1 dòng trống tiêu đề, 1 dòng trùng ISBN | 1. Gọi `ImportBooksExcelCommand` với stream Excel. | 1. Nhập thành công 8 đầu sách (40 bản sao có `Barcode` hoàn toàn duy nhất, không trùng `Ticks`).<br>2. Trả về `TotalImported = 8`, `TotalErrors = 2` kèm đúng số dòng bị lỗi. | `BUG-012`, `BUG-015` |
| **TC-CAT-06** | Tìm kiếm sách bằng tiếng Việt có dấu không phân biệt hoa/thường (`CAT-08`) | Bình thường / Biên | Sách trong DB có tiêu đề `"Đắc Nhân Tâm"` và tác giả `"Nguyễn Nhật Ánh"` | 1. Tìm kiếm với từ khóa `"đắc nhân tâm"` (chữ `đ` thường).<br>2. Tìm kiếm với từ khóa `"nguyễn"` (chữ `n` thường). | Trả về đúng đầu sách `"Đắc Nhân Tâm"` và sách của `"Nguyễn Nhật Ánh"`. | `BUG-014` |

---

## 3. Phân hệ Quản lý Bạn đọc (`Members`)

| Mã TC | Tên kịch bản | Loại trường hợp | Điều kiện tiên quyết / Đầu vào | Các bước thực hiện | Kết quả mong đợi | Lỗi liên quan |
|---|---|---|---|---|---|---|
| **TC-MEM-01** | Đăng ký thẻ bạn đọc Học sinh và Giáo viên | Bình thường | Mã thẻ chưa tồn tại | 1. Tạo bạn đọc `Student` (hạn thẻ 12 tháng).<br>2. Tạo bạn đọc `Teacher` (hạn thẻ 24 tháng). | Hạn mức mượn lần lượt là 3 cuốn (14 ngày) và 10 cuốn (30 ngày) hoặc theo cấu hình `Settings`. | `BUG-008` |
| **TC-MEM-02** | Cấp lại thẻ bạn đọc khi báo mất (`MEM-05`) | Bình thường | Bạn đọc đang có `ReissueCount = 0`, `CardNumber = "HS-001"` | 1. Gọi `ReissueCardCommand` với mã thẻ mới `"HS-001-R1"`. | 1. `CardNumber` cập nhật thành `"HS-001-R1"`.<br>2. `ReissueCount` tăng lên `1`, `CardStatus = Active`. | `BUG-015` |
| **TC-MEM-03** | Chặn khóa thẻ hoặc khóa tốt nghiệp khi bạn đọc còn sách đang mượn | Nghiệp vụ / Biên | Học sinh lớp `12A1` đang mượn 1 cuốn sách chưa trả | 1. Gọi `UpdateMemberCommand` chuyển `CardStatus = TemporarilyLocked`.<br>2. Hoặc gọi `BatchGraduateMembersCommand` với `LockGraduating = true` cho lớp `12A1`. | Hệ thống từ chối khóa thẻ của bạn đọc còn sách đang mượn (`MEM.HAS_ACTIVE_LOANS`) hoặc báo danh sách học sinh còn nợ sách. | `BUG-015` |
| **TC-MEM-04** | Xem chi tiết lịch sử mượn trả và phiếu phạt của bạn đọc (`MEM-06`) | Tích hợp | Bạn đọc có 1 sách đang mượn, 2 sách đã trả trong quá khứ, 1 phiếu phạt đã đóng và 1 phiếu phạt chưa đóng | 1. Gọi `GetMemberDetailHistoryQuery(memberId)`. | 1. `ActiveLoans` có 1 mục.<br>2. `BorrowHistory` có đầy đủ 3 mục (bao gồm cả 2 cuốn đã trả).<br>3. `Fines` hiển thị đầy đủ lịch sử phạt. | `BUG-015` |

---

## 4. Phân hệ Mượn / Trả / Gia hạn / Đặt trước (`Circulation`)

| Mã TC | Tên kịch bản | Loại trường hợp | Điều kiện tiên quyết / Đầu vào | Các bước thực hiện | Kết quả mong đợi | Lỗi liên quan |
|---|---|---|---|---|---|---|
| **TC-CIR-01** | Kiểm tra đầy đủ 6 điều kiện chặn mượn sách (`CIR-02`, `BR-01`..`BR-11`) | Nghiệp vụ / Biên | Các bạn đọc và bản sao ở các trạng thái khác nhau | Thử mượn sách trong 6 trường hợp:<br>1. Thẻ bị khóa hoặc hết hạn.<br>2. Sách `IsReferenceOnly = true`.<br>3. Bản sao đang `Borrowed`/`Damaged`/`Lost`.<br>4. Học sinh đã mượn đủ 3 cuốn.<br>5. Đang có 1 cuốn quá hạn chưa trả.<br>6. Tổng tiền phạt chưa đóng (`RemainingAmount`) > `20.000 đ`. | Trả về đúng các mã lỗi tương ứng: `CIR.MEMBER_BLOCKED`, `CIR.CANNOT_BORROW_REFERENCE`, `CIR.COPY_UNAVAILABLE`, `CIR.LIMIT_REACHED`, `CIR.HAS_OVERDUE`, `FIN.DEBT_EXCEEDED`. | *(Hoạt động tốt)* |
| **TC-CIR-02** | Đặt trước sách đang được mượn và người đặt trước đến nhận sách (`CIR-06`, `BR-10`) | Tích hợp đầu-cuối | Tất cả bản sao của sách A đang `Borrowed`; Bạn đọc M2 đặt trước sách A (`Pending`) | 1. Bạn đọc M1 trả bản sao `CP-A1` (tình trạng `Normal`).<br>2. Bạn đọc M3 (không đặt trước) thử mượn `CP-A1`.<br>3. Bạn đọc M2 (người đặt trước) đến mượn `CP-A1` trong vòng 3 ngày. | 1. Khi M1 trả sách: phiếu đặt trước của M2 chuyển sang `Notified`, `HoldExpiresAtUtc = now + 3 ngày`, `CP-A1.Status = Reserved`.<br>2. M3 mượn bị từ chối với lỗi `CIR.RESERVATION_CONFLICT`.<br>3. **M2 mượn thành công**: phiếu đặt trước chuyển sang `Completed`, `CP-A1.Status = Borrowed`. | `BUG-005` |
| **TC-CIR-03** | Trả sách quá hạn có áp dụng trần phạt theo giá bìa (`BR-05`, `BR-06`) | Biên | Sách có giá bìa `30.000 đ`; trả trễ `45 ngày` (mức phạt `1.000 đ/ngày`) | 1. Gọi `ReturnBookCommand` với tình trạng `Normal`. | 1. Tiền phạt tính ra là `30.000 đ` (bằng giá bìa, không vượt lên `45.000 đ`).<br>2. Khi mọi `LoanItem` trong `Loan` đã trả, `Loan.Status` chuyển sang `LoanStatus.Returned`. | `BUG-011` |
| **TC-CIR-04** | Trả sách bị hư hỏng nhẹ / hư hỏng nặng / làm mất sách (`BR-07`, `BR-08`) | Nghiệp vụ | Sách giá bìa `100.000 đ` | 1. Trả với `DamagedLight` -> kiểm tra phạt.<br>2. Trả với `DamagedHeavy` -> kiểm tra phạt.<br>3. Trả với `Lost` (kèm trễ hạn 5 ngày) -> kiểm tra phạt và trạng thái bản sao. | 1. `DamagedLight`: phạt `20.000 đ`, bản sao `Damaged`.<br>2. `DamagedHeavy`: phạt `50.000 đ`, bản sao `Damaged`.<br>3. `Lost` + trễ 5 ngày: phạt `100.000 + 20.000 + 5.000 = 125.000 đ`, bản sao `Lost`. | *(Hoạt động tốt)* |
| **TC-CIR-05** | Xung đột đồng thời khi 2 thao tác cùng cập nhật 1 bản sao (`RowVersion`) | Lỗi đồng thời / Server | 2 `LibraryDbContext` độc lập cùng đọc 1 `BookCopy` (`Available`) | 1. Context 1 thực hiện mượn và `SaveChangesAsync` thành công.<br>2. Context 2 (với `RowVersion` cũ) thực hiện mượn và `SaveChangesAsync`. | Context 2 bắt `DbUpdateConcurrencyException` và trả về `Result.Failure(DomainErrors.Circulation.ConcurrencyConflict)` mà không làm crash ứng dụng. | `BUG-003`, `BUG-011` |

---

## 5. Phân hệ Quản lý Tiền phạt (`Fines`)

| Mã TC | Tên kịch bản | Loại trường hợp | Điều kiện tiên quyết / Đầu vào | Các bước thực hiện | Kết quả mong đợi | Lỗi liên quan |
|---|---|---|---|---|---|---|
| **TC-FIN-01** | Thanh toán một phần (Partial Payment) phiếu phạt | Nghiệp vụ / Biên | Phiếu phạt có `Amount = 30.000 đ`, `Status = Pending`, `PaidAmount = 0` | 1. Gọi `PayFineCommand(fineId, Amount: 15000m)` (đóng 50%).<br>2. Kiểm tra `Status`, `PaidAmount`, `RemainingAmount` và `Member.GetUnpaidFineTotal()`. | 1. `PaidAmount = 15.000 đ`, `RemainingAmount = 15.000 đ`.<br>2. **`Status` vẫn phải là `FineStatus.Pending`** (không bị chuyển nhầm sang `Paid`).<br>3. `Member.GetUnpaidFineTotal()` trả về `15.000 đ`.<br>4. Doanh thu phạt trong tháng trên Dashboard tăng thêm `15.000 đ`. | `BUG-006`, `BUG-014` |
| **TC-FIN-02** | Thanh toán nốt phần còn lại và xuất biên lai PDF (`FIN-03`) | Bình thường / Tích hợp | Phiếu phạt ở `TC-FIN-01` còn nợ `15.000 đ` | 1. Gọi `PayFineCommand(fineId, Amount: 15000m)`.<br>2. Xuất biên lai PDF qua `IPdfService.GenerateReceiptPdf`. | 1. `PaidAmount = 30.000 đ`, `RemainingAmount = 0 đ`, `Status = FineStatus.Paid`.<br>2. File PDF biên lai được sinh ra hợp lệ (> 0 bytes). | `BUG-006`, `BUG-015` |
| **TC-FIN-03** | Chặn thanh toán số tiền âm, bằng 0 hoặc lớn hơn số tiền còn nợ | Dữ liệu sai / Biên | Phiếu phạt còn nợ `15.000 đ` | 1. Gọi `PayFineCommand` với `Amount = 0` hoặc `-5000`.<br>2. Gọi `PayFineCommand` với `Amount = 20000`. | 1. Validator chặn `Amount <= 0`.<br>2. Handler trả về `FIN.INVALID_PAYMENT_AMOUNT` khi `Amount > RemainingAmount`. | `BUG-009` |
| **TC-FIN-04** | Miễn phạt yêu cầu nhập lý do và ghi Audit Log (`FIN-04`) | Bảo mật / Nghiệp vụ | Phiếu phạt `Pending`; đăng nhập quyền `Librarian` | 1. Gọi `WaiveFineCommand` với lý do rỗng `""`.<br>2. Gọi `WaiveFineCommand` với lý do hợp lệ. | 1. Lý do rỗng bị chặn.<br>2. Khi có lý do: `Status = Waived`, tạo 1 bản ghi trong bảng `AuditLogs` với `Action = "WaiveFine"`. | `BUG-009`, `BUG-015` |

---

## 6. Phân hệ Kiểm kê kho & Hệ thống (`Inventory` & `System`)

| Mã TC | Tên kịch bản | Loại trường hợp | Điều kiện tiên quyết / Đầu vào | Các bước thực hiện | Kết quả mong đợi | Lỗi liên quan |
|---|---|---|---|---|---|---|
| **TC-INV-01** | Kiểm kê kho phát hiện sách bị thất lạc (`INV-01` → `INV-03`) | Tích hợp đầu-cuối | Kệ `"Kệ V1"` có 5 bản sao đang ở trạng thái `Available` | 1. Tạo đợt kiểm kê cho `"Kệ V1"`.<br>2. Quét mã vạch của 3 bản sao, bỏ qua 2 bản sao.<br>3. Bấm "Chốt & Hoàn tất đợt kiểm kê". | 1. Khi tạo đợt kiểm kê: `TotalExpected = 5`.<br>2. Sau khi quét 3 cuốn: `TotalScanned = 3`.<br>3. Khi hoàn tất: `TotalMissing = 2`, 2 bản sao không được quét mang `ResultStatus = Missing`. | `BUG-007` |
| **TC-SYS-01** | Thay đổi cấu hình tham số nghiệp vụ có hiệu lực ngay (`SYS-01`) | Tích hợp | Admin mở tab Cấu hình hệ thống | 1. Đổi `StudentBorrowLimit` từ `3` lên `5` và `DailyLateFineRate` từ `1000` lên `2000`.<br>2. Thực hiện cho học sinh mượn cuốn thứ 4 và trả sách quá hạn 2 ngày. | 1. Học sinh mượn được cuốn thứ 4 (hạn mức mới = 5).<br>2. Tiền phạt trễ hạn 2 ngày tính theo mức mới = `4.000 đ`. | `BUG-008` |
| **TC-SYS-02** | Sao lưu CSDL tự động giữ tối đa 14 bản và Phục hồi dữ liệu (`SYS-02`) | Biên / Hệ thống | Thư mục `Backups` đã có 14 file `library_backup_*.db` | 1. Gọi `CreateBackupCommand` tạo bản sao lưu thứ 15.<br>2. Gọi `RestoreBackupCommand` từ 1 bản sao lưu. | 1. Bản sao lưu cũ nhất bị xóa, thư mục chỉ giữ đúng 14 bản mới nhất.<br>2. Phục hồi dữ liệu thành công mà không bị lỗi `IOException: The process cannot access the file 'library.db'` và `AuditLog` ghi nhận thao tác. | `BUG-003`, `BUG-015` |
