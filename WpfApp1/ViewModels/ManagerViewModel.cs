using Dapper;
using PharmacyApp.Interfaces;
using PharmacyApp.Services;
using PharmacyApp.Views;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace PharmacyApp.ViewModels;

public class ManagerViewModel : INotifyPropertyChanged
{
    private readonly IReportService _reportService;
    private readonly IPriceService _priceService;
    private readonly IPrintService _printService;

    public ManagerViewModel(IReportService reportService, IPriceService priceService, IPrintService printService)
    {
        _reportService = reportService;
        _priceService = priceService;
        _printService = printService;

        WeeklyRevenue = new ObservableCollection<DailyRevenuePoint>();

        RefreshDashboardCommand = new RelayCommand(_ => LoadDashboard());
        ShowCategoryRevenueCommand = new RelayCommand(_ => ShowReport());
        ShowCashierStatsCommand = new RelayCommand(_ => ShowReport());
        ShowDeficitReportCommand = new RelayCommand(_ => ShowReport());
        ExportDailyReportCommand = new RelayCommand(_ => ExportReport());
        BulkPriceChangeCommand = new RelayCommand(_ => OpenPriceMarkupWindow());
        PrintPriceTagsCommand = new RelayCommand(_ => OpenPrintPriceTagsWindow());
        AddCategoryNoteCommand = new RelayCommand(_ => AddNote());
        CreateDraftOrderCommand = new RelayCommand(_ => CreateDraftOrder());
        LogoutCommand = new RelayCommand(_ => RequestLogout());

        LoadDashboard();
    }

    public string CurrentUser => App.CurrentUser?.FullName ?? "Менеджер";

    private decimal _yesterdayRevenue;
    public decimal YesterdayRevenue
    {
        get => _yesterdayRevenue;
        set { _yesterdayRevenue = value; OnPropertyChanged(); }
    }

    private int _yesterdayReceiptCount;
    public int YesterdayReceiptCount
    {
        get => _yesterdayReceiptCount;
        set { _yesterdayReceiptCount = value; OnPropertyChanged(); }
    }

    private decimal _yesterdayAverageCheck;
    public decimal YesterdayAverageCheck
    {
        get => _yesterdayAverageCheck;
        set { _yesterdayAverageCheck = value; OnPropertyChanged(); }
    }

    public ObservableCollection<DailyRevenuePoint> WeeklyRevenue { get; }

    public RelayCommand RefreshDashboardCommand { get; }
    public RelayCommand ShowCategoryRevenueCommand { get; }
    public RelayCommand ShowCashierStatsCommand { get; }
    public RelayCommand ShowDeficitReportCommand { get; }
    public RelayCommand ExportDailyReportCommand { get; }
    public RelayCommand BulkPriceChangeCommand { get; }
    public RelayCommand PrintPriceTagsCommand { get; }
    public RelayCommand AddCategoryNoteCommand { get; }
    public RelayCommand CreateDraftOrderCommand { get; }
    public RelayCommand LogoutCommand { get; }

    public static Action? LogoutRequested { get; set; }

    private void LoadDashboard()
    {
        var stats = _reportService.GetYesterdayStats();
        YesterdayRevenue = stats.revenue;
        YesterdayReceiptCount = stats.receiptCount;
        YesterdayAverageCheck = stats.avgCheck;

        WeeklyRevenue.Clear();
        for (int i = 6; i >= 0; i--)
        {
            var day = DateTime.Today.AddDays(-i);
            WeeklyRevenue.Add(new DailyRevenuePoint
            {
                Day = day.ToString("dd.MM"),
                Value = GetRevenueForDay(day)
            });
        }
    }

    private static decimal GetRevenueForDay(DateTime day)
    {
        using var conn = new Microsoft.Data.SqlClient.SqlConnection(ConfigManager.ConnectionString);
        return conn.QueryFirstOrDefault<decimal?>(@"
            SELECT ISNULL(SUM(TotalAmount), 0) FROM SALE
            WHERE CAST(Date AS DATE) = @day",
            new { day = day.Date }) ?? 0m;
    }

    private void ShowReport()
    {
        var reportsWindow = new ReportsWindow(_reportService);
        reportsWindow.Owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);
        reportsWindow.ShowDialog();
    }

    private void ExportReport()
    {
        try
        {
            string path = _reportService.ExportDailyReportToExcel(DateTime.Today.AddDays(-1));
            MessageBox.Show($"Отчёт сохранён: {path}", "Экспорт", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Ошибка экспорта", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OpenPriceMarkupWindow()
    {
        var win = new PriceMarkupWindow(_priceService);
        win.Owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);
        win.ShowDialog();
    }

    private void OpenPrintPriceTagsWindow()
    {
        var win = new PrintPriceTagsWindow(_priceService, _printService);
        win.Owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);
        win.ShowDialog();
    }

    private static void AddNote() =>
        MessageBox.Show("Функция добавления пометки к категории", "Информация",
            MessageBoxButton.OK, MessageBoxImage.Information);

    private void CreateDraftOrder()
    {
        try
        {
            int? docId = _reportService.CreateDraftOrderForDeficitItems();
            if (docId == null)
            {
                MessageBox.Show(
                    "Нет товаров с дефицитом на складе. Черновик не создан.",
                    "Заказ поставщику",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            MessageBox.Show(
                $"Черновик заказа поставщику создан (документ №{docId}).",
                "Успех",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static void RequestLogout() => LogoutRequested?.Invoke();

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public class DailyRevenuePoint
{
    public string Day { get; set; } = "";
    public decimal Value { get; set; }
}
