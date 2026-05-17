using PharmacyApp.Interfaces;
using PharmacyApp.Models;
using PharmacyApp.Services;
using PharmacyApp.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace PharmacyApp.Controls.Crud;

public partial class BatchCrudControl : UserControl
{
    private readonly EntityRepository _repo = new(ConfigManager.ConnectionString);
    private readonly IStorageLocationService _storageLocationService = ServiceLocator.StorageLocationService;
    private Batch? _selected;

    public BatchCrudControl()
    {
        InitializeComponent();
        DpProduction.SelectedDate = DateTime.Today;
        DpExpiry.SelectedDate = DateTime.Today.AddYears(1);
        Loaded += (_, _) => Refresh();
    }

    private void Refresh()
    {
        var items = _repo.GetAll<Item>();
        var suppliers = _repo.GetAll<Supplier>();
        var locations = _storageLocationService.GetAll();
        CmbItem.ItemsSource = items;
        CmbSupplier.ItemsSource = suppliers;
        CmbStorage.ItemsSource = locations;
        GridBatches.ItemsSource = CrudGridRowsFactory.CreateBatchRows(
            _repo.GetAll<Batch>(), items, suppliers, locations);
    }

    private void GridBatches_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (GridBatches.SelectedItem is not BatchGridRow row)
            return;
        var batch = row.Entity;
        _selected = batch;
        TxtBatchNumber.Text = batch.BatchNumber;
        TxtQuantity.Text = batch.Quantity.ToString();
        TxtPurchase.Text = batch.PurchasePrice.ToString("F2");
        TxtRetail.Text = batch.RetailPrice.ToString("F2");
        CmbStorage.SelectedItem = CmbStorage.Items.Cast<StorageLocation>()
            .FirstOrDefault(l => l.LocationId == batch.StorageLocationId);
        DpProduction.SelectedDate = batch.ProductionDate;
        DpExpiry.SelectedDate = batch.ExpiryDate;
        CmbItem.SelectedItem = CmbItem.Items.Cast<Item>().FirstOrDefault(i => i.Id == batch.ItemId);
        CmbSupplier.SelectedItem = CmbSupplier.Items.Cast<Supplier>().FirstOrDefault(s => s.Id == batch.SupplierId);
    }

    private void BtnNew_Click(object sender, RoutedEventArgs e)
    {
        _selected = null;
        GridBatches.SelectedItem = null;
        TxtBatchNumber.Text = $"B{DateTime.Now:yyyyMMddHHmm}";
        TxtQuantity.Text = "10";
        TxtPurchase.Text = "100";
        TxtRetail.Text = "150";
        if (CmbStorage.Items.Count > 0) CmbStorage.SelectedIndex = 0;
        DpProduction.SelectedDate = DateTime.Today;
        DpExpiry.SelectedDate = DateTime.Today.AddYears(1);
        if (CmbItem.Items.Count > 0) CmbItem.SelectedIndex = 0;
        if (CmbSupplier.Items.Count > 0) CmbSupplier.SelectedIndex = 0;
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(TxtBatchNumber.Text))
                throw new InvalidOperationException("Укажите номер серии.");
            if (CmbItem.SelectedItem is not Item item)
                throw new InvalidOperationException("Выберите товар.");
            if (CmbSupplier.SelectedItem is not Supplier supplier)
                throw new InvalidOperationException("Выберите поставщика.");
            if (CmbStorage.SelectedItem is not StorageLocation location)
                throw new InvalidOperationException("Выберите место хранения.");
            if (!int.TryParse(TxtQuantity.Text, out int qty) || qty < 0)
                throw new InvalidOperationException("Укажите корректное количество.");
            if (!decimal.TryParse(TxtPurchase.Text, out decimal purchase) || purchase < 0)
                throw new InvalidOperationException("Укажите закупочную цену.");
            if (!decimal.TryParse(TxtRetail.Text, out decimal retail) || retail < purchase)
                throw new InvalidOperationException("Розничная цена должна быть не ниже закупочной.");

            if (_selected == null)
            {
                var entity = new Batch(TxtBatchNumber.Text.Trim(), purchase, retail, qty, item.Id, supplier.Id)
                {
                    ProductionDate = DpProduction.SelectedDate ?? DateTime.Today,
                    ExpiryDate = DpExpiry.SelectedDate,
                    StorageLocationId = location.LocationId
                };
                _repo.Add(entity);
            }
            else
            {
                _selected.BatchNumber = TxtBatchNumber.Text.Trim();
                _selected.ItemId = item.Id;
                _selected.SupplierId = supplier.Id;
                _selected.Quantity = qty;
                _selected.PurchasePrice = purchase;
                _selected.RetailPrice = retail;
                _selected.ProductionDate = DpProduction.SelectedDate ?? DateTime.Today;
                _selected.ExpiryDate = DpExpiry.SelectedDate;
                _selected.StorageLocationId = location.LocationId;
                _repo.Update(_selected);
            }

            Refresh();
            MessageBox.Show("Партия сохранена.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnDelete_Click(object sender, RoutedEventArgs e)
    {
        if (_selected == null)
        {
            MessageBox.Show("Выберите партию.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (MessageBox.Show($"Удалить партию «{_selected.BatchNumber}»?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        try
        {
            _repo.Delete(_selected);
            Refresh();
            BtnNew_Click(sender, e);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnRefresh_Click(object sender, RoutedEventArgs e) => Refresh();
}
