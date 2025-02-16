using CommunityToolkit.Mvvm.Messaging;
using LoggerService;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace DVBTTelevizor.MAUI
{
    internal class DVBTTelevizorConfiguration : SQLiteTVConfiguration
    {
        public DVBTTelevizorConfiguration(ILoggingProvider loggingProvider, IPublicDirectoryProvider publicDirectoryProvider)
            :base(loggingProvider, publicDirectoryProvider)
        {
        }
    }
}
