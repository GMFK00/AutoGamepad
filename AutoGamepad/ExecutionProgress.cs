namespace AutoGamepad
{
    internal readonly record struct AutomationProgress(
        int CycleNumber,
        int? TotalCycles,
        int StepIndex,
        int StepCount,
        ActionType Action,
        string ActionLabel)
    {
        public int LineNumber => StepIndex + 1;
    }

    internal static class ExecutionProgressFormatter
    {
        public static string FormatRunning(AutomationProgress progress)
        {
            string cycle = progress.TotalCycles is int totalCycles
                ? $"Ciclo {progress.CycleNumber} de {totalCycles}"
                : $"Ciclo {progress.CycleNumber}";

            return $"Executando — {cycle} — Linha {progress.LineNumber} de {progress.StepCount}";
        }

        public static string FormatCompleted(int completedCycles)
        {
            string suffix = completedCycles == 1
                ? "ciclo concluído"
                : "ciclos concluídos";

            return $"Finalizado — {completedCycles} {suffix}";
        }

        public static string FormatInterrupted(AutomationProgress? progress)
        {
            return progress is AutomationProgress current
                ? $"Interrompido — Ciclo {current.CycleNumber}, linha {current.LineNumber}"
                : "Interrompido";
        }

        public static string FormatFailed(AutomationProgress? progress)
        {
            return progress is AutomationProgress current
                ? $"Falha — Ciclo {current.CycleNumber}, linha {current.LineNumber}"
                : "Falha";
        }
    }
}
