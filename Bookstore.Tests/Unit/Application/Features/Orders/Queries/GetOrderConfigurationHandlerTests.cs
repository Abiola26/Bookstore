using Bookstore.Application.Features.Orders.Queries;
using FluentAssertions;
using Xunit;

namespace Bookstore.Tests.Unit.Application.Features.Orders.Queries;

public class GetOrderConfigurationHandlerTests
{
    private readonly GetOrderConfigurationHandler _handler;

    public GetOrderConfigurationHandlerTests()
    {
        _handler = new GetOrderConfigurationHandler();
    }

    [Fact]
    public async Task Handle_ShouldReturnCorrectConfiguration()
    {
        // Arrange
        var query = new GetOrderConfigurationQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.ShippingFee.Should().Be(5.00m);
        result.Data!.Currency.Should().Be("USD");
    }
}
