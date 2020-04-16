using Chamaczi.Properties;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;

namespace Chamaczi
{
    public partial class AppForm : Form
    {
        String address = "s.jach.me";
        String networkName = "Chamaczi";
        TcpClient client;
        String localIp;

        public AppForm()
        {
            InitializeComponent();
            executeScript($"Add-VpnConnection -Name {networkName} -ServerAddress {address}");
        }

        private void onConnectButtonClick(object sender, EventArgs e)
        {
            executeScript($"rasdial {networkName} vpn vpn");
           
            try
            {
                client = new TcpClient(address, 8080);
                heartbeatTimer.Start();
            } catch
            {
                statusLabel.Text = "Server offline.";
            }
        }

        private void onTimerTick(object sender, EventArgs e)
        {
            if (localIp != "")
            {
                String clients = sendMessage("onPing=" + Environment.MachineName + " " + localIp);

                treeView.Nodes[0].Nodes.Clear();
                foreach (String client in clients.Split('|'))
                {
                    treeView.Nodes[0].Nodes.Add(new TreeNode(client));
                }
                treeView.ExpandAll();
                localIp = Dns.GetHostEntry(Dns.GetHostName()).AddressList.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork).ToString();
                ipTextBox.Text = localIp;
            }
        }

        private void onFormClosing(object sender, FormClosingEventArgs e)
        {
            executeScript($"rasdial {networkName} /disconnect");
        }

        private String sendMessage(String command)
        {
            try
            {
                Byte[] data = System.Text.Encoding.ASCII.GetBytes(command);
                NetworkStream stream = client.GetStream();
                stream.Write(data, 0, data.Length);

                data = new Byte[256];
                String responseData = String.Empty;

                Int32 bytes = stream.Read(data, 0, data.Length);
                responseData = System.Text.Encoding.ASCII.GetString(data, 0, bytes);
                statusLabel.Image = Resources.power;
                statusLabel.Text = "Connected.";
                connectButton.Enabled = false;
                return responseData;
            }
            catch
            {
                statusLabel.Image = Resources.green_energy;
                statusLabel.Text = "Disconnected.";
                connectButton.Enabled = true;
                return "Error";
            }
        }

        private void executeScript(string line)
        {
            var psCommandBytes = System.Text.Encoding.Unicode.GetBytes(line);
            var psCommandBase64 = Convert.ToBase64String(psCommandBytes);

            var startInfo = new ProcessStartInfo()
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy unrestricted -EncodedCommand {psCommandBase64}",
                UseShellExecute = false
            };
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;
            Process.Start(startInfo);
        }
    }
}
