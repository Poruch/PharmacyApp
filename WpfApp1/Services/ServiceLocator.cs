using PharmacyApp.Interfaces;

namespace PharmacyApp.Services;

public static class ServiceLocator
{
    public static IShiftService ShiftService { get; } = new ShiftService();
    public static ISaleService SaleService { get; } = new SaleService();
    public static IReturnService ReturnService { get; } = new ReturnService();
    public static IReceivingService ReceivingService { get; } = new ReceivingService();
    public static IItemService ItemService { get; } = new ItemService();
    public static IInventoryService InventoryService { get; } = new InventoryService();
    public static IPriceService PriceService { get; } = new PriceService();
    public static IPrintService PrintService { get; } = new PrintService();
    public static IReportService ReportService { get; } = new ReportService();
}
