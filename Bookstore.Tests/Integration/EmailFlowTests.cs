using System.Net.Http.Json;
using Bookstore.Application.DTOs;
using Bookstore.Application.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.EntityFrameworkCore;
using Bookstore.Infrastructure.Persistence;
using Xunit;

namespace Bookstore.Tests.Integration;

public class EmailFlowTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public EmailFlowTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // replace IEmailSender with in-memory implementation
                services.RemoveAll(typeof(IEmailSender));
                services.AddSingleton<InMemoryEmailSender>();
                services.AddSingleton<IEmailSender>(sp => sp.GetRequiredService<InMemoryEmailSender>());

                // Aggressively remove all EF Core related services to avoid provider conflicts
                var efDescriptors = services.Where(d =>
                    d.ServiceType.Namespace?.StartsWith("Microsoft.EntityFrameworkCore") == true ||
                    d.ImplementationType?.Namespace?.StartsWith("Microsoft.EntityFrameworkCore") == true).ToList();

                foreach (var d in efDescriptors)
                {
                    services.Remove(d);
                }

                // Also specifically remove the DbContext and its options
                services.RemoveAll(typeof(DbContextOptions<BookStoreDbContext>));
                services.RemoveAll(typeof(BookStoreDbContext));

                services.AddDbContext<BookStoreDbContext>(options =>
                {
                    options.UseInMemoryDatabase("TestDb");
                });
            });
        });
    }

    [Fact]
    public async Task Register_Confirm_Login_Flow()
    {
        var client = _factory.CreateClient();

        var registerDto = new UserRegisterDto
        {
            FullName = "Integration Tester",
            Email = "integration@test.local",
            Password = "Str0ngP@ssword!",
        };

        // Register
        var regResp = await client.PostAsJsonAsync("/api/auth/register", registerDto);
        regResp.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);

        var services = _factory.Services;
        var emailSender = services.GetRequiredService<InMemoryEmailSender>();
        emailSender.SentEmails.Should().NotBeEmpty();

        // Extract confirmation token from captured email
        var email = emailSender.SentEmails.Last();
        var body = email.Body;
        // crude extraction of token query param
        var tokenMarker = "token=";
        var tokenIndex = body.IndexOf(tokenMarker);
        tokenIndex.Should().BeGreaterThan(0);
        var tokenPart = body.Substring(tokenIndex + tokenMarker.Length);
        var token = System.Net.WebUtility.UrlDecode(tokenPart.Split('"', '&', '>')[0]);

        // Extract userId from email body
        var userIdMarker = "userId=";
        var uidIndex = body.IndexOf(userIdMarker);
        uidIndex.Should().BeGreaterThan(0);
        var uidPart = body.Substring(uidIndex + userIdMarker.Length);
        var userIdStr = uidPart.Split('&', '"', '>')[0];
        var userId = Guid.Parse(userIdStr);

        // Confirm email
        var confirmResp = await client.GetAsync($"/api/email/confirm?userId={userId}&token={System.Net.WebUtility.UrlEncode(token)}");
        confirmResp.EnsureSuccessStatusCode();

        // Request password reset
        var resetReq = new PasswordResetRequestDto { Email = registerDto.Email };
        var resetResp = await client.PostAsJsonAsync("/api/email/password/request-reset", resetReq);
        resetResp.EnsureSuccessStatusCode();

        // Check reset email
        emailSender.SentEmails.Count.Should().BeGreaterThanOrEqualTo(2);
        var resetEmail = emailSender.SentEmails.Last();
        var resetBody = resetEmail.Body;
        var rTokenIndex = resetBody.IndexOf("token=");
        rTokenIndex.Should().BeGreaterThan(0);
        var rTokenPart = resetBody.Substring(rTokenIndex + "token=".Length);
        var rToken = System.Net.WebUtility.UrlDecode(rTokenPart.Split('"', '&', '>')[0]);

        // Reset password
        var resetDto = new PasswordResetDto { UserId = userId, Token = rToken, NewPassword = "An0therStr0ng!" };
        var doResetResp = await client.PostAsJsonAsync("/api/email/password/reset", resetDto);
        doResetResp.EnsureSuccessStatusCode();

        // Try login
        var loginDto = new UserLoginDto { Email = registerDto.Email, Password = resetDto.NewPassword };
        var loginResp = await client.PostAsJsonAsync("/api/auth/login", loginDto);
        loginResp.EnsureSuccessStatusCode();
        var loginApi = await loginResp.Content.ReadFromJsonAsync<Bookstore.Application.Common.ApiResponse<AuthResponseDto>>();
        loginApi.Should().NotBeNull();
        loginApi!.Success.Should().BeTrue();
    }
}
