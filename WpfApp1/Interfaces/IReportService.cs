// Services/IReportService.cs
using System;
using System.Collections.Generic;
using PharmacyApp.Models;

namespace PharmacyApp.Interfaces
{
    public interface IReportService
    {
        /// <summary>Данные для дашборда: выручка за предыдущий день, кол-во чеков, средний чек</summary>
        (decimal revenue, int receiptCount, decimal avgCheck) GetYesterdayStats();

        /// <summary>Топ категорий по выручке (сортировка по убыванию)</summary>
        List<CategoryRevenueDto> GetTopCategoriesByRevenue();

        /// <summary>Агрегаты по кассирам: количество чеков, сумма продаж, средний чек</summary>
        List<CashierStatsDto> GetCashierStats();

        /// <summary>Мониторинг дефектуры: товары, где остаток < min_stock</summary>
        List<DeficitItemDto> GetDeficitItems();

        /// <summary>Экспорт ежедневного отчёта (продажи, списания) в Excel</summary>
        /// <returns>Путь к сохранённому файлу</returns>
        string ExportDailyReportToExcel(DateTime date);

        /// <summary>Создать черновик заказа поставщику по дефицитным товарам</summary>
        void CreateDraftOrderForDeficitItems();
    }

    public class CategoryRevenueDto
    {
        public string CategoryName { get; set; }
        public decimal Revenue { get; set; }
    }

    public class CashierStatsDto
    {
        public string CashierName { get; set; }
        public int ReceiptCount { get; set; }
        public decimal TotalSales { get; set; }
        public decimal AverageCheck { get; set; }
    }

    public class DeficitItemDto
    {
        public int ItemId { get; set; }
        public string ItemName { get; set; }
        public int CurrentStock { get; set; }
        public int MinStock { get; set; }
        public int SuggestedOrderQty { get; set; }
    }
}