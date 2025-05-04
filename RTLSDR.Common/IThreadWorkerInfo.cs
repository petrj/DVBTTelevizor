using LoggerService;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace RTLSDR.Common
{
    public interface IThreadWorkerInfo
    {
        double UpTimeMS { get; }
        double WorkingTimeMS { get; }
        int UpTimeS { get; }
        string Name { get; }
        int QueueItemsCount { get; }

        long CyclesCount { get; }
    }
}


