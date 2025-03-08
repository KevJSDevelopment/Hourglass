// HourglassMaui/Platforms/Windows/WindowsUsageTracker.cs
using HourglassLibrary.Interfaces;
using HourglassLibrary.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace HourglassMaui
{
    public class WindowsUsageTracker : UsageTrackerBase
    {
        public WindowsUsageTracker(ILogger<WindowsUsageTracker> logger) : base(logger)
        {
        }

        public override async Task<Dictionary<string, TimeSpan>> GetActiveAppUsage(Dictionary<string, string> processToPathMap)
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
                _logger.LogError(ex, "Error tracking app usage on Windows");
            }
            return await Task.FromResult(usage);
        }

        public override async Task<Dictionary<string, TimeSpan>> GetActiveWebsiteUsage(Dictionary<string, string> processToPathMap, IWebsiteTracker websiteTracker)
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
                        usage.TryAdd(website.OriginalUrl + "warning", TimeSpan.Zero);
                        usage[website.OriginalUrl + "warning"] += TimeSpan.FromSeconds(1);
                        _logger.LogDebug("Active website found: {Domain}", website.Domain);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error tracking website usage on Windows");
            }
            return await Task.FromResult(usage);
        }

        protected override async Task TerminateProcessAsync(string processName)
        {
            try
            {
                var processes = Process.GetProcessesByName(processName);
                foreach (var process in processes)
                {
                    process.Kill();
                    _logger.LogInformation($"Terminated {process.ProcessName} due to exceeded usage limit.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to terminate {processName}.");
            }
            await Task.CompletedTask;
        }
    }
}