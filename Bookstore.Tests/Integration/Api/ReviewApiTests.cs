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

public class ReviewApiTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ReviewApiTests(CustomWebApplicationFactory<Program> factory)
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

        var email = $"review_test_{Guid.NewGuid()}@example.com";
        var user = new User("Review User", email, "hashed_password", Bookstore.Domain.Enum.UserRole.User)
        {
            EmailConfirmed = true
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var token = authService.GenerateJwtToken(user.Id, user.Email, user.FullName, user.Role.ToString());
        return (user.Id, token);
    }

    [Fact]
    public async Task AddReview_ShouldReturnCreated()
    {
        // Arrange
        var (userId, token) = await CreateAndLoginUserAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        Guid bookId;
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<BookStoreDbContext>();
            var category = new Category("Review Category");
            context.Categories.Add(category);
            var book = new Book("Review Book", "D", new ISBN("9780123456789"), new Money(20, "USD"), "A", 10, category.Id);
            context.Books.Add(book);
            await context.SaveChangesAsync();
            bookId = book.Id;
        }

        var dto = new ReviewCreateDto { Rating = 4, Comment = "Good book" };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/books/{bookId}/reviews", dto);

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Created, $"Response: {content}");
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<ReviewResponseDto>>();
        result!.Success.Should().BeTrue();
        result.Data!.Rating.Should().Be(4);
    }
}
