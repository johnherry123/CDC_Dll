using System.Xml.Serialization;
using DeviceService.Core.Domain.Models;

namespace DeviceService.Core.Application.Telemetries;

public class TelemetryCache
{
    private Telemetry? _lastest;
    private ushort? _lastSeq ; 
    public long SeqMissCount { get; private set; } 
    public void update(Telemetry t)
    {
        if(_lastSeq.HasValue)
        {
            var expectedSeq = (ushort)(_lastSeq.Value +1);
            if(t.Seq != expectedSeq)
            {
                SeqMissCount ++;
            }
        }
        _lastSeq = t.Seq;
        _lastest = t;
    }

}