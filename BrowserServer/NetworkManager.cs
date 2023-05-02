using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BrowserServer
{
    public static class NetworkManager
    {
        //UDP discovery
        static UdpClient receivingClient;
        static UdpClient sendingClient;
        static Thread udpReciving;
        const int udpDiscoveryPort = 54545;
        const int udpSendDiscoveryPort = 54546;
        const string broadcastAddress = "255.255.255.255";

        delegate void AddMessage(string message);

        public static void StartUdpDiscoveryServer()
        {
            receivingClient = new UdpClient(udpDiscoveryPort);
            ThreadStart start = new ThreadStart(UdpDiscoveryReciver);
            udpReciving = new Thread(start);
            udpReciving.IsBackground = true;
            udpReciving.Start();


            sendingClient = new UdpClient(broadcastAddress, 1337);
            sendingClient.EnableBroadcast = true;

        }
        private static void UdpDiscoveryReciver()
        {
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, udpDiscoveryPort);
            AddMessage messageDelegate = UdpMessageRecived;
            while (true)
            {
                byte[] data = receivingClient.Receive(ref endPoint);
                string message = Encoding.ASCII.GetString(data);
                UdpMessageRecived(message);
            }
        }
        private static void UdpMessageRecived(string packetJSON)
        {
            try
            {
                var udpPacket = JsonConvert.DeserializeObject<DiscoveryPacket>(packetJSON);
                switch (udpPacket.PType)
                {
                    case DiscoveryPacketType.AddressRequest:
                         Console.WriteLine("request addr");
                        // byte[] data = Encoding.ASCII.GetBytes("hallo");
                        // sendingClient.Send(data, data.Length);


                        var packet = new DiscoveryPacket
                        {
                            PType = DiscoveryPacketType.ACK,
                            ServerAddress = GetLocalIPAddress()
                        };
                        var rawPacket = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(packet));
                        sendingClient.Send(rawPacket, rawPacket.Length);

                        break;

                    case DiscoveryPacketType.ACK:
                        break;
                    default:
                        break;
                }
            }
            catch (Exception){}
            
        }
        //UDP discovery

        //helpers
        public static string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            throw new Exception("No network adapters with an IPv4 address in the system!");
        }
        //helpers
    }
}
