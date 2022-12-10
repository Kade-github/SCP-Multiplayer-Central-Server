using System;
using System.IO;
using System.Threading;

namespace SCPCB_MultiplayerMod_CentralServer
{
    public class Log
    {
        public static bool doLogs = false;
        
        public static bool IsFileReady(string filename)
        {
            // If the file can be opened for exclusive access it means that the file
            // is no longer locked by another process.
            try
            {
                using (FileStream inputStream = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.None))
                    return inputStream.Length > 0;
            }
            catch (Exception)
            {
                return false;
            }
        } 
        public static void WriteLog(string message)
        {
            DateTime time = DateTime.Now;
            try
            {

                string log = "[" + time.ToLongDateString() + " | " + time.TimeOfDay + "] " + message;

                Console.WriteLine(log);

                if (doLogs)
                {
                    if (File.Exists("centralserver_log.txt"))
                    {
                        while (!IsFileReady("centralserver_log.txt"))
                            Thread.Sleep(1);
                        File.WriteAllText("centralserver_log.txt",
                            File.ReadAllText("centralserver_log.txt") + "\n" + log);
                    }
                    else
                        File.WriteAllText("centralserver_log.txt", log);
                }
            }
            catch (Exception)
            {
                Console.Write("[" + time.ToLongDateString() + " | " + time.TimeOfDay + "] Failed to write to log! (you can probably ignore this)");
            }
        }
    }
}