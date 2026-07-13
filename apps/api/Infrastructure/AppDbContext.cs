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
    public DbSet<TwoFactorChallenge> TwoFactorChallenges => Set<TwoFactorChallenge>();
    public DbSet<PasswordResetChallenge> PasswordResetChallenges => Set<PasswordResetChallenge>();
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
    public DbSet<ProductLink> ProductLinks => Set<ProductLink>();
    public DbSet<ProductPriceChange> ProductPriceChanges => Set<ProductPriceChange>();
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
    public DbSet<OrderInternalNote> OrderInternalNotes => Set<OrderInternalNote>();
    public DbSet<OrderCommunication> OrderCommunications => Set<OrderCommunication>();
    public DbSet<AdminOrderRefund> AdminOrderRefunds => Set<AdminOrderRefund>();
    public DbSet<ShipmentItem> ShipmentItems => Set<ShipmentItem>();
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
    public DbSet<ApprovalPolicy> ApprovalPolicies => Set<ApprovalPolicy>();
    public DbSet<ApprovalLevel> ApprovalLevels => Set<ApprovalLevel>();
    public DbSet<ApprovalAssignment> ApprovalAssignments => Set<ApprovalAssignment>();
    public DbSet<ApprovalRequest> ApprovalRequests => Set<ApprovalRequest>();
    public DbSet<ApprovalStep> ApprovalSteps => Set<ApprovalStep>();
    public DbSet<ApprovalAction> ApprovalActions => Set<ApprovalAction>();
    public DbSet<ApprovalAttachment> ApprovalAttachments => Set<ApprovalAttachment>();
    public DbSet<ApprovalDelegation> ApprovalDelegations => Set<ApprovalDelegation>();
    public DbSet<AppNotification> Notifications => Set<AppNotification>();
    public DbSet<Rfq> Rfqs => Set<Rfq>();
    public DbSet<RfqItem> RfqItems => Set<RfqItem>();
    public DbSet<RfqAttachment> RfqAttachments => Set<RfqAttachment>();
    public DbSet<SupplierQuoteRequest> SupplierQuoteRequests => Set<SupplierQuoteRequest>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<SupplierQuote> SupplierQuotes => Set<SupplierQuote>();
    public DbSet<SupplierQuoteItem> SupplierQuoteItems => Set<SupplierQuoteItem>();
    public DbSet<CustomerQuote> CustomerQuotes => Set<CustomerQuote>();
    public DbSet<CustomerQuoteVersion> CustomerQuoteVersions => Set<CustomerQuoteVersion>();
    public DbSet<CustomerQuoteItem> CustomerQuoteItems => Set<CustomerQuoteItem>();
    public DbSet<QuoteNegotiation> QuoteNegotiations => Set<QuoteNegotiation>();
    public DbSet<RfqTemporaryProduct> RfqTemporaryProducts => Set<RfqTemporaryProduct>();
    public DbSet<QuoteTemplate> QuoteTemplates => Set<QuoteTemplate>();
    public DbSet<Shipment> Shipments => Set<Shipment>();
    public DbSet<ShipmentEvent> ShipmentEvents => Set<ShipmentEvent>();
    public DbSet<DeliveryProof> DeliveryProofs => Set<DeliveryProof>();
    public DbSet<DeliveryConfirmation> DeliveryConfirmations => Set<DeliveryConfirmation>();
    public DbSet<OrderRating> OrderRatings => Set<OrderRating>();
    public DbSet<OrderItemRating> OrderItemRatings => Set<OrderItemRating>();
    public DbSet<OrderIssue> OrderIssues => Set<OrderIssue>();
    public DbSet<OrderCancellation> OrderCancellations => Set<OrderCancellation>();
    public DbSet<RecurringOrderSchedule> RecurringOrderSchedules => Set<RecurringOrderSchedule>();
    public DbSet<ReturnRequest> ReturnRequests => Set<ReturnRequest>();
    public DbSet<ReturnItem> ReturnItems => Set<ReturnItem>();
    public DbSet<ReturnAttachment> ReturnAttachments => Set<ReturnAttachment>();
    public DbSet<ReturnStatusHistory> ReturnStatusHistories => Set<ReturnStatusHistory>();
    public DbSet<RefundTransaction> RefundTransactions => Set<RefundTransaction>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceLine> InvoiceLines => Set<InvoiceLine>();
    public DbSet<InvoicePayment> InvoicePayments => Set<InvoicePayment>();
    public DbSet<CreditLimitRequest> CreditLimitRequests => Set<CreditLimitRequest>();
    public DbSet<BudgetAdjustmentRequest> BudgetAdjustmentRequests => Set<BudgetAdjustmentRequest>();
    public DbSet<CompanyInvite> CompanyInvites => Set<CompanyInvite>();
    public DbSet<CompanyBrandProfile> CompanyBrandProfiles => Set<CompanyBrandProfile>();
    public DbSet<CompanyBillingProfile> CompanyBillingProfiles => Set<CompanyBillingProfile>();
    public DbSet<CompanyContract> CompanyContracts => Set<CompanyContract>();
    public DbSet<ContractRenewalRequest> ContractRenewalRequests => Set<ContractRenewalRequest>();
    public DbSet<NotificationPreference> NotificationPreferences => Set<NotificationPreference>();
    public DbSet<SupportTicket> SupportTickets => Set<SupportTicket>();
    public DbSet<SupportMessage> SupportMessages => Set<SupportMessage>();
    public DbSet<SupportAttachment> SupportAttachments => Set<SupportAttachment>();
    public DbSet<CallbackRequest> CallbackRequests => Set<CallbackRequest>();
    public DbSet<SupportArticle> SupportArticles => Set<SupportArticle>();
    public DbSet<ContentPage> ContentPages => Set<ContentPage>();
    public DbSet<AccountDeletionRequest> AccountDeletionRequests => Set<AccountDeletionRequest>();
    public DbSet<MobileAppConfig> MobileAppConfigs => Set<MobileAppConfig>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        b.Entity<RolePermission>().HasKey(x => new { x.RoleId, x.PermissionId });
        b.Entity<UserRole>().HasKey(x => new { x.UserId, x.RoleId });

        b.Entity<Tenant>().HasOne(t => t.Company).WithOne(c => c.Tenant)
            .HasForeignKey<Company>(c => c.TenantId);

        b.Entity<User>().HasIndex(u => u.Phone).IsUnique();
        b.Entity<User>().HasIndex(u => u.Email).IsUnique();
        b.Entity<TwoFactorChallenge>().HasIndex(x => x.TokenHash).IsUnique();
        b.Entity<PasswordResetChallenge>().HasIndex(x => x.TokenHash).IsUnique();
        b.Entity<Product>().HasIndex(p => p.Sku).IsUnique();
        b.Entity<Product>().HasIndex(p => p.Slug).IsUnique();
        b.Entity<Category>().HasIndex(c => c.Slug).IsUnique();
        b.Entity<Permission>().HasIndex(p => p.Code).IsUnique();
        b.Entity<Favorite>().HasIndex(f => new { f.UserId, f.ProductId }).IsUnique();
        b.Entity<CompanyProductPrice>().HasIndex(p => new { p.TenantId, p.ProductId, p.ValidFrom });
        b.Entity<QuantityPriceTier>().HasIndex(t => new { t.ProductId, t.MinQty }).IsUnique();
        b.Entity<ProductVariant>().HasIndex(v => v.Sku).IsUnique();
        b.Entity<ProductLink>().HasIndex(x => new { x.ProductId, x.LinkedProductId, x.Type }).IsUnique();
        b.Entity<ProductPriceChange>().HasIndex(x => new { x.ProductId, x.CreatedAt });
        b.Entity<CompareItem>().HasIndex(c => new { c.UserId, c.ProductId }).IsUnique();
        b.Entity<RecentSearch>().HasIndex(s => new { s.UserId, s.Query }).IsUnique();
        b.Entity<Cart>().HasIndex(c => new { c.TenantId, c.UserId, c.Status });
        b.Entity<CartItem>().HasIndex(i => new { i.CartId, i.ProductId, i.VariantId });
        b.Entity<CheckoutSession>().HasIndex(s => new { s.TenantId, s.UserId, s.CartId, s.Status });
        b.Entity<Order>().HasIndex(o => o.Number).IsUnique();
        b.Entity<Order>().HasIndex(o => new { o.TenantId, o.UserId, o.CreatedAt });
        b.Entity<Order>().HasIndex(o => new { o.ArchivedAt, o.Status, o.RequiredDate });
        b.Entity<OrderInternalNote>().HasIndex(x => new { x.OrderId, x.CreatedAt });
        b.Entity<OrderCommunication>().HasIndex(x => new { x.OrderId, x.CreatedAt });
        b.Entity<AdminOrderRefund>().HasIndex(x => x.Reference).IsUnique();
        b.Entity<ShipmentItem>().HasIndex(x => new { x.ShipmentId, x.OrderItemId }).IsUnique();
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
        b.Entity<ApprovalRequest>().HasIndex(x => x.Number).IsUnique();
        b.Entity<ApprovalRequest>().HasIndex(x => new { x.TenantId, x.Status, x.DueAt });
        b.Entity<ApprovalLevel>().HasIndex(x => new { x.PolicyId, x.Sequence }).IsUnique().HasFilter("\"IsDeleted\" = 0");
        b.Entity<ApprovalStep>().HasIndex(x => new { x.RequestId, x.Sequence }).IsUnique();
        b.Entity<AppNotification>().HasIndex(x => new { x.TenantId, x.UserId, x.ReadAt });
        b.Entity<Rfq>().HasIndex(x => x.Number).IsUnique();
        b.Entity<Rfq>().HasIndex(x => new { x.TenantId, x.UserId, x.Status, x.CreatedAt });
        b.Entity<CustomerQuote>().HasIndex(x => x.Number).IsUnique();
        b.Entity<CustomerQuoteVersion>().HasIndex(x => new { x.QuoteId, x.VersionNumber }).IsUnique();
        b.Entity<SupplierQuote>().HasIndex(x => x.Number).IsUnique();
        b.Entity<Supplier>().HasIndex(x => x.Code).IsUnique();
        b.Entity<RfqTemporaryProduct>().HasIndex(x => x.RfqItemId).IsUnique();
        b.Entity<Shipment>().HasIndex(x => x.Number).IsUnique();
        b.Entity<OrderRating>().HasIndex(x => new { x.OrderId, x.UserId }).IsUnique();
        b.Entity<OrderItemRating>().HasIndex(x => new { x.OrderItemId, x.UserId }).IsUnique();
        b.Entity<DeliveryConfirmation>().HasIndex(x => x.OrderId);
        b.Entity<ReturnRequest>().HasIndex(x => x.Number).IsUnique();
        b.Entity<ReturnRequest>().HasIndex(x => new { x.TenantId, x.UserId, x.Status, x.CreatedAt });
        b.Entity<ReturnItem>().HasIndex(x => new { x.ReturnRequestId, x.OrderItemId }).IsUnique();
        b.Entity<RefundTransaction>().HasIndex(x => x.ProviderReference).IsUnique();
        b.Entity<Invoice>().HasIndex(x => x.Number).IsUnique();
        b.Entity<Invoice>().HasIndex(x => x.OrderId).IsUnique();
        b.Entity<Invoice>().HasIndex(x => new { x.TenantId, x.Status, x.DueAt });
        b.Entity<InvoicePayment>().HasIndex(x => x.Reference).IsUnique();
        b.Entity<CreditLimitRequest>().HasIndex(x => new { x.TenantId, x.Status, x.CreatedAt });
        b.Entity<BudgetAdjustmentRequest>().HasIndex(x => new { x.TenantId, x.CostCenterId, x.Status, x.CreatedAt });
        b.Entity<CompanyInvite>().HasIndex(x => x.TokenHash).IsUnique();
        b.Entity<CompanyInvite>().HasIndex(x => new { x.TenantId, x.Email, x.Status });
        b.Entity<CompanyBrandProfile>().HasIndex(x => x.CompanyId).IsUnique();
        b.Entity<CompanyBillingProfile>().HasIndex(x => x.CompanyId).IsUnique();
        b.Entity<CompanyContract>().HasIndex(x => x.Number).IsUnique();
        b.Entity<ContractRenewalRequest>().HasIndex(x => new { x.ContractId, x.Status });
        b.Entity<NotificationPreference>().HasIndex(x => new { x.TenantId, x.UserId }).IsUnique();
        b.Entity<SupportTicket>().HasIndex(x => x.Number).IsUnique();
        b.Entity<SupportTicket>().HasIndex(x => new { x.TenantId, x.UserId, x.Status, x.CreatedAt });
        b.Entity<SupportMessage>().HasIndex(x => new { x.TicketId, x.CreatedAt });
        b.Entity<CallbackRequest>().HasIndex(x => new { x.TenantId, x.UserId, x.Status, x.PreferredAt });
        b.Entity<SupportArticle>().HasIndex(x => x.Slug).IsUnique();
        b.Entity<ContentPage>().HasIndex(x => x.Slug).IsUnique();
        b.Entity<AccountDeletionRequest>().HasIndex(x => new { x.TenantId, x.UserId, x.Status });
        b.Entity<MobileAppConfig>().HasIndex(x => x.Platform).IsUnique();

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
        b.Entity<ProductLink>().HasQueryFilter(e => !e.IsDeleted);
        b.Entity<ProductPriceChange>().HasQueryFilter(e => !e.IsDeleted);
        b.Entity<CustomProductTemplate>().HasQueryFilter(e => !e.IsDeleted && !e.Product.IsDeleted);
        b.Entity<CustomizationOption>().HasQueryFilter(e => !e.IsDeleted && !e.Template.IsDeleted && !e.Template.Product.IsDeleted);
        b.Entity<PrintMethod>().HasQueryFilter(e => !e.IsDeleted && !e.Template.IsDeleted && !e.Template.Product.IsDeleted);
        b.Entity<CustomMaterial>().HasQueryFilter(e => !e.IsDeleted && !e.Template.IsDeleted && !e.Template.Product.IsDeleted);
        b.Entity<CustomColor>().HasQueryFilter(e => !e.IsDeleted && !e.Template.IsDeleted && !e.Template.Product.IsDeleted);
        b.Entity<CustomSize>().HasQueryFilter(e => !e.IsDeleted && !e.Template.IsDeleted && !e.Template.Product.IsDeleted);

        // dependents of User share its soft-delete filter to avoid filter-mismatch anomalies
        b.Entity<RefreshToken>().HasQueryFilter(e => !e.IsDeleted && !e.User.IsDeleted);
        b.Entity<TwoFactorChallenge>().HasQueryFilter(e => !e.IsDeleted && !e.User.IsDeleted);
        b.Entity<PasswordResetChallenge>().HasQueryFilter(e => !e.IsDeleted && !e.User.IsDeleted);
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
        b.Entity<OrderInternalNote>().HasQueryFilter(e => !e.IsDeleted && (tenantProvider.TenantId == null || e.TenantId == tenantProvider.TenantId));
        b.Entity<OrderCommunication>().HasQueryFilter(e => !e.IsDeleted && (tenantProvider.TenantId == null || e.TenantId == tenantProvider.TenantId));
        b.Entity<AdminOrderRefund>().HasQueryFilter(e => !e.IsDeleted && (tenantProvider.TenantId == null || e.TenantId == tenantProvider.TenantId));
        b.Entity<ShipmentItem>().HasQueryFilter(e => !e.IsDeleted && (tenantProvider.TenantId == null || e.TenantId == tenantProvider.TenantId));
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
        b.Entity<ApprovalPolicy>().HasQueryFilter(e => !e.IsDeleted && (tenantProvider.TenantId == null || e.TenantId == tenantProvider.TenantId));
        b.Entity<ApprovalLevel>().HasQueryFilter(e => !e.IsDeleted && !e.Policy.IsDeleted && (tenantProvider.TenantId == null || e.TenantId == tenantProvider.TenantId));
        b.Entity<ApprovalAssignment>().HasQueryFilter(e => !e.IsDeleted && !e.Level.IsDeleted && (tenantProvider.TenantId == null || e.TenantId == tenantProvider.TenantId));
        b.Entity<ApprovalRequest>().HasQueryFilter(e => !e.IsDeleted && !e.Order.IsDeleted && (tenantProvider.TenantId == null || e.TenantId == tenantProvider.TenantId));
        b.Entity<ApprovalStep>().HasQueryFilter(e => !e.IsDeleted && !e.Request.IsDeleted && (tenantProvider.TenantId == null || e.TenantId == tenantProvider.TenantId));
        b.Entity<ApprovalAction>().HasQueryFilter(e => !e.IsDeleted && !e.Request.IsDeleted && (tenantProvider.TenantId == null || e.TenantId == tenantProvider.TenantId));
        b.Entity<ApprovalAttachment>().HasQueryFilter(e => !e.IsDeleted && !e.Request.IsDeleted && (tenantProvider.TenantId == null || e.TenantId == tenantProvider.TenantId));
        b.Entity<ApprovalDelegation>().HasQueryFilter(e => !e.IsDeleted && (tenantProvider.TenantId == null || e.TenantId == tenantProvider.TenantId));
        b.Entity<AppNotification>().HasQueryFilter(e => !e.IsDeleted && (tenantProvider.TenantId == null || e.TenantId == tenantProvider.TenantId));
        b.Entity<Rfq>().HasQueryFilter(e => !e.IsDeleted && (tenantProvider.TenantId == null || e.TenantId == tenantProvider.TenantId));
        b.Entity<RfqItem>().HasQueryFilter(e => !e.IsDeleted && !e.Rfq.IsDeleted && (tenantProvider.TenantId == null || e.TenantId == tenantProvider.TenantId));
        b.Entity<RfqAttachment>().HasQueryFilter(e => !e.IsDeleted && !e.Rfq.IsDeleted && (tenantProvider.TenantId == null || e.TenantId == tenantProvider.TenantId));
        b.Entity<SupplierQuoteRequest>().HasQueryFilter(e => !e.IsDeleted && (tenantProvider.TenantId == null || e.TenantId == tenantProvider.TenantId));
        b.Entity<Supplier>().HasQueryFilter(e => !e.IsDeleted);
        b.Entity<SupplierQuote>().HasQueryFilter(e => !e.IsDeleted && (tenantProvider.TenantId == null || e.TenantId == tenantProvider.TenantId));
        b.Entity<SupplierQuoteItem>().HasQueryFilter(e => !e.IsDeleted && (tenantProvider.TenantId == null || e.TenantId == tenantProvider.TenantId));
        b.Entity<CustomerQuote>().HasQueryFilter(e => !e.IsDeleted && !e.Rfq.IsDeleted && (tenantProvider.TenantId == null || e.TenantId == tenantProvider.TenantId));
        b.Entity<CustomerQuoteVersion>().HasQueryFilter(e => !e.IsDeleted && !e.Quote.IsDeleted && (tenantProvider.TenantId == null || e.TenantId == tenantProvider.TenantId));
        b.Entity<CustomerQuoteItem>().HasQueryFilter(e => !e.IsDeleted && !e.Version.IsDeleted && (tenantProvider.TenantId == null || e.TenantId == tenantProvider.TenantId));
        b.Entity<QuoteNegotiation>().HasQueryFilter(e => !e.IsDeleted && !e.Rfq.IsDeleted && (tenantProvider.TenantId == null || e.TenantId == tenantProvider.TenantId));
        b.Entity<RfqTemporaryProduct>().HasQueryFilter(e => !e.IsDeleted && (tenantProvider.TenantId == null || e.TenantId == tenantProvider.TenantId));
        b.Entity<QuoteTemplate>().HasQueryFilter(e => !e.IsDeleted);
        b.Entity<Shipment>().HasQueryFilter(e => !e.IsDeleted && !e.Order.IsDeleted && (tenantProvider.TenantId == null || e.TenantId == tenantProvider.TenantId));
        b.Entity<ShipmentEvent>().HasQueryFilter(e => !e.IsDeleted && !e.Shipment.IsDeleted && (tenantProvider.TenantId == null || e.TenantId == tenantProvider.TenantId));
        b.Entity<DeliveryProof>().HasQueryFilter(e => !e.IsDeleted && !e.Order.IsDeleted && (tenantProvider.TenantId == null || e.TenantId == tenantProvider.TenantId));
        b.Entity<DeliveryConfirmation>().HasQueryFilter(e => !e.IsDeleted && (tenantProvider.TenantId == null || e.TenantId == tenantProvider.TenantId));
        b.Entity<OrderRating>().HasQueryFilter(e => !e.IsDeleted && (tenantProvider.TenantId == null || e.TenantId == tenantProvider.TenantId));
        b.Entity<OrderItemRating>().HasQueryFilter(e => !e.IsDeleted && (tenantProvider.TenantId == null || e.TenantId == tenantProvider.TenantId));
        b.Entity<OrderIssue>().HasQueryFilter(e => !e.IsDeleted && (tenantProvider.TenantId == null || e.TenantId == tenantProvider.TenantId));
        b.Entity<OrderCancellation>().HasQueryFilter(e => !e.IsDeleted && (tenantProvider.TenantId == null || e.TenantId == tenantProvider.TenantId));
        b.Entity<RecurringOrderSchedule>().HasQueryFilter(e => !e.IsDeleted && (tenantProvider.TenantId == null || e.TenantId == tenantProvider.TenantId));
        b.Entity<ReturnRequest>().HasQueryFilter(e => !e.IsDeleted && !e.Order.IsDeleted && (tenantProvider.TenantId == null || e.TenantId == tenantProvider.TenantId));
        b.Entity<ReturnItem>().HasQueryFilter(e => !e.IsDeleted && !e.ReturnRequest.IsDeleted && !e.OrderItem.IsDeleted && (tenantProvider.TenantId == null || e.TenantId == tenantProvider.TenantId));
        b.Entity<ReturnAttachment>().HasQueryFilter(e => !e.IsDeleted && !e.ReturnRequest.IsDeleted && (tenantProvider.TenantId == null || e.TenantId == tenantProvider.TenantId));
        b.Entity<ReturnStatusHistory>().HasQueryFilter(e => !e.IsDeleted && !e.ReturnRequest.IsDeleted && (tenantProvider.TenantId == null || e.TenantId == tenantProvider.TenantId));
        b.Entity<RefundTransaction>().HasQueryFilter(e => !e.IsDeleted && !e.ReturnRequest.IsDeleted && (tenantProvider.TenantId == null || e.TenantId == tenantProvider.TenantId));
        b.Entity<Invoice>().HasQueryFilter(e => !e.IsDeleted && !e.Order.IsDeleted && (tenantProvider.TenantId == null || e.TenantId == tenantProvider.TenantId));
        b.Entity<InvoiceLine>().HasQueryFilter(e => !e.IsDeleted && !e.Invoice.IsDeleted && (tenantProvider.TenantId == null || e.TenantId == tenantProvider.TenantId));
        b.Entity<InvoicePayment>().HasQueryFilter(e => !e.IsDeleted && !e.Invoice.IsDeleted && (tenantProvider.TenantId == null || e.TenantId == tenantProvider.TenantId));
        b.Entity<CreditLimitRequest>().HasQueryFilter(e => !e.IsDeleted && (tenantProvider.TenantId == null || e.TenantId == tenantProvider.TenantId));
        b.Entity<BudgetAdjustmentRequest>().HasQueryFilter(e => !e.IsDeleted && !e.CostCenter.IsDeleted && (tenantProvider.TenantId == null || e.TenantId == tenantProvider.TenantId));
        b.Entity<CompanyInvite>().HasQueryFilter(e => !e.IsDeleted && (tenantProvider.TenantId == null || e.TenantId == tenantProvider.TenantId));
        b.Entity<CompanyBrandProfile>().HasQueryFilter(e => !e.IsDeleted && (tenantProvider.TenantId == null || e.TenantId == tenantProvider.TenantId));
        b.Entity<CompanyBillingProfile>().HasQueryFilter(e => !e.IsDeleted && (tenantProvider.TenantId == null || e.TenantId == tenantProvider.TenantId));
        b.Entity<CompanyContract>().HasQueryFilter(e => !e.IsDeleted && (tenantProvider.TenantId == null || e.TenantId == tenantProvider.TenantId));
        b.Entity<ContractRenewalRequest>().HasQueryFilter(e => !e.IsDeleted && !e.Contract.IsDeleted && (tenantProvider.TenantId == null || e.TenantId == tenantProvider.TenantId));
        b.Entity<NotificationPreference>().HasQueryFilter(e => !e.IsDeleted && (tenantProvider.TenantId == null || e.TenantId == tenantProvider.TenantId));
        b.Entity<SupportTicket>().HasQueryFilter(e => !e.IsDeleted && (tenantProvider.TenantId == null || e.TenantId == tenantProvider.TenantId));
        b.Entity<SupportMessage>().HasQueryFilter(e => !e.IsDeleted && !e.Ticket.IsDeleted && (tenantProvider.TenantId == null || e.TenantId == tenantProvider.TenantId));
        b.Entity<SupportAttachment>().HasQueryFilter(e => !e.IsDeleted && !e.Ticket.IsDeleted && (tenantProvider.TenantId == null || e.TenantId == tenantProvider.TenantId));
        b.Entity<CallbackRequest>().HasQueryFilter(e => !e.IsDeleted && (tenantProvider.TenantId == null || e.TenantId == tenantProvider.TenantId));
        b.Entity<SupportArticle>().HasQueryFilter(e => !e.IsDeleted && e.IsPublished);
        b.Entity<ContentPage>().HasQueryFilter(e => !e.IsDeleted && e.IsPublished);
        b.Entity<AccountDeletionRequest>().HasQueryFilter(e => !e.IsDeleted && (tenantProvider.TenantId == null || e.TenantId == tenantProvider.TenantId));
        b.Entity<MobileAppConfig>().HasQueryFilter(e => !e.IsDeleted);
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
