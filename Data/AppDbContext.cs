using DotNetCorePatchNotifier.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace DotNetCorePatchNotifier.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<AppSettings> AppSettings => Set<AppSettings>();
        public DbSet<PatchNotification> PatchNotifications => Set<PatchNotification>();
        public DbSet<EmailSettings> EmailSettings => Set<EmailSettings>();

        public DbSet<PatchEmailNotification> PatchEmailNotifications => Set<PatchEmailNotification>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PatchNotification>()
                .HasIndex(x => new { x.RuntimeName, x.Version })
                .IsUnique();

            modelBuilder.Entity<PatchEmailNotification>()
                .HasIndex(x => new { x.RuntimeName, x.Version })
                .IsUnique();
        }
    }
}
