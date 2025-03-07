﻿// HourglassLibrary/Services/WindowsUsageTracker.cs
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

        // HourglassLibrary/Services/WindowsUsageTracker.cs
        public async Task<Dictionary<string, TimeSpan>> GetActiveWebsiteUsage(Dictionary<string, string> processToPathMap, IWebsiteTracker websiteTracker)
        {
            var usage = new Dictionary<string, TimeSpan>();
            try
            {
                var websiteLimits = processToPathMap
                    .Where(kvp => Uri.IsWellFormedUriString(kvp.Value, UriKind.Absolute))
                    .Select(kvp => new { OriginalUrl = kvp.Value, Domain = GetDomainFromUrl(kvp.Value) })
                    .ToList();

                foreach (var website in websiteLimits)
                {
                    if (websiteTracker.IsDomainActive(website.Domain))
                    {
                        usage.TryAdd(website.OriginalUrl, TimeSpan.Zero);
                        usage[website.OriginalUrl] += TimeSpan.FromSeconds(1);
                        // Add the warning variant
                        usage.TryAdd(website.OriginalUrl + "warning", TimeSpan.Zero);
                        usage[website.OriginalUrl + "warning"] += TimeSpan.FromSeconds(1);
                        _logger.LogDebug("Active website found: {Domain}", website.Domain);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error tracking website usage");
            }
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
                    string baseApp = app.EndsWith("warning") ? app[..^7] : app; // Remove "warning" if present

                    // Check warning limit
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

                    // Check kill limit
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
                                await communicator.SendCloseTabCommand(GetDomainFromUrl(baseApp));
                            }
                            else
                            {
                                await TerminateProcess(processName);
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