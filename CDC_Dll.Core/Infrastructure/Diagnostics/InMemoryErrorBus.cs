using DeviceService.Core.Abstractions.Diagnostics;
using DeviceService.Core.Domain.Errors;

namespace DeviceService.Core.Infrastructure.Diagnostics;

public sealed class InMemoryErrorBus : IErrorBus
{
    public event Action<ErrorInfo> ErrorRaised = delegate { };

    public void Publish(ErrorInfo error)
    {
        ErrorRaised.Invoke(error);
    }   

}