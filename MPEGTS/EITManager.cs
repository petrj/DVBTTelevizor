using System;
using System.Collections.Generic;
using System.Text;

namespace MPEGTS
{
    public class EITManager
    {
        /// <summary>
        /// ServiceID (program number) -> current event
        /// </summary>
        public Dictionary<int, EventItem> CurrentEvents { get; set; }  = new Dictionary<int, EventItem>();

        public Dictionary<int, List<EventItem>> ScheduledEvents { get; set; } = new Dictionary<int, List<EventItem>>();

        public Dictionary<int, int> ProgramNumberToMapPID { get; set; } = new Dictionary<int, int>();

        public bool Scan(List<byte> bytes)
        {
            if (bytes == null || bytes.Count == 0)
            {
                return false;
            }

            var packets = MPEGTransportStreamPacket.Parse(bytes);
            return Scan(packets);
        }

        /// <summary>
        /// Scanning actual and scheduled events for actual TS
        /// </summary>
        /// <param name="packets"></param>
        public bool Scan(List<MPEGTransportStreamPacket> packets)
        {
            var eitData = MPEGTransportStreamPacket.GetAllPacketsPayloadBytesByPID(packets, 18);

            var eventIDs = new Dictionary<int, Dictionary<int, EventItem>>(); // ServiceID -> (event id -> event item )

            foreach (var kvp in eitData)
            {
                try
                {
                    var eit = DVBTTable.Create<EITTable>(kvp.Value);

                    if (eit == null || !eit.CRCIsValid())
                        continue;

                    if (eit.ID == 78) // actual TS, present/following event information = table_id = 0x4E;
                    {
                        foreach (var item in eit.EventItems)
                        {
                            CurrentEvents[eit.ServiceId] = item;

                            break; // reading only the first one
                        }
                    }
                    else
                    if (eit.ID >= 80 && eit.ID <= 95) // actual TS, event schedule information = table_id = 0x50 to 0x5F;
                    {
                        foreach (var item in eit.EventItems)
                        {
                            if (!eventIDs.ContainsKey(eit.ServiceId))
                            {
                                eventIDs[eit.ServiceId] = new Dictionary<int, EventItem>();
                            }

                            var serviceItems = eventIDs[eit.ServiceId];

                            if (!serviceItems.ContainsKey(item.EventId))
                            {
                                serviceItems.Add(item.EventId, item);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Console.WriteLine($"Bad data ! {ex}");
                }
            }

            // transform to List and sorting:

            foreach (var kvp in eventIDs)
            {
                if (!ScheduledEvents.ContainsKey(kvp.Key))
                {                    
                    ScheduledEvents[kvp.Key] = new List<EventItem>();
                }

                ScheduledEvents[kvp.Key].AddRange(kvp.Value.Values);

                ScheduledEvents[kvp.Key].Sort();
            }

            var psiTable = DVBTTable.CreateFromPackets<PSITable>(packets, 0);
            if (psiTable != null && psiTable.ProgramAssociations!= null)
            {
                foreach (var kvp in psiTable.ProgramAssociations)
                {
                    ProgramNumberToMapPID[kvp.ProgramNumber] = kvp.ProgramMapPID;
                }
            }

            return true;
        }

        public EventItem GetEvent(DateTime date, int programMapPID)
        {
            var events = GetEvents(date, 1);

            if (events.ContainsKey(programMapPID) &&
                events[programMapPID].Count>0)
            {
                return events[programMapPID][0];
            }

            return null;
        }

        /// <summary>
        ///  Scheduled events supplemented with actual events
        /// </summary>
        /// <param name="date"></param>
        /// <param name="count"></param>
        /// <returns>Key: Program MapPID</returns>
        public Dictionary<int, List<EventItem>> GetEvents(DateTime date, int count = 2)
        {
            var res = new Dictionary<int, List<EventItem>>();

            // scheduled events

            foreach (var serviceId in ScheduledEvents.Keys)
            {
                if (!ProgramNumberToMapPID.ContainsKey(serviceId))
                    continue;

                var programMpaPID = ProgramNumberToMapPID[serviceId];

                res[programMpaPID] = new List<EventItem>();

                int actualCount = 0;
                foreach (var ev in ScheduledEvents[serviceId])
                {
                    if (actualCount == 0 &&
                        ev.StartTime <=date &&
                        ev.FinishTime >= date)
                    {
                        // actual running event
                        res[programMpaPID].Add(ev);
                        actualCount++;
                        continue;
                    }

                    if (actualCount >= count)
                    {
                        break;
                    }

                    if (actualCount >= 1)
                    {
                        res[programMpaPID].Add(ev);
                        actualCount++;
                    }
                }
            }

            // adding current events when scheduled not exists

            foreach (var kvp in CurrentEvents)
            {
                if (!res.ContainsKey(kvp.Key) &&
                    kvp.Value.StartTime <= date &&
                    kvp.Value.FinishTime >= date)
                {
                    if (!ProgramNumberToMapPID.ContainsKey(kvp.Key))
                        continue;

                    var programMpaPID = ProgramNumberToMapPID[kvp.Key];

                    res[programMpaPID] = new List<EventItem>();
                    res[programMpaPID].Add(kvp.Value);
                }
            }

            return res;
        }
    }
}
