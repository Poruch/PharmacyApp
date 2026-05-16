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

    public PharmacistViewModel(
        IReceivingService receivingService,
        IItemService itemService,
        IInventoryService inventoryService)
    {
        _receivingService = receivingService;
        _itemService = itemService;
        _inventoryService = inventoryService;

        Batches = new ObservableCollection<Batch>();
        InventoryItems = new ObservableCollection<InventoryItem>();
        ExpiredBatches = new ObservableCollection<Batch>();

        FindSupplierByInvoiceCommand = new RelayCommand(_ => FindSupplier());
        SaveBatchAndItemsCommand = new RelayCommand(_ => OpenReceivingWindow());
        MoveBatchCommand = new RelayCommand(_ => MoveBatch(), _ => SelectedBatch != null);
        CompareAndCorrectCommand = new RelayCommand(_ => CompareAndCorrect());
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
    public string StorageLocation { get; set; } = "";

    private string _newStorageLocation = "";
    public string NewStorageLocation
    {
        get => _newStorageLocation;
        set { _newStorageLocation = value; OnPropertyChanged(); }
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

            var item = _itemService.GetAllItems().FirstOrDefault(i => i.Id == SelectedBatch.ItemId);
            if (item?.TempMin == null && item?.TempMax == null)
                return "Обычные условия хранения (+15…+25 °C)";

            return $"Температура: {item.TempMin}…{item.TempMax} °C";
        }
    }

    public RelayCommand FindSupplierByInvoiceCommand { get; }
    public RelayCommand SaveBatchAndItemsCommand { get; }
    public RelayCommand MoveBatchCommand { get; }
    public RelayCommand CompareAndCorrectCommand { get; }
    public RelayCommand CreateWriteOffDocCommand { get; }
    public RelayCommand LogoutCommand { get; }

    private void LoadData()
    {
        Batches.Clear();
        foreach (var batch in LoadBatches())
            Batches.Add(batch);

        InventoryItems.Clear();
        foreach (var row in BuildInventoryRows())
            InventoryItems.Add(row);

        ExpiredBatches.Clear();
        foreach (var batch in _inventoryService.GetExpiredBatches())
            ExpiredBatches.Add(batch);
    }

    private static List<Batch> LoadBatches()
    {
        using var conn = new Microsoft.Data.SqlClient.SqlConnection(ConfigManager.ConnectionString);
        return conn.Query<Batch>("SELECT * FROM BATCH WHERE Quantity > 0 ORDER BY ExpiryDate").ToList();
    }

    private List<InventoryItem> BuildInventoryRows()
    {
        using var conn = new Microsoft.Data.SqlClient.SqlConnection(ConfigManager.ConnectionString);
        return conn.Query<InventoryItem>(@"
            SELECT
                i.Id AS ItemId,
                i.Name AS ItemName,
                ISNULL(SUM(b.Quantity), 0) AS AccountQuantity,
                ISNULL(SUM(b.Quantity), 0) AS ActualQuantity
            FROM ITEM i
            LEFT JOIN BATCH b ON b.ItemId = i.Id AND b.ExpiryDate >= GETDATE()
            GROUP BY i.Id, i.Name
            ORDER BY i.Name").ToList();
    }

    private void FindSupplier()
    {
        var supplier = _receivingService.FindSupplierByInvoiceNumber(InvoiceScanCode);
        FoundSupplierName = supplier?.Name ?? "Не найден";
    }

    private void OpenReceivingWindow()
    {
        var win = new ReceivingWindow(_receivingService, _itemService);
        win.Owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);
        if (win.ShowDialog() == true)
            LoadData();
    }

    private void MoveBatch()
    {
        if (SelectedBatch == null || string.IsNullOrWhiteSpace(NewStorageLocation))
            return;

        _inventoryService.MoveBatch(SelectedBatch.Id, NewStorageLocation);
        SelectedBatch.StorageLocation = NewStorageLocation;
        MessageBox.Show("Партия перемещена", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void CompareAndCorrect()
    {
        try
        {
            _inventoryService.CompareAndCorrect(InventoryItems);
            LoadData();
            MessageBox.Show("Остатки скорректированы", "Инвентаризация",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CreateWriteOff()
    {
        var expired = _inventoryService.GetExpiredBatches();
        if (expired.Count == 0)
        {
            MessageBox.Show("Нет просроченных партий для списания.", "Списание",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        _inventoryService.CreateWriteOffDocument(expired);
        LoadData();
        MessageBox.Show("Документ списания создан", "Успех",
            MessageBoxButton.OK, MessageBoxImage.Information);
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
