using MPEGTS;
using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DVBTTelevizor
{
    public class ChannelEPG
    {
        public List<long> AddEvents(EITScanResult scanRes)
        {
            var modifiedKeys = new List<long>();

            if (scanRes.CurrentEvents != null)
            {
                // adding current events with negative ID
                foreach (var ev in scanRes.CurrentEvents)
                {
                    if (!EventItems.ContainsKey(ev.Key))
                    {
                        EventItems.Add(ev.Key, new List<EventItem>());
                    }

                    var evCloned = ev.Value.Clone();
                    evCloned.EventId = -evCloned.EventId;

                    AddEvent(ev.Key, evCloned);
                    modifiedKeys.Add(ev.Key);
                }
            }

            if (scanRes.ScheduledEvents != null)
            {
                // adding scheduled events
                foreach (var sev in scanRes.ScheduledEvents)
                {
                    if (!EventItems.ContainsKey(sev.Key))
                    {
                        EventItems.Add(sev.Key, new List<EventItem>());
                    }

                    foreach (var ev in sev.Value)
                    {
                        AddEvent(sev.Key, ev);
                    }

                    modifiedKeys.Add(sev.Key);
                }
            }

            foreach (var key in modifiedKeys)
            {
                EventItems[key].Sort();
            }

            return modifiedKeys;
        }

        // MapPID -> List of events
        public Dictionary<long, List<EventItem>> EventItems { get; set; } = new Dictionary<long, List<EventItem>>();

        public void AddEvent(long mapPID, EventItem item)
        {
            if (EventExists(mapPID, item))
                return;

            if (!EventItems.ContainsKey(mapPID))
            {
                EventItems.Add(mapPID, new List<EventItem>());
            }

            if (item.FinishTime < DateTime.Now)
                return;

            EventItems[mapPID].Add(item);
        }

        private bool EventExists(long mapPID, EventItem item)
        {
            if (!EventItems.ContainsKey(mapPID))
            {
                return false;
            }

            foreach (var eventItem in EventItems[mapPID])
            {
                if (eventItem.SameEvent(item))
                {
                    return true;
                }
            }

            return false;
        }

        public List<EventItem> GetEvents(DateTime date, long programMapPID, int count)
        {
            if (!EventItems.ContainsKey(programMapPID))
            {
                return null;
            }

            var res = new List<EventItem>();

            EventItem currentEvent = null;
            var c = 0;

            foreach (var ev in EventItems[programMapPID])
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
                } else
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
    }
}
