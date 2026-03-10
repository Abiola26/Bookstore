using Bookstore.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Bookstore.Tests.Unit.Domain.ValueObjects;

public class ISBNTests
{
    [Fact]
    public void Constructor_ShouldInitializeValue_WhenValid()
    {
        var value = "978-3-16-148410-0";
        var isbn = new ISBN(value);
        isbn.Value.Should().Be(value);
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentException_WhenInvalidCharactersSpecified()
    {
        Action act = () => new ISBN("978-3-INVALID-?");
        act.Should().Throw<ArgumentException>().WithMessage("*invalid characters*");
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentOutOfRangeException_WhenLengthExceedsLimit()
    {
        Action act = () => new ISBN("123456789012345678901");
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Equals_ShouldReturnTrue_WhenValuesAreEqualIgnoreCase()
    {
        var isbn1 = new ISBN("978-0123-X");
        var isbn2 = new ISBN("978-0123-x");
        isbn1.Equals(isbn2).Should().BeTrue();
    }
}
