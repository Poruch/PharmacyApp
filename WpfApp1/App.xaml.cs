using PharmacyApp.Models;
using PharmacyApp.Services;
using System.Configuration;
using System.Data;
using System.Windows;

namespace PharmacyApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static AppUser CurrentUser { get; set; }
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            DatabaseInitializer.DropAllTables();
            DatabaseInitializer.EnsureDatabaseCreated();

        }
    }

}
