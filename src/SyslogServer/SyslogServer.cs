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

namespace WatsonSyslog
{
    /// <summary>
    /// Watson syslog server.
    /// </summary>
    public partial class SyslogServer
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private static string _Version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
        private static string _SettingsContents = "";
        private static Settings _Settings = new Settings();
        private static Thread _ListenerThread;
        private static UdpClient _ListenerUdp;
        private static DateTime _LastWritten = DateTime.Now;
        private static List<string> _MessageQueue = new List<string>();
        private static readonly object _WriterLock = new object();

        #endregion

        #region Public-Methods

        static void Main(string[] args)
        {
            #region Welcome

            Console.WriteLine("---");
            Console.WriteLine("Watson Syslog Server | v" + _Version);
            Console.WriteLine("(c)2022 Joel Christner");
            Console.WriteLine("https://github.com/jchristn/watsonsyslogserver");
            Console.WriteLine("---");

            #endregion

            #region Read-Config-File

            if (File.Exists("syslog.json"))
            {
                _SettingsContents = Encoding.UTF8.GetString(File.ReadAllBytes("syslog.json"));
            } 

            if (String.IsNullOrEmpty(_SettingsContents))
            {
                Console.WriteLine("Unable to read syslog.json, using default configuration:");
                Console.WriteLine(_Settings.ToString());
            }
            else
            {
                try
                {
                    _Settings = Common.DeserializeJson<Settings>(_SettingsContents);
                }
                catch (Exception)
                {
                    Console.WriteLine("Unable to deserialize syslog.json, please check syslog.json for correctness, exiting");
                    Environment.Exit(-1);
                }
            }

            if (!Directory.Exists(_Settings.LogFileDirectory)) Directory.CreateDirectory(_Settings.LogFileDirectory);

            #endregion
            
            #region Start-Server

            StartServer();

            #endregion

            #region Console
             
            while (true)
            {
                string userInput = Common.InputString("[syslog :: ? for help] >", null, false);
                switch (userInput)
                {
                    case "?":
                        Console.WriteLine("---");
                        Console.WriteLine("  q      quit the application");
                        Console.WriteLine("  cls    clear the screen");
                        break;

                    case "q": 
                        Console.WriteLine("Exiting.");
                        Environment.Exit(0);
                        break;

                    case "c":
                    case "cls":
                        Console.Clear();
                        break;
                            
                    default:
                        Console.WriteLine("Unknown command.  Type '?' for help.");
                        continue;
                }
            }
                  
            #endregion
        }

        #endregion

        #region Private-Methods

        private static void StartServer()
        {
            try
            { 
                Console.WriteLine("Starting at " + DateTime.Now);
                  
                _ListenerThread = new Thread(ReceiverThread);
                _ListenerThread.Start();
                Console.WriteLine("Listening on UDP/" + _Settings.UdpPort + ".");

                Task.Run(() => WriterTask());
                Console.WriteLine("Writer thread started successfully");
            }
            catch (Exception e)
            {
                Console.WriteLine("***");
                Console.WriteLine("Exiting due to exception: " + e.Message);
                Environment.Exit(-1);
            }
        }

        private static void ReceiverThread()
        {
            if (_ListenerUdp == null) _ListenerUdp = new UdpClient(_Settings.UdpPort);

            try
            {
                #region Start-Listener

                IPEndPoint endpoint = new IPEndPoint(IPAddress.Any, _Settings.UdpPort);
                string receivedData;
                byte[] receivedBytes;

                while (true)
                {
                    #region Receive-Data

                    receivedBytes = _ListenerUdp.Receive(ref endpoint);
                    receivedData = Encoding.ASCII.GetString(receivedBytes, 0, receivedBytes.Length);
                    string msg = null;
                    if (_Settings.DisplayTimestamps) msg = DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss") + " ";
                    msg += receivedData;
                    Console.WriteLine(msg);

                    #endregion

                    #region Add-to-Queue

                    lock (_WriterLock)
                    {
                        _MessageQueue.Add(msg);
                    }

                    #endregion
                }

                #endregion
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
            try
            {
                while (true)
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

                            foreach (string currMessage in _MessageQueue)
                            {
                                string currFilename = _Settings.LogFileDirectory + DateTime.Now.ToString("MMddyyyy") + "-" + _Settings.LogFilename;

                                if (!File.Exists(currFilename))
                                {
                                    Console.WriteLine("Creating file: " + currFilename + Environment.NewLine);
                                    {
                                        using (FileStream fsCreate = File.Create(currFilename))
                                        {
                                            Byte[] createData = new UTF8Encoding(true).GetBytes("--- Creating log file at " + DateTime.Now + " ---" + Environment.NewLine);
                                            fsCreate.Write(createData, 0, createData.Length);
                                        }
                                    }
                                }

                                using (StreamWriter swAppend = File.AppendText(currFilename))
                                {
                                    swAppend.WriteLine(currMessage);
                                }
                            }

                            _LastWritten = DateTime.Now;
                            _MessageQueue = new List<string>();
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("***");
                Console.WriteLine("WriterTask exiting due to exception: " + e.Message);
                Environment.Exit(-1);
            }
        }

        #endregion
    }
}
