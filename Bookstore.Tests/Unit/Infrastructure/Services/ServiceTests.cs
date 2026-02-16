using Bookstore.Application.DTOs;
using Bookstore.Application.Common;
using Bookstore.Application.Repositories;
using Bookstore.Infrastructure.Services;
using Bookstore.Tests.Builders;
using FluentAssertions;
using Moq;
using Xunit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Bookstore.Application.Settings;
using Bookstore.Application.Services;

namespace Bookstore.Tests.Unit.Infrastructure.Services;

/// <summary>
/// Unit tests for BookService following enterprise-level testing standards
/// </summary>
public class BookServiceTests
{
    private readonly Mock<IBookRepository> _bookRepositoryMock;
    private readonly Mock<ICategoryRepository> _categoryRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly BookService _service;

    public BookServiceTests()
    {
        _bookRepositoryMock = new Mock<IBookRepository>();
        _categoryRepositoryMock = new Mock<ICategoryRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _unitOfWorkMock.Setup(u => u.Books).Returns(_bookRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Categories).Returns(_categoryRepositoryMock.Object);

        _service = new BookService(_unitOfWorkMock.Object);
    }

    [Fact]
    public async Task GetBookByIdAsync_WithValidId_ShouldReturnSuccessResponse()
    {
        // Arrange
        var bookId = Guid.NewGuid();
        var book = new BookBuilder().Build();
        var cancellationToken = CancellationToken.None;

        _bookRepositoryMock
            .Setup(r => r.GetByIdAsync(bookId, cancellationToken))
            .Returns(Task.FromResult<Bookstore.Domain.Entities.Book?>(book));

        // Act
        var result = await _service.GetBookByIdAsync(bookId, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Title.Should().Be(book.Title);
        _bookRepositoryMock.Verify(r => r.GetByIdAsync(bookId, cancellationToken), Times.Once);
    }

    [Fact]
    public async Task GetBookByIdAsync_WithInvalidId_ShouldReturnErrorResponse()
    {
        // Arrange
        var invalidId = Guid.NewGuid();
        var cancellationToken = CancellationToken.None;

        _bookRepositoryMock
            .Setup(r => r.GetByIdAsync(invalidId, cancellationToken))
            .Returns(Task.FromResult<Bookstore.Domain.Entities.Book?>(null));

        // Act
        var result = await _service.GetBookByIdAsync(invalidId, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Book not found");
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task GetAllBooksAsync_ShouldReturnAllBooks()
    {
        // Arrange
        var books = new[]
        {
            new BookBuilder().WithTitle("Book 1").Build(),
            new BookBuilder().WithTitle("Book 2").Build(),
            new BookBuilder().WithTitle("Book 3").Build()
        };
        var cancellationToken = CancellationToken.None;

        _bookRepositoryMock
            .Setup(r => r.GetAllAsync(cancellationToken))
            .Returns(Task.FromResult<ICollection<Bookstore.Domain.Entities.Book>>(books));

        // Act
        var result = await _service.GetAllBooksAsync(cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(3);
        _bookRepositoryMock.Verify(r => r.GetAllAsync(cancellationToken), Times.Once);
    }

    [Fact]
    public async Task GetBooksPagedAsync_WithValidPageNumbers_ShouldReturnPaginatedBooks()
    {
        // Arrange
        var books = new[]
        {
            new BookBuilder().WithTitle("Book 1").Build(),
            new BookBuilder().WithTitle("Book 2").Build()
        };
        const int pageNumber = 1;
        const int pageSize = 10;
        const int totalCount = 2;
        var cancellationToken = CancellationToken.None;

        _bookRepositoryMock
            .Setup(r => r.GetPaginatedAsync(pageNumber, pageSize, cancellationToken))
            .Returns(Task.FromResult<ICollection<Bookstore.Domain.Entities.Book>>(books));
        _bookRepositoryMock
            .Setup(r => r.GetTotalCountAsync(cancellationToken))
            .Returns(Task.FromResult(totalCount));

        // Act
        var result = await _service.GetBooksPagedAsync(pageNumber, pageSize, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data!.PageNumber.Should().Be(pageNumber);
        result.Data!.PageSize.Should().Be(pageSize);
        result.Data!.TotalCount.Should().Be(totalCount);
    }

    [Fact]
    public async Task SearchByTitleAsync_WithValidTitle_ShouldReturnMatchingBooks()
    {
        // Arrange
        const string searchTitle = "C# Programming";
        var books = new[] { new BookBuilder().WithTitle(searchTitle).Build() };
        var cancellationToken = CancellationToken.None;

        _bookRepositoryMock
            .Setup(r => r.SearchByTitleAsync(searchTitle, cancellationToken))
            .Returns(Task.FromResult<ICollection<Bookstore.Domain.Entities.Book>>(books));

        // Act
        var result = await _service.SearchByTitleAsync(searchTitle, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(1);
        result.Data!.First().Title.Should().Be(searchTitle);
    }

    [Fact]
    public async Task GetBookByIdAsync_WhenRepositoryThrowsException_ShouldReturnErrorResponse()
    {
        // Arrange
        var bookId = Guid.NewGuid();
        var cancellationToken = CancellationToken.None;

        _bookRepositoryMock
            .Setup(r => r.GetByIdAsync(bookId, cancellationToken))
            .Throws(new Exception("Database connection failed"));

        // Act
        var result = await _service.GetBookByIdAsync(bookId, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(500);
    }
}

/// <summary>
/// Unit tests for CategoryService following enterprise-level testing standards
/// </summary>
public class CategoryServiceTests
{
    private readonly Mock<ICategoryRepository> _categoryRepositoryMock;
    private readonly Mock<IBookRepository> _bookRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly CategoryService _service;

    public CategoryServiceTests()
    {
        _categoryRepositoryMock = new Mock<ICategoryRepository>();
        _bookRepositoryMock = new Mock<IBookRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _unitOfWorkMock.Setup(u => u.Categories).Returns(_categoryRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Books).Returns(_bookRepositoryMock.Object);

        _service = new CategoryService(_unitOfWorkMock.Object);
    }

    [Fact]
    public async Task GetCategoryByIdAsync_WithValidId_ShouldReturnCategory()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var category = new CategoryBuilder().WithName("Science Fiction").Build();
        var cancellationToken = CancellationToken.None;

        _categoryRepositoryMock
            .Setup(r => r.GetByIdAsync(categoryId, cancellationToken))
            .Returns(Task.FromResult<Bookstore.Domain.Entities.Category?>(category));
        _bookRepositoryMock
            .Setup(r => r.GetCategoryBookCountAsync(categoryId, cancellationToken))
            .Returns(Task.FromResult(5));

        // Act
        var result = await _service.GetCategoryByIdAsync(categoryId, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Name.Should().Be("Science Fiction");
    }

    [Fact]
    public async Task GetCategoryByIdAsync_WithInvalidId_ShouldReturnErrorResponse()
    {
        // Arrange
        var invalidId = Guid.NewGuid();
        var cancellationToken = CancellationToken.None;

        _categoryRepositoryMock
            .Setup(r => r.GetByIdAsync(invalidId, cancellationToken))
            .Returns(Task.FromResult<Bookstore.Domain.Entities.Category?>(null));

        // Act
        var result = await _service.GetCategoryByIdAsync(invalidId, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task GetAllCategoriesAsync_ShouldReturnAllCategories()
    {
        // Arrange
        var categories = new[]
        {
            new CategoryBuilder().WithName("Fiction").Build(),
            new CategoryBuilder().WithName("Science").Build()
        };
        var cancellationToken = CancellationToken.None;

        _categoryRepositoryMock
            .Setup(r => r.GetAllAsync(cancellationToken))
            .Returns(Task.FromResult<ICollection<Bookstore.Domain.Entities.Category>>(categories));
        _bookRepositoryMock
            .Setup(r => r.GetCategoryBookCountAsync(It.IsAny<Guid>(), cancellationToken))
            .Returns(Task.FromResult(5));

        // Act
        var result = await _service.GetAllCategoriesAsync(cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(2);
    }
}

/// <summary>
/// Unit tests for AuthenticationService following enterprise-level testing standards
/// </summary>
public class AuthenticationServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly Mock<IEmailSender> _emailSenderMock;
    private readonly AuthenticationService _service;

    public AuthenticationServiceTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _unitOfWorkMock.Setup(u => u.Users).Returns(_userRepositoryMock.Object);

        _passwordHasherMock = new Mock<IPasswordHasher>();
        _passwordHasherMock
            .Setup(p => p.HashPassword(It.IsAny<string>()))
            .Returns<string>(pw => BCrypt.Net.BCrypt.HashPassword(pw));
        _passwordHasherMock
            .Setup(p => p.VerifyAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns<string, string>((pw, hash) => Task.FromResult(BCrypt.Net.BCrypt.Verify(pw, hash)));

        _emailSenderMock = new Mock<IEmailSender>();
        _emailSenderMock.Setup(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var jwtSettings = new JwtSettings
        {
            Key = "your-very-secret-key-that-is-at-least-32-characters-long-for-hs256-algorithm",
            Issuer = "BookstoreAPI",
            Audience = "BookstoreClients",
            ExpirationMinutes = 1440
        };

        var emailSettings = new EmailSettings
        {
            ConfirmationTokenExpiryHours = 24,
            PasswordResetTokenExpiryHours = 2,
            ConfirmationUrlOrigin = string.Empty
        };

        var jwtOptions = Options.Create(jwtSettings);
        var emailOptions = Options.Create(emailSettings);

        _service = new AuthenticationService(_unitOfWorkMock.Object, jwtOptions, emailOptions, _passwordHasherMock.Object, _emailSenderMock.Object);
    }

    // Configuration is provided via IOptions in tests

    [Fact]
    public async Task RegisterAsync_WithValidData_ShouldSucceed()
    {
        // Arrange
        var dto = new UserRegisterDto
        {
            FullName = "John Doe",
            Email = "john@example.com",
            Password = "SecurePassword123!",
            PhoneNumber = "+1234567890"
        };
        var cancellationToken = CancellationToken.None;

        _userRepositoryMock
            .Setup(r => r.EmailExistsAsync(dto.Email, null, cancellationToken))
            .Returns(Task.FromResult(false));
        _userRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Bookstore.Domain.Entities.User>(), cancellationToken))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(cancellationToken))
            .Returns(Task.FromResult(1));

        // Act
        var result = await _service.RegisterAsync(dto, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Email.Should().Be(dto.Email);
        result.StatusCode.Should().Be(201);
    }

    [Fact]
    public async Task RegisterAsync_WithExistingEmail_ShouldReturnError()
    {
        // Arrange
        var dto = new UserRegisterDto
        {
            FullName = "Jane Doe",
            Email = "existing@example.com",
            Password = "SecurePassword123!",
            PhoneNumber = "+1234567890"
        };
        var cancellationToken = CancellationToken.None;

        _userRepositoryMock
            .Setup(r => r.EmailExistsAsync(dto.Email, null, cancellationToken))
            .Returns(Task.FromResult(true));

        // Act
        var result = await _service.RegisterAsync(dto, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("already registered");
        result.StatusCode.Should().Be(409);
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ShouldReturnToken()
    {
        // Arrange
        var password = "SecurePassword123!";
        var user = new UserBuilder()
            .WithEmail("test@example.com")
            .WithPasswordHash(BCrypt.Net.BCrypt.HashPassword(password))
            .Build();

        var dto = new UserLoginDto
        {
            Email = "test@example.com",
            Password = password
        };
        var cancellationToken = CancellationToken.None;

        _userRepositoryMock
            .Setup(r => r.GetByEmailAsync(dto.Email, cancellationToken))
            .Returns(Task.FromResult<Bookstore.Domain.Entities.User?>(user));

        // Act
        var result = await _service.LoginAsync(dto, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Token.Should().NotBeNullOrEmpty();
        result.Data!.Email.Should().Be(dto.Email);
    }

    [Fact]
    public async Task LoginAsync_WithInvalidPassword_ShouldReturnError()
    {
        // Arrange
        var user = new UserBuilder()
            .WithEmail("test@example.com")
            .WithPasswordHash(BCrypt.Net.BCrypt.HashPassword("CorrectPassword123!"))
            .Build();

        var dto = new UserLoginDto
        {
            Email = "test@example.com",
            Password = "WrongPassword123!"
        };
        var cancellationToken = CancellationToken.None;

        _userRepositoryMock
            .Setup(r => r.GetByEmailAsync(dto.Email, cancellationToken))
            .Returns(Task.FromResult<Bookstore.Domain.Entities.User?>(user));

        // Act
        var result = await _service.LoginAsync(dto, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Invalid");
        result.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task LoginAsync_WithNonexistentUser_ShouldReturnError()
    {
        // Arrange
        var dto = new UserLoginDto
        {
            Email = "nonexistent@example.com",
            Password = "AnyPassword123!"
        };
        var cancellationToken = CancellationToken.None;

        _userRepositoryMock
            .Setup(r => r.GetByEmailAsync(dto.Email, cancellationToken))
            .Returns(Task.FromResult<Bookstore.Domain.Entities.User?>(null));

        // Act
        var result = await _service.LoginAsync(dto, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task ResendConfirmationAsync_WithNonexistentEmail_ShouldReturnNotFound()
    {
        // Arrange
        var email = "notfound@example.com";
        var cancellationToken = CancellationToken.None;

        _userRepositoryMock
            .Setup(r => r.GetByEmailAsync(email, cancellationToken))
            .Returns(Task.FromResult<Bookstore.Domain.Entities.User?>(null));

        // Act
        var result = await _service.ResendConfirmationAsync(email, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task ResendConfirmationAsync_WhenAlreadyConfirmed_ShouldReturnSuccess()
    {
        // Arrange
        var email = "confirmed@example.com";
        var user = new UserBuilder().WithEmail(email).Build();
        user.EmailConfirmed = true;

        _userRepositoryMock
            .Setup(r => r.GetByEmailAsync(email, It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Bookstore.Domain.Entities.User?>(user));

        // Act
        var result = await _service.ResendConfirmationAsync(email);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task ResendConfirmationAsync_WithValidEmail_ShouldResendAndReturnSuccess()
    {
        // Arrange
        var email = "user@example.com";
        var user = new UserBuilder().WithEmail(email).Build();
        user.EmailConfirmed = false;

        _userRepositoryMock
            .Setup(r => r.GetByEmailAsync(email, It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Bookstore.Domain.Entities.User?>(user));

        _userRepositoryMock
            .Setup(r => r.Update(It.IsAny<Bookstore.Domain.Entities.User>()));

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(1));

        // Act
        var result = await _service.ResendConfirmationAsync(email);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        _emailSenderMock.Verify(e => e.SendEmailAsync(email, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RequestPasswordResetAsync_WithNonexistentEmail_ShouldReturnSuccess()
    {
        // Arrange
        var email = "noexist@example.com";
        _userRepositoryMock
            .Setup(r => r.GetByEmailAsync(email, It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Bookstore.Domain.Entities.User?>(null));

        // Act
        var result = await _service.RequestPasswordResetAsync(email);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task ResetPasswordAsync_WithInvalidToken_ShouldReturnError()
    {
        // Arrange
        var user = new UserBuilder().Build();
        user.PasswordResetToken = "token";
        user.PasswordResetTokenExpiresAt = DateTimeOffset.UtcNow.AddHours(-1);

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Bookstore.Domain.Entities.User?>(user));

        // Act
        var result = await _service.ResetPasswordAsync(user.Id, "wrong", "NewPassw0rd!@#");

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task ResetPasswordAsync_WithValidToken_ShouldResetPassword()
    {
        // Arrange
        var user = new UserBuilder().Build();
        user.PasswordResetToken = "token";
        user.PasswordResetTokenExpiresAt = DateTimeOffset.UtcNow.AddHours(1);

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Bookstore.Domain.Entities.User?>(user));

        _userRepositoryMock
            .Setup(r => r.Update(It.IsAny<Bookstore.Domain.Entities.User>()));

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(1));

        // Act
        var result = await _service.ResetPasswordAsync(user.Id, "token", "Str0ngN3wP@ss!");

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        _userRepositoryMock.Verify(r => r.Update(It.IsAny<Bookstore.Domain.Entities.User>()), Times.Once);
    }

    [Fact]
    public async Task ChangePasswordAsync_WithWrongCurrentPassword_ShouldReturnError()
    {
        // Arrange
        var user = new UserBuilder().Build();
        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Bookstore.Domain.Entities.User?>(user));

        // Act
        var result = await _service.ChangePasswordAsync(user.Id, "WrongCurrent", "NewStr0ngP@ss!");

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task ChangePasswordAsync_WithValidCurrentPassword_ShouldChangePassword()
    {
        // Arrange
        var current = "CurrentP@ss123!";
        var user = new UserBuilder().WithPasswordHash(BCrypt.Net.BCrypt.HashPassword(current)).Build();

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Bookstore.Domain.Entities.User?>(user));

        _userRepositoryMock
            .Setup(r => r.Update(It.IsAny<Bookstore.Domain.Entities.User>()));

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(1));

        // Act
        var result = await _service.ChangePasswordAsync(user.Id, current, "NewStr0ngP@ss!");

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        _userRepositoryMock.Verify(r => r.Update(It.IsAny<Bookstore.Domain.Entities.User>()), Times.Once);
    }
}
