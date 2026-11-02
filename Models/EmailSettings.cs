using System;
using System.Collections.Generic;
using System.Text;

namespace DotNetCorePatchNotifier.Models
{
    public class EmailSettings
    {
        public int Id { get; set; } = 1;
        public bool Enabled { get; set; } = false;
        public string Provider { get; set; } = "Gmail";
        public string SmtpHost { get; set; } = "smtp.gmail.com";
        public int SmtpPort { get; set; } = 587;
        public string SenderEmail { get; set; } = string.Empty;
        public string RecipientEmail { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
    }
}
