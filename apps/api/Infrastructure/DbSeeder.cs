using Microsoft.EntityFrameworkCore;
using Mohandseto.Api.Domain.Entities;

namespace Mohandseto.Api.Infrastructure;

/// <summary>
/// Seeds base data (permissions, system roles, platform super admin).
/// Idempotent — safe to run on every startup.
/// </summary>
public static class DbSeeder
{
    // (code, module, descriptionAr)
    private static readonly (string Code, string Module, string Desc)[] BasePermissions =
    [
        ("catalog.view", "Catalog", "عرض الكتالوج والمنتجات"),
        ("catalog.manage", "Catalog", "إدارة المنتجات والأقسام"),
        ("orders.create", "Orders", "إنشاء الطلبات"),
        ("orders.view", "Orders", "عرض الطلبات"),
        ("orders.manage", "Orders", "إدارة الطلبات وتحديث حالاتها"),
        ("orders.cancel", "Orders", "إلغاء الطلبات"),
        ("rfq.create", "RFQ", "إنشاء طلبات عروض الأسعار"),
        ("rfq.view", "RFQ", "عرض طلبات عروض الأسعار"),
        ("rfq.manage", "RFQ", "إدارة عروض الأسعار والتسعير"),
        ("approvals.act", "Approvals", "الموافقة أو الرفض على الطلبات الداخلية"),
        ("approvals.manage", "Approvals", "إدارة سياسات الموافقات"),
        ("invoices.view", "Finance", "عرض الفواتير وكشوف الحساب"),
        ("invoices.manage", "Finance", "إدارة الفواتير والمدفوعات"),
        ("budgets.view", "Finance", "عرض الميزانيات ومراكز التكلفة"),
        ("budgets.manage", "Finance", "إدارة الميزانيات ومراكز التكلفة"),
        ("company.manage", "Company", "إدارة بيانات الشركة والفروع"),
        ("company.users.manage", "Company", "إدارة مستخدمي الشركة وأدوارهم"),
        ("inventory.view", "Inventory", "عرض المخزون"),
        ("inventory.manage", "Inventory", "إدارة المخزون والمستودعات"),
        ("suppliers.manage", "Procurement", "إدارة الموردين والمشتريات"),
        ("crm.view", "CRM", "عرض بيانات الشركات العملاء"),
        ("crm.manage", "CRM", "إدارة الشركات والعقود والأسعار الخاصة"),
        ("companies.verify", "CRM", "مراجعة واعتماد تسجيل الشركات"),
        ("design.manage", "Printing", "إدارة طلبات التصميم والطباعة"),
        ("shipping.manage", "Shipping", "إدارة الشحن والتوصيل"),
        ("shipping.driver", "Shipping", "تنفيذ مهام التوصيل (مندوب)"),
        ("support.handle", "Support", "التعامل مع تذاكر الدعم"),
        ("campaigns.manage", "Marketing", "إدارة الحملات والإشعارات"),
        ("reports.view", "Reports", "عرض التقارير والتحليلات"),
        ("settings.manage", "System", "إدارة إعدادات النظام"),
        ("roles.manage", "System", "إدارة الأدوار والصلاحيات"),
        ("audit.view", "System", "عرض سجل التدقيق ومراقبة النظام"),
    ];

    // (code, nameAr, nameEn, permissions) — tenant roles (TenantId=null template, IsSystem=true)
    private static readonly (string Code, string Ar, string En, string[] Perms)[] ClientRoles =
    [
        ("company_owner", "صاحب الشركة", "Company Owner",
            ["catalog.view","orders.create","orders.view","orders.cancel","rfq.create","rfq.view","approvals.act",
             "invoices.view","budgets.view","budgets.manage","company.manage","company.users.manage","reports.view"]),
        ("purchasing_officer", "موظف مشتريات", "Purchasing Officer",
            ["catalog.view","orders.create","orders.view","rfq.create","rfq.view"]),
        ("company_admin", "مسؤول إداري", "Company Admin",
            ["catalog.view","orders.view","company.manage","company.users.manage"]),
        ("finance_manager", "مدير مالي", "Finance Manager",
            ["invoices.view","budgets.view","budgets.manage","approvals.act","reports.view"]),
        ("warehouse_officer", "مسؤول مخازن", "Warehouse Officer",
            ["catalog.view","orders.view","inventory.view"]),
        ("department_manager", "مدير إدارة", "Department Manager",
            ["catalog.view","orders.create","orders.view","rfq.view","approvals.act","budgets.view"]),
        ("approver", "مسؤول موافقات", "Approver",
            ["orders.view","approvals.act"]),
        ("billing_officer", "مسؤول فواتير", "Billing Officer",
            ["invoices.view"]),
        ("requester", "مستخدم مخول بالطلب", "Requester",
            ["catalog.view","orders.create","orders.view"]),
    ];

