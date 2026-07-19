using Xunit;

namespace AutoGamepad.Tests
{
    public class ExecutionProgressFormatterTests
    {
        [Fact]
        public void FormatRunning_ShowsFiniteCycleAndLineTotals()
        {
            AutomationProgress progress = CreateProgress(cycle: 3, totalCycles: 10, stepIndex: 4, stepCount: 8);

            string status = ExecutionProgressFormatter.FormatRunning(progress);

            Assert.Equal("Executando — Ciclo 3 de 10 — Linha 5 de 8", status);
        }

        [Fact]
        public void FormatRunning_OmitsCycleTotalForContinuousExecution()
        {
            AutomationProgress progress = CreateProgress(cycle: 3, totalCycles: null, stepIndex: 4, stepCount: 8);

            string status = ExecutionProgressFormatter.FormatRunning(progress);

            Assert.Equal("Executando — Ciclo 3 — Linha 5 de 8", status);
        }

        [Theory]
        [InlineData(1, "Finalizado — 1 ciclo concluído")]
        [InlineData(10, "Finalizado — 10 ciclos concluídos")]
        public void FormatCompleted_UsesCorrectSingularAndPlural(int cycles, string expected)
        {
            Assert.Equal(expected, ExecutionProgressFormatter.FormatCompleted(cycles));
        }

        [Fact]
        public void FormatInterrupted_ShowsLastReportedPosition()
        {
            AutomationProgress progress = CreateProgress(cycle: 3, totalCycles: 10, stepIndex: 4, stepCount: 8);

            Assert.Equal(
                "Interrompido — Ciclo 3, linha 5",
                ExecutionProgressFormatter.FormatInterrupted(progress));
            Assert.Equal("Interrompido", ExecutionProgressFormatter.FormatInterrupted(null));
        }

        [Fact]
        public void FormatFailed_ShowsLastReportedPosition()
        {
            AutomationProgress progress = CreateProgress(cycle: 3, totalCycles: 10, stepIndex: 4, stepCount: 8);

            Assert.Equal(
                "Falha — Ciclo 3, linha 5",
                ExecutionProgressFormatter.FormatFailed(progress));
            Assert.Equal("Falha", ExecutionProgressFormatter.FormatFailed(null));
        }

        private static AutomationProgress CreateProgress(
            int cycle,
            int? totalCycles,
            int stepIndex,
            int stepCount)
        {
            return new AutomationProgress(
                cycle,
                totalCycles,
                stepIndex,
                stepCount,
                ActionType.Wait,
                "Pausa (Wait)");
        }
    }
}
