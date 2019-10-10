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
            var path = "TestData" + Path.DirectorySeparatorChar + "PID_768_16_17_0.ts";

            byte[] buffer = new byte[188];
            var streamBytes = new List<byte>();

            using (var fs = new FileStream(path, FileMode.Open))
            {
                var header = new MPEGTransportStreamPacket();

                // testing finding sync byte:
                //fs.Read(buffer, 0, 12);

                while (fs.Position + 188 < fs.Length)
                {
                    fs.Read(buffer, 0, 188);
                    streamBytes.AddRange(buffer);
                }
                fs.Close();
            }

            var packets = MPEGTransportStreamPacket.Parse(streamBytes);


            var pid768Packets = MPEGTransportStreamPacket.FindPacketsByPID(packets, 768);
            Console.WriteLine($"pid768Packets: {pid768Packets.Count}");

            foreach (var p in pid768Packets)
            {
                p.WriteToConsole();
            }



            var pid17Packets = MPEGTransportStreamPacket.FindPacketsByPID(packets, 17);

            Console.WriteLine($"id17Packets: {pid17Packets.Count}");

            var pid17PacketsPayLoad = new List<byte>();
            foreach (var pid17Packet in pid17Packets)
            {
                pid17Packet.WriteToConsole();
                pid17PacketsPayLoad.AddRange(pid17Packet.Payload);
            }

            var psiTAbleHeader = SDTTable.Parse(pid17PacketsPayLoad);
            psiTAbleHeader.WriteToConsole();


            var pid16Packets = MPEGTransportStreamPacket.FindPacketsByPID(packets, 16);
            var pid16PacketsPayLoad = new List<byte>();

            foreach (var pid16Packet in pid16Packets)
            {
                pid16Packet.WriteToConsole();
                pid16PacketsPayLoad.AddRange(pid16Packet.Payload);
            }

            var niTable = NITTable.Parse(pid16PacketsPayLoad);
            niTable.WriteToConsole();

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


            Console.WriteLine("Press Enter");
            Console.ReadLine();
        }
    }
}
