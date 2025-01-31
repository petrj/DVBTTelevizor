using DVBTTelevizor;
using MPEGTS;
using SQLite;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace DVBTTelevizor
{
    [Table("Channels")]
    public class Channel : JSONObject
    {
        [PrimaryKey, Column("Number")]
        public string Number { get; set; } = "0";

        public long Frequency { get; set; }
        public long ProgramMapPID { get; set; }
        public ServiceTypeEnum Type { get; set; } = ServiceTypeEnum.Other;
        public DVBTDriverServiceType ServiceType { get; set; } = DVBTDriverServiceType.Other;
        public EventItem? CurrentEventItem { get; set; } = null;
        public EventItem? NextEventItem { get; set; } = null;

        public long Bandwdith { get; set; }
        public int DVBTType { get; set; }
        public string? Name { get; set; } = null;
        public string? ProviderName { get; set; } = null;
        public bool NonFree { get; set; } = false; // backward compatibility!!!

        private bool _recording = false;

        public string BandwdithLabel
        {
            get
            {
                return (Bandwdith / 1000000).ToString("N3") + " MHz";
            }
        }

        public string FreeLabel
        {
            get
            {
                return NonFree ? "No (CA)" : "Yes";
            }
        }

        public string LockIcon
        {
            get
            {
                return NonFree ? "Lock.png" : "Empty.png";
            }
        }

        public string FrequencyLabel
        {
            get
            {
                return (Frequency / 1000000).ToString("N3") + " MHz";
            }
        }

        public string FrequencyShortLabel
        {
            get
            {
                if (Frequency % 1000000 == 0)
                {
                    return (Frequency / 1000000).ToString("N0") + " MHz";
                }

                return (Frequency / 1000000).ToString("N3") + " MHz";
            }
        }

        public bool Recording
        {
            get
            {
                return _recording;
            }
            set
            {
                _recording = value;
                OnPropertyChanged(nameof(RecordingLabel));
            }
        }

        public string RecordingLabel
        {
            get
            {
                if (Recording)
                    return "\u25CF";

                return string.Empty;
            }
        }

        public string DVBTTypeLabel
        {
            get
            {
                var res = string.Empty;
                if (DVBTType == 0)
                {
                    res = "DVBT";
                }
                if (DVBTType == 1)
                {
                    res = "DVBT2";
                }

                switch (SimplifiedServiceType)
                {
                    case DVBTDriverServiceType.Radio:
                    case DVBTDriverServiceType.TV:
                        return $"{res} {SimplifiedServiceType}";
                    default:
                        return $"{res}";
                }
            }
        }

        public DVBTDriverServiceType SimplifiedServiceType
        {
            get
            {
                if (Type == ServiceTypeEnum.Other)
                {
                    return ServiceType;
                }

                switch (Type)
                {
                    case ServiceTypeEnum.DigitalRadioSoundService:
                    case ServiceTypeEnum.AdvancedCodecDigitalRadioSoundService:
                        return DVBTDriverServiceType.Radio;
                    case ServiceTypeEnum.DigitalTelevisionService:
                    case ServiceTypeEnum.NVODReferenceService:
                    case ServiceTypeEnum.NVODTimeShiftedService:
                    case ServiceTypeEnum.MPEG2HDDigitalTelevisionService:
                    case ServiceTypeEnum.H264AVCSDDigitalTelevisionService:
                    case ServiceTypeEnum.H264AVCSDNVODReferenceService:
                    case ServiceTypeEnum.H264AVCSDNVODTimeShiftedService:
                    case ServiceTypeEnum.H264AVCHDDigitalTelevisionService:
                    case ServiceTypeEnum.H264AVCHDNVODReferenceService:
                    case ServiceTypeEnum.H264AVCHDNVODRTimeShiftedService:
                    case ServiceTypeEnum.H264AVCFrameCompatiblePlanoStereoscopicHDNVODTimeShiftedService:
                    case ServiceTypeEnum.H264AVCFrameCompatiblePlanoStereoscopicHDNVODReferenceService:
                    case ServiceTypeEnum.H264AVCFrameCompatiblePlanoStereoscopicHDDigitalTelevisionService:
                    case ServiceTypeEnum.HEVCDigitalTelevisionService:
                        return DVBTDriverServiceType.TV;
                }

                return DVBTDriverServiceType.Other;
            }
        }

        public string ServiceTypeLabel
        {
            get
            {
                if (Type == ServiceTypeEnum.Other)
                {
                    switch (ServiceType)
                    {
                        case DVBTDriverServiceType.TV:
                            return "TV";
                        case DVBTDriverServiceType.Radio:
                            return "Radio";
                    }

                    return "Other/unknown";
                }

                switch (Type)
                {
                    case ServiceTypeEnum.DigitalRadioSoundService:
                        return "Radio";
                    case ServiceTypeEnum.AdvancedCodecDigitalRadioSoundService:
                        return "Advanced codec radio";
                    case ServiceTypeEnum.DigitalTelevisionService:
                        return "TV";
                    case ServiceTypeEnum.NVODReferenceService:
                    case ServiceTypeEnum.NVODTimeShiftedService:
                        return "NVOD";
                    case ServiceTypeEnum.MPEG2HDDigitalTelevisionService:
                        return "MPEG2 HD TV";
                    case ServiceTypeEnum.H264AVCSDDigitalTelevisionService:
                    case ServiceTypeEnum.H264AVCSDNVODReferenceService:
                    case ServiceTypeEnum.H264AVCSDNVODTimeShiftedService:
                        return "H.264/AVC SD TV";
                    case ServiceTypeEnum.H264AVCHDDigitalTelevisionService:
                    case ServiceTypeEnum.H264AVCHDNVODReferenceService:
                    case ServiceTypeEnum.H264AVCHDNVODRTimeShiftedService:
                        return "H.264/AVC HD TV";
                    case ServiceTypeEnum.H264AVCFrameCompatiblePlanoStereoscopicHDNVODTimeShiftedService:
                    case ServiceTypeEnum.H264AVCFrameCompatiblePlanoStereoscopicHDNVODReferenceService:
                    case ServiceTypeEnum.H264AVCFrameCompatiblePlanoStereoscopicHDDigitalTelevisionService:
                        return "H.264/AVC stereoscopic TV";
                    case ServiceTypeEnum.HEVCDigitalTelevisionService:
                        return "HEVC TV";
                }

                return "Other/unknown";
            }
        }

        public string CurrentEPGEventTitle
        {
            get
            {
                if (CurrentEventItem == null)
                    return string.Empty;

                return CurrentEventItem.EventName;
            }
        }

        public double CurrentEPGEventProgress
        {
            get
            {
                var epg = CurrentEventItem;

                return epg == null
                    ? 0
                    : epg.Progress;
            }
        }

        public string CurrentEPGEventTime
        {
            get
            {
                if (CurrentEventItem == null ||
                    CurrentEventItem.StartTime > DateTime.Now ||
                    CurrentEventItem.FinishTime < DateTime.Now)
                    return string.Empty;

                return CurrentEventItem.StartTime.ToString("HH:mm") + " - " + CurrentEventItem.FinishTime.ToString("HH:mm");
            }
        }

        public string NextEPGEventTitle
        {
            get
            {
                if (NextEventItem == null)
                    return string.Empty;

                return "\u2192 " + NextEventItem.EventName;
            }
        }

        public void ClearEPG()
        {
            CurrentEventItem = null;
            NextEventItem = null;
        }

        public void SetCurrentEvent(EPGCurrentEvent currentEvent)
        {
            if (currentEvent == null)
                return;

            if (currentEvent.CurrentEventItem != null)
                CurrentEventItem = currentEvent.CurrentEventItem;

            if (currentEvent.NextEventItem != null)
                NextEventItem = currentEvent.NextEventItem;
        }

        public void NotifyEPGChanges()
        {
            OnPropertyChanged(nameof(CurrentEPGEventTitle));
            OnPropertyChanged(nameof(NextEPGEventTitle));
            OnPropertyChanged(nameof(CurrentEPGEventTime));
            OnPropertyChanged(nameof(CurrentEPGEventProgress));
        }

        public string Icon
        {
            get
            {
                switch (SimplifiedServiceType)
                {
                    case DVBTDriverServiceType.TV: return "TV.png";
                    case DVBTDriverServiceType.Radio: return "Radio.png";
                }

                return null;
            }
        }

        public Channel Clone()
        {
            var channel = new Channel();

            channel.Name = Name;
            channel.ProviderName = ProviderName;
            channel.Frequency = Frequency;
            channel.Bandwdith = Bandwdith;
            channel.ProgramMapPID = ProgramMapPID;
            channel.ServiceType = ServiceType;
            channel.Type = Type;
            channel.Bandwdith = Bandwdith;
            channel.DVBTType = DVBTType;
            channel.Number = Number;
            channel.NonFree = NonFree;

            return channel;
        }

        public string FrequencyAndMapPID
        {
            get
            {
                return Frequency.ToString() + "[" + ProgramMapPID.ToString() + "]";
            }
        }

        public bool ChannelExists(ObservableCollection<Channel> channels)
        {
            foreach (var ch in channels)
            {
                if (ch.FrequencyAndMapPID == FrequencyAndMapPID)
                {
                    return true;
                }
            }

            return false;
        }


        public static int GetNextChannelNumber(ObservableCollection<Channel> channels)
        {
            var res = 0;

            foreach (var ch in channels)
            {
                int n;
                if (int.TryParse(ch.Number, out n))
                {
                    if (n > res)
                    {
                        res = n;
                    }
                }
            }

            return res + 1;
        }
    }
}
