using DotNetCorePatchNotifier.Data;
using DotNetCorePatchNotifier.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using DotNetCorePatchNotifier.Platforms.Windows;


#if WINDOWS
using DotNetCorePatchNotifier.WinUI;
#endif

namespace DotNetCorePatchNotifier
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();

            builder.UseMauiApp<App>()
                                    .ConfigureFonts(fonts =>
                                    {
                                        fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                                    });

            builder.Services.AddMauiBlazorWebView();

            var databasePath = Path.Combine(
                                            FileSystem.AppDataDirectory,
                                            "DotNetCorePatchNotifier.db");

            //System.Diagnostics.Debug.WriteLine($"SQLite database: {databasePath}");

            builder.Services.AddDbContext<AppDbContext>(options =>
                                                                options.UseSqlite($"Data Source={databasePath}"));

            // Automatically register all services implementing interfaces.
            builder.Services.Scan(scan => scan
                    .FromAssemblyOf<IDotNetVersionService>()
                    .AddClasses(classes => classes.Where(type =>
                        type.Name.EndsWith("Service") &&
                        !typeof(IHostedService).IsAssignableFrom(type)))
                    .AsImplementedInterfaces()
                    .WithSingletonLifetime());

            // Automatically register all repositories implementing interfaces.
            builder.Services.Scan(scan => scan
                .FromAssemblyOf<IDotNetVersionService>()
                .AddClasses(classes => classes.Where(type => type.Name.EndsWith("Repository")))
                .AsImplementedInterfaces()
                .WithScopedLifetime());



            builder.Services.Scan(scan => scan
                .FromAssemblyOf<IDotNetVersionService>()
                .AddClasses(classes => classes.AssignableTo<BackgroundService>())
                .As<IHostedService>()
                .WithSingletonLifetime());

#if WINDOWS
            builder.Services.AddSingleton<ITrayIconService, WindowsTrayIconService>();
#endif


#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#endif



            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Database.EnsureCreated();

                //db.Database.ExecuteSqlRaw("""
                //                            CREATE TABLE IF NOT EXISTS EmailSettings (
                //                                Id INTEGER NOT NULL PRIMARY KEY,
                //                                Enabled INTEGER NOT NULL,
                //                                Provider TEXT NOT NULL,
                //                                SmtpHost TEXT NOT NULL,
                //                                SmtpPort INTEGER NOT NULL,
                //                                SenderEmail TEXT NOT NULL,
                //                                RecipientEmail TEXT NOT NULL,
                //                                Username TEXT NOT NULL
                //                            );
                //                            """);

                //db.Database.ExecuteSqlRaw("""
                //                            CREATE TABLE IF NOT EXISTS PatchEmailNotifications (
                //                                Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                //                                RuntimeName TEXT NOT NULL,
                //                                Version TEXT NOT NULL,
                //                                SentAtUTC TEXT NOT NULL
                //                            );
                //                            """);

                //db.Database.ExecuteSqlRaw("""
                //                            CREATE UNIQUE INDEX IF NOT EXISTS IX_PatchEmailNotifications_RuntimeName_Version
                //                            ON PatchEmailNotifications (RuntimeName, Version);
                //                            """);


               ////FOR TEST =>
                //db.Database.ExecuteSqlRaw("""
                //                            UPDATE AppSettings
                //                            SET LastCheckUTC = NULL
                //                            WHERE Id = 1;
                //                            """);
            }

            // Start background monitoring.
            // Start all automatically registered background services.
            foreach (var hostedService in app.Services.GetServices<IHostedService>())
            {
                _ = hostedService.StartAsync(CancellationToken.None);
            }

            return app;
        }
    }
}
