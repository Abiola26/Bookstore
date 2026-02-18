# Database Migration Commands

This guide provides step-by-step instructions for creating and managing the database migrations for the Bookstore API.

## Prerequisites

- SQL Server installed and running
- Package Manager Console in Visual Studio, or
- .NET CLI

## Initial Setup

### Option 1: Using Package Manager Console (Visual Studio)

1. Open Package Manager Console: `Tools → NuGet Package Manager → Package Manager Console`

2. Set the Default Project to `Bookstore.Infrastructure`

3. Run the following commands:

```powershell
# Create initial migration
Add-Migration InitialCreate

# Apply migration to database
Update-Database
```

### Option 2: Using .NET CLI

1. Navigate to the Bookstore.Infrastructure directory:

```bash
cd Bookstore.Infrastructure
```

2. Run the following commands:

```bash
# Create initial migration
dotnet ef migrations add InitialCreate

# Apply migration to database
dotnet ef database update
```

## Additional Migration Commands

### Create a New Migration

After changing entity models, create a new migration:

**Package Manager Console:**
```powershell
Add-Migration <MigrationName>
Update-Database
```

**CLI:**
```bash
dotnet ef migrations add <MigrationName>
dotnet ef database update
```

### View Pending Migrations

**Package Manager Console:**
```powershell
Get-Migration
```

**CLI:**
```bash
dotnet ef migrations list
```

### Rollback to Previous Migration

**Package Manager Console:**
```powershell
Update-Database -Migration <MigrationName>
```

**CLI:**
```bash
dotnet ef database update <MigrationName>
```

### Remove Latest Migration (Not Applied)

**Package Manager Console:**
```powershell
Remove-Migration
```

**CLI:**
```bash
dotnet ef migrations remove
```

### Script Migration for Production

**Package Manager Console:**
```powershell
# Generate SQL script for a specific migration
Script-Migration -From <FromMigration> -To <ToMigration>

# Generate all pending migrations as script
Script-Migration
```

**CLI:**
```bash
# Generate SQL script for migrations
dotnet ef migrations script --from <FromMigration> --to <ToMigration>
dotnet ef migrations script  # All pending
```

## Connection String Configuration

The connection string is configured in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(local)\\SQLEXPRESS;Database=BookstoreDb;Trusted_Connection=true;TrustServerCertificate=true;Connection Timeout=30;"
  }
}
```

### For Different Environments

**appsettings.Development.json:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(local)\\SQLEXPRESS;Database=BookstoreDb_Dev;Trusted_Connection=true;"
  }
}
```

**appsettings.Production.json:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=BookstoreDb_Prod;User Id=sa;Password=YOUR_PASSWORD;"
  }
}
```

## Database Initialization in Application

The application automatically creates the database if it doesn't exist when running:

```csharp
// In Program.cs
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<BookStoreDbContext>();
    dbContext.Database.EnsureCreated();  // Creates database if it doesn't exist
}
```

To use migrations instead of EnsureCreated:

```csharp
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<BookStoreDbContext>();
    dbContext.Database.Migrate();  // Applies pending migrations
}
```

## Seeding Sample Data

Create a seeding service to populate sample data:

```csharp
public class DatabaseSeeder
{
    public static async Task SeedAsync(BookStoreDbContext context)
    {
        if (context.Categories.Any())
            return;  // Database already seeded

        var categories = new[]
        {
            new Category("Fiction"),
            new Category("Science Fiction"),
            new Category("Mystery"),
            new Category("Biography")
        };

        await context.Categories.AddRangeAsync(categories);
        await context.SaveChangesAsync();
    }
}
```

Then in Program.cs:

```csharp
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<BookStoreDbContext>();
    await dbContext.Database.MigrateAsync();
    await DatabaseSeeder.SeedAsync(dbContext);
}
```

## Troubleshooting

### Migration Not Found
- Ensure you're running commands from the correct project directory
- Check that the migration name is spelled correctly

### "No database provider was configured"
- Ensure `UseSqlServer()` is called in `DbContext` configuration
- Verify connection string exists in `appsettings.json`

### "Cannot drop database because it's currently in use"
- Close any open connections to the database
- Restart Visual Studio or the database service

### Migration has changes that cannot be automatically applied
- This is a safety feature
- Manually create a migration and update the .cs file with proper logic

## Best Practices

1. **Always create a migration when changing models**
   - Don't manually edit the database

2. **Use meaningful migration names**
   - Good: `Add_UserRoleColumn`
   - Bad: `Migration123`

3. **Test migrations before production**
   - Run on development database first
   - Verify data integrity

4. **Keep migrations small and focused**
   - One feature per migration
   - Easier to debug and rollback

5. **Version control migrations**
   - Commit migrations to source control
   - Never delete migration files

## References

- [Entity Framework Core Migrations](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/)
- [EF Core Tools Reference](https://learn.microsoft.com/en-us/ef/core/cli/)
