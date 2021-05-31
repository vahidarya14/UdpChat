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
