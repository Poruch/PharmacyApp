using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PharmacyApp.ViewModels;

public class InventoryItem : INotifyPropertyChanged
{
    public int ItemId { get; set; }
    public string ItemName { get; set; } = "";

    public int AccountQuantity { get; set; }

    private int _actualQuantity;
    public int ActualQuantity
    {
        get => _actualQuantity;
        set { _actualQuantity = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
