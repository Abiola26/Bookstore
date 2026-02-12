using Bookstore.Application.DTOs;
using Bookstore.Application.Services;
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
    private readonly ICategoryService _categoryService;
    private readonly ILogger<CategoriesController> _logger;

    public CategoriesController(ICategoryService categoryService, ILogger<CategoriesController> logger)
    {
        _categoryService = categoryService;
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
    [ProducesResponseType(typeof(Bookstore.Application.Common.ApiResponse<ICollection<CategoryResponseDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCategories(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Get all categories");
        var response = await _categoryService.GetAllCategoriesAsync(cancellationToken);
        return StatusCode(response.StatusCode ?? 400, response);
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
    [ProducesResponseType(typeof(Bookstore.Application.Common.ApiResponse<CategoryResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Bookstore.Application.Common.ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCategoryById(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Get category {CategoryId}", id);
        var response = await _categoryService.GetCategoryByIdAsync(id, cancellationToken);
        return StatusCode(response.StatusCode ?? 400, response);
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
    [ProducesResponseType(typeof(Bookstore.Application.Common.ApiResponse<CategoryResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Bookstore.Application.Common.ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Bookstore.Application.Common.ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Bookstore.Application.Common.ApiResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(Bookstore.Application.Common.ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateCategory([FromBody] CategoryCreateDto dto, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Create category: {CategoryName}", dto.Name);
        var response = await _categoryService.CreateCategoryAsync(dto, cancellationToken);
        return StatusCode(response.StatusCode ?? 400, response);
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
    [ProducesResponseType(typeof(Bookstore.Application.Common.ApiResponse<CategoryResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Bookstore.Application.Common.ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Bookstore.Application.Common.ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Bookstore.Application.Common.ApiResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(Bookstore.Application.Common.ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Bookstore.Application.Common.ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateCategory(Guid id, [FromBody] CategoryUpdateDto dto, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Update category {CategoryId}", id);
        var response = await _categoryService.UpdateCategoryAsync(id, dto, cancellationToken);
        return StatusCode(response.StatusCode ?? 400, response);
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
    [ProducesResponseType(typeof(Bookstore.Application.Common.ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Bookstore.Application.Common.ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Bookstore.Application.Common.ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Bookstore.Application.Common.ApiResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(Bookstore.Application.Common.ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCategory(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Delete category {CategoryId}", id);
        var response = await _categoryService.DeleteCategoryAsync(id, cancellationToken);
        return StatusCode(response.StatusCode ?? 400, response);
    }
}
