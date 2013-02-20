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
using System.Windows.Forms;
using System.Threading;

namespace MyDLP.EndPoint.SessionAgent
{
    public class ServiceClient
    {
        //static ServiceClient serviceClient = null;
        int port = 9098;
        TcpClient client;
        NetworkStream stream;
        StreamReader reader;
        StreamWriter writer;

        public ServiceClient()
        {
            try
            {
                //Logger.GetInstance().Info("Initialize local service client port: " + port);               
                client = new TcpClient("localhost", port);
                stream = client.GetStream();
                reader = new StreamReader(stream, System.Text.Encoding.ASCII);
                writer = new StreamWriter(stream, System.Text.Encoding.ASCII);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public void Close()
        {
            reader.Dispose();
            writer.Dispose();
            stream.Dispose();
            client.Close();
        }

        public void Reconnect()
        {
            try
            {
                //MessageBox.Show("Reconnect seap client server");
                try
                {
                    reader.Dispose();
                    writer.Dispose();
                    stream.Dispose();
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
            }
            catch (Exception e)
            {
                //MessageBox.Show(e.Message + e.StackTrace);
                throw;
            }
        }

        public String sendMessage(String cmd)
        {
            String respMessage = null;

            lock (this)
            {
                int tryCount = 0;
                while (true)
                {
                    try
                    {
                        writer.WriteLine(cmd);
                        writer.Flush();

                        respMessage = reader.ReadLine();
                        respMessage.Trim();
                        break;
                    }
                    catch
                    {
                        if (tryCount >= 5)
                            throw;
                        else
                            tryCount++;
                    }
                }
            }
            return respMessage;
        }
    }
}
