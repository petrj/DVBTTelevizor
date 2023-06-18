using DVBTTelevizor.Models;
using LoggerService;
using MPEGTS;
using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace DVBTTelevizor.Services
{
    public class EITManager
    {
        private ILoggingService _log;
        private IDVBTDriverManager _driver;

        public EITManager(ILoggingService loggingService, IDVBTDriverManager driver)
        {
            _log = loggingService;
            _driver = driver;
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

        public Dictionary<long, ChannelEPG> FreqEPG { get; set; } = new Dictionary<long, ChannelEPG>();

        public EPGCurrentEvent GetEvent(DateTime date, long freq, long programMapPID)
        {
            var evs = GetEvents(date, freq, programMapPID, 2);

            if (evs == null || evs.Count == 0)
                return null;

            var res = new EPGCurrentEvent();

            if (evs.Count>0)
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

            var scanRes = await _driver.ScanEPG(msTimeout);

            if (!scanRes.OK)
            {
                _log.Debug($"[EIT] scanning failed");
                return scanRes;
            }

            if (scanRes.UnsupportedEncoding)
            {
                _log.Debug($"[EIT] unsupported encoding");
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
                var db = new SQLiteConnection(GetDBPath(_driver.LastTunedFreq, mapPID));

                db.DropTable<EventItem>();

                db.CreateTable<EventItem>();

                foreach (var ev in channelEPG.EventItems[mapPID])
                {
                    db.Insert(ev);
                }

                db.Close();
            }

            return new EITScanResult()
            {
                OK = true
            };
        }
    }
}
