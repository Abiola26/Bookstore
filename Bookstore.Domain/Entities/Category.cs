using System.Collections.Generic;

namespace Bookstore.Domain.Entities;

public class Category : BaseEntity
{
    private readonly List<Book> _books = new();

    public string Name { get; set; } = string.Empty;

    public IReadOnlyCollection<Book> Books => _books.AsReadOnly();

    private Category() { }

    public Category(string name)
    {
        Name = name;
    }
}
