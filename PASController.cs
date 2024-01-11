using Microsoft.Extensions.Logging;
using nanoFramework.Logging;
using System;
using System.Collections;

namespace EcoTest
{
    public enum PASLevels
    {
        Level0,
        Level1,
        Level2,
        Level3,
        Level4,
        Level5
    }
    public class PASController
    {
        private ILogger _logger;

        public PASLevels CurrentPasLevel { get; private set; }

        public EventHandler PASMappingsChangedEvent;

        public Hashtable PASLevels { get; private set; } = new Hashtable()//Dictionary<byte, PASLevels>()
        {
            {(byte)0x00, EcoTest.PASLevels.Level0},
            {(byte)0x7F, EcoTest.PASLevels.Level1 },
            {(byte)0x99, EcoTest.PASLevels.Level2 },
            {(byte)0xB7, EcoTest.PASLevels.Level3 },
            {(byte)0xE0, EcoTest.PASLevels.Level4 },
            {(byte)0xFF, EcoTest.PASLevels.Level5 },
        };

        public Hashtable PASMappings { get; private set; } = new Hashtable()//Dictionary<PASLevels, byte>()
        {
            {EcoTest.PASLevels.Level0, (byte)0},
            {EcoTest.PASLevels.Level1, (byte)96 },
            {EcoTest.PASLevels.Level2, (byte)128 },
            {EcoTest.PASLevels.Level3, (byte)160 },
            {EcoTest.PASLevels.Level4, (byte)192 },
            {EcoTest.PASLevels.Level5, (byte)255 },
        };

        private Hashtable _defaultPASMappings = new Hashtable();

        public PASController()
        {
            _logger = this.GetCurrentClassLogger();
            foreach (var key in PASMappings.Keys)
            {
                _defaultPASMappings[key] = PASMappings[key];
            }
        }

        public void SetPASMapping(PASLevels level, byte value)
        {
            _logger.LogInformation($"Setting PAS Level: {level} to Value: {value}");
            PASMappings[level] = value;
        }

        public PASLevels GetPASLevel(byte[] data)
        {
            if (data[ProtocolConstants.CommandCodeIndex] != ProtocolConstants.CommandCodeOperationMode)
            {
                CurrentPasLevel = EcoTest.PASLevels.Level0;
                return CurrentPasLevel;
            }

            var pasLevelValue = data[ProtocolConstants.PASLevelIndex];

            CurrentPasLevel = PASLevels.Contains(pasLevelValue)
                ? (PASLevels)PASLevels[pasLevelValue]
                : EcoTest.PASLevels.Level0;
            return CurrentPasLevel;
        }

        public bool UpdatePASLevel(byte[] data)
        {
            var pasLevel = GetPASLevel(data);
            var pasMapping = (byte)PASMappings[pasLevel];
            _logger.LogDebug($"Current PAS Level: {pasLevel}, New Value: {(uint)pasMapping}");
            data[ProtocolConstants.PASLevelIndex] = pasMapping;
            return true;
        }

        public void IncreaseCurrentPASLevel(byte up)
        {
            uint currentSpeed = (byte)PASMappings[CurrentPasLevel];
            currentSpeed += up;
            if (currentSpeed > 255) currentSpeed = 255;
            PASMappings[CurrentPasLevel] = (byte)currentSpeed;
            _logger.LogInformation($"Increase PAS Level: {CurrentPasLevel}, Speed: {currentSpeed}");
            PASMappingsChangedEvent?.Invoke(this, EventArgs.Empty);
        }

        public void DecreaseCurrentPASLevel(byte down)
        {
            uint currentSpeed = (byte)PASMappings[CurrentPasLevel];
            currentSpeed -= down;
            if (currentSpeed < 0) currentSpeed = 0;
            PASMappings[CurrentPasLevel] = (byte)currentSpeed;
            _logger.LogInformation($"Decrease PAS Level: {CurrentPasLevel}, Speed: {currentSpeed}");
            PASMappingsChangedEvent?.Invoke(this, EventArgs.Empty);
        }

        public void LockCurrentPASLevel(byte speed)
        {
            _logger.LogInformation($"Reset PAS Level: {CurrentPasLevel}");
            foreach (var key in _defaultPASMappings.Keys)
            {
                var defaultVal = _defaultPASMappings[key];
                PASMappings[key] = defaultVal;
                _logger.LogInformation("Setting Level: {0}, to {1}", key, defaultVal);
            }
            PASMappingsChangedEvent?.Invoke(this, EventArgs.Empty);
        }
    }
}