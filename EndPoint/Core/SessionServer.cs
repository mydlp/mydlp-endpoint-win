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

namespace MyDLP.EndPoint.Core
{
    public class SessionServer
    {

        public static SessionServer GetInstance()
        {
            if (sessionServer == null)
            {
                sessionServer = new SessionServer();
            }

            return sessionServer;
        }

        public void Stop()
        {
            //stop connection listener
            tcpListener.Stop();
        }

        private TcpListener tcpListener;
        private Thread listenThread;
        private static SessionServer sessionServer = null;

        private SessionServer()
        {
            this.tcpListener = new TcpListener(IPAddress.Any, 9098);//IPAddress.Parse("127.0.0.1"), 9098);
            this.listenThread = new Thread(new ThreadStart(ListenConnections));
            // this.listenThread.Start();
        }
        private void ListenConnections()
        {
            this.tcpListener.Start();

            while (true)
            {

                TcpClient client = this.tcpListener.AcceptTcpClient();

                Thread clientThread = new Thread(new ParameterizedThreadStart(HandleClient));
                clientThread.Start();
            }
        }

        private void HandleClient(object client)
        {
            TcpClient tcpClient = (TcpClient)client;
            NetworkStream clientStream = tcpClient.GetStream();

            byte[] message = new byte[4096];
            int bytesRead;

            while (true)
            {
                bytesRead = 0;

                try
                {
                    //blocks until a client sends a message
                    bytesRead = clientStream.Read(message, 0, 4096);
                    clientStream.Write(message, 0, 4096);
                }
                catch
                {
                    //a socket error has occured
                    break;
                }

                if (bytesRead == 0)
                {
                    //the client has disconnected from the server
                    break;
                }

                //message has successfully been received
                ASCIIEncoding encoder = new ASCIIEncoding();
                Logger.GetInstance().Debug("Session Agent received:<" + encoder.GetString(message, 0, bytesRead) + ">");
            }

            tcpClient.Close();
        }
    }
}
