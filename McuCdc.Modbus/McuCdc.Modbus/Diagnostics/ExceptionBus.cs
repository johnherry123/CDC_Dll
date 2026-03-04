using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace McuCdc.Modbus.Diagnostics
{
    public sealed class ExceptionBus : IExceptionSink
    {
        public event Action<Exception, string>? Error;

        public void Publish(Exception ex, string source)
            => Error?.Invoke(ex, source);
    }
}
