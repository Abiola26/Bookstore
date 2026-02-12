using Bookstore.Domain.Entities;
using Bookstore.Domain.ValueObjects;

namespace Bookstore.Tests.Fixtures;

/// <summary>
/// Test data factory for creating test entities with common patterns
/// Follows the Fixture Object Pattern for enterprise-level testing
/// </summary>
public static class TestDataFactory
{
    public static Book CreateValidBook(
        string title = "Test Book",
        string author = "Test Author",
        decimal price = 29.99m)
    {
        return new Book(
            title,
            "Test Description",
            new ISBN("978-3-16-148410-0"),
            new Money(price, "USD"),
            author,
            10,
            Guid.NewGuid()
        );
    }

    public static Category CreateValidCategory(string name = "Test Category")
    {
        return new Category(name);
    }

    public static User CreateValidUser(
        string email = "test@example.com",
        string fullName = "Test User",
        string passwordHash = "hashed_password")
    {
        var user = new User(fullName, email, passwordHash, UserRole.User);
        return user;
    }

    public static IEnumerable<Book> CreateMultipleBooks(int count = 5)
    {
        var books = new List<Book>();
        for (int i = 0; i < count; i++)
        {
            books.Add(CreateValidBook($"Book {i + 1}", $"Author {i + 1}", 19.99m + i));
        }
        return books;
    }

    public static IEnumerable<Category> CreateMultipleCategories(int count = 3)
    {
        var categories = new List<Category>();
        var categoryNames = new[] { "Fiction", "Science", "History", "Technology", "Art" };
        
        for (int i = 0; i < Math.Min(count, categoryNames.Length); i++)
        {
            categories.Add(CreateValidCategory(categoryNames[i]));
        }
        return categories;
    }
}

/// <summary>
/// Test data scenarios for parameterized and edge-case testing
/// Demonstrates the Theory pattern with inline data
/// </summary>
public static class TestScenarios
{
    public static TheoryData<decimal, bool> GetInvalidPrices()
    {
        return new TheoryData<decimal, bool>
        {
            { -10.00m, false },      // Negative price
            { 0m, false },           // Zero price
            { 99999999.99m, false }, // Unreasonably high
            { 0.01m, true },         // Valid minimum
            { 1000.00m, true }       // Valid maximum typical range
        };
    }

    public static TheoryData<int, bool> GetInvalidQuantities()
    {
        return new TheoryData<int, bool>
        {
            { -5, false },      // Negative quantity
            { 0, false },       // Zero quantity
            { 1, true },        // Valid minimum
            { 1000000, true }   // Valid maximum
        };
    }

    public static TheoryData<string, bool> GetInvalidEmails()
    {
        return new TheoryData<string, bool>
        {
            { "", false },                          // Empty
            { "invalid", false },                   // No @ symbol
            { "user@", false },                     // Missing domain
            { "user@domain", false },               // Missing TLD
            { "user@domain.com", true },            // Valid
            { "user.name@domain.co.uk", true },     // Valid with subdomain
            { "user+tag@domain.com", true }         // Valid with plus
        };
    }
}
