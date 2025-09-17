using DVBTTelevizor;
using System;

namespace SledovaniTV
{
    public class DeviceConnection : JSONObject
    {
        public string deviceId { get; set; }
        public string password { get; set; }

        public bool IsEmpty
        {
            get
            {
                return String.IsNullOrEmpty(deviceId) || String.IsNullOrEmpty(password);
            }
        }
    }
}
