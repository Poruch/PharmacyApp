using PharmacyApp.Models;
using PharmacyApp.Services;
using PharmacyApp.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace PharmacyApp.Controls.Crud;

public partial class ItemCrudControl : UserControl
{
    private readonly EntityRepository _repo = new(ConfigManager.ConnectionString);
    private Item? _selected;

    public ItemCrudControl()
    {
        InitializeComponent();
        Loaded += (_, _) => Refresh();
    }

    private void Refresh()
    {
        var categories = _repo.GetAll<Category>();
        CmbCategory.ItemsSource = categories;
        GridItems.ItemsSource = CrudGridRowsFactory.CreateItemRows(_repo.GetAll<Item>(), categories);
    }

    private void GridItems_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (GridItems.SelectedItem is not ItemGridRow row)
            return;
        var item = row.Entity;
        _selected = item;
        TxtName.Text = item.Name;
        TxtBarcode.Text = item.Barcode ?? "";
        TxtInn.Text = item.Inn ?? "";
        TxtDosage.Text = item.Dosage ?? "";
        TxtForm.Text = item.Form ?? "";
        TxtUnit.Text = item.Unit;
        TxtPack.Text = item.QuantityPerPack.ToString();
        TxtMinStock.Text = item.MinStock.ToString();
        ChkRx.IsChecked = item.PrescriptionRequired;
        ChkVital.IsChecked = item.IsVital;
        CmbCategory.SelectedValue = item.CategoryId;
        CmbCategory.SelectedItem = CmbCategory.Items.Cast<Category>().FirstOrDefault(c => c.Id == item.CategoryId);
    }

    private void BtnNew_Click(object sender, RoutedEventArgs e)
    {
        _selected = null;
        GridItems.SelectedItem = null;
        TxtName.Text = TxtBarcode.Text = TxtInn.Text = TxtDosage.Text = TxtForm.Text = "";
        TxtUnit.Text = "таб.";
        TxtPack.Text = "1";
        TxtMinStock.Text = "5";
        ChkRx.IsChecked = ChkVital.IsChecked = false;
        CmbCategory.SelectedIndex = CmbCategory.Items.Count > 0 ? 0 : -1;
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(TxtName.Text))
                throw new InvalidOperationException("Укажите название товара.");
            if (CmbCategory.SelectedItem is not Category category)
                throw new InvalidOperationException("Выберите категорию.");
            if (!int.TryParse(TxtPack.Text, out int pack) || pack <= 0)
                throw new InvalidOperationException("Укажите корректное кол-во в упаковке.");
            if (!int.TryParse(TxtMinStock.Text, out int minStock) || minStock < 0)
                throw new InvalidOperationException("Укажите корректный мин. остаток.");

            if (_selected == null)
            {
                var entity = new Item(TxtName.Text.Trim(), TxtUnit.Text.Trim(), pack, category.Id)
                {
                    Inn = TxtInn.Text.Trim(),
                    Dosage = TxtDosage.Text.Trim(),
                    Form = TxtForm.Text.Trim(),
                    Barcode = TxtBarcode.Text.Trim(),
                    MinStock = minStock,
                    PrescriptionRequired = ChkRx.IsChecked == true,
                    IsVital = ChkVital.IsChecked == true
                };
                _repo.Add(entity);
            }
            else
            {
                _selected.Name = TxtName.Text.Trim();
                _selected.Inn = TxtInn.Text.Trim();
                _selected.Dosage = TxtDosage.Text.Trim();
                _selected.Form = TxtForm.Text.Trim();
                _selected.Barcode = TxtBarcode.Text.Trim();
                _selected.Unit = TxtUnit.Text.Trim();
                _selected.QuantityPerPack = pack;
                _selected.CategoryId = category.Id;
                _selected.MinStock = minStock;
                _selected.PrescriptionRequired = ChkRx.IsChecked == true;
                _selected.IsVital = ChkVital.IsChecked == true;
                _repo.Update(_selected);
            }

            Refresh();
            MessageBox.Show("Товар сохранён.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
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
            MessageBox.Show("Выберите товар.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (MessageBox.Show($"Удалить товар «{_selected.Name}»?", "Подтверждение",
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
