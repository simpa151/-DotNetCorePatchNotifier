using System;
using System.Collections.Generic;
using System.Text;

namespace DotNetCorePatchNotifier.Services
{
    public interface ITrayIconService : IDisposable
    {
        void Initialize();
        void ShowWindow();
        void HideWindow();
        void ExitApplication();
        bool ShowPatchNotification(string runtimeName, string version);
    }
}
