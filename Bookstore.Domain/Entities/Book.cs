using Bookstore.Domain.ValueObjects;
using System;
using System.Collections.Generic;

namespace Bookstore.Domain.Entities;

public class Book : BaseEntity
{
    private readonly List<OrderItem> _orderItems = new();

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ISBN ISBN { get; set; } = null!;
    public string? Publisher { get; set; }
    public DateTime? PublicationDate { get; set; }
    public Money Price { get; set; } = null!;
    public string Author { get; set; } = string.Empty;
    public int Pages { get; set; }
    public string? Language { get; set; }
    public string? CoverImageUrl { get; set; }
    public int TotalQuantity { get; set; }

    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public IReadOnlyCollection<OrderItem> OrderItems => _orderItems.AsReadOnly();

    private Book() { }

    public Book(
        string title,
        string description,
        ISBN isbn,
        Money price,
        string author,
        int totalQuantity,
        Guid categoryId)
    {
        if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Title is required", nameof(title));
        if (string.IsNullOrWhiteSpace(description)) throw new ArgumentException("Description is required", nameof(description));
        if (totalQuantity < 0) throw new ArgumentOutOfRangeException(nameof(totalQuantity));

        Title = title;
        Description = description;
        ISBN = isbn ?? throw new ArgumentNullException(nameof(isbn));
        Price = price ?? throw new ArgumentNullException(nameof(price));
        Author = author ?? throw new ArgumentNullException(nameof(author));
        TotalQuantity = totalQuantity;
        CategoryId = categoryId;
    }
}

