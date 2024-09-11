using System.Diagnostics;
using System.IO.Pipes;

namespace AppLimiter
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly Dictionary<string, TimeSpan> _appUsage = new Dictionary<string, TimeSpan>();
        private readonly Dictionary<string, TimeSpan> _appLimits = new Dictionary<string, TimeSpan>();
        private bool _warningShown = false;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
            _appLimits["leagueclient"] = TimeSpan.FromSeconds(10);
            _appLimits["leagueclientwarning"] = TimeSpan.FromMinutes(3);
            // Add more apps and their limits as needed
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                TrackAppUsage();
                EnforceUsageLimits();
                await Task.Delay(1000, stoppingToken); // Check every second
            }
        }

        private void TrackAppUsage()
        {
            var processes = Process.GetProcesses();
            foreach (var process in processes)
            {
                if (_appLimits.ContainsKey(process.ProcessName.ToLower()))
                {
                    if (!_appUsage.ContainsKey(process.ProcessName.ToLower()))
                    {
                        _appUsage[process.ProcessName.ToLower()] = TimeSpan.Zero;
                        _appUsage[process.ProcessName.ToLower()+"warning"] = TimeSpan.Zero;
                    }
                    _appUsage[process.ProcessName.ToLower()] += TimeSpan.FromSeconds(1);
                    _appUsage[process.ProcessName.ToLower() + "warning"] += TimeSpan.FromSeconds(1);
                }
            }
        }

        private async Task EnforceUsageLimits()
        {
            foreach (var app in _appUsage.Keys.ToList())
            {
                if (_appUsage[app] >= _appLimits[app])
                {
                    if(app.ToLower().Contains("warning"))
                    {
                        await ShowWarningMessage();
                        _warningShown = true;
                    }

                    var processes = Process.GetProcessesByName(app);
                    foreach (var process in processes)
                    {
                        try
                        {
                            process.Kill();
                            _logger.LogInformation($"Terminated {app} due to exceeded usage limit.");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Failed to terminate {app}.");
                        }
                    }
                }
            }
        }

        private async Task ShowWarningMessage()
        {
            string warningMessage = "WARNING: You have been playing League of Legends for 90 minutes. The application will close in 30 minutes.";
            _logger.LogWarning(warningMessage);

            using (var client = new NamedPipeClientStream(".", "LimiterMessagingPipe", PipeDirection.Out))
            {
                try
                {
                    await client.ConnectAsync(5000); // Wait up to 5 seconds
                    using (var writer = new StreamWriter(client))
                    {
                        await writer.WriteLineAsync(warningMessage);
                        await writer.FlushAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send message to LimiterMessaging application.");
                }
            }
        }
    }
}
