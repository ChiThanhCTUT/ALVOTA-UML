using Library.Application.Common.Interfaces;
using Library.Domain.Common;
using Library.Domain.Entities;
using Library.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Library.Infrastructure.Persistence;

public sealed class DatabaseInitializer
{
    private readonly LibraryDbContext _context;
    private readonly IPasswordHasher _hasher;

    public DatabaseInitializer(LibraryDbContext context, IPasswordHasher hasher)
    {
        _context = context;
        _hasher = hasher;
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        await _context.Database.EnsureCreatedAsync(ct);

        // 1. Seed Roles (AUTH-04)
        var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Admin", ct);
        if (adminRole == null)
        {
            adminRole = new Role { Name = "Admin", Description = "Quản trị viên toàn quyền hệ thống" };
            await _context.Roles.AddAsync(adminRole, ct);
        }

        var librarianRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Librarian", ct);
        if (librarianRole == null)
        {
            librarianRole = new Role { Name = "Librarian", Description = "Thủ thư quản lý sách, bạn đọc, mượn trả, phạt" };
            await _context.Roles.AddAsync(librarianRole, ct);
        }

        var assistantRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Assistant", ct);
        if (assistantRole == null)
        {
            assistantRole = new Role { Name = "Assistant", Description = "Nhân viên hỗ trợ mượn trả, tra cứu" };
            await _context.Roles.AddAsync(assistantRole, ct);
        }

        await _context.SaveChangesAsync(ct);

        // 2. Seed Users (AUTH-01, AUTH-02)
        if (!await _context.Users.AnyAsync(ct))
        {
            var (adminHash, adminSalt) = _hasher.HashPassword("Admin@123");
            await _context.Users.AddAsync(new User
            {
                Username = "admin",
                FullName = "Quản trị viên hệ thống",
                PasswordHash = adminHash,
                Salt = adminSalt,
                RoleId = adminRole.Id,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            }, ct);

            var (libHash, libSalt) = _hasher.HashPassword("Librarian@123");
            await _context.Users.AddAsync(new User
            {
                Username = "librarian",
                FullName = "Thủ thư chính",
                PasswordHash = libHash,
                Salt = libSalt,
                RoleId = librarianRole.Id,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            }, ct);

            var (astHash, astSalt) = _hasher.HashPassword("Assistant@123");
            await _context.Users.AddAsync(new User
            {
                Username = "assistant",
                FullName = "Nhân viên hỗ trợ",
                PasswordHash = astHash,
                Salt = astSalt,
                RoleId = assistantRole.Id,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            }, ct);

            await _context.SaveChangesAsync(ct);
        }

        // 3. Seed Settings (SYS-01)
        if (!await _context.Settings.AnyAsync(ct))
        {
            var defaultSettings = new[]
            {
                new Setting { Key = "SchoolName", Value = "Trường THPT Chuyên Hà Nội - Amsterdam", Category = "General", Description = "Tên trường học" },
                new Setting { Key = "StudentBorrowLimit", Value = "3", Category = "Circulation", Description = "Hạn mức mượn sách học sinh (cuốn)" },
                new Setting { Key = "TeacherBorrowLimit", Value = "10", Category = "Circulation", Description = "Hạn mức mượn sách giáo viên (cuốn)" },
                new Setting { Key = "StudentLoanDurationDays", Value = "14", Category = "Circulation", Description = "Số ngày mượn sách học sinh" },
                new Setting { Key = "TeacherLoanDurationDays", Value = "30", Category = "Circulation", Description = "Số ngày mượn sách giáo viên" },
                new Setting { Key = "DailyLateFineRate", Value = "1000", Category = "Fine", Description = "Mức phạt trễ hạn (VNĐ/ngày/cuốn)" },
                new Setting { Key = "DebtThresholdToBlockBorrowing", Value = "20000", Category = "Fine", Description = "Ngưỡng nợ phạt chặn mượn sách (VNĐ)" },
                new Setting { Key = "MaxRenewalCount", Value = "2", Category = "Circulation", Description = "Số lần gia hạn tối đa" },
                new Setting { Key = "RenewalDays", Value = "7", Category = "Circulation", Description = "Số ngày gia hạn mỗi lần" }
            };

            await _context.Settings.AddRangeAsync(defaultSettings, ct);
            await _context.SaveChangesAsync(ct);
        }

        // 4. Seed Categories, Publishers, Authors, Sample Books and Members if empty
        if (!await _context.Categories.AnyAsync(ct))
        {
            var catScience = new Category { Name = "Khoa học Tự nhiên", Description = "Sách Toán, Lý, Hóa, Sinh" };
            var catLiterature = new Category { Name = "Văn học & Nghệ thuật", Description = "Tiểu thuyết, truyện ngắn, thơ" };
            var catIT = new Category { Name = "Công nghệ Thông tin", Description = "Lập trình, thuật toán, máy tính" };
            await _context.Categories.AddRangeAsync(new[] { catScience, catLiterature, catIT }, ct);

            var pubGD = new Publisher { Name = "NXB Giáo Dục Việt Nam", Address = "Hà Nội" };
            var pubKD = new Publisher { Name = "NXB Kim Đồng", Address = "Hà Nội" };
            var pubTre = new Publisher { Name = "NXB Trẻ", Address = "TP. Hồ Chí Minh" };
            await _context.Publishers.AddRangeAsync(new[] { pubGD, pubKD, pubTre }, ct);

            var authorNamCao = new Author { FullName = "Nam Cao" };
            var authorNguyenDu = new Author { FullName = "Nguyễn Du" };
            var authorRichter = new Author { FullName = "Jeffrey Richter" };
            await _context.Authors.AddRangeAsync(new[] { authorNamCao, authorNguyenDu, authorRichter }, ct);

            await _context.SaveChangesAsync(ct);

            // Sample Book 1
            var book1 = new Book
            {
                Title = "Chí Phèo",
                Isbn = "978-604-0-12345-6",
                PublicationYear = 2021,
                CategoryId = catLiterature.Id,
                PublisherId = pubKD.Id,
                Language = "Tiếng Việt",
                PageCount = 180,
                Price = 65000m,
                CreatedAtUtc = DateTime.UtcNow
            };
            book1.BookAuthors.Add(new BookAuthor { BookId = book1.Id, AuthorId = authorNamCao.Id });
            book1.Copies.Add(new BookCopy { BookId = book1.Id, Barcode = "CP-0010001", ShelfLocation = "Kệ V1", Status = CopyStatus.Available });
            book1.Copies.Add(new BookCopy { BookId = book1.Id, Barcode = "CP-0010002", ShelfLocation = "Kệ V1", Status = CopyStatus.Available });

            // Sample Book 2
            var book2 = new Book
            {
                Title = "CLR via C# (4th Edition)",
                Isbn = "978-0-7356-6745-7",
                PublicationYear = 2022,
                CategoryId = catIT.Id,
                PublisherId = pubTre.Id,
                Language = "Tiếng Anh",
                PageCount = 896,
                Price = 250000m,
                CreatedAtUtc = DateTime.UtcNow
            };
            book2.BookAuthors.Add(new BookAuthor { BookId = book2.Id, AuthorId = authorRichter.Id });
            book2.Copies.Add(new BookCopy { BookId = book2.Id, Barcode = "CP-0020001", ShelfLocation = "Kệ IT1", Status = CopyStatus.Available });
            book2.Copies.Add(new BookCopy { BookId = book2.Id, Barcode = "CP-0020002", ShelfLocation = "Kệ IT1", Status = CopyStatus.Available });

            // Sample Book 3: Reference Only
            var book3 = new Book
            {
                Title = "Từ điển Bách khoa Toàn thư",
                Isbn = "978-604-0-99999-9",
                PublicationYear = 2023,
                CategoryId = catScience.Id,
                PublisherId = pubGD.Id,
                Language = "Tiếng Việt",
                PageCount = 1200,
                Price = 500000m,
                IsReferenceOnly = true, // BR-11
                CreatedAtUtc = DateTime.UtcNow
            };
            book3.Copies.Add(new BookCopy { BookId = book3.Id, Barcode = "CP-REF0001", ShelfLocation = "Phòng đọc", Status = CopyStatus.Available });

            await _context.Books.AddRangeAsync(new[] { book1, book2, book3 }, ct);

            // Sample Members
            var member1 = new Member
            {
                CardNumber = "HS-2024-001",
                FullName = "Nguyễn Văn An",
                DateOfBirth = new DateTime(2008, 5, 12),
                Type = MemberType.Student,
                ClassOrDepartment = "11A1",
                PhoneNumber = "0901234567",
                CardStatus = CardStatus.Active,
                CardIssuedAtUtc = DateTime.UtcNow,
                CardExpiresAtUtc = DateTime.UtcNow.AddYears(1),
                CreatedAtUtc = DateTime.UtcNow
            };

            var member2 = new Member
            {
                CardNumber = "GV-2024-001",
                FullName = "Trần Thị Mai",
                DateOfBirth = new DateTime(1985, 9, 20),
                Type = MemberType.Teacher,
                ClassOrDepartment = "Tổ Ngữ Văn",
                PhoneNumber = "0987654321",
                CardStatus = CardStatus.Active,
                CardIssuedAtUtc = DateTime.UtcNow,
                CardExpiresAtUtc = DateTime.UtcNow.AddYears(2),
                CreatedAtUtc = DateTime.UtcNow
            };

            await _context.Members.AddRangeAsync(new[] { member1, member2 }, ct);
            await _context.SaveChangesAsync(ct);
        }
    }
}
