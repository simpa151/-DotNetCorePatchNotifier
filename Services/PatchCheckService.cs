using DotNetCorePatchNotifier.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace DotNetCorePatchNotifier.Services
{

    public interface IPatchCheckService
    {
        Task<List<RuntimePatchStatus>> CheckAsync();
    }

    public class PatchCheckService : IPatchCheckService
    {
        private const string ReleasesUrl =
            "https://builds.dotnet.microsoft.com/dotnet/release-metadata/releases-index.json";

        private readonly IDotNetVersionService _dotNetVersionService;
        private readonly HttpClient _httpClient;

        public PatchCheckService(IDotNetVersionService dotNetVersionService)
        {
            _dotNetVersionService = dotNetVersionService;

            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };
        }

        public async Task<List<RuntimePatchStatus>> CheckAsync()
        {
            var installedRuntimes = await _dotNetVersionService.GetInstalledRuntimesAsync();

            using var response = await _httpClient.GetAsync(ReleasesUrl);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync();
            using var document = await JsonDocument.ParseAsync(stream);

            if (!document.RootElement.TryGetProperty("releases-index", out var channels) ||
                channels.ValueKind != JsonValueKind.Array)
            {
                throw new InvalidOperationException("Invalid Microsoft .NET release metadata.");
            }

            var results = new List<RuntimePatchStatus>();

            foreach (var runtime in installedRuntimes.Where(x =>
                         string.Equals(x.Name, "Microsoft.NETCore.App",
                             StringComparison.OrdinalIgnoreCase)))
            {
                if (!Version.TryParse(runtime.Version, out var installedVersion))
                    continue;

                var channelVersion = $"{installedVersion.Major}.{installedVersion.Minor}";

                foreach (var channel in channels.EnumerateArray())
                {
                    if (!channel.TryGetProperty("channel-version", out var channelProperty) ||
                        channelProperty.GetString() != channelVersion)
                    {
                        continue;
                    }

                    if (!channel.TryGetProperty("support-phase", out var supportProperty))
                        break;

                    var supportPhase = supportProperty.GetString() ?? string.Empty;

                    if (string.Equals(supportPhase, "eol", StringComparison.OrdinalIgnoreCase))
                        break;

                    if (!channel.TryGetProperty("latest-runtime", out var latestProperty))
                        break;

                    var latestVersionText = latestProperty.GetString();

                    if (!Version.TryParse(latestVersionText, out var latestVersion))
                        break;

                    results.Add(new RuntimePatchStatus
                    {
                        Name = runtime.Name,
                        InstalledVersion = runtime.Version,
                        LatestVersion = latestVersionText!,
                        SupportPhase = supportPhase,
                        UpdateAvailable = latestVersion > installedVersion,
                        DownloadUrl = $"https://dotnet.microsoft.com/en-us/download/dotnet/{channelVersion}",
                        ReleaseNotesUrl = $"https://github.com/dotnet/core/tree/main/release-notes/{channelVersion}"
                    });

                    break;
                }
            }

            return results
                .OrderByDescending(x => Version.Parse(x.InstalledVersion))
                .ThenBy(x => x.Name)
                .ToList();
        }
    }
}
