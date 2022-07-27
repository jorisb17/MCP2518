using GHIElectronics.TinyCLR.Devices.Gpio;
using GHIElectronics.TinyCLR.Devices.Spi;
using System;
using System.Collections;
using System.Text;
using System.Threading;

namespace MCP2518
{
    internal class MCP2518
    {
        public bool __flgFDF = false;

        public const byte CAN_OK = 0;
        public const byte CAN_FAILINIT = 1;
        public const byte CAN_FAILTX = 2;
        public const byte CAN_MSGAVAIL = 3;
        public const byte CAN_NOMSG = 4;
        public const byte CAN_CTRLERROR = 5;
        public const byte CAN_GETTXBFTIMEOUT = 6;
        public const byte CAN_SENDMSGTIMEOUT = 7;
        public const byte CAN_FAIL = 0xff;
        uint mSysClock;   // PLL disabled, mSysClock = Oscillator Frequency
        uint mDesiredArbitrationBitRate; // desired ArbitrationBitRate
        byte mDataBitRateFactor; // multiplier between ArbitrationBitRate and DataBitrate
                                 //--- Data bit rate; if mDataBitRateFactor==1, theses properties are not used for configuring the MCP2517FD.
        byte mDataPhaseSegment1 = 0; // if mDataBitRateFactor > 1: 2...32, else equal to mArbitrationPhaseSegment1
        byte mDataPhaseSegment2 = 0; // if mDataBitRateFactor > 1: 1...16, else equal to mArbitrationPhaseSegment2
        byte mDataSJW = 0; // if mDataBitRateFactor > 1: 1...16, else equal to mArbitrationSJW
                           //--- Bit rate prescaler is common to arbitration and data bit rates
        ushort mBitRatePrescaler = 0; // 1...256
                                      //--- Arbitration bit rate
        ushort mArbitrationPhaseSegment1 = 0; // 2...256
        byte mArbitrationPhaseSegment2 = 0; // 1...128
        byte mArbitrationSJW = 0; // 1...128
        bool mArbitrationBitRateClosedToDesiredRate = false; // The above configuration is not correct
                                                             //--- Transmitter Delay Compensation Offset
        sbyte mTDCO = 0; // -64 ... +63

        byte nReservedTx;     // Count of tx buffers for reserved send
        CAN_OPERATION_MODE mcpMode = CAN_OPERATION_MODE.CAN_CLASSIC_MODE; // Current controller mode
        byte ext_flg; // identifier xxxID
                      // either extended (the 29 LSB) or standard (the 11 LSB)
        ulong can_id; // can id
        byte rtr;     // is remote frame
        byte SPICS;

        static CAN_CONFIG config = new CAN_CONFIG();

        // Receive objects
        static CAN_RX_FIFO_CONFIG rxConfig = new CAN_RX_FIFO_CONFIG();
        static REG_CiFLTOBJ fObj = new REG_CiFLTOBJ();
        static REG_CiMASK mObj = new REG_CiMASK();
        static CAN_RX_FIFO_EVENT rxFlags;
        public static CAN_RX_MSGOBJ rxObj = new CAN_RX_MSGOBJ();
        static byte[] rxd = new byte[MCP2518_Registers.MAX_DATA_BYTES];

        // Transmit objects
        static CAN_TX_FIFO_CONFIG txConfig = new CAN_TX_FIFO_CONFIG();
        static CAN_TX_FIFO_EVENT txFlags = new CAN_TX_FIFO_EVENT();
        public static CAN_TX_MSGOBJ txObj;
        static byte[] txd = new byte[MCP2518_Registers.MAX_DATA_BYTES];

        CAN_FIFO_CHANNEL APP_TX_FIFO = CAN_FIFO_CHANNEL.CAN_FIFO_CH2;
        CAN_FIFO_CHANNEL APP_RX_FIFO = CAN_FIFO_CHANNEL.CAN_FIFO_CH1;


        ushort CRCBASE = 0xFFFF;
        ushort CRCUPPER = 1;

        //! Reverse order of bits in byte
        static byte[] BitReverseTable256 = {
            0x00, 0x80, 0x40, 0xC0, 0x20, 0xA0, 0x60, 0xE0, 0x10, 0x90, 0x50, 0xD0,
            0x30, 0xB0, 0x70, 0xF0, 0x08, 0x88, 0x48, 0xC8, 0x28, 0xA8, 0x68, 0xE8,
            0x18, 0x98, 0x58, 0xD8, 0x38, 0xB8, 0x78, 0xF8, 0x04, 0x84, 0x44, 0xC4,
            0x24, 0xA4, 0x64, 0xE4, 0x14, 0x94, 0x54, 0xD4, 0x34, 0xB4, 0x74, 0xF4,
            0x0C, 0x8C, 0x4C, 0xCC, 0x2C, 0xAC, 0x6C, 0xEC, 0x1C, 0x9C, 0x5C, 0xDC,
            0x3C, 0xBC, 0x7C, 0xFC, 0x02, 0x82, 0x42, 0xC2, 0x22, 0xA2, 0x62, 0xE2,
            0x12, 0x92, 0x52, 0xD2, 0x32, 0xB2, 0x72, 0xF2, 0x0A, 0x8A, 0x4A, 0xCA,
            0x2A, 0xAA, 0x6A, 0xEA, 0x1A, 0x9A, 0x5A, 0xDA, 0x3A, 0xBA, 0x7A, 0xFA,
            0x06, 0x86, 0x46, 0xC6, 0x26, 0xA6, 0x66, 0xE6, 0x16, 0x96, 0x56, 0xD6,
            0x36, 0xB6, 0x76, 0xF6, 0x0E, 0x8E, 0x4E, 0xCE, 0x2E, 0xAE, 0x6E, 0xEE,
            0x1E, 0x9E, 0x5E, 0xDE, 0x3E, 0xBE, 0x7E, 0xFE, 0x01, 0x81, 0x41, 0xC1,
            0x21, 0xA1, 0x61, 0xE1, 0x11, 0x91, 0x51, 0xD1, 0x31, 0xB1, 0x71, 0xF1,
            0x09, 0x89, 0x49, 0xC9, 0x29, 0xA9, 0x69, 0xE9, 0x19, 0x99, 0x59, 0xD9,
            0x39, 0xB9, 0x79, 0xF9, 0x05, 0x85, 0x45, 0xC5, 0x25, 0xA5, 0x65, 0xE5,
            0x15, 0x95, 0x55, 0xD5, 0x35, 0xB5, 0x75, 0xF5, 0x0D, 0x8D, 0x4D, 0xCD,
            0x2D, 0xAD, 0x6D, 0xED, 0x1D, 0x9D, 0x5D, 0xDD, 0x3D, 0xBD, 0x7D, 0xFD,
            0x03, 0x83, 0x43, 0xC3, 0x23, 0xA3, 0x63, 0xE3, 0x13, 0x93, 0x53, 0xD3,
            0x33, 0xB3, 0x73, 0xF3, 0x0B, 0x8B, 0x4B, 0xCB, 0x2B, 0xAB, 0x6B, 0xEB,
            0x1B, 0x9B, 0x5B, 0xDB, 0x3B, 0xBB, 0x7B, 0xFB, 0x07, 0x87, 0x47, 0xC7,
            0x27, 0xA7, 0x67, 0xE7, 0x17, 0x97, 0x57, 0xD7, 0x37, 0xB7, 0x77, 0xF7,
            0x0F, 0x8F, 0x4F, 0xCF, 0x2F, 0xAF, 0x6F, 0xEF, 0x1F, 0x9F, 0x5F, 0xDF,
        0x3F, 0xBF, 0x7F, 0xFF};

        //! Look-up table for CRC calculation
        static ushort[] crc16_table = {
            0x0000, 0x8005, 0x800F, 0x000A, 0x801B, 0x001E, 0x0014, 0x8011, 0x8033,
            0x0036, 0x003C, 0x8039, 0x0028, 0x802D, 0x8027, 0x0022, 0x8063, 0x0066,
            0x006C, 0x8069, 0x0078, 0x807D, 0x8077, 0x0072, 0x0050, 0x8055, 0x805F,
            0x005A, 0x804B, 0x004E, 0x0044, 0x8041, 0x80C3, 0x00C6, 0x00CC, 0x80C9,
            0x00D8, 0x80DD, 0x80D7, 0x00D2, 0x00F0, 0x80F5, 0x80FF, 0x00FA, 0x80EB,
            0x00EE, 0x00E4, 0x80E1, 0x00A0, 0x80A5, 0x80AF, 0x00AA, 0x80BB, 0x00BE,
            0x00B4, 0x80B1, 0x8093, 0x0096, 0x009C, 0x8099, 0x0088, 0x808D, 0x8087,
            0x0082, 0x8183, 0x0186, 0x018C, 0x8189, 0x0198, 0x819D, 0x8197, 0x0192,
            0x01B0, 0x81B5, 0x81BF, 0x01BA, 0x81AB, 0x01AE, 0x01A4, 0x81A1, 0x01E0,
            0x81E5, 0x81EF, 0x01EA, 0x81FB, 0x01FE, 0x01F4, 0x81F1, 0x81D3, 0x01D6,
            0x01DC, 0x81D9, 0x01C8, 0x81CD, 0x81C7, 0x01C2, 0x0140, 0x8145, 0x814F,
            0x014A, 0x815B, 0x015E, 0x0154, 0x8151, 0x8173, 0x0176, 0x017C, 0x8179,
            0x0168, 0x816D, 0x8167, 0x0162, 0x8123, 0x0126, 0x012C, 0x8129, 0x0138,
            0x813D, 0x8137, 0x0132, 0x0110, 0x8115, 0x811F, 0x011A, 0x810B, 0x010E,
            0x0104, 0x8101, 0x8303, 0x0306, 0x030C, 0x8309, 0x0318, 0x831D, 0x8317,
            0x0312, 0x0330, 0x8335, 0x833F, 0x033A, 0x832B, 0x032E, 0x0324, 0x8321,
            0x0360, 0x8365, 0x836F, 0x036A, 0x837B, 0x037E, 0x0374, 0x8371, 0x8353,
            0x0356, 0x035C, 0x8359, 0x0348, 0x834D, 0x8347, 0x0342, 0x03C0, 0x83C5,
            0x83CF, 0x03CA, 0x83DB, 0x03DE, 0x03D4, 0x83D1, 0x83F3, 0x03F6, 0x03FC,
            0x83F9, 0x03E8, 0x83ED, 0x83E7, 0x03E2, 0x83A3, 0x03A6, 0x03AC, 0x83A9,
            0x03B8, 0x83BD, 0x83B7, 0x03B2, 0x0390, 0x8395, 0x839F, 0x039A, 0x838B,
            0x038E, 0x0384, 0x8381, 0x0280, 0x8285, 0x828F, 0x028A, 0x829B, 0x029E,
            0x0294, 0x8291, 0x82B3, 0x02B6, 0x02BC, 0x82B9, 0x02A8, 0x82AD, 0x82A7,
            0x02A2, 0x82E3, 0x02E6, 0x02EC, 0x82E9, 0x02F8, 0x82FD, 0x82F7, 0x02F2,
            0x02D0, 0x82D5, 0x82DF, 0x02DA, 0x82CB, 0x02CE, 0x02C4, 0x82C1, 0x8243,
            0x0246, 0x024C, 0x8249, 0x0258, 0x825D, 0x8257, 0x0252, 0x0270, 0x8275,
            0x827F, 0x027A, 0x826B, 0x026E, 0x0264, 0x8261, 0x0220, 0x8225, 0x822F,
            0x022A, 0x823B, 0x023E, 0x0234, 0x8231, 0x8213, 0x0216, 0x021C, 0x8219,
        0x0208, 0x820D, 0x8207, 0x0202};

        GpioPin cs;
        SpiConnectionSettings settings;
        SpiController controller;
        SpiDevice spi;

        uint MAX_TXQUEUE_ATTEMPTS = 50;

        public MCP2518(GpioPin cs, SpiConnectionSettings settings, SpiController controller)
        {
            this.cs = cs;
            this.settings = settings;
            this.controller = controller;

            spi = controller.GetDevice(settings);
        }

        // *****************************************************************************
        // *****************************************************************************
        // Section: Variables

        static int SPI_DEFAULT_BUFFER_LENGTH = 96;

        //! SPI Transmit buffer
        byte[] spiTransmitBuffer = new byte[SPI_DEFAULT_BUFFER_LENGTH + 2];

        //! SPI Receive buffer
        byte[] spiReceiveBuffer = new byte[SPI_DEFAULT_BUFFER_LENGTH];

