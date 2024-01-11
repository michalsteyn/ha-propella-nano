using System;
using System.IO.Ports;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Logging;
using nanoFramework.Logging;
using nanoFramework.Logging.Debug;

namespace BluetoothBeacon
{

    public enum LoggerTargets
    {
        Debugger,
        Serial,
    }
    internal class DebuggerManager
    {
        private readonly LoggerTargets _loggerTarget;
        private readonly LogLevel _minLogLevel;
        static SerialPort _serialDevice;
        private static ILogger _logger;

        public event EventHandler RequestSleep;

        public DebuggerManager(LoggerTargets loggerTarget, LogLevel minLogLevel)
        {
            _loggerTarget = loggerTarget;
            _minLogLevel = minLogLevel;
        }

        public void Start()
        {
            try
            {
                _serialDevice = new SerialPort("COM1");
                // set parameters
                _serialDevice.BaudRate = 9600;
                _serialDevice.Parity = Parity.None;
                _serialDevice.StopBits = StopBits.One;
                _serialDevice.Handshake = Handshake.None;
                _serialDevice.DataBits = 8;

                // if dealing with massive data input, increase the buffer size
                _serialDevice.ReadBufferSize = 2048;
                _serialDevice.ReadTimeout = 2000;
                _serialDevice.DataReceived += SerialDevice_DataReceived;

                // open the serial port with the above settings
                _serialDevice.Open();

                LogDispatcher.LoggerFactory = _loggerTarget switch
                {
                    LoggerTargets.Serial => new SharedSerialPortLoggerFactory(_serialDevice, _minLogLevel),
                    _ => new DebugLoggerFactory(),
                };

                _logger = this.GetCurrentClassLogger();
            }
            catch (Exception e)
            {
                LogDispatcher.LoggerFactory = new DebugLoggerFactory();
                _logger = this.GetCurrentClassLogger();
                _logger.LogError(e, "Error Opening COM... Will use Debugger Target");
            }
        }

        private void SerialDevice_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (_serialDevice.BytesToRead <= 0) return;
            try
            {
                byte[] buffer = new byte[_serialDevice.BytesToRead];
                var bytesRead = _serialDevice.Read(buffer, 0, buffer.Length);
                string data = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                if (string.IsNullOrEmpty(data)) return;
                var args = data.Split('=');
                var command = args[0];
                var param = args.Length > 1 ? args[1] : "";

                switch (command)
                {
                    case "sleep":
                        _logger.LogInformation("Received Sleep Command");
                        RequestSleep?.Invoke(this, EventArgs.Empty);
                        break;
                    case "log":
                        var logLevel = ParseLogLevel(param);
                        _logger.LogInformation("Going to set LogLevel to: {0}[{1}]", param, logLevel);
                        if (LogDispatcher.LoggerFactory is SharedSerialPortLoggerFactory loggerFactory)
                            loggerFactory.SetMinLogLevel(logLevel);
                        break;
                    default:
                        _logger.LogWarning("Received Unknown Command: {0}", data);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error Reading Debugger Command");
            }
            
        }

        private LogLevel ParseLogLevel(string param)
        {
            return param switch
            {
                "off" => LogLevel.None,
                "trace" => LogLevel.Trace,
                "debug" => LogLevel.Debug,
                "info" => LogLevel.Information,
                "warn" => LogLevel.Warning,
                "error" => LogLevel.Error,
                "fatal" => LogLevel.Critical,
                _ => LogLevel.None
            };
        }
    }
}
