using System;
using System.Collections.Generic;
using System.Text;

namespace DotNetCorePatchNotifier.Models
{

    public class PatchEmailNotification
    {
        public int Id { get; set; }
        public string RuntimeName { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public DateTime SentAtUTC { get; set; }
    }
}
