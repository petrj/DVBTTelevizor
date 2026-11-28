using MPEGTS;
using System;
using System.Collections.Generic;
using System.Text;

namespace DVBTTelevizor.TV
{
    public class EPGEventItem : JSONObject, IListViewSelector
    {
        private EventItem? _eventItem;

        public string CSV
        {
            get
            {
                if (_eventItem == null)
                    return string.Empty;

                return $"{_eventItem.StartTime},{_eventItem.FinishTime},{EventName},{EventID},{Text}";
            }
        }

        public EPGEventItem(EventItem? eventItem)
        {
            _eventItem = eventItem;
        }

        public bool Selected { get; set; } = false;
        public bool Focused { get; set; } = false;

        public string BackgroundColor
        {
            get
            {
                return Selected
                    ? Focused
                        ? "#007cd2"
                        : "#005794"   // unfocused
                    : "Transparent";  // unselected
            }
        }

        public string TextColor
        {
            get
            {
                //return Selected || Focused ?  "White" : "#31a3cf";
                if (Focused)
                {
                    return "White";
                }
                return "#41b3ff";
            }
        }

        public string EventName
        {
            get => _eventItem?.EventName ?? string.Empty;
        }

        public string TimeDescription
        {
            get => _eventItem?.TimeDescription ?? string.Empty;
        }

        public string DateDescription
        {
            get
            {

                if (_eventItem == null)
                {
                    return string.Empty;
                }

                if (_eventItem.FinishTime == DateTime.MinValue || _eventItem.StartTime == DateTime.MinValue || _eventItem.FinishTime == DateTime.MaxValue || _eventItem.StartTime == DateTime.MaxValue)
                {
                    return string.Empty;
                }

                return $"{_eventItem.StartTime:d.M.yy}";
            }
        }

        public string Text
        {
            get => _eventItem?.Text ?? string.Empty;
        }

        public string EventID
        {
            get => _eventItem?.EventId.ToString() ?? string.Empty;
        }

        public string UniqueIdentifier
        {
            get => CSV;
        }

        public string Number
        {
            get => EventID;
        }

        public double Progress
        {
            get => _eventItem?.Progress ?? 0.0;
        }

        public string StartTime
        {
            get => _eventItem?.StartTime == null ? String.Empty : _eventItem?.StartTime.ToString("HH:mm");
        }

        public string FinishTime
        {
            get => _eventItem?.FinishTime == null ? String.Empty : _eventItem?.FinishTime.ToString("HH:mm");
        }


        public void NotifyChanges()
        {
            OnPropertyChanged(nameof(BackgroundColor));
            OnPropertyChanged(nameof(TextColor));
            OnPropertyChanged(nameof(DateDescription));
            OnPropertyChanged(nameof(TimeDescription));
            OnPropertyChanged(nameof(EventName));
            OnPropertyChanged(nameof(StartTime));
            OnPropertyChanged(nameof(FinishTime));
            OnPropertyChanged(nameof(Progress));
        }
    }
}
