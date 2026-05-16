// IReceivingService.cs
using PharmacyApp.Models;
using PharmacyApp.Views;
using System.Collections.Generic;

namespace PharmacyApp.Interfaces
{
    public interface IReceivingService
    {
        Supplier FindSupplierByInvoiceNumber(string invoiceNumber);
        void SaveBatch(Batch batch, List<string> scannedCodes);
    }
}

// IReturnService.cs

namespace PharmacyApp.Interfaces
{
    public interface IReturnService
    {
        Sale GetSaleByReceiptNumber(string receiptNumber);
        List<SaleItem> GetSaleItemsBySaleId(int saleId);
        Item GetItemById(int itemId);
        AppUser GetUserById(int userId);
        void ProcessReturn(int originalSaleId, List<ReturnItemViewModel> itemsToReturn, string reason, int employeeId);
    }
}

// IItemService (если нужен для выбора товара в приёмке)
public interface IItemService
{
    List<Item> GetAllItems();
    Item GetItemByBarcode(string barcode);
}