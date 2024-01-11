using System;

namespace EcoTest
{
    public interface IDataInterface
    {
        event EventHandler DataReceivedEvent; //DataReceivedEventArgs
        void Init();
        void SendData(byte[] buffer, uint length);
    }

    public class DataReceivedEventArgs : EventArgs
    {
        public byte[] Data { get; set; }

        public DataReceivedEventArgs()
        {
        }

        public DataReceivedEventArgs(byte[] data)
        {
            Data = data;
        }
    }
}