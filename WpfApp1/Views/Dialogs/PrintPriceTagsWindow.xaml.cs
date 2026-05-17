using PharmacyApp.Interfaces;
using PharmacyApp.Models;
using PharmacyApp.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;

namespace PharmacyApp.Views
{
    public partial class PrintPriceTagsWindow : Window, INotifyPropertyChanged
    {
        private readonly IPriceService _priceService;
        private readonly IPrintService _printService;
        private Category _selectedCategory;

        public ObservableCollection<Category> Categories { get; set; }
        public ObservableCollection<ItemPrintModel> FilteredItems { get; set; }

        public Category SelectedCategory
        {
            get => _selectedCategory;
            set { _selectedCategory = value; OnPropertyChanged(); FilterCommand.Execute(null); }
        }

        public RelayCommand FilterCommand { get; }
        public RelayCommand PreviewCommand { get; }
        public RelayCommand PrintCommand { get; }
        public RelayCommand CancelCommand { get; }

        public PrintPriceTagsWindow(IPriceService priceService, IPrintService printService)
        {
            InitializeComponent();
            DataContext = this;
            _priceService = priceService;
            _printService = printService;

            Categories = new ObservableCollection<Category>(_priceService.GetAllCategories());
            FilteredItems = new ObservableCollection<ItemPrintModel>();

            FilterCommand = new RelayCommand(_ => ApplyFilter());
            PreviewCommand = new RelayCommand(_ => ShowPreview());
            PrintCommand = new RelayCommand(_ => Print());
            CancelCommand = new RelayCommand(_ => Close());

            ApplyFilter();
        }

        private void ApplyFilter()
        {
            var items = _priceService.GetItemsByCategory(SelectedCategory?.Id);
            FilteredItems.Clear();
            foreach (var i in items)
                FilteredItems.Add(new ItemPrintModel(i));
        }

        private void ShowPreview()
        {
            var selected = FilteredItems.Where(x => x.IsSelected).Select(x => x.Item).ToList();
            if (!selected.Any())
            {
                MessageBox.Show("Не выбрано ни одного товара.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            _printService.ShowPreview(selected);
        }

        private void Print()
        {
            var selected = FilteredItems.Where(x => x.IsSelected).Select(x => x.Item).ToList();
            if (!selected.Any())
            {
                MessageBox.Show("Не выбрано ни одного товара.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            _printService.PrintPriceTags(selected);
            MessageBox.Show($"Отправлено на печать {selected.Count} ценников.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class ItemPrintModel : INotifyPropertyChanged
    {
        public Item Item { get; }
        public string Name => Item.Name;

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(); }
        }

        public ItemPrintModel(Item item) => Item = item;

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}