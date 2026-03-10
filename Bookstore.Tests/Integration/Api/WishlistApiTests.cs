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

public class WishlistApiTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public WishlistApiTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _factory.SeedDatabase();
        _client = _factory.CreateClient();
    }

    private async Task<(Guid UserId, string Token)> CreateAndLoginUserAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<BookStoreDbContext>();
        var authService = scope.ServiceProvider.GetRequiredService<Application.Services.IAuthenticationService>();

        var email = $"wish_test_{Guid.NewGuid()}@example.com";
        var user = new User("Wish User", email, "hashed_password", Bookstore.Domain.Enum.UserRole.User)
        {
            EmailConfirmed = true
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var token = authService.GenerateJwtToken(user.Id, user.Email, user.FullName, user.Role.ToString());
        return (user.Id, token);
    }

    [Fact]
    public async Task AddToWishlist_ShouldReturnOk()
    {
        // Arrange
        var (userId, token) = await CreateAndLoginUserAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        Guid bookId;
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<BookStoreDbContext>();
            var category = new Category("Wish Category");
            context.Categories.Add(category);
            var book = new Book("Wish Book", "D", new ISBN("9780123456789"), new Money(25, "USD"), "A", 10, category.Id);
            context.Books.Add(book);
            await context.SaveChangesAsync();
            bookId = book.Id;
        }

        // Act
        var response = await _client.PostAsync($"/api/Wishlist/{bookId}", null);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ApiResponse>();
        result!.Success.Should().BeTrue();
    }

    [Fact]
    public async Task GetWishlist_ShouldReturnItems()
    {
        // Arrange
        var (userId, token) = await CreateAndLoginUserAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/Wishlist");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<ICollection<BookResponseDto>>>();
        result!.Success.Should().BeTrue();
    }
}
