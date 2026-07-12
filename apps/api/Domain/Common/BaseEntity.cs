namespace Mohandseto.Api.Domain.Common;

/// <summary>
/// Base for all business entities: Guid PK, audit fields, soft delete, optimistic concurrency.
/// All timestamps are stored in UTC.
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public byte[]? RowVersion { get; set; }
}

/// <summary>
/// Base for entities owned by a tenant (company-scoped data).
/// A global query filter on TenantId prevents cross-tenant data leakage.
/// </summary>
public abstract class TenantEntity : BaseEntity
{
    public Guid TenantId { get; set; }
}
