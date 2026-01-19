using DeviceService.Core.Domain.Errors;

namespace DeviceService.Core.Abstractions.Diagnostics
{
    public interface IErrorBus
    {
        event Action<ErrorInfo> ErrorRaised;
        void Publish(ErrorInfo error);
    }
}