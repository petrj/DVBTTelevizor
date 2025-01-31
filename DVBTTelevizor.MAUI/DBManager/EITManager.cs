using DVBTTelevizor.MAUI;
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

namespace DVBTTelevizor.DBManager
{
    public class EITManager : DBManager<EventItem>
    {
        public override string Key { get; set; } = "EIT";
        protected bool _scanning = false;

        public EITManager(ILoggingService loggingService, IPublicDirectoryProvider publicDirectoryProvider, IDriverConnector driver) :
            base(loggingService, publicDirectoryProvider, driver)
        {

        }

        public virtual bool Scanning
        {
            get
            {
                return _scanning;
            }
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


        public List<EventItem> GetEvents(DateTime date, long freq, long programMapPID, int count)
        {
            var events = GetValues(freq, programMapPID);

            if (events == null || events.Count == 0)
            {
                return null;
            }

            var res = new List<EventItem>();
            var c = 0;

            EventItem currentEvent = null;

            foreach (var ev in events)
            {
                if (currentEvent == null)
                {
                    if (ev.StartTime <= date &&
                        ev.FinishTime >= date)
                    {
                        res.Add(ev);
                        c++;
                        currentEvent = ev;
                    }
                }
                else
                {
                    res.Add(ev);
                    c++;
                }

                if (c >= count)
                {
                    break;
                }
            }

            res.Sort();

            return res;
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

                var modifiedMapPIDs = new Dictionary<string, object>();

                if (scanRes.CurrentEvents != null)
                {
                    // adding current events with negative ID
                    foreach (var ev in scanRes.CurrentEvents)
                    {
                        var evCloned = ev.Value.Clone();
                        evCloned.EventId = -evCloned.EventId;

                        if (AddEvent(_driver.LastTunedFreq, ev.Key, evCloned))
                        {
                            var key = GetKey(_driver.LastTunedFreq, ev.Key);
                            if (!modifiedMapPIDs.ContainsKey(key))
                            {
                                modifiedMapPIDs.Add(key, null);
                            }
                        }
                    }
                }

                if (scanRes.ScheduledEvents != null)
                {
                    // adding scheduled events
                    foreach (var sev in scanRes.ScheduledEvents)
                    {
                        foreach (var ev in sev.Value)
                        {
                            if (AddEvent(_driver.LastTunedFreq, sev.Key, ev))
                            {
                                var key = GetKey(_driver.LastTunedFreq, sev.Key);
                                if (!modifiedMapPIDs.ContainsKey(key))
                                {
                                    modifiedMapPIDs.Add(key, null);
                                }
                            }
                        }
                    }
                }

                foreach (var kvp in modifiedMapPIDs)
                {
                    var itemsToClear = new List<EventItem>();

                    // clearing outdated?
                    foreach (var itm in _freqValues[kvp.Key])
                    {
                        if (itm.FinishTime < DateTime.Now)
                        {
                            itemsToClear.Add(itm);
                        }
                    }

                    while (itemsToClear.Count > 0)
                    {
                        _freqValues[kvp.Key].Remove(itemsToClear[0]);
                        itemsToClear.RemoveAt(0);
                    }

                    AddItemsToDB(kvp.Key, _freqValues[kvp.Key]);
                }

                return new EITScanResult()
                {
                    OK = true
                };
            }
            catch (Exception ex)
            {
                _log.Error(ex);

                return new EITScanResult()
                {
                    OK = false
                };
            }
            finally
            {
                _scanning = false;
            }
        }

        private bool AddEvent(long freq, long programMapPID, EventItem eventItem)
        {
            if (eventItem.FinishTime<DateTime.Now)
            {
                return false;
            }

            var cachedItems = GetValues(freq, programMapPID);

            if (cachedItems != null)
            {
                foreach (var ev in cachedItems)
                {
                    if (eventItem.SameEvent(ev))
                    {
                        return false;
                    }
                }
            }

            var key = GetKey(freq, programMapPID);
            if (!_freqValues.ContainsKey(key))
            {
                _freqValues.TryAdd(key, new List<EventItem>());
            }

            _freqValues[key].Add(eventItem);

            return true;
        }
    }
}
