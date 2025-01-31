using LoggerService;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DVBTTelevizor.MAUI
{
    public class LoggerProvider : ILoggingProvider
    {
        public ILoggingService GetLoggingService()
        {
            Assembly assembly = typeof(App).GetTypeInfo().Assembly;
            NLog.Config.ISetupBuilder setupBuilder = NLog.LogManager.Setup();
            NLog.Config.ISetupBuilder configuredSetupBuilder = setupBuilder.LoadConfigurationFromAssemblyResource(assembly);

            return new NLogLoggingService(configuredSetupBuilder.GetCurrentClassLogger());
        }
    }
}
