using System;
using System.Windows;

namespace PharmacyApp.Views
{
    public partial class BatchWindow : Window
    {
        public string BatchSerialNumber
        {
            get => (string)GetValue(BatchSerialNumberProperty);
            set => SetValue(BatchSerialNumberProperty, value);
        }
        public static readonly DependencyProperty BatchSerialNumberProperty =
            DependencyProperty.Register("BatchSerialNumber", typeof(string), typeof(BatchWindow), new PropertyMetadata("", OnPropertyChanged));

        public DateTime? BatchProductionDate
        {
            get => (DateTime?)GetValue(BatchProductionDateProperty);
            set => SetValue(BatchProductionDateProperty, value);
        }
        public static readonly DependencyProperty BatchProductionDateProperty =
            DependencyProperty.Register("BatchProductionDate", typeof(DateTime?), typeof(BatchWindow), new PropertyMetadata(null, OnPropertyChanged));

        public DateTime? BatchExpiryDate
        {
            get => (DateTime?)GetValue(BatchExpiryDateProperty);
            set => SetValue(BatchExpiryDateProperty, value);
        }
        public static readonly DependencyProperty BatchExpiryDateProperty =
            DependencyProperty.Register("BatchExpiryDate", typeof(DateTime?), typeof(BatchWindow), new PropertyMetadata(null, OnPropertyChanged));

        public decimal BatchPurchasePrice
        {
            get => (decimal)GetValue(BatchPurchasePriceProperty);
            set => SetValue(BatchPurchasePriceProperty, value);
        }
        public static readonly DependencyProperty BatchPurchasePriceProperty =
            DependencyProperty.Register("BatchPurchasePrice", typeof(decimal), typeof(BatchWindow), new PropertyMetadata(0m, OnPropertyChanged));

        public decimal BatchSellingPrice
        {
            get => (decimal)GetValue(BatchSellingPriceProperty);
            set => SetValue(BatchSellingPriceProperty, value);
        }
        public static readonly DependencyProperty BatchSellingPriceProperty =
            DependencyProperty.Register("BatchSellingPrice", typeof(decimal), typeof(BatchWindow), new PropertyMetadata(0m, OnPropertyChanged));

        public int BatchQuantity
        {
            get => (int)GetValue(BatchQuantityProperty);
            set => SetValue(BatchQuantityProperty, value);
        }
        public static readonly DependencyProperty BatchQuantityProperty =
            DependencyProperty.Register("BatchQuantity", typeof(int), typeof(BatchWindow), new PropertyMetadata(0, OnPropertyChanged));

        public int BatchItemId
        {
            get => (int)GetValue(BatchItemIdProperty);
            set => SetValue(BatchItemIdProperty, value);
        }
        public static readonly DependencyProperty BatchItemIdProperty =
            DependencyProperty.Register("BatchItemId", typeof(int), typeof(BatchWindow), new PropertyMetadata(0, OnPropertyChanged));

        public int BatchSupplierId
        {
            get => (int)GetValue(BatchSupplierIdProperty);
            set => SetValue(BatchSupplierIdProperty, value);
        }
        public static readonly DependencyProperty BatchSupplierIdProperty =
            DependencyProperty.Register("BatchSupplierId", typeof(int), typeof(BatchWindow), new PropertyMetadata(0, OnPropertyChanged));

        public int BatchLocationId
        {
            get => (int)GetValue(BatchLocationIdProperty);
            set => SetValue(BatchLocationIdProperty, value);
        }
        public static readonly DependencyProperty BatchLocationIdProperty =
            DependencyProperty.Register("BatchLocationId", typeof(int), typeof(BatchWindow), new PropertyMetadata(0, OnPropertyChanged));

        public bool CanSave
        {
            get => (bool)GetValue(CanSaveProperty);
            set => SetValue(CanSaveProperty, value);
        }
        public static readonly DependencyProperty CanSaveProperty =
            DependencyProperty.Register("CanSave", typeof(bool), typeof(BatchWindow), new PropertyMetadata(false));
        private int? _itemId;
        public BatchWindow()
        {
            InitializeComponent();
            ValidateFields();
        }
        public BatchWindow(int itemId)
        {
            InitializeComponent();
            _itemId = itemId;
            Title = "Добавление партии";
            BatchProductionDate = DateTime.Today;
            ValidateFields();
        }
        public BatchWindow(string serialNumber, DateTime productionDate, DateTime? expiryDate,
                       decimal purchasePrice, decimal sellingPrice, int quantity)
        {
            InitializeComponent();
            Title = "Редактирование партии";
            BatchSerialNumber = serialNumber;
            BatchProductionDate = productionDate;
            BatchExpiryDate = expiryDate;
            BatchPurchasePrice = purchasePrice;
            BatchSellingPrice = sellingPrice;
            BatchQuantity = quantity;
            ValidateFields();
        }
        public BatchWindow(string serialNumber, DateTime? productionDate, DateTime? expiryDate,
                           decimal purchasePrice, decimal sellingPrice, int quantity,
                           int itemId, int supplierId, int locationId) : this()
        {
            Title = "Редактирование партии";
            BatchSerialNumber = serialNumber;
            BatchProductionDate = productionDate;
            BatchExpiryDate = expiryDate;
            BatchPurchasePrice = purchasePrice;
            BatchSellingPrice = sellingPrice;
            BatchQuantity = quantity;
            BatchItemId = itemId;
            BatchSupplierId = supplierId;
            BatchLocationId = locationId;
        }

        public int ItemId => _itemId ?? 0;
        private static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as BatchWindow)?.ValidateFields();
        }

        private void ValidateFields()
        {
            string errors = "";
            bool valid = true;

            if (string.IsNullOrWhiteSpace(BatchSerialNumber))
            {
                errors += "• Серийный номер обязателен\n";
                valid = false;
            }
            if (!BatchProductionDate.HasValue)
            {
                errors += "• Дата производства обязательна\n";
                valid = false;
            }
            if (BatchPurchasePrice < 0)
            {
                errors += "• Закупочная цена не может быть отрицательной\n";
                valid = false;
            }
            if (BatchSellingPrice < 0)
            {
                errors += "• Продажная цена не может быть отрицательной\n";
                valid = false;
            }
            if (BatchSellingPrice < BatchPurchasePrice)
            {
                errors += "• Продажная цена не может быть меньше закупочной\n";
                valid = false;
            }
            if (BatchQuantity < 0)
            {
                errors += "• Количество не может быть отрицательным\n";
                valid = false;
            }
            if (BatchItemId <= 0)
            {
                errors += "• ID товара должен быть положительным\n";
                valid = false;
            }
            if (BatchSupplierId <= 0)
            {
                errors += "• ID поставщика должен быть положительным\n";
                valid = false;
            }
            if (BatchLocationId <= 0)
            {
                errors += "• ID места хранения должен быть положительным\n";
                valid = false;
            }
            if (BatchExpiryDate.HasValue && BatchProductionDate.HasValue && BatchExpiryDate.Value <= BatchProductionDate.Value)
            {
                errors += "• Срок годности должен быть позже даты производства\n";
                valid = false;
            }

            txtError.Text = errors.TrimEnd();
            txtError.Visibility = string.IsNullOrEmpty(errors) ? Visibility.Collapsed : Visibility.Visible;
            CanSave = valid;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}