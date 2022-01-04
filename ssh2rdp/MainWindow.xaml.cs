using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace ssh2rdp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private secureShell myShell;

        public MainWindow()
        {
            InitializeComponent();
            myShell = new secureShell();
        }

        public void Window_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        public void MinBtn_Click(object sender, RoutedEventArgs e)

        {
            this.WindowState = WindowState.Minimized;
        }

        public void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
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

            if (rhost.Text == string.Empty || shost.Text == string.Empty || user.Text == string.Empty || pass.Password == string.Empty)
            {
                MessageBox.Show("Must enter credentials!");
            }
            else
            {
                var newPort = (uint)GetRandomUnusedPort();

                var authMethod = new List<AuthenticationMethod>();
                authMethod.Add(new PasswordAuthenticationMethod(user.Text, pass.Password));

                ConnectionInfo sshConnInfo = new ConnectionInfo(shost.Text, 22, user.Text, authMethod.ToArray());

                SshClient cSSH = new SshClient(sshConnInfo);
                var port = new ForwardedPortLocal("127.0.0.1", newPort, rhost.Text, 3389);

                try
                {
                    cSSH.Connect();
                }
                catch (Exception)
                {
                    cSSH.Disconnect();
                }
                finally
                {
                    if (cSSH.IsConnected)
                    {
                        rhost.IsReadOnly = true;
                        user.IsReadOnly = true;


                        cSSH.AddForwardedPort(port);

                        port.Start();

                        sshBtn.Background = Brushes.LimeGreen;
                        sshBtn.Content = "Connected!";
                        sshBtn.IsEnabled = false;

                        List<secureShell> myShell = new List<secureShell>();
                        if (cSSH.IsConnected)
                        {
                            myShell.Add(new secureShell() { host = cSSH.ConnectionInfo.Host, username = cSSH.ConnectionInfo.Username, lPort = (int)newPort });
                        }

                        listViewShell.ItemsSource = myShell;

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
                    }
                    else
                    {
                        MessageBox.Show("Connection failed! Try again.");
                    }
                }
            }
        }
    }

    public class secureShell
    {
        public string host { get; set; }
        public string username { get; set; }
        public int lPort { get; set; }
    }
}
