using McuCdc.Modbus.Internal;
using McuCdc.Modbus.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace McuCdc.Modbus.Client
{
    internal readonly record struct RequestWorkItem(
        PooledBuffer Request,
        IResponseMatcher Matcher,
        int TimeoutMs,
        CancellationToken Ct,
        TaskCompletionSource<PooledBuffer> Tcs)
    {
        public void Fail(Exception ex) => Tcs.TrySetException(ex);
        public void DisposeRequest() => Request.Dispose();
    }
}