        ushort CalculateCRC16(byte[] data, ushort size)
        {
            ushort init = CRCBASE;
            byte index;

            for (int i = 0; i < size; i++)
            {
                index = (byte)((init >> 8) ^ data[i]);
                init = (ushort)(((init << 8) ^ crc16_table[index]) & CRCBASE);
            }

            return init;
        }

        int Reset()
        {
            int spiTransferError = 0;
            spiTransmitBuffer[0] = (byte)(MCP2518_Registers.cINSTRUCTION_RESET << 4);
            spiTransmitBuffer[1] = 0;

            try
            {
                spi.Write(spiTransmitBuffer, 0, 2);
            }
            catch (Exception e)
            {
                spiTransferError = e.GetHashCode();
            }

            return spiTransferError;
        }

        /*********************************************************************************************************
** Function name:           begin
** Descriptions:            init can and set speed
*********************************************************************************************************/
        public byte Begin(uint speedset, CAN_SYSCLK_SPEED clockset = (CAN_SYSCLK_SPEED)MCP_CLOCK_T.MCP2518FD_20MHz)
        {
            /* compatible layer translation */
            speedset = Bittime_compat_to_mcp2518fd(speedset);
            if (speedset > (uint)MCP_BITTIME_SETUP.CAN20_1000KBPS) __flgFDF = true;
            byte res = Init(speedset, clockset);
            return res;
        }

        byte ReadByte(ushort address)
        {
            int spiTransferError = 0;
            byte[] spiTransmitBuffer = { (byte)((MCP2518_Registers.cINSTRUCTION_READ << 4) + ((address >> 8) & 0xF)), (byte)(address & 0xFF), 0 };

            try
            {
                spi.TransferFullDuplex(spiTransmitBuffer, spiReceiveBuffer);
            }
            catch (Exception e)
            {
                spiTransferError = e.GetHashCode(); ;
            }

            byte rxd = spiReceiveBuffer[2];

            return rxd;
        }

        int WriteByte(ushort address, byte txd)
        {
            int spiTransferError = 0;
            byte[] spiTransmitBuffer =
            {
                (byte)((MCP2518_Registers.cINSTRUCTION_WRITE << 4) + ((address >> 8) & 0xF)),
                (byte)(address & 0xFF),
                txd
            };

            try
            {
                spi.Write(spiTransmitBuffer);
            }
            catch (Exception e)
            {
                spiTransferError = e.GetHashCode();
            }

            return spiTransferError;


        }

        uint ReadWord(ushort address)
        {
            int spiTransferError = 0;
            byte[] spiTransmitBuffer =
            {
                (byte)((MCP2518_Registers.cINSTRUCTION_READ << 4) + ((address >> 8) & 0xF)),
                (byte)(address & 0xFF)
            };

            try
            {
                spi.TransferFullDuplex(spiTransmitBuffer, spiReceiveBuffer);
            }
            catch (Exception e)
            {
                spiTransferError = e.GetHashCode();
            }

            uint rxd = 0;
            uint x = 0;
            for (int i = 2; i < 6; i++)
            {
                x = spiReceiveBuffer[i];
                rxd += x << ((i - 2) * 8);
            }

            return rxd;
        }

        int WriteWord(ushort address, uint txd)
        {
            byte[] spiTransmitBuffer = new byte[6];
            int spiTransferError = 0;

            spiTransmitBuffer[0] = (byte)((MCP2518_Registers.cINSTRUCTION_WRITE << 4) + ((address >> 8) & 0xF));
            spiTransmitBuffer[1] = (byte)(address & 0xFF);

            for (int i = 0; i < 4; i++)
            {
                spiTransmitBuffer[i + 2] = (byte)((txd >> (i * 8)) & 0xFF);
            }

            try
            {
                spi.Write(spiTransmitBuffer);
            }
            catch (Exception e)
            {
                spiTransferError = e.GetHashCode();
            }

            return spiTransferError;
        }

        ushort ReadHalfWord(ushort address)
        {
            int spiTransferError = 0;
            byte[] spiTransmitBuffer =
            {
                (byte)((MCP2518_Registers.cINSTRUCTION_READ << 4) + ((address >> 8) & 0xF)),
                (byte)(address & 0xFF)
            };

            try
            {
                spi.TransferFullDuplex(spiTransmitBuffer, spiReceiveBuffer);
            }
            catch (Exception e)
            {
                spiTransferError = e.GetHashCode();
            }


            uint x = 0;
            ushort rxd = 0;
            for (int i = 2; i < 4; i++)
            {
                x = spiReceiveBuffer[i];
                rxd += (ushort)(x << ((i - 2) * 8));
            }

            return rxd;
        }

        int WriteHalfWord(ushort address, ushort txd)
        {
            int spiTransferError = 0;
            byte[] spiTransmitBuffer = new byte[4];

            spiTransmitBuffer[0] = (byte)((MCP2518_Registers.cINSTRUCTION_WRITE << 4) + ((address >> 8) & 0xF));
            spiTransmitBuffer[1] = (byte)(address & 0xFF);

            for (int i = 0; i < 2; i++)
            {
                spiTransmitBuffer[i + 2] = (byte)((txd >> (i * 8)) & 0xFF);
            }

            try
            {
                spi.Write(spiTransmitBuffer);
            }
            catch (Exception e)
            {
                spiTransferError = e.GetHashCode();
            }

            return spiTransferError;
        }

        int ReadByteArray(ushort address, byte[] rxd, ushort nBytes)
        {
            int spiTransferError = 0;
            ushort i;
            ushort spiTransferSize = (ushort)(nBytes + 2);

            byte[] spiTransmitBuffer = new byte[spiTransferSize];
            // Compose command
            spiTransmitBuffer[0] = (byte)((MCP2518_Registers.cINSTRUCTION_READ << 4) + ((address >> 8) & 0xF));
            spiTransmitBuffer[1] = (byte)(address & 0xFF);

            // Clear data
            for (i = 2; i < spiTransferSize; i++)
            {
                spiTransmitBuffer[i] = 0;
            }

            try
            {
                spi.TransferFullDuplex(spiTransmitBuffer, spiReceiveBuffer);
            }
            catch (Exception e)
            {
                spiTransferError = e.GetHashCode();
            }

            // Update data
            for (i = 0; i < nBytes; i++)
            {
                rxd[i] = spiReceiveBuffer[i + 2];
            }

            return spiTransferError;
        }

        int WriteByteArray(ushort address, byte[] txd, ushort nBytes)
        {
            int spiTransferError = 0;
            ushort i;
            ushort spiTransferSize = (ushort)(nBytes + 2);

            byte[] spiTransmitBuffer = new byte[spiTransferSize];

            // Compose command
            spiTransmitBuffer[0] = (byte)((MCP2518_Registers.cINSTRUCTION_WRITE << 4) + ((address >> 8) & 0xF));
            spiTransmitBuffer[1] = (byte)(address & 0xFF);

            // Add data
            for (i = 2; i < spiTransferSize; i++)
            {
                spiTransmitBuffer[i] = txd[i - 2];
            }

            try
            {
                spi.Write(spiTransmitBuffer);
            }
            catch (Exception e)
            {
                spiTransferError = e.GetHashCode();
            }

            return spiTransferError;
        }

        int WriteByteSafe(ushort address, byte txd)
        {
            int spiTransferError = 0;
            ushort crcResult = 0;
            byte[] spiTransmitBuffer = new byte[5];

            spiTransmitBuffer[0] = (byte)((MCP2518_Registers.cINSTRUCTION_WRITE_SAFE << 4) + ((address >> 8) & 0xF));
            spiTransmitBuffer[1] = ((byte)(address & 0xFF));
            spiTransmitBuffer[2] = txd;

            crcResult = CalculateCRC16(spiTransmitBuffer, 3);
            spiTransmitBuffer[3] = (byte)((crcResult >> 8) & 0xFF);
            spiTransmitBuffer[4] = (byte)(crcResult & 0xFF);

            try
            {
                spi.Write(spiTransmitBuffer);
            }
            catch (Exception e)
            {
                spiTransferError = e.GetHashCode();
            }

            return spiTransferError;
        }

        int WriteWordSafe(ushort address, uint txd)
        {
            int spiTransferError = 0;
            ushort crcResult = 0;
            byte[] spiTransmitBuffer = new byte[8];

            spiTransmitBuffer[0] = (byte)((MCP2518_Registers.cINSTRUCTION_WRITE_SAFE << 4) + ((address >> 8) & 0xF));
            spiTransmitBuffer[1] = (byte)(crcResult & 0xFF);

            for (int i = 0; i < 4; i++)
            {
                spiTransmitBuffer[i + 2] = (byte)((txd >> (i * 8)) & 0xFF);
            }

            crcResult = CalculateCRC16(spiTransmitBuffer, 6);
            spiTransmitBuffer[6] = (byte)((crcResult >> 8) & 0xFF);
            spiTransmitBuffer[7] = (byte)(crcResult & 0xFF);

            try
            {
                spi.Write(spiTransmitBuffer);
            }
            catch (Exception e)
            {
                spiTransferError = e.GetHashCode();
            }

            return spiTransferError;
        }


        ByteArrayCRC ReadByteArrayWithCRC(ushort address, ushort nBytes, bool fromRam)
        {
            byte i;
            ushort crcFromSpiSlave = 0;
            ushort crcAtController = 0;
            int spiTransferError = 0;
            ushort spiTransferSize = (ushort)(nBytes + 5); //first two bytes for sending command & address, third for size, last two bytes for CRC

            byte[] spiTransmitBuffer = new byte[spiTransferSize];
            // Compose command
            spiTransmitBuffer[0] = (byte)((MCP2518_Registers.cINSTRUCTION_READ_CRC << 4) + ((address >> 8) & 0xF));
            spiTransmitBuffer[1] = (byte)(address & 0xFF);

            if (fromRam)
            {
                spiTransmitBuffer[2] = (byte)(nBytes >> 2);
            }
            else
            {
                spiTransmitBuffer[2] = (byte)nBytes;
            }

            // Clear data
            for (i = 3; i < spiTransferSize; i++)
            {
                spiTransmitBuffer[i] = 0;
            }

            try
            {
                spi.TransferFullDuplex(spiTransmitBuffer, spiReceiveBuffer);
            }
            catch (Exception e)
            {
                spiTransferError = e.GetHashCode();
            }

            // Get CRC from controller
            crcFromSpiSlave = (ushort)((spiReceiveBuffer[spiTransferSize - 2] << 8) + (spiReceiveBuffer[spiTransferSize - 1]));

            // Use the receive buffer to calculate CRC
            // First three bytes need to be command
            spiReceiveBuffer[0] = spiTransmitBuffer[0];
            spiReceiveBuffer[1] = spiTransmitBuffer[1];
            spiReceiveBuffer[2] = spiTransmitBuffer[2];
            crcAtController = CalculateCRC16(spiReceiveBuffer, (ushort)(nBytes + 3));

            bool crcIsCorrect;

            // Compare CRC readings
            if (crcFromSpiSlave != crcAtController)
            {
                crcIsCorrect = true;
            }
            else
            {
                crcIsCorrect = false;
            }

            byte[] rxd = new byte[nBytes];
            // Update data
            for (i = 0; i < nBytes; i++)
            {
                rxd[i] = spiReceiveBuffer[i + 3];
            }

            ByteArrayCRC res = new ByteArrayCRC();
            res.crcIsCorrect = crcIsCorrect;
            res.rxd = rxd;

            return res;
        }
        int WriteByteArrayWithCRC(ushort address,
                byte[] txd, ushort nBytes, bool fromRam)
        {
            int spiTransferError = 0;
            ushort i;
            ushort crcResult = 0;
            ushort spiTransferSize = (ushort)(nBytes + 5);

            byte[] spiTransmitBuffer = new byte[spiTransferSize];

            // Compose command
            spiTransmitBuffer[0] = (byte)((MCP2518_Registers.cINSTRUCTION_WRITE_CRC << 4) + ((address >> 8) & 0xF));
            spiTransmitBuffer[1] = (byte)(address & 0xFF);
            if (fromRam)
            {
                spiTransmitBuffer[2] = (byte)(nBytes >> 2);
            }
            else
            {
                spiTransmitBuffer[2] = (byte)nBytes;
            }

            // Add data
            for (i = 0; i < nBytes; i++)
            {
                spiTransmitBuffer[i + 3] = txd[i];
            }

            // Add CRC
            crcResult = CalculateCRC16(spiTransmitBuffer, (ushort)(spiTransferSize - 2));
            spiTransmitBuffer[spiTransferSize - 2] = (byte)((crcResult >> 8) & 0xFF);
            spiTransmitBuffer[spiTransferSize - 1] = (byte)(crcResult & 0xFF);

            try
            {
                spi.Write(spiTransmitBuffer);
            }
            catch (Exception e)
            {
                spiTransferError = e.GetHashCode();
            }

            return spiTransferError;
        }