    private static readonly (string Code, string Ar, string En, string[] Perms)[] PlatformRoles =
    [
        ("super_admin", "مدير النظام الأعلى", "Super Admin", ["*"]),
        ("sales_manager", "مدير مبيعات", "Sales Manager",
            ["orders.view","orders.manage","rfq.view","rfq.manage","crm.view","crm.manage","reports.view"]),
        ("sales_agent", "موظف مبيعات", "Sales Agent", ["orders.view","rfq.view","crm.view"]),
        ("quotes_officer", "مسؤول عروض أسعار", "Quotes Officer", ["rfq.view","rfq.manage"]),
        ("products_manager", "مسؤول منتجات", "Products Manager", ["catalog.view","catalog.manage"]),
        ("inventory_manager", "مسؤول مخزون", "Inventory Manager", ["inventory.view","inventory.manage"]),
        ("warehouse_manager", "مسؤول مستودع", "Warehouse Manager", ["inventory.view","inventory.manage","orders.view"]),
        ("procurement_officer", "مسؤول مشتريات", "Procurement Officer", ["suppliers.manage","inventory.view"]),
        ("accountant", "مسؤول حسابات", "Accountant", ["invoices.view","invoices.manage","reports.view"]),
        ("support_agent", "خدمة عملاء", "Support Agent", ["support.handle","orders.view","crm.view"]),
        ("graphic_designer", "مصمم جرافيك", "Graphic Designer", ["design.manage"]),
        ("printing_officer", "مسؤول طباعة", "Printing Officer", ["design.manage"]),
        ("delivery_driver", "مندوب توصيل", "Delivery Driver", ["shipping.driver"]),
        ("operations_manager", "مدير تشغيل", "Operations Manager",
            ["orders.view","orders.manage","inventory.view","shipping.manage","reports.view"]),
        ("system_admin", "مدير نظام", "System Admin", ["settings.manage","roles.manage","audit.view"]),
        ("auditor", "مراجع (قراءة فقط)", "Auditor / Read Only",
            ["orders.view","rfq.view","invoices.view","inventory.view","crm.view","reports.view","audit.view"]),
    ];

