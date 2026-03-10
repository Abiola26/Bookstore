using System.Net.Http.Json;
using Bookstore.Application.DTOs;
using Bookstore.Application.Common;
using FluentAssertions;

namespace Bookstore.Tests.Integration.Api;

public class CategoryApiTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory<Program> _factory;

    public CategoryApiTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _factory.SeedDatabase();
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetAllCategories_ShouldReturnOk()
    {
        // Act
        var response = await _client.GetAsync("/api/Categories");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<ICollection<CategoryResponseDto>>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
    }

    [Fact]
    public async Task GetCategoryById_ShouldReturnNotFound_WhenIdIsInvalid()
    {
        // Act
        var response = await _client.GetAsync($"/api/Categories/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }
}
