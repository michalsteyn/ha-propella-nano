using System;
using System.Device.Gpio;
using EcoTest;
using Microsoft.Extensions.Logging;
using nanoFramework.Logging;

namespace BluetoothBeacon
{
    public class SpeedController
    {
        private ILogger _logger;

        private readonly PASController _pasController;
        private readonly GpioController _gpioController;

        private const int DebounceMs = 20;
        private const int SpeedChange = 5;

        private const int SpeedUpButtonPin = 6; //47
        private const int SpeedDownButtonPin = 5; //21
        private const int SpeedLockButtonPin = 4; //35

        public SpeedController(PASController pasController)
        {
            _logger = this.GetCurrentClassLogger();

            _pasController = pasController;

            _gpioController = new GpioController();

            var speedLock = _gpioController.OpenPin(SpeedLockButtonPin, PinMode.InputPullDown); //Device.Pins.D06,
            speedLock.DebounceTimeout = TimeSpan.FromMilliseconds(DebounceMs);
            speedLock.ValueChanged += SpeedLockOnChanged;

            var speedUp = _gpioController.OpenPin(SpeedUpButtonPin, PinMode.InputPullDown); //Device.Pins.D10,
            speedUp.DebounceTimeout = TimeSpan.FromMilliseconds(DebounceMs);
            speedUp.ValueChanged += SpeedUpOnChanged;


            var speedDown = _gpioController.OpenPin(SpeedDownButtonPin, PinMode.InputPullDown); //Device.Pins.D09,
            speedDown.DebounceTimeout = TimeSpan.FromMilliseconds(DebounceMs);
            speedDown.ValueChanged += SpeedDownOnChanged;
        }

        private void SpeedDownOnChanged(object sender, PinValueChangedEventArgs pinValueChangedEventArgs)
        {
            if (pinValueChangedEventArgs.ChangeType == PinEventTypes.Rising)
            {
                _logger.LogInformation("Speed Down Button Pressed");
                _pasController.DecreaseCurrentPASLevel(SpeedChange);
            }
        }

        private void SpeedUpOnChanged(object sender, PinValueChangedEventArgs pinValueChangedEventArgs)
        {
            if (pinValueChangedEventArgs.ChangeType == PinEventTypes.Rising)
            {
                _logger.LogInformation("Speed Up Button Pressed");
                _pasController.IncreaseCurrentPASLevel(SpeedChange);
            }
        }

        private void SpeedLockOnChanged(object sender, PinValueChangedEventArgs pinValueChangedEventArgs)
        {
            if (pinValueChangedEventArgs.ChangeType == PinEventTypes.Rising)
            {
                _logger.LogInformation("Speed Lock Button Pressed");
                _pasController.LockCurrentPASLevel(0);
            }
        }
    }
}
