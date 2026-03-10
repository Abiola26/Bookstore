using System.Net;
using System.Net.Http.Json;
using Bookstore.Application.DTOs;
using Bookstore.Application.Common;
using FluentAssertions;

namespace Bookstore.Tests.Integration.Api;

public class AuthApiTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public AuthApiTests(CustomWebApplicationFactory<Program> factory)
    {
        factory.SeedDatabase();
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_ShouldCreateUser_WhenValidData()
    {
        // Arrange
        var uniqueEmail = $"integration_test_{Guid.NewGuid()}@example.com";
        var registerDto = new UserRegisterDto
        {
            FullName = "Integration User",
            Email = uniqueEmail,
            Password = "Complex_Auth_123!",
            PhoneNumber = "+1234567890"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/Auth/register", registerDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponseDto>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data!.Email.Should().Be(registerDto.Email);
    }

    [Fact]
    public async Task Login_ShouldReturnUnauthorized_WhenInvalidCredentials()
    {
        // Arrange
        var loginDto = new UserLoginDto { Email = "non_existent@error.com", Password = "WrongPassword!" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/Auth/login", loginDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
