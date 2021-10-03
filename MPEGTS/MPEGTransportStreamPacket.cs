using System;
using System.Collections.Generic;
using System.Text;

namespace MPEGTS
{
    public class MPEGTransportStreamPacket
    {
        public const byte MPEGTSSyncByte = 71;

        public byte SyncByte { get; set; }

        public bool TransportErrorIndicator { get; set; }
        public bool PayloadUnitStartIndicator { get; set; }
        public bool TransportPriority { get; set; }

        public ScramblingControlEnum ScramblingControl { get; set; }
        public AdaptationFieldControlEnum AdaptationFieldControl { get; set; }

        public int PID { get; set; }
        public byte ContinuityCounter { get; set; }

        public List<byte> Payload { get; set; } = new List<byte>();

        public void WriteToConsole()
        {
            Console.WriteLine($"Sync Byte: {Convert.ToChar(SyncByte)} ({SyncByte.ToString()})");
            Console.WriteLine($"PID      : {PID}");
            Console.WriteLine($"TransportErrorIndicator  : {TransportErrorIndicator}");
            Console.WriteLine($"PayloadUnitStartIndicator: {PayloadUnitStartIndicator}");
            Console.WriteLine($"TransportPriority        : {TransportPriority}");

            Console.WriteLine($"ScramblingControl        : {ScramblingControl}");
            Console.WriteLine($"AdaptationFieldControl   : {AdaptationFieldControl}");

            Console.WriteLine($"ContinuityCounter        : {ContinuityCounter}");

            var sb = new StringBuilder();
            var sbc = new StringBuilder();
            var sbb = new StringBuilder();
            int c = 0;
            int row = 0;

            for (var i=0;i<Payload.Count;i++)
            {
                sbb.Append($"{Convert.ToString(Payload[i], 2).PadLeft(8, '0'),9} ");
                sb.Append($"{Payload[i].ToString(), 9} ");


                if (Payload[i] >= 32 && Payload[i] <= 128)
                {
                    sbc.Append($"{Convert.ToChar(Payload[i]), 9} ");
                } else
                {
                    sbc.Append($"{"",9} ");
                }
                c++;

                if (c>=10)
                {
                    Console.WriteLine(sbb.ToString());
                    Console.WriteLine(sb.ToString());
                    Console.WriteLine(sbc.ToString());
                    Console.WriteLine();
                    sb.Clear();
                    sbb.Clear();
                    sbc.Clear();

                    c = 0;
                    row++;
                }
            }
            Console.WriteLine(sbb.ToString());
            Console.WriteLine(sb.ToString());
            Console.WriteLine(sbc.ToString());
            Console.WriteLine();
        }
        public static string WriteBytesToString(List<byte> bytes)
        {
            var res = new StringBuilder();

            var sb = new StringBuilder();
            var sbc = new StringBuilder();
            var sbb = new StringBuilder();
            int c = 0;
            int row = 0;

            for (var i = 0; i < bytes.Count; i++)
            {
                sbb.Append($"{Convert.ToString(bytes[i], 2).PadLeft(8, '0'),9} ");
                sb.Append($"{bytes[i].ToString(),9} ");


                if (bytes[i] >= 32 && bytes[i] <= 128)
                {
                    sbc.Append($"{Convert.ToChar(bytes[i]),9} ");
                }
                else
                {
                    sbc.Append($"{"",9} ");
                }
                c++;

                if (c >= 10)
                {
                    res.AppendLine(sbb.ToString());
                    res.AppendLine(sb.ToString());
                    res.AppendLine(sbc.ToString());
                    res.AppendLine();
                    sb.Clear();
                    sbb.Clear();
                    sbc.Clear();

                    c = 0;
                    row++;
                }
            }
            res.AppendLine(sbb.ToString());
            res.AppendLine(sb.ToString());
            res.AppendLine(sbc.ToString());
            res.AppendLine();

            return res.ToString();
        }

        public static List<MPEGTransportStreamPacket> FindPacketsByPID(List<MPEGTransportStreamPacket> packets, int PID)
        {
            var res = new List<MPEGTransportStreamPacket>();
            bool firstPacketFound = false;

            foreach (var packet in packets)
            {
                if (packet.PID == PID)
                {
                    if (!firstPacketFound)
                    {
                        if (packet.PayloadUnitStartIndicator)
                        {
                            firstPacketFound = true;
                            res.Add(packet);
                        }
                        else
                        {
                            continue;
                        }
                    }
                    else
                    {
                        if (packet.PayloadUnitStartIndicator)
                        {
                            break;
                        } else
                        {
                            res.Add(packet);
                        }
                    }
                }
            }

            return res;
        }

