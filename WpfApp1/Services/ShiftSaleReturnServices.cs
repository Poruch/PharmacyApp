using Dapper;
using Microsoft.Data.SqlClient;
using PharmacyApp.Interfaces;
using PharmacyApp.Models;
using PharmacyApp.ViewModels;
using System.Collections.ObjectModel;
using System.Data;

namespace PharmacyApp.Services;

public class ShiftService : IShiftService
{
    private ShiftSession? _current;

    private sealed class ShiftSession
    {
        public DateTime StartTime { get; init; }
        public int CashierId { get; init; }
        public string CashierName { get; init; } = "";
        public List<ShiftSaleEntry> Sales { get; } = new();
        public int SalesCount { get; set; }
        public decimal TotalCash { get; set; }
        public decimal TotalCard { get; set; }
    }

    public bool IsShiftOpen => _current != null;

    public void OpenShift()
    {
        if (_current != null)
            throw new InvalidOperationException("Смена уже открыта");

        _current = new ShiftSession
        {
            StartTime = DateTime.Now,
            CashierId = App.CurrentUser?.UserId ?? 0,
            CashierName = App.CurrentUser?.FullName ?? "Кассир"
        };
    }

    public string? CloseShift()
    {
        if (_current == null)
            return null;

        var info = BuildShiftInfo(DateTime.Now);
        string path = ReceiptFileService.SaveShiftReport(info, _current.CashierName, _current.Sales);
        _current = null;
        return path;
    }

    public ShiftInfo GetCurrentShiftInfo()
    {
        if (_current == null)
            return new ShiftInfo();

        return BuildShiftInfo(null);
    }

    public void RecordSale(int saleId, decimal totalAmount, string paymentType)
    {
        if (_current == null)
            return;

        _current.Sales.Add(new ShiftSaleEntry
        {
            SaleId = saleId,
            Date = DateTime.Now,
            TotalAmount = totalAmount,
            PaymentType = paymentType
        });

        _current.SalesCount++;
        if (paymentType == "cash")
            _current.TotalCash += totalAmount;
        else
            _current.TotalCard += totalAmount;
    }

    private ShiftInfo BuildShiftInfo(DateTime? endTime) =>
        new()
        {
            StartTime = _current!.StartTime,
            EndTime = endTime,
            SalesCount = _current.SalesCount,
            TotalCash = _current.TotalCash,
            TotalCard = _current.TotalCard
        };
}

public class SaleService : ISaleService
{
    private readonly string _connectionString;
    private readonly IShiftService _shiftService;

    public SaleService(IShiftService shiftService)
    {
        _connectionString = ConfigManager.ConnectionString;
        _shiftService = shiftService;
    }

    public SaleResult Sale(ObservableCollection<CartItem> cart, string paymentType)
    {
        if (cart == null || cart.Count == 0)
            throw new InvalidOperationException("Корзина пуста");

        if (!_shiftService.IsShiftOpen)
            throw new InvalidOperationException("Смена не открыта. Откройте смену перед продажей.");

        var lines = cart.Select(c => new SaleLineResult
        {
            ItemName = c.ItemName,
            Quantity = c.Quantity,
            Price = c.Price
        }).ToList();

        decimal totalAmount = cart.Sum(i => i.Total);
        string cashierName = App.CurrentUser?.FullName ?? "Кассир";
        int cashierId = App.CurrentUser?.UserId ?? 1;

        using var conn = new SqlConnection(_connectionString);
        conn.Open();
        using var transaction = conn.BeginTransaction();
        try
        {
            var saleId = conn.QuerySingle<int>(@"
                INSERT INTO SALE (Date, TotalAmount, PaymentType, CashierId)
                VALUES (GETDATE(), @totalAmount, @paymentType, @cashierId);
                SELECT CAST(SCOPE_IDENTITY() as int)",
                new { totalAmount, paymentType, cashierId },
                transaction);

            foreach (var item in cart)
            {
                var batch = conn.QueryFirstOrDefault<Batch>(@"
                    SELECT TOP 1 * FROM BATCH
                    WHERE ItemId = @itemId AND Quantity >= @qty AND ExpiryDate > GETDATE()
                    ORDER BY ExpiryDate ASC",
                    new { itemId = item.ItemId, qty = item.Quantity },
                    transaction);

                if (batch == null)
                    throw new Exception($"Недостаточно товара '{item.ItemName}' на складе");

                bool isMarked = conn.ExecuteScalar<int>(
                    "SELECT COUNT(1) FROM UNIQUE_ITEM WHERE BatchId = @batchId AND Status = 'in_stock'",
                    new { batchId = batch.Id }, transaction) > 0;

                if (isMarked)
                {
                    var uniqueItems = conn.Query<UniqueItem>(@"
                        SELECT TOP (@qty) * FROM UNIQUE_ITEM
                        WHERE BatchId = @batchId AND Status = 'in_stock'",
                        new { batchId = batch.Id, qty = item.Quantity },
                        transaction).ToList();

                    if (uniqueItems.Count < item.Quantity)
                        throw new Exception($"Недостаточно маркированных экземпляров для '{item.ItemName}'");

                    foreach (var ui in uniqueItems)
                    {
                        conn.Execute("UPDATE UNIQUE_ITEM SET Status = 'sold' WHERE Id = @id",
                            new { ui.Id }, transaction);
                        conn.Execute(@"
                            INSERT INTO SALE_ITEM (Quantity, PriceAtSale, SaleId, UniqueItemId, ItemId)
                            VALUES (1, @price, @saleId, @uniqueItemId, @itemId)",
                            new { price = item.Price, saleId, uniqueItemId = ui.Id, itemId = item.ItemId },
                            transaction);
                    }
                }
                else
                {
                    conn.Execute(@"
                        INSERT INTO SALE_ITEM (Quantity, PriceAtSale, SaleId, UniqueItemId, ItemId)
                        VALUES (@qty, @price, @saleId, NULL, @itemId)",
                        new { qty = item.Quantity, price = item.Price, saleId, itemId = item.ItemId },
                        transaction);
                }

                conn.Execute("UPDATE BATCH SET Quantity = Quantity - @qty WHERE Id = @batchId",
                    new { qty = item.Quantity, batchId = batch.Id }, transaction);
            }

            transaction.Commit();
            cart.Clear();

            _shiftService.RecordSale(saleId, totalAmount, paymentType);

            return new SaleResult
            {
                SaleId = saleId,
                Date = DateTime.Now,
                TotalAmount = totalAmount,
                PaymentType = paymentType,
                CashierName = cashierName,
                Lines = lines
            };
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }
}

public class ReturnService : IReturnService
{
    private readonly string _connectionString;

