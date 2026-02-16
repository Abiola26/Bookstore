using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Bookstore.Domain.Entities;

public enum UserRole
{
    Admin,
    User
}

public sealed class User : BaseEntity
{
    private readonly List<Order> _orders = new();

    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool EmailConfirmed { get; set; } = false;
    public string? EmailConfirmationToken { get; set; }
    public DateTimeOffset? EmailConfirmationTokenExpiresAt { get; set; }
    public string? PasswordResetToken { get; set; }
    public DateTimeOffset? PasswordResetTokenExpiresAt { get; set; }
    public string? PhoneNumber { get; set; }
    public string PasswordHash { get; private set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.User;

    public IReadOnlyCollection<Order> Orders => _orders.AsReadOnly();

    public void AddOrder(Order order)
    {
        if (order is null) throw new ArgumentNullException(nameof(order));
        _orders.Add(order);
    }

    public bool RemoveOrder(Order order)
    {
        if (order is null) throw new ArgumentNullException(nameof(order));
        return _orders.Remove(order);
    }

    public void UpdatePassword(string newPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash)) throw new ArgumentException("Password hash is required", nameof(newPasswordHash));
        PasswordHash = newPasswordHash;
    }

    public void UpdatePhoneNumber(string? phoneNumber)
    {
        if (phoneNumber is null)
        {
            PhoneNumber = null;
            return;
        }

        var normalized = phoneNumber.Trim();
        // simple validation: allow digits, spaces, +, -, parentheses and require length >= 7
        if (!Regex.IsMatch(normalized, @"^[0-9+()\-\s]{7,}$"))
            throw new ArgumentException("Invalid phone number format", nameof(phoneNumber));

        PhoneNumber = normalized;
    }

    private User() { }

    public User(string fullName, string email, string passwordHash, UserRole role = UserRole.User)
    {
        if (string.IsNullOrWhiteSpace(fullName)) throw new ArgumentException("Full name is required", nameof(fullName));
        if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException("Email is required", nameof(email));
        if (string.IsNullOrWhiteSpace(passwordHash)) throw new ArgumentException("Password hash is required", nameof(passwordHash));

        FullName = fullName;
        Email = email;
        PasswordHash = passwordHash;
        Role = role;
    }
}