        public static Dictionary<int, List<byte>> GetAllPacketsPayloadBytesByPID(List<MPEGTransportStreamPacket> packets, int PID)
        {
            var res = new Dictionary<int, List<byte>>();
            var firstPacketFound = false;
            var currentKey = 0;

            foreach (var packet in packets)
            {
                if (packet.PID == PID)
                {
                    if (!firstPacketFound)
                    {
                        if (packet.PayloadUnitStartIndicator)
                        {
                            firstPacketFound = true;

                            res.Add(currentKey, new List<byte>());

                            res[currentKey].AddRange(packet.Payload);
                        }
                        else
                        {
                            continue;
                        }
                    }
                    else
                    {
                        if (packet.PayloadUnitStartIndicator)
                        {
                            currentKey++;
                            res.Add(currentKey, new List<byte>());
                        }

                        res[currentKey].AddRange(packet.Payload);
                    }
                }
            }

            return res;
        }

        public static List<byte> GetPacketPayloadBytes(List<MPEGTransportStreamPacket> packets)
        {
            var result = new List<byte>();

            foreach (var packet in packets)
            {
                result.AddRange(packet.Payload);
            }

            return result;
        }

        public static List<byte> GetPacketPayloadBytesByPID(List<byte> bytes, int PID)
        {
            var packets = Parse(bytes);

            var filteredPackets = FindPacketsByPID(packets, PID);

            var result = new List<byte>();

            foreach (var packet in filteredPackets)
            {
                result.AddRange(packet.Payload);
            }

            return result;
        }

        public void Parse(IEnumerable<byte> bytes)
        {
            Payload.Clear();
            int bytePos = 0;
            byte pidFirstByte = 0;
            foreach (var b in bytes)
            {
                switch (bytePos)
                {
                    case 0:
                        SyncByte = b;
                        break;
                    case 1:
                        TransportErrorIndicator = (b & 128) == 128;
                        PayloadUnitStartIndicator = (b & 64) == 64;
                        TransportPriority = (b & 32) == 32;
                        pidFirstByte = b;
                        break;
                    case 2:

                        var pidFirst5Bits = (pidFirstByte & 31) << 8;
                        PID = pidFirst5Bits + b;

                        break;
                    case 3:
                        var enumByte = (b & 192) >> 6;
                        ScramblingControl = (ScramblingControlEnum)enumByte;

                        enumByte = (b & 48) >> 4;
                        AdaptationFieldControl = (AdaptationFieldControlEnum) enumByte;

                        ContinuityCounter = Convert.ToByte(b & 15);

                        break;
                    default:
                        Payload.Add(b);
                        break;
                }
                bytePos++;
            }
        }

        public static int FindSyncBytePosition(List<byte> bytes)
        {
            var pos = 0;
            var buff = new byte[188];
            while (pos + 188 < bytes.Count)
            {
                if (bytes[pos] != MPEGTSSyncByte)
                {
                    // bad position
                    Console.WriteLine("Looking for sync byte .....");
                    pos++;
                    continue;
                }

                // is next byte sync byte?
                if (bytes[pos + 188] != MPEGTSSyncByte)
                {
                    pos++;
                    continue;
                }

                return pos;
            }

            return -1;
        }

        public static List<MPEGTransportStreamPacket> Parse(List<byte> bytes, int PIDFilter = -1)
        {
            var pos = FindSyncBytePosition(bytes);

            var res = new List<MPEGTransportStreamPacket>();

            if (pos == -1)
                return res;

            //Console.WriteLine($"TS packet position : {pos}");

            while (pos + 188 < bytes.Count)
            {
                var buff = new byte[188];
                for (var i=0;i<188;i++)
                {
                    buff[i] = bytes[pos + i];
                }

                var packet = new MPEGTransportStreamPacket();
                packet.Parse(buff);

                //Console.WriteLine($"Adding packet PID {packet.PID}");

                if (
                        (PIDFilter == -1)  // add all packets
                        ||
                        ((PIDFilter != -1) && (packet.PID == PIDFilter))
                   )
                {
                    res.Add(packet);
                }

                pos += 188;
            }

            return res;
        }

        public static Dictionary<ServiceDescriptor, long> GetAvailableServicesMapPIDs(SDTTable sDTTable, PSITable pSITable)
        {
            var res = new Dictionary<ServiceDescriptor, long>();

            foreach (var sdi in sDTTable.ServiceDescriptors)
            {
                foreach (var pr in pSITable.ProgramAssociations)
                {
                    if (pr.ProgramNumber == sdi.ProgramNumber)
                    {
                        res.Add(sdi, pr.ProgramMapPID);
                        break;
                    }
                }
            }

            return res;
        }
    }
}

