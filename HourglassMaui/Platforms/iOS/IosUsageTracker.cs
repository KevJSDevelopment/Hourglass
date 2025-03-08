// HourglassMaui/Platforms/iOS/IosUsageTracker.cs
using HourglassLibrary.Interfaces;
using HourglassLibrary.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HourglassMaui
{
    public class IosUsageTracker : UsageTrackerBase
    {
        public IosUsageTracker(ILogger<IosUsageTracker> logger) : base(logger)
        {
        }

        public override async Task<Dictionary<string, TimeSpan>> GetActiveAppUsage(Dictionary<string, string> processToPathMap)
        {
            // iOS doesn't provide direct app tracking like Android or Windows
            // Consider using NSWorkspace (macOS) or monitoring foreground app
            _logger.LogWarning("App usage tracking not implemented for iOS yet");
            return await Task.FromResult(new Dictionary<string, TimeSpan>());
        }

        public override async Task<Dictionary<string, TimeSpan>> GetActiveWebsiteUsage(Dictionary<string, string> processToPathMap, IWebsiteTracker websiteTracker)
        {
            // Implement website tracking (e.g., via WebView)
            return await Task.FromResult(new Dictionary<string, TimeSpan>());
        }
    }
}