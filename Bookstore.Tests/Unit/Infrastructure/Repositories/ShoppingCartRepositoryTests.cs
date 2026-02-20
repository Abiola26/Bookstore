using Bookstore.Domain.Entities;
using Bookstore.Domain.ValueObjects;
using Bookstore.Infrastructure.Persistence;
using Bookstore.Infrastructure.Persistence.Repositories;
using Bookstore.Tests.Builders;
using Bookstore.Tests.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Bookstore.Tests.Unit.Infrastructure.Repositories;

/// <summary>
/// Unit tests for ShoppingCartRepository
/// Tests data access patterns and query optimization
/// </summary>
public class ShoppingCartRepositoryTests
{
    private readonly BookStoreDbContext _context;
    private readonly ShoppingCartRepository _repository;

    public ShoppingCartRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<BookStoreDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new BookStoreDbContext(options);
        _repository = new ShoppingCartRepository(_context);
    }

    [Fact]
    public async Task AddAsync_WithValidCart_ShouldSucceed()
    {
        // Arrange
        var cart = new ShoppingCart(Guid.NewGuid());

        // Act
        await _repository.AddAsync(cart);
        await _context.SaveChangesAsync();

        // Assert
        var retrieved = await _repository.GetByIdAsync(cart.Id);
        retrieved.Should().NotBeNull();
        retrieved!.UserId.Should().Be(cart.UserId);
    }

    [Fact]
    public async Task GetByUserIdAsync_WithExistingUser_ShouldReturnCart()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var cart = new ShoppingCart(userId);
        await _repository.AddAsync(cart);
        await _context.SaveChangesAsync();

        // Act
        var retrieved = await _repository.GetByUserIdAsync(userId);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.UserId.Should().Be(userId);
    }

    [Fact]
    public async Task GetByUserIdAsync_WithNonexistentUser_ShouldReturnNull()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var retrieved = await _repository.GetByUserIdAsync(userId);

        // Assert
        retrieved.Should().BeNull();
    }

    [Fact]
    public async Task GetWithItemsAsync_ShouldIncludeItems()
    {
        // Arrange
        var cart = new ShoppingCart(Guid.NewGuid());
        var bookId = Guid.NewGuid();
        var item = new ShoppingCartItem(cart.Id, bookId, 2, new Money(10m, "USD"));
        cart.AddItem(item);
        await _repository.AddAsync(cart);
        await _context.SaveChangesAsync();

        // Act
        var retrieved = await _repository.GetWithItemsAsync(cart.Id);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Items.Should().HaveCount(1);
        retrieved.Items.First().BookId.Should().Be(bookId);
    }

    [Fact]
    public async Task GetWithItemsAsync_WithNonexistentCart_ShouldReturnNull()
    {
        // Arrange
        var cartId = Guid.NewGuid();

        // Act
        var retrieved = await _repository.GetWithItemsAsync(cartId);

        // Assert
        retrieved.Should().BeNull();
    }

    [Fact]
    public async Task GetUserCartWithItemsAsync_ShouldReturnCartWithItems()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var cart = new ShoppingCart(userId);
        var item = new ShoppingCartItem(cart.Id, Guid.NewGuid(), 3, new Money(15m, "USD"));
        cart.AddItem(item);
        await _repository.AddAsync(cart);
        await _context.SaveChangesAsync();

        // Act
        var retrieved = await _repository.GetUserCartWithItemsAsync(userId);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.UserId.Should().Be(userId);
        retrieved.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetUserCartWithItemsAsync_WithNonexistentUser_ShouldReturnNull()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var retrieved = await _repository.GetUserCartWithItemsAsync(userId);

        // Assert
        retrieved.Should().BeNull();
    }

    [Fact]
    public async Task SoftDeleteFilter_ShouldNotReturnDeletedCarts()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var cart = new ShoppingCart(userId);
        await _repository.AddAsync(cart);
        await _context.SaveChangesAsync();

        // Act
        cart.IsDeleted = true; // Soft delete
        _repository.Update(cart);
        await _context.SaveChangesAsync();

        // Assert
        var retrieved = await _repository.GetByUserIdAsync(userId);
        retrieved.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_ShouldPersistChanges()
    {
        // Arrange
        var cart = new ShoppingCart(Guid.NewGuid());
        await _repository.AddAsync(cart);
        await _context.SaveChangesAsync();

        // Act
        var item = new ShoppingCartItem(cart.Id, Guid.NewGuid(), 2, new Money(20m, "USD"));
        cart.AddItem(item);
        _repository.Update(cart);
        await _context.SaveChangesAsync();

        // Assert
        var retrieved = await _repository.GetWithItemsAsync(cart.Id);
        retrieved!.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task DeleteAsync_ShouldSoftDeleteCart()
    {
        // Arrange
        var cart = new ShoppingCart(Guid.NewGuid());
        await _repository.AddAsync(cart);
        await _context.SaveChangesAsync();

        // Act
        _repository.Delete(cart);
        await _context.SaveChangesAsync();

        // Assert
        cart.IsDeleted.Should().BeTrue();
        var retrieved = await _repository.GetByIdAsync(cart.Id);
        retrieved.Should().BeNull(); // Soft delete filter applies
    }

    [Fact]
    public async Task GetByUserIdAsync_WithMultipleCarts_ShouldReturnOnlyActiveCart()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var cart1 = new ShoppingCart(userId);
        await _repository.AddAsync(cart1);
        await _context.SaveChangesAsync();

        // Delete the cart
        _repository.Delete(cart1);
        await _context.SaveChangesAsync();

        // Create a new cart for the same user
        var cart2 = new ShoppingCart(userId);
        await _repository.AddAsync(cart2);
        await _context.SaveChangesAsync();

        // Act
        var retrieved = await _repository.GetByUserIdAsync(userId);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Id.Should().Be(cart2.Id);
    }
}
