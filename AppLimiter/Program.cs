using AppLimiter;
using AppLimiterLibrary.Data;
using Serilog;
using Serilog.Events;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        // Configure logging
        var logsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AppLimiter",
            "Logs"
        );
        Directory.CreateDirectory(logsPath);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithEnvironmentName()
            .Enrich.WithMachineName()
            .Enrich.WithProperty("ComputerId", ComputerIdentifier.GetUniqueIdentifier())
            .WriteTo.File(
                path: Path.Combine(logsPath, "app-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}"
            )
            .CreateLogger();

        try
        {
            Log.Information("Starting AppLimiter application");

            // Configure services
            builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            builder.Logging.ClearProviders();
            builder.Logging.AddSerilog();

            // Add required services
            builder.Services.AddSingleton<IWebSocketCommunicator, WebSocketServerService>();
            builder.Services.AddHostedService(sp => (WebSocketServerService)sp.GetRequiredService<IWebSocketCommunicator>());
            builder.Services.AddSingleton<WebsiteTracker>();
            builder.Services.AddHostedService<Worker>();


            var host = builder.Build();
            DatabaseManager.Initialize(host.Services.GetRequiredService<IConfiguration>());
            await host.RunAsync();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
        }
        finally
        {
            Log.Information("Shutting down AppLimiter application");
            Log.CloseAndFlush();
        }
    }
}