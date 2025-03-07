using HourglassLibrary.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HourglassLibrary.Interfaces
{
    public interface IUsageTracker
    {
        /// <summary>
        /// Tracks active app usage based on a mapping of process names to paths.
        /// </summary>
        Task<Dictionary<string, TimeSpan>> GetActiveAppUsage(Dictionary<string, string> processToPathMap);

        /// <summary>
        /// Tracks active website usage using the provided WebsiteTracker.
        /// </summary>
        Task<Dictionary<string, TimeSpan>> GetActiveWebsiteUsage(Dictionary<string, string> processToPathMap, IWebsiteTracker websiteTracker);

        /// <summary>
        /// Enforces time limits, triggering warnings or terminations as needed.
        /// </summary>
        Task EnforceLimits(
            Dictionary<string, TimeSpan> appUsage,
            Dictionary<string, TimeSpan> appLimits,
            Dictionary<string, string> processToPathMap,
            IWebSocketCommunicator communicator,
            Action<string> showWarningCallback);
    }
}
