namespace AutoGamepad
{
    internal sealed class ProfileDocumentState
    {
        private int _changeTrackingSuppressionDepth;

        public event Action? StateChanged;

        public string? FilePath { get; private set; }
        public bool IsDirty { get; private set; }
        public bool IsChangeTrackingSuppressed => _changeTrackingSuppressionDepth > 0;

        public string DisplayName => string.IsNullOrEmpty(FilePath)
            ? "Novo perfil"
            : Path.GetFileName(FilePath);

        public void MarkDirty()
        {
            if (IsChangeTrackingSuppressed || IsDirty)
            {
                return;
            }

            IsDirty = true;
            StateChanged?.Invoke();
        }

        public void MarkSaved(string filePath)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
            FilePath = Path.GetFullPath(filePath);
            IsDirty = false;
            StateChanged?.Invoke();
        }

        public void ResetUntitled()
        {
            FilePath = null;
            IsDirty = false;
            StateChanged?.Invoke();
        }

        public IDisposable SuppressChangeTracking()
        {
            _changeTrackingSuppressionDepth++;
            return new ChangeTrackingScope(this);
        }

        private void ResumeChangeTracking()
        {
            if (_changeTrackingSuppressionDepth <= 0)
            {
                throw new InvalidOperationException("O rastreamento de alterações não está suspenso.");
            }

            _changeTrackingSuppressionDepth--;
        }

        private sealed class ChangeTrackingScope : IDisposable
        {
            private ProfileDocumentState? _owner;

            public ChangeTrackingScope(ProfileDocumentState owner)
            {
                _owner = owner;
            }

            public void Dispose()
            {
                ProfileDocumentState? owner = Interlocked.Exchange(ref _owner, null);
                owner?.ResumeChangeTracking();
            }
        }
    }
}
