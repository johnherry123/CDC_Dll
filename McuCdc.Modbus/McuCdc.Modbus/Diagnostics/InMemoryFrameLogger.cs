using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace McuCdc.Modbus.Diagnostics
{
    
    
    
    
    
    
    public sealed class InMemoryFrameLogger : IFrameLogger
    {
        private readonly int _capacity;
        private readonly ConcurrentQueue<FrameLogEntry> _queue = new();

        
        public event Action<FrameLogEntry>? EntryAdded;

        
        public InMemoryFrameLogger(int capacity = 1000)
        {
            if (capacity < 1) throw new ArgumentOutOfRangeException(nameof(capacity));
            _capacity = capacity;
        }

        
        public int Count => _queue.Count;

        public void LogTx(ReadOnlySpan<byte> frame, DateTime timestampUtc)
            => Enqueue(new FrameLogEntry(FrameLogEntry.FrameDirection.Tx, timestampUtc, frame));

        public void LogRx(ReadOnlySpan<byte> frame, DateTime timestampUtc)
            => Enqueue(new FrameLogEntry(FrameLogEntry.FrameDirection.Rx, timestampUtc, frame));

        private void Enqueue(FrameLogEntry entry)
        {
            _queue.Enqueue(entry);

            
            while (_queue.Count > _capacity)
                _queue.TryDequeue(out _);

            try { EntryAdded?.Invoke(entry); } catch { }
        }

        
        
        
        public IReadOnlyList<FrameLogEntry> Snapshot()
            => _queue.ToArray();

        
        
        
        public bool TryDequeue(out FrameLogEntry entry)
            => _queue.TryDequeue(out entry);

        
        public void Clear()
        {
            while (_queue.TryDequeue(out _)) { }
        }
    }
}
