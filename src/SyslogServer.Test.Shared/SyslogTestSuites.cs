namespace SyslogServer.Test.Shared
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using SerializationHelper;
    using Syslog;
    using Touchstone.Core;

    /// <summary>
    /// Central source of truth for every SyslogServer test case.
    ///
    /// Test logic is authored once here as runner-agnostic Touchstone descriptors and
    /// then surfaced through the CLI runner (Test.Automated), the xUnit adapter
    /// (Test.Xunit), and the NUnit adapter (Test.Nunit). Adding a case here
    /// automatically exposes it through all three runners.
    /// </summary>
    public static class SyslogTestSuites
    {
        #region Public-Members

        /// <summary>
        /// All test suites exposed by the shared library.
        /// </summary>
        public static IReadOnlyList<TestSuiteDescriptor> All => _All;

        #endregion

        #region Private-Members

        private static readonly Serializer _Serializer = new Serializer();

        private static readonly IReadOnlyList<TestSuiteDescriptor> _All = BuildAll();

        #endregion

        #region Suite-Builders

        private static IReadOnlyList<TestSuiteDescriptor> BuildAll()
        {
            return new List<TestSuiteDescriptor>
            {
                BuildDefaultsSuite(),
                BuildUdpPortSuite(),
                BuildLogWriterIntervalSuite(),
                BuildPropertiesSuite(),
                BuildToStringSuite(),
                BuildSerializationSuite()
            };
        }

        private static TestSuiteDescriptor BuildDefaultsSuite()
        {
            const string suite = "Settings.Defaults";
            List<TestCaseDescriptor> cases = new List<TestCaseDescriptor>
            {
                Case(suite, "udp-port", "Default UdpPort is 514", () =>
                {
                    Settings s = new Settings();
                    Check.Equal(514, s.UdpPort);
                }),
                Case(suite, "display-timestamps", "Default DisplayTimestamps is false", () =>
                {
                    Settings s = new Settings();
                    Check.False(s.DisplayTimestamps);
                }),
                Case(suite, "log-file-directory", "Default LogFileDirectory is ./logs/", () =>
                {
                    Settings s = new Settings();
                    Check.Equal("./logs/", s.LogFileDirectory);
                }),
                Case(suite, "log-filename", "Default LogFilename is log.txt", () =>
                {
                    Settings s = new Settings();
                    Check.Equal("log.txt", s.LogFilename);
                }),
                Case(suite, "log-writer-interval", "Default LogWriterIntervalSec is 10", () =>
                {
                    Settings s = new Settings();
                    Check.Equal(10, s.LogWriterIntervalSec);
                })
            };

            return new TestSuiteDescriptor(suite, "Settings: default values", cases);
        }

        private static TestSuiteDescriptor BuildUdpPortSuite()
        {
            const string suite = "Settings.UdpPort";
            List<TestCaseDescriptor> cases = new List<TestCaseDescriptor>();

            // Positive: values within the accepted range [0, 65535].
            int[] validPorts = { 0, 1, 80, 443, 514, 1024, 1514, 5514, 65534, 65535 };
            foreach (int port in validPorts)
            {
                int captured = port;
                cases.Add(Case(suite, "accepts-" + captured, "UdpPort accepts " + captured, () =>
                {
                    Settings s = new Settings();
                    s.UdpPort = captured;
                    Check.Equal(captured, s.UdpPort);
                }));
            }

            // Negative: values outside the accepted range are rejected.
            int[] invalidPorts = { -1, -514, 65536, 100000, int.MinValue, int.MaxValue };
            foreach (int port in invalidPorts)
            {
                int captured = port;
                cases.Add(Case(suite, "rejects-" + captured, "UdpPort rejects " + captured, () =>
                {
                    Settings s = new Settings();
                    ArgumentOutOfRangeException ex = Check.Throws<ArgumentOutOfRangeException>(() => s.UdpPort = captured);
                    Check.Equal(nameof(Settings.UdpPort), ex.ParamName);
                }));
            }

            // A rejected assignment must not mutate the previously accepted value.
            cases.Add(Case(suite, "rejection-preserves-value", "Rejected UdpPort does not overwrite existing value", () =>
            {
                Settings s = new Settings();
                s.UdpPort = 1234;
                Check.Throws<ArgumentOutOfRangeException>(() => s.UdpPort = -5);
                Check.Equal(1234, s.UdpPort);
            }));

            // Re-assigning the same valid value is idempotent.
            cases.Add(Case(suite, "idempotent-set", "Assigning the same UdpPort twice is idempotent", () =>
            {
                Settings s = new Settings();
                s.UdpPort = 1601;
                s.UdpPort = 1601;
                Check.Equal(1601, s.UdpPort);
            }));

            return new TestSuiteDescriptor(suite, "Settings: UdpPort validation", cases);
        }

        private static TestSuiteDescriptor BuildLogWriterIntervalSuite()
        {
            const string suite = "Settings.LogWriterIntervalSec";
            List<TestCaseDescriptor> cases = new List<TestCaseDescriptor>();

            // Positive: values of one or greater are accepted.
            int[] validIntervals = { 1, 2, 10, 60, 3600, int.MaxValue };
            foreach (int interval in validIntervals)
            {
                int captured = interval;
                cases.Add(Case(suite, "accepts-" + captured, "LogWriterIntervalSec accepts " + captured, () =>
                {
                    Settings s = new Settings();
                    s.LogWriterIntervalSec = captured;
                    Check.Equal(captured, s.LogWriterIntervalSec);
                }));
            }

            // Negative: values below one are rejected.
            int[] invalidIntervals = { 0, -1, -10, int.MinValue };
            foreach (int interval in invalidIntervals)
            {
                int captured = interval;
                cases.Add(Case(suite, "rejects-" + captured, "LogWriterIntervalSec rejects " + captured, () =>
                {
                    Settings s = new Settings();
                    ArgumentOutOfRangeException ex = Check.Throws<ArgumentOutOfRangeException>(() => s.LogWriterIntervalSec = captured);
                    Check.Equal(nameof(Settings.LogWriterIntervalSec), ex.ParamName);
                }));
            }

            // A rejected assignment must not mutate the previously accepted value.
            cases.Add(Case(suite, "rejection-preserves-value", "Rejected LogWriterIntervalSec does not overwrite existing value", () =>
            {
                Settings s = new Settings();
                s.LogWriterIntervalSec = 30;
                Check.Throws<ArgumentOutOfRangeException>(() => s.LogWriterIntervalSec = 0);
                Check.Equal(30, s.LogWriterIntervalSec);
            }));

            return new TestSuiteDescriptor(suite, "Settings: LogWriterIntervalSec validation", cases);
        }

        private static TestSuiteDescriptor BuildPropertiesSuite()
        {
            const string suite = "Settings.Properties";
            List<TestCaseDescriptor> cases = new List<TestCaseDescriptor>
            {
                Case(suite, "display-timestamps-toggle", "DisplayTimestamps can be toggled on and off", () =>
                {
                    Settings s = new Settings();
                    s.DisplayTimestamps = true;
                    Check.True(s.DisplayTimestamps);
                    s.DisplayTimestamps = false;
                    Check.False(s.DisplayTimestamps);
                }),
                Case(suite, "log-file-directory-roundtrip", "LogFileDirectory round-trips an assigned value", () =>
                {
                    Settings s = new Settings();
                    s.LogFileDirectory = "/var/log/syslog/";
                    Check.Equal("/var/log/syslog/", s.LogFileDirectory);
                }),
                Case(suite, "log-file-directory-empty", "LogFileDirectory accepts an empty string", () =>
                {
                    Settings s = new Settings();
                    s.LogFileDirectory = string.Empty;
                    Check.Equal(string.Empty, s.LogFileDirectory);
                }),
                Case(suite, "log-file-directory-unicode", "LogFileDirectory accepts a Unicode path", () =>
                {
                    Settings s = new Settings();
                    s.LogFileDirectory = "./логи/日志/";
                    Check.Equal("./логи/日志/", s.LogFileDirectory);
                }),
                Case(suite, "log-file-directory-null", "LogFileDirectory accepts null (unvalidated)", () =>
                {
                    Settings s = new Settings();
                    s.LogFileDirectory = null;
                    Check.True(s.LogFileDirectory == null);
                }),
                Case(suite, "log-filename-roundtrip", "LogFilename round-trips an assigned value", () =>
                {
                    Settings s = new Settings();
                    s.LogFilename = "messages.log";
                    Check.Equal("messages.log", s.LogFilename);
                }),
                Case(suite, "log-filename-null", "LogFilename accepts null (unvalidated)", () =>
                {
                    Settings s = new Settings();
                    s.LogFilename = null;
                    Check.True(s.LogFilename == null);
                }),
                Case(suite, "object-initializer", "Settings can be built via object initializer", () =>
                {
                    Settings s = new Settings
                    {
                        UdpPort = 1601,
                        DisplayTimestamps = true,
                        LogFileDirectory = "./data/",
                        LogFilename = "events.txt",
                        LogWriterIntervalSec = 5
                    };
                    Check.Equal(1601, s.UdpPort);
                    Check.True(s.DisplayTimestamps);
                    Check.Equal("./data/", s.LogFileDirectory);
                    Check.Equal("events.txt", s.LogFilename);
                    Check.Equal(5, s.LogWriterIntervalSec);
                })
            };

            return new TestSuiteDescriptor(suite, "Settings: plain properties", cases);
        }

        private static TestSuiteDescriptor BuildToStringSuite()
        {
            const string suite = "Settings.ToString";
            List<TestCaseDescriptor> cases = new List<TestCaseDescriptor>
            {
                Case(suite, "not-null-or-whitespace", "ToString returns a non-empty string", () =>
                {
                    Settings s = new Settings();
                    Check.NotNullOrWhitespace(s.ToString());
                }),
                Case(suite, "includes-defaults", "ToString of a default instance includes default values", () =>
                {
                    Settings s = new Settings();
                    string output = s.ToString();
                    Check.Contains("514", output);
                    Check.Contains("False", output);
                    Check.Contains("./logs/", output);
                    Check.Contains("log.txt", output);
                    Check.Contains("10", output);
                }),
                Case(suite, "includes-configured-values", "ToString includes all configured values", () =>
                {
                    Settings s = new Settings
                    {
                        UdpPort = 1601,
                        DisplayTimestamps = true,
                        LogFileDirectory = "./data/",
                        LogFilename = "events.txt",
                        LogWriterIntervalSec = 5
                    };
                    string output = s.ToString();
                    Check.Contains("1601", output);
                    Check.Contains("True", output);
                    Check.Contains("./data/", output);
                    Check.Contains("events.txt", output);
                    Check.Contains("5", output);
                }),
                Case(suite, "includes-labels", "ToString includes human-readable field labels", () =>
                {
                    Settings s = new Settings();
                    string output = s.ToString();
                    Check.Contains("UDP port", output);
                    Check.Contains("Display timestamps", output);
                    Check.Contains("Log file directory", output);
                    Check.Contains("Log filename", output);
                    Check.Contains("Writer interval", output);
                })
            };

            return new TestSuiteDescriptor(suite, "Settings: ToString rendering", cases);
        }

        private static TestSuiteDescriptor BuildSerializationSuite()
        {
            const string suite = "Settings.Serialization";
            List<TestCaseDescriptor> cases = new List<TestCaseDescriptor>
            {
                Case(suite, "roundtrip-full-fidelity", "Settings survive a JSON round-trip with full fidelity", () =>
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

                    Check.Equal(original.UdpPort, restored.UdpPort);
                    Check.Equal(original.DisplayTimestamps, restored.DisplayTimestamps);
                    Check.Equal(original.LogFileDirectory, restored.LogFileDirectory);
                    Check.Equal(original.LogFilename, restored.LogFilename);
                    Check.Equal(original.LogWriterIntervalSec, restored.LogWriterIntervalSec);
                }),
                Case(suite, "defaults-serialize-non-empty", "Default settings serialize to non-empty JSON containing UdpPort", () =>
                {
                    Settings s = new Settings();
                    string json = _Serializer.SerializeJson(s, true);
                    Check.NotNullOrWhitespace(json);
                    Check.Contains("UdpPort", json);
                }),
                Case(suite, "defaults-roundtrip", "Default settings survive a JSON round-trip", () =>
                {
                    Settings original = new Settings();
                    string json = _Serializer.SerializeJson(original, true);
                    Settings restored = _Serializer.DeserializeJson<Settings>(json);

                    Check.Equal(original.UdpPort, restored.UdpPort);
                    Check.Equal(original.DisplayTimestamps, restored.DisplayTimestamps);
                    Check.Equal(original.LogFileDirectory, restored.LogFileDirectory);
                    Check.Equal(original.LogFilename, restored.LogFilename);
                    Check.Equal(original.LogWriterIntervalSec, restored.LogWriterIntervalSec);
                }),
                Case(suite, "deserialize-handwritten", "Settings deserialize from hand-written JSON", () =>
                {
                    string json = "{" +
                        "\"UdpPort\":2514," +
                        "\"DisplayTimestamps\":true," +
                        "\"LogFileDirectory\":\"./hand/\"," +
                        "\"LogFilename\":\"hand.txt\"," +
                        "\"LogWriterIntervalSec\":3" +
                        "}";

                    Settings restored = _Serializer.DeserializeJson<Settings>(json);

                    Check.Equal(2514, restored.UdpPort);
                    Check.True(restored.DisplayTimestamps);
                    Check.Equal("./hand/", restored.LogFileDirectory);
                    Check.Equal("hand.txt", restored.LogFilename);
                    Check.Equal(3, restored.LogWriterIntervalSec);
                }),
                Case(suite, "compact-serialization", "Settings serialize without pretty-printing", () =>
                {
                    Settings s = new Settings { UdpPort = 9999 };
                    string json = _Serializer.SerializeJson(s, false);
                    Check.NotNullOrWhitespace(json);
                    Check.Contains("9999", json);
                })
            };

            return new TestSuiteDescriptor(suite, "Settings: JSON serialization", cases);
        }

        #endregion

        #region Private-Methods

        private static TestCaseDescriptor Case(string suiteId, string caseId, string displayName, Action body)
        {
            return new TestCaseDescriptor(
                suiteId,
                caseId,
                displayName,
                (CancellationToken ct) =>
                {
                    body();
                    return Task.CompletedTask;
                });
        }

        #endregion
    }
}
