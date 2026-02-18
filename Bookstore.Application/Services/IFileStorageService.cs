namespace Bookstore.Application.Services;

public interface IFileStorageService
{
    /// <summary>
    /// Saves a file to the storage and returns the relative path/URL
    /// </summary>
    Task<string> SaveFileAsync(System.IO.Stream fileStream, string fileName, string folderName, string contentType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a file from the storage
    /// </summary>
    Task DeleteFileAsync(string fileUrl, string folderName);
}
