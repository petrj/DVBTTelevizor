using System;
using System.Collections.Generic;
using System.Text;

namespace MPEGTS
{
    public class EITManager
    {
        /// <summary>
        /// ServiceID -> current event
        /// </summary>
        public Dictionary<int, EventItem> CurrentEvents { get; set; }  = new Dictionary<int, EventItem>();

        public Dictionary<int, List<EventItem>> ScheduledEvents { get; set; } = new Dictionary<int, List<EventItem>>();

        /// <summary>
        /// Scanning actual and scheduled events for actual TS
        /// </summary>
        /// <param name="packets"></param>
        public void Scan(List<MPEGTransportStreamPacket> packets)
        {
            var eitData = MPEGTransportStreamPacket.GetAllPacketsPayloadBytesByPID(packets, 18);

            var eventIDs = new Dictionary<int, Dictionary<int,EventItem>>(); // ServiceID -> (event id -> event item )

            foreach (var kvp in eitData)
            {
                try
                {
                    var eit = EITTable.Parse(kvp.Value);

                    if (eit == null)
                        continue;

                    if (eit.ID == 78) // actual TS, present/following event information = table_id = 0x4E;
                    {
                        foreach (var item in eit.EventItems)
                        {
                            CurrentEvents[eit.ServiceId] = item;

                            break; // reading only the first one
                        }
                    } else
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
                if (ScheduledEvents.ContainsKey(kvp.Key))
                {
                    ScheduledEvents[kvp.Key].Clear();
                } else
                {
                    ScheduledEvents[kvp.Key] = new List<EventItem>();
                }

                ScheduledEvents[kvp.Key].AddRange(kvp.Value.Values);

                ScheduledEvents[kvp.Key].Sort();
            }
        }
    }
}
