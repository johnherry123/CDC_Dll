namespace CDC_Dll.Core.Infrastructure.Protocol;

public static class CRC16
{
    public static ushort Compute(ReadOnlySpan<byte> data)
    {
        ushort crc = 0xffff;
        for (int i =0; i< data.Length; i++)
        {
            crc ^= data[i];
            for(int j =0 ; j < 8; j++)
            {
                bool lsb = (crc & 0x001) !=0;
                crc>>=1;
                if(lsb) crc ^= 0xA001;
            }
        }
        return crc;
    }
}