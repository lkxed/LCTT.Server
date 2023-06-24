using Microsoft.EntityFrameworkCore;
using LCTT.Server.Models;

namespace LCTT.Server.Services;

public static class SQLiteService
{
    public class SQLiteContext : DbContext
    {
        public DbSet<URL> URLs { get; set; } = default !;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=Data/SQLite.db;");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<URL>().ToTable("URL");
        }
    }

    public static void AddURL(string url)
    {
        using var context = new SQLiteContext();
        url = url.TrimEnd('/');
        context.URLs.Add(new URL { Value = url });
        context.SaveChanges();
    }

    public static bool FindURL(string url)
    {
        using var context = new SQLiteContext();
        url = url.TrimEnd('/');
        return context.URLs.Where(URL => URL.Value == url).Any();
    }
}