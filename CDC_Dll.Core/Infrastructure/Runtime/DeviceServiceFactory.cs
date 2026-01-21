


using CDC_Dll.Core.Infrastructure.Transport;
using CDC_Dll.Core.Abstractions.Diagnostics;
using CDC_Dll.Core.Abstractions.Protocol;
using CDC_Dll.Core.Abstractions.Transport;
using CDC_Dll.Core.Infrastructure.Diagnostics;
using CDC_Dll.Core.Services;
using CDC_Dll.Core.Infrastructure.Protocol;
using CDC_Dll.Core.Infrastructure.Runtime;
using CDC_Dll.Core.Application.Services;
using CDC_Dll.Core.Infrastructure.Client;
using CDC_DLL.Core.Infrastructure.Client;

namespace CDC_DLL.Core.Infrastructure.Runtime;

public static class DeviceServiceFactory
{
    public static (IDeviceService service, IErrorBus errorBus, IProtocolClient client) Create(TransportOptions transportOptions)
    {
        IErrorBus errorBus = new InMemoryErrorBus();

        ITransport transport = new SerialCdcTransport(transportOptions);
        IFrameCodec codec = new FrameCodec();
        var clientOptions = new ProtocolClientOptions
        {
            EnableAlive = true, 
            AliveInterval = TimeSpan.FromMilliseconds(300),
            RxFaultAfter = TimeSpan.FromSeconds(5)
        };

        IProtocolClient client = new ProtocolClient(transport, codec, errorBus, clientOptions);



        IDeviceService service = new DeviceServiceCore(client, errorBus);

        GlobalExceptionHandler.Install(errorBus);

        return (service, errorBus, client);
    }
}