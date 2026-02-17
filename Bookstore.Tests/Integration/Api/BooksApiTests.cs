using Bookstore.Application.DTOs;
using Bookstore.Domain.Entities;
using Bookstore.Domain.Enum;
using Bookstore.Infrastructure;
using Bookstore.Infrastructure.Persistence;
using Bookstore.Tests.Integration.Api;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Bookstore.Tests.Integration.Api;

public class BooksApiTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory<Program> _factory;

    public BooksApiTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<string> GetAdminTokenAsync()
    {
        var email = $"admin-{Guid.NewGuid()}@test.com";
        var password = "AdminPassword123!";

        // Create admin user directly in the test DB to avoid relying on registration endpoint
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<BookStoreDbContext>();
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);
            var user = new Bookstore.Domain.Entities.User("Admin User", email, passwordHash, Bookstore.Domain.Enum.UserRole.Admin);
            user.EmailConfirmed = true;
            context.Users.Add(user);
            await context.SaveChangesAsync();
        }

        // Login
        var loginResp = await _client.PostAsJsonAsync("/api/auth/login", new UserLoginDto { Email = email, Password = password });
        loginResp.EnsureSuccessStatusCode();
        var authData = await loginResp.Content.ReadFromJsonAsync<Bookstore.Application.Common.ApiResponse<AuthResponseDto>>();
        return authData!.Data!.Token;
    }

    [Fact]
    public async Task GetBooks_PublicEndpoint_ShouldReturnSuccess()
    {
        // Act
        var response = await _client.GetAsync("/api/books");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<Bookstore.Application.Common.ApiResponse<BookPaginatedResponseDto>>();
        content.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateBook_AsAdmin_ShouldReturnCreated()
    {
        // Arrange
        var token = await GetAdminTokenAsync();

        // Need a category first
        Guid categoryId;
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<BookStoreDbContext>();
            var category = new Category("Test Category");
            context.Categories.Add(category);
            await context.SaveChangesAsync();
            categoryId = category.Id;
        }

        var createDto = new BookCreateDto
        {
            Title = "New Admin Book",
            Author = "Admin Author",
            Description = "A book created by an admin",
            Price = 29.99m,
            TotalQuantity = 50,
            CategoryId = categoryId,
            ISBN = "1234567890123"
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/books");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        request.Content = JsonContent.Create(createDto);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var content = await response.Content.ReadFromJsonAsync<Bookstore.Application.Common.ApiResponse<BookResponseDto>>();
        content!.Data!.Title.Should().Be(createDto.Title);
    }

    [Fact]
    public async Task CreateBook_AsNormalUser_ShouldReturnForbidden()
    {
        // Arrange
        var email = $"user-{Guid.NewGuid()}@test.com";
        var password = "UserPassword123!";

        // Create user directly in DB
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<BookStoreDbContext>();
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);
            var user = new Bookstore.Domain.Entities.User("Normal User", email, passwordHash, Bookstore.Domain.Enum.UserRole.User);
            user.EmailConfirmed = true;
            context.Users.Add(user);
            await context.SaveChangesAsync();
        }

        var loginResp = await _client.PostAsJsonAsync("/api/auth/login", new UserLoginDto { Email = email, Password = password });
        loginResp.EnsureSuccessStatusCode();
        var authData = await loginResp.Content.ReadFromJsonAsync<Bookstore.Application.Common.ApiResponse<AuthResponseDto>>();
        var token = authData!.Data!.Token;

        var createDto = new BookCreateDto { Title = "Unauthorized" };
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/books");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        request.Content = JsonContent.Create(createDto);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
