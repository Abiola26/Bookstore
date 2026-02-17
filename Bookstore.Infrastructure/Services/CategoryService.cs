using Bookstore.Application.DTOs;
using Bookstore.Application.Common;
using Bookstore.Application.Services;
using Bookstore.Application.Repositories;
using Bookstore.Application.Validators;
using Microsoft.Extensions.Logging;
using Bookstore.Domain.Entities;

namespace Bookstore.Infrastructure.Services;

public class CategoryService : ICategoryService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CategoryService> _logger;
    private readonly CategoryCreateDtoValidator _createValidator;

    public CategoryService(IUnitOfWork unitOfWork, ILogger<CategoryService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _createValidator = new CategoryCreateDtoValidator();
    }

    public async Task<ApiResponse<CategoryResponseDto>> GetCategoryByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var category = await _unitOfWork.Categories.GetByIdAsync(id, cancellationToken);
            if (category == null)
                return ApiResponse<CategoryResponseDto>.ErrorResponse("Category not found", null, 404);

            var bookCount = await _unitOfWork.Books.GetCategoryBookCountAsync(id, cancellationToken);
            return ApiResponse<CategoryResponseDto>.SuccessResponse(MapToDto(category, bookCount));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving category {CategoryId}", id);
            return ApiResponse<CategoryResponseDto>.ErrorResponse("An error occurred while retrieving the category", null, 500);
        }
    }

    public async Task<ApiResponse<ICollection<CategoryResponseDto>>> GetAllCategoriesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var categories = await _unitOfWork.Categories.GetAllAsync(cancellationToken);
            var dtos = new List<CategoryResponseDto>();

            foreach (var category in categories)
            {
                var bookCount = await _unitOfWork.Books.GetCategoryBookCountAsync(category.Id, cancellationToken);
                dtos.Add(MapToDto(category, bookCount));
            }

            return ApiResponse<ICollection<CategoryResponseDto>>.SuccessResponse(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all categories");
            return ApiResponse<ICollection<CategoryResponseDto>>.ErrorResponse("An error occurred while retrieving categories", null, 500);
        }
    }

    public async Task<ApiResponse<CategoryResponseDto>> CreateCategoryAsync(CategoryCreateDto dto, CancellationToken cancellationToken = default)
    {
        var validationErrors = _createValidator.Validate(dto);
        if (validationErrors.Count > 0)
            return ApiResponse<CategoryResponseDto>.ErrorResponse("Validation failed", validationErrors, 400);

        try
        {
            // Check if name already exists
            var nameExists = await _unitOfWork.Categories.NameExistsAsync(dto.Name, null, cancellationToken);
            if (nameExists)
                return ApiResponse<CategoryResponseDto>.ErrorResponse("Category name already exists", new List<string> { "A category with this name already exists" }, 409);

            var category = new Category(dto.Name);
            await _unitOfWork.Categories.AddAsync(category, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return ApiResponse<CategoryResponseDto>.SuccessResponse(MapToDto(category, 0), "Category created successfully", 201);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating category: {CategoryName}", dto.Name);
            return ApiResponse<CategoryResponseDto>.ErrorResponse("An error occurred while creating the category", null, 500);
        }
    }

    public async Task<ApiResponse<CategoryResponseDto>> UpdateCategoryAsync(Guid id, CategoryUpdateDto dto, CancellationToken cancellationToken = default)
    {
        try
        {
            var category = await _unitOfWork.Categories.GetByIdAsync(id, cancellationToken);
            if (category == null)
                return ApiResponse<CategoryResponseDto>.ErrorResponse("Category not found", null, 404);

            if (!string.IsNullOrWhiteSpace(dto.Name))
            {
                var nameExists = await _unitOfWork.Categories.NameExistsAsync(dto.Name, id, cancellationToken);
                if (nameExists)
                    return ApiResponse<CategoryResponseDto>.ErrorResponse("Category name already exists", new List<string> { "A category with this name already exists" }, 409);

                category.Name = dto.Name;
            }

            category.UpdatedAt = DateTimeOffset.UtcNow;
            _unitOfWork.Categories.Update(category);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var bookCount = await _unitOfWork.Books.GetCategoryBookCountAsync(id, cancellationToken);
            return ApiResponse<CategoryResponseDto>.SuccessResponse(MapToDto(category, bookCount), "Category updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating category {CategoryId}", id);
            return ApiResponse<CategoryResponseDto>.ErrorResponse("An error occurred while updating the category", null, 500);
        }
    }

    public async Task<ApiResponse> DeleteCategoryAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var category = await _unitOfWork.Categories.GetByIdAsync(id, cancellationToken);
            if (category == null)
                return ApiResponse.ErrorResponse("Category not found", null, 404);

            var bookCount = await _unitOfWork.Books.GetCategoryBookCountAsync(id, cancellationToken);
            if (bookCount > 0)
                return ApiResponse.ErrorResponse("Cannot delete category with books", new List<string> { "Please delete all books in this category first" }, 400);

            _unitOfWork.Categories.Delete(category);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return ApiResponse.SuccessResponse("Category deleted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting category {CategoryId}", id);
            return ApiResponse.ErrorResponse("An error occurred while deleting the category", null, 500);
        }
    }

    private static CategoryResponseDto MapToDto(Category category, int bookCount)
    {
        return new CategoryResponseDto
        {
            Id = category.Id,
            Name = category.Name,
            BookCount = bookCount,
            CreatedAt = category.CreatedAt,
            UpdatedAt = category.UpdatedAt
        };
    }
}
