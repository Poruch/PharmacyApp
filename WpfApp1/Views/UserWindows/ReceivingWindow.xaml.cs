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
    public partial class ReceivingWindow : Window, INotifyPropertyChanged
    {
        private readonly IReceivingService _receivingService;
        private readonly IItemService _itemService;

        public ReceivingWindow(IReceivingService receivingService, IItemService itemService)
        {
            InitializeComponent();
            DataContext = this;
            _receivingService = receivingService;
            _itemService = itemService;
            ScannedCodes = new ObservableCollection<string>();
            AvailableItems = new ObservableCollection<Item>(_itemService.GetAllItems());
        }

        // ---------- Свойства ----------
        private string _invoiceNumber;
        public string InvoiceNumber
        {
            get => _invoiceNumber;
            set { _invoiceNumber = value; OnPropertyChanged(); FindSupplierCommand.RaiseCanExecuteChanged(); }
        }

        private Supplier _foundSupplier;
        public Supplier FoundSupplier
        {
            get => _foundSupplier;
            set { _foundSupplier = value; OnPropertyChanged(); OnPropertyChanged(nameof(SupplierInfo)); SaveBatchCommand.RaiseCanExecuteChanged(); }
        }
        public string SupplierInfo => FoundSupplier != null ? $"{FoundSupplier.Name} (ИНН: {FoundSupplier.Inn})" : "Поставщик не найден";

        private string _scannedCode;
        public string ScannedCode
        {
            get => _scannedCode;
            set { _scannedCode = value; OnPropertyChanged(); AddCodeCommand.RaiseCanExecuteChanged(); }
        }
        public ObservableCollection<string> ScannedCodes { get; }

        private Item _selectedItem;
        public Item SelectedItem
        {
            get => _selectedItem;
            set { _selectedItem = value; OnPropertyChanged(); SaveBatchCommand.RaiseCanExecuteChanged(); }
        }
        public ObservableCollection<Item> AvailableItems { get; }

        private string _batchNumber;
        public string BatchNumber
        {
            get => _batchNumber;
            set { _batchNumber = value; OnPropertyChanged(); SaveBatchCommand.RaiseCanExecuteChanged(); }
        }

        private DateTime? _expiryDate;
        public DateTime? ExpiryDate
        {
            get => _expiryDate;
            set { _expiryDate = value; OnPropertyChanged(); SaveBatchCommand.RaiseCanExecuteChanged(); }
        }

        private decimal? _purchasePrice;
        public decimal? PurchasePrice
        {
            get => _purchasePrice;
            set { _purchasePrice = value; OnPropertyChanged(); SaveBatchCommand.RaiseCanExecuteChanged(); }
        }

        private decimal? _retailPrice;
        public decimal? RetailPrice
        {
            get => _retailPrice;
            set { _retailPrice = value; OnPropertyChanged(); SaveBatchCommand.RaiseCanExecuteChanged(); }
        }

        private string _storageLocation;
        public string StorageLocation
        {
            get => _storageLocation;
            set { _storageLocation = value; OnPropertyChanged(); }
        }

        private int? _quantity;
        public int? Quantity
        {
            get => _quantity;
            set { _quantity = value; OnPropertyChanged(); SaveBatchCommand.RaiseCanExecuteChanged(); }
        }

        // ---------- Команды ----------
        public RelayCommand FindSupplierCommand { get; }
        public RelayCommand AddCodeCommand { get; }
        public RelayCommand ClearCodesCommand { get; }
        public RelayCommand SaveBatchCommand { get; }
        public RelayCommand CancelCommand { get; }

        private void FindSupplier()
        {
            FoundSupplier = _receivingService.FindSupplierByInvoiceNumber(InvoiceNumber);
            if (FoundSupplier == null)
                MessageBox.Show("Поставщик по данной накладной не найден.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void AddCode()
        {
            if (!string.IsNullOrWhiteSpace(ScannedCode))
                ScannedCodes.Add(ScannedCode);
            ScannedCode = string.Empty;
        }

        private bool CanSave() =>
            FoundSupplier != null && SelectedItem != null && !string.IsNullOrWhiteSpace(BatchNumber) &&
            ExpiryDate.HasValue && PurchasePrice.HasValue && RetailPrice.HasValue && Quantity.HasValue && Quantity > 0;

        private void SaveBatch()
        {
            try
            {
                var batch = new Batch
                {
                    BatchNumber = BatchNumber,
                    ProductionDate = DateTime.Now,
                    ExpiryDate = ExpiryDate.Value,
                    PurchasePrice = PurchasePrice.Value,
                    RetailPrice = RetailPrice.Value,
                    Quantity = Quantity.Value,
                    StorageLocation = StorageLocation,
                    SupplierId = FoundSupplier.Id,
                    ItemId = SelectedItem.Id
                };
                _receivingService.SaveBatch(batch, ScannedCodes.ToList());
                MessageBox.Show("Партия и маркированные экземпляры сохранены.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}