using NLog;
using System;
using System.Collections.Generic;
using System.Text;

namespace LoggerService
{
    public enum LoggingLevelEnum
    {
        Debug = 1,
        Info = 5,
        Error = 9,
    }

    public interface ILoggingService
    {
        void Debug(string message);
        void Info(string message);

        void Error(Exception ex, string message = null);
        void Error(string message);
    }
}
