using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Windows;
using PharmacyApp.Services;
using PharmacyApp.Models;

namespace PharmacyApp.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly DataService _dataService;
    private object _selectedItem;

    public ObservableCollection<Category> Categories => _dataService.Categories;

    public object? SelectedItem
    {
        get => _selectedItem;
        set
        {
            _selectedItem = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedItemInfo));
            UpdateCommands();
        }
    }

    public string SelectedItemInfo
    {
        get
        {
            return "Неизвестный тип";
        }
    }
   


    // Команды
    public RelayCommand AddCategoryCommand { get; }
    public RelayCommand AddItemCommand { get; }
    public RelayCommand AddBatchCommand { get; }
    public RelayCommand EditCommand { get; }
    public RelayCommand DeleteCommand { get; }
    public RelayCommand ExportToJson { get; }
    public RelayCommand ExportToXml { get; }
    public MainViewModel()
    {
        try
        {
            _dataService = new DataService();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка инициализации: {ex.Message}\n{ex.InnerException?.Message}");
            throw;
        }

        AddCategoryCommand = new RelayCommand((object? x) => { }, _ => true);
        AddItemCommand = new RelayCommand((object? x) => { }, CanAddItem);
        AddBatchCommand = new RelayCommand((object? x) => { }, CanAddBatch);
        EditCommand = new RelayCommand((object? x) => { }, CanEditOrDelete);
        DeleteCommand = new RelayCommand((object? x) => { }, CanEditOrDelete);
        ExportToJson = new RelayCommand(ExportToJSON);
        ExportToXml = new RelayCommand(ExportToXML);
    }
    private void ExportToJSON(object parameter)
    {
        var dialog = new SaveFileDialog { Filter = "JSON файлы|*.json", DefaultExt = "json" };
        if (dialog.ShowDialog() == true)
        {
            ExportService.ExportToJson(dialog.FileName, Categories);
            MessageBox.Show("Экспорт завершён", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
    private void ExportToXML(object parameter)
    {
        var dialog = new SaveFileDialog { Filter = "XML files (*.xml)|*.xml", DefaultExt = "xml" };
        if (dialog.ShowDialog() == true)
        {
            ExportService.ExportToXml(dialog.FileName, Categories);
            MessageBox.Show("Экспорт в XML выполнен.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
    private bool CanAddItem(object parameter) => SelectedItem is Category;
    private bool CanAddBatch(object parameter) => SelectedItem is Item;
    private bool CanEditOrDelete(object parameter) => SelectedItem is Item || SelectedItem is Batch;

   

   

   

    

    

    private void UpdateCommands()
    {
        AddItemCommand.RaiseCanExecuteChanged();
        AddBatchCommand.RaiseCanExecuteChanged();
        EditCommand.RaiseCanExecuteChanged();
        DeleteCommand.RaiseCanExecuteChanged();
    }
}