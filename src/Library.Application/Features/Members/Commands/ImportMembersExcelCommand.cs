using Library.Application.Common.Interfaces;
using Library.Domain.Common;
using Library.Domain.Entities;
using Library.Domain.Enums;
using MediatR;

namespace Library.Application.Features.Members.Commands;

public record MemberImportRow(
    string CardNumber,
    string FullName,
    DateTime DateOfBirth,
    MemberType Type,
    string ClassOrDepartment,
    string? PhoneNumber,
    string? Email);

public record MemberImportResultDto(
    int TotalImported,
    int TotalErrors,
    IReadOnlyList<(int Row, string Error)> Errors);

public sealed record ImportMembersExcelCommand(Stream ExcelStream) : IRequest<Result<MemberImportResultDto>>;

public sealed class ImportMembersExcelCommandHandler : IRequestHandler<ImportMembersExcelCommand, Result<MemberImportResultDto>>
{
    private readonly IExcelService _excelService;
    private readonly IMemberRepository _members;
    private readonly IUnitOfWork _uow;
    private readonly TimeProvider _time;

    public ImportMembersExcelCommandHandler(
        IExcelService excelService,
        IMemberRepository members,
        IUnitOfWork uow,
        TimeProvider time)
    {
        _excelService = excelService;
        _members = members;
        _uow = uow;
        _time = time;
    }

    public async Task<Result<MemberImportResultDto>> Handle(ImportMembersExcelCommand request, CancellationToken ct)
    {
        var now = _time.GetUtcNow().UtcDateTime;

        var (rows, parseErrors) = _excelService.ImportFromExcel<MemberImportRow>(request.ExcelStream, dict =>
        {
            if (!dict.TryGetValue("Mã thẻ", out var cardNumber) || string.IsNullOrWhiteSpace(cardNumber))
                return Result.Failure<MemberImportRow>(new Error("ROW_INVALID", "Mã thẻ không được để trống."));

            if (!dict.TryGetValue("Họ và tên", out var fullName) || string.IsNullOrWhiteSpace(fullName))
                return Result.Failure<MemberImportRow>(new Error("ROW_INVALID", "Họ tên không được để trống."));

            dict.TryGetValue("Lớp / Đơn vị", out var dept);
            dict.TryGetValue("Số điện thoại", out var phone);
            dict.TryGetValue("Email", out var email);
            dict.TryGetValue("Loại", out var typeStr);
            dict.TryGetValue("Ngày sinh", out var dobStr);

            var type = typeStr?.Contains("Giáo viên", StringComparison.OrdinalIgnoreCase) == true
                ? MemberType.Teacher
                : MemberType.Student;

            if (!DateTime.TryParse(dobStr, out var dob))
            {
                dob = new DateTime(2010, 1, 1);
            }

            return Result.Success(new MemberImportRow(
                cardNumber.Trim(),
                fullName.Trim(),
                dob,
                type,
                string.IsNullOrWhiteSpace(dept) ? "10A1" : dept.Trim(),
                string.IsNullOrWhiteSpace(phone) ? null : phone.Trim(),
                string.IsNullOrWhiteSpace(email) ? null : email.Trim()));
        });

        var importedCount = 0;
        var errorList = new List<(int Row, string Error)>(parseErrors.Select(e => (e.RowNumber, e.ErrorMessage)));

        for (int i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            var rowIndex = i + 2;

            try
            {
                var exists = await _members.ExistsCardNumberAsync(row.CardNumber, ct: ct);
                if (exists)
                {
                    errorList.Add((rowIndex, $"Trùng mã thẻ bạn đọc: {row.CardNumber}"));
                    continue;
                }

                var member = new Member
                {
                    CardNumber = row.CardNumber,
                    FullName = row.FullName,
                    DateOfBirth = row.DateOfBirth,
                    Type = row.Type,
                    ClassOrDepartment = row.ClassOrDepartment,
                    PhoneNumber = row.PhoneNumber,
                    Email = row.Email,
                    CardStatus = CardStatus.Active,
                    CardIssuedAtUtc = now,
                    CardExpiresAtUtc = now.AddYears(1),
                    CreatedAtUtc = now,
                    CreatedBy = "ExcelImport"
                };

                await _members.AddAsync(member, ct);
                await _uow.SaveChangesAsync(ct);
                importedCount++;
            }
            catch (Exception ex)
            {
                errorList.Add((rowIndex, ex.Message));
            }
        }

        return Result.Success(new MemberImportResultDto(importedCount, errorList.Count, errorList));
    }
}
