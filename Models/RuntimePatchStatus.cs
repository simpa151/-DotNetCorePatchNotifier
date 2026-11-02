using System;
using System.Collections.Generic;
using System.Text;

namespace DotNetCorePatchNotifier.Models
{
    public class RuntimePatchStatus
    {
        public string Name { get; set; } = string.Empty;
        public string InstalledVersion { get; set; } = string.Empty;
        public string LatestVersion { get; set; } = string.Empty;
        public string SupportPhase { get; set; } = string.Empty;
        public bool UpdateAvailable { get; set; }

        public string DownloadUrl { get; set; } = string.Empty;
        public string ReleaseNotesUrl { get; set; } = string.Empty;
    }
}
