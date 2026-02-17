using Bookstore.Application.DTOs;
using Bookstore.Application.Services;
using Bookstore.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Bookstore.API.Controllers;

/// <summary>
/// Order management endpoints
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(IOrderService orderService, ILogger<OrdersController> logger)
    {
        _orderService = orderService;
        _logger = logger;
    }

    /// <summary>
    /// Get all orders with pagination (Admin only)
    /// </summary>
    /// <param name="pageNumber">Page number (default 1)</param>
    /// <param name="pageSize">Page size (default 10)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of all orders</returns>
    /// <response code="200">Orders retrieved successfully</response>
    /// <response code="400">Invalid pagination parameters</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden - Admin only</response>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(Bookstore.Application.Common.ApiResponse<PagedResult<OrderResponseDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Bookstore.Application.Common.ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Bookstore.Application.Common.ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Bookstore.Application.Common.ApiResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAllOrders([FromQuery] int pageNumber = ApplicationConstants.Pagination.DefaultPageNumber, [FromQuery] int pageSize = ApplicationConstants.Pagination.DefaultPageSize, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Get all orders - page {PageNumber}, size {PageSize}", pageNumber, pageSize);
        var response = await _orderService.GetAllOrdersPaginatedAsync(pageNumber, pageSize, cancellationToken);
        return StatusCode(response.StatusCode ?? 400, response);
    }

    /// <summary>
    /// Get order by ID
    /// </summary>
    /// <param name="id">Order ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Order details</returns>
    /// <response code="200">Order found</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="404">Order not found</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(Bookstore.Application.Common.ApiResponse<OrderResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Bookstore.Application.Common.ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Bookstore.Application.Common.ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrderById(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Get order {OrderId}", id);
        var response = await _orderService.GetOrderByIdAsync(id, cancellationToken);
        return StatusCode(response.StatusCode ?? 400, response);
    }

    /// <summary>
    /// Get user's orders with pagination
    /// </summary>
    /// <param name="pageNumber">Page number (default 1)</param>
    /// <param name="pageSize">Page size (default 10)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of user's orders</returns>
    /// <response code="200">Orders retrieved</response>
    /// <response code="400">Invalid pagination parameters</response>
    /// <response code="401">Unauthorized</response>
    [HttpGet("my-orders")]
    [ProducesResponseType(typeof(Bookstore.Application.Common.ApiResponse<PagedResult<OrderResponseDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Bookstore.Application.Common.ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Bookstore.Application.Common.ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyOrders([FromQuery] int pageNumber = ApplicationConstants.Pagination.DefaultPageNumber, [FromQuery] int pageSize = ApplicationConstants.Pagination.DefaultPageSize, CancellationToken cancellationToken = default)
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        _logger.LogInformation("Get orders for user {UserId} page {PageNumber}", userId, pageNumber);
        var response = await _orderService.GetUserOrdersPaginatedAsync(userId, pageNumber, pageSize, cancellationToken);
        return StatusCode(response.StatusCode ?? 400, response);
    }

    /// <summary>
    /// Create a new order
    /// </summary>
    /// <param name="dto">Order details with items</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created order</returns>
    /// <response code="201">Order created successfully</response>
    /// <response code="400">Validation failed or insufficient stock</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="404">Book not found</response>
    [HttpPost]
    [EnableRateLimiting("orderPolicy")]
    [ProducesResponseType(typeof(Bookstore.Application.Common.ApiResponse<OrderResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Bookstore.Application.Common.ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Bookstore.Application.Common.ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Bookstore.Application.Common.ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateOrder([FromBody] OrderCreateDto dto, CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        // Extract idempotency key from headers (standard practice for idempotent APIs)
        var idempotencyKey = Request.Headers["X-Idempotency-Key"].ToString();

        _logger.LogInformation("Create order for user {UserId} with {ItemCount} items. IdempotencyKey: {Key}", 
            userId, dto.Items.Count, string.IsNullOrEmpty(idempotencyKey) ? "None" : "Provided");
            
        var response = await _orderService.CreateOrderAsync(userId, dto, idempotencyKey, cancellationToken);
        return StatusCode(response.StatusCode ?? 400, response);
    }

    /// <summary>
    /// Update order status (Admin only)
    /// </summary>
    /// <param name="id">Order ID</param>
    /// <param name="dto">New order status</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated order</returns>
    /// <response code="200">Order status updated</response>
    /// <response code="400">Invalid status</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden - Admin only</response>
    /// <response code="404">Order not found</response>
    [HttpPut("{id:guid}/status")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(Bookstore.Application.Common.ApiResponse<OrderResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Bookstore.Application.Common.ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Bookstore.Application.Common.ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Bookstore.Application.Common.ApiResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(Bookstore.Application.Common.ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateOrderStatus(Guid id, [FromBody] OrderUpdateStatusDto dto, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Update order {OrderId} status to {Status}", id, dto.Status);
        var response = await _orderService.UpdateOrderStatusAsync(id, dto, cancellationToken);
        return StatusCode(response.StatusCode ?? 400, response);
    }

    /// <summary>
    /// Cancel an order
    /// </summary>
    /// <param name="id">Order ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success message</returns>
    /// <response code="200">Order cancelled successfully</response>
    /// <response code="400">Cannot cancel order</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="404">Order not found</response>
    [HttpDelete("{id:guid}/cancel")]
    [ProducesResponseType(typeof(Bookstore.Application.Common.ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Bookstore.Application.Common.ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Bookstore.Application.Common.ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Bookstore.Application.Common.ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelOrder(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Cancel order {OrderId}", id);
        var response = await _orderService.CancelOrderAsync(id, cancellationToken);
        return StatusCode(response.StatusCode ?? 400, response);
    }
}
