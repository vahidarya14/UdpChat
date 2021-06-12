using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace UdpChat
{
    public delegate void ClientAdded(string fullAddress);
    public class UdpBus
    {

        public event ClientAdded OnClientAddedd;
        UdpClient _udpClient;
        public string Host { get; private set; }
        public int Port { get; private set; } = 8000;
        public string FullName { get; private set; }

        public static  List<MyUdpClient> Clients = new List<MyUdpClient>();

        public IPAddress[] ListOfMyIps()
        {
            IPHostEntry ipHost = Dns.GetHostEntry(Dns.GetHostName());
            return ipHost.AddressList.Where(x=>x.AddressFamily==AddressFamily.InterNetwork).ToArray();
        }

        public async void Connect(string fullName, string ip, int port)
        {
            Port = port;
            Host = ip;
            FullName = string.IsNullOrWhiteSpace(fullName) ? $"{Host}:{Port}" : fullName;

            ////----------------------------scan current system ports---------------------------------------
            //while (true)
            //{

            //    try
            //    {
            _udpClient = new UdpClient(new IPEndPoint(IPAddress.Parse(Host), Port));
            FullName = string.IsNullOrWhiteSpace(fullName) ? $"{Host}:{Port}" : fullName;

            //        //Clients.Add(new MyUdpClient(fullName, Host, Port));
            //        break;
            //    }
            //    catch (SocketException ex)
            //    {
            //        if (ex.SocketErrorCode == SocketError.AddressAlreadyInUse)
            //            Clients.Add(new MyUdpClient("", Host, Port));
            //        Port++;
            //    }
            //}

            FindClients();
        }

        public async void FindClients()
        {
            Clients.Add(new MyUdpClient("(All)", "", 8000));

            //List<Task<PingReply>> pings = new List<Task<PingReply>>();
            var from = Host.Split('.').Select(x => int.Parse(x)).ToList();
            for (int i = 1; i <= 255; i++)
            {
                var address = $"{from[0]}.{from[1]}.{from[2]}.{i}";
                if (address == Host) continue;

                Ping(address).ContinueWith(x =>
                {
                    var res = x.Result;
                    if (res.Status == IPStatus.Success && !Clients.Any(y => y.Host == res.Address.ToString()))
                    {
                        Clients.Add(new MyUdpClient(res.Address.ToString(), res.Address.ToString(), 8000));

                        SendAsync( res.Address.ToString(), $"{res.Address};[what_is_your_name]");
                        if (OnClientAddedd != null) OnClientAddedd(address);
                    }
                });
                //pings.Add(Ping(address));
            }
            //await Task.WhenAll(pings);
            //var pingResults = pings.Select(x => x.Result).Where(x=>x.Status==IPStatus.Success).ToList();
            //Clients.AddRange(pingResults.Select(x => new MyUdpClient(x.Address.ToString(), x.Address.ToString(),8000)));
            //Clients.ForEach(x => { if (OnClientAddedd != null) OnClientAddedd(x.FullName); });

            //foreach (var client in Clients)
            //{
            //    if (client.TempName != client.FullName)
            //    {
            //        var sendBytes = Encoding.UTF8.GetBytes($"{client.IpEndPoint};[what_is_your_name]");
            //        _udpClient.SendAsync(sendBytes, sendBytes.Length, client.IpEndPoint);
            //    }
            //}
        }


        public Task<PingReply> Ping(string server, int delay = 100, CancellationToken token = default)
        {
            var myPing = new Ping();
            return myPing.SendPingAsync(server);
        }




        public void Disconnect()
        {
            if (_udpClient == null) return;

            var sendBytes = Encoding.UTF8.GetBytes($"{FullName};[just_left]");
            foreach (var item in Clients.Where(a => a.Port != Port))
                _udpClient.SendAsync(sendBytes, sendBytes.Length, item.IpEndPoint).ContinueWith(a => { });
            _udpClient.Dispose();
            _udpClient = null;
            Clients = new List<MyUdpClient>();
        }

        public void InfornOthers()
        {
            foreach (var item in Clients.Where(a => a.Port != Port))
                SendAsync($"{FullName};[just_joined]", item.IpEndPoint).ContinueWith(a => { });
        }

        public void StartListeningAsync(Action<Task<UdpReceiveResult>> onMsgReceived)
        {
            while (_udpClient.Client.Connected)
            {
                // Blocks until a message returns on this socket from a remote host.
                _udpClient.ReceiveAsync().ContinueWith(onMsgReceived);
            }
        }

        public Task<int> SendAsync(string to, string msg)
        {
            var c = Clients.First(a => a.FullName == to || a.Host + ":" + a.Port == to);
            return SendAsync(msg, new IPEndPoint(IPAddress.Parse(c.Host), c.Port));
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
            public string TempName => string.IsNullOrWhiteSpace(FullName) ? $"{Host}:{Port}" : FullName;

            public IPEndPoint IpEndPoint => new IPEndPoint(IPAddress.Parse(Host), Port);
            public MyUdpClient(string fullName, string host, int port)
            {
                FullName = fullName;
                Host = host;
                Port = port;
            }

            public override string ToString() => string.IsNullOrWhiteSpace(FullName) ? TempName : FullName;
        }
    }
}
