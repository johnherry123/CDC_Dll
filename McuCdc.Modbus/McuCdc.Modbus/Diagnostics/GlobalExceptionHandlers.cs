using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace McuCdc.Modbus.Diagnostics
{
    public static class GlobalExceptionHandlers
    {
        public static void Install(IExceptionSink sink)
        {
            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            {
                try
                {
                    if (e.ExceptionObject is Exception ex)
                        sink.Publish(ex, "AppDomain.UnhandledException");
                    else
                        sink.Publish(new Exception($"UnhandledException: {e.ExceptionObject}"), "AppDomain.UnhandledException");
                }
                catch {  }
            };

            TaskScheduler.UnobservedTaskException += (_, e) =>
            {
                try
                {
                    sink.Publish(e.Exception, "TaskScheduler.UnobservedTaskException");
                    e.SetObserved();
                }
                catch {  }
            };
        }
    }
}
