namespace AutoGamepad
{
    public enum ActionType
    {
        PressAndRelease,
        Hold,
        Release,
        Wait
    }

    internal enum GamepadControl
    {
        None,
        A,
        B,
        X,
        Y,
        Start,
        Back,
        DPadUp,
        DPadDown,
        DPadLeft,
        DPadRight,
        LeftShoulder,
        RightShoulder,
        LeftThumb,
        RightThumb,
        LeftTrigger,
        RightTrigger,
        LeftStickUp,
        LeftStickDown,
        LeftStickLeft,
        LeftStickRight,
        RightStickUp,
        RightStickDown,
        RightStickLeft,
        RightStickRight
    }

    internal enum AxisChannel
    {
        LeftTrigger,
        RightTrigger,
        LeftStickX,
        LeftStickY,
        RightStickX,
        RightStickY
    }

    internal readonly record struct AxisBinding(AxisChannel Channel, int Direction);

    internal static class GamepadControlCatalog
    {
        public static GamepadControl FromJsonId(string jsonId)
        {
            return jsonId switch
            {
                "A" => GamepadControl.A,
                "B" => GamepadControl.B,
                "X" => GamepadControl.X,
                "Y" => GamepadControl.Y,
                "Start" => GamepadControl.Start,
                "Back" => GamepadControl.Back,
                "Up" => GamepadControl.DPadUp,
                "Down" => GamepadControl.DPadDown,
                "Left" => GamepadControl.DPadLeft,
                "Right" => GamepadControl.DPadRight,
                "LB" => GamepadControl.LeftShoulder,
                "RB" => GamepadControl.RightShoulder,
                "L3" => GamepadControl.LeftThumb,
                "R3" => GamepadControl.RightThumb,
                "LT" => GamepadControl.LeftTrigger,
                "RT" => GamepadControl.RightTrigger,
                "LS_Up" => GamepadControl.LeftStickUp,
                "LS_Down" => GamepadControl.LeftStickDown,
                "LS_Left" => GamepadControl.LeftStickLeft,
                "LS_Right" => GamepadControl.LeftStickRight,
                "RS_Up" => GamepadControl.RightStickUp,
                "RS_Down" => GamepadControl.RightStickDown,
                "RS_Left" => GamepadControl.RightStickLeft,
                "RS_Right" => GamepadControl.RightStickRight,
                _ => GamepadControl.None
            };
        }

        public static bool TryGetAxisBinding(GamepadControl control, out AxisBinding binding)
        {
            binding = control switch
            {
                GamepadControl.LeftTrigger => new AxisBinding(AxisChannel.LeftTrigger, 1),
                GamepadControl.RightTrigger => new AxisBinding(AxisChannel.RightTrigger, 1),
                GamepadControl.LeftStickUp => new AxisBinding(AxisChannel.LeftStickY, 1),
                GamepadControl.LeftStickDown => new AxisBinding(AxisChannel.LeftStickY, -1),
                GamepadControl.LeftStickLeft => new AxisBinding(AxisChannel.LeftStickX, -1),
                GamepadControl.LeftStickRight => new AxisBinding(AxisChannel.LeftStickX, 1),
                GamepadControl.RightStickUp => new AxisBinding(AxisChannel.RightStickY, 1),
                GamepadControl.RightStickDown => new AxisBinding(AxisChannel.RightStickY, -1),
                GamepadControl.RightStickLeft => new AxisBinding(AxisChannel.RightStickX, -1),
                GamepadControl.RightStickRight => new AxisBinding(AxisChannel.RightStickX, 1),
                _ => default
            };

            return control is GamepadControl.LeftTrigger
                or GamepadControl.RightTrigger
                or GamepadControl.LeftStickUp
                or GamepadControl.LeftStickDown
                or GamepadControl.LeftStickLeft
                or GamepadControl.LeftStickRight
                or GamepadControl.RightStickUp
                or GamepadControl.RightStickDown
                or GamepadControl.RightStickLeft
                or GamepadControl.RightStickRight;
        }
    }

    internal static class SequenceGridRules
    {
        public static bool IsControlEditable(ActionType action)
        {
            return action != ActionType.Wait;
        }

        public static GamepadControl NormalizeControl(ActionType action, GamepadControl control)
        {
            if (action == ActionType.Wait)
            {
                return GamepadControl.None;
            }

            return control == GamepadControl.None ? GamepadControl.A : control;
        }
    }

    internal static class SequenceRowPositionRules
    {
        public static int GetInsertionIndex(int rowCount, int? selectedRowIndex)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(rowCount);

            if (selectedRowIndex is null)
            {
                return rowCount;
            }

            if (selectedRowIndex < 0 || selectedRowIndex >= rowCount)
            {
                throw new ArgumentOutOfRangeException(nameof(selectedRowIndex));
            }

            return selectedRowIndex.Value;
        }

        public static int? GetSelectionIndexAfterRemoval(int remainingRowCount, int removedRowIndex)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(remainingRowCount);

            // Antes da remoção havia remainingRowCount + 1 linhas.
            if (removedRowIndex < 0 || removedRowIndex > remainingRowCount)
            {
                throw new ArgumentOutOfRangeException(nameof(removedRowIndex));
            }

            if (remainingRowCount == 0)
            {
                return null;
            }

            // A linha abaixo assume o índice removido. Ao remover a última,
            // seleciona a nova última linha, que era a imediatamente anterior.
            return Math.Min(removedRowIndex, remainingRowCount - 1);
        }
    }

    internal sealed record AutomationStep(
        ActionType Action,
        string ActionLabel,
        GamepadControl Control,
        string ControlLabel,
        int ValuePercent,
        int RampMinMs,
        int RampMaxMs,
        int DurationMinMs,
        int DurationMaxMs,
        int JitterForcePercent);

    internal sealed record AutomationProgram(
        bool UseCycleLimit,
        int MaxCycles,
        bool EnableJitter,
        int JitterFrequencyMs,
        IReadOnlyList<AutomationStep> Steps);
}
