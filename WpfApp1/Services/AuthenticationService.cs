using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PharmacyApp.Models;
namespace PharmacyApp.Services
{
    public static class AuthenticationService
    {
        private static AppUser _currentUser;
        public static AppUser CurrentUser
        {
            get => _currentUser;
            private set => _currentUser = value;
        }

        /// <summary>
        /// Создаёт администратора по умолчанию, если в БД нет пользователей.
        /// </summary>
        public static async Task EnsureFirstAdminCreatedAsync()
        {
            using var db = new AppUserDbContext();
            bool adminExists = await db.Users.AnyAsync(u => u.Login == "admin");
            if (adminExists)
                return;

            string salt = PasswordHelper.GenerateSalt();
            string hash = PasswordHelper.HashPassword("admin123", salt);
            var admin = new AppUser
            {
                Login = "admin",
                PasswordHash = hash,
                Salt = salt,
                LastName = "Администратор",
                FirstName = "Системный",
                Patronymic = null,
                Role = "admin",
                IsActive = true,
                RegistrationDate = DateTime.Now
            };
            db.Users.Add(admin);
            await db.SaveChangesAsync();
        }

        /// <summary>
        /// Попытка входа. Возвращает true при успехе, иначе false.
        /// </summary>
        public static async Task<bool> LoginAsync(string login, string password)
        {
            using var db = new AppUserDbContext();
            var user = await db.Users.FirstOrDefaultAsync(u => u.Login == login);
            if (user == null || !user.IsActive || !PasswordHelper.VerifyPassword(password, user.Salt, user.PasswordHash))
                return false;
            _currentUser = user;
            return true;
        }

        /// <summary>
        /// Выход из системы.
        /// </summary>
        public static void Logout()
        {
            _currentUser = null;
        }

        /// <summary>
        /// Проверка, аутентифицирован ли пользователь.
        /// </summary>
        public static bool IsAuthenticated => _currentUser != null;

        /// <summary>
        /// Проверка, является ли текущий пользователь администратором.
        /// </summary>
        public static bool IsAdmin => _currentUser?.Role == "admin";
    }
}