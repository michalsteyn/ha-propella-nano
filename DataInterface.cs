using Microsoft.Extensions.Logging;
using nanoFramework.Hardware.Esp32;
using System;
using System.Diagnostics;
using System.IO.Ports;
using nanoFramework.Logging;

namespace EcoTest
{
    public class DataInterface : IDataInterface
    {
        private ILogger _logger;
        SerialPort _serialDevice;
        public event EventHandler DataReceivedEvent;
        private readonly string _portName;
        
        public DataInterface(string portName)
        {
            _logger = this.GetCurrentClassLogger();
            _portName = portName;
        }

        public void Init()
        {
            try
            {
                var ports = SerialPort.GetPortNames();

                Debug.WriteLine("Available ports: ");
                foreach (string port in ports)
                {
                    Debug.WriteLine($" {port}");
                }
                
                _logger.LogInformation($"Using {_portName}...");

                // open COM2
                _serialDevice = new SerialPort(_portName);
                _serialDevice.BaudRate = 9600;
                _serialDevice.Parity = Parity.None;
                _serialDevice.StopBits = StopBits.One;
                _serialDevice.Handshake = Handshake.None;
                _serialDevice.DataBits = 8;

                // if dealing with massive data input, increase the buffer size
                _serialDevice.ReadBufferSize = 2048;
                _logger.LogInformation("\tCreated");

                // open the serial port with the above settings
                _serialDevice.Open();


                _logger.LogInformation("\tOpened");

                _serialDevice.DataReceived += ProcessData;
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error Opening Port: {_portName}");
                throw;
            }
        }

        public void Close()
        {
            try
            {
                if (_serialDevice.IsOpen)
                {
                    _logger.LogInformation("Closing Serial Port: {0}", _portName);
                    _serialDevice.Close();
                }
                else
                {
                    _logger.LogWarning("Serial Port already Closed: {0}", _portName);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error Closing Serial Port: {0}", _portName);
            }
        }

        private void ProcessData(object sender, SerialDataReceivedEventArgs e)
        {
            var bytesToRead = _serialDevice.BytesToRead;
            var buffer = new byte[bytesToRead];
            int readCount = _serialDevice.Read(buffer, 0, bytesToRead);
            DataReceivedEvent?.Invoke(this, new DataReceivedEventArgs(buffer));
        }

        public void SendData(byte[] buffer, uint length)
        {
            _serialDevice.Write(buffer, 0, (int)length);
        }
    }
}
