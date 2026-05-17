using Dapper;
using Microsoft.Data.SqlClient;
using PharmacyApp.Models;
using PharmacyApp.Services;
using System.Windows;
using System.Windows.Controls;

namespace PharmacyApp.Controls.Crud;

public partial class StorageLocationCrudControl : UserControl
{
    private readonly EntityRepository _repo = new(ConfigManager.ConnectionString);
    private StorageLocation? _selected;

    public StorageLocationCrudControl()
    {
        InitializeComponent();
        Loaded += (_, _) => Refresh();
    }

    private void Refresh()
    {
        var list = _repo.GetAll<StorageLocation>()
            .OrderBy(l => l.Shelf)
            .ThenBy(l => l.Cell ?? "")
            .ToList();
        GridLocations.ItemsSource = list;
    }

    private void GridLocations_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (GridLocations.SelectedItem is not StorageLocation loc)
            return;
        _selected = loc;
        TxtShelf.Text = loc.Shelf;
        TxtCell.Text = loc.Cell ?? "";
    }

    private void BtnNew_Click(object sender, RoutedEventArgs e)
    {
        _selected = null;
        GridLocations.SelectedItem = null;
        TxtShelf.Text = "";
        TxtCell.Text = "";
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(TxtShelf.Text))
                throw new InvalidOperationException("Укажите стеллаж или зону хранения.");

            string shelf = TxtShelf.Text.Trim();
            string? cell = string.IsNullOrWhiteSpace(TxtCell.Text) ? null : TxtCell.Text.Trim();

            if (IsDuplicate(shelf, cell, _selected?.LocationId))
                throw new InvalidOperationException("Такое место хранения уже существует.");

            if (_selected == null)
            {
                _repo.Add(new StorageLocation(shelf, cell));
            }
            else
            {
                _selected.Shelf = shelf;
                _selected.Cell = cell;
                _repo.Update(_selected);
            }

            Refresh();
            MessageBox.Show("Место хранения сохранено.", "Успех",
                MessageBoxButton.OK, MessageBoxImage.Information);
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
            MessageBox.Show("Выберите место хранения.", "Внимание",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (MessageBox.Show($"Удалить «{_selected.DisplayName}»?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        try
        {
            int usedByBatches = CountBatchesAtLocation(_selected.LocationId);
            if (usedByBatches > 0)
            {
                MessageBox.Show(
                    $"Нельзя удалить: место используется в {usedByBatches} партиях. Сначала переместите партии.",
                    "Удаление", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

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

    private bool IsDuplicate(string shelf, string? cell, int? excludeId)
    {
        using var conn = new SqlConnection(ConfigManager.ConnectionString);
        return conn.ExecuteScalar<int>(@"
            SELECT COUNT(*) FROM STORAGE_LOCATION
            WHERE Shelf = @shelf
              AND ((@cell IS NULL AND Cell IS NULL) OR Cell = @cell)
              AND (@excludeId IS NULL OR LocationId <> @excludeId)",
            new { shelf, cell, excludeId }) > 0;
    }

    private static int CountBatchesAtLocation(int locationId)
    {
        using var conn = new SqlConnection(ConfigManager.ConnectionString);
        return conn.ExecuteScalar<int>(
            "SELECT COUNT(*) FROM BATCH WHERE StorageLocationId = @id",
            new { id = locationId });
    }
}
