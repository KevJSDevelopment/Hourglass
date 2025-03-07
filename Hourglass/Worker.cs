// Hourglass/Worker.cs
using HourglassLibrary.Data;
using HourglassLibrary.Interfaces;
using HourglassMessaging.WPF.Services;

namespace Hourglass
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly AppRepository _appRepo;
        private readonly MotivationalMessageRepository _messageRepo;
        private readonly SettingsRepository _settingsRepository;
        private readonly IUsageTracker _usageTracker; // New dependency
        private readonly IWebsiteTracker _websiteTracker; // Updated
        private readonly IWebSocketCommunicator _webSocketCommunicator; // Use the correct namespace
        private readonly Dictionary<string, TimeSpan> _appUsage = new();
        private readonly Dictionary<string, TimeSpan> _appLimits = new();
        private readonly Dictionary<string, string> _processToPathMap = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, bool> _ignoreStatusCache = new();
        private readonly HashSet<string> _shownWarnings = new(StringComparer.OrdinalIgnoreCase);
        private readonly WarningWindowManager _warningManager = new();
        private readonly string _computerId;

        public Worker(
            ILogger<Worker> logger,
            IConfiguration configuration,
            IUsageTracker usageTracker, // Inject interface
            IWebsiteTracker websiteTracker, // Updated
            IWebSocketCommunicator webSocketCommunicator)
        {
            _logger = logger;
            _usageTracker = usageTracker;
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

                        // Track usage and enforce limits using IUsageTracker
                        var appUsageTask = _usageTracker.GetActiveAppUsage(_processToPathMap);
                        var websiteUsageTask = _usageTracker.GetActiveWebsiteUsage(_processToPathMap, _websiteTracker);
                        var enforceTask = _usageTracker.EnforceLimits(_appUsage, _appLimits, _processToPathMap, _webSocketCommunicator, ShowWarningMessage);

                        await Task.WhenAll(appUsageTask, websiteUsageTask, enforceTask);

                        // Merge usage results
                        foreach (var usage in appUsageTask.Result.Concat(websiteUsageTask.Result))
                        {
                            _appUsage.TryAdd(usage.Key, TimeSpan.Zero);
                            _appUsage[usage.Key] += usage.Value;
                        }

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

                    string processName = limit.IsWebsite ? limit.Path : Path.GetFileNameWithoutExtension(limit.Path);
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

        private async void ShowWarningMessage(string executablePath)
        {
            var timeRemaining = _appLimits[executablePath] - _appLimits[executablePath + "warning"];
            var displayName = Uri.IsWellFormedUriString(executablePath, UriKind.Absolute)
                ? GetDomainFromUrl(executablePath)
                : Path.GetFileNameWithoutExtension(executablePath);
            try
            {
                await _appRepo.UpdateIgnoreStatus(executablePath, true);
                _ignoreStatusCache[executablePath] = true;

                string warning = timeRemaining >= TimeSpan.FromMinutes(1)
                    ? $"WARNING: You have been using {displayName} for an extended period. The application will close in {timeRemaining.Minutes} minutes once you select OK and usage continues."
                    : $"WARNING: You have been using {displayName} for an extended period. The application will close in {timeRemaining.Seconds} seconds once you select OK and usage continues.";

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
                _ignoreStatusCache[executablePath] = false;
                await _appRepo.UpdateIgnoreStatus(executablePath, false);
            }
        }

        private string GetDomainFromUrl(string url)
        {
            if (Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
            {
                return uri.Host.ToLower().Replace("www.", "");
            }
            return url.ToLower();
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