using System.Net.Http.Json;
using Bookstore.Application.DTOs;
using Bookstore.Domain.Entities;
using Bookstore.Domain.ValueObjects;
using Bookstore.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using FluentAssertions;

namespace Bookstore.Tests.Integration.Api;

public class BooksApiTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public BooksApiTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _factory.SeedDatabase();
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAllBooks_ShouldReturnOk_AndListOfBooks()
    {
        // Arrange
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<BookStoreDbContext>();

            var category = new Category("Integration Test Cat");
            context.Categories.Add(category);
            var book = new Book("Integration Test Title", "D", new ISBN("978-0-123-456"), new Money(10, "USD"), "A", 10, category.Id);
            context.Books.Add(book);
            await context.SaveChangesAsync();
        }

        // Act
        var response = await _client.GetAsync("/api/Books");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<Application.Common.ApiResponse<Application.Common.PagedResult<BookResponseDto>>>();

        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data!.Items.Should().NotBeEmpty();
    }
}
