using Dapper;
using PharmacyApp.Interfaces;
using PharmacyApp.Services;
using PharmacyApp.Models;
using PharmacyApp.Views;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace PharmacyApp.ViewModels;

public class PharmacistViewModel : INotifyPropertyChanged
{
    private readonly IReceivingService _receivingService;
    private readonly IItemService _itemService;
    private readonly IInventoryService _inventoryService;
    private readonly IStorageLocationService _storageLocationService;

    public PharmacistViewModel(
        IReceivingService receivingService,
        IItemService itemService,
        IInventoryService inventoryService,
        IStorageLocationService storageLocationService)
    {
        _receivingService = receivingService;
        _itemService = itemService;
        _inventoryService = inventoryService;
        _storageLocationService = storageLocationService;

        Batches = new ObservableCollection<Batch>();
        InventoryItems = new ObservableCollection<InventoryItem>();
        ExpiredBatches = new ObservableCollection<Batch>();
        StorageLocations = new ObservableCollection<StorageLocation>();

        FindSupplierByInvoiceCommand = new RelayCommand(_ => FindSupplier());
        SaveBatchAndItemsCommand = new RelayCommand(_ => OpenReceivingWindow());
        MoveBatchCommand = new RelayCommand(_ => MoveBatch(), _ => SelectedBatch != null);
        CompareAndCorrectCommand = new RelayCommand(_ => CompareAndCorrect());
        RefreshExpiredCommand = new RelayCommand(_ => RefreshExpiredBatches());
        CreateWriteOffDocCommand = new RelayCommand(_ => CreateWriteOff());
        LogoutCommand = new RelayCommand(_ => { ManagerViewModel.LogoutRequested?.Invoke(); });

        LoadData();
    }

    public string CurrentUser => App.CurrentUser?.FullName ?? "Провизор";

    private string _invoiceScanCode = "";
    public string InvoiceScanCode
    {
        get => _invoiceScanCode;
        set { _invoiceScanCode = value; OnPropertyChanged(); }
    }

    private string _foundSupplierName = "";
    public string FoundSupplierName
    {
        get => _foundSupplierName;
        set { _foundSupplierName = value; OnPropertyChanged(); }
    }

    public string PurchasePrice { get; set; } = "";
    public string RetailPrice { get; set; } = "";

    public ObservableCollection<StorageLocation> StorageLocations { get; }

    private StorageLocation? _selectedNewStorageLocation;
    public StorageLocation? SelectedNewStorageLocation
    {
        get => _selectedNewStorageLocation;
        set { _selectedNewStorageLocation = value; OnPropertyChanged(); }
    }

    public ObservableCollection<Batch> Batches { get; }
    public ObservableCollection<InventoryItem> InventoryItems { get; }
    public ObservableCollection<Batch> ExpiredBatches { get; }

