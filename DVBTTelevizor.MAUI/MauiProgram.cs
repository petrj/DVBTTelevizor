using LibVLCSharp.MAUI;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls.Hosting;

namespace DVBTTelevizor.MAUI
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseLibVLCSharp()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            builder.Services.AddSingleton<MainViewModel>();
            builder.Services.AddSingleton<MainPage>();
            builder.Services.AddSingleton<LoggerProvider>();
            builder.Services.AddSingleton<PublicDirectoryProvider>();
            builder.Services.AddSingleton<DVBTTelevizorConfiguration>();
            builder.Services.AddSingleton<IPublicDirectoryProvider, PublicDirectoryProvider>();
            builder.Services.AddSingleton<ITVCConfiguration, DVBTTelevizorConfiguration>();
            builder.Services.AddSingleton<ILoggingProvider, LoggerProvider>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
