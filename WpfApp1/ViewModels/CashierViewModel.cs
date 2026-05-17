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

public class CashierViewModel : INotifyPropertyChanged
{
    private readonly IShiftService _shiftService;
    private readonly ISaleService _saleService;
    private readonly IReturnService _returnService;
    private readonly IItemService _itemService;

    public CashierViewModel(
        IShiftService shiftService,
        ISaleService saleService,
        IReturnService returnService,
        IItemService itemService)
    {
        _shiftService = shiftService;
        _saleService = saleService;
        _returnService = returnService;
        _itemService = itemService;
        Cart = new ObservableCollection<CartItem>();
        ReturnItems = new ObservableCollection<ReturnItemViewModel>();

        OpenShiftCommand = new RelayCommand(_ => OpenShift());
        CloseShiftCommand = new RelayCommand(_ => CloseShift());
        PayCashCommand = new RelayCommand(_ => Pay("cash"), _ => Cart.Count > 0);
        PayCardCommand = new RelayCommand(_ => Pay("card"), _ => Cart.Count > 0);
        ProcessReturnCommand = new RelayCommand(_ => ShowReturnWindow());
        ScanBarcodeCommand = new RelayCommand(_ => ScanBarcode());
        AddItemManuallyCommand = new RelayCommand(_ => AddItemManually());

        Cart.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(CartTotal));
            PayCashCommand.RaiseCanExecuteChanged();
            PayCardCommand.RaiseCanExecuteChanged();
        };

        RefreshShiftInfo();
    }

    public string CurrentUser => App.CurrentUser?.FullName ?? "Кассир";
    public string ShiftStatusText => _shiftService.IsShiftOpen ? "Смена открыта" : "Смена закрыта";

    public ObservableCollection<CartItem> Cart { get; }
    public ObservableCollection<ReturnItemViewModel> ReturnItems { get; }

    private string _barcode = "";
    public string Barcode
    {
        get => _barcode;
        set { _barcode = value; OnPropertyChanged(); }
    }

    private bool _prescriptionVerified;
    public bool PrescriptionVerified
    {
        get => _prescriptionVerified;
        set { _prescriptionVerified = value; OnPropertyChanged(); }
    }

    private bool _isPrescriptionRequired;
    public bool IsPrescriptionRequired
    {
        get => _isPrescriptionRequired;
        set { _isPrescriptionRequired = value; OnPropertyChanged(); }
    }

    private string _returnReceiptNumber = "";
    public string ReturnReceiptNumber
    {
        get => _returnReceiptNumber;
        set { _returnReceiptNumber = value; OnPropertyChanged(); }
    }

    public decimal CartTotal => Cart.Sum(c => c.Total);

    public DateTime? ShiftStartTime { get; private set; }
    public int ShiftSalesCount { get; private set; }
    public decimal ShiftTotalCash { get; private set; }
    public decimal ShiftTotalCard { get; private set; }
    public decimal ShiftTotal { get; private set; }

    public RelayCommand OpenShiftCommand { get; }
    public RelayCommand CloseShiftCommand { get; }
    public RelayCommand PayCashCommand { get; }
    public RelayCommand PayCardCommand { get; }
    public RelayCommand ProcessReturnCommand { get; }
    public RelayCommand ScanBarcodeCommand { get; }
    public RelayCommand AddItemManuallyCommand { get; }

    private void OpenShift()
    {
        if (_shiftService.IsShiftOpen)
        {
            MessageBox.Show("Смена уже открыта.", "Смена", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (MessageBox.Show("Открыть смену?", "Подтверждение", MessageBoxButton.YesNo,
                MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        _shiftService.OpenShift();
        RefreshShiftInfo();
        MessageBox.Show("Смена открыта.", "Смена", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void CloseShift()
    {
        if (!_shiftService.IsShiftOpen)
        {
            MessageBox.Show("Смена не открыта.", "Смена", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var info = _shiftService.GetCurrentShiftInfo();
        string summary = $"Продаж: {info.SalesCount}\nНаличные: {info.TotalCash:C}\nКарта: {info.TotalCard:C}\nИтого: {info.Total:C}";
        if (MessageBox.Show($"Закрыть смену?\n\n{summary}", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        string? reportPath = _shiftService.CloseShift();
        RefreshShiftInfo();
        string message = reportPath != null
            ? $"Смена закрыта.\nОтчёт сохранён:\n{reportPath}"
            : "Смена закрыта.";
        MessageBox.Show(message, "Смена", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void Pay(string type)
    {
        if (!_shiftService.IsShiftOpen)
        {
            MessageBox.Show("Откройте смену перед оформлением продажи.", "Смена",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var result = _saleService.Sale(Cart, type);
            string receiptPath = ReceiptFileService.SaveSaleReceipt(result);
            OnPropertyChanged(nameof(CartTotal));
            RefreshShiftInfo();
            PayCashCommand.RaiseCanExecuteChanged();
            PayCardCommand.RaiseCanExecuteChanged();
            MessageBox.Show(
                $"Оплата прошла успешно.\nЧек №{result.SaleId}\nФайл чека:\n{receiptPath}",
                "Продажа",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Ошибка оплаты", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ScanBarcode()
    {
        if (string.IsNullOrWhiteSpace(Barcode))
            return;

        var item = _itemService.GetItemByBarcode(Barcode);
        if (item == null)
        {
            MessageBox.Show("Товар не найден.", "Сканирование", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        AddItemToCart(item, 1);
        Barcode = "";
    }

    private void AddItemManually()
    {
        var win = new ManualAddItemWindow(_itemService);
        win.Owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);
        if (win.ShowDialog() == true && win.SelectedItem != null)
            AddItemToCart(win.SelectedItem, win.Quantity);
    }

    private void AddItemToCart(Item item, int quantity)
    {
        IsPrescriptionRequired = item.PrescriptionRequired;
        if (item.PrescriptionRequired && !PrescriptionVerified)
        {
            MessageBox.Show("Требуется проверка рецепта.", "Рецептурный препарат",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        decimal price = GetRetailPrice(item.Id);
        if (price <= 0)
        {
            MessageBox.Show("Нет доступных партий товара на складе.", "Склад",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var existing = Cart.FirstOrDefault(c => c.ItemId == item.Id);
        if (existing != null)
        {
            existing.Quantity += quantity;
            OnPropertyChanged(nameof(CartTotal));
        }
        else
        {
            var cartItem = new CartItem { ItemId = item.Id, ItemName = item.Name, Quantity = quantity, Price = price };
            cartItem.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName is nameof(CartItem.Quantity) or nameof(CartItem.Total))
                    OnPropertyChanged(nameof(CartTotal));
            };
            Cart.Add(cartItem);
        }

        OnPropertyChanged(nameof(CartTotal));
        PayCashCommand.RaiseCanExecuteChanged();
        PayCardCommand.RaiseCanExecuteChanged();
    }

    private static decimal GetRetailPrice(int itemId)
    {
        using var conn = new Microsoft.Data.SqlClient.SqlConnection(ConfigManager.ConnectionString);
        return conn.QueryFirstOrDefault<decimal?>(@"
            SELECT TOP 1 RetailPrice FROM BATCH
            WHERE ItemId = @itemId AND Quantity > 0
            ORDER BY ExpiryDate ASC",
            new { itemId }) ?? 0m;
    }

    private void ShowReturnWindow()
    {
        var returnWindow = new ReturnWindow(_returnService);
        returnWindow.Owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);
        returnWindow.ShowDialog();
        RefreshShiftInfo();
    }

    private void RefreshShiftInfo()
    {
        var info = _shiftService.GetCurrentShiftInfo();
        ShiftStartTime = info.StartTime;
        ShiftSalesCount = info.SalesCount;
        ShiftTotalCash = info.TotalCash;
        ShiftTotalCard = info.TotalCard;
        ShiftTotal = info.Total;
        NotifyShiftChanged();
    }

    private void NotifyShiftChanged()
    {
        OnPropertyChanged(nameof(ShiftStatusText));
        OnPropertyChanged(nameof(ShiftStartTime));
        OnPropertyChanged(nameof(ShiftSalesCount));
        OnPropertyChanged(nameof(ShiftTotalCash));
        OnPropertyChanged(nameof(ShiftTotalCard));
        OnPropertyChanged(nameof(ShiftTotal));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
