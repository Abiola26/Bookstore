using System.Net.Http.Headers;
using System.Net.Http.Json;
using Bookstore.Application.DTOs;
using Bookstore.Application.Common;
using Bookstore.Domain.Entities;
using Bookstore.Domain.ValueObjects;
using Bookstore.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Bookstore.Tests.Integration.Api;

public class ShoppingCartApiTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ShoppingCartApiTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _factory.SeedDatabase();
        _client = _factory.CreateClient();
    }

    private async Task<(Guid UserId, string Token)> CreateAndLoginUserAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<BookStoreDbContext>();
        var jwtProvider = scope.ServiceProvider.GetRequiredService<Bookstore.Application.Services.IJwtProvider>();

        var email = $"cart_test_{Guid.NewGuid()}@example.com";
        var user = new User("Cart User", email, "hashed_password", Bookstore.Domain.Enum.UserRole.User)
        {
            EmailConfirmed = true
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var token = jwtProvider.GenerateJwtToken(user.Id, user.Email, user.FullName, user.Role.ToString());
        return (user.Id, token);
    }

    [Fact]
    public async Task AddToCart_ShouldAddItem()
    {
        // Arrange
        var (userId, token) = await CreateAndLoginUserAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        Guid bookId;
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<BookStoreDbContext>();
            var category = new Category("Cart Category");
            context.Categories.Add(category);
            var book = new Book("Cart Book", "D", new ISBN("9780123456789"), new Money(15, "USD"), "A", 10, category.Id);
            context.Books.Add(book);
            await context.SaveChangesAsync();
            bookId = book.Id;
        }

        var dto = new AddToCartDto { BookId = bookId, Quantity = 1 };

        // Act
        var response = await _client.PostAsJsonAsync("/api/ShoppingCart/items", dto);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<ShoppingCartResponseDto>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result!.Data!.Items.Should().ContainSingle(i => i.BookId == bookId);
    }

    [Fact]
    public async Task GetCart_ShouldReturnCart()
    {
        // Arrange
        var (userId, token) = await CreateAndLoginUserAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/ShoppingCart");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<ShoppingCartResponseDto>>();
        result!.Success.Should().BeTrue();
    }
}
