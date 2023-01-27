using System;
namespace MPEGTS
{
    public class ElementaryStreamSpecificData
    {
        public SubtitlingDescriptor SubtitleDescriptor { get; set; } = new SubtitlingDescriptor();

        public byte StreamType { get; set; }
        public int PID { get; set; }
        public int ESInfoLength { get; set; }

        public string GetSubtitleLanguageCode
        {
            get
            {
                if (SubtitleDescriptor == null ||
                    SubtitleDescriptor.SubtitleInfos == null ||
                    SubtitleDescriptor.SubtitleInfos.Count == 0)
                    return null;

                return SubtitleDescriptor.SubtitleInfos[0].LanguageCode;
            }
        }

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
