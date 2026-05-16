using PharmacyApp.Controls;
using PharmacyApp.Services;
using PharmacyApp.ViewModels;
using PharmacyApp.Views;
using System.Windows;
using System.Windows.Controls;

namespace PharmacyApp.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        ManagerViewModel.LogoutRequested = OnLogoutRequested;

        var loginWindow = new LoginRegisterWindow();
        if (loginWindow.ShowDialog() != true)
        {
            Application.Current.Shutdown();
            return;
        }

        App.CurrentUser = AuthenticationService.CurrentUser;
        ConfigureForRole(App.CurrentUser?.Role);
    }

    private void ConfigureForRole(string? role)
    {
        TxtCurrentUser.Text = App.CurrentUser?.FullName ?? "Гость";

        TabManager.Visibility = Visibility.Collapsed;
        TabPharmacist.Visibility = Visibility.Collapsed;
        TabCashier.Visibility = Visibility.Collapsed;
        TabCatalog.Visibility = Visibility.Collapsed;

        switch (role?.ToLowerInvariant())
        {
            case "manager":
            case "admin":
                TabManager.Visibility = Visibility.Visible;
                TabCatalog.Visibility = Visibility.Visible;
                MainTabs.SelectedItem = TabManager;
                TxtRoleHint.Text = role == "admin" ? "Администратор" : "Панель менеджера";
                break;
            case "provizor":
            case "pharmacist":
                TabPharmacist.Visibility = Visibility.Visible;
                TabCatalog.Visibility = Visibility.Visible;
                MainTabs.SelectedItem = TabPharmacist;
                TxtRoleHint.Text = "Панель провизора";
                break;
            case "cashier":
                TabCashier.Visibility = Visibility.Visible;
                MainTabs.SelectedItem = TabCashier;
                TxtRoleHint.Text = "Рабочее место кассира";
                break;
            default:
                TabCashier.Visibility = Visibility.Visible;
                MainTabs.SelectedItem = TabCashier;
                break;
        }

        // Демо: admin видит все вкладки
        if (role == "admin")
        {
            TabPharmacist.Visibility = Visibility.Visible;
            TabCashier.Visibility = Visibility.Visible;
        }
    }

    private void MainTabs_SelectionChanged(object sender, SelectionChangedEventArgs e) { }

    private void LogoutButton_Click(object sender, RoutedEventArgs e) => OnLogoutRequested();

    private void OnLogoutRequested()
    {
        App.CurrentUser = null;
        var loginWindow = new LoginRegisterWindow();
        if (loginWindow.ShowDialog() == true)
        {
            App.CurrentUser = AuthenticationService.CurrentUser;
            ConfigureForRole(App.CurrentUser?.Role);
        }
        else
        {
            Application.Current.Shutdown();
        }
    }
}
