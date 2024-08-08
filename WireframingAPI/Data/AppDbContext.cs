using Microsoft.EntityFrameworkCore;
using WireframingAPI.Models;

namespace WireframingAPI.Data
{
    namespace EntityFrameworkProject.Data
    {
        public class AppDbContext : DbContext
        {
            public DbSet<User> Users { get; set; }
            private readonly IConfiguration _configuration;

            public AppDbContext(DbContextOptions<AppDbContext> options, IConfiguration configuration) : base(options)
            {
                _configuration = configuration;
            }

            protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            {
                if (!optionsBuilder.IsConfigured)
                {
                    // Get the connection string from appsettings.json
                    var connectionString = _configuration.GetConnectionString("DefaultConnection");
                    optionsBuilder.UseSqlServer(connectionString);
                }
            }

        }
    }
}
