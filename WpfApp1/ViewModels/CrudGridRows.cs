using PharmacyApp.Models;

namespace PharmacyApp.ViewModels;

public class CategoryGridRow
{
    public required Category Entity { get; init; }
    public int Id => Entity.Id;
    public string Name => Entity.Name;
    public string? Description => Entity.Description;
    public string ParentName { get; init; } = "—";
}

public class ItemGridRow
{
    public required Item Entity { get; init; }
    public int Id => Entity.Id;
    public string Name => Entity.Name;
    public string? Barcode => Entity.Barcode;
    public string CategoryName { get; init; } = "—";
}

public class BatchGridRow
{
    public required Batch Entity { get; init; }
    public int Id => Entity.Id;
    public string BatchNumber => Entity.BatchNumber;
    public string ItemName { get; init; } = "—";
    public string SupplierName { get; init; } = "—";
    public int Quantity => Entity.Quantity;
    public decimal RetailPrice => Entity.RetailPrice;
}

public static class CrudGridRowsFactory
{
    public static List<CategoryGridRow> CreateCategoryRows(IEnumerable<Category> categories)
    {
        var byId = categories.ToDictionary(c => c.Id);
        return categories.Select(c => new CategoryGridRow
        {
            Entity = c,
            ParentName = c.ParentId is int parentId && byId.TryGetValue(parentId, out var parent)
                ? parent.Name
                : "—"
        }).ToList();
    }

    public static List<ItemGridRow> CreateItemRows(IEnumerable<Item> items, IEnumerable<Category> categories)
    {
        var byId = categories.ToDictionary(c => c.Id);
        return items.Select(i => new ItemGridRow
        {
            Entity = i,
            CategoryName = byId.TryGetValue(i.CategoryId, out var cat) ? cat.Name : "—"
        }).ToList();
    }

    public static List<BatchGridRow> CreateBatchRows(
        IEnumerable<Batch> batches,
        IEnumerable<Item> items,
        IEnumerable<Supplier> suppliers)
    {
        var itemsById = items.ToDictionary(i => i.Id);
        var suppliersById = suppliers.ToDictionary(s => s.Id);
        return batches.Select(b => new BatchGridRow
        {
            Entity = b,
            ItemName = itemsById.TryGetValue(b.ItemId, out var item) ? item.Name : "—",
            SupplierName = suppliersById.TryGetValue(b.SupplierId, out var sup) ? sup.Name : "—"
        }).ToList();
    }
}
