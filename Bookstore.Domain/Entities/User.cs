using System;
using System.Collections.Generic;

namespace Bookstore.Domain.Entities;

public enum UserRole
{
    Admin,
    User
}

public class User : BaseEntity
{
    private readonly List<Order> _orders = new();

    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.User;

    public IReadOnlyCollection<Order> Orders => _orders.AsReadOnly();

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
