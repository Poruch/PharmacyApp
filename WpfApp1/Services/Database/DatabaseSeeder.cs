using Dapper;
using Microsoft.Data.SqlClient;
using PharmacyApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PharmacyApp.Services;

public static class DatabaseSeeder
{
    // Вспомогательные DTO для Dapper
    private class BatchInfo
    {
        public int BatchId { get; set; }
        public int ItemId { get; set; }
        public string BatchNumber { get; set; }
    }
    private class ItemPrice
    {
        public int Id { get; set; }
        public decimal Price { get; set; }
    }

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
        EnsureMarkedItems(conn, repo);

        if (!hasSales)
        {
            int cashierId = conn.ExecuteScalar<int>(
                "SELECT TOP 1 UserId FROM [USER] WHERE Login = 'cashier' OR Role = 'cashier'");
            if (cashierId == 0)
                cashierId = conn.ExecuteScalar<int>("SELECT TOP 1 UserId FROM [USER] ORDER BY UserId");
            SeedSales(conn, cashierId);
            SeedReturn(conn, cashierId);
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
            ["Стеллаж А", "полка 4"],
            ["Стеллаж Б", "полка 1"],
            ["Стеллаж Б", "полка 2"],
            ["Стеллаж В", "стеллаж"],
            ["Холодильник №1", "полка 1"],
            ["Холодильник №1", "полка 2"],
            ["Холодильник №1", "полка 3"],
            ["Холодильник №2", "полка 1"],
            ["Касса", "витрина"],
            ["Склад", "зона возврата"],
            ["Склад", "карантин"]
        ];

