using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using Microsoft.EntityFrameworkCore;

namespace SoftU2F.Console.Storage
{
    public class AppDbContext : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite($"Filename={App.DBPath}");
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
