using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ssh2rdp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private SshClient sshclient;

        public MainWindow()
        {
            InitializeComponent();
            sshDisBtn.Visibility = Visibility.Hidden;
        }
        public static int GetRandomUnusedPort()
        {
            var listener = new TcpListener(IPAddress.Any, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        public void Button_Click(object sender, RoutedEventArgs e)
        {

            if (rhost.Text == string.Empty || user.Text == string.Empty || pass.Password == string.Empty)
            {
                MessageBox.Show("Must enter credentials!");
            }
            else
            {
                var newPort = (uint)GetRandomUnusedPort();

                var authMethod = new List<AuthenticationMethod>();
                authMethod.Add(new PasswordAuthenticationMethod(user.Text, pass.Password));

                ConnectionInfo sshConnInfo = new ConnectionInfo("Enter-Host", 22, user.Text, authMethod.ToArray());

                sshclient = new SshClient(sshConnInfo);
                var port = new ForwardedPortLocal("127.0.0.1", newPort, rhost.Text, 3389);

                try
                {
                    sshclient.Connect();
                }
                catch (Exception)
                {
                    sshclient.Disconnect();
                }
                finally
                {
                    if (sshclient.IsConnected)
                    {
                        sshDisBtn.Visibility = Visibility.Visible;
                        sshclient.AddForwardedPort(port);

                        port.Start();

                        listViewShell.Items.Add(new secureShell() { host = sshclient.ConnectionInfo.Host, remote = rhost.Text, username = sshclient.ConnectionInfo.Username, lPort = (int)newPort });


                        Thread.Sleep(1000);

                        string lp = newPort.ToString();

                        var process = new Process
                        {
                            StartInfo =
                            {
                                FileName = Environment.ExpandEnvironmentVariables(@"%SystemRoot%\system32\cmdkey.exe"),
                                Arguments = string.Format(@"/generic:TERMSRV{0} /user:{1} /pass:{2}", "127.0.0.1:{3}", user.Text, pass.Password, lp),
                                WindowStyle = ProcessWindowStyle.Hidden
                            }
                        };
                        process.Start();

                        process.StartInfo.FileName = Environment.ExpandEnvironmentVariables(@"%SystemRoot%\system32\mstsc.exe");
                        process.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
                        process.StartInfo.Arguments = string.Format("{0} /f /v {1}", "C:\\Users\\" + user.Text + "\\Documents\\Default.rdp", "127.0.0.1:" + lp);
                        process.Start();

                        rhost.Clear();
                        user.Clear();
                        pass.Clear();
                    }
                    else
                    {
                        MessageBox.Show("Connection failed! Try again.");
                    }
                }
            }
        }

        public void Disconnect_Click(object sender, RoutedEventArgs e)
        {
            if (sshclient.IsConnected)
            {
                sshclient.Disconnect();
                sshclient.Dispose();
                listViewShell.ClearValue(ListView.ItemsSourceProperty);
                sshDisBtn.Visibility = Visibility.Collapsed;
            }
            else if (!sshclient.IsConnected)
            {
                MessageBox.Show(sshclient.ConnectionInfo.ToString());
            }
        }
    }

    public class secureShell
    {
        public string host { get; set; }
        public string remote { get; set; }
        public string username { get; set; }
        public int lPort { get; set; }
    }
}
