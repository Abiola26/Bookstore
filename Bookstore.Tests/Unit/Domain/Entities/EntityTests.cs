using Bookstore.Domain.Entities;
using Bookstore.Domain.Enum;
using Bookstore.Domain.ValueObjects;
using Bookstore.Tests.Builders;
using FluentAssertions;
using Xunit;

namespace Bookstore.Tests.Unit.Domain.Entities;

/// <summary>
/// Unit tests for Book entity
/// Tests business logic and validation rules
/// </summary>
public class BookEntityTests
{
    [Fact]
    public void CreateBook_WithValidData_ShouldSucceed()
    {
        // Arrange
        var title = "The Great Gatsby";
        var description = "A classic American novel";
        var isbn = new ISBN("978-0-7432-7356-5");
        var price = new Money(15.99m, "USD");
        var author = "F. Scott Fitzgerald";
        var quantity = 50;
        var categoryId = Guid.NewGuid();

        // Act
        var book = new Book(title, description, isbn, price, author, quantity, categoryId);

        // Assert
        book.Title.Should().Be(title);
        book.Description.Should().Be(description);
        book.ISBN.Should().Be(isbn);
        book.Price.Should().Be(price);
        book.Author.Should().Be(author);
        book.TotalQuantity.Should().Be(quantity);
        book.CategoryId.Should().Be(categoryId);
        book.Id.Should().NotBeEmpty();
        book.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        book.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void CreateBook_WithNullTitle_ShouldThrow()
    {
        // Arrange
        var builder = new BookBuilder();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            new Book(
                null!,
                builder.Build().Description,
                new ISBN("978-0-7432-7356-5"),
                new Money(15.99m, "USD"),
                "Author",
                10,
                Guid.NewGuid()));

        exception.Message.Should().Contain("Title is required");
    }

    [Fact]
    public void CreateBook_WithNegativeQuantity_ShouldThrow()
    {
        // Arrange & Act & Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Book(
                "Title",
                "Description",
                new ISBN("978-0-7432-7356-5"),
                new Money(15.99m, "USD"),
                "Author",
                -5,  // Negative quantity
                Guid.NewGuid()));

        exception.ParamName.Should().Be("totalQuantity");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateBook_WithInvalidDescription_ShouldThrow(string description)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            new Book(
                "Title",
                description,
                new ISBN("978-0-7432-7356-5"),
                new Money(15.99m, "USD"),
                "Author",
                10,
                Guid.NewGuid()));

        exception.Message.Should().Contain("Description is required");
    }

    [Fact]
    public void BuildBook_WithBuilder_ShouldSucceed()
    {
        // Arrange & Act
        var book = new BookBuilder()
            .WithTitle("1984")
            .WithAuthor("George Orwell")
            .WithPrice(18.99m, "USD")
            .WithTotalQuantity(100)
            .Build();

        // Assert
        book.Title.Should().Be("1984");
        book.Author.Should().Be("George Orwell");
        book.Price.Amount.Should().Be(18.99m);
        book.Price.Currency.Should().Be("USD");
        book.TotalQuantity.Should().Be(100);
    }

    [Fact]
    public void Book_WithOptionalFields_ShouldSetCorrectly()
    {
        // Arrange & Act
        var book = new BookBuilder()
            .WithPublisher("Penguin Books")
            .Build();

        book.Publisher = "Penguin Books";
        book.Language = "English";
        book.Pages = 328;
        book.CoverImageUrl = "https://example.com/1984.jpg";

        // Assert
        book.Publisher.Should().Be("Penguin Books");
        book.Language.Should().Be("English");
        book.Pages.Should().Be(328);
        book.CoverImageUrl.Should().Be("https://example.com/1984.jpg");
    }

    [Fact]
    public void Book_WithNullPriceOrISBN_ShouldThrow()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new Book("Title", "Description", null!, new Money(15.99m, "USD"), "Author", 10, Guid.NewGuid()));

        exception.ParamName.Should().Be("isbn");
    }

    [Fact]
    public void Book_UpdateAuditFields_ShouldWork()
    {
        // Arrange
        var book = new BookBuilder().Build();
        var originalCreatedAt = book.CreatedAt;

        // Act
        book.Touch();

        // Assert
        book.UpdatedAt.Should().BeAfter(originalCreatedAt);
        book.CreatedAt.Should().Be(originalCreatedAt);
    }
}

/// <summary>
/// Unit tests for Category entity
/// </summary>
public class CategoryEntityTests
{
    [Fact]
    public void CreateCategory_WithValidName_ShouldSucceed()
    {
        // Arrange
        var name = "Science Fiction";

        // Act
        var category = new Category(name);

        // Assert
        category.Name.Should().Be(name);
        category.Id.Should().NotBeEmpty();
        category.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void CreateCategory_WithEmptyName_ShouldThrow()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new Category(""));
        exception.Message.Should().Contain("Name is required");
    }

    [Fact]
    public void BuildCategory_WithBuilder_ShouldSucceed()
    {
        // Arrange & Act
        var category = new CategoryBuilder()
            .WithName("Fantasy")
            .Build();

        // Assert
        category.Name.Should().Be("Fantasy");
    }
}

