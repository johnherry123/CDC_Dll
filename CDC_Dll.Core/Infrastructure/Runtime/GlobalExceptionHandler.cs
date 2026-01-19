

using DeviceService.Core.Abstractions.Diagnostics;
using DeviceService.Core.Application.Errors;


namespace CDC_Dll.Core.Infrastructure.Runtime;

public class GlobalExceptionHandler
{
    private static int _installed;
    public static void Install(IErrorBus errorBus)
    {
        if(Interlocked.Exchange(ref _installed, 1) == 1)
            return;
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            var ex = e.ExceptionObject as Exception ?? new Exception("Unknown unhandled exception");
            var ctx = ResultGuard.ctx("GlobalUnhandle", component: "GlobalHandler");
            var err = ErrorMapper.FromException(ex, ctx, "An unhandled exception occurred");
            errorBus.Publish(err);  
        };
        TaskScheduler.UnobservedTaskException += (s, e) =>
        {
           var ctx = ResultGuard.ctx("UnobservedTask", component: "GlobalHandler"); 
           var err = ErrorMapper.FromException(e.Exception, ctx, "An unobserved task exception occurred");
           errorBus.Publish(err);
        };
    }
}