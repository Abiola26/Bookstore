namespace Bookstore.Application.Services;

public class FileUploadResult
{
    public string FileUrl { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
}