        int ReadWordArray(ushort address, uint[] rxd, ushort nWords)
        {
            int spiTransferError = 0;
            ushort i, j, n;
            REG_t w = new REG_t();
            w.bytes = new byte[4];
            w.UpdateFromBytes();

            ushort spiTransferSize = (ushort)(nWords * 4 + 2);

            byte[] spiTransmitBuffer = new byte[spiTransferSize];

            // Compose command
            spiTransmitBuffer[0] = (byte)((MCP2518_Registers.cINSTRUCTION_READ << 4) + ((address >> 8) & 0xF));
            spiTransmitBuffer[1] = (byte)(address & 0xFF);

            // Clear data
            for (i = 2; i < spiTransferSize; i++)
            {
                spiTransmitBuffer[i] = 0;
            }

            try
            {
                spi.TransferFullDuplex(spiTransmitBuffer, spiReceiveBuffer);
            }
            catch (Exception e)
            {
                spiTransferError = e.GetHashCode();
            }

            // Convert Byte array to Word array
            n = 2;
            for (i = 0; i < nWords; i++)
            {
                w.word = 0;
                for (j = 0; j < 4; j++, n++)
                {
                    w.bytes[j] = spiReceiveBuffer[n];
                }
                w.UpdateFromBytes();
                rxd[i] = w.word;
            }

            return spiTransferError;
        }

        int WriteWordArray(ushort address,
                uint[] txd, ushort nWords)
        {
            int spiTransferError = 0;
            ushort i, j, n;
            REG_t w = new REG_t();
            w.bytes = new byte[nWords * 4];
            w.UpdateFromBytes();

            ushort spiTransferSize = (ushort)(nWords * 4 + 2);

            byte[] spiTransmitBuffer = new byte[spiTransferSize];

            // Compose command
            spiTransmitBuffer[0] = (byte)((MCP2518_Registers.cINSTRUCTION_WRITE << 4) + ((address >> 8) & 0xF));
            spiTransmitBuffer[1] = (byte)(address & 0xFF);

            // Convert ByteArray to word array
            n = 2;
            for (i = 0; i < nWords; i++)
            {
                w.word = txd[i];
                w.UpdateFromWord();
                for (j = 0; j < 4; j++, n++)
                {
                    spiTransmitBuffer[n] = w.bytes[j];
                }
            }

            try
            {
                spi.Write(spiTransmitBuffer);
            }
            catch (Exception e)
            {
                spiTransferError = e.GetHashCode();
            }

            return spiTransferError;
        }

        int EccEnable()
        {
            byte d = 0;
            int spiTransferError = 0;

            // Read
            d = ReadByte(MCP2518_Registers.cREGADDR_ECCCON);
            // Modify
            d |= 0x01;

            // Write
            spiTransferError = WriteByte(MCP2518_Registers.cREGADDR_ECCCON, d);
            if (spiTransferError > 0)
            {
                return -2;
            }

            return spiTransferError;
        }

        int RamInit(byte d)
        {
            byte[] txd = new byte[SPI_DEFAULT_BUFFER_LENGTH];
            uint k;
            int spiTransferError = 0;

            // Prepare data
            for (k = 0; k < SPI_DEFAULT_BUFFER_LENGTH; k++)
            {
                txd[k] = d;
            }

            ushort a = MCP2518_Registers.cRAMADDR_START;

            for (k = 0; k < (MCP2518_Registers.cRAM_SIZE / SPI_DEFAULT_BUFFER_LENGTH); k++)
            {
                spiTransferError = WriteByteArray(a, txd, (ushort)SPI_DEFAULT_BUFFER_LENGTH);
                if (spiTransferError > 0)
                {
                    return -1;
                }

                a += (ushort)SPI_DEFAULT_BUFFER_LENGTH;
            }

            return spiTransferError;
        }

        void ConfigureObjectReset(CAN_CONFIG config)
        {
            REG_CiCON ciCon = new REG_CiCON();
            ciCon.word = MCP2518_Registers.canControlResetValues[MCP2518_Registers.cREGADDR_CiCON / 4];
            ciCon.UpdateFromWord();

            config.DNetFilterCount = ciCon.bF.DNetFilterCount;
            config.IsoCrcEnable = ciCon.bF.IsoCrcEnable;
            config.ProtocolExpectionEventDisable = ciCon.bF.ProtocolExceptionEventDisable;
            config.WakeUpFilterEnable = ciCon.bF.WakeUpFilterEnable;
            config.WakeUpFilterTime = ciCon.bF.WakeUpFilterTime;
            config.BitRateSwitchDisable = ciCon.bF.BitRateSwitchDisable;
            config.RestrictReTxAttempts = ciCon.bF.RestrictReTxAttempts;
            config.EsiInGatewayMode = ciCon.bF.EsiInGatewayMode;
            config.SystemErrorToListenOnly = ciCon.bF.SystemErrorToListenOnly;
            config.StoreInTEF = ciCon.bF.StoreInTEF;
            config.TXQEnable = ciCon.bF.TXQEnable;
            config.TxBandWidthSharing = ciCon.bF.TxBandWidthSharing;
        }

        void Configure(CAN_CONFIG config)
        {
            REG_CiCON ciCon = new REG_CiCON();

            ciCon.word = MCP2518_Registers.canControlResetValues[MCP2518_Registers.cREGADDR_CiCON / 4];
            ciCon.UpdateFromWord();

            ciCon.bF.DNetFilterCount = config.DNetFilterCount;
            ciCon.bF.IsoCrcEnable = config.IsoCrcEnable;
            ciCon.bF.ProtocolExceptionEventDisable = config.ProtocolExpectionEventDisable;
            ciCon.bF.WakeUpFilterEnable = config.WakeUpFilterEnable;
            ciCon.bF.WakeUpFilterTime = config.WakeUpFilterTime;
            ciCon.bF.BitRateSwitchDisable = config.BitRateSwitchDisable;
            ciCon.bF.RestrictReTxAttempts = config.RestrictReTxAttempts;
            ciCon.bF.EsiInGatewayMode = config.EsiInGatewayMode;
            ciCon.bF.SystemErrorToListenOnly = config.SystemErrorToListenOnly;
            ciCon.bF.StoreInTEF = config.StoreInTEF;
            ciCon.bF.TXQEnable = config.TXQEnable;
            ciCon.bF.TxBandWidthSharing = config.TxBandWidthSharing;

            ciCon.UpdateFromReg();

            WriteWord(MCP2518_Registers.cREGADDR_CiCON, ciCon.word);
        }

        int TransmitChannelConfigureObjectReset(CAN_TX_FIFO_CONFIG config)
        {
            REG_CiFIFOCON ciFifoCon = new REG_CiFIFOCON();
            ciFifoCon.word = MCP2518_Registers.canFifoResetValues[0];
            ciFifoCon.UpdateFromWord();

            config.RTREnable = ciFifoCon.txBF.RTREnable;
            config.TxPriority = ciFifoCon.txBF.TxPriority;
            config.TxAttempts = ciFifoCon.txBF.TxAttempts;
            config.FifoSize = ciFifoCon.txBF.FifoSize;
            config.PayLoadSize = ciFifoCon.txBF.PayLoadSize;

            return 0;
        }

        int TransmitChannelConfigure(CAN_FIFO_CHANNEL channel,
        CAN_TX_FIFO_CONFIG config)
        {
            int spiTransferError = 0;
            ushort a = 0;
            // Setup FIFO
            REG_CiFIFOCON ciFifoCon = new REG_CiFIFOCON();
            ciFifoCon.word = MCP2518_Registers.canFifoResetValues[0];
            ciFifoCon.UpdateFromWord();

            ciFifoCon.txBF.TxEnable = 1;
            ciFifoCon.txBF.FifoSize = config.FifoSize;
            ciFifoCon.txBF.PayLoadSize = config.PayLoadSize;
            ciFifoCon.txBF.TxAttempts = config.TxAttempts;
            ciFifoCon.txBF.TxPriority = config.TxPriority;
            ciFifoCon.txBF.RTREnable = config.RTREnable;
            ciFifoCon.UpdateFromTxReg();

            a = (ushort)(MCP2518_Registers.cREGADDR_CiFIFOCON + ((int)channel * MCP2518_Registers.CiFIFO_OFFSET));

            spiTransferError = WriteWord(a, ciFifoCon.word);
            if (spiTransferError != 0)
            {
                return -1;
            }

            return spiTransferError;

        }

        void ReceiveChannelConfigureObjectReset(CAN_RX_FIFO_CONFIG config)
        {
            REG_CiFIFOCON ciFifoCon = new REG_CiFIFOCON();
            ciFifoCon.word = MCP2518_Registers.canFifoResetValues[0];
            ciFifoCon.UpdateFromWord();

            config.FifoSize = ciFifoCon.rxBF.FifoSize;
            config.PayLoadSize = ciFifoCon.rxBF.PayLoadSize;
            config.RxTimeStampEnable = ciFifoCon.rxBF.RxTimeStampEnable;
        }

        int ReceiveChannelConfigure(CAN_FIFO_CHANNEL channel, CAN_RX_FIFO_CONFIG config)
        {
            int spiTransferError = 0;
            ushort a = 0;

            if (channel == (CAN_FIFO_CHANNEL)MCP2518_Registers.CAN_TXQUEUE_CH0)
            {
                return -1;
            }

            // Setup FIFO
            REG_CiFIFOCON ciFifoCon = new REG_CiFIFOCON();
            ciFifoCon.word = MCP2518_Registers.canFifoResetValues[0];
            ciFifoCon.UpdateFromWord();

            ciFifoCon.rxBF.TxEnable = 0;
            ciFifoCon.rxBF.FifoSize = config.FifoSize;
            ciFifoCon.rxBF.PayLoadSize = config.PayLoadSize;
            ciFifoCon.rxBF.RxTimeStampEnable = config.RxTimeStampEnable;
            ciFifoCon.UpdateFromRxReg();

            a = (ushort)(MCP2518_Registers.cREGADDR_CiFIFOCON + ((ushort)channel * MCP2518_Registers.CiFIFO_OFFSET));

            spiTransferError = WriteWord(a, ciFifoCon.word);
            if (spiTransferError != 0)
            {
                return -2;
            }

            return spiTransferError;
        }

        int FilterObjectConfigure(CAN_FILTER filter, CAN_FILTEROBJ_ID id)
        {
            int spiTransferError = 0;
            ushort a;
            REG_CiFLTOBJ fObj = new REG_CiFLTOBJ();

            // Setup
            fObj.word = 0;
            fObj.bF = id;
            fObj.UpdateFromReg();
            a = (ushort)(MCP2518_Registers.cREGADDR_CiFLTOBJ + ((ushort)filter * MCP2518_Registers.CiFILTER_OFFSET));

            spiTransferError = WriteWord(a, fObj.word);
            if (spiTransferError != 0)
            {
                return -1;
            }

            return spiTransferError;
        }

        int FilterMaskConfigure(CAN_FILTER filter, CAN_MASKOBJ_ID mask)
        {
            int spiTransferError = 0;
            ushort a;
            REG_CiMASK mObj = new REG_CiMASK();

            // Setup
            mObj.word = 0;
            mObj.bF = mask;
            mObj.UpdateFromReg();

            a = (ushort)(MCP2518_Registers.cREGADDR_CiMASK + ((ushort)filter * MCP2518_Registers.CiFILTER_OFFSET));
            spiTransferError = WriteWord(a, mObj.word);
            if (spiTransferError != 0)
            {
                return -1;
            }
            return spiTransferError;
        }

        int FilterToFifoLink(CAN_FILTER filter, CAN_FIFO_CHANNEL channel, bool enable)
        {
            int spiTransferError = 0;
            ushort a;
            REG_CiFLTCON_BYTE fCtrl = new REG_CiFLTCON_BYTE();

            // Enable
            fCtrl.bF.Enable = !enable ? (uint)1 : (uint)0;

            // Link
            fCtrl.bF.BufferPointer = (uint)channel;

            fCtrl.UpdateFromReg();
            a = (ushort)(MCP2518_Registers.cREGADDR_CiFLTCON + filter);         // 1D0

            spiTransferError = WriteByte(a, fCtrl.single_byte);
            if (spiTransferError != 0)
            {
                return -1;
            }

            return spiTransferError;
        }


