// HourglassMaui/MauiProgram.cs
using HourglassLibrary.Data;
using HourglassLibrary.Interfaces;
using HourglassLibrary.Services;
using HourglassMaui.Services;
using HourglassMaui.ViewModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace HourglassMaui
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            // Set up configuration
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            // Register the IConfiguration instance with the dependency injection system
            builder.Services.AddSingleton<IConfiguration>(config);

            // Register services
            builder.Services.AddLogging(logging =>
            {
                logging.AddConsole();
            });
            builder.Services.AddSingleton<AppRepository>();
            builder.Services.AddSingleton<MotivationalMessageRepository>();
            builder.Services.AddSingleton<IWebsiteTracker, WebsiteTracker>();
            // Note: WebSocketCommunicator is not yet implemented, commenting out for now
            // builder.Services.AddSingleton<IWebSocketCommunicator, WebSocketCommunicator>();
            builder.Services.AddHostedService<UsageTrackingService>();
            builder.Services.AddSingleton<DashboardViewModel>();

            // Register platform-specific IUsageTracker implementations
            builder.Services.AddSingleton<IUsageTracker>(provider =>
            {
                var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
#if WINDOWS
                return new WindowsUsageTracker(loggerFactory.CreateLogger<WindowsUsageTracker>());
#elif ANDROID
                return new AndroidUsageTracker(loggerFactory.CreateLogger<AndroidUsageTracker>());
#elif IOS
                return new IosUsageTracker(loggerFactory.CreateLogger<IosUsageTracker>());
#elif MACCATALYST
                return new MacUsageTracker(loggerFactory.CreateLogger<MacUsageTracker>());
#else
                throw new PlatformNotSupportedException("Usage tracking not supported on this platform.");
#endif
            });

            // Initialize DatabaseManager with the configuration
            DatabaseManager.Initialize(config);

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}