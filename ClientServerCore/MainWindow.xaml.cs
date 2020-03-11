using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Windows;

namespace ClientServer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Server server = new Server();
        const bool resolveNames = true;
        static object lockObj = new object();
        static int upCount = 0;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void buttonStartServer_Click(object sender, RoutedEventArgs e)
        {
            textBoxInfo.AppendText(server.StartServer(textboxIP.Text, int.Parse(textboxPort.Text)) + '\n');
        }

        private void buttonStopServer_Click(object sender, RoutedEventArgs e)
        {
            textBoxInfo.AppendText(server.StopServer() + '\n');
        }

        private void buttonPing_Click(object sender, RoutedEventArgs e)
        {
            // TO DO
        }

        private static void ping_Completed(object sender, PingCompletedEventArgs e)
        {
            string ip = (string)e.UserState;
            if (e.Reply != null && e.Reply.Status == IPStatus.Success)
            {
                if (resolveNames)
                {
                    string name;
                    try
                    {
                        IPHostEntry hostEntry = Dns.GetHostEntry(ip);
                        name = hostEntry.HostName;
                    }
                    catch (SocketException ex)
                    {
                        name = "?";
                    }
                    Console.WriteLine("{0} ({1}) is up: ({2} ms)", ip, name, e.Reply.RoundtripTime);
                }
                else
                {
                    Console.WriteLine("{0} is up: ({1} ms)", ip, e.Reply.RoundtripTime);
                }
                lock (lockObj)
                {
                    upCount++;
                }
            }
            else
            {
                Console.WriteLine("Pinging {0} failed. (Null Reply object?)", ip);
            }
        }
    }
}

