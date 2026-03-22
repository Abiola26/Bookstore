using Bookstore.Application.Common;
using MediatR;

namespace Bookstore.Application.Features.Books.Queries;

public record GetBulkUploadTemplateQuery : IRequest<byte[]>;

public class GetBulkUploadTemplateHandler : IRequestHandler<GetBulkUploadTemplateQuery, byte[]>
{
    public Task<byte[]> Handle(GetBulkUploadTemplateQuery request, CancellationToken cancellationToken)
    {
        var header = "Title,Author,ISBN,Price,Currency,Quantity,Description,Category,CoverImageUrl\n";
        var sampleRow = "\"The Great Gatsby\",\"F. Scott Fitzgerald\",\"9780743273565\",15.99,USD,100,\"A classic novel about the American Dream.\",\"Classic Literature\",\"\"";
        var bytes = System.Text.Encoding.UTF8.GetBytes(header + sampleRow);
        return Task.FromResult(bytes);
    }
}
