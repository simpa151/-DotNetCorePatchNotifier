using DotNetCorePatchNotifier.Data;
using DotNetCorePatchNotifier.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace DotNetCorePatchNotifier.Services
{
    public class PatchMonitorService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<PatchMonitorService> _logger;
        private readonly ITrayIconService _trayIconService;

        private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(24);
        private static readonly TimeSpan RetryInterval = TimeSpan.FromMinutes(15);

        //private const bool TestPatchNotifications = true; // TEST ONLY

        public PatchMonitorService(IServiceScopeFactory scopeFactory, ILogger<PatchMonitorService> logger, ITrayIconService trayIconService)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _trayIconService = trayIconService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();

                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    var settings = await db.AppSettings
                        .AsNoTracking()
                        .FirstOrDefaultAsync(stoppingToken);

                    var lastCheckUTC = settings?.LastCheckUTC;

                    if (!lastCheckUTC.HasValue ||
                        DateTime.UtcNow - lastCheckUTC.Value >= CheckInterval)
                    {
                        await CheckPatchesAsync(stoppingToken);
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Patch monitoring failed. Will retry.");
                }

                try
                {
                    await Task.Delay(RetryInterval, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }

        private async Task CheckPatchesAsync(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();

            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var checker = scope.ServiceProvider.GetRequiredService<IPatchCheckService>();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailNotificationService>();

            var settings = await db.AppSettings.FirstOrDefaultAsync(cancellationToken);

            if (settings == null)
            {
                settings = new AppSettings();
                db.AppSettings.Add(settings);
            }

            var emailSettings = await db.EmailSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(cancellationToken);

            var results = await checker.CheckAsync();
            var deliveryFailed = false;

//#if DEBUG
//            if (TestPatchNotifications)
//            {
//                results = results
//                    .Select(x => x.Name == "Microsoft.NETCore.App"
//                        ? new RuntimePatchStatus
//                        {
//                            Name = x.Name,
//                            InstalledVersion = x.InstalledVersion,
//                            LatestVersion = "10.0.998-test",
//                            SupportPhase = x.SupportPhase,
//                            UpdateAvailable = true,
//                            DownloadUrl = "https://dotnet.microsoft.com/en-us/download/dotnet/10.0",
//                            ReleaseNotesUrl = x.ReleaseNotesUrl
//                        }
//                        : x)
//                    .ToList();
//            }
//#endif

            foreach (var patch in results.Where(x =>
                         x.Name == "Microsoft.NETCore.App" &&
                         x.UpdateAvailable))
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (settings.NotificationsEnabled)
                {
                    var alreadyNotified = await db.PatchNotifications.AnyAsync(
                        x => x.RuntimeName == patch.Name &&
                             x.Version == patch.LatestVersion,
                        cancellationToken);

                    if (!alreadyNotified)
                    {
                        var notificationSent = false;

                        try
                        {
                            await MainThread.InvokeOnMainThreadAsync(() =>
                            {
                                notificationSent = _trayIconService.ShowPatchNotification(
                                    patch.Name, patch.LatestVersion);
                            });
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Windows notification failed.");
                        }

                        if (notificationSent)
                        {
                            db.PatchNotifications.Add(new PatchNotification
                            {
                                RuntimeName = patch.Name,
                                Version = patch.LatestVersion,
                                NotifiedAtUTC = DateTime.UtcNow
                            });

                            await db.SaveChangesAsync(cancellationToken);
                        }
                        else
                        {
                            deliveryFailed = true;
                        }
                    }
                }

                if (emailSettings?.Enabled == true)
                {
                    var alreadyEmailed = await db.PatchEmailNotifications.AnyAsync(
                        x => x.RuntimeName == patch.Name &&
                             x.Version == patch.LatestVersion,
                        cancellationToken);

                    if (!alreadyEmailed)
                    {
                        try
                        {
                            var password = await SecureStorage.Default.GetAsync(
                                "DotNetCorePatchNotifier.SMTP.Password");

                            if (string.IsNullOrWhiteSpace(password))
                                throw new InvalidOperationException("SMTP password is missing.");

                            await emailService.SendPatchAsync(
                                emailSettings,
                                password,
                                patch.Name,
                                patch.InstalledVersion,
                                patch.LatestVersion,
                                patch.DownloadUrl,
                                cancellationToken);

                            db.PatchEmailNotifications.Add(new PatchEmailNotification
                            {
                                RuntimeName = patch.Name,
                                Version = patch.LatestVersion,
                                SentAtUTC = DateTime.UtcNow
                            });

                            await db.SaveChangesAsync(cancellationToken);

                            _logger.LogInformation(
                                "Patch email sent for {Runtime} {Version}",
                                patch.Name, patch.LatestVersion);
                        }
                        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                        {
                            throw;
                        }
                        catch (Exception ex)
                        {
                            deliveryFailed = true;
                            //_logger.LogWarning(ex,
                            //    "Email notification failed for {Runtime} {Version}. Will retry.",
                            //    patch.Name, patch.LatestVersion);
                        }
                    }
                }
            }

            if (!deliveryFailed)
            {
                settings.LastCheckUTC = DateTime.UtcNow;
                await db.SaveChangesAsync(cancellationToken);


                //        //_logger.LogInformation(
                //        //    "Patch check completed successfully at {TimeUTC}",
                //        //    settings.LastCheckUTC);


                //        //_logger.LogInformation("Patch check completed successfully at {Time}",
                //        //                        settings.LastCheckUTC);

                //        //System.Diagnostics.Debug.WriteLine($"DotNetCorePatchNotifier: LastCheckUTC = {settings.LastCheckUTC:O}");



                //        //_logger.LogInformation(
                //        //"Patch notification sent: {Runtime} {Installed} -> {Latest}",
                //        //patch.Name,
                //        //patch.InstalledVersion,
                //        //patch.LatestVersion);
            }
        }

    }
}
