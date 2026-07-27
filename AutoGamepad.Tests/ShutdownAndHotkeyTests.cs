using Xunit;

namespace AutoGamepad.Tests
{
    public class ShutdownAndHotkeyTests
    {
        [Fact]
        public void GlobalHotkeys_ActivateAndDeactivateAsAGroup()
        {
            var registrar = new FakeHotkeyRegistrar();
            using var manager = new GlobalHotkeyManager(registrar, new IntPtr(123));
            GlobalHotkeyDefinition[] definitions =
            [
                new(1, 6, 0x78, "Iniciar"),
                new(2, 6, 0x79, "Parar")
            ];

            GlobalHotkeyActivationResult result = manager.Activate(definitions);

            Assert.True(result.IsActive);
            Assert.Empty(result.Failures);
            Assert.True(manager.IsRegistered(1));
            Assert.True(manager.IsRegistered(2));

            manager.Deactivate();

            Assert.False(manager.IsActive);
            Assert.False(manager.IsRegistered(1));
            Assert.Equal([1, 2], registrar.UnregisteredIds.Order());
        }

        [Fact]
        public void GlobalHotkeys_RollBackPartialRegistrationWhenOneConflicts()
        {
            var registrar = new FakeHotkeyRegistrar();
            registrar.FailingIds.Add(2);
            using var manager = new GlobalHotkeyManager(registrar, new IntPtr(123));
            GlobalHotkeyDefinition[] definitions =
            [
                new(1, 6, 0x78, "Iniciar"),
                new(2, 6, 0x79, "Parar")
            ];

            GlobalHotkeyActivationResult result = manager.Activate(definitions);

            Assert.False(result.IsActive);
            GlobalHotkeyFailure failure = Assert.Single(result.Failures);
            Assert.Equal(2, failure.Definition.Id);
            Assert.Equal(1409, failure.ErrorCode);
            Assert.False(manager.IsRegistered(1));
            Assert.Contains(1, registrar.UnregisteredIds);
        }

        [Fact]
        public async Task ShutdownWaiter_ObservesCompletedAndCanceledTasks()
        {
            Assert.True(await ShutdownTaskWaiter.WaitForCompletionAsync(
                Task.CompletedTask,
                TimeSpan.FromSeconds(1)));

            Assert.True(await ShutdownTaskWaiter.WaitForCompletionAsync(
                Task.FromCanceled(new CancellationToken(canceled: true)),
                TimeSpan.FromSeconds(1)));
        }

        [Fact]
        public async Task ShutdownWaiter_ReturnsFalseWhenTimeoutExpires()
        {
            var pending = new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously);

            bool completed = await ShutdownTaskWaiter.WaitForCompletionAsync(
                pending.Task,
                TimeSpan.FromMilliseconds(20));

            Assert.False(completed);
        }

        private sealed class FakeHotkeyRegistrar : IGlobalHotkeyRegistrar
        {
            private readonly HashSet<int> _registeredIds = new();

            public HashSet<int> FailingIds { get; } = [];
            public List<int> UnregisteredIds { get; } = [];

            public bool Register(
                IntPtr windowHandle,
                int id,
                int modifiers,
                int virtualKey,
                out int errorCode)
            {
                if (FailingIds.Contains(id))
                {
                    errorCode = 1409;
                    return false;
                }

                errorCode = 0;
                return _registeredIds.Add(id);
            }

            public bool Unregister(IntPtr windowHandle, int id)
            {
                UnregisteredIds.Add(id);
                return _registeredIds.Remove(id);
            }
        }
    }
}
