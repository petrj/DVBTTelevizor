using DVBTTelevizor.Models;
using LoggerService;
using MPEGTS;
using SQLite;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DVBTTelevizor.Services
{
    public class EITManager : DBManager<EventItem>
    {
        public override string Key { get; set; } = "EIT";
        protected bool _scanning = false;

        private Dictionary<int,List<EventItem>> _eventsToSave = new Dictionary<int, List<EventItem>>();
        private Dictionary<long, ChannelEPG> FreqEPG { get; set; } = new Dictionary<long, ChannelEPG>();

        public EITManager(ILoggingService loggingService, IDVBTDriverManager driver):
            base(loggingService,driver)
        {
            var saveDBsWorker = new BackgroundWorker();
            saveDBsWorker.DoWork += SaveDBsWorker_DoWork;
            saveDBsWorker.RunWorkerAsync();
        }

        private void AddEventsToSave(int mapPID, List<EventItem> items)
        {
            try
            {
                // _log.Debug($"[EIT] addding PID {mapPID} events to save");

                _semaphoreSlim.WaitAsync();

                if (_eventsToSave.ContainsKey(mapPID))
                {
                    // removing older event in queue
                    _eventsToSave.Remove(mapPID);
                }

                _eventsToSave.Add(mapPID, items);
            }
            finally
            {
                _semaphoreSlim.Release();
            };
        }

        /// <summary>
        /// FIFO queue for saving EIT events
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SaveDBsWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            while (true)
            {
                var pid = -1;
                List<EventItem> events = null;

                try
                {
                    _semaphoreSlim.WaitAsync();

                    // removing oldest record from queue

                    if (_eventsToSave.Count > 0)
                    {
                        pid  = _eventsToSave.Keys.First();
                        events = _eventsToSave.Values.First();
                        _eventsToSave.Remove(pid);
                    }
                }
                finally
                {
                    _semaphoreSlim.Release();
                };

                if (pid != -1)
                {
                    // saving oldest record
                    try
                    {
                        var dbName = GetDBPath(_driver.LastTunedFreq, pid);

                        _log.Debug($"[EIT] saving {_driver.LastTunedFreq}.{pid}.sqllite");

                        var db = new SQLiteConnection(dbName);

                        db.DropTable<EventItem>();

                        db.CreateTable<EventItem>();

                        foreach (var ev in events)
                        {
                            db.Insert(ev);
                        }

                        db.Close();
                    }
                    catch (Exception ex)
                    {
                        _log.Error(ex, $"[EIT]");
                    }
                } else
                {
                    // no event to save

                    System.Threading.Thread.Sleep(200);
                }
            }
        }

        public void ClearAll()
        {
            _log.Debug($"[EIT] ClearAll");

            try
            {
                var folder = Path.Combine(BaseViewModel.AndroidAppDirectory, "EIT");
                Directory.Delete(folder, true);

                FreqEPG.Clear();

            } catch (Exception ex)
            {
                _log.Error(ex);
            }
        }


        public virtual bool Scanning
        {
            get
            {
                return _scanning;
            }
        }


        private static string GetDBPath(long freq, long programMapPID)
        {
            var folder = Path.Combine(BaseViewModel.AndroidAppDirectory, "EIT");
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
            return Path.Combine(folder, $"EIT.{freq}.{programMapPID}.sqllite");
        }

        public EPGCurrentEvent GetEvent(DateTime date, long freq, long programMapPID)
        {
            var evs = GetEvents(date, freq, programMapPID, 2);

            if (evs == null || evs.Count == 0)
                return null;

            var res = new EPGCurrentEvent();

            if (evs.Count > 0)
            {
                res.CurrentEventItem = evs[0].Clone();
            }

            if (evs.Count > 1)
            {
                res.NextEventItem = evs[1].Clone();
            }

            return res;
        }

        private void TryLoadEvents(long freq, long programMapPID)
        {
            var dbPath = GetDBPath(freq, programMapPID);
            if (!File.Exists(dbPath))
            {
                return;
            }

            ChannelEPG channelEPG = null;

            if (!FreqEPG.ContainsKey(freq))
            {
                channelEPG = new ChannelEPG();
                FreqEPG.Add(freq, channelEPG);
            }
            else
            {
                channelEPG = FreqEPG[freq];
            }

            _log.Debug($"[EIT] Loading events {freq}.{programMapPID}");

            var db = new SQLiteConnection(GetDBPath(freq, programMapPID));

            foreach (var ev in db.Table<EventItem>())
            {
                channelEPG.AddEvent(programMapPID, ev);
            }

            db.Close();
        }

        public List<EventItem> GetEvents(DateTime date, long freq, long programMapPID, int count)
        {
            if (!FreqEPG.ContainsKey(freq))
            {
                TryLoadEvents(freq, programMapPID);
            }

            if (!FreqEPG.ContainsKey(freq))
            {
                return null;
            }

            var channelEPG = FreqEPG[freq];

            if (!channelEPG.EventItems.ContainsKey(programMapPID))
            {
                TryLoadEvents(freq, programMapPID);
            }

            return channelEPG.GetEvents(date, programMapPID, count);
        }

        public async Task<EITScanResult> Scan(int msTimeout = 2000)
        {
            _log.Debug($"[EIT] Scanning freq {_driver.LastTunedFreq}");

            if (Scanning)
            {
                _log.Debug($"[EIT] Scanning already running");

                return new EITScanResult()
                {
                    OK = false
                };
            }

            try
            {
                _scanning = true;

                var scanRes = await _driver.ScanEPG(msTimeout);

                _log.Debug($"[EIT] scanned result: {scanRes.OK}");

                if (!scanRes.OK)
                {
                    _log.Debug($"[EIT] scanning failed");
                    return scanRes;
                }

                ChannelEPG channelEPG = null;

                if (!FreqEPG.ContainsKey(_driver.LastTunedFreq))
                {
                    channelEPG = new ChannelEPG();
                    FreqEPG.Add(_driver.LastTunedFreq, channelEPG);
                }
                else
                {
                    channelEPG = FreqEPG[_driver.LastTunedFreq];
                }

                var modifiedMapPIDs = channelEPG.AddEvents(scanRes);

                // save to DB
                foreach (var mapPID in modifiedMapPIDs)
                {
                    AddEventsToSave(Convert.ToInt32(mapPID), channelEPG.EventItems[mapPID]);
                }

                return new EITScanResult()
                {
                    OK = true
                };
            } finally
            {
                _scanning = false;
            }
        }
    }
}
