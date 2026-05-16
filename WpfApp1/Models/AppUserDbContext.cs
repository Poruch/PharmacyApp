using Microsoft.EntityFrameworkCore;
using PharmacyApp.Models;

public class AppUserDbContext : DbContext
{
    public DbSet<AppUser> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer(ConfigManager.ConnectionString);
    }
}