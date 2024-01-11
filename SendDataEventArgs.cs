using System;

namespace EcoTest
{
    public class SendDataEventArgs : EventArgs
    {
        public byte[] Data { get; set; }

        public uint Length { get; set; }

        public SendDataEventArgs()
        {
        }

        public SendDataEventArgs(byte[] data, uint length)
        {
            Data = data;
            Length = length;
        }
    }
}