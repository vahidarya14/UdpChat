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
        UdpBus UdpBus = new UdpBus();

        public Form1()
        {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;
            panel1.Visible = false;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            var loginForm = new LoginForm();
            DialogResult loginDialogResult  = loginForm.ShowDialog(this);

            if (loginDialogResult != DialogResult.OK) { Close(); return; }

            UdpBus.Connect(loginForm.textBox3.Text, loginForm.comboBox1.SelectedItem.ToString(), 8000);

            UdpBus.InfornOthers();

            RefreshOnlineClients();

            Text = loginForm.textBox3.Text+":"+UdpBus.Port.ToString();

            try
            {
                // Blocks until a message returns on this socket from a remote host.
                UdpBus.StartListeningAsync(a =>
                {
                    if (a.Status == TaskStatus.Faulted) return;

                    var receiveBytes = a.Result.Buffer;
                    string returnData = Encoding.UTF8.GetString(receiveBytes);

                    if (returnData.Contains(" just joined."))
                    {
                        var parts = returnData.Replace(" just joined.", "").Trim().Split(':');
                        UdpBus.Clients.Add(new UdpBus.MyUdpClient(parts[0], parts[1], int.Parse(parts[2])));
                        RefreshOnlineClients();
                    }
                    else
                    {
                        var c = UdpBus.Clients.First(x => x.Host == a.Result.RemoteEndPoint.Address.ToString() && x.Port == a.Result.RemoteEndPoint.Port);
                        textBox2.AppendText($"{c.FullName }: {returnData + postDistance} ");
                    }
                });

            }
            catch (Exception ex)
            {
                Console.WriteLine(e.ToString());
            }
        }



        private void button1_Click(object sender, EventArgs e)
        {

            if (listBox1.SelectedItem != null)
            {
                var p = listBox1.SelectedItem.ToString();
                UdpBus.SendAsync(p,textBox1.Text).ContinueWith(a =>
                {
                    textBox2.AppendText($"Me to {p[0]+":"+p[1]}:" + textBox1.Text + postDistance);
                    textBox1.Text = "";
                });
            }
            else
                foreach (var reciverPort in UdpBus.Clients.Where(a => a.Port != UdpBus. Port))
                {
                    UdpBus.SendAsync(textBox1.Text, reciverPort.IpEndPoint).ContinueWith(a =>
                    {
                        textBox2.AppendText($"Me to {reciverPort}:" + textBox1.Text + postDistance);
                        textBox1.Text = "";
                    });
                }
        }



        void RefreshOnlineClients()
        {
            listBox1.Items.Clear();
            foreach (var port in UdpBus.Clients)
            {
                if (port.Port != UdpBus.Port)
                    listBox1.Items.Add(port);
                //else
                //    listBox1.Items.Add("Me:" );
            }
        }

        private void listBox1_DoubleClick(object sender, EventArgs e)
        {
            label1.Text = listBox1.SelectedItem.ToString();
            panel1.Visible = true;
            listBox1.Visible = false;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            panel1.Visible = false;
            textBox2.Text = "";
            listBox1.Visible = true;
        }
    }
}
