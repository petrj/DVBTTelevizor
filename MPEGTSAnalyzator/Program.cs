using System;
using System.Collections.Generic;
using System.IO;
using MPEGTS;
using LoggerService;
using System.Text;

namespace MPEGTSAnalyzator
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
                Console.WriteLine("MPEGTSAnalyzator");
                Console.WriteLine();
                Console.WriteLine();
                Console.WriteLine("Usage:");
                Console.WriteLine();
                Console.WriteLine("MPEGTSAnalyzator.exe file.ts");
                Console.WriteLine();
                Console.WriteLine();

#if DEBUG
                //AnalyzeMPEGTSPackets("TestData" + Path.DirectorySeparatorChar + "PID_768_16_17_00.ts");
                //AnalyzeMPEGTSPackets("TestData" + Path.DirectorySeparatorChar + "CTS.ts");
                //AnalyzeMPEGTSPackets("TestData" + Path.DirectorySeparatorChar + "stream.ts");
                //AnalyzeMPEGTSPackets("TestData" + Path.DirectorySeparatorChar + "PMTs.ts");
                //AnalyzeMPEGTSPackets("TestData" + Path.DirectorySeparatorChar + "PID_0_16_17_18_410.ts");

                Console.WriteLine("Press Enter");
                Console.ReadLine();
#endif

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

            var packetsByPID = new SortedDictionary<long, List<MPEGTransportStreamPacket>>();

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

                sDTTable = DVBTTable.CreateFromPackets<SDTTable>(packetsByPID[17], 17);  // PID 0x11, Service Description Table (SDT)

                if (sDTTable != null)
                    sDTTable.WriteToConsole();
            }

            if (packetsByPID.ContainsKey(16))
            {
                Console.WriteLine();
                Console.WriteLine($"Network Information Table (NIT):");
                Console.WriteLine($"--------------------------------");


                var niTable = DVBTTable.CreateFromPackets<NITTable>(packetsByPID[16], 16);

                if (niTable != null)
                    niTable.WriteToConsole();
            }

            if (packetsByPID.ContainsKey(0))
            {
                Console.WriteLine();
                Console.WriteLine($"Program Specific Information(PSI):");
                Console.WriteLine($"----------------------------------");

                psiTable = DVBTTable.CreateFromPackets<PSITable>(packetsByPID[0], 0);

                if (psiTable != null)
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

                        if (packetsByPID.ContainsKey(kvp.Value))
                        {
                            var mptPacket = DVBTTable.CreateFromPackets<PMTTable>(packetsByPID[kvp.Value], kvp.Value);
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

                var eitManager = new EITManager(logger);

                var packetsEITwithSDT = new List<MPEGTransportStreamPacket>();
                packetsEITwithSDT.AddRange(packetsByPID[18]);

                if (packetsByPID.ContainsKey(0))
                {
                    packetsEITwithSDT.AddRange(packetsByPID[0]);
                }

                eitManager.Scan(packetsEITwithSDT);

                Console.WriteLine();
                Console.WriteLine("Current events");
                Console.WriteLine();

                Console.WriteLine($"{"Program number",14} {"Date".PadRight(10,' '),10} {"From "}-{" To  "} Text");
                Console.WriteLine($"{"--------------",14} {"----".PadRight(10,'-'),10} {"-----"}-{"-----"} -------------------------------");

                foreach (var kvp in eitManager.CurrentEvents)
                {
                    Console.WriteLine(kvp.Value.WriteToString());
                }


                Console.WriteLine();
                Console.WriteLine("Scheduled events");
                Console.WriteLine();

                foreach (var programNumber in eitManager.ScheduledEvents.Keys)
                {
                    foreach (var ev in eitManager.ScheduledEvents[programNumber])
                    {
                        Console.WriteLine(ev.WriteToString());
                    }
                }

                Console.WriteLine();
                Console.WriteLine("Present Events");
                Console.WriteLine();

                foreach (var kvp in eitManager.GetEvents(DateTime.Now))
                {
                    Console.WriteLine($"Program Map PID: {kvp.Key}");

                    foreach (var ev in kvp.Value)
                    {
                        Console.WriteLine(ev.WriteToString());
                    }
                }
            }
        }

        public static List<byte> LoadBytesFromFile(string path)
        {
            byte[] buffer = new byte[188];
            var streamBytes = new List<byte>();

            using (var fs = new FileStream(path, FileMode.Open))
            {
                while (fs.Position + 188 < fs.Length)
                {
                    fs.Read(buffer, 0, 188);
                    streamBytes.AddRange(buffer);
                }
                fs.Close();
            }

            return streamBytes;
        }
    }
}
