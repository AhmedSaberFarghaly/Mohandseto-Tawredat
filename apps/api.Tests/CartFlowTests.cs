using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;
using Mohandseto.Api.Application.Common;
using Mohandseto.Api.Application.Shopping;
using Mohandseto.Api.Domain.Entities;
using Mohandseto.Api.Infrastructure;

namespace Mohandseto.Api.Tests;

public sealed class CartFlowTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;
    private readonly TestTenantProvider _tenant = new();
    private readonly CartService _cart;
    private readonly string _filesRoot = Path.Combine(Path.GetTempPath(), "mohandseto-checkout-tests", Guid.NewGuid().ToString("N"));
    private sealed class TestTenantProvider : ITenantProvider { public Guid? TenantId { get; set; } }
    private sealed class TestEnvironment(string root) : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "Tests";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = root;
        public string EnvironmentName { get; set; } = "Development";
        public string ContentRootPath { get; set; } = root;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    public CartFlowTests()
    {
        Directory.CreateDirectory(_filesRoot);
        _connection = new SqliteConnection("DataSource=:memory:"); _connection.Open();
        _db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options, _tenant);
        _db.Database.EnsureCreated();
        CatalogSeeder.SeedAsync(_db, NullLogger.Instance).GetAwaiter().GetResult();
        _cart = new CartService(_db, _tenant);
    }

    public void Dispose()
    {
        _db.Dispose(); _connection.Dispose();
        if (Directory.Exists(_filesRoot)) Directory.Delete(_filesRoot, true);
    }

    [Fact]
    public async Task Cart_calculates_quantity_tiers_and_supports_saved_items()
    {
        _tenant.TenantId = Guid.NewGuid(); var userId = Guid.NewGuid();
        var product = await _db.Products.Include(p => p.PriceTiers).FirstAsync(p => p.StockQty >= 20);
        var quantity = Math.Max(10, product.MinOrderQty);
        var result = await _cart.AddAsync(userId, new AddCartItemDto(product.Id, null, quantity, null));
        var item = Assert.Single(result.Items);
        var expected = product.PriceTiers.Where(t => t.MinQty <= quantity).OrderByDescending(t => t.MinQty).First().UnitPrice;
        Assert.Equal(expected, item.UnitPrice);
        Assert.Equal(expected * quantity, result.Subtotal);
        Assert.True(result.TaxIncluded > 0);

        result = await _cart.SetSavedAsync(userId, item.Id, true);
        Assert.Empty(result.Items); Assert.Single(result.SavedItems); Assert.Equal(0, result.Total);
        result = await _cart.SetSavedAsync(userId, item.Id, false);
        Assert.Single(result.Items); Assert.Empty(result.SavedItems);
    }

    [Fact]
    public async Task Cart_is_tenant_isolated_and_rejects_unavailable_quantity()
    {
        var firstTenant = Guid.NewGuid(); _tenant.TenantId = firstTenant; var userId = Guid.NewGuid();
        var product = await _db.Products.FirstAsync(p => p.StockQty > 0);
        await _cart.AddAsync(userId, new AddCartItemDto(product.Id, null, product.MinOrderQty, null));

        _tenant.TenantId = Guid.NewGuid();
        Assert.Empty((await _cart.GetAsync(userId)).Items);
        _tenant.TenantId = firstTenant;
        var itemId = (await _cart.GetAsync(userId)).Items[0].Id;
        var error = await Assert.ThrowsAsync<ApiException>(() => _cart.UpdateAsync(userId, itemId, product.StockQty + 1));
        Assert.Equal(409, error.StatusCode);
    }

    [Fact]
    public async Task Cart_supports_coupon_notes_price_acknowledgement_and_saved_cart_restore()
    {
        var tenantId = Guid.NewGuid(); _tenant.TenantId = tenantId; var userId = Guid.NewGuid();
        var coupon = new Coupon { TenantId = tenantId, Code = "SAVE10", NameAr = "خصم اختبار",
            DiscountType = CouponDiscountType.Percentage, DiscountValue = 10, MinimumSubtotal = 1, MaximumDiscount = 500 };
        _db.Coupons.Add(coupon); await _db.SaveChangesAsync();
        var product = await _db.Products.Include(p => p.PriceTiers).FirstAsync(p => p.StockQty >= Math.Max(10, p.MinOrderQty));
        var quantity = Math.Max(10, product.MinOrderQty);
        var cart = await _cart.AddAsync(userId, new AddCartItemDto(product.Id, null, quantity, null));
        var item = Assert.Single(cart.Items);
        cart = await _cart.SetItemNoteAsync(userId, item.Id, "تغليف كل عشر قطع معًا");
        Assert.Equal("تغليف كل عشر قطع معًا", cart.Items.Single().CustomerNote);
        cart = await _cart.ApplyCouponAsync(userId, " save10 ");
        Assert.Equal("SAVE10", cart.CouponCode); Assert.True(cart.CouponDiscount > 0);
        Assert.Equal(cart.Subtotal + cart.Shipping, cart.Total);

        product.BasePrice += 20;
        foreach (var tier in product.PriceTiers) tier.UnitPrice += 20;
        await _db.SaveChangesAsync();
        cart = await _cart.GetAsync(userId);
        Assert.True(cart.HasPriceChanges); Assert.True(cart.Items.Single().PriceChanged);
        cart = await _cart.AcknowledgePricesAsync(userId);
        Assert.False(cart.HasPriceChanges);

        var saved = await _cart.SaveCartAsync(userId, "احتياجات الاختبار");
        Assert.Equal("احتياجات الاختبار", saved.Name); Assert.Empty((await _cart.GetAsync(userId)).Items);
        Assert.Equal(saved.Id, Assert.Single(await _cart.SavedCartsAsync(userId)).Id);
        cart = await _cart.RestoreCartAsync(userId, saved.Id);
        Assert.Single(cart.Items); Assert.Equal("SAVE10", cart.CouponCode);

        product.StockQty = quantity - 1; await _db.SaveChangesAsync();
        cart = await _cart.GetAsync(userId);
        Assert.True(cart.HasAvailabilityIssues); Assert.True(cart.Items.Single().HasAvailabilityIssue);
    }

    [Fact]
    public async Task Checkout_creates_immutable_order_snapshot_and_converts_cart()
    {
        var tenantId = Guid.NewGuid(); _tenant.TenantId = tenantId; var userId = Guid.NewGuid();
        var tenant = new Tenant { Id = tenantId, Name = "Checkout Co", Status = TenantStatus.Active };
        var company = new Company { TenantId = tenantId, LegalName = "Checkout Co", Phone = "01000000000", CreditLimit = 100000 };
        var branch = new CompanyBranch { TenantId = tenantId, CompanyId = company.Id, Name = "الفرع الرئيسي", Governorate = "القاهرة", City = "مدينة نصر", AddressLine = "شارع الاختبار", IsMain = true };
        var center = new CostCenter { TenantId = tenantId, Code = "CC-TEST", NameAr = "مركز تكلفة الاختبار", BudgetAmount = 100000, ApprovalThreshold = 5000 };
        var project = new CompanyProject { TenantId = tenantId, Code = "PRJ-TEST", NameAr = "مشروع الاختبار" };
        _db.AddRange(tenant, company, branch, center, project); await _db.SaveChangesAsync();

        var product = await _db.Products.OrderByDescending(p => p.BasePrice).FirstAsync(p => p.StockQty >= 20);
        await _cart.AddAsync(userId, new AddCartItemDto(product.Id, null, Math.Max(20, product.MinOrderQty), null));
        var environment = new TestEnvironment(_filesRoot);
        var gateway = new PaymentGatewayService(_db, _tenant, environment, new ConfigurationBuilder().Build());
        var checkout = new CheckoutService(_db, _tenant, _cart, gateway, environment);
        var options = await checkout.OptionsAsync(userId);
        Assert.Single(options.Branches); Assert.Contains(options.PaymentOptions, p => p.Code == "CreditLine" && p.Enabled);
        await checkout.ContextAsync(userId, new UpdateCheckoutContextDto(center.Id, project.Id, "إدارة المشتريات", "طلب اختبار", "مرجع الاختبار"));
        await checkout.DeliveryAsync(userId, new UpdateDeliveryDto(branch.Id, "أحمد المستلم", "01011112222", DateTime.UtcNow.AddDays(3), "09:00-12:00", "Standard"));
        await checkout.PaymentAsync(userId, new UpdatePaymentDto("CreditLine", "PO-TEST-1", "مركز تكلفة الاختبار"));
        var review = await checkout.ReviewAsync(userId);
        Assert.NotEmpty(review.Items); Assert.Equal(branch.Name, review.BranchName); Assert.Equal("PO-TEST-1", review.PurchaseOrderNumber);

        var created = await checkout.SubmitAsync(userId, true);
        var order = await _db.Orders.Include(o => o.Items).Include(o => o.History).SingleAsync(o => o.Id == created.Id);
        Assert.Equal(review.Total, order.Total); Assert.Equal(review.Items.Count, order.Items.Count); Assert.Single(order.History);
        Assert.Equal(CartStatus.Converted, (await _db.Carts.IgnoreQueryFilters().SingleAsync(c => c.UserId == userId)).Status);
        Assert.Empty((await _cart.GetAsync(userId)).Items);
    }

    [Fact]
    public async Task Checkout_reserves_exceeded_budget_and_protects_purchase_order_attachment()
    {
        var tenantId = Guid.NewGuid(); _tenant.TenantId = tenantId; var userId = Guid.NewGuid();
        var tenant = new Tenant { Id = tenantId, Name = "Budget Co", Status = TenantStatus.Active };
        var company = new Company { TenantId = tenantId, LegalName = "Budget Co", Phone = "01000000000", CreditLimit = 100000 };
        var branch = new CompanyBranch { TenantId = tenantId, CompanyId = company.Id, Name = "المخزن", Governorate = "الجيزة", City = "الدقي", AddressLine = "شارع التحرير" };
        var center = new CostCenter { TenantId = tenantId, Code = "CC-LIMIT", NameAr = "ميزانية محدودة", BudgetAmount = 1, ApprovalThreshold = 1 };
        _db.AddRange(tenant, company, branch, center); await _db.SaveChangesAsync();
        var product = await _db.Products.FirstAsync(p => p.StockQty >= p.MinOrderQty);
        await _cart.AddAsync(userId, new AddCartItemDto(product.Id, null, product.MinOrderQty, null));
        var environment = new TestEnvironment(_filesRoot);
        var gateway = new PaymentGatewayService(_db, _tenant, environment, new ConfigurationBuilder().Build());
        var checkout = new CheckoutService(_db, _tenant, _cart, gateway, environment);
        await checkout.ContextAsync(userId, new UpdateCheckoutContextDto(center.Id, null, "التشغيل", "توريد عاجل", null));
        await checkout.DeliveryAsync(userId, new UpdateDeliveryDto(branch.Id, "مسؤول المخزن", "01011112222", DateTime.UtcNow.AddDays(2), "12:00-15:00", "Express", true));
        await checkout.PaymentAsync(userId, new UpdatePaymentDto("BankTransfer", "PO-SECURE", null));

        var bytes = new byte[] { 37, 80, 68, 70, 45, 49, 46, 55 };
        var upload = new FormFile(new MemoryStream(bytes), 0, bytes.Length, "file", "../purchase-order.pdf")
        { Headers = new HeaderDictionary(), ContentType = "application/pdf" };
        var attachment = await checkout.UploadAttachmentAsync(userId, upload);
        Assert.Equal("purchase-order.pdf", attachment.Name);
        var stored = await checkout.AttachmentAsync(userId, attachment.Id);
        Assert.True(File.Exists(stored.Path)); Assert.StartsWith(_filesRoot, stored.Path, StringComparison.OrdinalIgnoreCase);

        var review = await checkout.ReviewAsync(userId);
        Assert.True(review.BudgetExceeded); Assert.True(review.RequiresApproval); Assert.True(review.AllowSplitDelivery);
        var order = await checkout.SubmitAsync(userId, true);
        Assert.True(order.RequiresApproval);
        Assert.Equal(review.Total, (await _db.CostCenters.SingleAsync(c => c.Id == center.Id)).ReservedAmount);
        Assert.Equal(order.Id, (await _db.CheckoutAttachments.SingleAsync(a => a.Id == attachment.Id)).OrderId);

        _tenant.TenantId = Guid.NewGuid();
        var hidden = await Assert.ThrowsAsync<ApiException>(() => checkout.AttachmentAsync(userId, attachment.Id));
        Assert.Equal(404, hidden.StatusCode);
    }

    [Fact]
    public async Task Electronic_payment_is_idempotent_and_supports_failed_retry_and_partial_payment()
    {
        var tenantId = Guid.NewGuid(); _tenant.TenantId = tenantId; var userId = Guid.NewGuid();
        var tenant = new Tenant { Id = tenantId, Name = "Payment Co", Status = TenantStatus.Active };
        var company = new Company { TenantId = tenantId, LegalName = "Payment Co", Phone = "01000000000", CreditLimit = 100 };
        var branch = new CompanyBranch { TenantId = tenantId, CompanyId = company.Id, Name = "المقر", Governorate = "القاهرة", City = "المعادي", AddressLine = "شارع النصر" };
        var center = new CostCenter { TenantId = tenantId, Code = "CC-PAY", NameAr = "مركز الدفع", BudgetAmount = 100000 };
        var user = new User { Id = userId, TenantId = tenantId, FullName = "مستخدم الدفع", Phone = "01012345678", PhoneVerified = true };
        _db.AddRange(tenant, company, branch, center, user); await _db.SaveChangesAsync();
        var product = await _db.Products.OrderByDescending(p => p.BasePrice).FirstAsync(p => p.StockQty >= p.MinOrderQty);
        await _cart.AddAsync(userId, new AddCartItemDto(product.Id, null, product.MinOrderQty, null));
        var environment = new TestEnvironment(_filesRoot);
        var gateway = new PaymentGatewayService(_db, _tenant, environment, new ConfigurationBuilder().Build());
        var checkout = new CheckoutService(_db, _tenant, _cart, gateway, environment);
        await checkout.ContextAsync(userId, new UpdateCheckoutContextDto(center.Id, null, "المشتريات", null, null));
        await checkout.DeliveryAsync(userId, new UpdateDeliveryDto(branch.Id, "المستلم", "01011112222", DateTime.UtcNow.AddDays(2), "09:00-12:00", "Express"));
        var options = await checkout.OptionsAsync(userId);
        var total = options.Cart.Subtotal + 150;
        Assert.True(total > company.CreditLimit);
        Assert.Contains(options.PaymentOptions, option => option.Code == "Partial" && option.Enabled);
        var cardPart = total - company.CreditLimit;

        var created = await checkout.CreatePaymentAsync(userId, new CreatePaymentAttemptDto("partial-payment-test", cardPart));
        var duplicate = await checkout.CreatePaymentAsync(userId, new CreatePaymentAttemptDto("partial-payment-test", cardPart));
        Assert.Equal(created.Id, duplicate.Id);
        var failed = await checkout.ConfirmPaymentAsync(userId, created.Id, new ConfirmPaymentAttemptDto("tok_test_declined"));
        Assert.Equal("Failed", failed.Status); Assert.NotNull(failed.FailureMessage);
        var succeeded = await checkout.ConfirmPaymentAsync(userId, created.Id, new ConfirmPaymentAttemptDto("tok_test_success"));
        Assert.Equal("Succeeded", succeeded.Status);

        await checkout.PaymentAsync(userId, new UpdatePaymentDto("Partial", null, null, created.Id, company.CreditLimit, cardPart));
        var review = await checkout.ReviewAsync(userId);
        Assert.Equal(total, review.Total);
        var session = await _db.CheckoutSessions.SingleAsync(s => s.Id == options.SessionId);
        Assert.Equal(company.CreditLimit, session.CreditPortion); Assert.Equal(cardPart, session.CardPortion);
    }
}
