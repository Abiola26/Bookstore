using Bookstore.Application.DTOs;
using Bookstore.Application.Features.Books.Queries;
using Bookstore.Application.Features.Books.Commands;
using Bookstore.Application.Common;
using Bookstore.Application.Services;
using MediatR;
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
    private readonly IMediator _mediator;
    private readonly ILogger<BooksController> _logger;
    private readonly IFileStorageService _fileStorage;

    public BooksController(IMediator mediator, ILogger<BooksController> logger, IFileStorageService fileStorage)
    {
        _mediator = mediator;
        _logger = logger;
        _fileStorage = fileStorage;
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
    [ProducesResponseType(typeof(ApiResponse<PagedResult<BookResponseDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetBooks([FromQuery] int pageNumber = ApplicationConstants.Pagination.DefaultPageNumber, [FromQuery] int pageSize = ApplicationConstants.Pagination.DefaultPageSize, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Get books page {PageNumber} with size {PageSize}", pageNumber, pageSize);
        var response = await _mediator.Send(new GetBooksPagedQuery(pageNumber, pageSize), cancellationToken);
        return StatusCode(response.StatusCode ?? 200, response);
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
    [ProducesResponseType(typeof(ApiResponse<BookResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBookById(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Get book {BookId}", id);
        var response = await _mediator.Send(new GetBookByIdQuery(id), cancellationToken);
        return StatusCode(response.StatusCode ?? 200, response);
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
    [ProducesResponseType(typeof(ApiResponse<ICollection<BookResponseDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SearchByTitle([FromRoute] string title, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(title) || title.Length < ApplicationConstants.Validation.MinSearchLength)
            return BadRequest(Bookstore.Application.Common.ApiResponse.ErrorResponse($"Search title must be at least {ApplicationConstants.Validation.MinSearchLength} characters", null, 400));

        if (title.Length > ApplicationConstants.Validation.MaxSearchLength)
            return BadRequest(Bookstore.Application.Common.ApiResponse.ErrorResponse($"Search title is too long (max {ApplicationConstants.Validation.MaxSearchLength} characters)", null, 400));

        _logger.LogInformation("Search books by title: {SearchQuery}", title);
        var response = await _mediator.Send(new SearchBooksQuery(title), cancellationToken);
        return StatusCode(response.StatusCode ?? 200, response);
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
    [ProducesResponseType(typeof(ApiResponse<PagedResult<BookResponseDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByCategory(Guid categoryId, [FromQuery] int pageNumber = ApplicationConstants.Pagination.DefaultPageNumber, [FromQuery] int pageSize = ApplicationConstants.Pagination.DefaultPageSize, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Get books for category {CategoryId}", categoryId);
        var response = await _mediator.Send(new GetBooksByCategoryQuery(categoryId, pageNumber, pageSize), cancellationToken);
        return StatusCode(response.StatusCode ?? 200, response);
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
    [ProducesResponseType(typeof(ApiResponse<BookResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateBook([FromBody] BookCreateDto dto, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Create book with ISBN {ISBN}", dto.ISBN);
        var response = await _mediator.Send(new CreateBookCommand(dto), cancellationToken);
        return StatusCode(response.StatusCode ?? 201, response);
    }

    /// <summary>
    /// Bulk upload books from CSV (Admin only)
    /// </summary>
    [HttpPost("bulk-upload")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> BulkUpload(IFormFile file, CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
            return BadRequest(Bookstore.Application.Common.ApiResponse.ErrorResponse("No file uploaded", null, 400));

        if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            return BadRequest(Bookstore.Application.Common.ApiResponse.ErrorResponse("Only CSV files are allowed", null, 400));

        using var stream = file.OpenReadStream();
        var response = await _mediator.Send(new BulkUploadBooksCommand(stream), cancellationToken);
        return StatusCode(response.StatusCode ?? 200, response);
    }

    /// <summary>
    /// Download CSV template for bulk upload (Admin only)
    /// </summary>
    [HttpGet("bulk-template")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DownloadTemplate()
    {
        var bytes = await _mediator.Send(new GetBulkUploadTemplateQuery());
        return File(bytes, "text/csv", "books_bulk_upload_template.csv");
    }

    /// <summary>
    /// Upload cover image for a book (Admin only) - multipart/form-data
    /// </summary>
    [HttpPost("{id:guid}/cover")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UploadBookCover([FromRoute] Guid id, IFormFile file, CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
            return BadRequest(Bookstore.Application.Common.ApiResponse.ErrorResponse("No file uploaded", null, 400));

        var allowed = new[] { ".jpg", ".jpeg", ".png", ".gif" };
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowed.Contains(ext))
            return BadRequest(Bookstore.Application.Common.ApiResponse.ErrorResponse("Unsupported file type", null, 400));

        if (file.Length > 5 * 1024 * 1024)
            return BadRequest(Bookstore.Application.Common.ApiResponse.ErrorResponse("File too large (max 5MB)", null, 400));

        // Save using file storage service
        var uploadResult = await _fileStorage.SaveFileAsync(file.OpenReadStream(), file.FileName, Path.Combine("uploads", "covers"), file.ContentType, cancellationToken);

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var fullUrl = baseUrl.TrimEnd('/') + uploadResult.FileUrl;
        var thumbUrl = uploadResult.ThumbnailUrl != null ? baseUrl.TrimEnd('/') + uploadResult.ThumbnailUrl : null;

        var updateDto = new BookUpdateDto { CoverImageUrl = fullUrl };
        var updateResult = await _mediator.Send(new UpdateBookCommand(id, updateDto), cancellationToken);
        if (!updateResult.Success)
            return StatusCode(updateResult.StatusCode ?? 500, updateResult);

        return Ok(Bookstore.Application.Common.ApiResponse<object>.SuccessResponse(new
        {
            Url = fullUrl,
            ThumbnailUrl = thumbUrl
        }, "Cover image uploaded successfully", 200));
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
    [ProducesResponseType(typeof(ApiResponse<BookResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateBook(Guid id, [FromBody] BookUpdateDto dto, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Update book {BookId}", id);
        var response = await _mediator.Send(new UpdateBookCommand(id, dto), cancellationToken);
        return StatusCode(response.StatusCode ?? 200, response);
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
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteBook(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Delete book {BookId}", id);
        var response = await _mediator.Send(new DeleteBookCommand(id), cancellationToken);
        return StatusCode(response.StatusCode ?? 200, response);
    }
}
