namespace EcoTest
{
    public class ProtocolConstants
    {
        public const byte Start = 0x3A;
        public const byte StopLow = 0x0D;
        public const byte StopHigh = 0x0A;

        public const byte DeviceId = 0x1A;

        public const byte CommandCodeInit = 0x53;
        public const byte CommandCodeOperationMode = 0x52;

        public const byte CommandCodeIndex = 2;
        public const byte DataLengthIndex = 3;
        public const byte PASLevelIndex = 4;
    }
}