    public ReturnService() => _connectionString = ConfigManager.ConnectionString;

    public Sale? GetSaleByReceiptNumber(string receiptNumber)
    {
        if (!int.TryParse(receiptNumber?.Trim(), out int saleId))
            return null;

        using var conn = new SqlConnection(_connectionString);
        return conn.QueryFirstOrDefault<Sale>("SELECT * FROM SALE WHERE Id = @id", new { id = saleId });
    }

    public List<SaleItem> GetSaleItemsBySaleId(int saleId)
    {
        using var conn = new SqlConnection(_connectionString);
        return conn.Query<SaleItem>("SELECT * FROM SALE_ITEM WHERE SaleId = @saleId", new { saleId }).AsList();
    }

    public Item? GetItemById(int itemId)
    {
        using var conn = new SqlConnection(_connectionString);
        return conn.QueryFirstOrDefault<Item>("SELECT * FROM ITEM WHERE Id = @id", new { id = itemId });
    }

    public AppUser? GetUserById(int userId)
    {
        using var conn = new SqlConnection(_connectionString);
        return conn.QueryFirstOrDefault<AppUser>("SELECT * FROM [USER] WHERE UserId = @id", new { id = userId });
    }

    public void ProcessReturn(int originalSaleId, List<ReturnItemViewModel> itemsToReturn, string reason, int employeeId)
    {
        using var conn = new SqlConnection(_connectionString);
        conn.Open();
        using var transaction = conn.BeginTransaction();
        try
        {
            decimal totalRefund = itemsToReturn.Sum(x => x.ReturnSum);
            var returnId = conn.QuerySingle<int>(@"
                INSERT INTO [RETURN] (Date, Reason, RefundAmount, OriginalSaleId, EmployeeId)
                VALUES (GETDATE(), @reason, @refund, @saleId, @empId);
                SELECT CAST(SCOPE_IDENTITY() as int)",
                new { reason, refund = totalRefund, saleId = originalSaleId, empId = employeeId },
                transaction);

            foreach (var item in itemsToReturn)
            {
                var saleItem = conn.QueryFirstOrDefault<SaleItem>(
                    "SELECT * FROM SALE_ITEM WHERE Id = @id", new { id = item.SaleItemId }, transaction);
                if (saleItem == null)
                    throw new Exception($"Не найден проданный товар с ID {item.SaleItemId}");

                conn.Execute(@"
                    INSERT INTO [RETURN_ITEM] (Quantity, ReturnId, UniqueItemId, ItemId)
                    VALUES (@qty, @retId, @uniqId, @itemId)",
                    new { qty = item.ReturnQuantity, retId = returnId, uniqId = saleItem.UniqueItemId, itemId = saleItem.ItemId },
                    transaction);

                if (saleItem.UniqueItemId.HasValue)
                {
                    conn.Execute("UPDATE UNIQUE_ITEM SET Status = 'returned' WHERE Id = @id",
                        new { id = saleItem.UniqueItemId }, transaction);
                    conn.Execute(@"
                        UPDATE BATCH SET Quantity = Quantity + @qty
                        WHERE Id = (SELECT BatchId FROM UNIQUE_ITEM WHERE Id = @uniqId)",
                        new { qty = item.ReturnQuantity, uniqId = saleItem.UniqueItemId }, transaction);
                }
                else
                {
                    var batch = conn.QueryFirstOrDefault<Batch>(@"
                        SELECT TOP 1 * FROM BATCH
                        WHERE ItemId = @itemId
                        ORDER BY ExpiryDate ASC",
                        new { itemId = saleItem.ItemId }, transaction);

                    if (batch == null)
                        throw new Exception($"Не найдена партия для возврата товара с ID {saleItem.ItemId}");

                    conn.Execute("UPDATE BATCH SET Quantity = Quantity + @qty WHERE Id = @batchId",
                        new { qty = item.ReturnQuantity, batchId = batch.Id }, transaction);
                }
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }
}
