// HourglassMaui/Platforms/Android/AndroidUsageTracker.cs
using Android.App;
using Android.App.Usage;
using Android.Content;
using Android.OS;
using Android.Provider; // Add this for Settings.ActionUsageAccessSettings
using HourglassLibrary.Interfaces;
using HourglassLibrary.Services;
using Microsoft.Extensions.Logging;

namespace HourglassMaui
{
    public class AndroidUsageTracker : UsageTrackerBase
    {
        public AndroidUsageTracker(ILogger<AndroidUsageTracker> logger) : base(logger)
        {
        }

        public override async Task<Dictionary<string, TimeSpan>> GetActiveAppUsage(Dictionary<string, string> processToPathMap)
        {
            var usage = new Dictionary<string, TimeSpan>();
            try
            {
                var usageStatsManager = (UsageStatsManager)Platform.AppContext.GetSystemService(Context.UsageStatsService);
                if (usageStatsManager == null)
                {
                    _logger.LogWarning("UsageStatsManager not available");
                    return usage;
                }

                // Check if the app has usage stats permission
                var appOpsManager = (AppOpsManager)Platform.AppContext.GetSystemService(Context.AppOpsService);
                var mode = appOpsManager.CheckOpNoThrow(AppOpsManager.OpstrGetUsageStats, Process.MyUid(), Platform.AppContext.PackageName);
                if (mode != AppOpsManagerMode.Allowed)
                {
                    _logger.LogWarning("Usage stats permission not granted. Prompting user...");
                    var intent = new Intent(Settings.ActionUsageAccessSettings);
                    intent.AddFlags(ActivityFlags.NewTask);
                    Platform.AppContext.StartActivity(intent);
                    return usage; // Return empty until permission is granted
                }

                // Query usage stats for the last minute to determine active apps
                long currentTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                long startTime = currentTime - 60 * 1000; // Last 60 seconds
                var stats = usageStatsManager.QueryUsageStats(UsageStatsInterval.Daily, startTime, currentTime);

                if (stats != null)
                {
                    foreach (var stat in stats)
                    {
                        if (stat.LastTimeUsed > 0 && (currentTime - stat.LastTimeUsed) <= 60 * 1000) // Active within the last minute
                        {
                            string packageName = stat.PackageName;
                            if (processToPathMap.ContainsKey(packageName))
                            {
                                usage.TryAdd(packageName, TimeSpan.Zero);
                                usage[packageName] += TimeSpan.FromSeconds(1);
                            }
                        }
                    }
                }

                _logger.LogDebug("Tracked usage for {Count} apps on Android", usage.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error tracking app usage on Android");
            }
            return await Task.FromResult(usage);
        }

        public override async Task<Dictionary<string, TimeSpan>> GetActiveWebsiteUsage(Dictionary<string, string> processToPathMap, IWebsiteTracker websiteTracker)
        {
            // Implement website tracking for Android (e.g., via WebView or browser monitoring)
            // For now, return empty as a placeholder
            return await Task.FromResult(new Dictionary<string, TimeSpan>());
        }

        protected override async Task TerminateProcessAsync(string processName)
        {
            try
            {
                var activityManager = (ActivityManager)Platform.AppContext.GetSystemService(Context.ActivityService);
                activityManager.KillBackgroundProcesses(processName);
                _logger.LogInformation($"Terminated {processName} on Android due to exceeded usage limit.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to terminate {processName} on Android.");
            }
            await Task.CompletedTask;
        }
    }
}