using AdsAgregator.DAL.Database.Tables;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;

namespace AdsAgregator.DAL.Database
{
    public class AppDbContext : DbContext
    {
        public IConfigurationRoot Configuration { get; set; }

        public AppDbContext()
        {

        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            //optionsBuilder.UseSqlServer("Server=tcp:adsagregatorserver.database.windows.net,1433;Initial Catalog=adsagregatordb;Persist Security Info=False;User ID=Boss;Password=hello123.;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;");
            optionsBuilder.UseSqlServer($"Data Source={Environment.MachineName};Initial Catalog=AdsAgregatorLocalDb;Integrated Security=True;Persist Security Info=False;Pooling=False;MultipleActiveResultSets=False;Encrypt=False;TrustServerCertificate=False");
            base.OnConfiguring(optionsBuilder);
        }

        public DbSet<Ad> Ads { get; set; }
        public DbSet<SearchItem> SearchItems { get; set; }
        public DbSet<ApplicationUser> Users { get; set; }
        public DbSet<Log> Logs { get; set; }
    }
}
