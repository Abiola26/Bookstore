using Bookstore.Application.DTOs;
using Bookstore.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bookstore.API.Controllers;

/// <summary>
/// Book management endpoints
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class BooksController : ControllerBase
{
    private readonly IBookService _bookService;
    private readonly ILogger<BooksController> _logger;

    public BooksController(IBookService bookService, ILogger<BooksController> logger)
    {
        _bookService = bookService;
        _logger = logger;
    }

    /// <summary>
    /// Get all books with pagination
    /// </summary>
    /// <param name="pageNumber">Page number (default 1)</param>
    /// <param name="pageSize">Page size (default 10)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of books</returns>
    /// <response code="200">Books retrieved successfully</response>
    /// <response code="400">Invalid pagination parameters</response>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(Bookstore.Application.Common.ApiResponse<BookPaginatedResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Bookstore.Application.Common.ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetBooks([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Get books page {PageNumber} with size {PageSize}", pageNumber, pageSize);
        var response = await _bookService.GetBooksPagedAsync(pageNumber, pageSize, cancellationToken);
        return StatusCode(response.StatusCode ?? 400, response);
    }

    /// <summary>
    /// Get book by ID
    /// </summary>
    /// <param name="id">Book ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Book details</returns>
    /// <response code="200">Book found</response>
    /// <response code="404">Book not found</response>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(Bookstore.Application.Common.ApiResponse<BookResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Bookstore.Application.Common.ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBookById(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Get book {BookId}", id);
        var response = await _bookService.GetBookByIdAsync(id, cancellationToken);
        return StatusCode(response.StatusCode ?? 400, response);
    }

    /// <summary>
    /// Search books by title
    /// </summary>
    /// <param name="title">Title to search for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of matching books</returns>
    /// <response code="200">Search completed</response>
    /// <response code="400">Invalid search query</response>
    [HttpGet("search/{title}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(Bookstore.Application.Common.ApiResponse<ICollection<BookResponseDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Bookstore.Application.Common.ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SearchByTitle(string title, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Search books by title: {Title}", title);
        var response = await _bookService.SearchByTitleAsync(title, cancellationToken);
        return StatusCode(response.StatusCode ?? 400, response);
    }

    /// <summary>
    /// Get books by category with pagination
    /// </summary>
    /// <param name="categoryId">Category ID</param>
    /// <param name="pageNumber">Page number (default 1)</param>
    /// <param name="pageSize">Page size (default 10)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of books in category</returns>
    /// <response code="200">Books retrieved</response>
    /// <response code="400">Invalid parameters</response>
    /// <response code="404">Category not found</response>
    [HttpGet("category/{categoryId:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(Bookstore.Application.Common.ApiResponse<BookPaginatedResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Bookstore.Application.Common.ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Bookstore.Application.Common.ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByCategory(Guid categoryId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Get books for category {CategoryId}", categoryId);
        var response = await _bookService.GetBooksByCategoryAsync(categoryId, pageNumber, pageSize, cancellationToken);
        return StatusCode(response.StatusCode ?? 400, response);
    }

    /// <summary>
    /// Create a new book (Admin only)
    /// </summary>
    /// <param name="dto">Book details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created book</returns>
    /// <response code="201">Book created successfully</response>
    /// <response code="400">Validation failed</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden - Admin only</response>
    /// <response code="409">ISBN already exists</response>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(Bookstore.Application.Common.ApiResponse<BookResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Bookstore.Application.Common.ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Bookstore.Application.Common.ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Bookstore.Application.Common.ApiResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(Bookstore.Application.Common.ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateBook([FromBody] BookCreateDto dto, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Create book with ISBN {ISBN}", dto.ISBN);
        var response = await _bookService.CreateBookAsync(dto, cancellationToken);
        return StatusCode(response.StatusCode ?? 400, response);
    }

    /// <summary>
    /// Update book (Admin only)
    /// </summary>
    /// <param name="id">Book ID</param>
    /// <param name="dto">Updated book details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated book</returns>
    /// <response code="200">Book updated successfully</response>
    /// <response code="400">Validation failed</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden - Admin only</response>
    /// <response code="404">Book not found</response>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(Bookstore.Application.Common.ApiResponse<BookResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Bookstore.Application.Common.ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Bookstore.Application.Common.ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Bookstore.Application.Common.ApiResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(Bookstore.Application.Common.ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateBook(Guid id, [FromBody] BookUpdateDto dto, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Update book {BookId}", id);
        var response = await _bookService.UpdateBookAsync(id, dto, cancellationToken);
        return StatusCode(response.StatusCode ?? 400, response);
    }

    /// <summary>
    /// Delete book (Admin only)
    /// </summary>
    /// <param name="id">Book ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success message</returns>
    /// <response code="200">Book deleted successfully</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden - Admin only</response>
    /// <response code="404">Book not found</response>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(Bookstore.Application.Common.ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Bookstore.Application.Common.ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Bookstore.Application.Common.ApiResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(Bookstore.Application.Common.ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteBook(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Delete book {BookId}", id);
        var response = await _bookService.DeleteBookAsync(id, cancellationToken);
        return StatusCode(response.StatusCode ?? 400, response);
    }
}
