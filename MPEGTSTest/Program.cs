using System;
using System.Collections.Generic;
using System.IO;
using MPEGTS;
using LoggerService;
using System.Text;

namespace MPEGTSTest
{
    class MainClass
    {
        public static void Main(string[] args)
        {


            //var path = "TestData" + Path.DirectorySeparatorChar + "SDTTable.dat";
            //var path = "TestData" + Path.DirectorySeparatorChar + "PID_768_16_17_00.ts";
            //ScanPSI(path);

            ScanEIT("TestData" + Path.DirectorySeparatorChar + "PID_18.ts");

            // 33 s video sample:
            //var path = "TestData" + Path.DirectorySeparatorChar + "stream.ts";
            //RecordMpegTS(path);

            Console.WriteLine("Press Enter");
            Console.ReadLine();
        }

        public static void RecordMpegTS(string path)
        {
            try
            {
                var recBuffer = new RecordBuffer(new BasicLoggingService());

                var bufferLength = 4096;
                byte[] buffer = new byte[bufferLength];
                long totalBytesRead = 0;

                var startTime = DateTime.Now;

                // reading bytes from file - simulation of HW byte stream

                using (var fs = new FileStream(path, FileMode.Open))
                {
                    fs.Read(new byte[1024], 0, 10); // simulate bad stream begin

                    while (fs.Position + bufferLength < fs.Length)
                    {
                        var bytesRead = fs.Read(buffer, 0, bufferLength);
                        totalBytesRead += bytesRead;

                        recBuffer.AddBytes(buffer, bytesRead);
                        //Console.WriteLine($"Read {bytesRead} bytes (total: {totalBytesRead})");
                    }
                    fs.Close();
                }

                var totalSeconds = (DateTime.Now - startTime).TotalSeconds;
                var bitRate = (totalBytesRead * 8 / totalSeconds) / 1000000.00;

                Console.WriteLine($"Total time: {totalSeconds}");
                Console.WriteLine($"Bitrate: { bitRate} Mb/sec");

                Console.WriteLine($"RecordFileName: { recBuffer.RecordFileName}");



            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public static List<byte> LoadBytesFromFile(string path)
        {
            byte[] buffer = new byte[188];
            var streamBytes = new List<byte>();

            using (var fs = new FileStream(path, FileMode.Open))
            {
                // testing finding sync byte:
                //fs.Read(buffer, 0, 12);

                while (fs.Position + 188 < fs.Length)
                {
                    fs.Read(buffer, 0, 188);
                    streamBytes.AddRange(buffer);
                }
                fs.Close();
            }

            return streamBytes;
        }

        private static void ScanEIT(string path)
        {
            var logger = new FileLoggingService(LoggingLevelEnum.Debug);
            logger.LogFilename = "Log.log";

            var bytes = LoadBytesFromFile(path);
            var packets = MPEGTransportStreamPacket.Parse(bytes);

            // PID 18 ( EIT )

            var events = new Dictionary<int, List<EventItem>>(); // ServiceID -> eventItems
            var eventsUsed = new Dictionary<int, List<int>>(); // ServiceID -> eventIDs

            var eitData = MPEGTransportStreamPacket.GetAllPacketsPayloadBytesByPID(packets, 18);
            foreach (var kvp in eitData)
            {
                //Console.WriteLine(MPEGTransportStreamPacket.WriteBytesToString(kvp.Value));

                try
                {
                    var eit = EITTable.Parse(kvp.Value);

                    foreach (var item in eit.EventItems)
                    {
                        if (
                          item.StartTime >= DateTime.Now.AddHours(-10) &&
                          item.StartTime < DateTime.Now.AddHours(10))
                        {
                            if (!events.ContainsKey(eit.ServiceId))
                            {
                                events[eit.ServiceId] = new List<EventItem>();
                                eventsUsed[eit.ServiceId] = new List<int>();
                            }

                            if (!eventsUsed[eit.ServiceId].Contains(item.EventId))
                            {
                                eventsUsed[eit.ServiceId].Add(item.EventId);

                                events[eit.ServiceId].Add(item);
                            }
                        }
                    }

                } catch (Exception ex)
                {
                    Console.WriteLine($"Bad data ! {ex}");
                }
            }

            foreach (var kvp in events)
            {
                kvp.Value.Sort();

                logger.Info($"Service id: {kvp.Key}");
                foreach (var ev in kvp.Value)
                {
                    logger.Info(ev.WriteToString());
                }
            }
        }

        private static void ScanPSI(string path)
        {
            var bytes = LoadBytesFromFile(path);
            var packets = MPEGTransportStreamPacket.Parse(bytes);

            // step 1: reading packets with PID 0, 17 (16)

            // PID 17 ( SDT )

            var sdtBytes = MPEGTransportStreamPacket.GetPacketPayloadBytesByPID(bytes, 17);
            var pid17Packets = MPEGTransportStreamPacket.FindPacketsByPID(packets, 17);
            foreach (var packet in pid17Packets)
            {
                packet.WriteToConsole();
            }

            Console.WriteLine($"SDT (PID 17) packets found: {pid17Packets.Count}");

            var sDTTable = SDTTable.Parse(sdtBytes);
            sDTTable.WriteToConsole();

            /*
            // PID 16 ( NIT )

            var nitBytes = MPEGTransportStreamPacket.GetPacketPayloadBytesByPID(bytes, 16);
            var pid16Packets = MPEGTransportStreamPacket.FindPacketsByPID(packets, 16);

            Console.WriteLine($"NIT (PID 16) packets found: {pid16Packets.Count}");

            foreach (var pid16Packet in pid16Packets)
            {
                pid16Packet.WriteToConsole();
            }

            var niTable = NITTable.Parse(nitBytes);
            niTable.WriteToConsole();
            */

            // PID 0 ( PSI )

            var psiBytes = MPEGTransportStreamPacket.GetPacketPayloadBytesByPID(bytes, 0);
            var pid0Packets = MPEGTransportStreamPacket.FindPacketsByPID(packets, 0);

            Console.WriteLine($"PSI (PID 0) packets found: {pid0Packets.Count}");

            foreach (var pid0Packet in pid0Packets)
            {
                pid0Packet.WriteToConsole();
            }

            var psiTable = PSITable.Parse(psiBytes);
            psiTable.WriteToConsole();

            // step 2: find map PIDs from SDT a PSI

            // list of services and PMT PIDs:

            Console.WriteLine("----- Services ---------------");
            var services = MPEGTransportStreamPacket.GetAvailableServicesMapPIDs(sDTTable, psiTable);
            foreach (var service in services)
            {
                Console.WriteLine($"Map PID  : {service.Value}");
                Console.WriteLine($"Provider : {service.Key.ProviderName}");
                Console.WriteLine($"Name     : {service.Key.ServiceName}");
                Console.WriteLine("--------------------------------------------------");
            }

            // step 3: reading packets with PID 0, 17 (16) and map PID packet of given service

            var pmtPackets = MPEGTransportStreamPacket.FindPacketsByPID(packets, 768);

            foreach (var packet in pmtPackets)
            {
                packet.WriteToConsole();
                var mptPacket = PMTTable.Parse(packet.Payload);
                mptPacket.WriteToConsole();
            }
        }
    }
}
