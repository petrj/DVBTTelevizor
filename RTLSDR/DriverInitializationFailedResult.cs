using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RTLSDR
{
    public class DriverInitializationFailedResult
    {
        public int ErrorId { get; set; }
        public int ExceptionCode { get; set; }
        public string DetailedDescription { get; set; }
    }
}