    private Batch? _selectedBatch;
    public Batch? SelectedBatch
    {
        get => _selectedBatch;
        set
        {
            _selectedBatch = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedBatchStorageConditions));
            MoveBatchCommand.RaiseCanExecuteChanged();
        }
    }

    public string SelectedBatchStorageConditions
    {
        get
        {
            if (SelectedBatch == null)
                return "Выберите партию";

            string location = SelectedBatch.StorageLocationName ?? "—";
            var item = _itemService.GetAllItems().FirstOrDefault(i => i.Id == SelectedBatch.ItemId);
            if (item?.TempMin == null && item?.TempMax == null)
                return $"Место: {location}\nОбычные условия хранения (+15…+25 °C)";

            return $"Место: {location}\nТемпература: {item.TempMin}…{item.TempMax} °C";
        }
    }

    public RelayCommand FindSupplierByInvoiceCommand { get; }
    public RelayCommand SaveBatchAndItemsCommand { get; }
    public RelayCommand MoveBatchCommand { get; }
    public RelayCommand CompareAndCorrectCommand { get; }
    public RelayCommand RefreshExpiredCommand { get; }
    public RelayCommand CreateWriteOffDocCommand { get; }
    public RelayCommand LogoutCommand { get; }

    private void LoadData()
    {
        Batches.Clear();
        foreach (var batch in LoadBatches())
            Batches.Add(batch);

        InventoryItems.Clear();
        foreach (var row in _inventoryService.GetInventoryRows())
            InventoryItems.Add(row);

        ExpiredBatches.Clear();
        foreach (var batch in _inventoryService.GetExpiredBatches())
            ExpiredBatches.Add(batch);

        StorageLocations.Clear();
        foreach (var loc in _storageLocationService.GetAll())
            StorageLocations.Add(loc);
    }

    private static List<Batch> LoadBatches()
    {
        using var conn = new Microsoft.Data.SqlClient.SqlConnection(ConfigManager.ConnectionString);
        return conn.Query<Batch>(@"
            SELECT
                b.[ID] AS Id,
                b.BatchNumber,
                b.ProductionDate,
                b.ExpiryDate,
                b.PurchasePrice,
                b.RetailPrice,
                b.Quantity,
                b.StorageLocationId,
                b.ItemId,
                b.SupplierId,
                CASE
                    WHEN sl.LocationId IS NULL THEN N'—'
                    WHEN sl.Cell IS NULL OR sl.Cell = '' THEN sl.Shelf
                    ELSE sl.Shelf + N', ' + sl.Cell
                END AS StorageLocationName
            FROM BATCH b
            LEFT JOIN STORAGE_LOCATION sl ON sl.LocationId = b.StorageLocationId
            WHERE b.Quantity > 0
            ORDER BY b.ExpiryDate").ToList();
    }

    private void FindSupplier()
    {
        var supplier = _receivingService.FindSupplierByInvoiceNumber(InvoiceScanCode);
        FoundSupplierName = supplier?.Name ?? "Не найден";
    }

    private void OpenReceivingWindow()
    {
        var win = new ReceivingWindow(_receivingService, _itemService, _storageLocationService);
        win.Owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);
        if (win.ShowDialog() == true)
            LoadData();
    }

    private void MoveBatch()
    {
        if (SelectedBatch == null || SelectedNewStorageLocation == null)
        {
            MessageBox.Show("Выберите партию и новое место хранения.", "Склад",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _inventoryService.MoveBatch(SelectedBatch.Id, SelectedNewStorageLocation.LocationId);
        SelectedBatch.StorageLocationId = SelectedNewStorageLocation.LocationId;
        SelectedBatch.StorageLocationName = SelectedNewStorageLocation.DisplayName;
        OnPropertyChanged(nameof(SelectedBatchStorageConditions));
        MessageBox.Show($"Партия перемещена на: {SelectedNewStorageLocation.DisplayName}",
            "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void CompareAndCorrect()
    {
        var changes = _inventoryService.GetInventoryChanges(InventoryItems);
        if (changes.Count == 0)
        {
            MessageBox.Show("Расхождений не обнаружено. Фактические остатки совпадают с учётными.",
                "Инвентаризация", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var confirmWindow = new InventoryConfirmWindow(changes);
        confirmWindow.Owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);
        if (confirmWindow.ShowDialog() != true)
            return;

        try
        {
            _inventoryService.CompareAndCorrect(InventoryItems);
            LoadData();
            MessageBox.Show($"Остатки по партиям скорректированы ({changes.Count} поз.).", "Инвентаризация",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public void RefreshExpiredBatches()
    {
        ExpiredBatches.Clear();
        foreach (var batch in _inventoryService.GetExpiredBatches())
            ExpiredBatches.Add(batch);
        OnPropertyChanged(nameof(ExpiredBatchesCount));
    }

    public int ExpiredBatchesCount => ExpiredBatches.Count;

    private void CreateWriteOff()
    {
        RefreshExpiredBatches();

        if (ExpiredBatches.Count == 0)
        {
            MessageBox.Show("Нет просроченных партий для списания.", "Списание",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        string itemsList = string.Join("\n", ExpiredBatches.Take(8)
            .Select(b => $"• {b.ItemName} — {b.Quantity} шт., срок {b.ExpiryDate:dd.MM.yyyy}"));
        if (ExpiredBatches.Count > 8)
            itemsList += $"\n... и ещё {ExpiredBatches.Count - 8}";

        if (MessageBox.Show(
                $"Списать {ExpiredBatches.Count} просроченных партий?\n\n{itemsList}",
                "Подтверждение списания",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        try
        {
            _inventoryService.CreateWriteOffDocument(ExpiredBatches);
            LoadData();
            MessageBox.Show("Документ списания создан.", "Успех",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
