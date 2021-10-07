using System;
using System.Collections.Generic;
using System.Text;

namespace MPEGTS
{
    /// <summary>
    /// Table 87: Service type coding
    /// </summary>
    public enum ServiceTypeEnum
    {
        Other = 0x00,

        DigitalTelevisionService = 0x01,
        DigitalRadioSoundService = 0x02,
        TeletextService = 0x03,
        NVODReferenceService = 0x04,
        NVODTimeShiftedService= 0x05 ,
        MosaicService = 0x06,
        FMRadioService = 0x07,
        DVBSRMService = 0x08,
        AdvancedCodecDigitalRadioSoundService = 0x0A,
        H264AVCMosaicService = 0x0B,
        DataBroadcastService = 0x0C,
        CommonInterfaceUsage = 0x0D,
        RCSMap = 0x0E,
        RCSFLS = 0x0F,
        DVBMHPService = 0x10,
        MPEG2HDDigitalTelevisionService = 0x11,
        H264AVCSDDigitalTelevisionService = 0x16,
        H264AVCSDNVODTimeShiftedService = 0x17,
        H264AVCSDNVODReferenceService = 0x18,
        H264AVCHDDigitalTelevisionService = 0x19,
        H264AVCHDNVODRTimeShiftedService = 0x1A,
        H264AVCHDNVODReferenceService = 0x1B,
        H264AVCFrameCompatiblePlanoStereoscopicHDDigitalTelevisionService = 0x1C,
        H264AVCFrameCompatiblePlanoStereoscopicHDNVODTimeShiftedService = 0x1D,
        H264AVCFrameCompatiblePlanoStereoscopicHDNVODReferenceService = 0x1E,
        HEVCDigitalTelevisionService=0x1F
    }
}
