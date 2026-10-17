using Library.Domain.Common;
using Library.Domain.Enums;

namespace Library.Domain.Entities;

public sealed class Book : AuditableEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    public string? Isbn { get; set; }
    public int? PublicationYear { get; set; }
    public Guid CategoryId { get; set; }
    public Category? Category { get; set; }
    public Guid PublisherId { get; set; }
    public Publisher? Publisher { get; set; }
    public string Language { get; set; } = "Tiếng Việt";
    public int PageCount { get; set; }
    public decimal Price { get; set; }
    public string? CoverImagePath { get; set; }
    public string? Summary { get; set; }

    /// <summary>
    /// BR-11: Sách chỉ đọc tại chỗ, không cho mượn về nhà
    /// </summary>
    public bool IsReferenceOnly { get; set; }

    public ICollection<BookAuthor> BookAuthors { get; set; } = new List<BookAuthor>();
    public ICollection<BookCopy> Copies { get; set; } = new List<BookCopy>();
    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();

    /// <summary>
    /// CAT-07: Không cho xóa đầu sách còn bản sao đang mượn
    /// </summary>
    public bool CanBeDeleted => Copies.All(c => c.Status != CopyStatus.Borrowed);

    public int TotalCopies => Copies.Count(c => !c.IsDeleted);
    public int AvailableCopies => Copies.Count(c => !c.IsDeleted && c.Status == CopyStatus.Available);
}
