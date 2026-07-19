using System.Diagnostics;

namespace AutoGamepad
{
    internal interface IGamepadOutput
    {
        void SetDigital(GamepadControl control, bool isPressed);
        void SetAxis(AxisChannel channel, float valuePercent);
        void Reset();
    }

    internal sealed class AutomationEngine
    {
        private const int FrameDelayMs = 16;

        private readonly IGamepadOutput _output;
        private readonly Action<string> _log;
        private readonly Action<AutomationProgress>? _progress;
        private readonly Random _random;
        private readonly Dictionary<AxisChannel, float> _axisStates = new();

        public AutomationEngine(
            IGamepadOutput output,
            Action<string> log,
            Random? random = null,
            Action<AutomationProgress>? progress = null)
        {
            _output = output;
            _log = log;
            _progress = progress;
            _random = random ?? new Random();
        }

        public async Task RunAsync(AutomationProgram program, CancellationToken token)
        {
            ArgumentNullException.ThrowIfNull(program);

            if (program.Steps.Count == 0)
            {
                throw new InvalidOperationException("A sequência precisa conter pelo menos uma etapa.");
            }

            _axisStates.Clear();

            try
            {
                _log("Iniciando execução da tabela de sequências...");

                int loopCount = 1;
                while (true)
                {
                    token.ThrowIfCancellationRequested();

                    if (program.UseCycleLimit && loopCount > program.MaxCycles)
                    {
                        _log($"\n[INFO] Limite de {program.MaxCycles} ciclos atingido. Finalizando com sucesso.");
                        break;
                    }

                    _log($"\n=== Iniciando Ciclo {loopCount} ===");
                    var cycleTimer = Stopwatch.StartNew();

                    for (int index = 0; index < program.Steps.Count; index++)
                    {
                        token.ThrowIfCancellationRequested();
                        AutomationStep step = program.Steps[index];
                        _progress?.Invoke(new AutomationProgress(
                            loopCount,
                            program.UseCycleLimit ? program.MaxCycles : null,
                            index,
                            program.Steps.Count,
                            step.Action,
                            step.ActionLabel));

                        if (step.Action == ActionType.Log)
                        {
                            LogStep(index, step, 0, 0, isAxis: false);
                            continue;
                        }

                        int rampTime = NextInclusive(step.RampMinMs, step.RampMaxMs);
                        int actionTime = NextInclusive(step.DurationMinMs, step.DurationMaxMs);
                        bool isAxis = GamepadControlCatalog.TryGetAxisBinding(step.Control, out AxisBinding binding);

                        LogStep(index, step, rampTime, actionTime, isAxis);

                        if (step.Action == ActionType.Wait)
                        {
                            await DelayAsync(actionTime, token).ConfigureAwait(false);
                            continue;
                        }

                        if (isAxis)
                        {
                            await ExecuteAxisStepAsync(program, step, binding, rampTime, actionTime, token).ConfigureAwait(false);
                        }
                        else
                        {
                            await ExecuteDigitalStepAsync(step, actionTime, token).ConfigureAwait(false);
                        }
                    }

                    loopCount++;

                    // Limita ciclos totalmente instantâneos à frequência do motor (aprox. 60 Hz).
                    long remainingCycleTime = SequenceTimeEstimator.MinimumCycleDurationMs - cycleTimer.ElapsedMilliseconds;
                    if (remainingCycleTime > 0)
                    {
                        await Task.Delay((int)remainingCycleTime, token).ConfigureAwait(false);
                    }
                }
            }
            finally
            {
                _axisStates.Clear();
                try
                {
                    _output.Reset();
                }
                catch (Exception ex)
                {
                    _log($"[ERRO] O motor não conseguiu neutralizar o controle: {ex.Message}");
                }
            }
        }

        private void LogStep(int index, AutomationStep step, int rampTime, int actionTime, bool isAxis)
        {
            if (step.Action == ActionType.Log)
            {
                string message = string.IsNullOrWhiteSpace(step.Message)
                    ? "(mensagem vazia)"
                    : step.Message.Trim();
                _log($"[Linha {index + 1}] [MARCADOR] {message}");
            }
            else if (step.Action == ActionType.Wait)
            {
                _log($"[Linha {index + 1}] ⏳ PAUSA | Duração: {actionTime}ms");
            }
            else if (isAxis)
            {
                _log($"[Linha {index + 1}] 🎮 {step.ActionLabel} [{step.ControlLabel}] -> Alvo: {step.ValuePercent}% | Rampa: {rampTime}ms | Platô: {actionTime}ms | Jitter: ±{step.JitterForcePercent}%");
            }
            else
            {
                _log($"[Linha {index + 1}] 🔘 {step.ActionLabel} [{step.ControlLabel}] -> Duração: {actionTime}ms");
            }
        }

        private async Task ExecuteDigitalStepAsync(AutomationStep step, int actionTime, CancellationToken token)
        {
            switch (step.Action)
            {
                case ActionType.PressAndRelease:
                    _output.SetDigital(step.Control, true);
                    await DelayAsync(actionTime, token).ConfigureAwait(false);
                    _output.SetDigital(step.Control, false);
                    break;

                case ActionType.Hold:
                    _output.SetDigital(step.Control, true);
                    break;

                case ActionType.Release:
                    _output.SetDigital(step.Control, false);
                    break;
            }
        }

