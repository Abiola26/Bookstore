using Bookstore.Application.DTOs;
using Bookstore.Application.Validators;
using FluentAssertions;
using Xunit;

namespace Bookstore.Tests.Unit.Application.Validators;

public class UserRegisterDtoValidatorTests
{
    private readonly UserRegisterDtoValidator _validator = new();

    [Fact]
    public void Validate_WithWeakPassword_ShouldReturnErrors()
    {
        var dto = new UserRegisterDto
        {
            FullName = "Test User",
            Email = "test@example.com",
            Password = "weak",
            PhoneNumber = null
        };

        var errors = _validator.Validate(dto);

        errors.Should().NotBeEmpty();
        errors.Should().Contain(e => e.Contains("at least 12 characters"));
    }

    [Fact]
    public void Validate_WithStrongPassword_ShouldReturnNoPasswordErrors()
    {
        var dto = new UserRegisterDto
        {
            FullName = "Test User",
            Email = "test@example.com",
            Password = "Str0ngP@ssw0rd!",
            PhoneNumber = null
        };

        var errors = _validator.Validate(dto);

        errors.Should().NotContain(e => e.ToLower().Contains("password"));
    }
}
