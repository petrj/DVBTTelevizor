using MPEGTS;
using System;
using System.Collections.Generic;
using System.Text;

namespace DVBTTelevizor.TV
{
    public class EPGEventItem : JSONObject
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

        public string EventName
        {
            get => _eventItem?.EventName ?? string.Empty;
        }

        public string TimeDescription
        {
            get => _eventItem?.TimeDescription ?? string.Empty;
        }

        public string Text
        {
            get => _eventItem?.Text ?? string.Empty;
        }

        public string EventID
        {
            get => _eventItem?.EventId.ToString() ?? string.Empty;
        }

        public void NotifyChanges()
        {
            OnPropertyChanged(nameof(BackgroundColor));
            //OnPropertyChanged(nameof(ChannelTextLabelColor));
            //OnPropertyChanged(nameof(CurrentEPGEventTitle));
            //OnPropertyChanged(nameof(NextEPGEventTitle));
            //OnPropertyChanged(nameof(CurrentEPGEventTime));
            //OnPropertyChanged(nameof(CurrentEPGEventProgress));
            //OnPropertyChanged(nameof(ProviderNameAndFrequencyShortLabel));
        }
    }
}
