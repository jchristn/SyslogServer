using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WatsonSyslog
{
    public class Settings
    {
        public string Version { get; set; }
        public int UdpPort { get; set; } 
        public bool DisplayTimestamps { get; set; }  
        public string LogFileDirectory { get; set; }
        public string LogFilename { get; set; }
        public int LogWriterIntervalSec { get; set; } 

        public static Settings Default()
        {
            Settings ret = new Settings();
            ret.Version = "Watson Syslog Server v1.0.1";
            ret.UdpPort = 514; 
            ret.DisplayTimestamps = false; 
            ret.LogFileDirectory = "logs\\";
            ret.LogFilename = "log.txt";
            ret.LogWriterIntervalSec = 10;
            return ret;
        }
    }
}
