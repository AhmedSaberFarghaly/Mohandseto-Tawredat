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
    public DbSet<RecentSearch> RecentSearches => Set<RecentSearch>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<CheckoutSession> CheckoutSessions => Set<CheckoutSession>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OrderStatusHistory> OrderStatusHistories => Set<OrderStatusHistory>();
    public DbSet<CustomProductTemplate> CustomProductTemplates => Set<CustomProductTemplate>();
    public DbSet<CustomizationOption> CustomizationOptions => Set<CustomizationOption>();
    public DbSet<PrintMethod> PrintMethods => Set<PrintMethod>();
    public DbSet<CustomMaterial> Materials => Set<CustomMaterial>();
    public DbSet<CustomColor> Colors => Set<CustomColor>();
    public DbSet<CustomSize> Sizes => Set<CustomSize>();
    public DbSet<CustomProductRequest> CustomProductRequests => Set<CustomProductRequest>();
    public DbSet<CustomRequestItem> CustomRequestItems => Set<CustomRequestItem>();
    public DbSet<LogoAsset> LogoAssets => Set<LogoAsset>();
    public DbSet<DesignBrief> DesignBriefs => Set<DesignBrief>();
    public DbSet<DesignVersion> DesignVersions => Set<DesignVersion>();
    public DbSet<DesignMockup> DesignMockups => Set<DesignMockup>();
    public DbSet<DesignComment> DesignComments => Set<DesignComment>();
    public DbSet<DesignApproval> DesignApprovals => Set<DesignApproval>();
    public DbSet<ProductionJob> ProductionJobs => Set<ProductionJob>();
    public DbSet<ProductionStage> ProductionStages => Set<ProductionStage>();
    public DbSet<ProductionSample> ProductionSamples => Set<ProductionSample>();
    public DbSet<QualityCheck> QualityChecks => Set<QualityCheck>();
    public DbSet<CostCenter> CostCenters => Set<CostCenter>();
    public DbSet<CompanyProject> CompanyProjects => Set<CompanyProject>();
    public DbSet<CheckoutAttachment> CheckoutAttachments => Set<CheckoutAttachment>();
    public DbSet<PaymentAttempt> PaymentAttempts => Set<PaymentAttempt>();
    public DbSet<Coupon> Coupons => Set<Coupon>();

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
        b.Entity<RecentSearch>().HasIndex(s => new { s.UserId, s.Query }).IsUnique();
        b.Entity<Cart>().HasIndex(c => new { c.TenantId, c.UserId, c.Status });
        b.Entity<CartItem>().HasIndex(i => new { i.CartId, i.ProductId, i.VariantId });
        b.Entity<CheckoutSession>().HasIndex(s => new { s.TenantId, s.UserId, s.CartId, s.Status });
        b.Entity<Order>().HasIndex(o => o.Number).IsUnique();
        b.Entity<Order>().HasIndex(o => new { o.TenantId, o.UserId, o.CreatedAt });
        b.Entity<CustomProductTemplate>().HasIndex(t => t.ProductId).IsUnique();
        b.Entity<CustomizationOption>().HasIndex(o => new { o.TemplateId, o.Code }).IsUnique();
        b.Entity<PrintMethod>().HasIndex(o => new { o.TemplateId, o.Code }).IsUnique();
        b.Entity<CustomMaterial>().HasIndex(o => new { o.TemplateId, o.Code }).IsUnique();
        b.Entity<CustomColor>().HasIndex(o => new { o.TemplateId, o.Code }).IsUnique();
        b.Entity<CustomSize>().HasIndex(o => new { o.TemplateId, o.Code }).IsUnique();
        b.Entity<CustomProductRequest>().HasIndex(r => r.Number).IsUnique();
        b.Entity<CustomProductRequest>().HasIndex(r => new { r.TenantId, r.UserId, r.CreatedAt });
        b.Entity<DesignBrief>().HasIndex(x => x.RequestId).IsUnique();
        b.Entity<DesignVersion>().HasIndex(x => new { x.RequestId, x.VersionNumber }).IsUnique();
        b.Entity<ProductionJob>().HasIndex(x => x.RequestId).IsUnique();
        b.Entity<ProductionJob>().HasIndex(x => x.Number).IsUnique();
        b.Entity<ProductionSample>().HasIndex(x => new { x.ProductionJobId, x.VersionNumber }).IsUnique();
        b.Entity<CostCenter>().HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
        b.Entity<CompanyProject>().HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
        b.Entity<PaymentAttempt>().HasIndex(x => x.IdempotencyKey).IsUnique();
        b.Entity<PaymentAttempt>().HasIndex(x => x.ProviderReference).IsUnique();
        b.Entity<PaymentAttempt>().HasOne(x => x.CheckoutSession).WithMany()
            .HasForeignKey(x => x.CheckoutSessionId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<CheckoutAttachment>().HasIndex(x => new { x.CheckoutSessionId, x.Type });
        b.Entity<Coupon>().HasIndex(x => new { x.TenantId, x.Code }).IsUnique();

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
        b.Entity<CustomProductTemplate>().HasQueryFilter(e => !e.IsDeleted && !e.Product.IsDeleted);
        b.Entity<CustomizationOption>().HasQueryFilter(e => !e.IsDeleted && !e.Template.IsDeleted && !e.Template.Product.IsDeleted);
        b.Entity<PrintMethod>().HasQueryFilter(e => !e.IsDeleted && !e.Template.IsDeleted && !e.Template.Product.IsDeleted);
        b.Entity<CustomMaterial>().HasQueryFilter(e => !e.IsDeleted && !e.Template.IsDeleted && !e.Template.Product.IsDeleted);
        b.Entity<CustomColor>().HasQueryFilter(e => !e.IsDeleted && !e.Template.IsDeleted && !e.Template.Product.IsDeleted);
        b.Entity<CustomSize>().HasQueryFilter(e => !e.IsDeleted && !e.Template.IsDeleted && !e.Template.Product.IsDeleted);

        // dependents of User share its soft-delete filter to avoid filter-mismatch anomalies
        b.Entity<RefreshToken>().HasQueryFilter(e => !e.IsDeleted && !e.User.IsDeleted);
        b.Entity<UserRole>().HasQueryFilter(e => !e.User.IsDeleted);

        b.Entity<CompanyBranch>().HasQueryFilter(e => !e.IsDeleted && (tenantProvider.TenantId == null || e.TenantId == tenantProvider.TenantId));
        b.Entity<CompanyDocument>().HasQueryFilter(e => !e.IsDeleted && (tenantProvider.TenantId == null || e.TenantId == tenantProvider.TenantId));
        b.Entity<CompanyProductPrice>().HasQueryFilter(e => !e.IsDeleted && (tenantProvider.TenantId == null || e.TenantId == tenantProvider.TenantId));
        b.Entity<Favorite>().HasQueryFilter(e => !e.IsDeleted && (tenantProvider.TenantId == null || e.TenantId == tenantProvider.TenantId));
        b.Entity<RecentlyViewed>().HasQueryFilter(e => !e.IsDeleted && (tenantProvider.TenantId == null || e.TenantId == tenantProvider.TenantId));
        b.Entity<CompareItem>().HasQueryFilter(e => !e.IsDeleted && (tenantProvider.TenantId == null || e.TenantId == tenantProvider.TenantId));
        b.Entity<RecentSearch>().HasQueryFilter(e => !e.IsDeleted && (tenantProvider.TenantId == null || e.TenantId == tenantProvider.TenantId));
        b.Entity<Cart>().HasQueryFilter(e => !e.IsDeleted && (tenantProvider.TenantId == null || e.TenantId == tenantProvider.TenantId));
        b.Entity<CartItem>().HasQueryFilter(e => !e.IsDeleted && !e.Cart.IsDeleted && (tenantProvider.TenantId == null || e.TenantId == tenantProvider.TenantId));
        b.Entity<CheckoutSession>().HasQueryFilter(e => !e.IsDeleted && !e.Cart.IsDeleted && (tenantProvider.TenantId == null || e.TenantId == tenantProvider.TenantId));
        b.Entity<Order>().HasQueryFilter(e => !e.IsDeleted && (tenantProvider.TenantId == null || e.TenantId == tenantProvider.TenantId));
        b.Entity<OrderItem>().HasQueryFilter(e => !e.IsDeleted && !e.Order.IsDeleted && (tenantProvider.TenantId == null || e.TenantId == tenantProvider.TenantId));
        b.Entity<OrderStatusHistory>().HasQueryFilter(e => !e.IsDeleted && !e.Order.IsDeleted && (tenantProvider.TenantId == null || e.TenantId == tenantProvider.TenantId));
        b.Entity<CustomProductRequest>().HasQueryFilter(e => !e.IsDeleted && (tenantProvider.TenantId == null || e.TenantId == tenantProvider.TenantId));
        b.Entity<CustomRequestItem>().HasQueryFilter(e => !e.IsDeleted && !e.Request.IsDeleted && (tenantProvider.TenantId == null || e.TenantId == tenantProvider.TenantId));
        b.Entity<LogoAsset>().HasQueryFilter(e => !e.IsDeleted && !e.Request.IsDeleted && (tenantProvider.TenantId == null || e.TenantId == tenantProvider.TenantId));
        b.Entity<DesignBrief>().HasQueryFilter(e => !e.IsDeleted && !e.Request.IsDeleted && (tenantProvider.TenantId == null || e.TenantId == tenantProvider.TenantId));
        b.Entity<DesignVersion>().HasQueryFilter(e => !e.IsDeleted && !e.Request.IsDeleted && (tenantProvider.TenantId == null || e.TenantId == tenantProvider.TenantId));
        b.Entity<DesignMockup>().HasQueryFilter(e => !e.IsDeleted && !e.Version.IsDeleted && (tenantProvider.TenantId == null || e.TenantId == tenantProvider.TenantId));
        b.Entity<DesignComment>().HasQueryFilter(e => !e.IsDeleted && !e.Request.IsDeleted && (tenantProvider.TenantId == null || e.TenantId == tenantProvider.TenantId));
        b.Entity<DesignApproval>().HasQueryFilter(e => !e.IsDeleted && !e.Request.IsDeleted && !e.Version.IsDeleted && (tenantProvider.TenantId == null || e.TenantId == tenantProvider.TenantId));
        b.Entity<ProductionJob>().HasQueryFilter(e => !e.IsDeleted && !e.Request.IsDeleted && (tenantProvider.TenantId == null || e.TenantId == tenantProvider.TenantId));
        b.Entity<ProductionStage>().HasQueryFilter(e => !e.IsDeleted && !e.ProductionJob.IsDeleted && (tenantProvider.TenantId == null || e.TenantId == tenantProvider.TenantId));
        b.Entity<ProductionSample>().HasQueryFilter(e => !e.IsDeleted && !e.ProductionJob.IsDeleted && (tenantProvider.TenantId == null || e.TenantId == tenantProvider.TenantId));
        b.Entity<QualityCheck>().HasQueryFilter(e => !e.IsDeleted && !e.ProductionJob.IsDeleted && (tenantProvider.TenantId == null || e.TenantId == tenantProvider.TenantId));
        b.Entity<CostCenter>().HasQueryFilter(e => !e.IsDeleted && (tenantProvider.TenantId == null || e.TenantId == tenantProvider.TenantId));
        b.Entity<CompanyProject>().HasQueryFilter(e => !e.IsDeleted && (tenantProvider.TenantId == null || e.TenantId == tenantProvider.TenantId));
        b.Entity<CheckoutAttachment>().HasQueryFilter(e => !e.IsDeleted && !e.CheckoutSession.IsDeleted && (tenantProvider.TenantId == null || e.TenantId == tenantProvider.TenantId));
        b.Entity<PaymentAttempt>().HasQueryFilter(e => !e.IsDeleted && !e.CheckoutSession.IsDeleted && (tenantProvider.TenantId == null || e.TenantId == tenantProvider.TenantId));
        b.Entity<Coupon>().HasQueryFilter(e => !e.IsDeleted && (tenantProvider.TenantId == null || e.TenantId == tenantProvider.TenantId));
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
