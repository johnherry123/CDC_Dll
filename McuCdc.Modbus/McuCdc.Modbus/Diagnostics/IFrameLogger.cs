using System;

namespace McuCdc.Modbus.Diagnostics
{
    
    
    
    
    public interface IFrameLogger
    {
        
        void LogTx(ReadOnlySpan<byte> frame, DateTime timestampUtc);

        
        void LogRx(ReadOnlySpan<byte> frame, DateTime timestampUtc);
    }
}
