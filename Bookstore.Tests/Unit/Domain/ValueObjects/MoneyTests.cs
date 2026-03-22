using Bookstore.Domain.ValueObjects;
using FluentAssertions;

namespace Bookstore.Tests.Unit.Domain.ValueObjects;

public class MoneyTests
{
    [Fact]
    public void Constructor_ShouldRoundAmountToTwoDecimals()
    {
        var money = new Money(10.5555m, "USD");
        money.Amount.Should().Be(10.56m);
    }

    [Fact]
    public void Constructor_ShouldNormalizeCurrency()
    {
        var money = new Money(100, " usd ");
        money.Currency.Should().Be("USD");
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentOutOfRangeException_WhenAmountIsNegative()
    {
        Action act = () => new Money(-1, "USD");
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Addition_ShouldWork_WhenCurrenciesMatch()
    {
        var m1 = new Money(10, "USD");
        var m2 = new Money(5, "USD");
        var result = m1 + m2;
        result.Amount.Should().Be(15);
        result.Currency.Should().Be("USD");
    }

    [Fact]
    public void Addition_ShouldThrowInvalidOperationException_WhenCurrenciesMismatch()
    {
        var m1 = new Money(10, "USD");
        var m2 = new Money(10, "EUR");
        Action act = () => { var res = m1 + m2; };
        act.Should().Throw<InvalidOperationException>().WithMessage("Currency mismatch");
    }

    [Fact]
    public void Multiplication_ShouldCalculateCorrectly()
    {
        var money = new Money(10.5m, "USD");
        var result = money * 2;
        result.Amount.Should().Be(21.0m);
    }
}
