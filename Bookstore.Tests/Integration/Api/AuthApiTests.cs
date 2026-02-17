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

public class AuthApiTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory<Program> _factory;

    public AuthApiTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task ConfirmUserEmailAsync(string email)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<BookStoreDbContext>();
        var user = await context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user != null)
        {
            user.EmailConfirmed = true;
            await context.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task Register_WithValidData_ShouldReturnSuccess()
    {
        // Arrange
        var registerDto = new UserRegisterDto
        {
            FullName = "Test User",
            Email = $"test-{Guid.NewGuid()}@example.com",
            Password = "SecureSecret123!",
            PhoneNumber = "1234567890"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", registerDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var content = await response.Content.ReadFromJsonAsync<Bookstore.Application.Common.ApiResponse<AuthResponseDto>>();
        content.Should().NotBeNull();
        content!.Data!.Email.Should().Be(registerDto.Email);
        content.Data.Token.Should().BeEmpty(); // Token is empty until email confirmed
    }

    [Fact]
    public async Task Login_WithValidCredentials_ShouldReturnToken()
    {
        // Arrange
        var email = $"login-{Guid.NewGuid()}@example.com";
        var password = "SecureSecret123!";

        await _client.PostAsJsonAsync("/api/auth/register", new UserRegisterDto
        {
            FullName = "Login User",
            Email = email,
            Password = password,
            PhoneNumber = "1234567890"
        });

        // Must confirm email before login
        await ConfirmUserEmailAsync(email);

        var loginDto = new UserLoginDto
        {
            Email = email,
            Password = password
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<Bookstore.Application.Common.ApiResponse<AuthResponseDto>>();
        content.Should().NotBeNull();
        content!.Data!.Token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetProfile_WithValidToken_ShouldReturnProfile()
    {
        // Arrange
        var email = $"profile-{Guid.NewGuid()}@example.com";
        var password = "SecureSecret123!";

        await _client.PostAsJsonAsync("/api/auth/register", new UserRegisterDto
        {
            FullName = "Profile User",
            Email = email,
            Password = password,
            PhoneNumber = "1234567890"
        });

        await ConfirmUserEmailAsync(email);

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new UserLoginDto
        {
            Email = email,
            Password = password
        });

        var authData = await loginResponse.Content.ReadFromJsonAsync<Bookstore.Application.Common.ApiResponse<AuthResponseDto>>();
        var token = authData!.Data!.Token;

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetProfile_WithoutToken_ShouldReturnUnauthorized()
    {
        // Act
        // Ensure no token is set
        _client.DefaultRequestHeaders.Authorization = null;
        var response = await _client.GetAsync("/api/auth/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
