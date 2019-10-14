using System;
namespace MPEGTS
{
    public class ElementaryStreamSpecificData
    {
        public byte StreamType { get; set; }
        public int PID { get; set; }
        public int ESInfoLength { get; set; }

        public StreamTypeEnum StreamTypeDesc
        {
            get
            {
                try
                {
                    return (StreamTypeEnum)StreamType;
                } catch
                {
                    return StreamTypeEnum.Unknown;
                }
            }
        }
    }
}
