using PharmacyApp.Services;
using PharmacyApp.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace PharmacyApp.Controls;

public partial class PharmacistControl : UserControl
{
    private PharmacistViewModel? _viewModel;

    public PharmacistControl()
    {
        InitializeComponent();
        _viewModel = new PharmacistViewModel(
            ServiceLocator.ReceivingService,
            ServiceLocator.ItemService,
            ServiceLocator.InventoryService,
            ServiceLocator.StorageLocationService);
        DataContext = _viewModel;

        Loaded += (_, _) => _viewModel?.RefreshExpiredBatches();

        var tabControl = (TabControl)Content;
        tabControl.SelectionChanged += OnTabSelectionChanged;
    }

    private void OnTabSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.Source is not TabControl tabControl || tabControl.SelectedItem is not TabItem tab)
            return;

        if (tab.Header?.ToString()?.Contains("Списание") == true)
            _viewModel?.RefreshExpiredBatches();
    }
}
