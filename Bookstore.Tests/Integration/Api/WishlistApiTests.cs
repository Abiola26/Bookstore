using Bookstore.Application.Common;
using Bookstore.Application.DTOs;
using Bookstore.Domain.Entities;
using Bookstore.Domain.Enum;
using Bookstore.Domain.ValueObjects;
using Bookstore.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Bookstore.Tests.Integration.Api;

public class WishlistApiTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory<Program> _factory;

    public WishlistApiTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<(string Token, Guid UserId)> CreateUserAndGetTokenAsync(UserRole role = UserRole.User)
    {
        var email = $"{role.ToString().ToLower()}-{Guid.NewGuid()}@test.com";
        var password = "TestPassword123!";
        Guid userId;

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<BookStoreDbContext>();
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);
            var user = new User($"{role} User", email, passwordHash, role);
            user.EmailConfirmed = true;
            context.Users.Add(user);
            await context.SaveChangesAsync();
            userId = user.Id;
        }

        var loginResp = await _client.PostAsJsonAsync("/api/auth/login", new UserLoginDto { Email = email, Password = password });
        loginResp.EnsureSuccessStatusCode();
        var authData = await loginResp.Content.ReadFromJsonAsync<ApiResponse<AuthResponseDto>>();
        return (authData!.Data!.Token, userId);
    }

    private async Task<Guid> CreateTestBookAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<BookStoreDbContext>();
        
        var category = new Category("Test Category " + Guid.NewGuid());
        context.Categories.Add(category);
        
        var isbn = new ISBN("978" + DateTime.UtcNow.Ticks.ToString().Substring(DateTime.UtcNow.Ticks.ToString().Length - 10));
        var price = new Money(19.99m, "USD");
        var book = new Book("Test Book " + Guid.NewGuid(), "Desc", isbn, price, "Author", 10, category.Id);
        context.Books.Add(book);
        
        await context.SaveChangesAsync();
        return book.Id;
    }

    [Fact]
    public async Task AddToWishlist_Authenticated_ShouldReturnOk()
    {
        // Arrange
        var (token, _) = await CreateUserAndGetTokenAsync();
        var bookId = await CreateTestBookAsync();

        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.PostAsync($"/api/wishlist/{bookId}", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<ApiResponse>();
        content!.Success.Should().BeTrue();
    }

    [Fact]
    public async Task AddToWishlist_Duplicate_ShouldReturnConflict()
    {
        // Arrange
        var (token, _) = await CreateUserAndGetTokenAsync();
        var bookId = await CreateTestBookAsync();

        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        await _client.PostAsync($"/api/wishlist/{bookId}", null);

        // Act
        var response = await _client.PostAsync($"/api/wishlist/{bookId}", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task GetWishlist_ShouldReturnItems()
    {
        // Arrange
        var (token, _) = await CreateUserAndGetTokenAsync();
        var bookId = await CreateTestBookAsync();

        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        await _client.PostAsync($"/api/wishlist/{bookId}", null);

        // Act
        var response = await _client.GetAsync("/api/wishlist");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<ApiResponse<ICollection<BookResponseDto>>>();
        content!.Data.Should().NotBeEmpty();
        content.Data.Should().Contain(b => b.Id == bookId);
    }

    [Fact]
    public async Task RemoveFromWishlist_ShouldReturnOk()
    {
        // Arrange
        var (token, _) = await CreateUserAndGetTokenAsync();
        var bookId = await CreateTestBookAsync();

        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        await _client.PostAsync($"/api/wishlist/{bookId}", null);

        // Act
        var response = await _client.DeleteAsync($"/api/wishlist/{bookId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        // Verify it's gone
        var checkResponse = await _client.GetAsync("/api/wishlist");
        var content = await checkResponse.Content.ReadFromJsonAsync<ApiResponse<ICollection<BookResponseDto>>>();
        content!.Data.Should().NotContain(b => b.Id == bookId);
    }

    [Fact]
    public async Task IsInWishlist_ShouldReturnCorrectStatus()
    {
        // Arrange
        var (token, _) = await CreateUserAndGetTokenAsync();
        var bookId = await CreateTestBookAsync();

        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act & Assert 1: Not in wishlist
        var response1 = await _client.GetAsync($"/api/wishlist/check/{bookId}");
        var content1 = await response1.Content.ReadFromJsonAsync<ApiResponse<bool>>();
        content1!.Data.Should().BeFalse();

        // Act & Assert 2: In wishlist
        await _client.PostAsync($"/api/wishlist/{bookId}", null);
        var response2 = await _client.GetAsync($"/api/wishlist/check/{bookId}");
        var content2 = await response2.Content.ReadFromJsonAsync<ApiResponse<bool>>();
        content2!.Data.Should().BeTrue();
    }
}
