using AppLimiterLibrary.Data;
using AppLimiterLibrary.Dtos;
using LimiterMessaging.WPF.Services;
using System.Diagnostics;

namespace AppLimiter
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly AppRepository _appRepo;
        private readonly MotivationalMessageRepository _messageRepo;
        private readonly SettingsRepository _settingsRepository;
        private readonly WebsiteTracker _websiteTracker;
        private readonly IWebSocketCommunicator _webSocketCommunicator;
        private readonly Dictionary<string, TimeSpan> _appUsage = new Dictionary<string, TimeSpan>();
        private readonly Dictionary<string, TimeSpan> _appLimits = new Dictionary<string, TimeSpan>();
        private readonly Dictionary<string, string> _processToPathMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, bool> _ignoreStatusCache = new Dictionary<string, bool>();
        private readonly HashSet<string> _shownWarnings = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly WarningWindowManager _warningManager = new();
        private readonly string _computerId;

        public Worker(
            ILogger<Worker> logger,
            IConfiguration configuration,
            WebsiteTracker websiteTracker,
            IWebSocketCommunicator webSocketCommunicator)
        {
            _logger = logger;
            _websiteTracker = websiteTracker;
            _webSocketCommunicator = webSocketCommunicator; 
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
                            Task.Run(() => TrackWebsiteUsage()),
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
                _logger.LogInformation("Loading application and website limits");

                var limits = await _appRepo.LoadAllLimits(_computerId);

                _appLimits.Clear();
                _processToPathMap.Clear();

                foreach (var limit in limits)
                {
                    _logger.LogDebug("Processing limit for {AppName}: Warning={Warning}, Kill={Kill}, Ignore={Ignore}",
                        limit.Name, limit.WarningTime, limit.KillTime, limit.Ignore);

                    if (!limit.Ignore)
                    {
                        // Handle both app paths and website URLs
                        string trackingKey = limit.Path.ToLower();

                        if (TimeSpan.TryParse(limit.KillTime, out TimeSpan killTime) && killTime > TimeSpan.Zero)
                        {
                            _appLimits[trackingKey] = killTime;
                        }
                        if (TimeSpan.TryParse(limit.WarningTime, out TimeSpan warningTime) && warningTime > TimeSpan.Zero)
                        {
                            _appLimits[trackingKey + "warning"] = warningTime;
                        }
                    }

                    // For websites, use the domain as the process name
                    string processName = limit.IsWebsite
                        ? limit.Path  // For websites, Path should be the domain
                        : Path.GetFileNameWithoutExtension(limit.Path);

                    _processToPathMap[processName] = limit.Path;
                }

                _logger.LogInformation("Successfully loaded {Count} limits", limits.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading limits");
                _appLimits.Clear();
            }
        }

        private void TrackAppUsage()
        {
            try
            {
                var processNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var activeProcesses = Process.GetProcesses()
                    .Where(p => _processToPathMap.ContainsKey(p.ProcessName))
                    .ToList();

                _logger.LogDebug("Tracking usage for {Count} monitored processes", activeProcesses.Count);

                foreach (var process in activeProcesses)
                {
                    if (processNames.Add(process.ProcessName) &&
                        _processToPathMap.TryGetValue(process.ProcessName, out string executable))
                    {
                        UpdateUsageTime(executable);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error tracking app usage");
            }
        }

        private void TrackWebsiteUsage()
        {
            try
            {
                var websiteLimits = _processToPathMap
                    .Where(kvp => Uri.IsWellFormedUriString(kvp.Value, UriKind.Absolute))
                    .Select(kvp => new {
                        OriginalUrl = kvp.Value,
                        Domain = GetDomainFromUrl(kvp.Value)
                    })
                    .ToList();

                foreach (var website in websiteLimits)
                {
                    if (_websiteTracker.IsDomainActive(website.Domain))
                    {
                        UpdateUsageTime(website.OriginalUrl);
                        _logger.LogDebug("Active website found: {Domain}", website.Domain);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error tracking website usage");
            }
        }

        private string GetDomainFromUrl(string url)
        {
            if (Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
            {
                return uri.Host.ToLower().Replace("www.", "");
            }
            return url.ToLower(); // Return as-is if not a valid URI
        }

        private void UpdateUsageTime(string key)
        {
            var matchingLimit = _appLimits.Keys
                .FirstOrDefault(k => k.Equals(key, StringComparison.OrdinalIgnoreCase));

            if (matchingLimit != null)
            {
                _appUsage.TryAdd(matchingLimit, TimeSpan.Zero);
                _appUsage.TryAdd(matchingLimit + "warning", TimeSpan.Zero);
                _appUsage[matchingLimit] += TimeSpan.FromSeconds(1);
                _appUsage[matchingLimit + "warning"] += TimeSpan.FromSeconds(1);

                _logger.LogDebug("Updated usage for {Key}: {Usage}",
                    key, _appUsage[matchingLimit]);
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
                            _logger.LogWarning("Usage limit exceeded for {AppName}. Usage: {Usage}, Limit: {Limit}", baseApp, _appUsage[app], limit);

                            var processName = _processToPathMap
                                .FirstOrDefault(x => x.Value.Equals(baseApp, StringComparison.OrdinalIgnoreCase))
                                .Key;

                            if (!string.IsNullOrEmpty(processName))
                            {
                                // Check if it's a website or application
                                if (Uri.IsWellFormedUriString(baseApp, UriKind.Absolute))
                                {
                                    // Handle website limit exceeded (browser extension will handle blocking)
                                    _logger.LogInformation("Website limit exceeded for {Domain}", baseApp);
                                    await _webSocketCommunicator.SendCloseTabCommand(GetDomainFromUrl(baseApp));

                                }
                                else
                                {
                                    await TerminateProcess(processName);
                                }

                                // Reset usage tracking
                                _shownWarnings.Remove(baseApp);
                                _appUsage[baseApp] = TimeSpan.Zero;
                                _appUsage[baseApp + "warning"] = TimeSpan.Zero;
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

        // Rest of your existing methods (TerminateProcess, ShowWarningMessage) remain unchanged
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
                var executable = _processToPathMap[processName];
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

            try
            {
                await _appRepo.UpdateIgnoreStatus(executablePath, true);
                _ignoreStatusCache[executablePath] = true;

                string warning = timeRemaining >= TimeSpan.FromMinutes(1)
                    ? $"WARNING: You have been using {executablePath} for an extended period. The application will close in {timeRemaining.Minutes} minutes once you select OK and usage continues."
                    : $"WARNING: You have been using {executablePath} for an extended period. The application will close in {timeRemaining.Seconds} seconds once you select OK and usage continues.";

                var messages = await _messageRepo.GetMessagesForComputer(_computerId);
                Random r = new Random();
                var message = messages[r.Next(0, messages.Count)];

                await _warningManager.ShowWarning(
                    message,
                    warning,
                    executablePath,
                    _computerId,
                    UpdateIgnoreStatus,
                    _appRepo,
                    _messageRepo,
                    _settingsRepository,
                    null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to show warning message");
                // Reset cache on any error
                _ignoreStatusCache[executablePath] = false;
                await _appRepo.UpdateIgnoreStatus(executablePath, false);
            }
        }

        public void UpdateIgnoreStatus(string processName, bool status)
        {
            if (_ignoreStatusCache.ContainsKey(processName))
            {
                _ignoreStatusCache[processName] = status;
                _logger.LogInformation("Updated ignore status for {ProcessName} to {Status}", processName, status);
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _warningManager.Cleanup();
            await base.StopAsync(cancellationToken);
        }
    }
}