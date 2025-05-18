using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RTLSDR.Common
{
    public class Wave
    {
        private AudioDataDescription _dataDesc = new AudioDataDescription();

        private FileStream _fileStream;
        private BinaryWriter _writer;

        private long _dataChunkSizePosition;
        private uint _dataChunkSize;

        public Wave()
        {
        }

        public void CreateWaveFile(string filePath, AudioDataDescription audioDescription)
        {
            _fileStream = new FileStream(filePath, FileMode.Create);
            _writer = new BinaryWriter(_fileStream);

            _dataDesc = audioDescription;

            WriteWaveHeader();
        }

        private void WriteWaveHeader()
        {
            // RIFF header
            _writer.Write(new char[4] { 'R', 'I', 'F', 'F' });
            _writer.Write((uint)0); // Placeholder for file size
            _writer.Write(new char[4] { 'W', 'A', 'V', 'E' });

            // fmt subchunk
            _writer.Write(new char[4] { 'f', 'm', 't', ' ' });
            _writer.Write((uint)16); // Subchunk1Size for PCM
            _writer.Write((short)1); // AudioFormat (1 for PCM)
            _writer.Write(_dataDesc.Channels); // NumChannels
            _writer.Write(_dataDesc.SampleRate); // SampleRate
            _writer.Write(_dataDesc.SampleRate * _dataDesc.Channels * _dataDesc.BitsPerSample / 8); // ByteRate
            _writer.Write((short)(_dataDesc.Channels * _dataDesc.BitsPerSample / 8)); // BlockAlign
            _writer.Write(_dataDesc.BitsPerSample); // BitsPerSample

            // data subchunk
            _writer.Write(new char[4] { 'd', 'a', 't', 'a' });
            _dataChunkSizePosition = _fileStream.Position;
            _writer.Write((uint)0); // Placeholder for data chunk size
        }

        public void WriteSampleData(byte[] data)
        {
            _writer.Write(data);
            _dataChunkSize += (uint)data.Length;
        }

        public void CloseWaveFile()
        {
            _writer.Flush();
            _fileStream.Flush();

            // Update file size
            _writer.Seek(4, SeekOrigin.Begin);
            _writer.Write((uint)(_fileStream.Length - 8));

            // Update data chunk size
            _writer.Seek((int)_dataChunkSizePosition, SeekOrigin.Begin);
            _writer.Write(_dataChunkSize);

            _writer.Close();
            _fileStream.Close();

            _writer.Dispose();
            _fileStream.Dispose();
        }

    }
}
