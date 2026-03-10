using Bookstore.Domain.Entities;
using Bookstore.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Bookstore.Tests.Unit.Domain.Entities;

public class BookTests
{
    [Fact]
    public void Constructor_ShouldInitializeProperties_WhenValidArguments()
    {
        // Arrange
        var title = "Title";
        var description = "Description";
        var isbn = new ISBN("1234567890");
        var price = new Money(10, "USD");
        var author = "Author";
        var quantity = 5;
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
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Constructor_ShouldThrowArgumentException_WhenTitleIsEmpty(string? title)
    {
        // Arrange
        var description = "Description";
        var isbn = new ISBN("1234567890");
        var price = new Money(10, "USD");
        var author = "Author";
        var quantity = 5;
        var categoryId = Guid.NewGuid();

        // Act
        Action act = () => new Book(title!, description, isbn, price, author, quantity, categoryId);

        // Assert
        act.Should().Throw<ArgumentException>().WithMessage("*Title*");
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentOutOfRangeException_WhenQuantityIsNegative()
    {
        // Arrange
        Action act = () => new Book("T", "D", new ISBN("123"), new Money(1, "USD"), "A", -1, Guid.NewGuid());

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
