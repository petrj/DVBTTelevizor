using System;
using System.Collections.Generic;
using System.Text;

namespace MPEGTS
{
    [Serializable]    
    public sealed class MPEGTSUnsupportedEncodingException : Exception
    { 
        public MPEGTSUnsupportedEncodingException()
        { }
        
        public MPEGTSUnsupportedEncodingException(string message) : base(message)
        { }

        public MPEGTSUnsupportedEncodingException(string message, Exception innerException) : base(message, innerException)
        { }
    }
}
