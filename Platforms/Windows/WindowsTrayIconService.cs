using DotNetCorePatchNotifier.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Windowing;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using WinRT.Interop;

namespace DotNetCorePatchNotifier.Platforms.Windows
{
    public sealed class WindowsTrayIconService : ITrayIconService
    {
        private const uint NIM_ADD = 0;
        private const uint NIM_DELETE = 2;
        private const uint NIF_MESSAGE = 1;
        private const uint NIF_ICON = 2;
        private const uint NIF_TIP = 4;
        private const uint WM_APP_TRAY = 0x8001;
        private const uint WM_LBUTTONDBLCLK = 0x0203;
        private const uint WM_RBUTTONUP = 0x0205;
        private const uint WM_COMMAND = 0x0111;
        private const uint MF_STRING = 0;
        private const uint TPM_RIGHTBUTTON = 2;
        private const int SW_RESTORE = 9;
        private const uint ICON_ID = 1;
        private const uint OPEN_ID = 1001;
        private const uint EXIT_ID = 1002;

        private const uint NIM_MODIFY = 1;
        private const uint NIF_INFO = 0x10;
        private const uint NIIF_INFO = 1;


        private const uint MINIMIZE_ID = 1003;
        private const int SW_MINIMIZE = 6;

        private const uint NIN_BALLOONUSERCLICK = 0x0405;
        private string? _notificationDownloadUrl;

        private IntPtr _hwnd;
        private Microsoft.UI.Xaml.Window? _window;
        private AppWindow? _appWindow;
        private SubclassProc? _callback;
        private bool _initialized;
        private bool _exiting;

        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<WindowsTrayIconService> _logger;

        public WindowsTrayIconService(IServiceProvider serviceProvider, ILogger<WindowsTrayIconService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct NotifyIconData
        {
            public uint cbSize;
            public IntPtr hWnd;
            public uint uID;
            public uint uFlags;
            public uint uCallbackMessage;
            public IntPtr hIcon;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string szTip;
            public uint dwState;
            public uint dwStateMask;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string szInfo;
            public uint uTimeoutOrVersion;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
            public string szInfoTitle;
            public uint dwInfoFlags;
            public Guid guidItem;
            public IntPtr hBalloonIcon;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Point
        {
            public int X;
            public int Y;
        }

        private delegate IntPtr SubclassProc(
            IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam,
            UIntPtr subclassId, IntPtr refData);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern bool Shell_NotifyIcon(uint message, ref NotifyIconData data);

        [DllImport("user32.dll")]
        private static extern IntPtr LoadIcon(IntPtr instance, IntPtr iconName);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hwnd, int command);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hwnd);

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out Point point);

        [DllImport("user32.dll")]
        private static extern IntPtr CreatePopupMenu();

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern bool AppendMenu(
            IntPtr menu, uint flags, uint id, string text);

        [DllImport("user32.dll")]
        private static extern bool TrackPopupMenu(
            IntPtr menu, uint flags, int x, int y,
            int reserved, IntPtr hwnd, IntPtr rect);

        [DllImport("user32.dll")]
        private static extern bool DestroyMenu(IntPtr menu);

        [DllImport("comctl32.dll")]
        private static extern bool SetWindowSubclass(
            IntPtr hwnd, SubclassProc callback, UIntPtr id, IntPtr data);

        [DllImport("comctl32.dll")]
        private static extern bool RemoveWindowSubclass(
            IntPtr hwnd, SubclassProc callback, UIntPtr id);

