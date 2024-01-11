using System;
using System.Device.Gpio;
using System.Reflection;
using System.Threading;
using EcoTest;
using Microsoft.Extensions.Logging;
using nanoFramework.Hardware.Esp32;
using nanoFramework.Logging;

namespace BluetoothBeacon
{
    internal class PowerController
    {
        private readonly DataInterface _dataInterface;
        private readonly Sleep.WakeupGpioPin _wakeupGpioPin;
        private ILogger _logger;
        private readonly Timer _sleepTimeoutTimer;
        private const int TimeoutToSleepMs = 10000;

        //private const Sleep.WakeupGpioPin WakeupPin = Sleep.WakeupGpioPin.Pin12;
        //private const int SleepPin = 12;
        //private const int DebounceMs = 200;

        //private readonly GpioController _gpioController;

        public PowerController(DataInterface dataInterface, Sleep.WakeupGpioPin wakeupGpioPin)
        {
            _logger = this.GetCurrentClassLogger();

            _dataInterface = dataInterface;
            _dataInterface.DataReceivedEvent += DataInterfaceOnDataReceivedEvent;
            _wakeupGpioPin = wakeupGpioPin;


            _sleepTimeoutTimer = new Timer(Callback, this, TimeoutToSleepMs, -1);
            //_gpioController = new GpioController();

            //var sleepButton = _gpioController.OpenPin(SleepPin, PinMode.InputPullDown); //Device.Pins.D06,
            //sleepButton.DebounceTimeout = TimeSpan.FromMilliseconds(DebounceMs);
            //sleepButton.ValueChanged += SleepPinOnChanged;

            try
            {
                // Get the Wakeup cause
                var cause = Sleep.GetWakeupCause();
                _logger.LogInformation($"Previous Sleep Cause: {cause}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading wakeup cause");
            }

            //try
            //{
            //    // Wakeup when WakeupPin is high
            //    Sleep.EnableWakeupByPin(WakeupPin, 1);
            //}
            //catch (Exception ex)
            //{
            //    _logger.LogError(ex, "Error configuring wakeup");
            //}
        }

        private void Callback(object state)
        {
            _logger.LogInformation("Comms Timeout. Going to Activate Sleep...");
            ActivateSleep();
        }

        private void DataInterfaceOnDataReceivedEvent(object sender, EventArgs e)
        {
            _sleepTimeoutTimer.Change(TimeoutToSleepMs, -1);
        }

        //private void SleepPinOnChanged(object sender, PinValueChangedEventArgs pinValueChangedEventArgs)
        //{
        //    if (pinValueChangedEventArgs.ChangeType == PinEventTypes.Falling)
        //    {
        //        _logger.LogInformation("Power Button Off");

        //        try
        //        {
        //            // Start Deep Sleep
        //            Sleep.StartDeepSleep();
        //            //Sleep.StartLightSleep();
        //        }
        //        catch (Exception ex)
        //        {
        //            _logger.LogError(ex, "Error sleeping");
        //        }
        //    }
        //}

        public void ActivateSleep()
        {
            try
            {
                _logger.LogInformation("Activating Sleep");
                _dataInterface.Close();
                Thread.Sleep(200);
                Sleep.EnableWakeupByPin(_wakeupGpioPin, 1);
                Sleep.StartDeepSleep();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error Activating Sleep");
            }
            
        }
    }
}
