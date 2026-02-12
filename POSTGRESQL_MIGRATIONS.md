# PostgreSQL Database Migration Guide

## 🔄 Migrating from SQL Server to PostgreSQL

This guide covers creating and managing database migrations for PostgreSQL with the Bookstore API.

---

## ✅ Prerequisites

- PostgreSQL installed and running
- Connection string configured in `appsettings.json`
- Database created: `bookstoredb`
- .NET CLI installed

---

## 📋 Initial Setup (First Time)

### Step 1: Remove Old Migrations (if migrating from SQL Server)

```powershell
# In Package Manager Console
cd Bookstore.Infrastructure

# Remove all migrations one by one
Remove-Migration -Force
# Keep running until "No migrations have been applied..."
```

Or using CLI:
```bash
cd Bookstore.Infrastructure
dotnet ef migrations list  # See all migrations
```

### Step 2: Create Initial PostgreSQL Migration

**Using Package Manager Console:**
```powershell
cd Bookstore.Infrastructure
Add-Migration InitialCreate
Update-Database
```

**Using .NET CLI:**
```bash
cd Bookstore.Infrastructure
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### Step 3: Verify Database Created

```bash
# Connect to PostgreSQL
psql -U postgres -d bookstoredb

# List tables
\dt

# Expected output should show:
# - public | Users
# - public | Categories
# - public | Books
# - public | Orders
# - public | OrderItems

# Check a table structure
\d "Books"

# Disconnect
\q
```

---

## 📝 Making Schema Changes

### When You Modify an Entity

```csharp
// Example: Adding a field to Book entity
public class Book : BaseEntity
{
    public string Title { get; set; }
    public string NewField { get; set; }  // ← New field added
}
```

### Create and Apply Migration

**Using Package Manager Console:**
```powershell
Add-Migration AddNewFieldToBook
Update-Database
```

**Using .NET CLI:**
```bash
dotnet ef migrations add AddNewFieldToBook
dotnet ef database update
```

### Verify Changes

```bash
psql -U postgres -d bookstoredb
\d "Books"  # Check new column exists
\q
```

---

## 🔍 Migration Management Commands

### View All Migrations

**Package Manager Console:**
```powershell
Get-Migration
```

**CLI:**
```bash
dotnet ef migrations list
```

### View SQL for Migration

**Package Manager Console:**
```powershell
# Show SQL that will be executed
Script-Migration -From <FromMigration> -To <ToMigration>
```

**CLI:**
```bash
dotnet ef migrations script --from <FromMigration> --to <ToMigration>
dotnet ef migrations script > migration.sql
```

### Rollback to Previous Migration

**Package Manager Console:**
```powershell
Update-Database -Migration <PreviousMigration>
```

**CLI:**
```bash
dotnet ef database update <PreviousMigration>
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

---

## 🗄️ PostgreSQL-Specific Migration Notes

### 1. Identity (Auto-increment)

SQL Server uses `IDENTITY`, PostgreSQL uses `SERIAL` or `BIGSERIAL`:

```sql
-- PostgreSQL automatically handles this:
CREATE TABLE Books (
    Id BIGSERIAL PRIMARY KEY,  -- Auto-incrementing integer
    ...
);

-- Or with GUID (UUID):
CREATE TABLE Books (
    Id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    ...
);
```

EF Core handles this automatically!

### 2. Column Names

PostgreSQL converts identifiers to lowercase by default:

```sql
-- EF Core table
public class Book : BaseEntity {
    public string Title { get; set; }
}

-- Becomes in PostgreSQL:
CREATE TABLE "Books" (
    "Id" uuid PRIMARY KEY,
    "Title" varchar(200) NOT NULL
);
```

To maintain exact case, use quotes in queries (EF Core does this automatically).

### 3. Text vs VARCHAR

- `VARCHAR(n)` - Variable length string with max n
- `TEXT` - Variable length unlimited

```csharp
[MaxLength(200)]
public string Title { get; set; }  // → VARCHAR(200)

public string Description { get; set; }  // → TEXT (no limit)
```

### 4. Boolean Types

PostgreSQL uses `BOOLEAN`:

```csharp
public bool IsDeleted { get; set; }  // → BOOLEAN in PostgreSQL
```

### 5. DateTime/Timestamps

```csharp
public DateTimeOffset CreatedAt { get; set; }  // → TIMESTAMP WITH TIME ZONE

public DateTime UpdatedAt { get; set; }  // → TIMESTAMP
```

---

## 🔐 Database User Setup

### Create Limited User for Application

```bash
# Connect as postgres superuser
psql -U postgres

# Create user for application
CREATE USER appuser WITH PASSWORD 'secure_password';

# Grant permissions
GRANT CONNECT ON DATABASE bookstoredb TO appuser;
GRANT USAGE ON SCHEMA public TO appuser;
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA public TO appuser;
GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA public TO appuser;

# Disconnect
\q
```

