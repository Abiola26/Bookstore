using Bookstore.Domain.Entities;
using Bookstore.Domain.ValueObjects;
using Bookstore.Infrastructure.Persistence.Repositories;
using Bookstore.Tests.Fixtures;
using FluentAssertions;
using Xunit;

namespace Bookstore.Tests.Unit.Infrastructure.Repositories;

public class ShoppingCartRepositoryTests : IClassFixture<TestDatabaseFixture>
{
    private readonly TestDatabaseFixture _fixture;

    public ShoppingCartRepositoryTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetUserCartWithItemsAsync_ShouldReturnCartWithItems()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var repo = new ShoppingCartRepository(context);
        
        var user = new User("Test User", "test@test.com", "hash", Bookstore.Domain.Enum.UserRole.User);
        context.Users.Add(user);
        
        var cart = new ShoppingCart(user.Id);
        context.ShoppingCarts.Add(cart);
        
        var category = new Category("Test Cat");
        context.Categories.Add(category);
        var book = new Book("T", "D", new ISBN("123"), new Money(10, "USD"), "A", 10, category.Id);
        context.Books.Add(book);
        
        var item = new ShoppingCartItem(cart.Id, book.Id, 2, book.Price);
        cart.AddItem(item);
        
        await context.SaveChangesAsync();

        // Act
        var result = await repo.GetUserCartWithItemsAsync(user.Id);

        // Assert
        result.Should().NotBeNull();
        result!.UserId.Should().Be(user.Id);
        result.Items.Should().HaveCount(1);
        result.Items.First().Book.Should().NotBeNull();
        result.Items.First().Book!.Title.Should().Be("T");
    }
}
