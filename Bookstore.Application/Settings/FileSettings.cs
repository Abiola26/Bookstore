namespace Bookstore.Application.Settings;

public class FileSettings
{
    public int MaxFileSizeMB { get; set; } = 5;
    public string[] AllowedExtensions { get; set; } = new[] { ".jpg", ".jpeg", ".png", ".gif" };
    public int ThumbnailMaxWidth { get; set; } = 300;
    public int ThumbnailMaxHeight { get; set; } = 300;
    public string UploadsFolder { get; set; } = "uploads/covers";
}
