using DotNetCorePatchNotifier.Models;
using MailKit.Security;
using MimeKit;
using MailKit.Net.Smtp;
using System;
using System.Collections.Generic;
using System.Text;

namespace DotNetCorePatchNotifier.Services
{
    public interface IEmailNotificationService
    {
        Task SendTestAsync(EmailSettings settings, string password, CancellationToken cancellationToken = default);
        Task SendPatchAsync(EmailSettings settings, string password, string runtimeName, string installedVersion, string latestVersion, string downloadUrl, CancellationToken cancellationToken = default);
    }

    public class EmailNotificationService : IEmailNotificationService
    {
        public async Task SendTestAsync(EmailSettings settings, string password, CancellationToken cancellationToken = default)
        {
            await SendAsync(settings, password,
                "DotNetCorePatchNotifier - Test Email",
                "Your email notification settings are working correctly.",
                cancellationToken);
        }

        public async Task SendPatchAsync(EmailSettings settings, string password, string runtimeName, string installedVersion, string latestVersion, string downloadUrl, CancellationToken cancellationToken = default)
        {
//#if DEBUG
//            if (latestVersion == "10.0.998-test")
//                throw new InvalidOperationException("TEST: Simulated SMTP failure.");
//#endif

            var body = $"""
            A new .NET Core runtime patch is available.

            Runtime: {runtimeName}
            Installed version: {installedVersion}
            Latest version: {latestVersion}

            Download:
            {downloadUrl}

            Sent by DotNetCorePatchNotifier.
            """;

            await SendAsync(settings, password,
                $".NET Core patch available - {latestVersion}",
                body,
                cancellationToken);
        }

        private static async Task SendAsync(EmailSettings settings, string password, string subject, string body, CancellationToken cancellationToken)
        {
            var gmail = settings.Provider.Equals("Gmail", StringComparison.OrdinalIgnoreCase);

            var host = gmail ? "smtp.gmail.com" : settings.SmtpHost.Trim();
            var port = gmail ? 587 : settings.SmtpPort;

            if (string.IsNullOrWhiteSpace(host) || port < 1 || port > 65535)
                throw new InvalidOperationException("Invalid SMTP server or port.");

            if (string.IsNullOrWhiteSpace(settings.SenderEmail) ||
                string.IsNullOrWhiteSpace(settings.RecipientEmail) ||
                string.IsNullOrWhiteSpace(settings.Username) ||
                string.IsNullOrWhiteSpace(password))
            {
                throw new InvalidOperationException("Complete the email addresses, username and password.");
            }

            var message = new MimeMessage();
            message.From.Add(MailboxAddress.Parse(settings.SenderEmail.Trim()));
            message.To.Add(MailboxAddress.Parse(settings.RecipientEmail.Trim()));
            message.Subject = subject;
            message.Body = new TextPart("plain") { Text = body };

            using var client = new SmtpClient();
            client.Timeout = 15000;

            var security = port == 465
                ? SecureSocketOptions.SslOnConnect
                : SecureSocketOptions.StartTls;

            await client.ConnectAsync(host, port, security, cancellationToken);
            await client.AuthenticateAsync(settings.Username.Trim(), password, cancellationToken);
            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);
        }
    }

}
