using Bookstore.Domain.ValueObjects;
using FluentAssertions;

namespace Bookstore.Tests.Unit.Domain.ValueObjects;

public class MoneyExtendedTests
{
    [Fact]
    public void Zero_ShouldReturnMoneyWithZeroAmount()
    {
        var zero = Money.Zero();
        zero.Amount.Should().Be(0m);
        zero.Currency.Should().Be("USD");
    }

    [Fact]
    public void Zero_ShouldRespectCustomCurrency()
    {
        var zero = Money.Zero("EUR");
        zero.Currency.Should().Be("EUR");
    }

    [Fact]
    public void Equals_ShouldReturnTrue_WhenAmountAndCurrencyMatch()
    {
        var a = new Money(10m, "USD");
        var b = new Money(10m, "USD");
        a.Equals(b).Should().BeTrue();
    }

    [Fact]
    public void Equals_ShouldReturnFalse_WhenAmountDiffers()
    {
        var a = new Money(10m, "USD");
        var b = new Money(20m, "USD");
        a.Equals(b).Should().BeFalse();
    }

    [Fact]
    public void Equals_ShouldReturnFalse_WhenCurrencyDiffers()
    {
        var a = new Money(10m, "USD");
        var b = new Money(10m, "EUR");
        a.Equals(b).Should().BeFalse();
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentException_WhenCurrencyIsEmpty()
    {
        Action act = () => new Money(5m, "");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentException_WhenCurrencyExceedsMaxLength()
    {
        Action act = () => new Money(5m, "TOOLONGCURRENCYCODE");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Multiplication_CommutativeOverload_ShouldWork()
    {
        var money = new Money(5m, "USD");
        var result = 3 * money;
        result.Amount.Should().Be(15m);
        result.Currency.Should().Be("USD");
    }
}
