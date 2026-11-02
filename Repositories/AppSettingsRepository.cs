using DotNetCorePatchNotifier.Data;
using DotNetCorePatchNotifier.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace DotNetCorePatchNotifier.Repositories
{
    public interface IAppSettingsRepository
    {
        Task<AppSettings> GetAsync();
        Task SaveAsync(AppSettings settings);
    }



    public class AppSettingsRepository : IAppSettingsRepository
    {
        private readonly AppDbContext _db;

        public AppSettingsRepository(AppDbContext db)
        {
            _db = db;
        }

        public async Task<AppSettings> GetAsync()
        {
            var settings = await _db.AppSettings.FirstOrDefaultAsync();

            if (settings != null)
                return settings;

            settings = new AppSettings();
            _db.AppSettings.Add(settings);

            await _db.SaveChangesAsync();

            return settings;
        }

        public async Task SaveAsync(AppSettings settings)
        {
            var existing = await _db.AppSettings.FirstOrDefaultAsync();

            if (existing == null)
            {
                _db.AppSettings.Add(settings);
            }
            else
            {
                existing.NotificationsEnabled = settings.NotificationsEnabled;
                existing.Email = settings.Email;
            }

            await _db.SaveChangesAsync();
        }
    }
}
