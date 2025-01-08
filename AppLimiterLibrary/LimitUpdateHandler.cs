using System;
using System.Collections.Generic;

namespace HourglassLibrary
{
    public static class LimitUpdateHandler
    {
        private static Dictionary<string, DateTime> ignoredProcesses = new Dictionary<string, DateTime>();

        public static void IgnoreLimitForDay(string processName)
        {
            ignoredProcesses[processName] = DateTime.Today.AddDays(1);
        }

        public static bool IsLimitIgnored(string processName)
        {
            if (ignoredProcesses.TryGetValue(processName, out DateTime ignoreUntil))
            {
                if (DateTime.Now < ignoreUntil)
                {
                    return true;
                }
                else
                {
                    ignoredProcesses.Remove(processName);
                }
            }
            return false;
        }
    }
}