using Microsoft.EntityFrameworkCore;

namespace AviAppFinal.Server.Models
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                // This will create a SQLite file called appdata.db in the app folder
                optionsBuilder.UseSqlite("Data Source=appdata.db");
            }
        }

        
    }
}
