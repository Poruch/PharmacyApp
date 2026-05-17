using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PharmacyApp.ViewModels;

public class InventoryItem : INotifyPropertyChanged
{
    public int BatchId { get; set; }
    public string BatchNumber { get; set; } = "";
    public string ItemName { get; set; } = "";
    public DateTime? ExpiryDate { get; set; }
    public int? StorageLocationId { get; set; }
    public string? StorageLocationName { get; set; }

    public int AccountQuantity { get; set; }

    private int _actualQuantity;
    public int ActualQuantity
    {
        get => _actualQuantity;
        set
        {
            if (value < 0)
                value = 0;
            _actualQuantity = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Difference));
            OnPropertyChanged(nameof(HasDifference));
        }
    }

    public int Difference => ActualQuantity - AccountQuantity;
    public bool HasDifference => Difference != 0;

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
