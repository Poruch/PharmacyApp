using PharmacyApp.Services;
using PharmacyApp.ViewModels;
using System.Windows.Controls;

namespace PharmacyApp.Controls;

public partial class CashierControl : UserControl
{
    public CashierControl()
    {
        InitializeComponent();
        DataContext = new CashierViewModel(
            ServiceLocator.ShiftService,
            ServiceLocator.SaleService,
            ServiceLocator.ReturnService,
            ServiceLocator.ItemService);
    }
}
