using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using BnC;

namespace lab3Client.dialogs
{
    public partial class chooseServer : Window
    {
        public string host;

        public chooseServer(string host = null, string port = null)
        {
            InitializeComponent();
            hostBox.Text = host;
            portBox.Text = port;
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            StringBuilder sb = new StringBuilder();
            if (hostBox.Text.Length > 6)
                if (hostBox.Text.Substring(0, 7) != "http://")
                    sb.Append("http://");
            sb.Append(hostBox.Text);
            sb.Append(":");
            sb.Append(portBox.Text);
            sb.Append("/");
            host = sb.ToString();
            try
            {
                if (Client.TryConnection(host))
                    DialogResult = true;
                else
                    MessageBox.Show(Properties.Resources.NoConnection);
            }
            catch
            {
                MessageBox.Show(Properties.Resources.NoConnection);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
