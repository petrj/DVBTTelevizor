using System;
using System.Collections.Generic;
using System.Text;

namespace DVBTTelevizor
{
    public class DVBTBackgroundRequest
    {
        public RequestStateEnum State { get; set; } = RequestStateEnum.Ready;

        public DVBTRequest Request { get; set; }
        public DVBTResponse Response { get; set; } = new DVBTResponse();


    }
}
