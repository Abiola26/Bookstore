namespace Bookstore.Application.DTOs;

public class BulkUploadResultDto
{
    public int TotalProcessed { get; set; }
    public int Successful { get; set; }
    public int Failed { get; set; }
    public List<BulkUploadErrorDto> Errors { get; set; } = new();
}

public class BulkUploadErrorDto
{
    public int RowNumber { get; set; }
    public string Identifier { get; set; } = string.Empty; // e.g., ISBN or Title
    public string ErrorMessage { get; set; } = string.Empty;
}
