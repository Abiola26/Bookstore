using Bookstore.Application.Common;
using Bookstore.Application.DTOs;
using Bookstore.Application.Features.Wishlist.Queries;
using Bookstore.Application.Features.Wishlist.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Bookstore.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class WishlistController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<WishlistController> _logger;

    public WishlistController(IMediator mediator, ILogger<WishlistController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpPost("{bookId}")]
    public async Task<IActionResult> AddToWishlist(Guid bookId, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        _logger.LogInformation("User {UserId} adding book {BookId} to wishlist", userId, bookId);
        var result = await _mediator.Send(new AddToWishlistCommand(userId, bookId), cancellationToken);

        if (!result.Success)
        {
            return result.StatusCode == 409 ? Conflict(result) : BadRequest(result);
        }

        return Ok(result);
    }

    [HttpDelete("{bookId}")]
    public async Task<IActionResult> RemoveFromWishlist(Guid bookId, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        _logger.LogInformation("User {UserId} removing book {BookId} from wishlist", userId, bookId);
        var result = await _mediator.Send(new RemoveFromWishlistCommand(userId, bookId), cancellationToken);

        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<ICollection<BookResponseDto>>>> GetWishlist(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        _logger.LogInformation("Retrieving wishlist for user {UserId}", userId);
        var result = await _mediator.Send(new GetUserWishlistQuery(userId), cancellationToken);
        return Ok(result);
    }

    [HttpGet("check/{bookId}")]
    public async Task<ActionResult<ApiResponse<bool>>> IsInWishlist(Guid bookId, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        _logger.LogInformation("Checking wishlist status for user {UserId} and book {BookId}", userId, bookId);
        var result = await _mediator.Send(new GetWishlistStatusQuery(userId, bookId), cancellationToken);
        return Ok(result);
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null)
            throw new UnauthorizedAccessException("User ID not found in token");

        return Guid.Parse(userIdClaim.Value);
    }
}