/// <summary>
/// Unit tests for User entity
/// </summary>
public class UserEntityTests
{
    [Fact]
    public void CreateUser_WithValidData_ShouldSucceed()
    {
        // Arrange
        var fullName = "John Doe";
        var email = "john@example.com";
        var passwordHash = BCrypt.Net.BCrypt.HashPassword("SecurePassword123!");
        var role = UserRole.User;

        // Act
        var user = new User(fullName, email, passwordHash, role);

        // Assert
        user.FullName.Should().Be(fullName);
        user.Email.Should().Be(email);
        user.PasswordHash.Should().Be(passwordHash);
        user.Role.Should().Be(role);
        user.Id.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void CreateUser_WithInvalidEmail_ShouldThrow(string? email)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            new User("Name", email!, BCrypt.Net.BCrypt.HashPassword("Pass123!"), UserRole.User));

        exception.Message.Should().Contain("Email is required");
    }

    [Fact]
    public void BuildUser_WithBuilder_ShouldSucceed()
    {
        // Arrange & Act
        var user = new UserBuilder()
            .WithFullName("Jane Smith")
            .WithEmail("jane@example.com")
            .WithRole(UserRole.Admin)
            .Build();

        // Assert
        user.FullName.Should().Be("Jane Smith");
        user.Email.Should().Be("jane@example.com");
        user.Role.Should().Be(UserRole.Admin);
    }
}

/// <summary>
/// Unit tests for Money value object
/// </summary>
public class MoneyValueObjectTests
{
    [Fact]
    public void CreateMoney_WithValidData_ShouldSucceed()
    {
        // Arrange & Act
        var money = new Money(99.99m, "USD");

        // Assert
        money.Amount.Should().Be(99.99m);
        money.Currency.Should().Be("USD");
    }

    [Fact]
    public void Money_EqualityComparison_ShouldWork()
    {
        // Arrange
        var money1 = new Money(100m, "USD");
        var money2 = new Money(100m, "USD");
        var money3 = new Money(50m, "USD");

        // Act & Assert
        money1.Should().Be(money2);
        money1.Should().NotBe(money3);
    }

    [Fact]
    public void Money_WithNegativeAmount_ShouldThrow()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Money(-50m, "USD"));
    }
}

/// <summary>
/// Unit tests for ISBN value object
/// </summary>
public class ISBNValueObjectTests
{
    [Fact]
    public void CreateISBN_WithValidISBN13_ShouldSucceed()
    {
        // Arrange
        var isbnString = "978-0-7432-7356-5";

        // Act
        var isbn = new ISBN(isbnString);

        // Assert
        isbn.Value.Should().Contain("978");
    }

    [Fact]
    public void ISBN_ToString_ShouldReturnFormattedString()
    {
        // Arrange
        var isbn = new ISBN("978-0-7432-7356-5");

        // Act & Assert
        isbn.ToString().Should().NotBeEmpty();
    }

    [Fact]
    public void ISBN_Equality_ShouldWork()
    {
        // Arrange
        var isbn1 = new ISBN("978-0-7432-7356-5");
        var isbn2 = new ISBN("978-0-7432-7356-5");
        var isbn3 = new ISBN("978-0-545-01022-1");

        // Act & Assert
        (isbn1 == isbn2).Should().BeTrue();
        (isbn1 == isbn3).Should().BeFalse();
    }
}

/// <summary>
/// Unit tests for WishlistItem entity
/// </summary>
public class WishlistItemEntityTests
{
    [Fact]
    public void CreateWishlistItem_WithValidData_ShouldSucceed()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var bookId = Guid.NewGuid();

        // Act
        var item = new WishlistItem(userId, bookId);

        // Assert
        item.UserId.Should().Be(userId);
        item.BookId.Should().Be(bookId);
        item.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void CreateWishlistItem_TwoItems_ShouldHaveUniqueIds()
    {
        // Arrange & Act
        var item1 = new WishlistItem(Guid.NewGuid(), Guid.NewGuid());
        var item2 = new WishlistItem(Guid.NewGuid(), Guid.NewGuid());

        // Assert
        item1.Id.Should().NotBe(item2.Id);
    }
}

/// <summary>
/// Unit tests for Review entity
/// </summary>
public class ReviewEntityTests
{
    [Fact]
    public void CreateReview_WithValidData_ShouldSucceed()
    {
        // Arrange
        var bookId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        const int rating = 4;
        const string comment = "Very good read!";

        // Act
        var review = new Review(bookId, userId, rating, comment);

        // Assert
        review.BookId.Should().Be(bookId);
        review.UserId.Should().Be(userId);
        review.Rating.Should().Be(rating);
        review.Comment.Should().Be(comment);
        review.Id.Should().NotBeEmpty();
        review.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Theory]
    [InlineData(0)]   // below minimum
    [InlineData(6)]   // above maximum
    [InlineData(-1)]  // negative
    public void CreateReview_WithInvalidRating_ShouldThrow(int rating)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Review(Guid.NewGuid(), Guid.NewGuid(), rating, "Comment"));

        exception.ParamName.Should().Be("rating");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateReview_WithEmptyComment_ShouldThrow(string comment)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            new Review(Guid.NewGuid(), Guid.NewGuid(), 3, comment));

        exception.ParamName.Should().Be("comment");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    public void CreateReview_WithBoundaryRatings_ShouldSucceed(int rating)
    {
        // Act
        var review = new Review(Guid.NewGuid(), Guid.NewGuid(), rating, "A review");

        // Assert
        review.Rating.Should().Be(rating);
    }
}
