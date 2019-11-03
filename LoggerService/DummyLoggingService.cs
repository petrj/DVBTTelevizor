using System;
using System.Collections.Generic;
using System.Text;

namespace LoggerService
{
    public class DummyLoggingService : ILoggingService
    {
        public void Debug(string message)
        { }

        public void Error(Exception ex, string message = null)
        { }

        public void Error(string message)
        { }

        public void Info(string message)
        { }

        public void Warn(string message)
        { }
    }
}
