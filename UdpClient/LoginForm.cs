using System;
using System.Windows.Forms;

namespace UdpChat
{
    public partial class LoginForm : Form
    {
        public LoginForm()
        {
            InitializeComponent();


            foreach (var item in new UdpBus().ListOfMyIps())
                comboBox1.Items.Add(item);

            var lastIp = Properties.Settings.Default["lastIP"].ToString();
            if (lastIp != null)
            {
                for (int i = 0; i < comboBox1.Items.Count; i++)
                {
                    if (string.Compare(comboBox1.Items[i].ToString(), lastIp)==0)
                        comboBox1.SelectedIndex = i;
                }

            }


        }

        private void button1_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }
    }
}
