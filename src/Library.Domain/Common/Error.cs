namespace Library.Domain.Common;

public sealed record Error(string Code, string Message)
{
    public static readonly Error None = new(string.Empty, string.Empty);
    public static readonly Error NullValue = new("ERROR.NULL_VALUE", "Giá trị không được để trống.");
}
