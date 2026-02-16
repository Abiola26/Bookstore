using Bookstore.Infrastructure.Persistence;
using Bookstore.Infrastructure.Persistence.Repositories;
using Bookstore.Tests.Builders;
using Bookstore.Tests.Fixtures;
using FluentAssertions;
using Xunit;

namespace Bookstore.Tests.Unit.Infrastructure.Repositories;

/// <summary>
/// Integration tests for BookRepository using in-memory database
/// </summary>
[Collection("Database collection")]
public class BookRepositoryTests
{
    private readonly TestDatabaseFixture _fixture;
    private readonly BookRepository _repository;

    public BookRepositoryTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
        _repository = new BookRepository(_fixture.Context);
    }

    [Fact]
    public async Task GetByIdAsync_WithValidId_ShouldReturnBook()
    {
        // Arrange
        var category = new CategoryBuilder().Build();
        await _fixture.Context.Categories.AddAsync(category);
        await _fixture.Context.SaveChangesAsync();

        var book = new BookBuilder()
            .WithCategoryId(category.Id)
            .WithTitle("Test Book")
            .Build();

        await _fixture.Context.Books.AddAsync(book);
        await _fixture.Context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(book.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Title.Should().Be("Test Book");
        result.Id.Should().Be(book.Id);
    }

    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ShouldReturnNull()
    {
        // Act
        var result = await _repository.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllBooks()
    {
        // Arrange
        var category = new CategoryBuilder().Build();
        await _fixture.Context.Categories.AddAsync(category);
        await _fixture.Context.SaveChangesAsync();

        var books = new[]
        {
            new BookBuilder().WithCategoryId(category.Id).WithTitle("Book 1").Build(),
            new BookBuilder().WithCategoryId(category.Id).WithTitle("Book 2").Build(),
            new BookBuilder().WithCategoryId(category.Id).WithTitle("Book 3").Build()
        };

        await _fixture.Context.Books.AddRangeAsync(books);
        await _fixture.Context.SaveChangesAsync();

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().HaveCountGreaterThanOrEqualTo(3);
    }

    [Fact]
    public async Task AddAsync_WithValidBook_ShouldSucceed()
    {
        // Arrange
        var category = new CategoryBuilder().Build();
        await _fixture.Context.Categories.AddAsync(category);
        await _fixture.Context.SaveChangesAsync();

        var book = new BookBuilder()
            .WithCategoryId(category.Id)
            .WithTitle("New Book")
            .Build();

        // Act
        await _repository.AddAsync(book);
        await _fixture.Context.SaveChangesAsync();

        // Assert
        var result = await _repository.GetByIdAsync(book.Id);
        result.Should().NotBeNull();
        result!.Title.Should().Be("New Book");
    }

    [Fact]
    public async Task UpdateAsync_WithValidBook_ShouldSucceed()
    {
        // Arrange
        var category = new CategoryBuilder().Build();
        await _fixture.Context.Categories.AddAsync(category);
        await _fixture.Context.SaveChangesAsync();

        var book = new BookBuilder().WithCategoryId(category.Id).Build();
        await _fixture.Context.Books.AddAsync(book);
        await _fixture.Context.SaveChangesAsync();

        // Act
        book.Title = "Updated Title";
        _repository.Update(book);
        await _fixture.Context.SaveChangesAsync();

        // Assert
        var result = await _repository.GetByIdAsync(book.Id);
        result!.Title.Should().Be("Updated Title");
    }

    [Fact]
    public async Task DeleteAsync_WithValidId_ShouldSoftDelete()
    {
        // Arrange
        var category = new CategoryBuilder().Build();
        await _fixture.Context.Categories.AddAsync(category);
        await _fixture.Context.SaveChangesAsync();

        var book = new BookBuilder().WithCategoryId(category.Id).Build();
        await _fixture.Context.Books.AddAsync(book);
        await _fixture.Context.SaveChangesAsync();

        // Act
        book.IsDeleted = true;  // Soft delete
        _repository.Update(book);
        await _fixture.Context.SaveChangesAsync();

        // Assert
        var result = await _repository.GetByIdAsync(book.Id);
        result.Should().BeNull(); // Soft delete filters out
    }
}

/// <summary>
/// Integration tests for CategoryRepository using in-memory database
/// </summary>
[Collection("Database collection")]
public class CategoryRepositoryTests
{
    private readonly TestDatabaseFixture _fixture;
    private readonly CategoryRepository _repository;

    public CategoryRepositoryTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
        _repository = new CategoryRepository(_fixture.Context);
    }

    [Fact]
    public async Task GetByIdAsync_WithValidId_ShouldReturnCategory()
    {
        // Arrange
        var category = new CategoryBuilder().WithName("Science Fiction").Build();
        await _fixture.Context.Categories.AddAsync(category);
        await _fixture.Context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(category.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Science Fiction");
    }

    [Fact]
    public async Task AddAsync_WithValidCategory_ShouldSucceed()
    {
        // Arrange
        var category = new CategoryBuilder().WithName("Fantasy").Build();

        // Act
        await _repository.AddAsync(category);
        await _fixture.Context.SaveChangesAsync();

        // Assert
        var result = await _repository.GetByIdAsync(category.Id);
        result.Should().NotBeNull();
        result!.Name.Should().Be("Fantasy");
    }
}

/// <summary>
/// Integration tests for UserRepository using in-memory database
/// </summary>
[Collection("Database collection")]
public class UserRepositoryTests
{
    private readonly TestDatabaseFixture _fixture;
    private readonly UserRepository _repository;

    public UserRepositoryTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
        _repository = new UserRepository(_fixture.Context);
    }

    [Fact]
    public async Task GetByIdAsync_WithValidId_ShouldReturnUser()
    {
        // Arrange
        var user = new UserBuilder()
            .WithEmail("test@example.com")
            .WithFullName("Test User")
            .Build();

        await _fixture.Context.Users.AddAsync(user);
        await _fixture.Context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(user.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Email.Should().Be("test@example.com");
    }

    [Fact]
    public async Task AddAsync_WithValidUser_ShouldSucceed()
    {
        // Arrange
        var user = new UserBuilder()
            .WithEmail("newuser@example.com")
            .Build();

        // Act
        await _repository.AddAsync(user);
        await _fixture.Context.SaveChangesAsync();

        // Assert
        var result = await _repository.GetByIdAsync(user.Id);
        result.Should().NotBeNull();
    }
}

/// <summary>
/// Integration tests for OrderRepository using in-memory database
/// </summary>
[Collection("Database collection")]
public class OrderRepositoryTests
{
    private readonly TestDatabaseFixture _fixture;
    private readonly OrderRepository _repository;

    public OrderRepositoryTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
        _repository = new OrderRepository(_fixture.Context);
    }

    [Fact]
    public async Task GetByIdAsync_WithValidId_ShouldReturnOrder()
    {
        // Arrange
        var user = new UserBuilder().Build();
        await _fixture.Context.Users.AddAsync(user);
        await _fixture.Context.SaveChangesAsync();

        var order = new OrderBuilder().WithUserId(user.Id).Build();
        await _fixture.Context.Orders.AddAsync(order);
        await _fixture.Context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(order.Id);

        // Assert
        result.Should().NotBeNull();
        result!.UserId.Should().Be(user.Id);
    }

    [Fact]
    public async Task AddAsync_WithValidOrder_ShouldSucceed()
    {
        // Arrange
        var user = new UserBuilder().Build();
        await _fixture.Context.Users.AddAsync(user);
        await _fixture.Context.SaveChangesAsync();

        var order = new OrderBuilder().WithUserId(user.Id).Build();

        // Act
        await _repository.AddAsync(order);
        await _fixture.Context.SaveChangesAsync();

        // Assert
        var result = await _repository.GetByIdAsync(order.Id);
        result.Should().NotBeNull();
        result!.Status.Should().Be(Bookstore.Domain.Entities.OrderStatus.Pending);
    }
}
