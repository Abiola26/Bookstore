using Bookstore.Domain.Entities;
using Bookstore.Domain.Enum;
using FluentAssertions;

namespace Bookstore.Tests.Unit.Domain.Entities;

public class UserTests
{
    [Fact]
    public void Constructor_ShouldInitializeProperties()
    {
        // Arrange
        var fullName = "John Doe";
        var email = "john@example.com";
        var passwordHash = "hashed_password";
        var role = UserRole.User;

        // Act
        var user = new User(fullName, email, passwordHash, role);

        // Assert
        user.FullName.Should().Be(fullName);
        user.Email.Should().Be(email);
        user.PasswordHash.Should().Be(passwordHash);
        user.Role.Should().Be(role);
        user.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void UpdatePassword_ShouldChangePasswordHash()
    {
        // Arrange
        var user = new User("Name", "email@test.com", "old_hash", UserRole.User);
        var newHash = "new_hash";

        // Act
        user.UpdatePassword(newHash);

        // Assert
        user.PasswordHash.Should().Be(newHash);
    }

    [Theory]
    [InlineData("+1 234 567 890")]
    [InlineData("1234567")]
    [InlineData("(123) 456-7890")]
    public void UpdatePhoneNumber_ShouldSetPhoneNumber_WhenValid(string phone)
    {
        // Arrange
        var user = new User("Name", "email@test.com", "hash", UserRole.User);

        // Act
        user.UpdatePhoneNumber(phone);

        // Assert
        user.PhoneNumber.Should().Be(phone.Trim());
    }

    [Fact]
    public void UpdatePhoneNumber_ShouldThrowArgumentException_WhenInvalidFormat()
    {
        // Arrange
        var user = new User("Name", "email@test.com", "hash", UserRole.User);

        // Act
        Action act = () => user.UpdatePhoneNumber("12345"); // Too short

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AddOrder_ShouldAddOrderToList()
    {
        // Arrange
        var user = new User("Name", "email@test.com", "hash", UserRole.User);
        var order = new Order(user.Id);

        // Act
        user.AddOrder(order);

        // Assert
        user.Orders.Should().Contain(order);
    }

    [Fact]
    public void RemoveOrder_ShouldRemoveOrderFromList()
    {
        // Arrange
        var user = new User("Name", "email@test.com", "hash", UserRole.User);
        var order = new Order(user.Id);
        user.AddOrder(order);

        // Act
        var result = user.RemoveOrder(order);

        // Assert
        result.Should().BeTrue();
        user.Orders.Should().NotContain(order);
    }
}
