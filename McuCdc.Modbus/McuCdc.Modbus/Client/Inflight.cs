using McuCdc.Modbus.Internal;
using McuCdc.Modbus.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace McuCdc.Modbus.Client
{
    internal sealed class Inflight
    {
        public IResponseMatcher Matcher { get; }
        private readonly TaskCompletionSource<PooledBuffer> _tcs;

        public Inflight(IResponseMatcher matcher, TaskCompletionSource<PooledBuffer> tcs)
        {
            Matcher = matcher;
            _tcs = tcs;
        }

        public bool TryComplete(PooledBuffer frame) => _tcs.TrySetResult(frame);
        public void Fail(Exception ex) => _tcs.TrySetException(ex);
    }
}
