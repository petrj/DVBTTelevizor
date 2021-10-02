using System;
using System.Collections.Generic;
using System.Text;

namespace MPEGTS
{
    public abstract class DVBTTable
    {
        public byte ID { get; set; }
        public bool SectionSyntaxIndicator { get; set; }
        public bool Private { get; set; }
        public byte Reserved { get; set; }

        public int SectionLength { get; set; }
        public int NetworkID { get; set; }
        public int TableIDExt { get; set; }
        public int SectionNumber { get; set; }
        public int LastSectionNumber { get; set; }
        public byte Version { get; set; }
        public bool CurrentIndicator { get; set; }


        public byte[] Data { get; set; }
        public byte[] CRC { get; set; }

        public abstract void Parse(List<byte> bytes);

        public static T CreateFromPackets<T>(List<MPEGTransportStreamPacket> packets, int PID) where T : DVBTTable, new()
        {
            var filteredPackets = MPEGTransportStreamPacket.GetAllPacketsPayloadBytesByPID(packets, PID);

            foreach (var kvp in filteredPackets)
            {
                var t = new T();

                try
                {
                    t.Parse(kvp.Value);

                    if (t.CRCIsValid())
                    {
                        return t;
                    } 

                } catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }

            return null;
        }

        public static T Create<T>(List<byte> bytes) where T : DVBTTable, new()
        {
            var t = new T();
            t.Parse(bytes);
            return t;
        }

        public static string GetStringFromByteArray(byte[] bytes, int pos, int length)
        {
            var res = String.Empty;
            for (var i=pos;i<pos+length;i++)
            {
                res += Convert.ToChar(bytes[i]);
            }

            return res;
        }

        /// <summary>
        /// Parsing DataTime
        /// </summary>
        /// <param name="bytes">5 byte array</param>
        /// <param name="pos">byte array start position</param>
        /// <returns></returns>
        public static DateTime ParseTime(List<byte> bytes, int pos = 0)
        {
            // start_time: This 40-bit field contains the start time of the event in Universal Time, Co-ordinated (UTC) and Modified
            //  Julian Date(MJD) (see annex C). This field is coded as 16 bits giving the 16 LSBs of MJD followed by 24 bits coded as
            //  6 digits in 4 - bit Binary Coded Decimal(BCD).If the start time is undefined (e.g. for an event in a NVOD reference
            //  service) all bits of the field are set to "1".

            var MJD = (bytes[pos + 0] << 8) + bytes[pos + 1];

            // To find Y, M, D from MJD
            // Y' = int [ (MJD - 15 078,2) / 365,25 ]
            // M' = int { [ MJD - 14 956,1 - int (Y' × 365,25) ] / 30,6001 }
            // D = MJD - 14 956 - int (Y' × 365,25) - int (M' × 30,6001)
            // If M' = 14 or M' = 15, then K = 1; else K = 0
            // Y = Y' + K
            // M = M' - 1 - K × 12

            // 93/10/13 12:45:00 is coded as "0xC079124500".

            var y2 = Math.Floor((MJD - 15078.2) / 365.25);  // 93.62.........
            var m2 = Math.Floor((MJD - 14956.1 - Math.Floor(y2 * 365.25)) / 30.6001);
            var day = MJD - 14956 - Math.Floor(y2 * 365.25) - Math.Floor(m2 * 30.6001);

            var k = (m2 == 14 || m2 == 15) ? 1 : 0;

            var yearFrom1900 = y2 + k;
            var year = 1900 + yearFrom1900;
            var month = m2 - 1 - k * 12;

            var decNumber = bytes[pos + 2] << 16;
            decNumber += bytes[pos + 3] << 8;
            decNumber += bytes[pos + 4];

            var hexaNumber = Convert.ToString(decNumber, 16).PadLeft(6, '0');

            var hour = Convert.ToInt32(hexaNumber.Substring(0, 2));
            var minute = Convert.ToInt32(hexaNumber.Substring(2, 2));
            var second = Convert.ToInt32(hexaNumber.Substring(4, 2));

            var utcTime = new DateTime(Convert.ToInt32(year), Convert.ToInt32(month), Convert.ToInt32(day), hour, minute, second, DateTimeKind.Utc);

            return utcTime.ToLocalTime();
        }

        /// <summary>
        ///  Parsing duration
        /// </summary>
        /// <param name="bytes">3 byte array</param>
        /// <param name="pos">byte array start position</param>
        /// <returns>Duration in seconds</returns>
        public static int ParseDuration(List<byte> bytes, int pos = 0)
        {
            // A 24 - bit field containing the duration of the event in hours, minutes, seconds. format: 6 digits, 4-bit BCD = 24 bit.
            // 01:45:30 is coded as "0x014530".

            var decNumber =  bytes[pos + 0] << 16;
                decNumber += bytes[pos + 1] << 8;
                decNumber += bytes[pos + 2];

            var hexaNumber = Convert.ToString(decNumber, 16).PadLeft(6,'0');

            var hours = Convert.ToInt32(hexaNumber.Substring(0, 2));
            var minutes = Convert.ToInt32(hexaNumber.Substring(2, 2));
            var seconds = Convert.ToInt32(hexaNumber.Substring(4, 2));

            return hours*3600 + minutes*60 + seconds;
        }

        public static uint ComputeCRC(byte[] bytes)
        {
            uint crc32 = 0xffffffff;
            for (int i = 1 + bytes[0]; i < bytes.Length; i++)
            {
                byte b = bytes[i];
                for (int bit = 0; bit < 8; bit++)
                {
                    if ((crc32 >= 0x80000000) != (b >= 0x80))
                        crc32 = (crc32 << 1) ^ 0x04C11DB7;
                    else
                        crc32 = (crc32 << 1);
                    b <<= 1;
                }
            }
            return crc32;
        }

        public virtual bool CRCIsValid()
        {
            var computedCRC = BitConverter.GetBytes(ComputeCRC(Data));

            return
                computedCRC[0] == CRC[3] &&
                computedCRC[1] == CRC[2] &&
                computedCRC[2] == CRC[1] &&
                computedCRC[3] == CRC[0];
        }
    }
}
