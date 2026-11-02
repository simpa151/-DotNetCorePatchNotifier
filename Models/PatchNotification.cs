using System;
using System.Collections.Generic;
using System.Text;

namespace DotNetCorePatchNotifier.Models
{
    public class PatchNotification
    {
        public int Id { get; set; }
        public string RuntimeName { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public DateTime NotifiedAtUTC { get; set; }
    }
}
