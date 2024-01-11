//
// Copyright (c) .NET Foundation and Contributors
// See LICENSE file in the project root for full license information.
//

using System;
using System.Threading;
using EcoTest;
using Microsoft.Extensions.Logging;
using nanoFramework.Hardware.Esp32;
using nanoFramework.Logging;

//using nanoFramework.Device.Bluetooth;
//using nanoFramework.Device.Bluetooth.Advertisement;

namespace BluetoothBeacon
{
    public class Program
    {
        private static ILogger _logger;

        public static void Main()
        {
            var debuggerManager = new DebuggerManager(LoggerTargets.Serial, LogLevel.Information);
            debuggerManager.Start();
            _logger = LogDispatcher.GetLogger("Main");

            var healthMonitor = new HealthMonitor();
            healthMonitor.Start();

            Configuration.SetPinFunction(15, DeviceFunction.COM2_RX); //4 blue //15
            Configuration.SetPinFunction(7, DeviceFunction.COM2_TX); //5 green //16

            Configuration.SetPinFunction(1, DeviceFunction.COM3_TX); //4 blue
            Configuration.SetPinFunction(2, DeviceFunction.COM3_RX); //5 green

            var dataInterface = new DataInterface("COM2");
            var passController = new PASController();
            var speedController = new SpeedController(passController);
            var dataProcessor = new DataProcessor("Display", dataInterface, passController);
            dataProcessor.Start();
            dataInterface.Init();

            var dataInterface2 = new DataInterface("COM3");
            var dataProcessor2 = new DataProcessor("Controller", dataInterface2);
            dataProcessor2.Start();
            dataInterface2.Init();

            var powerController = new PowerController(dataInterface, Sleep.WakeupGpioPin.Pin15);
            debuggerManager.RequestSleep += (sender, args) => powerController.ActivateSleep();

            _logger.LogInformation("Beacon Sample");

            //beacon();

            Thread.Sleep(Timeout.Infinite);
        }

        //public static void beacon()
        //{
        //    Guid proximityUUID = new Guid("E2C56DB5-DFFB-48D2-B060-D0F5A71096E0");

        //    iBeacon beacon = new iBeacon(proximityUUID, 0, 1, -59);
        //    beacon.Start();

        //    Thread.Sleep((int)Timeout.Infinite);

        //    beacon.Stop();
        //}

        //private static void Publisher_StatusChanged(object sender, BluetoothLEAdvertisementPublisherStatusChangedEventArgs args)
        //{
        //    BluetoothLEAdvertisementPublisher pub = sender as BluetoothLEAdvertisementPublisher;

        //    _logger.LogInformation($"Status:{args.Status} Error:{args.Error} TxPowerLevel:{args.SelectedTransmitPowerLevelInDBm}");
        //}
    }
}
