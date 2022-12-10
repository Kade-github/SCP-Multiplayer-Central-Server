using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Steamworks;

namespace SCPCB_MultiplayerMod_CentralServer
{
    internal class Program
    {
        public static List<Client> Clients = new List<Client>();
        public static Settings settings;
        public static bool Real = true;

        public static int MaxConnections = 20;
        
        public static void Main(string[] args)
        {
            Log.WriteLog("[CONFIG] Loading centralserver.cfg...");
            settings = new Settings("centralserver.cfg");
            Log.WriteLog("[CONFIG] Complete!");

            int listenPort = settings.Values["port"];
            MaxConnections = settings.Values["maxconnectionsfromip"];
            Log.doLogs = settings.Values["logs"] == 1;
            if (Log.doLogs)
                Log.WriteLog("[SERVER] Started Server with logs enabled!");
            
            Log.WriteLog("[STEAM] Connecting to steam...");
            
            var init = new SteamServerInit("SCP:CB Multiplayer Mod Central Server", "SCP:CB Multiplayer Mod Central Server")
            {
                GamePort = (ushort)1200,
                SteamPort = (ushort)1200,
                QueryPort = (ushort)1200,
                VersionString = "1",
                Secure = true
            };

            try
            {
                SteamServer.Init(1782380, init);
            }
            catch (Exception e)
            {
                Log.WriteLog("Failed to create steam server, is steam running?");
                Console.Read();
                return;
            }

            SteamServer.LogOnAnonymous();
            
            SteamServer.OnValidateAuthTicketResponse += Steam.SteamServerOnOnValidateAuthTicketResponse;

            Steam.lastResult = new Dictionary<SteamId, Result>();
            
            Log.WriteLog("[STEAM] Connected to steam as Anonymous!");
            
            Log.WriteLog("[INFO] SCP - Containment Breach Multiplayer Mod Central Server");
            
            
            UdpClient listener = new UdpClient(listenPort);
            TcpListener tcpListener = TcpListener.Create(listenPort);


            try
            {
                tcpListener.Start();

                Log.WriteLog("[SERVER] Started server on port " + listenPort);

                Thread udp = new Thread(() =>
                {
                    UdpServer(listener);
                });
                udp.Start();

                while (Real)
                {
                    string command = Console.ReadLine();
                    switch (command?.ToLower())
                    {
                        case "quit":
                            Log.WriteLog("[SERVER] Stopping...");
                            Real = false;
                            udp.Join();
                            break;
                        case "help":
                            Log.WriteLog("[SERVER] Commands:");
                            Log.WriteLog("[SERVER] quit");
                            Log.WriteLog("[SERVER] help");
                            break;
                        default:
                            Log.WriteLog("[SERVER] '" + command + "' is not a command! Type 'help' for a list.");
                            break;
                    }
                    Thread.Sleep(1);
                }
            }
            catch (Exception e)
            {
                Log.WriteLog("[EXCEPTION] " + e);
            }
            SteamServer.Shutdown();
            listener.Close();
        }

        public static async void TestUDP(int listenPort)
        {
            UdpClient c = new UdpClient();
            c.Connect("127.0.0.1", listenPort);
            Log.WriteLog("Sending test message!");
            await c.SendAsync(new byte[] {4}, 1);
            Log.WriteLog("Sent, awaiting response...");
            Task<UdpReceiveResult> result = c.ReceiveAsync();
                
            Log.WriteLog("Test UDP Connection result: " + Encoding.UTF8.GetString(result.Result.Buffer));
                
            c.Dispose();
        }
        
        public static async void UdpServer(UdpClient listener)
        {
            Log.WriteLog("[SERVER] Udp port up and running...");
            while (true)
            {
                UdpReceiveResult result = await listener.ReceiveAsync();

                string ip = result.RemoteEndPoint.Address.MapToIPv4().ToString();
                
                Client c = Client.FindClientInList(Clients, ip);
    
                if (c == null)
                {
                    c = new Client(ip, "udp");
                    Log.WriteLog("First connection from " + ip);
                    Clients.Add(c);
                }

                if ((DateTime.Now - c.lastConnection).Seconds > 2)
                    c.connections = 0;
                else
                {
                    c.connections++;
                    if (c.connections > MaxConnections)
                        continue;
                }
                
                c.lastConnection = DateTime.Now;

                c.udp = listener;
                
                c.Type = "udp";
                c.EndPoint = result.RemoteEndPoint;

                c.ReadBytes(result.Buffer);
            }
        }
        public static async void TcpServer(TcpListener listener)
        {
            Log.WriteLog("[SERVER] Tcp port up and running...");
            // check stuff

            Thread checkThread = new Thread(() =>
            {
                foreach (Client c in Clients)
                {
                    if (c.tcp != null)
                    {
                        if (c.tcp.Connected)
                        {
                            byte[] msg = new byte[1024];
                            msg[0] = 255;
                            c.tcp.GetStream().Read(msg, 0, msg.Length);
                            if (msg[0] != 255)
                            {
                                c.Type = "tcp";
                                c.ReadBytes(msg);
                            }
                        }
                    }
                }
            
            });           
            
            checkThread.Start();
            
            while (true)
            {
                TcpClient client = await listener.AcceptTcpClientAsync();

                string ip = ((IPEndPoint) client.Client.RemoteEndPoint).Address.MapToIPv4().ToString();
                
                Client c = Client.FindClientInList(Clients, ip);

                if (c == null)
                {
                    c = new Client(ip, "tcp");
                    Clients.Add(c);
                    Log.WriteLog("First connection from " + ip);
                }
                c.Type = "tcp";
                c.tcp = client;

            }
        }
    }
}