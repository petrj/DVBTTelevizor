using System;
using System.Collections.Generic;
using System.Text;

namespace MPEGTS
{
    public class EventItem : IComparable
    {
        public DateTime StartTime { get; set; }
        public DateTime FinishTime { get; set; }

        public int EventId { get; set; }
        public int ServiceId { get; set; }

        public string LanguageCode { get; set; }
        public string EventName { get; set; }
        public string Text { get; set; }

        public string TextValue
        {
            get
            {
                var res = string.Empty;
                if (!String.IsNullOrEmpty(EventName))
                {
                    res += EventName;
                }
                if (!string.IsNullOrEmpty(Text))
                {
                    if (!string.IsNullOrEmpty(res))
                    {
                        res += $" ({Text})";
                    } else
                    {
                        res = Text;
                    }
                }

                return res;
            }
        }

        public int CompareTo(object obj)
        {
            if (!(obj is EventItem))
                return 0;

            var eit = obj as EventItem;

            if (eit.StartTime > StartTime)
                return -1;

            if (eit.StartTime < StartTime)
                return 1;

            return 0;
        }

        public string WriteToString()
        {
            return ($"{ServiceId,14} {StartTime.ToString("dd.MM.yyyy")} {StartTime.ToString("HH:mm")}-{FinishTime.ToString("HH:mm")} {TextValue}");
        }

        public static EventItem Create(int eventId, int serviceId, DateTime start, DateTime finish, ShortEventDescriptor shortEventDescriptor)
        {
            var res = new EventItem();

            res.EventId = eventId;
            res.ServiceId = serviceId;

            res.StartTime = start;
            res.FinishTime = finish;
            res.LanguageCode = shortEventDescriptor.LanguageCode;

            res.EventName = shortEventDescriptor.EventName;
            res.Text = shortEventDescriptor.Text;

            return res;
        }
    }
}
