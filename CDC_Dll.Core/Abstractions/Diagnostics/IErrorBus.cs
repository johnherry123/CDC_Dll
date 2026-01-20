using DeviceService.Core.Domain.Errors;

namespace CDC_Dll.Core.Abstractions.Diagnostics
{
    public interface IErrorBus
    {
        event Action<ErrorInfo> ErrorRaised;
        void Publish(ErrorInfo error);
    }
}