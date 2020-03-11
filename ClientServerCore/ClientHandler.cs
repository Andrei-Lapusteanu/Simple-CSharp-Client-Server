using System;
using System.ComponentModel;
using System.Net.Sockets;
using System.Text;
using System.Windows;
using System.Windows.Threading;

namespace ClientServer
{
    class ClientHandler
    {
        private BackgroundWorker bgWorker;

        public TcpClient ClientSocket { get; set; }

        public ClientHandler(TcpClient client)
        {
            this.ClientSocket = client;
            InitializebgWorker();
            bgWorker.RunWorkerAsync();
        }

        void bgWorker_DoWork(object obj, DoWorkEventArgs e)
        {
            // Client got connected
            bgWorker.ReportProgress(2, ClientSocket);

            while (ClientSocket != null)
            {
                string message = ReadFromClient();

                // Client has message
                if (message != "")
                    bgWorker.ReportProgress(1, message);

                // Client disconnected
                else if (message == "" && !CheckIfClientAlive())
                {
                    bgWorker.ReportProgress(0, ClientSocket);
                    break;
                }
            }
        }

        void bgWorker_ProgressChanged(object obj, ProgressChangedEventArgs e)
        {
            try
            {
                if (ClientSocket != null)
                {
                    // If client disconnected
                    if (e.ProgressPercentage == 0)
                    {
                        PrintConnectionAction(e.UserState as TcpClient, " disconnected.\n");
                        CloseClientSocket();
                    }

                    // If client sent a message
                    else if (e.ProgressPercentage == 1)
                        PrintMessage(e.UserState.ToString());
               
                    // If client connected
                    else if (e.ProgressPercentage == 2)
                        PrintConnectionAction(e.UserState as TcpClient, " connected.\n");
                        
                }
            }
            catch (Exception ex) { Console.WriteLine(ex); }
        }

        private string ReadFromClient()
        {
            try
            {
                // Get stream and buffer from client
                NetworkStream nwStream = ClientSocket.GetStream();
                byte[] buffer = new byte[ClientSocket.ReceiveBufferSize];

                // Read stream from client
                int bytesRead = nwStream.Read(buffer, 0, ClientSocket.ReceiveBufferSize);
                string dataRecieved = Encoding.ASCII.GetString(buffer, 0, bytesRead);

                return dataRecieved;
            }
            catch (Exception ex) { Console.WriteLine(ex); return null; }
        }

        private bool CheckIfClientAlive()
        {
            return !(ClientSocket.Client.Poll(1000, SelectMode.SelectRead) && ClientSocket.Client.Available == 0);
        }

        private void CloseClientSocket()
        {
            ClientSocket.Close();
            ClientSocket = null;
        }

        public void ForceClientDisconnect()
        {
            if (ClientSocket != null)
            {
                PrintConnectionAction(ClientSocket, " was forced to disconnect.\n");
                CloseClientSocket();
            }
        }

        private void PrintConnectionAction(TcpClient client, string statusMessage)
        {
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                ((MainWindow)Application.Current.MainWindow).textBoxInfo.AppendText(
                "Client " + client.Client.RemoteEndPoint + statusMessage);
            }), DispatcherPriority.ContextIdle);
        }

        private void PrintMessage(string message)
        {
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                if (message != "\r\n")
                    ((MainWindow)Application.Current.MainWindow).textBoxInfo.AppendText(
                        ClientSocket.Client.RemoteEndPoint + ": " + message + '\n');
            }), DispatcherPriority.ContextIdle);
        }

        private void InitializebgWorker()
        {
            bgWorker = new BackgroundWorker();
            bgWorker.DoWork += new DoWorkEventHandler(bgWorker_DoWork);
            bgWorker.ProgressChanged += new ProgressChangedEventHandler(bgWorker_ProgressChanged);
            bgWorker.WorkerReportsProgress = true;
        }
    }
}
