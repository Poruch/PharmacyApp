using Microsoft.Extensions.Configuration;
using System.IO;

public static class ConfigManager
{
    public static string ConnectionString { get; private set; }

    static ConfigManager()
    {
        // Базовый путь к исполняемому файлу приложения
        string basePath = AppDomain.CurrentDomain.BaseDirectory;
        string jsonPath = Path.Combine(basePath, "appsettings.json");

        if (!File.Exists(jsonPath))
        {
            throw new FileNotFoundException($"Configuration file not found: {jsonPath}");
        }

        var builder = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);

        var config = builder.Build();
        ConnectionString = config.GetConnectionString("PharmacyDb");
    }
}