using NLog.Config;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Xml;

namespace LoggerService
{
    public class NLogLoggingService : ILoggingService
    {
        NLog.Logger _logger = null;

        public NLogLoggingService(Assembly assembly, string projectName)
        {
            var location = $"{projectName}.NLog.config";
            var stream = assembly.GetManifestResourceStream(location);
            NLog.LogManager.Configuration = new XmlLoggingConfiguration(XmlReader.Create(stream), null);
            _logger = NLog.LogManager.GetCurrentClassLogger();
        }

        public void Debug(string message)
        {
            _logger.Debug(message);
        }

        public void Error(Exception ex, string message = null)
        {
            _logger.Error(ex, message);
        }

        public void Error(string message)
        {
            _logger.Error(message);
        }

        public void Info(string message)
        {
            _logger.Info(message);
        }

        public void Warn(string message)
        {
            _logger.Warn(message);
        }
    }
}
