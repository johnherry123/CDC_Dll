

using CDC_Dll.Core.Abstractions.Diagnostics;
using CDC_Dll.Core.Domain.Errors;

namespace CDC_Dll.Core.Infrastructure.Diagnostics;

public sealed class InMemoryErrorBus : IErrorBus
{
    public event Action<ErrorInfo> ErrorRaised = delegate { };

    public void Publish(ErrorInfo error)
    {
        ErrorRaised.Invoke(error);
    }   

}