using LoggerService;
using System;
using System.Collections.Generic;
using System.Text;

namespace MPEGTS
{
    public class EITManager
    {
        private ILoggingService _log;

        public EITManager(ILoggingService loggingService)
        {
            _log = loggingService;
        }

        /// <summary>
        /// ServiceID (program number) -> current event
        /// </summary>
        public Dictionary<int, EventItem> CurrentEvents { get; set; }  = new Dictionary<int, EventItem>();

        /// <summary>
        /// ServiceID (program number) -> List of events
        /// </summary>
        public Dictionary<int, List<EventItem>> ScheduledEvents { get; set; } = new Dictionary<int, List<EventItem>>();

        public Dictionary<int, int> ProgramNumberToMapPID { get; set; } = new Dictionary<int, int>();

        public EITScanResult Scan(List<byte> bytes)
        {
            if (bytes == null || bytes.Count == 0)
            {
                return new EITScanResult()
                {
                    OK = false
                };
            }

            var packets = MPEGTransportStreamPacket.Parse(bytes);
            return Scan(packets);
        }

        /// <summary>
        /// Scanning actual and scheduled events for actual TS
        /// </summary>
        /// <param name="packets"></param>
        public EITScanResult Scan(List<MPEGTransportStreamPacket> packets)
        {
            _log.Debug($"Scanning EIT from packets");

            var res = new EITScanResult()
            {
                OK = true
            };

            try
            {
                var eitData = MPEGTransportStreamPacket.GetAllPacketsPayloadBytesByPID(packets, 18);

                _log.Debug($"EIT packets count: {eitData.Count}");

                var eventIDs = new Dictionary<int, Dictionary<int, EventItem>>(); // ServiceID -> (event id -> event item )

                var currentEventsCountFound = 0;
                var scheduledEventsCountFound = 0;

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
                                if (item.StartTime < DateTime.Now &&
                                    item.FinishTime > DateTime.Now)
                                {
                                    // reading only the actual event
                                    // there can be event that start in future!

                                    CurrentEvents[eit.ServiceId] = item;                                    

                                    currentEventsCountFound++;

                                    break; 
                                }
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

                                    scheduledEventsCountFound++;
                                }
                            }
                        }
                    }
                    catch (MPEGTSUnsupportedEncodingException)
                    {
                        res.UnsupportedEncoding = true;                    
                    }
                    catch (Exception ex)
                    {
                        // Bad data EIT data
                    }
                }

                _log.Debug($"Scheduled Events found: {scheduledEventsCountFound}");
                _log.Debug($"Current Events found: {currentEventsCountFound}");

                // transform to List and sorting:

                foreach (var kvp in eventIDs)
                {
                    foreach (var eventItem in kvp.Value)
                    {
                        AddScheduledEventItem(kvp.Key, eventItem.Value);
                    }
                    ScheduledEvents[kvp.Key].Sort();
                }

                var psiTable = DVBTTable.CreateFromPackets<PSITable>(packets, 0);
                if (psiTable != null && psiTable.ProgramAssociations != null)
                {
                    foreach (var kvp in psiTable.ProgramAssociations)
                    {
                        _log.Debug($"Associate  program number {kvp.ProgramNumber} to PID {kvp.ProgramMapPID}");
                        ProgramNumberToMapPID[kvp.ProgramNumber] = kvp.ProgramMapPID;
                    }
                }
                else
                {
                    _log.Debug($"No PSI table found");
                }

            } catch (Exception e)
            {
                _log.Error(e);
                res.OK = false;
            }            

            return res;
        }

        public void AddScheduledEventItem(int serviceId, EventItem eventItem)
        {
            if (!ScheduledEvents.ContainsKey(serviceId))
            {
                ScheduledEvents[serviceId] = new List<EventItem>();
            }

            foreach (var item in ScheduledEvents[serviceId])
            {
                if (item.EventId == eventItem.EventId)
                {
                    return;
                }
            }

            ScheduledEvents[serviceId].Add(eventItem);
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
        /// <param name="count">only 1 or 2 supporting</param>
        /// <returns>Key: Program MapPID</returns>
        public Dictionary<int, List<EventItem>> GetEvents(DateTime date, int count = 2)
        {
            var res = new Dictionary<int, List<EventItem>>();

            // current events:

            foreach (var kvp in CurrentEvents)
            {
                if (kvp.Value.StartTime <= date &&
                    kvp.Value.FinishTime >= date &&
                    ProgramNumberToMapPID.ContainsKey(kvp.Key))
                {
                    var programMapPID = ProgramNumberToMapPID[kvp.Key];

                    res[programMapPID] = new List<EventItem>();
                    res[programMapPID].Add(kvp.Value);                    
                }
            }


            // scheduled events

            foreach (var serviceId in ScheduledEvents.Keys)
            {
                if (!ProgramNumberToMapPID.ContainsKey(serviceId))
                    continue;

                var programMapPID = ProgramNumberToMapPID[serviceId];

                if (!res.ContainsKey(programMapPID))
                {
                    res[programMapPID] = new List<EventItem>();
                }

                EventItem currentEvent = null;

                foreach (var ev in ScheduledEvents[serviceId])
                {
                    if (ev.StartTime <= date &&
                        ev.FinishTime >= date)
                    {
                        // actual running event found

                        if (res[programMapPID].Count == 0)
                        {
                            // cuurrent event not added
                            res[programMapPID].Add(ev);
                            currentEvent = ev;
                        }
                        else 
                        {
                            currentEvent = res[programMapPID][0];
                        }

                        if (count == 1)
                        {
                            break; // second event not wanted
                        }
                    }
                    
                    if (currentEvent != null &&
                        currentEvent.FinishTime == ev.StartTime )
                    {
                        // found second event
                        res[programMapPID].Add(ev);
                        break;
                    }
                }
            }           

            return res;
        }
    }
}
