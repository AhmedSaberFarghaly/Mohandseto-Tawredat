using Microsoft.EntityFrameworkCore;
using Mohandseto.Api.Domain.Entities;

namespace Mohandseto.Api.Infrastructure;

public static class InventorySeeder
{
    public static async Task SeedAsync(AppDbContext db, ILogger logger)
    {
        if (await db.Warehouses.AnyAsync()) return;
        var main = new Warehouse { Code = "WH-OBR", NameAr = "مخزن العبور الرئيسي", NameEn = "Obour Main Warehouse", Governorate = "القاهرة", Address = "المنطقة الصناعية، مدينة العبور" };
        var west = new Warehouse { Code = "WH-OCT", NameAr = "مخزن أكتوبر", NameEn = "October Warehouse", Governorate = "الجيزة", Address = "المنطقة الصناعية، السادس من أكتوبر" };
        db.Warehouses.AddRange(main, west);
        var products = await db.Products.OrderBy(x => x.Sku).ToListAsync();
        foreach (var (product, index) in products.Select((value, index) => (value, index)))
        {
            var total = Math.Max(0, product.StockQty); var westQty = index < 80 ? total / 3 : 0; var mainQty = total - westQty;
            if (mainQty > 0) Add(main, product, mainQty, index, "A");
            if (westQty > 0) Add(west, product, westQty, index, "B");
        }
        await db.SaveChangesAsync(); logger.LogInformation("Seeded inventory warehouses and {Count} warehouse stock rows", db.ChangeTracker.Entries<WarehouseStock>().Count());

        void Add(Warehouse warehouse, Product product, int quantity, int index, string zone)
        {
            var stock = new WarehouseStock { Warehouse = warehouse, WarehouseId = warehouse.Id, Product = product, ProductId = product.Id, OnHandQty = quantity, ReorderLevel = Math.Max(5, product.LowStockThreshold), ShelfLocation = $"{zone}-{index / 25 + 1:00}-{index % 25 + 1:00}", Barcode = $"{warehouse.Code.Replace("-", "")}-{product.Sku}" };
            db.WarehouseStocks.Add(stock);
            db.InventoryMovements.Add(new InventoryMovement { Number = $"SEED-{warehouse.Code}-{product.Sku}", WarehouseId = warehouse.Id, ProductId = product.Id, Type = InventoryMovementType.Add, Quantity = quantity, BalanceAfter = quantity, UnitCost = product.CostPrice > 0 ? product.CostPrice : product.BasePrice, Reason = "رصيد افتتاحي", ReferenceType = "seed", ActorUserId = Guid.Empty });
        }
    }
}
