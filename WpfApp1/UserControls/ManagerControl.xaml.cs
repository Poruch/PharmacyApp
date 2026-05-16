using PharmacyApp.Services;
using PharmacyApp.ViewModels;
using System.Windows.Controls;

namespace PharmacyApp.Controls;

public partial class ManagerControl : UserControl
{
    public ManagerControl()
    {
        InitializeComponent();
        DataContext = new ManagerViewModel(
            ServiceLocator.ReportService,
            ServiceLocator.PriceService,
            ServiceLocator.PrintService);
    }
}
