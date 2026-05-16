using PharmacyApp.Interfaces;
using PharmacyApp.Models;
using PharmacyApp.ViewModels;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace PharmacyApp.Views;

public partial class ReturnWindow : Window, INotifyPropertyChanged
{
    private readonly IReturnService _returnService;
    private Sale? _currentSale;

    public ReturnWindow(IReturnService returnService)
    {
        InitializeComponent();
        _returnService = returnService;
        SaleItems = new ObservableCollection<ReturnItemViewModel>();

        LoadSaleCommand = new RelayCommand(_ => LoadSale(), _ => !string.IsNullOrWhiteSpace(ReceiptNumber));
        ProcessReturnCommand = new RelayCommand(_ => ProcessReturn(), _ => _currentSale != null);
        CancelCommand = new RelayCommand(_ => Close());

        DataContext = this;
    }

    private string? _receiptNumber;
    public string? ReceiptNumber
    {
        get => _receiptNumber;
        set { _receiptNumber = value; OnPropertyChanged(); LoadSaleCommand.RaiseCanExecuteChanged(); }
    }

    private string? _saleInfo;
    public string? SaleInfo
    {
        get => _saleInfo;
        set { _saleInfo = value; OnPropertyChanged(); }
    }

    private string? _saleDate;
    public string? SaleDate
    {
        get => _saleDate;
        set { _saleDate = value; OnPropertyChanged(); }
    }

    private string? _returnReason;
    public string? ReturnReason
    {
        get => _returnReason;
        set { _returnReason = value; OnPropertyChanged(); }
    }

    public ObservableCollection<ReturnItemViewModel> SaleItems { get; }

    public RelayCommand LoadSaleCommand { get; }
    public RelayCommand ProcessReturnCommand { get; }
    public RelayCommand CancelCommand { get; }

    private void LoadSale()
    {
        _currentSale = _returnService.GetSaleByReceiptNumber(ReceiptNumber ?? "");
        if (_currentSale == null)
        {
            MessageBox.Show("Чек не найден.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            SaleInfo = "Чек не найден";
            SaleItems.Clear();
            ProcessReturnCommand.RaiseCanExecuteChanged();
            return;
        }

        var cashier = _returnService.GetUserById(_currentSale.CashierId);
        SaleInfo = $"Кассир: {cashier?.LastName} {cashier?.FirstName} | Сумма чека: {_currentSale.TotalAmount:C}";
        SaleDate = _currentSale.Date.ToString("dd.MM.yyyy HH:mm");

        var saleItems = _returnService.GetSaleItemsBySaleId(_currentSale.Id);
        SaleItems.Clear();
        foreach (var si in saleItems)
        {
            var item = _returnService.GetItemById(si.ItemId);
            SaleItems.Add(new ReturnItemViewModel
            {
                SaleItemId = si.Id,
                ItemName = item?.Name ?? "Неизвестно",
                Quantity = si.Quantity,
                Price = si.PriceAtSale,
                UniqueItemId = si.UniqueItemId,
                ItemId = si.ItemId,
                MaxReturnQuantity = si.Quantity
            });
        }
        ProcessReturnCommand.RaiseCanExecuteChanged();
    }

    private void ProcessReturn()
    {
        if (_currentSale == null)
            return;

        var itemsToReturn = SaleItems.Where(x => x.ReturnQuantity > 0).ToList();
        if (itemsToReturn.Count == 0)
        {
            MessageBox.Show("Не выбрано ни одного товара для возврата.", "Внимание",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            int employeeId = App.CurrentUser?.UserId ?? 1;
            _returnService.ProcessReturn(_currentSale.Id, itemsToReturn, ReturnReason ?? "", employeeId);
            MessageBox.Show("Возврат оформлен успешно.", "Успех",
                MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при возврате: {ex.Message}", "Ошибка",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