        [DllImport("comctl32.dll")]
        private static extern IntPtr DefSubclassProc(
            IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr LoadImage(
                                                IntPtr hInst,
                                                string name,
                                                uint type,
                                                int cx,
                                                int cy,
                                                uint fuLoad);


        private IntPtr _loadedIcon;

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DestroyIcon(IntPtr hIcon);

        public void Initialize()
        {
            if (_initialized)
                return;

            _window = Microsoft.Maui.Controls.Application.Current?
                .Windows.FirstOrDefault()?.Handler?.PlatformView
                as Microsoft.UI.Xaml.Window;

            if (_window == null)
                return;

            _appWindow = _window.AppWindow;
            _hwnd = WindowNative.GetWindowHandle(_window);

            _callback = WindowProcedure;

            if (!SetWindowSubclass(_hwnd, _callback, UIntPtr.Zero, IntPtr.Zero))
                throw new InvalidOperationException("Unable to initialize tray window.");

            _appWindow.Closing += OnClosing;

            var data = CreateIconData();

            data.uFlags = NIF_MESSAGE | NIF_ICON | NIF_TIP;
            data.uCallbackMessage = WM_APP_TRAY;
            //data.hIcon = LoadIcon(IntPtr.Zero, new IntPtr(32512));

            string iconPath = Path.Combine(
                                            AppContext.BaseDirectory,
                                            "Platforms",
                                            "Windows",
                                            "Icons",
                                            "DotNetCorePatchNotifier.ico");

            _loadedIcon = LoadImage(
                                    IntPtr.Zero,
                                    iconPath,
                                    1,
                                    0,
                                    0,
                                    0x00000010);

            if (_loadedIcon == IntPtr.Zero)
            {
                throw new InvalidOperationException(
                    $"Failed to load tray icon: {iconPath}");
            }

            data.hIcon = _loadedIcon;

            if (!Shell_NotifyIcon(NIM_ADD, ref data))
            {
                _appWindow.Closing -= OnClosing;
                RemoveWindowSubclass(_hwnd, _callback, UIntPtr.Zero);
                throw new InvalidOperationException("Unable to create tray icon.");
            }

            _initialized = true;
        }

        private NotifyIconData CreateIconData()
        {
            return new NotifyIconData
            {
                cbSize = (uint)Marshal.SizeOf<NotifyIconData>(),
                hWnd = _hwnd,
                uID = ICON_ID,
                szTip = "DotNetCorePatchNotifier",
                szInfo = string.Empty,
                szInfoTitle = string.Empty
            };
        }

        private void OnClosing(AppWindow sender, AppWindowClosingEventArgs args)
        {
            if (_exiting)
                return;

            args.Cancel = true;
            HideWindow();
        }

        private IntPtr WindowProcedure(
            IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam,
            UIntPtr subclassId, IntPtr refData)
        {
            if (msg == WM_APP_TRAY)
            {
                var action = unchecked((uint)lParam.ToInt64()) & 0xFFFF;

                if (action == WM_LBUTTONDBLCLK)
                    ShowWindow();
                else if (action == WM_RBUTTONUP)
                    ShowMenu();
                else if (action == NIN_BALLOONUSERCLICK && !string.IsNullOrEmpty(_notificationDownloadUrl))
                {
                    _ = Launcher.Default.OpenAsync(_notificationDownloadUrl);
                }

                return IntPtr.Zero;
            }

            if (msg == WM_COMMAND)
            {
                var command = unchecked((uint)wParam.ToInt64()) & 0xFFFF;

                if (command == MINIMIZE_ID)
                {
                    ShowWindow(_hwnd, SW_MINIMIZE);
                    return IntPtr.Zero;
                }

                if (command == OPEN_ID)
                {
                    ShowWindow();
                    return IntPtr.Zero;
                }

                if (command == EXIT_ID)
                {
                    ExitApplication();
                    return IntPtr.Zero;
                }
            }

            return DefSubclassProc(hwnd, msg, wParam, lParam);
        }

        private void ShowMenu()
        {
            var menu = CreatePopupMenu();

            if (menu == IntPtr.Zero)
                return;

            try
            {
                AppendMenu(menu, MF_STRING, MINIMIZE_ID, "Minimize");
                AppendMenu(menu, MF_STRING, OPEN_ID, "Open");
                AppendMenu(menu, MF_STRING, EXIT_ID, "Exit");

                GetCursorPos(out var point);
                SetForegroundWindow(_hwnd);

                TrackPopupMenu(
                    menu, TPM_RIGHTBUTTON,
                    point.X, point.Y, 0, _hwnd, IntPtr.Zero);
            }
            finally
            {
                DestroyMenu(menu);
            }
        }

        public void ShowWindow()
        {
            if (_appWindow == null)
                return;

            _appWindow.Show();
            ShowWindow(_hwnd, SW_RESTORE);
            _window?.Activate();
            SetForegroundWindow(_hwnd);
        }

        public void HideWindow()
        {
            _appWindow?.Hide();
        }

        //public void ExitApplication()
        //{
        //    _exiting = true;
        //    Dispose();
        //    Microsoft.UI.Xaml.Application.Current.Exit();
        //}
        public void ExitApplication()
        {
            if (_exiting)
                return;

            _exiting = true;

            _ = ExitApplicationAsync();
        }

        private async Task ExitApplicationAsync()
        {
            try
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));

                var hostedServices = _serviceProvider.GetServices<IHostedService>();

                foreach (var service in hostedServices.Reverse())
                {
                    try
                    {
                        await service.StopAsync(timeout.Token);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to stop background service cleanly.");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during application shutdown.");
            }
            finally
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    Dispose();
                    Microsoft.UI.Xaml.Application.Current.Exit();
                });
            }
        }

        public void Dispose()
        {
            if (!_initialized)
                return;

            var data = CreateIconData();
            Shell_NotifyIcon(NIM_DELETE, ref data);

            if (_appWindow != null)
                _appWindow.Closing -= OnClosing;

            if (_callback != null)
                RemoveWindowSubclass(_hwnd, _callback, UIntPtr.Zero);

            _initialized = false;

            if (_loadedIcon != IntPtr.Zero)
            {
                DestroyIcon(_loadedIcon);
                _loadedIcon = IntPtr.Zero;
            }
        }

        public bool ShowPatchNotification(string runtimeName, string version)
        {
            if (!_initialized)
                return false;

            var data = CreateIconData();

            data.uFlags = NIF_INFO;
            data.szInfoTitle = ".NET Core patch available";
            data.szInfo = $"{runtimeName} {version} is available.";
            data.dwInfoFlags = NIIF_INFO;


            if (Version.TryParse(version, out var parsedVersion))
            {
                _notificationDownloadUrl =
                    $"https://dotnet.microsoft.com/en-us/download/dotnet/{parsedVersion.Major}.{parsedVersion.Minor}";
            }

            return Shell_NotifyIcon(NIM_MODIFY, ref data);
        }
    }
}
