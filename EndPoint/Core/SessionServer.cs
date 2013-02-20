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

namespace MyDLP.EndPoint.Core
{
    public class SessionServer
    {
        public static bool stopFlag = false;
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
            stopFlag = true;
        }

        private TcpListener tcpListener;
        private Thread listenThread;
        private static SessionServer sessionServer = null;


        private SessionServer()
        {
            Logger.GetInstance().Debug("Started Session Server");
            this.tcpListener = new TcpListener(IPAddress.Parse("127.0.0.1"), 9098);
            this.listenThread = new Thread(new ThreadStart(ListenConnections));
            this.listenThread.Start();
        }
        private void ListenConnections()
        {
            try
            {
                this.tcpListener.Start();

                while (!stopFlag)
                {
                    try
                    {
                        TcpClient client = this.tcpListener.AcceptTcpClient();

                        Thread clientThread = new Thread(new ParameterizedThreadStart(HandleClient));
                        clientThread.Start(client);
                    }
                    catch (Exception e)
                    {
                        Logger.GetInstance().Error("SessionServer ListenClient error:" + e.Message + e.StackTrace);
                    }
                }
            }
            catch(Exception e)
            {
                Logger.GetInstance().Error("SessionServer ListenClient, stop listener:" + e.Message + e.StackTrace);                
            }
        }

        private void HandleClient(object client)
        {
            TcpClient tcpClient = (TcpClient)client;
            NetworkStream clientStream = tcpClient.GetStream();
            StreamReader reader = new StreamReader(clientStream, System.Text.Encoding.ASCII);
            StreamWriter writer = new StreamWriter(clientStream, System.Text.Encoding.ASCII);

            String request;
            String driveLetter;
 
            String format;

            while (!stopFlag)
            {
                try
                {
                    request = ReadMessage(reader);
                    if (request.StartsWith("BEGIN"))
                    {
                        WriteMessage(writer, "OK");
                    }

                    else if (request.StartsWith("HASKEY"))
                    {
                        if (Configuration.HasEncryptionKey)
                        {
                            WriteMessage(writer, "OK YES");
                        }
                        else
                        {
                            WriteMessage(writer, "OK NO");
                        }
                    }

                    else if (request.StartsWith("NEWVOLUME"))
                    {

                        driveLetter = request.Split(' ')[1];

                        if (!DiskCryptor.DoesDriveLetterNeedsFormatting(driveLetter))
                        {
                            WriteMessage(writer, "OK NOFORMAT");
                            return;
                        }

                        WriteMessage(writer, "OK NEEDFORMAT");
                        request = ReadMessage(reader);

                        if (!request.StartsWith("FORMAT"))
                        {
                            tcpClient.Close();
                            throw new InvalidRequestException("Expected FORMAT recv:" + request);
                        }

                        driveLetter = request.Split(' ')[1];
                        format = request.Split(' ')[2];
                        DiskCryptor.FormatDriveLetter(driveLetter, format);

                        Thread.Sleep(21000);
                        WriteMessage(writer, "OK FINISHED");
                    }
                    else 
                    {
                        Logger.GetInstance().Error("Discarded buffer");
                        reader.DiscardBufferedData();
                    }
                }

                catch (InvalidRequestException e)
                {
                    WriteMessage(writer, "ERROR " + e.Message);
                    Logger.GetInstance().Error("SessionServer HandleClient error:" + e.Message + e.StackTrace);
                }

                catch (Exception e)
                {
                    Logger.GetInstance().Error("SessionServer HandleClient error:" + e.Message + e.StackTrace);
                    break;
                }
                
            }
        }


        private String ReadMessage(StreamReader reader)
        {
            String message = "";        
            
            message = reader.ReadLine().Trim();
            Logger.GetInstance().Debug("ReadMessage <" + message + ">");          

            return message;
        }

        private void WriteMessage(StreamWriter writer, String message)
        {
            try
            {
                Logger.GetInstance().Debug("WriteMessage <" + message + ">");
                writer.WriteLine(message + "\n");
                writer.Flush();
            }
            catch (Exception e)
            {
                Logger.GetInstance().Error("WriteMessage error: " + e.Message + e.StackTrace);
            }
        }

        public class InvalidRequestException : Exception
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
}
