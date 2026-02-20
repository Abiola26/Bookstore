using Bookstore.Application.DTOs;
using Bookstore.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Bookstore.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ShoppingCartController : ControllerBase
{
    private readonly IShoppingCartService _shoppingCartService;
    private readonly ILogger<ShoppingCartController> _logger;

    public ShoppingCartController(IShoppingCartService shoppingCartService, ILogger<ShoppingCartController> logger)
    {
        _shoppingCartService = shoppingCartService;
        _logger = logger;
    }

    /// <summary>
    /// Gets the current user's shopping cart
    /// </summary>
    /// <returns>Shopping cart with all items</returns>
    [HttpGet]
    public async Task<ActionResult<ShoppingCartResponseDto>> GetCart(CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = GetCurrentUserId();
            var response = await _shoppingCartService.GetUserCartAsync(userId, cancellationToken);

            if (!response.Success)
                return StatusCode(response.StatusCode ?? 500, response);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving shopping cart");
            return StatusCode(500, new { message = "An error occurred while retrieving the shopping cart" });
        }
    }

    /// <summary>
    /// Adds an item to the shopping cart
    /// </summary>
    /// <param name="dto">Item details to add</param>
    /// <returns>Updated shopping cart</returns>
    [HttpPost("items")]
    public async Task<ActionResult<ShoppingCartResponseDto>> AddToCart([FromBody] AddToCartDto dto, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = GetCurrentUserId();
            var response = await _shoppingCartService.AddToCartAsync(userId, dto, cancellationToken);

            if (!response.Success)
                return StatusCode(response.StatusCode ?? 500, response);

            return CreatedAtAction(nameof(GetCart), response.Data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding item to cart");
            return StatusCode(500, new { message = "An error occurred while adding item to cart" });
        }
    }

    /// <summary>
    /// Updates the quantity of an item in the shopping cart
    /// </summary>
    /// <param name="cartItemId">ID of the cart item to update</param>
    /// <param name="dto">New quantity</param>
    /// <returns>Updated shopping cart</returns>
    [HttpPut("items/{cartItemId}")]
    public async Task<ActionResult<ShoppingCartResponseDto>> UpdateCartItem(Guid cartItemId, [FromBody] UpdateCartItemDto dto, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = GetCurrentUserId();
            var response = await _shoppingCartService.UpdateCartItemAsync(userId, cartItemId, dto, cancellationToken);

            if (!response.Success)
                return StatusCode(response.StatusCode ?? 500, response);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating cart item {CartItemId}", cartItemId);
            return StatusCode(500, new { message = "An error occurred while updating cart item" });
        }
    }

    /// <summary>
    /// Removes an item from the shopping cart
    /// </summary>
    /// <param name="cartItemId">ID of the cart item to remove</param>
    /// <returns>Updated shopping cart</returns>
    [HttpDelete("items/{cartItemId}")]
    public async Task<ActionResult<ShoppingCartResponseDto>> RemoveFromCart(Guid cartItemId, CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = GetCurrentUserId();
            var response = await _shoppingCartService.RemoveFromCartAsync(userId, cartItemId, cancellationToken);

            if (!response.Success)
                return StatusCode(response.StatusCode ?? 500, response);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing item from cart {CartItemId}", cartItemId);
            return StatusCode(500, new { message = "An error occurred while removing item from cart" });
        }
    }

    /// <summary>
    /// Clears all items from the shopping cart
    /// </summary>
    /// <returns>Empty shopping cart</returns>
    [HttpDelete]
    public async Task<ActionResult<ShoppingCartResponseDto>> ClearCart(CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = GetCurrentUserId();
            var response = await _shoppingCartService.ClearCartAsync(userId, cancellationToken);

            if (!response.Success)
                return StatusCode(response.StatusCode ?? 500, response);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing cart");
            return StatusCode(500, new { message = "An error occurred while clearing cart" });
        }
    }

    /// <summary>
    /// Gets the current authenticated user's ID
    /// </summary>
    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        return userIdClaim != null ? Guid.Parse(userIdClaim.Value) : throw new UnauthorizedAccessException("User ID not found in claims");
    }
}
