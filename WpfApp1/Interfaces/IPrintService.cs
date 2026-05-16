// Services/IPrintService.cs
using System.Collections.Generic;
using PharmacyApp.Models;

namespace PharmacyApp.Interfaces
{
    public interface IPrintService
    {
        /// <summary>Показать предварительный просмотр ценников для выбранных товаров</summary>
        void ShowPreview(List<Item> items);

        /// <summary>Отправить на печать ценники для выбранных товаров</summary>
        void PrintPriceTags(List<Item> items);
    }
}