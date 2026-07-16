using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.Xbox360;

namespace AutoGamepad
{
    internal sealed class Xbox360GamepadOutput : IGamepadOutput, IDisposable
    {
        private readonly object _sync = new();
        private ViGEmClient? _client;
        private IXbox360Controller? _controller;

        private Xbox360GamepadOutput(ViGEmClient client, IXbox360Controller controller)
        {
            _client = client;
            _controller = controller;
        }

        public static Xbox360GamepadOutput Connect()
        {
            var client = new ViGEmClient();

            try
            {
                IXbox360Controller controller = client.CreateXbox360Controller();
                controller.Connect();
                return new Xbox360GamepadOutput(client, controller);
            }
            catch
            {
                client.Dispose();
                throw;
            }
        }

        public void SetDigital(GamepadControl control, bool isPressed)
        {
            lock (_sync)
            {
                if (_controller == null)
                {
                    return;
                }

                Xbox360Button? button = control switch
                {
                    GamepadControl.A => Xbox360Button.A,
                    GamepadControl.B => Xbox360Button.B,
                    GamepadControl.X => Xbox360Button.X,
                    GamepadControl.Y => Xbox360Button.Y,
                    GamepadControl.Start => Xbox360Button.Start,
                    GamepadControl.Back => Xbox360Button.Back,
                    GamepadControl.DPadUp => Xbox360Button.Up,
                    GamepadControl.DPadDown => Xbox360Button.Down,
                    GamepadControl.DPadLeft => Xbox360Button.Left,
                    GamepadControl.DPadRight => Xbox360Button.Right,
                    GamepadControl.LeftShoulder => Xbox360Button.LeftShoulder,
                    GamepadControl.RightShoulder => Xbox360Button.RightShoulder,
                    GamepadControl.LeftThumb => Xbox360Button.LeftThumb,
                    GamepadControl.RightThumb => Xbox360Button.RightThumb,
                    _ => null
                };

                if (button != null)
                {
                    _controller.SetButtonState(button, isPressed);
                }
            }
        }

        public void SetAxis(AxisChannel channel, float valuePercent)
        {
            lock (_sync)
            {
                if (_controller == null)
                {
                    return;
                }

                float clamped = channel is AxisChannel.LeftTrigger or AxisChannel.RightTrigger
                    ? Math.Clamp(valuePercent, 0f, 100f)
                    : Math.Clamp(valuePercent, -100f, 100f);

                switch (channel)
                {
                    case AxisChannel.LeftTrigger:
                        _controller.SetSliderValue(Xbox360Slider.LeftTrigger, ToTriggerValue(clamped));
                        break;
                    case AxisChannel.RightTrigger:
                        _controller.SetSliderValue(Xbox360Slider.RightTrigger, ToTriggerValue(clamped));
                        break;
                    case AxisChannel.LeftStickX:
                        _controller.SetAxisValue(Xbox360Axis.LeftThumbX, ToStickValue(clamped));
                        break;
                    case AxisChannel.LeftStickY:
                        _controller.SetAxisValue(Xbox360Axis.LeftThumbY, ToStickValue(clamped));
                        break;
                    case AxisChannel.RightStickX:
                        _controller.SetAxisValue(Xbox360Axis.RightThumbX, ToStickValue(clamped));
                        break;
                    case AxisChannel.RightStickY:
                        _controller.SetAxisValue(Xbox360Axis.RightThumbY, ToStickValue(clamped));
                        break;
                }
            }
        }

        public void Reset()
        {
            lock (_sync)
            {
                if (_controller == null)
                {
                    return;
                }

                foreach (Xbox360Button button in new[]
                {
                    Xbox360Button.A,
                    Xbox360Button.B,
                    Xbox360Button.X,
                    Xbox360Button.Y,
                    Xbox360Button.Start,
                    Xbox360Button.Back,
                    Xbox360Button.Up,
                    Xbox360Button.Down,
                    Xbox360Button.Left,
                    Xbox360Button.Right,
                    Xbox360Button.LeftShoulder,
                    Xbox360Button.RightShoulder,
                    Xbox360Button.LeftThumb,
                    Xbox360Button.RightThumb
                })
                {
                    _controller.SetButtonState(button, false);
                }

                _controller.SetSliderValue(Xbox360Slider.LeftTrigger, 0);
                _controller.SetSliderValue(Xbox360Slider.RightTrigger, 0);
                _controller.SetAxisValue(Xbox360Axis.LeftThumbX, 0);
                _controller.SetAxisValue(Xbox360Axis.LeftThumbY, 0);
                _controller.SetAxisValue(Xbox360Axis.RightThumbX, 0);
                _controller.SetAxisValue(Xbox360Axis.RightThumbY, 0);
            }
        }

        public void Dispose()
        {
            lock (_sync)
            {
                if (_controller != null)
                {
                    try
                    {
                        Reset();
                        _controller.Disconnect();
                    }
                    catch
                    {
                        // O Windows pode já ter removido o dispositivo virtual.
                    }
                    finally
                    {
                        _controller = null;
                    }
                }

                if (_client != null)
                {
                    try
                    {
                        _client.Dispose();
                    }
                    catch
                    {
                        // A limpeza do cliente não deve impedir o encerramento.
                    }
                    finally
                    {
                        _client = null;
                    }
                }
            }
        }

        private static byte ToTriggerValue(float valuePercent)
        {
            return (byte)Math.Round(valuePercent * byte.MaxValue / 100f);
        }

        private static short ToStickValue(float valuePercent)
        {
            return valuePercent >= 0
                ? (short)Math.Round(valuePercent * short.MaxValue / 100f)
                : (short)Math.Round(valuePercent * 32768f / 100f);
        }
    }
}
