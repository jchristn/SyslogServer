namespace SyslogServer.Test.Automated
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using SyslogServer.Test.Shared;
    using Touchstone.Cli;

    /// <summary>
    /// Touchstone CLI runner for the SyslogServer test suites.
    ///
    /// Executes every shared test descriptor from <see cref="SyslogTestSuites"/> and
    /// writes a colored, tabular report to the console. Returns exit code 0 when all
    /// tests pass and 1 when any test fails, making it suitable for CI pipelines.
    ///
    /// Usage: SyslogServer.Test.Automated [resultsJsonPath]
    /// When a path is supplied, machine-readable JSON results are written to it.
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// Entry point.
        /// </summary>
        /// <param name="args">Optional single argument: a file path for JSON result export.</param>
        /// <returns>0 when all tests pass; 1 when any test fails.</returns>
        public static async Task<int> Main(string[] args)
        {
            string resultsPath = args != null && args.Length > 0 ? args[0] : null;

            Console.WriteLine("");
            Console.WriteLine("SyslogServer | Touchstone automated test runner");
            Console.WriteLine("");

            int exitCode = await ConsoleRunner.RunAsync(
                SyslogTestSuites.All,
                null,
                resultsPath,
                CancellationToken.None);

            return exitCode;
        }
    }
}
