using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace UdpChat
{
    public partial class Form1 : Form
    {
        string postDistance = $"{Environment.NewLine}---------------------{Environment.NewLine}";
        UdpBus _udpBus = new UdpBus();

        public Form1()
        {
            InitializeComponent();
            //CheckForIllegalCrossThreadCalls = false;
            panel1.Visible = false;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            var loginForm = new LoginForm();
            DialogResult loginDialogResult = loginForm.ShowDialog(this);

            if (loginDialogResult != DialogResult.OK) { Close(); return; }

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

                    if (returnData.Contains("[just_joined]"))
                    {
                        var parts = returnData.Split(';')[0].Trim().Split(':');
                        UdpBus.Clients.Add(new UdpBus.MyUdpClient(parts[0], parts[1], int.Parse(parts[2])));
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
                        _udpBus.SendAsync($"{a.Result.RemoteEndPoint.Address}:{a.Result.RemoteEndPoint.Port}", $"{_udpBus.FullName};[my_name_is]");
                    }
                    else if (returnData.EndsWith("[my_name_is]"))
                    {
                        var parts = returnData.Split(';')[0].Trim();
                        var c = UdpBus.Clients.First(x => x.Host == a.Result.RemoteEndPoint.Address.ToString() && x.Port == a.Result.RemoteEndPoint.Port);
                        c.FullName = parts;
                        RefreshOnlineClients();
                    }
                    else
                    {
                        var c = UdpBus.Clients.First(x => x.Host == a.Result.RemoteEndPoint.Address.ToString() && x.Port == a.Result.RemoteEndPoint.Port);
                  
                        Invoke(new MethodInvoker(() => {
                            textBox2.AppendText($"{c.FullName }: {returnData + postDistance} ");
                        }));
                    }
                });

            }
            catch (Exception ex)
            {
                Console.WriteLine(e.ToString());
            }
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
                _udpBus.SendAsync(p, textBox1.Text).ContinueWith(a =>
                 {
                     Invoke(new MethodInvoker(() =>
                     {
                         textBox2.AppendText($"Me to {p}:" + textBox1.Text + postDistance);
                         textBox1.Text = "";
                     }));

                 });
            }
            else
                foreach (var reciverPort in UdpBus.Clients.Where(a => a.Port != _udpBus.Port))
                {
                    _udpBus.SendAsync(textBox1.Text, reciverPort.IpEndPoint).ContinueWith(a =>
                    {
                        Invoke(new MethodInvoker(() =>
                        {
                            textBox2.AppendText($"Me to {reciverPort}:" + textBox1.Text + postDistance);
                            textBox1.Text = "";
                        }));
                    });
                }
        }



        void RefreshOnlineClients()
        {
            Invoke(new MethodInvoker(()=>
            {
                listBox1.Items.Clear();
                foreach (var port in UdpBus.Clients)
                {
                    if (port.Port != _udpBus.Port)
                        listBox1.Items.Add(port);
                    //else
                    //    listBox1.Items.Add("Me:" );
                }

            }));


        }

        private void listBox1_DoubleClick(object sender, EventArgs e)
        {
            if (listBox1.SelectedItem == null) return;

            Invoke(new MethodInvoker(() =>
            {
                label1.Text = listBox1.SelectedItem.ToString();
                panel1.Visible = true;
                listBox1.Visible = false;
            }));

        }

        private void button2_Click(object sender, EventArgs e)
        {
            Invoke(new MethodInvoker(() =>
            {
                panel1.Visible = false;
                textBox2.Text = "";
                listBox1.Visible = true;
            }));

        }

    }
}
