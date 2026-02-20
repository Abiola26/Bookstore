using Bookstore.Application.Common;
using Bookstore.Application.DTOs;
using Bookstore.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Bookstore.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class WishlistController : ControllerBase
{
    private readonly IWishlistService _wishlistService;

    public WishlistController(IWishlistService wishlistService)
    {
        _wishlistService = wishlistService;
    }

    [HttpPost("{bookId}")]
    public async Task<IActionResult> AddToWishlist(Guid bookId, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var result = await _wishlistService.AddToWishlistAsync(userId, bookId, cancellationToken);
        
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
        var result = await _wishlistService.RemoveFromWishlistAsync(userId, bookId, cancellationToken);
        
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
        var result = await _wishlistService.GetUserWishlistAsync(userId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("check/{bookId}")]
    public async Task<ActionResult<ApiResponse<bool>>> IsInWishlist(Guid bookId, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var result = await _wishlistService.IsInWishlistAsync(userId, bookId, cancellationToken);
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
