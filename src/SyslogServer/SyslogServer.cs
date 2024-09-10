namespace Syslog
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Reflection;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using SerializationHelper;

    /// <summary>
    /// Syslog server.
    /// </summary>
    public class ViewSyslogServer
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private static string _Version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
        private static Serializer _Serializer = new Serializer();
        private static Settings _Settings = new Settings();
        private static Thread _ListenerThread;
        private static UdpClient _ListenerUdp;
        private static DateTime _LastWritten = DateTime.Now;

        private static string _SettingsFile = "syslog.json";

        private static List<string> _MessageQueue = new List<string>();
        private static readonly object _WriterLock = new object();

        #endregion

        #region Public-Methods

        static void Main(string[] args)
        {
            Console.WriteLine("");
            Console.WriteLine("Syslog Server | v" + _Version);
            Console.WriteLine("(c)2024 Joel Christner");
            Console.WriteLine("");

            if (File.Exists(_SettingsFile))
            {
                _Settings = _Serializer.DeserializeJson<Settings>(File.ReadAllText(_SettingsFile));
            } 
            else
            {
                Console.WriteLine("Settings file " + _SettingsFile + " does not exist, creating");
                File.WriteAllText(_SettingsFile, _Serializer.SerializeJson(_Settings, true));
            }

            if (!Directory.Exists(_Settings.LogFileDirectory))
            {
                Console.WriteLine("Creating directory " + _Settings.LogFileDirectory);
                Directory.CreateDirectory(_Settings.LogFileDirectory);
            }

            StartServer();

            EventWaitHandle waitHandle = new EventWaitHandle(false, EventResetMode.AutoReset);
            bool waitHandleSignal = false;
            do
            {
                waitHandleSignal = waitHandle.WaitOne(1000);
            }
            while (!waitHandleSignal);
        }

        #endregion

        #region Private-Methods

        private static void StartServer()
        {
            Console.WriteLine("Starting at " + DateTime.Now);
                  
            _ListenerThread = new Thread(ReceiverThread);
            _ListenerThread.Start();
            Console.WriteLine("Listening on UDP/" + _Settings.UdpPort + ".");

            Task.Run(() => WriterTask());
            Console.WriteLine("Writer thread started successfully");
        }

        private static void ReceiverThread()
        {
            if (_ListenerUdp == null) _ListenerUdp = new UdpClient(_Settings.UdpPort);

            try
            {
                IPEndPoint endpoint = new IPEndPoint(IPAddress.Any, _Settings.UdpPort);
                string receivedData;
                byte[] receivedBytes;

                while (true)
                {
                    receivedBytes = _ListenerUdp.Receive(ref endpoint);
                    receivedData = Encoding.UTF8.GetString(receivedBytes, 0, receivedBytes.Length);
                    string msg = null;
                    if (_Settings.DisplayTimestamps) msg = DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss") + " ";
                    msg += receivedData;
                    Console.WriteLine(msg);

                    lock (_WriterLock)
                        _MessageQueue.Add(msg);
                }
            }
            catch (Exception e)
            {
                _ListenerUdp.Close();
                _ListenerUdp = null;
                Console.WriteLine("***");
                Console.WriteLine("ReceiverThread exiting due to exception: " + e.Message);
                return;
            }
        }

        static void WriterTask()
        {
            while (true)
            {
                try
                {
                    Task.Delay(1000).Wait();

                    if (DateTime.Compare(_LastWritten.AddSeconds(_Settings.LogWriterIntervalSec), DateTime.Now) < 0)
                    {
                        lock (_WriterLock)
                        {
                            if (_MessageQueue == null || _MessageQueue.Count < 1)
                            {
                                _LastWritten = DateTime.Now;
                                continue;
                            }

                            foreach (string msg in _MessageQueue)
                            {
                                string filename = _Settings.LogFileDirectory + DateTime.Now.ToString("yyyyMMdd") + "-" + _Settings.LogFilename;

                                if (!File.Exists(filename))
                                {
                                    Console.WriteLine("Creating file: " + filename + Environment.NewLine);
                                    {
                                        using (FileStream fs = File.Create(filename))
                                        {
                                            byte[] contents = Encoding.UTF8.GetBytes("--- Creating log file at " + DateTime.Now + " ---" + Environment.NewLine);
                                            fs.Write(contents, 0, contents.Length);
                                        }
                                    }
                                }

                                using (StreamWriter swAppend = File.AppendText(filename))
                                    swAppend.WriteLine(msg);
                            }

                            _LastWritten = DateTime.Now;
                            _MessageQueue = new List<string>();
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("");
                    Console.WriteLine("WriterTask exception: " + Environment.NewLine + e.ToString());
                    Environment.Exit(-1);
                }
            }
        }

        #endregion
    }
}
