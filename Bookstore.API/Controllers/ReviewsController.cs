using Bookstore.Application.DTOs;
using Bookstore.Application.Features.Reviews.Queries;
using Bookstore.Application.Features.Reviews.Commands;
using Bookstore.Application.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Bookstore.API.Controllers;

[ApiController]
[Route("api")]
public class ReviewsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<ReviewsController> _logger;

    public ReviewsController(IMediator mediator, ILogger<ReviewsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Add a review for a book (Authenticated users only)
    /// </summary>
    [HttpPost("books/{bookId:guid}/reviews")]
    [Authorize]
    public async Task<IActionResult> AddReview(Guid bookId, [FromBody] ReviewCreateDto dto, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();

        _logger.LogInformation("User {UserId} adding review for book {BookId}", userId, bookId);
        var response = await _mediator.Send(new AddReviewCommand(bookId, userId, dto), cancellationToken);
        
        if (response.Success)
            return CreatedAtAction(nameof(GetBookReviews), new { bookId }, response);

        return StatusCode(response.StatusCode ?? 400, response);
    }

    /// <summary>
    /// Get all reviews for a specific book
    /// </summary>
    [HttpGet("books/{bookId:guid}/reviews")]
    [AllowAnonymous]
    public async Task<IActionResult> GetBookReviews(Guid bookId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving reviews for book {BookId}", bookId);
        var response = await _mediator.Send(new GetBookReviewsQuery(bookId), cancellationToken);
        return StatusCode(response.StatusCode ?? 200, response);
    }

    /// <summary>
    /// Get review summary for a specific book (Average rating, count, etc.)
    /// </summary>
    [HttpGet("books/{bookId:guid}/reviews/summary")]
    [AllowAnonymous]
    public async Task<IActionResult> GetBookReviewSummary(Guid bookId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving review summary for book {BookId}", bookId);
        var response = await _mediator.Send(new GetBookReviewSummaryQuery(bookId), cancellationToken);
        return StatusCode(response.StatusCode ?? 200, response);
    }

    /// <summary>
    /// Update a review (Owner only)
    /// </summary>
    [HttpPut("reviews/{id:guid}")]
    [Authorize]
    public async Task<IActionResult> UpdateReview(Guid id, [FromBody] ReviewUpdateDto dto, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();

        _logger.LogInformation("User {UserId} updating review {ReviewId}", userId, id);
        var response = await _mediator.Send(new UpdateReviewCommand(id, userId, dto), cancellationToken);
        return StatusCode(response.StatusCode ?? 200, response);
    }

    /// <summary>
    /// Delete a review (Owner or Admin)
    /// </summary>
    [HttpDelete("reviews/{id:guid}")]
    [Authorize]
    public async Task<IActionResult> DeleteReview(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var isAdmin = User.IsInRole("Admin");
        _logger.LogInformation("User {UserId} deleting review {ReviewId} (IsAdmin: {IsAdmin})", userId, id, isAdmin);
        
        var response = await _mediator.Send(new DeleteReviewCommand(id, userId, isAdmin), cancellationToken);
        return StatusCode(response.StatusCode ?? 200, response);
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }
}
