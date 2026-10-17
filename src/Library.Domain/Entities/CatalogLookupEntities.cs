using Library.Domain.Common;

namespace Library.Domain.Entities;

public sealed class Author : AuditableEntity
{
    public string FullName { get; set; } = string.Empty;
    public string? Biography { get; set; }
    public ICollection<BookAuthor> BookAuthors { get; set; } = new List<BookAuthor>();
}

public sealed class Category : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ICollection<Book> Books { get; set; } = new List<Book>();
}

public sealed class Publisher : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? ContactNumber { get; set; }
    public ICollection<Book> Books { get; set; } = new List<Book>();
}

public sealed class BookAuthor
{
    public Guid BookId { get; set; }
    public Book Book { get; set; } = null!;

    public Guid AuthorId { get; set; }
    public Author Author { get; set; } = null!;
}
