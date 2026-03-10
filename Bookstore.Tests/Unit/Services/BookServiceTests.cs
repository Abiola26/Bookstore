using Bookstore.Application.DTOs;
using Bookstore.Application.Repositories;
using Bookstore.Domain.Entities;
using Bookstore.Domain.ValueObjects;
using Bookstore.Infrastructure.Persistence;
using Bookstore.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Bookstore.Tests.Unit.Services;

public class BookServiceTests : IDisposable
{
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<ILogger<BookService>> _loggerMock;
    private readonly BookStoreDbContext _dbContext;
    private readonly BookService _service;

    public BookServiceTests()
    {
        _uowMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<BookService>>();

        var options = new DbContextOptionsBuilder<BookStoreDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;

        _dbContext = new BookStoreDbContext(options);
        _dbContext.Database.OpenConnection();
        _dbContext.Database.EnsureCreated();

        _service = new BookService(_uowMock.Object, _loggerMock.Object, _dbContext);
    }

    [Fact]
    public async Task GetBookByIdAsync_ShouldReturnBook_WhenBookExists()
    {
        // Arrange
        var bookId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var isbn = new ISBN("978-3-16-148410-0");
        var price = new Money(29.99m, "USD");
        var book = new Book("Test Book", "Description", isbn, price, "Author", 10, categoryId)
        {
            Id = bookId,
            Category = new Category("Test Category") { Id = categoryId }
        };

        _uowMock.Setup(x => x.Books.GetByIdAsync(bookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(book);

        // Act
        var result = await _service.GetBookByIdAsync(bookId);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Id.Should().Be(bookId);
        result.Data.Title.Should().Be("Test Book");
    }

    [Fact]
    public async Task GetBookByIdAsync_ShouldReturnNotFound_WhenBookDoesNotExist()
    {
        // Arrange
        var bookId = Guid.NewGuid();
        _uowMock.Setup(x => x.Books.GetByIdAsync(bookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Book?)null);

        // Act
        var result = await _service.GetBookByIdAsync(bookId);

        // Assert
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(404);
        result.Message.Should().Be("Book not found");
    }

    [Fact]
    public async Task CreateBookAsync_ShouldCreateBook_WhenDataIsValid()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var dto = new BookCreateDto
        {
            Title = "New Book",
            Description = "Description",
            ISBN = "978-3-16-148410-0",
            Author = "Author",
            Price = 19.99m,
            Currency = "USD",
            TotalQuantity = 5,
            CategoryId = categoryId
        };

        _uowMock.Setup(x => x.Books.ISBNExistsAsync(dto.ISBN, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _uowMock.Setup(x => x.Categories.GetByIdAsync(categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Category("Category"));

        // Act
        var result = await _service.CreateBookAsync(dto);

        // Assert
        result.Success.Should().BeTrue();
        result.StatusCode.Should().Be(201);
        result.Data.Should().NotBeNull();
        _uowMock.Verify(x => x.Books.AddAsync(It.IsAny<Book>(), It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateBookAsync_ShouldReturnConflict_WhenISBNExists()
    {
        // Arrange
        var dto = new BookCreateDto 
        { 
            ISBN = "123", 
            Title = "Title", 
            Description = "Description",
            Author = "Author", 
            Price = 1, 
            Currency = "USD", 
            CategoryId = Guid.NewGuid() 
        };
        _uowMock.Setup(x => x.Books.ISBNExistsAsync(dto.ISBN, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.CreateBookAsync(dto);

        // Assert
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(409);
        result.Message.Should().Be("ISBN already exists");
    }

    [Fact]
    public async Task DeleteBookAsync_ShouldDeleteBook_WhenBookExists()
    {
        // Arrange
        var bookId = Guid.NewGuid();
        var book = new Book("T", "D", new ISBN("1234567890"), new Money(1, "USD"), "A", 1, Guid.NewGuid()) { Id = bookId };
        
        _uowMock.Setup(x => x.Books.GetByIdAsync(bookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(book);

        // Act
        var result = await _service.DeleteBookAsync(bookId);

        // Assert
        result.Success.Should().BeTrue();
        _uowMock.Verify(x => x.Books.Delete(book), Times.Once);
        _uowMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    public void Dispose()
    {
        _dbContext.Database.CloseConnection();
        _dbContext.Dispose();
    }
    [Fact]
    public async Task UpdateBookAsync_ShouldUpdateTitle_WhenValid()
    {
        // Arrange
        var bookId = Guid.NewGuid();
        var book = new Book("Old Title", "D", new ISBN("123"), new Money(1, "USD"), "A", 1, Guid.NewGuid()) { Id = bookId };
        var dto = new BookUpdateDto { Title = "New Title" };

        _uowMock.Setup(x => x.Books.GetByIdAsync(bookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(book);

        // Act
        var result = await _service.UpdateBookAsync(bookId, dto);

        // Assert
        result.Success.Should().BeTrue();
        book.Title.Should().Be("New Title");
        _uowMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllBooksAsync_ShouldReturnList()
    {
        // Arrange
        var books = new List<Book> { new Book("T", "D", new ISBN("123"), new Money(1, "USD"), "A", 1, Guid.NewGuid()) };
        _uowMock.Setup(x => x.Books.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(books);

        // Act
        var result = await _service.GetAllBooksAsync();

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(1);
    }
}
