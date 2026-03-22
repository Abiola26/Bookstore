using Bookstore.Domain.Entities;
using FluentAssertions;

namespace Bookstore.Tests.Unit.Domain.Entities;

public class WishlistItemTests
{
    [Fact]
    public void Constructor_ShouldInitializeProperties()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var bookId = Guid.NewGuid();

        // Act
        var item = new WishlistItem(userId, bookId);

        // Assert
        item.UserId.Should().Be(userId);
        item.BookId.Should().Be(bookId);
        item.Id.Should().NotBeEmpty();
    }
}
