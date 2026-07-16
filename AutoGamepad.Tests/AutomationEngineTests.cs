using System.Collections.Concurrent;
using Xunit;

namespace AutoGamepad.Tests
{
    public class AutomationEngineTests
    {
        [Fact]
        public async Task RunAsync_RejectsEmptySequence()
        {
            var engine = new AutomationEngine(new FakeGamepadOutput(), _ => { });
            AutomationProgram program = CreateProgram([]);

            InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(
                () => engine.RunAsync(program, CancellationToken.None));

            Assert.Contains("pelo menos uma etapa", error.Message);
        }

        [Fact]
        public void OppositeStickDirections_UseSamePhysicalChannelWithOppositeSigns()
        {
            Assert.True(GamepadControlCatalog.TryGetAxisBinding(GamepadControl.LeftStickLeft, out AxisBinding left));
            Assert.True(GamepadControlCatalog.TryGetAxisBinding(GamepadControl.LeftStickRight, out AxisBinding right));

            Assert.Equal(left.Channel, right.Channel);
            Assert.Equal(-1, left.Direction);
            Assert.Equal(1, right.Direction);
        }

        [Fact]
        public async Task RunAsync_UsesSignedValuesForStickDirections()
        {
            var output = new FakeGamepadOutput();
            var engine = new AutomationEngine(output, _ => { });
            AutomationStep tapLeft = CreateStep(
                ActionType.PressAndRelease,
                GamepadControl.LeftStickLeft,
                valuePercent: 100);

            await engine.RunAsync(CreateProgram([tapLeft], useCycleLimit: true), CancellationToken.None);

            Assert.Contains((AxisChannel.LeftStickX, -100f), output.AxisEvents);
            Assert.Equal((AxisChannel.LeftStickX, 0f), output.AxisEvents.Last());
        }

        [Fact]
        public async Task RunAsync_CancellationDuringHoldNeutralizesDigitalButtons()
        {
            var output = new FakeGamepadOutput();
            var engine = new AutomationEngine(output, _ => { });
            AutomationStep hold = CreateStep(ActionType.Hold, GamepadControl.A);
            AutomationStep wait = CreateStep(
                ActionType.Wait,
                GamepadControl.None,
                durationMinMs: 10_000,
                durationMaxMs: 10_000);
            AutomationStep release = CreateStep(ActionType.Release, GamepadControl.A);
            using var cancellation = new CancellationTokenSource();
            Task run = engine.RunAsync(CreateProgram([hold, wait, release]), cancellation.Token);

            GamepadControl pressedControl = await output.DigitalPressed.Task.WaitAsync(TimeSpan.FromSeconds(2));
            Assert.Equal(GamepadControl.A, pressedControl);
            Assert.True(output.IsPressed(GamepadControl.A));

            cancellation.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run);
            Assert.False(output.IsPressed(GamepadControl.A));
            Assert.Equal(1, output.ResetCount);
        }

        [Fact]
        public async Task RunAsync_AllowsIntMaxValueWithoutInclusiveRangeOverflow()
        {
            var output = new FakeGamepadOutput();
            var reachedStep = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var engine = new AutomationEngine(output, message =>
            {
                if (message.StartsWith("[Linha", StringComparison.Ordinal))
                {
                    reachedStep.TrySetResult();
                }
            }, new Random(1234));

            AutomationStep wait = CreateStep(
                ActionType.Wait,
                GamepadControl.None,
                durationMinMs: int.MaxValue - 1,
                durationMaxMs: int.MaxValue);
            using var cancellation = new CancellationTokenSource();
            Task run = engine.RunAsync(CreateProgram([wait]), cancellation.Token);

            await reachedStep.Task.WaitAsync(TimeSpan.FromSeconds(2));
            cancellation.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run);
        }

        [Fact]
        public async Task RunAsync_InstantSequenceRemainsCancellable()
        {
            var output = new FakeGamepadOutput();
            var firstCycle = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var engine = new AutomationEngine(output, message =>
            {
                if (message.Contains("Ciclo 1", StringComparison.Ordinal))
                {
                    firstCycle.TrySetResult();
                }
            });

            AutomationStep hold = CreateStep(ActionType.Hold, GamepadControl.A);
            AutomationStep release = CreateStep(ActionType.Release, GamepadControl.A);
            using var cancellation = new CancellationTokenSource();
            Task run = Task.Run(() => engine.RunAsync(CreateProgram([hold, release]), cancellation.Token));

            await firstCycle.Task.WaitAsync(TimeSpan.FromSeconds(2));
            cancellation.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => run.WaitAsync(TimeSpan.FromSeconds(2)));
            Assert.Equal(1, output.ResetCount);
        }

        private static AutomationProgram CreateProgram(
            IReadOnlyList<AutomationStep> steps,
            bool useCycleLimit = false)
        {
            return new AutomationProgram(
                useCycleLimit,
                1,
                false,
                100,
                steps);
        }

        private static AutomationStep CreateStep(
            ActionType action,
            GamepadControl control,
            int valuePercent = 100,
            int durationMinMs = 0,
            int durationMaxMs = 0)
        {
            return new AutomationStep(
                action,
                action.ToString(),
                control,
                control.ToString(),
                valuePercent,
                0,
                0,
                durationMinMs,
                durationMaxMs,
                0);
        }

        private sealed class FakeGamepadOutput : IGamepadOutput
        {
            private int _resetCount;
            private readonly ConcurrentDictionary<GamepadControl, bool> _digitalStates = new();

            public ConcurrentQueue<(AxisChannel Channel, float Value)> AxisEvents { get; } = new();
            public TaskCompletionSource<GamepadControl> DigitalPressed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
            public int ResetCount => _resetCount;

            public bool IsPressed(GamepadControl control)
            {
                return _digitalStates.GetValueOrDefault(control);
            }

            public void SetDigital(GamepadControl control, bool isPressed)
            {
                _digitalStates[control] = isPressed;
                if (isPressed)
                {
                    DigitalPressed.TrySetResult(control);
                }
            }

            public void SetAxis(AxisChannel channel, float valuePercent)
            {
                AxisEvents.Enqueue((channel, valuePercent));
            }

            public void Reset()
            {
                foreach (GamepadControl control in _digitalStates.Keys)
                {
                    _digitalStates[control] = false;
                }

                Interlocked.Increment(ref _resetCount);
            }
        }
    }
}
