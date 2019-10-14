using System;
using System.Collections.Generic;
using System.Text;

namespace MPEGTS
{
    public enum StreamTypeEnum
    {
        Unknown = -1,

        MPEG1Cideo = 1,
        MPEG2Video = 2,
        MPEG1Audio = 3,
        MPEG2AudioHalvedSampleRate = 4,
        MPEG2TabledData = 5,
        MPEG2PacketizedData = 6,
        MHEG = 7,
        DSMCC = 8,
        AuxiliaryData = 9,
        DSMCCMultiprotocolEncapsulation = 10,
        DSMCCUNMessages = 11,
        DSMCCUNStreamDescriptors = 12,
        DSMCCUNStreamTabledData = 13,
        AuxiliaryDataInPacketizedStream = 14,
        MPEG2AudioLowBitRateAudio = 15,
        MPEG4H263Video = 16,
        MPEG4LOASMultiFormatFramedAudio = 17,
        MPEG4FlexMuxInPacketizedStream = 18,
        MPEG4FlexMuxInTables = 19,
        DSMCCSynchronizedDownloadProtocol = 20,
        PacketizedMetadata = 21,
        SectionedMetadata = 22,
        DSMCCDataCarouselMetadata = 23,
        DSMCCObjectCarouselMetadata = 24,
        SynchronizedDownloadProtocolMetadata = 25,
        IPMP = 26,
        H264LowerBitRateVideo = 27,
        MPEG4RawAudio = 28,
        MPEG4Text = 29,
        MPEG4AuxiliaryVideo = 30,
        MPEG4AVCSubBitstreamSCV = 31,
        MPEG4AVCSubBitstreamMVC = 32,
        JPEG2000Video = 33,
        H265UltraHDVideo= 36,
        ChineseVideoStandard = 66,
        IPMPDRM = 127,
        H262EncryptionBluRayAudio = 128,
        AC3UpTo6Channels = 129,
        SCTESubtitle = 130,
        DolbyTrueHDLosslessAudio = 131,
        AC3UpTo16Channels = 132,
        DTS8Audio = 133,
        SCTE35OrDTS8 = 134,
        AC3UpTo16SchannelsATSC = 135,
        PresentationGraphicStream = 144,
        ATSCDSMCCNetworkResourcesTable = 145,
        DigiCipherIIText = 192,
        AC3UpTo6ChannelsAES128CBCDataEncryption  = 193,
        ATSCDSMCCSynchronousData = 194,
        ADTSAACAES128CBCFrameEncryption = 207,
        BBCDiracUltraHDVideo = 209,
        ITUTRecH264 = 219,
        MSWindowsMediaVideo9LowerBitRate = 234

    }
}
