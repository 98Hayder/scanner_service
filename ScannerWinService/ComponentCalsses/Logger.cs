using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScannerWinService.ComponentCalsses
{
   public class Logger
    {
        public static void WriteLog(string Message)
        {
            string LogPath = ConfigurationManager.AppSettings["LogPath"];


            bool exists = System.IO.Directory.Exists(LogPath);

            if (!exists)
                System.IO.Directory.CreateDirectory(LogPath);

            var FullPath = LogPath + "/" + DateTime.Now.Day.ToString() + "_" + DateTime.Now.Month.ToString() + "_" + DateTime.Now.Year.ToString() + "_log.text";

            if (LogPath != "")
            {
                using (StreamWriter sw = new StreamWriter(FullPath, true))
                {
                    sw.WriteLine($"{DateTime.Now} :{Message}");
                    sw.WriteLine("-----------------------------");
                }

            }

        }

    }
}
