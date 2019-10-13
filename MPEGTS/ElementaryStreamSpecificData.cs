using System;
namespace MPEGTS
{
    public class ElementaryStreamSpecificData
    {
        public byte StreamType { get; set; }
        public int PID { get; set; }
        public int ESInfoLength { get; set; }
    }
}
