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
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Threading;

namespace MyDLP.SeapTestServer
{
    class Program
    {
        static void Main(string[] args)
        {
            Server server = new Server();
        }
    }

    class Server
    {
        private TcpListener tcpListener;
        private Thread listenThread;

        public Server()
        {
            this.tcpListener = new TcpListener(IPAddress.Any, 4000);
            this.listenThread = new Thread(new ThreadStart(ListenForClients));
            this.listenThread.Start();
        }

        private void ListenForClients()
        {
            this.tcpListener.Start();

            while (true)
            {
                //blocks until a client has connected to the server
                TcpClient client = this.tcpListener.AcceptTcpClient();

                //create a thread to handle communication 
                //with connected client
                Thread clientThread = new Thread(new ParameterizedThreadStart(HandleClientComm));
                clientThread.Start(client);
            }
        }

        private void HandleClientComm(object client)
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
                String request = System.Text.Encoding.ASCII.GetString(message, 0, bytesRead);
                Console.WriteLine("<Request length: " + request.Length + " request: " + request + ">");
                String path = request.Split(' ')[2];
                String op = request.Split(' ')[0];
                String respString = "ALLOW";

                if (op.Contains("WRITE"))
                {
                    if (path.ToLower().Contains("blockwrite"))
                        respString = "BLOCK";
                }
                else if (op.Contains("READ"))
                {
                    if (path.ToLower().Contains("blockread"))
                        respString = "BLOCK";
                }

                Byte[] response = System.Text.Encoding.ASCII.GetBytes(respString);
                Console.WriteLine("<response length:" + response.Length + " response: " + response + ">");
                clientStream.Write(response, 0, respString.Length);
            }

            tcpClient.Close();
        }
    }
}
