using Bookstore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Tests.Fixtures;

/// <summary>
/// Provides in-memory database context for testing
/// </summary>
public class TestDatabaseFixture : IDisposable
{
    private readonly DbContextOptions<BookStoreDbContext> _options;
    public BookStoreDbContext Context { get; }

    public TestDatabaseFixture()
    {
        _options = new DbContextOptionsBuilder<BookStoreDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        Context = new BookStoreDbContext(_options);
    }

    public void Dispose()
    {
        Context?.Dispose();
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Collection fixture for sharing database between tests
/// </summary>
[CollectionDefinition("Database collection")]
public class DatabaseCollection : ICollectionFixture<TestDatabaseFixture>
{
    // This class has no code, and is never created. Its purpose is to define the collection.
}
