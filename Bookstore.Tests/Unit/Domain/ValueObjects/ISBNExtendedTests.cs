using Bookstore.Domain.ValueObjects;
using FluentAssertions;

namespace Bookstore.Tests.Unit.Domain.ValueObjects;

public class ISBNExtendedTests
{
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Constructor_ShouldThrowArgumentException_WhenValueIsNullOrWhitespace(string? value)
    {
        Action act = () => new ISBN(value!);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ToString_ShouldReturnValue()
    {
        var isbn = new ISBN("978-0123");
        isbn.ToString().Should().Be("978-0123");
    }

    [Fact]
    public void EqualityOperator_ShouldReturnTrue_WhenValuesMatch()
    {
        var a = new ISBN("978-0123");
        var b = new ISBN("978-0123");
        (a == b).Should().BeTrue();
    }

    [Fact]
    public void EqualityOperator_ShouldReturnFalse_WhenValuesDiffer()
    {
        var a = new ISBN("978-0123");
        var b = new ISBN("978-0456");
        (a != b).Should().BeTrue();
    }

    [Fact]
    public void ImplicitStringConversion_ShouldReturnValue()
    {
        var isbn = new ISBN("978-0123");
        string? value = isbn;
        value.Should().Be("978-0123");
    }

    [Fact]
    public void ExplicitConversionFromString_ShouldCreateISBN()
    {
        var isbn = (ISBN)"978-0123";
        isbn.Value.Should().Be("978-0123");
    }
}
