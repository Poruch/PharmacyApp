using PharmacyApp.Interfaces;
using PharmacyApp.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace PharmacyApp.Views
{
    public partial class ReportsWindow : Window, INotifyPropertyChanged
    {
        private readonly IReportService _reportService;
        private decimal _yesterdayRevenue;
        private int _yesterdayReceiptCount;
        private decimal _yesterdayAverageCheck;

        public decimal YesterdayRevenue
        {
            get => _yesterdayRevenue;
            set { _yesterdayRevenue = value; OnPropertyChanged(); }
        }
        public int YesterdayReceiptCount
        {
            get => _yesterdayReceiptCount;
            set { _yesterdayReceiptCount = value; OnPropertyChanged(); }
        }
        public decimal YesterdayAverageCheck
        {
            get => _yesterdayAverageCheck;
            set { _yesterdayAverageCheck = value; OnPropertyChanged(); }
        }

        public ObservableCollection<CategoryRevenueDto> TopCategories { get; set; }
        public ObservableCollection<CashierStatsDto> CashierStats { get; set; }
        public ObservableCollection<DeficitItemDto> DeficitItems { get; set; }

        public RelayCommand RefreshDashboardCommand { get; }
        public RelayCommand CreateDraftOrderCommand { get; }
        public RelayCommand ExportDailyReportCommand { get; }
        public RelayCommand CloseCommand { get; }

        public ReportsWindow(IReportService reportService)
        {
            InitializeComponent();
            DataContext = this;
            _reportService = reportService;
            TopCategories = new ObservableCollection<CategoryRevenueDto>();
            CashierStats = new ObservableCollection<CashierStatsDto>();
            DeficitItems = new ObservableCollection<DeficitItemDto>();

            RefreshDashboardCommand = new RelayCommand(_ => LoadDashboard());
            CreateDraftOrderCommand = new RelayCommand(_ => _reportService.CreateDraftOrderForDeficitItems());
            ExportDailyReportCommand = new RelayCommand(_ => ExportDailyReport());
            CloseCommand = new RelayCommand(_ => Close());

            LoadAllData();
        }

        private void LoadAllData()
        {
            LoadDashboard();
            LoadTopCategories();
            LoadCashierStats();
            LoadDeficitItems();
        }

        private void LoadDashboard()
        {
            var (revenue, count, avg) = _reportService.GetYesterdayStats();
            YesterdayRevenue = revenue;
            YesterdayReceiptCount = count;
            YesterdayAverageCheck = avg;
        }

        private void LoadTopCategories()
        {
            TopCategories.Clear();
            foreach (var item in _reportService.GetTopCategoriesByRevenue())
                TopCategories.Add(item);
        }

        private void LoadCashierStats()
        {
            CashierStats.Clear();
            foreach (var item in _reportService.GetCashierStats())
                CashierStats.Add(item);
        }

        private void LoadDeficitItems()
        {
            DeficitItems.Clear();
            foreach (var item in _reportService.GetDeficitItems())
                DeficitItems.Add(item);
        }

        private void ExportDailyReport()
        {
            try
            {
                string filePath = _reportService.ExportDailyReportToExcel(DateTime.Today.AddDays(-1));
                MessageBox.Show($"Отчёт сохранён: {filePath}", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при экспорте: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}