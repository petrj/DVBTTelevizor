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

        public DVBTServiceType ServiceType { get; set; }

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
                return ch.ToString();
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

                switch (ServiceType)
                {
                    case DVBTServiceType.Radio:
                    case DVBTServiceType.TV:
                        return $"{res} {ServiceType}";
                    default:
                        return $"{res}";
                }
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

    }
}
