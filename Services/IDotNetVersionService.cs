using DotNetCorePatchNotifier.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace DotNetCorePatchNotifier.Services
{
    public interface IDotNetVersionService
    {
        Task<List<InstalledRuntime>> GetInstalledRuntimesAsync();
    }
}
