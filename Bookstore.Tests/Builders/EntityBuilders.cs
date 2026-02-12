using Bookstore.Domain.Entities;
using Bookstore.Domain.ValueObjects;

namespace Bookstore.Tests.Builders;

/// <summary>
/// Builder for creating test Book entities
/// </summary>
public class BookBuilder
{
    private string _title = "Test Book";
    private string _description = "Test Description";
    private ISBN _isbn = new("978-3-16-148410-0");
    private Money _price = new(29.99m, "USD");
    private string _author = "Test Author";
    private int _totalQuantity = 10;
    private Guid _categoryId = Guid.NewGuid();
    private string? _publisher = "Test Publisher";
    private DateTime? _publicationDate = DateTime.Now;
    private int _pages = 300;
    private string? _language = "English";
    private string? _coverImageUrl = "https://example.com/cover.jpg";

    public BookBuilder WithTitle(string title)
    {
        _title = title;
        return this;
    }

    public BookBuilder WithDescription(string description)
    {
        _description = description;
        return this;
    }

    public BookBuilder WithISBN(string isbn)
    {
        _isbn = new ISBN(isbn);
        return this;
    }

    public BookBuilder WithPrice(decimal amount, string currency = "USD")
    {
        _price = new Money(amount, currency);
        return this;
    }

    public BookBuilder WithAuthor(string author)
    {
        _author = author;
        return this;
    }

    public BookBuilder WithTotalQuantity(int quantity)
    {
        _totalQuantity = quantity;
        return this;
    }

    public BookBuilder WithCategoryId(Guid categoryId)
    {
        _categoryId = categoryId;
        return this;
    }

    public BookBuilder WithPublisher(string publisher)
    {
        _publisher = publisher;
        return this;
    }

    public BookBuilder WithPublicationDate(DateTime publicationDate)
    {
        _publicationDate = publicationDate;
        return this;
    }

    public BookBuilder WithPages(int pages)
    {
        _pages = pages;
        return this;
    }

    public BookBuilder WithLanguage(string language)
    {
        _language = language;
        return this;
    }

    public BookBuilder WithCoverImageUrl(string coverImageUrl)
    {
        _coverImageUrl = coverImageUrl;
        return this;
    }

    public Book Build()
    {
        return new Book(
            _title,
            _description,
            _isbn,
            _price,
            _author,
            _totalQuantity,
            _categoryId)
        {
            Publisher = _publisher,
            PublicationDate = _publicationDate,
            Pages = _pages,
            Language = _language,
            CoverImageUrl = _coverImageUrl
        };
    }
}

/// <summary>
/// Builder for creating test Category entities
/// </summary>
public class CategoryBuilder
{
    private string _name = "Test Category";

    public CategoryBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public Category Build()
    {
        return new Category(_name);
    }
}

/// <summary>
/// Builder for creating test User entities
/// </summary>
public class UserBuilder
{
    private string _fullName = "Test User";
    private string _email = "test@example.com";
    private string _passwordHash = BCrypt.Net.BCrypt.HashPassword("TestPassword123!");
    private UserRole _role = UserRole.User;
    private string? _phoneNumber = "+1234567890";

    public UserBuilder WithFullName(string fullName)
    {
        _fullName = fullName;
        return this;
    }

    public UserBuilder WithEmail(string email)
    {
        _email = email;
        return this;
    }

    public UserBuilder WithPasswordHash(string passwordHash)
    {
        _passwordHash = passwordHash;
        return this;
    }

    public UserBuilder WithRole(UserRole role)
    {
        _role = role;
        return this;
    }

    public UserBuilder WithPhoneNumber(string phoneNumber)
    {
        _phoneNumber = phoneNumber;
        return this;
    }

    public User Build()
    {
        return new User(_fullName, _email, _passwordHash, _role)
        {
            PhoneNumber = _phoneNumber
        };
    }
}

/// <summary>
/// Builder for creating test Order entities
/// </summary>
public class OrderBuilder
{
    private Guid _userId = Guid.NewGuid();
    private OrderStatus _status = OrderStatus.Pending;

    public OrderBuilder WithUserId(Guid userId)
    {
        _userId = userId;
        return this;
    }

    public OrderBuilder WithStatus(OrderStatus status)
    {
        _status = status;
        return this;
    }

    public Order Build()
    {
        return new Order(_userId)
        {
            Status = _status
        };
    }
}

/// <summary>
/// Builder for creating test OrderItem entities
/// </summary>
public class OrderItemBuilder
{
    private Guid _orderId = Guid.NewGuid();
    private Guid _bookId = Guid.NewGuid();
    private Book? _book;
    private int _quantity = 1;
    private Money _unitPrice = new(29.99m, "USD");

    public OrderItemBuilder WithOrderId(Guid orderId)
    {
        _orderId = orderId;
        return this;
    }

    public OrderItemBuilder WithBookId(Guid bookId)
    {
        _bookId = bookId;
        return this;
    }

    public OrderItemBuilder WithBook(Book book)
    {
        _book = book;
        _bookId = book.Id;
        return this;
    }

    public OrderItemBuilder WithQuantity(int quantity)
    {
        _quantity = quantity;
        return this;
    }

    public OrderItemBuilder WithUnitPrice(Money unitPrice)
    {
        _unitPrice = unitPrice;
        return this;
    }

    public OrderItem Build()
    {
        var orderItem = new OrderItem(_orderId, _bookId, _quantity, _unitPrice);
        return orderItem;
    }
}