    public static async Task SeedAsync(AppDbContext db, IConfiguration config, ILogger logger)
    {
        // 1) permissions
        var existingPerms = await db.Permissions.Select(p => p.Code).ToListAsync();
        foreach (var (code, module, desc) in BasePermissions.Where(p => !existingPerms.Contains(p.Code)))
            db.Permissions.Add(new Permission { Code = code, Module = module, DescriptionAr = desc });
        await db.SaveChangesAsync();

        // 2) roles + role-permissions
        var allPerms = await db.Permissions.ToListAsync();
        var existingRoles = await db.Roles.IgnoreQueryFilters().Select(r => r.Code).ToListAsync();
        foreach (var (code, ar, en, permCodes) in ClientRoles.Concat(PlatformRoles))
        {
            if (existingRoles.Contains(code)) continue;
            var role = new Role { Code = code, NameAr = ar, NameEn = en, IsSystem = true };
            var target = permCodes is ["*"] ? allPerms : allPerms.Where(p => permCodes.Contains(p.Code));
            foreach (var p in target)
                role.Permissions.Add(new RolePermission { RoleId = role.Id, PermissionId = p.Id });
            db.Roles.Add(role);
        }
        await db.SaveChangesAsync();

        // 3) platform super admin (development credentials; production overrides via env)
        var adminEmail = config["Seed:AdminEmail"] ?? "admin@mohandseto.com";
        if (!await db.Users.AnyAsync(u => u.Email == adminEmail))
        {
            var superRole = await db.Roles.FirstAsync(r => r.Code == "super_admin");
            var admin = new User
            {
                FullName = "أحمد سليمان",
                Phone = "+201000000001",
                Email = adminEmail,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(config["Seed:AdminPassword"] ?? "Admin@12345"),
                IsPlatformStaff = true,
                PhoneVerified = true,
                EmailVerified = true,
            };
            admin.Roles.Add(new UserRole { UserId = admin.Id, RoleId = superRole.Id });
            db.Users.Add(admin);
            await db.SaveChangesAsync();
            logger.LogInformation("Seeded platform super admin: {Email}", adminEmail);
        }

        // 4) catalog reference/demo data (idempotent)
        if (!await db.ContentPages.AnyAsync())
        {
            db.ContentPages.AddRange(
                new ContentPage { Slug = "terms", TitleAr = "الشروط والأحكام", BodyAr = "تنظم هذه الشروط استخدام منصة مهندسيتو توريدات وطلبات الشراء وعروض الأسعار والدفع والتسليم والمرتجعات. تُعرض الأسعار والضرائب ومواعيد التسليم النهائية قبل تأكيد كل طلب، ويلتزم المستخدم المخول بسياسات شركته وصلاحياتها." },
                new ContentPage { Slug = "privacy", TitleAr = "سياسة الخصوصية", BodyAr = "نجمع بيانات الحساب والشركة والطلبات اللازمة لتقديم الخدمة وحمايتها. تُعزل بيانات كل شركة، وتُشفّر الاتصالات، ولا نبيع البيانات الشخصية. يمكن طلب الوصول أو التصحيح أو الحذف من مركز الدعم." },
                new ContentPage { Slug = "about", TitleAr = "من نحن", BodyAr = "مهندسيتو توريدات منصة B2B مصرية توحّد مشتريات الشركات وعروض الأسعار والموافقات والميزانيات والتسليم في تجربة رقمية واحدة دقيقة وقابلة للتدقيق." },
                new ContentPage { Slug = "contact", TitleAr = "تواصل معنا", BodyAr = "فريقنا متاح لمساعدتك في الطلبات والحسابات والتوريدات الخاصة.", ContactPhone = "+20200000000", WhatsAppPhone = "+201000000000", ContactEmail = "support@mohandseto.com", Address = "القاهرة، جمهورية مصر العربية" });
        }
        if (!await db.SupportArticles.AnyAsync())
        {
            db.SupportArticles.AddRange(
                new SupportArticle { Slug = "track-order", Category = "orders", SortOrder = 1, QuestionAr = "كيف أتابع حالة طلبي؟", AnswerAr = "افتح الطلب ثم اختر تتبع الشحنات لعرض الخط الزمني والموقع المتوقع وإثبات التسليم." },
                new SupportArticle { Slug = "quote-time", Category = "quotes", SortOrder = 2, QuestionAr = "كم يستغرق استلام عرض السعر؟", AnswerAr = "يعتمد الوقت على عدد الأصناف والتخصيص، وستصلك إشعارات بكل تحديث أو طلب معلومات إضافية." },
                new SupportArticle { Slug = "invoice-download", Category = "billing", SortOrder = 3, QuestionAr = "كيف أحصل على الفاتورة الإلكترونية؟", AnswerAr = "من قسم المالية افتح الفاتورة ثم اختر تنزيل PDF أو Excel حسب الصلاحيات." },
                new SupportArticle { Slug = "return-order", Category = "returns", SortOrder = 4, QuestionAr = "كيف أطلب إرجاع صنف؟", AnswerAr = "يمكن بدء طلب الإرجاع من تفاصيل الطلب خلال فترة الأهلية وتحديد الكمية والسبب وإرفاق الصور." });
        }
        if (!await db.MobileAppConfigs.AnyAsync())
            db.MobileAppConfigs.Add(new MobileAppConfig { Platform = "all", MinimumVersion = "0.2.0", LatestVersion = "0.2.0", MessageAr = "يتوفر إصدار أحدث من تطبيق مهندسيتو توريدات." });
        if (!await db.HomeSections.AnyAsync())
        {
            db.HomeSections.AddRange(
                new HomeSection { Key = "hero_banners", NameAr = "البنرات الرئيسية", SortOrder = 0 },
                new HomeSection { Key = "featured_categories", NameAr = "الأقسام المميزة", SortOrder = 1 },
                new HomeSection { Key = "recommended_products", NameAr = "منتجات مقترحة لشركتك", SortOrder = 2 },
                new HomeSection { Key = "latest_offers", NameAr = "أحدث العروض", SortOrder = 3 },
                new HomeSection { Key = "recently_viewed", NameAr = "شوهدت مؤخرًا", SortOrder = 4 });
        }
        await db.SaveChangesAsync();
        await CatalogSeeder.SeedAsync(db, logger);
        await InventorySeeder.SeedAsync(db, logger);
        await CustomizationSeeder.SeedAsync(db, logger);
    }
}