        /*
         * bittime calculation code from
         *   https://github.com/pierremolinaro/acan2517FD
         *
         */

        const ushort MAX_BRP = 256;
        const ushort MAX_ARBITRATION_PHASE_SEGMENT_1 = 256;
        const byte MAX_ARBITRATION_PHASE_SEGMENT_2 = 128;
        const byte MAX_ARBITRATION_SJW = 128;
        const ushort MAX_DATA_PHASE_SEGMENT_1 = 32;
        const byte MAX_DATA_PHASE_SEGMENT_2 = 16;
        const byte MAX_DATA_SJW = 16;

        bool calcBittime(uint inDesiredArbitrationBitRate, uint inTolerancePPM = 1000)
        {
            if (mDataBitRateFactor <= 1)
            { // Single bit rate
                const uint maxTQCount = MAX_ARBITRATION_PHASE_SEGMENT_1 + MAX_ARBITRATION_PHASE_SEGMENT_2 + 1; // Setting for slowest bit rate
                uint BRP = MAX_BRP;
                uint smallestError = uint.MaxValue;
                uint bestBRP = 1; // Setting for highest bit rate
                uint bestTQCount = 4; // Setting for highest bit rate
                uint TQCount = mSysClock / inDesiredArbitrationBitRate / BRP;
                //--- Loop for finding best BRP and best TQCount
                while ((TQCount <= (MAX_ARBITRATION_PHASE_SEGMENT_1 + MAX_ARBITRATION_PHASE_SEGMENT_2 + 1)) && (BRP > 0))
                {
                    //--- Compute error using TQCount
                    if ((TQCount >= 4) && (TQCount <= maxTQCount))
                    {
                        uint error = mSysClock - inDesiredArbitrationBitRate * TQCount * BRP; // error is always >= 0
                        if (error <= smallestError)
                        {
                            smallestError = error;
                            bestBRP = BRP;
                            bestTQCount = TQCount;
                        }
                    }
                    //--- Compute error using TQCount+1
                    if ((TQCount >= 3) && (TQCount < maxTQCount))
                    {
                        uint error = inDesiredArbitrationBitRate * (TQCount + 1) * BRP - mSysClock; // error is always >= 0
                        if (error <= smallestError)
                        {
                            smallestError = error;
                            bestBRP = BRP;
                            bestTQCount = TQCount + 1;
                        }
                    }
                    //--- Continue with next value of BRP
                    BRP--;
                    TQCount = (BRP == 0) ? (maxTQCount + 1) : (mSysClock / inDesiredArbitrationBitRate / BRP);
                }
                //--- Compute PS2 (1 <= PS2 <= 128)
                uint PS2 = bestTQCount / 5; // For sampling point at 80%
                if (PS2 == 0)
                {
                    PS2 = 1;
                }
                else if (PS2 > MAX_ARBITRATION_PHASE_SEGMENT_2)
                {
                    PS2 = MAX_ARBITRATION_PHASE_SEGMENT_2;
                }
                //--- Compute PS1 (1 <= PS1 <= 256)
                uint PS1 = bestTQCount - PS2 - 1 /* Sync Seg */ ;
                if (PS1 > MAX_ARBITRATION_PHASE_SEGMENT_1)
                {
                    PS2 += PS1 - MAX_ARBITRATION_PHASE_SEGMENT_1;
                    PS1 = MAX_ARBITRATION_PHASE_SEGMENT_1;
                }
                //---
                mBitRatePrescaler = (ushort)bestBRP;
                mArbitrationPhaseSegment1 = (ushort)PS1;
                mArbitrationPhaseSegment2 = (byte)PS2;
                mArbitrationSJW = mArbitrationPhaseSegment2; // Always 1 <= SJW <= 128, and SJW <= mArbitrationPhaseSegment2
                                                             //--- Final check of the nominal configuration
                uint W = bestTQCount * inDesiredArbitrationBitRate * bestBRP;
                ulong diff = (mSysClock > W) ? (mSysClock - W) : (W - mSysClock);
                ulong ppm = (1000UL * 1000UL); // UL suffix is required for Arduino Uno
                mArbitrationBitRateClosedToDesiredRate = (diff * ppm) <= (((ulong)W) * inTolerancePPM);
            }
            else
            { // Dual bit rate, first compute data bit rate
                uint maxDataTQCount = MAX_DATA_PHASE_SEGMENT_1 + MAX_DATA_PHASE_SEGMENT_2; // Setting for slowest bit rate
                uint desiredDataBitRate = inDesiredArbitrationBitRate * mDataBitRateFactor;
                uint smallestError = uint.MaxValue;
                uint bestBRP = MAX_BRP; // Setting for lowest bit rate
                uint bestDataTQCount = maxDataTQCount; // Setting for lowest bit rate
                uint dataTQCount = 4;
                uint brp = mSysClock / desiredDataBitRate / dataTQCount;
                //--- Loop for finding best BRP and best TQCount
                while ((dataTQCount <= maxDataTQCount) && (brp > 0))
                {
                    //--- Compute error using brp
                    if (brp <= MAX_BRP)
                    {
                        uint error = mSysClock - desiredDataBitRate * dataTQCount * brp; // error is always >= 0
                        if (error <= smallestError)
                        {
                            smallestError = error;
                            bestBRP = brp;
                            bestDataTQCount = dataTQCount;
                        }
                    }
                    //--- Compute error using brp+1
                    if (brp < MAX_BRP)
                    {
                        uint error = desiredDataBitRate * dataTQCount * (brp + 1) - mSysClock; // error is always >= 0
                        if (error <= smallestError)
                        {
                            smallestError = error;
                            bestBRP = brp + 1;
                            bestDataTQCount = dataTQCount;
                        }
                    }
                    //--- Continue with next value of BRP
                    dataTQCount += 1;
                    brp = mSysClock / desiredDataBitRate / dataTQCount;
                }
                //--- Compute data PS2 (1 <= PS2 <= 16)
                uint dataPS2 = bestDataTQCount / 5; // For sampling point at 80%
                if (dataPS2 == 0)
                {
                    dataPS2 = 1;
                }
                //--- Compute data PS1 (1 <= PS1 <= 32)
                uint dataPS1 = bestDataTQCount - dataPS2 - 1 /* Sync Seg */ ;
                if (dataPS1 > MAX_DATA_PHASE_SEGMENT_1)
                {
                    dataPS2 += dataPS1 - MAX_DATA_PHASE_SEGMENT_1;
                    dataPS1 = MAX_DATA_PHASE_SEGMENT_1;
                }
                //---
                int TDCO = (int)(bestBRP * dataPS1); // According to DS20005678D, ??3.4.8 Page 20
                mTDCO = (sbyte)((TDCO > 63) ? 63 : (byte)TDCO);
                mDataPhaseSegment1 = (byte)dataPS1;
                mDataPhaseSegment2 = (byte)dataPS2;
                mDataSJW = mDataPhaseSegment2;
                uint arbitrationTQCount = bestDataTQCount * mDataBitRateFactor;
                //--- Compute arbiration PS2 (1 <= PS2 <= 128)
                uint arbitrationPS2 = arbitrationTQCount / 5; // For sampling point at 80%
                if (arbitrationPS2 == 0)
                {
                    arbitrationPS2 = 1;
                }
                //--- Compute PS1 (1 <= PS1 <= 256)
                uint arbitrationPS1 = arbitrationTQCount - arbitrationPS2 - 1 /* Sync Seg */ ;
                if (arbitrationPS1 > MAX_ARBITRATION_PHASE_SEGMENT_1)
                {
                    arbitrationPS2 += arbitrationPS1 - MAX_ARBITRATION_PHASE_SEGMENT_1;
                    arbitrationPS1 = MAX_ARBITRATION_PHASE_SEGMENT_1;
                }
                //---
                mBitRatePrescaler = (ushort)bestBRP;
                mArbitrationPhaseSegment1 = (ushort)arbitrationPS1;
                mArbitrationPhaseSegment2 = (byte)arbitrationPS2;
                mArbitrationSJW = mArbitrationPhaseSegment2; // Always 1 <= SJW <= 128, and SJW <= mArbitrationPhaseSegment2
                                                             //--- Final check of the nominal configuration
                uint W = arbitrationTQCount * inDesiredArbitrationBitRate * bestBRP;
                ulong diff = (mSysClock > W) ? (mSysClock - W) : (W - mSysClock);
                ulong ppm = (1000UL * 1000UL); // UL suffix is required for Arduino Uno
                mArbitrationBitRateClosedToDesiredRate = (diff * ppm) <= (((ulong)W) * inTolerancePPM);
            }

            return mArbitrationBitRateClosedToDesiredRate;
        }

        int BitTimeConfigureNominal()
        {
            int spiTransferError = 0;
            REG_CiNBTCFG ciNbtcfg = new REG_CiNBTCFG();

            ciNbtcfg.word = MCP2518_Registers.canControlResetValues[MCP2518_Registers.cREGADDR_CiNBTCFG / 4];
            ciNbtcfg.UpdateFromWord();

            // Arbitration Bit rate
            ciNbtcfg.bF.BRP = (uint)mBitRatePrescaler - 1;
            ciNbtcfg.bF.TSEG1 = (uint)mArbitrationPhaseSegment1 - 1;
            ciNbtcfg.bF.TSEG2 = (uint)mArbitrationPhaseSegment2 - 1;
            ciNbtcfg.bF.SJW = (uint)mArbitrationSJW - 1;

            ciNbtcfg.UpdateFromReg();

            // Write Bit time registers
            spiTransferError = WriteWord(MCP2518_Registers.cREGADDR_CiNBTCFG, ciNbtcfg.word);
            if (spiTransferError != 0)
            {
                return -2;
            }

            return spiTransferError;
        }

        int BitTimeConfigureData(CAN_SSP_MODE sspMode)
        {
            int spiTransferError = 0;
            REG_CiDBTCFG ciDbtcfg = new REG_CiDBTCFG();
            REG_CiTDC ciTdc = new REG_CiTDC();

            // Write Bit time registers
            ciDbtcfg.word = MCP2518_Registers.canControlResetValues[MCP2518_Registers.cREGADDR_CiDBTCFG / 4];
            ciDbtcfg.UpdateFromWord();

            ciDbtcfg.bF.BRP = (uint)mBitRatePrescaler - 1;
            ciDbtcfg.bF.TSEG1 = (uint)mDataPhaseSegment1 - 1;
            ciDbtcfg.bF.TSEG2 = (uint)mDataPhaseSegment2 - 1;
            ciDbtcfg.bF.SJW = (uint)mDataSJW - 1;
            ciDbtcfg.UpdateFromReg();

            spiTransferError = WriteWord(MCP2518_Registers.cREGADDR_CiDBTCFG, ciDbtcfg.word);
            if (spiTransferError != 0)
            {
                return -2;
            }

            // Configure Bit time and sample point, SSP
            ciTdc.word = MCP2518_Registers.canControlResetValues[MCP2518_Registers.cREGADDR_CiTDC / 4];
            ciTdc.UpdateFromWord();

            ciTdc.bF.TDCMode = (uint)sspMode;
            ciTdc.bF.TDCOffset = (uint)mTDCO;
            ciTdc.UpdateFromReg();
            // ciTdc.bF.TDCValue = ?;

            spiTransferError = WriteWord(MCP2518_Registers.cREGADDR_CiTDC, ciTdc.word);
            if (spiTransferError != 0)
            {
                return -3;
            }

            return spiTransferError;
        }

        void BitTimeConfigure(uint speedset, CAN_SSP_MODE sspMode, CAN_SYSCLK_SPEED clk)
        {

            // Decode bitrate
            mDesiredArbitrationBitRate = (uint)(speedset & 0xFFFFFUL);
            mDataBitRateFactor = (byte)((speedset >> 24) & 0xFF);

            // Decode clk
            switch (clk)
            {
                case CAN_SYSCLK_SPEED.CAN_SYSCLK_10M:
                    mSysClock = (uint)(10UL * 1000UL * 1000UL); break;
                case CAN_SYSCLK_SPEED.CAN_SYSCLK_20M:
                    mSysClock = (uint)(20UL * 1000UL * 1000UL); break;
                case CAN_SYSCLK_SPEED.CAN_SYSCLK_40M:
                default:
                    mSysClock = (uint)(40UL * 1000UL * 1000UL); break;
            }

            calcBittime(mDesiredArbitrationBitRate);
            BitTimeConfigureNominal();
            BitTimeConfigureData(sspMode);
        }

