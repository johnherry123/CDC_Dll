namespace DeviceService.Core.Abstractions.Protocol;

public readonly record struct LinkMetricsSnapshot
(
    long RxByte,
    long TxByte,
    long RxFrame,
    long TxFrame,
    long CRCFailure,
    long SeqMisses,
    long CommandTimeouts,
    int PendingRequests,
    DateTime LastRxUtc,
    DateTime LastTxUtc,
    double RxBytesPerSec,
    double TxBytesPerSec
);