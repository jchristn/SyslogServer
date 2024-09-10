namespace Syslog
{
    using System;
    using System.Text;

    /// <summary>
    /// Settings.
    /// </summary>
    public class Settings
    {
        #region Public-Members

        /// <summary>
        /// UDP port on which to listen.
        /// </summary>
        public int UdpPort
        {
            get
            {
                return _UdpPort;
            }
            set
            {
                if (value < 0 || value > 65535) throw new ArgumentOutOfRangeException(nameof(UdpPort));
                _UdpPort = value;
            }
        }

        /// <summary>
        /// Flag to enable or disable displaying timestamps.
        /// </summary>
        public bool DisplayTimestamps { get; set; } = false;

        /// <summary>
        /// Directory in which to write log files.
        /// </summary>
        public string LogFileDirectory { get; set; } = "./logs/";

        /// <summary>
        /// Log filename.
        /// </summary>
        public string LogFilename { get; set; } = "log.txt";

        /// <summary>
        /// Number of seconds between each log file update.
        /// </summary>
        public int LogWriterIntervalSec
        {
            get
            {
                return _LogWriterIntervalSec;
            }
            set
            {
                if (value < 1) throw new ArgumentOutOfRangeException(nameof(LogWriterIntervalSec));
                _LogWriterIntervalSec = value;
            }
        }

        #endregion

        #region Private-Members

        private int _UdpPort = 514;
        private int _LogWriterIntervalSec = 10;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public Settings()
        {

        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Human readable representation of the object.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("Syslog server settings  : " + Environment.NewLine);
            sb.Append("  UDP port              : " + UdpPort + Environment.NewLine);
            sb.Append("  Display timestamps    : " + DisplayTimestamps + Environment.NewLine);
            sb.Append("  Log file directory    : " + LogFileDirectory + Environment.NewLine);
            sb.Append("  Log filename          : " + LogFilename + Environment.NewLine);
            sb.Append("  Writer interval (sec) : " + LogWriterIntervalSec + Environment.NewLine);
            return sb.ToString();
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}
