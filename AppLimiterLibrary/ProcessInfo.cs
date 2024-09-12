using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppLimiterLibrary
{
    public class ProcessInfo
    {
        public string Name { get; set; }
        public string Executable { get; set; }
        public string WarningTime { get; set; }
        public string KillTime { get; set; }
    }
}
