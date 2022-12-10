using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using SteamIDs_Engine;
using Steamworks;

namespace SCPCB_MultiplayerMod_CentralServer
{
    public class Client
    {
        public DateTime lastConnection;
        public int connections = 0;
        public IPEndPoint EndPoint;
        public UdpClient udp;
        public TcpClient tcp;
        public string Connection;

        public string Type = "";
        public static Client FindClientInList(List<Client> clients, string ip)
        {
            foreach(Client c in clients)
                if (c.Connection == ip)
                    return c;
            return null;
        }
        
        public Client(string connection, string type)
        {
            Connection = connection;
            Type = type;
            ResetRead();
        }

        public int bIndex;
        
        public void ResetRead()
        {
            bIndex = 0;
        }
        
                
        public int ReadAvail(byte[] b)
        {
            int am = 0;
            while (bIndex != b.Length)
            {
                bIndex++;
                am++;
            }

            return am;
        }

        public ushort ReadShort(byte[] b)
        {
            byte[] toConvert = {b[bIndex + 1], b[bIndex + 0]};
            ushort sh = BitConverter.ToUInt16(toConvert, 0);
            bIndex += 2;
            return sh;
        }
        
        public int ReadByte(byte[] b)
        {
            int by = b[bIndex];
            bIndex++;
            return by;
        }
        
        public int ReadInt(byte[] b)
        {
            byte[] toConvert = {b[bIndex + 3], b[bIndex + 2], b[bIndex + 1], b[bIndex ]};
            int by = BinaryPrimitives.ReverseEndianness(BitConverter.ToInt32(toConvert, 0));
            bIndex += 4;
            return by;
        }

        public void ReadBytes(byte[] b)
        {
            ResetRead();
            int type = ReadByte(b);
            
            byte[] bb;
            switch (type)
            {
                case 52: // request playerauth
                    int randomId = ReadInt(b);
                    int cryptedKey = ReadInt(b);

                    bool result = ((randomId / 2) * 4 * 2 - 20) == cryptedKey;

                    if (result)
                    {

                        int authConnection = ReadInt(b);
                        int steamId = ReadInt(b);

                        int last = bIndex;

                        List<byte> data = new List<byte>();
                        
                        string steamId32 = "U:1:" + steamId;

                        SteamId id = (ulong) SteamIDConvert.Steam32ToSteam64(steamId32);

                        for (int i = 0; i < b.Length - last; i++)
                            data.Add(b[last + i]);

                        Result r = new Result();
                        
                        r.Owner = new SteamId();
                        
                        Steam.lastResult[id] = new Result();

                        bool worked = SteamServer.BeginAuthSession(data.ToArray(), id);

                        if (!worked)
                        {
                            Log.WriteLog("[" + Type.ToUpper() + ":" + Connection +
                                         "] failed to start auth session for steam id " + id + " ^ " + steamId32 +
                                         " with encoded ticket length " + data.ToArray().Length);
                            return;
                        }

                        new Thread(() => {
                            DateTime time = DateTime.Now;
                            
                            while (!Steam.lastResult[id].Owner.IsValid)
                            {
                                Thread.Sleep(1); // shitty
                            }

                            r = Steam.lastResult[id];

                            if (r.Status == AuthResponse.OK ||
                                r.Status == AuthResponse.VACBanned)
                            {
                                List<Byte> d = new List<byte> {2};
                                byte[] authByte = BitConverter.GetBytes(authConnection);

                                foreach (byte by in authByte)
                                    d.Add(by);
                                foreach (byte by in BitConverter.GetBytes((int) r.Status))
                                    d.Add(by);

                                Log.WriteLog("[" + Type.ToUpper() + ":" + Connection + "] authenticated " + steamId +
                                             " with status " + (int) r.Status + " in " +
                                             (DateTime.Now - time).Milliseconds + "ms");
                                byte[] finalData = d.ToArray();
                                udp.Send(finalData, finalData.Length, EndPoint);
                            }
                            else
                            {
                                List<Byte> d = new List<byte> {2};
                                foreach (byte by in BitConverter.GetBytes(authConnection))
                                    d.Add(by);
                                foreach (byte by in BitConverter.GetBytes((int) r.Status))
                                    d.Add(by);

                                Log.WriteLog("[" + Type.ToUpper() + ":" + Connection + "] authenticated " + steamId +
                                             " with status " + r.Status);
                                byte[] finalData = d.ToArray();
                                udp.Send(finalData, finalData.Length, EndPoint);
                            }
                            // exit thread
                        }).Start();
                    }
                    else
                    {
                        Log.WriteLog("[" + Type.ToUpper() + ":" + Connection +
                                     "] failed to authenticate due to a failed crypt key. This server is probably doing something weird.");
                    }

                    break;
            }
        }
    }
}