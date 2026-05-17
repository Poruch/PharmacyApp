using System;
using System.Windows;
using PharmacyApp.Services;

namespace PharmacyApp.Views
{
    public partial class LoginRegisterWindow : Window
    {
        bool _isDevelope = true;
        static bool _isFirstRequest = true;
        public LoginRegisterWindow()
        {
            InitializeComponent();
            Loaded += OnLoaded;

        }
        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            await AuthenticationService.EnsureFirstAdminCreatedAsync();
            if (_isDevelope && _isFirstRequest)
            {
                bool success = await AuthenticationService.LoginAsync("admin", "admin123");
                if (success)
                {
                    DialogResult = true;
                    _isFirstRequest = false;
                    Close();
                    return;
                }
            }
        }
        private async void Login_Click(object sender, RoutedEventArgs e)
        {
            string login = txtLogin.Text.Trim();
            string password = txtPassword.Password;

            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            {
                ShowError("Введите логин и пароль.");
                return;
            }

            bool success = await AuthenticationService.LoginAsync(login, password);
            if (success)
            {
                DialogResult = true;
                Close();
            }
            else
            {
                ShowError("Неверный логин, пароль или пользователь заблокирован.");
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void ShowError(string message)
        {
            txtError.Text = message;
            txtError.Visibility = Visibility.Visible;
        }
    }
}