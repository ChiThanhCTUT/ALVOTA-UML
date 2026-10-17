namespace Library.Domain.Common;

/// <summary>
/// Các hằng số mặc định cho quy tắc nghiệp vụ theo SRS mục 4.
/// Mọi giá trị có thể cấu hình lại trong bảng Setting (SYS-01).
/// </summary>
public static class BusinessRuleConstants
{
    /// <summary>BR-01: Hạn mức mượn sách cho học sinh</summary>
    public const int StudentBorrowLimit = 3;

    /// <summary>BR-02: Hạn mức mượn sách cho giáo viên</summary>
    public const int TeacherBorrowLimit = 10;

    /// <summary>BR-03: Thời hạn mượn sách học sinh (ngày)</summary>
    public const int StudentLoanDurationDays = 14;

    /// <summary>BR-03: Thời hạn mượn sách giáo viên (ngày)</summary>
    public const int TeacherLoanDurationDays = 30;

    /// <summary>BR-04: Số lần gia hạn tối đa</summary>
    public const int MaxRenewalCount = 2;

    /// <summary>BR-04: Số ngày gia hạn thêm mỗi lần</summary>
    public const int RenewalDays = 7;

    /// <summary>BR-05: Mức phạt trễ hạn mỗi ngày mỗi cuốn (VNĐ)</summary>
    public const decimal DailyLateFineRate = 1000m;

    /// <summary>BR-07: Phí xử lý cố định khi mất sách (VNĐ)</summary>
    public const decimal LostBookProcessingFee = 20000m;

    /// <summary>BR-08: Tỷ lệ phạt hỏng nhẹ theo giá bìa</summary>
    public const decimal LightDamageRate = 0.20m;

    /// <summary>BR-08: Tỷ lệ phạt hỏng nặng theo giá bìa</summary>
    public const decimal HeavyDamageRate = 0.50m;

    /// <summary>BR-09: Ngưỡng nợ phạt chặn quyền mượn sách (VNĐ)</summary>
    public const decimal DebtThresholdToBlockBorrowing = 20000m;

    /// <summary>BR-10: Số ngày giữ chỗ đặt trước sau khi có sách trả</summary>
    public const int ReservationHoldDays = 3;

    /// <summary>AUTH-03: Số lần đăng nhập sai tối đa trước khi khóa tài khoản</summary>
    public const int MaxFailedLoginAttempts = 5;

    /// <summary>AUTH-03: Thời gian khóa tài khoản tạm thời (phút)</summary>
    public const int AccountLockoutMinutes = 5;

    /// <summary>AUTH-06: Thời gian tự động đăng xuất khi không thao tác (phút)</summary>
    public const int InactivityAutoLogoutMinutes = 15;
}
