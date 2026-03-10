using Bookstore.Application.DTOs;
using Bookstore.Application.Repositories;
using Bookstore.Domain.Entities;
using Bookstore.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Bookstore.Tests.Unit.Services;

public class CategoryServiceTests
{
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<ILogger<CategoryService>> _loggerMock;
    private readonly CategoryService _service;

    public CategoryServiceTests()
    {
        _uowMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<CategoryService>>();
        _service = new CategoryService(_uowMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetCategoryByIdAsync_ShouldReturnCategory_WhenCategoryExists()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var category = new Category("Test Category") { Id = categoryId };
        
        _uowMock.Setup(x => x.Categories.GetByIdAsync(categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);
        _uowMock.Setup(x => x.Books.GetCategoryBookCountAsync(categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        // Act
        var result = await _service.GetCategoryByIdAsync(categoryId);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Id.Should().Be(categoryId);
        result.Data.Name.Should().Be("Test Category");
        result.Data.BookCount.Should().Be(5);
    }

    [Fact]
    public async Task CreateCategoryAsync_ShouldCreateCategory_WhenNameIsUnique()
    {
        // Arrange
        var dto = new CategoryCreateDto { Name = "New Category" };
        _uowMock.Setup(x => x.Categories.NameExistsAsync(dto.Name, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _service.CreateCategoryAsync(dto);

        // Assert
        result.Success.Should().BeTrue();
        result.StatusCode.Should().Be(201);
        _uowMock.Verify(x => x.Categories.AddAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteCategoryAsync_ShouldReturnError_WhenCategoryHasBooks()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var category = new Category("Test") { Id = categoryId };
        
        _uowMock.Setup(x => x.Categories.GetByIdAsync(categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);
        _uowMock.Setup(x => x.Books.GetCategoryBookCountAsync(categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1); // Has 1 book

        // Act
        var result = await _service.DeleteCategoryAsync(categoryId);

        // Assert
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.Message.Should().Be("Cannot delete category with books");
    }
    [Fact]
    public async Task UpdateCategoryAsync_ShouldUpdateName_WhenNameIsUnique()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var category = new Category("Old Name") { Id = categoryId };
        var dto = new CategoryUpdateDto { Name = "New Name" };

        _uowMock.Setup(x => x.Categories.GetByIdAsync(categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);
        _uowMock.Setup(x => x.Categories.NameExistsAsync(dto.Name, categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _uowMock.Setup(x => x.Books.GetCategoryBookCountAsync(categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        // Act
        var result = await _service.UpdateCategoryAsync(categoryId, dto);

        // Assert
        result.Success.Should().BeTrue();
        category.Name.Should().Be("New Name");
        _uowMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllCategoriesAsync_ShouldReturnList()
    {
        // Arrange
        var data = new List<(Category, int)> { (new Category("C1"), 5) };
        _uowMock.Setup(x => x.Categories.GetAllWithBookCountsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((ICollection<(Category Category, int BookCount)>)data);

        // Act
        var result = await _service.GetAllCategoriesAsync();

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(1);
        result.Data!.First().BookCount.Should().Be(5);
    }
}
