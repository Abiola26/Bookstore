using Bookstore.Domain.Entities;
using FluentAssertions;

namespace Bookstore.Tests.Unit.Domain.Entities;

public class ReviewTests
{
    private readonly Guid _bookId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    [Fact]
    public void Constructor_ShouldInitializeProperties()
    {
        // Arrange
        var rating = 5;
        var comment = "Great book!";

        // Act
        var review = new Review(_bookId, _userId, rating, comment);

        // Assert
        review.BookId.Should().Be(_bookId);
        review.UserId.Should().Be(_userId);
        review.Rating.Should().Be(rating);
        review.Comment.Should().Be(comment);
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentOutOfRangeException_WhenRatingInvalid()
    {
        // Act
        Action act = () => new Review(_bookId, _userId, 6, "Excellent");

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void UpdateReview_ShouldUpdateRatingAndComment()
    {
        // Arrange
        var review = new Review(_bookId, _userId, 3, "Average");

        // Act
        review.UpdateReview(5, "Updated comment");

        // Assert
        review.Rating.Should().Be(5);
        review.Comment.Should().Be("Updated comment");
    }
}
