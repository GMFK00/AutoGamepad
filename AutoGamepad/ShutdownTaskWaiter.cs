namespace AutoGamepad
{
    internal static class ShutdownTaskWaiter
    {
        public static async Task<bool> WaitForCompletionAsync(
            Task? task,
            TimeSpan timeout)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(timeout, TimeSpan.Zero);

            if (task is null)
            {
                return true;
            }

            if (task.IsCompleted)
            {
                await ObserveCompletionAsync(task).ConfigureAwait(false);
                return true;
            }

            using var timeoutCancellation = new CancellationTokenSource();
            Task timeoutTask = Task.Delay(timeout, timeoutCancellation.Token);
            Task completedTask = await Task.WhenAny(task, timeoutTask).ConfigureAwait(false);

            if (completedTask != task)
            {
                return false;
            }

            timeoutCancellation.Cancel();
            await ObserveCompletionAsync(task).ConfigureAwait(false);
            return true;
        }

        private static async Task ObserveCompletionAsync(Task task)
        {
            try
            {
                await task.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Cancelamento é a conclusão esperada durante o encerramento.
            }
            catch
            {
                // O fluxo que iniciou o motor é responsável por registrar a falha.
            }
        }
    }
}
