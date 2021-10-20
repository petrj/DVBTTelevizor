using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using NLog;
using NLog.Config;

namespace LoggerService
{
    public class BasicLoggingService : ILoggingService
    {
        private LoggingLevelEnum _minLevel;

        public BasicLoggingService(LoggingLevelEnum minLevel = LoggingLevelEnum.Debug)
        {
            MinLevel = minLevel;
        }

        public LoggingLevelEnum MinLevel { get => _minLevel; set => _minLevel = value; }

        private void Write(LoggingLevelEnum level, string message)
        {
            try
            {
                if ((int)level < (int)MinLevel)
                    return;

                string msg = $"[{DateTime.Now.ToString("yyyy-MM-dd--HH-mm-ss")}] [{level}] {message}";

                //System.Diagnostics.Debug.WriteLine(msg);

                Console.WriteLine(msg);
            }
            catch
            {
                // log failed
            }
        }

        public void Debug(string message)
        {
            Write(LoggingLevelEnum.Debug, message);
        }

        public void Info(string message)
        {
            Write(LoggingLevelEnum.Info, message);
        }

        public void Error(Exception ex, string message)
        {
            Write(LoggingLevelEnum.Error, $"{message} {ex}");
        }

        public void Error(string message)
        {
            Write(LoggingLevelEnum.Error, $"{message}");
        }
    }
}
