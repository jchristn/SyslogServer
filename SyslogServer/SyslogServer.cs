using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WatsonSyslog
{
    public partial class SyslogServer
    {
        public static string _SettingsContents = "";
        public static Settings _Settings;
        public static Thread _ListenerThread;
        public static UdpClient _ListenerUdp; 
        public static bool _ConsoleDisplay = true;
        
        public static DateTime _LastWritten = DateTime.Now;

        private static List<string> _MessageQueue = new List<string>();
        private static readonly object _WriterLock = new object();

        static void Main(string[] args)
        {
            #region Read-Config-File

            if (File.Exists("syslog.json"))
            {
                _SettingsContents = Encoding.UTF8.GetString(File.ReadAllBytes("syslog.json"));
            } 

            if (String.IsNullOrEmpty(_SettingsContents))
            {
                Console.WriteLine("Unable to read syslog.json, using default configuration:");
                _Settings = Settings.Default();
                Console.WriteLine(Common.SerializeJson(_Settings));
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

            Console.WriteLine("---");
            Console.WriteLine(_Settings.Version);
            Console.WriteLine("(c)2017 Joel Christner");
            Console.WriteLine("---");
            
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

        static void StartServer()
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
    }
}
