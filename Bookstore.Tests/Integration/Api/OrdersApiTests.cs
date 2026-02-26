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

public class OrdersApiTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory<Program> _factory;

    public OrdersApiTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<string> GetTokenAsync(string email = "test@example.com", string role = "User")
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<BookStoreDbContext>();

        var user = await context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null)
        {
            user = new User("Test User", email, BCrypt.Net.BCrypt.HashPassword("Password123!"), 
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

    private async Task<Guid> SeedCategoryAndBookAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<BookStoreDbContext>();

        var category = new Category("Test Category");
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var book = new Book(
            "Test Book",
            "Description",
            new Bookstore.Domain.ValueObjects.ISBN("978-3-16-148410-0"),
            new Bookstore.Domain.ValueObjects.Money(29.99m, "USD"),
            "Author",
            100,
            category.Id
        );
        context.Books.Add(book);
        await context.SaveChangesAsync();

        return book.Id;
    }

    [Fact]
    public async Task CreateOrder_AsAuthenticatedUser_ShouldReturnCreated()
    {
        // Arrange
        var token = await GetTokenAsync($"user-{Guid.NewGuid()}@example.com");
        var bookId = await SeedCategoryAndBookAsync();
        var orderDto = new OrderCreateDto
        {
            Items = new List<OrderItemCreateDto>
            {
                new OrderItemCreateDto { BookId = bookId, Quantity = 1 }
            }
        };

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.PostAsJsonAsync("/api/orders", orderDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var content = await response.Content.ReadFromJsonAsync<Bookstore.Application.Common.ApiResponse<OrderResponseDto>>();
        content.Should().NotBeNull();
        content!.Data!.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task CreateOrder_WithoutAuth_ShouldReturnUnauthorized()
    {
        // Arrange
        var orderDto = new OrderCreateDto { Items = new List<OrderItemCreateDto>() };
        _client.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await _client.PostAsJsonAsync("/api/orders", orderDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetOrders_AsAuthenticatedUser_ShouldReturnUserOrders()
    {
        // Arrange
        var email = $"user-{Guid.NewGuid()}@example.com";
        var token = await GetTokenAsync(email);
        var bookId = await SeedCategoryAndBookAsync();
        
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        await _client.PostAsJsonAsync("/api/orders", new OrderCreateDto
        {
            Items = new List<OrderItemCreateDto> { new OrderItemCreateDto { BookId = bookId, Quantity = 1 } }
        });

        // Act
        var response = await _client.GetAsync("/api/orders/my-orders");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<Bookstore.Application.Common.ApiResponse<ICollection<OrderResponseDto>>>();
        content!.Data.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetOrderById_WithValidId_ShouldReturnOrder()
    {
        // Arrange
        var email = $"user-{Guid.NewGuid()}@example.com";
        var token = await GetTokenAsync(email);
        var bookId = await SeedCategoryAndBookAsync();
        
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var createResponse = await _client.PostAsJsonAsync("/api/orders", new OrderCreateDto
        {
            Items = new List<OrderItemCreateDto> { new OrderItemCreateDto { BookId = bookId, Quantity = 1 } }
        });
        var order = await createResponse.Content.ReadFromJsonAsync<Bookstore.Application.Common.ApiResponse<OrderResponseDto>>();

        // Act
        var response = await _client.GetAsync($"/api/orders/{order!.Data!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateOrderStatus_AsAdmin_ShouldReturnOk()
    {
        // Arrange
        var adminToken = await GetTokenAsync($"admin-{Guid.NewGuid()}@example.com", "Admin");
        var userToken = await GetTokenAsync($"user-{Guid.NewGuid()}@example.com");
        var bookId = await SeedCategoryAndBookAsync();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userToken);
        var createResponse = await _client.PostAsJsonAsync("/api/orders", new OrderCreateDto
        {
            Items = new List<OrderItemCreateDto> { new OrderItemCreateDto { BookId = bookId, Quantity = 1 } }
        });
        var order = await createResponse.Content.ReadFromJsonAsync<Bookstore.Application.Common.ApiResponse<OrderResponseDto>>();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var updateDto = new OrderUpdateStatusDto { Status = "Paid" };

        // Act
        var response = await _client.PatchAsJsonAsync($"/api/orders/{order!.Data!.Id}/status", updateDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<Bookstore.Application.Common.ApiResponse<OrderResponseDto>>();
        content!.Data!.Status.Should().Be("Paid");
    }

    [Fact]
    public async Task UpdateOrderStatus_AsUser_ShouldReturnForbidden()
    {
        // Arrange
        var userToken = await GetTokenAsync($"user-{Guid.NewGuid()}@example.com");
        var bookId = await SeedCategoryAndBookAsync();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userToken);
        var createResponse = await _client.PostAsJsonAsync("/api/orders", new OrderCreateDto
        {
            Items = new List<OrderItemCreateDto> { new OrderItemCreateDto { BookId = bookId, Quantity = 1 } }
        });
        var order = await createResponse.Content.ReadFromJsonAsync<Bookstore.Application.Common.ApiResponse<OrderResponseDto>>();

        var updateDto = new OrderUpdateStatusDto { Status = "Paid" };

        // Act
        var response = await _client.PatchAsJsonAsync($"/api/orders/{order!.Data!.Id}/status", updateDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
