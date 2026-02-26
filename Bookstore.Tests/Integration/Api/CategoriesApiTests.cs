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
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Xunit;

namespace Bookstore.Tests.Integration.Api;

public class CategoriesApiTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory<Program> _factory;

    public CategoriesApiTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<string> GetTokenAsync(string email, string role = "User")
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<BookStoreDbContext>();

        var user = await context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null)
        {
            user = new User("Category User", email, BCrypt.Net.BCrypt.HashPassword("Password123!"), 
                role == "Admin" ? UserRole.Admin : UserRole.User)
            {
                EmailConfirmed = true
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();
        }

        var loginDto = new UserLoginDto { Email = email, Password = "Password123!" };
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginDto);
        var content = await response.Content.ReadFromJsonAsync<Bookstore.Application.Common.ApiResponse<AuthResponseDto>>();
        return content!.Data!.Token;
    }

    [Fact]
    public async Task GetCategories_PublicEndpoint_ShouldReturnSuccess()
    {
        // Act
        var response = await _client.GetAsync("/api/categories");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<Bookstore.Application.Common.ApiResponse<ICollection<CategoryResponseDto>>>();
        content.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateCategory_AsAdmin_ShouldReturnCreated()
    {
        // Arrange
        var token = await GetTokenAsync($"admin-{Guid.NewGuid()}@example.com", "Admin");
        var dto = new CategoryCreateDto { Name = $"New Category {Guid.NewGuid()}" };

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.PostAsJsonAsync("/api/categories", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var content = await response.Content.ReadFromJsonAsync<Bookstore.Application.Common.ApiResponse<CategoryResponseDto>>();
        content!.Data!.Name.Should().Be(dto.Name);
    }

    [Fact]
    public async Task CreateCategory_AsUser_ShouldReturnForbidden()
    {
        // Arrange
        var token = await GetTokenAsync($"user-{Guid.NewGuid()}@example.com", "User");
        var dto = new CategoryCreateDto { Name = "Forbidden Category" };

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.PostAsJsonAsync("/api/categories", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateCategory_AsAdmin_ShouldReturnOk()
    {
        // Arrange
        var token = await GetTokenAsync($"admin-{Guid.NewGuid()}@example.com", "Admin");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createResponse = await _client.PostAsJsonAsync("/api/categories", new CategoryCreateDto { Name = $"To Update {Guid.NewGuid()}" });
        var category = await createResponse.Content.ReadFromJsonAsync<Bookstore.Application.Common.ApiResponse<CategoryResponseDto>>();

        var updateDto = new CategoryUpdateDto { Name = $"Updated-{Guid.NewGuid()}" };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/categories/{category!.Data!.Id}", updateDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<Bookstore.Application.Common.ApiResponse<CategoryResponseDto>>();
        content!.Data!.Name.Should().Be(updateDto.Name);
    }

    [Fact]
    public async Task DeleteCategory_AsAdmin_ShouldReturnOk()
    {
        // Arrange
        var token = await GetTokenAsync($"admin-{Guid.NewGuid()}@example.com", "Admin");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createResponse = await _client.PostAsJsonAsync("/api/categories", new CategoryCreateDto { Name = $"ToDelete-{Guid.NewGuid()}" });
        var category = await createResponse.Content.ReadFromJsonAsync<Bookstore.Application.Common.ApiResponse<CategoryResponseDto>>();

        // Act
        var response = await _client.DeleteAsync($"/api/categories/{category!.Data!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
