using Bookstore.Application.DTOs;
using Bookstore.Application.Features.ShoppingCart.Queries;
using Bookstore.Application.Features.ShoppingCart.Commands;
using Bookstore.Application.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Bookstore.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ShoppingCartController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<ShoppingCartController> _logger;

    public ShoppingCartController(IMediator mediator, ILogger<ShoppingCartController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Gets the current user's shopping cart
    /// </summary>
    /// <returns>Shopping cart with all items</returns>
    [HttpGet]
    public async Task<IActionResult> GetCart(CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation("Retrieving shopping cart for user {UserId}", userId);
        var response = await _mediator.Send(new GetUserCartQuery(userId), cancellationToken);
        return StatusCode(response.StatusCode ?? 200, response);
    }

    /// <summary>
    /// Adds an item to the shopping cart
    /// </summary>
    /// <param name="dto">Item details to add</param>
    /// <returns>Updated shopping cart</returns>
    [HttpPost("items")]
    public async Task<IActionResult> AddToCart([FromBody] AddToCartDto dto, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = GetCurrentUserId();
        _logger.LogInformation("User {UserId} adding book {BookId} to cart", userId, dto.BookId);
        var response = await _mediator.Send(new AddToCartCommand(userId, dto), cancellationToken);
        
        if (response.Success)
            return CreatedAtAction(nameof(GetCart), response);

        return StatusCode(response.StatusCode ?? 400, response);
    }

    /// <summary>
    /// Updates the quantity of an item in the shopping cart
    /// </summary>
    /// <param name="cartItemId">ID of the cart item to update</param>
    /// <param name="dto">New quantity</param>
    /// <returns>Updated shopping cart</returns>
    [HttpPut("items/{cartItemId}")]
    public async Task<IActionResult> UpdateCartItem(Guid cartItemId, [FromBody] UpdateCartItemDto dto, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = GetCurrentUserId();
        _logger.LogInformation("User {UserId} updating cart item {CartItemId} quantity to {Quantity}", userId, cartItemId, dto.Quantity);
        var response = await _mediator.Send(new UpdateCartItemCommand(userId, cartItemId, dto), cancellationToken);
        return StatusCode(response.StatusCode ?? 200, response);
    }

    /// <summary>
    /// Removes an item from the shopping cart
    /// </summary>
    /// <param name="cartItemId">ID of the cart item to remove</param>
    /// <returns>Updated shopping cart</returns>
    [HttpDelete("items/{cartItemId}")]
    public async Task<IActionResult> RemoveFromCart(Guid cartItemId, CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation("User {UserId} removing cart item {CartItemId}", userId, cartItemId);
        var response = await _mediator.Send(new RemoveFromCartCommand(userId, cartItemId), cancellationToken);
        return StatusCode(response.StatusCode ?? 200, response);
    }

    /// <summary>
    /// Clears all items from the shopping cart
    /// </summary>
    /// <returns>Empty shopping cart</returns>
    [HttpDelete]
    public async Task<IActionResult> ClearCart(CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation("User {UserId} clearing shopping cart", userId);
        var response = await _mediator.Send(new ClearCartCommand(userId), cancellationToken);
        return StatusCode(response.StatusCode ?? 200, response);
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
