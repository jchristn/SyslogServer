namespace SyslogServer.Test.Shared
{
    using System;

    /// <summary>
    /// Exception thrown by <see cref="Check"/> helpers when an assertion fails.
    /// Touchstone captures this as a failed test case.
    /// </summary>
    public class CheckException : Exception
    {
        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="message">Failure message.</param>
        public CheckException(string message) : base(message)
        {
        }
    }
}
