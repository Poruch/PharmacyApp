using PharmacyApp.Interfaces;
using PharmacyApp.Models;
using PharmacyApp.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;

namespace PharmacyApp.Views
{
    public partial class ReturnWindow : Window, INotifyPropertyChanged
    {
        private readonly IReturnService _returnService;
        private Sale _currentSale;

        public ReturnWindow(IReturnService returnService)
        {
            InitializeComponent();
            DataContext = this;
            _returnService = returnService;
            SaleItems = new ObservableCollection<ReturnItemViewModel>();
        }

        private string _receiptNumber;
        public string ReceiptNumber
        {
            get => _receiptNumber;
            set { _receiptNumber = value; OnPropertyChanged(); LoadSaleCommand.RaiseCanExecuteChanged(); }
        }

        private string _saleInfo;
        public string SaleInfo
        {
            get => _saleInfo;
            set { _saleInfo = value; OnPropertyChanged(); }
        }

        private string _saleDate;
        public string SaleDate
        {
            get => _saleDate;
            set { _saleDate = value; OnPropertyChanged(); }
        }

        private string _returnReason;
        public string ReturnReason
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
            _currentSale = _returnService.GetSaleByReceiptNumber(ReceiptNumber);
            if (_currentSale == null)
            {
                MessageBox.Show("Чек не найден.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                SaleInfo = "Чек не найден";
                SaleItems.Clear();
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
            var itemsToReturn = SaleItems.Where(x => x.ReturnQuantity > 0).ToList();
            if (!itemsToReturn.Any())
            {
                MessageBox.Show("Не выбрано ни одного товара для возврата.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            try
            {
                _returnService.ProcessReturn(_currentSale.Id, itemsToReturn, ReturnReason, App.CurrentUser.UserId);
                MessageBox.Show("Возврат оформлен успешно.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при возврате: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class ReturnItemViewModel : INotifyPropertyChanged
    {
        public int SaleItemId { get; set; }
        public int ItemId { get; set; }
        public string ItemName { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public int? UniqueItemId { get; set; }
        public int MaxReturnQuantity { get; set; }

        private int _returnQuantity;
        public int ReturnQuantity
        {
            get => _returnQuantity;
            set
            {
                if (value < 0) value = 0;
                if (value > MaxReturnQuantity) value = MaxReturnQuantity;
                _returnQuantity = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ReturnSum));
            }
        }
        public decimal ReturnSum => ReturnQuantity * Price;

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}