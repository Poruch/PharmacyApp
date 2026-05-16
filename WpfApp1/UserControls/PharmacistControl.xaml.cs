using PharmacyApp.Services;
using PharmacyApp.ViewModels;
using System.Windows.Controls;

namespace PharmacyApp.Controls;

public partial class PharmacistControl : UserControl
{
    public PharmacistControl()
    {
        InitializeComponent();
        DataContext = new PharmacistViewModel(
            ServiceLocator.ReceivingService,
            ServiceLocator.ItemService,
            ServiceLocator.InventoryService);
    }
}
