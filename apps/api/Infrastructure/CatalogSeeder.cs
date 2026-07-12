using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Mohandseto.Api.Domain.Entities;

namespace Mohandseto.Api.Infrastructure;

public static class CatalogSeeder
{
    private static readonly (string Ar, string En, string Slug, string Icon, string[] Children)[] CategoryMap =
    [
        ("الأدوات المكتبية", "Office Supplies", "office-supplies", "business_center", ["ملفات وحافظات|Files & Folders", "دباسات وخزامات|Staplers & Punches", "تنظيم المكتب|Desk Organization", "آلات حاسبة|Calculators"]),
        ("الأقلام والكتابة", "Pens & Writing", "writing", "edit", ["أقلام جاف|Ballpoint Pens", "أقلام ماركر|Markers", "أقلام رصاص|Pencils", "أدوات تصحيح|Correction Tools"]),
        ("الورق والكراسات", "Paper & Notebooks", "paper", "description", ["ورق تصوير|Copy Paper", "دفاتر وكراسات|Notebooks", "ملاحظات لاصقة|Sticky Notes"]),
        ("الطباعة والأحبار", "Printing & Ink", "printing", "print", ["أحبار طابعات|Printer Ink", "تونر ليزر|Laser Toner", "ملحقات الطباعة|Print Accessories"]),
        ("النظافة والعناية", "Cleaning", "cleaning", "cleaning_services", ["منظفات أسطح|Surface Cleaners", "مناديل ورقية|Tissues", "أكياس قمامة|Garbage Bags", "أدوات تنظيف|Cleaning Tools"]),
        ("التغليف والشحن", "Packaging", "packaging", "inventory_2", ["كراتين شحن|Shipping Boxes", "أشرطة لاصقة|Packing Tapes", "أغلفة وحماية|Protective Wrap"]),
        ("الأمن والسلامة", "Safety", "safety", "health_and_safety", ["مهمات وقاية|PPE", "لافتات سلامة|Safety Signs", "إسعافات أولية|First Aid"]),
        ("الكهرباء والإضاءة", "Electrical", "electrical", "electrical_services", ["لمبات وإضاءة|Lighting", "بطاريات|Batteries", "مشتركات وكابلات|Power & Cables"]),
        ("العدد والأدوات", "Tools", "tools", "handyman", ["عدد يدوية|Hand Tools", "عدد كهربائية|Power Tools", "قياس وتثبيت|Measuring & Fixing"]),
        ("ضيافة وبوفيه", "Hospitality", "hospitality", "local_cafe", ["مشروبات ساخنة|Hot Drinks", "مياه ومشروبات|Water & Beverages", "مستلزمات بوفيه|Pantry Supplies"]),
        ("تقنية وملحقات", "Technology", "technology", "devices", ["ملحقات كمبيوتر|Computer Accessories", "شبكات واتصالات|Networking", "تخزين بيانات|Data Storage", "أجهزة مكتبية|Office Electronics"]),
        ("أثاث وتجهيزات", "Furniture", "furniture", "chair", ["كراسي مكتبية|Office Chairs", "مكاتب وطاولات|Desks & Tables", "وحدات تخزين|Storage Furniture"]),
    ];

    private static readonly (string Ar, string En, string Slug)[] BrandMap =
    [
        ("دلي", "Deli", "deli"), ("روترينج", "Rotring", "rotring"), ("دبل إيه", "Double A", "double-a"),
        ("إتش بي", "HP", "hp"), ("كانون", "Canon", "canon"), ("فاين", "Fine", "fine"),
        ("فريش", "Fresh", "fresh"), ("تورنيدو", "Tornado", "tornado"), ("بوش", "Bosch", "bosch"),
        ("مهندسيتو", "Mohandseto", "mohandseto"),
    ];

