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

        var repo = new EntityRepository(ConfigManager.ConnectionString);
        var locationService = new StorageLocationService();

        EnsureDemoUsers(conn, repo);
        EnsureStorageLocations(locationService);

        bool hasItems = conn.ExecuteScalar<int>("SELECT COUNT(*) FROM ITEM") > 0;
        bool hasSales = conn.ExecuteScalar<int>("SELECT COUNT(*) FROM SALE") > 0;

        if (!hasItems)
            SeedCatalog(conn, repo, locationService);

        EnsureExtraTestData(conn, repo, locationService);

        if (!hasSales)
        {
            int cashierId = conn.ExecuteScalar<int>(
                "SELECT TOP 1 UserId FROM [USER] WHERE Login = 'cashier' OR Role = 'cashier'");
            if (cashierId == 0)
                cashierId = conn.ExecuteScalar<int>("SELECT TOP 1 UserId FROM [USER] ORDER BY UserId");
            SeedSales(conn, cashierId);
        }
    }

    private static void EnsureStorageLocations(StorageLocationService locationService)
    {
        string[][] locations =
        [
            ["Стеллаж А", null],
            ["Стеллаж А", "полка 1"],
            ["Стеллаж А", "полка 2"],
            ["Стеллаж А", "полка 3"],
            ["Стеллаж Б", "полка 1"],
            ["Стеллаж Б", "полка 2"],
            ["Холодильник №1", "полка 1"],
            ["Холодильник №1", "полка 2"],
            ["Касса", "витрина"],
            ["Склад", "зона возврата"]
        ];

        foreach (var loc in locations)
            locationService.GetOrCreate(loc[0], loc[1]);
    }

    private static void EnsureDemoUsers(SqlConnection conn, EntityRepository repo)
    {
        SeedUser(conn, repo, "admin", "admin123", "Администратор", "Системный", null, "admin");
        SeedUser(conn, repo, "cashier", "cashier1", "Иванова", "Мария", "Сергеевна", "cashier");
        SeedUser(conn, repo, "cashier2", "cashier2", "Смирнов", "Дмитрий", "Андреевич", "cashier");
        SeedUser(conn, repo, "manager", "manager1", "Петров", "Алексей", "Игоревич", "manager");
        SeedUser(conn, repo, "provizor", "provizor1", "Сидорова", "Елена", "Павловна", "provizor");
    }

    private static void SeedUser(SqlConnection conn, EntityRepository repo, string login, string password,
        string lastName, string firstName, string? patronymic, string role)
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

    private static void SeedCatalog(SqlConnection conn, EntityRepository repo, StorageLocationService locationService)
    {
        var medicines = new Category("Лекарственные средства", "Основная группа");
        repo.Add(medicines);

        var antibiotics = new Category("Антибиотики", null, medicines.Id);
        var vitamins = new Category("Витамины", null, medicines.Id);
        var otc = new Category("Безрецептурные", null, medicines.Id);
        var devices = new Category("Медицинские изделия", "Перевязочные материалы");
        repo.Add(antibiotics);
        repo.Add(vitamins);
        repo.Add(otc);
        repo.Add(devices);

        var supplier1 = new Supplier("ООО ФармПоставка") { Inn = "7701234567", Phone = "+7-495-100-20-30" };
        var supplier2 = new Supplier("МедТехСервис") { Inn = "771234567890", Phone = "+7-812-234-56-78" };
        repo.Add(supplier1);
        repo.Add(supplier2);

        int locA1 = locationService.GetOrCreate("Стеллаж А", "полка 1");
        int locA2 = locationService.GetOrCreate("Стеллаж А", "полка 2");
        int locB1 = locationService.GetOrCreate("Стеллаж Б", "полка 1");
        int locCold = locationService.GetOrCreate("Холодильник №1", "полка 1");
        int locVitrina = locationService.GetOrCreate("Касса", "витрина");

        var catalog = new (string name, string unit, int pack, int catId, string barcode, int minStock, bool rx,
            decimal purchase, decimal retail, int qty, int supplierId, int locationId, int monthsToExpiry)[]
        {
            ("Амоксициллин 500 мг", "таб.", 20, antibiotics.Id, "4601234567890", 10, true, 120m, 189m, 45, supplier1.Id, locCold, 18),
            ("Азитромицин 250 мг", "таб.", 6, antibiotics.Id, "4601234567891", 8, true, 250m, 390m, 30, supplier1.Id, locCold, 18),
            ("Цефтриаксон 1 г", "фл.", 1, antibiotics.Id, "4601234567898", 5, true, 180m, 299m, 12, supplier2.Id, locCold, 12),
            ("Нурофен 200 мг", "таб.", 20, otc.Id, "4601234567892", 15, false, 180m, 279m, 80, supplier1.Id, locA1, 24),
            ("Парацетамол 500 мг", "таб.", 10, otc.Id, "4601234567893", 20, false, 45m, 79m, 120, supplier1.Id, locA1, 24),
            ("Ибупрофен 400 мг", "таб.", 10, otc.Id, "4601234567899", 18, false, 95m, 159m, 60, supplier1.Id, locA2, 24),
            ("Компливит", "таб.", 60, vitamins.Id, "4601234567894", 12, false, 210m, 329m, 40, supplier1.Id, locA2, 18),
            ("Витрум", "таб.", 30, vitamins.Id, "4601234567895", 10, false, 320m, 499m, 35, supplier1.Id, locB1, 18),
            ("Смекта", "пак.", 10, otc.Id, "4601234567896", 14, false, 95m, 149m, 55, supplier2.Id, locB1, 24),
            ("Мезим форте", "таб.", 20, otc.Id, "4601234567897", 16, false, 140m, 219m, 50, supplier2.Id, locA1, 24),
            ("Бинт стерильный 5м", "рулон", 1, devices.Id, "4601234567900", 30, false, 25m, 49m, 200, supplier2.Id, locB1, 36),
            ("Вата стерильная 50г", "уп.", 1, devices.Id, "4601234567901", 25, false, 35m, 65m, 90, supplier2.Id, locB1, 36),
            ("Аспирин Кардио", "таб.", 28, otc.Id, "4601234567902", 12, false, 110m, 175m, 8, supplier1.Id, locVitrina, 24),
            ("Ношпа 40 мг", "таб.", 24, otc.Id, "4601234567903", 10, false, 130m, 210m, 6, supplier1.Id, locVitrina, 24),
        };

        int batchIndex = 1;
        foreach (var row in catalog)
        {
            var item = CreateItem(row.name, row.unit, row.pack, row.catId, row.barcode, row.minStock, row.rx);
            repo.Add(item);

            var batch = new Batch($"B2026-{batchIndex:D3}", row.purchase, row.retail, row.qty, item.Id, row.supplierId)
            {
                ProductionDate = DateTime.Today.AddMonths(-3),
                ExpiryDate = DateTime.Today.AddMonths(row.monthsToExpiry),
                StorageLocationId = row.locationId
            };
            repo.Add(batch);
            batchIndex++;
        }
    }

    /// <summary>
    /// Дополнительные данные для демо: просрочка, дефицит, вторая партия.
    /// </summary>
    private static void EnsureExtraTestData(SqlConnection conn, EntityRepository repo, StorageLocationService locationService)
    {
        if (conn.ExecuteScalar<int>("SELECT COUNT(*) FROM BATCH WHERE BatchNumber = 'EXP-DEMO-001'") > 0)
            return;

        int itemId = conn.ExecuteScalar<int>("SELECT TOP 1 Id FROM ITEM ORDER BY Id");
        int supplierId = conn.ExecuteScalar<int>("SELECT TOP 1 Id FROM SUPPLIER ORDER BY Id");
        if (itemId == 0 || supplierId == 0)
            return;

        int locReturn = locationService.GetOrCreate("Склад", "зона возврата");
        int locCold = locationService.GetOrCreate("Холодильник №1", "полка 2");

        // Просроченные партии (для вкладки «Списание»)
        repo.Add(new Batch("EXP-DEMO-001", 80m, 140m, 15, itemId, supplierId)
        {
            ProductionDate = DateTime.Today.AddYears(-2),
            ExpiryDate = DateTime.Today.AddMonths(-2),
            StorageLocationId = locReturn
        });
        repo.Add(new Batch("EXP-DEMO-002", 45m, 79m, 22, itemId, supplierId)
        {
            ProductionDate = DateTime.Today.AddYears(-1),
            ExpiryDate = DateTime.Today.AddDays(-10),
            StorageLocationId = locReturn
        });

        // Дефицит: остаток ниже MinStock
        var deficitItem = conn.QueryFirstOrDefault<Item>(@"
            SELECT TOP 1 * FROM ITEM WHERE Name LIKE N'%Парацетамол%'");
        if (deficitItem != null)
        {
            repo.Add(new Batch("LOW-DEMO-001", 45m, 79m, 3, deficitItem.Id, supplierId)
            {
                ProductionDate = DateTime.Today.AddMonths(-1),
                ExpiryDate = DateTime.Today.AddMonths(12),
                StorageLocationId = locCold
            });
        }

        // Вторая партия того же товара (другая серия / место)
        var amox = conn.QueryFirstOrDefault<Item>(@"
            SELECT TOP 1 * FROM ITEM WHERE Name LIKE N'%Амоксициллин%'");
        if (amox != null)
        {
            int locA3 = locationService.GetOrCreate("Стеллаж А", "полка 3");
            repo.Add(new Batch("B2026-AMOX-2", 115m, 185m, 25, amox.Id, supplierId)
            {
                ProductionDate = DateTime.Today.AddMonths(-1),
                ExpiryDate = DateTime.Today.AddMonths(20),
                StorageLocationId = locA3
            });
        }
    }

    private static Item CreateItem(string name, string unit, int pack, int categoryId, string barcode, int minStock, bool rx)
    {
        return new Item(name, unit, pack, categoryId)
        {
            Barcode = barcode,
            MinStock = minStock,
            PrescriptionRequired = rx
        };
    }

    private static void SeedSales(SqlConnection conn, int cashierId)
    {
        var itemPrices = conn.Query<(int Id, decimal Price)>(@"
            SELECT i.Id, MIN(b.RetailPrice) AS Price
            FROM ITEM i
            INNER JOIN BATCH b ON b.ItemId = i.Id AND b.Quantity > 0
                AND (b.ExpiryDate IS NULL OR b.ExpiryDate >= GETDATE())
            GROUP BY i.Id").ToList();

        if (itemPrices.Count == 0)
            return;

        var random = new Random(42);
        decimal[] dailyTotals = [820m, 1240m, 1560m, 1890m, 2100m, 2450m, 2780m, 3120m, 2650m, 1980m];

        for (int dayOffset = 9; dayOffset >= 0; dayOffset--)
        {
            var day = DateTime.Today.AddDays(-dayOffset);
            int checksPerDay = dayOffset is 1 or 0 ? 14 : 6;
            decimal targetDayRevenue = dailyTotals[9 - dayOffset];
            decimal checkAmount = Math.Round(targetDayRevenue / checksPerDay, 2);

            for (int c = 0; c < checksPerDay; c++)
            {
                var pick = itemPrices[random.Next(itemPrices.Count)];
                decimal total = c == checksPerDay - 1
                    ? targetDayRevenue - checkAmount * (checksPerDay - 1)
                    : checkAmount;
                total = Math.Round(total, 2);

                string payment = random.Next(3) == 0 ? "cash" : "card";
                var saleTime = day.AddHours(9 + random.Next(9)).AddMinutes(random.Next(60));

                int saleId = conn.QuerySingle<int>(@"
                    INSERT INTO SALE (Date, TotalAmount, PaymentType, CashierId)
                    VALUES (@date, @total, @payment, @cashierId);
                    SELECT CAST(SCOPE_IDENTITY() AS INT)",
                    new { date = saleTime, total, payment, cashierId });

                conn.Execute(@"
                    INSERT INTO SALE_ITEM (Quantity, PriceAtSale, SaleId, UniqueItemId, ItemId)
                    VALUES (@qty, @price, @saleId, NULL, @itemId)",
                    new { qty = 1, price = total, saleId, itemId = pick.Id });
            }
        }
    }
}
