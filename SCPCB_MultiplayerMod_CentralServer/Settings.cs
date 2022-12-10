using System.Collections.Generic;
using System.IO;

namespace SCPCB_MultiplayerMod_CentralServer
{
    public class Settings
    {
        public Dictionary<string, int> Values = new Dictionary<string, int>();

        public Settings(string file)
        {
            string[] stuff = File.ReadAllLines(file);
            foreach (string line in stuff)
            {
                if (line.Contains(" "))
                {
                    string[] bruh = line.Split(' ');
                    Values.Add(bruh[0], int.Parse(bruh[1]));
                    Log.WriteLog("[CONFIG] " + bruh[0] + "=" + bruh[1]);
                }
            }
        }
    }
}