using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace UdpChat
{
    public class UdpBus
    {
        UdpClient _udpClient;
        public int Port { get; private set; } = 8000;
        string _fullName;

        public static readonly List<MyUdpClient> Clients = new List<MyUdpClient>();

        public IPAddress[] ListOfMyIps()
        {
            IPHostEntry ipHost = Dns.GetHostByName(Dns.GetHostName());
            return ipHost.AddressList;
        }

        public void Connect(string fullName, string ip, int port)
        {
            Port = port;
            _fullName = string.IsNullOrWhiteSpace(fullName) ? ip : fullName; 
            while (true)
            {
                var ipEndPoint = new IPEndPoint(IPAddress.Parse(ip), Port);
                try
                {

                    _udpClient = new UdpClient(ipEndPoint);
                    Clients.Add(new MyUdpClient(fullName, ipEndPoint.Address.ToString(), Port));
                    break;
                }
                catch (SocketException ex)
                {
                    if (ex.SocketErrorCode == SocketError.AddressAlreadyInUse)
                        Clients.Add(new MyUdpClient(ipEndPoint.Address.ToString(), ipEndPoint.Address.ToString(), Port));
                    Port++;
                }
            }
        }

        public void InfornOthers()
        {
            var sendBytes = Encoding.UTF8.GetBytes($"{_fullName}:{_udpClient.Client.LocalEndPoint} just joined.");
            foreach (var item in Clients.Where(a => a.Port != Port))
                _udpClient.SendAsync(sendBytes, sendBytes.Length, item.IpEndPoint).ContinueWith(a => { });
        }

        public void StartListeningAsync(Action<Task<UdpReceiveResult>> onMsgReceived)
        {
            while (true)
            {
                // Blocks until a message returns on this socket from a remote host.
                _udpClient.ReceiveAsync().ContinueWith(onMsgReceived);
            }
        }

        public Task<int> SendAsync(string to, string msg)
        {
            var datagram = Encoding.UTF8.GetBytes(msg);
            var c = Clients.First(a => a.FullName == to || a.Host==to);
            return SendAsync(datagram, new IPEndPoint(IPAddress.Parse( c.Host),c.Port));
        }

        public Task<int> SendAsync(string msg, IPEndPoint endPoint)
        {
            var datagram = Encoding.UTF8.GetBytes(msg);
            return SendAsync(datagram, endPoint);
        }

        public Task<int> SendAsync(byte[] datagram, IPEndPoint endPoint)
        {
            return _udpClient.SendAsync(datagram, datagram.Length, endPoint);
        }

        public class MyUdpClient
        {
            public string Host { get; }
            public int Port { get; }
            public string FullName { get; set; }

            public IPEndPoint IpEndPoint => new IPEndPoint(IPAddress.Parse(Host), Port);
            public MyUdpClient(string fullName, string host, int port)
            {
                FullName = string.IsNullOrWhiteSpace( fullName)? host:fullName;
                Host = host;
                Port = port;
            }

            public override string ToString() => $"{FullName}";
        }
    }
}
