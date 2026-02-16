using System;
using System.Collections.Generic;
using System.Linq;

namespace Bookstore.Application.Validators;

public static class PasswordPolicy
{
    public static List<string> Validate(string? password)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(password))
        {
            errors.Add("Password is required.");
            return errors;
        }

        if (password.Length < 12)
            errors.Add("Password must be at least 12 characters long.");

        if (!password.Any(char.IsUpper))
            errors.Add("Password must contain at least one uppercase letter.");

        if (!password.Any(char.IsLower))
            errors.Add("Password must contain at least one lowercase letter.");

        if (!password.Any(char.IsDigit))
            errors.Add("Password must contain at least one digit.");

        if (!password.Any(ch => !char.IsLetterOrDigit(ch)))
            errors.Add("Password must contain at least one special character.");

        var blacklist = new[] { "password", "123456", "12345678", "qwerty", "letmein" };
        if (blacklist.Any(b => password.IndexOf(b, StringComparison.OrdinalIgnoreCase) >= 0))
            errors.Add("Password is too common or easily guessable.");

        return errors;
    }
}
