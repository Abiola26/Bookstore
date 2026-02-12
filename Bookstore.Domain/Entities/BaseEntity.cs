using System;

namespace Bookstore.Domain.Entities;

public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Prepare for soft-delete support in the future.
    public bool IsDeleted { get; set; }

    // Audit fields - optional
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }

    // Concurrency token for optimistic concurrency control
    public byte[]? RowVersion { get; set; }

    protected BaseEntity() { }

    public void Touch() => UpdatedAt = DateTimeOffset.UtcNow;
}
