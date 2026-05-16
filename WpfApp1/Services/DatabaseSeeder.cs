using Dapper;
using Microsoft.Data.SqlClient;
using PharmacyApp.Models;

namespace PharmacyApp.Services;

public static class DatabaseSeeder
{
    public static void SeedIfEmpty()
    {
        using var conn = new SqlConnection(ConfigManager.ConnectionString);
        conn.Open();

        if (conn.ExecuteScalar<int>("SELECT COUNT(*) FROM SALE") > 0)
            return;

        var repo = new EntityRepository(ConfigManager.ConnectionString);
        EnsureDemoUsers(conn, repo);

        int cashierId = conn.ExecuteScalar<int>(
            "SELECT TOP 1 UserId FROM [USER] WHERE Login = 'cashier' OR Role = 'cashier'");
        if (cashierId == 0)
            cashierId = conn.ExecuteScalar<int>("SELECT TOP 1 UserId FROM [USER] ORDER BY UserId");

        if (conn.ExecuteScalar<int>("SELECT COUNT(*) FROM ITEM") == 0)
            SeedCatalog(conn, repo);

        SeedSales(conn, cashierId);
    }

    private static void EnsureDemoUsers(SqlConnection conn, EntityRepository repo)
    {
        SeedUser(conn, repo, "cashier", "cashier1", "Иванова", "Мария", "Сергеевна", "cashier");
        SeedUser(conn, repo, "manager", "manager1", "Петров", "Алексей", "Игоревич", "manager");
        SeedUser(conn, repo, "provizor", "provizor1", "Сидорова", "Елена", "Павловна", "provizor");
    }

    private static void SeedUser(SqlConnection conn, EntityRepository repo, string login, string password,
        string lastName, string firstName, string patronymic, string role)
    {
        if (conn.ExecuteScalar<int>("SELECT COUNT(*) FROM [USER] WHERE Login = @login", new { login }) > 0)
            return;

        string salt = PasswordHelper.GenerateSalt();
        var user = new AppUser
        {
            Login = login,
            PasswordHash = PasswordHelper.HashPassword(password, salt),
            Salt = salt,
            LastName = lastName,
            FirstName = firstName,
            Patronymic = patronymic,
            Role = role,
            IsActive = true,
            RegistrationDate = DateTime.Now
        };
        repo.Add(user);
    }

    private static void SeedCatalog(SqlConnection conn, EntityRepository repo)
    {
        var medicines = new Category("Лекарственные средства", "Основная группа");
        repo.Add(medicines);

        var antibiotics = new Category("Антибиотики", null, medicines.Id);
        var vitamins = new Category("Витамины", null, medicines.Id);
        var otc = new Category("Безрецептурные", null, medicines.Id);
        repo.Add(antibiotics);
        repo.Add(vitamins);
        repo.Add(otc);

        var supplier = new Supplier("ООО ФармПоставка") { Inn = "7701234567", Phone = "+7-495-100-20-30" };
        repo.Add(supplier);

        var items = new[]
        {
            CreateItem("Амоксициллин 500 мг", "таб.", 20, antibiotics.Id, "4601234567890", 10, true),
            CreateItem("Азитромицин 250 мг", "таб.", 6, antibiotics.Id, "4601234567891", 8, true),
            CreateItem("Нурофен 200 мг", "таб.", 20, otc.Id, "4601234567892", 15, false),
            CreateItem("Парацетамол 500 мг", "таб.", 10, otc.Id, "4601234567893", 20, false),
            CreateItem("Компливит", "таб.", 60, vitamins.Id, "4601234567894", 12, false),
            CreateItem("Витрум", "таб.", 30, vitamins.Id, "4601234567895", 10, false),
            CreateItem("Смекта", "пак.", 10, otc.Id, "4601234567896", 14, false),
            CreateItem("Мезим форте", "таб.", 20, otc.Id, "4601234567897", 16, false)
        };

        // Item model doesn't have PurchasePrice on item - only on batch
        decimal[][] prices =
        [
            [120m, 189m], [250m, 390m], [180m, 279m], [45m, 79m],
            [210m, 329m], [320m, 499m], [95m, 149m], [140m, 219m]
        ];

        for (int i = 0; i < items.Length; i++)
        {
            var item = items[i];
            repo.Add(item);

            var batch = new Batch($"B2026-{i + 1:D3}", prices[i][0], prices[i][1], 50 + i * 5, item.Id, supplier.Id)
            {
                ProductionDate = DateTime.Today.AddMonths(-2),
                ExpiryDate = DateTime.Today.AddMonths(18),
                StorageLocation = i % 2 == 0 ? "Стеллаж А" : "Стеллаж Б"
            };
            repo.Add(batch);
        }
    }

    private static Item CreateItem(string name, string unit, int pack, int categoryId, string barcode, int minStock, bool rx)
    {
        var item = new Item(name, unit, pack, categoryId)
        {
            Barcode = barcode,
            MinStock = minStock,
            PrescriptionRequired = rx
        };
        return item;
    }

    private static void SeedSales(SqlConnection conn, int cashierId)
    {
        var itemPrices = conn.Query<(int Id, decimal Price)>(@"
            SELECT i.Id, MIN(b.RetailPrice) AS Price
            FROM ITEM i
            INNER JOIN BATCH b ON b.ItemId = i.Id AND b.Quantity > 0
            GROUP BY i.Id").ToList();

        if (itemPrices.Count == 0)
            return;

        var random = new Random(42);
        decimal[] dailyTotals = [820m, 1240m, 1560m, 1890m, 2100m, 2450m, 2780m, 3120m];

        for (int dayOffset = 7; dayOffset >= 0; dayOffset--)
        {
            var day = DateTime.Today.AddDays(-dayOffset);
            int checksPerDay = dayOffset == 1 ? 12 : 5;
            decimal targetDayRevenue = dailyTotals[7 - dayOffset];
            decimal checkAmount = Math.Round(targetDayRevenue / checksPerDay, 2);

            for (int c = 0; c < checksPerDay; c++)
            {
                var pick = itemPrices[random.Next(itemPrices.Count)];
                int qty = 1;
                decimal total = c == checksPerDay - 1
                    ? targetDayRevenue - checkAmount * (checksPerDay - 1)
                    : checkAmount;
                total = Math.Round(total, 2);

                string payment = random.Next(2) == 0 ? "cash" : "card";
                var saleTime = day.AddHours(9 + random.Next(9)).AddMinutes(random.Next(60));

                int saleId = conn.QuerySingle<int>(@"
                    INSERT INTO SALE (Date, TotalAmount, PaymentType, CashierId)
                    VALUES (@date, @total, @payment, @cashierId);
                    SELECT CAST(SCOPE_IDENTITY() AS INT)",
                    new { date = saleTime, total, payment, cashierId });

                conn.Execute(@"
                    INSERT INTO SALE_ITEM (Quantity, PriceAtSale, SaleId, UniqueItemId, ItemId)
                    VALUES (@qty, @price, @saleId, NULL, @itemId)",
                    new { qty, price = total, saleId, itemId = pick.Id });
            }
        }
    }
}
