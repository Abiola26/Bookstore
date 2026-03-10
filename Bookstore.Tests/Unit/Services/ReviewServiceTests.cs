using Bookstore.Application.DTOs;
using Bookstore.Application.Repositories;
using Bookstore.Domain.Entities;
using Bookstore.Domain.Enum;
using Bookstore.Domain.ValueObjects;
using Bookstore.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Bookstore.Tests.Unit.Services;

public class ReviewServiceTests
{
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<ILogger<ReviewService>> _loggerMock;
    private readonly ReviewService _service;

    public ReviewServiceTests()
    {
        _uowMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<ReviewService>>();
        _service = new ReviewService(_uowMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task AddReviewAsync_ShouldAddReview_WhenDataIsValid()
    {
        // Arrange
        var bookId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var dto = new ReviewCreateDto { Rating = 5, Comment = "Great book!" };
        
        var book = new Book("T", "D", new ISBN("123"), new Money(1, "USD"), "A", 1, Guid.NewGuid()) { Id = bookId };
        var user = new User("Test User", "test@test.com", "hash", UserRole.User) { Id = userId, EmailConfirmed = true };

        _uowMock.Setup(x => x.Books.GetByIdAsync(bookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(book);
        _uowMock.Setup(x => x.Users.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _uowMock.Setup(x => x.Reviews.HasUserReviewedBookAsync(userId, bookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _uowMock.Setup(x => x.Reviews.GetByBookIdAsync(bookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Review>());
        _uowMock.Setup(x => x.Reviews.GetAverageRatingAsync(bookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(5.0m);

        // Act
        var result = await _service.AddReviewAsync(bookId, userId, dto);

        // Assert
        result.Success.Should().BeTrue();
        result.StatusCode.Should().Be(201);
        _uowMock.Verify(x => x.Reviews.AddAsync(It.IsAny<Review>(), It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task AddReviewAsync_ShouldReturnForbidden_WhenEmailNotConfirmed()
    {
        // Arrange
        var bookId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var dto = new ReviewCreateDto { Rating = 5, Comment = "Great book!" };
        
        var book = new Book("T", "D", new ISBN("123"), new Money(1, "USD"), "A", 1, Guid.NewGuid()) { Id = bookId };
        var user = new User("Test User", "test@test.com", "hash", UserRole.User) { Id = userId, EmailConfirmed = false };

        _uowMock.Setup(x => x.Books.GetByIdAsync(bookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(book);
        _uowMock.Setup(x => x.Users.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _service.AddReviewAsync(bookId, userId, dto);

        // Assert
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(403);
        result.Message.Should().Be("Email must be confirmed before posting reviews");
    }

    [Fact]
    public async Task GetBookReviewsAsync_ShouldReturnReviews()
    {
        // Arrange
        var bookId = Guid.NewGuid();
        var reviews = new List<Review>
        {
            new Review(bookId, Guid.NewGuid(), 5, "Nice") { User = new User("User 1", "e1", "h", UserRole.User) },
            new Review(bookId, Guid.NewGuid(), 4, "Good") { User = new User("User 2", "e2", "h", UserRole.User) }
        };

        _uowMock.Setup(x => x.Reviews.GetByBookIdAsync(bookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reviews);

        // Act
        var result = await _service.GetBookReviewsAsync(bookId);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(2);
    }
    [Fact]
    public async Task UpdateReviewAsync_ShouldUpdateRating_WhenUserIsOwner()
    {
        // Arrange
        var reviewId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var bookId = Guid.NewGuid();
        var review = new Review(bookId, userId, 3, "Old") { Id = reviewId };
        var dto = new ReviewUpdateDto { Rating = 5, Comment = "New" };
        var book = new Book("T", "D", new ISBN("123"), new Money(1, "USD"), "A", 1, Guid.NewGuid()) { Id = bookId };
        var user = new User("Test User", "test@test.com", "hash", UserRole.User) { Id = userId };

        _uowMock.Setup(x => x.Reviews.GetByIdAsync(reviewId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(review);
        _uowMock.Setup(x => x.Books.GetByIdAsync(bookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(book);
        _uowMock.Setup(x => x.Reviews.GetByBookIdAsync(bookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Review> { review });
        _uowMock.Setup(x => x.Reviews.GetAverageRatingAsync(bookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(5.0m);
        _uowMock.Setup(x => x.Users.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _service.UpdateReviewAsync(reviewId, userId, dto);

        // Assert
        result.Success.Should().BeTrue();
        review.Rating.Should().Be(5);
        review.Comment.Should().Be("New");
        _uowMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task DeleteReviewAsync_ShouldDeleteReview_WhenUserIsOwner()
    {
        // Arrange
        var reviewId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var bookId = Guid.NewGuid();
        var review = new Review(bookId, userId, 5, "Comment") { Id = reviewId };

        _uowMock.Setup(x => x.Reviews.GetByIdAsync(reviewId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(review);
        _uowMock.Setup(x => x.Books.GetByIdAsync(bookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Book?)null); // SyncBookRatingsAsync will skip if book is null

        // Act
        var result = await _service.DeleteReviewAsync(reviewId, userId);

        // Assert
        result.Success.Should().BeTrue();
        _uowMock.Verify(x => x.Reviews.Delete(review), Times.Once);
        _uowMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }
}
