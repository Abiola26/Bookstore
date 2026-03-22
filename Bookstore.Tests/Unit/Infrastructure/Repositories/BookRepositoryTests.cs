using Bookstore.Domain.Entities;
using Bookstore.Domain.ValueObjects;
using Bookstore.Infrastructure.Persistence.Repositories;
using Bookstore.Tests.Fixtures;
using FluentAssertions;

namespace Bookstore.Tests.Unit.Infrastructure.Repositories;

public class BookRepositoryTests : IClassFixture<TestDatabaseFixture>
{
    private readonly TestDatabaseFixture _fixture;

    public BookRepositoryTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnBook()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var repo = new BookRepository(context);
        var category = new Category("Test Cat");
        context.Categories.Add(category);
        var book = new Book("T", "D", new ISBN("123"), new Money(1, "USD"), "A", 1, category.Id);
        context.Books.Add(book);
        await context.SaveChangesAsync();

        // Act
        var result = await repo.GetByIdAsync(book.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Title.Should().Be("T");
    }

    [Fact]
    public async Task ISBNExistsAsync_ShouldReturnTrue_WhenExists()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var repo = new BookRepository(context);
        var isbn = "978-0123-456";
        var category = new Category("Test Cat 2");
        context.Categories.Add(category);
        var book = new Book("T", "D", new ISBN(isbn), new Money(1, "USD"), "A", 1, category.Id);
        context.Books.Add(book);
        await context.SaveChangesAsync();

        // Act
        var exists = await repo.ISBNExistsAsync(isbn);

        // Assert
        exists.Should().BeTrue();
    }
}
