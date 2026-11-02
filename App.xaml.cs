using DotNetCorePatchNotifier.Services;

namespace DotNetCorePatchNotifier
{
    public partial class App : Application
    {
        private readonly IServiceProvider _services;

        public App(IServiceProvider services)
        {
            InitializeComponent();
            _services = services;
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = new Window(new MainPage())
            {
                Title = "DotNetCorePatchNotifier"
            };

#if WINDOWS
            window.Created += (sender, args) =>
            {
                var tray = _services.GetRequiredService<ITrayIconService>();

                
                
                window.Dispatcher.Dispatch(() => tray.Initialize());


                //window.Dispatcher.Dispatch(() =>
                //{
                //    tray.Initialize();

                //    tray.ShowPatchNotification(
                //        "Microsoft.NETCore.App",
                //        "10.0.12");
                //});
            };
#endif

            return window;
        }
    }
}
