using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace MCP2518
{
    static class MCP2518Dfs
    {

        // *****************************************************************************
        // *****************************************************************************
        /* Register Addresses */

        /* CAN FD Controller */
        public static ushort cREGADDR_CiCON = 0x000;
        public static ushort cREGADDR_CiNBTCFG = 0x004;
        public static ushort cREGADDR_CiDBTCFG = 0x008;
        public static ushort cREGADDR_CiTDC = 0x00C;

        public static ushort cREGADDR_CiTBC = 0x010;
        public static ushort cREGADDR_CiTSCON = 0x014;
        public static ushort cREGADDR_CiVEC = 0x018;
        public static ushort cREGADDR_CiINT = 0x01C;
        public static ushort cREGADDR_CiINTFLAG = cREGADDR_CiINT;
        public static ushort cREGADDR_CiINTENABLE = (ushort)(cREGADDR_CiINT + 2);

        public static ushort cREGADDR_CiRXIF = 0x020;
        public static ushort cREGADDR_CiTXIF = 0x024;
        public static ushort cREGADDR_CiRXOVIF = 0x028;
        public static ushort cREGADDR_CiTXATIF = 0x02C;

        public static ushort cREGADDR_CiTXREQ = 0x030;
        public static ushort cREGADDR_CiTREC = 0x034;
        public static ushort cREGADDR_CiBDIAG0 = 0x038;
        public static ushort cREGADDR_CiBDIAG1 = 0x03C;

        public static ushort cREGADDR_CiTEFCON = 0x040;
        public static ushort cREGADDR_CiTEFSTA = 0x044;
        public static ushort cREGADDR_CiTEFUA = 0x048;
        public static ushort cREGADDR_CiFIFOBA = 0x04C;

        public static ushort cREGADDR_CiFIFOCON = 0x050;
        public static ushort cREGADDR_CiFIFOSTA = 0x054;
        public static ushort cREGADDR_CiFIFOUA = 0x058;
        public static ushort CiFIFO_OFFSET = (3 * 4);

        public static ushort cREGADDR_CiTXQCON = 0x050;
        public static ushort cREGADDR_CiTXQSTA = 0x054;
        public static ushort cREGADDR_CiTXQUA = 0x058;

        // The filters start right after the FIFO control/status registers
        public static ushort cREGADDR_CiFLTCON =
                (ushort)(cREGADDR_CiFIFOCON + (CiFIFO_OFFSET * (ushort)CAN_FIFO_CHANNEL.CAN_FIFO_TOTAL_CHANNELS));              // 0x50+12*32
        public static ushort cREGADDR_CiFLTOBJ = (ushort)(cREGADDR_CiFLTCON + (ushort)CAN_FIFO_CHANNEL.CAN_FIFO_TOTAL_CHANNELS);
        public static ushort cREGADDR_CiMASK = (ushort)(cREGADDR_CiFLTOBJ + 4);

        public static ushort CiFILTER_OFFSET = (2 * 4);

        /* MCP25xxFD Specific */
        public static ushort cREGADDR_OSC = 0xE00;
        public static ushort cREGADDR_IOCON = 0xE04;
        public static ushort cREGADDR_CRC = 0xE08;
        public static ushort cREGADDR_ECCCON = 0xE0C;
        public static ushort cREGADDR_ECCSTA = 0xE10;
        public static ushort cREGADDR_DEVID = 0xE14;

        /* RAM addresses */
        public static ushort cRAM_SIZE = 2048;

        public static ushort cRAMADDR_START = 0x400;
        public static ushort cRAMADDR_END = (ushort)(cRAMADDR_START + cRAM_SIZE);

        // Maximum Size of TX/RX Object
        public static uint MAX_MSG_SIZE = 76;

        // Maximum number of data bytes in message
        public static uint MAX_DATA_BYTES = 64;

        public static uint CAN_TXQUEUE_CH0 = (uint)CAN_FIFO_CHANNEL.CAN_FIFO_CH0;

        // *****************************************************************************
        // *****************************************************************************
        /* Register Reset Values */

        // *****************************************************************************
        /* CAN FD Controller */

        // Control Register Reset Values up to FIFOs
        public static uint[] canControlResetValues = {
        /* Address 0x000 to 0x00C */
        0x04980760, 0x003E0F0F, 0x000E0303, 0x00021000,
        /* Address 0x010 to 0x01C */
        0x00000000, 0x00000000, 0x40400040, 0x00000000,
        /* Address 0x020 to 0x02C */
        0x00000000, 0x00000000, 0x00000000, 0x00000000,
        /* Address 0x030 to 0x03C */
        0x00000000, 0x00200000, 0x00000000, 0x00000000,
        /* Address 0x040 to 0x04C */
        0x00000400, 0x00000000, 0x00000000, 0x00000000};

        // FIFO Register Reset Values
        public static uint[] canFifoResetValues = {0x00600400, 0x00000000,
                                                  0x00000000};

        // Filter Control Register Reset Values
        public static uint canFilterControlResetValue = 0x00000000;

        // Filter and Mask Object Reset Values
        public static uint[] canFilterObjectResetValues = { 0x00000000, 0x00000000 };

        // *****************************************************************************
        /* MCP25xxFD */

        public static uint[] mcp25xxfdControlResetValues = {
        0x00000460, 0x00000003, 0x00000000, 0x00000000, 0x00000000};
    }

    // *****************************************************************************
    // *****************************************************************************
    /* Register classures */

    // DOM-IGNORE-END

    // *****************************************************************************
    // *****************************************************************************
    // Section: Implementation

    //! CAN FIFO Channels

    public enum CAN_FIFO_CHANNEL
    {
        CAN_FIFO_CH0, // CAN_TXQUEUE_CH0
        CAN_FIFO_CH1,
        CAN_FIFO_CH2,
        CAN_FIFO_CH3,
        CAN_FIFO_CH4,
        CAN_FIFO_CH5,
        CAN_FIFO_CH6,
        CAN_FIFO_CH7,
        CAN_FIFO_CH8,
        CAN_FIFO_CH9,
        CAN_FIFO_CH10,
        CAN_FIFO_CH11,
        CAN_FIFO_CH12,
        CAN_FIFO_CH13,
        CAN_FIFO_CH14,
        CAN_FIFO_CH15,
        CAN_FIFO_CH16,
        CAN_FIFO_CH17,
        CAN_FIFO_CH18,
        CAN_FIFO_CH19,
        CAN_FIFO_CH20,
        CAN_FIFO_CH21,
        CAN_FIFO_CH22,
        CAN_FIFO_CH23,
        CAN_FIFO_CH24,
        CAN_FIFO_CH25,
        CAN_FIFO_CH26,
        CAN_FIFO_CH27,
        CAN_FIFO_CH28,
        CAN_FIFO_CH29,
        CAN_FIFO_CH30,
        CAN_FIFO_CH31,
        CAN_FIFO_TOTAL_CHANNELS
    }

    //! CAN Filter Channels

    public enum CAN_FILTER
    {
        CAN_FILTER0,
        CAN_FILTER1,
        CAN_FILTER2,
        CAN_FILTER3,
        CAN_FILTER4,
        CAN_FILTER5,
        CAN_FILTER6,
        CAN_FILTER7,
        CAN_FILTER8,
        CAN_FILTER9,
        CAN_FILTER10,
        CAN_FILTER11,
        CAN_FILTER12,
        CAN_FILTER13,
        CAN_FILTER14,
        CAN_FILTER15,
        CAN_FILTER16,
        CAN_FILTER17,
        CAN_FILTER18,
        CAN_FILTER19,
        CAN_FILTER20,
        CAN_FILTER21,
        CAN_FILTER22,
        CAN_FILTER23,
        CAN_FILTER24,
        CAN_FILTER25,
        CAN_FILTER26,
        CAN_FILTER27,
        CAN_FILTER28,
        CAN_FILTER29,
        CAN_FILTER30,
        CAN_FILTER31,
        CAN_FILTER_TOTAL,
    };




    //! Transmit Bandwidth Sharing

    public enum CAN_TX_BANDWITH_SHARING
    {
        CAN_TXBWS_NO_DELAY,
        CAN_TXBWS_2,
        CAN_TXBWS_4,
        CAN_TXBWS_8,
        CAN_TXBWS_16,
        CAN_TXBWS_32,
        CAN_TXBWS_64,
        CAN_TXBWS_128,
        CAN_TXBWS_256,
        CAN_TXBWS_512,
        CAN_TXBWS_1024,
        CAN_TXBWS_2048,
        CAN_TXBWS_4096
    };

    //! Wake-up Filter Time

    public enum CAN_WAKEUP_FILTER_TIME
    {
        CAN_WFT00,
        CAN_WFT01,
        CAN_WFT10,
        CAN_WFT11
    };

    //! Data Byte Filter Number

    public enum CAN_DNET_FILTER_SIZE
    {
        CAN_DNET_FILTER_DISABLE = 0,
        CAN_DNET_FILTER_SIZE_1_BIT,
        CAN_DNET_FILTER_SIZE_2_BIT,
        CAN_DNET_FILTER_SIZE_3_BIT,
        CAN_DNET_FILTER_SIZE_4_BIT,
        CAN_DNET_FILTER_SIZE_5_BIT,
        CAN_DNET_FILTER_SIZE_6_BIT,
        CAN_DNET_FILTER_SIZE_7_BIT,
        CAN_DNET_FILTER_SIZE_8_BIT,
        CAN_DNET_FILTER_SIZE_9_BIT,
        CAN_DNET_FILTER_SIZE_10_BIT,
        CAN_DNET_FILTER_SIZE_11_BIT,
        CAN_DNET_FILTER_SIZE_12_BIT,
        CAN_DNET_FILTER_SIZE_13_BIT,
        CAN_DNET_FILTER_SIZE_14_BIT,
        CAN_DNET_FILTER_SIZE_15_BIT,
        CAN_DNET_FILTER_SIZE_16_BIT,
        CAN_DNET_FILTER_SIZE_17_BIT,
        CAN_DNET_FILTER_SIZE_18_BIT
    };

    //! FIFO Payload Size

    public enum CAN_FIFO_PLSIZE
    {
        CAN_PLSIZE_8,
        CAN_PLSIZE_12,
        CAN_PLSIZE_16,
        CAN_PLSIZE_20,
        CAN_PLSIZE_24,
        CAN_PLSIZE_32,
        CAN_PLSIZE_48,
        CAN_PLSIZE_64
    };

    //! CAN Configure

    public class CAN_CONFIG
    {
        public uint DNetFilterCount;
        public uint IsoCrcEnable;
        public uint ProtocolExpectionEventDisable;
        public uint WakeUpFilterEnable;
        public uint WakeUpFilterTime;
        public uint BitRateSwitchDisable;
        public uint RestrictReTxAttempts;
        public uint EsiInGatewayMode;
        public uint SystemErrorToListenOnly;
        public uint StoreInTEF;
        public uint TXQEnable;
        public uint TxBandWidthSharing;
    };

    //! CAN Transmit Channel Configure

    public class CAN_TX_FIFO_CONFIG
    {
        public uint RTREnable;
        public uint TxPriority;
        public uint TxAttempts;
        public uint FifoSize;
        public uint PayLoadSize;

        internal void TransmitChannelConfigure(CAN_FIFO_CHANNEL aPP_TX_FIFO, CAN_TX_FIFO_CONFIG txConfig)
        {
            throw new NotImplementedException();
        }
    }

    //! CAN Transmit Queue Configure

    public class CAN_TX_QUEUE_CONFIG
    {
        public uint TxPriority;
        public uint TxAttempts;
        public uint FifoSize;
        public uint PayLoadSize;
    };

    //! CAN Receive Channel Configure

    public class CAN_RX_FIFO_CONFIG
    {
        public uint RxTimeStampEnable;
        public uint FifoSize;
        public uint PayLoadSize;
    };

    //! CAN Transmit Event FIFO Configure

    public class CAN_TEF_CONFIG
    {
        public uint TimeStampEnable;
        public uint FifoSize;
    };

    /* CAN Message Objects */

    //! CAN Message Object ID

    public class CAN_MSGOBJ_ID
    {
        public uint SID;
        public uint EID;
        public uint SID11;
        public uint unimplemented1;
    };


    //! CAN TX Message Object Control

    public class CAN_TX_MSGOBJ_CTRL
    {
        public uint DLC;
        public uint IDE;
        public uint RTR;
        public uint BRS;
        public uint FDF;
        public uint ESI;
        public uint SEQ;
    };

    //! CAN RX Message Object Control

    public class CAN_RX_MSGOBJ_CTRL
    {
        public uint DLC;
        public uint IDE;
        public uint RTR;
        public uint BRS;
        public uint FDF;
        public uint ESI;
        public uint unimplemented1;
        public uint FilterHit;
        public uint unimplemented2;
    };

    //! CAN TX Message Object

    public class CAN_TX_MSGOBJ
    {

        public class bFrame
        {
            public CAN_MSGOBJ_ID id = new CAN_MSGOBJ_ID();
            public CAN_TX_MSGOBJ_CTRL ctrl = new CAN_TX_MSGOBJ_CTRL();
            public uint timeStamp;
        }
        public bFrame bF = new bFrame();
        public uint[] word = new uint[3];
        public byte[] bytes = new byte[12];

        public void UpdateFromWord()
        {
            bytes[0] = BitConverter.GetBytes(word[0])[0];
            bytes[1] = BitConverter.GetBytes(word[0])[1];
            bytes[2] = BitConverter.GetBytes(word[0])[2];
            bytes[3] = BitConverter.GetBytes(word[0])[3];
            bytes[4] = BitConverter.GetBytes(word[1])[0];
            bytes[5] = BitConverter.GetBytes(word[1])[1];
            bytes[6] = BitConverter.GetBytes(word[1])[2];
            bytes[7] = BitConverter.GetBytes(word[1])[3];
            bytes[8] = BitConverter.GetBytes(word[2])[0];
            bytes[9] = BitConverter.GetBytes(word[2])[1];
            bytes[10] = BitConverter.GetBytes(word[2])[2];
            bytes[11] = BitConverter.GetBytes(word[2])[3];

            bF.id.unimplemented1 = (word[0] & 0xC000_0000) >> 30;
            bF.id.SID11 = (word[0] & 0x2000_0000) >> 29;
            bF.id.EID = (word[0] & 0x1FFF_F800) >> 11;
            bF.id.SID = (word[0] & 0x3FF);
            bF.ctrl.SEQ = (word[1] & 0xFFFF_FE00) >> 9;
            bF.ctrl.ESI = (word[1] & 0x100) >> 8;
            bF.ctrl.FDF = (word[1] & 0x80) >> 7;
            bF.ctrl.BRS = (word[1] & 0x40) >> 6;
            bF.ctrl.RTR = (word[1] & 0x20) >> 5;
            bF.ctrl.IDE = (word[1] & 0x10) >> 4;
            bF.ctrl.DLC = (word[1] & 0xF);
            bF.timeStamp = word[2];
        }

        public void UpdateFromBytes()
        {
            word[0] = BitConverter.ToUInt32(bytes, 0);
            word[1] = BitConverter.ToUInt32(bytes, 4);
            word[2] = BitConverter.ToUInt32(bytes, 8);

            bF.id.unimplemented1 = (word[0] & 0xC000_0000) >> 30;
            bF.id.SID11 = (word[0] & 0x2000_0000) >> 29;
            bF.id.EID = (word[0] & 0x1FFF_F800) >> 11;
            bF.id.SID = (word[0] & 0x3FF);
            bF.ctrl.SEQ = (word[1] & 0xFFFF_FE00) >> 9;
            bF.ctrl.ESI = (word[1] & 0x100) >> 8;
            bF.ctrl.FDF = (word[1] & 0x80) >> 7;
            bF.ctrl.BRS = (word[1] & 0x40) >> 6;
            bF.ctrl.RTR = (word[1] & 0x20) >> 5;
            bF.ctrl.IDE = (word[1] & 0x10) >> 4;
            bF.ctrl.DLC = (word[1] & 0xF);
            bF.timeStamp = word[2];
        }

        public void UpdateFromReg()
        {
            word[0] |= bF.id.unimplemented1 << 30;
            word[0] |= bF.id.SID11 << 29;
            word[0] |= bF.id.EID << 11;
            word[0] |= bF.id.SID;

            word[1] |= bF.ctrl.SEQ << 9;
            word[1] |= bF.ctrl.ESI << 8;
            word[1] |= bF.ctrl.FDF << 7;
            word[1] |= bF.ctrl.BRS << 6;
            word[1] |= bF.ctrl.RTR << 5;
            word[1] |= bF.ctrl.IDE << 4;
            word[1] |= bF.ctrl.DLC;

            word[2] = bF.timeStamp;

            bytes[0] = BitConverter.GetBytes(word[0])[0];
            bytes[1] = BitConverter.GetBytes(word[0])[1];
            bytes[2] = BitConverter.GetBytes(word[0])[2];
            bytes[3] = BitConverter.GetBytes(word[0])[3];
            bytes[4] = BitConverter.GetBytes(word[1])[0];
            bytes[5] = BitConverter.GetBytes(word[1])[1];
            bytes[6] = BitConverter.GetBytes(word[1])[2];
            bytes[7] = BitConverter.GetBytes(word[1])[3];
            bytes[8] = BitConverter.GetBytes(word[2])[0];
            bytes[9] = BitConverter.GetBytes(word[2])[1];
            bytes[10] = BitConverter.GetBytes(word[2])[2];
            bytes[11] = BitConverter.GetBytes(word[2])[3];
        }
    };

    //! CAN RX Message Object

    public class CAN_RX_MSGOBJ
    {

        public class bFrame
        {
            public CAN_MSGOBJ_ID id = new CAN_MSGOBJ_ID();
            public CAN_RX_MSGOBJ_CTRL ctrl = new CAN_RX_MSGOBJ_CTRL();
            public uint timeStamp;
        }
        public bFrame bF = new bFrame();
        public uint[] word = new uint[3];
        public byte[] bytes = new byte[12];

        public void UpdateFromWord()
        {
            bytes[0] = BitConverter.GetBytes(word[0])[0];
            bytes[1] = BitConverter.GetBytes(word[0])[1];
            bytes[2] = BitConverter.GetBytes(word[0])[2];
            bytes[3] = BitConverter.GetBytes(word[0])[3];
            bytes[4] = BitConverter.GetBytes(word[1])[0];
            bytes[5] = BitConverter.GetBytes(word[1])[1];
            bytes[6] = BitConverter.GetBytes(word[1])[2];
            bytes[7] = BitConverter.GetBytes(word[1])[3];
            bytes[8] = BitConverter.GetBytes(word[2])[0];
            bytes[9] = BitConverter.GetBytes(word[2])[1];
            bytes[10] = BitConverter.GetBytes(word[2])[2];
            bytes[11] = BitConverter.GetBytes(word[2])[3];

            bF.id.unimplemented1 = (word[0] & 0xC000_0000) >> 30;
            bF.id.SID11 = (word[0] & 0x2000_0000) >> 29;
            bF.id.EID = (word[0] & 0x1FFF_F800) >> 11;
            bF.id.SID = (word[0] & 0x3FF);

            bF.ctrl.unimplemented1 = (word[1] & 0xFFFF_0000) >> 16;
            bF.ctrl.FilterHit = (word[1] & 0xF800) >> 11;
            bF.ctrl.unimplemented2 = (word[1] & 0x600) >> 9;
            bF.ctrl.ESI = (word[1] & 0x100) >> 8;
            bF.ctrl.FDF = (word[1] & 0x80) >> 7;
            bF.ctrl.BRS = (word[1] & 0x40) >> 6;
            bF.ctrl.RTR = (word[1] & 0x20) >> 5;
            bF.ctrl.IDE = (word[1] & 0x10) >> 4;
            bF.ctrl.DLC = (word[1] & 0xF);
            bF.timeStamp = word[2];

            bF.timeStamp = word[2];
        }

        public void UpdateFromBytes()
        {
            word[0] = BitConverter.ToUInt32(bytes, 0);
            word[1] = BitConverter.ToUInt32(bytes, 4);
            word[2] = BitConverter.ToUInt32(bytes, 8);

            bF.id.unimplemented1 = (word[0] & 0xC000_0000) >> 30;
            bF.id.SID11 = (word[0] & 0x2000_0000) >> 29;
            bF.id.EID = (word[0] & 0x1FFF_F800) >> 11;
            bF.id.SID = (word[0] & 0x3FF);

            bF.ctrl.unimplemented1 = (word[1] & 0xFFFF_0000) >> 16;
            bF.ctrl.FilterHit = (word[1] & 0xF800) >> 11;
            bF.ctrl.unimplemented2 = (word[1] & 0x600) >> 9;
            bF.ctrl.ESI = (word[1] & 0x100) >> 8;
            bF.ctrl.FDF = (word[1] & 0x80) >> 7;
            bF.ctrl.BRS = (word[1] & 0x40) >> 6;
            bF.ctrl.RTR = (word[1] & 0x20) >> 5;
            bF.ctrl.IDE = (word[1] & 0x10) >> 4;
            bF.ctrl.DLC = (word[1] & 0xF);
            bF.timeStamp = word[2];
        }

        public void UpdateFromReg()
        {
            word[0] |= bF.id.unimplemented1 << 30;
            word[0] |= bF.id.SID11 << 29;
            word[0] |= bF.id.EID << 11;
            word[0] |= bF.id.SID;

            word[1] |= bF.ctrl.unimplemented1 << 16;
            word[1] |= bF.ctrl.FilterHit << 11;
            word[1] |= bF.ctrl.unimplemented2 << 9;
            word[1] |= bF.ctrl.ESI << 8;
            word[1] |= bF.ctrl.FDF << 7;
            word[1] |= bF.ctrl.BRS << 6;
            word[1] |= bF.ctrl.RTR << 5;
            word[1] |= bF.ctrl.IDE << 4;
            word[1] |= bF.ctrl.DLC;

            word[2] = bF.timeStamp;

            bytes[0] = BitConverter.GetBytes(word[0])[0];
            bytes[1] = BitConverter.GetBytes(word[0])[1];
            bytes[2] = BitConverter.GetBytes(word[0])[2];
            bytes[3] = BitConverter.GetBytes(word[0])[3];
            bytes[4] = BitConverter.GetBytes(word[1])[0];
            bytes[5] = BitConverter.GetBytes(word[1])[1];
            bytes[6] = BitConverter.GetBytes(word[1])[2];
            bytes[7] = BitConverter.GetBytes(word[1])[3];
            bytes[8] = BitConverter.GetBytes(word[2])[0];
            bytes[9] = BitConverter.GetBytes(word[2])[1];
            bytes[10] = BitConverter.GetBytes(word[2])[2];
            bytes[11] = BitConverter.GetBytes(word[2])[3];
        }
    };

    //! CAN TEF Message Object

    public class CAN_TEF_MSGOBJ
    {

        public class bFrame
        {
            public CAN_MSGOBJ_ID id = new CAN_MSGOBJ_ID();
            public CAN_TX_MSGOBJ_CTRL ctrl = new CAN_TX_MSGOBJ_CTRL();
            public uint timeStamp;
        }
        public bFrame bF = new bFrame();
        public uint[] word = new uint[3];
        public byte[] bytes = new byte[12];

        public void UpdateFromWord()
        {
            bytes[0] = BitConverter.GetBytes(word[0])[0];
            bytes[1] = BitConverter.GetBytes(word[0])[1];
            bytes[2] = BitConverter.GetBytes(word[0])[2];
            bytes[3] = BitConverter.GetBytes(word[0])[3];
            bytes[4] = BitConverter.GetBytes(word[1])[0];
            bytes[5] = BitConverter.GetBytes(word[1])[1];
            bytes[6] = BitConverter.GetBytes(word[1])[2];
            bytes[7] = BitConverter.GetBytes(word[1])[3];
            bytes[8] = BitConverter.GetBytes(word[2])[0];
            bytes[9] = BitConverter.GetBytes(word[2])[1];
            bytes[10] = BitConverter.GetBytes(word[2])[2];
            bytes[11] = BitConverter.GetBytes(word[2])[3];

            bF.id.unimplemented1 = (word[0] & 0xC000_0000) >> 30;
            bF.id.SID11 = (word[0] & 0x2000_0000) >> 29;
            bF.id.EID = (word[0] & 0x1FFF_F800) >> 11;
            bF.id.SID = (word[0] & 0x3FF);
            bF.ctrl.SEQ = (word[1] & 0xFFFF_FE00) >> 9;
            bF.ctrl.ESI = (word[1] & 0x100) >> 8;
            bF.ctrl.FDF = (word[1] & 0x80) >> 7;
            bF.ctrl.BRS = (word[1] & 0x40) >> 6;
            bF.ctrl.RTR = (word[1] & 0x20) >> 5;
            bF.ctrl.IDE = (word[1] & 0x10) >> 4;
            bF.ctrl.DLC = (word[1] & 0xF);
            bF.timeStamp = word[2];
        }

        public void UpdateFromBytes()
        {
            word[0] = BitConverter.ToUInt32(bytes, 0);
            word[1] = BitConverter.ToUInt32(bytes, 4);
            word[2] = BitConverter.ToUInt32(bytes, 8);

            bF.id.unimplemented1 = (word[0] & 0xC000_0000) >> 30;
            bF.id.SID11 = (word[0] & 0x2000_0000) >> 29;
            bF.id.EID = (word[0] & 0x1FFF_F800) >> 11;
            bF.id.SID = (word[0] & 0x3FF);
            bF.ctrl.SEQ = (word[1] & 0xFFFF_FE00) >> 9;
            bF.ctrl.ESI = (word[1] & 0x100) >> 8;
            bF.ctrl.FDF = (word[1] & 0x80) >> 7;
            bF.ctrl.BRS = (word[1] & 0x40) >> 6;
            bF.ctrl.RTR = (word[1] & 0x20) >> 5;
            bF.ctrl.IDE = (word[1] & 0x10) >> 4;
            bF.ctrl.DLC = (word[1] & 0xF);
            bF.timeStamp = word[2];
        }

        public void UpdateFromReg()
        {
            word[0] |= bF.id.unimplemented1 << 30;
            word[0] |= bF.id.SID11 << 29;
            word[0] |= bF.id.EID << 11;
            word[0] |= bF.id.SID;

            word[1] |= bF.ctrl.SEQ << 9;
            word[1] |= bF.ctrl.ESI << 8;
            word[1] |= bF.ctrl.FDF << 7;
            word[1] |= bF.ctrl.BRS << 6;
            word[1] |= bF.ctrl.RTR << 5;
            word[1] |= bF.ctrl.IDE << 4;
            word[1] |= bF.ctrl.DLC;

            word[2] = bF.timeStamp;

            bytes[0] = BitConverter.GetBytes(word[0])[0];
            bytes[1] = BitConverter.GetBytes(word[0])[1];
            bytes[2] = BitConverter.GetBytes(word[0])[2];
            bytes[3] = BitConverter.GetBytes(word[0])[3];
            bytes[4] = BitConverter.GetBytes(word[1])[0];
            bytes[5] = BitConverter.GetBytes(word[1])[1];
            bytes[6] = BitConverter.GetBytes(word[1])[2];
            bytes[7] = BitConverter.GetBytes(word[1])[3];
            bytes[8] = BitConverter.GetBytes(word[2])[0];
            bytes[9] = BitConverter.GetBytes(word[2])[1];
            bytes[10] = BitConverter.GetBytes(word[2])[2];
            bytes[11] = BitConverter.GetBytes(word[2])[3];
        }
    };

    //! CAN Filter Object ID

    public class CAN_FILTEROBJ_ID
    {
        public uint SID;
        public uint EID;
        public uint SID11;
        public uint EXIDE;
        public uint unimplemented1;
    }

    //! CAN Mask Object ID

    public class CAN_MASKOBJ_ID
    {
        public uint MSID;
        public uint MEID;
        public uint MSID11;
        public uint MIDE;
        public uint unimplemented1;
    }

    //! CAN RX FIFO Status

    public enum CAN_RX_FIFO_STATUS
    {
        CAN_RX_FIFO_EMPTY = 0,
        CAN_RX_FIFO_STATUS_MASK = 0x0F,
        CAN_RX_FIFO_NOT_EMPTY = 0x01,
        CAN_RX_FIFO_HALF_FULL = 0x02,
        CAN_RX_FIFO_FULL = 0x04,
        CAN_RX_FIFO_OVERFLOW = 0x08
    };

    //! CAN TX FIFO Status

    public enum CAN_TX_FIFO_STATUS
    {
        CAN_TX_FIFO_FULL = 0,
        CAN_TX_FIFO_STATUS_MASK = 0x1F7,
        CAN_TX_FIFO_NOT_FULL = 0x01,
        CAN_TX_FIFO_HALF_FULL = 0x02,
        CAN_TX_FIFO_EMPTY = 0x04,
        CAN_TX_FIFO_ATTEMPTS_EXHAUSTED = 0x10,
        CAN_TX_FIFO_ERROR = 0x20,
        CAN_TX_FIFO_ARBITRATION_LOST = 0x40,
        CAN_TX_FIFO_ABORTED = 0x80,
        CAN_TX_FIFO_TRANSMITTING = 0x100
    };

    //! CAN TEF FIFO Status

    public enum CAN_TEF_FIFO_STATUS
    {
        CAN_TEF_FIFO_EMPTY = 0,
        CAN_TEF_FIFO_STATUS_MASK = 0x0F,
        CAN_TEF_FIFO_NOT_EMPTY = 0x01,
        CAN_TEF_FIFO_HALF_FULL = 0x02,
        CAN_TEF_FIFO_FULL = 0x04,
        CAN_TEF_FIFO_OVERFLOW = 0x08
    };

    //! CAN Module Event (Interrupts)

    public enum CAN_MODULE_EVENT
    {
        CAN_NO_EVENT = 0,
        CAN_ALL_EVENTS = 0xFF1F,
        CAN_TX_EVENT = 0x0001,
        CAN_RX_EVENT = 0x0002,
        CAN_TIME_BASE_COUNTER_EVENT = 0x0004,
        CAN_OPERATION_MODE_CHANGE_EVENT = 0x0008,
        CAN_TEF_EVENT = 0x0010,

        CAN_RAM_ECC_EVENT = 0x0100,
        CAN_SPI_CRC_EVENT = 0x0200,
        CAN_TX_ATTEMPTS_EVENT = 0x0400,
        CAN_RX_OVERFLOW_EVENT = 0x0800,
        CAN_SYSTEM_ERROR_EVENT = 0x1000,
        CAN_BUS_ERROR_EVENT = 0x2000,
        CAN_BUS_WAKEUP_EVENT = 0x4000,
        CAN_RX_INVALID_MESSAGE_EVENT = 0x8000
    };

    //! CAN TX FIFO Event (Interrupts)

    public enum CAN_TX_FIFO_EVENT
    {
        CAN_TX_FIFO_NO_EVENT = 0,
        CAN_TX_FIFO_ALL_EVENTS = 0x17,
        CAN_TX_FIFO_NOT_FULL_EVENT = 0x01,
        CAN_TX_FIFO_HALF_FULL_EVENT = 0x02,
        CAN_TX_FIFO_EMPTY_EVENT = 0x04,
        CAN_TX_FIFO_ATTEMPTS_EXHAUSTED_EVENT = 0x10
    };

    //! CAN RX FIFO Event (Interrupts)

    public enum CAN_RX_FIFO_EVENT
    {
        CAN_RX_FIFO_NO_EVENT = 0,
        CAN_RX_FIFO_ALL_EVENTS = 0x0F,
        CAN_RX_FIFO_NOT_EMPTY_EVENT = 0x01,
        CAN_RX_FIFO_HALF_FULL_EVENT = 0x02,
        CAN_RX_FIFO_FULL_EVENT = 0x04,
        CAN_RX_FIFO_OVERFLOW_EVENT = 0x08
    };

    //! CAN TEF FIFO Event (Interrupts)

    public enum CAN_TEF_FIFO_EVENT
    {
        CAN_TEF_FIFO_NO_EVENT = 0,
        CAN_TEF_FIFO_ALL_EVENTS = 0x0F,
        CAN_TEF_FIFO_NOT_EMPTY_EVENT = 0x01,
        CAN_TEF_FIFO_HALF_FULL_EVENT = 0x02,
        CAN_TEF_FIFO_FULL_EVENT = 0x04,
        CAN_TEF_FIFO_OVERFLOW_EVENT = 0x08
    };

    //! CAN Bit Time Setup: Arbitration/Data Bit Phase

    /* not apply to AVR platform */
    public enum MCP2518FD_BITTIME_SETUP : uint
    {
        CAN_125K_500K = (uint)((4UL << 24) | (125000UL)),
        CAN_250K_500K = (uint)((2UL << 24) | (250000UL)),
        CAN_250K_750K = (uint)((3UL << 24) | (250000UL)),
        CAN_250K_1M = (uint)((4UL << 24) | (250000UL)),
        CAN_250K_1M5 = (uint)((6UL << 24) | (250000UL)),
        CAN_250K_2M = (uint)((8UL << 24) | (250000UL)),
        CAN_250K_3M = (uint)((12UL << 24) | (250000UL)),
        CAN_250K_4M = (uint)((16UL << 24) | (250000UL)),
        CAN_500K_1M = (uint)((2UL << 24) | (500000UL)),
        CAN_500K_2M = (uint)((4UL << 24) | (500000UL)),
        CAN_500K_3M = (uint)((6UL << 24) | (500000UL)),
        CAN_500K_4M = (uint)((8UL << 24) | (500000UL)),
        CAN_500K_5M = (uint)((10UL << 24) | (500000UL)),
        CAN_500K_6M5 = (uint)((13UL << 24) | (500000UL)),
        CAN_500K_8M = (uint)((16UL << 24) | (500000UL)),
        CAN_500K_10M = (uint)((20UL << 24) | (500000UL)),
        CAN_1000K_4M = (uint)((4UL << 24) | (1000000UL)),
        CAN_1000K_8M = (uint)((8UL << 24) | (1000000UL)),
    };

    //! Secondary Sample Point Mode

    public enum CAN_SSP_MODE
    {
        CAN_SSP_MODE_OFF,
        CAN_SSP_MODE_MANUAL,
        CAN_SSP_MODE_AUTO
    };

    //! CAN Error State

    public enum CAN_ERROR_STATE
    {
        CAN_ERROR_FREE_STATE = 0,
        CAN_ERROR_ALL = 0x3F,
        CAN_TX_RX_WARNING_STATE = 0x01,
        CAN_RX_WARNING_STATE = 0x02,
        CAN_TX_WARNING_STATE = 0x04,
        CAN_RX_BUS_PASSIVE_STATE = 0x08,
        CAN_TX_BUS_PASSIVE_STATE = 0x10,
        CAN_TX_BUS_OFF_STATE = 0x20
    };

    //! CAN Time Stamp Mode Select

    public enum CAN_TS_MODE
    {
        CAN_TS_SOF = 0x00,
        CAN_TS_EOF = 0x01,
        CAN_TS_RES = 0x02
    };

    //! CAN ECC EVENT

    public enum CAN_ECC_EVENT
    {
        CAN_ECC_NO_EVENT = 0x00,
        CAN_ECC_ALL_EVENTS = 0x06,
        CAN_ECC_SEC_EVENT = 0x02,
        CAN_ECC_DED_EVENT = 0x04
    };

    //! CAN CRC EVENT

    public enum CAN_CRC_EVENT
    {
        CAN_CRC_NO_EVENT = 0x00,
        CAN_CRC_ALL_EVENTS = 0x03,
        CAN_CRC_CRCERR_EVENT = 0x01,
        CAN_CRC_FORMERR_EVENT = 0x02
    };

    //! GPIO Pin Position

    public enum GPIO_PIN_POS { GPIO_PIN_0, GPIO_PIN_1 };

    //! GPIO Pin Modes

    public enum GPIO_PIN_MODE { GPIO_MODE_INT, GPIO_MODE_GPIO };

    //! GPIO Pin Directions

    public enum GPIO_PIN_DIRECTION { GPIO_OUTPUT, GPIO_INPUT };

    //! GPIO Open Drain Mode

    public enum GPIO_OPEN_DRAIN_MODE { GPIO_PUSH_PULL, GPIO_OPEN_DRAIN };

    //! GPIO Pin State

    public enum GPIO_PIN_STATE { GPIO_LOW, GPIO_HIGH };

    //! Clock Output Mode

    public enum GPIO_CLKO_MODE { GPIO_CLKO_CLOCK, GPIO_CLKO_SOF };

    //! CAN Bus Diagnostic flags

    public class CAN_BUS_DIAG_FLAGS
    {
        public uint NBIT0_ERR;
        public uint NBIT1_ERR;
        public uint NACK_ERR;
        public uint NFORM_ERR;
        public uint NSTUFF_ERR;
        public uint NCRC_ERR;
        public uint unimplemented1;
        public uint TXBO_ERR;
        public uint DBIT0_ERR;
        public uint DBIT1_ERR;
        public uint unimplemented2;
        public uint DFORM_ERR;
        public uint DSTUFF_ERR;
        public uint DCRC_ERR;
        public uint ESI;
        public uint DLC_MISMATCH;
    };

    //! CAN Bus Diagnostic Error Counts

    public class CAN_BUS_ERROR_COUNT
    {
        public byte NREC;
        public byte NTEC;
        public byte DREC;
        public byte DTEC;
    };

    //! CAN BUS DIAGNOSTICS

    public class CAN_BUS_DIAGNOSTIC
    {

        public class bFrame
        {
            public CAN_BUS_ERROR_COUNT errorCount = new CAN_BUS_ERROR_COUNT();
            public ushort errorFreeMsgCount;
            public CAN_BUS_DIAG_FLAGS flag = new CAN_BUS_DIAG_FLAGS();
        }
        public bFrame bF = new bFrame();
        uint[] word = new uint[3];
        byte[] bytes = new byte[12];

        public void UpdateFromWord()
        {
            bytes[0] = BitConverter.GetBytes(word[0])[0];
            bytes[1] = BitConverter.GetBytes(word[0])[1];
            bytes[2] = BitConverter.GetBytes(word[0])[2];
            bytes[3] = BitConverter.GetBytes(word[0])[3];
            bytes[4] = BitConverter.GetBytes(word[1])[0];
            bytes[5] = BitConverter.GetBytes(word[1])[1];
            bytes[6] = BitConverter.GetBytes(word[1])[2];
            bytes[7] = BitConverter.GetBytes(word[1])[3];
            bytes[8] = BitConverter.GetBytes(word[2])[0];
            bytes[9] = BitConverter.GetBytes(word[2])[1];
            bytes[10] = BitConverter.GetBytes(word[2])[2];
            bytes[11] = BitConverter.GetBytes(word[2])[3];

            bF.errorCount.DTEC = (byte)((word[0] & 0xFF00_0000) >> 24);
            bF.errorCount.DREC = (byte)((word[0] & 0xFF_0000) >> 16);
            bF.errorCount.NTEC = (byte)((word[0] & 0xFF00) >> 8);
            bF.errorCount.NREC = (byte)((word[0] & 0xFF));

            bF.flag.DLC_MISMATCH = (word[1] & 0x8000_0000) >> 31;
            bF.flag.ESI = (word[1] & 0x4000_0000) >> 30;
            bF.flag.DCRC_ERR = (word[1] & 0x2000_0000) >> 29;
            bF.flag.DSTUFF_ERR = (word[1] & 0x1000_0000) >> 28;
            bF.flag.DFORM_ERR = (word[1] & 0x800_000) >> 27;
            bF.flag.unimplemented1 = (word[1] & 0x400_0000) >> 26;
            bF.flag.DBIT1_ERR = (word[1] & 0x200_0000) >> 25;
            bF.flag.DBIT0_ERR = (word[1] & 0x100_0000) >> 24;
            bF.flag.TXBO_ERR = (word[1] & 0x80_0000) >> 23;
            bF.flag.unimplemented2 = (word[1] & 0x40_0000) >> 22;
            bF.flag.NCRC_ERR = (word[1] & 0x20_0000) >> 21;
            bF.flag.NSTUFF_ERR = (word[1] & 0x10_0000) >> 20;
            bF.flag.NFORM_ERR = (word[1] & 0x8_0000) >> 19;
            bF.flag.NACK_ERR = (word[1] & 0x4_0000) >> 18;
            bF.flag.NBIT1_ERR = (word[1] & 0x2_0000) >> 17;
            bF.flag.NBIT0_ERR = (word[1] & 0x1_0000) >> 16;
            bF.errorFreeMsgCount = (ushort)(word[1] & 0xFFFF);
        }

        public void UpdateFromBytes()
        {
            word[0] = BitConverter.ToUInt32(bytes, 0);
            word[1] = BitConverter.ToUInt32(bytes, 4);
            word[2] = BitConverter.ToUInt32(bytes, 8);

            bF.errorCount.DTEC = (byte)((word[0] & 0xFF00_0000) >> 24);
            bF.errorCount.DREC = (byte)((word[0] & 0xFF_0000) >> 16);
            bF.errorCount.NTEC = (byte)((word[0] & 0xFF00) >> 8);
            bF.errorCount.NREC = (byte)((word[0] & 0xFF));

            bF.flag.DLC_MISMATCH = (word[1] & 0x8000_0000) >> 31;
            bF.flag.ESI = (word[1] & 0x4000_0000) >> 30;
            bF.flag.DCRC_ERR = (word[1] & 0x2000_0000) >> 29;
            bF.flag.DSTUFF_ERR = (word[1] & 0x1000_0000) >> 28;
            bF.flag.DFORM_ERR = (word[1] & 0x800_000) >> 27;
            bF.flag.unimplemented1 = (word[1] & 0x400_0000) >> 26;
            bF.flag.DBIT1_ERR = (word[1] & 0x200_0000) >> 25;
            bF.flag.DBIT0_ERR = (word[1] & 0x100_0000) >> 24;
            bF.flag.TXBO_ERR = (word[1] & 0x80_0000) >> 23;
            bF.flag.unimplemented2 = (word[1] & 0x40_0000) >> 22;
            bF.flag.NCRC_ERR = (word[1] & 0x20_0000) >> 21;
            bF.flag.NSTUFF_ERR = (word[1] & 0x10_0000) >> 20;
            bF.flag.NFORM_ERR = (word[1] & 0x8_0000) >> 19;
            bF.flag.NACK_ERR = (word[1] & 0x4_0000) >> 18;
            bF.flag.NBIT1_ERR = (word[1] & 0x2_0000) >> 17;
            bF.flag.NBIT0_ERR = (word[1] & 0x1_0000) >> 16;
            bF.errorFreeMsgCount = (ushort)(word[1] & 0xFFFF);
        }

        public void UpdateFromReg()
        {
            word[0] |= (uint)bF.errorCount.DTEC << 24;
            word[0] |= (uint)bF.errorCount.DREC << 16;
            word[0] |= (uint)bF.errorCount.NTEC << 8;
            word[0] |= (uint)bF.errorCount.NREC;

            word[1] |= bF.flag.DLC_MISMATCH << 31;
            word[1] |= bF.flag.ESI << 30;
            word[1] |= bF.flag.DCRC_ERR << 29;
            word[1] |= bF.flag.DSTUFF_ERR << 28;
            word[1] |= bF.flag.DFORM_ERR << 27;
            word[1] |= bF.flag.unimplemented1 << 26;
            word[1] |= bF.flag.DBIT1_ERR << 25;
            word[1] |= bF.flag.DBIT0_ERR << 24;
            word[1] |= bF.flag.TXBO_ERR << 23;
            word[1] |= bF.flag.unimplemented1 << 22;
            word[1] |= bF.flag.NCRC_ERR << 21;
            word[1] |= bF.flag.NSTUFF_ERR << 20;
            word[1] |= bF.flag.NFORM_ERR << 19;
            word[1] |= bF.flag.NACK_ERR << 18;
            word[1] |= bF.flag.NBIT1_ERR << 17;
            word[1] |= bF.flag.NBIT0_ERR << 16;
            word[1] |= bF.errorFreeMsgCount;

            bytes[0] = BitConverter.GetBytes(word[0])[0];
            bytes[1] = BitConverter.GetBytes(word[0])[1];
            bytes[2] = BitConverter.GetBytes(word[0])[2];
            bytes[3] = BitConverter.GetBytes(word[0])[3];
            bytes[4] = BitConverter.GetBytes(word[1])[0];
            bytes[5] = BitConverter.GetBytes(word[1])[1];
            bytes[6] = BitConverter.GetBytes(word[1])[2];
            bytes[7] = BitConverter.GetBytes(word[1])[3];
            bytes[8] = BitConverter.GetBytes(word[2])[0];
            bytes[9] = BitConverter.GetBytes(word[2])[1];
            bytes[10] = BitConverter.GetBytes(word[2])[2];
            bytes[11] = BitConverter.GetBytes(word[2])[3];
        }

    };

    //! TXREQ Channel Bits
    // Multiple channels can be or'ed together

    public enum CAN_TXREQ_CHANNEL : uint
    {
        CAN_TXREQ_CH0 = 0x00000001,
        CAN_TXREQ_CH1 = 0x00000002,
        CAN_TXREQ_CH2 = 0x00000004,
        CAN_TXREQ_CH3 = 0x00000008,
        CAN_TXREQ_CH4 = 0x00000010,
        CAN_TXREQ_CH5 = 0x00000020,
        CAN_TXREQ_CH6 = 0x00000040,
        CAN_TXREQ_CH7 = 0x00000080,

        CAN_TXREQ_CH8 = 0x00000100,
        CAN_TXREQ_CH9 = 0x00000200,
        CAN_TXREQ_CH10 = 0x00000400,
        CAN_TXREQ_CH11 = 0x00000800,
        CAN_TXREQ_CH12 = 0x00001000,
        CAN_TXREQ_CH13 = 0x00002000,
        CAN_TXREQ_CH14 = 0x00004000,
        CAN_TXREQ_CH15 = 0x00008000,

        CAN_TXREQ_CH16 = 0x00010000,
        CAN_TXREQ_CH17 = 0x00020000,
        CAN_TXREQ_CH18 = 0x00040000,
        CAN_TXREQ_CH19 = 0x00080000,
        CAN_TXREQ_CH20 = 0x00100000,
        CAN_TXREQ_CH21 = 0x00200000,
        CAN_TXREQ_CH22 = 0x00400000,
        CAN_TXREQ_CH23 = 0x00800000,

        CAN_TXREQ_CH24 = 0x01000000,
        CAN_TXREQ_CH25 = 0x02000000,
        CAN_TXREQ_CH26 = 0x04000000,
        CAN_TXREQ_CH27 = 0x08000000,
        CAN_TXREQ_CH28 = 0x10000000,
        CAN_TXREQ_CH29 = 0x20000000,
        CAN_TXREQ_CH30 = 0x40000000,
        CAN_TXREQ_CH31 = 0x80000000
    };

    //! Oscillator Control

    public class CAN_OSC_CTRL
    {
        public uint PllEnable;
        public uint OscDisable;
        public uint SclkDivide;
        public uint ClkOutDivide;
        public uint LowPowerModeEnable;

    };

    //! Oscillator Status

    public class CAN_OSC_STATUS
    {
        public uint PllReady;
        public uint OscReady;
        public uint SclkReady;
    };

    //! ICODE

    public enum CAN_ICODE
    {
        CAN_ICODE_FIFO_CH0,
        CAN_ICODE_FIFO_CH1,
        CAN_ICODE_FIFO_CH2,
        CAN_ICODE_FIFO_CH3,
        CAN_ICODE_FIFO_CH4,
        CAN_ICODE_FIFO_CH5,
        CAN_ICODE_FIFO_CH6,
        CAN_ICODE_FIFO_CH7,
        CAN_ICODE_FIFO_CH8,
        CAN_ICODE_FIFO_CH9,
        CAN_ICODE_FIFO_CH10,
        CAN_ICODE_FIFO_CH11,
        CAN_ICODE_FIFO_CH12,
        CAN_ICODE_FIFO_CH13,
        CAN_ICODE_FIFO_CH14,
        CAN_ICODE_FIFO_CH15,
        CAN_ICODE_FIFO_CH16,
        CAN_ICODE_FIFO_CH17,
        CAN_ICODE_FIFO_CH18,
        CAN_ICODE_FIFO_CH19,
        CAN_ICODE_FIFO_CH20,
        CAN_ICODE_FIFO_CH21,
        CAN_ICODE_FIFO_CH22,
        CAN_ICODE_FIFO_CH23,
        CAN_ICODE_FIFO_CH24,
        CAN_ICODE_FIFO_CH25,
        CAN_ICODE_FIFO_CH26,
        CAN_ICODE_FIFO_CH27,
        CAN_ICODE_FIFO_CH28,
        CAN_ICODE_FIFO_CH29,
        CAN_ICODE_FIFO_CH30,
        CAN_ICODE_FIFO_CH31,
        CAN_ICODE_TOTAL_CHANNELS,
        CAN_ICODE_NO_INT = 0x40,
        CAN_ICODE_CERRIF,
        CAN_ICODE_WAKIF,
        CAN_ICODE_RXOVIF,
        CAN_ICODE_ADDRERR_SERRIF,
        CAN_ICODE_MABOV_SERRIF,
        CAN_ICODE_TBCIF,
        CAN_ICODE_MODIF,
        CAN_ICODE_IVMIF,
        CAN_ICODE_TEFIF,
        CAN_ICODE_TXATIF,
        CAN_ICODE_RESERVED
    };

    //! RXCODE

    public enum CAN_RXCODE
    {
        CAN_RXCODE_FIFO_CH1 = 1,
        CAN_RXCODE_FIFO_CH2,
        CAN_RXCODE_FIFO_CH3,
        CAN_RXCODE_FIFO_CH4,
        CAN_RXCODE_FIFO_CH5,
        CAN_RXCODE_FIFO_CH6,
        CAN_RXCODE_FIFO_CH7,
        CAN_RXCODE_FIFO_CH8,
        CAN_RXCODE_FIFO_CH9,
        CAN_RXCODE_FIFO_CH10,
        CAN_RXCODE_FIFO_CH11,
        CAN_RXCODE_FIFO_CH12,
        CAN_RXCODE_FIFO_CH13,
        CAN_RXCODE_FIFO_CH14,
        CAN_RXCODE_FIFO_CH15,
        CAN_RXCODE_FIFO_CH16,
        CAN_RXCODE_FIFO_CH17,
        CAN_RXCODE_FIFO_CH18,
        CAN_RXCODE_FIFO_CH19,
        CAN_RXCODE_FIFO_CH20,
        CAN_RXCODE_FIFO_CH21,
        CAN_RXCODE_FIFO_CH22,
        CAN_RXCODE_FIFO_CH23,
        CAN_RXCODE_FIFO_CH24,
        CAN_RXCODE_FIFO_CH25,
        CAN_RXCODE_FIFO_CH26,
        CAN_RXCODE_FIFO_CH27,
        CAN_RXCODE_FIFO_CH28,
        CAN_RXCODE_FIFO_CH29,
        CAN_RXCODE_FIFO_CH30,
        CAN_RXCODE_FIFO_CH31,
        CAN_RXCODE_TOTAL_CHANNELS,
        CAN_RXCODE_NO_INT = 0x40,
        CAN_RXCODE_RESERVED
    };

    //! TXCODE

    public enum CAN_TXCODE
    {
        CAN_TXCODE_FIFO_CH0,
        CAN_TXCODE_FIFO_CH1,
        CAN_TXCODE_FIFO_CH2,
        CAN_TXCODE_FIFO_CH3,
        CAN_TXCODE_FIFO_CH4,
        CAN_TXCODE_FIFO_CH5,
        CAN_TXCODE_FIFO_CH6,
        CAN_TXCODE_FIFO_CH7,
        CAN_TXCODE_FIFO_CH8,
        CAN_TXCODE_FIFO_CH9,
        CAN_TXCODE_FIFO_CH10,
        CAN_TXCODE_FIFO_CH11,
        CAN_TXCODE_FIFO_CH12,
        CAN_TXCODE_FIFO_CH13,
        CAN_TXCODE_FIFO_CH14,
        CAN_TXCODE_FIFO_CH15,
        CAN_TXCODE_FIFO_CH16,
        CAN_TXCODE_FIFO_CH17,
        CAN_TXCODE_FIFO_CH18,
        CAN_TXCODE_FIFO_CH19,
        CAN_TXCODE_FIFO_CH20,
        CAN_TXCODE_FIFO_CH21,
        CAN_TXCODE_FIFO_CH22,
        CAN_TXCODE_FIFO_CH23,
        CAN_TXCODE_FIFO_CH24,
        CAN_TXCODE_FIFO_CH25,
        CAN_TXCODE_FIFO_CH26,
        CAN_TXCODE_FIFO_CH27,
        CAN_TXCODE_FIFO_CH28,
        CAN_TXCODE_FIFO_CH29,
        CAN_TXCODE_FIFO_CH30,
        CAN_TXCODE_FIFO_CH31,
        CAN_TXCODE_TOTAL_CHANNELS,
        CAN_TXCODE_NO_INT = 0x40,
        CAN_TXCODE_RESERVED
    };

    //! System Clock Selection

    public enum CAN_SYSCLK_SPEED
    {
        CAN_SYSCLK_40M = MCPCanInterface.MCP_CLOCK.MCP2518FD_40MHz,
        CAN_SYSCLK_20M = MCPCanInterface.MCP_CLOCK.MCP2518FD_20MHz,
        CAN_SYSCLK_10M = MCPCanInterface.MCP_CLOCK.MCP2518FD_10MHz,
    };

    //! CLKO Divide

    public enum OSC_CLKO_DIVIDE
    {
        OSC_CLKO_DIV1,
        OSC_CLKO_DIV2,
        OSC_CLKO_DIV4,
        OSC_CLKO_DIV10
    };

    // *****************************************************************************
    //! General 32-bit Register

    public class REG_t
    {
        public void UpdateFromBytes()
        {
            word = (uint)BitConverter.ToInt32(bytes, 0);
        }

        public void UpdateFromWord()
        {
            bytes = BitConverter.GetBytes(word);
        }
        public byte[] bytes = new byte[4];
        public uint word;
    };

    // *****************************************************************************
    // *****************************************************************************
    /* CAN FD Controller */

    // *****************************************************************************
    //! CAN Control Register

    public class REG_CiCON
    {

        public class bFrame
        {
            public uint DNetFilterCount;
            public uint IsoCrcEnable;
            public uint ProtocolExceptionEventDisable;
            public uint unimplemented1;
            public uint WakeUpFilterEnable;
            public uint WakeUpFilterTime;
            public uint unimplemented2;
            public uint BitRateSwitchDisable;
            public uint unimplemented3;
            public uint RestrictReTxAttempts;
            public uint EsiInGatewayMode;
            public uint SystemErrorToListenOnly;
            public uint StoreInTEF;
            public uint TXQEnable;
            public uint OpMode;
            public uint RequestOpMode;
            public uint AbortAllTx;
            public uint TxBandWidthSharing;
        }

        public bFrame bF = new();

        public uint word;

        public byte[] bytes = new byte[4];
        public void UpdateFromWord()
        {
            bytes = BitConverter.GetBytes(word);
            bF.DNetFilterCount = word & 0x1F;
            bF.IsoCrcEnable = (word & 0x20) >> 5;
            bF.ProtocolExceptionEventDisable = (word & 0x40) >> 6;
            bF.unimplemented1 = (word & 0x80) >> 7;
            bF.WakeUpFilterEnable = (word & 0x100) >> 8;
            bF.WakeUpFilterTime = (word & 0x600) >> 9;
            bF.unimplemented2 = (word & 0x800) >> 11;
            bF.BitRateSwitchDisable = (word & 0x1000) >> 12;
            bF.unimplemented3 = (word & 0xE000) >> 13;
            bF.RestrictReTxAttempts = (word & 0x10000) >> 16;
            bF.EsiInGatewayMode = (word & 0x20000) >> 17;
            bF.SystemErrorToListenOnly = (word & 0x40000) >> 18;
            bF.StoreInTEF = (word & 0x80000) >> 19;
            bF.TXQEnable = (word & 0x100000) >> 20;
            bF.OpMode = (word & 0xE00000) >> 21;
            bF.RequestOpMode = (word & 0x3800000) >> 24;
            bF.AbortAllTx = (word & 0x8000000) >> 27;
            bF.TxBandWidthSharing = (word & 0xF000_0000) >> 28;
        }

        public void UpdateFromBytes()
        {
            word = BitConverter.ToUInt32(bytes, 0);
            bF.DNetFilterCount = word & 0x1F;
            bF.IsoCrcEnable = (word & 0x20) >> 5;
            bF.ProtocolExceptionEventDisable = (word & 0x40) >> 6;
            bF.unimplemented1 = (word & 0x80) >> 7;
            bF.WakeUpFilterEnable = (word & 0x100) >> 8;
            bF.WakeUpFilterTime = (word & 0x600) >> 9;
            bF.unimplemented2 = (word & 0x800) >> 11;
            bF.BitRateSwitchDisable = (word & 0x1000) >> 12;
            bF.unimplemented3 = (word & 0xE000) >> 13;
            bF.RestrictReTxAttempts = (word & 0x10000) >> 16;
            bF.EsiInGatewayMode = (word & 0x20000) >> 17;
            bF.SystemErrorToListenOnly = (word & 0x40000) >> 18;
            bF.StoreInTEF = (word & 0x80000) >> 19;
            bF.TXQEnable = (word & 0x100000) >> 20;
            bF.OpMode = (word & 0xE00000) >> 21;
            bF.RequestOpMode = (word & 0x3800000) >> 24;
            bF.AbortAllTx = (word & 0x8000000) >> 27;
            bF.TxBandWidthSharing = (word & 0xF000_0000) >> 28;
        }

        public void UpdateFromReg()
        {
            word |= (bF.TxBandWidthSharing << 28);
            word |= (bF.AbortAllTx << 27);
            word |= (bF.RequestOpMode << 24);
            word |= (bF.OpMode << 21);
            word |= (bF.TXQEnable << 20);
            word |= (bF.StoreInTEF << 19);
            word |= (bF.SystemErrorToListenOnly << 18);
            word |= (bF.EsiInGatewayMode << 17);
            word |= (bF.RestrictReTxAttempts << 16);
            word |= (bF.unimplemented3 << 13);
            word |= (bF.BitRateSwitchDisable << 12);
            word |= (bF.unimplemented2 << 11);
            word |= (bF.WakeUpFilterTime << 9);
            word |= (bF.WakeUpFilterEnable << 8);
            word |= (bF.unimplemented1 << 7);
            word |= (bF.ProtocolExceptionEventDisable << 6);
            word |= (bF.IsoCrcEnable << 5);
            word |= (bF.DNetFilterCount);
            bytes = BitConverter.GetBytes(word);
        }
    };

    // *****************************************************************************
    //! Nominal Bit Time Configuration Register

    public class REG_CiNBTCFG
    {

        public class bFrame
        {
            public uint SJW;
            public uint unimplemented1;
            public uint TSEG2;
            public uint unimplemented2;
            public uint TSEG1;
            public uint BRP;
        }
        public bFrame bF = new bFrame();
        public uint word;
        public byte[] bytes = new byte[4];

        public void UpdateFromWord()
        {
            bytes = BitConverter.GetBytes(word);
            bF.SJW = (word & 0x7F);
            bF.unimplemented1 = (word & 0x80) >> 7;
            bF.TSEG2 = (word & 0x7F00) >> 8;
            bF.unimplemented2 = (word & 0x8000) >> 15;
            bF.TSEG1 = (word & 0xFF_0000) >> 16;
            bF.BRP = (word & 0xFF00_0000) >> 24;
        }

        public void UpdateFromBytes()
        {
            word = BitConverter.ToUInt32(bytes, 0);
            bF.SJW = (word & 0x7F);
            bF.unimplemented1 = (word & 0x80) >> 7;
            bF.TSEG2 = (word & 0x7F00) >> 8;
            bF.unimplemented2 = (word & 0x8000) >> 15;
            bF.TSEG1 = (word & 0xFF_0000) >> 16;
            bF.BRP = (word & 0xFF00_0000) >> 24;
        }

        public void UpdateFromReg()
        {
            word |= bF.BRP << 24;
            word |= bF.TSEG1 << 16;
            word |= bF.unimplemented2 << 15;
            word |= bF.TSEG2 << 8;
            word |= bF.unimplemented1 << 7;
            word |= bF.SJW;
            bytes = BitConverter.GetBytes(word);
        }
    };

    // *****************************************************************************
    //! Data Bit Time Configuration Register

    public class REG_CiDBTCFG
    {

        public class bFrame
        {
            public uint SJW;
            public uint unimplemented1;
            public uint TSEG2;
            public uint unimplemented2;
            public uint TSEG1;
            public uint unimplemented3;
            public uint BRP;
        }
        public bFrame bF = new bFrame();
        public uint word;
        public byte[] bytes = new byte[4];

        public void UpdateFromWord()
        {
            bytes = BitConverter.GetBytes(word);

            bF.BRP = (word & 0xFF00_0000) >> 24;
            bF.unimplemented3 = (word & 0x700000) >> 21;
            bF.TSEG1 = (word & 0xF_8000) >> 16;
            bF.unimplemented2 = (word & 0x7800) >> 12;
            bF.TSEG2 = (word & 0x780) >> 8;
            bF.unimplemented1 = (word & 0xF0) >> 4;
            bF.SJW = (word & 0xF);
        }

        public void UpdateFromBytes()
        {
            word = BitConverter.ToUInt32(bytes, 0);

            bF.BRP = (word & 0xFF00_0000) >> 24;
            bF.unimplemented3 = (word & 0x700000) >> 21;
            bF.TSEG1 = (word & 0xF_8000) >> 16;
            bF.unimplemented2 = (word & 0x7800) >> 12;
            bF.TSEG2 = (word & 0x780) >> 8;
            bF.unimplemented1 = (word & 0xF0) >> 4;
            bF.SJW = (word & 0xF);
        }

        public void UpdateFromReg()
        {
            word |= bF.BRP << 24;
            word |= bF.unimplemented3 << 21;
            word |= bF.TSEG1 << 16;
            word |= bF.unimplemented2 << 12;
            word |= bF.TSEG2 << 8;
            word |= bF.unimplemented1 << 4;
            word |= bF.SJW;

            bytes = BitConverter.GetBytes(word);
        }
    };

    // *****************************************************************************
    //! Transmitter Delay Compensation Register

    public class REG_CiTDC
    {

        public class bFrame
        {
            public uint TDCValue;
            public uint unimplemented1;
            public uint TDCOffset;
            public uint unimplemented2;
            public uint TDCMode;
            public uint unimplemented3;
            public uint SID11Enable;
            public uint EdgeFilterEnable;
            public uint unimplemented4;
        }
        public bFrame bF = new bFrame();
        public uint word;
        public byte[] bytes = new byte[4];

        public void UpdateFromWord()
        {
            bytes = BitConverter.GetBytes(word);

            bF.unimplemented4 = (word & 0xFC00_0000) >> 26;
            bF.EdgeFilterEnable = (word & 0x200_0000) >> 25;
            bF.EdgeFilterEnable = (word & 0x100_0000) >> 24;
            bF.unimplemented3 = (word & 0xFC_0000) >> 18;
            bF.TDCMode = (word & 0x3000) >> 16;
            bF.unimplemented2 = (word & 0x8000) >> 15;
            bF.TDCOffset = (word & 0x3F00) >> 8;
            bF.unimplemented1 = (word & 0xC0) >> 6;
            bF.TDCValue = (word & 0x1F);
        }

        public void UpdateFromBytes()
        {
            word = BitConverter.ToUInt32(bytes, 0);

            bF.unimplemented4 = (word & 0xFC00_0000) >> 26;
            bF.EdgeFilterEnable = (word & 0x200_0000) >> 25;
            bF.EdgeFilterEnable = (word & 0x100_0000) >> 24;
            bF.unimplemented3 = (word & 0xFC_0000) >> 18;
            bF.TDCMode = (word & 0x3000) >> 16;
            bF.unimplemented2 = (word & 0x8000) >> 15;
            bF.TDCOffset = (word & 0x3F00) >> 8;
            bF.unimplemented1 = (word & 0xC0) >> 6;
            bF.TDCValue = (word & 0x1F);
        }

        public void UpdateFromReg()
        {
            word |= bF.unimplemented4 << 26;
            word |= bF.EdgeFilterEnable << 25;
            word |= bF.SID11Enable << 24;
            word |= bF.unimplemented3 << 18;
            word |= bF.TDCMode << 16;
            word |= bF.unimplemented2 << 15;
            word |= bF.TDCOffset << 8;
            word |= bF.unimplemented1 << 6;
            word |= bF.TDCValue;

            bytes = BitConverter.GetBytes(word);
        }
    };

    // *****************************************************************************
    //! Time Stamp Configuration Register

    public class REG_CiTSCON
    {

        public class bFrame
        {
            public uint TBCPrescaler;
            public uint unimplemented1;
            public uint TBCEnable;
            public uint TimeStampEOF;
            public uint TimeStampRES;
            public uint unimplemented2;
        }
        public bFrame bF = new bFrame();
        public uint word;
        public byte[] bytes = new byte[4];

        public void UpdateFromWord()
        {
            bytes = BitConverter.GetBytes(word);

            bF.unimplemented2 = (word & 0xFFF8_0000) >> 19;
            bF.TimeStampRES = (word & 0x4000) >> 18;
            bF.TimeStampEOF = (word & 0x2000) >> 17;
            bF.TBCEnable = (word & 0x1000) >> 16;
            bF.unimplemented1 = (word & 0xFC00) >> 10;
            bF.TBCPrescaler = (word & 0x3FF);
        }

        public void UpdateFromBytes()
        {
            word = BitConverter.ToUInt32(bytes, 0);

            bF.unimplemented2 = (word & 0xFFF8_0000) >> 19;
            bF.TimeStampRES = (word & 0x4000) >> 18;
            bF.TimeStampEOF = (word & 0x2000) >> 17;
            bF.TBCEnable = (word & 0x1000) >> 16;
            bF.unimplemented1 = (word & 0xFC00) >> 10;
            bF.TBCPrescaler = (word & 0x3FF);
        }

        public void UpdateFromReg()
        {

            word |= bF.unimplemented2 << 19;
            word |= bF.TimeStampRES << 18;
            word |= bF.TimeStampEOF << 17;
            word |= bF.TimeStampEOF << 16;
            word |= bF.unimplemented1 << 10;
            word |= bF.TBCPrescaler;

            bytes = BitConverter.GetBytes(word);
        }
    };

    // *****************************************************************************
    //! Interrupt Vector Register

    public class REG_CiVEC
    {

        public class bFrame
        {
            public uint ICODE;
            public uint unimplemented1;
            public uint FilterHit;
            public uint unimplemented2;
            public uint TXCODE;
            public uint unimplemented3;
            public uint RXCODE;
            public uint unimplemented4;
        }
        public bFrame bF = new bFrame();
        public uint word;
        public byte[] bytes = new byte[4];

        public void UpdateFromWord()
        {
            bytes = BitConverter.GetBytes(word);

            bF.unimplemented4 = (word & 0x8000_0000) >> 31;
            bF.RXCODE = (word & 0x7F00_0000) >> 24;
            bF.unimplemented3 = (word & 0x80_0000) >> 23;
            bF.TXCODE = (word & 0x7F_0000) >> 16;
            bF.unimplemented2 = (word & 0xE000) >> 13;
            bF.FilterHit = (word & 0x1F00) >> 8;
            bF.unimplemented1 = (word & 0x80) >> 7;
            bF.ICODE = (word & 0x7F);
        }

        public void UpdateFromBytes()
        {
            word = BitConverter.ToUInt32(bytes, 0);

            bF.unimplemented4 = (word & 0x8000_0000) >> 31;
            bF.RXCODE = (word & 0x7F00_0000) >> 24;
            bF.unimplemented3 = (word & 0x80_0000) >> 23;
            bF.TXCODE = (word & 0x7F_0000) >> 16;
            bF.unimplemented2 = (word & 0xE000) >> 13;
            bF.FilterHit = (word & 0x1F00) >> 8;
            bF.unimplemented1 = (word & 0x80) >> 7;
            bF.ICODE = (word & 0x7F);

        }

        public void UpdateFromReg()
        {

            word |= bF.unimplemented4 << 31;
            word |= bF.RXCODE << 24;
            word |= bF.unimplemented3 << 23;
            word |= bF.TXCODE << 16;
            word |= bF.unimplemented2 << 13;
            word |= bF.FilterHit << 8;
            word |= bF.unimplemented1 << 7;
            word |= bF.ICODE;

            bytes = BitConverter.GetBytes(word);
        }
    };

    // *****************************************************************************
    //! Interrupt Flags

    public class CAN_INT_FLAGS
    {
        public uint TXIF;
        public uint RXIF;
        public uint TBCIF;
        public uint MODIF;
        public uint TEFIF;
        public uint unimplemented1;

        public uint ECCIF;
        public uint SPICRCIF;
        public uint TXATIF;
        public uint RXOVIF;
        public uint SERRIF;
        public uint CERRIF;
        public uint WAKIF;
        public uint IVMIF;
    };

    // *****************************************************************************
    //! Interrupt Enables

    public class CAN_INT_ENABLES
    {
        public uint TXIE;
        public uint RXIE;
        public uint TBCIE;
        public uint MODIE;
        public uint TEFIE;
        public uint unimplemented2;

        public uint ECCIE;
        public uint SPICRCIE;
        public uint TXATIE;
        public uint RXOVIE;
        public uint SERRIE;
        public uint CERRIE;
        public uint WAKIE;
        public uint IVMIE;
    };

    // *****************************************************************************
    //! Interrupt Register

    public class REG_CiINT
    {

        public class bFrame
        {
            public CAN_INT_FLAGS IF = new CAN_INT_FLAGS();
            public CAN_INT_ENABLES IE = new CAN_INT_ENABLES();
        }
        public bFrame bF = new bFrame();
        public uint word;
        public byte[] bytes = new byte[4];

        public void UpdateFromWord()
        {
            bytes = BitConverter.GetBytes(word);

            bF.IE.IVMIE = (word & 0x8000_0000) >> 31;
            bF.IE.WAKIE = (word & 0x4000_0000) >> 30;
            bF.IE.CERRIE = (word & 0x2000_0000) >> 29;
            bF.IE.SERRIE = (word & 0x1000_0000) >> 28;
            bF.IE.RXIE = (word & 800_0000) >> 27;
            bF.IE.TXATIE = (word & 0x400_0000) >> 26;
            bF.IE.SPICRCIE = (word & 0x200_0000) >> 25;
            bF.IE.ECCIE = (word & 0x100_0000) >> 24;
            bF.IE.unimplemented2 = (word & 0xE0_0000);
            bF.IE.TEFIE = (word & 0x10_0000) >> 20;
            bF.IE.MODIE = (word & 0x8_0000) >> 19;
            bF.IE.TBCIE = (word & 0x4_0000) >> 18;
            bF.IE.RXIE = (word & 0x2_0000) >> 17;
            bF.IE.TXIE = (word & 0x1_0000) >> 16;

            bF.IF.IVMIF = (word & 0x8000) >> 15;
            bF.IF.WAKIF = (word & 0x4000) >> 14;
            bF.IF.CERRIF = (word & 0x2000) >> 13;
            bF.IF.SERRIF = (word & 0x1000) >> 12;
            bF.IF.RXOVIF = (word & 0x800) >> 11;
            bF.IF.TXATIF = (word & 0x400) >> 10;
            bF.IF.SPICRCIF = (word & 0x200) >> 9;
            bF.IF.ECCIF = (word & 0x100) >> 8;
            bF.IF.unimplemented1 = (word & 0xE0) >> 5;
            bF.IF.TEFIF = (word & 0x10) >> 4;
            bF.IF.MODIF = (word & 0x8) >> 3;
            bF.IF.TBCIF = (word & 0x4) >> 2;
            bF.IF.RXIF = (word & 0x2) >> 1;
            bF.IF.TXIF = (word & 0x1);



        }

        public void UpdateFromBytes()
        {
            word = BitConverter.ToUInt32(bytes, 0);

            bF.IE.IVMIE = (word & 0x8000_0000) >> 31;
            bF.IE.WAKIE = (word & 0x4000_0000) >> 30;
            bF.IE.CERRIE = (word & 0x2000_0000) >> 29;
            bF.IE.SERRIE = (word & 0x1000_0000) >> 28;
            bF.IE.RXIE = (word & 800_0000) >> 27;
            bF.IE.TXATIE = (word & 0x400_0000) >> 26;
            bF.IE.SPICRCIE = (word & 0x200_0000) >> 25;
            bF.IE.ECCIE = (word & 0x100_0000) >> 24;
            bF.IE.unimplemented2 = (word & 0xE0_0000) >> 21;
            bF.IE.TEFIE = (word & 0x10_0000) >> 20;
            bF.IE.MODIE = (word & 0x8_0000) >> 19;
            bF.IE.TBCIE = (word & 0x4_0000) >> 18;
            bF.IE.RXIE = (word & 0x2_0000) >> 17;
            bF.IE.TXIE = (word & 0x1_0000) >> 16;

            bF.IF.IVMIF = (word & 0x8000) >> 15;
            bF.IF.WAKIF = (word & 0x4000) >> 14;
            bF.IF.CERRIF = (word & 0x2000) >> 13;
            bF.IF.SERRIF = (word & 0x1000) >> 12;
            bF.IF.RXOVIF = (word & 0x800) >> 11;
            bF.IF.TXATIF = (word & 0x400) >> 10;
            bF.IF.SPICRCIF = (word & 0x200) >> 9;
            bF.IF.ECCIF = (word & 0x100) >> 8;
            bF.IF.unimplemented1 = (word & 0xE0) >> 5;
            bF.IF.TEFIF = (word & 0x10) >> 4;
            bF.IF.MODIF = (word & 0x8) >> 3;
            bF.IF.TBCIF = (word & 0x4) >> 2;
            bF.IF.RXIF = (word & 0x2) >> 1;
            bF.IF.TXIF = (word & 0x1);
        }

        public void UpdateFromReg()
        {
            word |= bF.IE.IVMIE << 31;
            word |= bF.IE.WAKIE << 30;
            word |= bF.IE.CERRIE << 29;
            word |= bF.IE.SERRIE << 28;
            word |= bF.IE.RXIE << 27;
            word |= bF.IE.TXATIE << 26;
            word |= bF.IE.SPICRCIE << 25;
            word |= bF.IE.ECCIE << 24;
            word |= bF.IE.unimplemented2 << 21;
            word |= bF.IE.TEFIE << 20;
            word |= bF.IE.MODIE << 19;
            word |= bF.IE.TBCIE << 18;
            word |= bF.IE.RXIE << 17;
            word |= bF.IE.TXIE << 16;

            word |= bF.IF.IVMIF << 15;
            word |= bF.IF.WAKIF << 14;
            word |= bF.IF.CERRIF << 13;
            word |= bF.IF.SERRIF << 12;
            word |= bF.IF.RXOVIF << 11;
            word |= bF.IF.TXATIF << 10;
            word |= bF.IF.SPICRCIF << 9;
            word |= bF.IF.ECCIF << 8;
            word |= bF.IF.unimplemented1 << 5;
            word |= bF.IF.TEFIF << 4;
            word |= bF.IF.MODIF << 3;
            word |= bF.IF.TBCIF << 2;
            word |= bF.IF.RXIF << 1;
            word |= bF.IF.TXIF;

            bytes = BitConverter.GetBytes(word);
        }
    };

    // *****************************************************************************
    //! Interrupt Flag Register

    public class REG_CiINTFLAG
    {
        public CAN_INT_FLAGS IF;
        public ushort word;
        public byte[] bytes = new byte[2];

        public void UpdateFromWord()
        {
            bytes = BitConverter.GetBytes(word);


            IF.IVMIF = (uint)((word & 0x8000) >> 15);
            IF.WAKIF = (uint)((word & 0x4000) >> 14);
            IF.CERRIF = (uint)((word & 0x2000) >> 13);
            IF.SERRIF = (uint)((word & 0x1000) >> 12);
            IF.RXOVIF = (uint)((word & 0x800) >> 11);
            IF.TXATIF = (uint)((word & 0x400) >> 10);
            IF.SPICRCIF = (uint)((word & 0x200) >> 9);
            IF.ECCIF = (uint)((word & 0x100) >> 8);
            IF.unimplemented1 = (uint)((word & 0xE0) >> 5);
            IF.TEFIF = (uint)((word & 0x10) >> 4);
            IF.MODIF = (uint)((word & 0x8) >> 3);
            IF.TBCIF = (uint)((word & 0x4) >> 2);
            IF.RXIF = (uint)((word & 0x2) >> 1);
            IF.TXIF = (uint)((word & 0x1));



        }

        public void UpdateFromBytes()
        {
            word = BitConverter.ToUInt16(bytes, 0);

            IF.IVMIF = (uint)((word & 0x8000) >> 15);
            IF.WAKIF = (uint)((word & 0x4000) >> 14);
            IF.CERRIF = (uint)((word & 0x2000) >> 13);
            IF.SERRIF = (uint)((word & 0x1000) >> 12);
            IF.RXOVIF = (uint)((word & 0x800) >> 11);
            IF.TXATIF = (uint)((word & 0x400) >> 10);
            IF.SPICRCIF = (uint)((word & 0x200) >> 9);
            IF.ECCIF = (uint)((word & 0x100) >> 8);
            IF.unimplemented1 = (uint)((word & 0xE0) >> 5);
            IF.TEFIF = (uint)((word & 0x10) >> 4);
            IF.MODIF = (uint)((word & 0x8) >> 3);
            IF.TBCIF = (uint)((word & 0x4) >> 2);
            IF.RXIF = (uint)((word & 0x2) >> 1);
            IF.TXIF = (uint)((word & 0x1));
        }

        public void UpdateFromReg()
        {

            word |= (ushort)(IF.IVMIF << 15);
            word |= (ushort)(IF.WAKIF << 14);
            word |= (ushort)(IF.CERRIF << 13);
            word |= (ushort)(IF.SERRIF << 12);
            word |= (ushort)(IF.RXOVIF << 11);
            word |= (ushort)(IF.TXATIF << 10);
            word |= (ushort)(IF.SPICRCIF << 9);
            word |= (ushort)(IF.ECCIF << 8);
            word |= (ushort)(IF.unimplemented1 << 5);
            word |= (ushort)(IF.TEFIF << 4);
            word |= (ushort)(IF.MODIF << 3);
            word |= (ushort)(IF.TBCIF << 2);
            word |= (ushort)(IF.RXIF << 1);
            word |= (ushort)IF.TXIF;

            bytes = BitConverter.GetBytes(word);
        }
    };

    // *****************************************************************************
    //! Interrupt Enable Register

    public class REG_CiINTENABLE
    {
        public CAN_INT_ENABLES IE = new CAN_INT_ENABLES();
        public ushort word;
        public byte[] bytes = new byte[2];

        public void UpdateFromWord()
        {
            bytes = BitConverter.GetBytes(word);


            IE.IVMIE = (uint)((word & 0x8000_0000) >> 15);
            IE.WAKIE = (uint)((word & 0x4000_0000) >> 14);
            IE.CERRIE = (uint)((word & 0x2000_0000) >> 13);
            IE.SERRIE = (uint)((word & 0x1000_0000) >> 12);
            IE.RXIE = (uint)((word & 800_0000) >> 11);
            IE.TXATIE = (uint)((word & 0x400_0000) >> 10);
            IE.SPICRCIE = (uint)((word & 0x200_0000) >> 9);
            IE.ECCIE = (uint)((word & 0x100_0000) >> 8);
            IE.unimplemented2 = (uint)((word & 0xE0_0000) >> 5);
            IE.TEFIE = (uint)((word & 0x10_0000) >> 4);
            IE.MODIE = (uint)((word & 0x8_0000) >> 3);
            IE.TBCIE = (uint)(word & 0x4_0000) >> 2;
            IE.RXIE = (uint)(word & 0x2_0000) >> 1;
            IE.TXIE = (uint)(word & 0x1_0000);



        }

        public void UpdateFromBytes()
        {
            word = BitConverter.ToUInt16(bytes, 0);

            IE.IVMIE = (uint)((word & 0x8000_0000) >> 15);
            IE.WAKIE = (uint)((word & 0x4000_0000) >> 14);
            IE.CERRIE = (uint)((word & 0x2000_0000) >> 13);
            IE.SERRIE = (uint)((word & 0x1000_0000) >> 12);
            IE.RXIE = (uint)((word & 800_0000) >> 11);
            IE.TXATIE = (uint)((word & 0x400_0000) >> 10);
            IE.SPICRCIE = (uint)((word & 0x200_0000) >> 9);
            IE.ECCIE = (uint)((word & 0x100_0000) >> 8);
            IE.unimplemented2 = (uint)((word & 0xE0_0000) >> 5);
            IE.TEFIE = (uint)((word & 0x10_0000) >> 4);
            IE.MODIE = (uint)((word & 0x8_0000) >> 3);
            IE.TBCIE = (uint)(word & 0x4_0000) >> 2;
            IE.RXIE = (uint)(word & 0x2_0000) >> 1;
            IE.TXIE = (uint)(word & 0x1_0000);
        }

        public void UpdateFromReg()
        {

            word |= (ushort)(IE.IVMIE << 15);
            word |= (ushort)(IE.WAKIE << 14);
            word |= (ushort)(IE.CERRIE << 13);
            word |= (ushort)(IE.SERRIE << 12);
            word |= (ushort)(IE.RXIE << 11);
            word |= (ushort)(IE.TXATIE << 10);
            word |= (ushort)(IE.SPICRCIE << 9);
            word |= (ushort)(IE.ECCIE << 8);
            word |= (ushort)(IE.unimplemented2 << 5);
            word |= (ushort)(IE.TEFIE << 4);
            word |= (ushort)(IE.MODIE << 3);
            word |= (ushort)(IE.TBCIE << 2);
            word |= (ushort)(IE.RXIE << 1);
            word |= (ushort)IE.TXIE;

            bytes = BitConverter.GetBytes(word);
        }
    };

    // *****************************************************************************
    //! Transmit/Receive Error Count Register

    public class REG_CiTREC
    {

        public class bFrame
        {
            public uint RxErrorCount;
            public uint TxErrorCount;
            public uint ErrorStateWarning;
            public uint RxErrorStateWarning;
            public uint TxErrorStateWarning;
            public uint RxErrorStatePassive;
            public uint TxErrorStatePassive;
            public uint TxErrorStateBusOff;
            public uint unimplemented1;
        }
        public bFrame bF = new bFrame();
        public uint word;
        public byte[] bytes = new byte[4];

        public void UpdateFromWord()
        {
            bytes = BitConverter.GetBytes(word);

            bF.unimplemented1 = (word & 0xFFC0_0000) >> 22;
            bF.TxErrorStateBusOff = (word & 0x20_0000) >> 21;
            bF.TxErrorStatePassive = (word & 0x10_0000) >> 20;
            bF.RxErrorStatePassive = (word & 0x8_0000) >> 19;
            bF.TxErrorStateWarning = (word & 0x4_0000) >> 18;
            bF.RxErrorStateWarning = (word & 0x2_0000) >> 17;
            bF.ErrorStateWarning = (word & 0x1_0000) >> 16;
            bF.TxErrorCount = (word & 0xFF00) >> 8;
            bF.RxErrorCount = (word & 0xFF);

        }

        public void UpdateFromBytes()
        {
            word = BitConverter.ToUInt16(bytes, 0);

            bF.unimplemented1 = (word & 0xFFC0_0000) >> 22;
            bF.TxErrorStateBusOff = (word & 0x20_0000) >> 21;
            bF.TxErrorStatePassive = (word & 0x10_0000) >> 20;
            bF.RxErrorStatePassive = (word & 0x8_0000) >> 19;
            bF.TxErrorStateWarning = (word & 0x4_0000) >> 18;
            bF.RxErrorStateWarning = (word & 0x2_0000) >> 17;
            bF.ErrorStateWarning = (word & 0x1_0000) >> 16;
            bF.TxErrorCount = (word & 0xFF00) >> 8;
            bF.RxErrorCount = (word & 0xFF);

        }

        public void UpdateFromReg()
        {

            word |= bF.unimplemented1 << 22;
            word |= bF.TxErrorStateBusOff << 21;
            word |= bF.TxErrorStatePassive << 20;
            word |= bF.RxErrorStatePassive << 19;
            word |= bF.TxErrorStateWarning << 18;
            word |= bF.RxErrorStateWarning << 17;
            word |= bF.ErrorStateWarning << 16;
            word |= bF.TxErrorCount << 8;
            word |= bF.RxErrorCount;

            bytes = BitConverter.GetBytes(word);
        }
    };

    // *****************************************************************************
    //! Diagnostic Register 0

    public class REG_CiBDIAG0
    {

        public class bFrame
        {
            public uint NRxErrorCount;
            public uint NTxErrorCount;
            public uint DRxErrorCount;
            public uint DTxErrorCount;
        }
        public bFrame bF = new bFrame();
        public uint word;
        public byte[] bytes = new byte[4];

        public void UpdateFromWord()
        {
            bytes = BitConverter.GetBytes(word);

            bF.DTxErrorCount = (word & 0xFF00_0000) >> 24;
            bF.DRxErrorCount = (word & 0xFF_0000) >> 16;
            bF.NTxErrorCount = (word & 0xFF00) >> 8;
            bF.NRxErrorCount = (word & 0xFF);

        }

        public void UpdateFromBytes()
        {
            word = BitConverter.ToUInt16(bytes, 0);

            bF.DTxErrorCount = (word & 0xFF00_0000) >> 24;
            bF.DRxErrorCount = (word & 0xFF_0000) >> 16;
            bF.NTxErrorCount = (word & 0xFF00) >> 8;
            bF.NRxErrorCount = (word & 0xFF);

        }

        public void UpdateFromReg()
        {

            word |= bF.DTxErrorCount << 24;
            word |= bF.DRxErrorCount << 16;
            word |= bF.NTxErrorCount << 8;
            word |= bF.NRxErrorCount;

            bytes = BitConverter.GetBytes(word);
        }
    };

    // *****************************************************************************
    //! Diagnostic Register 1

    public class REG_CiBDIAG1
    {

        public class bFrame
        {
            public uint ErrorFreeMsgCount;
            public uint NBit0Error;
            public uint NBit1Error;
            public uint NAckError;
            public uint NFormError;
            public uint NStuffError;
            public uint NCRCError;
            public uint unimplemented1;
            public uint TXBOError;
            public uint DBit0Error;
            public uint DBit1Error;
            public uint unimplemented2;
            public uint DFormError;
            public uint DStuffError;
            public uint DCRCError;
            public uint ESI;
            public uint DLCMM;
        }
        public bFrame bF = new bFrame();
        public uint word;
        public byte[] bytes = new byte[4];

        public void UpdateFromWord()
        {
            bytes = BitConverter.GetBytes(word);

            bF.DLCMM = (word & 0x8000_0000) >> 31;
            bF.ESI = (word & 0x4000_0000) >> 30;
            bF.DCRCError = (word & 0x2000_0000) >> 29;
            bF.DStuffError = (word & 0x1000_0000) >> 28;
            bF.DFormError = (word & 0x800_0000) >> 27;
            bF.unimplemented2 = (word & 0x400_0000) >> 26;
            bF.DBit1Error = (word & 0x200_0000) >> 25;
            bF.DBit0Error = (word & 0x100_0000) >> 24;
            bF.TXBOError = (word & 0x80_0000) >> 23;
            bF.unimplemented1 = (word & 0x40_0000) >> 22;
            bF.NCRCError = (word & 0x20_0000) >> 21;
            bF.NStuffError = (word & 0x10_0000) >> 20;
            bF.NFormError = (word & 0x8_0000) >> 19;
            bF.NAckError = (word & 0x4_0000) >> 18;
            bF.NBit1Error = (word & 0x2_0000) >> 17;
            bF.NBit0Error = (word & 0x1_0000) >> 16;
            bF.ErrorFreeMsgCount = (word & 0xFFFF);
        }

        public void UpdateFromBytes()
        {
            word = BitConverter.ToUInt16(bytes, 0);

            bF.DLCMM = (word & 0x8000_0000) >> 31;
            bF.ESI = (word & 0x4000_0000) >> 30;
            bF.DCRCError = (word & 0x2000_0000) >> 29;
            bF.DStuffError = (word & 0x1000_0000) >> 28;
            bF.DFormError = (word & 0x800_0000) >> 27;
            bF.unimplemented2 = (word & 0x400_0000) >> 26;
            bF.DBit1Error = (word & 0x200_0000) >> 25;
            bF.DBit0Error = (word & 0x100_0000) >> 24;
            bF.TXBOError = (word & 0x80_0000) >> 23;
            bF.unimplemented1 = (word & 0x40_0000) >> 22;
            bF.NCRCError = (word & 0x20_0000) >> 21;
            bF.NStuffError = (word & 0x10_0000) >> 20;
            bF.NFormError = (word & 0x8_0000) >> 19;
            bF.NAckError = (word & 0x4_0000) >> 18;
            bF.NBit1Error = (word & 0x2_0000) >> 17;
            bF.NBit0Error = (word & 0x1_0000) >> 16;
            bF.ErrorFreeMsgCount = (word & 0xFFFF);

        }

        public void UpdateFromReg()
        {

            word |= bF.DLCMM << 31;
            word |= bF.ESI << 30;
            word |= bF.DCRCError << 29;
            word |= bF.DStuffError << 28;
            word |= bF.DFormError << 27;
            word |= bF.unimplemented2 << 26;
            word |= bF.DBit1Error << 25;
            word |= bF.DBit1Error << 24;
            word |= bF.TXBOError << 23;
            word |= bF.unimplemented1 << 22;
            word |= bF.NCRCError << 21;
            word |= bF.NStuffError << 20;
            word |= bF.NFormError << 19;
            word |= bF.NAckError << 18;
            word |= bF.NBit1Error << 17;
            word |= bF.NBit0Error << 16;
            word |= bF.ErrorFreeMsgCount;

            bytes = BitConverter.GetBytes(word);
        }
    };

    // *****************************************************************************
    //! Transmit Event FIFO Control Register

    public class REG_CiTEFCON
    {

        public class bFrame
        {
            public uint TEFNEIE;
            public uint TEFHFIE;
            public uint TEFFULIE;
            public uint TEFOVIE;
            public uint unimplemented1;
            public uint TimeStampEnable;
            public uint unimplemented2;
            public uint UINC;
            public uint unimplemented3;
            public uint FRESET;
            public uint unimplemented4;
            public uint FifoSize;
            public uint unimplemented5;
        }
        public bFrame bF = new bFrame();
        public uint word;
        public byte[] bytes = new byte[4];

        public void UpdateFromWord()
        {
            bytes = BitConverter.GetBytes(word);

            bF.unimplemented5 = (word & 0xE000_0000) >> 29;
            bF.FifoSize = (word & 0x1F00_0000) >> 24;
            bF.unimplemented4 = (word & 0xFF_F800) >> 11;
            bF.FRESET = (word & 0x400) >> 10;
            bF.unimplemented3 = (word & 0x200) >> 9;
            bF.UINC = (word & 0x100) >> 8;
            bF.unimplemented2 = (word & 0xC0) >> 6;
            bF.TimeStampEnable = (word & 0x20) >> 5;
            bF.unimplemented2 = (word & 0x10) >> 4;
            bF.TEFOVIE = (word & 0x8) >> 3;
            bF.TEFFULIE = (word & 0x4) >> 2;
            bF.TEFHFIE = (word & 0x2) >> 1;
            bF.TEFNEIE = (word & 0x1);
        }

        public void UpdateFromBytes()
        {
            word = BitConverter.ToUInt16(bytes, 0);

            bF.unimplemented5 = (word & 0xE000_0000) >> 29;
            bF.FifoSize = (word & 0x1F00_0000) >> 24;
            bF.unimplemented4 = (word & 0xFF_F800) >> 11;
            bF.FRESET = (word & 0x400) >> 10;
            bF.unimplemented3 = (word & 0x200) >> 9;
            bF.UINC = (word & 0x100) >> 8;
            bF.unimplemented2 = (word & 0xC0) >> 6;
            bF.TimeStampEnable = (word & 0x20) >> 5;
            bF.unimplemented2 = (word & 0x10) >> 4;
            bF.TEFOVIE = (word & 0x8) >> 3;
            bF.TEFFULIE = (word & 0x4) >> 2;
            bF.TEFHFIE = (word & 0x2) >> 1;
            bF.TEFNEIE = (word & 0x1);

        }

        public void UpdateFromReg()
        {
            word |= bF.unimplemented5 << 29;
            word |= bF.FifoSize << 24;
            word |= bF.unimplemented4 << 11;
            word |= bF.FRESET << 10;
            word |= bF.unimplemented3 << 9;
            word |= bF.UINC << 8;
            word |= bF.unimplemented2 << 6;
            word |= bF.TimeStampEnable << 5;
            word |= bF.unimplemented2 << 4;
            word |= bF.TEFOVIE << 3;
            word |= bF.TEFFULIE << 2;
            word |= bF.TEFHFIE << 1;
            word |= bF.TEFNEIE;

            bytes = BitConverter.GetBytes(word);
        }
    };

    // *****************************************************************************
    //! Transmit Event FIFO Status Register

    public class REG_CiTEFSTA
    {

        public class bFrame
        {
            public uint TEFNotEmptyIF;
            public uint TEFHalfFullIF;
            public uint TEFFullIF;
            public uint TEFOVIF;
            public uint unimplemented1;
        }
        public bFrame bF = new bFrame();
        public uint word;
        public byte[] bytes = new byte[4];

        public void UpdateFromWord()
        {
            bytes = BitConverter.GetBytes(word);

            bF.unimplemented1 = (word & 0xFFFF_FFF0) >> 4;
            bF.TEFOVIF = (word & 0x8) >> 3;
            bF.TEFFullIF = (word & 0x4) >> 2;
            bF.TEFHalfFullIF = (word & 0x2) >> 1;
            bF.TEFNotEmptyIF = (word & 0x1);

        }

        public void UpdateFromBytes()
        {
            word = BitConverter.ToUInt16(bytes, 0);

            bF.unimplemented1 = (word & 0xFFFF_FFF0) >> 4;
            bF.TEFOVIF = (word & 0x8) >> 3;
            bF.TEFFullIF = (word & 0x4) >> 2;
            bF.TEFHalfFullIF = (word & 0x2) >> 1;
            bF.TEFNotEmptyIF = (word & 0x1);

        }

        public void UpdateFromReg()
        {
            word |= bF.unimplemented1 << 4;
            word |= bF.TEFOVIF << 3;
            word |= bF.TEFFullIF << 2;
            word |= bF.TEFHalfFullIF << 1;
            word |= bF.TEFNotEmptyIF;

            bytes = BitConverter.GetBytes(word);
        }
    };

    // *****************************************************************************
    //! Transmit Queue Control Register

    public class REG_CiTXQCON
    {

        public class txBFrame
        {
            public uint TxNotFullIE;
            public uint unimplemented1;
            public uint TxEmptyIE;
            public uint unimplemented2;
            public uint TxAttemptIE;
            public uint unimplemented3;
            public uint TxEnable;
            public uint UINC;
            public uint TxRequest;
            public uint FRESET;
            public uint unimplemented4;
            public uint TxPriority;
            public uint TxAttempts;
            public uint unimplemented5;
            public uint FifoSize;
            public uint PayLoadSize;
        }
        public txBFrame txBF = new txBFrame();
        public uint word;
        public byte[] bytes = new byte[4];

        public void UpdateFromWord()
        {
            bytes = BitConverter.GetBytes(word);

            txBF.PayLoadSize = (word & 0xE000_0000) >> 29;
            txBF.FifoSize = (word & 0x1F00_0000) >> 24;
            txBF.unimplemented5 = (word & 0x80_0000) >> 23;
            txBF.TxAttempts = (word & 0x60_0000) >> 21;
            txBF.TxPriority = (word & 0x1F_0000) >> 16;
            txBF.unimplemented4 = (word & 0xF800) >> 11;
            txBF.FRESET = (word & 0x400) >> 10;
            txBF.TxRequest = (word & 0x200) >> 9;
            txBF.UINC = (word & 0x100) >> 8;
            txBF.TxEnable = (word & 0x80) >> 7;
            txBF.unimplemented3 = (word & 0x60) >> 5;
            txBF.TxAttemptIE = (word & 0x10) >> 4;
            txBF.unimplemented2 = (word & 0x8) >> 3;
            txBF.TxEmptyIE = (word & 0x4) >> 2;
            txBF.unimplemented1 = (word & 0x2) >> 1;
            txBF.TxNotFullIE = (word & 0x01);
        }

        public void UpdateFromBytes()
        {
            word = BitConverter.ToUInt16(bytes, 0);

            txBF.PayLoadSize = (word & 0xE000_0000) >> 29;
            txBF.FifoSize = (word & 0x1F00_0000) >> 24;
            txBF.unimplemented5 = (word & 0x80_0000) >> 23;
            txBF.TxAttempts = (word & 0x60_0000) >> 21;
            txBF.TxPriority = (word & 0x1F_0000) >> 16;
            txBF.unimplemented4 = (word & 0xF800) >> 11;
            txBF.FRESET = (word & 0x400) >> 10;
            txBF.TxRequest = (word & 0x200) >> 9;
            txBF.UINC = (word & 0x100) >> 8;
            txBF.TxEnable = (word & 0x80) >> 7;
            txBF.unimplemented3 = (word & 0x60) >> 5;
            txBF.TxAttemptIE = (word & 0x10) >> 4;
            txBF.unimplemented2 = (word & 0x8) >> 3;
            txBF.TxEmptyIE = (word & 0x4) >> 2;
            txBF.unimplemented1 = (word & 0x2) >> 1;
            txBF.TxNotFullIE = (word & 0x01);


        }

        public void UpdateFromReg()
        {
            word |= txBF.PayLoadSize << 29;
            word |= txBF.FifoSize << 24;
            word |= txBF.unimplemented5 << 23;
            word |= txBF.TxAttempts << 21;
            word |= txBF.TxPriority << 16;
            word |= txBF.unimplemented4 << 11;
            word |= txBF.FRESET << 10;
            word |= txBF.TxRequest << 9;
            word |= txBF.UINC << 8;
            word |= txBF.TxEnable << 7;
            word |= txBF.unimplemented3 << 5;
            word |= txBF.TxAttemptIE << 4;
            word |= txBF.unimplemented2 << 3;
            word |= txBF.TxEmptyIE << 2;
            word |= txBF.unimplemented1 << 1;
            word |= txBF.TxNotFullIE;

            bytes = BitConverter.GetBytes(word);
        }
    };

    // *****************************************************************************
    //! Transmit Queue Status Register

    public class REG_CiTXQSTA
    {

        public class txBFrame
        {
            public uint TxNotFullIF;
            public uint unimplemented1;
            public uint TxEmptyIF;
            public uint unimplemented2;
            public uint TxAttemptIF;
            public uint TxError;
            public uint TxLostArbitration;
            public uint TxAborted;
            public uint FifoIndex;
            public uint unimplemented3;
        }
        public txBFrame txBF = new txBFrame();
        public uint word;
        public byte[] bytes = new byte[4];

        public void UpdateFromWord()
        {
            bytes = BitConverter.GetBytes(word);

            txBF.unimplemented3 = (word & 0xFFFF_E000) >> 13;
            txBF.FifoIndex = (word & 0x1F00) >> 8;
            txBF.TxAborted = (word & 0x80) >> 7;
            txBF.TxLostArbitration = (word & 0x40) >> 6;
            txBF.TxError = (word & 0x20) >> 5;
            txBF.TxAttemptIF = (word & 0x10) >> 4;
            txBF.unimplemented2 = (word & 0x8) >> 3;
            txBF.TxEmptyIF = (word & 0x4) >> 2;
            txBF.unimplemented1 = (word & 0x2) >> 1;
            txBF.TxNotFullIF = (word & 0x1);
        }

        public void UpdateFromBytes()
        {
            word = BitConverter.ToUInt16(bytes, 0);

            txBF.unimplemented3 = (word & 0xFFFF_E000) >> 13;
            txBF.FifoIndex = (word & 0x1F00) >> 8;
            txBF.TxAborted = (word & 0x80) >> 7;
            txBF.TxLostArbitration = (word & 0x40) >> 6;
            txBF.TxError = (word & 0x20) >> 5;
            txBF.TxAttemptIF = (word & 0x10) >> 4;
            txBF.unimplemented2 = (word & 0x8) >> 3;
            txBF.TxEmptyIF = (word & 0x4) >> 2;
            txBF.unimplemented1 = (word & 0x2) >> 1;
            txBF.TxNotFullIF = (word & 0x1);


        }

        public void UpdateFromReg()
        {
            word |= txBF.unimplemented3 << 13;
            word |= txBF.FifoIndex << 8;
            word |= txBF.TxAborted << 7;
            word |= txBF.TxLostArbitration << 6;
            word |= txBF.TxError << 5;
            word |= txBF.TxAttemptIF << 4;
            word |= txBF.unimplemented2 << 3;
            word |= txBF.TxEmptyIF << 2;
            word |= txBF.unimplemented1 << 1;
            word |= txBF.TxNotFullIF;

            bytes = BitConverter.GetBytes(word);
        }
    };

    // *****************************************************************************
    //! FIFO Control Register

    public class REG_CiFIFOCON
    {
        // Receive FIFO

        public class rxBFrame
        {
            public uint RxNotEmptyIE;
            public uint RxHalfFullIE;
            public uint RxFullIE;
            public uint RxOverFlowIE;
            public uint unimplemented1;
            public uint RxTimeStampEnable;
            public uint unimplemented2;
            public uint TxEnable;
            public uint UINC;
            public uint unimplemented3;
            public uint FRESET;
            public uint unimplemented4;
            public uint FifoSize;
            public uint PayLoadSize;
        }

        // Transmit FIFO

        public class txBFrame
        {
            public uint TxNotFullIE;
            public uint TxHalfFullIE;
            public uint TxEmptyIE;
            public uint unimplemented1;
            public uint TxAttemptIE;
            public uint unimplemented2;
            public uint RTREnable;
            public uint TxEnable;
            public uint UINC;
            public uint TxRequest;
            public uint FRESET;
            public uint unimplemented3;
            public uint TxPriority;
            public uint TxAttempts;
            public uint unimplemented4;
            public uint FifoSize;
            public uint PayLoadSize;
        }
        public rxBFrame rxBF = new rxBFrame();
        public txBFrame txBF = new txBFrame();
        public uint word;
        public byte[] bytes = new byte[4];

        public void UpdateFromWord()
        {
            bytes = BitConverter.GetBytes(word);

            txBF.PayLoadSize = (word & 0xE000_0000) >> 29;
            txBF.FifoSize = (word & 0x1F00_0000) >> 24;
            txBF.unimplemented4 = (word & 0x80_0000) >> 23;
            txBF.TxAttempts = (word & 0x60_0000) >> 21;
            txBF.TxPriority = (word & 0x1F_0000) >> 16;
            txBF.unimplemented3 = (word & 0xF800) >> 11;
            txBF.FRESET = (word & 0x400) >> 10;
            txBF.TxRequest = (word & 0x200) >> 9;
            txBF.UINC = (word & 0x100) >> 8;
            txBF.TxEnable = (word & 0x80) >> 7;
            txBF.RTREnable = (word & 0x40) >> 6;
            txBF.unimplemented2 = (word & 0x20) >> 5;
            txBF.TxAttemptIE = (word & 0x10) >> 4;
            txBF.unimplemented1 = (word & 0x8) >> 3;
            txBF.TxEmptyIE = (word & 0x4) >> 2;
            txBF.TxHalfFullIE = (word & 0x02) >> 1;
            txBF.TxNotFullIE = (word & 0x01);

            rxBF.PayLoadSize = (word & 0xE000_0000) >> 29;
            rxBF.FifoSize = (word & 0x1F00_0000) >> 24;
            rxBF.unimplemented4 = (word & 0xFF_F800) >> 11;
            rxBF.FRESET = (word & 0x400) >> 10;
            rxBF.unimplemented3 = (word & 0x200) >> 9;
            rxBF.UINC = (word & 0x100) >> 8;
            rxBF.TxEnable = (word & 0x80) >> 7;
            rxBF.unimplemented2 = (word & 0x40) >> 6;
            rxBF.RxTimeStampEnable = (word & 0x20) >> 5;
            rxBF.unimplemented1 = (word & 0x10) >> 4;
            rxBF.RxOverFlowIE = (word & 0x8) >> 3;
            rxBF.RxFullIE = (word & 0x4) >> 2;
            rxBF.RxHalfFullIE = (word & 0x2) >> 1;
            rxBF.RxNotEmptyIE = (word & 0x1);

        }

        public void UpdateFromBytes()
        {
            word = BitConverter.ToUInt16(bytes, 0);

            txBF.PayLoadSize = (word & 0xE000_0000) >> 29;
            txBF.FifoSize = (word & 0x1F00_0000) >> 24;
            txBF.unimplemented4 = (word & 0x80_0000) >> 23;
            txBF.TxAttempts = (word & 0x60_0000) >> 21;
            txBF.TxPriority = (word & 0x1F_0000) >> 16;
            txBF.unimplemented3 = (word & 0xF800) >> 11;
            txBF.FRESET = (word & 0x400) >> 10;
            txBF.TxRequest = (word & 0x200) >> 9;
            txBF.UINC = (word & 0x100) >> 8;
            txBF.TxEnable = (word & 0x80) >> 7;
            txBF.RTREnable = (word & 0x40) >> 6;
            txBF.unimplemented2 = (word & 0x20) >> 5;
            txBF.TxAttemptIE = (word & 0x10) >> 4;
            txBF.unimplemented1 = (word & 0x8) >> 3;
            txBF.TxEmptyIE = (word & 0x4) >> 2;
            txBF.TxHalfFullIE = (word & 0x02) >> 1;
            txBF.TxNotFullIE = (word & 0x01);

            rxBF.PayLoadSize = (word & 0xE000_0000) >> 29;
            rxBF.FifoSize = (word & 0x1F00_0000) >> 24;
            rxBF.unimplemented4 = (word & 0xFF_F800) >> 11;
            rxBF.FRESET = (word & 0x400) >> 10;
            rxBF.unimplemented3 = (word & 0x200) >> 9;
            rxBF.UINC = (word & 0x100) >> 8;
            rxBF.TxEnable = (word & 0x80) >> 7;
            rxBF.unimplemented2 = (word & 0x40) >> 6;
            rxBF.RxTimeStampEnable = (word & 0x20) >> 5;
            rxBF.unimplemented1 = (word & 0x10) >> 4;
            rxBF.RxOverFlowIE = (word & 0x8) >> 3;
            rxBF.RxFullIE = (word & 0x4) >> 2;
            rxBF.RxHalfFullIE = (word & 0x2) >> 1;
            rxBF.RxNotEmptyIE = (word & 0x1);

        }

        public void UpdateFromTxReg()
        {
            word |= txBF.PayLoadSize << 29;
            word |= txBF.FifoSize << 24;
            word |= txBF.unimplemented4 << 23;
            word |= txBF.TxAttempts << 21;
            word |= txBF.TxPriority << 16;
            word |= txBF.unimplemented3 << 11;
            word |= txBF.FRESET << 10;
            word |= txBF.TxRequest << 9;
            word |= txBF.UINC << 8;
            word |= txBF.TxEnable << 7;
            word |= txBF.RTREnable << 6;
            word |= txBF.unimplemented2 << 5;
            word |= txBF.TxAttemptIE << 4;
            word |= txBF.unimplemented1 << 3;
            word |= txBF.TxEmptyIE << 2;
            word |= txBF.TxHalfFullIE << 1;
            word |= txBF.TxNotFullIE;

            bytes = BitConverter.GetBytes(word);
        }

        public void UpdateFromRxReg()
        {
            word |= rxBF.PayLoadSize << 29;
            word |= rxBF.FifoSize >> 24;
            word |= rxBF.unimplemented4 << 11;
            word |= rxBF.FRESET << 10;
            word |= rxBF.unimplemented3 << 9;
            word |= rxBF.UINC << 8;
            word |= rxBF.TxEnable << 7;
            word |= rxBF.unimplemented2 << 6;
            word |= rxBF.RxTimeStampEnable << 5;
            word |= rxBF.unimplemented1 << 4;
            word |= rxBF.RxOverFlowIE << 3;
            word |= rxBF.RxFullIE << 2;
            word |= rxBF.RxHalfFullIE << 1;
            word |= rxBF.RxNotEmptyIE;

            bytes = BitConverter.GetBytes(word);
        }
    };

    // *****************************************************************************
    //! FIFO Status Register

    public class REG_CiFIFOSTA
    {
        // Receive FIFO

        public class rxBFrame
        {
            public uint RxNotEmptyIF;
            public uint RxHalfFullIF;
            public uint RxFullIF;
            public uint RxOverFlowIF;
            public uint unimplemented1;
            public uint FifoIndex;
            public uint unimplemented2;
        }

        // Transmit FIFO

        public class txBFrame
        {
            public uint TxNotFullIF;
            public uint TxHalfFullIF;
            public uint TxEmptyIF;
            public uint unimplemented1;
            public uint TxAttemptIF;
            public uint TxError;
            public uint TxLostArbitration;
            public uint TxAborted;
            public uint FifoIndex;
            public uint unimplemented2;
        }
        public rxBFrame rxBF = new rxBFrame();
        public txBFrame txBF = new txBFrame();
        public uint word;
        public byte[] bytes = new byte[4];

        public void UpdateFromWord()
        {
            bytes = BitConverter.GetBytes(word);

            txBF.unimplemented2 = (word & 0xFFFF_E000) >> 13;
            txBF.FifoIndex = (word & 0x1F00) >> 8;
            txBF.TxAborted = (word & 0x80) >> 7;
            txBF.TxLostArbitration = (word & 0x40) >> 6;
            txBF.TxError = (word & 0x20) >> 5;
            txBF.TxAttemptIF = (word & 0x10) >> 4;
            txBF.unimplemented1 = (word & 0x8) >> 3;
            txBF.TxEmptyIF = (word & 0x4) >> 2;
            txBF.TxHalfFullIF = (word & 0x2) >> 1;
            txBF.TxNotFullIF = (word & 0x1);

            rxBF.unimplemented2 = (word & 0xFFFF_E000) >> 13;
            rxBF.FifoIndex = (word & 0x1F00) >> 8;
            rxBF.unimplemented1 = (word & 0xF0) >> 4;
            rxBF.RxOverFlowIF = (word & 0x8) >> 3;
            rxBF.RxFullIF = (word & 0x4) >> 2;
            rxBF.RxHalfFullIF = (word & 0x2) >> 1;
            rxBF.RxNotEmptyIF = (word & 0x1);

        }

        public void UpdateFromBytes()
        {
            word = BitConverter.ToUInt16(bytes, 0);

            txBF.unimplemented2 = (word & 0xFFFF_E000) >> 13;
            txBF.FifoIndex = (word & 0x1F00) >> 8;
            txBF.TxAborted = (word & 0x80) >> 7;
            txBF.TxLostArbitration = (word & 0x40) >> 6;
            txBF.TxError = (word & 0x20) >> 5;
            txBF.TxAttemptIF = (word & 0x10) >> 4;
            txBF.unimplemented1 = (word & 0x8) >> 3;
            txBF.TxEmptyIF = (word & 0x4) >> 2;
            txBF.TxHalfFullIF = (word & 0x2) >> 1;
            txBF.TxNotFullIF = (word & 0x1);

            rxBF.unimplemented2 = (word & 0xFFFF_E000) >> 13;
            rxBF.FifoIndex = (word & 0x1F00) >> 8;
            rxBF.unimplemented1 = (word & 0xF0) >> 4;
            rxBF.RxOverFlowIF = (word & 0x8) >> 3;
            rxBF.RxFullIF = (word & 0x4) >> 2;
            rxBF.RxHalfFullIF = (word & 0x2) >> 1;
            rxBF.RxNotEmptyIF = (word & 0x1);

        }

        public void UpdateFromTxReg()
        {
            word |= txBF.unimplemented2 << 13;
            word |= txBF.FifoIndex << 8;
            word |= txBF.TxAborted << 7;
            word |= txBF.TxLostArbitration << 6;
            word |= txBF.TxError << 5;
            word |= txBF.TxAttemptIF << 4;
            word |= txBF.unimplemented1 << 3;
            word |= txBF.TxEmptyIF << 2;
            word |= txBF.TxHalfFullIF << 1;
            word |= txBF.TxNotFullIF;

            bytes = BitConverter.GetBytes(word);
        }

        public void UpdateFromRxReg()
        {
            word |= rxBF.unimplemented2 << 13;
            word |= rxBF.FifoIndex << 8;
            word |= rxBF.unimplemented1 << 4;
            word |= rxBF.RxOverFlowIF << 3;
            word |= rxBF.RxFullIF << 2;
            word |= rxBF.RxHalfFullIF << 1;
            word |= rxBF.RxNotEmptyIF;

            bytes = BitConverter.GetBytes(word);
        }

    };

    // *****************************************************************************
    //! FIFO User Address Register

    public class REG_CiFIFOUA
    {

        public class bFrame
        {
            public uint UserAddress;
            public uint unimplemented1;
        }
        public bFrame bF = new bFrame();
        public uint word;
        public byte[] bytes = new byte[4];

        public void UpdateFromWord()
        {
            bytes = BitConverter.GetBytes(word);

            bF.unimplemented1 = (word & 0xFFFF_F000) >> 12;
            bF.UserAddress = (word & 0xFFF);

        }

        public void UpdateFromBytes()
        {
            word = BitConverter.ToUInt16(bytes, 0);

            bF.unimplemented1 = (word & 0xFFFF_F000) >> 12;
            bF.UserAddress = (word & 0xFFF);

        }

        public void UpdateFromReg()
        {
            word |= bF.unimplemented1 << 12;
            word |= bF.UserAddress;

            bytes = BitConverter.GetBytes(word);
        }

    };

    // *****************************************************************************
    //! Filter Control Register

    public class REG_CiFLTCON_BYTE
    {

        public class bFrame
        {
            public uint BufferPointer;
            public uint unimplemented1;
            public uint Enable;
        }
        public bFrame bF = new bFrame();
        public byte single_byte;

        public void UpdateFromByte()
        {

            bF.Enable = (byte)((single_byte & 0x80) >> 7);
            bF.unimplemented1 = (byte)((single_byte & 0x60) >> 5);
            bF.BufferPointer = (byte)(single_byte & 0x1F);

        }

        public void UpdateFromReg()
        {
            single_byte |= (byte)(bF.Enable << 7);
            single_byte |= (byte)(bF.unimplemented1 << 5);
            single_byte |= (byte)(bF.BufferPointer);
        }
    };

    // *****************************************************************************
    //! Filter Object Register

    public class REG_CiFLTOBJ
    {
        public CAN_FILTEROBJ_ID bF = new CAN_FILTEROBJ_ID();
        public uint word;
        public byte[] bytes = new byte[4];

        public void UpdateFromWord()
        {
            bytes = BitConverter.GetBytes(word);

            bF.unimplemented1 = (word & 0x8000_0000) >> 31;
            bF.EXIDE = (word & 0x4000_0000) >> 30;
            bF.SID11 = (word & 0x2000_0000) >> 29;
            bF.EID = (word & 0x1FFF_F800) >> 11;
            bF.SID = (word & 0x7FF);
        }
        public void UpdateFromByte()
        {
            word = BitConverter.ToUInt32(bytes, 0);

            bF.unimplemented1 = (word & 0x8000_0000) >> 31;
            bF.EXIDE = (word & 0x4000_0000) >> 30;
            bF.SID11 = (word & 0x2000_0000) >> 29;
            bF.EID = (word & 0x1FFF_F800) >> 11;
            bF.SID = (word & 0x7FF);

        }

        public void UpdateFromReg()
        {
            word |= bF.unimplemented1 << 31;
            word |= bF.EXIDE << 30;
            word |= bF.SID11 << 29;
            word |= bF.EID << 11;
            word |= bF.SID;

            bytes = BitConverter.GetBytes(word);
        }
    };

    // *****************************************************************************
    //! Mask Object Register

    public class REG_CiMASK
    {
        public CAN_MASKOBJ_ID bF = new CAN_MASKOBJ_ID();
        public uint word;
        public byte[] bytes = new byte[4];

        public void UpdateFromWord()
        {
            bytes = BitConverter.GetBytes(word);

            bF.unimplemented1 = (word & 0x8000_0000) >> 31;
            bF.MIDE = (word & 0x4000_0000) >> 30;
            bF.MSID11 = (word & 0x2000_0000) >> 29;
            bF.MEID = (word & 0x1FFF_F800) >> 11;
            bF.MSID = (word & 0x7FF);

        }
        public void UpdateFromByte()
        {
            word = BitConverter.ToUInt32(bytes, 0);

            bF.unimplemented1 = (word & 0x8000_0000) >> 31;
            bF.MIDE = (word & 0x4000_0000) >> 30;
            bF.MSID11 = (word & 0x2000_0000) >> 29;
            bF.MEID = (word & 0x1FFF_F800) >> 11;
            bF.MSID = (word & 0x7FF);

        }

        public void UpdateFromReg()
        {

            word |= bF.unimplemented1 << 31;
            word |= bF.MIDE << 30;
            word |= bF.MSID11 << 29;
            word |= bF.MEID << 11;
            word |= bF.MSID;

            bytes = BitConverter.GetBytes(word);
        }
    };

    // *****************************************************************************
    // *****************************************************************************
    /* MCP25xxFD Specific */

    // *****************************************************************************
    //! Oscillator Control Register

    public class REG_OSC
    {

        public class bFrame
        {
            public uint PllEnable;
            public uint unimplemented1;
            public uint OscDisable;
            public uint LowPowerModeEnable;
            public uint SCLKDIV;
            public uint CLKODIV;
            public uint unimplemented3;
            public uint PllReady;
            public uint unimplemented4;
            public uint OscReady;
            public uint unimplemented5;
            public uint SclkReady;
            public uint unimplemented6;
        }
        public bFrame bF = new bFrame();
        public uint word;
        public byte[] bytes = new byte[4];

        public void UpdateFromWord()
        {
            bytes = BitConverter.GetBytes(word);

            bF.unimplemented6 = (word & 0xFFFF_F000) >> 13;
            bF.SclkReady = (word & 0x1000) >> 12;
            bF.unimplemented5 = (word & 0x800) >> 11;
            bF.OscReady = (word & 0x400) >> 10;
            bF.unimplemented4 = (word & 0x200) >> 9;
            bF.PllReady = (word & 0x100) >> 8;
            bF.unimplemented3 = (word & 0x80) >> 7;
            bF.CLKODIV = (word & 0x60) >> 5;
            bF.SCLKDIV = (word & 0x10) >> 4;
            bF.LowPowerModeEnable = (word & 0x8) >> 3;
            bF.OscDisable = (word & 0x4) >> 2;
            bF.unimplemented1 = (word & 0x2) >> 1;
            bF.PllEnable = (word & 0x1);

        }
        public void UpdateFromByte()
        {
            word = BitConverter.ToUInt32(bytes, 0);

            bF.unimplemented6 = (word & 0xFFFF_F000) >> 13;
            bF.SclkReady = (word & 0x1000) >> 12;
            bF.unimplemented5 = (word & 0x800) >> 11;
            bF.OscReady = (word & 0x400) >> 10;
            bF.unimplemented4 = (word & 0x200) >> 9;
            bF.PllReady = (word & 0x100) >> 8;
            bF.unimplemented3 = (word & 0x80) >> 7;
            bF.CLKODIV = (word & 0x60) >> 5;
            bF.SCLKDIV = (word & 0x10) >> 4;
            bF.LowPowerModeEnable = (word & 0x8) >> 3;
            bF.OscDisable = (word & 0x4) >> 2;
            bF.unimplemented1 = (word & 0x2) >> 1;
            bF.PllEnable = (word & 0x1);

        }

        public void UpdateFromReg()
        {
            word |= bF.unimplemented6 << 13;
            word |= bF.SclkReady << 12;
            word |= bF.unimplemented5 << 11;
            word |= bF.OscReady << 10;
            word |= bF.unimplemented4 << 9;
            word |= bF.PllReady << 8;
            word |= bF.unimplemented3 << 7;
            word |= bF.CLKODIV << 5;
            word |= bF.SCLKDIV << 4;
            word |= bF.LowPowerModeEnable << 3;
            word |= bF.OscDisable << 2;
            word |= bF.unimplemented1 << 1;
            word |= bF.PllEnable;

            bytes = BitConverter.GetBytes(word);
        }
    };

    // *****************************************************************************
    //! I/O Control Register

    public class REG_IOCON
    {

        public class bFrame
        {
            public uint TRIS0;
            public uint TRIS1;
            public uint unimplemented1;
            public uint XcrSTBYEnable;
            public uint unimplemented2;
            public uint LAT0;
            public uint LAT1;
            public uint unimplemented3;
            public uint HVDETSEL;
            public uint GPIO0;
            public uint GPIO1;
            public uint unimplemented4;
            public uint PinMode0;
            public uint PinMode1;
            public uint unimplemented5;
            public uint TXCANOpenDrain;
            public uint SOFOutputEnable;
            public uint INTPinOpenDrain;
            public uint unimplemented6;
        }
        public bFrame bF = new bFrame();
        public uint word;
        public byte[] bytes = new byte[4];

        public void UpdateFromWord()
        {
            bytes = BitConverter.GetBytes(word);

            bF.unimplemented6 = (word & 0x8000_0000) >> 31;
            bF.INTPinOpenDrain = (word & 0x4000_0000) >> 30;
            bF.SOFOutputEnable = (word & 0x2000_0000) >> 29;
            bF.TXCANOpenDrain = (word & 0x1000_0000) >> 28;
            bF.unimplemented5 = (word & 0xC00_0000) >> 26;
            bF.PinMode1 = (word & 0x200_0000) >> 25;
            bF.PinMode0 = (word & 0x100_0000) >> 24;
            bF.unimplemented4 = (word & 0xFC_0000) >> 18;
            bF.GPIO1 = (word & 0x2_0000) >> 17;
            bF.GPIO0 = (word & 0x1_0000) >> 16;
            bF.unimplemented3 = (word & 0xFC00) >> 10;
            bF.LAT1 = (word & 0x200) >> 9;
            bF.LAT0 = (word & 0x100) >> 8;
            bF.unimplemented2 = (word & 0x80) >> 7;
            bF.XcrSTBYEnable = (word & 0x40) >> 6;
            bF.unimplemented1 = (word & 0xFC) >> 2;
            bF.TRIS1 = (word & 0x2) >> 1;
            bF.TRIS0 = (word & 0x1);

        }
        public void UpdateFromByte()
        {
            word = BitConverter.ToUInt32(bytes, 0);

            bF.unimplemented6 = (word & 0x8000_0000) >> 31;
            bF.INTPinOpenDrain = (word & 0x4000_0000) >> 30;
            bF.SOFOutputEnable = (word & 0x2000_0000) >> 29;
            bF.TXCANOpenDrain = (word & 0x1000_0000) >> 28;
            bF.unimplemented5 = (word & 0xC00_0000) >> 26;
            bF.PinMode1 = (word & 0x200_0000) >> 25;
            bF.PinMode0 = (word & 0x100_0000) >> 24;
            bF.unimplemented4 = (word & 0xFC_0000) >> 18;
            bF.GPIO1 = (word & 0x2_0000) >> 17;
            bF.GPIO0 = (word & 0x1_0000) >> 16;
            bF.unimplemented3 = (word & 0xFC00) >> 10;
            bF.LAT1 = (word & 0x200) >> 9;
            bF.LAT0 = (word & 0x100) >> 8;
            bF.unimplemented2 = (word & 0x80) >> 7;
            bF.XcrSTBYEnable = (word & 0x40) >> 6;
            bF.unimplemented1 = (word & 0xFC) >> 2;
            bF.TRIS1 = (word & 0x2) >> 1;
            bF.TRIS0 = (word & 0x1);

        }

        public void UpdateFromReg()
        {
            word |= bF.unimplemented6 << 31;
            word |= bF.INTPinOpenDrain << 30;
            word |= bF.SOFOutputEnable << 29;
            word |= bF.TXCANOpenDrain << 28;
            word |= bF.unimplemented5 << 26;
            word |= bF.PinMode1 << 25;
            word |= bF.PinMode0 << 24;
            word |= bF.unimplemented4 << 18;
            word |= bF.GPIO0 << 17;
            word |= bF.GPIO0 << 16;
            word |= bF.unimplemented3 << 10;
            word |= bF.LAT1 << 9;
            word |= bF.LAT0 << 8;
            word |= bF.unimplemented2 << 7;
            word |= bF.XcrSTBYEnable << 6;
            word |= bF.unimplemented1 << 2;
            word |= bF.TRIS1 << 1;
            word |= bF.TRIS0;

            bytes = BitConverter.GetBytes(word);
        }
    };

    // *****************************************************************************
    //! CRC Regsiter

    public class REG_CRC
    {

        public class bFrame
        {
            public uint CRC;
            public uint CRCERRIF;
            public uint FERRIF;
            public uint unimplemented1;
            public uint CRCERRIE;
            public uint FERRIE;
            public uint unimplemented2;
        }
        public bFrame bF = new bFrame();
        public uint word;
        public byte[] bytes = new byte[4];

        public void UpdateFromWord()
        {
            bytes = BitConverter.GetBytes(word);

            bF.unimplemented2 = (word & 0xFC00_0000) >> 26;
            bF.FERRIF = (word & 0x200_0000) >> 25;
            bF.CRCERRIE = (word & 0x100_0000) >> 24;
            bF.unimplemented1 = (word & 0xFC_0000) >> 18;
            bF.FERRIF = (word & 0x2_0000) >> 17;
            bF.CRCERRIF = (word & 0x1_0000) >> 16;
            bF.CRC = (word & 0xFFFF);
        }
        public void UpdateFromByte()
        {
            word = BitConverter.ToUInt32(bytes, 0);

            bF.unimplemented2 = (word & 0xFC00_0000) >> 26;
            bF.FERRIF = (word & 0x200_0000) >> 25;
            bF.CRCERRIE = (word & 0x100_0000) >> 24;
            bF.unimplemented1 = (word & 0xFC_0000) >> 18;
            bF.FERRIF = (word & 0x2_0000) >> 17;
            bF.CRCERRIF = (word & 0x1_0000) >> 16;
            bF.CRC = (word & 0xFFFF);
        }

        public void UpdateFromReg()
        {
            word = bF.unimplemented2 << 26;
            word |= bF.FERRIF << 25;
            word |= bF.CRCERRIE << 24;
            word |= bF.unimplemented1 << 18;
            word |= bF.FERRIF << 17;
            word |= bF.CRCERRIF;

            bytes = BitConverter.GetBytes(word);
        }
    };

    // *****************************************************************************
    //! ECC Control Register

    public class REG_ECCCON
    {

        public class bFrame
        {
            public uint EccEn;
            public uint SECIE;
            public uint DEDIE;
            public uint unimplemented1;
            public uint Parity;
            public uint unimplemented2;
        }
        public bFrame bF = new bFrame();
        public uint word;
        public byte[] bytes = new byte[4];

        public void UpdateFromWord()
        {
            bytes = BitConverter.GetBytes(word);

            bF.unimplemented2 = (word & 0xFFFF_8000) >> 15;
            bF.Parity = (word & 0x7F00) >> 8;
            bF.unimplemented1 = (word & 0xF8) >> 3;
            bF.DEDIE = (word & 0x4) >> 2;
            bF.SECIE = (word & 0x2) >> 1;
            bF.EccEn = (word & 0x1);
        }
        public void UpdateFromByte()
        {
            word = BitConverter.ToUInt32(bytes, 0);

            bF.unimplemented2 = (word & 0xFFFF_8000) >> 15;
            bF.Parity = (word & 0x7F00) >> 8;
            bF.unimplemented1 = (word & 0xF8) >> 3;
            bF.DEDIE = (word & 0x4) >> 2;
            bF.SECIE = (word & 0x2) >> 1;
            bF.EccEn = (word & 0x1);
        }


        public void UpdateFromReg()
        {
            word |= bF.unimplemented2 << 15;
            word |= bF.Parity << 8;
            word |= bF.unimplemented1 << 3;
            word |= bF.DEDIE << 2;
            word |= bF.SECIE << 1;
            word |= bF.EccEn;

            bytes = BitConverter.GetBytes(word);
        }
    }

    // *****************************************************************************
    //! ECC Status Register

    public class REG_ECCSTA
    {

        public class bFrame
        {
            public uint unimplemented1;
            public uint SECIF;
            public uint DEDIF;
            public uint unimplemented2;
            public uint ErrorAddress;
            public uint unimplemented3;
        }
        public bFrame bF = new bFrame();
        public uint word;
        public byte[] bytes = new byte[4];

        public void UpdateFromWord()
        {
            bytes = BitConverter.GetBytes(word);

            bF.unimplemented3 = (word & 0xF000_0000) >> 28;
            bF.ErrorAddress = (word & 0xFFF_0000) >> 16;
            bF.unimplemented2 = (word & 0xFFF8) >> 3;
            bF.DEDIF = (word & 0x4) >> 2;
            bF.SECIF = (word & 0x2) >> 1;
            bF.unimplemented1 = (word & 0x1);
        }
        public void UpdateFromByte()
        {
            word = BitConverter.ToUInt32(bytes, 0);

            bF.unimplemented3 = (word & 0xF000_0000) >> 28;
            bF.ErrorAddress = (word & 0xFFF_0000) >> 16;
            bF.unimplemented2 = (word & 0xFFF8) >> 3;
            bF.DEDIF = (word & 0x4) >> 2;
            bF.SECIF = (word & 0x2) >> 1;
            bF.unimplemented1 = (word & 0x1);
        }


        public void UpdateFromReg()
        {
            word |= bF.unimplemented3 << 28;
            word |= bF.ErrorAddress << 16;
            word |= bF.unimplemented2 << 3;
            word |= bF.DEDIF << 2;
            word |= bF.SECIF << 1;
            word |= bF.unimplemented1;


            bytes = BitConverter.GetBytes(word);
        }
    };

    // *****************************************************************************
    //! DEVID Register

    public class REG_DEVID
    {

        public class bFrame
        {
            public uint REV;
            public uint DID;
            public uint unimplemented;
        }
        public bFrame bF = new bFrame();
        public uint word;
        public byte[] bytes = new byte[4];

        public void UpdateFromWord()
        {
            bytes = BitConverter.GetBytes(word);

            bF.unimplemented = (word & 0xFFFF_FF00) >> 8;
            bF.DID = (word & 0xF0) >> 4;
            bF.REV = (word & 0xF);
        }
        public void UpdateFromByte()
        {
            word = BitConverter.ToUInt32(bytes, 0);

            bF.unimplemented = (word & 0xFFFF_FF00) >> 8;
            bF.DID = (word & 0xF0) >> 4;
            bF.REV = (word & 0xF);

        }


        public void UpdateFromReg()
        {

            word |= bF.unimplemented << 8;
            word |= bF.DID << 4;
            word |= bF.REV;

            bytes = BitConverter.GetBytes(word);
        }
    };
}
