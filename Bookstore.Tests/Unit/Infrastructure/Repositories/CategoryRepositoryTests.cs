using Bookstore.Domain.Entities;
using Bookstore.Infrastructure.Persistence.Repositories;
using Bookstore.Tests.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Bookstore.Tests.Unit.Infrastructure.Repositories;

public class CategoryRepositoryTests : IClassFixture<TestDatabaseFixture>
{
    private readonly TestDatabaseFixture _fixture;

    public CategoryRepositoryTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnCategories()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var repo = new CategoryRepository(context);
        
        var category1 = new Category("Sci-Fi");
        var category2 = new Category("History");
        context.Categories.AddRange(category1, category2);
        await context.SaveChangesAsync();

        // Act
        var result = await repo.GetAllAsync();

        // Assert
        result.Should().HaveCountGreaterThanOrEqualTo(2);
        result.Any(c => c.Name == "Sci-Fi").Should().BeTrue();
        result.Any(c => c.Name == "History").Should().BeTrue();
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnCategory_WhenExists()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var repo = new CategoryRepository(context);
        
        var category = new Category("Fantasy");
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        // Act
        var result = await repo.GetByIdAsync(category.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Fantasy");
    }

    [Fact]
    public async Task AddAsync_ShouldAddCategory()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var repo = new CategoryRepository(context);
        var category = new Category("Cookbooks");

        // Act
        await repo.AddAsync(category);
        await context.SaveChangesAsync();

        // Assert
        context.Categories.Find(category.Id).Should().NotBeNull();
    }

    [Fact]
    public async Task Delete_ShouldRemoveCategory()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var repo = new CategoryRepository(context);
        var category = new Category("Drama");
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        // Act
        repo.Delete(category);
        await context.SaveChangesAsync();

        // Assert
        category.IsDeleted.Should().BeTrue();

        var deleted = await context.Categories.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == category.Id);
        deleted.Should().NotBeNull();
        deleted!.IsDeleted.Should().BeTrue();
    }
}
