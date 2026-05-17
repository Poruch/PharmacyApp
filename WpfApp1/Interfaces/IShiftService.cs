using System.Collections.ObjectModel;
using PharmacyApp.Models;
using PharmacyApp.ViewModels;

namespace PharmacyApp.Interfaces
{
  public interface IShiftService
  {
    bool IsShiftOpen { get; }
    void OpenShift();
    string? CloseShift();
    ShiftInfo GetCurrentShiftInfo();
    void RecordSale(int saleId, decimal totalAmount, string paymentType);
  }

  public interface ISaleService
  {
    SaleResult Sale(ObservableCollection<CartItem> cart, string paymentType);
  }

  public interface IInventoryService
  {
    void MoveBatch(int batchId, int storageLocationId);
    List<InventoryItem> GetInventoryRows();
    List<InventoryChangeDto> GetInventoryChanges(ObservableCollection<InventoryItem> inventoryItems);
    void CompareAndCorrect(ObservableCollection<InventoryItem> inventoryItems);
    ObservableCollection<Batch> GetExpiredBatches();
    void CreateWriteOffDocument(ObservableCollection<Batch> expiredBatches);
  }

  public class InventoryChangeDto
  {
    public string BatchNumber { get; set; } = "";
    public string ItemName { get; set; } = "";
    public DateTime? ExpiryDate { get; set; }
    public string? StorageLocationName { get; set; }
    public int AccountQuantity { get; set; }
    public int ActualQuantity { get; set; }
    public int Difference => ActualQuantity - AccountQuantity;
    public string ChangeDescription => Difference > 0
        ? $"Излишек +{Difference}"
        : $"Недостача {Difference}";
  }
}
