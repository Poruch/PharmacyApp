using System.Windows;
using PharmacyApp.Models;

namespace PharmacyApp.Views
{
    public partial class ItemWindow : Window
    {
        public string ItemName
        {
            get => (string)GetValue(ItemNameProperty);
            set => SetValue(ItemNameProperty, value);
        }
        public static readonly DependencyProperty ItemNameProperty =
            DependencyProperty.Register("ItemName", typeof(string), typeof(ItemWindow), new PropertyMetadata("", OnPropertyChanged));

        public string ItemMnfName
        {
            get => (string)GetValue(ItemMnfNameProperty);
            set => SetValue(ItemMnfNameProperty, value);
        }
        public static readonly DependencyProperty ItemMnfNameProperty =
            DependencyProperty.Register("ItemMnfName", typeof(string), typeof(ItemWindow), new PropertyMetadata("", OnPropertyChanged));

        public string ItemDosage
        {
            get => (string)GetValue(ItemDosageProperty);
            set => SetValue(ItemDosageProperty, value);
        }
        public static readonly DependencyProperty ItemDosageProperty =
            DependencyProperty.Register("ItemDosage", typeof(string), typeof(ItemWindow), new PropertyMetadata("", OnPropertyChanged));

        public string ItemForm
        {
            get => (string)GetValue(ItemFormProperty);
            set => SetValue(ItemFormProperty, value);
        }
        public static readonly DependencyProperty ItemFormProperty =
            DependencyProperty.Register("ItemForm", typeof(string), typeof(ItemWindow), new PropertyMetadata("", OnPropertyChanged));

        public bool ItemPrescriptionRequired
        {
            get => (bool)GetValue(ItemPrescriptionRequiredProperty);
            set => SetValue(ItemPrescriptionRequiredProperty, value);
        }
        public static readonly DependencyProperty ItemPrescriptionRequiredProperty =
            DependencyProperty.Register("ItemPrescriptionRequired", typeof(bool), typeof(ItemWindow), new PropertyMetadata(false, OnPropertyChanged));

        public int ItemCategoryId
        {
            get => (int)GetValue(ItemCategoryIdProperty);
            set => SetValue(ItemCategoryIdProperty, value);
        }
        public static readonly DependencyProperty ItemCategoryIdProperty =
            DependencyProperty.Register("ItemCategoryId", typeof(int), typeof(ItemWindow), new PropertyMetadata(0, OnPropertyChanged));

        public int ItemManufacturerId
        {
            get => (int)GetValue(ItemManufacturerIdProperty);
            set => SetValue(ItemManufacturerIdProperty, value);
        }
        public static readonly DependencyProperty ItemManufacturerIdProperty =
            DependencyProperty.Register("ItemManufacturerId", typeof(int), typeof(ItemWindow), new PropertyMetadata(0, OnPropertyChanged));

        public int ItemUnitId
        {
            get => (int)GetValue(ItemUnitIdProperty);
            set => SetValue(ItemUnitIdProperty, value);
        }
        public static readonly DependencyProperty ItemUnitIdProperty =
            DependencyProperty.Register("ItemUnitId", typeof(int), typeof(ItemWindow), new PropertyMetadata(0, OnPropertyChanged));

        public bool CanSave
        {
            get => (bool)GetValue(CanSaveProperty);
            set => SetValue(CanSaveProperty, value);
        }
        public static readonly DependencyProperty CanSaveProperty =
            DependencyProperty.Register("CanSave", typeof(bool), typeof(ItemWindow), new PropertyMetadata(false));

        public ItemWindow()
        {
            InitializeComponent();
            ValidateFields();
        }


        private int? _categoryId;
        public ItemWindow(string name, string mnfName, string dosage, string form, bool prescriptionRequired,
                          int categoryId, int manufacturerId, int unitId) : this()
        {
            Title = "Редактирование товара";
            ItemName = name;
            ItemMnfName = mnfName;
            ItemDosage = dosage;
            ItemForm = form;
            ItemPrescriptionRequired = prescriptionRequired;
            ItemCategoryId = categoryId;
            ItemManufacturerId = manufacturerId;
            ItemUnitId = unitId;
        }
        public ItemWindow(string name, string mnfName, string dosage, string form, bool prescriptionRequired)
        {
            InitializeComponent();
            Title = "Редактирование товара";
            ItemName = name;
            ItemMnfName = mnfName;
            ItemDosage = dosage;
            ItemForm = form;
            ItemPrescriptionRequired = prescriptionRequired;
            ValidateFields();
        }
        public int CategoryId => _categoryId ?? 0;
        private static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as ItemWindow)?.ValidateFields();
        }
        public ItemWindow(int categoryId)
        {
            InitializeComponent();
            _categoryId = categoryId;
            Title = "Добавление товара";
            ValidateFields();
        }
        private void ValidateFields()
        {
            string errors = "";
            bool valid = true;

            if (string.IsNullOrWhiteSpace(ItemName))
            {
                errors += "• Название товара обязательно\n";
                valid = false;
            }
            if (ItemCategoryId <= 0)
            {
                errors += "• ID категории должен быть положительным\n";
                valid = false;
            }
            if (ItemManufacturerId <= 0)
            {
                errors += "• ID производителя должен быть положительным\n";
                valid = false;
            }
            if (ItemUnitId <= 0)
            {
                errors += "• ID единицы измерения должен быть положительным\n";
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