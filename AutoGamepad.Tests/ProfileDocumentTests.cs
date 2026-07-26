using Xunit;

namespace AutoGamepad.Tests
{
    public class ProfileDocumentTests
    {
        [Fact]
        public void DocumentState_TracksDirtyAndSavedPath()
        {
            var state = new ProfileDocumentState();
            int stateChanges = 0;
            state.StateChanged += () => stateChanges++;

            Assert.Equal("Novo perfil", state.DisplayName);
            Assert.False(state.IsDirty);

            state.MarkDirty();
            state.MarkDirty();

            Assert.True(state.IsDirty);
            Assert.Equal(1, stateChanges);

            string filePath = Path.Combine(Path.GetTempPath(), "perfil.json");
            state.MarkSaved(filePath);

            Assert.False(state.IsDirty);
            Assert.Equal(Path.GetFullPath(filePath), state.FilePath);
            Assert.Equal("perfil.json", state.DisplayName);
            Assert.Equal(2, stateChanges);
        }

        [Fact]
        public void DocumentState_SuppressesProgrammaticChanges()
        {
            var state = new ProfileDocumentState();

            using (state.SuppressChangeTracking())
            {
                state.MarkDirty();

                using (state.SuppressChangeTracking())
                {
                    state.MarkDirty();
                }
            }

            Assert.False(state.IsDirty);

            state.MarkDirty();

            Assert.True(state.IsDirty);
        }

        [Fact]
        public void ProfileFileWriter_CreatesAndReplacesProfile()
        {
            string directory = Path.Combine(
                Path.GetTempPath(),
                $"AutoGamepad.Tests.{Guid.NewGuid():N}");
            Directory.CreateDirectory(directory);
            string filePath = Path.Combine(directory, "perfil.json");

            try
            {
                ProfileFileWriter.WriteAllTextSafely(filePath, "primeira versão");
                Assert.Equal("primeira versão", File.ReadAllText(filePath));

                ProfileFileWriter.WriteAllTextSafely(filePath, "segunda versão");
                Assert.Equal("segunda versão", File.ReadAllText(filePath));
                Assert.Empty(Directory.GetFiles(directory, "*.tmp"));
            }
            finally
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }
}
