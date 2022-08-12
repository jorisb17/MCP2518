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

            byte[] stmp = { 0, 0, 0, 0, 0, 0, 0, 0 };




            var settings2 = new SpiConnectionSettings()
            {
                ChipSelectType = SpiChipSelectType.Gpio,
                ChipSelectLine = cs2,
                Mode = SpiMode.Mode0,
                ClockFrequency = 10_000_000,
                ChipSelectActiveState = false
            };

            var spi2 = controller.GetDevice(settings2);

            while (true)
            {
                
            }
        }
    }
}
