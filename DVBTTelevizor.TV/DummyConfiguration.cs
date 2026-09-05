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
        public bool UpdatedTo2026_rev2 { get; set; } = false;
        public bool RTLSDREnabled { get; set; } = false;

        public string ConfigDirectory { get; set; } = String.Empty;
        public string LastSelectedChannelUniqueIdentifier { get; set; } = String.Empty;
        public string AutoPlayedChannelUniqueID { get; set; } = String.Empty;
        public AppDriverTypeEnum AppDriverType { get; set; } = AppDriverTypeEnum.DVBT;
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
        public long DABFrequencyFromKHz { get; set; } = 0;
        public long DABFrequencyToKHz { get; set; } = 0;
        public long DABFrequencyKHz { get; set; } = 0;
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
        public bool TestingMode { get; set; } = false;
        public GainEnum Gain { get; set; } = GainEnum.HW;
        public int GainValue { get; set; } = 0;

        public bool AllowRemoteSDR { get; set; } = false;

        public string RemoteSDRIP { get; set; } = "127.0.0.1";

        public int RemoteSDRPort { get; set; } = 1234;

        public bool AllowRemoteVLC { get; set; } = false;

        public string RemoteVLCIP { get; set; } = "127.0.0.1";

        public int RemoteVLCPort { get; set; } = 1234;

        public string RemoteVLCPassword { get; set; } = "123";

        public ObservableCollection<Channel> GetChannels()
        {
            return new ObservableCollection<Channel>();
        }

        public void SaveChannels(ObservableCollection<Channel> channels)
        {
        }

        public static DummyConfiguration FromConfiguration(ITVConfiguration configuration)
        {
            var dummyConfig = new DummyConfiguration();

            dummyConfig.LastSelectedChannelUniqueIdentifier = configuration.LastSelectedChannelUniqueIdentifier;
            dummyConfig.AutoPlayedChannelUniqueID = configuration.AutoPlayedChannelUniqueID;
            dummyConfig.AppDriverType = configuration.AppDriverType;
            dummyConfig.AppFontSize = configuration.AppFontSize;
            dummyConfig.Language = configuration.Language;
            dummyConfig.Fullscreen = configuration.Fullscreen;
            dummyConfig.PlayOnBackground = configuration.PlayOnBackground;
            dummyConfig.ShowTVChannels = configuration.ShowTVChannels;
            dummyConfig.ShowNonFreeChannels = configuration.ShowNonFreeChannels;
            dummyConfig.ShowRadioChannels = configuration.ShowRadioChannels;
            dummyConfig.ShowOtherChannels = configuration.ShowOtherChannels;
            dummyConfig.AllowRemoteAccessService = configuration.AllowRemoteAccessService;
            dummyConfig.RemoteAccessServiceIP = configuration.RemoteAccessServiceIP;
            dummyConfig.RemoteAccessServicePort = configuration.RemoteAccessServicePort;
            dummyConfig.RemoteAccessServiceSecurityKey = configuration.RemoteAccessServiceSecurityKey;
            dummyConfig.EnableLogging = configuration.EnableLogging;
            dummyConfig.LoggingUDPIP = configuration.LoggingUDPIP;
            dummyConfig.TuneDVBTEnabled = configuration.TuneDVBTEnabled;
            dummyConfig.TuneDVBT2Enabled = configuration.TuneDVBT2Enabled;
            dummyConfig.TuneDVBTPreferred = configuration.TuneDVBTPreferred;
            dummyConfig.FrequencyFromKHz = configuration.FrequencyFromKHz;
            dummyConfig.FrequencyToKHz = configuration.FrequencyToKHz;
            dummyConfig.FrequencyKHz = configuration.FrequencyKHz;
            dummyConfig.FMFrequencyFromKHz = configuration.FMFrequencyFromKHz;
            dummyConfig.FMFrequencyToKHz = configuration.FMFrequencyToKHz;
            dummyConfig.FMFrequencyKHz = configuration.FMFrequencyKHz;
            dummyConfig.DABFrequencyFromKHz = configuration.DABFrequencyFromKHz;
            dummyConfig.DABFrequencyToKHz = configuration.DABFrequencyToKHz;
            dummyConfig.DABFrequencyKHz = configuration.DABFrequencyKHz;
            dummyConfig.DVBTBandwidthKHz = configuration.DVBTBandwidthKHz;
            dummyConfig.FMDVBTBandwidthKHz = configuration.FMDVBTBandwidthKHz;
            dummyConfig.SDRDriverStreamPort = configuration.SDRDriverStreamPort;
            dummyConfig.SDRDriverPort = configuration.SDRDriverPort;
            dummyConfig.SDRSampleRate = configuration.SDRSampleRate;
            dummyConfig.WriteToExternalDevice = configuration.WriteToExternalDevice;
            dummyConfig.ExternalDevicePath = configuration.ExternalDevicePath;
            dummyConfig.ExternalDevicePathUri = configuration.ExternalDevicePathUri;
            dummyConfig.FilteredMultiplexes = configuration.FilteredMultiplexes;
            dummyConfig.SledovaniTVEnabled = configuration.SledovaniTVEnabled;
            dummyConfig.SledovaniTVUserName = configuration.SledovaniTVUserName;
            dummyConfig.SledovaniTVPassword = configuration.SledovaniTVPassword;
            dummyConfig.SledovaniTVShowAdultChannels = configuration.SledovaniTVShowAdultChannels;
            dummyConfig.SledovaniTVPIN = configuration.SledovaniTVPIN;
            dummyConfig.SledovaniTVDeviceID = configuration.SledovaniTVDeviceID;
            dummyConfig.SledovaniTVDevicePassword = configuration.SledovaniTVDevicePassword;
            dummyConfig.RTLSDREnabled = configuration.RTLSDREnabled;
            dummyConfig.TestingMode = configuration.TestingMode;
            dummyConfig.Gain = configuration.Gain;
            dummyConfig.GainValue = configuration.GainValue;
            dummyConfig.AllowRemoteSDR = configuration.AllowRemoteSDR;
            dummyConfig.RemoteSDRIP = configuration.RemoteSDRIP;
            dummyConfig.RemoteSDRPort = configuration.RemoteSDRPort;
            dummyConfig.AllowRemoteVLC = configuration.AllowRemoteVLC;
            dummyConfig.RemoteVLCIP = configuration.RemoteVLCIP;
            dummyConfig.RemoteVLCPort = configuration.RemoteVLCPort;
            dummyConfig.RemoteVLCPassword = configuration.RemoteVLCPassword;
            dummyConfig.UpdatedTo2026 = configuration.UpdatedTo2026;
            dummyConfig.UpdatedTo2026_rev2 = configuration.UpdatedTo2026_rev2;
            dummyConfig.ConfigDirectory = configuration.ConfigDirectory;

            return dummyConfig;
        }

        public void UpdateConfig(ITVConfiguration configuration)
        {
            configuration.LastSelectedChannelUniqueIdentifier = LastSelectedChannelUniqueIdentifier;
            configuration.AutoPlayedChannelUniqueID = AutoPlayedChannelUniqueID;
            configuration.AppDriverType = AppDriverType;
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
            configuration.FMFrequencyFromKHz = FMFrequencyFromKHz;
            configuration.FMFrequencyToKHz = FMFrequencyToKHz;
            configuration.FMFrequencyKHz = FMFrequencyKHz;
            configuration.DABFrequencyFromKHz = DABFrequencyFromKHz;
            configuration.DABFrequencyToKHz = DABFrequencyToKHz;
            configuration.DABFrequencyKHz = DABFrequencyKHz;
            configuration.DVBTBandwidthKHz = DVBTBandwidthKHz;
            configuration.FMDVBTBandwidthKHz = FMDVBTBandwidthKHz;
            configuration.SDRDriverStreamPort = SDRDriverStreamPort;
            configuration.SDRDriverPort = SDRDriverPort;
            configuration.SDRSampleRate = SDRSampleRate;
            configuration.WriteToExternalDevice = WriteToExternalDevice;
            configuration.ExternalDevicePath = ExternalDevicePath;
            configuration.ExternalDevicePathUri = ExternalDevicePathUri;
            configuration.FilteredMultiplexes = FilteredMultiplexes;
            configuration.SledovaniTVEnabled = SledovaniTVEnabled;
            configuration.SledovaniTVUserName = SledovaniTVUserName;
            configuration.SledovaniTVPassword = SledovaniTVPassword;
            configuration.SledovaniTVShowAdultChannels = SledovaniTVShowAdultChannels;
            configuration.SledovaniTVPIN = SledovaniTVPIN;
            configuration.SledovaniTVDeviceID = SledovaniTVDeviceID;
            configuration.SledovaniTVDevicePassword = SledovaniTVDevicePassword;
            configuration.RTLSDREnabled = RTLSDREnabled;
            configuration.TestingMode = TestingMode;
            configuration.Gain = Gain;
            configuration.GainValue = GainValue;
            configuration.AllowRemoteSDR = AllowRemoteSDR;
            configuration.RemoteSDRIP = RemoteSDRIP;
            configuration.RemoteSDRPort = RemoteSDRPort;
            configuration.AllowRemoteVLC = AllowRemoteVLC;
            configuration.RemoteVLCIP = RemoteVLCIP;
            configuration.RemoteVLCPort = RemoteVLCPort;
            configuration.RemoteVLCPassword = RemoteVLCPassword;
            configuration.UpdatedTo2026 = UpdatedTo2026;
            configuration.UpdatedTo2026_rev2 = UpdatedTo2026_rev2;
            configuration.ConfigDirectory = ConfigDirectory;
        }
    }
}
