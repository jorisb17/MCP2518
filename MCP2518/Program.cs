using System;
using System.Collections;
using System.Diagnostics;
using System.Text;
using System.Threading;
using GHIElectronics.TinyCLR.Devices.Gpio;
using GHIElectronics.TinyCLR.Devices.Spi;
using GHIElectronics.TinyCLR.Pins;

namespace MCP2518
{
    internal class Program
    {
        static void Main()
        {

            var cs2 = GpioController.GetDefault().OpenPin(SC20100.GpioPin.PD14);

            var cs1 = GpioController.GetDefault().OpenPin(SC20100.GpioPin.PD3);

            var settings1 = new SpiConnectionSettings()
            {
                ChipSelectType = SpiChipSelectType.Gpio,
                ChipSelectLine = cs1,
                Mode = SpiMode.Mode0,
                ClockFrequency = 10_000_000,
                ChipSelectActiveState = false
            };

            var controller = SpiController.FromName(SC20100.SpiBus.Spi3);

            var spi1 = controller.GetDevice(settings1);

            var settings2 = new SpiConnectionSettings()
            {
                ChipSelectType = SpiChipSelectType.Gpio,
                ChipSelectLine = cs2,
                Mode = SpiMode.Mode0,
                ClockFrequency = 10_000_000,
                ChipSelectActiveState = false
            };

            var spi2 = controller.GetDevice(settings2);

            MCP2518 can1 = new(spi1);
            can1.Begin(MCPCanInterface.MCP_BITTIME_SETUP.CAN20_500KBPS);
            can1.SetMode(MCPCanInterface.CAN_OPERATION_MODE.CAN_NORMAL_MODE);

            MCP2518 can2 = new(spi2);
            can2.Begin(MCPCanInterface.MCP_BITTIME_SETUP.CAN20_500KBPS);
            can2.SetMode(MCPCanInterface.CAN_OPERATION_MODE.CAN_NORMAL_MODE);

            byte[] txd = new byte[8];

            for (int i = 0; i < 8; i++)
            {
                txd[i] = (byte)i;
            }

            byte len = 0;
            byte[] buf = new byte[16];

            while (true)
            {
                can1.SendMsgBuf(0x01, 0, MCPCanInterface.CANFD.Dlc2len(8), txd);
                Thread.Sleep(10);

                if (can2.CheckReceive() > 0)
                {
                    can2.ReadMsgBuf(out len, buf);                  // You should call readMsgBuff before getCanId
                    ulong id = can2.GetCanId();
                    bool ext = can2.IsExtendedFrame();

                    Debug.Write(ext ? "GET EXTENDED FRAME FROM ID: 0X" : "GET STANDARD FRAME FROM ID: 0X");
                    Debug.WriteLine(id.ToString("X"));

                    Debug.Write("Len = ");
                    Debug.WriteLine(len.ToString());
                    // print the data
                    for (int i = 0; i < len; i++)
                    {
                        Debug.Write(buf[i].ToString());
                        Debug.Write("\t");
                    }
                    Debug.WriteLine("");
                }
            }
        }
    }
}
