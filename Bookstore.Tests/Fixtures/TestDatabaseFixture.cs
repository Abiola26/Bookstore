using Bookstore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;

namespace Bookstore.Tests.Fixtures;

public class TestDatabaseFixture : IDisposable
{
    private readonly SqliteConnection _connection;
    public DbContextOptions<BookStoreDbContext> Options { get; }

    public TestDatabaseFixture()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        Options = new DbContextOptionsBuilder<BookStoreDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var context = new BookStoreDbContext(Options);
        context.Database.EnsureCreated();
    }

    public BookStoreDbContext CreateContext()
    {
        return new BookStoreDbContext(Options);
    }

    public void Dispose()
    {
        _connection.Close();
    }
}
