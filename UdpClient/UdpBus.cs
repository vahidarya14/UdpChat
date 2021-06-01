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
        public string Host { get; private set; }
        public int Port { get; private set; } = 8000;
        public string FullName { get; private set; }

        public static readonly List<MyUdpClient> Clients = new List<MyUdpClient>();

        public IPAddress[] ListOfMyIps()
        {
            IPHostEntry ipHost = Dns.GetHostEntry(Dns.GetHostName());
            return ipHost.AddressList;
        }

        public void Connect(string fullName, string ip, int port)
        {
            Port = port;
            Host = ip;
            FullName = string.IsNullOrWhiteSpace(fullName) ? $"{Host}:{Port}" : fullName; 
            while (true)
            {
                var ipEndPoint = new IPEndPoint(IPAddress.Parse(Host), Port);
                try
                {

                    _udpClient = new UdpClient(ipEndPoint);
                    Clients.Add(new MyUdpClient(fullName, Host, Port));
                    break;
                }
                catch (SocketException ex)
                {
                    if (ex.SocketErrorCode == SocketError.AddressAlreadyInUse)
                        Clients.Add(new MyUdpClient("", Host, Port));
                    Port++;
                }
            }

            foreach (var client in Clients)
            {
                if (client.TempName != client.FullName)
                {
                    var sendBytes = Encoding.UTF8.GetBytes($"{client.IpEndPoint};[what_is_your_name]");
                        _udpClient.SendAsync(sendBytes, sendBytes.Length, client.IpEndPoint);
                }
            }
        }

        public void Disconnect()
        {
            if (_udpClient == null) return;

            var sendBytes = Encoding.UTF8.GetBytes($"{FullName}:{_udpClient.Client.LocalEndPoint};[just_left]");
            foreach (var item in Clients.Where(a => a.Port != Port))
                _udpClient.SendAsync(sendBytes, sendBytes.Length, item.IpEndPoint).ContinueWith(a => { });
            _udpClient.Dispose();
        }

        public void InfornOthers()
        {
            var sendBytes = Encoding.UTF8.GetBytes($"{FullName}:{_udpClient.Client.LocalEndPoint};[just_joined]");
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
            var c = Clients.First(a => a.FullName == to || a.Host+":"+a.Port==to);
            return SendAsync(msg, new IPEndPoint(IPAddress.Parse( c.Host),c.Port));
        }

        public Task<int> SendAsync(string msg, IPEndPoint endPoint)
        {
            var datagram = Encoding.UTF8.GetBytes(msg);
            return _udpClient.SendAsync(datagram, datagram.Length, endPoint);
        }


        public class MyUdpClient
        {
            public string Host { get; }
            public int Port { get; }
            public string FullName { get; set; }
            public string TempName  => string.IsNullOrWhiteSpace(FullName) ?  $"{Host}:{Port}" : FullName;

            public IPEndPoint IpEndPoint => new IPEndPoint(IPAddress.Parse(Host), Port);
            public MyUdpClient(string fullName, string host, int port)
            {
                FullName = fullName;
                Host = host;
                Port = port;
            }

            public override string ToString() =>string.IsNullOrWhiteSpace(FullName)?TempName:FullName;
        }
    }
}
