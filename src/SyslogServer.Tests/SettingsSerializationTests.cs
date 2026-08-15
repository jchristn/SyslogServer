namespace SyslogServer.Tests
{
    using Syslog;
    using SerializationHelper;
    using Xunit;

    /// <summary>
    /// Tests that exercise <see cref="Settings"/> through the same JSON
    /// serialization path the application uses to persist and load its config.
    /// </summary>
    public class SettingsSerializationTests
    {
        private readonly Serializer _Serializer = new Serializer();

        [Fact]
        public void Settings_SurviveJsonRoundTrip()
        {
            Settings original = new Settings
            {
                UdpPort = 5514,
                DisplayTimestamps = true,
                LogFileDirectory = "./roundtrip/",
                LogFilename = "roundtrip.txt",
                LogWriterIntervalSec = 7
            };

            string json = _Serializer.SerializeJson(original, true);
            Settings restored = _Serializer.DeserializeJson<Settings>(json);

            Assert.Equal(original.UdpPort, restored.UdpPort);
            Assert.Equal(original.DisplayTimestamps, restored.DisplayTimestamps);
            Assert.Equal(original.LogFileDirectory, restored.LogFileDirectory);
            Assert.Equal(original.LogFilename, restored.LogFilename);
            Assert.Equal(original.LogWriterIntervalSec, restored.LogWriterIntervalSec);
        }

        [Fact]
        public void DefaultSettings_SerializeToNonEmptyJson()
        {
            Settings settings = new Settings();

            string json = _Serializer.SerializeJson(settings, true);

            Assert.False(string.IsNullOrWhiteSpace(json));
            Assert.Contains("UdpPort", json);
        }
    }
}
