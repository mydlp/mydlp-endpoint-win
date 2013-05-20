//    Copyright (C) 2011 Huseyin Ozgur Batur <ozgur@medra.com.tr>
//
//--------------------------------------------------------------------------
//    This file is part of MyDLP.
//
//    MyDLP is free software: you can redistribute it and/or modify
//    it under the terms of the GNU General Public License as published by
//    the Free Software Foundation, either version 3 of the License, or
//    (at your option) any later version.
//
//    MyDLP is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//    GNU General Public License for more details.
//
//    You should have received a copy of the GNU General Public License
//    along with MyDLP.  If not, see <http://www.gnu.org/licenses/>.
//--------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.IO;
using Microsoft.Win32;
using System.Collections;

namespace MyDLP.EndPoint.Core
{
    public class AsyncTcpServer
    {
        private TcpListener tcpListener;
        private List<Client> clients;

        public AsyncTcpServer(IPAddress localaddr, int port)
            : this()
        {
            tcpListener = new TcpListener(localaddr, port);
        }


        private AsyncTcpServer()
        {
            this.clients = new List<Client>();
        }

        public IEnumerable<TcpClient> TcpClients
        {
            get
            {
                foreach (Client client in this.clients)
                {
                    yield return client.TcpClient;
                }
            }
        }

        public void Start()
        {
            this.tcpListener.Start();
            this.tcpListener.BeginAcceptTcpClient(AcceptTcpClientCallback, null);
        }

        public void Stop()
        {
            this.tcpListener.Stop();
            lock (this.clients)
            {
                foreach (Client client in this.clients)
                {
                    client.TcpClient.Client.Disconnect(false);
                }
                this.clients.Clear();
            }
        }

        public void Write(TcpClient tcpClient, string data)
        {
            Logger.GetInstance().Debug("SessionManager response <" + data + ">");
            byte[] bytes = Encoding.ASCII.GetBytes(data + "\r\n");
            Write(tcpClient, bytes);
        }

        public void Write(TcpClient tcpClient, byte[] bytes)
        {
            NetworkStream networkStream = tcpClient.GetStream();
            networkStream.BeginWrite(bytes, 0, bytes.Length, WriteCallback, tcpClient);
        }

        private void WriteCallback(IAsyncResult result)
        {
            TcpClient tcpClient = result.AsyncState as TcpClient;
            NetworkStream networkStream = tcpClient.GetStream();
            networkStream.EndWrite(result);
        }

        private void AcceptTcpClientCallback(IAsyncResult result)
        {
            TcpClient tcpClient = tcpListener.EndAcceptTcpClient(result);
            byte[] buffer = new byte[tcpClient.ReceiveBufferSize];
            Client client = new Client(tcpClient, buffer);
            lock (this.clients)
            {
                this.clients.Add(client);
            }
            NetworkStream networkStream = client.NetworkStream;
            networkStream.BeginRead(client.Buffer, 0, client.Buffer.Length, ReadCallback, client);
            tcpListener.BeginAcceptTcpClient(AcceptTcpClientCallback, null);
        }

        private void ReadCallback(IAsyncResult result)
        {
            Client client = result.AsyncState as Client;
            if (client == null) return;
            NetworkStream networkStream = client.NetworkStream;
            try
            {
                int read = networkStream.EndRead(result);
                if (read == 0)
                {
                    lock (this.clients)
                    {
                        this.clients.Remove(client);
                        return;
                    }
                }
                string data = Encoding.ASCII.GetString(client.Buffer, 0, read);
                data = data.Trim();
                if (data != "")
                {
                    Logger.GetInstance().Debug("SessionManager request <" + data + ">");
                    HandleData(data, client);
                }

                networkStream.BeginRead(client.Buffer, 0, client.Buffer.Length, ReadCallback, client);
            }
            catch (Exception e)
            {
                Logger.GetInstance().Error("Session Manager ReadCallback error:" + e);
            }
        }

