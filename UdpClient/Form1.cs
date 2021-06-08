using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace UdpChat
{
    public partial class Form1 : Form
    {
        string seperator = $"{Environment.NewLine}---------------------{Environment.NewLine}";
        UdpBus _udpBus = new UdpBus();

        List<(string from, string to, string msg,DateTime dateTime)> Msgs = new List<(string from, string to, string msg, DateTime dateTime)>();

        public Form1()
        {
            InitializeComponent();

            _udpBus.OnClientAddedd += (a) => RefreshOnlineClients();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            var loginForm = new LoginForm();
            DialogResult loginDialogResult = loginForm.ShowDialog(this);

            if (loginDialogResult != DialogResult.OK) { Close(); return; }

            Properties.Settings.Default["lastIP"] = loginForm.comboBox1.SelectedItem.ToString();
            Properties.Settings.Default.Save();

            _udpBus.Connect(loginForm.textBox3.Text, loginForm.comboBox1.SelectedItem.ToString(), 8000);


            _udpBus.InfornOthers();

            RefreshOnlineClients();

            Text = loginForm.textBox3.Text + ":" + _udpBus.Port.ToString();


            try
            {
                // Blocks until a message returns on this socket from a remote host.
                _udpBus.StartListeningAsync(a =>
                {
                    if (a.Status == TaskStatus.Faulted) return;

                    var receiveBytes = a.Result.Buffer;
                    string returnData = Encoding.UTF8.GetString(receiveBytes);
                    var fromIp = a.Result.RemoteEndPoint.Address.ToString();
                    var fromPort = a.Result.RemoteEndPoint.Port;
                    var toIp = _udpBus.Host;
                    var toPort = _udpBus.Port;

                    if (returnData.Contains("[just_joined]"))
                    {
                        var fullName = returnData.Split(';')[0].Trim();
                        var parts = returnData.Split(';')[0].Trim().Split(':');
                        UdpBus.Clients.Add(new UdpBus.MyUdpClient(fullName, parts[0], int.Parse(parts[1])));
                        RefreshOnlineClients();
                    }
                    else if (returnData.EndsWith("[just_left]"))
                    {
                        var parts = returnData.Split(';')[0].Trim().Split(':');
                        UdpBus.Clients.RemoveAll(x => x.FullName == parts[0] || x.Host == parts[1]);
                        RefreshOnlineClients();
                    }
                    else if (returnData.EndsWith("[what_is_your_name]"))
                    {
                        var parts = returnData.Split(';')[0].Trim();
                        _udpBus.SendAsync($"{fromIp}:{fromPort}", $"{_udpBus.FullName};[my_name_is]");
                    }
                    else if (returnData.EndsWith("[my_name_is]"))
                    {
                        var parts = returnData.Split(';')[0].Trim();
                        var c = UdpBus.Clients.First(x => x.Host == fromIp && x.Port == fromPort);
                        c.FullName = parts;
                        RefreshOnlineClients();
                    }
                    else
                    {
                        Invoke(new MethodInvoker(() =>
                        {
                            if (listBox1.SelectedItem == null || listBox1.SelectedItem.ToString() != $"{fromIp}")
                            {
                                if (!string.IsNullOrEmpty(returnData))
                                {
                                    notifyIcon1.Icon = Icon;
                                    notifyIcon1.ShowBalloonTip(800, fromIp, returnData, ToolTipIcon.None);
                                    //notifyIcon1.Visible = false;
                                }

                            }

                        }));




                        var c = UdpBus.Clients.FirstOrDefault(x => x.Host == fromIp && x.Port == fromPort);
                        if (c == null)
                        {
                            c = new UdpBus.MyUdpClient($"{fromIp}:{fromPort}", fromIp, fromPort);
                            UdpBus.Clients.Add(c);
                        }
                        Msgs.Add((c.FullName, "Me", returnData,DateTime.Now));
                        RefreshMsgs();

                        Invoke(new MethodInvoker(() =>
                        {
                            panel2.Controls.Add(new Label
                            {
                                BackColor = Color.Blue,
                                Text = returnData,
                                Margin = new Padding(0, 0, 0, 15)
                            });
                        }));
                       
                    }
                });

            }
            catch (Exception ex)
            {
                Console.WriteLine(e.ToString());
            }



        }


        void ThreadSafeUi(Action action)
        {
            Invoke(new MethodInvoker(() =>action()));
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            _udpBus.Disconnect();
        }

        private void button1_Click(object sender, EventArgs e)
        {

            if (listBox1.SelectedItem != null)
            {
                var p = listBox1.SelectedItem.ToString();
                Msgs.Add(("Me", p, textBox1.Text, DateTime.Now));
                _udpBus.SendAsync(p, textBox1.Text).ContinueWith(a =>
                 {
                     Invoke(new MethodInvoker(() =>
                     {
                         textBox1.Text = "";
                     }));

                     Invoke(new MethodInvoker(() =>
                     {
                         panel2.Controls.Add(new Label
                         {
                             BackColor = Color.Pink,
                             Text = textBox1.Text,
                             Margin = new Padding(0, 0, 0, 15)
                         });
                     }));

                     RefreshMsgs();
                 });
            }
            else
                foreach (var reciverPort in UdpBus.Clients.Where(x=>x.FullName!= "(All)"))
                {
                    _udpBus.SendAsync(textBox1.Text, reciverPort.IpEndPoint).ContinueWith(a =>
                    {
                        Invoke(new MethodInvoker(() =>
                        {
                            textBox2.AppendText($"Me to {reciverPort}:" + textBox1.Text + seperator);
                            textBox1.Text = "";
                        }));
                    });
                }
        }



        void RefreshOnlineClients()
        {
            Invoke(new MethodInvoker(() =>
            {
                listBox1.Items.Clear();
                for (int i = 0; i < UdpBus.Clients.Count; i++)
                {
                    listBox1.Items.Add(UdpBus.Clients[i]);
                }

            }));


        }

        void RefreshMsgs()
        {
            Invoke(new MethodInvoker(() =>
            {
                if (listBox1.SelectedItem == null) return;

                label1.Text = listBox1.SelectedItem.ToString();

                textBox2.Text = "";
                var ourMsg = string.Join(seperator, 
                    Msgs.Where(x => x.from == label1.Text || x.to == label1.Text).Select(x => $"{x.from}          {x.dateTime}\r\n{x.msg}").ToList());
                textBox2.Text = ourMsg;
            }));
        }

        private void listBox1_DoubleClick(object sender, EventArgs e)
        {
            if (listBox1.SelectedItem == null) return;

            RefreshMsgs();

        }

        private void button2_Click(object sender, EventArgs e)
        {
            Invoke(new MethodInvoker(() =>
            {
                //panel1.Visible = false;
                //listBox1.Visible = true;
                textBox2.Text = "";
            }));

        }

    }
}
