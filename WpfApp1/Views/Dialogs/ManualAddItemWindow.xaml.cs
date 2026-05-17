using PharmacyApp.Interfaces;
using PharmacyApp.Models;
using PharmacyApp.ViewModels;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace PharmacyApp.Views;

public partial class ManualAddItemWindow : Window, INotifyPropertyChanged
{
    private readonly IItemService _itemService;

    public Item? SelectedItem { get; private set; }
    public int Quantity { get; private set; }

    public ObservableCollection<Item> Items { get; }

    private Item? _selectedItem;
    public Item? BoundSelectedItem
    {
        get => _selectedItem;
        set { _selectedItem = value; OnPropertyChanged(); AddCommand.RaiseCanExecuteChanged(); }
    }

    private string _quantity = "1";
    public string QuantityText
    {
        get => _quantity;
        set { _quantity = value; OnPropertyChanged(); AddCommand.RaiseCanExecuteChanged(); }
    }

    public RelayCommand AddCommand { get; }
    public RelayCommand CancelCommand { get; }

    public ManualAddItemWindow(IItemService itemService)
    {
        InitializeComponent();
        _itemService = itemService;
        Items = new ObservableCollection<Item>(_itemService.GetAllItems());

        AddCommand = new RelayCommand(_ => Confirm(), _ => CanAdd());
        CancelCommand = new RelayCommand(_ => { DialogResult = false; Close(); });

        DataContext = this;
    }

    private bool CanAdd() =>
        BoundSelectedItem != null && int.TryParse(QuantityText, out int q) && q > 0;

    private void Confirm()
    {
        if (!int.TryParse(QuantityText, out int qty) || qty <= 0 || BoundSelectedItem == null)
        {
            MessageBox.Show("Укажите корректное количество.", "Внимание",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        SelectedItem = BoundSelectedItem;
        Quantity = qty;
        DialogResult = true;
        Close();
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
