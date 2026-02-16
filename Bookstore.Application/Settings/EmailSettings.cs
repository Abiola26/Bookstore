namespace Bookstore.Application.Settings;

public class EmailSettings
{
    public string? SmtpHost { get; set; }
    public int SmtpPort { get; set; } = 25;
    public bool EnableSsl { get; set; } = false;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? FromAddress { get; set; }
    public string? FromName { get; set; }
    public int ConfirmationTokenExpiryHours { get; set; } = 24;
    public int PasswordResetTokenExpiryHours { get; set; } = 2;
    public string? ConfirmationUrlOrigin { get; set; }
}
