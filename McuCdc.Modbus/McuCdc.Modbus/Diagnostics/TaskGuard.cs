using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace McuCdc.Modbus.Diagnostics
{
    internal static class TaskGuard
    {
        public static Task Run(
            string name,
            IExceptionSink sink,
            Func<CancellationToken, Task> loop,
            CancellationToken ct,
            int restartDelayMs = 200)
        {
            return Task.Run(async () =>
            {
                while (!ct.IsCancellationRequested)
                {
                    try
                    {
                        await loop(ct).ConfigureAwait(false);
                        if (!ct.IsCancellationRequested)
                            await Task.Delay(restartDelayMs, ct).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) when (ct.IsCancellationRequested)
                    {
                        return;
                    }
                    catch (Exception ex)
                    {
                        sink.Publish(ex, name);
                        try { await Task.Delay(restartDelayMs, ct).ConfigureAwait(false); }
                        catch { }
                    }
                }
            }, ct);
        }
    }
}
