using Bookstore.Application.Services;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace Bookstore.Infrastructure.Services;

public class LocalFileStorageService : IFileStorageService
{
    private readonly ILogger<LocalFileStorageService> _logger;
    private readonly Bookstore.Application.Settings.FileSettings _fileSettings;

    public LocalFileStorageService(ILogger<LocalFileStorageService> logger, Microsoft.Extensions.Options.IOptions<Bookstore.Application.Settings.FileSettings> fileSettings)
    {
        _logger = logger;
        _fileSettings = fileSettings?.Value ?? new Bookstore.Application.Settings.FileSettings();
    }
    public async Task<string> SaveFileAsync(System.IO.Stream fileStream, string fileName, string folderName, string contentType, CancellationToken cancellationToken = default)
    {
        if (fileStream == null || !fileStream.CanRead)
            throw new ArgumentException("File stream is empty or not readable", nameof(fileStream));
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        if (!_fileSettings.AllowedExtensions.Contains(ext))
            throw new InvalidOperationException("File type not allowed");

        var maxBytes = _fileSettings.MaxFileSizeMB * 1024 * 1024;
        if (fileStream.Length > maxBytes)
            throw new InvalidOperationException($"File exceeds maximum size of {_fileSettings.MaxFileSizeMB} MB");

        var webRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var uploadsFolder = Path.Combine(webRoot, folderName);
        if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

        var uniqueName = $"{Guid.NewGuid()}{ext}";
        var filePath = Path.Combine(uploadsFolder, uniqueName);

        // Save original
        using (var outStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await fileStream.CopyToAsync(outStream, cancellationToken);
        }

        try
        {
            // Create thumbnail
            using Image image = Image.Load(filePath);
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Mode = ResizeMode.Max,
                Size = new SixLabors.ImageSharp.Size(_fileSettings.ThumbnailMaxWidth, _fileSettings.ThumbnailMaxHeight)
            }));

            var thumbName = Path.GetFileNameWithoutExtension(uniqueName) + "_thumb" + ext;
            var thumbPath = Path.Combine(uploadsFolder, thumbName);
            // Save synchronously to avoid encoder overload issues in this environment
            image.Save(thumbPath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate thumbnail for {File}", filePath);
        }

        return $"/{folderName}/{uniqueName}";
    }

    public Task DeleteFileAsync(string fileUrl, string folderName)
    {
        if (string.IsNullOrEmpty(fileUrl)) return Task.CompletedTask;

        try
        {
            var fileName = Path.GetFileName(fileUrl);
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", folderName, fileName);
            if (File.Exists(filePath)) File.Delete(filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file {FileUrl}", fileUrl);
        }

        return Task.CompletedTask;
    }
}
