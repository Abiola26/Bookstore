using Bookstore.Application.DTOs;
using Bookstore.Application.Common;
using Bookstore.Application.Services;
using Bookstore.Application.Exceptions;
using Bookstore.Application.Repositories;
using Bookstore.Application.Validators;
using Bookstore.Domain.Entities;
using Bookstore.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Infrastructure.Services;

public class BookService : IBookService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly BookCreateDtoValidator _createValidator;
    private readonly BookUpdateDtoValidator _updateValidator;

    public BookService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
        _createValidator = new BookCreateDtoValidator();
        _updateValidator = new BookUpdateDtoValidator();
    }

    public async Task<ApiResponse<BookResponseDto>> GetBookByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var book = await _unitOfWork.Books.GetByIdAsync(id, cancellationToken);
            if (book == null)
                return ApiResponse<BookResponseDto>.ErrorResponse("Book not found", null, 404);

            return ApiResponse<BookResponseDto>.SuccessResponse(MapToDto(book));
        }
        catch (Exception ex)
        {
            return ApiResponse<BookResponseDto>.ErrorResponse($"Failed to retrieve book: {ex.Message}", null, 500);
        }
    }

    public async Task<ApiResponse<ICollection<BookResponseDto>>> GetAllBooksAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var books = await _unitOfWork.Books.GetAllAsync(cancellationToken);
            var dtos = books.Select(MapToDto).ToList();
            return ApiResponse<ICollection<BookResponseDto>>.SuccessResponse(dtos);
        }
        catch (Exception ex)
        {
            return ApiResponse<ICollection<BookResponseDto>>.ErrorResponse($"Failed to retrieve books: {ex.Message}", null, 500);
        }
    }

    public async Task<ApiResponse<BookPaginatedResponseDto>> GetBooksPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        try
        {
            if (pageNumber < 1 || pageSize < 1)
                return ApiResponse<BookPaginatedResponseDto>.ErrorResponse("Page number and page size must be greater than 0", null, 400);

            var books = await _unitOfWork.Books.GetPaginatedAsync(pageNumber, pageSize, cancellationToken);
            var totalCount = await _unitOfWork.Books.GetTotalCountAsync(cancellationToken);

            var response = new BookPaginatedResponseDto
            {
                Data = books.Select(MapToDto).ToList(),
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            };

            return ApiResponse<BookPaginatedResponseDto>.SuccessResponse(response);
        }
        catch (Exception ex)
        {
            return ApiResponse<BookPaginatedResponseDto>.ErrorResponse($"Failed to retrieve books: {ex.Message}", null, 500);
        }
    }

    public async Task<ApiResponse<ICollection<BookResponseDto>>> SearchByTitleAsync(string title, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(title))
                return ApiResponse<ICollection<BookResponseDto>>.ErrorResponse("Search title is required", null, 400);

            var books = await _unitOfWork.Books.SearchByTitleAsync(title, cancellationToken);
            var dtos = books.Select(MapToDto).ToList();
            return ApiResponse<ICollection<BookResponseDto>>.SuccessResponse(dtos);
        }
        catch (Exception ex)
        {
            return ApiResponse<ICollection<BookResponseDto>>.ErrorResponse($"Search failed: {ex.Message}", null, 500);
        }
    }

    public async Task<ApiResponse<BookPaginatedResponseDto>> GetBooksByCategoryAsync(Guid categoryId, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        try
        {
            if (pageNumber < 1 || pageSize < 1)
                return ApiResponse<BookPaginatedResponseDto>.ErrorResponse("Page number and page size must be greater than 0", null, 400);

            if (categoryId == Guid.Empty)
                return ApiResponse<BookPaginatedResponseDto>.ErrorResponse("Category ID is required", null, 400);

            var category = await _unitOfWork.Categories.GetByIdAsync(categoryId, cancellationToken);
            if (category == null)
                return ApiResponse<BookPaginatedResponseDto>.ErrorResponse("Category not found", null, 404);

            var books = await _unitOfWork.Books.GetPaginatedByCategoryAsync(categoryId, pageNumber, pageSize, cancellationToken);
            var totalCount = await _unitOfWork.Books.GetCategoryBookCountAsync(categoryId, cancellationToken);

            var response = new BookPaginatedResponseDto
            {
                Data = books.Select(MapToDto).ToList(),
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            };

            return ApiResponse<BookPaginatedResponseDto>.SuccessResponse(response);
        }
        catch (Exception ex)
        {
            return ApiResponse<BookPaginatedResponseDto>.ErrorResponse($"Failed to retrieve books: {ex.Message}", null, 500);
        }
    }

    public async Task<ApiResponse<BookResponseDto>> CreateBookAsync(BookCreateDto dto, CancellationToken cancellationToken = default)
    {
        var validationErrors = _createValidator.Validate(dto);
        if (validationErrors.Count > 0)
            return ApiResponse<BookResponseDto>.ErrorResponse("Validation failed", validationErrors, 400);

        try
        {
            // Check ISBN uniqueness
            var isbnExists = await _unitOfWork.Books.ISBNExistsAsync(dto.ISBN, null, cancellationToken);
            if (isbnExists)
                return ApiResponse<BookResponseDto>.ErrorResponse("ISBN already exists", new List<string> { "A book with this ISBN already exists" }, 409);

            // Verify category exists
            var category = await _unitOfWork.Categories.GetByIdAsync(dto.CategoryId, cancellationToken);
            if (category == null)
                return ApiResponse<BookResponseDto>.ErrorResponse("Category not found", null, 404);

            var isbn = new ISBN(dto.ISBN);
            var price = new Money(dto.Price, dto.Currency);

            var book = new Book(
                dto.Title,
                dto.Description,
                isbn,
                price,
                dto.Author,
                dto.TotalQuantity,
                dto.CategoryId
            )
            {
                Publisher = dto.Publisher,
                PublicationDate = dto.PublicationDate,
                Pages = dto.Pages,
                Language = dto.Language,
                CoverImageUrl = dto.CoverImageUrl
            };

            await _unitOfWork.Books.AddAsync(book, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return ApiResponse<BookResponseDto>.SuccessResponse(MapToDto(book), "Book created successfully", 201);
        }
        catch (Exception ex)
        {
            return ApiResponse<BookResponseDto>.ErrorResponse($"Failed to create book: {ex.Message}", null, 500);
        }
    }

    public async Task<ApiResponse<BookResponseDto>> UpdateBookAsync(Guid id, BookUpdateDto dto, CancellationToken cancellationToken = default)
    {
        var validationErrors = _updateValidator.Validate(dto);
        if (validationErrors.Count > 0)
            return ApiResponse<BookResponseDto>.ErrorResponse("Validation failed", validationErrors, 400);

        try
        {
            var book = await _unitOfWork.Books.GetByIdAsync(id, cancellationToken);
            if (book == null)
                return ApiResponse<BookResponseDto>.ErrorResponse("Book not found", null, 404);

            if (!string.IsNullOrWhiteSpace(dto.Title))
                book.Title = dto.Title;

            if (!string.IsNullOrWhiteSpace(dto.Description))
                book.Description = dto.Description;

            if (!string.IsNullOrWhiteSpace(dto.Author))
                book.Author = dto.Author;

            if (!string.IsNullOrWhiteSpace(dto.Publisher))
                book.Publisher = dto.Publisher;

            if (dto.PublicationDate.HasValue)
                book.PublicationDate = dto.PublicationDate;

            if (dto.Price.HasValue)
            {
                var currency = dto.Currency ?? book.Price.Currency;
                book.Price = new Money(dto.Price.Value, currency);
            }

            if (dto.Pages.HasValue)
                book.Pages = dto.Pages.Value;

            if (!string.IsNullOrWhiteSpace(dto.Language))
                book.Language = dto.Language;

            if (!string.IsNullOrWhiteSpace(dto.CoverImageUrl))
                book.CoverImageUrl = dto.CoverImageUrl;

            if (dto.TotalQuantity.HasValue)
                book.TotalQuantity = dto.TotalQuantity.Value;

            if (dto.CategoryId.HasValue)
            {
                var category = await _unitOfWork.Categories.GetByIdAsync(dto.CategoryId.Value, cancellationToken);
                if (category == null)
                    return ApiResponse<BookResponseDto>.ErrorResponse("Category not found", null, 404);

                book.CategoryId = dto.CategoryId.Value;
            }

            book.UpdatedAt = DateTimeOffset.UtcNow;
            _unitOfWork.Books.Update(book);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return ApiResponse<BookResponseDto>.SuccessResponse(MapToDto(book), "Book updated successfully");
        }
        catch (Exception ex)
        {
            return ApiResponse<BookResponseDto>.ErrorResponse($"Failed to update book: {ex.Message}", null, 500);
        }
    }

    public async Task<ApiResponse> DeleteBookAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var book = await _unitOfWork.Books.GetByIdAsync(id, cancellationToken);
            if (book == null)
                return ApiResponse.ErrorResponse("Book not found", null, 404);

            _unitOfWork.Books.Delete(book);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return ApiResponse.SuccessResponse("Book deleted successfully");
        }
        catch (Exception ex)
        {
            return ApiResponse.ErrorResponse($"Failed to delete book: {ex.Message}", null, 500);
        }
    }

    private static BookResponseDto MapToDto(Book book)
    {
        return new BookResponseDto
        {
            Id = book.Id,
            Title = book.Title,
            Description = book.Description,
            ISBN = book.ISBN.ToString(),
            Publisher = book.Publisher,
            PublicationDate = book.PublicationDate,
            Price = book.Price.Amount,
            Currency = book.Price.Currency,
            Author = book.Author,
            Pages = book.Pages,
            Language = book.Language,
            CoverImageUrl = book.CoverImageUrl,
            TotalQuantity = book.TotalQuantity,
            CategoryId = book.CategoryId,
            CategoryName = book.Category?.Name ?? string.Empty,
            CreatedAt = book.CreatedAt,
            UpdatedAt = book.UpdatedAt
        };
    }
}
