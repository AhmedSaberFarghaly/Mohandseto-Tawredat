using Microsoft.EntityFrameworkCore;
using Mohandseto.Api.Domain.Entities;

namespace Mohandseto.Api.Infrastructure;

public static class CustomizationSeeder
{
    public static async Task SeedAsync(AppDbContext db, ILogger logger, CancellationToken ct = default)
    {
        var products = await db.Products.OrderBy(p => p.Sku).Take(30).ToListAsync(ct);
        foreach (var product in products) product.IsPrintable = true;
        await db.SaveChangesAsync(ct);

        var existingProductIds = await db.CustomProductTemplates.IgnoreQueryFilters()
            .Select(t => t.ProductId).ToHashSetAsync(ct);
        var added = 0;
        foreach (var product in products.Where(p => !existingProductIds.Contains(p.Id)))
        {
            var template = new CustomProductTemplate
            {
                ProductId = product.Id,
                NameAr = $"تخصيص {product.NameAr}",
                DescriptionAr = "خصص اللون والخامة والمقاس وطريقة وموضع الطباعة، وارفع شعار شركتك أو اطلب تصميمًا احترافيًا.",
                SetupFee = 250,
                MinQuantity = 25,
                LeadTimeDays = 7 + added % 4,
            };
            template.Options.Add(new CustomizationOption { TemplateId = template.Id, Code = "front", NameAr = "الواجهة الأمامية", Type = "placement", SortOrder = 1 });
            template.Options.Add(new CustomizationOption { TemplateId = template.Id, Code = "back", NameAr = "الجهة الخلفية", Type = "placement", PriceAdjustment = 1.5m, SortOrder = 2 });
            template.Options.Add(new CustomizationOption { TemplateId = template.Id, Code = "wrap", NameAr = "طباعة محيطية", Type = "placement", PriceAdjustment = 3m, SortOrder = 3 });
            template.PrintMethods.Add(new PrintMethod { TemplateId = template.Id, Code = "screen", NameAr = "طباعة سلك سكرين", DescriptionAr = "اقتصادية للكميات الكبيرة والألوان المحدودة", UnitPriceAdjustment = 4m, MinQuantity = 50, SortOrder = 1 });
            template.PrintMethods.Add(new PrintMethod { TemplateId = template.Id, Code = "digital", NameAr = "طباعة رقمية", DescriptionAr = "ألوان دقيقة وتفاصيل عالية", UnitPriceAdjustment = 8m, MinQuantity = 25, SortOrder = 2 });
            template.PrintMethods.Add(new PrintMethod { TemplateId = template.Id, Code = "uv", NameAr = "طباعة UV", DescriptionAr = "ثبات وجودة عالية على الخامات الصلبة", UnitPriceAdjustment = 12m, MinQuantity = 25, SortOrder = 3 });
            template.Materials.Add(new CustomMaterial { TemplateId = template.Id, Code = "standard", NameAr = "خامة قياسية", SortOrder = 1 });
            template.Materials.Add(new CustomMaterial { TemplateId = template.Id, Code = "premium", NameAr = "خامة فاخرة", UnitPriceAdjustment = 9m, SortOrder = 2 });
            template.Colors.Add(new CustomColor { TemplateId = template.Id, Code = "navy", NameAr = "كحلي", Hex = "#0E2D6D", SortOrder = 1 });
            template.Colors.Add(new CustomColor { TemplateId = template.Id, Code = "white", NameAr = "أبيض", Hex = "#FFFFFF", SortOrder = 2 });
            template.Colors.Add(new CustomColor { TemplateId = template.Id, Code = "black", NameAr = "أسود", Hex = "#20252D", SortOrder = 3 });
            template.Colors.Add(new CustomColor { TemplateId = template.Id, Code = "orange", NameAr = "برتقالي", Hex = "#FF9D34", SortOrder = 4 });
            template.Sizes.Add(new CustomSize { TemplateId = template.Id, Code = "s", NameAr = "صغير", SortOrder = 1 });
            template.Sizes.Add(new CustomSize { TemplateId = template.Id, Code = "m", NameAr = "متوسط", UnitPriceAdjustment = 2m, SortOrder = 2 });
            template.Sizes.Add(new CustomSize { TemplateId = template.Id, Code = "l", NameAr = "كبير", UnitPriceAdjustment = 4m, SortOrder = 3 });
            db.CustomProductTemplates.Add(template);
            added++;
        }
        if (added > 0)
        {
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Seeded {Count} custom product templates", added);
        }
    }
}
