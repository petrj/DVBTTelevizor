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
            if (args != null &&
                args.Length == 1 &&
                File.Exists(args[0]))
            {
                AnalyzeMPEGTSPackets(args[0]);
            }
            else
            {
                //ScanPSI("TestData" + Path.DirectorySeparatorChar + "PID_768_16_17_00.ts");
                //ScanEIT("TestData" + Path.DirectorySeparatorChar + "PID_18.ts");
                //AnalyzeMPEGTSPackets("TestData" + Path.DirectorySeparatorChar + "PID_768_16_17_00.ts");
                AnalyzeMPEGTSPackets("TestData" + Path.DirectorySeparatorChar + "badSDT.ts");

                // 33 s video sample:
                //var path = "TestData" + Path.DirectorySeparatorChar + "stream.ts";
                //RecordMpegTS(path);

                Console.WriteLine("Press Enter");
                Console.ReadLine();
            }
        }

        public static void AnalyzeMPEGTSPackets(string path)
        {
            var logger = new FileLoggingService(LoggingLevelEnum.Debug);
            logger.LogFilename = "Log.log";

            Console.Write($"Reading ....... ");

            var bytes = LoadBytesFromFile(path);
            var packets = MPEGTransportStreamPacket.Parse(bytes);

            Console.WriteLine($" {packets.Count} packets found");

            var packetsByPID = new SortedDictionary<int, List<MPEGTransportStreamPacket>>();

            foreach (var packet in packets)
            {
                if (!packetsByPID.ContainsKey(packet.PID))
                {
                    packetsByPID.Add(packet.PID, new List<MPEGTransportStreamPacket>());
                }

                packetsByPID[packet.PID].Add(packet);
            }

            Console.WriteLine();
            Console.WriteLine($"PID:             Packets count");
            Console.WriteLine("-------------------------------");

            SDTTable sDTTable = null;
            PSITable psiTable = null;

            foreach (var kvp in packetsByPID)
            {
                Console.WriteLine($"{kvp.Key,6} ({"0x" + Convert.ToString(kvp.Key, 16),6}): {kvp.Value.Count,8}");
            }

            if (packetsByPID.ContainsKey(17))
            {
                Console.WriteLine();
                Console.WriteLine($"Service Description Table(SDT):");
                Console.WriteLine($"------------------------------");

                var sdtBytes = MPEGTransportStreamPacket.GetPacketPayloadBytes(packetsByPID[17]);
                sDTTable = SDTTable.Parse(sdtBytes);
                sDTTable.WriteToConsole();
            }

            if (packetsByPID.ContainsKey(16))
            {
                Console.WriteLine();
                Console.WriteLine($"Network Information Table (NIT):");
                Console.WriteLine($"--------------------------------");

                var nitBytes = MPEGTransportStreamPacket.GetPacketPayloadBytes(packetsByPID[16]);
                var niTable = NITTable.Parse(nitBytes);
                niTable.WriteToConsole();
            }


            if (packetsByPID.ContainsKey(0))
            {
                Console.WriteLine();
                Console.WriteLine($"Program Specific Information(PSI):");
                Console.WriteLine($"----------------------------------");

                var psiBytes = MPEGTransportStreamPacket.GetPacketPayloadBytes(packetsByPID[0]);
                psiTable = PSITable.Parse(psiBytes);

                psiTable.WriteToConsole();
            }


            if ((psiTable != null) &&
                (sDTTable != null))
            {
                Console.WriteLine();
                Console.WriteLine($"Program Map Table (PMT):");
                Console.WriteLine($"----------------------------------");
                Console.WriteLine();

                var servicesMapPIDs = MPEGTransportStreamPacket.GetAvailableServicesMapPIDs(sDTTable, psiTable);

                Console.WriteLine($"{"Program name".PadRight(40,' '),40} {"Program number",14} {"     PID",8}");
                Console.WriteLine($"{"------------".PadRight(40,' '),40} {"--------------",14} {"--------"}");

                // scan PMT for each program number
                foreach (var kvp in servicesMapPIDs)
                {
                    Console.WriteLine($"{kvp.Key.ServiceName.PadRight(40, ' ')} {kvp.Key.ProgramNumber,14} {kvp.Value,8}");

                    if (packetsByPID.ContainsKey(Convert.ToInt32(kvp.Value)))
                    {
                        // stream contains this Map PID

                        if (packetsByPID.ContainsKey(Convert.ToInt32(kvp.Value)))
                        {
                            var pmtBytes = MPEGTransportStreamPacket.GetPacketPayloadBytes(packetsByPID[Convert.ToInt32(kvp.Value)]);
                            var mptPacket = PMTTable.Parse(pmtBytes);
                            mptPacket.WriteToConsole();
                        }
                    }
                }
            }

            if (packetsByPID.ContainsKey(18))
            {
                Console.WriteLine();
                Console.WriteLine($"Event Information Table (EIT):");
                Console.WriteLine($"------------------------------");

                var eitManager = new EITManager();
                eitManager.Scan(packetsByPID[18]);

                Console.WriteLine();
                Console.WriteLine("Current events");
                Console.WriteLine();

                Console.WriteLine($"{"Program number",14} {"Date".PadRight(10,' '),10} {"From "}-{" To  "} Text");
                Console.WriteLine($"{"--------------",14} {"----".PadRight(10,'-'),10} {"-----"}-{"-----"} -------------------------------");

                foreach (var kvp in eitManager.CurrentEvents)
                {
                    Console.WriteLine(kvp.Value.WriteToString());
                }

                /*
                Console.WriteLine();
                Console.WriteLine("Scheduled events");
                Console.WriteLine();

                foreach (var kvp in eitManager.ScheduledEvents)
                {
                    foreach (var ev in kvp.Value)
                    {
                        Console.WriteLine(ev.WriteToString());
                    }
                }
                */
            }        
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

            var eitManager = new EITManager();
            eitManager.Scan(packets);

            Console.WriteLine();
            Console.WriteLine("---------------------------------------------------------------");
            Console.WriteLine("Current events:");
            foreach (var kvp in eitManager.CurrentEvents)
            {
                Console.WriteLine($"ServiceId: {kvp.Key}");
                Console.WriteLine(kvp.Value.WriteToString());
            }

            /*
            Console.WriteLine();
            Console.WriteLine("---------------------------------------------------------------");
            Console.WriteLine("Scheduled events:");
            foreach (var kvp in eitManager.ScheduledEvents)
            {
                Console.WriteLine($"ServiceId: {kvp.Key}");
                foreach (var ev in kvp.Value)
                {
                    if (ev.FinishTime >= DateTime.Now)
                    {
                        Console.WriteLine(ev.WriteToString());
                    }
                }
            }
            */
        }

        private static void ScanPSI(string path)
        {
            var bytes = LoadBytesFromFile(path);
            var packets = MPEGTransportStreamPacket.Parse(bytes);

            // step 1: reading packets with PID 0, 17 (16)

            // PID 17 ( SDT )

            var sdtBytes = MPEGTransportStreamPacket.GetPacketPayloadBytesByPID(bytes, 17);
            var sDTTable = SDTTable.Parse(sdtBytes);
            sDTTable.WriteToConsole();

            /*
            // PID 16 ( NIT )

            var nitBytes = MPEGTransportStreamPacket.GetPacketPayloadBytesByPID(bytes, 16);

            var niTable = NITTable.Parse(nitBytes);
            niTable.WriteToConsole();
            */

            // PID 0 ( PSI )

            var psiBytes = MPEGTransportStreamPacket.GetPacketPayloadBytesByPID(bytes, 0);
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

            var pmtBytes = MPEGTransportStreamPacket.GetPacketPayloadBytesByPID(bytes, 768);
            var mptPacket = PMTTable.Parse(pmtBytes);
            mptPacket.WriteToConsole();
        }
    }
}
