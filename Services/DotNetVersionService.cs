using DotNetCorePatchNotifier.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace DotNetCorePatchNotifier.Services
{
    public class DotNetVersionService : IDotNetVersionService
    {
        public async Task<List<InstalledRuntime>> GetInstalledRuntimesAsync()
        {
            var runtimes = new List<InstalledRuntime>();

            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "--list-runtimes",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo };

            process.Start();

            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                throw new Exception($"Failed to detect .NET runtimes: {error}");
            }

            foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = line.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length < 2)
                {
                    continue;
                }

                // Only monitor the .NET Core runtime
                if (!parts[0].Equals("Microsoft.NETCore.App", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                runtimes.Add(new InstalledRuntime
                {
                    Name = parts[0],
                    Version = parts[1]
                });
            }

            return runtimes;
        }
    }
}
