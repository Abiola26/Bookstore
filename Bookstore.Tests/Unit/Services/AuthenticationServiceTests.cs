using Bookstore.Application.DTOs;
using Bookstore.Application.Repositories;
using Bookstore.Application.Services;
using Bookstore.Application.Settings;
using Bookstore.Domain.Entities;
using Bookstore.Domain.Enum;
using Bookstore.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Bookstore.Tests.Unit.Services;

public class AuthenticationServiceTests
{
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<ILogger<AuthenticationService>> _loggerMock;
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly Mock<IEmailSender> _emailSenderMock;
    private readonly Mock<IOptions<JwtSettings>> _jwtOptionsMock;
    private readonly Mock<IOptions<EmailSettings>> _emailOptionsMock;
    private readonly AuthenticationService _service;

    public AuthenticationServiceTests()
    {
        _uowMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<AuthenticationService>>();
        _passwordHasherMock = new Mock<IPasswordHasher>();
        _emailSenderMock = new Mock<IEmailSender>();
        _jwtOptionsMock = new Mock<IOptions<JwtSettings>>();
        _emailOptionsMock = new Mock<IOptions<EmailSettings>>();

        _jwtOptionsMock.Setup(x => x.Value).Returns(new JwtSettings
        {
            Key = "super_secret_key_for_testing_purposes_only_12345",
            Issuer = "test_issuer",
            Audience = "test_audience",
            ExpirationMinutes = 60
        });

        _emailOptionsMock.Setup(x => x.Value).Returns(new EmailSettings
        {
            SmtpHost = "localhost",
            SmtpPort = 25,
            FromName = "Test",
            FromAddress = "test@test.com",
            ConfirmationTokenExpiryHours = 24
        });

        _service = new AuthenticationService(
            _uowMock.Object,
            _loggerMock.Object,
            _jwtOptionsMock.Object,
            _emailOptionsMock.Object,
            _passwordHasherMock.Object,
            _emailSenderMock.Object);
    }

    [Fact]
    public async Task RegisterAsync_ShouldCreateUser_WhenDataIsValid()
    {
        // Arrange
        var dto = new UserRegisterDto
        {
            FullName = "Test User",
            Email = "test@example.com",
            Password = "StrongPass123!",
            PhoneNumber = "1234567890"
        };

        _uowMock.Setup(x => x.Users.EmailExistsAsync(dto.Email, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _passwordHasherMock.Setup(x => x.HashPassword(dto.Password))
            .Returns("hashed_password");

        // Act
        var result = await _service.RegisterAsync(dto);

        // Assert
        result.Success.Should().BeTrue();
        result.StatusCode.Should().Be(201);
        result.Data!.Email.Should().Be(dto.Email);
        _uowMock.Verify(x => x.Users.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _emailSenderMock.Verify(x => x.SendEmailAsync(dto.Email, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnToken_WhenCredentialsAreValid()
    {
        // Arrange
        var dto = new UserLoginDto { Email = "test@example.com", Password = "Password123!" };
        var user = new User("Test User", dto.Email, "hashed_password", UserRole.User)
        {
            Id = Guid.NewGuid(),
            EmailConfirmed = true
        };

        _uowMock.Setup(x => x.Users.GetByEmailAsync(dto.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasherMock.Setup(x => x.VerifyAsync(dto.Password, user.PasswordHash))
            .ReturnsAsync(true);

        // Act
        var result = await _service.LoginAsync(dto);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.UserId.Should().Be(user.Id);
        result.Data.Token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnForbidden_WhenEmailNotConfirmed()
    {
        // Arrange
        var dto = new UserLoginDto { Email = "test@example.com", Password = "Password123!" };
        var user = new User("Test User", dto.Email, "hashed_password", UserRole.User)
        {
            EmailConfirmed = false
        };

        _uowMock.Setup(x => x.Users.GetByEmailAsync(dto.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _service.LoginAsync(dto);

        // Assert
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(403);
        result.Message.Should().Be("Email not confirmed");
    }
}
