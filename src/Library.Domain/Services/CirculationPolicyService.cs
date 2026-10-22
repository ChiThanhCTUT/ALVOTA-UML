using Library.Domain.Common;
using Library.Domain.Entities;
using Library.Domain.Enums;

namespace Library.Domain.Services;

public static class CirculationPolicyService
{
    /// <summary>
    /// CIR-02, BR-01, BR-02, BR-09, BR-11: Kiểm tra toàn diện điều kiện mượn sách
    /// </summary>
    public static Result CanBorrow(
        Member member,
        BookCopy copy,
        int activeLoansCount,
        bool hasOverdueLoans,
        decimal unpaidFineAmount,
        DateTime utcNow,
        decimal debtThreshold = BusinessRuleConstants.DebtThresholdToBlockBorrowing,
        int? customStudentLimit = null,
        int? customTeacherLimit = null)
    {
        // 1. Thẻ bị khóa hoặc hết hạn
        if (!member.IsCardActive(utcNow))
            return Result.Failure(DomainErrors.Circulation.MemberBlocked);

        // 2. Sách chỉ đọc tại chỗ (BR-11)
        if (copy.Book != null && copy.Book.IsReferenceOnly)
            return Result.Failure(DomainErrors.Circulation.CannotBorrowReference);

        // 3. Bản sao không ở trạng thái sẵn sàng (CIR-02, CAT-04)
        if (!copy.CanBeBorrowed)
            return Result.Failure(DomainErrors.Circulation.CopyUnavailable);

        // 4. Đạt hạn mức mượn (BR-01, BR-02)
        var limit = member.GetBorrowLimit(customStudentLimit, customTeacherLimit);
        if (activeLoansCount >= limit)
            return Result.Failure(DomainErrors.Circulation.LimitReached);

        // 5. Có sách quá hạn chưa trả (CIR-02)
        if (hasOverdueLoans)
            return Result.Failure(DomainErrors.Circulation.HasOverdue);

        // 6. Nợ phạt vượt ngưỡng (BR-09)
        if (unpaidFineAmount > debtThreshold)
            return Result.Failure(DomainErrors.Fine.DebtExceeded);

        return Result.Success();
    }

    /// <summary>
    /// CIR-05, BR-04: Kiểm tra điều kiện gia hạn
    /// </summary>
    public static Result CanRenew(
        LoanItem item,
        DateTime utcNow,
        bool hasActiveReservation,
        int maxRenewalCount = BusinessRuleConstants.MaxRenewalCount)
    {
        if (item.IsReturned)
            return Result.Failure(DomainErrors.Circulation.AlreadyReturned);

        if (item.RenewalCount >= maxRenewalCount)
            return Result.Failure(DomainErrors.Circulation.RenewNotAllowed);

        if (utcNow > item.DueDateUtc)
            return Result.Failure(DomainErrors.Circulation.RenewNotAllowed);

        if (hasActiveReservation)
            return Result.Failure(DomainErrors.Circulation.RenewNotAllowed);

        return Result.Success();
    }
}
