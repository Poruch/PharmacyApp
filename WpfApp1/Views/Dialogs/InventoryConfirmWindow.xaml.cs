using PharmacyApp.Interfaces;
using System.Collections.Generic;
using System.Windows;

namespace PharmacyApp.Views;

public partial class InventoryConfirmWindow : Window
{
    public List<InventoryChangeDto> Changes { get; }

    public InventoryConfirmWindow(IEnumerable<InventoryChangeDto> changes)
    {
        InitializeComponent();
        Changes = changes.ToList();
        DataContext = this;
    }

    private void Confirm_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
