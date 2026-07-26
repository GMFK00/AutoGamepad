using System.Runtime.InteropServices;

namespace AutoGamepad
{
    internal readonly record struct GlobalHotkeyDefinition(
        int Id,
        int Modifiers,
        int VirtualKey,
        string DisplayName);

    internal readonly record struct GlobalHotkeyFailure(
        GlobalHotkeyDefinition Definition,
        int ErrorCode);

    internal sealed record GlobalHotkeyActivationResult(
        bool IsActive,
        IReadOnlyList<GlobalHotkeyFailure> Failures);

    internal interface IGlobalHotkeyRegistrar
    {
        bool Register(
            IntPtr windowHandle,
            int id,
            int modifiers,
            int virtualKey,
            out int errorCode);

        bool Unregister(IntPtr windowHandle, int id);
    }

    internal sealed class WindowsGlobalHotkeyRegistrar : IGlobalHotkeyRegistrar
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterHotKey(
            IntPtr windowHandle,
            int id,
            int modifiers,
            int virtualKey);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnregisterHotKey(IntPtr windowHandle, int id);

        public bool Register(
            IntPtr windowHandle,
            int id,
            int modifiers,
            int virtualKey,
            out int errorCode)
        {
            bool registered = RegisterHotKey(windowHandle, id, modifiers, virtualKey);
            errorCode = registered ? 0 : Marshal.GetLastWin32Error();
            return registered;
        }

        public bool Unregister(IntPtr windowHandle, int id)
        {
            return UnregisterHotKey(windowHandle, id);
        }
    }

    internal sealed class GlobalHotkeyManager : IDisposable
    {
        private readonly IGlobalHotkeyRegistrar _registrar;
        private readonly IntPtr _windowHandle;
        private readonly HashSet<int> _registeredIds = new();

        public GlobalHotkeyManager(
            IGlobalHotkeyRegistrar registrar,
            IntPtr windowHandle)
        {
            ArgumentNullException.ThrowIfNull(registrar);
            _registrar = registrar;
            _windowHandle = windowHandle;
        }

        public bool IsActive { get; private set; }

        public bool IsRegistered(int id)
        {
            return IsActive && _registeredIds.Contains(id);
        }

        public GlobalHotkeyActivationResult Activate(
            IReadOnlyList<GlobalHotkeyDefinition> definitions)
        {
            ArgumentNullException.ThrowIfNull(definitions);
            Deactivate();

            var failures = new List<GlobalHotkeyFailure>();
            foreach (GlobalHotkeyDefinition definition in definitions)
            {
                if (_registrar.Register(
                    _windowHandle,
                    definition.Id,
                    definition.Modifiers,
                    definition.VirtualKey,
                    out int errorCode))
                {
                    _registeredIds.Add(definition.Id);
                }
                else
                {
                    failures.Add(new GlobalHotkeyFailure(definition, errorCode));
                }
            }

            if (failures.Count > 0)
            {
                Deactivate();
                return new GlobalHotkeyActivationResult(false, failures.AsReadOnly());
            }

            IsActive = true;
            return new GlobalHotkeyActivationResult(true, failures.AsReadOnly());
        }

        public void Deactivate()
        {
            foreach (int id in _registeredIds)
            {
                _registrar.Unregister(_windowHandle, id);
            }

            _registeredIds.Clear();
            IsActive = false;
        }

        public void Dispose()
        {
            Deactivate();
        }
    }
}
