
using System.IO;
using System.Text.Json;
using PharmacyApp.Models;
using System.Text;
using System.Xml.Serialization;

namespace PharmacyApp.Services
{
    internal class ExportService
    {
        public static void ExportToJson(string filePath, IEnumerable<Category> categories)
        {
            var exportData = categories.Select(c => new
            {
            });

            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(exportData, options);
            File.WriteAllText(filePath, json, Encoding.UTF8);
        }
        public static void ExportToXml(string filePath, IEnumerable<Category> categories)
        {
            var categoriesList = categories.ToList();
            var overrides = new XmlAttributeOverrides();
            var itemAttrs = new XmlAttributes { XmlIgnore = true };
            overrides.Add(typeof(Item), "Category", itemAttrs);
            var batchAttrs = new XmlAttributes { XmlIgnore = true };
            overrides.Add(typeof(Batch), "Item", batchAttrs);
            var serializer = new XmlSerializer(typeof(List<Category>), overrides);

            using var writer = new StreamWriter(filePath, false, Encoding.UTF8);
            serializer.Serialize(writer, categoriesList);
        }
    }
}
