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
    public class SessionServer
    {
        public static bool stopFlag = false;
        public static Hashtable socketTable;
        
        private static TcpListener tcpListener = null;
        private static Thread listenThread;

        public static void Start()
        {
            try
            {
                Logger.GetInstance().Debug("Started Session Server");               
                listenThread = new Thread(new ThreadStart(ListenConnections));
                listenThread.Start();
                socketTable = new Hashtable();

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
                    Logger.GetInstance().Error("Unable to add Notification Agent:" + e);
                }
            }
            catch (Exception e)
            {
                Logger.GetInstance().Error("Unable to start session server :" + e);
            }
        }


        public static void Stop()
        {
            try
            {
                tcpListener.Server.Close();
                tcpListener.Stop();
                stopFlag = true;
                closeAllSockets();
            }
            catch (Exception e)
            {
                Logger.GetInstance().Error("Error at SessionServer stop:" + e);
            }
        }
        
        protected static void closeSocket(TcpClient client) 
        {
            if (client == null) return;
            if (socketTable == null) return;
            if (!socketTable.Contains(client)) return;

            try
            {
                client.Close();
            }
            catch (Exception e)
            {
                Logger.GetInstance().Error("Error at client socket close:" + e);
            }
            finally
            {
                socketTable.Remove(client);
            }
        }

        protected static void closeAllSockets()
        {
            if (socketTable == null) return;

            foreach (TcpClient client in socketTable.Keys)
            {
                try
                {
                    client.Close();
                }
                catch (Exception e)
                {
                    Logger.GetInstance().Error("Error at client socket close:" + e);
                }
            }

            socketTable = new Hashtable();
        }

        private static void ListenConnections()
        {
            int errorCount = 0;
            while (!stopFlag && errorCount < 5)
            {
                try
                {
                    try
                    {
                        if (tcpListener != null) 
                        {
                            tcpListener.Server.Close();
                            tcpListener.Stop(); 
                            closeAllSockets();                                                    
                        }
                    }
                    catch (Exception e){
                        Logger.GetInstance().Error("Try to close previous listener error:" + e);
                    }

                    tcpListener = new TcpListener(IPAddress.Parse("127.0.0.1"), 9098);
                    tcpListener.Start();

                    while (!stopFlag)
                    {
                        TcpClient client = null;
                        try
                        {
                            client = tcpListener.AcceptTcpClient();                            
                        }
                        catch
                        {
                            throw;
                        }

                        try
                        {
                            socketTable.Add(client, null);
                            Logger.GetInstance().Debug("Hashcode client:" + client.GetHashCode());
                            Thread clientThread = new Thread(new ParameterizedThreadStart(HandleClient));
                            clientThread.Start(client);
                        }
                        catch (Exception e)
                        {
                            Logger.GetInstance().Error("SessionServer ListenClient error:" + e);
                            closeSocket(client);
                        }
                    }
                }
                catch (Exception e)
                {
                    errorCount++;
                    closeAllSockets();
                    Logger.GetInstance().Error("SessionServer ListenClient, restart listener:" + e);
                }
            }
            if (errorCount > 5) 
            {
                Logger.GetInstance().Error("SessionServer ListenClient, stopped listener due to too much error"); 
            }
        }

        private static void HandleClient(object client)
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

                            if (!DiskCryptor.DoesDriveLetterNeedsFormatting(driveLetter) || !Configuration.RemovableStorageEncryption)
                            {
                                WriteMessage(writer, "OK NOFORMAT");

                            }
                            else
                            {
                                WriteMessage(writer, "OK NEEDFORMAT");
                            }
                    }
                    else if (request.StartsWith("FORMAT"))
                    {
                        driveLetter = request.Split(' ')[1];
                        format = request.Split(' ')[2];
                        DiskCryptor.FormatDriveLetter(driveLetter, format);
                        WriteMessage(writer, "OK FINISHED");
                    }
                    else 
                    {
                        Logger.GetInstance().Error("SessionServer HandleClient invalid request" + request);
                        throw new InvalidRequestException("Expected valid request received:" + request);                        
                    }
                }

                catch (InvalidRequestException e)
                {
                    WriteMessage(writer, "ERROR message:" + e);
                    reader.DiscardBufferedData();
                    Logger.GetInstance().Error("SessionServer HandleClient error:" + e);
                }

                catch (Exception e)
                {                   
                    Logger.GetInstance().Error("SessionServer HandleClient error:" + e);
                    break;
                }                
            }
            
            SessionServer.closeSocket(tcpClient);
        }


        private static String ReadMessage(StreamReader reader)
        {
            try
            {
                String message = "";

                message = reader.ReadLine().Trim();
                Logger.GetInstance().Debug("ReadMessage <" + message + ">");

                return message;
            }
            catch 
            {
                throw;
            }
        }

        private static void WriteMessage(StreamWriter writer, String message)
        {
            try
            {
                Logger.GetInstance().Debug("WriteMessage <" + message + ">");
                writer.WriteLine(message);
                writer.Flush();
            }
            catch 
            {
                throw;
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
