using CommunityToolkit.Mvvm.Messaging;
using DVBTTelevizor.MAUI.Messages;
using LoggerService;
using Microsoft.UI.Xaml;
using SharpHook;
using System.Threading;
using Windows.Networking.Vpn;
using Windows.UI.Core;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace DVBTTelevizor.MAUI.WinUI
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : MauiWinUIApplication
    {
        private ILoggingService _loggingService = null;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            // TODO: add NLOG - app cannot have any argument
            _loggingService = new BasicLoggingService();
            this.InitializeComponent();


            var hook = new SharpHook.TaskPoolGlobalHook();
            hook.KeyPressed += Hook_KeyPressed; ;       // EventHandler<KeyboardHookEventArgs>

            Task.Run(async () =>
            {
                await hook.RunAsync();
            });

        }

        private void Hook_KeyPressed(object? sender, KeyboardHookEventArgs e)
        {
            var code = e.Data.KeyCode.ToString();
            var keyAction = KeyboardDeterminer.GetKeyAction(code);
            var res = new KeyDownMessage(e.Data.KeyCode.ToString());

            WeakReferenceMessenger.Default.Send(res);
        }

        protected override MauiApp CreateMauiApp()
        {
            var app = MauiProgram.CreateMauiApp();

            return app;
        }
    }

}
