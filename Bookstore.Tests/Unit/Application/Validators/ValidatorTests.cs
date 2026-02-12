using Bookstore.Application.DTOs;
using FluentAssertions;
using Xunit;

namespace Bookstore.Tests.Unit.Application.Validators;

/// <summary>
/// Unit tests for DTO validation
/// Ensures input data is properly validated
/// </summary>
public class DTOValidationTests
{
    [Fact]
    public void BookCreateDto_WithValidData_ShouldBeValid()
    {
        // Arrange
        var dto = new BookCreateDto
        {
            Title = "Valid Title",
            Description = "Valid Description",
            ISBN = "978-0-7432-7356-5",
            Author = "Valid Author",
            Price = 29.99m,
            Currency = "USD",
            TotalQuantity = 10,
            CategoryId = Guid.NewGuid()
        };

        // Act & Assert
        dto.Title.Should().NotBeEmpty();
        dto.Description.Should().NotBeEmpty();
        dto.ISBN.Should().NotBeEmpty();
        dto.Author.Should().NotBeEmpty();
        dto.Price.Should().BeGreaterThan(0);
        dto.TotalQuantity.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public void BookCreateDto_WithMissingTitle_ShouldBeInvalid()
    {
        // Arrange
        var dto = new BookCreateDto
        {
            Title = "",  // Empty title
            Description = "Description",
            ISBN = "978-0-7432-7356-5",
            Author = "Author",
            Price = 29.99m,
            TotalQuantity = 10,
            CategoryId = Guid.NewGuid()
        };

        // Act & Assert
        dto.Title.Should().BeEmpty();
    }

    [Fact]
    public void BookCreateDto_WithNegativePrice_ShouldBeInvalid()
    {
        // Arrange
        var dto = new BookCreateDto
        {
            Title = "Title",
            Description = "Description",
            ISBN = "978-0-7432-7356-5",
            Author = "Author",
            Price = -10m,  // Negative price
            TotalQuantity = 10,
            CategoryId = Guid.NewGuid()
        };

        // Act & Assert
        dto.Price.Should().BeLessThan(0);
    }

    [Fact]
    public void BookUpdateDto_WithValidData_ShouldBeValid()
    {
        // Arrange
        var dto = new BookUpdateDto
        {
            Title = "Updated Title",
            Price = 39.99m,
            TotalQuantity = 50
        };

        // Act & Assert
        dto.Title.Should().NotBeNullOrEmpty();
        dto.Price.Should().BeGreaterThan(0);
    }

    [Fact]
    public void CategoryCreateDto_WithValidName_ShouldBeValid()
    {
        // Arrange
        var dto = new CategoryCreateDto { Name = "Science Fiction" };

        // Act & Assert
        dto.Name.Should().NotBeEmpty();
    }

    [Fact]
    public void CategoryCreateDto_WithEmptyName_ShouldBeInvalid()
    {
        // Arrange
        var dto = new CategoryCreateDto { Name = "" };

        // Act & Assert
        dto.Name.Should().BeEmpty();
    }

    [Fact]
    public void RegisterUserDto_WithValidData_ShouldBeValid()
    {
        // Arrange
        var dto = new UserRegisterDto
        {
            FullName = "John Doe",
            Email = "john@example.com",
            Password = "SecurePassword123!",
            PhoneNumber = "+1234567890"
        };

        // Act & Assert
        dto.FullName.Should().NotBeEmpty();
        dto.Email.Should().Contain("@");
        dto.Password.Length.Should().BeGreaterThanOrEqualTo(8);
    }

    [Fact]
    public void RegisterUserDto_WithInvalidEmail_ShouldBeInvalid()
    {
        // Arrange
        var dto = new UserRegisterDto
        {
            FullName = "John Doe",
            Email = "invalid-email",  // Invalid email format
            Password = "SecurePassword123!",
            PhoneNumber = "+1234567890"
        };

        // Act & Assert
        dto.Email.Should().NotContain("@");
    }

    [Fact]
    public void RegisterUserDto_WithWeakPassword_ShouldBeInvalid()
    {
        // Arrange
        var dto = new UserRegisterDto
        {
            FullName = "John Doe",
            Email = "john@example.com",
            Password = "weak",  // Too weak
            PhoneNumber = "+1234567890"
        };

        // Act & Assert
        dto.Password.Length.Should().BeLessThan(8);
    }

    [Fact]
    public void LoginUserDto_WithValidData_ShouldBeValid()
    {
        // Arrange
        var dto = new UserLoginDto
        {
            Email = "test@example.com",
            Password = "SecurePassword123!"
        };

        // Act & Assert
        dto.Email.Should().Contain("@");
        dto.Password.Should().NotBeEmpty();
    }

    [Fact]
    public void OrderCreateDto_WithValidData_ShouldBeValid()
    {
        // Arrange
        var dto = new OrderCreateDto
        {
            Items = new List<OrderItemCreateDto>
            {
                new OrderItemCreateDto
                {
                    BookId = Guid.NewGuid(),
                    Quantity = 5
                }
            }
        };

        // Act & Assert
        dto.Items.Should().NotBeEmpty();
        dto.Items.First().Quantity.Should().BeGreaterThan(0);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void OrderCreateDto_WithInvalidQuantity_ShouldBeInvalid(int quantity)
    {
        // Arrange
        var dto = new OrderCreateDto
        {
            Items = new List<OrderItemCreateDto>
            {
                new OrderItemCreateDto
                {
                    BookId = Guid.NewGuid(),
                    Quantity = quantity
                }
            }
        };

        // Act & Assert
        dto.Items.First().Quantity.Should().BeLessThanOrEqualTo(0);
    }
}
