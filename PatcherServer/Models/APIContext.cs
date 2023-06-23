using Microsoft.EntityFrameworkCore;

namespace PatcherServer.Models
{
    public class APIContext : DbContext
    {
        public DbSet<Patcher> patchers { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) => optionsBuilder.UseSqlite(@"Data Source=patcherdatabase.db");

        public APIContext()
        {
            Database.EnsureCreated();
        }

    }
}
