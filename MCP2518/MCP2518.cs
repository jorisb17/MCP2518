using GHIElectronics.TinyCLR.Devices.Gpio;
using GHIElectronics.TinyCLR.Devices.Spi;
using System;
using System.Collections;
using System.Text;
using System.Threading;
using MCP2518;

namespace MCP2518
{
    public class MCP2518 : MCPCanInterface
    {
        #region Defines
        public int CRCBASE = 0xFFFF;
        public int CRCUPPER = 1;

        public int SPI_DEFAULT_BUFFER_LENGTH = 96;

        //! Reverse order of bits in byte
        public byte[] BitReverseTable256 = {
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
        public ushort[] crc16_table = {
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


        #endregion

        #region Private Vars

        byte nReservedTx;     // Count of tx buffers for reserved send
        CAN_OPERATION_MODE mcpMode = CAN_OPERATION_MODE.CAN_CLASSIC_MODE; // Current controller mode

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


        //! SPI Transmit buffer
        static byte[] spiTransmitBuffer = new byte[98];

        //! SPI Receive buffer
        static byte[] spiReceiveBuffer = new byte[96];

        #endregion

        #region Tx/Rx objects

        static MCP2518Dfs.CAN_CONFIG config = new();
        private bool _flgFDF = false; // true: FD, false: 2.0

        // Receive objects
        private static MCP2518Dfs.CAN_RX_FIFO_CONFIG rxConfig = new();
        private static MCP2518Dfs.REG_CiFLTOBJ fObj = new();
        private static MCP2518Dfs.REG_CiMASK mObj = new();
        private static MCP2518Dfs.CAN_RX_FIFO_EVENT rxFlags;
        private static MCP2518Dfs.CAN_RX_MSGOBJ rxObj = new();
        private static byte[] rxd = new byte[MCP2518Dfs.MAX_DATA_BYTES];

        // Transmit objects
        private static MCP2518Dfs.CAN_TX_FIFO_CONFIG txConfig = new();
        private static MCP2518Dfs.CAN_TX_FIFO_EVENT txFlags = new();
        private static MCP2518Dfs.CAN_TX_MSGOBJ txObj = new();
        private static byte[] txd = new byte[MCP2518Dfs.MAX_DATA_BYTES];

        private static int MAX_TXQUEUE_ATTEMPTS = 50;

        // Transmit Channels
        private MCP2518Dfs.CAN_FIFO_CHANNEL APP_TX_FIFO = MCP2518Dfs.CAN_FIFO_CHANNEL.CAN_FIFO_CH2;

        // Receive Channels
        private MCP2518Dfs.CAN_FIFO_CHANNEL APP_RX_FIFO = MCP2518Dfs.CAN_FIFO_CHANNEL.CAN_FIFO_CH1;

        #endregion

        private ushort DRV_CANFDSPI_CalculateCRC16(byte[] data, ushort size)
        {
            ushort init = 0xFFFF;
            int index;

            for (int i = 0; i < size; i++)
            {
                index = (init >> 8) ^ data[i];
                init = (ushort)(((init << 8) ^ crc16_table[index]) & 0xFFFF);
            }

            return init;
        }

        public MCP2518(SpiDevice spi)
        {
            this.spi = spi;
        }
        public override byte Begin(MCP_BITTIME_SETUP speedSet, MCP_CLOCK clockSet = MCP_CLOCK.MCP2518FD_20MHz)
        {
            var speed = BittimeCompatToMcp2518fd(speedSet);
            if(speedSet > MCP_BITTIME_SETUP.CAN20_1000KBPS)
            {
                _flgFDF = true;
            }

            Init((MCP_BITTIME_SETUP)speed, (MCP2518Dfs.CAN_SYSCLK_SPEED)clockSet);

            return 0;
        }

        private byte Reset()
        {
            byte spiTransferError = 0;

            spiTransmitBuffer[0] = (byte)(cINSTRUCTION_RESET << 4);
            spiTransmitBuffer[1] = 0;

            Thread.Sleep(10);

            return spiTransferError;
        }

        private byte ReadByte(ushort address, out byte rxd)
        {
            byte spiTransferError = 0;

            // Compose command
            spiTransmitBuffer[0] =
                (byte)((cINSTRUCTION_READ << 4) + ((address >> 8) & 0xF));
            spiTransmitBuffer[1] = (byte)(address & 0xFF);

            spi.TransferSequential(spiTransmitBuffer, 0, 2, spiReceiveBuffer, 0, 1);

            rxd = spiReceiveBuffer[0];

            return spiTransferError;
        }

        private byte WriteByte(ushort address, byte txd)
        {
            byte spiTransferError = 0;

            //compose command
            // Compose command
            spiTransmitBuffer[0] =
                (byte)((cINSTRUCTION_WRITE << 4) + ((address >> 8) & 0xF));
            spiTransmitBuffer[1] = (byte)(address & 0xFF);
            spiTransmitBuffer[2] = txd;

            spi.Write(spiTransmitBuffer, 0, 3);

            return spiTransferError;
        }

        private byte ReadWord(ushort address, out int rxd)
        {
            int x;
            byte spiTransferError = 0;

            // Compose command
            spiTransmitBuffer[0] =
                (byte)((cINSTRUCTION_READ << 4) + ((address >> 8) & 0xF));
            spiTransmitBuffer[1] = (byte)(address & 0xFF);

            spi.TransferSequential(spiTransmitBuffer, 0, 2, spiReceiveBuffer, 0, 4);

            rxd = 0;
            for (int i = 0; i < 4; i++)
            {
                x = spiReceiveBuffer[i];
                rxd += x << (i * 8);
            }

            return spiTransferError;
        }

        private byte WriteWord(ushort address, int txd)
        {
            byte spiTransferError = 0;

            // Compose command
            spiTransmitBuffer[0] =
                (byte)((cINSTRUCTION_WRITE << 4) + ((address >> 8) & 0xF));
            spiTransmitBuffer[1] = (byte)(address & 0xFF);

            for (int i = 0; i < 4; i++)
            {
                spiTransmitBuffer[i + 2] = (byte)((txd >> (i * 8)) & 0xFF);
            }

            spi.Write(spiTransmitBuffer, 0, 6);

            return spiTransferError;
        }

        private byte ReadHalfWord(ushort address, out short rxd)
        {
            byte spiTransferError = 0;

            int x;

            // Compose command
            spiTransmitBuffer[0] =
                (byte)((cINSTRUCTION_READ << 4) + ((address >> 8) & 0xF));
            spiTransmitBuffer[1] = (byte)(address & 0xFF);

            spi.TransferSequential(spiTransmitBuffer, 0, 2, spiReceiveBuffer, 0, 2);

            rxd = 0;

            for (int i = 0; i < 2; i++)
            {
                x = spiReceiveBuffer[i];
                rxd += (short)(x << (i * 8));
            }

            return spiTransferError;
        }

        private byte WriteHalfWord(ushort address, short txd)
        {
            byte spiTransferError = 0;

            // Compose command
            spiTransmitBuffer[0] =
            (byte)((cINSTRUCTION_WRITE << 4) + ((address >> 8) & 0xF));
            spiTransmitBuffer[1] = (byte)(address & 0xFF);

            // Split word into 2 bytes and add them to buffer
            for (int i = 0; i < 2; i++)
            {
                spiTransmitBuffer[i + 2] = (byte)((txd >> (i * 8)) & 0xFF);
            }

            spi.Write(spiTransmitBuffer, 0, 4);

            return spiTransferError;
        }

        private byte ReadByteArray(ushort address, byte[] rxd, ushort nBytes)
        {
            ushort spiTransferSize = (ushort)(nBytes + 2);
            byte spiTransferError = 0;

            // Compose command
            spiTransmitBuffer[0] =
                (byte)((cINSTRUCTION_READ << 4) + ((address >> 8) & 0xF));
            spiTransmitBuffer[1] = (byte)(address & 0xFF);

            // Clear data
            for (int i = 2; i < spiTransferSize; i++)
            {
                spiTransmitBuffer[i] = 0;
            }

            spi.TransferSequential(spiTransmitBuffer, 0, 2, spiReceiveBuffer, 0, nBytes);

            for (int i = 0; i < nBytes; i++)
            {
                rxd[i] = spiReceiveBuffer[i];
            }

            return spiTransferError;
        }

        private byte WriteByteArray(ushort address, byte[] txd, ushort nBytes)
        {
            ushort spiTransferSize = (ushort)(nBytes + 2);
            byte spiTransferError = 0;

            // Compose command
            spiTransmitBuffer[0] =
                (byte)((cINSTRUCTION_WRITE << 4) + ((address >> 8) & 0xF));
            spiTransmitBuffer[1] = (byte)(address & 0xFF);
            // Add data
            for (int i = 2; i < spiTransferSize; i++)
            {
                spiTransmitBuffer[i] = txd[i - 2];
            }

            spi.Write(spiReceiveBuffer, 0, spiTransferSize);

            return spiTransferError;
        }

        private byte WriteByteSafe(ushort address, byte txd)
        {
            ushort crcResult = 0;
            byte spiTransferError = 0;

            // Compose command
            spiTransmitBuffer[0] =
                (byte)((cINSTRUCTION_WRITE_SAFE << 4) + ((address >> 8) & 0xF));
            spiTransmitBuffer[1] = (byte)(address & 0xFF);
            spiTransmitBuffer[2] = txd;

            crcResult = DRV_CANFDSPI_CalculateCRC16(spiTransmitBuffer, 3);
            spiTransmitBuffer[3] = (byte)((crcResult >> 8) & 0xFF);
            spiTransmitBuffer[4] = (byte)(crcResult & 0xFF);

            spi.Write(spiTransmitBuffer, 0, 5);

            return spiTransferError;
        }

        private byte WriteWordSafe(ushort address, int txd)
        {
            ushort crcResult = 0;
            byte spiTransferError = 0;

            // Compose command
            spiTransmitBuffer[0] =
                (byte)((cINSTRUCTION_WRITE_SAFE << 4) + ((address >> 8) & 0xF));
            spiTransmitBuffer[1] = (byte)(address & 0xFF);

            // Split word into 4 bytes and add them to buffer
            for (int i = 0; i < 4; i++)
            {
                spiTransmitBuffer[i + 2] = (byte)((txd >> (i * 8)) & 0xFF);
            }

            // Add CRC
            crcResult = DRV_CANFDSPI_CalculateCRC16(spiTransmitBuffer, 6);
            spiTransmitBuffer[6] = (byte)((crcResult >> 8) & 0xFF);
            spiTransmitBuffer[7] = (byte)(crcResult & 0xFF);

            spi.Write(spiTransmitBuffer, 0, 8);

            return spiTransferError;
        }

        private byte ReadByteArrayWithCRC(ushort address, byte[] rxd, ushort nBytes, bool fromRam, out bool crcIsCorrect)
        {
            ushort crcFromSpiSalve = 0;
            ushort crcAtController = 0;
            ushort spiTransferSize = (ushort)(nBytes + 5); // first two bytes for sending command & address, third for
            // size, last two bytes for CRC

            byte spiTransferError = 0;

            // Compose command
            spiTransmitBuffer[0] =
                (byte)((cINSTRUCTION_READ_CRC << 4) + ((address >> 8) & 0xF));
            spiTransmitBuffer[1] = (byte)(address & 0xFF);
            spiTransmitBuffer[2] = fromRam ? (byte)(nBytes >> 2) : (byte)nBytes;

            // Clear data
            for (int i = 3; i < nBytes + 2; i++)
            {
                spiTransmitBuffer[i + 3] = 0;
            }

            spi.TransferSequential(spiTransmitBuffer, 0, 3, spiReceiveBuffer, 0, spiTransferSize - 3);

            // Get CRC from controller
            crcFromSpiSalve = (ushort)((spiReceiveBuffer[nBytes + 2] << 8) + (spiReceiveBuffer[nBytes + 1]));

            // Use the receive buffer to calculate CRC
            // First three bytes need to be command
            spiReceiveBuffer[0] = spiTransmitBuffer[0];
            spiReceiveBuffer[1] = spiTransmitBuffer[1];
            spiReceiveBuffer[2] = spiTransmitBuffer[2];
            crcAtController = DRV_CANFDSPI_CalculateCRC16(spiReceiveBuffer, (ushort)(nBytes + 3));

            if (crcFromSpiSalve == crcAtController)
            {
                crcIsCorrect = true;
            }
            else
            {
                crcIsCorrect = false;
            }

            for (int i = 0; i < nBytes; i++)
            {
                rxd[i] = spiReceiveBuffer[i];
            }


            return spiTransferError;
        }

        private byte WriteByteArrayWithCRC(ushort address, byte[] txd, ushort nBytes, bool fromRam)
        {
            ushort crcResult = 0;
            ushort spiTransferSize = (ushort)(nBytes + 5);
            byte spiTransferError = 0;

            // Compose command
            spiTransmitBuffer[0] =
                (byte)((cINSTRUCTION_WRITE_CRC << 4) + ((address >> 8) & 0xF));
            spiTransmitBuffer[1] = (byte)(address & 0xFF);
            spiTransmitBuffer[2] = fromRam ? (byte)(nBytes >> 2) : (byte)nBytes;

            // Add data
            for (int i = 0; i < nBytes; i++)
            {
                spiTransmitBuffer[i + 3] = txd[i];
            }

            // Add CRC
            crcResult = DRV_CANFDSPI_CalculateCRC16(spiTransmitBuffer, (ushort)(spiTransferSize - 2));
            spiTransmitBuffer[spiTransferSize - 2] = (byte)((crcResult >> 8) & 0xFF);
            spiTransmitBuffer[spiTransferSize - 1] = (byte)(crcResult & 0xFF);

            spi.Write(spiTransmitBuffer, 0, spiTransferSize - 1);

            return spiTransferError;
        }

        private byte ReadWordArray(ushort address, int[] rxd, ushort nWords)
        {
            MCP2518Dfs.REG_t w = new();
            ushort spiTransferSize = (ushort)(nWords * 4 + 2);
            byte spiTransferError = 0;

            // Compose command
            spiTransmitBuffer[0] = (byte)((cINSTRUCTION_READ << 4) + ((address >> 8) & 0xF));
            spiTransmitBuffer[1] = (byte)(address & 0xFF);

            // Clear data
            for (int i = 2; i < spiTransferSize; i++)
            {
                spiTransmitBuffer[i] = 0;
            }

            spi.TransferSequential(spiTransmitBuffer, 0, 2, spiReceiveBuffer, 0, spiTransferSize - 2);

            int n = 0;
            for (int i = 0; i < nWords; i++)
            {
                w.word = 0;
                w.UpdateFromWord();
                for (int j = 0; j < 4; j++, n++)
                {
                    w.bytes[j] = spiReceiveBuffer[n];
                }
                w.UpdateFromBytes();
                rxd[i] = w.word;
            }

            return spiTransferError;
        }

        private byte WriteWordArray(ushort address, int[] txd, ushort nWords)
        {
            MCP2518Dfs.REG_t w = new();
            ushort spiTransferSize = (ushort)(nWords * 4 + 2);
            byte spiTransferError = 0;

            // Compose command
            spiTransmitBuffer[0] = (byte)((cINSTRUCTION_WRITE << 4) + ((address >> 8) & 0xF));
            spiTransmitBuffer[1] = (byte)(address & 0xFF);

            int n = 2;
            for (int i = 0; n < nWords; i++)
            {
                w.word = (int)txd[i];
                w.UpdateFromWord();
                for (int j = 0; j < 4; j++, n++)
                {
                    spiTransmitBuffer[n] = w.bytes[j];
                }
            }

            spi.Write(spiTransmitBuffer, 0, spiTransferSize);

            return spiTransferError;
        }

        private void EccEnable()
        {
            byte d = 0;

            // Read
            ReadByte(MCP2518Dfs.cREGADDR_ECCCON, out d);


            // Modify
            d |= 0x01;

            //Write
            WriteByte(MCP2518Dfs.cREGADDR_ECCCON, d);
        }

        private byte RamInit(byte d)
        {
            byte[] txd = new byte[96];
            uint k;
            byte spiTransferError = 0;

            // Prepare data
            for (k = 0; k < 96; k++)
            {
                txd[k] = d;
            }

            ushort a = MCP2518Dfs.cRAMADDR_START;

            for (k = 0; k < (MCP2518Dfs.cRAM_SIZE / SPI_DEFAULT_BUFFER_LENGTH); k++)
            {
                WriteByteArray(a, txd, (ushort)SPI_DEFAULT_BUFFER_LENGTH);
                a += (ushort)SPI_DEFAULT_BUFFER_LENGTH;
            }

            return spiTransferError;
        }

        private void ConfigureObjectReset(MCP2518Dfs.CAN_CONFIG config)
        {
            MCP2518Dfs.REG_CiCON ciCon = new();
            ciCon.word = (int)MCP2518Dfs.canControlResetValues[MCP2518Dfs.cREGADDR_CiCON / 4];
            ciCon.UpdateFromBytes();

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

        private void Configure(MCP2518Dfs.CAN_CONFIG config)
        {
            MCP2518Dfs.REG_CiCON ciCon = new();

            ciCon.word = (int)MCP2518Dfs.canControlResetValues[MCP2518Dfs.cREGADDR_CiCON / 4];
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

            WriteWord(MCP2518Dfs.cREGADDR_CiCON, ciCon.word);

        }

        private void TransmitChannelConfigureObjectReset(MCP2518Dfs.CAN_TX_FIFO_CONFIG config)
        {
            MCP2518Dfs.REG_CiFIFOCON ciFifoCon = new();
            ciFifoCon.word = (int)MCP2518Dfs.canFifoResetValues[0]; // 10010010100101010000
            ciFifoCon.UpdateFromWord();

            config.RTREnable = ciFifoCon.txBF.RTREnable;
            config.TxPriority = ciFifoCon.txBF.TxPriority;
            config.TxAttempts = ciFifoCon.txBF.TxAttempts;
            config.FifoSize = ciFifoCon.txBF.FifoSize;
            config.PayLoadSize = ciFifoCon.txBF.PayLoadSize;
        }

        private void TransmitChannelConfigure(MCP2518Dfs.CAN_FIFO_CHANNEL channel, MCP2518Dfs.CAN_TX_FIFO_CONFIG config)
        {
            ushort a = 0;

            // Setup FIFO
            MCP2518Dfs.REG_CiFIFOCON ciFifoCon = new();
            ciFifoCon.word = (int)MCP2518Dfs.canFifoResetValues[0];
            ciFifoCon.UpdateFromWord();

            ciFifoCon.txBF.TxEnable = 1;
            ciFifoCon.txBF.FifoSize = config.FifoSize;
            ciFifoCon.txBF.PayLoadSize = config.PayLoadSize;
            ciFifoCon.txBF.TxAttempts = config.TxAttempts;
            ciFifoCon.txBF.TxPriority = config.TxPriority;
            ciFifoCon.txBF.RTREnable = config.RTREnable;
            ciFifoCon.UpdateFromTxReg();

            a = (ushort)(MCP2518Dfs.cREGADDR_CiFIFOCON + ((ushort)channel * MCP2518Dfs.CiFIFO_OFFSET));

            WriteWord(a, ciFifoCon.word);
        }

        private void ReceiveChannelConfigureObjectReset(MCP2518Dfs.CAN_RX_FIFO_CONFIG config)
        {
            MCP2518Dfs.REG_CiFIFOCON ciFifoCon = new();
            ciFifoCon.word = (int)MCP2518Dfs.canFifoResetValues[0];
            ciFifoCon.UpdateFromWord();

            config.FifoSize = ciFifoCon.rxBF.FifoSize;
            config.PayLoadSize = ciFifoCon.rxBF.PayLoadSize;
            config.RxTimeStampEnable = ciFifoCon.rxBF.RxTimeStampEnable;
        }

        private void ReceiveChannelConfigure(MCP2518Dfs.CAN_FIFO_CHANNEL channel, MCP2518Dfs.CAN_RX_FIFO_CONFIG config)
        {
            ushort a = 0;

            if (channel == MCP2518Dfs.CAN_TXQUEUE_CH0)
            {
                return;
            }

            MCP2518Dfs.REG_CiFIFOCON ciFifoCon = new();
            ciFifoCon.word = (int)MCP2518Dfs.canFifoResetValues[0];
            ciFifoCon.UpdateFromWord();

            ciFifoCon.rxBF.TxEnable = 0;
            ciFifoCon.rxBF.FifoSize = config.FifoSize;
            ciFifoCon.rxBF.PayLoadSize = config.PayLoadSize;
            ciFifoCon.rxBF.RxTimeStampEnable = config.RxTimeStampEnable;
            ciFifoCon.UpdateFromRxReg();

            a = (ushort)(MCP2518Dfs.cREGADDR_CiFIFOCON + ((ushort)channel * MCP2518Dfs.CiFIFO_OFFSET));

            WriteWord(a, ciFifoCon.word);
        }

        private void FilterObjectConfigure(MCP2518Dfs.CAN_FILTER filter, MCP2518Dfs.CAN_FILTEROBJ_ID id)
        {
            ushort a = 0;
            MCP2518Dfs.REG_CiFLTOBJ fObj = new();

            fObj.word = 0;
            fObj.UpdateFromWord();

            fObj.bF = id;
            fObj.UpdateFromReg();

            a = (ushort)(MCP2518Dfs.cREGADDR_CiFLTOBJ + ((ushort)filter * MCP2518Dfs.CiFILTER_OFFSET));

            WriteWord(a, fObj.word);
        }

        private void FilterMaskConfigure(MCP2518Dfs.CAN_FILTER filter, MCP2518Dfs.CAN_MASKOBJ_ID mask)
        {
            ushort a = 0;
            MCP2518Dfs.REG_CiMASK mObj = new();

            mObj.word = 0;
            mObj.UpdateFromWord();

            mObj.bF = mask;
            mObj.UpdateFromReg();

            a = (ushort)(MCP2518Dfs.cREGADDR_CiMASK + ((ushort)filter * MCP2518Dfs.CiFILTER_OFFSET));
        }

        private void FilterToFifoLink(MCP2518Dfs.CAN_FILTER filter, MCP2518Dfs.CAN_FIFO_CHANNEL channel, bool enable)
        {
            ushort a = 0;
            MCP2518Dfs.REG_CiFLTCON_BYTE fCtrl = new();

            fCtrl.bF.Enable = enable ? (int)1 : 0;
            fCtrl.bF.BufferPointer = (int)channel;
            fCtrl.UpdateFromReg();

            a = (ushort)(MCP2518Dfs.cREGADDR_CiFLTCON + (ushort)filter);

            WriteByte(a, fCtrl.single_byte);
        }

        /*
        * bittime calculation code from
        *   https://github.com/pierremolinaro/acan2517FD
        *
        */

        private const ushort MAX_BRP = 256;
        private const ushort MAX_ARBITRATION_PHASE_SEGMENT_1 = 256;
        private const byte MAX_ARBITRATION_PHASE_SEGMENT_2 = 128;
        private const byte MAX_ARBITRATION_SJW = 128;
        private const ushort MAX_DATA_PHASE_SEGMENT_1 = 32;
        private const byte MAX_DATA_PHASE_SEGMENT_2 = 16;
        private const byte MAX_DATA_SJW = 16;

        private bool CalcBittime(uint inDesiredArbitrationBitRate, uint inTolerancePPM = 10000)
        {
            if (mDataBitRateFactor <= 1) // Single bit rate
            {
                uint maxTQCount = MAX_ARBITRATION_PHASE_SEGMENT_1 + MAX_ARBITRATION_PHASE_SEGMENT_2 + 1; // Setting for slowest bit rate
                uint BRP = MAX_BRP;
                uint smallestError = uint.MaxValue;
                uint bestBRP = 1; // Setting for highest bit rate
                uint bestTQCount = 4; // Setting for highest bit rate
                uint TQCount = mSysClock / inDesiredArbitrationBitRate / BRP;
                //--- Loop for finding best BRP and best TQCount
                while ((TQCount <= (MAX_ARBITRATION_PHASE_SEGMENT_1 + MAX_ARBITRATION_PHASE_SEGMENT_2 + 1)) && BRP > 0)
                {
                    //--- Compute error using TQCount
                    if ((TQCount >= 4) && TQCount <= maxTQCount)
                    {
                        uint error = mSysClock - inDesiredArbitrationBitRate * (TQCount + 1) * BRP - mSysClock; // error is always >= 0
                        if (error <= smallestError)
                        {
                            smallestError = error;
                            bestBRP = BRP;
                            bestTQCount = TQCount + 1;
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
                ulong ppm = (1000UL * 1000UL);
                mArbitrationBitRateClosedToDesiredRate = (diff * ppm) <= (((ulong)W) * inTolerancePPM);
            }
            else
            {
                uint maxDataTQCount = MAX_DATA_PHASE_SEGMENT_1 + MAX_DATA_PHASE_SEGMENT_2; // Setting for slowest bit rate
                uint desiredDataBitRate = inDesiredArbitrationBitRate * (mDataBitRateFactor);
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
                int TDCO = (int)(bestBRP * dataPS1); // According to DS20005678D, ??3.4.8 Page 20
                mTDCO = (sbyte)((TDCO > 63) ? 63 : (byte)TDCO);
                mDataPhaseSegment1 = (byte)dataPS1;
                mDataPhaseSegment2 = (byte)dataPS2;
                mDataSJW = mDataPhaseSegment2;
                uint arbitrationTQCount = bestDataTQCount * (mDataBitRateFactor);
                //--- Compute arbiration PS2 (1 <= PS2 <= 128)
                uint arbitrationPS2 = arbitrationTQCount / 5;
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
                ulong ppm = (1000UL * 1000UL);
                mArbitrationBitRateClosedToDesiredRate = (diff * ppm) <= (((ulong)W) * inTolerancePPM);
            }
            return mArbitrationBitRateClosedToDesiredRate;
        }

        private void BitTimeConfigureNominal()
        {
            MCP2518Dfs.REG_CiNBTCFG ciNbtcfg = new();

            ciNbtcfg.word = (int)MCP2518Dfs.canControlResetValues[MCP2518Dfs.cREGADDR_CiNBTCFG / 4];
            ciNbtcfg.UpdateFromWord();

            // Arbitration Bit rate
            ciNbtcfg.bF.BRP = mBitRatePrescaler - 1;
            ciNbtcfg.bF.TSEG1 = mArbitrationPhaseSegment1 - 1;
            ciNbtcfg.bF.TSEG2 = mArbitrationPhaseSegment2 - 1;
            ciNbtcfg.bF.SJW = mArbitrationSJW - 1;

            ciNbtcfg.UpdateFromReg();

            WriteWord(MCP2518Dfs.cREGADDR_CiNBTCFG, ciNbtcfg.word);
        }

        private void BitTimeConfigureData(MCP2518Dfs.CAN_SSP_MODE sspMode)
        {
            MCP2518Dfs.REG_CiDBTCFG ciDbtcfg = new();
            MCP2518Dfs.REG_CiTDC ciTdc = new();

            // Write Bit time registers
            ciDbtcfg.word = (int)MCP2518Dfs.canControlResetValues[MCP2518Dfs.cREGADDR_CiDBTCFG / 4];
            ciDbtcfg.UpdateFromWord();

            ciDbtcfg.bF.BRP = mBitRatePrescaler - 1;
            ciDbtcfg.bF.TSEG1 = mDataPhaseSegment1 - 1;
            ciDbtcfg.bF.TSEG2 = mDataPhaseSegment2 - 1;
            ciDbtcfg.bF.SJW = mDataSJW - 1;
            ciDbtcfg.UpdateFromReg();

            WriteWord(MCP2518Dfs.cREGADDR_CiDBTCFG, ciDbtcfg.word);

            // Configure Bit time and sample point, SSP
            ciTdc.word = (int)MCP2518Dfs.canControlResetValues[MCP2518Dfs.cREGADDR_CiTDC / 4];
            ciTdc.UpdateFromWord();

            ciTdc.bF.TDCMode = (int)sspMode;
            ciTdc.bF.TDCOffset = mTDCO;
            ciTdc.UpdateFromReg();

            WriteWord(MCP2518Dfs.cREGADDR_CiTDC, ciTdc.word);
        }

        private void BitTimeConfigure(MCP_BITTIME_SETUP speedset, MCP2518Dfs.CAN_SSP_MODE sspMode, MCP2518Dfs.CAN_SYSCLK_SPEED clk)
        {
            // Decode bitrate
            mDesiredArbitrationBitRate = (uint)((uint)speedset & 0xFFFFFUL);
            mDataBitRateFactor = (byte)(((uint)speedset >> 24) & 0xFF);

            // Decode clk
            switch (clk)
            {
                case MCP2518Dfs.CAN_SYSCLK_SPEED.CAN_SYSCLK_10M:
                    mSysClock = (uint)(10UL * 1000UL * 1000UL);
                    break;
                case MCP2518Dfs.CAN_SYSCLK_SPEED.CAN_SYSCLK_20M:
                    mSysClock = (uint)(20UL * 1000UL * 1000UL);
                    break;
                case MCP2518Dfs.CAN_SYSCLK_SPEED.CAN_SYSCLK_40M:
                default:
                    mSysClock = (uint)(40UL * 1000UL * 1000UL);
                    break;
            }

            CalcBittime(mDesiredArbitrationBitRate);
            BitTimeConfigureNominal();
            BitTimeConfigureData(sspMode);
        }

        private void GpioModeConfigure(MCP2518Dfs.GPIO_PIN_MODE gpio0, MCP2518Dfs.GPIO_PIN_MODE gpio1)
        {
            // Read
            ushort a = (ushort)(MCP2518Dfs.cREGADDR_IOCON + 3);
            MCP2518Dfs.REG_IOCON iocon = new();
            iocon.word = 0;
            iocon.UpdateFromWord();

            ReadByte(a, out iocon.bytes[3]);
            iocon.UpdateFromBytes();

            // Modify
            iocon.bF.PinMode0 = (int)gpio0;
            iocon.bF.PinMode1 = (int)gpio1;
            iocon.UpdateFromReg();

            WriteByte(a, iocon.bytes[3]);
        }

        private void TransmitChannelEventEnable(MCP2518Dfs.CAN_FIFO_CHANNEL channel, MCP2518Dfs.CAN_TX_FIFO_EVENT flags)
        {
            // Read Interrupt Enables
            ushort a = (ushort)(MCP2518Dfs.cREGADDR_CiFIFOCON + (ushort)((ushort)channel + MCP2518Dfs.CiFIFO_OFFSET));
            MCP2518Dfs.REG_CiFIFOCON ciFifoCon = new();
            ciFifoCon.word = 0;
            ciFifoCon.UpdateFromWord();

            ReadByte(a, out ciFifoCon.bytes[0]);
            ciFifoCon.UpdateFromBytes();

            // Modify
            ciFifoCon.bytes[0] |= (byte)(flags & MCP2518Dfs.CAN_TX_FIFO_EVENT.CAN_TX_FIFO_ALL_EVENTS);
            ciFifoCon.UpdateFromBytes();

            // Write
            WriteByte(a, ciFifoCon.bytes[0]);
        }

        private void ReceiveChannelEventEnable(MCP2518Dfs.CAN_FIFO_CHANNEL channel, MCP2518Dfs.CAN_RX_FIFO_EVENT flags)
        {
            ushort a = 0;

            if (channel == MCP2518Dfs.CAN_TXQUEUE_CH0)
            {
                return;
            }

            // Read Interrupt Enables
            a = (ushort)(MCP2518Dfs.cREGADDR_CiFIFOCON + ((uint)channel * MCP2518Dfs.CiFIFO_OFFSET));
            MCP2518Dfs.REG_CiFIFOCON ciFifoCon = new();
            ciFifoCon.word = 0;
            ciFifoCon.UpdateFromWord();

            ReadByte(a, out ciFifoCon.bytes[0]);
            ciFifoCon.UpdateFromBytes();

            //Modify
            ciFifoCon.bytes[0] |= (byte)(flags & MCP2518Dfs.CAN_RX_FIFO_EVENT.CAN_RX_FIFO_ALL_EVENTS);

            //Write
            WriteByte(a, ciFifoCon.bytes[0]);
        }

        private void ModuleEventEnable(MCP2518Dfs.CAN_MODULE_EVENT flags)
        {
            ushort a = 0;

            // Read Interrupt Enables
            a = MCP2518Dfs.cREGADDR_CiINTENABLE;
            MCP2518Dfs.REG_CiINTENABLE intEnables = new();
            intEnables.word = 0;
            intEnables.UpdateFromWord();

            ReadHalfWord(a, out intEnables.word);
            intEnables.UpdateFromWord();

            // Modify
            intEnables.word |= (short)(flags & MCP2518Dfs.CAN_MODULE_EVENT.CAN_ALL_EVENTS);

            // Write
            WriteHalfWord(a, intEnables.word);
        }

        private void OperationModeSelect(CAN_OPERATION_MODE opMode)
        {
            byte d = 0;

            ReadByte((ushort)(MCP2518Dfs.cREGADDR_CiCON + 3), out d);

            d &= 0b11111000;
            d |= (byte)opMode;

            WriteByte((ushort)(MCP2518Dfs.cREGADDR_CiCON + 3), d);
        }

        private CAN_OPERATION_MODE OperationModeGet()
        {
            byte d;
            CAN_OPERATION_MODE mode = CAN_OPERATION_MODE.CAN_INVALID_MODE;

            // Read Mode
            ReadByte((ushort)(MCP2518Dfs.cREGADDR_CiCON + 2), out d);

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

        private void TransmitChannelEventGet(MCP2518Dfs.CAN_FIFO_CHANNEL channel, out MCP2518Dfs.CAN_TX_FIFO_EVENT flags)
        {
            ushort a;

            // Read Interrupt flags
            MCP2518Dfs.REG_CiFIFOSTA ciFifoSta = new();
            ciFifoSta.word = 0;
            ciFifoSta.UpdateFromWord();

            a = (ushort)(MCP2518Dfs.cREGADDR_CiFIFOSTA + ((ushort)channel * MCP2518Dfs.CiFIFO_OFFSET));

            ReadByte(a, out ciFifoSta.bytes[0]);
            ciFifoSta.UpdateFromBytes();

            //Update data
            flags = (MCP2518Dfs.CAN_TX_FIFO_EVENT)ciFifoSta.bytes[0] & MCP2518Dfs.CAN_TX_FIFO_EVENT.CAN_TX_FIFO_ALL_EVENTS;
        }

        private void ErrorCountStateGet(out byte tec, out byte rec, out MCP2518Dfs.CAN_ERROR_STATE flags)
        {
            // Read Error
            ushort a = MCP2518Dfs.cREGADDR_CiTREC;
            MCP2518Dfs.REG_CiTREC ciTrec = new();
            ciTrec.word = 0;
            ciTrec.UpdateFromWord();

            ReadWord(a, out ciTrec.word);
            ciTrec.UpdateFromWord();

            // Update Data
            tec = ciTrec.bytes[0];
            rec = ciTrec.bytes[1];
            flags = ((MCP2518Dfs.CAN_ERROR_STATE)ciTrec.bytes[3] & MCP2518Dfs.CAN_ERROR_STATE.CAN_ERROR_ALL);
        }


        // *****************************************************************************
        // *****************************************************************************
        // Miscellaneous

        byte DlcToDataBytes(CAN_DLC dlc)
        {
            byte dataBytesInObject = 0;

            if (dlc < CAN_DLC.CAN_DLC_12)
            {
                dataBytesInObject = (byte)dlc;
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

        private void TransmitChannelLoad(MCP2518Dfs.CAN_FIFO_CHANNEL channel, MCP2518Dfs.CAN_TX_MSGOBJ txObj, byte[] txd, uint txdNumBytes, bool flush) 
        {
            ushort a;
            int[] fifoReg = new int[3];
            uint dataBytesInObject;
            byte i = 0;

            MCP2518Dfs.REG_CiFIFOCON ciFifoCon = new();
            MCP2518Dfs.REG_CiFIFOUA ciFifoUa = new();

            // Get FIFO registers
            a = (ushort)(MCP2518Dfs.cREGADDR_CiFIFOCON + ((ushort)channel * MCP2518Dfs.CiFIFO_OFFSET));

            ReadWordArray(a, fifoReg, 3);
            ciFifoCon.word = fifoReg[0];
            ciFifoCon.UpdateFromWord();

            dataBytesInObject = DlcToDataBytes((CAN_DLC)txObj.bF.ctrl.DLC);
            if(dataBytesInObject < txdNumBytes)
            {
                return;
            }

            // Get Address
            ciFifoUa.word = fifoReg[2];
            ciFifoUa.UpdateFromWord();

            a = (ushort)ciFifoUa.bF.UserAddress;
            a += MCP2518Dfs.cRAMADDR_START;

            byte[] txBuffer = new byte[MCP2518Dfs.MAX_MSG_SIZE];

            for(i = 0; i < 8; i++)
            {
                txBuffer[i] = txObj.bytes[i];
            }

            for (i = 0; i < txdNumBytes; i++)
            {
                txBuffer[i + 8] = txd[i];
            }

            ushort n = 0;
            byte j = 0;
            

            if(txdNumBytes % 4 > 0)
            {
                n = (ushort)(4 - (txdNumBytes % 4));
                i = (byte)(txdNumBytes + 8);

                for (j = 0; j < n; j++)
                {
                    txBuffer[i + 8 + j] = 0;
                }
            }

            WriteByteArray(a, txBuffer, (ushort)(txdNumBytes + 8 + n));

            // Set UINC and TXREQ
            TransmitChannelUpdate(channel, flush);
        }

        private void ReceiveChannelEventGet(MCP2518Dfs.CAN_FIFO_CHANNEL channel, out MCP2518Dfs.CAN_RX_FIFO_EVENT flags)
        {
            ushort a;

            if(channel == MCP2518Dfs.CAN_TXQUEUE_CH0)
            {
                flags = MCP2518Dfs.CAN_RX_FIFO_EVENT.CAN_RX_FIFO_NO_EVENT;
                return;
            }

            // Read Interrupt fkafs
            MCP2518Dfs.REG_CiFIFOSTA ciFifoSta = new();
            ciFifoSta.word = 0;
            ciFifoSta.UpdateFromWord();

            a = (ushort)(MCP2518Dfs.cREGADDR_CiFIFOSTA + ((ushort)channel * MCP2518Dfs.CiFIFO_OFFSET));

            ReadByte(a, out ciFifoSta.bytes[0]);

            flags = (MCP2518Dfs.CAN_RX_FIFO_EVENT)ciFifoSta.bytes[0] & MCP2518Dfs.CAN_RX_FIFO_EVENT.CAN_RX_FIFO_ALL_EVENTS;
        }

        private void ReceiveMessageGet(MCP2518Dfs.CAN_FIFO_CHANNEL channel, MCP2518Dfs.CAN_RX_MSGOBJ rxObj, byte[] rxd, byte nBytes)
        {
            byte n = 0, i = 0;
            ushort a;
            int[] fifoReg = new int[3];
            MCP2518Dfs.REG_CiFIFOCON ciFifoCon = new();
            MCP2518Dfs.REG_CiFIFOUA ciFifoUa = new();

            // Get Fifo registers
            a = (ushort)(MCP2518Dfs.cREGADDR_CiFIFOCON + ((ushort)channel * MCP2518Dfs.CiFIFO_OFFSET));

            ReadWordArray(a, fifoReg, 3);

            ciFifoCon.word = fifoReg[0];
            ciFifoCon.UpdateFromWord();
            ciFifoCon.txBF.TxEnable = 0;

            // Get address
            ciFifoUa.word = fifoReg[2];
            ciFifoUa.UpdateFromWord();

            a = (ushort)ciFifoUa.bF.UserAddress;
            a += MCP2518Dfs.cRAMADDR_START;

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
            byte[] ba = new byte[MCP2518Dfs.MAX_MSG_SIZE];

            if(n > MCP2518Dfs.MAX_MSG_SIZE)
            {
                n = (byte)MCP2518Dfs.MAX_MSG_SIZE;
            }

            ReadByteArray(a, ba, n);

            // Assagin message header
            MCP2518Dfs.REG_t myReg = new();

            myReg.bytes[0] = ba[0];
            myReg.bytes[1] = ba[1];
            myReg.bytes[2] = ba[2];
            myReg.bytes[3] = ba[3];
            myReg.UpdateFromBytes();
            rxObj.word[0] = myReg.word;

            myReg.bytes[0] = ba[4];
            myReg.bytes[1] = ba[5];
            myReg.bytes[2] = ba[6];
            myReg.bytes[3] = ba[7];
            myReg.UpdateFromBytes();
            rxObj.word[1] = myReg.word;

            if (ciFifoCon.rxBF.RxTimeStampEnable > 0)
            {
                myReg.bytes[0] = ba[8];
                myReg.bytes[1] = ba[9];
                myReg.bytes[2] = ba[10];
                myReg.bytes[3] = ba[11];
                myReg.UpdateFromBytes();
                rxObj.word[2] = myReg.word;

                // Assign message data
                for (i = 0; i < nBytes; i++)
                {
                    rxd[i] = ba[i + 12];
                }
            }
            else
            {
                rxObj.word[2] = 0;

                // Assign message data
                for (i = 0; i < nBytes; i++)
                {
                    rxd[i] = ba[i + 8];
                }
            }
            rxObj.UpdateFromWord();

            // UINC channel
            ReceiveChannelUpdate(channel);
        }

        private void ReceiveChannelUpdate(MCP2518Dfs.CAN_FIFO_CHANNEL channel)
        {
            ushort a;
            MCP2518Dfs.REG_CiFIFOCON ciFifoCon = new();
            ciFifoCon.word = 0;
            ciFifoCon.UpdateFromWord();

            // Set UINC
            a = (ushort)(MCP2518Dfs.cREGADDR_CiFIFOCON + ((ushort)channel * MCP2518Dfs.CiFIFO_OFFSET) + 
                1); // Byte that contains FRESET
            ciFifoCon.rxBF.UINC = 1;
            ciFifoCon.UpdateFromRxReg();

            WriteByte(a, ciFifoCon.bytes[1]);
        }

        private void TransmitChannelUpdate(MCP2518Dfs.CAN_FIFO_CHANNEL channel, bool flush)
        {
            ushort a;
            MCP2518Dfs.REG_CiFIFOCON ciFifoCon = new();

            // Set UINC
            a = (ushort)(MCP2518Dfs.cREGADDR_CiFIFOCON + ((ushort)channel * MCP2518Dfs.CiFIFO_OFFSET) +
                1); // Byte that contains FRESET
            ciFifoCon.word = 0;
            ciFifoCon.UpdateFromWord();
            ciFifoCon.txBF.UINC = 1;
            ciFifoCon.UpdateFromTxReg();

            // Set TXREQ
            if (flush)
            {
                ciFifoCon.txBF.TxRequest = 1;
                ciFifoCon.UpdateFromTxReg();
            }

            WriteByte(a, ciFifoCon.bytes[1]);
        }

        private void ReceiveChannelStatusGet(MCP2518Dfs.CAN_FIFO_CHANNEL channel, out MCP2518Dfs.CAN_RX_FIFO_STATUS status)
        {
            ushort a;
            MCP2518Dfs.REG_CiFIFOCON ciFifoCon = new();

            // Read
            ciFifoCon.word = 0;
            ciFifoCon.UpdateFromWord();

            a = (ushort)(MCP2518Dfs.cREGADDR_CiFIFOCON + ((ushort)channel * MCP2518Dfs.CiFIFO_OFFSET));
            ReadByte(a, out ciFifoCon.bytes[0]);

            ciFifoCon.UpdateFromBytes();

            status = (MCP2518Dfs.CAN_RX_FIFO_STATUS)(ciFifoCon.bytes[0] & 0x0F);
        }

        private void ErrorStateGet(out MCP2518Dfs.CAN_ERROR_STATE flags)
        {
            // Read Error state
            byte f = 0;

            ReadByte((ushort)(MCP2518Dfs.cREGADDR_CiTREC + 2), out f);

            // Update data
            flags = (MCP2518Dfs.CAN_ERROR_STATE)(f * (int)MCP2518Dfs.CAN_ERROR_STATE.CAN_ERROR_ALL);
        }

        private void ModuleEventRxCodeGet(out MCP2518Dfs.CAN_RXCODE rxCode)
        {
            byte rxCodeByte;

            ReadByte((ushort)(MCP2518Dfs.cREGADDR_CiVEC + 3), out rxCodeByte);

            // Decode data
            // 0x40 = "no interrupt" (CAN_FIFO_CIVEC_NOINTERRUPT)
            if(rxCodeByte < (byte)MCP2518Dfs.CAN_RXCODE.CAN_RXCODE_TOTAL_CHANNELS || rxCodeByte == (byte)MCP2518Dfs.CAN_RXCODE.CAN_RXCODE_NO_INT)
            {
                rxCode = (MCP2518Dfs.CAN_RXCODE)rxCodeByte;
            }
            else
            {
                rxCode = MCP2518Dfs.CAN_RXCODE.CAN_RXCODE_RESERVED; // shouldn't get here
            }
        }

        private void ModuleEventTxCodeGet(out MCP2518Dfs.CAN_TXCODE txCode)
        {
            ushort a;
            byte txCodeByte;

            // Read
            a = (ushort)(MCP2518Dfs.cREGADDR_CiVEC + 2);

            ReadByte(a, out txCodeByte);

            // Decode data
            // 0x40 = "no interrupt" (CAN_FIFO_CIVEC_NOINTERRUPT)
            if ((txCodeByte < (byte)MCP2518Dfs.CAN_TXCODE.CAN_TXCODE_TOTAL_CHANNELS) ||
            (txCodeByte == (byte)MCP2518Dfs.CAN_TXCODE.CAN_TXCODE_NO_INT))
            {
                txCode = (MCP2518Dfs.CAN_TXCODE)txCodeByte;
            }
            else
            {
                txCode = MCP2518Dfs.CAN_TXCODE.CAN_TXCODE_RESERVED; // shouldn't get here
            }
        }

        private void TransmitChannelEventAttemptClear(MCP2518Dfs.CAN_FIFO_CHANNEL channel)
        {
            ushort a;

            // Read Interrup Enables
            a = (ushort)(MCP2518Dfs.cREGADDR_CiFIFOSTA + ((ushort)channel * MCP2518Dfs.CiFIFO_OFFSET));
            MCP2518Dfs.REG_CiFIFOSTA ciFifoSta = new();
            ciFifoSta.word = 0;
            ciFifoSta.UpdateFromWord();

            ReadByte(a, out ciFifoSta.bytes[0]);
            ciFifoSta.UpdateFromBytes();

            // Modify
            ciFifoSta.bytes[0] &= 0b11101111;
            ciFifoSta.UpdateFromBytes();

            // Write
            WriteByte(a, ciFifoSta.bytes[0]);
        }

        public void LowPowerModeEnable()
        {
            byte d;

            // Read
            ReadByte(MCP2518Dfs.cREGADDR_OSC, out d);

            // Modify
            d |= 0x08;

            // Write
            WriteByte(MCP2518Dfs.cREGADDR_OSC, d);
        }

        private void LowPowerModeDisable()
        {
            byte d;

            // Read
            ReadByte(MCP2518Dfs.cREGADDR_OSC, out d);

            // Modify
            d |= 0xF7;

            // Write
            WriteByte(MCP2518Dfs.cREGADDR_OSC, d);
        }

        private void TransmitMessageQueue()
        {
            byte attempts = (byte)MAX_TXQUEUE_ATTEMPTS;
            MCP2518Dfs.CAN_ERROR_STATE errorFlags;
            byte tec, rec;

            // Check if FIFO is not full
            do
            {
                TransmitChannelEventGet(APP_TX_FIFO, out txFlags);
                if (attempts == 0)
                {
                    ErrorCountStateGet(out tec, out rec, out errorFlags);
                    return;
                }
                attempts--;
            } while (!((txFlags & MCP2518Dfs.CAN_TX_FIFO_EVENT.CAN_TX_FIFO_NOT_FULL_EVENT) > 0));

            // Load message and transmit
            byte n = (byte)DlcToDataBytes((CAN_DLC)txObj.bF.ctrl.DLC);
            TransmitChannelLoad(APP_TX_FIFO, txObj, txd, n, true);
        }


        private void SendMsg(byte[] buf, byte len, ulong id, byte ext, byte rtr, bool wait_sent)
        {
            byte n;
            int i;

            // Configure message object
            txObj.word[0] = 0;
            txObj.word[1] = 0;
            txObj.word[2] = 0;
            txObj.UpdateFromWord();

            txObj.bF.ctrl.RTR = rtr > 0 ? 1 : 0;
            if (rtr > 0 && len > (int)CAN_DLC.CAN_DLC_8)
            {
                len = (byte)CAN_DLC.CAN_DLC_8;
            }

            txObj.bF.ctrl.DLC = len;

            txObj.bF.ctrl.IDE = ext > 0 ? 1 : 0;
            if (ext > 0)
            {
                txObj.bF.id.SID = (int)((id >> 18) & 0x7FF);
                txObj.bF.id.EID = (int)(id & 0x3FFFF);
            }
            else
            {
                txObj.bF.id.SID = (int)id;
            }

            txObj.bF.ctrl.BRS = 1;
            txObj.bF.ctrl.FDF = _flgFDF ? 1 : 0;

            txObj.UpdateFromReg();


            n = (byte)DlcToDataBytes((CAN_DLC)txObj.bF.ctrl.DLC);
            // Prepare data
            for (i = 0; i < n; i++)
            {
                txd[i] = buf[i];
            }

            TransmitMessageQueue();
        }

        private void ReceiveMsg()
        {
            ReceiveChannelEventGet(APP_RX_FIFO, out rxFlags);

            if((rxFlags & MCP2518Dfs.CAN_RX_FIFO_EVENT.CAN_RX_FIFO_NOT_EMPTY_EVENT) > 0)
            {
                ReceiveMessageGet(APP_RX_FIFO, rxObj, rxd, 8);
            }
        }

        private uint BittimeCompatToMcp2518fd(MCP_BITTIME_SETUP speedset)
        {
            uint r = 0;

            if((uint)speedset > 0x100)
            {
                return (uint)speedset;
            }

            switch (speedset)
            {
                case MCP_BITTIME_SETUP.CAN20_5KBPS: r = CANFD.BITRATE(5000, 0); break;
                case MCP_BITTIME_SETUP.CAN20_10KBPS: r = CANFD.BITRATE(10000, 0); break;
                case MCP_BITTIME_SETUP.CAN20_20KBPS: r = CANFD.BITRATE(20000, 0); break;
                case MCP_BITTIME_SETUP.CAN20_25KBPS: r = CANFD.BITRATE(25000, 0); break;
                case MCP_BITTIME_SETUP.CAN20_31K25BPS: r = CANFD.BITRATE(31250, 0); break;
                case MCP_BITTIME_SETUP.CAN20_33KBPS: r = CANFD.BITRATE(33000, 0); break;
                case MCP_BITTIME_SETUP.CAN20_40KBPS: r = CANFD.BITRATE(40000, 0); break;
                case MCP_BITTIME_SETUP.CAN20_50KBPS: r = CANFD.BITRATE(50000, 0); break;
                case MCP_BITTIME_SETUP.CAN20_80KBPS: r = CANFD.BITRATE(80000, 0); break;
                case MCP_BITTIME_SETUP.CAN20_83K3BPS: r = CANFD.BITRATE(83300, 0); break;
                case MCP_BITTIME_SETUP.CAN20_95KBPS: r = CANFD.BITRATE(95000, 0); break;
                case MCP_BITTIME_SETUP.CAN20_100KBPS: r = CANFD.BITRATE(100000, 0); break;
                case MCP_BITTIME_SETUP.CAN20_125KBPS: r = CANFD.BITRATE(125000, 0); break;
                case MCP_BITTIME_SETUP.CAN20_200KBPS: r = CANFD.BITRATE(200000, 0); break;
                case MCP_BITTIME_SETUP.CAN20_250KBPS: r = CANFD.BITRATE(250000, 0); break;
                default:
                case MCP_BITTIME_SETUP.CAN20_500KBPS: r = CANFD.BITRATE(500000, 0); break;
                case MCP_BITTIME_SETUP.CAN20_666KBPS: r = CANFD.BITRATE(666000, 0); break;
                case MCP_BITTIME_SETUP.CAN20_800KBPS: r = CANFD.BITRATE(800000, 0); break;
                case MCP_BITTIME_SETUP.CAN20_1000KBPS: r = CANFD.BITRATE(1000000, 0); break;
            }

            return r;
        }

        private void Init(MCP_BITTIME_SETUP speedset, MCP2518Dfs.CAN_SYSCLK_SPEED clock)
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
            txConfig.PayLoadSize = (int)MCP2518Dfs.CAN_FIFO_PLSIZE.CAN_PLSIZE_64;
            txConfig.TxPriority = 1;
            TransmitChannelConfigure(APP_TX_FIFO, txConfig);

            // Setup RX FIFO
            ReceiveChannelConfigureObjectReset(rxConfig);
            rxConfig.FifoSize = 15;
            rxConfig.PayLoadSize = (int)MCP2518Dfs.CAN_FIFO_PLSIZE.CAN_PLSIZE_64; ;
            ReceiveChannelConfigure(APP_RX_FIFO, rxConfig);

            for (int i = 0; i < 32; i++)
                FilterDisable((MCP2518Dfs.CAN_FILTER)i);          // disable all filter

            // Link FIFO to Filter
            FilterToFifoLink(MCP2518Dfs.CAN_FILTER.CAN_FILTER0, APP_RX_FIFO, true);

            // Setup Bit Time
            BitTimeConfigure(speedset, MCP2518Dfs.CAN_SSP_MODE.CAN_SSP_MODE_AUTO, clock);

            // Setup Transmit and Receive Interrupts
            GpioModeConfigure(MCP2518Dfs.GPIO_PIN_MODE.GPIO_MODE_INT, MCP2518Dfs.GPIO_PIN_MODE.GPIO_MODE_INT);

            ReceiveChannelEventEnable(APP_RX_FIFO, MCP2518Dfs.CAN_RX_FIFO_EVENT.CAN_RX_FIFO_NOT_EMPTY_EVENT);
            ModuleEventEnable((MCP2518Dfs.CAN_MODULE_EVENT.CAN_TX_EVENT | MCP2518Dfs.CAN_MODULE_EVENT.CAN_RX_EVENT));
            SetMode(mcpMode);
        }

        public override void EnableTxInterrupt(bool enable = true)
        {
            if (enable)
            {
                ModuleEventEnable(MCP2518Dfs.CAN_MODULE_EVENT.CAN_TX_EVENT);
            }
        }

        public void FilterDisable(MCP2518Dfs.CAN_FILTER filter)
        {
            ushort a;
            MCP2518Dfs.REG_CiFLTCON_BYTE fCtrl = new();

            // Read
            a = (ushort)(MCP2518Dfs.cREGADDR_CiFLTCON + filter);
            ReadByte(a, out fCtrl.single_byte);
            fCtrl.UpdateFromByte();

            // Modify
            fCtrl.bF.Enable = 0;
            fCtrl.UpdateFromReg();

            WriteByte(a, fCtrl.single_byte);
        }

        public void InitFiltMask(MCP2518Dfs.CAN_FILTER num, bool ext, ulong f, ulong m)
        {
            OperationModeSelect(CAN_OPERATION_MODE.CAN_CONFIGURATION_MODE);

            FilterDisable(num);

            MCP2518Dfs.CAN_FILTEROBJ_ID fObj = new();
            if (ext)
            {
                fObj.SID = 0;
                fObj.SID11 = (int)(f >> 18);
                fObj.EID = (int)(f & 0x3ffff);
                fObj.EXIDE = 1;
            }
            else
            {
                fObj.SID = (int)f;
                fObj.SID11 = 0;
                fObj.EID = 0;
                fObj.EXIDE = 0;
            }
            
            FilterObjectConfigure(num, fObj);

            MCP2518Dfs.CAN_MASKOBJ_ID mObj = new();

            if (ext)
            {
                mObj.MSID = 0;
                mObj.MSID11 = (int)(m >> 18);
                mObj.MEID = (int)(m & 0x3ffff);
                mObj.MIDE = 1;
            }
            else
            {
                mObj.MSID = (int)m;
                mObj.MSID11 = 0;
                mObj.MEID = 0;
                mObj.MIDE = 1;
            }
            FilterMaskConfigure(num, mObj);

            FilterToFifoLink(num, APP_RX_FIFO, true);

            OperationModeSelect(mcpMode);
        }

        public override void SetSleepWakeup(bool enable)
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

        public override byte Sleep()
        {
            if(GetMode() != (CAN_OPERATION_MODE)0x01)
            {
                OperationModeSelect(CAN_OPERATION_MODE.CAN_SLEEP_MODE);
                return 0;
            }
            else
            {
                return CAN_OK;
            }
        }
        public override byte Wake()
        {
            CAN_OPERATION_MODE currMode = GetMode();
            if(currMode != mcpMode)
            {
                OperationModeSelect(mcpMode);
                return 0;
            }
            else
            {
                return 0;
            }
        }


        public override byte SetMode(CAN_OPERATION_MODE opMode)
        {
            if(opMode != CAN_OPERATION_MODE.CAN_SLEEP_MODE)
            {
                mcpMode = opMode;
            }
            OperationModeSelect(opMode);

            return 0;
        }

        public CAN_OPERATION_MODE GetMode()
        {
            return OperationModeGet();
        }

        public override void ReadMsgBufID(MCP2518Dfs.CAN_RX_FIFO_EVENT status, out ulong id, out byte ext, out byte rtr, out byte len, byte[] buf)
        {
            ReadMsgBufID(out len, buf);

            id = can_id;
            ext = ext_flag;
            rtr = this.rtr;
        }

        /* wrapper */
        public override void ReadMsgBufID(out ulong ID, out byte len, byte[] buf)
        {
            ReadMsgBufID(ReadRxTxStatus(), out ID, out ext_flag, out rtr, out len, buf);
        }
        public override void ReadMsgBuf(out byte len, byte[] buf)
        {
            ReadMsgBufID(ReadRxTxStatus(), out can_id, out ext_flag, out rtr, out len, buf);
        }

        public override byte CheckReceive()
        {
            MCP2518Dfs.CAN_RX_FIFO_STATUS status;

            ReceiveChannelStatusGet(APP_RX_FIFO, out status);

            byte res = (byte)(status & MCP2518Dfs.CAN_RX_FIFO_STATUS.CAN_RX_FIFO_NOT_EMPTY);

            return res;
        }

        public override void CheckError(out MCP2518Dfs.CAN_ERROR_STATE err)
        {
            ErrorStateGet(out err);
        }

        public void ReadMsgBufID(out byte len, byte[] buf)
        {
            ReceiveMessageGet(APP_RX_FIFO, rxObj, rxd, (byte)MCP2518Dfs.MAX_DATA_BYTES);
            ext_flag = (byte)rxObj.bF.ctrl.IDE;
            can_id = ext_flag > 0 ? (ulong)(rxObj.bF.id.EID | (rxObj.bF.id.SID << 18)) : (ulong)rxObj.bF.id.SID;
            rtr = (byte)rxObj.bF.ctrl.RTR;

            byte n = DlcToDataBytes((CAN_DLC)rxObj.bF.ctrl.DLC);
            len = n;

            for (int i = 0; i < n; i++)
            {
                buf[i] = rxd[i];
            }
        }

        public override byte TrySendMsgBuf(ulong id, byte ext, byte rtr, byte len, byte[] buf)
        {
            SendMsg(buf, len, id, ext, rtr, false);
            return 0;
        }

        public override void ClearBufferTransmitIfFlags(byte flags = 0)
        {
            TransmitChannelEventAttemptClear(APP_TX_FIFO);
        }

        public override byte SendMsgBuf(byte status, ulong id, byte ext, byte rtr, byte len, byte[] buf)
        {
            SendMsg(buf, len, id, ext, rtr, true);

            return 0;
        }

        public override byte SendMsgBuf(ulong id, byte ext, byte rtr, byte len, byte[] buf, bool waitSent = true)
        {
            SendMsg(buf, len, id, ext, rtr, waitSent);

            return 0;
        }

        public void SendMsgBuf(ulong id, byte ext, byte len, byte[] buf)
        {
            SendMsgBuf(id, ext, 0, len, buf, true);
        }

        public override MCP2518Dfs.CAN_RX_FIFO_EVENT ReadRxTxStatus()
        {
            ReceiveChannelEventGet(APP_RX_FIFO, out rxFlags);

            return rxFlags;
        }

        public override void McpPinMode(MCP2518Dfs.GPIO_PIN_POS pin, MCP2518Dfs.GPIO_PIN_MODE mode)
        {
            ushort a = (ushort)(MCP2518Dfs.cREGADDR_IOCON + 3);
            MCP2518Dfs.REG_IOCON iocon = new();
            iocon.word = 0;
            iocon.UpdateFromWord();

            ReadByte(a, out iocon.bytes[3]);

            iocon.UpdateFromBytes();

            if(pin == MCP2518Dfs.GPIO_PIN_POS.GPIO_PIN_0)
            {
                // Modify
                iocon.bF.PinMode0 = (int)mode;
            }
            if(pin == MCP2518Dfs.GPIO_PIN_POS.GPIO_PIN_1)
            {
                // Modify
                iocon.bF.PinMode1 = (int)mode;
            }
            iocon.UpdateFromReg();

            WriteByte(a, iocon.bytes[3]);
        }

        public override void DigitalWrite(MCP2518Dfs.GPIO_PIN_POS pin, MCP2518Dfs.GPIO_PIN_STATE state)
        {
            ushort a = (ushort)(MCP2518Dfs.cREGADDR_IOCON + 3);
            MCP2518Dfs.REG_IOCON iocon = new();
            iocon.word = 0;
            iocon.UpdateFromWord();

            ReadByte(a, out iocon.bytes[1]);

            switch (pin)
            {
                case MCP2518Dfs.GPIO_PIN_POS.GPIO_PIN_0:
                    iocon.bF.LAT0 = (int)state;
                    break;
                case MCP2518Dfs.GPIO_PIN_POS.GPIO_PIN_1:
                    iocon.bF.LAT1 = (int)state;
                    break;
                default:
                    return;
            }

            iocon.UpdateFromReg();

            WriteByte(a, iocon.bytes[1]);

        }

        public override MCP2518Dfs.GPIO_PIN_STATE DigitalRead(MCP2518Dfs.GPIO_PIN_POS pin)
        {
            MCP2518Dfs.GPIO_PIN_STATE state = MCP2518Dfs.GPIO_PIN_STATE.GPIO_LOW;

            ushort a = (ushort)(MCP2518Dfs.cREGADDR_IOCON + 2);
            MCP2518Dfs.REG_IOCON iocon = new();
            iocon.word = 0;
            iocon.UpdateFromWord();

            ReadByte(a, out iocon.bytes[2]);

            switch (pin)
            {
                case MCP2518Dfs.GPIO_PIN_POS.GPIO_PIN_0:
                    state = (MCP2518Dfs.GPIO_PIN_STATE)iocon.bF.GPIO0;
                    break;
                case MCP2518Dfs.GPIO_PIN_POS.GPIO_PIN_1:
                    state = (MCP2518Dfs.GPIO_PIN_STATE)iocon.bF.GPIO1;
                    break;
            }


            return state;
        }

        public override byte GetLastTxBuffer()
        {
            throw new NotImplementedException();
        }


        public override void ReserveTxBuffers(byte nTxBuf = 0)
        {
            throw new NotImplementedException();
        }




    }

    /// <summary>Represents a CAN message.</summary>
    public class CANMSG
    {
        public ulong CANID { get; set; }
        public bool IsExtended { get; set; }
        public bool IsRemote { get; set; }
        public byte len { get; set; }

        public byte rtr { get; set; }

        public byte[] buf = new byte[MCP2518Dfs.MAX_DATA_BYTES];
    }
}