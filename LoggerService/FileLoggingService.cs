using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using NLog;
using NLog.Config;

namespace LoggerService
{
    public class FileLoggingService : ILoggingService
    {
        private string _logFileName;
        private LoggingLevelEnum _minLevel;

        public FileLoggingService(LoggingLevelEnum minLevel = LoggingLevelEnum.Debug)
        {
            MinLevel = minLevel;
        }

        public LoggingLevelEnum MinLevel { get => _minLevel; set => _minLevel = value; }

        public string LogFilename
        {
            get
            {
                return _logFileName;
            }
            set
            {
                _logFileName = value;
            }
        }

        private void Write(LoggingLevelEnum level, string message)
        {
            try
            {
                string msg = $"[{DateTime.Now.ToString("yyyy-MM-dd--HH-mm-ss")}] {level} {message}";

#if DEBUG
    System.Diagnostics.Debug.WriteLine(msg);
    Console.WriteLine(msg);
#endif

                var logFolder = System.IO.Path.GetDirectoryName(LogFilename);
                if(string.IsNullOrEmpty(logFolder))
                {
                    // app folder
                    logFolder = AppDomain.CurrentDomain.BaseDirectory;
                }

                if (!Directory.Exists(logFolder))
                    Directory.CreateDirectory(logFolder);

                if ((int)level < (int)MinLevel)
                    return;

                using (var fs = new FileStream(LogFilename, FileMode.Append, FileAccess.Write))
                {
                    using (var sw = new StreamWriter(fs))
                    {
                        sw.WriteLine(msg);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error while writing log to {LogFilename} ({ex})");
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
