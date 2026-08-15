namespace SyslogServer.Test.Xunit
{
    using System.Threading;
    using System.Threading.Tasks;
    using global::Xunit;
    using SyslogServer.Test.Shared;
    using Touchstone.Core;
    using Touchstone.XunitAdapter;

    /// <summary>
    /// xUnit surface for the shared Touchstone suites.
    ///
    /// Every <see cref="TestCaseDescriptor"/> authored in <see cref="SyslogTestSuites"/>
    /// is projected into a separate xUnit theory row via the Touchstone xUnit adapter,
    /// so each shared case appears and reports as an individual xUnit test.
    /// </summary>
    public class TouchstoneTests
    {
        /// <summary>
        /// Theory data: one row per shared test case, supplied by the Touchstone xUnit adapter.
        /// </summary>
        public static TouchstoneTheoryData Cases => new TouchstoneTheoryData(SyslogTestSuites.All);

        /// <summary>
        /// Execute a single shared test case. The descriptor throws on failure,
        /// which xUnit records as a failed assertion.
        /// </summary>
        /// <param name="testCase">Shared test case descriptor.</param>
        /// <returns>Task.</returns>
        [Theory]
        [MemberData(nameof(Cases))]
        public async Task Run(TestCaseDescriptor testCase)
        {
            await testCase.ExecuteAsync(CancellationToken.None);
        }
    }
}
