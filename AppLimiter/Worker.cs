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
            _appLimits["leagueclient"] = TimeSpan.FromMinutes(150);
            _appLimits["leagueclientwarning"] = TimeSpan.FromMinutes(120);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                TrackAppUsage();
                EnforceUsageLimits();
                await Task.Delay(1000, stoppingToken);
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

        private void EnforceUsageLimits()
        {
            foreach (var app in _appUsage.Keys.ToList())
            {
                if (_appUsage[app] >= _appLimits[app])
                {
                    if(app.ToLower().Contains("warning") && !_warningShown)
                    {
                        ShowWarningMessage();
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

        private void ShowWarningMessage()
        {
            string warningMessage = "WARNING: You have been playing League of Legends for 90 minutes. The application will close in 30 minutes.";
            _logger.LogWarning(warningMessage);

            try
            {
                // Launch the WinForms application
                Process.Start(new ProcessStartInfo
                {
                    FileName = "..\\LimiterMessaging\\bin\\Debug\\net8.0-windows\\LimiterMessaging.exe",
                    Arguments = $"\"{warningMessage}\"",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to launch LimiterMessaging application.");
            }
        }
    }
}
