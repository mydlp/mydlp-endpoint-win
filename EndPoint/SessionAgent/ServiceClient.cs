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
using System.IO;

namespace MyDLP.EndPoint.SessionAgent
{
    public class ServiceClient
    {
        static ServiceClient serviceClient = null;
        int port = 9098;
        TcpClient client;
        NetworkStream stream;
        StreamReader reader;
        StreamWriter writer;
        BinaryWriter binaryWriter;

        
        public static bool ServiceConnectionTest()
        {
            try
            {
                ServiceClient sClient = ServiceClient.GetInstance();
                String response;                
                response = sClient.sendMessage("BEGIN");

            }

            catch (Exception e)
            {
                //Logger.GetInstance().Debug(e.Message);
                return false;
            }
            return true;
        }

        private ServiceClient()
        {
            try
            {
                //Logger.GetInstance().Info("Initialize local service client port: " + port);
                client = new TcpClient("localhost", port);
                stream = client.GetStream();
                reader = new StreamReader(stream, System.Text.Encoding.ASCII);
                writer = new StreamWriter(stream, System.Text.Encoding.ASCII);
                binaryWriter = new BinaryWriter(stream);
            }
            catch (Exception)
            {
                throw;
            }
        }

        private void Reconnect()
        {
            try
            {
                //Logger.GetInstance().Debug("Reconnect seap client server: " + server + " port: " + port);
                try
                {
                    client.Close();
                }
                catch (Exception e)
                {
                   //todo
                }

                client = new TcpClient("localhost", port);
                stream = client.GetStream();
                reader = new StreamReader(stream, System.Text.Encoding.ASCII);
                writer = new StreamWriter(stream, System.Text.Encoding.ASCII);
                binaryWriter = new BinaryWriter(stream);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static ServiceClient GetInstance()
        {
            try
            {
                if (serviceClient == null)
                {
                    serviceClient = new ServiceClient();
                }
                return serviceClient;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public String sendMessage(String cmd, MemoryStream msg)
        {
            int tryCount = 0;
            int tryLimit = 3;
            Exception lastException = new Exception("Unknown exception");
            String respMessage = "";

            lock (serviceClient)
            {
                while (tryCount < tryLimit)
                {
                    try
                    {
                        //clean and discard data on sockect if any
                        if (stream.DataAvailable == true)
                            reader.ReadToEnd();

                        writer.WriteLine(cmd);
                        writer.Flush();

                        if (msg != null)
                        {
                            binaryWriter.Write(msg.GetBuffer(), 0, (int)msg.Length);
                            binaryWriter.Flush();
                        }

                        respMessage = reader.ReadLine().Trim();

                        tryCount = 0;
                        break;
                    }
                    catch (Exception e)
                    {
                        //Logger.GetInstance().Debug("IO Exception tryCount:" + tryCount);
                        lastException = e;
                        tryCount = handleStreamError(tryCount);
                    }
                }

                if (tryCount >= tryLimit)
                {
                    throw lastException;
                }
            }

            //Logger.GetInstance().Debug("SeapClient read response:  <" + respMessage + ">");
            return respMessage;
        }

        public String sendMessage(String msg)
        {
            String respMessage = "";
            try
            {
                respMessage = sendMessage(msg, null);
            }
            catch
            {
                throw;
            }

            return respMessage;
        }


        public int handleStreamError(int tryCount)
        {
            try
            {
                //consume &discard error message if any
                if (stream.DataAvailable)
                {
                    reader.ReadToEnd();
                }
            }
            catch
            {
                //Logger.GetInstance().Debug("SeapClient discard not possible");
            }
            try
            {
                tryCount++;
                Reconnect();
            }
            catch
            {
                //Logger.GetInstance().Debug("Reconnect failed");
            }
            return tryCount;
        }
    }
}
