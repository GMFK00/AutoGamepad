using System.Globalization;

namespace AutoGamepad
{
    internal readonly record struct TimeEstimateRange(long MinimumMs, long MaximumMs)
    {
        public static TimeEstimateRange Zero => new(0, 0);
    }

    internal sealed record SequenceTimeEstimate(
        IReadOnlyList<TimeEstimateRange> StepDurations,
        IReadOnlyList<TimeEstimateRange> CumulativeDurations,
        TimeEstimateRange CycleDuration,
        TimeEstimateRange? TotalDuration)
    {
        public bool IsContinuous => TotalDuration is null;
    }

    internal static class SequenceTimeEstimator
    {
        public const int MinimumCycleDurationMs = 16;

        public static SequenceTimeEstimate Calculate(AutomationProgram program)
        {
            ArgumentNullException.ThrowIfNull(program);

            if (program.Steps.Count == 0)
            {
                throw new InvalidOperationException("A sequência precisa conter pelo menos uma etapa.");
            }

            if (program.UseCycleLimit && program.MaxCycles <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(program.MaxCycles));
            }

            var stepDurations = new List<TimeEstimateRange>(program.Steps.Count);
            var cumulativeDurations = new List<TimeEstimateRange>(program.Steps.Count);
            TimeEstimateRange cumulative = TimeEstimateRange.Zero;

            foreach (AutomationStep step in program.Steps)
            {
                TimeEstimateRange stepDuration = CalculateStep(step);
                cumulative = Add(cumulative, stepDuration);
                stepDurations.Add(stepDuration);
                cumulativeDurations.Add(cumulative);
            }

            var cycleDuration = new TimeEstimateRange(
                Math.Max(cumulative.MinimumMs, MinimumCycleDurationMs),
                Math.Max(cumulative.MaximumMs, MinimumCycleDurationMs));
            TimeEstimateRange? totalDuration = program.UseCycleLimit
                ? Multiply(cycleDuration, program.MaxCycles)
                : null;

            return new SequenceTimeEstimate(
                stepDurations.AsReadOnly(),
                cumulativeDurations.AsReadOnly(),
                cycleDuration,
                totalDuration);
        }

        private static TimeEstimateRange CalculateStep(AutomationStep step)
        {
            bool isAxis = GamepadControlCatalog.TryGetAxisBinding(step.Control, out _);

            return step.Action switch
            {
                ActionType.Log => TimeEstimateRange.Zero,
                ActionType.Wait => CreateRange(step.DurationMinMs, step.DurationMaxMs, "duração"),
                ActionType.PressAndRelease when isAxis => CalculateAxisTap(step),
                ActionType.PressAndRelease => CreateRange(step.DurationMinMs, step.DurationMaxMs, "duração"),
                ActionType.Hold when isAxis => CreateRange(step.RampMinMs, step.RampMaxMs, "rampa"),
                ActionType.Release when isAxis => CreateRange(step.RampMinMs, step.RampMaxMs, "rampa"),
                ActionType.Hold or ActionType.Release => TimeEstimateRange.Zero,
                _ => throw new ArgumentOutOfRangeException(nameof(step.Action))
            };
        }

        private static TimeEstimateRange CalculateAxisTap(AutomationStep step)
        {
            TimeEstimateRange ramp = CreateRange(step.RampMinMs, step.RampMaxMs, "rampa");
            TimeEstimateRange duration = CreateRange(step.DurationMinMs, step.DurationMaxMs, "duração");

            return new TimeEstimateRange(
                checked((2L * ramp.MinimumMs) + duration.MinimumMs),
                checked((2L * ramp.MaximumMs) + duration.MaximumMs));
        }

        private static TimeEstimateRange CreateRange(int minimum, int maximum, string parameterName)
        {
            if (minimum < 0 || maximum < 0 || minimum > maximum)
            {
                throw new ArgumentOutOfRangeException(parameterName);
            }

            return new TimeEstimateRange(minimum, maximum);
        }

        private static TimeEstimateRange Add(TimeEstimateRange left, TimeEstimateRange right)
        {
            return new TimeEstimateRange(
                checked(left.MinimumMs + right.MinimumMs),
                checked(left.MaximumMs + right.MaximumMs));
        }

        private static TimeEstimateRange Multiply(TimeEstimateRange range, int multiplier)
        {
            return new TimeEstimateRange(
                checked(range.MinimumMs * multiplier),
                checked(range.MaximumMs * multiplier));
        }
    }

    internal static class TimeEstimateFormatter
    {
        public static string FormatRange(TimeEstimateRange range)
        {
            string minimum = FormatDuration(range.MinimumMs);
            return range.MinimumMs == range.MaximumMs
                ? minimum
                : $"{minimum} – {FormatDuration(range.MaximumMs)}";
        }

        private static string FormatDuration(long milliseconds)
        {
            if (milliseconds < 1_000)
            {
                return $"{milliseconds} ms";
            }

            if (milliseconds < 60_000)
            {
                return FormatScaled(milliseconds, 1_000d, "s");
            }

            if (milliseconds < 3_600_000)
            {
                return FormatScaled(milliseconds, 60_000d, "min");
            }

            if (milliseconds < 86_400_000)
            {
                return FormatScaled(milliseconds, 3_600_000d, "h");
            }

            return FormatScaled(milliseconds, 86_400_000d, "d");
        }

        private static string FormatScaled(long milliseconds, double divisor, string suffix)
        {
            double value = milliseconds / divisor;
            return $"{value.ToString("0.###", CultureInfo.CurrentCulture)} {suffix}";
        }
    }
}
