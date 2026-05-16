using Dapper;
using Microsoft.Data.SqlClient;
using PharmacyApp.Interfaces;
using PharmacyApp.Models;

namespace PharmacyApp.Services;

public class PriceService : IPriceService
{
    private readonly string _connectionString;
    private readonly EntityRepository _repo;

    public PriceService()
    {
        _connectionString = ConfigManager.ConnectionString;
        _repo = new EntityRepository(_connectionString);
    }

    public List<Category> GetAllCategories()
    {
        var all = _repo.GetAll<Category>();
        var roots = all.Where(c => c.ParentId == null).ToList();
        foreach (var root in roots)
            AttachChildren(root, all);
        return roots;
    }

    private static void AttachChildren(Category parent, List<Category> all)
    {
        parent.ChildCategories = all.Where(c => c.ParentId == parent.Id).ToList();
        foreach (var child in parent.ChildCategories)
            AttachChildren(child, all);
    }

    public void ApplyMarkupToCategory(int categoryId, decimal markupPercent, bool respectVitalLimits)
    {
        var categoryIds = GetCategoryAndDescendantIds(categoryId);
        using var conn = new SqlConnection(_connectionString);
        foreach (int catId in categoryIds)
        {
            var items = conn.Query<Item>("SELECT * FROM ITEM WHERE CategoryId = @catId", new { catId }).ToList();
            foreach (var item in items)
            {
                var batches = conn.Query<Batch>(
                    "SELECT * FROM BATCH WHERE ItemId = @itemId AND Quantity > 0",
                    new { itemId = item.Id }).ToList();

                foreach (var batch in batches)
                {
                    decimal newRetail = batch.PurchasePrice * (1 + markupPercent / 100m);
                    if (respectVitalLimits && item.IsVital)
                        newRetail = Math.Min(newRetail, batch.PurchasePrice * 1.2m);

                    if (newRetail < batch.PurchasePrice)
                        newRetail = batch.PurchasePrice;

                    conn.Execute(
                        "UPDATE BATCH SET RetailPrice = @price WHERE Id = @id",
                        new { price = newRetail, id = batch.Id });
                }
            }
        }
    }

    public List<Item> GetAllItems() => _repo.GetAll<Item>();

    public List<Item> GetItemsByCategory(int? categoryId)
    {
        if (!categoryId.HasValue)
            return GetAllItems();

        var ids = GetCategoryAndDescendantIds(categoryId.Value);
        return GetAllItems().Where(i => ids.Contains(i.CategoryId)).ToList();
    }

    private List<int> GetCategoryAndDescendantIds(int categoryId)
    {
        var all = _repo.GetAll<Category>();
        var result = new List<int> { categoryId };
        void Collect(int parentId)
        {
            foreach (var child in all.Where(c => c.ParentId == parentId))
            {
                result.Add(child.Id);
                Collect(child.Id);
            }
        }
        Collect(categoryId);
        return result;
    }
}
