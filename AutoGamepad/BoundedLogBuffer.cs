namespace AutoGamepad
{
    internal readonly record struct LogBufferResult(
        bool WriteAttempted,
        bool WriteSucceeded,
        bool FailureStarted,
        bool Recovered,
        int PendingLineCount,
        long DroppedLineCount,
        string? ErrorMessage);

    internal sealed class BoundedLogBuffer
    {
        private readonly int _flushThreshold;
        private readonly int _capacity;
        private readonly TimeSpan _retryDelay;
        private readonly Action<IReadOnlyList<string>> _writeBatch;
        private readonly Queue<string> _pendingLines;
        private readonly object _sync = new();

        private DateTimeOffset _nextRetryAt = DateTimeOffset.MinValue;
        private bool _writeUnavailable;
        private long _droppedLineCount;

        public BoundedLogBuffer(
            int flushThreshold,
            int capacity,
            TimeSpan retryDelay,
            Action<IReadOnlyList<string>> writeBatch)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(flushThreshold, 1);
            ArgumentOutOfRangeException.ThrowIfLessThan(capacity, flushThreshold);
            ArgumentOutOfRangeException.ThrowIfLessThan(retryDelay, TimeSpan.Zero);
            ArgumentNullException.ThrowIfNull(writeBatch);

            _flushThreshold = flushThreshold;
            _capacity = capacity;
            _retryDelay = retryDelay;
            _writeBatch = writeBatch;
            _pendingLines = new Queue<string>(capacity);
        }

        public int PendingLineCount
        {
            get
            {
                lock (_sync)
                {
                    return _pendingLines.Count;
                }
            }
        }

        public long DroppedLineCount
        {
            get
            {
                lock (_sync)
                {
                    return _droppedLineCount;
                }
            }
        }

        public LogBufferResult Append(string line, DateTimeOffset now)
        {
            ArgumentNullException.ThrowIfNull(line);

            lock (_sync)
            {
                var attempt = new WriteAttempt();

                if (_pendingLines.Count >= _capacity)
                {
                    TryFlushLocked(now, force: false, attempt);

                    if (_pendingLines.Count >= _capacity)
                    {
                        _pendingLines.Dequeue();
                        _droppedLineCount++;
                    }
                }

                _pendingLines.Enqueue(line);

                if (_pendingLines.Count >= _flushThreshold)
                {
                    TryFlushLocked(now, force: false, attempt);
                }

                return CreateResult(attempt);
            }
        }

        public LogBufferResult Flush(DateTimeOffset now, bool force)
        {
            lock (_sync)
            {
                var attempt = new WriteAttempt();
                TryFlushLocked(now, force, attempt);
                return CreateResult(attempt);
            }
        }

        private void TryFlushLocked(DateTimeOffset now, bool force, WriteAttempt attempt)
        {
            if (_pendingLines.Count == 0
                || attempt.WriteAttempted
                || (!force && now < _nextRetryAt))
            {
                return;
            }

            attempt.WriteAttempted = true;
            string[] batch = _pendingLines.ToArray();

            try
            {
                _writeBatch(batch);
                _pendingLines.Clear();
                attempt.WriteSucceeded = true;
                attempt.Recovered = _writeUnavailable;
                _writeUnavailable = false;
                _nextRetryAt = DateTimeOffset.MinValue;
            }
            catch (Exception ex)
            {
                attempt.FailureStarted = !_writeUnavailable;
                attempt.ErrorMessage = ex.Message;
                _writeUnavailable = true;
                _nextRetryAt = now + _retryDelay;
            }
        }

        private LogBufferResult CreateResult(WriteAttempt attempt)
        {
            return new LogBufferResult(
                attempt.WriteAttempted,
                attempt.WriteSucceeded,
                attempt.FailureStarted,
                attempt.Recovered,
                _pendingLines.Count,
                _droppedLineCount,
                attempt.ErrorMessage);
        }

        private sealed class WriteAttempt
        {
            public bool WriteAttempted { get; set; }
            public bool WriteSucceeded { get; set; }
            public bool FailureStarted { get; set; }
            public bool Recovered { get; set; }
            public string? ErrorMessage { get; set; }
        }
    }
}
