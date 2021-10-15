using System;
using System.Collections.Generic;
using System.IO;
using LoggerService;

namespace MPEGTS
{
    public class RecordBuffer
    {
        public string Folder { get; set; } = "/temp";
        public string FileNamePrefix { get; set; } = "stream";

        private long _totalBytesWritten = 0;
        private int _syncPos = -1;
        private ILoggingService _logger;
        private FileStream _fs;

        private DateTime _timeOfNextStreamCut = DateTime.MinValue;
        private DateTime _recordStartTime = DateTime.MinValue;
        private int _recordFileNumber = 0;

        private List<byte> Buffer { get; set; } = new List<byte>();

        public RecordBuffer(ILoggingService loggingService)
        {
            _logger = loggingService;
            Clear();
        }

        public string RecordFileName
        {
            get
            {
                return Path.Combine(Folder, $"{FileNamePrefix}{_recordFileNumber.ToString().PadLeft(4, '0')}.ts");
            }
        }

        public void Clear()
        {
            Buffer.Clear();
            _syncPos = -1;
            _timeOfNextStreamCut = DateTime.MinValue;
        }

        public void AddBytes(IEnumerable<byte> bytes, int totalCount)
        {
            if (_recordStartTime == DateTime.MinValue)
            {
                _recordStartTime = DateTime.Now;
                _timeOfNextStreamCut = DateTime.Now.AddSeconds(5);
            }

            int addedCount = 0;
            foreach (var b in bytes)
            {
                Buffer.Add(b);
                addedCount++;
                //

                if (addedCount >=totalCount)
                {
                    break;
                }
            }

            _logger.Info($"RecordBuffer: added {addedCount} bytes");
            System.Threading.Thread.Sleep(20);

            if ((_syncPos == -1) && (Buffer.Count >= 10 * 188))  // min 10 packets
            {
                _syncPos = MPEGTransportStreamPacket.FindSyncBytePosition(Buffer);
                if (_syncPos == -1)
                {
                    // bad data ?
                    _logger.Info($"Data sync error");
                    //Clear();
                }
                else
                {
                    _logger.Info($"Data sync OK ({_syncPos})");
                }
            }

            if (_syncPos == -1)
                return;

            // writting bytes
            if (_fs == null)
            {
                if (File.Exists(RecordFileName))
                {
                    File.Delete(RecordFileName);
                }
                _fs = new FileStream(RecordFileName, FileMode.CreateNew, FileAccess.Write);
            }

            if (DateTime.Now<_timeOfNextStreamCut)
            {
                _fs.Write(Buffer.ToArray(), _syncPos, Buffer.Count - _syncPos);
                _syncPos = 0;
                Buffer.Clear();
            } else
            {
                // cut stream
                if (Buffer.Count < 10 * 188)
                    return; // cut after more packets load

                var cutSyncPos = MPEGTransportStreamPacket.FindSyncBytePosition(Buffer);
                if (cutSyncPos == -1)
                {
                    // bad data ?
                    _logger.Info($"Data sync error");
                }
                else
                {
                    _logger.Info($"Data sync OK ({cutSyncPos}), cutting");

                    _fs.Write(Buffer.ToArray(), 0, cutSyncPos);
                    for (var i = 0; i < cutSyncPos; i++)
                        Buffer.RemoveAt(0);

                    _recordFileNumber++;
                    _fs = null;

                    _timeOfNextStreamCut = DateTime.Now.AddSeconds(10);
                }
            }
        }
    }
}
