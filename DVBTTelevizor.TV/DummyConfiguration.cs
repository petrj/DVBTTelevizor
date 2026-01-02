using DVBTTelevizor.MAUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DVBTTelevizor.TV
{
    public class DummyConfiguration : ITVConfiguration
    {
        public bool UpdatedTo2026 { get; set; } = false;
        public string ConfigDirectory { get; set; } = String.Empty;
        public string LastSelectedChannelUniqueIdentifier { get; set; } = String.Empty;
        public string AutoPlayedChannelUniqueID { get; set; } = String.Empty;
        public DriverTypeEnum DVBTDriverType { get; set; } = DriverTypeEnum.AndroidDVBTDriver;
        public AppFontSizeEnum AppFontSize { get; set; } = AppFontSizeEnum.Normal;
        public string Language { get; set; } = String.Empty;
        public bool Fullscreen { get; set; } = false;
        public bool PlayOnBackground { get; set; } = false;
        public bool ShowTVChannels { get; set; } = true;
        public bool ShowNonFreeChannels { get; set; } = false;
        public bool ShowRadioChannels { get; set; } = true;
        public bool ShowOtherChannels { get; set; } = false;
        public bool AllowRemoteAccessService { get; set; } = false;
        public string RemoteAccessServiceIP { get; set; } = String.Empty;
        public int RemoteAccessServicePort { get; set; } = 0;
        public string RemoteAccessServiceSecurityKey { get; set; } = String.Empty;
        public bool EnableLogging { get; set; } = false;
        public string LoggingUDPIP { get; set; } = String.Empty;
        public bool TuneDVBTEnabled { get; set; } = true;
        public bool TuneDVBT2Enabled { get; set; } = true;
        public bool TuneDVBTPreferred { get; set; } = false;
        public long FrequencyFromKHz { get; set; } = 0;
        public long FrequencyToKHz { get; set; } = 0;
        public long FrequencyKHz { get; set; } = 0;
        public long FMFrequencyFromKHz { get; set; } = 0;
        public long FMFrequencyToKHz { get; set; } = 0;
        public long FMFrequencyKHz { get; set; } = 0;
        public long DVBTBandwidthKHz { get; set; } = 0;
        public long FMDVBTBandwidthKHz { get; set; } = 0;
        public int SDRDriverStreamPort { get; set; } = 0;
        public int SDRDriverPort { get; set; } = 0;
        public int SDRSampleRate { get; set; } = 0;
        public bool WriteToExternalDevice { get; set; } = false;
        public string ExternalDevicePath { get; set; } = String.Empty;
        public string ExternalDevicePathUri { get; set; } = String.Empty;
        public string FilteredMultiplexes { get; set; } = String.Empty;

        public string OutputDirectory { get; } = String.Empty;

        public bool SledovaniTVEnabled { get; set; } = false;
        public string SledovaniTVUserName { get; set; } = String.Empty;
        public string SledovaniTVPassword { get; set; } = String.Empty;
        public bool SledovaniTVShowAdultChannels { get; set; } = false;
        public string SledovaniTVPIN { get; set; } = String.Empty;
        public string SledovaniTVDeviceID { get; set; } = String.Empty;
        public string SledovaniTVDevicePassword { get; set; } = String.Empty;

        public ObservableCollection<Channel> GetChannels()
        {
            return new ObservableCollection<Channel>();
        }

        public void SaveChannels(ObservableCollection<Channel> channels)
        {
        }

        public void UpdateConfig(ITVConfiguration configuration)
        {
            configuration.LastSelectedChannelUniqueIdentifier = LastSelectedChannelUniqueIdentifier;
            configuration.AutoPlayedChannelUniqueID = AutoPlayedChannelUniqueID;
            configuration.DVBTDriverType = DVBTDriverType;
            configuration.AppFontSize = AppFontSize;
            configuration.Language = Language;
            configuration.Fullscreen = Fullscreen;
            configuration.PlayOnBackground = PlayOnBackground;
            configuration.ShowTVChannels = ShowTVChannels;
            configuration.ShowNonFreeChannels = ShowNonFreeChannels;
            configuration.ShowRadioChannels = ShowRadioChannels;
            configuration.ShowOtherChannels = ShowOtherChannels;
            configuration.AllowRemoteAccessService = AllowRemoteAccessService;
            configuration.RemoteAccessServiceIP = RemoteAccessServiceIP;
            configuration.RemoteAccessServicePort = RemoteAccessServicePort;
            configuration.RemoteAccessServiceSecurityKey = RemoteAccessServiceSecurityKey;
            configuration.EnableLogging = EnableLogging;
            configuration.LoggingUDPIP = LoggingUDPIP;
            configuration.TuneDVBTEnabled = TuneDVBTEnabled;
            configuration.TuneDVBT2Enabled = TuneDVBT2Enabled;
            configuration.TuneDVBTPreferred = TuneDVBTPreferred;
            configuration.FrequencyFromKHz = FrequencyFromKHz;
            configuration.FrequencyToKHz = FrequencyToKHz;
            configuration.FrequencyKHz = FrequencyKHz;
            configuration.DVBTBandwidthKHz = DVBTBandwidthKHz;
            configuration.SDRDriverStreamPort = SDRDriverStreamPort;
            configuration.SDRDriverPort = SDRDriverPort;
            configuration.SDRSampleRate = SDRSampleRate;
            configuration.WriteToExternalDevice = WriteToExternalDevice;
            configuration.ExternalDevicePath = ExternalDevicePath;
            configuration.ExternalDevicePathUri = ExternalDevicePathUri;
            configuration.SledovaniTVEnabled = SledovaniTVEnabled;
            configuration.SledovaniTVUserName = SledovaniTVUserName;
            configuration.SledovaniTVPassword = SledovaniTVPassword;
            configuration.SledovaniTVShowAdultChannels = SledovaniTVShowAdultChannels;
            configuration.SledovaniTVPIN = SledovaniTVPIN;
            configuration.SledovaniTVDeviceID = SledovaniTVDeviceID;
            configuration.SledovaniTVDevicePassword = SledovaniTVDevicePassword;
        }
    }
}
