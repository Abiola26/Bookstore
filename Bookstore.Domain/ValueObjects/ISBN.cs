using System;
using System.Text.RegularExpressions;

namespace Bookstore.Domain.ValueObjects;

public sealed class ISBN : IEquatable<ISBN>
{
    private static readonly Regex Allowed = new("^[0-9Xx-]{1,20}$", RegexOptions.Compiled);

    public string Value { get; }

    public ISBN(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("ISBN cannot be empty", nameof(value));
        if (value.Length > 20) throw new ArgumentOutOfRangeException(nameof(value), "ISBN max length is 20");
        if (!Allowed.IsMatch(value)) throw new ArgumentException("ISBN contains invalid characters", nameof(value));

        Value = value;
    }

    public override bool Equals(object? obj) => Equals(obj as ISBN);
    public bool Equals(ISBN? other) => other is not null && string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);
    public override int GetHashCode() => Value.ToUpperInvariant().GetHashCode();

    public override string ToString() => Value;

    public static implicit operator string?(ISBN? isbn) => isbn?.Value;
    public static explicit operator ISBN(string value) => new ISBN(value);
}
