namespace Library.Domain.Enums;

public enum MemberType
{
    Student = 1,
    Teacher = 2
}

public enum CardStatus
{
    Active = 1,
    TemporarilyLocked = 2,
    Expired = 3
}

public enum CopyStatus
{
    Available = 1,
    Borrowed = 2,
    Reserved = 3,
    Damaged = 4,
    Lost = 5,
    Disposed = 6
}

public enum LoanStatus
{
    Active = 1,
    Returned = 2,
    Overdue = 3
}

public enum ReturnCondition
{
    Normal = 1,
    DamagedLight = 2,
    DamagedHeavy = 3,
    Lost = 4
}

public enum ReservationStatus
{
    Pending = 1,
    Notified = 2,
    Completed = 3,
    Cancelled = 4,
    Expired = 5
}

public enum FineStatus
{
    Pending = 1,
    Paid = 2,
    Waived = 3
}

public enum FineReason
{
    Overdue = 1,
    DamagedLight = 2,
    DamagedHeavy = 3,
    Lost = 4
}

public enum SystemRole
{
    Admin = 1,
    Librarian = 2,
    Assistant = 3
}

public enum InventorySessionStatus
{
    InProgress = 1,
    Completed = 2,
    Cancelled = 3
}

public enum InventoryResultStatus
{
    Normal = 1,
    Missing = 2,
    Found = 3,
    Damaged = 4
}
