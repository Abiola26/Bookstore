using System.Net.Http.Headers;
using System.Net.Http.Json;
using Bookstore.Application.DTOs;
using Bookstore.Application.Common;
using Bookstore.Domain.Entities;
using Bookstore.Domain.Enum;
using Bookstore.Domain.ValueObjects;
using Bookstore.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Bookstore.Tests.Integration.Api;

public class OrderApiTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public OrderApiTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _factory.SeedDatabase();
        _client = _factory.CreateClient();
    }

    private async Task<(Guid UserId, string Token)> CreateAndLoginUserAsync(UserRole role = UserRole.User)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<BookStoreDbContext>();
        var jwtProvider = scope.ServiceProvider.GetRequiredService<Bookstore.Application.Services.IJwtProvider>();

        var email = $"order_test_{Guid.NewGuid()}@example.com";
        var user = new User("Order User", email, "hashed_password", role)
        {
            EmailConfirmed = true
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var token = jwtProvider.GenerateJwtToken(user.Id, user.Email, user.FullName, user.Role.ToString());
        return (user.Id, token);
    }

    [Fact]
    public async Task CreateOrder_ShouldReturnCreated_WhenDataIsValid()
    {
        // Arrange
        var (userId, token) = await CreateAndLoginUserAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        Guid bookId;
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<BookStoreDbContext>();
            var category = new Category("Order Category");
            context.Categories.Add(category);
            var book = new Book("Order Book", "D", new ISBN("1234567890"), new Money(10, "USD"), "A", 100, category.Id);
            context.Books.Add(book);
            await context.SaveChangesAsync();
            bookId = book.Id;
        }

        var dto = new OrderCreateDto
        {
            ShippingAddress = "123 Test St, Test City",
            PaymentMethod = "CashOnDelivery",
            Items = new List<OrderItemCreateDto>
            {
                new OrderItemCreateDto { BookId = bookId, Quantity = 2 }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/Orders", dto);

        // Assert
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            throw new Exception($"Order creation failed. Status: {response.StatusCode}. Body: {errorBody}");
        }
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<OrderResponseDto>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data!.Items.Should().HaveCount(1);
        result.Data.TotalAmount.Should().Be(20m);
    }

    [Fact]
    public async Task CreateOrder_ShouldReturnForbidden_WhenNotAuthenticated()
    {
        // Act
        var response = await _client.PostAsJsonAsync("/api/Orders", new OrderCreateDto());

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMyOrders_ShouldReturnOrders()
    {
        // Arrange
        var (userId, token) = await CreateAndLoginUserAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/Orders/my-orders");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<OrderResponseDto>>>();
        result!.Success.Should().BeTrue();
    }
}
