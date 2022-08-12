using System;
using System.Collections;
using System.Reflection;
using System.Text;
using System.Threading;
using GHIElectronics.TinyCLR.Devices.Spi;

namespace MCP2518
{
    public abstract class MCPCanInterface
    {
        public const int CAN_OK = 0;
        public const int CAN_FAILINIT = 1;
        public const int CAN_FAILTX = 2;
        public const int CAN_MSGAVAIL = 3;
        public const int CAN_NOMSG = 4;
        public const int CAN_CTRLERROR = 5;
        public const int CAN_GETTXBFTIMEOUT = 6;
        public const int CAN_SETTXBFTIMEOUT = 7;
        public const int CAN_SENDMSGTIMEOUT = 8;
        public const int CAN_FAIL = 0xFF;

        // *****************************************************************************
        // *****************************************************************************
        /* SPI Instruction Set */

        public static uint cINSTRUCTION_RESET = 0x00;
        public static uint cINSTRUCTION_READ = 0x03;
        public static uint cINSTRUCTION_READ_CRC = 0x0B;
        public static uint cINSTRUCTION_WRITE = 0x02;
        public static uint cINSTRUCTION_WRITE_CRC = 0x0A;
        public static uint cINSTRUCTION_WRITE_SAFE = 0x0C;

        public enum MCP_CLOCK
        {
            MCP_NO_MHz,
            /* apply to MCP2515 */
            MCP_16MHz,
            MCP_8MHz,
            /* apply to MCP2518FD */
            MCP2518FD_40MHz = MCP_16MHz /* To compatible MCP2515 shield */,
            MCP2518FD_20MHz,
            MCP2518FD_10MHz,
        }

        public enum MCP_BITTIME_SETUP
        {
            CAN_NOBPS,
            CAN20_5KBPS,
            CAN20_10KBPS,
            CAN20_20KBPS,
            CAN20_25KBPS,
            CAN20_31K25BPS,
            CAN20_33KBPS,
            CAN20_40KBPS,
            CAN20_50KBPS,
            CAN20_80KBPS,
            CAN20_83K3BPS,
            CAN20_95KBPS,
            CAN20_100KBPS,
            CAN20_125KBPS,
            CAN20_200KBPS,
            CAN20_250KBPS,
            CAN20_500KBPS,
            CAN20_666KBPS,
            CAN20_800KBPS,
            CAN20_1000KBPS
        }

        //! CAN Data Length Code

        public enum CAN_DLC
        {
            CAN_DLC_0,
            CAN_DLC_1,
            CAN_DLC_2,
            CAN_DLC_3,
            CAN_DLC_4,
            CAN_DLC_5,
            CAN_DLC_6,
            CAN_DLC_7,
            CAN_DLC_8,
            CAN_DLC_12,
            CAN_DLC_16,
            CAN_DLC_20,
            CAN_DLC_24,
            CAN_DLC_32,
            CAN_DLC_48,
            CAN_DLC_64
        };

        //! CAN Operation Modes

        public enum CAN_OPERATION_MODE
        {
            CAN_NORMAL_MODE = 0x00,
            CAN_SLEEP_MODE = 0x01,
            CAN_INTERNAL_LOOPBACK_MODE = 0x02,
            CAN_LISTEN_ONLY_MODE = 0x03,
            CAN_CONFIGURATION_MODE = 0x04,
            CAN_EXTERNAL_LOOPBACK_MODE = 0x05,
            CAN_CLASSIC_MODE = 0x06,
            CAN_RESTRICTED_MODE = 0x07,
            CAN_INVALID_MODE = 0xFF
        };

        protected byte ext_flag;
        protected ulong can_id;
        protected byte rtr;
        protected SpiDevice spi;
        protected byte mcpMode;

        public abstract void EnableTxInterrupt(bool enable = true);
        public abstract void ReserveTxBuffers(byte nTxBuf = 0);
        public abstract byte GetLastTxBuffer();
        /*
        * speedset be in MCP_BITTIME_SETUP
        * clockset be in MCP_CLOCK_T
        */
        public abstract byte Begin(MCP_BITTIME_SETUP speedSet, MCP_CLOCK clockSet);
        public abstract void SetSleepWakeup(bool enable);
        public abstract byte Sleep();
        public abstract byte Wake();
        public abstract byte SetMode(CAN_OPERATION_MODE opMode);

        public abstract byte CheckError(out byte err);
        
        /* ---- receiving ---- */
        public abstract byte CheckReceive();
        public abstract byte ReadMessageBufID(out ulong id, out byte ext, out byte trt, out byte len, out byte[] buf);

        /* wrapper */
        public abstract byte ReadMessageBufID(out ulong ID, out byte len, byte[] buf);
        public abstract byte ReadMessageBuf(out byte len, byte[] buf);

        /* could be called after a successful readMsgBufID() */
        public ulong GetCanId()
        {
            return can_id;
        }

        public bool IsRemoteFrame()
        {
            return rtr > 0;
        }

        public bool IsExtendedFrame()
        {
            return ext_flag > 0;
        }

        /* ---- sending ---- */
        public abstract byte TrySendMsgBuf(ulong id, byte ext, byte rtr, byte len, byte[] buf, byte iTxBuf = 0xff);  // as sendMsgBuf, but does not have any wait for free buffer
        public abstract byte SendMsgBuf(byte status, ulong id, byte ext, byte rtr, byte len, byte[] buf); // send message buf by using parsed buffer status
        public abstract byte SendMsgBuf(ulong id, byte ext, byte rtr, byte len, byte[] buf, bool waitSent = true); // send message with wait

        public abstract void ClearBufferTransmitIfFlags(byte flags = 0);
        public abstract byte ReadRxTxStatus();
        public abstract byte CheckClearRxStatus(out byte status);
        public abstract byte CheckClearTxStatus(out byte status);
        public abstract bool McpPinMode(byte pin, byte mode);
        public abstract bool McpDigitalWrite(byte pin, byte mode);
        public abstract byte McpDigitalRead(byte pin);

        /* CANFD Auxiliary helper */
        protected class CANFD
        {
            public static byte Dlc2len(byte dlc)
            {
                if ((CAN_DLC)dlc <= CAN_DLC.CAN_DLC_8)
                    return dlc;
                switch ((CAN_DLC)dlc)
                {
                    case CAN_DLC.CAN_DLC_12: return 12;
                    case CAN_DLC.CAN_DLC_16: return 16;
                    case CAN_DLC.CAN_DLC_20: return 20;
                    case CAN_DLC.CAN_DLC_24: return 24;
                    case CAN_DLC.CAN_DLC_32: return 32;
                    case CAN_DLC.CAN_DLC_48: return 48;
                    default:
                    case CAN_DLC.CAN_DLC_64: return 64;
                }
            }

            public static CAN_DLC Len2dlc(byte len)
            {
                if (len <= (byte)CAN_DLC.CAN_DLC_8)
                    return (CAN_DLC)len;
                else if (len <= 12) return CAN_DLC.CAN_DLC_12;
                else if (len <= 16) return CAN_DLC.CAN_DLC_16;
                else if (len <= 20) return CAN_DLC.CAN_DLC_20;
                else if (len <= 24) return CAN_DLC.CAN_DLC_24;
                else if (len <= 32) return CAN_DLC.CAN_DLC_32;
                else if (len <= 48) return CAN_DLC.CAN_DLC_48;
                return CAN_DLC.CAN_DLC_64;
            }
            public static uint BITRATE(uint arbitration, byte factor)
            {
                return ((uint)factor << 24) | (uint)(arbitration & 0xFFFFFUL);
            }
        };
    }
}
