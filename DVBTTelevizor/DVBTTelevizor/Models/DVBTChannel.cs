using MPEGTS;
using SQLite;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace DVBTTelevizor
{
    [Table("Channels")]
    public class DVBTChannel : JSONObject
    {
        [PrimaryKey, Column("Number")]
        public string Number { get; set; }

        public long Frequency { get; set; }

        public long ProgramMapPID { get; set; }        

        public ServiceTypeEnum Type { get; set; } = ServiceTypeEnum.Other;

        public DVBTServiceType ServiceType { get; set; } = DVBTServiceType.Other;        

        public string FrequencyLabel
        {
            get
            {
                return Frequency/1000000 + " Mhz";
            }
        }

        public string ChannelLabel
        {
            get
            {
                var ch = (Convert.ToInt32(Frequency / 1000000) - 306) / 8;
                return "CH #" + ch.ToString();
            }
        }

        public long Bandwdith { get; set; }

        public string BandwdithLabel
        {
            get
            {
                return Bandwdith / 1000000 + " Mhz";
            }
        }

        public int DVBTType { get; set; }

        //[Column("Name")]
        public string Name { get; set; }

        //[Column("ProviderName")]
        public string ProviderName { get; set; }

        public bool Recording { get; set; }

        public string RecordingLabel
        {
            get
            {
                if (Recording)
                    return "REC";

                return String.Empty;
            }
        }

        //[Column("PIDs")]
        public string PIDs { get; set; }

        public string PIDsLabel
        {
            get
            {
                return $"PIDs: {ProgramMapPID.ToString()},{PIDs}";
            }
        }

        public string DVBTTypeLabel
        {
            get
            {
                var res = String.Empty;
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
                    case DVBTServiceType.Radio:
                    case DVBTServiceType.TV:
                        return $"{res} {ServiceType}";
                    default:
                        return $"{res}";
                }
            }
        }

        public string DVBTChannelNubmer
        {
            get
            {
                return (((Frequency / 1000000) - 306) / 8).ToString();
            }
        }


        public string ServiceTypelWithChannelLabel
        {
            get
            {
                return SimplifiedServiceType + ", " + ChannelLabel;
            }
        }

        public List<long> PIDsArary
        {
            get
            {
                var res = new List<long>();
                res.Add(ProgramMapPID);
                res.Add(0);  // PSI
                res.Add(16); // NIT
                res.Add(17); // SDT
                res.Add(18); // EIT

                if (!String.IsNullOrEmpty(PIDs))
                {
                    foreach (var pid in PIDs.Split(','))
                    {
                        res.Add(Convert.ToInt64(pid));
                    }
                }

                return res;
            }
        }

        public DVBTServiceType SimplifiedServiceType
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
                        return DVBTServiceType.Radio;
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
                        return DVBTServiceType.TV;
                }

                return DVBTServiceType.Other;
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
                        case DVBTServiceType.TV:
                            return "TV";
                        case DVBTServiceType.Radio:
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

    }
}
