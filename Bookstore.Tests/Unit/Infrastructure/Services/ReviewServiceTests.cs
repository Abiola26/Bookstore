using Bookstore.Application.Common;
using Bookstore.Application.DTOs;
using Bookstore.Application.Repositories;
using Bookstore.Domain.Entities;
using Bookstore.Infrastructure.Services;
using Bookstore.Tests.Builders;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Bookstore.Tests.Unit.Infrastructure.Services;

/// <summary>
/// Unit tests for ReviewService
/// Tests all review operations: add, get, summary, update, delete
/// </summary>
public class ReviewServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IReviewRepository> _reviewRepositoryMock;
    private readonly Mock<IBookRepository> _bookRepositoryMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<ILogger<ReviewService>> _loggerMock;
    private readonly ReviewService _service;

    public ReviewServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _reviewRepositoryMock = new Mock<IReviewRepository>();
        _bookRepositoryMock = new Mock<IBookRepository>();
        _userRepositoryMock = new Mock<IUserRepository>();
        _loggerMock = new Mock<ILogger<ReviewService>>();

        _unitOfWorkMock.Setup(u => u.Reviews).Returns(_reviewRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Books).Returns(_bookRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Users).Returns(_userRepositoryMock.Object);

        _service = new ReviewService(_unitOfWorkMock.Object, _loggerMock.Object);
    }

    // ──────────────────────────────────────────────────────────────
    // AddReviewAsync
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task AddReviewAsync_WithValidData_ShouldReturnCreated()
    {
        // Arrange
        var bookId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var book = new BookBuilder().Build();
        var user = new UserBuilder().WithFullName("Jane Doe").Build();
        var dto = new ReviewCreateDto { Rating = 5, Comment = "Great book!" };
        var ct = CancellationToken.None;

        _bookRepositoryMock.Setup(r => r.GetByIdAsync(bookId, ct)).ReturnsAsync(book);
        _userRepositoryMock.Setup(r => r.GetByIdAsync(userId, ct)).ReturnsAsync(user);
        _reviewRepositoryMock.Setup(r => r.HasUserReviewedBookAsync(userId, bookId, ct)).ReturnsAsync(false);
        _reviewRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Review>(), ct)).Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(ct)).ReturnsAsync(1);

        // Sync ratings deps
        _reviewRepositoryMock.Setup(r => r.GetByBookIdAsync(bookId, ct)).ReturnsAsync(new List<Review>());
        _reviewRepositoryMock.Setup(r => r.GetAverageRatingAsync(bookId, ct)).ReturnsAsync(5.0m);
        _bookRepositoryMock.Setup(r => r.GetByIdAsync(bookId, ct)).ReturnsAsync(book);
        _bookRepositoryMock.Setup(r => r.Update(It.IsAny<Book>()));

        // Act
        var result = await _service.AddReviewAsync(bookId, userId, dto, ct);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.StatusCode.Should().Be(201);
        result.Data.Should().NotBeNull();
        result.Data!.Rating.Should().Be(5);
        result.Data.Comment.Should().Be("Great book!");
        _reviewRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Review>(), ct), Times.Once);
    }

    [Fact]
    public async Task AddReviewAsync_WithNonexistentBook_ShouldReturn404()
    {
        // Arrange
        var bookId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var dto = new ReviewCreateDto { Rating = 4, Comment = "Test" };
        var ct = CancellationToken.None;

        _bookRepositoryMock.Setup(r => r.GetByIdAsync(bookId, ct)).ReturnsAsync((Book?)null);

        // Act
        var result = await _service.AddReviewAsync(bookId, userId, dto, ct);

        // Assert
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(404);
        result.Message.Should().Contain("Book not found");
    }

    [Fact]
    public async Task AddReviewAsync_WithNonexistentUser_ShouldReturn404()
    {
        // Arrange
        var bookId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var book = new BookBuilder().Build();
        var dto = new ReviewCreateDto { Rating = 4, Comment = "Test" };
        var ct = CancellationToken.None;

        _bookRepositoryMock.Setup(r => r.GetByIdAsync(bookId, ct)).ReturnsAsync(book);
        _userRepositoryMock.Setup(r => r.GetByIdAsync(userId, ct)).ReturnsAsync((Bookstore.Domain.Entities.User?)null);

        // Act
        var result = await _service.AddReviewAsync(bookId, userId, dto, ct);

        // Assert
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(404);
        result.Message.Should().Contain("User not found");
    }

    [Fact]
    public async Task AddReviewAsync_WhenAlreadyReviewed_ShouldReturn400()
    {
        // Arrange
        var bookId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var book = new BookBuilder().Build();
        var user = new UserBuilder().Build();
        var dto = new ReviewCreateDto { Rating = 3, Comment = "Another review" };
        var ct = CancellationToken.None;

        _bookRepositoryMock.Setup(r => r.GetByIdAsync(bookId, ct)).ReturnsAsync(book);
        _userRepositoryMock.Setup(r => r.GetByIdAsync(userId, ct)).ReturnsAsync(user);
        _reviewRepositoryMock.Setup(r => r.HasUserReviewedBookAsync(userId, bookId, ct)).ReturnsAsync(true);

        // Act
        var result = await _service.AddReviewAsync(bookId, userId, dto, ct);

        // Assert
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.Message.Should().Contain("already reviewed");
    }

    // ──────────────────────────────────────────────────────────────
    // GetBookReviewsAsync
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetBookReviewsAsync_ShouldReturnListOfReviews()
    {
        // Arrange
        var bookId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var ct = CancellationToken.None;
        var user = new UserBuilder().WithFullName("Alice").Build();

        var reviews = new List<Review>
        {
            new Review(bookId, userId, 5, "Excellent!") { User = user },
            new Review(bookId, userId, 4, "Very good") { User = user }
        };

        _reviewRepositoryMock.Setup(r => r.GetByBookIdAsync(bookId, ct)).ReturnsAsync(reviews);

        // Act
        var result = await _service.GetBookReviewsAsync(bookId, ct);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(2);
        result.Data!.First().Rating.Should().Be(5);
    }

    [Fact]
    public async Task GetBookReviewsAsync_WhenRepositoryThrows_ShouldReturn500()
    {
        // Arrange
        var bookId = Guid.NewGuid();
        var ct = CancellationToken.None;

        _reviewRepositoryMock.Setup(r => r.GetByBookIdAsync(bookId, ct)).ThrowsAsync(new Exception("DB error"));

        // Act
        var result = await _service.GetBookReviewsAsync(bookId, ct);

        // Assert
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(500);
    }

    // ──────────────────────────────────────────────────────────────
    // GetBookReviewSummaryAsync
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetBookReviewSummaryAsync_ShouldReturnSummaryWithAverageRating()
    {
        // Arrange
        var bookId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var ct = CancellationToken.None;
        var user = new UserBuilder().WithFullName("Bob").Build();

        var reviews = new List<Review>
        {
            new Review(bookId, userId, 4, "Good") { User = user },
            new Review(bookId, userId, 5, "Great") { User = user }
        };

        _reviewRepositoryMock.Setup(r => r.GetByBookIdAsync(bookId, ct)).ReturnsAsync(reviews);
        _reviewRepositoryMock.Setup(r => r.GetAverageRatingAsync(bookId, ct)).ReturnsAsync(4.5m);

        // Act
        var result = await _service.GetBookReviewSummaryAsync(bookId, ct);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.AverageRating.Should().Be(4.5m);
        result.Data.ReviewCount.Should().Be(2);
        result.Data.RecentReviews.Should().HaveCount(2);
    }

    // ──────────────────────────────────────────────────────────────
    // UpdateReviewAsync
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateReviewAsync_ByOwner_ShouldUpdateSuccessfully()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var reviewId = Guid.NewGuid();
        var bookId = Guid.NewGuid();
        var ct = CancellationToken.None;
        var user = new UserBuilder().WithFullName("Carol").Build();
        var review = new Review(bookId, userId, 3, "Original comment");

        var dto = new ReviewUpdateDto { Rating = 5, Comment = "Updated comment" };

        _reviewRepositoryMock.Setup(r => r.GetByIdAsync(reviewId, ct)).ReturnsAsync(review);
        _reviewRepositoryMock.Setup(r => r.Update(It.IsAny<Review>()));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(ct)).ReturnsAsync(1);
        _userRepositoryMock.Setup(r => r.GetByIdAsync(userId, ct)).ReturnsAsync(user);

        // Sync ratings deps
        _reviewRepositoryMock.Setup(r => r.GetByBookIdAsync(bookId, ct)).ReturnsAsync(new List<Review>());
        _reviewRepositoryMock.Setup(r => r.GetAverageRatingAsync(bookId, ct)).ReturnsAsync(5.0m);
        _bookRepositoryMock.Setup(r => r.GetByIdAsync(bookId, ct)).ReturnsAsync(new BookBuilder().Build());
        _bookRepositoryMock.Setup(r => r.Update(It.IsAny<Book>()));

        // Act
        var result = await _service.UpdateReviewAsync(reviewId, userId, dto, ct);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.Rating.Should().Be(5);
        result.Data.Comment.Should().Be("Updated comment");
        _reviewRepositoryMock.Verify(r => r.Update(It.IsAny<Review>()), Times.Once);
    }

    [Fact]
    public async Task UpdateReviewAsync_ByNonOwner_ShouldReturn403()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var reviewId = Guid.NewGuid();
        var bookId = Guid.NewGuid();
        var ct = CancellationToken.None;
        var review = new Review(bookId, ownerId, 3, "Original");
        var dto = new ReviewUpdateDto { Rating = 5, Comment = "Hacked" };

        _reviewRepositoryMock.Setup(r => r.GetByIdAsync(reviewId, ct)).ReturnsAsync(review);

        // Act
        var result = await _service.UpdateReviewAsync(reviewId, otherUserId, dto, ct);

        // Assert
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(403);
        result.Message.Should().Contain("only update your own");
    }

    [Fact]
    public async Task UpdateReviewAsync_WithNonexistentReview_ShouldReturn404()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var reviewId = Guid.NewGuid();
        var ct = CancellationToken.None;
        var dto = new ReviewUpdateDto { Rating = 4, Comment = "Updated" };

        _reviewRepositoryMock.Setup(r => r.GetByIdAsync(reviewId, ct)).ReturnsAsync((Review?)null);

        // Act
        var result = await _service.UpdateReviewAsync(reviewId, userId, dto, ct);

        // Assert
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(404);
        result.Message.Should().Contain("Review not found");
    }

    [Fact]
    public async Task UpdateReviewAsync_WithInvalidRating_ShouldReturn400()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var reviewId = Guid.NewGuid();
        var bookId = Guid.NewGuid();
        var ct = CancellationToken.None;
        var review = new Review(bookId, userId, 3, "Original");
        var dto = new ReviewUpdateDto { Rating = 10, Comment = "Bad rating" }; // Rating > 5

        _reviewRepositoryMock.Setup(r => r.GetByIdAsync(reviewId, ct)).ReturnsAsync(review);

        // Act
        var result = await _service.UpdateReviewAsync(reviewId, userId, dto, ct);

        // Assert
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.Message.Should().Contain("Rating must be between 1 and 5");
    }

    // ──────────────────────────────────────────────────────────────
    // DeleteReviewAsync
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteReviewAsync_ByOwner_ShouldDeleteSuccessfully()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var reviewId = Guid.NewGuid();
        var bookId = Guid.NewGuid();
        var ct = CancellationToken.None;
        var review = new Review(bookId, userId, 4, "To delete");

        _reviewRepositoryMock.Setup(r => r.GetByIdAsync(reviewId, ct)).ReturnsAsync(review);
        _reviewRepositoryMock.Setup(r => r.Delete(review));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(ct)).ReturnsAsync(1);

        // Sync ratings deps
        _reviewRepositoryMock.Setup(r => r.GetByBookIdAsync(bookId, ct)).ReturnsAsync(new List<Review>());
        _reviewRepositoryMock.Setup(r => r.GetAverageRatingAsync(bookId, ct)).ReturnsAsync(0m);
        _bookRepositoryMock.Setup(r => r.GetByIdAsync(bookId, ct)).ReturnsAsync(new BookBuilder().Build());
        _bookRepositoryMock.Setup(r => r.Update(It.IsAny<Book>()));

        // Act
        var result = await _service.DeleteReviewAsync(reviewId, userId, isAdmin: false, ct);

        // Assert
        result.Success.Should().BeTrue();
        _reviewRepositoryMock.Verify(r => r.Delete(review), Times.Once);
    }

    [Fact]
    public async Task DeleteReviewAsync_ByAdmin_ShouldDeleteSuccessfully()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var adminId = Guid.NewGuid(); // Different user, but is admin
        var reviewId = Guid.NewGuid();
        var bookId = Guid.NewGuid();
        var ct = CancellationToken.None;
        var review = new Review(bookId, ownerId, 4, "Admin can delete");

        _reviewRepositoryMock.Setup(r => r.GetByIdAsync(reviewId, ct)).ReturnsAsync(review);
        _reviewRepositoryMock.Setup(r => r.Delete(review));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(ct)).ReturnsAsync(1);

        // Sync ratings deps
        _reviewRepositoryMock.Setup(r => r.GetByBookIdAsync(bookId, ct)).ReturnsAsync(new List<Review>());
        _reviewRepositoryMock.Setup(r => r.GetAverageRatingAsync(bookId, ct)).ReturnsAsync(0m);
        _bookRepositoryMock.Setup(r => r.GetByIdAsync(bookId, ct)).ReturnsAsync(new BookBuilder().Build());
        _bookRepositoryMock.Setup(r => r.Update(It.IsAny<Book>()));

        // Act
        var result = await _service.DeleteReviewAsync(reviewId, adminId, isAdmin: true, ct);

        // Assert
        result.Success.Should().BeTrue();
        _reviewRepositoryMock.Verify(r => r.Delete(review), Times.Once);
    }

    [Fact]
    public async Task DeleteReviewAsync_ByNonOwner_ShouldReturn403()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var otherId = Guid.NewGuid();
        var reviewId = Guid.NewGuid();
        var bookId = Guid.NewGuid();
        var ct = CancellationToken.None;
        var review = new Review(bookId, ownerId, 4, "Review");

        _reviewRepositoryMock.Setup(r => r.GetByIdAsync(reviewId, ct)).ReturnsAsync(review);

        // Act
        var result = await _service.DeleteReviewAsync(reviewId, otherId, isAdmin: false, ct);

        // Assert
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(403);
    }

    [Fact]
    public async Task DeleteReviewAsync_WithNonexistentReview_ShouldReturn404()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var reviewId = Guid.NewGuid();
        var ct = CancellationToken.None;

        _reviewRepositoryMock.Setup(r => r.GetByIdAsync(reviewId, ct)).ReturnsAsync((Review?)null);

        // Act
        var result = await _service.DeleteReviewAsync(reviewId, userId, isAdmin: false, ct);

        // Assert
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(404);
        result.Message.Should().Contain("Review not found");
    }
}
