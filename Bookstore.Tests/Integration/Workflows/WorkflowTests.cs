using Bookstore.Infrastructure.Persistence;
using Bookstore.Infrastructure.Persistence.Repositories;
using Bookstore.Tests.Builders;
using Bookstore.Tests.Fixtures;
using FluentAssertions;
using Xunit;

namespace Bookstore.Tests.Integration.Workflows;

/// <summary>
/// Integration tests that verify complete workflows across multiple services
/// </summary>
[Collection("Database collection")]
public class BookInventoryWorkflowTests
{
    private readonly TestDatabaseFixture _fixture;

    public BookInventoryWorkflowTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CompleteBookInventoryWorkflow_ShouldSucceed()
    {
        // Arrange - Setup repositories
        var categoryRepository = new CategoryRepository(_fixture.Context);
        var bookRepository = new BookRepository(_fixture.Context);
        var orderRepository = new OrderRepository(_fixture.Context);
        var orderItemRepository = new OrderItemRepository(_fixture.Context);

        // Create a category
        var category = new CategoryBuilder().WithName("Science Fiction").Build();
        await categoryRepository.AddAsync(category);
        await _fixture.Context.SaveChangesAsync();

        // Act 1 - Create a book
        var book = new BookBuilder()
            .WithTitle("Dune")
            .WithAuthor("Frank Herbert")
            .WithCategoryId(category.Id)
            .WithTotalQuantity(100)
            .Build();

        await bookRepository.AddAsync(book);
        await _fixture.Context.SaveChangesAsync();

        // Assert 1 - Book should be retrievable
        var retrievedBook = await bookRepository.GetByIdAsync(book.Id);
        retrievedBook.Should().NotBeNull();
        retrievedBook.TotalQuantity.Should().Be(100);

        // Act 2 - Create an order
        var user = new UserBuilder().Build();
        await _fixture.Context.Users.AddAsync(user);
        await _fixture.Context.SaveChangesAsync();

        var order = new OrderBuilder().WithUserId(user.Id).Build();
        await orderRepository.AddAsync(order);

        var orderItem = new OrderItemBuilder()
            .WithBook(book)
            .WithQuantity(5)
            .WithUnitPrice(book.Price)
            .Build();

        await orderItemRepository.AddAsync(orderItem);
        await _fixture.Context.SaveChangesAsync();

        // Assert 2 - Order should be retrievable with items
        var retrievedOrder = await orderRepository.GetByIdAsync(order.Id);
        retrievedOrder.Should().NotBeNull();
        retrievedOrder.OrderItems.Should().HaveCount(1);

        // Act 3 - Update book stock
        book.TotalQuantity -= orderItem.Quantity;
        bookRepository.Update(book);
        await _fixture.Context.SaveChangesAsync();

        // Assert 3 - Stock should be updated
        var updatedBook = await bookRepository.GetByIdAsync(book.Id);
        updatedBook.TotalQuantity.Should().Be(95);
    }

    [Fact]
    public async Task SoftDeleteWorkflow_ShouldHideDeletedRecords()
    {
        // Arrange
        var categoryRepository = new CategoryRepository(_fixture.Context);
        var category = new CategoryBuilder().WithName("Mystery").Build();
        await categoryRepository.AddAsync(category);
        await _fixture.Context.SaveChangesAsync();

        var initialId = category.Id;

        // Act - Delete the category (soft delete)
        category.IsDeleted = true;
        categoryRepository.Update(category);
        await _fixture.Context.SaveChangesAsync();

        // Assert - Category should not be retrievable (soft deleted)
        var result = await categoryRepository.GetByIdAsync(initialId);
        result.Should().BeNull();
    }

    [Fact]
    public async Task MultipleOperationsInTransaction_ShouldMaintainDataConsistency()
    {
        // Arrange
        var categoryRepository = new CategoryRepository(_fixture.Context);
        var bookRepository = new BookRepository(_fixture.Context);

        var categories = new[]
        {
            new CategoryBuilder().WithName("Category 1").Build(),
            new CategoryBuilder().WithName("Category 2").Build(),
            new CategoryBuilder().WithName("Category 3").Build()
        };

        // Act
        foreach (var category in categories)
        {
            await categoryRepository.AddAsync(category);
        }
        await _fixture.Context.SaveChangesAsync();

        foreach (var category in categories)
        {
            var book = new BookBuilder()
                .WithCategoryId(category.Id)
                .WithTitle($"Book for {category.Name}")
                .Build();
            await bookRepository.AddAsync(book);
        }
        await _fixture.Context.SaveChangesAsync();

        // Assert
        var allBooks = await bookRepository.GetAllAsync();
        allBooks.Should().HaveCountGreaterThanOrEqualTo(3);
    }
}

