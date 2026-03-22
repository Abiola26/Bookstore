using Bookstore.Application.Common;
using Bookstore.Application.DTOs;
using MediatR;
using AutoMapper;
using Bookstore.Application.Repositories;
using Bookstore.Domain.Entities;
using Bookstore.Domain.ValueObjects;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;

namespace Bookstore.Application.Features.Books.Commands;

public record BulkUploadBooksCommand(Stream CsvStream) : IRequest<ApiResponse<BulkUploadResultDto>>;

public class BulkUploadBooksHandler : IRequestHandler<BulkUploadBooksCommand, ApiResponse<BulkUploadResultDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public BulkUploadBooksHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<ApiResponse<BulkUploadResultDto>> Handle(BulkUploadBooksCommand request, CancellationToken cancellationToken)
    {
        var result = new BulkUploadResultDto();
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            HeaderValidated = null,
            MissingFieldFound = null,
            TrimOptions = TrimOptions.Trim
        };

        using var reader = new StreamReader(request.CsvStream);
        using var csv = new CsvReader(reader, config);

        try
        {
            var records = csv.GetRecordsAsync<dynamic>(cancellationToken);
            int rowNumber = 1;

            await foreach (var record in records)
            {
                rowNumber++;
                result.TotalProcessed++;

                try
                {
                    var dict = (IDictionary<string, object>)record;

                    string title = dict.TryGetValue("Title", out var t) ? t?.ToString() ?? "" : "";
                    string isbnStr = dict.TryGetValue("ISBN", out var i) ? i?.ToString() ?? "" : "";
                    string author = dict.TryGetValue("Author", out var a) ? a?.ToString() ?? "" : "";
                    string categoryName = dict.TryGetValue("Category", out var c) ? c?.ToString() ?? "" : "";
                    string priceStr = dict.TryGetValue("Price", out var p) ? p?.ToString() ?? "0" : "0";
                    string currency = dict.TryGetValue("Currency", out var curr) ? curr?.ToString() ?? "USD" : "USD";
                    string quantityStr = dict.TryGetValue("Quantity", out var q) ? q?.ToString() ?? "0" : "0";
                    string description = dict.TryGetValue("Description", out var d) ? d?.ToString() ?? "" : "";
                    string coverUrl = dict.TryGetValue("CoverImageUrl", out var curl) ? curl?.ToString() ?? "" : "";

                    if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(isbnStr) || string.IsNullOrWhiteSpace(categoryName))
                    {
                        result.Failed++;
                        result.Errors.Add(new BulkUploadErrorDto { RowNumber = rowNumber, Identifier = isbnStr, ErrorMessage = "Missing required fields (Title, ISBN, or Category)" });
                        continue;
                    }

                    if (await _unitOfWork.Books.ISBNExistsAsync(isbnStr, null, cancellationToken))
                    {
                        result.Failed++;
                        result.Errors.Add(new BulkUploadErrorDto { RowNumber = rowNumber, Identifier = isbnStr, ErrorMessage = "ISBN already exists" });
                        continue;
                    }

                    var category = await _unitOfWork.Categories.GetByNameAsync(categoryName, cancellationToken);
                    if (category == null)
                    {
                        category = new Category(categoryName);
                        await _unitOfWork.Categories.AddAsync(category, cancellationToken);
                        await _unitOfWork.SaveChangesAsync(cancellationToken);
                    }

                    decimal price = decimal.TryParse(priceStr, out var pr) ? pr : 0;
                    int quantity = int.TryParse(quantityStr, out var qty) ? qty : 0;

                    var isbn = new ISBN(isbnStr);
                    var money = new Money(price, currency);

                    var book = new Book(title, description, isbn, money, author, quantity, category.Id)
                    {
                        Category = category,
                        CoverImageUrl = coverUrl
                    };

                    await _unitOfWork.Books.AddAsync(book, cancellationToken);
                    result.Successful++;
                }
                catch (Exception ex)
                {
                    result.Failed++;
                    result.Errors.Add(new BulkUploadErrorDto { RowNumber = rowNumber, Identifier = "Unknown", ErrorMessage = ex.Message });
                }
            }

            if (result.Successful > 0)
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            return ApiResponse<BulkUploadResultDto>.SuccessResponse(result, $"Processed {result.TotalProcessed} books. {result.Successful} successful, {result.Failed} failed.");
        }
        catch (Exception ex)
        {
            return ApiResponse<BulkUploadResultDto>.ErrorResponse("Critical error during bulk upload: " + ex.Message, null, 500);
        }
    }
}
