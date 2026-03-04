using McuCdc.Modbus.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace McuCdc.Modbus.Spooling
{
    internal sealed class SpoolReplayWorker
    {
        private readonly FrameSpoolFile _spool;
        private readonly ChannelWriter<PooledBuffer> _rxWriter;

        public SpoolReplayWorker(FrameSpoolFile spool, ChannelWriter<PooledBuffer> rxWriter)
        {
            _spool = spool ?? throw new ArgumentNullException(nameof(spool));
            _rxWriter = rxWriter ?? throw new ArgumentNullException(nameof(rxWriter));
        }

        public async Task RunAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                if (_spool.TryDequeue(out var pb))
                {
                    await _rxWriter.WriteAsync(pb, ct).ConfigureAwait(false);
                    continue;
                }

                await Task.Delay(25, ct).ConfigureAwait(false);
            }
        }
    }
}
