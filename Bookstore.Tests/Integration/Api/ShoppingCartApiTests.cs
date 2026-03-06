using Bookstore.Application.DTOs;
using Bookstore.Domain.Entities;
using Bookstore.Domain.Enum;
using Bookstore.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Bookstore.Tests.Integration.Api;

public class ShoppingCartApiTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory<Program> _factory;

    public ShoppingCartApiTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<string> GetTokenAsync(string email)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<BookStoreDbContext>();

        var user = await context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null)
        {
            user = new User("Cart User", email, BCrypt.Net.BCrypt.HashPassword("Password123!"), UserRole.User)
            {
                EmailConfirmed = true
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();
        }

        var loginDto = new UserLoginDto { Email = email, Password = "Password123!" };
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginDto);
        var content = await response.Content.ReadFromJsonAsync<Application.Common.ApiResponse<AuthResponseDto>>();
        return content!.Data!.Token;
    }

    private async Task<Guid> SeedBookAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<BookStoreDbContext>();

        var category = await context.Categories.FirstOrDefaultAsync();
        if (category == null)
        {
            category = new Category($"Cart Category {Guid.NewGuid()}");
            context.Categories.Add(category);
            await context.SaveChangesAsync();
        }

        var book = new Book(
            $"Cart Book {Guid.NewGuid()}",
            "Description",
            new Domain.ValueObjects.ISBN("978-0-" + Math.Abs(Guid.NewGuid().GetHashCode() % 1000000).ToString("D6")),
            new Domain.ValueObjects.Money(15.00m, "USD"),
            "Author",
            50,
            category.Id
        );
        context.Books.Add(book);
        await context.SaveChangesAsync();

        return book.Id;
    }

    [Fact]
    public async Task GetCart_Authenticated_ShouldReturnCart()
    {
        // Arrange
        var token = await GetTokenAsync($"cart-user-{Guid.NewGuid()}@example.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/shopping-cart");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<Application.Common.ApiResponse<ShoppingCartResponseDto>>();
        content.Should().NotBeNull();
        content!.Data!.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task AddToCart_WithValidBook_ShouldReturnUpdatedCart()
    {
        // Arrange
        var token = await GetTokenAsync($"cart-user-{Guid.NewGuid()}@example.com");
        var bookId = await SeedBookAsync();
        var addToCartDto = new AddToCartDto { BookId = bookId, Quantity = 2 };

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.PostAsJsonAsync("/api/shopping-cart/items", addToCartDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<Application.Common.ApiResponse<ShoppingCartResponseDto>>();
        content!.Data!.Items.Should().ContainSingle(i => i.BookId == bookId && i.Quantity == 2);
    }

    [Fact]
    public async Task UpdateCartItem_WithValidQuantity_ShouldReturnUpdatedCart()
    {
        // Arrange
        var token = await GetTokenAsync($"cart-user-{Guid.NewGuid()}@example.com");
        var bookId = await SeedBookAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await _client.PostAsJsonAsync("/api/shopping-cart/items", new AddToCartDto { BookId = bookId, Quantity = 1 });
        var updateDto = new UpdateCartItemDto { Quantity = 5 };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/shopping-cart/items/{bookId}", updateDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<Application.Common.ApiResponse<ShoppingCartResponseDto>>();
        content!.Data!.Items.Should().ContainSingle(i => i.BookId == bookId && i.Quantity == 5);
    }

    [Fact]
    public async Task RemoveFromCart_WithValidItem_ShouldReturnUpdatedCart()
    {
        // Arrange
        var token = await GetTokenAsync($"cart-user-{Guid.NewGuid()}@example.com");
        var bookId = await SeedBookAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await _client.PostAsJsonAsync("/api/shopping-cart/items", new AddToCartDto { BookId = bookId, Quantity = 1 });

        // Act
        var response = await _client.DeleteAsync($"/api/shopping-cart/items/{bookId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<Application.Common.ApiResponse<ShoppingCartResponseDto>>();
        content!.Data!.Items.Should().NotContain(i => i.BookId == bookId);
    }

    [Fact]
    public async Task ClearCart_ShouldReturnEmptyCart()
    {
        // Arrange
        var token = await GetTokenAsync($"cart-user-{Guid.NewGuid()}@example.com");
        var bookId = await SeedBookAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await _client.PostAsJsonAsync("/api/shopping-cart/items", new AddToCartDto { BookId = bookId, Quantity = 1 });

        // Act
        var response = await _client.DeleteAsync("/api/shopping-cart");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<Application.Common.ApiResponse<ShoppingCartResponseDto>>();
        content!.Data!.Items.Should().BeEmpty();
    }
}
