using System;
using System.Collections.Generic;
using System.IO;
using MPEGTS;

namespace MPEGTSTest
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            var path = "TestData" + Path.DirectorySeparatorChar + "PID_1024_16_17_0.ts";

            //AnalyzeTSPackets(path);
            var packets = MPEGTransportStreamPacket.Parse(LoadBytesFromFile(path));
            var pmtPackets = MPEGTransportStreamPacket.FindPacketsByPID(packets, 1024);
           
            foreach (var packet in pmtPackets)
            {
                packet.WriteToConsole();
                var mptPacket = PMTTable.Parse(packet.Payload);
                mptPacket.WriteToConsole();
            }

            //Console.WriteLine("Press Enter");
            //Console.ReadLine();
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

        public static void AnalyzeTSPackets(string path)
        {        
            var bytes = LoadBytesFromFile(path);         
            var packets = MPEGTransportStreamPacket.Parse(bytes);

            // step 1: reading packets with PID 0, 17 (16)

            // PID 17 ( SDT )

            var pid17Packets = MPEGTransportStreamPacket.FindPacketsByPID(packets, 17);

            Console.WriteLine($"id17Packets: {pid17Packets.Count}");

            var pid17PacketsPayLoad = new List<byte>();
            foreach (var pid17Packet in pid17Packets)
            {
                pid17Packet.WriteToConsole();
                pid17PacketsPayLoad.AddRange(pid17Packet.Payload);
            }

            var sDTTable = SDTTable.Parse(pid17PacketsPayLoad);
            sDTTable.WriteToConsole();

            // PID 16 ( NIT )

            var pid16Packets = MPEGTransportStreamPacket.FindPacketsByPID(packets, 16);
            var pid16PacketsPayLoad = new List<byte>();

            foreach (var pid16Packet in pid16Packets)
            {
                pid16Packet.WriteToConsole();
                pid16PacketsPayLoad.AddRange(pid16Packet.Payload);
            }

            var niTable = NITTable.Parse(pid16PacketsPayLoad);
            niTable.WriteToConsole();

            // PID 0 ( PSI )

            var pid0Packets = MPEGTransportStreamPacket.FindPacketsByPID(packets, 0);

            Console.WriteLine($"id0Packets: {pid0Packets.Count}");

            var pid0PacketsPayLoad = new List<byte>();
            foreach (var pid0Packet in pid0Packets)
            {
                pid0Packet.WriteToConsole();
                pid0PacketsPayLoad.AddRange(pid0Packet.Payload);
            }

            var psiTable = PSITable.Parse(pid0PacketsPayLoad);
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

            var pid768Packets = MPEGTransportStreamPacket.FindPacketsByPID(packets, 768);
            Console.WriteLine($"pid768Packets: {pid768Packets.Count}");

            foreach (var p in pid768Packets)
            {
                p.WriteToConsole();
            }

            var pids = MPEGTransportStreamPacket.GetAvailableServicesMapPIDs(sDTTable, psiTable);
            Console.WriteLine("Map PIDs list:");
            Console.WriteLine("--------------------------------------------------");
            foreach (var p in pids)
            {
                Console.WriteLine($"PID            : {p.Value}");
                Console.WriteLine($"Service Name   : {p.Key.ServiceName}");
                Console.WriteLine($"Program Number : {p.Key.ProgramNumber}");
                Console.WriteLine($"Provider       : {p.Key.ProviderName}");
                Console.WriteLine("--------------------------------------------------");
            }
        }
    }
}
