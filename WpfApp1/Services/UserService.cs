using Microsoft.EntityFrameworkCore;
using PharmacyApp.Models;

namespace PharmacyApp.Services
{
    public static class UserService
    {
        private static AppUserDbContext CreateDbContext() => new AppUserDbContext();

        public static async Task<List<AppUser>> GetAllUsersAsync()
        {
            using var db = CreateDbContext();
            return await db.Users.OrderBy(u => u.UserId).ToListAsync();
        }

        public static async Task AddUserAsync(AppUser user)
        {
            using var db = CreateDbContext();
            db.Users.Add(user);
            await db.SaveChangesAsync();
        }

        public static async Task UpdateUserAsync(AppUser user)
        {
            using var db = CreateDbContext();
            var existing = await db.Users.FindAsync(user.UserId);
            if (existing != null)
            {
                existing.Login = user.Login;
                existing.LastName = user.LastName;
                existing.FirstName = user.FirstName;
                existing.Patronymic = user.Patronymic;
                existing.Role = user.Role;
                existing.IsActive = user.IsActive;
                if (!string.IsNullOrEmpty(user.PasswordHash))
                {
                    existing.PasswordHash = user.PasswordHash;
                    existing.Salt = user.Salt;
                }
                await db.SaveChangesAsync();
            }
        }

        public static async Task DeleteUserAsync(int userId)
        {
            using var db = CreateDbContext();
            var user = await db.Users.FindAsync(userId);
            if (user != null)
            {
                // Проверка на последнего администратора
                if (user.Role == "admin" && await db.Users.CountAsync(u => u.Role == "admin") == 1)
                    throw new InvalidOperationException("Нельзя удалить последнего администратора.");
                db.Users.Remove(user);
                await db.SaveChangesAsync();
            }
        }

        public static async Task<bool> IsLastAdminAsync(int userId)
        {
            using var db = CreateDbContext();
            var user = await db.Users.FindAsync(userId);
            if (user?.Role != "admin") return false;
            return await db.Users.CountAsync(u => u.Role == "admin") == 1;
        }
    }
}