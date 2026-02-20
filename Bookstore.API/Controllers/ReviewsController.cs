using Bookstore.Application.DTOs;
using Bookstore.Application.Services;
using Bookstore.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Bookstore.API.Controllers;

[ApiController]
[Route("api")]
public class ReviewsController : ControllerBase
{
    private readonly IReviewService _reviewService;
    private readonly ILogger<ReviewsController> _logger;

    public ReviewsController(IReviewService reviewService, ILogger<ReviewsController> logger)
    {
        _reviewService = reviewService;
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

        var result = await _reviewService.AddReviewAsync(bookId, userId, dto, cancellationToken);
        if (!result.Success)
            return StatusCode(result.StatusCode ?? 400, result);

        return CreatedAtAction(nameof(GetBookReviews), new { bookId }, result);
    }

    /// <summary>
    /// Get all reviews for a specific book
    /// </summary>
    [HttpGet("books/{bookId:guid}/reviews")]
    [AllowAnonymous]
    public async Task<IActionResult> GetBookReviews(Guid bookId, CancellationToken cancellationToken)
    {
        var result = await _reviewService.GetBookReviewsAsync(bookId, cancellationToken);
        if (!result.Success)
            return StatusCode(result.StatusCode ?? 500, result);

        return Ok(result);
    }

    /// <summary>
    /// Get review summary for a specific book (Average rating, count, etc.)
    /// </summary>
    [HttpGet("books/{bookId:guid}/reviews/summary")]
    [AllowAnonymous]
    public async Task<IActionResult> GetBookReviewSummary(Guid bookId, CancellationToken cancellationToken)
    {
        var result = await _reviewService.GetBookReviewSummaryAsync(bookId, cancellationToken);
        if (!result.Success)
            return StatusCode(result.StatusCode ?? 500, result);

        return Ok(result);
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

        var result = await _reviewService.UpdateReviewAsync(id, userId, dto, cancellationToken);
        if (!result.Success)
            return StatusCode(result.StatusCode ?? 400, result);

        return Ok(result);
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
        var result = await _reviewService.DeleteReviewAsync(id, userId, isAdmin, cancellationToken);
        
        if (!result.Success)
            return StatusCode(result.StatusCode ?? 400, result);

        return Ok(result);
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }
}
