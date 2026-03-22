using Bookstore.Application.DTOs;
using Bookstore.Application.Features.Categories.Queries;
using Bookstore.Application.Features.Categories.Commands;
using Bookstore.Application.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bookstore.API.Controllers;

/// <summary>
/// Category management endpoints
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class CategoriesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<CategoriesController> _logger;

    public CategoriesController(IMediator mediator, ILogger<CategoriesController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get all categories
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of all categories</returns>
    /// <response code="200">Categories retrieved successfully</response>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<ICollection<CategoryResponseDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCategories(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Get all categories");
        var response = await _mediator.Send(new GetAllCategoriesQuery(), cancellationToken);
        return StatusCode(response.StatusCode ?? 200, response);
    }

    /// <summary>
    /// Get category by ID
    /// </summary>
    /// <param name="id">Category ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Category details</returns>
    /// <response code="200">Category found</response>
    /// <response code="404">Category not found</response>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<CategoryResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCategoryById(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Get category {CategoryId}", id);
        var response = await _mediator.Send(new GetCategoryByIdQuery(id), cancellationToken);
        return StatusCode(response.StatusCode ?? 200, response);
    }

    /// <summary>
    /// Create a new category (Admin only)
    /// </summary>
    /// <param name="dto">Category details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created category</returns>
    /// <response code="201">Category created successfully</response>
    /// <response code="400">Validation failed</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden - Admin only</response>
    /// <response code="409">Category name already exists</response>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<CategoryResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateCategory([FromBody] CategoryCreateDto dto, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Create category: {CategoryName}", dto.Name);
        var response = await _mediator.Send(new CreateCategoryCommand(dto), cancellationToken);
        return StatusCode(response.StatusCode ?? 201, response);
    }

    /// <summary>
    /// Update category (Admin only)
    /// </summary>
    /// <param name="id">Category ID</param>
    /// <param name="dto">Updated category details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated category</returns>
    /// <response code="200">Category updated successfully</response>
    /// <response code="400">Validation failed</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden - Admin only</response>
    /// <response code="404">Category not found</response>
    /// <response code="409">Category name already exists</response>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<CategoryResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateCategory(Guid id, [FromBody] CategoryUpdateDto dto, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Update category {CategoryId}", id);
        var response = await _mediator.Send(new UpdateCategoryCommand(id, dto), cancellationToken);
        return StatusCode(response.StatusCode ?? 200, response);
    }

    /// <summary>
    /// Delete category (Admin only)
    /// </summary>
    /// <param name="id">Category ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success message</returns>
    /// <response code="200">Category deleted successfully</response>
    /// <response code="400">Category has books</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden - Admin only</response>
    /// <response code="404">Category not found</response>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCategory(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Delete category {CategoryId}", id);
        var response = await _mediator.Send(new DeleteCategoryCommand(id), cancellationToken);
        return StatusCode(response.StatusCode ?? 200, response);
    }
}
