// HourglassLibrary/Services/UsageTrackerBase.cs
using HourglassLibrary.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HourglassLibrary.Services
{
    public abstract class UsageTrackerBase : IUsageTracker
    {
        protected readonly ILogger _logger;
        protected readonly HashSet<string> _shownWarnings = new(StringComparer.OrdinalIgnoreCase);

        protected UsageTrackerBase(ILogger logger)
        {
            _logger = logger;
        }

        public abstract Task<Dictionary<string, TimeSpan>> GetActiveAppUsage(Dictionary<string, string> processToPathMap);
        public abstract Task<Dictionary<string, TimeSpan>> GetActiveWebsiteUsage(Dictionary<string, string> processToPathMap, IWebsiteTracker websiteTracker);

        public virtual async Task EnforceLimits(
            Dictionary<string, TimeSpan> appUsage,
            Dictionary<string, TimeSpan> appLimits,
            Dictionary<string, string> processToPathMap,
            IWebSocketCommunicator communicator,
            Action<string> showWarningCallback)
        {
            foreach (var app in appUsage.Keys.ToList())
            {
                try
                {
                    string baseApp = app.EndsWith("warning") ? app[..^7] : app;

                    if (appLimits.TryGetValue(baseApp + "warning", out TimeSpan warningLimit) && appUsage.ContainsKey(baseApp + "warning") && appUsage[baseApp + "warning"] >= warningLimit)
                    {
                        if (!_shownWarnings.Contains(baseApp))
                        {
                            _logger.LogInformation("Warning threshold reached for {AppName}. Usage: {Usage}, Limit: {Limit}",
                                baseApp, appUsage[baseApp + "warning"], warningLimit);
                            showWarningCallback(baseApp);
                            _shownWarnings.Add(baseApp);
                        }
                    }

                    if (appLimits.TryGetValue(baseApp, out TimeSpan killLimit) && appUsage[baseApp] >= killLimit)
                    {
                        _logger.LogWarning("Usage limit exceeded for {AppName}. Usage: {Usage}, Limit: {Limit}",
                            baseApp, appUsage[baseApp], killLimit);

                        var processName = processToPathMap
                            .FirstOrDefault(x => x.Value.Equals(baseApp, StringComparison.OrdinalIgnoreCase))
                            .Key;

                        if (!string.IsNullOrEmpty(processName))
                        {
                            if (Uri.IsWellFormedUriString(baseApp, UriKind.Absolute))
                            {
                                _logger.LogInformation("Website limit exceeded for {Domain}", baseApp);
                                await communicator.SendCloseTabCommand(UrlHandler.GetDomainFromUrl(baseApp));
                            }
                            else
                            {
                                await TerminateProcessAsync(processName);
                            }

                            appUsage[baseApp] = TimeSpan.Zero;
                            if (appUsage.ContainsKey(baseApp + "warning"))
                                appUsage[baseApp + "warning"] = TimeSpan.Zero;
                            _shownWarnings.Remove(baseApp);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error enforcing limits for {AppName}", app);
                }
            }
        }

        protected virtual Task TerminateProcessAsync(string processName)
        {
            // Default implementation (override in platform-specific classes)
            return Task.CompletedTask;
        }
    }
}