using DVBTTelevizor.MAUI;
using LoggerService;
using MPEGTS;
using SQLite;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

        public EPGCurrentEvent GetEvent(DateTime date, string key)
        {
            var evs = GetEvents(date, key, 2);

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

        public List<EventItem> GetEvents(DateTime date, string key, int count)
        {
            var events = GetValues(key);

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

        private void RemoveCurrentEvents(string key)
        {
            if (!_freqValues.ContainsKey(key))
            {
                return;
            }

            var eventItemsToDelete = new List<EventItem>();

            foreach (var ev in _freqValues[key])
            {
                if (ev.EventId<0)
                {
                    eventItemsToDelete.Add(ev);
                }
            }

            while (eventItemsToDelete.Count > 0)
            {
                _freqValues[key].Remove(eventItemsToDelete.ElementAt(0));
                eventItemsToDelete.RemoveAt(0);
            }
        }

        public async Task AddOnlineEvents(Dictionary<string, List<EventItem>> items, ObservableCollection<Channel>? Channels)
        {
            _log.Debug($"[EIT] Adding online events");

            var modifiedKeys = new List<string>();

            foreach (var channelKey in items.Keys)
            {
                // find online channel by key
                var channel = Channels?.FirstOrDefault(c => (c.ChannelType == ChannelTypeEnum.SledovaniTV) && (c.ChannelId == channelKey));

                if (channel == null)
                {
                    _log.Debug($"[EIT] Channel not found for key {channelKey}");
                    continue;
                }

                foreach (var ev in items[channelKey])
                {
                    if (AddEvent(channel.UniqueIdentifier, ev))
                    {
                        modifiedKeys.Add(channel.UniqueIdentifier);
                    }
                }
            }

            foreach (var key in modifiedKeys.Distinct())
            {
                await SaveKeyToDB(key);
            }
        }

        public async Task SaveKeyToDB(string key, bool deleteOutdated = true)
        {
            if (!_freqValues.ContainsKey(key))
            {
                return;
            }

            var itemsToClear = new List<EventItem>();

            // dete outdated events

            if (deleteOutdated)
            {
                foreach (var itm in _freqValues[key])
                {
                    if (itm.FinishTime < DateTime.Now)
                    {
                        itemsToClear.Add(itm);
                    }
                }
            }

            while (itemsToClear.Count > 0)
            {
                _freqValues[key].Remove(itemsToClear[0]);
                itemsToClear.RemoveAt(0);
            }

            SaveItemsToDB(key, _freqValues[key]);

        }

        public async Task<EITScanResult> Scan(ObservableCollection<Channel>? channels, int msTimeout = 2000)
        {
            if (_driver == null)
            {
                return new EITScanResult() {  OK = false };
            }

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
                        // find online channel by key
                        var channel = channels?.FirstOrDefault(c => (c.Frequency == _driver.LastTunedFreq) && (c.ProgramMapPID == ev.Key));

                        if (channel == null)
                        {
                            _log.Debug($"[EIT] Channel not found for freq {_driver.LastTunedFreq} and programMapPID {ev.Key}");
                            continue;
                        }

                        var evCloned = ev.Value.Clone();
                        evCloned.EventId = -evCloned.EventId;

                        if (AddEvent(channel.UniqueIdentifier, evCloned))
                        {
                            if (!modifiedMapPIDs.ContainsKey(channel.UniqueIdentifier))
                            {
                                modifiedMapPIDs.Add(channel.UniqueIdentifier, null);
                            }
                        }
                    }
                }

                if (scanRes.ScheduledEvents != null)
                {
                    // adding scheduled events
                    foreach (var sev in scanRes.ScheduledEvents)
                    {
                        // find online channel by key
                        var channel = channels?.FirstOrDefault(c => (c.Frequency == _driver.LastTunedFreq) && (c.ProgramMapPID == sev.Key));

                        if (channel == null)
                        {
                            _log.Debug($"[EIT] Channel not found for freq {_driver.LastTunedFreq} and programMapPID {sev.Key}");
                            continue;
                        }

                        foreach (var ev in sev.Value)
                        {
                            if (AddEvent(channel.UniqueIdentifier, ev))
                            {
                                if (!modifiedMapPIDs.ContainsKey(channel.UniqueIdentifier))
                                {
                                    modifiedMapPIDs.Add(channel.UniqueIdentifier, null);
                                }
                            }
                        }
                    }
                }

                foreach (var kvp in modifiedMapPIDs)
                {
                    await SaveKeyToDB(kvp.Key);
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

        private bool AddEvent(string key, EventItem eventItem)
        {
            if (eventItem.FinishTime<DateTime.Now)
            {
                return false;
            }

            var cachedItems = GetValues(key);

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

            if (!_freqValues.ContainsKey(key))
            {
                _freqValues.TryAdd(key, new List<EventItem>());
            }

            // negative event IDs are for current events, so we can have duplicates with different IDs
            if (eventItem.EventId < 0)
            {
               RemoveCurrentEvents(key);
            }

            _freqValues[key].Add(eventItem);

            return true;
        }
    }
}
