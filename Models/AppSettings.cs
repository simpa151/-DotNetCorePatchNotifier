using System;
using System.Collections.Generic;
using System.Text;

namespace DotNetCorePatchNotifier.Models
{
    public class AppSettings
    {
        public int Id { get; set; } = 1;
        public bool NotificationsEnabled { get; set; } = true;
        public string Email { get; set; } = string.Empty;
        public DateTime? LastCheckUTC { get; set; }
    }
}
