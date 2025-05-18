using LoggerService;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace RTLSDR.Common
{
    public class ThreadWorker<T> : IThreadWorkerInfo
    {
        private ConcurrentQueue<T> _queue;
        private Thread _thread = null;
        private string _name;
        private ILoggingService _logger = null;

        private int _actionMSDelay = 1000;

        private DateTime _timeStarted = DateTime.MinValue;

        private const int MinThreadNoDataMSDelay = 25;

        private Action<T> _action = null;

        private bool _running = false;

        private bool _threadRunning = false;

        private double _workingTimeMS = 0;

        public bool ReadingQueue { get; set; } = false;
        private long _cycles = 0;

        public ThreadWorker(ILoggingService logger, string name = "Threadworker")
        {
            _logger = logger;
            _name = name;
            _logger.Debug($"Starting Threadworker {name}");
        }

        public bool ThreadRunning
        {
            get
            {
                return _threadRunning;
            }
        }

        public void SetThreadMethod(Action<T> action, int actionMSDelay)
        {
            _action = action;
            _actionMSDelay = actionMSDelay;
        }

        public void SetQueue(ConcurrentQueue<T> queue)
        {
            _queue = queue;
        }

        public void Start()
        {
            _logger.Debug($"Threadworker {_name} starting");
            _timeStarted = DateTime.Now;

            _running = true;
            _thread = new Thread(ThreadLoop);
            _thread.Start();
        }

        public void Stop()
        {
            _logger.Debug($"Stopping Threadworker {_name}");

            while (_threadRunning)
            {
                _running = false;
                Thread.Sleep(MinThreadNoDataMSDelay);
            }
            
            _logger.Debug($"Threadworker {_name} stopped");
        }

        private void ThreadLoop()
        {
            try
            {
                _threadRunning = true;
                while (_running)
                {
                    _cycles++;
                    var data = default(T);

                    if (ReadingQueue)
                    {
                        var ok = _queue.TryDequeue(out data);
 
                        if (_action != null && data != null)
                        {
                            var startTime = DateTime.Now;
                       
                            _action(data);                        

                            _workingTimeMS += (DateTime.Now - startTime).TotalMilliseconds;
                        } else
                        {
                            Thread.Sleep(_actionMSDelay);
                        }
                    } else
                    {
                        if (_action != null)
                        {
                            var startTime = DateTime.Now;

                            _action(data);

                            _workingTimeMS += (DateTime.Now - startTime).TotalMilliseconds;
                        }

                        Thread.Sleep(_actionMSDelay);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
            }

            _threadRunning = false;
            _logger.Debug($"Threadworker {_name} stopped");
        }

        public double UpTimeMS
        {
            get
            {
                if (_timeStarted == DateTime.MinValue)
                    return 0;

                return (DateTime.Now - _timeStarted).TotalMilliseconds;
            }
        }

        public double WorkingTimeMS
        {
            get
            {
                return _workingTimeMS;
            }
        }

        public int UpTimeS
        {
            get
            {
                return Convert.ToInt32(UpTimeMS / 1000);
            }
        }

        public string Name
        {
            get
            {
                return _name;
            }
        }

        public int QueueItemsCount
        {
            get
            {
                if (_queue == null)
                    return 0;

                return _queue.Count;
            }
        }

        public long CyclesCount
        {
            get
            {
                return _cycles;
            }
        }
    }
}
