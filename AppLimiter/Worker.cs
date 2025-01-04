using AppLimiterLibrary.Data;
using AppLimiterLibrary.Dtos;
using System.Diagnostics;

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
            _logger.LogInformation("Worker service started for computer {computerId}", _computerId);

            try
            {
                await LoadAndApplyLimits(); // Initial load

                while (!stoppingToken.IsCancellationRequested)
                {
                    using (_logger.BeginScope(new Dictionary<string, object>
                    {
                        ["MonitoringCycle"] = DateTime.UtcNow
                    }))
                    {
                        var startTime = DateTime.UtcNow;

                        await Task.WhenAll(
                            Task.Run(() => TrackAppUsage()),
                            EnforceUsageLimits()
                        );

                        var executionTime = DateTime.UtcNow - startTime;

                        if (executionTime > TimeSpan.FromSeconds(1))
                        {
                            _logger.LogWarning("Monitoring cycle took longer than expected: {ExecutionTime}ms",
                                                        executionTime.TotalMilliseconds);
                        }

                        var delayTime = TimeSpan.FromSeconds(1) - executionTime;

                        if (delayTime > TimeSpan.Zero)
                        {
                            await Task.Delay(delayTime, stoppingToken);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Critical error in worker service");
                throw;
            }

        }

        private async Task LoadAndApplyLimits()
        {
            try
            {
                _logger.LogInformation("Loading application limits");

                var limits = await _appRepo.LoadAllLimits(_computerId);

                _appLimits.Clear();
                _processToExecutableMap.Clear();

                foreach (var limit in limits)
                {
                    _logger.LogDebug("Processing limit for {AppName}: Warning={Warning}, Kill={Kill}, Ignore={Ignore}", limit.Name, limit.WarningTime, limit.KillTime, limit.Ignore);

                    if (!limit.Ignore)
                    {
                        if (TimeSpan.TryParse(limit.KillTime, out TimeSpan killTime) && killTime > TimeSpan.Zero)
                        {
                            _appLimits[limit.Path.ToLower()] = killTime;
                        }
                        if (TimeSpan.TryParse(limit.WarningTime, out TimeSpan warningTime) && warningTime > TimeSpan.Zero)
                        {
                            _appLimits[limit.Path.ToLower() + "warning"] = warningTime;
                        }
                    }

                    string processName = Path.GetFileNameWithoutExtension(limit.Path);
                    _processToExecutableMap[processName] = limit.Path;
                }

                _logger.LogInformation("Successfully loaded {Count} application limits", limits.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading process limits");
                _appLimits.Clear();
            }
        }

        private void TrackAppUsage()
        {
            try
            {
                var processNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var activeProcesses = Process.GetProcesses()
                    .Where(p => _processToExecutableMap.ContainsKey(p.ProcessName))
                    .ToList();

                _logger.LogDebug("Tracking usage for {Count} monitored processes", activeProcesses.Count);

                foreach (var process in activeProcesses)
                {
                    if (processNames.Add(process.ProcessName) &&
                        _processToExecutableMap.TryGetValue(process.ProcessName, out string executable))
                    {
                        var matchingLimit = _appLimits.Keys
                            .FirstOrDefault(k => k.Equals(executable, StringComparison.OrdinalIgnoreCase));

                        if (matchingLimit != null)
                        {
                            _appUsage.TryAdd(matchingLimit, TimeSpan.Zero);
                            _appUsage.TryAdd(matchingLimit + "warning", TimeSpan.Zero);
                            _appUsage[matchingLimit] += TimeSpan.FromSeconds(1);
                            _appUsage[matchingLimit + "warning"] += TimeSpan.FromSeconds(1);

                            _logger.LogDebug("Updated usage for {ProcessName}: {Usage}",
                                process.ProcessName, _appUsage[matchingLimit]);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error tracking app usage");
            }
        }

        private async Task EnforceUsageLimits()
        {
            foreach (var app in _appUsage.Keys.ToList())
            {
                try
                {
                    string baseApp = app.EndsWith("warning") ? app[..^7] : app;

                    if (!_ignoreStatusCache.TryGetValue(baseApp, out bool isIgnored))
                    {
                        isIgnored = await _appRepo.CheckIgnoreStatus(baseApp);
                        _ignoreStatusCache[baseApp] = isIgnored;
                    }

                    if (_appLimits.TryGetValue(app, out TimeSpan limit) &&
                        _appUsage[app] >= limit &&
                        !_ignoreStatusCache[baseApp])
                    {
                        if (app.EndsWith("warning"))
                        {
                            if (!_shownWarnings.Contains(baseApp))
                            {
                                _logger.LogInformation(
                                    "Warning threshold reached for {AppName}. Usage: {Usage}, Limit: {Limit}",
                                    baseApp, _appUsage[app], limit);

                                ShowWarningMessage(baseApp);
                                _shownWarnings.Add(baseApp);
                            }
                        }
                        else
                        {
                            _logger.LogWarning(
                                "Usage limit exceeded for {AppName}. Usage: {Usage}, Limit: {Limit}. Terminating process.",
                                baseApp, _appUsage[app], limit);

                            var processName = _processToExecutableMap
                                .FirstOrDefault(x => x.Value.Equals(baseApp, StringComparison.OrdinalIgnoreCase))
                                .Key;

                            if (!string.IsNullOrEmpty(processName))
                            {
                                await TerminateProcess(processName);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error enforcing limits for {AppName}", app);
                }
            }
        }

        private async Task TerminateProcess(string processName)
        {
            try
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
                            _logger.LogInformation("Successfully terminated process {ProcessName}", process.ProcessName);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to terminate process {ProcessName}", process.ProcessName);
                        }
                    }
                }

                // Reset usage tracking
                var executable = _processToExecutableMap[processName];
                _shownWarnings.Remove(executable);
                _appUsage[executable] = TimeSpan.Zero;
                _appUsage[executable + "warning"] = TimeSpan.Zero;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in process termination for {ProcessName}", processName);
                throw;
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
                        var app = new LimiterMessaging.WPF.App(); // Use your actual App class instead
                        app.InitializeComponent();

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
                        _logger.LogError(ex, "Error initializing messaging window");
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
