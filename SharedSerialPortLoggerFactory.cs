using System;
using System.Collections;
using System.IO.Ports;
using Microsoft.Extensions.Logging;
using nanoFramework.Logging.Serial;

namespace BluetoothBeacon
{
    public class SharedSerialPortLoggerFactory : ILoggerFactory, IDisposable
    {
        private SerialPort _serial;
        private readonly LogLevel _minLogLevel;
        private ArrayList _loggers = new ArrayList();

        /// <summary>
        /// Create a new instance of <see cref="T:nanoFramework.Logging.Serial.SerialLoggerFactory" /> from a <see cref="T:System.IO.Ports.SerialPort" />.
        /// </summary>
        /// <param name="serial">The Serial Port</param>
        /// <param name="minLogLevel"></param>
        public SharedSerialPortLoggerFactory(SerialPort serial, LogLevel minLogLevel)
        {
            _serial = serial;
            _minLogLevel = minLogLevel;
        }

        /// <inheritdoc />
        public ILogger CreateLogger(string categoryName)
        {
            var logger = new SerialLogger(ref _serial, categoryName)
            {
                MinLogLevel = _minLogLevel
            };
            _loggers.Add(logger);
            return logger;
        }

        /// <inheritdoc />
        public void Dispose() => _serial.Dispose();

        public void SetMinLogLevel(LogLevel logLevel)
        {
            foreach (var logger in _loggers)
            {
                if (logger is SerialLogger serialLogger) 
                    serialLogger.MinLogLevel = logLevel;
            }
        }
    }
}
