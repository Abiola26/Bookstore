namespace Bookstore.Application.DTOs;

public class ConfirmEmailDto
{
    public Guid UserId { get; set; }
    public string Token { get; set; } = string.Empty;
}