    private static readonly (string Code, string Ar, string En)[] UnitMap =
    [
        ("piece", "قطعة", "Piece"), ("box", "علبة", "Box"), ("pack", "عبوة", "Pack"),
        ("carton", "كرتونة", "Carton"), ("ream", "رزمة", "Ream"), ("set", "طقم", "Set"),
    ];

    private static readonly string[] QualitiesAr = ["اقتصادي", "قياسي", "احترافي", "ممتاز", "للاستخدام الكثيف"];
    private static readonly string[] QualitiesEn = ["Economy", "Standard", "Professional", "Premium", "Heavy Duty"];

    public static async Task SeedAsync(AppDbContext db, ILogger logger, CancellationToken ct = default)
    {
        var units = await SeedUnitsAsync(db, ct);
        var brands = await SeedBrandsAsync(db, ct);
        var categories = await SeedCategoriesAsync(db, ct);
        await db.SaveChangesAsync(ct);

        var existingSkus = await db.Products.IgnoreQueryFilters().Select(p => p.Sku).ToHashSetAsync(ct);
        var subcategories = categories.Where(c => c.ParentId != null).OrderBy(c => c.SortOrder).ToList();
        var added = 0;
        for (var i = 1; i <= 250; i++)
        {
            var sku = $"MT-{i:00000}";
            if (existingSkus.Contains(sku)) continue;
            var category = subcategories[(i - 1) % subcategories.Count];
            var brand = brands[(i - 1) % brands.Count];
            var unit = units[(i - 1) % units.Count];
            var qualityIndex = (i - 1) % QualitiesAr.Length;
            var basePrice = decimal.Round(18m + ((i * 37) % 4200) + (i % 7) * 0.75m, 2);
            var stock = i % 17 == 0 ? 0 : i % 11 == 0 ? 6 : 25 + (i * 13 % 240);
            var slug = $"{category.Slug}-{i:000}";
            var product = new Product
            {
                Sku = sku,
                NameAr = $"{category.NameAr} {QualitiesAr[qualityIndex]} {i:000}",
                NameEn = $"{category.NameEn} {QualitiesEn[qualityIndex]} {i:000}",
                Slug = slug,
                DescriptionAr = $"منتج {category.NameAr} بجودة {QualitiesAr[qualityIndex]} مناسب لاحتياجات الشركات والاستخدام اليومي. متاح بكميات وأسعار توريد تنافسية.",
                DescriptionEn = $"{QualitiesEn[qualityIndex]} {category.NameEn} for business and daily office use, available with competitive quantity pricing.",
                CategoryId = category.Id,
                BrandId = brand.Id,
                UnitId = unit.Id,
                BasePrice = basePrice,
                CompareAtPrice = i % 4 == 0 ? decimal.Round(basePrice * 1.12m, 2) : null,
                MinOrderQty = unit.Code is "carton" or "box" ? 2 : 1,
                StockQty = stock,
                LowStockThreshold = 10,
                IsFeatured = i <= 24 || i % 23 == 0,
                IsPrintable = i % 13 == 0,
                RatingAvg = 3.7 + (i % 13) / 10d,
                RatingCount = 4 + (i * 7 % 180),
                WarrantyAr = i % 8 == 0 ? "ضمان استبدال لمدة 12 شهرًا ضد عيوب الصناعة" : "ضمان جودة وتوافق مع المواصفات",
                DeliveryEstimateDays = 1 + i % 5,
            };
            product.Images.Add(new ProductImage
            {
                ProductId = product.Id,
                Path = $"asset://catalog/{slug}.webp",
                AltAr = product.NameAr,
                IsPrimary = true,
            });
            product.PriceTiers.Add(new QuantityPriceTier { ProductId = product.Id, MinQty = product.MinOrderQty, UnitPrice = basePrice });
            product.PriceTiers.Add(new QuantityPriceTier { ProductId = product.Id, MinQty = 25, UnitPrice = decimal.Round(basePrice * .96m, 2) });
            product.PriceTiers.Add(new QuantityPriceTier { ProductId = product.Id, MinQty = 100, UnitPrice = decimal.Round(basePrice * .91m, 2) });
            product.Attributes.Add(new ProductAttributeValue { ProductId = product.Id, NameAr = "العلامة التجارية", ValueAr = brand.NameAr, SortOrder = 1 });
            product.Attributes.Add(new ProductAttributeValue { ProductId = product.Id, NameAr = "وحدة البيع", ValueAr = unit.NameAr, SortOrder = 2 });
            product.Attributes.Add(new ProductAttributeValue { ProductId = product.Id, NameAr = "بلد المنشأ", ValueAr = i % 3 == 0 ? "مصر" : "متعدد", SortOrder = 3 });
            if (i % 5 == 0)
            {
                product.Variants.Add(new ProductVariant { ProductId = product.Id, Sku = $"{sku}-BL", NameAr = "أزرق", NameEn = "Blue", OptionsJson = JsonSerializer.Serialize(new { color = "blue" }), StockQty = Math.Max(0, stock / 2) });
                product.Variants.Add(new ProductVariant { ProductId = product.Id, Sku = $"{sku}-BK", NameAr = "أسود", NameEn = "Black", OptionsJson = JsonSerializer.Serialize(new { color = "black" }), PriceAdjustment = 2m, StockQty = Math.Max(0, stock / 2) });
            }
            if (i % 10 == 0)
                product.Documents.Add(new ProductDocument { ProductId = product.Id, NameAr = "ورقة المواصفات الفنية", Path = $"asset://catalog/docs/{slug}.pdf" });
            db.Products.Add(product);
            added++;
        }
        if (added > 0)
        {
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Seeded {Count} catalog products", added);
        }
    }

