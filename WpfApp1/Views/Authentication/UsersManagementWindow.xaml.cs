using System;
using System.Windows;
using System.Windows.Controls;
using PharmacyApp.Models;
using PharmacyApp.Services;

namespace PharmacyApp.Views
{
    public partial class UsersManagementWindow : Window
    {
        public UsersManagementWindow()
        {
            InitializeComponent();
            Loaded += async (s, e) => await LoadUsersAsync();
        }

        private async Task LoadUsersAsync()
        {
            var users = await UserService.GetAllUsersAsync();
            lvUsers.ItemsSource = users;
        }

        private async void AddUser_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new UserEditWindow();
            if (dialog.ShowDialog() == true && dialog.EditedUser != null)
            {
                await UserService.AddUserAsync(dialog.EditedUser);
                await LoadUsersAsync();
            }
        }

        private async void EditUser_Click(object sender, RoutedEventArgs e)
        {
            var selected = lvUsers.SelectedItem as AppUser;
            if (selected == null)
            {
                MessageBox.Show("Выберите пользователя.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var dialog = new UserEditWindow(selected);
            if (dialog.ShowDialog() == true && dialog.EditedUser != null)
            {
                // Копируем изменения из dialog.EditedUser в selected
                selected.Login = dialog.EditedUser.Login;
                selected.LastName = dialog.EditedUser.LastName;
                selected.FirstName = dialog.EditedUser.FirstName;
                selected.Patronymic = dialog.EditedUser.Patronymic;
                selected.Role = dialog.EditedUser.Role;
                selected.IsActive = dialog.EditedUser.IsActive;
                if (!string.IsNullOrEmpty(dialog.EditedUser.PasswordHash))
                {
                    selected.PasswordHash = dialog.EditedUser.PasswordHash;
                    selected.Salt = dialog.EditedUser.Salt;
                }
                await UserService.UpdateUserAsync(selected);
                await LoadUsersAsync();
            }
        }

        private async void DeleteUser_Click(object sender, RoutedEventArgs e)
        {
            var selected = lvUsers.SelectedItem as AppUser;
            if (selected == null) return;
            if (await UserService.IsLastAdminAsync(selected.UserId))
            {
                MessageBox.Show("Нельзя удалить последнего администратора.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (MessageBox.Show($"Удалить пользователя {selected.Login}?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                await UserService.DeleteUserAsync(selected.UserId);
                await LoadUsersAsync();
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}