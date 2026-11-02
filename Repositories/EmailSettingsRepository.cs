using DotNetCorePatchNotifier.Data;
using DotNetCorePatchNotifier.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace DotNetCorePatchNotifier.Repositories
{
    public interface IEmailSettingsRepository
    {
        Task<EmailSettings> GetAsync();
        Task SaveAsync(EmailSettings settings);
    }

    public class EmailSettingsRepository : IEmailSettingsRepository
    {
        private readonly AppDbContext _db;

        public EmailSettingsRepository(AppDbContext db)
        {
            _db = db;
        }

        public async Task<EmailSettings> GetAsync()
        {
            var settings = await _db.EmailSettings.FirstOrDefaultAsync();

            if (settings != null)
                return settings;

            settings = new EmailSettings();
            _db.EmailSettings.Add(settings);

            await _db.SaveChangesAsync();

            return settings;
        }

        public async Task SaveAsync(EmailSettings settings)
        {
            var existing = await _db.EmailSettings.FirstOrDefaultAsync();

            if (existing == null)
            {
                _db.EmailSettings.Add(settings);
            }
            else
            {
                existing.Enabled = settings.Enabled;
                existing.Provider = settings.Provider;
                existing.SmtpHost = settings.SmtpHost;
                existing.SmtpPort = settings.SmtpPort;
                existing.SenderEmail = settings.SenderEmail;
                existing.RecipientEmail = settings.RecipientEmail;
                existing.Username = settings.Username;
            }

            await _db.SaveChangesAsync();
        }
    }
}
