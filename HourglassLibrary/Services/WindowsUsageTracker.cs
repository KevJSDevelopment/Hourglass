// HourglassLibrary/Services/WindowsUsageTracker.cs
using HourglassLibrary.Data;
using HourglassLibrary.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace HourglassLibrary.Services
{
    public class WindowsUsageTracker : IUsageTracker
    {
        private readonly ILogger<WindowsUsageTracker> _logger;
        private readonly HashSet<string> _shownWarnings = new(StringComparer.OrdinalIgnoreCase);

        public WindowsUsageTracker(ILogger<WindowsUsageTracker> logger)
        {
            _logger = logger;
        }

        public async Task<Dictionary<string, TimeSpan>> GetActiveAppUsage(Dictionary<string, string> processToPathMap)
        {
            var usage = new Dictionary<string, TimeSpan>();
            try
            {
                var processNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var activeProcesses = Process.GetProcesses()
                    .Where(p => processToPathMap.ContainsKey(p.ProcessName))
                    .ToList();

                _logger.LogDebug("Tracking usage for {Count} monitored processes", activeProcesses.Count);

                foreach (var process in activeProcesses)
                {
                    if (processNames.Add(process.ProcessName) &&
                        processToPathMap.TryGetValue(process.ProcessName, out string executable))
                    {
                        usage.TryAdd(executable, TimeSpan.Zero);
                        usage[executable] += TimeSpan.FromSeconds(1);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error tracking app usage");
            }
            return await Task.FromResult(usage);
        }

        public async Task<Dictionary<string, TimeSpan>> GetActiveWebsiteUsage(Dictionary<string, string> processToPathMap, IWebsiteTracker websiteTracker)
        {
            var usage = new Dictionary<string, TimeSpan>();
            try
            {
                _logger.LogDebug("Starting website usage tracking. processToPathMap: {Map}", string.Join(", ", processToPathMap.Select(kvp => $"{kvp.Key}: {kvp.Value}")));

                var websiteLimits = processToPathMap
                    .Where(kvp => Uri.IsWellFormedUriString(kvp.Value, UriKind.Absolute))
                    .Select(kvp => new { OriginalUrl = kvp.Value, Domain = GetDomainFromUrl(kvp.Value) })
                    .ToList();

                _logger.LogDebug("Website limits found: {Count} entries", websiteLimits.Count);
                foreach (var website in websiteLimits)
                {
                    _logger.LogDebug("Checking domain: {Domain} from URL: {OriginalUrl}", website.Domain, website.OriginalUrl);
                    if (websiteTracker.IsDomainActive(website.Domain))
                    {
                        usage.TryAdd(website.OriginalUrl, TimeSpan.Zero);
                        usage[website.OriginalUrl] += TimeSpan.FromSeconds(1);
                        _logger.LogDebug("Active website found: {Domain}", website.Domain);
                    }
                    else
                    {
                        _logger.LogDebug("Domain {Domain} is NOT active", website.Domain);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error tracking website usage");
            }
            _logger.LogDebug("Returning website usage: {Usage}", string.Join(", ", usage.Select(kvp => $"{kvp.Key}: {kvp.Value}")));
            return await Task.FromResult(usage);
        }

        public async Task EnforceLimits(
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

                    if (appLimits.TryGetValue(app, out TimeSpan limit) && appUsage[app] >= limit)
                    {
                        if (app.EndsWith("warning"))
                        {
                            if (!_shownWarnings.Contains(baseApp))
                            {
                                _logger.LogInformation("Warning threshold reached for {AppName}. Usage: {Usage}, Limit: {Limit}",
                                    baseApp, appUsage[app], limit);
                                showWarningCallback(baseApp);
                                _shownWarnings.Add(baseApp);
                            }
                        }
                        else
                        {
                            _logger.LogWarning("Usage limit exceeded for {AppName}. Usage: {Usage}, Limit: {Limit}",
                                baseApp, appUsage[app], limit);

                            var processName = processToPathMap
                                .FirstOrDefault(x => x.Value.Equals(baseApp, StringComparison.OrdinalIgnoreCase))
                                .Key;

                            if (!string.IsNullOrEmpty(processName))
                            {
                                if (Uri.IsWellFormedUriString(baseApp, UriKind.Absolute))
                                {
                                    _logger.LogInformation("Website limit exceeded for {Domain}", baseApp);
                                    await communicator.SendCloseTabCommand(GetDomainFromUrl(baseApp));
                                }
                                else
                                {
                                    await TerminateProcess(processName);
                                }

                                appUsage[baseApp] = TimeSpan.Zero;
                                appUsage[baseApp + "warning"] = TimeSpan.Zero;
                                _shownWarnings.Remove(baseApp);
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
                foreach (var process in processes)
                {
                    process.Kill();
                    _logger.LogInformation("Terminated process {ProcessName}", process.ProcessName);
                    process.Dispose();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error terminating process {ProcessName}", processName);
            }
            await Task.CompletedTask;
        }

        private string GetDomainFromUrl(string url)
        {
            if (Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
            {
                return uri.Host.ToLower().Replace("www.", "");
            }
            return url.ToLower();
        }
    }
}