        internal class Client
        {
            public Client(TcpClient tcpClient, byte[] buffer)
            {
                if (tcpClient == null) throw new ArgumentNullException("tcpClient");
                if (buffer == null) throw new ArgumentNullException("buffer");
                this.TcpClient = tcpClient;
                this.Buffer = buffer;
            }
            public TcpClient TcpClient { get; private set; }
            public byte[] Buffer { get; private set; }
            public NetworkStream NetworkStream { get { return TcpClient.GetStream(); } }
        }

        private void HandleData(String request, Client client)
        {
            String driveLetter = "";
            String format = "";
            try
            {
                if (request.StartsWith("BEGIN"))
                {
                    Write(client.TcpClient, "OK");
                }

                else if (request.StartsWith("HASKEY"))
                {
                    if (Configuration.HasEncryptionKey)
                    {
                        Write(client.TcpClient, "OK YES");
                    }
                    else
                    {
                        Write(client.TcpClient, "OK NO");
                    }
                }

                else if (request.StartsWith("NEWVOLUME"))
                {

                    driveLetter = request.Split(' ')[1];

                    if (!DiskCryptor.DoesDriveLetterNeedsFormatting(driveLetter) || !Configuration.RemovableStorageEncryption)
                    {
                        Write(client.TcpClient, "OK NOFORMAT");

                    }
                    else
                    {
                        Write(client.TcpClient, "OK NEEDFORMAT");
                    }
                }
                else if (request.StartsWith("FORMAT"))
                {
                    driveLetter = request.Split(' ')[1];
                    format = request.Split(' ')[2];
                    DiskCryptor.FormatDriveLetter(driveLetter, format);
                    Write(client.TcpClient, "OK FINISHED");
                }
                else
                {
                    Logger.GetInstance().Error("SessionServer HandleData invalid request" + request);
                    throw new InvalidRequestException("HandleData Expected valid request received:" + request);
                }
            }

            catch (InvalidRequestException e)
            {
                Write(client.TcpClient, "ERROR message:" + e);
                Logger.GetInstance().Error("SessionServer HandleData error:" + e);
            }

            catch (Exception e)
            {
                Logger.GetInstance().Error("SessionServer HandleData error:" + e);
            }

        }

        internal class InvalidRequestException : Exception
        {
            public InvalidRequestException()
                : base()
            {
            }
            public InvalidRequestException(String message)
                : base(message)
            {
            }
            public InvalidRequestException(String message, Exception InnerException)
                : base(message, InnerException)
            {
            }
        }
    }

    public class SessionServer
    {
        private static AsyncTcpServer sessionServer = null;
        private static bool started = false;

        public static void Start()
        {
            try
            {
                AddAgent();
                if (sessionServer == null)
                {
                    sessionServer = new AsyncTcpServer(IPAddress.Parse("127.0.0.1"), 9098);
                }
                if (started == false)
                {
                    Logger.GetInstance().Debug("Started Session Server");
                    sessionServer.Start();
                }
            }
            catch (Exception e)
            {
                Logger.GetInstance().Error("SessionServer Start:" + e);
            }
        }

        public static void Stop()
        {
            try
            {
                if (sessionServer != null && started)
                {
                    sessionServer.Stop();
                }
            }
            catch (Exception e)
            {
                Logger.GetInstance().Error("SessionServer Stop:" + e);
            }
        }

        public static void AddAgent()
        {
            try
            {
                if (!Environment.UserInteractive)
                {
                    RegistryKey runKey = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
                    bool exist = false;
                    foreach (String name in runKey.GetValueNames())
                    {
                        if (name == "mydlp_agent")
                        {
                            exist = true;
                        }
                    }

                    if (!exist)
                    {
                        runKey.SetValue("mydlp_agent", "\"" + Configuration.AppPath + "mydlpui.exe\"");
                    }
                }
            }
            catch (Exception e)
            {
                Logger.GetInstance().Error("Session Server: Unable to add Notification Agent:" + e);
            }

        }
    }
}
