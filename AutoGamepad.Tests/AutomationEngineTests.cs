using System.Collections.Concurrent;
using System.Text.Json;
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
        public async Task RunAsync_ReportsCycleLineAndActionProgress()
        {
            var updates = new ConcurrentQueue<AutomationProgress>();
            var engine = new AutomationEngine(
                new FakeGamepadOutput(),
                _ => { },
                progress: updates.Enqueue);
            AutomationStep first = CreateStep(ActionType.Hold, GamepadControl.A);
            AutomationStep second = CreateStep(ActionType.Release, GamepadControl.A);

            await engine.RunAsync(
                CreateProgram([first, second], useCycleLimit: true, maxCycles: 2),
                CancellationToken.None);

            AutomationProgress[] actual = updates.ToArray();
            Assert.Equal(4, actual.Length);

            Assert.Equal(1, actual[0].CycleNumber);
            Assert.Equal(2, actual[0].TotalCycles);
            Assert.Equal(0, actual[0].StepIndex);
            Assert.Equal(1, actual[0].LineNumber);
            Assert.Equal(2, actual[0].StepCount);
            Assert.Equal(ActionType.Hold, actual[0].Action);
            Assert.Equal(ActionType.Hold.ToString(), actual[0].ActionLabel);

            Assert.Equal(2, actual[3].CycleNumber);
            Assert.Equal(1, actual[3].StepIndex);
            Assert.Equal(2, actual[3].LineNumber);
            Assert.Equal(ActionType.Release, actual[3].Action);
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

        [Theory]
        [InlineData(ActionType.Wait, (int)GamepadControl.A, (int)GamepadControl.None, false)]
        [InlineData(ActionType.Log, (int)GamepadControl.A, (int)GamepadControl.None, false)]
        [InlineData(ActionType.PressAndRelease, (int)GamepadControl.None, (int)GamepadControl.A, true)]
        [InlineData(ActionType.Hold, (int)GamepadControl.None, (int)GamepadControl.A, true)]
        [InlineData(ActionType.Release, (int)GamepadControl.B, (int)GamepadControl.B, true)]
        public void SequenceGridRules_NormalizeControlForSelectedAction(
            ActionType action,
            int currentControlValue,
            int expectedControlValue,
            bool expectedEditable)
        {
            var currentControl = (GamepadControl)currentControlValue;
            var expectedControl = (GamepadControl)expectedControlValue;

            Assert.Equal(expectedControl, SequenceGridRules.NormalizeControl(action, currentControl));
            Assert.Equal(expectedEditable, SequenceGridRules.IsControlEditable(action));
        }

        [Theory]
        [InlineData(ActionType.PressAndRelease, true, true)]
        [InlineData(ActionType.Hold, true, true)]
        [InlineData(ActionType.Release, true, true)]
        [InlineData(ActionType.Wait, true, false)]
        [InlineData(ActionType.Log, true, false)]
        [InlineData(ActionType.PressAndRelease, false, false)]
        public void SequenceGridRules_EnablesJitterForEveryAxisRamp(
            ActionType action,
            bool isAxis,
            bool expected)
        {
            Assert.Equal(expected, SequenceGridRules.IsJitterEditable(action, isAxis));
        }

        [Theory]
        [InlineData(ActionType.PressAndRelease, true, true, true, true)]
        [InlineData(ActionType.Hold, true, true, true, false)]
        [InlineData(ActionType.Release, true, false, true, false)]
        [InlineData(ActionType.Wait, false, false, false, true)]
        [InlineData(ActionType.Log, false, false, false, false)]
        [InlineData(ActionType.PressAndRelease, false, false, false, true)]
        [InlineData(ActionType.Hold, false, false, false, false)]
        [InlineData(ActionType.Release, false, false, false, false)]
        public void SequenceGridRules_EnablesOnlyNumericFieldsApplicableToAction(
            ActionType action,
            bool isAxis,
            bool expectedAxisValue,
            bool expectedRamp,
            bool expectedDuration)
        {
            Assert.Equal(expectedAxisValue, SequenceGridRules.IsAxisValueEditable(action, isAxis));
            Assert.Equal(expectedRamp, SequenceGridRules.IsRampEditable(action, isAxis));
            Assert.Equal(expectedDuration, SequenceGridRules.IsDurationEditable(action));
        }

        [Theory]
        [InlineData(0, null, 0)]
        [InlineData(3, null, 3)]
        [InlineData(3, 0, 0)]
        [InlineData(3, 1, 1)]
        [InlineData(3, 2, 2)]
        public void SequenceRowPositionRules_InsertAboveSelectionOrAppendWhenUnselected(
            int rowCount,
            int? selectedRowIndex,
            int expectedInsertionIndex)
        {
            Assert.Equal(
                expectedInsertionIndex,
                SequenceRowPositionRules.GetInsertionIndex(rowCount, selectedRowIndex));
        }

        [Theory]
        [InlineData(0, 0, null)]
        [InlineData(2, 0, 0)]
        [InlineData(2, 1, 1)]
        [InlineData(2, 2, 1)]
        public void SequenceRowPositionRules_SelectsFollowingRowOrPreviousWhenLastWasRemoved(
            int remainingRowCount,
            int removedRowIndex,
            int? expectedSelectionIndex)
        {
            Assert.Equal(
                expectedSelectionIndex,
                SequenceRowPositionRules.GetSelectionIndexAfterRemoval(remainingRowCount, removedRowIndex));
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
        public async Task RunAsync_HoldKeepsAxisJitterActiveDuringWait()
        {
            var output = new FakeGamepadOutput();
            var engine = new AutomationEngine(output, _ => { }, new MaximumRandom());
            AutomationStep hold = CreateStep(
                ActionType.Hold,
                GamepadControl.LeftTrigger,
                valuePercent: 50,
                jitterForcePercent: 10);
            AutomationStep wait = CreateStep(
                ActionType.Wait,
                GamepadControl.None,
                durationMinMs: 60,
                durationMaxMs: 60);
            AutomationStep release = CreateStep(ActionType.Release, GamepadControl.LeftTrigger);

            await engine.RunAsync(
                CreateProgram(
                    [hold, wait, release],
                    useCycleLimit: true,
                    enableJitter: true,
                    jitterFrequencyMs: 5),
                CancellationToken.None);

            Assert.True(output.AxisEvents.Count(value =>
                value == (AxisChannel.LeftTrigger, 60f)) >= 2);
            Assert.Equal((AxisChannel.LeftTrigger, 0f), output.AxisEvents.Last());
        }

        [Fact]
        public async Task RunAsync_AxisTapAppliesJitterToDescendingRamp()
        {
            var output = new FakeGamepadOutput();
            var engine = new AutomationEngine(output, _ => { }, new MaximumRandom());
            AutomationStep tap = CreateStep(
                ActionType.PressAndRelease,
                GamepadControl.LeftTrigger,
                valuePercent: 50,
                rampMinMs: 40,
                rampMaxMs: 40,
                jitterForcePercent: 10);

            await engine.RunAsync(
                CreateProgram(
                    [tap],
                    useCycleLimit: true,
                    enableJitter: true,
                    jitterFrequencyMs: 1),
                CancellationToken.None);

            Assert.Contains(
                output.AxisEvents.Zip(output.AxisEvents.Skip(1)),
                pair => pair.First == (AxisChannel.LeftTrigger, 50f)
                    && pair.Second == (AxisChannel.LeftTrigger, 60f));
            Assert.Equal((AxisChannel.LeftTrigger, 0f), output.AxisEvents.Last());
        }

        [Fact]
        public async Task RunAsync_AxisReleaseUsesItsOwnJitter()
        {
            var output = new FakeGamepadOutput();
            var engine = new AutomationEngine(output, _ => { }, new MaximumRandom());
            AutomationStep hold = CreateStep(
                ActionType.Hold,
                GamepadControl.LeftTrigger,
                valuePercent: 50);
            AutomationStep release = CreateStep(
                ActionType.Release,
                GamepadControl.LeftTrigger,
                rampMinMs: 40,
                rampMaxMs: 40,
                jitterForcePercent: 10);

            await engine.RunAsync(
                CreateProgram(
                    [hold, release],
                    useCycleLimit: true,
                    enableJitter: true,
                    jitterFrequencyMs: 1),
                CancellationToken.None);

            Assert.Contains(
                output.AxisEvents.Zip(output.AxisEvents.Skip(1)),
                pair => pair.First == (AxisChannel.LeftTrigger, 50f)
                    && pair.Second == (AxisChannel.LeftTrigger, 60f));
            Assert.Equal((AxisChannel.LeftTrigger, 0f), output.AxisEvents.Last());
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

        [Fact]
        public async Task RunAsync_LogMarkerWritesMessageWithoutDelayOrGamepadOutput()
        {
            var output = new FakeGamepadOutput();
            var messages = new ConcurrentQueue<string>();
            var engine = new AutomationEngine(output, messages.Enqueue);
            AutomationStep marker = CreateStep(
                ActionType.Log,
                GamepadControl.None,
                durationMinMs: int.MaxValue,
                durationMaxMs: int.MaxValue,
                message: "Iniciando movimento lateral");

            await engine.RunAsync(CreateProgram([marker], useCycleLimit: true), CancellationToken.None)
                .WaitAsync(TimeSpan.FromSeconds(2));

            Assert.Contains(
                messages,
                message => message == "[Linha 1] [MARCADOR] Iniciando movimento lateral");
            Assert.Empty(output.DigitalEvents);
            Assert.Empty(output.AxisEvents);
        }

        [Fact]
        public void SequenceStep_MessageRemainsOptionalForLegacyProfiles()
        {
            const string legacyJson = """
                {
                  "Action": "Tap",
                  "Button": "A"
                }
                """;

            SequenceStep step = JsonSerializer.Deserialize<SequenceStep>(legacyJson)!;
            string serialized = JsonSerializer.Serialize(step);

            Assert.Null(step.Message);
            Assert.DoesNotContain("\"Message\"", serialized, StringComparison.Ordinal);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("-")]
        [InlineData("texto")]
        [InlineData("2147483648")]
        public void SequenceNumericRules_RejectsMissingOrInvalidRequiredValues(string? text)
        {
            Assert.False(SequenceNumericRules.TryParseRequired(text, out _));
        }

        [Theory]
        [InlineData("0", 0)]
        [InlineData("1", 1)]
        [InlineData("100", 100)]
        [InlineData("2147483647", int.MaxValue)]
        public void SequenceNumericRules_ParsesExplicitIntegerValues(string text, int expected)
        {
            Assert.True(SequenceNumericRules.TryParseRequired(text, out int actual));
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(0, false)]
        [InlineData(1, true)]
        [InlineData(100, true)]
        [InlineData(101, false)]
        public void SequenceNumericRules_RequiresEffectiveAxisMagnitude(int value, bool expected)
        {
            Assert.Equal(expected, SequenceNumericRules.IsAxisMagnitudeValid(value));
        }

        [Fact]
        public void SequenceStep_MissingNumericValuesRemainAbsentInJson()
        {
            const string incompleteJson = """
                {
                  "Action": "Tap",
                  "Button": "LT"
                }
                """;

            SequenceStep step = JsonSerializer.Deserialize<SequenceStep>(incompleteJson)!;
            string serialized = JsonSerializer.Serialize(step);

            Assert.Null(step.ValuePercent);
            Assert.Null(step.WaitMin);
            Assert.DoesNotContain("\"ValuePercent\"", serialized, StringComparison.Ordinal);
            Assert.DoesNotContain("\"WaitMin\"", serialized, StringComparison.Ordinal);
        }

        [Fact]
        public void SequenceStep_LogMessageRoundTripsThroughJson()
        {
            var expected = new SequenceStep
            {
                Action = "Log",
                Button = "None",
                Message = "Checkpoint carregado"
            };

            string json = JsonSerializer.Serialize(expected);
            SequenceStep actual = JsonSerializer.Deserialize<SequenceStep>(json)!;

            Assert.Equal(expected.Message, actual.Message);
        }

        [Theory]
        [InlineData(ActionType.Log, (int)GamepadControl.None, 500, 900, 1_000, 2_000, 0L, 0L)]
        [InlineData(ActionType.Wait, (int)GamepadControl.None, 500, 900, 1_000, 2_000, 1_000L, 2_000L)]
        [InlineData(ActionType.PressAndRelease, (int)GamepadControl.A, 500, 900, 1_000, 2_000, 1_000L, 2_000L)]
        [InlineData(ActionType.PressAndRelease, (int)GamepadControl.LeftTrigger, 500, 900, 1_000, 2_000, 2_000L, 3_800L)]
        [InlineData(ActionType.Hold, (int)GamepadControl.A, 500, 900, 1_000, 2_000, 0L, 0L)]
        [InlineData(ActionType.Hold, (int)GamepadControl.LeftTrigger, 500, 900, 1_000, 2_000, 500L, 900L)]
        [InlineData(ActionType.Release, (int)GamepadControl.A, 500, 900, 1_000, 2_000, 0L, 0L)]
        [InlineData(ActionType.Release, (int)GamepadControl.LeftTrigger, 500, 900, 1_000, 2_000, 500L, 900L)]
        public void SequenceTimeEstimator_CalculatesEveryActionAndControlCombination(
            ActionType action,
            int controlValue,
            int rampMinMs,
            int rampMaxMs,
            int durationMinMs,
            int durationMaxMs,
            long expectedMinimumMs,
            long expectedMaximumMs)
        {
            AutomationStep step = CreateStep(
                action,
                (GamepadControl)controlValue,
                rampMinMs: rampMinMs,
                rampMaxMs: rampMaxMs,
                durationMinMs: durationMinMs,
                durationMaxMs: durationMaxMs);

            SequenceTimeEstimate estimate = SequenceTimeEstimator.Calculate(CreateProgram([step]));

            Assert.Equal(
                new TimeEstimateRange(expectedMinimumMs, expectedMaximumMs),
                estimate.StepDurations[0]);
        }

        [Fact]
        public void SequenceTimeEstimator_AccumulatesStepsInSequenceOrder()
        {
            AutomationStep wait = CreateStep(
                ActionType.Wait,
                GamepadControl.None,
                durationMinMs: 100,
                durationMaxMs: 200);
            AutomationStep axisTap = CreateStep(
                ActionType.PressAndRelease,
                GamepadControl.LeftTrigger,
                rampMinMs: 50,
                rampMaxMs: 100,
                durationMinMs: 300,
                durationMaxMs: 400);
            AutomationStep marker = CreateStep(ActionType.Log, GamepadControl.None);

            SequenceTimeEstimate estimate = SequenceTimeEstimator.Calculate(
                CreateProgram([wait, axisTap, marker]));

            Assert.Equal(new TimeEstimateRange(100, 200), estimate.CumulativeDurations[0]);
            Assert.Equal(new TimeEstimateRange(500, 800), estimate.CumulativeDurations[1]);
            Assert.Equal(new TimeEstimateRange(500, 800), estimate.CumulativeDurations[2]);
            Assert.Equal(new TimeEstimateRange(500, 800), estimate.CycleDuration);
        }

        [Fact]
        public void SequenceTimeEstimator_AppliesMinimumCycleDurationToInstantSequence()
        {
            AutomationStep marker = CreateStep(ActionType.Log, GamepadControl.None);

            SequenceTimeEstimate estimate = SequenceTimeEstimator.Calculate(CreateProgram([marker]));

            Assert.Equal(TimeEstimateRange.Zero, estimate.CumulativeDurations[0]);
            Assert.Equal(
                new TimeEstimateRange(
                    SequenceTimeEstimator.MinimumCycleDurationMs,
                    SequenceTimeEstimator.MinimumCycleDurationMs),
                estimate.CycleDuration);
        }

        [Fact]
        public void SequenceTimeEstimator_MultipliesCycleRangeWhenCycleLimitIsEnabled()
        {
            AutomationStep wait = CreateStep(
                ActionType.Wait,
                GamepadControl.None,
                durationMinMs: 100,
                durationMaxMs: 250);

            SequenceTimeEstimate estimate = SequenceTimeEstimator.Calculate(
                CreateProgram([wait], useCycleLimit: true, maxCycles: 4));

            Assert.False(estimate.IsContinuous);
            Assert.Equal(new TimeEstimateRange(400, 1_000), estimate.TotalDuration);
        }

        [Fact]
        public void SequenceTimeEstimator_ReportsContinuousExecutionWithoutCycleLimit()
        {
            AutomationStep wait = CreateStep(
                ActionType.Wait,
                GamepadControl.None,
                durationMinMs: 100,
                durationMaxMs: 250);

            SequenceTimeEstimate estimate = SequenceTimeEstimator.Calculate(CreateProgram([wait]));

            Assert.True(estimate.IsContinuous);
            Assert.Null(estimate.TotalDuration);
        }

        [Fact]
        public void SequenceTimeEstimator_UsesLongArithmeticForMaximumIntRanges()
        {
            AutomationStep axisTap = CreateStep(
                ActionType.PressAndRelease,
                GamepadControl.LeftTrigger,
                rampMinMs: int.MaxValue,
                rampMaxMs: int.MaxValue,
                durationMinMs: int.MaxValue,
                durationMaxMs: int.MaxValue);

            SequenceTimeEstimate estimate = SequenceTimeEstimator.Calculate(CreateProgram([axisTap]));

            Assert.Equal(
                new TimeEstimateRange(6_442_450_941L, 6_442_450_941L),
                estimate.StepDurations[0]);
        }

        private static AutomationProgram CreateProgram(
            IReadOnlyList<AutomationStep> steps,
            bool useCycleLimit = false,
            int maxCycles = 1,
            bool enableJitter = false,
            int jitterFrequencyMs = 100)
        {
            return new AutomationProgram(
                useCycleLimit,
                maxCycles,
                enableJitter,
                jitterFrequencyMs,
                steps);
        }

        private static AutomationStep CreateStep(
            ActionType action,
            GamepadControl control,
            int valuePercent = 100,
            int rampMinMs = 0,
            int rampMaxMs = 0,
            int durationMinMs = 0,
            int durationMaxMs = 0,
            string message = "",
            int jitterForcePercent = 0)
        {
            return new AutomationStep(
                action,
                action.ToString(),
                control,
                control.ToString(),
                message,
                valuePercent,
                rampMinMs,
                rampMaxMs,
                durationMinMs,
                durationMaxMs,
                jitterForcePercent);
        }

        private sealed class FakeGamepadOutput : IGamepadOutput
        {
            private int _resetCount;
            private readonly ConcurrentDictionary<GamepadControl, bool> _digitalStates = new();

            public ConcurrentQueue<(AxisChannel Channel, float Value)> AxisEvents { get; } = new();
            public ConcurrentQueue<(GamepadControl Control, bool IsPressed)> DigitalEvents { get; } = new();
            public TaskCompletionSource<GamepadControl> DigitalPressed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
            public int ResetCount => _resetCount;

            public bool IsPressed(GamepadControl control)
            {
                return _digitalStates.GetValueOrDefault(control);
            }

            public void SetDigital(GamepadControl control, bool isPressed)
            {
                DigitalEvents.Enqueue((control, isPressed));
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

        private sealed class MaximumRandom : Random
        {
            public override long NextInt64(long minValue, long maxValue)
            {
                return maxValue - 1;
            }
        }
    }
}
