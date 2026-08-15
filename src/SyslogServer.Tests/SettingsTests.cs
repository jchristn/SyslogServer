namespace SyslogServer.Tests
{
    using System;
    using Syslog;
    using Xunit;

    /// <summary>
    /// Unit tests for the <see cref="Settings"/> class, covering both positive
    /// (accepted) and negative (rejected) inputs for its validated properties.
    /// </summary>
    public class SettingsTests
    {
        #region Defaults

        [Fact]
        public void Constructor_ProducesExpectedDefaults()
        {
            Settings settings = new Settings();

            Assert.Equal(514, settings.UdpPort);
            Assert.False(settings.DisplayTimestamps);
            Assert.Equal("./logs/", settings.LogFileDirectory);
            Assert.Equal("log.txt", settings.LogFilename);
            Assert.Equal(10, settings.LogWriterIntervalSec);
        }

        #endregion

        #region UdpPort-Positive

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(514)]
        [InlineData(1514)]
        [InlineData(65535)]
        public void UdpPort_AcceptsValuesWithinRange(int port)
        {
            Settings settings = new Settings();

            settings.UdpPort = port;

            Assert.Equal(port, settings.UdpPort);
        }

        #endregion

        #region UdpPort-Negative

        [Theory]
        [InlineData(-1)]
        [InlineData(-514)]
        [InlineData(65536)]
        [InlineData(100000)]
        [InlineData(int.MinValue)]
        [InlineData(int.MaxValue)]
        public void UdpPort_RejectsValuesOutsideRange(int port)
        {
            Settings settings = new Settings();

            ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() => settings.UdpPort = port);
            Assert.Equal(nameof(Settings.UdpPort), ex.ParamName);
        }

        [Fact]
        public void UdpPort_RejectionDoesNotMutateExistingValue()
        {
            Settings settings = new Settings();
            settings.UdpPort = 1234;

            Assert.Throws<ArgumentOutOfRangeException>(() => settings.UdpPort = -5);

            Assert.Equal(1234, settings.UdpPort);
        }

        #endregion

        #region LogWriterIntervalSec-Positive

        [Theory]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(60)]
        [InlineData(int.MaxValue)]
        public void LogWriterIntervalSec_AcceptsValuesOfOneOrGreater(int interval)
        {
            Settings settings = new Settings();

            settings.LogWriterIntervalSec = interval;

            Assert.Equal(interval, settings.LogWriterIntervalSec);
        }

        #endregion

        #region LogWriterIntervalSec-Negative

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-10)]
        [InlineData(int.MinValue)]
        public void LogWriterIntervalSec_RejectsValuesBelowOne(int interval)
        {
            Settings settings = new Settings();

            ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() => settings.LogWriterIntervalSec = interval);
            Assert.Equal(nameof(Settings.LogWriterIntervalSec), ex.ParamName);
        }

        [Fact]
        public void LogWriterIntervalSec_RejectionDoesNotMutateExistingValue()
        {
            Settings settings = new Settings();
            settings.LogWriterIntervalSec = 30;

            Assert.Throws<ArgumentOutOfRangeException>(() => settings.LogWriterIntervalSec = 0);

            Assert.Equal(30, settings.LogWriterIntervalSec);
        }

        #endregion

        #region Plain-Properties

        [Fact]
        public void DisplayTimestamps_CanBeToggled()
        {
            Settings settings = new Settings();

            settings.DisplayTimestamps = true;
            Assert.True(settings.DisplayTimestamps);

            settings.DisplayTimestamps = false;
            Assert.False(settings.DisplayTimestamps);
        }

        [Fact]
        public void LogFileDirectory_RoundTripsAssignedValue()
        {
            Settings settings = new Settings();

            settings.LogFileDirectory = "/var/log/syslog/";

            Assert.Equal("/var/log/syslog/", settings.LogFileDirectory);
        }

        [Fact]
        public void LogFilename_RoundTripsAssignedValue()
        {
            Settings settings = new Settings();

            settings.LogFilename = "messages.log";

            Assert.Equal("messages.log", settings.LogFilename);
        }

        #endregion

        #region ToString

        [Fact]
        public void ToString_IncludesAllConfiguredValues()
        {
            Settings settings = new Settings
            {
                UdpPort = 1601,
                DisplayTimestamps = true,
                LogFileDirectory = "./data/",
                LogFilename = "events.txt",
                LogWriterIntervalSec = 5
            };

            string output = settings.ToString();

            Assert.Contains("1601", output);
            Assert.Contains("True", output);
            Assert.Contains("./data/", output);
            Assert.Contains("events.txt", output);
            Assert.Contains("5", output);
        }

        #endregion
    }
}
