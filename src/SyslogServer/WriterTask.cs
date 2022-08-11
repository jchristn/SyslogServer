using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace WatsonSyslog
{
    public partial class SyslogServer
    {
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
    }
}
