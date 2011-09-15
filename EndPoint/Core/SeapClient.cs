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

namespace MyDLP.EndPoint.Core
{
    class SeapClient
    {
        static SeapClient seapClient = null;
        Int32 port = 4000;
        String server = "127.0.0.1";
        TcpClient client;
        NetworkStream stream;
        Int32 responseLength = 256;


        public static FileOperation.Action GetWriteDecisionByPath(String filePath)
        {
            SeapClient sClient = SeapClient.GetInstance();
            String response = sClient.sendMessage("WRITE PATH " + filePath);
            Console.WriteLine("Response:" + response);
            if (response.Contains("ALLOW"))
            {
                return FileOperation.Action.ALLOW;
            }
            else if (response.Contains("BLOCK"))
            {
                return FileOperation.Action.BLOCK;
            }
            else if (response.Contains("NOACTION"))
            {
                return FileOperation.Action.NOACTION;
            }
            //todo: Default Acion
            return FileOperation.Action.ALLOW;
        }

        public static FileOperation.Action GetReadDecisionByPath(String filePath)
        {
            SeapClient sClient = SeapClient.GetInstance();
            String response = sClient.sendMessage("READ PATH " + filePath);
            Console.WriteLine("Response:" + response);
            if (response.Contains("ALLOW"))
            {
                return FileOperation.Action.ALLOW;
            }
            else if (response.Contains("BLOCK"))
            {
                return FileOperation.Action.BLOCK;
            }
            else if (response.Contains("NOACTION"))
            {
                return FileOperation.Action.NOACTION;
            }
            //todo: Default Acion
            return FileOperation.Action.ALLOW;
        }

        private SeapClient()
        {
            try
            {
                client = new TcpClient(server, port);
                stream = client.GetStream();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static SeapClient GetInstance()
        {
            try
            {
                if (seapClient == null)
                {
                    seapClient = new SeapClient();
                }
                return seapClient;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public byte[] sendMessage(byte[] msg)
        {
            try
            {
                Byte[] response = new Byte[responseLength];
                stream.Write(msg, 0, msg.Length);
                stream.Read(response, 0, responseLength);
                return response;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public String sendMessage(String msg)
        {
            try
            {
                Byte[] data = System.Text.Encoding.ASCII.GetBytes(msg);
                Byte[] response = new Byte[responseLength];

                Console.WriteLine("data length: " + data.Length + " data: " + data.Length);
                stream.Write(data, 0, msg.Length);

                int readCount = stream.Read(response, 0, responseLength);
                String respMessage = System.Text.Encoding.ASCII.GetString(response, 0, readCount);
                Console.WriteLine("< resp length: " + respMessage.Length + " resp:" + respMessage + ">");
                return respMessage;
            }
            catch (Exception)
            {
                throw;
            }
        }

    }
}