        int GpioModeConfigure(GPIO_PIN_MODE gpio0, GPIO_PIN_MODE gpio1)
        {
            int spiTransferError = 0;
            // Read
            ushort a = (ushort)(MCP2518_Registers.cREGADDR_IOCON + 3);
            REG_IOCON iocon = new REG_IOCON();
            iocon.word = 0;
            iocon.UpdateFromWord();


            iocon.bytes[3] = ReadByte(a);

            iocon.UpdateFromByte();

            // Modify
            iocon.bF.PinMode0 = (uint)gpio0;
            iocon.bF.PinMode1 = (uint)gpio1;
            iocon.UpdateFromReg();

            // Write
            spiTransferError = WriteByte(a, iocon.bytes[3]);
            if (spiTransferError != 0)
            {
                return -2;
            }

            return spiTransferError;
        }
        int TransmitChannelEventEnable(CAN_FIFO_CHANNEL channel, CAN_TX_FIFO_EVENT flags)
        {
            int spiTransferError = 0;
            // Read Interrupt Enables
            ushort a = (ushort)(MCP2518_Registers.cREGADDR_CiFIFOCON + ((ushort)channel * MCP2518_Registers.CiFIFO_OFFSET));
            REG_CiFIFOCON ciFifoCon = new REG_CiFIFOCON();
            ciFifoCon.word = 0;
            ciFifoCon.UpdateFromWord();

            ciFifoCon.bytes[0] = ReadByte(a);

            if (spiTransferError != 0)
            {
                return -1;
            }

            ciFifoCon.UpdateFromBytes();

            // Modify
            ciFifoCon.bytes[0] |= (byte)(flags & CAN_TX_FIFO_EVENT.CAN_TX_FIFO_ALL_EVENTS);
            ciFifoCon.UpdateFromBytes();

            // Write
            spiTransferError = WriteByte(a, ciFifoCon.bytes[0]);
            if (spiTransferError != 0)
            {
                return -2;
            }

            return spiTransferError;
        }

        int ReceiveChannelEventEnable(CAN_FIFO_CHANNEL channel, CAN_RX_FIFO_EVENT flags)
        {
            int spiTransferError = 0;
            ushort a = 0;

            if (channel == (CAN_FIFO_CHANNEL)MCP2518_Registers.CAN_TXQUEUE_CH0)
                return -100;

            // Read Interrupt Enables
            a = (ushort)(MCP2518_Registers.cREGADDR_CiFIFOCON + ((ushort)channel * MCP2518_Registers.CiFIFO_OFFSET));
            REG_CiFIFOCON ciFifoCon = new REG_CiFIFOCON();
            ciFifoCon.word = 0;
            ciFifoCon.UpdateFromWord();

            ciFifoCon.bytes[0] = ReadByte(a);

            ciFifoCon.UpdateFromBytes();

            // Modify
            ciFifoCon.bytes[0] |= (byte)(flags & CAN_RX_FIFO_EVENT.CAN_RX_FIFO_ALL_EVENTS);
            ciFifoCon.UpdateFromBytes();

            // Write
            spiTransferError = WriteByte(a, ciFifoCon.bytes[0]);
            if (spiTransferError != 0)
            {
                return -2;
            }

            return spiTransferError;
        }

        int ModuleEventEnable(CAN_MODULE_EVENT flags)
        {
            int spiTransferError = 0;
            ushort a = 0;

            // Read Interrupt Enables
            a = MCP2518_Registers.cREGADDR_CiINTENABLE;
            REG_CiINTENABLE intEnables = new REG_CiINTENABLE();
            intEnables.word = 0;
            intEnables.UpdateFromWord();

            intEnables.word = ReadHalfWord(a);

            intEnables.UpdateFromBytes();

            // Modify
            intEnables.word |= (ushort)(flags & CAN_MODULE_EVENT.CAN_ALL_EVENTS);
            intEnables.UpdateFromWord();

            // Write
            spiTransferError = WriteHalfWord(a, intEnables.word);
            if (spiTransferError != 0)
            {
                return -2;
            }

            return spiTransferError;
        }

        int OperationModeSelect(CAN_OPERATION_MODE opMode)
        {
            int spiTransferError = 0;
            byte d = 0;

            // Read
            d = ReadByte((ushort)(MCP2518_Registers.cREGADDR_CiCON + 3));

            d &= 0xF8;
            d |= (byte)opMode;

            // Write
            spiTransferError = WriteByte((ushort)(MCP2518_Registers.cREGADDR_CiCON + 3), d);
            if (spiTransferError != 0)
            {
                return -2;
            }

            return spiTransferError;
        }

        CAN_OPERATION_MODE OperationModeGet()
        {
            int spiTransferError = 0;
            byte d = 0;
            CAN_OPERATION_MODE mode = CAN_OPERATION_MODE.CAN_INVALID_MODE;

            // Read Opmode
            d = ReadByte((ushort)(MCP2518_Registers.cREGADDR_CiCON + 2));

            // Get Opmode bits
            d = (byte)((d >> 5) & 0x7);

            // Decode Opmode
            switch ((CAN_OPERATION_MODE)d)
            {
                case CAN_OPERATION_MODE.CAN_NORMAL_MODE:
                    mode = CAN_OPERATION_MODE.CAN_NORMAL_MODE;
                    break;
                case CAN_OPERATION_MODE.CAN_SLEEP_MODE:
                    mode = CAN_OPERATION_MODE.CAN_SLEEP_MODE;
                    break;
                case CAN_OPERATION_MODE.CAN_INTERNAL_LOOPBACK_MODE:
                    mode = CAN_OPERATION_MODE.CAN_INTERNAL_LOOPBACK_MODE;
                    break;
                case CAN_OPERATION_MODE.CAN_EXTERNAL_LOOPBACK_MODE:
                    mode = CAN_OPERATION_MODE.CAN_EXTERNAL_LOOPBACK_MODE;
                    break;
                case CAN_OPERATION_MODE.CAN_LISTEN_ONLY_MODE:
                    mode = CAN_OPERATION_MODE.CAN_LISTEN_ONLY_MODE;
                    break;
                case CAN_OPERATION_MODE.CAN_CONFIGURATION_MODE:
                    mode = CAN_OPERATION_MODE.CAN_CONFIGURATION_MODE;
                    break;
                case CAN_OPERATION_MODE.CAN_CLASSIC_MODE:
                    mode = CAN_OPERATION_MODE.CAN_CLASSIC_MODE;
                    break;
                case CAN_OPERATION_MODE.CAN_RESTRICTED_MODE:
                    mode = CAN_OPERATION_MODE.CAN_RESTRICTED_MODE;
                    break;
                default:
                    mode = CAN_OPERATION_MODE.CAN_INVALID_MODE;
                    break;
            }

            return mode;
        }

        CAN_TX_FIFO_EVENT TransmitChannelEventGet(CAN_FIFO_CHANNEL channel)
        {
            CAN_TX_FIFO_EVENT flags;
            int spiTransferError = 0;
            ushort a = 0;

            // Read Interrupt flags
            REG_CiFIFOSTA ciFifoSta = new REG_CiFIFOSTA();
            ciFifoSta.word = 0;
            ciFifoSta.UpdateFromWord();
            a = (ushort)(MCP2518_Registers.cREGADDR_CiFIFOSTA + ((ushort)channel * MCP2518_Registers.CiFIFO_OFFSET));

            ciFifoSta.bytes[0] = ReadByte(a);
            ciFifoSta.UpdateFromBytes();

            // Update data
            flags = (CAN_TX_FIFO_EVENT)(ciFifoSta.bytes[0] & (byte)CAN_TX_FIFO_EVENT.CAN_TX_FIFO_ALL_EVENTS);

            return flags;
        }

        ErrorCountState ErrorCountStateGet()
        {
            int spiTransferError = 0;
            // Read Error
            ushort a = MCP2518_Registers.cREGADDR_CiTREC;
            REG_CiTREC ciTrec = new REG_CiTREC();
            ciTrec.word = 0;
            ciTrec.UpdateFromWord();

            ciTrec.word = ReadWord(a);

            ciTrec.UpdateFromWord();

            // Update data
            ErrorCountState res = new ErrorCountState();
            res.tec = ciTrec.bytes[1];
            res.rec = ciTrec.bytes[0];
            res.flags = (CAN_ERROR_STATE)(ciTrec.bytes[2] & (byte)CAN_ERROR_STATE.CAN_ERROR_ALL);

            return res;
        }

        // *****************************************************************************
        // *****************************************************************************
        // Section: Miscellaneous
        uint DlcToDataBytes(CAN_DLC dlc)
        {
            uint dataBytesInObject = 0;

            if (dlc < CAN_DLC.CAN_DLC_12)
            {
                dataBytesInObject = (uint)dlc;
            }
            else
            {
                switch (dlc)
                {
                    case CAN_DLC.CAN_DLC_12:
                        dataBytesInObject = 12;
                        break;
                    case CAN_DLC.CAN_DLC_16:
                        dataBytesInObject = 16;
                        break;
                    case CAN_DLC.CAN_DLC_20:
                        dataBytesInObject = 20;
                        break;
                    case CAN_DLC.CAN_DLC_24:
                        dataBytesInObject = 24;
                        break;
                    case CAN_DLC.CAN_DLC_32:
                        dataBytesInObject = 32;
                        break;
                    case CAN_DLC.CAN_DLC_48:
                        dataBytesInObject = 48;
                        break;
                    case CAN_DLC.CAN_DLC_64:
                        dataBytesInObject = 64;
                        break;
                    default:
                        break;
                }
            }

            return dataBytesInObject;
        }

        int TransmitChannelLoad(CAN_FIFO_CHANNEL channel, CAN_TX_MSGOBJ txObj, byte[] txd, uint txdNumBytes, bool flush)
        {
            int spiTransferError = 0;
            ushort a;
            uint[] fifoReg = new uint[3];
            uint dataBytesInObject;
            REG_CiFIFOCON ciFifoCon = new REG_CiFIFOCON();
            REG_CiFIFOUA ciFifoUa = new REG_CiFIFOUA();

            // Get FIFO registers
            a = (ushort)(MCP2518_Registers.cREGADDR_CiFIFOCON + ((ushort)channel * MCP2518_Registers.CiFIFO_OFFSET));

            spiTransferError = ReadWordArray(a, fifoReg, 3);
            if (spiTransferError != 0)
            {
                return -1;
            }

            // Check that it is a transmit buffer
            ciFifoCon.word = fifoReg[0];
            ciFifoCon.UpdateFromWord();

            if (!(ciFifoCon.txBF.TxEnable > 0))
            {
                return -2;
            }

            // Check that DLC is big enough for data
            dataBytesInObject = DlcToDataBytes((CAN_DLC)txObj.bF.ctrl.DLC);

            if (dataBytesInObject < txdNumBytes)
            {
                return -3;
            }

            // Get address
            ciFifoUa.word = fifoReg[2];
            ciFifoUa.UpdateFromWord();
            a = (ushort)ciFifoUa.bF.UserAddress;
            a += MCP2518_Registers.cRAMADDR_START;
            byte[] txBuffer = new byte[MCP2518_Registers.MAX_MSG_SIZE];

            txBuffer[0] = txObj.bytes[0]; // not using 'for' to reduce no of instructions
            txBuffer[1] = txObj.bytes[1];
            txBuffer[2] = txObj.bytes[2];
            txBuffer[3] = txObj.bytes[3];

            txBuffer[4] = txObj.bytes[4];
            txBuffer[5] = txObj.bytes[5];
            txBuffer[6] = txObj.bytes[6];
            txBuffer[7] = txObj.bytes[7];

            byte i;
            for (i = 0; i < txdNumBytes; i++)
            {
                txBuffer[i + 8] = txd[i];
            }

            // Make sure we write a multiple of 4 bytes to RAM
            ushort n = 0;
            byte j = 0;

            if (txdNumBytes % 4 > 0)
            {
                // Need to add bytes
                n = (ushort)(4 - (txdNumBytes % 4));
                i = (byte)(txdNumBytes + 8);

                for (j = 0; j < n; j++)
                {
                    txBuffer[i + 8 + j] = 0;
                }
            }
            spiTransferError = WriteByteArray(a, txBuffer, (ushort)(txdNumBytes + 8 + n));
            if (spiTransferError != 0)
            {
                return -4;
            }

            // Set UINC and TXREQ
            spiTransferError = TransmitChannelUpdate(channel, flush);
            if (spiTransferError != 0)
            {
                return -5;
            }

            return spiTransferError;
        }

