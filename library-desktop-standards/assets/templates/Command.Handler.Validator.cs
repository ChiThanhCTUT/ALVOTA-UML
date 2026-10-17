// Application/Features/Circulation/Commands/BorrowBook/
// Yêu cầu SRS: CIR-01, CIR-02, CIR-09
using FluentValidation;
using MediatR;

namespace Library.Application.Features.Circulation.Commands.BorrowBook;

public sealed record BorrowBookCommand(Guid MemberId, string CopyBarcode) : IRequest<Result<LoanDto>>;

public sealed class BorrowBookCommandValidator : AbstractValidator<BorrowBookCommand>
{
    public BorrowBookCommandValidator()
    {
        RuleFor(x => x.MemberId).NotEmpty();
        RuleFor(x => x.CopyBarcode).NotEmpty().MaximumLength(50);
    }
}

public sealed class BorrowBookCommandHandler : IRequestHandler<BorrowBookCommand, Result<LoanDto>>
{
    private readonly IMemberRepository _members;
    private readonly IBookCopyRepository _copies;
    private readonly IUnitOfWork _uow;
    private readonly TimeProvider _time;

    public BorrowBookCommandHandler(
        IMemberRepository members, IBookCopyRepository copies, IUnitOfWork uow, TimeProvider time)
        => (_members, _copies, _uow, _time) = (members, copies, uow, time);

    public async Task<Result<LoanDto>> Handle(BorrowBookCommand request, CancellationToken ct)
    {
        var now = _time.GetUtcNow();

        var member = await _members.GetByIdAsync(request.MemberId, ct);
        if (member is null) return Result.Failure<LoanDto>(Errors.Member.NotFound);

        var copy = await _copies.GetByBarcodeAsync(request.CopyBarcode, ct);
        if (copy is null) return Result.Failure<LoanDto>(Errors.Copy.NotFound);

        // Quy tắc nghiệp vụ nằm trong Domain, handler chỉ điều phối.
        var loan = member.Borrow(copy, now);
        if (loan.IsFailure) return Result.Failure<LoanDto>(loan.Error);

        await _uow.SaveChangesAsync(ct); // một transaction; RowVersion chống xung đột
        return Result.Success(LoanDto.From(loan.Value));
    }
}
