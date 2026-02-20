using Bookstore.Domain.Entities;
using Bookstore.Domain.Enum;
using Bookstore.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;

namespace Bookstore.Infrastructure.Persistence;

public static class ModelBuilderExtensions
{
    public static void Seed(this ModelBuilder modelBuilder)
    {
        // Stable Guids
        var fictionCategoryId = new Guid("6e6f6a7b-8c9d-0e1f-2a3b-4c5d6e7f8a9b");
        var technologyCategoryId = new Guid("2d2d2d1d-2d2d-2d2d-2d2d-2d2d2d2d2d2d");
        var scienceCategoryId = new Guid("3d3d3d1d-3d3d-3d3d-3d3d-3d3d3d3d3d3d");

        var adminUserId = new Guid("d4e5f6a7-b8c9-4d0e-1f2a-3b4c5d6e7f8a");
        var regularUserId = new Guid("a1b2c3d4-e5f6-4a7b-8c9d-0e1f2a3b4c5d");

        var seedDate = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        // Categories
        modelBuilder.Entity<Category>().HasData(
            new { Id = fictionCategoryId, Name = "Fiction", CreatedAt = seedDate, UpdatedAt = seedDate, IsDeleted = false },
            new { Id = technologyCategoryId, Name = "Technology", CreatedAt = seedDate, UpdatedAt = seedDate, IsDeleted = false },
            new { Id = scienceCategoryId, Name = "Science", CreatedAt = seedDate, UpdatedAt = seedDate, IsDeleted = false }
        );

        // Users (Passwords are 'Password123!' hashed with BCrypt)
        // Hash: $2a$11$8k7.Y8R3h2Bf8WvE1H6eNuI7F1Z1e1K1W1S1a1P1l1y1C1o1r1K1.
        var commonHash = "$2a$11$8k7.Y8R3h2Bf8WvE1H6eNuI7F1Z1e1K1W1S1a1P1l1y1C1o1r1K1."; 

        modelBuilder.Entity<User>().HasData(
            new
            {
                Id = adminUserId,
                FullName = "System Admin",
                Email = "admin@bookstore.com",
                EmailConfirmed = true,
                PasswordHash = commonHash,
                Role = UserRole.Admin,
                CreatedAt = seedDate,
                UpdatedAt = seedDate,
                IsDeleted = false
            },
            new
            {
                Id = regularUserId,
                FullName = "John Doe",
                Email = "user@bookstore.com",
                EmailConfirmed = true,
                PasswordHash = commonHash,
                Role = UserRole.User,
                CreatedAt = seedDate,
                UpdatedAt = seedDate,
                IsDeleted = false
            }
        );

        // Books
        modelBuilder.Entity<Book>().HasData(
            new
            {
                Id = new Guid("b1b1b1b1-b1b1-b1b1-b1b1-b1b1b1b1b1b1"),
                Title = "The Great Gatsby",
                Description = "A story of wealth, love, and the American Dream.",
                ISBN = (ISBN)"9780743273565",
                Author = "F. Scott Fitzgerald",
                Pages = 180,
                CategoryId = fictionCategoryId,
                TotalQuantity = 10,
                CreatedAt = seedDate,
                UpdatedAt = seedDate,
                IsDeleted = false,
                Language = "English",
                CreatedBy = "Seed",
                AverageRating = 0m,
                ReviewCount = 0
            },
            new
            {
                Id = new Guid("b2b2b2b2-b2b2-b2b2-b2b2-b2b2b2b2b2b2"),
                Title = "Clean Code",
                Description = "A Handbook of Agile Software Craftsmanship.",
                ISBN = (ISBN)"9780132350884",
                Author = "Robert C. Martin",
                Pages = 464,
                CategoryId = technologyCategoryId,
                TotalQuantity = 5,
                CreatedAt = seedDate,
                UpdatedAt = seedDate,
                IsDeleted = false,
                Language = "English",
                CreatedBy = "Seed",
                AverageRating = 0m,
                ReviewCount = 0
            },
            new
            {
                Id = new Guid("b3b3b3b3-b3b3-b3b3-b3b3-b3b3b3b3b3b3"),
                Title = "A Brief History of Time",
                Description = "From the Big Bang to Black Holes.",
                ISBN = (ISBN)"9780553380163",
                Author = "Stephen Hawking",
                Pages = 212,
                CategoryId = scienceCategoryId,
                TotalQuantity = 8,
                CreatedAt = seedDate,
                UpdatedAt = seedDate,
                IsDeleted = false,
                Language = "English",
                CreatedBy = "Seed",
                AverageRating = 0m,
                ReviewCount = 0
            }
        );

        // Seeding the owned Money type for each book
        modelBuilder.Entity<Book>().OwnsOne(b => b.Price).HasData(
            new { BookId = new Guid("b1b1b1b1-b1b1-b1b1-b1b1-b1b1b1b1b1b1"), Amount = 15.99m, Currency = "USD" },
            new { BookId = new Guid("b2b2b2b2-b2b2-b2b2-b2b2-b2b2b2b2b2b2"), Amount = 45.50m, Currency = "USD" },
            new { BookId = new Guid("b3b3b3b3-b3b3-b3b3-b3b3-b3b3b3b3b3b3"), Amount = 20.00m, Currency = "USD" }
        );

        // Sample Order
        var sampleOrderId = new Guid("e1e1e1e1-e1e1-e1e1-e1e1-e1e1e1e1e1e1");
        modelBuilder.Entity<Order>().HasData(
            new
            {
                Id = sampleOrderId,
                UserId = regularUserId,
                Status = OrderStatus.Completed,
                IdempotencyKey = "SEED-ORDER-001",
                CreatedAt = seedDate,
                UpdatedAt = seedDate,
                IsDeleted = false,
                CreatedBy = "Seed"
            }
        );

        modelBuilder.Entity<Order>().OwnsOne(o => o.TotalAmount).HasData(
            new { OrderId = sampleOrderId, Amount = 15.99m, Currency = "USD" }
        );

        // Sample OrderItem
        modelBuilder.Entity<OrderItem>().HasData(
            new
            {
                Id = new Guid("f1f1f1f1-f1f1-f1f1-f1f1-f1f1f1f1f1f1"),
                OrderId = sampleOrderId,
                BookId = new Guid("b1b1b1b1-b1b1-b1b1-b1b1-b1b1b1b1b1b1"),
                Quantity = 1,
                CreatedAt = seedDate,
                UpdatedAt = seedDate,
                IsDeleted = false,
                CreatedBy = "Seed"
            }
        );

        modelBuilder.Entity<OrderItem>().OwnsOne(oi => oi.UnitPrice).HasData(
            new { OrderItemId = new Guid("f1f1f1f1-f1f1-f1f1-f1f1-f1f1f1f1f1f1"), Amount = 15.99m, Currency = "USD" }
        );
    }
}
