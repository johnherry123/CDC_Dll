using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace McuCdc.Modbus.Diagnostics
{
    public interface IExceptionSink
    {
        void Publish(Exception ex, string source);
    }
}
