using Microsoft.Data.SqlClient;
using System.Collections.ObjectModel;
using PharmacyApp.Models;

namespace PharmacyApp.Services
{
    public class DataService
    {
        private string _connectionString = ConfigManager.ConnectionString;
        private readonly EntityRepository _repo;

        // Локальные коллекции, которые будут привязаны к TreeView
        public ObservableCollection<Category> Categories { get; private set; }
        public ObservableCollection<Item> Items { get; private set; }
        public ObservableCollection<Batch> Batches { get; private set; }

        public DataService()
        {
            _repo = new EntityRepository(_connectionString);
            // Загружаем все данные из БД при инициализации
            LoadAllData();
        }

        private void LoadAllData()
        {
            
        }

       
        // Если нужно получить элементы по родительскому ID (например, Items категории)
        public IEnumerable<Item> GetItemsByCategory(int categoryId)
        {
            return Items.Where(i => i.CategoryId == categoryId);
        }

        public IEnumerable<Batch> GetBatchesByItem(int itemId)
        {
            return Batches.Where(b => b.ItemId == itemId);
        }
    }
}