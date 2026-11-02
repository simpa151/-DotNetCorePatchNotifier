# DotNetCorePatchNotifier

A lightweight Windows application built with **.NET 10 MAUI Blazor Hybrid** that monitors installed .NET Core runtimes and notifies you when security and maintenance patches are available.

## Features

- Detects installed `Microsoft.NETCore.App` runtimes.
- Checks official Microsoft .NET release metadata.
- Monitors for new runtime patches automatically.
- Displays Windows system tray notifications.
- Opens the official Microsoft .NET download page when a notification is clicked.
- Supports optional email notifications using Gmail or custom SMTP.
- Stores SMTP passwords using MAUI SecureStorage.
- Remembers notification history to avoid repeated alerts.
- Retries failed notifications.
- Persists settings and notification history using SQLite.

## Requirements

- Windows 10 or Windows 11
- A supported .NET runtime for the application
- Internet access to retrieve Microsoft release information
- Optional: Gmail App Password or custom SMTP credentials for email notifications

## How it works

DotNetCorePatchNotifier reads installed .NET runtimes using `dotnet --list-runtimes` and compares their versions with Microsoft's official release metadata.

The application checks automatically in the background, normally once every 24 hours, and uses a 15-minute monitoring interval for retries.

Only supported `Microsoft.NETCore.App` runtime channels are monitored. Updates are not installed automatically.

## Email notifications

Open **Settings**, enable email notifications and choose either Gmail or Custom SMTP.

For Gmail, use your Gmail address and a Google App Password. The application does not require your normal Google account password.

Use **Send test email** to verify your configuration.

## Technologies

- C# / .NET 10
- .NET MAUI Blazor Hybrid
- Windows system tray integration
- Entity Framework Core
- SQLite
- MailKit
- Scrutor

## Platform support

Windows is the initial target platform. macOS support may be considered in a future release.

## Disclaimer

This project is an independent open-source utility and is not affiliated with or endorsed by Microsoft.

DotNetCorePatchNotifier does not download or install patches automatically.

## License

DotNetCorePatchNotifier is publicly available under the **PolyForm Noncommercial License 1.0.0**.

You may study, modify, and fork the project for permitted noncommercial purposes, including education, personal projects, and portfolio demonstrations.

Commercial use and selling the original or modified software are not permitted without separate permission from the copyright holder.

See [LICENSE](LICENSE) for the complete terms.