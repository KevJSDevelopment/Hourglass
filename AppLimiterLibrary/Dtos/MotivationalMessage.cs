using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppLimiterLibrary.Dtos
{
    public class MotivationalMessage
    {
        public int Id { get; set; }
        public int TypeId { get; set; }
        public string TypeDescription { get; set; }
        public string ComputerId { get; set; }
        public string Message { get; set; }
        public string FilePath { get; set; }
    }
}
