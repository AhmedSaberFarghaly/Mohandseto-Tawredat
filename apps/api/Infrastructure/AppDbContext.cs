using Microsoft.EntityFrameworkCore;
using Mohandseto.Api.Domain.Common;
using Mohandseto.Api.Domain.Entities;

namespace Mohandseto.Api.Infrastructure;

public interface ITenantProvider
{
    Guid? TenantId { get; }
}

/// <summary>Resolves tenant from the authenticated user's claims; null for platform staff / anonymous.</summary>
public class HttpTenantProvider(IHttpContextAccessor accessor) : ITenantProvider
{
    public Guid? TenantId
    {
        get
        {
            var claim = accessor.HttpContext?.User.FindFirst("tenant_id")?.Value;
            return Guid.TryParse(claim, out var id) ? id : null;
        }
    }
}

public class AppDbContext(DbContextOptions<AppDbContext> options, ITenantProvider tenantProvider) : DbContext(options)
{
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<CompanyBranch> CompanyBranches => Set<CompanyBranch>();
    public DbSet<CompanyDocument> CompanyDocuments => Set<CompanyDocument>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<OtpCode> OtpCodes => Set<OtpCode>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<UnitOfMeasure> Units => Set<UnitOfMeasure>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<QuantityPriceTier> QuantityPriceTiers => Set<QuantityPriceTier>();
    public DbSet<ProductAttributeValue> ProductAttributeValues => Set<ProductAttributeValue>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<ProductDocument> ProductDocuments => Set<ProductDocument>();
    public DbSet<CompanyProductPrice> CompanyProductPrices => Set<CompanyProductPrice>();
    public DbSet<Favorite> Favorites => Set<Favorite>();
    public DbSet<RecentlyViewed> RecentlyVieweds => Set<RecentlyViewed>();
    public DbSet<CompareItem> CompareItems => Set<CompareItem>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        b.Entity<RolePermission>().HasKey(x => new { x.RoleId, x.PermissionId });
        b.Entity<UserRole>().HasKey(x => new { x.UserId, x.RoleId });

        b.Entity<Tenant>().HasOne(t => t.Company).WithOne(c => c.Tenant)
            .HasForeignKey<Company>(c => c.TenantId);

        b.Entity<User>().HasIndex(u => u.Phone).IsUnique();
        b.Entity<User>().HasIndex(u => u.Email).IsUnique();
        b.Entity<Product>().HasIndex(p => p.Sku).IsUnique();
        b.Entity<Product>().HasIndex(p => p.Slug).IsUnique();
        b.Entity<Category>().HasIndex(c => c.Slug).IsUnique();
        b.Entity<Permission>().HasIndex(p => p.Code).IsUnique();
        b.Entity<Favorite>().HasIndex(f => new { f.UserId, f.ProductId }).IsUnique();
        b.Entity<CompanyProductPrice>().HasIndex(p => new { p.TenantId, p.ProductId, p.ValidFrom });
        b.Entity<QuantityPriceTier>().HasIndex(t => new { t.ProductId, t.MinQty }).IsUnique();
        b.Entity<ProductVariant>().HasIndex(v => v.Sku).IsUnique();
        b.Entity<CompareItem>().HasIndex(c => new { c.UserId, c.ProductId }).IsUnique();

        foreach (var entity in b.Model.GetEntityTypes())
        {
            // decimal precision for money columns
            foreach (var prop in entity.GetProperties().Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
                prop.SetPrecision(18); // scale
            // soft-delete filter for all BaseEntity types
        }

        // Global query filters: soft delete + tenant isolation
        b.Entity<Company>().HasQueryFilter(e => !e.IsDeleted);
        b.Entity<Tenant>().HasQueryFilter(e => !e.IsDeleted);
        b.Entity<User>().HasQueryFilter(e => !e.IsDeleted);
        b.Entity<Product>().HasQueryFilter(e => !e.IsDeleted);
        b.Entity<Category>().HasQueryFilter(e => !e.IsDeleted);
        b.Entity<Brand>().HasQueryFilter(e => !e.IsDeleted);
        b.Entity<ProductImage>().HasQueryFilter(e => !e.IsDeleted && !e.Product.IsDeleted);
        b.Entity<QuantityPriceTier>().HasQueryFilter(e => !e.IsDeleted && !e.Product.IsDeleted);
        b.Entity<ProductAttributeValue>().HasQueryFilter(e => !e.IsDeleted && !e.Product.IsDeleted);
        b.Entity<ProductVariant>().HasQueryFilter(e => !e.IsDeleted && !e.Product.IsDeleted);
        b.Entity<ProductDocument>().HasQueryFilter(e => !e.IsDeleted && !e.Product.IsDeleted);

        // dependents of User share its soft-delete filter to avoid filter-mismatch anomalies
        b.Entity<RefreshToken>().HasQueryFilter(e => !e.IsDeleted && !e.User.IsDeleted);
        b.Entity<UserRole>().HasQueryFilter(e => !e.User.IsDeleted);

        b.Entity<CompanyBranch>().HasQueryFilter(e => !e.IsDeleted && (tenantProvider.TenantId == null || e.TenantId == tenantProvider.TenantId));
        b.Entity<CompanyDocument>().HasQueryFilter(e => !e.IsDeleted && (tenantProvider.TenantId == null || e.TenantId == tenantProvider.TenantId));
        b.Entity<CompanyProductPrice>().HasQueryFilter(e => !e.IsDeleted && (tenantProvider.TenantId == null || e.TenantId == tenantProvider.TenantId));
        b.Entity<Favorite>().HasQueryFilter(e => !e.IsDeleted && (tenantProvider.TenantId == null || e.TenantId == tenantProvider.TenantId));
        b.Entity<RecentlyViewed>().HasQueryFilter(e => !e.IsDeleted && (tenantProvider.TenantId == null || e.TenantId == tenantProvider.TenantId));
        b.Entity<CompareItem>().HasQueryFilter(e => !e.IsDeleted && (tenantProvider.TenantId == null || e.TenantId == tenantProvider.TenantId));
    }

    public override Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    break;
                case EntityState.Deleted:
                    // soft delete by default
                    entry.State = EntityState.Modified;
                    entry.Entity.IsDeleted = true;
                    entry.Entity.DeletedAt = now;
                    break;
            }
        }
        return base.SaveChangesAsync(ct);
    }
}
