using Library.Domain.Common;
using Library.Domain.Enums;

namespace Library.Domain.Services;

public static class FineCalculator
{
    /// <summary>
    /// BR-05, BR-06: Phạt trễ hạn = số ngày trễ × mức phạt/ngày, có trần theo giá bìa
    /// </summary>
    public static decimal CalculateOverdueFine(
        DateTime dueDateUtc,
        DateTime returnDateUtc,
        decimal coverPrice,
        decimal dailyRate = BusinessRuleConstants.DailyLateFineRate)
    {
        if (returnDateUtc <= dueDateUtc)
            return 0m;

        var overdueDays = (int)Math.Ceiling((returnDateUtc - dueDateUtc).TotalDays);
        if (overdueDays <= 0)
            return 0m;

        var rawFine = overdueDays * dailyRate;

        // BR-06: Trần phạt trễ hạn không vượt giá bìa (nếu sách có giá bìa > 0)
        return coverPrice > 0 ? Math.Min(rawFine, coverPrice) : rawFine;
    }

    /// <summary>
    /// BR-07: Phạt mất sách = 100% giá bìa + phí xử lý
    /// </summary>
    public static decimal CalculateLostFine(
        decimal coverPrice,
        decimal processingFee = BusinessRuleConstants.LostBookProcessingFee)
    {
        return Math.Max(0m, coverPrice) + Math.Max(0m, processingFee);
    }

    /// <summary>
    /// BR-08: Phạt hỏng nặng / nhẹ = 50% / 20% giá bìa
    /// </summary>
    public static decimal CalculateDamagedFine(
        decimal coverPrice,
        ReturnCondition condition,
        decimal lightRate = BusinessRuleConstants.LightDamageRate,
        decimal heavyRate = BusinessRuleConstants.HeavyDamageRate)
    {
        var price = Math.Max(0m, coverPrice);
        return condition switch
        {
            ReturnCondition.DamagedLight => Math.Round(price * lightRate, 0),
            ReturnCondition.DamagedHeavy => Math.Round(price * heavyRate, 0),
            _ => 0m
        };
    }
}
