using System.Data.SqlClient;
using System.Diagnostics;
using System.IO.Pipes;
using System.Text.Json;
using LimiterMessaging.WPF;
using System.Windows.Forms;
using Microsoft.Extensions.Configuration;
using AppLimiterLibrary.Data;
using AppLimiterLibrary.Dtos;

namespace AppLimiter
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly AppRepository _appRepo;
        private readonly MotivationalMessageRepository _messageRepo;
        private readonly SettingsRepository _settingsRepository;
        private readonly Dictionary<string, TimeSpan> _appUsage = new Dictionary<string, TimeSpan>();
        private readonly Dictionary<string, TimeSpan> _appLimits = new Dictionary<string, TimeSpan>();
        private readonly Dictionary<string, string> _processToExecutableMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, bool> _ignoreStatusCache = new Dictionary<string, bool>();
        private readonly HashSet<string> _shownWarnings = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly string _computerId;
        public Worker(ILogger<Worker> logger, IConfiguration configuration)
        {
            _logger = logger;
            _appRepo = new AppRepository();
            _messageRepo = new MotivationalMessageRepository();
            _computerId = ComputerIdentifier.GetUniqueIdentifier();
            _settingsRepository = new SettingsRepository(_computerId);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await LoadAndApplyLimits(); // Initial load

            while (!stoppingToken.IsCancellationRequested)
            {
                var startTime = DateTime.UtcNow;

                await Task.WhenAll(
                    Task.Run(() => TrackAppUsage()),
                    EnforceUsageLimits()
                );

                var executionTime = DateTime.UtcNow - startTime;
                var delayTime = TimeSpan.FromSeconds(1) - executionTime;

                if (delayTime > TimeSpan.Zero)
                {
                    await Task.Delay(delayTime, stoppingToken);
                }
                else
                {
                    _logger.LogWarning($"Execution cycle took longer than 1 second: {executionTime.TotalMilliseconds}ms");
                }
            }
        }

        private async Task LoadAndApplyLimits()
        {
            try
            {
                var limits = await _appRepo.LoadAllLimits(_computerId);

                _appLimits.Clear();
                _processToExecutableMap.Clear();

                foreach (var limit in limits)
                {
                    if (!limit.Ignore)
                    {
                        if (TimeSpan.TryParse(limit.KillTime, out TimeSpan killTime) && killTime > TimeSpan.Zero)
                        {
                            _appLimits[limit.Executable.ToLower()] = killTime;
                        }
                        if (TimeSpan.TryParse(limit.WarningTime, out TimeSpan warningTime) && warningTime > TimeSpan.Zero)
                        {
                            _appLimits[limit.Executable.ToLower() + "warning"] = warningTime;
                        }
                    }

                    string processName = Path.GetFileNameWithoutExtension(limit.Executable);
                    _processToExecutableMap[processName] = limit.Executable;
                }
                _logger.LogInformation("Limits updated successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading process limits. Using empty limits.");
                _appLimits.Clear();
            }
        }

        private void TrackAppUsage()
        {
            var processNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var process in Process.GetProcesses())
            {
                if (processNames.Add(process.ProcessName) && _processToExecutableMap.TryGetValue(process.ProcessName, out string executable))
                {
                    var matchingLimit = _appLimits.Keys.FirstOrDefault(k => k.Equals(executable, StringComparison.OrdinalIgnoreCase));
                    if (matchingLimit != null)
                    {
                        _appUsage.TryAdd(matchingLimit, TimeSpan.Zero);
                        _appUsage.TryAdd(matchingLimit + "warning", TimeSpan.Zero);
                        _appUsage[matchingLimit] += TimeSpan.FromSeconds(1);
                        _appUsage[matchingLimit + "warning"] += TimeSpan.FromSeconds(1);
                    }
                }
            }
            _logger.LogInformation($"AppUsage updated at {DateTime.Now:HH:mm:ss fff}");
        }

        private async Task EnforceUsageLimits()
        {
            foreach (var app in _appUsage.Keys.ToList())
            {
                string baseApp = app.EndsWith("warning") ? app.Substring(0, app.Length - 7) : app; // Remove "warning" suffix

                if (!_ignoreStatusCache.TryGetValue(baseApp, out bool isIgnored))
                {
                    isIgnored = await _appRepo.CheckIgnoreStatus(baseApp);
                    _ignoreStatusCache[baseApp] = isIgnored;
                }

                if (_appLimits.TryGetValue(app, out TimeSpan limit) && _appUsage[app] >= limit && !_ignoreStatusCache[baseApp])
                {
                    if (app.EndsWith("warning"))
                    {
                        
                        if (!_shownWarnings.Contains(baseApp))
                        {
                            ShowWarningMessage(baseApp);
                            _shownWarnings.Add(baseApp);
                        }
                    }
                    else
                    {
                        var processName = _processToExecutableMap
                            .FirstOrDefault(x => x.Value.Equals(app, StringComparison.OrdinalIgnoreCase))
                            .Key;

                        if (!string.IsNullOrEmpty(processName))
                        {
                            var processes = Process.GetProcessesByName(processName);
                            HashSet<string> processedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                            foreach (var process in processes)
                            {
                                if (processedNames.Add(process.ProcessName))
                                {
                                    try
                                    {
                                        process.Kill();
                                        _logger.LogInformation($"Terminated {process.ProcessName} due to exceeded usage limit.");
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogError(ex, $"Failed to terminate {process.ProcessName}.");
                                    }
                                }
                            }

                            // Reset usage and warning status after attempting to kill all instances
                            _shownWarnings.Remove(app);
                            _appUsage[app] = TimeSpan.Zero;
                            _appUsage[app + "warning"] = TimeSpan.Zero;
                        }
                        else
                        {
                            _logger.LogWarning($"No process name found for executable: {app}");
                        }
                    }
                }
            }
        }

        private async void ShowWarningMessage(string executablePath)
        {
            var timeRemaining = _appLimits[executablePath] - _appLimits[executablePath + "warning"];
            var appName = Path.GetFileNameWithoutExtension(executablePath);

            await _appRepo.UpdateIgnoreStatus(appName, true);
            _ignoreStatusCache[executablePath] = true;

            string warning = timeRemaining >= TimeSpan.FromMinutes(1)
                ? $"WARNING: You have been using {appName} for an extended period. The application will close in {timeRemaining.Minutes} minutes once you select OK and usage continues."
                : $"WARNING: You have been using {appName} for an extended period. The application will close in {timeRemaining.Seconds} seconds once you select OK and usage continues.";

            var messages = await _messageRepo.GetMessagesForComputer(_computerId);

            Random r = new Random();
            var message = messages[r.Next(0, messages.Count)];

            _logger.LogWarning(string.IsNullOrEmpty(message.Message) ? message.FileName : message.Message);

            try
            {
                var tcs = new TaskCompletionSource<bool>();

                Thread thread = new Thread(() =>
                {
                    try
                    {
                        // Create and configure WPF application
                        var app = new System.Windows.Application();

                        var window = new LimiterMessaging.WPF.Views.MessagingWindow(
                            message,
                            warning,
                            appName,
                            _computerId,
                            _ignoreStatusCache,
                            _appRepo,
                            _messageRepo,
                            _settingsRepository,
                            null);

                        window.Closed += (s, e) =>
                        {
                            app.Shutdown();
                            tcs.SetResult(true);
                        };

                        app.Run(window);
                    }
                    catch (Exception ex)
                    {
                        tcs.SetException(ex);
                    }
                });

                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();

                await tcs.Task;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to show LimiterMessaging window.");
            }
        }
    }
}
