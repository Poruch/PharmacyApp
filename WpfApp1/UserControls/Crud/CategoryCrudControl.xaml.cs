using PharmacyApp.Models;
using PharmacyApp.Services;
using PharmacyApp.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace PharmacyApp.Controls.Crud;

public partial class CategoryCrudControl : UserControl
{
    private readonly EntityRepository _repo = new(ConfigManager.ConnectionString);
    private Category? _selected;
    private List<Category> _allCategories = [];

    public CategoryCrudControl()
    {
        InitializeComponent();
        Loaded += (_, _) => Refresh();
    }

    private void Refresh()
    {
        _allCategories = _repo.GetAll<Category>();
        GridCategories.ItemsSource = CrudGridRowsFactory.CreateCategoryRows(_allCategories);
        LoadParentCombo();
        _selected = null;
    }

    private void LoadParentCombo(int? excludeId = null)
    {
        var parents = _allCategories.Where(c => excludeId == null || c.Id != excludeId).ToList();
        CmbParent.ItemsSource = parents;
    }

    private void GridCategories_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (GridCategories.SelectedItem is not CategoryGridRow row)
            return;
        _selected = row.Entity;
        TxtName.Text = row.Entity.Name;
        TxtDescription.Text = row.Entity.Description ?? "";
        LoadParentCombo(row.Entity.Id);
        CmbParent.SelectedItem = row.Entity.ParentId is int parentId
            ? _allCategories.FirstOrDefault(c => c.Id == parentId)
            : null;
    }

    private void BtnNew_Click(object sender, RoutedEventArgs e)
    {
        _selected = null;
        GridCategories.SelectedItem = null;
        TxtName.Text = "";
        TxtDescription.Text = "";
        LoadParentCombo();
        CmbParent.SelectedIndex = -1;
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(TxtName.Text))
                throw new InvalidOperationException("Укажите название категории.");

            int? parentId = (CmbParent.SelectedItem as Category)?.Id;

            if (_selected == null)
            {
                var entity = new Category(TxtName.Text.Trim(), TxtDescription.Text.Trim(), parentId);
                _repo.Add(entity);
            }
            else
            {
                if (parentId == _selected.Id)
                    throw new InvalidOperationException("Категория не может быть родителем самой себе.");

                _selected.Name = TxtName.Text.Trim();
                _selected.Description = TxtDescription.Text.Trim();
                _selected.ParentId = parentId;
                _repo.Update(_selected);
            }

            Refresh();
            MessageBox.Show("Категория сохранена.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
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
            MessageBox.Show("Выберите категорию.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (MessageBox.Show($"Удалить категорию «{_selected.Name}»?", "Подтверждение",
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