        CAN_RX_FIFO_EVENT ReceiveChannelEventGet(CAN_FIFO_CHANNEL channel)
        {
            CAN_RX_FIFO_EVENT flags;
            int spiTransferError = 0;
            ushort a = 0;

            if ((uint)channel == MCP2518_Registers.CAN_TXQUEUE_CH0)
                return CAN_RX_FIFO_EVENT.CAN_RX_FIFO_NO_EVENT;

            // Read Interrupt flags
            REG_CiFIFOSTA ciFifoSta = new REG_CiFIFOSTA();
            ciFifoSta.word = 0;
            ciFifoSta.UpdateFromWord();

            a = (ushort)(MCP2518_Registers.cREGADDR_CiFIFOSTA + ((uint)channel * MCP2518_Registers.CiFIFO_OFFSET));

            ciFifoSta.bytes[0] = ReadByte(a);
            ciFifoSta.UpdateFromBytes();

            // Update data
            flags = (CAN_RX_FIFO_EVENT)(ciFifoSta.bytes[0] & (byte)CAN_RX_FIFO_EVENT.CAN_RX_FIFO_ALL_EVENTS);

            return flags;
        }

        int ReceiveMessageGet(CAN_FIFO_CHANNEL channel, CAN_RX_MSGOBJ rxObj, byte[] rxd, byte nBytes)
        {
            int spiTransferError = 0;
            byte n = 0;
            byte i = 0;
            ushort a;
            uint[] fifoReg = new uint[3];
            REG_CiFIFOCON ciFifoCon = new REG_CiFIFOCON();
            REG_CiFIFOUA ciFifoUa = new REG_CiFIFOUA();

            // Get FIFO registers
            a = (ushort)(MCP2518_Registers.cREGADDR_CiFIFOCON + ((ushort)channel * MCP2518_Registers.CiFIFO_OFFSET));

            spiTransferError = ReadWordArray(a, fifoReg, 3);
            if (spiTransferError != 0)
            {
                return -1;
            }

            // Check that it is a receive buffer
            ciFifoCon.word = fifoReg[0];
            ciFifoCon.UpdateFromWord();
            ciFifoCon.txBF.TxEnable = 0;
            ciFifoCon.UpdateFromTxReg();
            if (ciFifoCon.txBF.TxEnable > 0)
            {
                return -2;
            }

            // Get address
            ciFifoUa.word = fifoReg[2];
            a = (ushort)ciFifoUa.bF.UserAddress;
            a += MCP2518_Registers.cRAMADDR_START;

            // Number of bytes to read
            n = (byte)(nBytes + 8); // Add 8 header bytes

            if (ciFifoCon.rxBF.RxTimeStampEnable > 0)
            {
                n += 4; // Add 4 time stamp bytes
            }

            // Make sure we read a multiple of 4 bytes from RAM
            if (n % 4 > 0)
            {
                n = (byte)(n + 4 - (n % 4));
            }

            // Read rxObj using one access
            byte[] ba = new byte[MCP2518_Registers.MAX_MSG_SIZE];

            if (n > MCP2518_Registers.MAX_MSG_SIZE)
            {
                n = (byte)MCP2518_Registers.MAX_MSG_SIZE;
            }

            spiTransferError = ReadByteArray(a, ba, n);
            if (spiTransferError != 0)
            {
                return -3;
            }

            // Assign message header
            REG_t myReg = new REG_t();

            myReg.bytes[0] = ba[0];
            myReg.bytes[1] = ba[1];
            myReg.bytes[2] = ba[2];
            myReg.bytes[3] = ba[3];
            myReg.UpdateFromBytes();
            rxObj.word[0] = myReg.word;
            rxObj.UpdateFromWord();

            myReg.bytes[0] = ba[4];
            myReg.bytes[1] = ba[5];
            myReg.bytes[2] = ba[6];
            myReg.bytes[3] = ba[7];
            myReg.UpdateFromBytes();
            rxObj.word[1] = myReg.word;
            rxObj.UpdateFromWord();

            if (ciFifoCon.rxBF.RxTimeStampEnable > 0)
            {
                myReg.bytes[0] = ba[8];
                myReg.bytes[1] = ba[9];
                myReg.bytes[2] = ba[10];
                myReg.bytes[3] = ba[11];
                rxObj.word[2] = myReg.word;
                rxObj.UpdateFromWord();
                // Assign message data
                for (i = 0; i < nBytes; i++)
                {
                    rxd[i] = ba[i + 12];
                }
            }
            else
            {
                rxObj.word[2] = 0;
                rxObj.UpdateFromWord();
                // Assign message data
                for (i = 0; i < nBytes; i++)
                {
                    rxd[i] = ba[i + 8];
                }
            }

            // UINC channel
            spiTransferError = ReceiveChannelUpdate(channel);
            if (spiTransferError != 0)
            {
                return -4;
            }

            return spiTransferError;
        }

        int ReceiveChannelUpdate(CAN_FIFO_CHANNEL channel)
        {
            int spiTransferError = 0;
            ushort a = 0;
            REG_CiFIFOCON ciFifoCon = new REG_CiFIFOCON();
            ciFifoCon.word = 0;
            ciFifoCon.UpdateFromWord();

            // Set UINC
            a = (ushort)(MCP2518_Registers.cREGADDR_CiFIFOCON + ((uint)channel * MCP2518_Registers.CiFIFO_OFFSET) +
            1); // Byte that contains FRESET
            ciFifoCon.rxBF.UINC = 1;
            ciFifoCon.UpdateFromRxReg();
            // Write byte
            spiTransferError = WriteByte(a, ciFifoCon.bytes[1]);

            return spiTransferError;
        }

        int TransmitChannelUpdate(CAN_FIFO_CHANNEL channel, bool flush)
        {
            int spiTransferError = 0;
            ushort a;
            REG_CiFIFOCON ciFifoCon = new REG_CiFIFOCON();

            // Set UINC
            a = (ushort)(MCP2518_Registers.cREGADDR_CiFIFOCON + ((uint)channel * MCP2518_Registers.CiFIFO_OFFSET) +
            1); // Byte that contains FRESET
            ciFifoCon.word = 0;
            ciFifoCon.UpdateFromWord();
            ciFifoCon.txBF.UINC = 1;

            // Set TXREQ
            if (flush)
            {
                ciFifoCon.txBF.TxRequest = 1;
            }
            ciFifoCon.UpdateFromTxReg();

            spiTransferError = WriteByte(a, ciFifoCon.bytes[1]);
            if (spiTransferError != 0)
            {
                return -1;
            }

            return spiTransferError;

        }

        CAN_RX_FIFO_STATUS ReceiveChannelStatusGet(CAN_FIFO_CHANNEL channel)
        {
            CAN_RX_FIFO_STATUS status;
            int spiTransferError = 0;
            ushort a;
            REG_CiFIFOSTA ciFifoSta = new REG_CiFIFOSTA();

            // Read
            ciFifoSta.word = 0;
            a = (ushort)(MCP2518_Registers.cREGADDR_CiFIFOSTA + ((uint)channel * MCP2518_Registers.CiFIFO_OFFSET));


            ciFifoSta.bytes[0] = ReadByte(a);
            ciFifoSta.UpdateFromBytes();

            // Update data
            status = (CAN_RX_FIFO_STATUS)(ciFifoSta.bytes[0] & 0x0F);

            return status;
        }

        CAN_ERROR_STATE ErrorStateGet()
        {
            CAN_ERROR_STATE flags;
            int spiTransferError = 0;
            // Read Error state
            byte f = 0;

            f = ReadByte((ushort)(MCP2518_Registers.cREGADDR_CiTREC + 2));

            // Update data
            flags = (CAN_ERROR_STATE)(f & (byte)CAN_ERROR_STATE.CAN_ERROR_ALL);

            return flags;
        }

        CAN_RXCODE ModuleEventRxCodeGet()
        {
            CAN_RXCODE rxCode;
            int spiTransferError = 0;
            byte rxCodeByte = 0;

            rxCodeByte = ReadByte((ushort)(MCP2518_Registers.cREGADDR_CiVEC + 3));

            // Decode data
            // 0x40 = "no interrupt" (CAN_FIFO_CIVEC_NOINTERRUPT)
            if ((rxCodeByte < (byte)CAN_RXCODE.CAN_RXCODE_TOTAL_CHANNELS) ||
            (rxCodeByte == (byte)CAN_RXCODE.CAN_RXCODE_NO_INT))
            {
                rxCode = (CAN_RXCODE)rxCodeByte;
            }
            else
            {
                rxCode = CAN_RXCODE.CAN_RXCODE_RESERVED; // shouldn't get here
            }

            return rxCode;
        }

        CAN_TXCODE ModuleEventTxCodeGet()
        {
            CAN_TXCODE txCode;
            int spiTransferError = 0;
            ushort a = 0;
            byte txCodeByte;
            // Read
            a = (ushort)(MCP2518_Registers.cREGADDR_CiVEC + 2);

            txCodeByte = ReadByte(a);

            // Decode data
            // 0x40 = "no interrupt" (CAN_FIFO_CIVEC_NOINTERRUPT)
            if ((txCodeByte < (byte)CAN_TXCODE.CAN_TXCODE_TOTAL_CHANNELS) ||
            (txCodeByte == (byte)CAN_TXCODE.CAN_TXCODE_NO_INT))
            {
                txCode = (CAN_TXCODE)txCodeByte;
            }
            else
            {
                txCode = CAN_TXCODE.CAN_TXCODE_RESERVED; // shouldn't get here
            }

            return txCode;
        }

        int TransmitChannelEventAttemptClear(CAN_FIFO_CHANNEL channel)
        {
            int spiTransferError = 0;
            ushort a = 0;

            // Read Interrupt Enables
            a = (ushort)(MCP2518_Registers.cREGADDR_CiFIFOSTA + ((uint)channel * MCP2518_Registers.CiFIFO_OFFSET));
            REG_CiFIFOSTA ciFifoSta = new REG_CiFIFOSTA();
            ciFifoSta.word = 0;
            ciFifoSta.UpdateFromWord();

            ciFifoSta.bytes[0] = ReadByte(a);
            ciFifoSta.UpdateFromBytes();

            // Modify
            ciFifoSta.bytes[0] &= 0b11101111;
            ciFifoSta.UpdateFromBytes();

            // Write
            spiTransferError = WriteByte(a, ciFifoSta.bytes[0]);

            if (spiTransferError != 0)
            {
                return -2;
            }

            return spiTransferError;
        }


        int LowPowerModeEnable()
        {
            int spiTransferError = 0;
            byte d = 0;

            // Read
            d = ReadByte((ushort)MCP2518_Registers.cREGADDR_OSC);

            // Modify
            d |= 0x08;

            // Write
            spiTransferError = WriteByte(MCP2518_Registers.cREGADDR_OSC, d);
            if (spiTransferError != 0)
            {
                return -2;
            }

            return spiTransferError;
        }

        int LowPowerModeDisable()
        {
            int spiTransferError = 0;
            byte d = 0;
            // Read
            d = ReadByte(MCP2518_Registers.cREGADDR_OSC);

            // Modify
            d &= 0b0111;

            // Write
            spiTransferError = WriteByte(MCP2518_Registers.cREGADDR_OSC, d);
            if (spiTransferError != 0)
            {
                return -2;
            }

            return spiTransferError;
        }

        void TransmitMessageQueue()
        {
            byte attempts = (byte)MAX_TXQUEUE_ATTEMPTS;

            // Check if FIFO is not full

            do
            {
                txFlags = TransmitChannelEventGet(APP_TX_FIFO);
                if (attempts == 0)
                {
                    var res = ErrorCountStateGet();
                    return;
                }
                attempts--;
            } while (!((txFlags & CAN_TX_FIFO_EVENT.CAN_TX_FIFO_NOT_FULL_EVENT) > 0));

            // Load message and transmit
            byte n = (byte)DlcToDataBytes((CAN_DLC)txObj.bF.ctrl.DLC);
            TransmitChannelLoad(APP_TX_FIFO, txObj, txd, n, true);
        }

