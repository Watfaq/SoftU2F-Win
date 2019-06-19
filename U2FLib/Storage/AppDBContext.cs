using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using Microsoft.EntityFrameworkCore;

namespace U2FLib.Storage
{
    public class AppDbContext : DbContext
    {
        public AppDbContext() { }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var dbPath = Environment.GetEnvironmentVariable("DBPath");
            optionsBuilder.UseSqlite($"Filename = {dbPath}");
        }

        public DbSet<KeyPair> KeyPairs { get; set; }
        public DbSet<ApplicationData> ApplicationDatum { get; set; }
    }

    public class ApplicationData
    {
        public uint Id { get; set; }
        public UInt32 Counter { get; set; }
    }
}
