using SQLite;
using System;
using System.Collections.Generic;
using System.Text;

namespace DVBTTelevizor
{
    [Table("Channels")]
    public class Channel
    {
        [PrimaryKey, Column("Number")]
        public int Number { get; set; }

        public long Frequency { get; set; }

        
        public string FrequencyLabel
        {
            get
            {
                return "Freq: " + Frequency/1000000 + " Mhz";
            }
        }

        public long Bandwdith { get; set; }

        public int DVBTType { get; set; }

        //[Column("Name")]
        public string Name { get; set; }

        //[Column("ProviderName")]
        public string ProviderName { get; set; }

        //[Column("PIDs")]
        public string PIDs { get; set; }

        public string PIDsLabel
        {
            get
            {
                return "PIDs: " + PIDs;
            }
        }

    }
}
