namespace Library.Domain.Common;

public static class DomainErrors
{
    public static class Auth
    {
        public static readonly Error AccountLocked =
            new("AUTH.ACCOUNT_LOCKED", "Tài khoản đang bị tạm khóa. Vui lòng thử lại sau 5 phút.");
        public static readonly Error AccountDisabled =
            new("AUTH.ACCOUNT_DISABLED", "Tài khoản đã bị vô hiệu hóa. Vui lòng liên hệ quản trị viên.");
        public static readonly Error InvalidCredentials =
            new("AUTH.INVALID_CREDENTIALS", "Tên đăng nhập hoặc mật khẩu không chính xác.");
        public static readonly Error UserNotFound =
            new("AUTH.USER_NOT_FOUND", "Không tìm thấy người dùng.");
        public static readonly Error Unauthorized =
            new("AUTH.UNAUTHORIZED", "Bạn không có quyền thực hiện chức năng này.");
        public static readonly Error DuplicateUsername =
            new("AUTH.DUPLICATE_USERNAME", "Tên đăng nhập đã tồn tại trong hệ thống.");
    }

    public static class Catalog
    {
        public static readonly Error BookNotFound =
            new("CAT.BOOK_NOT_FOUND", "Không tìm thấy đầu sách.");
        public static readonly Error CopyNotFound =
            new("CAT.COPY_NOT_FOUND", "Không tìm thấy bản sao sách.");
        public static readonly Error DuplicateBarcode =
            new("CAT.DUPLICATE_BARCODE", "Mã vạch bản sao sách đã tồn tại.");
        public static readonly Error DuplicateIsbn =
            new("CAT.DUPLICATE_ISBN", "Mã ISBN đã tồn tại cho một đầu sách khác.");
        public static readonly Error HasActiveLoans =
            new("CAT.HAS_ACTIVE_LOANS", "Không thể xóa đầu sách khi vẫn còn bản sao đang được mượn.");
        public static readonly Error CopyUnavailable =
            new("CAT.COPY_UNAVAILABLE", "Bản sao không ở trạng thái sẵn sàng.");
        public static readonly Error CategoryNotFound =
            new("CAT.CATEGORY_NOT_FOUND", "Không tìm thấy thể loại.");
        public static readonly Error AuthorNotFound =
            new("CAT.AUTHOR_NOT_FOUND", "Không tìm thấy tác giả.");
        public static readonly Error PublisherNotFound =
            new("CAT.PUBLISHER_NOT_FOUND", "Không tìm thấy nhà xuất bản.");
    }

    public static class Member
    {
        public static readonly Error NotFound =
            new("MEM.NOT_FOUND", "Không tìm thấy bạn đọc.");
        public static readonly Error DuplicateCardNumber =
            new("MEM.DUPLICATE_CARD_NUMBER", "Mã thẻ bạn đọc đã tồn tại.");
        public static readonly Error CardBlocked =
            new("MEM.CARD_BLOCKED", "Thẻ bạn đọc đang bị tạm khóa.");
        public static readonly Error CardExpired =
            new("MEM.CARD_EXPIRED", "Thẻ bạn đọc đã hết hạn.");
        public static readonly Error HasActiveLoans =
            new("MEM.HAS_ACTIVE_LOANS", "Không thể khóa hoặc xóa bạn đọc còn sách đang mượn.");
    }

    public static class Circulation
    {
        public static readonly Error MemberBlocked =
            new("CIR.MEMBER_BLOCKED", "Thẻ bạn đọc đang bị khóa hoặc đã hết hạn.");
        public static readonly Error LimitReached =
            new("CIR.LIMIT_REACHED", "Bạn đọc đã đạt giới hạn số sách được mượn cùng lúc.");
        public static readonly Error HasOverdue =
            new("CIR.HAS_OVERDUE", "Bạn đọc đang có sách mượn quá hạn chưa trả.");
        public static readonly Error CopyUnavailable =
            new("CIR.COPY_UNAVAILABLE", "Bản sao sách hiện không ở trạng thái sẵn sàng để mượn.");
        public static readonly Error RenewNotAllowed =
            new("CIR.RENEW_NOT_ALLOWED", "Không đủ điều kiện gia hạn (đã quá hạn, hết lượt gia hạn hoặc có người đặt trước).");
        public static readonly Error CannotBorrowReference =
            new("CIR.CANNOT_BORROW_REFERENCE", "Sách thuộc diện chỉ đọc tại chỗ, không được mượn về.");
        public static readonly Error LoanItemNotFound =
            new("CIR.LOAN_ITEM_NOT_FOUND", "Không tìm thấy bản ghi mượn sách tương ứng.");
        public static readonly Error AlreadyReturned =
            new("CIR.ALREADY_RETURNED", "Bản sao sách này đã được trả.");
        public static readonly Error ReservationConflict =
            new("CIR.RESERVATION_CONFLICT", "Bản sao sách đang được giữ cho người đã đặt trước.");
        public static readonly Error ConcurrencyConflict =
            new("CIR.CONCURRENCY_CONFLICT", "Dữ liệu vừa được cập nhật bởi thao tác khác. Vui lòng tải lại.");
    }

    public static class Fine
    {
        public static readonly Error DebtExceeded =
            new("FIN.DEBT_EXCEEDED", "Bạn đọc nợ tiền phạt vượt quá ngưỡng cho phép (> 20.000 đ).");
        public static readonly Error NotFound =
            new("FIN.NOT_FOUND", "Không tìm thấy phiếu phạt.");
        public static readonly Error AlreadyPaid =
            new("FIN.ALREADY_PAID", "Phiếu phạt đã được thanh toán hoặc miễn giảm trước đó.");
        public static readonly Error ReasonRequiredForWaiver =
            new("FIN.REASON_REQUIRED_FOR_WAIVER", "Bắt buộc nhập lý do khi miễn giảm tiền phạt.");
        public static readonly Error InvalidPaymentAmount =
            new("FIN.INVALID_PAYMENT_AMOUNT", "Số tiền thanh toán không hợp lệ.");
    }

    public static class Inventory
    {
        public static readonly Error SessionNotFound =
            new("INV.SESSION_NOT_FOUND", "Không tìm thấy đợt kiểm kê.");
        public static readonly Error SessionClosed =
            new("INV.SESSION_CLOSED", "Đợt kiểm kê đã kết thúc hoặc bị hủy.");
    }

    public static class System
    {
        public static readonly Error SettingNotFound =
            new("SYS.SETTING_NOT_FOUND", "Không tìm thấy thiết lập hệ thống.");
        public static readonly Error BackupFailed =
            new("SYS.BACKUP_FAILED", "Sao lưu dữ liệu thất bại.");
        public static readonly Error RestoreFailed =
            new("SYS.RESTORE_FAILED", "Phục hồi dữ liệu thất bại.");
    }
}