        /*********************************************************************************************************
        ** Function name:           sendMsg
        ** Descriptions:            send message
        *********************************************************************************************************/
        byte SendMsg(byte[] buf, byte len, ulong id, byte ext, byte rtr, bool wait_sent)
        {
            byte n;
            int i;
            byte spiTransferError = 0;
            // Configure message data
            txObj.word[0] = 0;
            txObj.word[1] = 0;

            txObj.bF.ctrl.RTR = (uint)(rtr > 0 ? 1 : 0);
            if (rtr > 0 && len > (byte)CAN_DLC.CAN_DLC_8)
            {
                len = (byte)CAN_DLC.CAN_DLC_8;
            }
            txObj.bF.ctrl.DLC = len;

            txObj.bF.ctrl.IDE = (uint)(ext > 0 ? 1 : 0);
            if (ext > 0)
            {
                txObj.bF.id.SID = (uint)((id >> 18) & 0x7FF);
                txObj.bF.id.EID = (uint)(id & 0x3FFFF);
            }
            else
            {
                txObj.bF.id.SID = (uint)id;
            }

            txObj.bF.ctrl.BRS = 1;

            //txObj.bF.ctrl.FDF = (len > 8);
            txObj.bF.ctrl.FDF = (uint)(__flgFDF ? 1 : 0);
            //if(__flgFDF)

            txObj.UpdateFromReg();

            n = (byte)DlcToDataBytes((CAN_DLC)txObj.bF.ctrl.DLC);
            // Prepare data
            for (i = 0; i < n; i++)
            {
                txd[i] = buf[i];
            }

            TransmitMessageQueue();
            return spiTransferError;
        }

        int ReceiveMsg()
        {
            rxFlags = ReceiveChannelEventGet(APP_RX_FIFO);


            if ((rxFlags & CAN_RX_FIFO_EVENT.CAN_RX_FIFO_NOT_EMPTY_EVENT) > 0)
            {
                ReceiveMessageGet(APP_RX_FIFO, rxObj, rxd, 8);
            }

            return 0;
        }

        uint Bittime_compat_to_mcp2518fd(uint speedset)
        {
            uint r = 0;

            if (speedset > 0x100)
            {
                return speedset;
            }
            switch ((MCP_BITTIME_SETUP)speedset)
            {
                case MCP_BITTIME_SETUP.CAN20_5KBPS: r = BITRATE((uint)5000UL, 0); break;
                case MCP_BITTIME_SETUP.CAN20_10KBPS: r = BITRATE((uint)10000UL, 0); break;
                case MCP_BITTIME_SETUP.CAN20_20KBPS: r = BITRATE((uint)20000UL, 0); break;
                case MCP_BITTIME_SETUP.CAN20_25KBPS: r = BITRATE((uint)25000UL, 0); break;
                case MCP_BITTIME_SETUP.CAN20_31K25BPS: r = BITRATE((uint)31250UL, 0); break;
                case MCP_BITTIME_SETUP.CAN20_33KBPS: r = BITRATE((uint)33000UL, 0); break;
                case MCP_BITTIME_SETUP.CAN20_40KBPS: r = BITRATE((uint)40000UL, 0); break;
                case MCP_BITTIME_SETUP.CAN20_50KBPS: r = BITRATE((uint)50000UL, 0); break;
                case MCP_BITTIME_SETUP.CAN20_80KBPS: r = BITRATE((uint)80000UL, 0); break;
                case MCP_BITTIME_SETUP.CAN20_83K3BPS: r = BITRATE((uint)83300UL, 0); break;
                case MCP_BITTIME_SETUP.CAN20_95KBPS: r = BITRATE((uint)95000UL, 0); break;
                case MCP_BITTIME_SETUP.CAN20_100KBPS: r = BITRATE((uint)100000UL, 0); break;
                case MCP_BITTIME_SETUP.CAN20_125KBPS: r = BITRATE((uint)125000UL, 0); break;
                case MCP_BITTIME_SETUP.CAN20_200KBPS: r = BITRATE((uint)200000UL, 0); break;
                case MCP_BITTIME_SETUP.CAN20_250KBPS: r = BITRATE((uint)250000UL, 0); break;
                case MCP_BITTIME_SETUP.CAN20_500KBPS: r = BITRATE((uint)500000UL, 0); break;
                case MCP_BITTIME_SETUP.CAN20_666KBPS: r = BITRATE((uint)666000UL, 0); break;
                case MCP_BITTIME_SETUP.CAN20_800KBPS: r = BITRATE((uint)800000UL, 0); break;
                case MCP_BITTIME_SETUP.CAN20_1000KBPS: r = BITRATE((uint)1000000UL, 0); break;
            }
            return r;
        }

        uint BITRATE(uint arbitration, byte factor)
        {
            return ((uint)factor << 24) | (uint)(arbitration & 0xFFFFFUL);
        }

        /*********************************************************************************************************
        ** Function name:           mcp2515_init
        ** Descriptions:            init the device
        **                          speedset msb  8 bits = factor (0 or 1 is no bit rate switch)
        **                                   lsb 24 bits = arbitration bitrate
        *********************************************************************************************************/
        byte Init(uint speedset, CAN_SYSCLK_SPEED clock)
        {
            // Reset device
            Reset();

            // Enable ECC and initialize RAM
            EccEnable();

            RamInit(0xff);

            // Configure device
            ConfigureObjectReset(config);
            config.IsoCrcEnable = 1;
            config.StoreInTEF = 0;
            Configure(config);

            // Setup TX FIFO
            TransmitChannelConfigureObjectReset(txConfig);
            txConfig.FifoSize = 7;
            txConfig.PayLoadSize = (uint)CAN_FIFO_PLSIZE.CAN_PLSIZE_64;
            txConfig.TxPriority = 1;

            TransmitChannelConfigure(APP_TX_FIFO, txConfig);

            // Setup RX FIFO
            ReceiveChannelConfigureObjectReset(rxConfig);
            rxConfig.FifoSize = 15;
            rxConfig.PayLoadSize = (uint)CAN_FIFO_PLSIZE.CAN_PLSIZE_64;
            ReceiveChannelConfigure(APP_RX_FIFO, rxConfig);

            // Setup RX Filter
            //fObj.word = 0;
            //mcp2518fd_FilterObjectConfigure(CAN_FILTER0, &fObj.bF);

            // Setup RX Mask
            //mObj.word = 0; // Only allow standard IDs
            //mcp2518fd_FilterMaskConfigure(CAN_FILTER0, &mObj.bF);

            for (int i = 0; i < 32; i++)
                FilterDisable((CAN_FILTER)i);          // disable all filter


            // Link FIFO and Filter
            FilterToFifoLink(CAN_FILTER.CAN_FILTER0, APP_RX_FIFO, true);

            // Setup Bit Time
            BitTimeConfigure(speedset, CAN_SSP_MODE.CAN_SSP_MODE_AUTO, clock);

            // Setup Transmit and Receive Interrupts
            GpioModeConfigure(GPIO_PIN_MODE.GPIO_MODE_INT, GPIO_PIN_MODE.GPIO_MODE_INT);

            TransmitChannelEventEnable(APP_TX_FIFO, CAN_TX_FIFO_EVENT.CAN_TX_FIFO_NOT_FULL_EVENT);

            ReceiveChannelEventEnable(APP_RX_FIFO, CAN_RX_FIFO_EVENT.CAN_RX_FIFO_NOT_EMPTY_EVENT);
            ModuleEventEnable(CAN_MODULE_EVENT.CAN_TX_EVENT | CAN_MODULE_EVENT.CAN_RX_EVENT);
            SetMode((byte)mcpMode);

            return 0;
        }

        /*********************************************************************************************************
        ** Function name:           enableTxInterrupt
        ** Descriptions:            enable interrupt for all tx buffers
        *********************************************************************************************************/
        public void EnableTxInterrupt(bool enable)
        {
            if (enable == true)
            {
                ModuleEventEnable(CAN_MODULE_EVENT.CAN_TX_EVENT);
            }
            return;
        }

        public void ReserveTxBuffers(byte nTxBuf = 0)
        {
            nReservedTx = (byte)(nTxBuf < 3 ? nTxBuf : 3 - 1);
        }

        public byte GetLastTxBuffer()
        {
            return 3 - 1; // read index of last tx buffer
        }

        //
        public int FilterDisable(CAN_FILTER filter)
        {
            int spiTransferError = 0;
            ushort a;
            REG_CiFLTCON_BYTE fCtrl = new REG_CiFLTCON_BYTE();

            // Read
            a = (ushort)(MCP2518_Registers.cREGADDR_CiFLTCON + filter);

            fCtrl.single_byte = ReadByte(a);
            fCtrl.UpdateFromByte();

            // Modify
            fCtrl.bF.Enable = 0;
            fCtrl.UpdateFromReg();

            // mcp2518fd_WriteByte(ushort address, byte txd)
            spiTransferError = WriteByte(a, fCtrl.single_byte);
            if (spiTransferError != 0)
            {
                return -2;
            }

            return spiTransferError;
        }


        /*
        typedef struct _CAN_FILTEROBJ_ID {
          uint SID : 11;
          uint EID : 18;
          uint SID11 : 1;
          uint EXIDE : 1;
          uint unimplemented1 : 1;
        } CAN_FILTEROBJ_ID;
        */
        public void Init_Filt_Mask(byte num, byte ext, ulong f, ulong m)
        {

            OperationModeSelect(CAN_OPERATION_MODE.CAN_CONFIGURATION_MODE);        // enter into setting mode


            FilterDisable((CAN_FILTER)num);

            CAN_FILTEROBJ_ID fObj = new CAN_FILTEROBJ_ID();
            if (ext > 0)
            {
                fObj.SID = 0;
                fObj.SID11 = (uint)f >> 18;
                fObj.EID = (uint)f & 0x3ffff;
                fObj.EXIDE = 1;
            }
            else
            {
                fObj.SID = (uint)f;
                fObj.SID11 = 0;
                fObj.EID = 0;
                fObj.EXIDE = 0;
            }

            FilterObjectConfigure((CAN_FILTER)num, fObj);

            CAN_MASKOBJ_ID mObj = new CAN_MASKOBJ_ID();
            if (ext > 0)
            {
                mObj.MSID = 0;
                mObj.MSID11 = (uint)m >> 18;
                mObj.MEID = (uint)m & 0x3ffff;
                mObj.MIDE = 1;
            }
            else
            {
                mObj.MSID = (uint)m;
                mObj.MSID11 = 0;
                mObj.MEID = 0;
                mObj.MIDE = 1;
            }

            FilterMaskConfigure((CAN_FILTER)num, mObj);
            bool filterEnable = true;

            FilterToFifoLink((CAN_FILTER)num, APP_RX_FIFO, filterEnable);

            OperationModeSelect(mcpMode);
        }

        /*********************************************************************************************************
        ** Function name:           setSleepWakeup
        ** Descriptions:            Enable or disable the wake up interrupt (If disabled
        *the MCP2515 will not be woken up by CAN bus activity)
        *********************************************************************************************************/
        public void SetSleepWakeup(bool enable)
        {
            if (enable)
            {
                LowPowerModeEnable();
            }
            else
            {
                LowPowerModeDisable();
            }
        }

        /*********************************************************************************************************
        ** Function name:           sleep
        ** Descriptions:            Put mcp2515 in sleep mode to save power
        *********************************************************************************************************/
        public int Sleep()
        {
            if (GetMode() != 0x01)
            {
                return OperationModeSelect(CAN_OPERATION_MODE.CAN_SLEEP_MODE);
            }
            else
            {
                return CAN_OK;
            }
        }

        /*********************************************************************************************************
        ** Function name:           wake
        ** Descriptions:            wake MCP2515 manually from sleep. It will come back
        *in the mode it was before sleeping.
        *********************************************************************************************************/
        public int Wake()
        {
            byte currMode = GetMode();
            if (currMode != (byte)mcpMode)
            {
                return OperationModeSelect(mcpMode);
            }
            else
            {
                return CAN_OK;
            }
        }

        /*********************************************************************************************************
        ** Function name:           getMode
        ** Descriptions:            Returns current control mode
        *********************************************************************************************************/
        public byte GetMode()
        {
            CAN_OPERATION_MODE mode;
            mode = OperationModeGet();
            return (byte)mode;
        }

        /*********************************************************************************************************
        ** Function name:           __setMode
        ** Descriptions:            Sets control mode
        *********************************************************************************************************/
        public int SetMode(byte opMode)
        {
            if ((CAN_OPERATION_MODE)opMode != CAN_OPERATION_MODE.CAN_SLEEP_MODE)
            { // if going to sleep, the value stored in opMode is not
              // changed so that we can return to it later
                mcpMode = (CAN_OPERATION_MODE)opMode;
            }
            return OperationModeSelect(mcpMode);
        }

