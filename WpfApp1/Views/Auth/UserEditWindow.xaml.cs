using System;
using System.Windows;
using System.Windows.Controls;
using PharmacyApp.Models;
using PharmacyApp.Services;

namespace PharmacyApp.Views
{
    public partial class UserEditWindow : Window
    {
        public AppUser? EditedUser { get; private set; }
        private bool _isEdit;

        public UserEditWindow(AppUser? existing = null)
        {
            InitializeComponent();
            if (existing != null)
            {
                _isEdit = true;
                txtLogin.Text = existing.Login;
                txtLastName.Text = existing.LastName;
                txtFirstName.Text = existing.FirstName;
                txtPatronymic.Text = existing.Patronymic;
                cmbRole.Text = existing.Role;
                chkIsActive.IsChecked = existing.IsActive;
                // пароль не заполняем
            }
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            string login = txtLogin.Text.Trim();
            string lastName = txtLastName.Text.Trim();
            string firstName = txtFirstName.Text.Trim();
            string patronymic = txtPatronymic.Text.Trim();
            string role = (cmbRole.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "pharmacist";
            bool isActive = chkIsActive.IsChecked == true;

            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(lastName) || string.IsNullOrEmpty(firstName))
            {
                MessageBox.Show("Логин, фамилия и имя обязательны.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string password = txtPassword.Password;
            if (!_isEdit && string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Пароль обязателен для нового пользователя.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string salt = null;
            string hash = null;
            if (!string.IsNullOrEmpty(password))
            {
                salt = PasswordHelper.GenerateSalt();
                hash = PasswordHelper.HashPassword(password, salt);
            }

            EditedUser = new AppUser
            {
                Login = login,
                LastName = lastName,
                FirstName = firstName,
                Patronymic = patronymic,
                Role = role,
                IsActive = isActive,
                Salt = salt,
                PasswordHash = hash,
                RegistrationDate = DateTime.Now
            };

            if (_isEdit && EditedUser != null && !string.IsNullOrEmpty(hash))
            {
                var user = await (Task<AppUser>)this.DataContext;
                EditedUser.UserId = user?.UserId ?? 0;
            }

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}