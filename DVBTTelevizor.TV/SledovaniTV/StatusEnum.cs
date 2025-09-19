using System;

namespace SledovaniTV
{
        public enum StatusEnum
        {
            GeneralError = -2,
            ConnectionNotAvailable = -1,
            NotInitialized = 0,
            EmptyCredentials = 1,
            Paired = 2,
            PairingFailed = 3,
            Logged = 4,
            LoginFailed = 5,
            BadPin = 6
        }
}