        /*********************************************************************************************************
        ** Function name:           readMsgBufID
        ** Descriptions:            Read message buf and can bus source ID according to
        *status.
        **                          Status has to be read with readRxTxStatus.
        *********************************************************************************************************/
        public CANMSG ReadMsgBufID(byte status, CANMSG rxMSG)
        {

            int r = ReadMsgBufID(rxMSG.len, rxMSG.buf);
            rxMSG.CANID = can_id;

            rxMSG.IsExtended = ext_flg > 0;
            
            rxMSG.IsExtended = this.rtr > 0;

            return rxMSG;
        }

        public CANMSG ReadMsgBufID(ulong ID, byte len, byte[] buf)
        {
            CANMSG rxMSG = new CANMSG();
            rxMSG.CANID = ID;
            rxMSG.len = len;
            rxMSG.buf = buf;
            rxMSG.IsRemote = rtr > 0;
            rxMSG.IsExtended = ext_flg > 0;
            return ReadMsgBufID(ReadRxTxStatus(), rxMSG);

        }

        public CANMSG ReadMsgBuf(byte len, byte[] buf)
        {
            CANMSG rxMSG = new CANMSG();
            rxMSG.CANID = can_id;
            rxMSG.len = len;
            rxMSG.buf = buf;
            rxMSG.IsRemote = rtr > 0;
            rxMSG.IsExtended = ext_flg > 0;
            return ReadMsgBufID(ReadRxTxStatus(), rxMSG);

        }

        /*********************************************************************************************************
        ** Function name:           checkReceive
        ** Descriptions:            check if got something
        *********************************************************************************************************/
        unsafe public byte CheckReceive()
        {
            CAN_RX_FIFO_STATUS status;
            // RXnIF in Bit 1 and 0 return ((res & MCP_STAT_RXIF_MASK)? CAN_MSGAVAIL: CAN_NOMSG);
            ReceiveChannelStatusGet(APP_RX_FIFO, &status);

            byte res = (byte)(((byte)status & (byte)CAN_RX_FIFO_EVENT.CAN_RX_FIFO_NOT_EMPTY_EVENT) + 2);
            return res;
        }

        /*********************************************************************************************************
        ** Function name:           checkError
        ** Descriptions:            if something error
        *********************************************************************************************************/

        unsafe public byte CheckError(CAN_ERROR_STATE* err_ptr)
        {
            CAN_ERROR_STATE flags;
            ErrorStateGet(&flags);
            *err_ptr = flags;

            return (byte)flags;

        }


        // /*********************************************************************************************************
        // ** Function name:           readMsgBufID
        // ** Descriptions:            Read message buf and can bus source ID according
        // to status.
        // **                          Status has to be read with readRxTxStatus.
        // *********************************************************************************************************/
        public CANMSG ReadMsgBufID(CANMSG rxMSG)
        {
            var r = ReceiveMessageGet(APP_RX_FIFO, rxObj, rxMSG.rxd, (byte)MCP2518_Registers.MAX_DATA_BYTES);
            ext_flg = (byte)rxObj.bF.ctrl.IDE;
            //can_id = ext_flg? (rxObj.bF.id.EID | (rxObj.bF.id.SID << 18))
            can_id = ext_flg > 0 ? (rxObj.bF.id.EID | ((uint)rxObj.bF.id.SID << 18)) : rxObj.bF.id.SID;
            //:  rxObj.bF.id.SID;
            rtr = (byte)rxObj.bF.ctrl.RTR;
            byte n = (byte)DlcToDataBytes((CAN_DLC)rxObj.bF.ctrl.DLC);
            if (len > 0)
            {
                len = n;
            }

            for (int i = 0; i < n; i++)
            {
                buf[i] = rxd[i];
            }

            CANMSG rxMSG = new CANMSG();

            return 0;
        }

        /*********************************************************************************************************
        ** Function name:           trySendMsgBuf
        ** Descriptions:            Try to send message. There is no delays for waiting
        *free buffer.
        *********************************************************************************************************/
        public byte TrySendMsgBuf(ulong id, byte ext, byte rtr, byte len, byte[] buf, byte iTxBuf)
        {
            return SendMsg(buf, len, id, ext, rtr, false);
        }

        /*********************************************************************************************************
        ** Function name:           clearBufferTransmitIfFlags
        ** Descriptions:            Clear transmit interrupt flags for specific buffer
        *or for all unreserved buffers.
        **                          If interrupt will be used, it is important to clear
        *all flags, when there is no
        **                          more data to be sent. Otherwise IRQ will newer
        *change state.
        *********************************************************************************************************/
        public void ClearBufferTransmitIfFlags(byte flags = 0)
        {
            TransmitChannelEventAttemptClear(APP_TX_FIFO);
        }

        /*********************************************************************************************************
        ** Function name:           sendMsgBuf
        ** Descriptions:            Send message by using buffer read as free from
        *CANINTF status
        **                          Status has to be read with readRxTxStatus and
        *filtered with checkClearTxStatus
        *********************************************************************************************************/
        public byte SendMsgBuf(byte status, ulong id, byte ext, byte rtr,
        byte len, byte[] buf)
        {
            return SendMsg(buf, len, id, ext, rtr, true);
        }

        public byte SendMsgBuf(ulong id, byte ext, byte len, byte[] buf)
        {
            return SendMsgBuf(id, ext, 0, len, buf, true);
        }

        /*********************************************************************************************************
        ** Function name:           sendMsgBuf
        ** Descriptions:            send buf
        *********************************************************************************************************/
        byte SendMsgBuf(ulong id, byte ext, byte rtr, byte len, byte[] buf, bool wait_sent)
        {
            return SendMsg(buf, len, id, ext, rtr, wait_sent);
        }

        /*********************************************************************************************************
        ** Function name:           readRxTxStatus
        ** Descriptions:            Read RX and TX interrupt bits. Function uses status
        *reading, but translates.
        **                          result to MCP_CANINTF. With this you can check
        *status e.g. on interrupt sr
        **                          with one single call to save SPI calls. Then use
        *checkClearRxStatus and
        **                          checkClearTxStatus for testing.
        *********************************************************************************************************/
        unsafe public byte ReadRxTxStatus()
        {
            byte ret;
            fixed (CAN_RX_FIFO_EVENT* ptr = &rxFlags)
            {
                ReceiveChannelEventGet(APP_RX_FIFO, ptr);
            }

            ret = (byte)rxFlags;
            return ret;
        }

        /*********************************************************************************************************
        ** Function name:           checkClearRxStatus
        ** Descriptions:            Return first found rx CANINTF status and clears it
        *from parameter.
        **                          Note that this does not affect to chip CANINTF at
        *all. You can use this
        **                          with one single readRxTxStatus call.
        *********************************************************************************************************/
        public byte CheckClearRxStatus(byte status)
        {
            return 1;
        }

        /*********************************************************************************************************
        ** Function name:           checkClearTxStatus
        ** Descriptions:            Return specified buffer of first found tx CANINTF
        *status and clears it from parameter.
        **                          Note that this does not affect to chip CANINTF at
        *all. You can use this
        **                          with one single readRxTxStatus call.
        *********************************************************************************************************/
        public byte CheckClearTxStatus(byte status, byte iTxBuf)
        {
            return 1;
        }

        /*********************************************************************************************************
        ** Function name:           mcpPinMode
        ** Descriptions:            switch supported pins between HiZ, interrupt, output
        *or input
        *********************************************************************************************************/
        unsafe public int McpPinMode(byte pin, GPIO_PIN_MODE mode)
        {
            int spiTransferError = 0;
            // Read
            ushort a = (ushort)(MCP2518_Registers.cREGADDR_IOCON + 3);
            REG_IOCON iocon = new REG_IOCON();
            iocon.word = 0;
            iocon.UpdateFromWord();

            fixed (byte* ptr = &iocon.bytes[3])
            {
                spiTransferError = ReadByte(a, ptr);
            }
            if (spiTransferError != 0)
            {
                return -1;
            }

            if (pin == (byte)GPIO_PIN_POS.GPIO_PIN_0)
            {
                // Modify
                iocon.bF.PinMode0 = (uint)mode;
            }
            if (pin == (byte)GPIO_PIN_POS.GPIO_PIN_1)
            {
                // Modify
                iocon.bF.PinMode1 = (byte)mode;
            }
            iocon.UpdateFromReg();
            // Write
            spiTransferError = WriteByte(a, iocon.bytes[3]);
            if (spiTransferError != 0)
            {
                return -2;
            }

            return spiTransferError;
        }

        /*********************************************************************************************************
        ** Function name:           mcpDigitalWrite
        ** Descriptions:            write HIGH or LOW to RX0BF/RX1BF
        *********************************************************************************************************/
        unsafe public int McpDigitalWrite(GPIO_PIN_POS pin, GPIO_PIN_STATE mode)
        {
            int spiTransferError = 0;
            // Read
            ushort a = (ushort)(MCP2518_Registers.cREGADDR_IOCON + 1);
            REG_IOCON iocon = new REG_IOCON();
            iocon.word = 0;
            iocon.UpdateFromWord();

            fixed (byte* ptr = &iocon.bytes[1])
            {
                spiTransferError = ReadByte(a, ptr);
            }
            if (spiTransferError != 0)
            {
                return -1;
            }


            // Modify
            switch (pin)
            {
                case GPIO_PIN_POS.GPIO_PIN_0:
                    iocon.bF.LAT0 = (byte)mode;
                    break;
                case GPIO_PIN_POS.GPIO_PIN_1:
                    iocon.bF.LAT1 = (byte)mode;
                    break;
                default:
                    return -2;
            }
            iocon.UpdateFromReg();

            // Write
            spiTransferError = WriteByte(a, iocon.bytes[1]);
            if (spiTransferError != 0)
            {
                return -3;
            }

            return spiTransferError;
        }

        /*********************************************************************************************************
        ** Function name:           mcpDigitalRead
        ** Descriptions:            read HIGH or LOW from supported pins
        *********************************************************************************************************/
        unsafe public sbyte McpDigitalRead(GPIO_PIN_POS pin)
        {
            int spiTransferError = 0;
            GPIO_PIN_STATE state;

            // Read
            REG_IOCON iocon = new REG_IOCON();
            iocon.word = 0;
            iocon.UpdateFromWord();

            fixed (byte* ptr = &iocon.bytes[2])
            {
                spiTransferError = ReadByte((ushort)(MCP2518_Registers.cREGADDR_IOCON + 2), ptr);
            }

            if (spiTransferError != 0)
            {
                return -1;
            }

            iocon.UpdateFromByte();

            // Update data
            switch (pin)
            {
                case GPIO_PIN_POS.GPIO_PIN_0:
                    state = (GPIO_PIN_STATE)iocon.bF.GPIO0;
                    break;
                case GPIO_PIN_POS.GPIO_PIN_1:
                    state = (GPIO_PIN_STATE)iocon.bF.GPIO1;
                    break;
                default:
                    return -2;
            }

            return (sbyte)state; ;
        }

        /* CANFD Auxiliary helper */
        public byte dlc2len(CAN_DLC dlc)
        {
            if (dlc <= CAN_DLC.CAN_DLC_8)
                return (byte)dlc;
            switch (dlc)
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

        public byte len2dlc(byte len)
        {
            if (len <= (byte)CAN_DLC.CAN_DLC_8)
                return len;
            else if (len <= 12) return (byte)CAN_DLC.CAN_DLC_12;
            else if (len <= 16) return (byte)CAN_DLC.CAN_DLC_16;
            else if (len <= 20) return (byte)CAN_DLC.CAN_DLC_20;
            else if (len <= 24) return (byte)CAN_DLC.CAN_DLC_24;
            else if (len <= 32) return (byte)CAN_DLC.CAN_DLC_32;
            else if (len <= 48) return (byte)CAN_DLC.CAN_DLC_48;
            return (byte)CAN_DLC.CAN_DLC_64;
        }
    }

    /// <summary>Represents a CAN message.</summary>
    public class CANMSG
    {
        public ulong CANID { get; set; }
        public bool IsExtended { get; set; }
        public bool IsRemote { get; set; }
        public byte len { get; set; }
        public byte[] buf = new byte[MCP2518_Registers.MAX_DATA_BYTES];
    }

    public class ByteArrayCRC
    {
        public bool crcIsCorrect;
        public byte[] rxd;
    }

    public class ErrorCountState
    {
        public byte tec;
        public byte rec;
        public CAN_ERROR_STATE flags;
    }


    public enum MCP_CLOCK_T
    {
        MCP_NO_MHz,
        /* apply to MCP2515 */
        MCP_16MHz,
        MCP_8MHz,
        /* apply to MCP2518FD */
        MCP2518FD_40MHz = MCP_16MHz /* To compatible MCP2515 shield */,
        MCP2518FD_20MHz,
        MCP2518FD_10MHz,
    };

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
}
