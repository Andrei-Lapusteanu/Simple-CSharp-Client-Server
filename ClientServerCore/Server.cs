using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace ClientServer
{
    class Server
    {
        private IPAddress localAddr;
        private TcpListener tcpListener;
        private List<ClientHandler> chList = new List<ClientHandler>();
        private bool isServerRunning = false;

        private BackgroundWorker bgWorker;
        private BackgroundWorker bgWorkerPingClients;

        public Server()
        {
            this.localAddr = null;
            this.tcpListener = null;
            InitializebgWorker();
        }

        void bgWorker_DoWork(object obj, DoWorkEventArgs e)
        {
            while (tcpListener != null)
            {
                WaitForClient();
            }
        }

        void bgWorker_WorkCompleted(object obj, RunWorkerCompletedEventArgs e)
        {
            // If server gets stopped
            DisconnectClients();
            chList = new List<ClientHandler>();
            isServerRunning = false;
        }

        void bgWorkerPingClients_DoWork(object obj, DoWorkEventArgs e)
        {
             while (true)
            {
                foreach (ClientHandler ch in chList)
                    if (ch.ClientSocket == null)
                    {
                        chList.Remove(ch);
                        bgWorkerPingClients.ReportProgress(0, chList.Count);
                    }

                bgWorkerPingClients.ReportProgress(0, chList.Count);

                Thread.Sleep(250);
            }
        }

        void bgWorkerPingClients_ProgressChanged(object obj, ProgressChangedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                ((MainWindow)Application.Current.MainWindow).labelConnectedClients.Content = e.UserState.ToString();
            }), DispatcherPriority.ContextIdle);
        }

        public string StartServer(string ipAddressString, int portNumber)
        {
            try
            {
                if (!isServerRunning)
                {
                    // Parse IP and port number
                    localAddr = IPAddress.Parse(ipAddressString);
                    tcpListener = new TcpListener(IPAddress.Any, portNumber);

                    // Start BackgroundWorker and TcpListener
                    tcpListener.Start();
                    isServerRunning = true;

                    bgWorker.RunWorkerAsync();
                    
                    return "Server started successfully.";
                }
                else
                    return "Server is already running.";
            }
            catch (Exception ex) { return "Failure to start server! Cause of error: \n" + ex; }
        }

        public string StopServer()
        {
            try
            {
                if (isServerRunning)
                {
                    tcpListener.Stop();
                    tcpListener = null;

                    return "Server stopped.";
                }
                else
                    return "Server is already stopped.";
            }
            catch (Exception ex) { return "Failure to stop server! Cause of error: \n" + ex; }
        }

        private void WaitForClient()
        {
            try
            {
                // Poll for a client
                TcpClient client = new TcpClient();
                client = tcpListener.AcceptTcpClient();

                ClientHandler ch = new ClientHandler(client);
                chList.Add(ch);

                bgWorkerPingClients.RunWorkerAsync();
            }
            catch (Exception ex) { Console.WriteLine(ex); }
        }

        private void DisconnectClients()
        {
            try
            {
                foreach (ClientHandler ch in chList)
                    ch.ForceClientDisconnect();
            }
            catch (Exception ex) { Console.WriteLine(ex); }
        }

        private void InitializebgWorker()
        {
            bgWorker = new BackgroundWorker();
            bgWorker.DoWork += new DoWorkEventHandler(bgWorker_DoWork);
            bgWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bgWorker_WorkCompleted);

            bgWorkerPingClients = new BackgroundWorker();
            bgWorkerPingClients.DoWork += new DoWorkEventHandler(bgWorkerPingClients_DoWork);
            bgWorkerPingClients.ProgressChanged += new ProgressChangedEventHandler(bgWorkerPingClients_ProgressChanged);
            bgWorkerPingClients.WorkerReportsProgress = true;
        }
    }
}
