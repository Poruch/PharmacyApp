using System.Collections.ObjectModel;
using PharmacyApp.Models;
using PharmacyApp.ViewModels;

namespace PharmacyApp.Interfaces
{
    /// <summary>
    /// Сервис для управления сменой кассира
    /// </summary>
    public interface IShiftService
    {
        /// <summary>Открыта ли смена в данный момент</summary>
        bool IsShiftOpen { get; }

        /// <summary>Открыть новую смену</summary>
        void OpenShift();

        /// <summary>Закрыть текущую смену</summary>
        void CloseShift();

        /// <summary>Получить информацию о текущей смене</summary>
        ShiftInfo GetCurrentShiftInfo();
    }

    /// <summary>
    /// Сервис для продажи товаров
    /// </summary>
    public interface ISaleService
    {
        /// <summary>Оформить продажу товаров из корзины</summary>
        /// <param name="cart">Корзина с товарами</param>
        /// <param name="paymentType">Тип оплаты (cash/card)</param>
        void Sale(ObservableCollection<CartItem> cart, string paymentType);
    }

    /// <summary>
    /// Сервис для инвентаризации и управления складскими остатками
    /// </summary>
    public interface IInventoryService
    {
        /// <summary>Переместить партию в другое место хранения</summary>
        /// <param name="batchId">ID партии</param>
        /// <param name="newStorageLocation">Новое место хранения</param>
        void MoveBatch(int batchId, string newStorageLocation);

        /// <summary>Сравнить учётные и фактические остатки и скорректировать</summary>
        /// <param name="inventoryItems">Список товаров с фактическими остатками</param>
        void CompareAndCorrect(ObservableCollection<InventoryItem> inventoryItems);

        /// <summary>Получить список просроченных партий для списания</summary>
        ObservableCollection<Batch> GetExpiredBatches();

        /// <summary>Создать документ списания просроченных товаров</summary>
        /// <param name="expiredBatches">Список просроченных партий</param>
        void CreateWriteOffDocument(ObservableCollection<Batch> expiredBatches);
    }
}