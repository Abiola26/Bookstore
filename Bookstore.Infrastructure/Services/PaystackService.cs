using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;
using Bookstore.Application.Common;
using Bookstore.Application.DTOs;
using Bookstore.Application.Services;
using Bookstore.Application.Settings;
using Microsoft.Extensions.Options;

namespace Bookstore.Infrastructure.Services;

public class PaystackService : IPaystackService
{
    private readonly HttpClient _httpClient;
    private readonly PaystackSettings _settings;

    public PaystackService(HttpClient httpClient, IOptions<PaystackSettings> settings)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        
        _httpClient.BaseAddress = new Uri(_settings.BaseUrl);
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _settings.SecretKey);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<ApiResponse<PaystackResponseDto>> InitializeTransactionAsync(PaystackInitializeDto dto)
    {
        try
        {
            var body = new
            {
                email = dto.Email,
                amount = (int)(dto.Amount * 100), // Paystack expects amount in kobo/cents
                callback_url = "http://localhost:3000/checkout/verify",
                metadata = new { order_id = dto.OrderId }
            };

            var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("/transaction/initialize", content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return ApiResponse<PaystackResponseDto>.ErrorResponse($"Paystack Error: {error}", null, (int)response.StatusCode);
            }

            var result = await response.Content.ReadAsStreamAsync();
            using var doc = await JsonDocument.ParseAsync(result);
            var data = doc.RootElement.GetProperty("data");

            var paystackResponse = new PaystackResponseDto
            {
                AuthorizationUrl = data.GetProperty("authorization_url").GetString() ?? string.Empty,
                Reference = data.GetProperty("reference").GetString() ?? string.Empty
            };

            return ApiResponse<PaystackResponseDto>.SuccessResponse(paystackResponse, "Transaction initialized");
        }
        catch (Exception ex)
        {
            return ApiResponse<PaystackResponseDto>.ErrorResponse($"Initialization failed: {ex.Message}");
        }
    }

    public async Task<ApiResponse<bool>> VerifyTransactionAsync(string reference)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/transaction/verify/{reference}");
            if (!response.IsSuccessStatusCode)
            {
                return ApiResponse<bool>.ErrorResponse("Verification failed on Paystack server", null, (int)response.StatusCode);
            }

            var result = await response.Content.ReadAsStreamAsync();
            using var doc = await JsonDocument.ParseAsync(result);
            var status = doc.RootElement.GetProperty("data").GetProperty("status").GetString();

            if (status == "success")
            {
                return ApiResponse<bool>.SuccessResponse(true, "Transaction verified successfully");
            }

            return ApiResponse<bool>.ErrorResponse($"Transaction status: {status}", null);
        }
        catch (Exception ex)
        {
            return ApiResponse<bool>.ErrorResponse($"Verification failed: {ex.Message}", null);
        }
    }
}
