using Bookstore.Application.Common;
using Bookstore.Application.DTOs;

namespace Bookstore.Application.Services;

public interface IPaystackService
{
    Task<ApiResponse<PaystackResponseDto>> InitializeTransactionAsync(PaystackInitializeDto dto);
    Task<ApiResponse<bool>> VerifyTransactionAsync(string reference);
}