        private async Task ExecuteAxisStepAsync(
            AutomationProgram program,
            AutomationStep step,
            AxisBinding binding,
            int rampTime,
            int actionTime,
            CancellationToken token)
        {
            switch (step.Action)
            {
                case ActionType.PressAndRelease:
                    await MoveAxisAsync(program, binding, step.ValuePercent, rampTime, actionTime, step.JitterForcePercent, token).ConfigureAwait(false);
                    await MoveAxisAsync(program, binding, 0, rampTime, 0, 0, token).ConfigureAwait(false);
                    break;

                case ActionType.Hold:
                    await MoveAxisAsync(program, binding, step.ValuePercent, rampTime, 0, step.JitterForcePercent, token).ConfigureAwait(false);
                    break;

                case ActionType.Release:
                    await MoveAxisAsync(program, binding, 0, rampTime, 0, 0, token).ConfigureAwait(false);
                    break;
            }
        }

        private async Task MoveAxisAsync(
            AutomationProgram program,
            AxisBinding binding,
            int targetMagnitudePercent,
            int rampTime,
            int holdTime,
            int jitterForce,
            CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            float startValue = _axisStates.GetValueOrDefault(binding.Channel);
            float targetValue = targetMagnitudePercent * binding.Direction;
            bool useJitter = program.EnableJitter && jitterForce > 0;

            if (rampTime == 0 && !useJitter)
            {
                _output.SetAxis(binding.Channel, targetValue);
                _axisStates[binding.Channel] = targetValue;
                await DelayAsync(holdTime, token).ConfigureAwait(false);
                return;
            }

            int currentJitter = 0;
            long lastJitterChangeMs = -program.JitterFrequencyMs;

            if (rampTime > 0)
            {
                var rampTimer = Stopwatch.StartNew();
                while (rampTimer.ElapsedMilliseconds < rampTime)
                {
                    token.ThrowIfCancellationRequested();

                    long elapsedMs = rampTimer.ElapsedMilliseconds;
                    float progress = Math.Min(1f, (float)elapsedMs / rampTime);
                    float currentValue = startValue + ((targetValue - startValue) * progress);

                    if (useJitter && elapsedMs - lastJitterChangeMs >= program.JitterFrequencyMs)
                    {
                        currentJitter = NextInclusive(-jitterForce, jitterForce);
                        lastJitterChangeMs = elapsedMs;
                    }

                    _output.SetAxis(binding.Channel, ClampAxis(binding.Channel, currentValue + currentJitter));
                    await DelayFrameAsync(rampTime - elapsedMs, token).ConfigureAwait(false);
                }
            }

            _axisStates[binding.Channel] = targetValue;
            _output.SetAxis(binding.Channel, targetValue);

            if (holdTime <= 0)
            {
                return;
            }

            if (!useJitter)
            {
                await DelayAsync(holdTime, token).ConfigureAwait(false);
                return;
            }

            var holdTimer = Stopwatch.StartNew();
            lastJitterChangeMs = -program.JitterFrequencyMs;

            while (holdTimer.ElapsedMilliseconds < holdTime)
            {
                token.ThrowIfCancellationRequested();

                long elapsedMs = holdTimer.ElapsedMilliseconds;
                if (elapsedMs - lastJitterChangeMs >= program.JitterFrequencyMs)
                {
                    currentJitter = NextInclusive(-jitterForce, jitterForce);
                    lastJitterChangeMs = elapsedMs;
                    _output.SetAxis(binding.Channel, ClampAxis(binding.Channel, targetValue + currentJitter));
                }

                await DelayFrameAsync(holdTime - elapsedMs, token).ConfigureAwait(false);
            }

            _output.SetAxis(binding.Channel, targetValue);
        }

        private int NextInclusive(int minimum, int maximum)
        {
            if (minimum > maximum)
            {
                throw new ArgumentOutOfRangeException(nameof(minimum), "O valor mínimo não pode superar o máximo.");
            }

            if (minimum == maximum)
            {
                return minimum;
            }

            // NextInt64 evita overflow quando maximum == int.MaxValue.
            return (int)_random.NextInt64(minimum, (long)maximum + 1L);
        }

        private static float ClampAxis(AxisChannel channel, float value)
        {
            return channel is AxisChannel.LeftTrigger or AxisChannel.RightTrigger
                ? Math.Clamp(value, 0f, 100f)
                : Math.Clamp(value, -100f, 100f);
        }

        private static Task DelayAsync(int milliseconds, CancellationToken token)
        {
            if (milliseconds <= 0)
            {
                token.ThrowIfCancellationRequested();
                return Task.CompletedTask;
            }

            return Task.Delay(milliseconds, token);
        }

        private static Task DelayFrameAsync(long remainingMilliseconds, CancellationToken token)
        {
            int delay = (int)Math.Clamp(remainingMilliseconds, 1L, FrameDelayMs);
            return Task.Delay(delay, token);
        }
    }
}
