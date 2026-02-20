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

public class ReviewsApiTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory<Program> _factory;

    public ReviewsApiTests(CustomWebApplicationFactory<Program> factory)
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
        
        var isbn = new ISBN("1234567890123");
        var price = new Money(19.99m, "USD");
        var book = new Book("Test Book " + Guid.NewGuid(), "Desc", isbn, price, "Author", 10, category.Id);
        context.Books.Add(book);
        
        await context.SaveChangesAsync();
        return book.Id;
    }

    [Fact]
    public async Task AddReview_Authenticated_ShouldReturnCreated()
    {
        // Arrange
        var (token, _) = await CreateUserAndGetTokenAsync();
        var bookId = await CreateTestBookAsync();
        var createDto = new ReviewCreateDto { Rating = 5, Comment = "Excellent book!" };

        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.PostAsJsonAsync($"/api/books/{bookId}/reviews", createDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var content = await response.Content.ReadFromJsonAsync<ApiResponse<ReviewResponseDto>>();
        content!.Data!.Rating.Should().Be(createDto.Rating);
        content.Data.Comment.Should().Be(createDto.Comment);
    }

    [Fact]
    public async Task AddReview_Duplicate_ShouldReturnBadRequest()
    {
        // Arrange
        var (token, _) = await CreateUserAndGetTokenAsync();
        var bookId = await CreateTestBookAsync();
        var createDto = new ReviewCreateDto { Rating = 5, Comment = "First review" };

        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        await _client.PostAsJsonAsync($"/api/books/{bookId}/reviews", createDto);

        // Act
        var response = await _client.PostAsJsonAsync($"/api/books/{bookId}/reviews", createDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadFromJsonAsync<ApiResponse<ReviewResponseDto>>();
        content!.Message.Should().Contain("already reviewed");
    }

    [Fact]
    public async Task GetReviews_ShouldReturnReviewList()
    {
        // Arrange
        var bookId = await CreateTestBookAsync();
        var (token, _) = await CreateUserAndGetTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        await _client.PostAsJsonAsync($"/api/books/{bookId}/reviews", new ReviewCreateDto { Rating = 4, Comment = "Good" });

        // Act
        var response = await _client.GetAsync($"/api/books/{bookId}/reviews");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<ApiResponse<ICollection<ReviewResponseDto>>>();
        content!.Data.Should().NotBeEmpty();
    }

    [Fact]
    public async Task UpdateReview_Owner_ShouldReturnOk()
    {
        // Arrange
        var (token, _) = await CreateUserAndGetTokenAsync();
        var bookId = await CreateTestBookAsync();
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        
        var createResp = await _client.PostAsJsonAsync($"/api/books/{bookId}/reviews", new ReviewCreateDto { Rating = 3, Comment = "Original" });
        var createdReview = (await createResp.Content.ReadFromJsonAsync<ApiResponse<ReviewResponseDto>>())!.Data!;

        var updateDto = new ReviewUpdateDto { Rating = 5, Comment = "Updated" };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/reviews/{createdReview.Id}", updateDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<ApiResponse<ReviewResponseDto>>();
        content!.Data!.Comment.Should().Be("Updated");
        content.Data.Rating.Should().Be(5);
    }

    [Fact]
    public async Task DeleteReview_Owner_ShouldReturnOk()
    {
        // Arrange
        var (token, _) = await CreateUserAndGetTokenAsync();
        var bookId = await CreateTestBookAsync();
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        
        var createResp = await _client.PostAsJsonAsync($"/api/books/{bookId}/reviews", new ReviewCreateDto { Rating = 3, Comment = "To delete" });
        var createdReview = (await createResp.Content.ReadFromJsonAsync<ApiResponse<ReviewResponseDto>>())!.Data!;

        // Act
        var response = await _client.DeleteAsync($"/api/reviews/{createdReview.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeleteReview_Admin_ShouldReturnOk()
    {
        // Arrange
        var (userToken, _) = await CreateUserAndGetTokenAsync(UserRole.User);
        var (adminToken, _) = await CreateUserAndGetTokenAsync(UserRole.Admin);
        var bookId = await CreateTestBookAsync();
        
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", userToken);
        var createResp = await _client.PostAsJsonAsync($"/api/books/{bookId}/reviews", new ReviewCreateDto { Rating = 3, Comment = "User review" });
        var createdReview = (await createResp.Content.ReadFromJsonAsync<ApiResponse<ReviewResponseDto>>())!.Data!;

        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);

        // Act
        var response = await _client.DeleteAsync($"/api/reviews/{createdReview.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
