// HourglassMaui/Services/UsageTrackingService.cs
using HourglassLibrary.Data;
using HourglassLibrary.Interfaces;
using HourglassMaui.Views;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HourglassMaui.Services
{
    public class UsageTrackingService : BackgroundService
    {
        private readonly ILogger<UsageTrackingService> _logger;
        private readonly IUsageTracker _usageTracker;
        private readonly IWebsiteTracker _websiteTracker;
        private readonly IWebSocketCommunicator _webSocketCommunicator;
        private readonly AppRepository _appRepo; // Add dependency
        private readonly MotivationalMessageRepository _messageRepo;
        private readonly Dictionary<string, TimeSpan> _appUsage = new();
        private readonly Dictionary<string, TimeSpan> _appLimits = new();
        private readonly Dictionary<string, string> _processToPathMap = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _shownWarnings = new(StringComparer.OrdinalIgnoreCase);
        private readonly string _computerId;

        public UsageTrackingService(
            ILogger<UsageTrackingService> logger,
            IUsageTracker usageTracker,
            IWebsiteTracker websiteTracker,
            IWebSocketCommunicator webSocketCommunicator,
            AppRepository appRepo,
            MotivationalMessageRepository messageRepo) // Inject AppRepository
        {
            _logger = logger;
            _usageTracker = usageTracker;
            _websiteTracker = websiteTracker;
            _webSocketCommunicator = webSocketCommunicator;
            _appRepo = appRepo;
            _messageRepo = messageRepo;
            _computerId = ComputerIdentifier.GetUniqueIdentifier(); // Ensure ComputerIdentifier is available
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Usage tracking service started for computer {ComputerId}", _computerId);

            await LoadAndApplyLimits();

            while (!stoppingToken.IsCancellationRequested)
            {
                var startTime = DateTime.UtcNow;

                var appUsageTask = _usageTracker.GetActiveAppUsage(_processToPathMap);
                var websiteUsageTask = _usageTracker.GetActiveWebsiteUsage(_processToPathMap, _websiteTracker);
                var enforceTask = _usageTracker.EnforceLimits(_appUsage, _appLimits, _processToPathMap, _webSocketCommunicator, ShowWarningMessage);

                await Task.WhenAll(appUsageTask, websiteUsageTask, enforceTask);

                _logger.LogDebug("Merged usage before update: {Usage}", string.Join(", ", _appUsage.Select(kvp => $"{kvp.Key}: {kvp.Value}")));
                foreach (var usage in appUsageTask.Result.Concat(websiteUsageTask.Result))
                {
                    _appUsage.TryAdd(usage.Key, TimeSpan.Zero);
                    _appUsage[usage.Key] += usage.Value;
                }
                _logger.LogDebug("Merged usage after update: {Usage}", string.Join(", ", _appUsage.Select(kvp => $"{kvp.Key}: {kvp.Value}")));

                var executionTime = DateTime.UtcNow - startTime;
                if (executionTime > TimeSpan.FromSeconds(1))
                {
                    _logger.LogWarning("Monitoring cycle took longer than expected: {ExecutionTime}ms", executionTime.TotalMilliseconds);
                }

                var delayTime = TimeSpan.FromSeconds(1) - executionTime;
                if (delayTime > TimeSpan.Zero)
                {
                    await Task.Delay(delayTime, stoppingToken);
                }
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

                    string processName = limit.IsWebsite ? GetDomainFromUrl(limit.Path) : Path.GetFileNameWithoutExtension(limit.Path);
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

        private string GetDomainFromUrl(string url)
        {
            if (Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
            {
                return uri.Host.ToLower().Replace("www.", "");
            }
            return url.ToLower();
        }

        private async void ShowWarningMessage(string executablePath)
        {
            _logger.LogDebug("Starting ShowWarningMessage for {Path}", executablePath);
            var timeRemaining = _appLimits[executablePath] - _appLimits[executablePath + "warning"];
            var displayName = Uri.IsWellFormedUriString(executablePath, UriKind.Absolute)
                ? GetDomainFromUrl(executablePath)
                : Path.GetFileNameWithoutExtension(executablePath);
            var warning = timeRemaining >= TimeSpan.FromMinutes(1)
                ? $"WARNING: You have been using {displayName} for an extended period. The application will close in {timeRemaining.Minutes} minutes once you select OK and usage continues."
                : $"WARNING: You have been using {displayName} for an extended period. The application will close in {timeRemaining.Seconds} seconds once you select OK and usage continues.";

            var messages = await _messageRepo.GetMessagesForComputer(_computerId); // Async call
            if (messages == null || !messages.Any())
            {
                _logger.LogWarning("No motivational messages found for computer {ComputerId}", _computerId);
                return;
            }
            var message = messages[new Random().Next(0, messages.Count)].Message;

            await Application.Current.Dispatcher.DispatchAsync(async () =>
            {
                await Application.Current.MainPage.Navigation.PushModalAsync(new WarningPage(warning, message));
            });
            _logger.LogDebug("Finished ShowWarningMessage for {Path}", executablePath);
        }
    }
}