// Services/IPriceService.cs
using System.Collections.Generic;
using PharmacyApp.Models;

namespace PharmacyApp.Interfaces
{
    public interface IPriceService
    {
        /// <summary>Получить все категории (с подкатегориями)</summary>
        List<Category> GetAllCategories();

        /// <summary>Обновить розничные цены товаров в категории (включая подкатегории)</summary>
        /// <param name="categoryId">ID категории</param>
        /// <param name="markupPercent">Наценка в процентах (может быть отрицательной)</param>
        /// <param name="respectVitalLimits">Учитывать предельные наценки для ЖНВЛП</param>
        void ApplyMarkupToCategory(int categoryId, decimal markupPercent, bool respectVitalLimits);

        /// <summary>Получить список товаров (для печати ценников)</summary>
        List<Item> GetAllItems();

        /// <summary>Получить товары по категории</summary>
        List<Item> GetItemsByCategory(int? categoryId);
    }
}