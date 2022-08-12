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

        static CAN_CONFIG config = new();

        // Receive objects
        private static CAN_RX_FIFO_CONFIG rxConfig = new();
        private static REG_CiFLTOBJ fObj = new();
        private static REG_CiMASK mObj = new();
        private static CAN_RX_FIFO_EVENT rxFlags;
        private static CAN_RX_MSGOBJ rxObj = new();
        private static byte[] rxd = new byte[MCP2518Dfs.MAX_DATA_BYTES];

        // Transmit objects
        private static CAN_TX_FIFO_CONFIG txConfig = new();
        private static CAN_TX_FIFO_EVENT txFlags = new();
        private static CAN_TX_MSGOBJ txObj = new();
        private static byte[] txd = new byte[MCP2518Dfs.MAX_DATA_BYTES];

        private static int MAX_TXQUEUE_ATTEMPTS = 50;

        // Transmit Channels
        private CAN_FIFO_CHANNEL APP_TX_FIFO = CAN_FIFO_CHANNEL.CAN_FIFO_CH2;

        // Receive Channels
        private CAN_FIFO_CHANNEL APP_RX_FIFO = CAN_FIFO_CHANNEL.CAN_FIFO_CH1;

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
        public override byte Begin(MCP_BITTIME_SETUP speedSet, MCP_CLOCK clockSet)
        {
            throw new NotImplementedException();
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

        private byte ReadWord(ushort address, out uint rxd)
        {
            uint x;
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

        private byte WriteWord(ushort address, uint txd)
        {
            byte spiTransferError = 0;

            // Compose command
            spiTransmitBuffer[0] =
                (byte)((cINSTRUCTION_WRITE << 4) + ((address >> 8) & 0xF));
            spiTransmitBuffer[1] = (byte)(address & 0xFF);

            for (int i = 0; i < 4; i++)
            {
                spiTransmitBuffer[i+2] = (byte)((txd >> (i * 8)) & 0xFF);
            }

            spi.Write(spiTransmitBuffer, 0, 6);

            return spiTransferError;
        }

        private byte ReadHalfWord(ushort address, out ushort rxd)
        {
            byte spiTransferError = 0;

            uint x;

            // Compose command
            spiTransmitBuffer[0] =
                (byte)((cINSTRUCTION_READ << 4) + ((address >> 8) & 0xF));
            spiTransmitBuffer[1] = (byte)(address & 0xFF);

            spi.TransferSequential(spiTransmitBuffer, 0, 2, spiReceiveBuffer, 0, 2);

            rxd = 0;

            for (int i = 0; i < 2; i++)
            {
                x = spiReceiveBuffer[i];
                rxd += (ushort)(x << (i * 8));
            }

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

        private byte WriteWordSafe(ushort address, uint txd)
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
            crcFromSpiSalve = (ushort)((spiReceiveBuffer[nBytes + 2 ] << 8) + (spiReceiveBuffer[nBytes + 1]));

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

        private byte ReadWordArray(ushort address, uint[] rxd, ushort nWords)
        {
            REG_t w = new();
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

            int n = 2;
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

        private byte WriteWordArray(ushort address, uint[] txd, ushort nWords)
        {
            REG_t w = new();
            ushort spiTransferSize = (ushort)(nWords * 4 + 2);
            byte spiTransferError = 0;

            // Compose command
            spiTransmitBuffer[0] = (byte)((cINSTRUCTION_WRITE << 4) + ((address >> 8) & 0xF));
            spiTransmitBuffer[1] = (byte)(address & 0xFF);

            int n = 2;
            for (int i = 0; n < nWords; i++)
            {
                w.word = txd[i];
                w.UpdateFromWord();
                for (int j = 0; j < 4; j++, n++)
                {
                    spiTransmitBuffer[n] = w.bytes[j];
                }
            }

            spi.Write(spiTransmitBuffer, 0, spiTransferSize);

            return spiTransferError;
        }

        private byte EccEnable()
        {
            byte spiTranferError = 0;
            byte d = 0;

            // Read
            spiTranferError = ReadByte(MCP2518Dfs.cREGADDR_ECCCON, out d);
            if (spiTranferError > 0)
            {
                return -1;
            }

            // Modify
            d |= 0x01;

            //Write
            spiTranferError = WriteByte(MCP2518Dfs.cREGADDR_ECCCON, d);

            return spiTranferError;
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

        private void ConfigureObjectReset(CAN_CONFIG config)
        {
            REG_CiCON ciCon = new REG_CiCON();
            ciCon.word = MCP2518Dfs.canControlResetValues[MCP2518Dfs.cREGADDR_CiCON / 4];
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

        private void Configure(CAN_CONFIG config)
        {
            REG_CiCON ciCon = new();

            ciCon.word = MCP2518Dfs.canControlResetValues[MCP2518Dfs.cREGADDR_CiCON / 4];
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

        private void TransmitChannelConfigureObjectReset(CAN_TX_FIFO_CONFIG config)
        {
            REG_CiFIFOCON ciFifoCon = new ();
            ciFifoCon.word = MCP2518Dfs.canFifoResetValues[0]; // 10010010100101010000
            ciFifoCon.UpdateFromWord();

            config.RTREnable = ciFifoCon.txBF.RTREnable;
            config.TxPriority = ciFifoCon.txBF.TxPriority;
            config.TxAttempts = ciFifoCon.txBF.TxAttempts;
            config.FifoSize = ciFifoCon.txBF.FifoSize;
            config.PayLoadSize = ciFifoCon.txBF.PayLoadSize;
        }

        private void TransmitChannelConfigure(CAN_FIFO_CHANNEL channel, CAN_TX_FIFO_CONFIG config)
        {
            ushort a = 0;

            // Setup FIFO
            REG_CiFIFOCON ciFifoCon = new();
            ciFifoCon.word = MCP2518Dfs.canFifoResetValues[0];
            ciFifoCon.UpdateFromWord();

            ciFifoCon.txBF.TxEnable = 1;
            ciFifoCon.txBF.FifoSize = config.FifoSize;
            ciFifoCon.txBF.PayLoadSize = config.PayLoadSize;
            ciFifoCon.txBF.TxAttempts = config.TxAttempts;
            ciFifoCon.txBF.TxPriority = config.TxPriority;
            ciFifoCon.txBF.RTREnable = config.RTREnable;
        }

        public override byte CheckClearRxStatus(out byte status)
        {
            throw new NotImplementedException();
        }

        public override byte CheckClearTxStatus(out byte status)
        {
            throw new NotImplementedException();
        }

        public override byte CheckError(out byte err)
        {
            throw new NotImplementedException();
        }

        public override byte CheckReceive()
        {
            throw new NotImplementedException();
        }

        public override void ClearBufferTransmitIfFlags(byte flags = 0)
        {
            throw new NotImplementedException();
        }

        public override void EnableTxInterrupt(bool enable = true)
        {
            throw new NotImplementedException();
        }

        public override byte GetLastTxBuffer()
        {
            throw new NotImplementedException();
        }

        public override byte McpDigitalRead(byte pin)
        {
            throw new NotImplementedException();
        }

        public override bool McpDigitalWrite(byte pin, byte mode)
        {
            throw new NotImplementedException();
        }

        public override bool McpPinMode(byte pin, byte mode)
        {
            throw new NotImplementedException();
        }

        public override byte ReadMessageBuf(out byte len, byte[] buf)
        {
            throw new NotImplementedException();
        }

        public override byte ReadMessageBufID(out ulong id, out byte ext, out byte trt, out byte len, out byte[] buf)
        {
            throw new NotImplementedException();
        }

        public override byte ReadMessageBufID(out ulong ID, out byte len, byte[] buf)
        {
            throw new NotImplementedException();
        }

        public override byte ReadRxTxStatus()
        {
            throw new NotImplementedException();
        }

        public override void ReserveTxBuffers(byte nTxBuf = 0)
        {
            throw new NotImplementedException();
        }

        public override byte SendMsgBuf(byte status, ulong id, byte ext, byte rtr, byte len, byte[] buf)
        {
            throw new NotImplementedException();
        }

        public override byte SendMsgBuf(ulong id, byte ext, byte rtr, byte len, byte[] buf, bool waitSent = true)
        {
            throw new NotImplementedException();
        }

        public override byte SetMode(CAN_OPERATION_MODE opMode)
        {
            throw new NotImplementedException();
        }

        public override void SetSleepWakeup(bool enable)
        {
            throw new NotImplementedException();
        }

        public override byte Sleep()
        {
            throw new NotImplementedException();
        }

        public override byte TrySendMsgBuf(ulong id, byte ext, byte rtr, byte len, byte[] buf, byte iTxBuf = 255)
        {
            throw new NotImplementedException();
        }

        public override byte Wake()
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
}


