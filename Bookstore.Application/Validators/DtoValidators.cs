using Bookstore.Application.DTOs;

namespace Bookstore.Application.Validators;

public class BookCreateDtoValidator
{
    public List<string> Validate(BookCreateDto dto)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(dto.Title) || dto.Title.Length > 200)
            errors.Add("Title is required and must not exceed 200 characters.");

        if (string.IsNullOrWhiteSpace(dto.Description))
            errors.Add("Description is required.");

        if (string.IsNullOrWhiteSpace(dto.ISBN) || dto.ISBN.Length > 20)
            errors.Add("ISBN is required and must not exceed 20 characters.");

        if (string.IsNullOrWhiteSpace(dto.Author) || dto.Author.Length > 150)
            errors.Add("Author is required and must not exceed 150 characters.");

        if (dto.Price < 0)
            errors.Add("Price must be greater than or equal to 0.");

        if (dto.TotalQuantity < 0)
            errors.Add("Total Quantity must be greater than or equal to 0.");

        if (dto.CategoryId == Guid.Empty)
            errors.Add("Category ID is required.");

        return errors;
    }
}

public class BookUpdateDtoValidator
{
    public List<string> Validate(BookUpdateDto dto)
    {
        var errors = new List<string>();

        if (dto.Title != null && (dto.Title.Length == 0 || dto.Title.Length > 200))
            errors.Add("Title must not exceed 200 characters.");

        if (dto.Author != null && (dto.Author.Length == 0 || dto.Author.Length > 150))
            errors.Add("Author must not exceed 150 characters.");

        if (dto.Price.HasValue && dto.Price < 0)
            errors.Add("Price must be greater than or equal to 0.");

        if (dto.TotalQuantity.HasValue && dto.TotalQuantity < 0)
            errors.Add("Total Quantity must be greater than or equal to 0.");

        return errors;
    }
}

public class UserRegisterDtoValidator
{
    public List<string> Validate(UserRegisterDto dto)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(dto.FullName) || dto.FullName.Length > 150)
            errors.Add("Full Name is required and must not exceed 150 characters.");

        if (string.IsNullOrWhiteSpace(dto.Email) || !IsValidEmail(dto.Email) || dto.Email.Length > 256)
            errors.Add("Email is required, must be valid, and must not exceed 256 characters.");

        // Stronger password policy:
        // - Minimum length 12
        // - At least one uppercase letter
        // - At least one lowercase letter
        // - At least one digit
        // - At least one special character
        if (string.IsNullOrWhiteSpace(dto.Password))
        {
            errors.Add("Password is required.");
        }
        else
        {
            if (dto.Password.Length < 12)
                errors.Add("Password must be at least 12 characters long.");

            if (!dto.Password.Any(char.IsUpper))
                errors.Add("Password must contain at least one uppercase letter.");

            if (!dto.Password.Any(char.IsLower))
                errors.Add("Password must contain at least one lowercase letter.");

            if (!dto.Password.Any(char.IsDigit))
                errors.Add("Password must contain at least one digit.");

            if (!dto.Password.Any(ch => !char.IsLetterOrDigit(ch)))
                errors.Add("Password must contain at least one special character.");

            // Simple blacklist check (case-insensitive)
            var blacklist = new[] { "password", "123456", "12345678", "qwerty", "letmein" };
            if (blacklist.Any(b => dto.Password.IndexOf(b, StringComparison.OrdinalIgnoreCase) >= 0))
                errors.Add("Password is too common or easily guessable.");
        }

        if (dto.PhoneNumber != null && dto.PhoneNumber.Length > 20)
            errors.Add("Phone Number must not exceed 20 characters.");

        return errors;
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}

public class UserLoginDtoValidator
{
    public List<string> Validate(UserLoginDto dto)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(dto.Email))
            errors.Add("Email is required.");

        if (string.IsNullOrWhiteSpace(dto.Password))
            errors.Add("Password is required.");

        return errors;
    }
}

public class CategoryCreateDtoValidator
{
    public List<string> Validate(CategoryCreateDto dto)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(dto.Name) || dto.Name.Length > 100)
            errors.Add("Category Name is required and must not exceed 100 characters.");

        return errors;
    }
}

public class OrderCreateDtoValidator
{
    public List<string> Validate(OrderCreateDto dto)
    {
        var errors = new List<string>();

        if (dto.Items == null || dto.Items.Count == 0)
            errors.Add("At least one item is required for an order.");

        foreach (var item in dto.Items ?? new List<OrderItemCreateDto>())
        {
            if (item.BookId == Guid.Empty)
                errors.Add("Book ID is required for each item.");

            if (item.Quantity <= 0)
                errors.Add("Quantity must be greater than 0.");
        }

        return errors;
    }
}
