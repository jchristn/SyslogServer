namespace SyslogServer.Test.Nunit
{
    using System.Threading;
    using System.Threading.Tasks;
    using global::NUnit.Framework;
    using SyslogServer.Test.Shared;
    using Touchstone.Core;
    using Touchstone.NunitAdapter;

    /// <summary>
    /// NUnit surface for the shared Touchstone suites.
    ///
    /// Every <see cref="TestCaseDescriptor"/> authored in <see cref="SyslogTestSuites"/>
    /// is projected into a separate NUnit test case via the Touchstone NUnit adapter's
    /// <see cref="TouchstoneTestCaseSource"/>, so each shared case appears and reports
    /// as an individual NUnit test.
    /// </summary>
    [TestFixture]
    public class TouchstoneTests
    {
        /// <summary>
        /// Test case source: one entry per shared test case, supplied by the Touchstone NUnit adapter.
        /// </summary>
        public static readonly TouchstoneTestCaseSource Cases = new TouchstoneTestCaseSource(SyslogTestSuites.All);

        /// <summary>
        /// Execute a single shared test case. The descriptor throws on failure,
        /// which NUnit records as a failed assertion.
        /// </summary>
        /// <param name="testCase">Shared test case descriptor.</param>
        /// <returns>Task.</returns>
        [TestCaseSource(nameof(Cases))]
        public async Task Run(TestCaseDescriptor testCase)
        {
            await testCase.ExecuteAsync(CancellationToken.None);
        }
    }
}