/// <summary>
/// Integration tests for user authentication workflow
/// </summary>
[Collection("Database collection")]
public class UserAuthenticationWorkflowTests
{
    private readonly TestDatabaseFixture _fixture;

    public UserAuthenticationWorkflowTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task UserRegistrationAndRetrievalWorkflow_ShouldSucceed()
    {
        // Arrange
        var userRepository = new UserRepository(_fixture.Context);
        var password = "SecurePassword123!";
        var user = new UserBuilder()
            .WithEmail("newuser@example.com")
            .WithFullName("New User")
            .WithPasswordHash(BCrypt.Net.BCrypt.HashPassword(password))
            .Build();

        // Act - Register user
        await userRepository.AddAsync(user);
        await _fixture.Context.SaveChangesAsync();

        var userId = user.Id;

        // Act - Retrieve user
        var retrievedUser = await userRepository.GetByIdAsync(userId);

        // Assert
        retrievedUser.Should().NotBeNull();
        retrievedUser.Email.Should().Be("newuser@example.com");
        retrievedUser.FullName.Should().Be("New User");

        // Verify password
        var passwordValid = BCrypt.Net.BCrypt.Verify(password, retrievedUser.PasswordHash);
        passwordValid.Should().BeTrue();
    }

    [Fact]
    public async Task MultipleUserCreation_ShouldNotConflict()
    {
        // Arrange
        var userRepository = new UserRepository(_fixture.Context);
        var users = new[]
        {
            new UserBuilder().WithEmail("user1@example.com").WithFullName("User 1").Build(),
            new UserBuilder().WithEmail("user2@example.com").WithFullName("User 2").Build(),
            new UserBuilder().WithEmail("user3@example.com").WithFullName("User 3").Build()
        };

        // Act
        foreach (var user in users)
        {
            await userRepository.AddAsync(user);
        }
        await _fixture.Context.SaveChangesAsync();

        // Assert
        foreach (var user in users)
        {
            var retrieved = await userRepository.GetByIdAsync(user.Id);
            retrieved.Should().NotBeNull();
            retrieved.Email.Should().Be(user.Email);
        }
    }
}

/// <summary>
/// Integration tests for order processing workflow
/// </summary>
[Collection("Database collection")]
public class OrderProcessingWorkflowTests
{
    private readonly TestDatabaseFixture _fixture;

    public OrderProcessingWorkflowTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CompleteOrderProcessingWorkflow_ShouldUpdateInventory()
    {
        // Arrange
        var categoryRepository = new CategoryRepository(_fixture.Context);
        var bookRepository = new BookRepository(_fixture.Context);
        var userRepository = new UserRepository(_fixture.Context);
        var orderRepository = new OrderRepository(_fixture.Context);
        var orderItemRepository = new OrderItemRepository(_fixture.Context);

        // Setup initial data
        var category = new CategoryBuilder().WithName("Fiction").Build();
        await categoryRepository.AddAsync(category);
        await _fixture.Context.SaveChangesAsync();

        var book = new BookBuilder()
            .WithCategoryId(category.Id)
            .WithTotalQuantity(100)
            .Build();
        await bookRepository.AddAsync(book);
        await _fixture.Context.SaveChangesAsync();

        var user = new UserBuilder().Build();
        await userRepository.AddAsync(user);
        await _fixture.Context.SaveChangesAsync();

        var initialQuantity = book.TotalQuantity;

        // Act - Create order
        var order = new OrderBuilder().WithUserId(user.Id).Build();
        await orderRepository.AddAsync(order);

        var orderItem = new OrderItemBuilder()
            .WithBook(book)
            .WithQuantity(10)
            .WithUnitPrice(book.Price)
            .Build();

        await orderItemRepository.AddAsync(orderItem);
        await _fixture.Context.SaveChangesAsync();

        // Act - Update inventory
        var updatedBook = await bookRepository.GetByIdAsync(book.Id);
        updatedBook.TotalQuantity -= 10;
        bookRepository.Update(updatedBook);
        await _fixture.Context.SaveChangesAsync();

        // Assert - Verify changes
        var finalBook = await bookRepository.GetByIdAsync(book.Id);
        finalBook.TotalQuantity.Should().Be(initialQuantity - 10);

        var retrievedOrder = await orderRepository.GetByIdAsync(order.Id);
        retrievedOrder.OrderItems.Should().HaveCount(1);
        retrievedOrder.OrderItems.First().Quantity.Should().Be(10);
    }
}