### Update Connection String

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=bookstoredb;Username=appuser;Password=secure_password;SSL Mode=Disable"
  }
}
```

---

## 🌱 Seeding Data

Create a `SeedData.cs` file:

```csharp
public static class SeedData
{
    public static async Task SeedAsync(BookStoreDbContext context)
    {
        if (await context.Categories.AnyAsync())
            return;  // Already seeded

        var categories = new[]
        {
            new Category("Fiction"),
            new Category("Science Fiction"),
            new Category("Mystery")
        };

        await context.Categories.AddRangeAsync(categories);
        await context.SaveChangesAsync();

        Console.WriteLine("✅ Database seeded successfully");
    }
}
```

In `Program.cs`:

```csharp
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<BookStoreDbContext>();
    await dbContext.Database.MigrateAsync();
    await SeedData.SeedAsync(dbContext);
}
```

---

## 📊 Performance Optimization

### Create Indexes

Indexes are created automatically from EF Core configurations, but you can verify:

```bash
psql -U postgres -d bookstoredb

# List indexes
\di

# Check index usage
SELECT schemaname, tablename, indexname FROM pg_indexes 
WHERE schemaname = 'public' 
ORDER BY tablename;

# Analyze query performance
EXPLAIN ANALYZE SELECT * FROM "Books" WHERE "ISBN" = '123';

\q
```

### Maintenance Commands

```bash
psql -U postgres -d bookstoredb

-- Update statistics (improves query planning)
ANALYZE;

-- Reclaim storage (similar to SQL Server SHRINK)
VACUUM;

-- Aggressive cleanup
VACUUM FULL ANALYZE;

\q
```

---

## 🔄 Backup & Restore

### Backup Database

```bash
# Full backup
pg_dump -U postgres -h localhost bookstoredb > bookstore_backup.sql

# Binary backup (faster)
pg_dump -U postgres -h localhost -F c -f bookstore_backup.dump bookstoredb
```

### Restore Database

```bash
# From SQL file
psql -U postgres -h localhost bookstoredb < bookstore_backup.sql

# From binary file
pg_restore -U postgres -h localhost -d bookstoredb bookstore_backup.dump
```

### Automated Backup Script (Linux)

Create `backup.sh`:
```bash
#!/bin/bash
BACKUP_DIR="/backups/postgresql"
DATE=$(date +%Y%m%d_%H%M%S)

pg_dump -U postgres bookstoredb > $BACKUP_DIR/bookstore_$DATE.sql

# Keep only last 30 backups
find $BACKUP_DIR -name "bookstore_*.sql" -mtime +30 -delete
```

Add to crontab for daily backups:
```bash
0 2 * * * /path/to/backup.sh
```

---

## 🐛 Troubleshooting

### Issue: "Command Timeout"

**Solution:**
```csharp
// In DependencyInjection.cs
options.UseNpgsql(connectionString, sqlOptions =>
{
    sqlOptions.CommandTimeout(60);  // Increase from 30
});
```

### Issue: "Role 'postgres' does not exist"

**Solution:**
```bash
# Reinstall PostgreSQL or create user:
sudo -u postgres psql -c "CREATE USER postgres WITH SUPERUSER;"
```

### Issue: "Database does not exist"

**Solution:**
```bash
# Create database manually
psql -U postgres -c "CREATE DATABASE bookstoredb;"

# Or let EF Core create it
dotnet ef database update
```

### Issue: "SSL certificate error"

**Solution:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=bookstoredb;Username=postgres;Password=pwd;SSL Mode=Disable"
  }
}
```

### Issue: "Cannot connect from Docker"

**Solution:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=host.docker.internal;Port=5432;Database=bookstoredb;Username=postgres;Password=pwd;SSL Mode=Disable"
  }
}
```

---

## 📚 Useful PostgreSQL Queries

```bash
psql -U postgres -d bookstoredb

-- Check database size
SELECT pg_size_pretty(pg_database_size('bookstoredb'));

-- Check table sizes
SELECT 
    schemaname,
    tablename,
    pg_size_pretty(pg_total_relation_size(schemaname||'.'||tablename)) AS size
FROM pg_tables
ORDER BY pg_total_relation_size(schemaname||'.'||tablename) DESC;

-- List all users
\du

-- Check active connections
SELECT datname, usename, count(*) FROM pg_stat_activity GROUP BY datname, usename;

-- Kill idle connections
SELECT pg_terminate_backend(pid) FROM pg_stat_activity 
WHERE state = 'idle' AND query_start < now() - interval '5 minutes';

\q
```

---

## ✅ Checklist: First Time Setup

- [ ] PostgreSQL installed
- [ ] Connection string updated
- [ ] Database created
- [ ] Migrations applied: `dotnet ef database update`
- [ ] Tables verified: `\dt` in psql
- [ ] Application starts
- [ ] Swagger API works
- [ ] Can register user
- [ ] Can create category
- [ ] Can create book
- [ ] Data persists in PostgreSQL

---

## 🔗 Reference Links

- [PostgreSQL Docs](https://www.postgresql.org/docs/)
- [Npgsql Docs](https://www.npgsql.org/)
- [EF Core PostgreSQL](https://www.npgsql.org/efcore/)
- [PostgreSQL Types](https://www.postgresql.org/docs/current/datatype.html)

---

**PostgreSQL Migration Guide Complete! 🎉**

Last Updated: January 2025
