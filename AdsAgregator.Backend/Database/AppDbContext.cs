﻿using AdsAgregator.Backend.Database.Tables;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AdsAgregator.Backend.Database
{
    public class AppDbContext: DbContext
    {
        public IConfigurationRoot Configuration { get; set; }

        public AppDbContext()
        {
            var builder = new ConfigurationBuilder()
               .SetBasePath(Directory.GetCurrentDirectory())
               .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            Configuration = builder.Build();

        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(Configuration.GetConnectionString("DefaultConnection"));
            base.OnConfiguring(optionsBuilder);
        }

        public DbSet<Ad> Ads { get; set; }
        public DbSet<SearchItem> SearchItems { get; set; }
        public DbSet<ApplicationUser> Users { get; set; }
        public DbSet<Log> Logs { get; set; }

    }
}