    private static async Task<List<UnitOfMeasure>> SeedUnitsAsync(AppDbContext db, CancellationToken ct)
    {
        var list = await db.Units.ToListAsync(ct);
        foreach (var item in UnitMap.Where(x => list.All(u => u.Code != x.Code)))
        {
            var unit = new UnitOfMeasure { Code = item.Code, NameAr = item.Ar, NameEn = item.En };
            db.Units.Add(unit); list.Add(unit);
        }
        return list;
    }

    private static async Task<List<Brand>> SeedBrandsAsync(AppDbContext db, CancellationToken ct)
    {
        var list = await db.Brands.IgnoreQueryFilters().ToListAsync(ct);
        foreach (var item in BrandMap.Where(x => list.All(b => b.Slug != x.Slug)))
        {
            var brand = new Brand { NameAr = item.Ar, NameEn = item.En, Slug = item.Slug, LogoPath = $"asset://brands/{item.Slug}.webp" };
            db.Brands.Add(brand); list.Add(brand);
        }
        return list;
    }

    private static async Task<List<Category>> SeedCategoriesAsync(AppDbContext db, CancellationToken ct)
    {
        var list = await db.Categories.IgnoreQueryFilters().ToListAsync(ct);
        var order = 0;
        foreach (var main in CategoryMap)
        {
            var parent = list.FirstOrDefault(c => c.Slug == main.Slug);
            if (parent is null)
            {
                parent = new Category { NameAr = main.Ar, NameEn = main.En, Slug = main.Slug, IconName = main.Icon, ImagePath = $"asset://categories/{main.Slug}.webp", SortOrder = order++ };
                db.Categories.Add(parent); list.Add(parent);
            }
            var childOrder = 0;
            foreach (var child in main.Children)
            {
                var names = child.Split('|');
                var slug = $"{main.Slug}-{Slugify(names[1])}";
                if (list.Any(c => c.Slug == slug)) continue;
                var category = new Category { ParentId = parent.Id, NameAr = names[0], NameEn = names[1], Slug = slug, SortOrder = childOrder++ };
                db.Categories.Add(category); list.Add(category);
            }
        }
        return list;
    }

    private static string Slugify(string value) => value.ToLowerInvariant().Replace(" & ", "-").Replace(' ', '-');
}
