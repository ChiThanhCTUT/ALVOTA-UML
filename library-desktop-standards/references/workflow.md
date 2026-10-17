# Quy trình làm việc

## Mục lục
1. Nhánh Git
2. Commit
3. Mô tả Pull Request
4. CI
5. Definition of Done
6. Phân công và bàn giao
7. Phát hành

## 1. Nhánh Git

| Nhánh | Mục đích |
|---|---|
| `main` | Bản ổn định, chỉ merge qua PR |
| `develop` | Tích hợp |
| `feature/<mã>-<mô-tả>` | Tính năng, ví dụ `feature/CIR-02-block-overdue` |
| `fix/<mã>-<mô-tả>` | Sửa lỗi |
| `release/x.y.z` | Chuẩn bị phát hành |

## 2. Commit (Conventional Commits)

`<type>(<scope>): <mô tả ngắn, thể mệnh lệnh>`

Type: `feat`, `fix`, `refactor`, `test`, `docs`, `chore`, `perf`, `build`.

Ví dụ:
- `feat(circulation): block borrowing when member has overdue loans (CIR-02)`
- `fix(fine): avoid duplicate fine on repeated return (FIN-05)`

## 3. Mô tả Pull Request

```
## Mục đích
(Yêu cầu SRS: CIR-02, BR-05)

## Thay đổi chính
-

## Cách kiểm thử
-

## Checklist
- [ ] Đã chạy build và toàn bộ test
- [ ] Có test cho thay đổi
- [ ] Không có bí mật/dữ liệu cá nhân trong code và log
- [ ] Đã cập nhật tài liệu / migration (nếu có)
```

Tối thiểu 1 người duyệt; tác giả không tự merge; lỗi chặn phải xử lý xong mới merge.

## 4. CI (mỗi PR)

1. `dotnet restore` và `dotnet build -warnaserror`
2. `dotnet format --verify-no-changes`
3. `dotnet test` kèm độ phủ
4. `dotnet list package --vulnerable`
5. Quét bí mật (ví dụ gitleaks)

## 5. Definition of Done

- [ ] Đáp ứng tiêu chí chấp nhận của yêu cầu SRS
- [ ] Code qua review và CI
- [ ] Có unit test (và integration test nếu đụng dữ liệu)
- [ ] Không còn cảnh báo build, không còn TODO chưa có ticket
- [ ] Migration chạy được trên CSDL trống và CSDL cũ
- [ ] Tài liệu liên quan đã cập nhật
- [ ] QA đã kiểm thử và chấp nhận

## 6. Phân công và bàn giao

| Nhóm | Trách nhiệm chính |
|---|---|
| Application/Domain | Nghiệp vụ, handler, validator, quy tắc BR-xx |
| Desktop | View, ViewModel, dialog, điều hướng, Design Token |
| Database | Entity configuration, migration, chỉ mục, sao lưu/phục hồi |
| UX | Thiết kế màn hình, Design Token, luồng quét mã vạch |
| QA | Kịch bản test, hồi quy, nghiệm thu |
| DevOps | CI, đóng gói (MSIX/Inno Setup), phát hành |

Bàn giao: thống nhất trước DTO/Command/Query và mã lỗi; ghi vào PR những gì nhóm kế tiếp cần biết (interface, migration, cấu hình mới).

## 7. Phát hành

- SemVer `MAJOR.MINOR.PATCH`, tag Git cho mỗi bản phát hành, ghi `CHANGELOG.md`.
- Build `Release`, đóng gói MSIX hoặc Inno Setup.
- Trước khi cập nhật trên máy người dùng: tự động sao lưu CSDL rồi mới chạy migration.
- Có kế hoạch quay lui (giữ bản cài đặt trước và bản sao lưu).
