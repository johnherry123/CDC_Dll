using System;

namespace McuCdc.Modbus.Diagnostics
{
    
    
    
    public readonly struct FrameLogEntry
    {
        public enum FrameDirection { Tx, Rx }

        
        public FrameDirection Direction { get; }

        
        public DateTime TimestampUtc { get; }

        
        
        
        
        
        public byte[] Data { get; }

        public FrameLogEntry(FrameDirection direction, DateTime timestampUtc, ReadOnlySpan<byte> data)
        {
            Direction = direction;
            TimestampUtc = timestampUtc;
            Data = data.ToArray();
        }

        
        public string ToHexString() => BitConverter.ToString(Data).Replace("-", " ");

        public override string ToString()
            => $"[{TimestampUtc:HH:mm:ss.fff}] {Direction}: {ToHexString()}";
    }
}
