using Bookstore.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace Bookstore.Tests.Unit.Domain.Entities;

public class CategoryTests
{
    [Fact]
    public void Constructor_ShouldInitializeName()
    {
        // Act
        var category = new Category("Test Category");

        // Assert
        category.Name.Should().Be("Test Category");
        category.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void Constructor_ShouldThrowException_WhenNameEmpty()
    {
        // Act
        Action act = () => new Category("");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UpdateName_ShouldUpdateName()
    {
        // Arrange
        var category = new Category("Old Name");

        // Act
        category.UpdateName("New Name");

        // Assert
        category.Name.Should().Be("New Name");
    }
}
