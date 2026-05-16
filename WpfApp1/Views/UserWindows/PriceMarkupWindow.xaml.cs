using PharmacyApp.Interfaces;
using PharmacyApp.Models;
using PharmacyApp.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace PharmacyApp.Views
{
    public partial class PriceMarkupWindow : Window, INotifyPropertyChanged
    {
        private readonly IPriceService _priceService;
        private Category _selectedCategory;
        private decimal _markupPercent;
        private bool _respectVitalLimits = true;

        public ObservableCollection<Category> Categories { get; set; }

        public Category SelectedCategory
        {
            get => _selectedCategory;
            set { _selectedCategory = value; OnPropertyChanged(); ApplyMarkupCommand.RaiseCanExecuteChanged(); }
        }

        public decimal MarkupPercent
        {
            get => _markupPercent;
            set { _markupPercent = value; OnPropertyChanged(); ApplyMarkupCommand.RaiseCanExecuteChanged(); }
        }

        public bool RespectVitalLimits
        {
            get => _respectVitalLimits;
            set { _respectVitalLimits = value; OnPropertyChanged(); }
        }

        public RelayCommand ApplyMarkupCommand { get; }
        public RelayCommand CancelCommand { get; }

        public PriceMarkupWindow(IPriceService priceService)
        {
            InitializeComponent();
            DataContext = this;   // привязка к самому окну
            _priceService = priceService;
            Categories = new ObservableCollection<Category>(_priceService.GetAllCategories());
            ApplyMarkupCommand = new RelayCommand(
                _ => ApplyMarkup(),
                _ => SelectedCategory != null && MarkupPercent != 0
            );
            CancelCommand = new RelayCommand(_ => Close());
        }

        private void ApplyMarkup()
        {
            try
            {
                _priceService.ApplyMarkupToCategory(SelectedCategory.Id, MarkupPercent, RespectVitalLimits);
                MessageBox.Show($"Цены в категории \"{SelectedCategory.Name}\" обновлены.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is Category cat)
                SelectedCategory = cat;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}