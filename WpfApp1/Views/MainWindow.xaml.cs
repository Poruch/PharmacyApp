using PharmacyApp.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using PharmacyApp.Controls;
namespace PharmacyApp.Views
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            var loginWindow = new LoginRegisterWindow();
            if (loginWindow.ShowDialog() != true)
            {
                App.Current.Shutdown();
                return;
            }
            App.CurrentUser = AuthenticationService.CurrentUser;
            if (App.CurrentUser != null)
            {


            }
        }
        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var tab = (TabItem)e.AddedItems[0];
            switch (tab.Tag.ToString())
            {
                case "Manager":
                    MainContent.Content = new ManagerControl();
                    break;
                case "Pharmacist":
                    MainContent.Content = new PharmacistControl();
                    break;
                case "Cashier":
                    MainContent.Content = new CashierControl();
                    break;
            }
        }
        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            App.CurrentUser = null;
            var loginWindow = new LoginRegisterWindow();
            if (loginWindow.ShowDialog() == true)
            {
                App.CurrentUser = AuthenticationService.CurrentUser;
            }
            else
            {
                Application.Current.Shutdown();
            }
        }



        private void UsersButton_Click(object sender, RoutedEventArgs e)
        {

            var usersWindow = new UsersManagementWindow();
            usersWindow.Owner = this;
            usersWindow.ShowDialog();
        }

    }
}
