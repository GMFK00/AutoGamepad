using Xunit;

namespace AutoGamepad.Tests
{
    public class BoundedLogBufferTests
    {
        [Fact]
        public void Append_FlushesCompleteBatchAtThreshold()
        {
            var written = new List<string>();
            var buffer = new BoundedLogBuffer(
                flushThreshold: 3,
                capacity: 5,
                retryDelay: TimeSpan.FromSeconds(2),
                batch => written.AddRange(batch));
            DateTimeOffset now = DateTimeOffset.UtcNow;

            buffer.Append("linha 1", now);
            buffer.Append("linha 2", now);
            LogBufferResult result = buffer.Append("linha 3", now);

            Assert.True(result.WriteAttempted);
            Assert.True(result.WriteSucceeded);
            Assert.Equal(["linha 1", "linha 2", "linha 3"], written);
            Assert.Equal(0, buffer.PendingLineCount);
            Assert.Equal(0, buffer.DroppedLineCount);
        }

        [Fact]
        public void Append_PermanentFailureKeepsMemoryBoundedAndHonorsBackoff()
        {
            int writeAttempts = 0;
            var buffer = new BoundedLogBuffer(
                flushThreshold: 2,
                capacity: 5,
                retryDelay: TimeSpan.FromSeconds(2),
                _ =>
                {
                    writeAttempts++;
                    throw new IOException("sem permissão");
                });
            DateTimeOffset now = DateTimeOffset.UtcNow;

            for (int index = 0; index < 12; index++)
            {
                buffer.Append($"linha {index}", now);
            }

            Assert.Equal(1, writeAttempts);
            Assert.Equal(5, buffer.PendingLineCount);
            Assert.Equal(7, buffer.DroppedLineCount);
        }

        [Fact]
        public void Append_RetriesAndReportsRecoveryAfterDelay()
        {
            bool canWrite = false;
            var written = new List<string>();
            var buffer = new BoundedLogBuffer(
                flushThreshold: 2,
                capacity: 5,
                retryDelay: TimeSpan.FromSeconds(2),
                batch =>
                {
                    if (!canWrite)
                    {
                        throw new UnauthorizedAccessException("sem permissão");
                    }

                    written.AddRange(batch);
                });
            DateTimeOffset now = DateTimeOffset.UtcNow;

            LogBufferResult failure = buffer.Append("linha 1", now);
            failure = buffer.Append("linha 2", now);
            canWrite = true;
            buffer.Append("linha 3", now.AddSeconds(1));
            LogBufferResult recovery = buffer.Append("linha 4", now.AddSeconds(2));

            Assert.True(failure.FailureStarted);
            Assert.False(recovery.FailureStarted);
            Assert.True(recovery.Recovered);
            Assert.Equal(["linha 1", "linha 2", "linha 3", "linha 4"], written);
            Assert.Equal(0, buffer.PendingLineCount);
        }

        [Fact]
        public void Flush_ForcedAttemptIgnoresRetryDelay()
        {
            bool canWrite = false;
            var written = new List<string>();
            var buffer = new BoundedLogBuffer(
                flushThreshold: 1,
                capacity: 2,
                retryDelay: TimeSpan.FromMinutes(1),
                batch =>
                {
                    if (!canWrite)
                    {
                        throw new IOException("falha temporária");
                    }

                    written.AddRange(batch);
                });
            DateTimeOffset now = DateTimeOffset.UtcNow;

            buffer.Append("linha pendente", now);
            canWrite = true;
            LogBufferResult result = buffer.Flush(now, force: true);

            Assert.True(result.WriteAttempted);
            Assert.True(result.WriteSucceeded);
            Assert.True(result.Recovered);
            Assert.Equal(["linha pendente"], written);
        }
    }
}
