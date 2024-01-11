using Microsoft.Extensions.Logging;
using nanoFramework.Logging;
using System;
using System.Collections;
using System.Threading;

namespace EcoTest
{
    public class DataProcessor
    {
        private ILogger _logger;
        private readonly string _name;
        private readonly PASController _pasController;
        private readonly IDataInterface _dataInterface;
        private Queue _rxBuf = new Queue();//BlockingCollection<byte>(32);
        private AutoResetEvent _rxBufLock = new AutoResetEvent(false);
        private byte[] _sendBuf = new byte[32];

        public event EventHandler SendDataEvent; //<SendDataEventArgs>

        public DataProcessor(string name, IDataInterface dataInterface,
            PASController pasController = null)
        {
            _logger = this.GetCurrentClassLogger();
            _name = name;
            _pasController = pasController;
            _dataInterface = dataInterface;
            _dataInterface.DataReceivedEvent += (sender, args) => ReceiveData(args);
        }

        public void ReceiveData(EventArgs data)
        {
            if (data is DataReceivedEventArgs dataReceivedEventArgs)
                ReceiveData(dataReceivedEventArgs.Data);
        }

        public void ReceiveData(byte[] data)
        {
            if (data is not { Length: > 0 }) return;
            _logger.LogDebug($"Received Data: {data.Length} bytes");

            foreach (var b in data)
                _rxBuf.Enqueue(b);

            _rxBufLock.Set();
        }

        public void Start()
        {
            _logger.LogInformation("Going to Start Data Processor Thread");
            var thread = new Thread(RunThread);
            thread.Start();
        }

        private byte TakeByte()
        {
            if (_rxBuf.Count == 0)
                _rxBufLock.WaitOne();
            return (byte)_rxBuf.Dequeue();
        }

        private void RunThread()
        {
            _logger.LogInformation("Data Processor Thread Started");
            //Thread.CurrentThread.Name = "Data Processor";
            while (true)
            {
                try
                {
                    //while (true)
                    //{
                    //    var d = TakeByte();
                    //    _logger.LogInformation($"Rx: {d}");
                    //}

                    uint sendIndex = 0;

                    //[0] Get Start Byte
                    byte start;
                    do
                    {
                        start = TakeByte();
                        if (start == ProtocolConstants.Start)
                        {
                            //_logger.LogInformation("Receive Start");
                        }
                        else
                            _logger.LogError($"Invalid Start Byte {(int)start}");

                    } while (start != ProtocolConstants.Start);
                    _sendBuf[sendIndex++] = ProtocolConstants.Start;

                    //[1] Get Device ID
                    var deviceId = TakeByte();
                    if (deviceId != ProtocolConstants.DeviceId)
                    {
                        _logger.LogError($"Invalid DeviceId: {(int)deviceId}");
                        continue;
                    }
                    _sendBuf[sendIndex++] = deviceId;

                    //[2] Get Command Code
                    var commandCode = TakeByte();
                    if (commandCode != ProtocolConstants.CommandCodeInit && commandCode != ProtocolConstants.CommandCodeOperationMode)
                        _logger.LogError($"Unknown Command Code: {(int)commandCode}");
                    _sendBuf[sendIndex++] = commandCode;

                    //[3] Get Data Payload Length
                    var payloadLength = TakeByte();
                    _sendBuf[sendIndex++] = payloadLength;
                    //_logger.LogInformation($"Going to Receive Payload: {(int)payloadLength}");

                    //[4] Get Payload
                    for (int i = 0; i < payloadLength; i++)
                    {
                        _sendBuf[sendIndex++] = TakeByte();
                    }

                    //[5] Get Checksum
                    var checksumLow = TakeByte();
                    var checksumHigh = TakeByte();
                    _sendBuf[sendIndex++] = checksumLow;
                    _sendBuf[sendIndex++] = checksumHigh;

                    uint checksum = checksumLow + ((uint)checksumHigh << 8);

                    int calChecksum = 0;
                    for (int i = 0; i < 3 + payloadLength; i++)
                    {
                        calChecksum += _sendBuf[i + 1];
                    }

                    if (checksum != calChecksum)
                    {
                        _logger.LogError($"Invalid Checkum, Expected: {calChecksum}, Received: {checksum}");
                        continue;
                    }

                    //[6] Get Stop Byte
                    var stopLow = TakeByte();
                    var stopHigh = TakeByte();
                    _sendBuf[sendIndex++] = stopLow;
                    _sendBuf[sendIndex++] = stopHigh;

                    if (stopLow != ProtocolConstants.StopLow || stopHigh != ProtocolConstants.StopHigh)
                    {
                        _logger.LogError($"Invalid Stop Code: {(int)stopLow},{(int)stopHigh}");
                        continue;
                    }

                    if (commandCode == ProtocolConstants.CommandCodeOperationMode)
                    {
                        ModifyOperationMessage(_sendBuf);
                    }

                    SendMessage(_sendBuf, sendIndex);
                }
                catch (Exception e)
                {
                    _logger.LogError($"Error Parsing Data: {e.Message}");
                }
            }
        }

        private uint CalculateChecksum(byte[] sendBuf)
        {
            uint calChecksum = 0;

            var dataLength = sendBuf[ProtocolConstants.DataLengthIndex];

            for (uint i = 0; i < 3 + dataLength; i++)
            {
                calChecksum += _sendBuf[i + 1];
            }

            return calChecksum;
        }

        private void UpdateChecksum(byte[] sendBuf, uint checksum)
        {
            byte lowByte = (byte)(checksum & 0xff);
            byte highByte = (byte)(checksum >> 8);

            var dataLength = sendBuf[ProtocolConstants.DataLengthIndex];
            var checksumIndex = ProtocolConstants.DataLengthIndex + 1 + dataLength;
            sendBuf[checksumIndex] = lowByte;
            sendBuf[checksumIndex + 1] = highByte;
        }

        private void RecalculateChecksum(byte[] sendBuf)
        {
            var checksum = CalculateChecksum(sendBuf);
            UpdateChecksum(sendBuf, checksum);
        }

        private void ModifyOperationMessage(byte[] sendBuf)
        {
            if (_pasController != null && _pasController.UpdatePASLevel(sendBuf))
            {
                RecalculateChecksum(sendBuf);
            }
        }

        private void SendMessage(byte[] data, uint length)
        {
            _logger.LogDebug($"{_name}: Sending Data");
            _dataInterface.SendData(data, length);
            SendDataEvent?.Invoke(this, new SendDataEventArgs(data, length));
        }
    }
}
