using System;

namespace Bookstore.Domain.ValueObjects;

public sealed class Money : IEquatable<Money>
{
    public decimal Amount { get; }
    public string Currency { get; }

    public Money(decimal amount, string currency = "USD")
    {
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
        Amount = decimal.Round(amount, 2);
        Currency = currency;
    }

    public static Money Zero(string currency = "USD") => new Money(0m, currency);

    public static Money operator *(Money m, int multiplier)
    {
        return new Money(m.Amount * multiplier, m.Currency);
    }

    public static Money operator *(int multiplier, Money m) => m * multiplier;

    public override bool Equals(object? obj) => Equals(obj as Money);
    public bool Equals(Money? other) => other is not null && Amount == other.Amount && Currency == other.Currency;
    public override int GetHashCode() => HashCode.Combine(Amount, Currency);

    public static Money operator +(Money a, Money b)
    {
        if (a.Currency != b.Currency) throw new InvalidOperationException("Currency mismatch");
        return new Money(a.Amount + b.Amount, a.Currency);
    }
}
