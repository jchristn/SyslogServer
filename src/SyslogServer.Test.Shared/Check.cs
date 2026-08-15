namespace SyslogServer.Test.Shared
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Minimal, runner-agnostic assertion helpers for Touchstone test descriptors.
    /// Each method throws <see cref="CheckException"/> when its condition is not met;
    /// Touchstone treats any thrown exception as a failed test case, so these helpers
    /// work identically under the CLI runner, xUnit, and NUnit.
    /// </summary>
    public static class Check
    {
        /// <summary>
        /// Assert that a condition is true.
        /// </summary>
        /// <param name="condition">Condition expected to be true.</param>
        /// <param name="message">Optional message describing the expectation.</param>
        public static void True(bool condition, string message = null)
        {
            if (!condition) throw new CheckException(message ?? "Expected condition to be true, but it was false.");
        }

        /// <summary>
        /// Assert that a condition is false.
        /// </summary>
        /// <param name="condition">Condition expected to be false.</param>
        /// <param name="message">Optional message describing the expectation.</param>
        public static void False(bool condition, string message = null)
        {
            if (condition) throw new CheckException(message ?? "Expected condition to be false, but it was true.");
        }

        /// <summary>
        /// Assert that two values are equal using the default equality comparer.
        /// </summary>
        /// <typeparam name="T">Type of the values.</typeparam>
        /// <param name="expected">Expected value.</param>
        /// <param name="actual">Actual value.</param>
        public static void Equal<T>(T expected, T actual)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new CheckException("Expected [" + Format(expected) + "] but got [" + Format(actual) + "].");
        }

        /// <summary>
        /// Assert that two values are not equal using the default equality comparer.
        /// </summary>
        /// <typeparam name="T">Type of the values.</typeparam>
        /// <param name="notExpected">Value the actual result must not equal.</param>
        /// <param name="actual">Actual value.</param>
        public static void NotEqual<T>(T notExpected, T actual)
        {
            if (EqualityComparer<T>.Default.Equals(notExpected, actual))
                throw new CheckException("Expected value to differ from [" + Format(notExpected) + "] but it was equal.");
        }

        /// <summary>
        /// Assert that a string contains an expected substring.
        /// </summary>
        /// <param name="expectedSubstring">Substring expected to appear.</param>
        /// <param name="actual">String to search.</param>
        public static void Contains(string expectedSubstring, string actual)
        {
            if (actual == null || !actual.Contains(expectedSubstring))
                throw new CheckException("Expected string to contain [" + expectedSubstring + "] but it did not. Actual: [" + Format(actual) + "].");
        }

        /// <summary>
        /// Assert that a value is not null.
        /// </summary>
        /// <param name="value">Value expected to be non-null.</param>
        /// <param name="message">Optional message describing the expectation.</param>
        public static void NotNull(object value, string message = null)
        {
            if (value == null) throw new CheckException(message ?? "Expected value to be non-null, but it was null.");
        }

        /// <summary>
        /// Assert that a string is neither null nor whitespace.
        /// </summary>
        /// <param name="value">String expected to contain content.</param>
        public static void NotNullOrWhitespace(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new CheckException("Expected a non-empty string, but it was null or whitespace.");
        }

        /// <summary>
        /// Assert that invoking an action throws an exception of the specified type
        /// (or a derived type) and return the caught exception for further inspection.
        /// </summary>
        /// <typeparam name="TException">Expected exception type.</typeparam>
        /// <param name="action">Action expected to throw.</param>
        /// <returns>The exception that was thrown.</returns>
        public static TException Throws<TException>(Action action) where TException : Exception
        {
            if (action == null) throw new ArgumentNullException(nameof(action));

            try
            {
                action();
            }
            catch (TException ex)
            {
                return ex;
            }
            catch (Exception ex)
            {
                throw new CheckException("Expected exception of type [" + typeof(TException).Name + "] but got [" + ex.GetType().Name + "]: " + ex.Message);
            }

            throw new CheckException("Expected exception of type [" + typeof(TException).Name + "] but no exception was thrown.");
        }

        private static string Format(object value)
        {
            if (value == null) return "null";
            return value.ToString();
        }
    }
}
