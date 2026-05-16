using PharmacyApp.Models;
using PharmacyApp.Services;
using System.Windows;
using System.Windows.Controls;

namespace PharmacyApp.Controls.Crud;

public partial class SupplierCrudControl : UserControl
{
    private readonly EntityRepository _repo = new(ConfigManager.ConnectionString);
    private Supplier? _selected;

    public SupplierCrudControl()
    {
        InitializeComponent();
        Loaded += (_, _) => Refresh();
    }

    private void Refresh() => GridSuppliers.ItemsSource = _repo.GetAll<Supplier>();

    private void GridSuppliers_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (GridSuppliers.SelectedItem is not Supplier s)
            return;
        _selected = s;
        TxtName.Text = s.Name;
        TxtInn.Text = s.Inn ?? "";
        TxtPhone.Text = s.Phone ?? "";
        TxtEmail.Text = s.Email ?? "";
    }

    private void BtnNew_Click(object sender, RoutedEventArgs e)
    {
        _selected = null;
        GridSuppliers.SelectedItem = null;
        TxtName.Text = TxtInn.Text = TxtPhone.Text = TxtEmail.Text = "";
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(TxtName.Text))
                throw new InvalidOperationException("Укажите название поставщика.");

            if (_selected == null)
            {
                var entity = new Supplier(TxtName.Text.Trim())
                {
                    Inn = TxtInn.Text.Trim(),
                    Phone = TxtPhone.Text.Trim(),
                    Email = TxtEmail.Text.Trim()
                };
                _repo.Add(entity);
            }
            else
            {
                _selected.Name = TxtName.Text.Trim();
                _selected.Inn = TxtInn.Text.Trim();
                _selected.Phone = TxtPhone.Text.Trim();
                _selected.Email = TxtEmail.Text.Trim();
                _repo.Update(_selected);
            }

            Refresh();
            MessageBox.Show("Поставщик сохранён.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
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
            MessageBox.Show("Выберите поставщика.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (MessageBox.Show($"Удалить поставщика «{_selected.Name}»?", "Подтверждение",
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