        foreach (var loc in locations)
            locationService.GetOrCreate(loc[0], loc[1]);
    }

    private static void EnsureDemoUsers(SqlConnection conn, EntityRepository repo)
    {
        SeedUser(conn, repo, "admin", "admin123", "Администраторов", "Админ", null, "admin");
        SeedUser(conn, repo, "cashier", "cashier1", "Иванова", "Мария", "Сергеевна", "cashier");
        SeedUser(conn, repo, "cashier2", "cashier2", "Смирнов", "Дмитрий", "Андреевич", "cashier");
        SeedUser(conn, repo, "manager", "manager1", "Петров", "Алексей", "Игоревич", "manager");
        SeedUser(conn, repo, "provizor", "provizor1", "Сидорова", "Елена", "Павловна", "provizor");
        SeedUser(conn, repo, "provizor2", "provizor2", "Кузнецов", "Игорь", "Владимирович", "provizor");
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
        // Категории
        var medicines = new Category("Лекарственные средства", "Основная группа");
        repo.Add(medicines);

        var antibiotics = new Category("Антибиотики", null, medicines.Id);
        var vitamins = new Category("Витамины", null, medicines.Id);
        var otc = new Category("Безрецептурные", null, medicines.Id);
        var cardiovascular = new Category("Сердечно-сосудистые", null, medicines.Id);
        var gastro = new Category("ЖКТ", null, medicines.Id);
        var dermatology = new Category("Дерматология", null, medicines.Id);
        var medicalDevices = new Category("Медицинские изделия", "Перевязочные материалы, инструменты");
        var cosmetics = new Category("Косметика и гигиена", null);
        var supplements = new Category("БАДы", null);

        repo.Add(antibiotics);
        repo.Add(vitamins);
        repo.Add(otc);
        repo.Add(cardiovascular);
        repo.Add(gastro);
        repo.Add(dermatology);
        repo.Add(medicalDevices);
        repo.Add(cosmetics);
        repo.Add(supplements);

        // Поставщики
        var supplier1 = new Supplier("ООО ФармПоставка") { Inn = "7701234567", Phone = "+7-495-100-20-30", Email = "order@farmpostavka.ru" };
        var supplier2 = new Supplier("МедТехСервис") { Inn = "771234567890", Phone = "+7-812-234-56-78", Email = "sales@medtech.ru" };
        var supplier3 = new Supplier("Здоровье Плюс") { Inn = "772345678901", Phone = "+7-343-345-67-89", Email = "zakaz@zdorovie.ru" };
        repo.Add(supplier1);
        repo.Add(supplier2);
        repo.Add(supplier3);

        // Места хранения
        int locA1 = locationService.GetOrCreate("Стеллаж А", "полка 1");
        int locA2 = locationService.GetOrCreate("Стеллаж А", "полка 2");
        int locA3 = locationService.GetOrCreate("Стеллаж А", "полка 3");
        int locA4 = locationService.GetOrCreate("Стеллаж А", "полка 4");
        int locB1 = locationService.GetOrCreate("Стеллаж Б", "полка 1");
        int locB2 = locationService.GetOrCreate("Стеллаж Б", "полка 2");
        int locCold1 = locationService.GetOrCreate("Холодильник №1", "полка 1");
        int locCold2 = locationService.GetOrCreate("Холодильник №1", "полка 2");
        int locCold3 = locationService.GetOrCreate("Холодильник №2", "полка 1");
        int locVitrina = locationService.GetOrCreate("Касса", "витрина");
        int locQuarantine = locationService.GetOrCreate("Склад", "карантин");

        var catalog = new List<(string name, string unit, int pack, int catId, string barcode, int minStock, bool rx,
            decimal purchase, decimal retail, int qty, int supplierId, int locationId, int monthsToExpiry, bool isMarked)>();

        // Лекарства (антибиотики)
        catalog.Add(("Амоксициллин 500 мг", "таб.", 20, antibiotics.Id, "4601234567890", 10, true, 120m, 189m, 45, supplier1.Id, locCold1, 18, true));
        catalog.Add(("Азитромицин 250 мг", "таб.", 6, antibiotics.Id, "4601234567891", 8, true, 250m, 390m, 30, supplier1.Id, locCold1, 18, true));
        catalog.Add(("Цефтриаксон 1 г", "фл.", 1, antibiotics.Id, "4601234567898", 5, true, 180m, 299m, 12, supplier2.Id, locCold2, 12, true));
        catalog.Add(("Левофлоксацин 500 мг", "таб.", 10, antibiotics.Id, "4601234567905", 6, true, 210m, 340m, 20, supplier3.Id, locCold2, 20, true));

        // Безрецептурные
        catalog.Add(("Нурофен 200 мг", "таб.", 20, otc.Id, "4601234567892", 15, false, 180m, 279m, 80, supplier1.Id, locA1, 24, false));
        catalog.Add(("Парацетамол 500 мг", "таб.", 10, otc.Id, "4601234567893", 20, false, 45m, 79m, 120, supplier1.Id, locA1, 24, false));
        catalog.Add(("Ибупрофен 400 мг", "таб.", 10, otc.Id, "4601234567899", 18, false, 95m, 159m, 60, supplier1.Id, locA2, 24, false));
        catalog.Add(("Смекта", "пак.", 10, otc.Id, "4601234567896", 14, false, 95m, 149m, 55, supplier2.Id, locB1, 24, false));
        catalog.Add(("Мезим форте", "таб.", 20, otc.Id, "4601234567897", 16, false, 140m, 219m, 50, supplier2.Id, locA1, 24, false));
        catalog.Add(("Но-шпа 40 мг", "таб.", 24, otc.Id, "4601234567903", 10, false, 130m, 210m, 6, supplier1.Id, locVitrina, 24, false));
        catalog.Add(("Лоперамид 2 мг", "капс.", 10, gastro.Id, "4601234567910", 12, false, 30m, 55m, 40, supplier3.Id, locA2, 18, false));
        catalog.Add(("Омепразол 20 мг", "капс.", 30, gastro.Id, "4601234567911", 10, false, 80m, 135m, 35, supplier2.Id, locA2, 18, false));
        catalog.Add(("Супрастин 25 мг", "таб.", 20, otc.Id, "4601234567912", 8, false, 110m, 185m, 25, supplier3.Id, locA3, 18, false));

        // Сердечно-сосудистые
        catalog.Add(("Аспирин Кардио", "таб.", 28, cardiovascular.Id, "4601234567902", 12, false, 110m, 175m, 8, supplier1.Id, locVitrina, 24, false));
        catalog.Add(("Эналаприл 10 мг", "таб.", 20, cardiovascular.Id, "4601234567913", 10, true, 70m, 125m, 30, supplier2.Id, locA3, 18, false));
        catalog.Add(("Аторвастатин 20 мг", "таб.", 30, cardiovascular.Id, "4601234567914", 8, true, 190m, 290m, 25, supplier1.Id, locA3, 20, false));

        // Витамины и БАДы
        catalog.Add(("Компливит", "таб.", 60, vitamins.Id, "4601234567894", 12, false, 210m, 329m, 40, supplier1.Id, locA2, 18, false));
        catalog.Add(("Витрум", "таб.", 30, vitamins.Id, "4601234567895", 10, false, 320m, 499m, 35, supplier1.Id, locB1, 18, false));
        catalog.Add(("Омега-3", "капс.", 60, supplements.Id, "4601234567915", 8, false, 450m, 690m, 20, supplier3.Id, locA4, 24, false));
        catalog.Add(("Магний В6", "таб.", 50, supplements.Id, "4601234567916", 12, false, 260m, 399m, 30, supplier2.Id, locA4, 18, false));

        // Дерматология
        catalog.Add(("Левомеколь", "мазь 40г", 1, dermatology.Id, "4601234567917", 15, false, 55m, 89m, 50, supplier3.Id, locCold3, 18, false));
        catalog.Add(("Акридерм", "крем 15г", 1, dermatology.Id, "4601234567918", 10, true, 180m, 285m, 25, supplier2.Id, locCold3, 20, false));

        // Медицинские изделия
        catalog.Add(("Бинт стерильный 5м", "рулон", 1, medicalDevices.Id, "4601234567900", 30, false, 25m, 49m, 200, supplier2.Id, locB1, 36, false));
        catalog.Add(("Бинт эластичный", "рулон", 1, medicalDevices.Id, "4601234567919", 20, false, 60m, 99m, 80, supplier2.Id, locB1, 36, false));
        catalog.Add(("Лейкопластырь", "рулон", 1, medicalDevices.Id, "4601234567920", 40, false, 30m, 55m, 150, supplier3.Id, locB2, 36, false));
        catalog.Add(("Перчатки мед. S", "пара", 1, medicalDevices.Id, "4601234567921", 25, false, 15m, 29m, 300, supplier1.Id, locB2, 36, false));

        // Косметика и гигиена
        catalog.Add(("Детский крем", "туба 50мл", 1, cosmetics.Id, "4601234567922", 20, false, 35m, 65m, 70, supplier3.Id, locB2, 18, false));
        catalog.Add(("Шампунь детский", "фл. 200мл", 1, cosmetics.Id, "4601234567923", 15, false, 80m, 139m, 45, supplier2.Id, locB1, 18, false));

        // Вторые партии
        catalog.Add(("Амоксициллин 500 мг", "таб.", 20, antibiotics.Id, "4601234567890", 10, true, 115m, 185m, 30, supplier1.Id, locCold1, 24, true));
        catalog.Add(("Нурофен 200 мг", "таб.", 20, otc.Id, "4601234567892", 15, false, 170m, 269m, 40, supplier1.Id, locA1, 24, false));
        catalog.Add(("Парацетамол 500 мг", "таб.", 10, otc.Id, "4601234567893", 20, false, 42m, 75m, 60, supplier1.Id, locA2, 20, false));

        int batchId = 1;
        foreach (var row in catalog)
        {
            var item = CreateItem(row.name, row.unit, row.pack, row.catId, row.barcode, row.minStock, row.rx);
            repo.Add(item);

            var batch = new Batch($"B2026-{batchId:D4}", row.purchase, row.retail, row.qty, item.Id, row.supplierId)
            {
                ProductionDate = DateTime.Today.AddMonths(-3),
                ExpiryDate = DateTime.Today.AddMonths(row.monthsToExpiry),
                StorageLocationId = row.locationId
            };
            repo.Add(batch);
            batchId++;
        }
    }

    private static void EnsureExtraTestData(SqlConnection conn, EntityRepository repo, StorageLocationService locationService)
    {
        if (conn.ExecuteScalar<int>("SELECT COUNT(*) FROM BATCH WHERE BatchNumber = 'EXP-DEMO-001'") == 0)
        {
            int itemId = conn.ExecuteScalar<int>("SELECT TOP 1 Id FROM ITEM ORDER BY Id");
            int supplierId = conn.ExecuteScalar<int>("SELECT TOP 1 Id FROM SUPPLIER ORDER BY Id");
            if (itemId != 0 && supplierId != 0)
            {
                int locReturn = locationService.GetOrCreate("Склад", "зона возврата");
                int locQuarantine = locationService.GetOrCreate("Склад", "карантин");

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
                    StorageLocationId = locQuarantine
                });
            }
        }

        var deficitItem = conn.QueryFirstOrDefault<dynamic>(@"
            SELECT TOP 1 i.Id 
            FROM ITEM i
            WHERE i.Name LIKE N'%Парацетамол%' 
              AND i.Id NOT IN (SELECT ItemId FROM BATCH WHERE Quantity > MinStock)");
        if (deficitItem != null)
        {
            int deficitId = deficitItem.Id;
            int supplierId = conn.ExecuteScalar<int>("SELECT TOP 1 Id FROM SUPPLIER ORDER BY Id");
            int locCold = locationService.GetOrCreate("Холодильник №1", "полка 1");
            repo.Add(new Batch("LOW-DEMO-001", 42m, 75m, 2, deficitId, supplierId)
            {
                ProductionDate = DateTime.Today.AddMonths(-1),
                ExpiryDate = DateTime.Today.AddMonths(12),
                StorageLocationId = locCold
            });
        }
    }

    private static void EnsureMarkedItems(SqlConnection conn, EntityRepository repo)
    {
        var markedBatches = conn.Query<BatchInfo>(@"
            SELECT b.Id AS BatchId, b.ItemId, b.BatchNumber
            FROM BATCH b
            INNER JOIN ITEM i ON i.Id = b.ItemId
            WHERE i.Name LIKE N'%Амоксициллин%' OR i.Name LIKE N'%Азитромицин%' OR i.Name LIKE N'%Цефтриаксон%'");

        int uniqueId = conn.ExecuteScalar<int>("SELECT ISNULL(MAX(Id),0) FROM UNIQUE_ITEM") + 1;
        foreach (var batch in markedBatches)
        {
            int count = conn.ExecuteScalar<int>("SELECT COUNT(*) FROM UNIQUE_ITEM WHERE BatchId = @BatchId", new { batch.BatchId });
            int needed = batch.BatchNumber.Contains("AMOX") ? 20 : 5;
            if (count < needed)
            {
                for (int i = 0; i < needed - count; i++)
                {
                    string qr = $"01{DateTime.Now:yyMMdd}ABCD{batch.BatchNumber}{uniqueId + i}";
                    repo.Add(new UniqueItem(qr, "in_stock", batch.BatchId));
                }
                uniqueId += needed - count;
            }
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
        var itemPrices = conn.Query<ItemPrice>(@"
            SELECT i.Id, MIN(b.RetailPrice) AS Price
            FROM ITEM i
            INNER JOIN BATCH b ON b.ItemId = i.Id AND b.Quantity > 0
                AND (b.ExpiryDate IS NULL OR b.ExpiryDate >= GETDATE())
            GROUP BY i.Id").ToList();

        if (itemPrices.Count == 0)
            return;

        var random = new Random(42);
        decimal[] dailyTotals = { 820m, 1240m, 1560m, 1890m, 2100m, 2450m, 2780m, 3120m, 2650m, 1980m, 3420m, 1230m, 940m, 1670m, 2890m };

        for (int dayOffset = 14; dayOffset >= 0; dayOffset--)
        {
            var day = DateTime.Today.AddDays(-dayOffset);
            int checksPerDay = dayOffset <= 2 ? 15 : (dayOffset <= 7 ? 8 : 5);
            decimal targetDayRevenue = dailyTotals[dayOffset % dailyTotals.Length];
            decimal checkAmount = Math.Round(targetDayRevenue / checksPerDay, 2);

            for (int c = 0; c < checksPerDay; c++)
            {
                var pick = itemPrices[random.Next(itemPrices.Count)];
                decimal total = c == checksPerDay - 1
                    ? targetDayRevenue - checkAmount * (checksPerDay - 1)
                    : checkAmount;
                total = Math.Round(total, 2);
                if (total <= 0) total = 50m + (decimal)random.NextDouble() * 200m;

                string payment = random.Next(4) == 0 ? "cash" : "card";
                var saleTime = day.AddHours(9 + random.Next(10)).AddMinutes(random.Next(60));

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

    private static void SeedReturn(SqlConnection conn, int cashierId)
    {
        var recentSale = conn.QueryFirstOrDefault<dynamic>(
            @"SELECT TOP 1 Id, CashierId, Date, TotalAmount 
              FROM SALE 
              ORDER BY Date DESC");
        if (recentSale != null && recentSale.Date >= DateTime.Today.AddDays(-2))
        {
            bool hasReturn = conn.ExecuteScalar<bool>(
                "SELECT COUNT(1) FROM [RETURN] WHERE OriginalSaleId = @saleId",
                new { saleId = (int)recentSale.Id });
            if (!hasReturn)
            {
                conn.Execute(@"
                    INSERT INTO [RETURN] (Date, Reason, RefundAmount, OriginalSaleId, EmployeeId)
                    VALUES (@date, @reason, @amount, @saleId, @empId)",
                    new
                    {
                        date = DateTime.Now,
                        reason = "Брак упаковки",
                        amount = (decimal)recentSale.TotalAmount * 0.5m,
                        saleId = (int)recentSale.Id,
                        empId = cashierId
                    });
            }
        }
    }
}