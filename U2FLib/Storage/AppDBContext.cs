using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<KeyPair>()
                .Property(b => b.ApplicationTag)
                .HasField("_applicationTag").UsePropertyAccessMode(PropertyAccessMode.Field);
            modelBuilder.Entity<KeyPair>()
                .Property(b => b.PrivateKey)
                .HasField("_privateKey").UsePropertyAccessMode(PropertyAccessMode.Field);
            modelBuilder.Entity<KeyPair>()
                .Property(b => b.PublicKey)
                .HasField("_publicKey").UsePropertyAccessMode(PropertyAccessMode.Field);
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
