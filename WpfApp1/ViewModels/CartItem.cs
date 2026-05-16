using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PharmacyApp.ViewModels;

public class CartItem : INotifyPropertyChanged
{
    public int ItemId { get; set; }
    public string ItemName { get; set; } = "";

    private int _quantity = 1;
    public int Quantity
    {
        get => _quantity;
        set { _quantity = value; OnPropertyChanged(); OnPropertyChanged(nameof(Total)); }
    }

    private decimal _price;
    public decimal Price
    {
        get => _price;
        set { _price = value; OnPropertyChanged(); OnPropertyChanged(nameof(Total)); }
    }

    public decimal Total => Quantity * Price;

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
