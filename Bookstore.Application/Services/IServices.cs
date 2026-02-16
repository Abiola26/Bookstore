using Bookstore.Application.DTOs;
using Bookstore.Application.Common;

namespace Bookstore.Application.Services;

public interface IBookService
{
    Task<ApiResponse<BookResponseDto>> GetBookByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ApiResponse<ICollection<BookResponseDto>>> GetAllBooksAsync(CancellationToken cancellationToken = default);
    Task<ApiResponse<BookPaginatedResponseDto>> GetBooksPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<ApiResponse<ICollection<BookResponseDto>>> SearchByTitleAsync(string title, CancellationToken cancellationToken = default);
    Task<ApiResponse<BookPaginatedResponseDto>> GetBooksByCategoryAsync(Guid categoryId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<ApiResponse<BookResponseDto>> CreateBookAsync(BookCreateDto dto, CancellationToken cancellationToken = default);
    Task<ApiResponse<BookResponseDto>> UpdateBookAsync(Guid id, BookUpdateDto dto, CancellationToken cancellationToken = default);
    Task<ApiResponse> DeleteBookAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface ICategoryService
{
    Task<ApiResponse<CategoryResponseDto>> GetCategoryByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ApiResponse<ICollection<CategoryResponseDto>>> GetAllCategoriesAsync(CancellationToken cancellationToken = default);
    Task<ApiResponse<CategoryResponseDto>> CreateCategoryAsync(CategoryCreateDto dto, CancellationToken cancellationToken = default);
    Task<ApiResponse<CategoryResponseDto>> UpdateCategoryAsync(Guid id, CategoryUpdateDto dto, CancellationToken cancellationToken = default);
    Task<ApiResponse> DeleteCategoryAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface IAuthenticationService
{
    Task<ApiResponse<AuthResponseDto>> RegisterAsync(UserRegisterDto dto, CancellationToken cancellationToken = default);
    Task<ApiResponse<AuthResponseDto>> LoginAsync(UserLoginDto dto, CancellationToken cancellationToken = default);
    Task<ApiResponse<UserResponseDto>> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default);
    string GenerateJwtToken(Guid userId, string email, string fullName, string role);
    Task<ApiResponse> ConfirmEmailAsync(Guid userId, string token, CancellationToken cancellationToken = default);
    Task<ApiResponse> ResendConfirmationAsync(string email, CancellationToken cancellationToken = default);
    Task<ApiResponse> RequestPasswordResetAsync(string email, CancellationToken cancellationToken = default);
    Task<ApiResponse> ResetPasswordAsync(Guid userId, string token, string newPassword, CancellationToken cancellationToken = default);
    Task<ApiResponse> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default);
}

public interface IOrderService
{
    Task<ApiResponse<OrderResponseDto>> GetOrderByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ApiResponse<ICollection<OrderResponseDto>>> GetUserOrdersAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<ApiResponse<ICollection<OrderResponseDto>>> GetUserOrdersPaginatedAsync(Guid userId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<ApiResponse<OrderResponseDto>> CreateOrderAsync(Guid userId, OrderCreateDto dto, CancellationToken cancellationToken = default);
    Task<ApiResponse<OrderResponseDto>> UpdateOrderStatusAsync(Guid orderId, OrderUpdateStatusDto dto, CancellationToken cancellationToken = default);
    Task<ApiResponse> CancelOrderAsync(Guid orderId, CancellationToken cancellationToken = default);
}
