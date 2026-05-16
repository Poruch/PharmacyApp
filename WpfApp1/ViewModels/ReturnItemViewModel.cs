using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PharmacyApp.ViewModels;

public class ReturnItemViewModel : INotifyPropertyChanged
{
    public int SaleItemId { get; set; }
    public int ItemId { get; set; }
    public string ItemName { get; set; } = "";
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

    private bool _selectedForReturn;
    public bool SelectedForReturn
    {
        get => _selectedForReturn;
        set
        {
            _selectedForReturn = value;
            OnPropertyChanged();
            if (value && ReturnQuantity == 0)
                ReturnQuantity = Quantity;
            else if (!value)
                ReturnQuantity = 0;
        }
    }

    public decimal ReturnSum => ReturnQuantity * Price;

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
