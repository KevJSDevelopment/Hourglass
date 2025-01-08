using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HourglassLibrary.Dtos
{
    public class ProcessInfo
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public bool Ignore { get; set; }
        public string WarningTime { get; set; }
        public string KillTime { get; set; }
        public string ComputerId { get; set; }
        public bool IsWebsite { get; set; }  
    }
}
